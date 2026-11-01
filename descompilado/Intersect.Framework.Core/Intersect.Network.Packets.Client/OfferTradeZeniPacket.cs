using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class OfferTradeZeniPacket : IntersectPacket
{
	[Key(0)]
	public long Amount { get; set; }

	public OfferTradeZeniPacket()
	{
	}

	public OfferTradeZeniPacket(long amount)
	{
		Amount = amount;
	}
}
