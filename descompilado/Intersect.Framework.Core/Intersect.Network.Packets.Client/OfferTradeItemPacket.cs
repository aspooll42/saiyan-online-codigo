using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class OfferTradeItemPacket : SlotQuantityPacket
{
	public OfferTradeItemPacket()
		: base(0, 0)
	{
	}

	public OfferTradeItemPacket(int slot, int quantity)
		: base(slot, quantity)
	{
	}
}
