using MessagePack;

namespace Intersect.Framework.Core.GameObjects.Dungeons;

[MessagePackObject(false)]
public sealed class DungeonAchievementInfo
{
	[Key(0)]
	public string Id { get; set; } = string.Empty;

	[Key(1)]
	public DungeonFeat Feat { get; set; }

	[Key(2)]
	public string Name { get; set; } = string.Empty;

	[Key(3)]
	public string Description { get; set; } = string.Empty;

	[Key(4)]
	public int Points { get; set; }

	[Key(5)]
	public DungeonDifficulty MinDifficulty { get; set; }

	[Key(6)]
	public bool Obtained { get; set; }

	[Key(7)]
	public long Value { get; set; }

	[Key(8)]
	public long Objective { get; set; } = 1L;

	[Key(9)]
	public string Sprite { get; set; } = string.Empty;

	[Key(10)]
	public long Actual { get; set; }

	[Key(11)]
	public byte MissKind { get; set; }

	[Key(12)]
	public bool Custom { get; set; }

	[Key(13)]
	public int Seconds { get; set; }

	[IgnoreMember]
	public bool SoloOnly => Feat == DungeonFeat.Solo;
}
