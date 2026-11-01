using System;
using System.Globalization;
using Intersect.Client.Localization;
using Intersect.Framework.Core.GameObjects.Crafting;
using Intersect.Framework.Core.GameObjects.Resources;

namespace Intersect.Client.Entities;

public static class Professions
{
	public const int Count = 9;

	public const int HarvestCount = 4;

	public const int Hunter = 4;

	public const int GatheringCount = 5;

	private static readonly string[] Icons = new string[9] { "buche_bois.png", "pierre.png", "herbe_medicinale.png", "poisson_paozu.png", "viande_dinosaure.png", "gants_combat.png", "poisson_grille.png", "fiole_soin.png", "capsule.png" };

	private static readonly Color[] Colors = new Color[9]
	{
		new Color(255, 232, 176, 100),
		new Color(255, 174, 189, 208),
		new Color(255, 143, 212, 110),
		new Color(255, 98, 201, 239),
		new Color(255, 224, 122, 90),
		new Color(255, 255, 154, 82),
		new Color(255, 245, 207, 106),
		new Color(255, 142, 214, 124),
		new Color(255, 98, 201, 239)
	};

	public static bool IsValid(int profession)
	{
		if (profession >= 0)
		{
			return profession < 9;
		}
		return false;
	}

	public static bool IsCrafting(int profession)
	{
		return MetiersArtisanat.EstArtisanat(profession);
	}

	public static string Name(int profession)
	{
		return profession switch
		{
			0 => Strings.Professions.Woodcutter, 
			1 => Strings.Professions.Miner, 
			2 => Strings.Professions.Gatherer, 
			3 => Strings.Professions.Fisher, 
			4 => Strings.Professions.Hunter, 
			5 => Strings.Professions.Blacksmith, 
			6 => Strings.Professions.Cook, 
			7 => Strings.Professions.Apothecary, 
			8 => Strings.Professions.Engineer, 
			_ => string.Empty, 
		};
	}

	public static string Subtitle(int profession)
	{
		return profession switch
		{
			0 => Strings.Professions.WoodcutterSubtitle, 
			1 => Strings.Professions.MinerSubtitle, 
			2 => Strings.Professions.GathererSubtitle, 
			3 => Strings.Professions.FisherSubtitle, 
			4 => Strings.Professions.HunterSubtitle, 
			5 => Strings.Professions.BlacksmithSubtitle, 
			6 => Strings.Professions.CookSubtitle, 
			7 => Strings.Professions.ApothecarySubtitle, 
			8 => Strings.Professions.EngineerSubtitle, 
			_ => string.Empty, 
		};
	}

	public static string SpeedLabel(int profession)
	{
		switch (profession)
		{
		case 0:
			return Strings.Professions.WoodcutterSpeed;
		case 1:
			return Strings.Professions.MinerSpeed;
		case 2:
			return Strings.Professions.GathererSpeed;
		case 3:
			return Strings.Professions.FisherSpeed;
		case 4:
			return Strings.Professions.HunterSpeed;
		case 5:
		case 6:
		case 7:
		case 8:
			return Strings.Professions.PreciseChance;
		default:
			return string.Empty;
		}
	}

	public static string Workshop(int profession)
	{
		return profession switch
		{
			5 => Strings.Professions.BlacksmithWorkshop, 
			6 => Strings.Professions.CookWorkshop, 
			7 => Strings.Professions.ApothecaryWorkshop, 
			8 => Strings.Professions.EngineerWorkshop, 
			_ => string.Empty, 
		};
	}

	public static string Icon(int profession)
	{
		if (!IsValid(profession))
		{
			return Icons[0];
		}
		return Icons[profession];
	}

	public static Color Tint(int profession)
	{
		if (!IsValid(profession))
		{
			return Colors[0];
		}
		return Colors[profession];
	}

	public static string Percent(double value)
	{
		return Math.Round(value, 1).ToString((Math.Abs(value % 1.0) < 0.05) ? "0" : "0.0", CultureInfo.GetCultureInfo("fr-FR"));
	}

	public static int Of(ResourceDescriptor? descriptor)
	{
		switch (descriptor?.Tool)
		{
		default:
			return -1;
		case 0:
		case 1:
		case 2:
		case 3:
			return descriptor.Tool;
		}
	}
}
