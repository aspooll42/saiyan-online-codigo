using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class DropItemPacket : SlotQuantityPacket
{
	public DropItemPacket()
		: base(0, 0)
	{
	}

	public DropItemPacket(int slot, int quantity)
		: base(slot, quantity)
	{
	}
}
