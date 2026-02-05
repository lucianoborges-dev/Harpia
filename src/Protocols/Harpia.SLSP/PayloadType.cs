namespace Harpia.SLSP;

public enum PayloadType
{
	ConnectionRequest,
	IdentityRequest,
	IdentityResponse,
	ConnectionAck,
	Rejected,
	Bridge,
	Heartbeat,
	AesKey,
	Payload
}
