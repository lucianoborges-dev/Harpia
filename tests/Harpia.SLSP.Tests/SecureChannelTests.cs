//using Harpia.SLSP.Models;

//namespace Harpia.SLSP.Tests;

//public class SecureChannelTests
//{
//	private readonly SecureChannel _secureChannel = new();

//	[Fact]
//	public async Task Should_Raise_KeyEstablished_When_Handshake_Frame_Received()
//	{
//		// Arrange
//		var session = new SessionContext(0x01, "test-client");
//		bool eventRaised = false;

//		_secureChannel.KeyEstablished += async args =>
//		{
//			eventRaised = true;
//			await Task.CompletedTask;
//		};

//		// Act
//		byte[] fakeHandshakePayload = session.Handshake.MyPublicKey;
//		await _secureChannel.RunParserAsync(
//			FrameFactory.Create(session.Handshake.MyPublicKey, 0xFF, fakeHandshakePayload, false),
//			session,
//			CancellationToken.None);

//		// Assert
//		Assert.True(eventRaised);
//		Assert.NotNull(session.AesKey);
//		Assert.Equal(32, session.AesKey!.Length);
//	}

//	[Theory]
//	[InlineData("HEARTBEAT", true, false)]
//	[InlineData("PAYLOAD", false, true)]
//	public async Task Should_Raise_Correct_Event_Based_On_Payload(string payload, bool expectHeartbeat, bool expectPayload)
//	{
//		var session = new SessionContext(0x02, "test-client");
//		bool heartbeatRaised = false;
//		bool payloadRaised = false;

//		_secureChannel.HeartbeatReceived += async args =>
//		{
//			heartbeatRaised = true;
//			await Task.CompletedTask;
//		};

//		_secureChannel.PayloadReceived += async args =>
//		{
//			payloadRaised = true;
//			await Task.CompletedTask;
//		};

//		await _secureChannel.RunParserAsync(
//			FrameFactory.Create(session.AesKey ?? new byte[32], session.DeviceId, System.Text.Encoding.ASCII.GetBytes(payload), false),
//			session,
//			CancellationToken.None);

//		Assert.Equal(expectHeartbeat, heartbeatRaised);
//		Assert.Equal(expectPayload, payloadRaised);
//	}
//}
