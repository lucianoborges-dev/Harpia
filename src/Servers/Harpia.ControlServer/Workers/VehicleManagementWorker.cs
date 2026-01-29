using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

using Harpia.SLSP;

namespace Harpia.ControlServer.Workers;

public class VehicleManagementWorker : BackgroundService
{
	// Dictionary to track sessions per client
	private readonly Dictionary<IPEndPoint, SessionContext> _sessions = [];

	private readonly UdpClient _client = new(11000);

	private readonly Channel<UdpReceiveResult> _channel;

	private readonly SecureChannel _secureChannel;

	public VehicleManagementWorker()
	{
		_channel = Channel.CreateBounded<UdpReceiveResult>(
			new BoundedChannelOptions(10000) { FullMode = BoundedChannelFullMode.Wait });

		_secureChannel = new SecureChannel();
		_secureChannel.PayloadReceived += Channel_PayloadReceivedAsync;
		_secureChannel.KeyEstablished += Channel_KeyEstablishedAsync;
		_secureChannel.HeartbeatReceived += Channel_HeartbeatReceivedAsync;
	}


	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

		var readTask = ReadFromChannelAsync(cts.Token);
		var fillTask = FillChannelAsync(cts.Token);
		var writeTask = WriteToStreamAsync(cts.Token);

		await Task.WhenAll(readTask, fillTask, writeTask);
	}

	private async Task ReadFromChannelAsync(CancellationToken cancellationToken)
	{
		await foreach (var result in _channel.Reader.ReadAllAsync(cancellationToken))
		{
			// Try to get existing session
			if (!_sessions.TryGetValue(result.RemoteEndPoint, out var session))
			{
				// Create new session if client not tracked yet
				session = new SessionContext(0x00, result.RemoteEndPoint);
				_sessions[result.RemoteEndPoint] = session;
			}

			// Pass buffer + session to SecureChannel
			await _secureChannel.RunParserAsync(result.Buffer, session, cancellationToken);
		}
	}


	private async Task FillChannelAsync(CancellationToken cancellationToken)
	{
		try
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				var result = await _client.ReceiveAsync(cancellationToken);
				await _channel.Writer.WriteAsync(result, cancellationToken);
			}
		}
		catch (OperationCanceledException)
		{
			// expected at shutdown
		}
		finally
		{
			_channel.Writer.Complete();
		}
	}

	private async Task WriteToStreamAsync(CancellationToken token)
	{
		//await foreach (object cmdInfo in _channels.Read(token))
		//{
		//    // recebe do outro worker e escreve no stream upd/tcp
		//}
	}

	private async Task Channel_PayloadReceivedAsync(SLSP.FrameReceivedEventArgs arg) => await Task.CompletedTask;
	private async Task Channel_KeyEstablishedAsync(SLSP.FrameReceivedEventArgs arg) => await Task.CompletedTask;
	private async Task Channel_HeartbeatReceivedAsync(SLSP.FrameReceivedEventArgs arg) => await Task.CompletedTask;
}


