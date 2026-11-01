using Intersect.Config;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class HellBailOfferPacket : IntersectPacket
{
	[Key(0)]
	public long Bail { get; set; }

	[Key(1)]
	public DetentionPlace Place { get; set; }

	public HellBailOfferPacket()
	{
	}

	public HellBailOfferPacket(long bail, DetentionPlace place)
	{
		Bail = bail;
		Place = place;
	}
}
