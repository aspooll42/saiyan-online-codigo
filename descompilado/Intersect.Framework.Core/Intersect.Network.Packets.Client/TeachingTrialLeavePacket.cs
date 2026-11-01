using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class TeachingTrialLeavePacket : IntersectPacket
{
	[Key(0)]
	public bool Confirmed { get; set; } = true;
}
