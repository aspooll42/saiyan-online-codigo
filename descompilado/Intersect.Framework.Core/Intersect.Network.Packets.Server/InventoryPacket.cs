using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class InventoryPacket : IntersectPacket
{
	[Key(0)]
	public InventoryUpdatePacket[] Slots { get; set; }

	public InventoryPacket()
	{
	}

	public InventoryPacket(InventoryUpdatePacket[] slots)
	{
		Slots = slots;
	}
}
