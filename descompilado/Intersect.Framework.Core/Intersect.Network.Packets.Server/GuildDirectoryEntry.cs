using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class GuildDirectoryEntry
{
	[Key(0)]
	public string Name = string.Empty;

	[Key(1)]
	public int Members;

	[Key(2)]
	public string Leader = string.Empty;

	[Key(3)]
	public long FoundedAt;

	[Key(4)]
	public int Level = 1;

	[Key(5)]
	public string Tag = string.Empty;

	[Key(6)]
	public int Emblem;

	[Key(7)]
	public int Color;

	public GuildDirectoryEntry()
	{
	}

	public GuildDirectoryEntry(string name, int members, string leader, long foundedAt, int level = 1)
	{
		Name = name ?? string.Empty;
		Members = members;
		Leader = leader ?? string.Empty;
		FoundedAt = foundedAt;
		Level = level;
	}
}
