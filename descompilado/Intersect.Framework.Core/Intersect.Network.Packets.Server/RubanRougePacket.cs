using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class RubanRougePacket : IntersectPacket
{
	[Key(0)]
	public int Reputation { get; set; }

	[Key(1)]
	public long Bounty { get; set; }

	[Key(2)]
	public long Zeni { get; set; }

	[Key(3)]
	public BlackMarketArticlePacket[] Articles { get; set; } = Array.Empty<BlackMarketArticlePacket>();

	[Key(4)]
	public string Place { get; set; } = string.Empty;

	[Key(5)]
	public RibbonContractEntryPacket[] Contracts { get; set; } = Array.Empty<RibbonContractEntryPacket>();

	[Key(6)]
	public int Held { get; set; }

	[Key(7)]
	public int MaxHeld { get; set; } = 1;

	[Key(8)]
	public int TakenToday { get; set; }

	[Key(9)]
	public int PerDay { get; set; } = 3;
}
