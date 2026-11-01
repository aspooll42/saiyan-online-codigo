using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class CharacterPacket : IntersectPacket
{
	[Key(0)]
	public Guid Id { get; set; }

	[Key(1)]
	public string Name { get; set; }

	[Key(2)]
	public string Sprite { get; set; }

	[Key(3)]
	public string Face { get; set; }

	[Key(4)]
	public int Level { get; set; }

	[Key(5)]
	public string ClassName { get; set; }

	[Key(6)]
	public EquipmentFragment[] Equipment { get; set; }

	public CharacterPacket()
	{
	}

	public CharacterPacket(Guid id, string name, string sprite, string face, int level, string className, EquipmentFragment[] equipment)
	{
		Id = id;
		Name = name;
		Sprite = sprite;
		Face = face;
		Level = level;
		ClassName = className;
		Equipment = equipment;
	}
}
