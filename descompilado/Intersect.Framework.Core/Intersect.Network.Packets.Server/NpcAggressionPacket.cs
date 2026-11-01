using System;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class NpcAggressionPacket : IntersectPacket
{
	[Key(0)]
	public Guid EntityId { get; set; }

	[Key(1)]
	public NpcAggression Aggression { get; set; }

	public NpcAggressionPacket()
	{
	}

	public NpcAggressionPacket(Guid entityId, NpcAggression aggression)
	{
		EntityId = entityId;
		Aggression = aggression;
	}
}
