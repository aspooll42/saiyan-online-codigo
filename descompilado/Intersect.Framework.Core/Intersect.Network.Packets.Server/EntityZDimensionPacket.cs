using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class EntityZDimensionPacket : IntersectPacket
{
	[Key(0)]
	public Guid EntityId { get; set; }

	[Key(1)]
	public byte Level { get; set; }

	public EntityZDimensionPacket()
	{
	}

	public EntityZDimensionPacket(Guid entityId, byte level)
	{
		EntityId = entityId;
		Level = level;
	}
}
