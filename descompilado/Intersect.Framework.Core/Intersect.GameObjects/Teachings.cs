using System;
using System.Collections.Generic;
using System.Linq;

namespace Intersect.GameObjects;

public static class Teachings
{
	public const int FirstLevel = 10;

	public const int MaxPoints = 70;

	public const int SecondaryLevel = 40;

	public const int SecondaryMaxPoints = 15;

	public const int SecondaryMaxTier = 1;

	public const int MainMaxPoints = 55;

	public static readonly int[] TierRequirement = new int[6] { 0, 5, 15, 25, 35, 45 };

	public static readonly TeachingNode[] All = new TeachingNode[44]
	{
		new TeachingNode("provocation", TeachingMaster.Piccolo, 0, 5, 1, 1.0, Implemented: true),
		new TeachingNode("peau", TeachingMaster.Piccolo, 0, 5, 1, 1.0, Implemented: true),
		new TeachingNode("garde", TeachingMaster.Piccolo, 1, 5, 1, 2.0, Implemented: true),
		new TeachingNode("interposition", TeachingMaster.Piccolo, 1, 5, 1, 2.0, Implemented: true),
		new TeachingNode("riposte", TeachingMaster.Piccolo, 2, 5, 1, 3.0, Implemented: true),
		new TeachingNode("ancrage", TeachingMaster.Piccolo, 2, 5, 1, 6.0, Implemented: true),
		new TeachingNode("regeneration", TeachingMaster.Piccolo, 3, 5, 1, 0.2, Implemented: true),
		new TeachingNode("cape", TeachingMaster.Piccolo, 3, 5, 1, 2.0, Implemented: true),
		new TeachingNode("rempart", TeachingMaster.Piccolo, 4, 5, 1, 1.0, Implemented: true),
		new TeachingNode("volonte", TeachingMaster.Piccolo, 4, 5, 1, 30.0, Implemented: true),
		new TeachingNode("mur-namek", TeachingMaster.Piccolo, 5, 1, 5, 1.0, Implemented: true),
		new TeachingNode("poings", TeachingMaster.Tortue, 0, 5, 1, 1.0, Implemented: true),
		new TeachingNode("enchainement", TeachingMaster.Tortue, 0, 5, 1, 3.0, Implemented: true),
		new TeachingNode("plexus", TeachingMaster.Tortue, 1, 5, 1, 4.0, Implemented: true),
		new TeachingNode("assaut", TeachingMaster.Tortue, 1, 5, 1, 4.0, Implemented: true),
		new TeachingNode("grace", TeachingMaster.Tortue, 2, 5, 1, 10.0, Implemented: true),
		new TeachingNode("oeil", TeachingMaster.Tortue, 2, 5, 1, 1.0, Implemented: true),
		new TeachingNode("brise", TeachingMaster.Tortue, 3, 5, 1, 2.0, Implemented: true),
		new TeachingNode("duel", TeachingMaster.Tortue, 3, 5, 1, 2.0, Implemented: true),
		new TeachingNode("second", TeachingMaster.Tortue, 4, 5, 1, 0.5, Implemented: true),
		new TeachingNode("rythme", TeachingMaster.Tortue, 4, 5, 1, 40.0, Implemented: true),
		new TeachingNode("style-tortue", TeachingMaster.Tortue, 5, 1, 5, 1.0, Implemented: true),
		new TeachingNode("affute", TeachingMaster.Tenshinhan, 0, 5, 1, 1.0, Implemented: true),
		new TeachingNode("concentration", TeachingMaster.Tenshinhan, 0, 5, 1, 4.0, Implemented: true),
		new TeachingNode("economie", TeachingMaster.Tenshinhan, 1, 5, 1, 2.0, Implemented: true),
		new TeachingNode("portee", TeachingMaster.Tenshinhan, 1, 5, 1, 0.5, Implemented: true),
		new TeachingNode("percant", TeachingMaster.Tenshinhan, 2, 5, 1, 2.0, Implemented: true),
		new TeachingNode("eclat", TeachingMaster.Tenshinhan, 2, 5, 1, 6.0, Implemented: true),
		new TeachingNode("rafale", TeachingMaster.Tenshinhan, 3, 5, 1, 3.0, Implemented: true),
		new TeachingNode("distance", TeachingMaster.Tenshinhan, 3, 5, 1, 2.0, Implemented: true),
		new TeachingNode("calme", TeachingMaster.Tenshinhan, 4, 5, 1, 1.0, Implemented: true),
		new TeachingNode("fuite", TeachingMaster.Tenshinhan, 4, 5, 1, 3.0, Implemented: true),
		new TeachingNode("tri-faisceau", TeachingMaster.Tenshinhan, 5, 1, 5, 1.0, Implemented: true),
		new TeachingNode("partage", TeachingMaster.Krillin, 0, 5, 1, 3.0, Implemented: true),
		new TeachingNode("pas", TeachingMaster.Krillin, 0, 5, 1, 1.0, Implemented: true),
		new TeachingNode("repos", TeachingMaster.Krillin, 1, 5, 1, 4.0, Implemented: true),
		new TeachingNode("deviation", TeachingMaster.Krillin, 1, 5, 1, 2.0, Implemented: true),
		new TeachingNode("encouragement", TeachingMaster.Krillin, 2, 5, 1, 1.0, Implemented: true),
		new TeachingNode("reflexes", TeachingMaster.Krillin, 2, 5, 1, 0.5, Implemented: true),
		new TeachingNode("reserve", TeachingMaster.Krillin, 3, 5, 1, 2.0, Implemented: true),
		new TeachingNode("relais", TeachingMaster.Krillin, 3, 5, 1, 20.0, Implemented: true),
		new TeachingNode("taiyoken", TeachingMaster.Krillin, 4, 5, 1, 3.0, Implemented: true),
		new TeachingNode("veille", TeachingMaster.Krillin, 4, 5, 1, 6.0, Implemented: true),
		new TeachingNode("haricot-magique", TeachingMaster.Krillin, 5, 1, 5, 1.0, Implemented: true)
	};

	private static readonly Dictionary<string, TeachingNode> ById = All.ToDictionary((TeachingNode node) => node.Id);

	public static TeachingNode? Find(string? id)
	{
		if (id == null || !ById.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public static int PointsAt(int level)
	{
		return Math.Clamp(level - 10 + 1, 0, 70);
	}

	public static int RangeBonus(double value)
	{
		if (!(value <= 0.0))
		{
			return (int)Math.Ceiling(value - 1E-09);
		}
		return 0;
	}

	public static int Rank(IReadOnlyDictionary<string, int>? ranks, string id)
	{
		if (ranks == null || !ranks.TryGetValue(id, out var value))
		{
			return 0;
		}
		return value;
	}

	public static int Spent(IReadOnlyDictionary<string, int>? ranks)
	{
		return ranks?.Sum(delegate(KeyValuePair<string, int> pair)
		{
			TeachingNode teachingNode = Find(pair.Key);
			return ((object)teachingNode != null) ? (pair.Value * teachingNode.Cost) : 0;
		}) ?? 0;
	}

	public static int SpentIn(IReadOnlyDictionary<string, int>? ranks, TeachingMaster master, int belowTier = int.MaxValue)
	{
		return ranks?.Sum(delegate(KeyValuePair<string, int> pair)
		{
			TeachingNode teachingNode = Find(pair.Key);
			return ((object)teachingNode != null && teachingNode.Master == master && teachingNode.Tier < belowTier) ? (pair.Value * teachingNode.Cost) : 0;
		}) ?? 0;
	}

	public static double Value(TeachingMaster main, TeachingMaster secondary, IReadOnlyDictionary<string, int>? ranks, string id)
	{
		TeachingNode teachingNode = Find(id);
		if ((object)teachingNode == null)
		{
			return 0.0;
		}
		if (teachingNode.Master == TeachingMaster.None || (teachingNode.Master != main && (teachingNode.Master != secondary || teachingNode.Tier > 1)))
		{
			return 0.0;
		}
		return (double)Rank(ranks, id) * teachingNode.PerRank;
	}

	public static string? Validate(TeachingMaster main, TeachingMaster secondary, IReadOnlyDictionary<string, int> ranks, int level)
	{
		if (main == TeachingMaster.None && (secondary != TeachingMaster.None || ranks.Values.Any((int rank) => rank > 0)))
		{
			return "Choisis d'abord ton maître principal.";
		}
		if (main != TeachingMaster.None && level < 20)
		{
			return $"Les maîtres ne se présentent qu'au niveau {20}.";
		}
		if (secondary != TeachingMaster.None && (level < 40 || secondary == main))
		{
			return $"Le maître secondaire se choisit au niveau {40}, parmi les autres maîtres.";
		}
		foreach (var (id, num2) in ranks)
		{
			if (num2 != 0)
			{
				TeachingNode teachingNode = Find(id);
				if ((object)teachingNode == null || num2 < 0 || num2 > teachingNode.MaxRank)
				{
					return "Enseignement inconnu ou rang invalide.";
				}
				if (teachingNode.Master != main && teachingNode.Master != secondary)
				{
					return "Cette voie est fermée.";
				}
				if (teachingNode.Master == secondary && teachingNode.Tier > 1)
				{
					return "Le maître secondaire s'arrête au deuxième palier.";
				}
				if (SpentIn(ranks, teachingNode.Master, teachingNode.Tier) < TierRequirement[teachingNode.Tier])
				{
					return "Un palier n'est pas encore ouvert.";
				}
			}
		}
		if (secondary != TeachingMaster.None && SpentIn(ranks, secondary) > 15)
		{
			return $"Le maître secondaire est limité à {15} points.";
		}
		if (Spent(ranks) <= PointsAt(level))
		{
			return null;
		}
		return "Pas assez de points.";
	}
}
