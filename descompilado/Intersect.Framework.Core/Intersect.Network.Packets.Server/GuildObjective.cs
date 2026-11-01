using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class GuildObjective
{
	[Key(0)]
	public int Kind { get; set; }

	[Key(1)]
	public Guid TargetId { get; set; }

	[Key(2)]
	public string TargetName { get; set; } = string.Empty;

	[Key(3)]
	public long Goal { get; set; }

	[Key(4)]
	public long Progress { get; set; }

	[Key(5)]
	public bool Done { get; set; }

	[Key(6)]
	public long RewardExperience { get; set; }

	[Key(7)]
	public long RewardZeni { get; set; }

	[Key(8)]
	public int Participants { get; set; }
}
