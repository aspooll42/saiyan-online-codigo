using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class MapEntityVitalsPacket : IntersectPacket
{
	[Key(0)]
	public Guid MapId { get; set; }

	[Key(1)]
	public EntityVitalData[] EntityUpdates { get; set; }

	public MapEntityVitalsPacket(Guid mapId, EntityVitalData[] entityUpdates)
	{
		MapId = mapId;
		EntityUpdates = entityUpdates;
	}

	public MapEntityVitalsPacket()
	{
	}
}
