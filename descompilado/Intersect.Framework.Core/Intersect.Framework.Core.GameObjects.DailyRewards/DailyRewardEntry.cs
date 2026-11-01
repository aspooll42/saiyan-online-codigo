using MessagePack;

namespace Intersect.Framework.Core.GameObjects.DailyRewards;

[MessagePackObject(false)]
public class DailyRewardEntry
{
	[Key(0)]
	public int Day { get; set; }

	[Key(1)]
	public long Zeni { get; set; }

	[Key(2)]
	public long Experience { get; set; }

	[Key(3)]
	public string ItemName { get; set; } = string.Empty;

	[Key(4)]
	public int ItemQuantity { get; set; } = 1;

	[Key(5)]
	public int BonusPercent { get; set; }

	[Key(6)]
	public int BonusMinutes { get; set; }

	[Key(7)]
	public string Label { get; set; } = string.Empty;

	[Key(8)]
	public bool Milestone { get; set; }
}
