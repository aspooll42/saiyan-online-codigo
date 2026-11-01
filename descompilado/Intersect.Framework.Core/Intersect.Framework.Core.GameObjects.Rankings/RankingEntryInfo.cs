using System;
using MessagePack;

namespace Intersect.Framework.Core.GameObjects.Rankings;

[MessagePackObject(false)]
public sealed class RankingEntryInfo
{
	[Key(0)]
	public int Rank { get; set; }

	[Key(1)]
	public Guid Id { get; set; }

	[Key(2)]
	public string Name { get; set; } = string.Empty;

	[Key(3)]
	public Guid ClassId { get; set; }

	[Key(4)]
	public string Sprite { get; set; } = string.Empty;

	[Key(5)]
	public int Level { get; set; }

	[Key(6)]
	public string GuildName { get; set; } = string.Empty;

	[Key(7)]
	public Guid TitleId { get; set; }

	[Key(8)]
	public long Value { get; set; }

	[Key(9)]
	public long Detail { get; set; }

	[Key(10)]
	public int Evolution { get; set; }

	[Key(11)]
	public bool IsNew { get; set; }

	[Key(12)]
	public RankingMemberInfo[] Team { get; set; } = Array.Empty<RankingMemberInfo>();

	[Key(13)]
	public int Members { get; set; }

	[Key(14)]
	public string LeaderName { get; set; } = string.Empty;

	[Key(15)]
	public string LeaderSprite { get; set; } = string.Empty;

	[Key(16)]
	public Guid LeaderClassId { get; set; }

	[Key(17)]
	public string GuildTag { get; set; } = string.Empty;

	[Key(18)]
	public int GuildEmblem { get; set; }

	[Key(19)]
	public int GuildColor { get; set; }
}
