using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class WithdrawItemPacket : SlotQuantityPacket
{
	[Key(3)]
	public int InvSlot { get; set; }

	public WithdrawItemPacket()
		: base(0, 0)
	{
	}

	public WithdrawItemPacket(int slot, int quantity, int invSlot = -1)
		: base(slot, quantity)
	{
		InvSlot = invSlot;
	}
}
