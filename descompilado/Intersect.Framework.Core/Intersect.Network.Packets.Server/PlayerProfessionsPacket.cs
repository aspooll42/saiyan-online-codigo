using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class PlayerProfessionsPacket : IntersectPacket
{
	[Key(0)]
	public int[] Levels { get; set; } = Array.Empty<int>();

	[Key(1)]
	public long[] Experience { get; set; } = Array.Empty<long>();

	[Key(2)]
	public long[] NextLevel { get; set; } = Array.Empty<long>();

	[Key(3)]
	public int MaximumLevel { get; set; }

	[Key(4)]
	public double SpeedPerLevel { get; set; }

	[Key(5)]
	public double RarityPerLevel { get; set; }

	[Key(6)]
	public int GainedProfession { get; set; } = -1;

	[Key(7)]
	public long Gained { get; set; }

	[Key(8)]
	public double AbundancePerLevel { get; set; }

	[Key(9)]
	public double PreciseBase { get; set; }

	[Key(10)]
	public double PrecisePerLevel { get; set; }

	[Key(11)]
	public double CraftExperiencePerSecond { get; set; } = 10.0;

	public PlayerProfessionsPacket()
	{
	}

	public PlayerProfessionsPacket(int[] levels, long[] experience, long[] nextLevel, int maximumLevel, double speedPerLevel, double rarityPerLevel, int gainedProfession, long gained)
	{
		Levels = levels;
		Experience = experience;
		NextLevel = nextLevel;
		MaximumLevel = maximumLevel;
		SpeedPerLevel = speedPerLevel;
		RarityPerLevel = rarityPerLevel;
		GainedProfession = gainedProfession;
		Gained = gained;
	}
}
