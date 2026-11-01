using System;
using Intersect.Framework.Core.Market;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class MarketPricesResultPacket : IntersectPacket
{
	[Key(0)]
	public MarketPriceInfo[] Prices { get; set; } = Array.Empty<MarketPriceInfo>();

	[Key(1)]
	public int Slot { get; set; } = -1;

	[Key(2)]
	public int SaleTaxPercent { get; set; }

	public MarketPricesResultPacket()
	{
	}

	public MarketPricesResultPacket(MarketPriceInfo[] prices, int slot, int saleTaxPercent)
	{
		Prices = prices;
		Slot = slot;
		SaleTaxPercent = saleTaxPercent;
	}
}
