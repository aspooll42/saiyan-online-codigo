using System;
using Intersect.Framework.Core.Market;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class MarketResultPacket : IntersectPacket
{
	[Key(0)]
	public MarketResult Result { get; set; }

	[Key(1)]
	public Guid ItemId { get; set; }

	[Key(2)]
	public int Quantity { get; set; }

	[Key(3)]
	public long Amount { get; set; }

	public MarketResultPacket()
	{
	}

	public MarketResultPacket(MarketResult result, Guid itemId, int quantity, long amount)
	{
		Result = result;
		ItemId = itemId;
		Quantity = quantity;
		Amount = amount;
	}
}
