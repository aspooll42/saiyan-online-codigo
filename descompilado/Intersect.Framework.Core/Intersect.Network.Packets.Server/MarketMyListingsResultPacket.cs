using System;
using Intersect.Framework.Core.Market;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class MarketMyListingsResultPacket : IntersectPacket
{
	[Key(0)]
	public MarketListingInfo[] Listings { get; set; } = Array.Empty<MarketListingInfo>();

	[Key(1)]
	public Guid CurrencyItemId { get; set; }

	[Key(2)]
	public int SaleTaxPercent { get; set; }

	[Key(3)]
	public int MaxListings { get; set; }

	public MarketMyListingsResultPacket()
	{
	}

	public MarketMyListingsResultPacket(MarketListingInfo[] listings, Guid currencyItemId, int saleTaxPercent, int maxListings)
	{
		Listings = listings;
		CurrencyItemId = currencyItemId;
		SaleTaxPercent = saleTaxPercent;
		MaxListings = maxListings;
	}
}
