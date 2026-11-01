using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class PickupItemPacket : IntersectPacket
{
	[Key(0)]
	public Guid UniqueId { get; set; }

	[Key(1)]
	public Guid MapId { get; set; }

	[Key(2)]
	public int TileIndex { get; set; }

	public PickupItemPacket()
	{
	}

	public PickupItemPacket(Guid mapId, int tileIndex, Guid uniqueId)
	{
		MapId = mapId;
		UniqueId = uniqueId;
		TileIndex = tileIndex;
	}
}
