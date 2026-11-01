using Intersect.Config;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class BlackMarketBuyPacket : IntersectPacket
{
	[Key(0)]
	public BlackMarketArticle Article { get; set; }

	public BlackMarketBuyPacket()
	{
	}

	public BlackMarketBuyPacket(BlackMarketArticle article)
	{
		Article = article;
	}
}
