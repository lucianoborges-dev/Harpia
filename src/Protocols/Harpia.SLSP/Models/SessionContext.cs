using System.Net.Sockets;

namespace Harpia.SLSP.Models;

public class SessionContext(byte deviceId, object transportContext)
{
	public byte DeviceId { get; set; } = deviceId;
	public byte[]? AesKey { get; set; }
	public HandshakeManager Handshake { get; } = new();

	// TcpClient or IPEndPoint
	public object TransportContext { get; } = transportContext;
}
