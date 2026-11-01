using System;
using MessagePack;

namespace Intersect.Framework.Core.Market;

[MessagePackObject(false)]
public class MarketPriceInfo
{
	[Key(0)]
	public Guid ItemId { get; set; }

	[Key(1)]
	public long Median { get; set; }

	[Key(2)]
	public long Cheapest { get; set; }

	[Key(3)]
	public int Listings { get; set; }

	[Key(4)]
	public long CheapestSameRollOrBetter { get; set; }

	[Key(5)]
	public long PerfectRollMedian { get; set; }
}
