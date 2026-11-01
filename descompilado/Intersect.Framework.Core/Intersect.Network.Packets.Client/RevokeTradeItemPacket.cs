using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class RevokeTradeItemPacket : SlotQuantityPacket
{
	public RevokeTradeItemPacket()
		: base(0, 0)
	{
	}

	public RevokeTradeItemPacket(int slot, int quantity)
		: base(slot, quantity)
	{
	}
}
