using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class RetrieveBagItemPacket : SlotQuantityPacket
{
	[Key(3)]
	public int InventorySlot { get; set; }

	public RetrieveBagItemPacket()
		: base(0, 0)
	{
	}

	public RetrieveBagItemPacket(int bagSlot, int quantity, int invSlot)
		: base(bagSlot, quantity)
	{
		InventorySlot = invSlot;
	}
}
