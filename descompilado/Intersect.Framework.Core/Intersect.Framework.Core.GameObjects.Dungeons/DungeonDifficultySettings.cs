namespace Intersect.Framework.Core.GameObjects.Dungeons;

public sealed class DungeonDifficultySettings
{
	public int LevelMin { get; set; } = 1;

	public int LevelMax { get; set; } = 10;

	public int HealthPercent { get; set; } = 100;

	public int DamagePercent { get; set; } = 100;

	public int ExperiencePercent { get; set; } = 100;

	public int Beans { get; set; } = 2;

	public long ChestZeni { get; set; }

	public long ChestExperience { get; set; }

	public DungeonEnragedAbilities EnragedAbilities { get; set; } = DungeonEnragedAbilities.WhenEnraged;

	public int TelegraphPercent { get; set; } = 100;

	public DungeonDifficultySettings Clone()
	{
		return (DungeonDifficultySettings)MemberwiseClone();
	}
}
