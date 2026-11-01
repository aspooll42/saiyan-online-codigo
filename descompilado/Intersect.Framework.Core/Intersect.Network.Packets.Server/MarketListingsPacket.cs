using System;
using Intersect.Framework.Core.Market;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class MarketListingsPacket : IntersectPacket
{
	[Key(0)]
	public MarketListingInfo[] Listings { get; set; } = Array.Empty<MarketListingInfo>();

	[Key(1)]
	public int Page { get; set; }

	[Key(2)]
	public int PageCount { get; set; }

	[Key(3)]
	public int TotalCount { get; set; }

	[Key(4)]
	public Guid CurrencyItemId { get; set; }

	[Key(5)]
	public int SaleTaxPercent { get; set; }

	public MarketListingsPacket()
	{
	}

	public MarketListingsPacket(MarketListingInfo[] listings, int page, int pageCount, int totalCount, Guid currencyItemId, int saleTaxPercent)
	{
		Listings = listings;
		Page = page;
		PageCount = pageCount;
		TotalCount = totalCount;
		CurrencyItemId = currencyItemId;
		SaleTaxPercent = saleTaxPercent;
	}
}
