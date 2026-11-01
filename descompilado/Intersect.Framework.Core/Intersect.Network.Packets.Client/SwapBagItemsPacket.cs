using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class SwapBagItemsPacket : SlotSwapPacket
{
	public SwapBagItemsPacket()
		: base(0, 0)
	{
	}

	public SwapBagItemsPacket(int slot1, int slot2)
		: base(slot1, slot2)
	{
	}
}
