using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using Harpia.SLSP.Helpers;
using Harpia.SLSP.Models;

namespace Harpia.SLSP;


public class SecureChannel : ISecureChannel
{
	// Async events
	public event Func<FrameReceivedEventArgs, Task>? PayloadReceived;
	public event Func<FrameReceivedEventArgs, Task>? ControlFrameReceived;

	public async Task RunParserAsync(byte[] bytes, SessionContext context, CancellationToken cancellationToken)
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

		// Guards
		if (span[0] != Constants.ProtocolHeader) return;
		if (span[1] != Constants.CurrentProtocolVersion) return;
		if (span[2] is not 0 or 1) return;

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
				var payload = new Payload(decrypted);
				await NotifyFrameReceivedAsync(deviceId, payload, context);
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

			var payload = new Payload(span.Slice(5, payloadLen));
			await NotifyFrameReceivedAsync(deviceId, payload, context);
		}
	}

	private async Task NotifyFrameReceivedAsync(byte deviceId, Payload payload, SessionContext context)
	{
		var args = new FrameReceivedEventArgs(deviceId, payload, context);

		if (payload.PayloadType == PayloadType.Payload)
		{
			await PayloadReceived?.Invoke(args)!;
		}
		else
		{
			await ControlFrameReceived?.Invoke(args)!;
		}
	}
}
