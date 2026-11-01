using System.Collections.Generic;
using Intersect.Collections;
using Intersect.Network.Packets.Client;
using MessagePack;

namespace Intersect.Network.Packets;

[MessagePackObject(false)]
[Union(0, typeof(BuyItemPacket))]
[Union(1, typeof(DepositItemPacket))]
[Union(2, typeof(DropItemPacket))]
[Union(3, typeof(OfferTradeItemPacket))]
[Union(4, typeof(RetrieveBagItemPacket))]
[Union(5, typeof(RevokeTradeItemPacket))]
[Union(6, typeof(SellItemPacket))]
[Union(7, typeof(StoreBagItemPacket))]
[Union(8, typeof(WithdrawItemPacket))]
public abstract class SlotQuantityPacket : IntersectPacket
{
	[Key(1)]
	public int Slot { get; set; }

	[Key(2)]
	public int Quantity { get; set; }

	[IgnoreMember]
	public override bool IsValid
	{
		get
		{
			if (Slot >= 0)
			{
				return Quantity >= 0;
			}
			return false;
		}
	}

	public SlotQuantityPacket()
	{
	}

	public SlotQuantityPacket(int slot, int quantity)
	{
		Slot = slot;
		Quantity = quantity;
	}

	public override Dictionary<string, SanitizedValue<object>> Sanitize()
	{
		Sanitizer sanitizer = default(Sanitizer);
		Quantity = sanitizer.Maximum("Quantity", Quantity, 0);
		Slot = sanitizer.Maximum("Slot", Slot, 0);
		return sanitizer.Sanitized;
	}
}
