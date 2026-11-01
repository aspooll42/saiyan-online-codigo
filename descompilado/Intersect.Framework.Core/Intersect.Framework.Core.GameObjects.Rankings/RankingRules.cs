using System;

namespace Intersect.Framework.Core.GameObjects.Rankings;

public static class RankingRules
{
	public const int TopCount = 100;

	public const int ComputeIntervalSeconds = 300;

	public const int GuildTopMembers = 20;

	public const int WeekResetHour = 6;

	public const int RewardedPlaces = 3;

	public static readonly Guid ChampionTitleId = new Guid("A7C4E1B2-5D3F-4E8A-9B61-2F0C7D4E1A01");

	public static readonly Guid PodiumTitleId = new Guid("A7C4E1B2-5D3F-4E8A-9B61-2F0C7D4E1A02");

	public static DateTime WeekStart(DateTime local)
	{
		int num = (int)(local.DayOfWeek + 6) % 7;
		DateTime dateTime = local.Date.AddDays(-num).AddHours(6.0);
		if (!(local < dateTime))
		{
			return dateTime;
		}
		return dateTime.AddDays(-7.0);
	}

	public static bool IsRewarded(RankingQuery query)
	{
		if (query.Period != RankingPeriod.Week)
		{
			return false;
		}
		switch (query.Board)
		{
		case RankingBoard.Level:
		case RankingBoard.Power:
		case RankingBoard.Achievements:
			return query.Target == Guid.Empty;
		case RankingBoard.Gathering:
		case RankingBoard.Crafting:
			return query.Option < 0;
		case RankingBoard.Heroes:
			return true;
		case RankingBoard.Dungeon:
			return query.Option == 2;
		default:
			return false;
		}
	}

	public static Guid TitleFor(int rank)
	{
		if (rank != 1)
		{
			if (rank > 3)
			{
				return Guid.Empty;
			}
			return PodiumTitleId;
		}
		return ChampionTitleId;
	}
}
