
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using Harpia.SLSP;

using UdpClient client = new(12000);
var server = IPEndPoint.Parse("127.0.0.1:11000");

byte[] key = new HandshakeManager().MyPublicKey;
var frame = FrameFactory.Create
	(
		key,
		0xf1,
		Encoding.ASCII.GetBytes("HEARTBEAT"),
		false
	);

Console.WriteLine("SENDING");
await client.SendAsync(frame, server);

await Task.Delay(2000);

Console.WriteLine("RECEIVING");
UdpReceiveResult result = await client.ReceiveAsync();
var responseMessage = Encoding.UTF8.GetString(result.Buffer);
Console.WriteLine("\n{0}\n{1}", "message", responseMessage);
