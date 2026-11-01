using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class EquipmentPacket : IntersectPacket
{
	[Key(0)]
	public Guid EntityId { get; set; }

	[Key(1)]
	public int[] InventorySlots { get; set; }

	[Key(2)]
	public Guid[] ItemIds { get; set; }

	public EquipmentPacket()
	{
	}

	public EquipmentPacket(Guid entityId, int[] invSlots, Guid[] itemIds)
	{
		EntityId = entityId;
		InventorySlots = invSlots;
		ItemIds = itemIds;
	}
}
