using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class PingPacket : AbstractTimedPacket
{
	[Key(3)]
	public bool RequestingReply { get; set; }

	public PingPacket()
	{
	}

	public PingPacket(bool requestingReply)
	{
		RequestingReply = requestingReply;
	}
}
