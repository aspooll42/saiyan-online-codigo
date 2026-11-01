using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class KarmaPacket : IntersectPacket
{
	[Key(0)]
	public int Karma { get; set; }

	public KarmaPacket()
	{
	}

	public KarmaPacket(int karma)
	{
		Karma = karma;
	}
}
