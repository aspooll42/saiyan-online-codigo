using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class ExperiencePacket : IntersectPacket
{
	[Key(0)]
	public long Experience { get; set; }

	[Key(1)]
	public long ExperienceToNextLevel { get; set; }

	public ExperiencePacket()
	{
	}

	public ExperiencePacket(long exp, long tnl)
	{
		Experience = exp;
		ExperienceToNextLevel = tnl;
	}
}
