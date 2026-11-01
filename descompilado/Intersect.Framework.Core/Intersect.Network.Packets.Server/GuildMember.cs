using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class GuildMember
{
	[Key(0)]
	public Guid Id;

	[Key(1)]
	public string Name;

	[Key(2)]
	public int Rank;

	[Key(3)]
	public int Level;

	[Key(4)]
	public string ClassName;

	[Key(5)]
	public string MapName;

	[Key(6)]
	public bool Online;

	[Key(7)]
	public string Sprite = string.Empty;

	[Key(8)]
	public long Contribution;

	[Key(9)]
	public Guid[] Equipment = Array.Empty<Guid>();

	[Key(10)]
	public Guid ClassId;

	[Key(11)]
	public int Gender;

	public GuildMember()
	{
	}

	public GuildMember(Guid id, string name, int rank, int level, string className, string mapName, string? sprite = null)
	{
		Id = id;
		Name = name;
		Rank = rank;
		Level = level;
		ClassName = className;
		MapName = mapName;
		Sprite = sprite ?? string.Empty;
	}
}
