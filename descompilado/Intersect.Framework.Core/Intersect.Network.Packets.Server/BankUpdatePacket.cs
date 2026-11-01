using System;
using Intersect.Framework.Core.GameObjects.Items;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class BankUpdatePacket : InventoryUpdatePacket
{
	public BankUpdatePacket()
		: base(0, Guid.Empty, 0, null, null)
	{
	}

	public BankUpdatePacket(int slot, Guid id, int quantity, Guid? bagId, ItemProperties properties)
		: base(slot, id, quantity, bagId, properties)
	{
	}
}
