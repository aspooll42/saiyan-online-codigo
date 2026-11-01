using System;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class EntitySprintPacket : IntersectPacket
{
	[Key(0)]
	public Guid Id { get; set; }

	[Key(1)]
	public EntityType Type { get; set; }

	[Key(2)]
	public Guid MapId { get; set; }

	[Key(3)]
	public bool Sprinting { get; set; }

	public EntitySprintPacket()
	{
	}

	public EntitySprintPacket(Guid id, EntityType type, Guid mapId, bool sprinting)
	{
		Id = id;
		Type = type;
		MapId = mapId;
		Sprinting = sprinting;
	}
}
