using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class MarketSellPacket : IntersectPacket
{
	[Key(0)]
	public int Slot { get; set; }

	[Key(1)]
	public int Quantity { get; set; }

	[Key(2)]
	public long UnitPrice { get; set; }

	public MarketSellPacket()
	{
	}

	public MarketSellPacket(int slot, int quantity, long unitPrice)
	{
		Slot = slot;
		Quantity = quantity;
		UnitPrice = unitPrice;
	}
}
