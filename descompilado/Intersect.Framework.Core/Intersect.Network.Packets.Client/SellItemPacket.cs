using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class SellItemPacket : SlotQuantityPacket
{
	public SellItemPacket()
		: base(0, 0)
	{
	}

	public SellItemPacket(int slot, int quantity)
		: base(slot, quantity)
	{
	}
}
