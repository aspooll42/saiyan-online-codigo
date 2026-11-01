using System;

namespace Intersect.Framework.Core.GameObjects.Dungeons;

public static class DungeonAchievementRules
{
	public const string Category = "Donjons";

	public const int RegularRuns = 10;

	public const int ExplorerDungeons = 5;

	public const int VeteranRuns = 100;

	public static readonly DungeonFeat[] Base = new DungeonFeat[7]
	{
		DungeonFeat.CompleteNormal,
		DungeonFeat.CompleteHard,
		DungeonFeat.CompleteHeroic,
		DungeonFeat.NoBeans,
		DungeonFeat.UnderTarget,
		DungeonFeat.Solo,
		DungeonFeat.Regular
	};

	public static readonly DungeonFeat[] Career = new DungeonFeat[3]
	{
		DungeonFeat.Explorer,
		DungeonFeat.Veteran,
		DungeonFeat.Legend
	};

	public static readonly DungeonFeat[] Custom = new DungeonFeat[3]
	{
		DungeonFeat.NpcUntouched,
		DungeonFeat.RoomUnderSeconds,
		DungeonFeat.RoomWithoutKo
	};

	public static int Points(DungeonFeat feat)
	{
		return feat switch
		{
			DungeonFeat.CompleteNormal => 10, 
			DungeonFeat.CompleteHard => 20, 
			DungeonFeat.CompleteHeroic => 40, 
			DungeonFeat.NoBeans => 25, 
			DungeonFeat.UnderTarget => 25, 
			DungeonFeat.Solo => 30, 
			DungeonFeat.Regular => 15, 
			DungeonFeat.Explorer => 30, 
			DungeonFeat.Veteran => 50, 
			DungeonFeat.Legend => 100, 
			_ => 0, 
		};
	}

	public static DungeonDifficulty MinDifficulty(DungeonFeat feat)
	{
		switch (feat)
		{
		case DungeonFeat.CompleteHard:
		case DungeonFeat.NoBeans:
		case DungeonFeat.UnderTarget:
		case DungeonFeat.Solo:
			return DungeonDifficulty.Hard;
		case DungeonFeat.CompleteHeroic:
			return DungeonDifficulty.Heroic;
		default:
			return DungeonDifficulty.Normal;
		}
	}

	public static string Name(DungeonFeat feat)
	{
		return feat switch
		{
			DungeonFeat.CompleteNormal => "Terminer en Normal", 
			DungeonFeat.CompleteHard => "Terminer en Difficile", 
			DungeonFeat.CompleteHeroic => "Terminer en Héroïque", 
			DungeonFeat.NoBeans => "Sans filet", 
			DungeonFeat.UnderTarget => "Contre la montre", 
			DungeonFeat.Solo => "Loup solitaire", 
			DungeonFeat.Regular => "Habitué", 
			DungeonFeat.NpcUntouched => "Intouchable", 
			DungeonFeat.RoomUnderSeconds => "Salle éclair", 
			DungeonFeat.RoomWithoutKo => "Salle sans K.O.", 
			DungeonFeat.Explorer => "Explorateur", 
			DungeonFeat.Veteran => "Vétéran", 
			DungeonFeat.Legend => "Légende", 
			_ => string.Empty, 
		};
	}

	public static string Condition(DungeonFeat feat, int targetSeconds = 0)
	{
		return feat switch
		{
			DungeonFeat.CompleteNormal => "Terminer le donjon en Normal ou plus", 
			DungeonFeat.CompleteHard => "Terminer le donjon en Difficile ou plus", 
			DungeonFeat.CompleteHeroic => "Terminer le donjon en Héroïque", 
			DungeonFeat.NoBeans => "Difficile ou plus, sans utiliser de haricot", 
			DungeonFeat.UnderTarget => "Difficile ou plus, en moins de " + Clock(targetSeconds), 
			DungeonFeat.Solo => "Difficile ou plus, seul", 
			DungeonFeat.Regular => $"Terminer le donjon {10} fois, toutes difficultés", 
			DungeonFeat.Explorer => $"Terminer {5} donjons différents", 
			DungeonFeat.Veteran => $"Terminer {100} donjons", 
			DungeonFeat.Legend => "Terminer tous les donjons en Héroïque", 
			_ => string.Empty, 
		};
	}

	public static string DifficultyName(DungeonDifficulty difficulty)
	{
		return difficulty switch
		{
			DungeonDifficulty.Hard => "Difficile", 
			DungeonDifficulty.Heroic => "Héroïque", 
			_ => "Normal", 
		};
	}

	public static string Id(Guid dungeonId, DungeonFeat feat)
	{
		return $"dungeon:{dungeonId:N}:{feat}";
	}

	public static string CareerId(DungeonFeat feat)
	{
		return $"dungeon-career:{feat}";
	}

	public static string Clock(int seconds)
	{
		seconds = Math.Max(0, seconds);
		if (seconds >= 3600)
		{
			return $"{seconds / 3600}:{seconds / 60 % 60:00}:{seconds % 60:00}";
		}
		return $"{seconds / 60}:{seconds % 60:00}";
	}
}
