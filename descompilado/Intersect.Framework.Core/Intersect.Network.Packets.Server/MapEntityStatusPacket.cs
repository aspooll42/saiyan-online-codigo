using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class MapEntityStatusPacket : IntersectPacket
{
	[Key(0)]
	public Guid MapId { get; set; }

	[Key(1)]
	public EntityStatusData[] EntityUpdates { get; set; }

	public MapEntityStatusPacket(Guid mapId, EntityStatusData[] entityUpdates)
	{
		MapId = mapId;
		EntityUpdates = entityUpdates;
	}

	public MapEntityStatusPacket()
	{
	}
}
