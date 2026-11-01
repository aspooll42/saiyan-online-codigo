using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class GuildProfile
{
	[Key(0)]
	public string Name = string.Empty;

	[Key(1)]
	public string Leader = string.Empty;

	[Key(2)]
	public long FoundedAt;

	[Key(3)]
	public GuildMember[] Members = Array.Empty<GuildMember>();

	[Key(4)]
	public int Level = 1;

	[Key(5)]
	public long Experience;

	[Key(6)]
	public long ExperienceToNext;

	[Key(7)]
	public string Tag = string.Empty;

	[Key(8)]
	public int Emblem;

	[Key(9)]
	public int Color;

	[Key(10)]
	public int Rank;

	[Key(11)]
	public long WeekExperience;

	[Key(12)]
	public int ObjectivesDone;

	[Key(13)]
	public int ObjectivesTotal;

	public GuildProfile()
	{
	}

	public GuildProfile(string name, string leader, long foundedAt, GuildMember[] members)
	{
		Name = name ?? string.Empty;
		Leader = leader ?? string.Empty;
		FoundedAt = foundedAt;
		Members = members ?? Array.Empty<GuildMember>();
	}
}
