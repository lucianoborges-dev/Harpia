using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

using Harpia.SLSP;
using Harpia.SLSP.Models;

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
		_secureChannel.ControlFrameReceived += Channel_ControlFrameReceivedAsync;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

		Task readTask = ReadFromChannelAsync(cts.Token);
		Task fillTask = FillChannelAsync(cts.Token);
		Task writeTask = WriteToStreamAsync(cts.Token);

		await Task.WhenAll(readTask, fillTask, writeTask);
	}

	private readonly SessionManager _sessionManager = new();
	
	
	private async Task ReadFromChannelAsync(CancellationToken cancellationToken)
	{
		await foreach (UdpReceiveResult result in _channel.Reader.ReadAllAsync(cancellationToken))
		{
			if (!_sessionManager.TryGet(result.RemoteEndPoint, out SessionItem item))
			{
				_sessionManager.TryAdd(result.RemoteEndPoint, out byte deviceId);
			}
			
			// Try to get existing session
			if (!_sessions.TryGetValue(result.RemoteEndPoint, out SessionContext? session))
			{
				// Create new session if client not tracked yet
				session = new SessionContext(0x00, result.RemoteEndPoint);
				_sessions[result.RemoteEndPoint] = session;
			}

			// Pass buffer + session to SecureChannel
			_ = _secureChannel.RunParserAsync(result.Buffer, session, cancellationToken);
		}
	}


	private async Task FillChannelAsync(CancellationToken cancellationToken)
	{
		try
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				UdpReceiveResult result = await _client.ReceiveAsync(cancellationToken);
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

	private async Task Channel_PayloadReceivedAsync(FrameReceivedEventArgs args) => await Task.CompletedTask;
	private async Task Channel_ControlFrameReceivedAsync(FrameReceivedEventArgs args) => 
		await HandleControlFrameAsync(args);


	private async Task HandleControlFrameAsync(FrameReceivedEventArgs args)
	{
		//var context = args.Context;

		//switch (args.PayloadType)
		//{
		//	case PayloadType.ConnectionRequest:
		//		context.State = ConnectionState.ConnRequested;
		//		// Server should respond with IdentityRequest
		//		break;

		//	case PayloadType.IdentityRequest:
		//		context.State = ConnectionState.AwaitingIdentity;
		//		break;

		//	case PayloadType.IdentityResponse:
		//		context.State = ConnectionState.IdentityReceived;
		//		context.Identity = Encoding.ASCII.GetString(args.Payload);
		//		break;

		//	case PayloadType.AesKey:
		//		context.AesKey = context.Handshake!.DeriveFinalKey(args.Payload);
		//		context.State = ConnectionState.KeyExchange;
		//		break;

		//	case PayloadType.ConnectionAck:
		//		context.State = ConnectionState.Established;
		//		break;

		//	case PayloadType.Heartbeat:
		//		context.State = ConnectionState.Active;
		//		context.LastHeartbeat = DateTime.UtcNow;
		//		break;

		//	case PayloadType.Rejected:
		//		context.State = ConnectionState.Disconnected;
		//		break;

		//	case PayloadType.Bridge:
		//		// Exemplo: encaminhar payload para outro cliente
		//		break;
		//}

		await Task.CompletedTask;
	}
}


public sealed class SessionItem
{
	public byte DeviceId { get; init; }
	public TcpClient? TcpClient { get; init; }
	public IPEndPoint? UdpEndpoint { get; init; }

	public byte[]? AesKey { get; set; }
	public HandshakeManager Handshake { get; } = new();
}

public sealed class SessionManager
{
	private readonly ConcurrentDictionary<byte, SessionItem> _items = new();

	private static bool TryGetNextAvailableId(ICollection<byte> existingIds, out byte deviceId)
	{
		for (byte id = 1; id < byte.MaxValue - 1; id++)
		{
			if (!existingIds.Contains(id))
			{
				deviceId = id;
				return true;
			}
		}
		deviceId = 0;
		return false;
	}

	public bool TryAdd(TcpClient client, out byte deviceId)
	{
		if (TryGetNextAvailableId(_items.Keys, out deviceId))
		{
			var item = new SessionItem { TcpClient = client };
			return _items.TryAdd(deviceId, item);
		}
		return false;
	}

	public bool TryAdd(IPEndPoint endpoint, out byte deviceId)
	{
		if (TryGetNextAvailableId(_items.Keys, out deviceId))
		{
			var item = new SessionItem { UdpEndpoint = endpoint };
			return _items.TryAdd(deviceId, item);
		}
		return false;
	}

	public bool TryGet(IPEndPoint endpoint, out SessionItem item)
	{
		item = _items.FirstOrDefault(x => Equals(x.Value.UdpEndpoint, endpoint)).Value;
		return item != null;
	}

	public bool TryGet(byte id, out SessionItem? value) =>
		_items.TryGetValue(id, out value);

	public bool TryRemove(byte id, out SessionItem? value) =>
		_items.TryRemove(id, out value);

	public IEnumerable<SessionItem> GetAll() => _items.Values;

	public byte[] GetAllDeviceIds() => _items.Keys.ToArray();
}
