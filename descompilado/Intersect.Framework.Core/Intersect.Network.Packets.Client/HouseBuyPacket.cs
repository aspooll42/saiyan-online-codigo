using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class HouseBuyPacket : IntersectPacket
{
	[Key(0)]
	public Guid MapId { get; set; }

	[Key(1)]
	public Guid TerrainId { get; set; }

	[Key(2)]
	public Guid CapsuleId { get; set; }

	[Key(3)]
	public int OffsetX { get; set; } = -1;

	[Key(4)]
	public int OffsetY { get; set; } = -1;

	public HouseBuyPacket()
	{
	}

	public HouseBuyPacket(Guid mapId, Guid terrainId, Guid capsuleId, int offsetX, int offsetY)
	{
		MapId = mapId;
		TerrainId = terrainId;
		CapsuleId = capsuleId;
		OffsetX = offsetX;
		OffsetY = offsetY;
	}
}
