namespace Harpia.SLSP.Models;

public class FrameReceivedEventArgs(byte deviceId, Payload payload, object transportContext) : EventArgs
{
	public byte DeviceId { get; } = deviceId;
	public Payload Payload { get; } = payload;
	public object TransportContext { get; } = transportContext;
}
