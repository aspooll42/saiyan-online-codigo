using System;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class EntityDiePacket : IntersectPacket
{
	[Key(0)]
	public Guid Id { get; set; }

	[Key(1)]
	public EntityType Type { get; set; }

	[Key(2)]
	public Guid MapId { get; set; }

	public EntityDiePacket()
	{
	}

	public EntityDiePacket(Guid id, EntityType type, Guid mapId)
	{
		Id = id;
		Type = type;
		MapId = mapId;
	}
}
