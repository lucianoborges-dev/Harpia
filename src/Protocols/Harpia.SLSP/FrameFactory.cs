using System.Buffers;
using System.Security.Cryptography;

using Harpia.SLSP.Helpers;

namespace Harpia.SLSP;

public static class FrameFactory
{
	public static byte[] Create(byte[] key, byte deviceId, byte[] payload, bool encrypt)
	{
		if (payload.Length > byte.MaxValue)
			throw new ArgumentOutOfRangeException(nameof(payload), "Payload too large");

		int size = encrypt ? 5 + 12 + payload.Length + 16 : 5 + payload.Length + 1;
		byte[] buffer = new byte[size];
		Span<byte> span = buffer.AsSpan();

		span[0] = 0xAA; // Magic
		span[1] = 0x01; // Version
		span[2] = (byte)(encrypt ? 1 : 0);
		span[3] = deviceId;
		span[4] = (byte)payload.Length;

		if (encrypt)
		{
			var headerAad = span.Slice(0, 5);
			var nonce = span.Slice(5, 12); // 12-byte nonce
			var ciphertext = span.Slice(17, payload.Length);
			var tag = span.Slice(17 + payload.Length, 16);

			RandomNumberGenerator.Fill(nonce);
			using var aesGcm = new AesGcm(key, 16);
			aesGcm.Encrypt(nonce, payload, ciphertext, tag, headerAad);
		}
		else
		{
			payload.CopyTo(span.Slice(5));
			var crcTarget = span.Slice(0, span.Length - 1);
			span[^1] = Crc8.ComputeChecksum(crcTarget);
		}

		return buffer;
	}
}

