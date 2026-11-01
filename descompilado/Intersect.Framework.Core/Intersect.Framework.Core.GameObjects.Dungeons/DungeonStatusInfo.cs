using System;
using System.Linq;
using MessagePack;

namespace Intersect.Framework.Core.GameObjects.Dungeons;

[MessagePackObject(false)]
public sealed class DungeonStatusInfo
{
	[Key(0)]
	public Guid DungeonId { get; set; }

	[Key(1)]
	public long ChestReadyInSeconds { get; set; }

	[Key(2)]
	public int BestSeconds { get; set; }

	[Key(3)]
	public int Completions { get; set; }

	[Key(4)]
	public byte ClearedMask { get; set; }

	[Key(5)]
	public int[] BestByDifficulty { get; set; } = new int[3];

	[Key(6)]
	public DungeonAchievementInfo[] Achievements { get; set; } = Array.Empty<DungeonAchievementInfo>();

	[Key(7)]
	public byte[][] RoomAbilities { get; set; } = Array.Empty<byte[]>();

	[Key(8)]
	public string[] RoomSprites { get; set; } = Array.Empty<string>();

	[Key(9)]
	public string[] RoomBosses { get; set; } = Array.Empty<string>();

	public int BestFor(DungeonDifficulty difficulty)
	{
		return BestByDifficulty.ElementAtOrDefault((int)difficulty);
	}
}
