using System;

namespace Intersect.Framework.Core.GameObjects.Dungeons;

public sealed class DungeonRoom
{
	public string Name { get; set; } = string.Empty;

	public Guid MapId { get; set; }

	public int X { get; set; }

	public int Y { get; set; }

	public DungeonObjective Objective { get; set; } = DungeonObjective.KillAll;

	public Guid NpcId { get; set; }

	public string NpcName { get; set; } = string.Empty;

	public string NpcSprite { get; set; } = string.Empty;
}
