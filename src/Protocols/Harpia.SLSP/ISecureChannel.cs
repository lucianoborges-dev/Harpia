namespace Harpia.SLSP;

public interface ISecureChannel
{
	Task RunParserAsync(byte[] bytes, SessionContext context, CancellationToken ct);
}
