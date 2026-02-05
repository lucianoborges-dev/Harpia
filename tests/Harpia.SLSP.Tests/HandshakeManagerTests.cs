namespace Harpia.SLSP.Tests;

public class HandshakeManagerTests
{
	[Fact]
	public void Should_Derive_Same_Key_On_Both_Sides()
	{
		using var server = new HandshakeManager();
		using var client = new HandshakeManager();

		var serverKey = server.DeriveFinalKey(client.MyPublicKey);
		var clientKey = client.DeriveFinalKey(server.MyPublicKey);

		Assert.Equal(serverKey, clientKey);
	}
}
