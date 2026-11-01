using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class HotbarPacket : IntersectPacket
{
	[Key(0)]
	public string[] SlotData { get; set; }

	public HotbarPacket()
	{
	}

	public HotbarPacket(string[] slotData)
	{
		SlotData = slotData;
	}
}
