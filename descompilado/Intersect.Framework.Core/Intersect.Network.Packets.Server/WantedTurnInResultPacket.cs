using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class WantedTurnInResultPacket : IntersectPacket
{
	[Key(0)]
	public bool Paid { get; set; }

	[Key(1)]
	public long Zeni { get; set; }

	[Key(2)]
	public long Experience { get; set; }

	[Key(3)]
	public int ReputationBefore { get; set; }

	[Key(4)]
	public int ReputationAfter { get; set; }
}
