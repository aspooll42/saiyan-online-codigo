using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class StoreBagItemPacket : SlotQuantityPacket
{
	[Key(3)]
	public int BagSlot { get; set; }

	public StoreBagItemPacket()
		: base(0, 0)
	{
	}

	public StoreBagItemPacket(int invSlot, int quantity, int bagSlot)
		: base(invSlot, quantity)
	{
		BagSlot = bagSlot;
	}
}
