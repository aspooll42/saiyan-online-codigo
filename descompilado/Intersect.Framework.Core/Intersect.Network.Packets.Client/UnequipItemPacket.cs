using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class UnequipItemPacket : IntersectPacket
{
	[Key(0)]
	public int Slot { get; set; }

	public UnequipItemPacket()
	{
	}

	public UnequipItemPacket(int slot)
	{
		Slot = slot;
	}
}
