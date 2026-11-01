using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class MarketCancelPacket : IntersectPacket
{
	[Key(0)]
	public Guid ListingId { get; set; }

	public MarketCancelPacket()
	{
	}

	public MarketCancelPacket(Guid listingId)
	{
		ListingId = listingId;
	}
}
