using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class MarketCollectPacket : IntersectPacket
{
	[Key(0)]
	public bool Unused { get; set; }
}
