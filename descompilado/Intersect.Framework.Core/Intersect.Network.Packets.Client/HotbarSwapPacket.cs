using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class HotbarSwapPacket : SlotSwapPacket
{
	public HotbarSwapPacket()
		: base(0, 0)
	{
	}

	public HotbarSwapPacket(int slot1, int slot2)
		: base(slot1, slot2)
	{
	}
}
