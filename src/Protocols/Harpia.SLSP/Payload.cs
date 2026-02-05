using System.Text;

namespace Harpia.SLSP;

public readonly struct Payload
{
	// Public const strings for frame building
	public const string ConnectRequest = "CONNECT_REQUEST";
	public const string IdentityRequest = "IDENTITY_REQUEST";
	public const string IdentityResponse = "IDENTITY_RESPONSE";
	public const string ConnectionAck = "CONNECTION_ACK";
	public const string Rejected = "REJECTED";
	public const string Bridge = "BRIDGE";
	public const string Heartbeat = "HEARTBEAT";
	public const string AesKey = "AESKEY";

	private static readonly byte[] ConnectRequestBytes = Encoding.ASCII.GetBytes(ConnectRequest);
	private static readonly byte[] IdentityRequestBytes = Encoding.ASCII.GetBytes(IdentityRequest);
	private static readonly byte[] IdentityResponseBytes = Encoding.ASCII.GetBytes(IdentityResponse);
	private static readonly byte[] ConnectionAckBytes = Encoding.ASCII.GetBytes(ConnectionAck);
	private static readonly byte[] RejectedBytes = Encoding.ASCII.GetBytes(Rejected);
	private static readonly byte[] BridgeBytes = Encoding.ASCII.GetBytes(Bridge);
	private static readonly byte[] AesKeyBytes = Encoding.ASCII.GetBytes(AesKey);
	private static readonly byte[] HeartbeatBytes = Encoding.ASCII.GetBytes(Heartbeat);

	public Payload(PayloadType payloadType, byte[] data)
	{
		PayloadType = payloadType;
		Data = data;
	}

	public Payload(ReadOnlySpan<byte> frame)
	{
		Data = [];

		if (frame.SequenceEqual(ConnectRequestBytes))
		{
			PayloadType = PayloadType.ConnectionRequest;
		}
		else if (frame.SequenceEqual(IdentityRequestBytes))
		{
			PayloadType = PayloadType.IdentityRequest;
		}
		else if (frame.StartsWith(IdentityResponseBytes))
		{
			PayloadType = PayloadType.IdentityResponse;
			Data = ReadData(frame, IdentityResponseBytes);
		}
		else if (frame.SequenceEqual(ConnectionAckBytes))
		{
			PayloadType = PayloadType.ConnectionAck;
		}
		else if (frame.SequenceEqual(RejectedBytes))
		{
			PayloadType = PayloadType.Rejected;
		}
		else if (frame.SequenceEqual(BridgeBytes))
		{
			PayloadType = PayloadType.Bridge;
			Data = ReadData(frame, BridgeBytes);
		}
		else if (frame.SequenceEqual(HeartbeatBytes))
		{
			PayloadType = PayloadType.Heartbeat;
		}
		else if (frame.StartsWith(AesKeyBytes))
		{
			PayloadType = PayloadType.AesKey;
			Data = ReadData(frame, AesKeyBytes);
		}
		else
		{
			PayloadType = PayloadType.Payload;
		}
	}

	// Same as string.trim()
	private static byte[] ReadData(ReadOnlySpan<byte> frame, byte[] skip)
	{
		ReadOnlySpan<byte> whitespace = " \t\r\n"u8;
		return frame[skip.Length..].Trim(whitespace).ToArray();
	}


	public PayloadType PayloadType { get; }
	public byte[] Data { get; }
}
