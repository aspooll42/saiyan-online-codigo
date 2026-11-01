using Intersect.Framework.Core.Market;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class MarketSearchPacket : IntersectPacket
{
	[Key(0)]
	public string Query { get; set; } = string.Empty;

	[Key(1)]
	public MarketCategory Category { get; set; }

	[Key(2)]
	public MarketSort Sort { get; set; }

	[Key(3)]
	public int Page { get; set; }

	public MarketSearchPacket()
	{
	}

	public MarketSearchPacket(string query, MarketCategory category, MarketSort sort, int page)
	{
		Query = query;
		Category = category;
		Sort = sort;
		Page = page;
	}
}
