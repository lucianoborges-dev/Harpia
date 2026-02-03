namespace Harpia.SLSP;

using Harpia.SLSP.Helpers;

public class Frame
{
	public byte Header { get; init; }
	public byte Version { get; init; }
	public bool Encrypted { get; init; }
	public byte DeviceId { get; init; }
	public byte PayloadLength { get; init; }
	public byte[] Payload { get; init; }
	public byte Checksum { get; set; }

	override public string ToString()
	{
		return $"Frame(DeviceId=0x{DeviceId:X2}, Encrypted={Encrypted}, PayloadLength={PayloadLength})";
	}
}

public static class FrameSerializer
{
	/// <summary>
	/// Gets the size of the serialized frame.
	/// Encrypted: Header(5) + Nonce(12) + PayloadLength + Tag(16)
	/// Not Encrypted: Header(5) + PayloadLength + Checksum(1)
	/// </summary>
	/// <returns></returns>
    private static int GetFrameSize(bool isEncrypted, byte payloadLength)
	{
		const int EncryptedOverhead = 33;
		const int UnencryptedOverhead = 6;

		return isEncrypted
			? EncryptedOverhead + payloadLength
			: UnencryptedOverhead + payloadLength;
	}

	public static byte[] Serialize(Frame frame, byte[] key = null)
	{
		int size = GetFrameSize(frame.Encrypted, frame.PayloadLength);
		byte[] buffer = new byte[size];
		Span<byte> span = buffer.AsSpan();

		span[0] = frame.Header;
		span[1] = frame.Version;
		span[2] = (byte)(frame.Encrypted ? 1 : 0);
		span[3] = frame.DeviceId;
		span[4] = frame.PayloadLength;
		if (frame.Encrypted)
		{
			var headerAad = span.Slice(0, 5);
			var nonce = span.Slice(5, 12); // 12-byte nonce
			var ciphertext = span.Slice(17, frame.PayloadLength);
			var tag = span.Slice(17 + frame.PayloadLength, 16);

			RandomNumberGenerator.Fill(nonce);
			using var aesGcm = new AesGcm(key, key.Length);
			aesGcm.Encrypt(nonce, frame.Payload, ciphertext, tag, headerAad);
		}
		else
		{
			frame.Payload.CopyTo(span.Slice(5));
			var crcTarget = span.Slice(0, span.Length - 1);
			span[^1] = Crc8.ComputeChecksum(crcTarget);
		}

		return buffer;
	}

	public static Frame Deserialize(ReadOnlySpan<byte> span, byte[] key = null)
	{
		byte header = span[0];
		byte version = span[1];
		bool isEncrypted = span[2] == 1;
		byte deviceId = span[3];
		byte payloadLen = span[4];

        // Move validations to builder
		if (header != 0x83)
			throw new ArgumentOutOfRangeException(nameof(header), "Invalid header byte");

		if (version != 0x01)
			throw new ArgumentOutOfRangeException(nameof(version), "Invalid version byte");

		if (deviceId == 0x00)
			throw new ArgumentOutOfRangeException(nameof(deviceId), "DeviceId cannot be 0x00");

		if (deviceId == 0xFF)
			throw new ArgumentOutOfRangeException(nameof(deviceId), "DeviceId cannot be 0xFF");

		if (payloadLen > byte.MaxValue)
			throw new ArgumentOutOfRangeException(nameof(payloadLen), "Payload too large");

        
        // The caller must ensure that the span length is sufficient
		// int size = GetFrameSize(isEncrypted, payloadLen);
		// if (span.Length < size)
		// 	throw new ArgumentOutOfRangeException(nameof(span), "Span length is less than expected frame size");

		byte[] payload;

		if (isEncrypted)
		{
			if (key == null || key.Length != 16)
				throw new ArgumentException("A valid 16-byte key must be provided for decryption.", nameof(key));

			var headerAad = span.Slice(0, 5);
			var nonce = span.Slice(5, 12);
			var ciphertext = span.Slice(17, payloadLen);
			var tag = span.Slice(17 + payloadLen, 16);

			payload = new byte[payloadLen];
			using var aesGcm = new AesGcm(key, key.Length);
			aesGcm.Decrypt(nonce, ciphertext, tag, payload, headerAad);
		}
		else
		{
			var checksum = span[^1];
			var crc8 = Crc8.ComputeChecksum(span[..^1]);
			if (checksum != crc8)
				throw new InvalidOperationException("Checksum validation failed.");

			payload = span.Slice(5, payloadLen).ToArray();
		}

        if (payload.Length != payloadLen)
			throw new ArgumentOutOfRangeException(nameof(payload), "Payload length does not match PayloadLength");
	
		return new SLSPFrame(deviceId, payload, key);
	}
}

public class FrameBuilder
{
	private byte header = 0x83;
	private byte version = 0x01;
	private bool encrypted = false;
	private byte deviceId;
	private byte[] payload = [];

	public FrameBuilder SetHeader(byte header)
	{
		this.header = header;
		return this;
	}

	public FrameBuilder SetVersion(byte version)
	{
		this.version = version;
		return this;
	}

	public FrameBuilder SetEncrypted(bool encrypted)
	{
		this.encrypted = encrypted;
		return this;
	}

	public FrameBuilder SetDeviceId(byte deviceId)
	{
		this.deviceId = deviceId;
		return this;
	}

	public FrameBuilder SetPayload(byte[] payload)
	{
		this.payload = payload;
		return this;
	}

	public Frame Build()
	{
		return new SLSPFrame(header, version, encrypted, deviceId, (byte)payload.Length, payload, 0);
	}
}


private async Task ProcessBufferAsync(byte[] result, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
	{
		// Try to get existing session
		if (!_sessions.TryGetValue(remoteEndPoint, out var session))
		{
			// Create new session if client not tracked yet
			session = new SessionContext(0x00, remoteEndPoint);
			_sessions[remoteEndPoint] = session;
		}

		var buffer = new System.Buffers.ReadOnlySequence<byte>(result);

		while (true)
		{
			if (buffer.Length < 5) break;

			var encrypted = buffer.First.Span[2];
			var payloadLen = buffer.First.Span[4];
			int totalSize = encrypted == 1
				? 5 + 12 + payloadLen + 16
				: 5 + payloadLen + 1;

			if (buffer.Length < totalSize) break;

			// var frame = buffer.Slice(0, totalSize);
			// await ProcessFrameAsync(frame, context);

			var frame = FrameSerializer.Deserialize(buffer.Slice(0, totalSize), session.AesKey);
			var payload = PayloadSerializer.Deserialize(frame.Payload);
			_ = ProcessPayloadAsync(payload, session, cancellationToken);
			
			// Advance buffer
			buffer = buffer.Slice(totalSize);
		}
	}
