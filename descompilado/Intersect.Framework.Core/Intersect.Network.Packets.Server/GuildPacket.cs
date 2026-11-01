using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class GuildPacket : IntersectPacket
{
	[Key(0)]
	public GuildMember[] Members { get; set; }

	[Key(1)]
	public string Motd { get; set; } = string.Empty;

	[Key(2)]
	public GuildActivityEntry[] Activity { get; set; } = Array.Empty<GuildActivityEntry>();

	[Key(3)]
	public GuildApplication[] Applications { get; set; } = Array.Empty<GuildApplication>();

	[Key(4)]
	public int Level { get; set; } = 1;

	[Key(5)]
	public long Experience { get; set; }

	[Key(6)]
	public long ExperienceToNext { get; set; }

	[Key(7)]
	public long WeekExperience { get; set; }

	[Key(8)]
	public GuildObjective[] Objectives { get; set; } = Array.Empty<GuildObjective>();

	[Key(9)]
	public long ObjectivesEndInSeconds { get; set; }

	[Key(10)]
	public string Tag { get; set; } = string.Empty;

	[Key(11)]
	public int Emblem { get; set; }

	[Key(12)]
	public int Color { get; set; }

	public GuildPacket()
	{
	}

	public GuildPacket(GuildMember[] members, string motd = "", GuildActivityEntry[]? activity = null, GuildApplication[]? applications = null)
	{
		Members = members;
		Motd = motd;
		Activity = activity ?? Array.Empty<GuildActivityEntry>();
		Applications = applications ?? Array.Empty<GuildApplication>();
	}
}
