using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class GuildDirectoryPacket : IntersectPacket
{
	[Key(0)]
	public GuildDirectoryEntry[] Entries { get; set; } = Array.Empty<GuildDirectoryEntry>();

	[Key(1)]
	public string Query { get; set; } = string.Empty;

	[Key(2)]
	public int Page { get; set; }

	[Key(3)]
	public int Total { get; set; }

	[Key(4)]
	public string AppliedTo { get; set; } = string.Empty;

	[Key(5)]
	public GuildProfile? Profile { get; set; }

	public GuildDirectoryPacket()
	{
	}

	public GuildDirectoryPacket(GuildDirectoryEntry[] entries, string query, int page, int total, string appliedTo = "")
	{
		Entries = entries;
		Query = query;
		Page = page;
		Total = total;
		AppliedTo = appliedTo ?? string.Empty;
	}
}
