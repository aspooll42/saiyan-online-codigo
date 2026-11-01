using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class TeachingTrialPacket : IntersectPacket
{
	[Key(0)]
	public string TrialId { get; set; } = string.Empty;

	[Key(1)]
	public string Name { get; set; } = string.Empty;

	[Key(2)]
	public int Master { get; set; }

	[Key(3)]
	public long RemainingMs { get; set; }

	[Key(4)]
	public long DurationMs { get; set; }

	[Key(5)]
	public TeachingTrialObjective[] Objectives { get; set; } = Array.Empty<TeachingTrialObjective>();
}
