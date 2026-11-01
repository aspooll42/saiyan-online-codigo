using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class RibbonContractHeldPacket : IntersectPacket
{
	[Key(0)]
	public bool Has { get; set; }

	[Key(1)]
	public RibbonContractEntryPacket? Entry { get; set; }
}
