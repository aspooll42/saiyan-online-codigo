using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class BuyItemPacket : SlotQuantityPacket
{
	public BuyItemPacket()
		: base(0, 0)
	{
	}

	public BuyItemPacket(int slot, int quantity)
		: base(slot, quantity)
	{
	}
}
