using System;
using Intersect.Config;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class BlackMarketArticlePacket : IntersectPacket
{
	[Key(0)]
	public BlackMarketArticle Article { get; set; }

	[Key(1)]
	public Guid ItemId { get; set; }

	[Key(2)]
	public string Name { get; set; } = string.Empty;

	[Key(3)]
	public string Icon { get; set; } = string.Empty;

	[Key(4)]
	public long Price { get; set; }

	[Key(5)]
	public int Grade { get; set; }

	[Key(6)]
	public int Carried { get; set; }
}
