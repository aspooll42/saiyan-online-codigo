using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class SwapSpellsPacket : SlotSwapPacket
{
	public SwapSpellsPacket()
		: base(0, 0)
	{
	}

	public SwapSpellsPacket(int slot1, int slot2)
		: base(slot1, slot2)
	{
	}
}
