using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class GuildApplication
{
	[Key(0)]
	public Guid Id;

	[Key(1)]
	public string Name = string.Empty;

	[Key(2)]
	public int Level;

	[Key(3)]
	public string ClassName = string.Empty;

	[Key(4)]
	public long When;

	public GuildApplication()
	{
	}

	public GuildApplication(Guid id, string name, int level, string className, long when)
	{
		Id = id;
		Name = name ?? string.Empty;
		Level = level;
		ClassName = className ?? string.Empty;
		When = when;
	}
}
