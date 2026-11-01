using System;

namespace Intersect.Framework.Core.GameObjects.Dungeons;

public sealed class AchievementDungeonCondition
{
	public Guid DungeonId { get; set; }

	public DungeonFeat Feat { get; set; } = DungeonFeat.NpcUntouched;

	public Guid NpcId { get; set; }

	public string NpcName { get; set; } = string.Empty;

	public string NpcSprite { get; set; } = string.Empty;

	public int RoomIndex { get; set; }

	public int Seconds { get; set; } = 60;

	public DungeonDifficulty MinDifficulty { get; set; }
}
