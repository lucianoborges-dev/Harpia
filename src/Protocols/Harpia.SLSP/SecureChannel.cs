using System.Buffers;
using System.Security.Cryptography;
using System.Text;

using Harpia.SLSP.Helpers;

namespace Harpia.SLSP;

public class SessionContext(byte deviceId, object transportContext)
{
	public byte DeviceId { get; set; } = deviceId;
	public byte[]? AesKey { get; set; }
	public HandshakeManager Handshake { get; } = new();
	public object TransportContext { get; } = transportContext;
}

/// <summary>
/// Arguments for received frame events.
/// </summary>
public class FrameReceivedEventArgs : EventArgs
{
	public byte DeviceId { get; }
	public byte[] Payload { get; }
	public object TransportContext { get; }

	public FrameReceivedEventArgs(byte deviceId, byte[] payload, object transportContext)
	{
		DeviceId = deviceId;
		Payload = payload;
		TransportContext = transportContext;
	}
}

public class SecureChannel : ISecureChannel
{
	// Async events
	public event Func<FrameReceivedEventArgs, Task>? HeartbeatReceived;
	public event Func<FrameReceivedEventArgs, Task>? PayloadReceived;
	public event Func<FrameReceivedEventArgs, Task>? KeyEstablished;

	private static FrameReceivedEventArgs CreateArgs(byte deviceId, byte[] payload, SessionContext context)
		=> new(deviceId, payload, context.TransportContext);

	private async Task OnHeartbeatReceived(byte deviceId, byte[] payload, SessionContext context)
	{
		if (HeartbeatReceived is not null)
			await HeartbeatReceived(CreateArgs(deviceId, payload, context));
	}

	private async Task OnPayloadReceived(byte deviceId, byte[] payload, SessionContext context)
	{
		if (PayloadReceived is not null)
			await PayloadReceived(CreateArgs(deviceId, payload, context));
	}

	private async Task OnKeyEstablished(byte[] payload, SessionContext context)
	{
		if (KeyEstablished is not null)
			await KeyEstablished(CreateArgs(0xFF, payload, context));
	}

	private static readonly byte[] HeartbeatBytes = Encoding.ASCII.GetBytes("HEARTBEAT");

	private async Task NotifyFrameReceived(byte deviceId, byte[] payload, SessionContext context)
	{
		if (payload.SequenceEqual(HeartbeatBytes))
			await OnHeartbeatReceived(deviceId, payload, context);
		else
			await OnPayloadReceived(deviceId, payload, context);
	}

	public async Task RunParserAsync(byte[] bytes, SessionContext context, CancellationToken ct)
	{
		var buffer = new ReadOnlySequence<byte>(bytes);

		while (true)
		{
			if (buffer.Length < 5) break;

			var reader = new SequenceReader<byte>(buffer);
			reader.Advance(2); // Skip Magic/Version
			reader.TryRead(out byte isEncrypted);
			reader.Advance(1); // Skip DeviceID
			reader.TryRead(out byte payloadLen);

			int totalSize = isEncrypted == 1
				? 5 + 12 + payloadLen + 16
				: 5 + payloadLen + 1;

			if (buffer.Length < totalSize) break;

			var frame = buffer.Slice(0, totalSize);
			await ProcessFrameAsync(frame, context);
			buffer = buffer.Slice(totalSize);
		}
	}

	private async Task ProcessFrameAsync(ReadOnlySequence<byte> frame, SessionContext context)
	{
		Span<byte> span = stackalloc byte[(int)frame.Length];
		frame.CopyTo(span);

		bool isEncrypted = span[2] == 1;
		byte deviceId = span[3];
		byte payloadLen = span[4];

		if (isEncrypted)
		{
			if (context.AesKey == null) return;

			var headerAad = span.Slice(0, 5);
			var nonce = span.Slice(5, 12);
			var ciphertext = span.Slice(17, payloadLen);
			var tag = span.Slice(17 + payloadLen, 16);

			Span<byte> decrypted = stackalloc byte[payloadLen];
			using var aesGcm = new AesGcm(context.AesKey, 16);
			try
			{
				aesGcm.Decrypt(nonce, ciphertext, tag, decrypted, headerAad);
				await NotifyFrameReceived(deviceId, decrypted.ToArray(), context);
			}
			catch (CryptographicException)
			{
				// Log: tampered packet
			}
		}
		else
		{
			var checksum = span[^1];
			var crc8 = Crc8.ComputeChecksum(span[..^1]);
			if (checksum != crc8) return;

			var payload = span.Slice(5, payloadLen);

			if (deviceId == 0xFF && context.Handshake != null)
			{
				context.AesKey = context.Handshake.DeriveFinalKey(payload.ToArray());
				await OnKeyEstablished(context.AesKey, context);
			}
			else if (context.AesKey == null)
			{
				await NotifyFrameReceived(deviceId, payload.ToArray(), context);
			}
		}
	}
}
