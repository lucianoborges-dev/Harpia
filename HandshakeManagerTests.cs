using Xunit;
using Harpia.SLSP;

namespace Harpia.SLSP.Tests;
{
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
}


	public class FrameFactoryTests
	{
		[Fact]
		public void Should_Create_Plaintext_Frame_With_Correct_CRC()
		{
			byte[] payload = System.Text.Encoding.ASCII.GetBytes("TEST");
			byte[] frame = FrameFactory.Create(new byte[32], 0x01, payload, false);

			Assert.Equal(0xAA, frame[0]); // Magic
			Assert.Equal(0x01, frame[1]); // Version
			Assert.Equal(payload.Length, frame[4]); // PayloadLen
		}

		[Fact]
		public void Should_Create_Encrypted_Frame()
		{
			byte[] key = new byte[32];
			System.Security.Cryptography.RandomNumberGenerator.Fill(key);

			byte[] payload = System.Text.Encoding.ASCII.GetBytes("SECRET");
			byte[] frame = FrameFactory.Create(key, 0x01, payload, true);

			Assert.Equal(0xAA, frame[0]);
			Assert.Equal(1, frame[2]); // isEncrypted flag
		}
	}
