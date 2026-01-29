namespace Harpia.SLSP.Models;

public record FrameReceivedEventArgs(
	byte DeviceId, 
	byte[] Payload, 
	object Context
);
