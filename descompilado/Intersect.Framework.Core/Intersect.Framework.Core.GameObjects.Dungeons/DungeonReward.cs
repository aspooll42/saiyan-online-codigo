using System;

namespace Intersect.Framework.Core.GameObjects.Dungeons;

public sealed class DungeonReward
{
	public Guid ItemId { get; set; }

	public int Quantity { get; set; } = 1;

	public int Chance { get; set; } = 100;

	public byte Difficulties { get; set; } = 7;

	public bool DropsIn(DungeonDifficulty difficulty)
	{
		return (Difficulties & DungeonRules.Bit(difficulty)) != 0;
	}
}
