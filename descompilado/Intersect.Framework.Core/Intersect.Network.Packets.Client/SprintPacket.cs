using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class SprintPacket : IntersectPacket
{
	[Key(0)]
	public bool Sprinting { get; set; }

	public SprintPacket()
	{
	}

	public SprintPacket(bool sprinting)
	{
		Sprinting = sprinting;
	}
}
