using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class HouseDoorPacket : IntersectPacket
{
	[Key(0)]
	public Guid MapId { get; set; }

	[Key(1)]
	public Guid TerrainId { get; set; }

	public HouseDoorPacket()
	{
	}

	public HouseDoorPacket(Guid mapId, Guid terrainId)
	{
		MapId = mapId;
		TerrainId = terrainId;
	}
}
