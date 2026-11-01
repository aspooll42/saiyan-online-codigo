using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class SwapInvItemsPacket : SlotSwapPacket
{
	public SwapInvItemsPacket()
		: base(0, 0)
	{
	}

	public SwapInvItemsPacket(int slot1, int slot2)
		: base(slot1, slot2)
	{
	}
}
