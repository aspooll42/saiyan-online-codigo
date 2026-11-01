using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class MarketOpenPacket : IntersectPacket
{
	[Key(0)]
	public bool Open { get; set; }

	[Key(1)]
	public bool TooFar { get; set; }

	public MarketOpenPacket()
	{
	}

	public MarketOpenPacket(bool open, bool tooFar)
	{
		Open = open;
		TooFar = tooFar;
	}
}
