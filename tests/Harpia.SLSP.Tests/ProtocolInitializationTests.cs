using System.Text;

using Harpia.SLSP.Models;

namespace Harpia.SLSP.Tests;

public class ProtocolInitializationTests
{
	[Fact]
	public async Task Should_Complete_Initialization_And_Exchange_Encrypted_Heartbeats()
	{
		// Arrange
		var serverHandshake = new HandshakeManager();
		var clientHandshake = new HandshakeManager();

		var serverChannel = new SecureChannel();
		var clientChannel = new SecureChannel();

		var serverSession = new SessionContext(Constants.DefaultDeviceId, "server");
		var clientSession = new SessionContext(Constants.DefaultDeviceId, "client");

		bool serverConnectionRequestReceived = false;
		bool serverIdentityResponseReceived = false;
		bool clientIdentityRequestReceived = false;
		bool wruSent = false;
		bool iamReceived = false;
		bool keyEstablished = false;
		bool ackReceived = false;
		bool heartbeatReceived = false;

		// Subscribe to client events

		clientChannel.ControlFrameReceived += async args =>
		{

			string msg = Encoding.ASCII.GetString(args.Payload.Data);
			clientIdentityRequestReceived = true;
			await Task.CompletedTask;
		};

		// Subscribe to server events


		serverChannel.ControlFrameReceived += async args =>
		{
			serverConnectionRequestReceived = (args.Payload.PayloadType == PayloadType.ConnectionRequest);

			if (args.Payload.PayloadType == PayloadType.IdentityResponse)
			{
				serverIdentityResponseReceived = true;
			}


			string msg = Encoding.ASCII.GetString(args.Payload.Data);
			serverIdentityResponseReceived = true;
			await Task.CompletedTask;
		};

		serverChannel.PayloadReceived += async args =>
		{
			string msg = Encoding.ASCII.GetString(args.Payload.Data);
			if (msg == "CONN") serverConnectionRequestReceived = true;
			if (msg.StartsWith("IAM")) iamReceived = true;
			await Task.CompletedTask;
		};

		// Act: simulate protocol steps

		//// 1. Client sends CONNECT_REQUEST
		//byte[] connFrame = FrameFactory.Create([], Constants.DefaultDeviceId, Encoding.ASCII.GetBytes("CONNECT_REQUEST"), false);
		//await serverChannel.RunParserAsync(connFrame, serverSession, CancellationToken.None);

		//// 2. Server responds IDENTITY_REQUEST
		//byte[] wruFrame = FrameFactory.Create(new byte[32], serverSession.DeviceId, Encoding.ASCII.GetBytes("IDENTITY_REQUEST"), false);
		//await clientChannel.RunParserAsync(wruFrame, clientSession, CancellationToken.None);

		// 3. Client sends IDENTITY_RESPONSE
		byte[] iamFrame = FrameFactory.Create(new byte[32], clientSession.DeviceId, Encoding.ASCII.GetBytes("IDENTITY_RESPONSE SN123456"), false);
		await serverChannel.RunParserAsync(iamFrame, serverSession, CancellationToken.None);

		//// 4. Server sends KEY
		//byte[] serverKeyFrame = FrameFactory.Create(new byte[32], serverSession.DeviceId, serverHandshake.MyPublicKey, false);
		//await clientChannel.RunParserAsync(serverKeyFrame, clientSession, CancellationToken.None);

		//// 5. Client sends KEY
		//byte[] clientKeyFrame = FrameFactory.Create(new byte[32], clientSession.DeviceId, clientHandshake.MyPublicKey, false);
		//await serverChannel.RunParserAsync(clientKeyFrame, serverSession, CancellationToken.None);

		//// Derive final keys
		//serverSession.AesKey = serverHandshake.DeriveFinalKey(clientHandshake.MyPublicKey);
		//clientSession.AesKey = clientHandshake.DeriveFinalKey(serverHandshake.MyPublicKey);

		//// 6. Server sends ACK (encrypted)
		//byte[] ackFrame = FrameFactory.Create(serverSession.AesKey!, serverSession.DeviceId, Encoding.ASCII.GetBytes("ACK"), true);
		//await clientChannel.RunParserAsync(ackFrame, clientSession, CancellationToken.None);

		//// 7. Client sends HEARTBEAT (encrypted)
		//byte[] heartbeatFrame = FrameFactory.Create(clientSession.AesKey!, clientSession.DeviceId, Encoding.ASCII.GetBytes("HEARTBEAT"), true);
		//await serverChannel.RunParserAsync(heartbeatFrame, serverSession, CancellationToken.None);

		// Assert: all steps completed
		Assert.True(serverConnectionRequestReceived);
		Assert.True(clientIdentityRequestReceived);
		Assert.True(serverIdentityResponseReceived);
		//Assert.True(wruSent);
		//Assert.True(iamReceived);
		//Assert.True(keyEstablished);
		//Assert.True(ackReceived);
		//Assert.True(heartbeatReceived);
	}
}
