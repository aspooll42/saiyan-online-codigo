using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class GlobalCooldownPacket : IntersectPacket
{
	[Key(0)]
	public long GlobalCooldown { get; set; }

	public GlobalCooldownPacket()
	{
	}

	public GlobalCooldownPacket(long cooldown)
	{
		GlobalCooldown = cooldown;
	}
}
