using System;
using Intersect.Framework.Core.GameObjects.Rankings;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class RankingPacket : IntersectPacket
{
	[Key(0)]
	public RankingQuery Query { get; set; } = new RankingQuery();

	[Key(1)]
	public RankingEntryInfo[] Entries { get; set; } = Array.Empty<RankingEntryInfo>();

	[Key(2)]
	public RankingEntryInfo? Me { get; set; }

	[Key(3)]
	public long MeGap { get; set; }

	[Key(4)]
	public int Total { get; set; }

	[Key(5)]
	public int ComputedSecondsAgo { get; set; }

	[Key(6)]
	public int NextInSeconds { get; set; }

	[Key(7)]
	public long WeekResetInSeconds { get; set; }

	[Key(8)]
	public RankingPlaceInfo[] Places { get; set; } = Array.Empty<RankingPlaceInfo>();

	[Key(9)]
	public bool Ready { get; set; }
}
