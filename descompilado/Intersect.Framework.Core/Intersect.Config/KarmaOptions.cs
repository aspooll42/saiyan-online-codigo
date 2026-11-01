using System;
using System.Linq;

namespace Intersect.Config;

public class KarmaOptions
{
	public int Minimum { get; set; } = -1000;

	public int Maximum { get; set; } = 1000;

	public int HeroThreshold { get; set; } = 500;

	public int SuspectThreshold { get; set; } = -200;

	public int CriminalThreshold { get; set; } = -600;

	public int AggressorSeconds { get; set; } = 60;

	public int MurderPenalty { get; set; } = 100;

	public int WeakVictimPenalty { get; set; } = 200;

	public int WeakVictimLevelGap { get; set; } = 10;

	public int SuspectKillReward { get; set; } = 25;

	public int CriminalKillReward { get; set; } = 100;

	public int CriminalDeathPurge { get; set; } = 150;

	public int MonsterReward { get; set; } = 1;

	public int MonsterLevelGap { get; set; } = 5;

	public int RewardCooldownMinutes { get; set; } = 60;

	public bool AllowRewardsFromSameAddress { get; set; }

	public string HellMapName { get; set; } = "Enfer";

	public int HellSpawnX { get; set; } = 15;

	public int HellSpawnY { get; set; } = 12;

	public int HellSentenceMinMinutes { get; set; } = 10;

	public int HellSentenceMaxMinutes { get; set; } = 30;

	public int HellServedKarma { get; set; }

	public int HellBailKarma { get; set; } = -599;

	public long HellBailMin { get; set; } = 2000L;

	public long HellBailMax { get; set; } = 5000L;

	public string HellCounterEventName { get; set; } = "Guichet de l'Enfer";

	public string PoliceNpcNames { get; set; } = "Policier";

	public int PoliceSpeedPercent { get; set; } = 90;

	public int PoliceLungeTiles { get; set; } = 2;

	public int PoliceSightTiles { get; set; } = 7;

	public int PoliceNoticeMilliseconds { get; set; } = 600;

	public int PoliceGiveUpSeconds { get; set; } = 4;

	public string PrisonMapName { get; set; } = "Prison";

	public int PrisonSpawnX { get; set; } = 15;

	public int PrisonSpawnY { get; set; } = 14;

	public int PrisonSentenceMinMinutes { get; set; } = 5;

	public int PrisonSentenceMaxMinutes { get; set; } = 15;

	public long PrisonBailMin { get; set; } = 1000L;

	public long PrisonBailMax { get; set; } = 2500L;

	public string PrisonCounterEventName { get; set; } = "Guichet de la prison";

	public int PrisonServedKarma { get; set; }

	public int PrisonBailKarma { get; set; } = -599;

	public long BountyPerMurder { get; set; } = 1000L;

	public int BountyShareSeconds { get; set; } = 30;

	public int BountyPartyRange { get; set; } = 10;

	public int BountyLevelGap { get; set; } = 10;

	public int BountyLevelGapPercent { get; set; } = 50;

	public string TrackingRadarItemName { get; set; } = "Radar de Traque";

	public string WantedBoardEventName { get; set; } = "Avis de recherche";

	public int OutlawLogoutSeconds { get; set; } = 30;

	public string HideoutMapPrefix { get; set; } = "Repaire";

	public int WantedMaxContracts { get; set; } = 3;

	public int WantedContractHours { get; set; } = 24;

	public string PoliceGradeThresholds { get; set; } = "0,100,300,600,1000,1600";

	public string PoliceGradeBountiesPerDay { get; set; } = "3,3,4,4,5,5";

	public string PoliceGradeRadarDiscount { get; set; } = "0,10,10,20,20,20";

	public int PoliceEliteBonusGrade { get; set; } = 3;

	public int PoliceEliteBonusPercent { get; set; } = 25;

	public int ReputationPoliceBounty { get; set; } = 20;

	public int ReputationCriminalDown { get; set; } = 40;

	public long PoliceBountyZeniBase { get; set; } = 100L;

	public long PoliceBountyZeniPerLevel { get; set; } = 50L;

	public long PoliceBountyExperiencePerLevel { get; set; } = 20L;

	public int PoliceBountyMinSharePercent { get; set; } = 10;

	public int PoliceBountyLevelBand { get; set; } = 2;

	public int AnnouncementSeconds { get; set; } = 8;

	public string TraffickerEventName { get; set; } = "Trafiquant";

	public string SmokeBombItemName { get; set; } = "Fumigène";

	public string DisguiseItemName { get; set; } = "Déguisement";

	public string JammerItemName { get; set; } = "Brouilleur";

	public long SmokeBombPrice { get; set; } = 600L;

	public long DisguisePrice { get; set; } = 1200L;

	public long JammerPrice { get; set; } = 1800L;

	public int SmokeBombGrade { get; set; }

	public int DisguiseGrade { get; set; } = 2;

	public int JammerGrade { get; set; } = 3;

	public int DisguiseSeconds { get; set; } = 300;

	public int JammerSeconds { get; set; } = 300;

	public int SmokeEscapeSeconds { get; set; } = 8;

	public string RibbonGradeThresholds { get; set; } = "0,100,300,600,1000,1600";

	public int RibbonMaxContracts { get; set; } = 1;

	public int RibbonContractsPerDay { get; set; } = 3;

	public int RibbonHeroLevelGap { get; set; } = 5;

	public int RibbonArrestReputationLoss { get; set; } = 50;

	public string RibbonFenceEventName { get; set; } = "Receleur";

	public long RibbonSmugglingZeniBase { get; set; } = 200L;

	public long RibbonSmugglingZeniPerLevel { get; set; } = 45L;

	public int RibbonSmugglingReputation { get; set; } = 25;

	public int RibbonSmugglingGrade { get; set; }

	public long RibbonHeadZeniBase { get; set; } = 300L;

	public long RibbonHeadZeniPerLevel { get; set; } = 100L;

	public int RibbonHeadReputation { get; set; } = 60;

	public int RibbonHeadGrade { get; set; } = 3;

	public int RibbonSeizurePercent { get; set; } = 25;

	public long RibbonSabotageZeniBase { get; set; } = 300L;

	public long RibbonSabotageZeniPerLevel { get; set; } = 85L;

	public int RibbonSabotageReputation { get; set; } = 40;

	public int RibbonSabotageGrade { get; set; } = 1;

	public int RibbonSabotageBountyPercent { get; set; } = 20;

	public int RibbonSabotageHitSeconds { get; set; } = 60;

	public int PoliceSaboteurReputation { get; set; } = 30;

	public bool IsHideout(string? mapName)
	{
		if (!string.IsNullOrWhiteSpace(HideoutMapPrefix) && !string.IsNullOrWhiteSpace(mapName))
		{
			return mapName.TrimStart().StartsWith(HideoutMapPrefix.Trim(), StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	public int RibbonGradeOf(int reputation)
	{
		int[] array = ParseList(RibbonGradeThresholds);
		int result = 0;
		for (int i = 0; i < array.Length; i++)
		{
			if (reputation >= array[i])
			{
				result = i;
			}
		}
		return result;
	}

	public (long Price, int Grade, string ItemName) Article(BlackMarketArticle article)
	{
		return article switch
		{
			BlackMarketArticle.SmokeBomb => (Price: SmokeBombPrice, Grade: SmokeBombGrade, ItemName: SmokeBombItemName), 
			BlackMarketArticle.Disguise => (Price: DisguisePrice, Grade: DisguiseGrade, ItemName: DisguiseItemName), 
			_ => (Price: JammerPrice, Grade: JammerGrade, ItemName: JammerItemName), 
		};
	}

	public static int[] ParseList(string? list)
	{
		int result;
		return (from value in (list ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			select int.TryParse(value, out result) ? result : 0).ToArray();
	}

	public int PoliceGradeOf(int reputation)
	{
		int[] array = ParseList(PoliceGradeThresholds);
		int result = 0;
		for (int i = 0; i < array.Length; i++)
		{
			if (reputation >= array[i])
			{
				result = i;
			}
		}
		return result;
	}

	public static int ForGrade(string? list, int grade)
	{
		int[] array = ParseList(list);
		if (array.Length != 0)
		{
			return array[Math.Clamp(grade, 0, array.Length - 1)];
		}
		return 0;
	}

	public KarmaTier TierOf(int karma)
	{
		if (karma >= HeroThreshold)
		{
			return KarmaTier.Hero;
		}
		if (karma <= CriminalThreshold)
		{
			return KarmaTier.Criminal;
		}
		if (karma > SuspectThreshold)
		{
			return KarmaTier.Neutral;
		}
		return KarmaTier.Suspect;
	}
}
