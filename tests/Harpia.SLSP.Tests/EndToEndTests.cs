//using System.Text;

//using Harpia.SLSP.Models;

//namespace Harpia.SLSP.Tests;

//public class EndToEndTests
//{
//	[Fact]
//	public async Task Client_And_Server_Should_Derive_Same_Key_And_Exchange_Encrypted_Frame()
//	{
//		// Arrange: create client and server handshake managers
//		using var serverHandshake = new HandshakeManager();
//		using var clientHandshake = new HandshakeManager();

//		// Exchange public keys
//		var serverPubKey = serverHandshake.MyPublicKey;
//		var clientPubKey = clientHandshake.MyPublicKey;

//		// Derive final keys
//		var serverKey = serverHandshake.DeriveFinalKey(clientPubKey);
//		var clientKey = clientHandshake.DeriveFinalKey(serverPubKey);

//		// Assert: both sides derived the same key
//		Assert.Equal(serverKey, clientKey);

//		// Create SecureChannel instances
//		var serverChannel = new SecureChannel();
//		var clientChannel = new SecureChannel();

//		var serverSession = new SessionContext(0x01, "server") { AesKey = serverKey };
//		var clientSession = new SessionContext(0x01, "client") { AesKey = clientKey };

//		string message = "Hello Secure World!";
//		byte[] payload = Encoding.UTF8.GetBytes(message);

//		bool clientReceived = false;

//		clientChannel.PayloadReceived += async args =>
//		{
//			string received = Encoding.UTF8.GetString(args.Payload);
//			Assert.Equal(message, received);
//			clientReceived = true;
//			await Task.CompletedTask;
//		};

//		// Act: server creates encrypted frame and client parses it
//		byte[] frame = FrameFactory.Create(serverKey, serverSession.DeviceId, payload, encrypt: true);

//		await clientChannel.RunParserAsync(frame, clientSession, CancellationToken.None);

//		// Assert: client successfully received and decrypted payload
//		Assert.True(clientReceived);
//	}
//}
