using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class ChatMsgPacket : IntersectPacket
{
	[Key(0)]
	public string Message { get; set; }

	[Key(1)]
	public byte Channel { get; set; }

	public ChatMsgPacket()
	{
	}

	public ChatMsgPacket(string msg, byte channel)
	{
		Message = msg;
		Channel = channel;
	}
}
