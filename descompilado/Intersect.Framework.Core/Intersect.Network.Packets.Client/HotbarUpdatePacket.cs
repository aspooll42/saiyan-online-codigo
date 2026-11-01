using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class HotbarUpdatePacket : IntersectPacket
{
	[Key(0)]
	public int HotbarSlot { get; set; }

	[Key(1)]
	public sbyte Type { get; set; }

	[Key(2)]
	public int Index { get; set; }

	public HotbarUpdatePacket()
	{
	}

	public HotbarUpdatePacket(int slot, sbyte type, int itemIndex)
	{
		HotbarSlot = slot;
		Type = type;
		Index = itemIndex;
	}
}
