using System;
using Intersect.Framework.Core.GameObjects.Items;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class ItemPickedUpPacket : IntersectPacket
{
	[Key(0)]
	public Guid ItemId { get; set; }

	[Key(1)]
	public int Quantity { get; set; }

	[Key(2)]
	public ItemProperties? Properties { get; set; }

	[Key(3)]
	public bool Precise { get; set; }

	public ItemPickedUpPacket()
	{
	}

	public ItemPickedUpPacket(Guid itemId, int quantity, ItemProperties? properties = null, bool precise = false)
	{
		ItemId = itemId;
		Quantity = quantity;
		Properties = properties;
		Precise = precise;
	}
}
