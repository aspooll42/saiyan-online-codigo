using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class MarketPricesPacket : IntersectPacket
{
	[Key(0)]
	public Guid[] ItemIds { get; set; } = Array.Empty<Guid>();

	[Key(1)]
	public int Slot { get; set; } = -1;

	public MarketPricesPacket()
	{
	}

	public MarketPricesPacket(Guid[] itemIds, int slot)
	{
		ItemIds = itemIds;
		Slot = slot;
	}
}
