using System;
using MessagePack;

namespace Intersect.Framework.Core.GameObjects.Rankings;

[MessagePackObject(false)]
public sealed class RankingQuery
{
	[Key(0)]
	public RankingBoard Board { get; set; } = RankingBoard.Power;

	[Key(1)]
	public RankingPeriod Period { get; set; }

	[Key(2)]
	public Guid Target { get; set; }

	[Key(3)]
	public int Option { get; set; }

	[Key(4)]
	public bool Group { get; set; }

	[IgnoreMember]
	public string Key => $"{Board}|{Period}|{Target:N}|{Option}|{Group}";

	public RankingQuery With(RankingPeriod period)
	{
		return new RankingQuery
		{
			Board = Board,
			Period = period,
			Target = Target,
			Option = Option,
			Group = Group
		};
	}

	public static RankingQuery Of(RankingBoard board, RankingPeriod period, Guid target = default(Guid), int option = 0, bool group = false)
	{
		return new RankingQuery
		{
			Board = board,
			Period = period,
			Target = target,
			Option = option,
			Group = group
		};
	}
}
