using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class MarketBuyPacket : IntersectPacket
{
	[Key(0)]
	public Guid ListingId { get; set; }

	[Key(1)]
	public int Quantity { get; set; }

	[Key(2)]
	public long UnitPrice { get; set; }

	public MarketBuyPacket()
	{
	}

	public MarketBuyPacket(Guid listingId, int quantity, long unitPrice)
	{
		ListingId = listingId;
		Quantity = quantity;
		UnitPrice = unitPrice;
	}
}
