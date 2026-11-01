using System;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class EntityDirectionPacket : IntersectPacket
{
	[Key(0)]
	public Guid Id { get; set; }

	[Key(1)]
	public EntityType Type { get; set; }

	[Key(2)]
	public Guid MapId { get; set; }

	[Key(3)]
	public byte Direction { get; set; }

	public EntityDirectionPacket()
	{
	}

	public EntityDirectionPacket(Guid id, EntityType type, Guid mapId, byte direction)
	{
		Id = id;
		Type = type;
		MapId = mapId;
		Direction = direction;
	}
}
