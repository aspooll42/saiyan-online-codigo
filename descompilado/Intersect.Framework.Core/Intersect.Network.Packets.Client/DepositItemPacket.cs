using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class DepositItemPacket : SlotQuantityPacket
{
	[Key(3)]
	public int BankSlot { get; set; }

	public DepositItemPacket()
		: base(0, 0)
	{
	}

	public DepositItemPacket(int slot, int quantity, int bankSlot = -1)
		: base(slot, quantity)
	{
		BankSlot = bankSlot;
	}
}
