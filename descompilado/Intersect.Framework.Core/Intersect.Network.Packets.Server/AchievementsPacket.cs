using System;
using Intersect.Framework.Core.GameObjects.Achievements;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class AchievementsPacket : IntersectPacket
{
	[Key(0)]
	public AchievementProgress[] Achievements { get; set; } = Array.Empty<AchievementProgress>();

	[Key(1)]
	public int EarnedPoints { get; set; }

	[Key(2)]
	public int TotalPoints { get; set; }

	public AchievementsPacket()
	{
	}

	public AchievementsPacket(AchievementProgress[] achievements, int earnedPoints, int totalPoints)
	{
		Achievements = achievements;
		EarnedPoints = earnedPoints;
		TotalPoints = totalPoints;
	}
}
