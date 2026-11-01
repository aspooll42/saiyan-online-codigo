using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class SwapBankItemsPacket : SlotSwapPacket
{
	public SwapBankItemsPacket()
		: base(0, 0)
	{
	}

	public SwapBankItemsPacket(int slot1, int slot2)
		: base(slot1, slot2)
	{
	}
}
