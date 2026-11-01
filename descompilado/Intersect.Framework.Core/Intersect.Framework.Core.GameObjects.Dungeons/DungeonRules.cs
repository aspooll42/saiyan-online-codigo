using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Intersect.Framework.Core.GameObjects.Dungeons;

public static class DungeonRules
{
	public const int MaxPlayers = 4;

	public const int EntranceRange = 2;

	public const int GroupRange = 8;

	public const int ChestResetHour = 6;

	public const int ExitDelaySeconds = 120;

	public const int DifficultyCount = 3;

	public const byte AllDifficulties = 7;

	public static int HealthPercent(DungeonDescriptor dungeon, int players)
	{
		if (players > 1)
		{
			return Math.Clamp(100 + (players - 2) * dungeon.HealthPercentPerExtraPlayer, 10, 1000);
		}
		return Math.Clamp(dungeon.SoloHealthPercent, 10, 1000);
	}

	public static int HealthPercent(DungeonDescriptor dungeon, int players, DungeonDifficulty difficulty)
	{
		return Math.Clamp(HealthPercent(dungeon, players) * Math.Clamp(dungeon.Settings(difficulty).HealthPercent, 10, 1000) / 100, 5, 2000);
	}

	public static byte Bit(DungeonDifficulty difficulty)
	{
		return (byte)(1 << Math.Clamp((int)difficulty, 0, 2));
	}

	public static byte MaskUpTo(DungeonDifficulty difficulty)
	{
		return (byte)((1 << Math.Clamp((int)difficulty, 0, 2) + 1) - 1);
	}

	public static bool IsUnlocked(int clearedMask, DungeonDifficulty difficulty)
	{
		if (difficulty != DungeonDifficulty.Normal)
		{
			if ((int)difficulty <= 2)
			{
				return (clearedMask & Bit(difficulty - 1)) != 0;
			}
			return false;
		}
		return true;
	}

	public static List<DungeonDifficultySettings> DefaultDifficulties(DungeonDescriptor dungeon)
	{
		int num = Math.Clamp(dungeon.Beans, 0, 9);
		int num2 = 3;
		List<DungeonDifficultySettings> list = new List<DungeonDifficultySettings>(num2);
		CollectionsMarshal.SetCount(list, num2);
		Span<DungeonDifficultySettings> span = CollectionsMarshal.AsSpan(list);
		int num3 = 0;
		span[num3] = new DungeonDifficultySettings
		{
			LevelMin = dungeon.LevelMin,
			LevelMax = dungeon.LevelMax,
			HealthPercent = 70,
			DamagePercent = 70,
			ExperiencePercent = 80,
			Beans = num,
			ChestZeni = Round((double)dungeon.ChestZeni * 0.55),
			ChestExperience = Round((double)dungeon.ChestExperience * 0.55),
			EnragedAbilities = DungeonEnragedAbilities.Never
		};
		num3++;
		span[num3] = new DungeonDifficultySettings
		{
			LevelMin = dungeon.LevelMin + 2,
			LevelMax = dungeon.LevelMax + 2,
			Beans = Math.Max(1, num - 1),
			ChestZeni = dungeon.ChestZeni,
			ChestExperience = dungeon.ChestExperience
		};
		num3++;
		span[num3] = new DungeonDifficultySettings
		{
			LevelMin = dungeon.LevelMin + 4,
			LevelMax = dungeon.LevelMax + 4,
			HealthPercent = 140,
			DamagePercent = 130,
			ExperiencePercent = 130,
			Beans = Math.Max(1, num - 1),
			ChestZeni = Round((double)dungeon.ChestZeni * 1.65),
			ChestExperience = Round((double)dungeon.ChestExperience * 1.65),
			EnragedAbilities = DungeonEnragedAbilities.FromStart,
			TelegraphPercent = 70
		};
		num3++;
		return list;
		static long Round(double value)
		{
			return (long)Math.Round(value / 50.0) * 50;
		}
	}

	public static DateTime NextChestReset(DateTime openedLocal)
	{
		DateTime dateTime = openedLocal.Date.AddHours(6.0);
		if (!(openedLocal < dateTime))
		{
			return dateTime.AddDays(1.0);
		}
		return dateTime;
	}
}
