using System;
using Intersect.Framework.Core.GameObjects.Dungeons;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class DungeonCompletePacket : IntersectPacket
{
	[Key(0)]
	public Guid DungeonId { get; set; }

	[Key(1)]
	public int Seconds { get; set; }

	[Key(2)]
	public int BestSeconds { get; set; }

	[Key(3)]
	public bool NewRecord { get; set; }

	[Key(4)]
	public int BeansLeft { get; set; }

	[Key(5)]
	public int BeansMax { get; set; }

	[Key(6)]
	public int Kills { get; set; }

	[Key(7)]
	public bool ChestOpened { get; set; }

	[Key(8)]
	public long ChestReadyInSeconds { get; set; }

	[Key(9)]
	public long Zeni { get; set; }

	[Key(10)]
	public long Experience { get; set; }

	[Key(11)]
	public DungeonLootInfo[] Items { get; set; } = Array.Empty<DungeonLootInfo>();

	[Key(12)]
	public Guid TitleId { get; set; }

	[Key(13)]
	public bool FirstClear { get; set; }

	[Key(14)]
	public int ExitInSeconds { get; set; }

	[Key(15)]
	public DungeonDifficulty Difficulty { get; set; }

	[Key(16)]
	public int Players { get; set; }

	[Key(17)]
	public DungeonAchievementInfo[] Unlocked { get; set; } = Array.Empty<DungeonAchievementInfo>();

	[Key(18)]
	public DungeonAchievementInfo[] Progress { get; set; } = Array.Empty<DungeonAchievementInfo>();

	[Key(19)]
	public long KillExperience { get; set; }

	[Key(20)]
	public int ExperiencePercent { get; set; } = 100;
}
