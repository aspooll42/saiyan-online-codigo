using System;
using Intersect.Framework.Core.GameObjects.Items;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class MapItemUpdatePacket : IntersectPacket
{
	[Key(0)]
	public Guid MapId { get; set; }

	[Key(1)]
	public int TileIndex { get; set; }

	[Key(2)]
	public Guid Id { get; set; }

	[Key(3)]
	public Guid ItemId { get; set; }

	[Key(4)]
	public Guid? BagId { get; set; }

	[Key(5)]
	public int Quantity { get; set; }

	[Key(6)]
	public ItemProperties Properties { get; set; }

	public MapItemUpdatePacket()
	{
	}

	public MapItemUpdatePacket(Guid mapId, int tileIndex, Guid uniqueId)
	{
		MapId = mapId;
		TileIndex = tileIndex;
		Id = uniqueId;
	}

	public MapItemUpdatePacket(Guid mapId, int tileIndex, Guid uniqueId, Guid itemId, Guid? bagId, int quantity, ItemProperties properties)
	{
		MapId = mapId;
		TileIndex = tileIndex;
		Id = uniqueId;
		ItemId = itemId;
		BagId = bagId;
		Quantity = quantity;
		Properties = properties;
	}
}
