using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Intersect.Enums;
using Intersect.Models;

namespace Intersect.GameObjects;

public static class SpellMastery
{
	public const int MaxRank = 5;

	public const int MaxStages = 4;

	public static int Clamp(int rank)
	{
		return Math.Clamp(rank, 1, 5);
	}

	public static bool HasMastery(SpellDescriptor? spell)
	{
		SpellMasteryDescriptor spellMasteryDescriptor = spell?.Mastery;
		if (spellMasteryDescriptor != null)
		{
			return spellMasteryDescriptor.ExperiencePerRank > 0;
		}
		return false;
	}

	public static int ExperienceToNext(SpellDescriptor spell, int rank)
	{
		rank = Clamp(rank);
		if (rank < 5 && HasMastery(spell))
		{
			return spell.Mastery.ExperiencePerRank * rank;
		}
		return 0;
	}

	private static int Steps(SpellDescriptor spell, int rank)
	{
		if (!HasMastery(spell))
		{
			return 0;
		}
		return Clamp(rank) - 1;
	}

	public static long VitalDiff(SpellDescriptor spell, Vital vital, int rank)
	{
		long num = spell.Combat.VitalDiff[(int)vital];
		if (vital != Vital.Health || num == 0L)
		{
			return num;
		}
		long num2 = num + (long)Math.Sign(num) * (long)spell.Mastery.DamagePerRank * Steps(spell, rank);
		if (Math.Sign(num2) != Math.Sign(num))
		{
			return Math.Sign(num);
		}
		return num2;
	}

	public static long VitalCost(SpellDescriptor spell, Vital vital, int rank)
	{
		long num = spell.VitalCost[(int)vital];
		if (vital != Vital.Mana || num <= 0)
		{
			return num;
		}
		return Math.Max(0L, num + (long)spell.Mastery.KiCostPerRank * (long)Steps(spell, rank));
	}

	public static int CastDuration(SpellDescriptor spell, int rank)
	{
		return Math.Max(0, spell.CastDuration + spell.Mastery.CastTimePerRankMs * Steps(spell, rank));
	}

	public static int Cooldown(SpellDescriptor spell, int rank)
	{
		return Math.Max(0, spell.CooldownDuration + spell.Mastery.CooldownPerRankMs * Steps(spell, rank));
	}

	public static int EffectDuration(SpellDescriptor spell, int rank)
	{
		if (spell.Combat.Duration > 0)
		{
			return Math.Max(0, spell.Combat.Duration + spell.Mastery.DurationPerRankMs * Steps(spell, rank));
		}
		return spell.Combat.Duration;
	}

	public static double LeakFactor(SpellDescriptor spell, int rank)
	{
		return Math.Max(0.1, 1.0 + (double)(spell.Mastery.LeakPercentPerRank * Steps(spell, rank)) / 100.0);
	}

	public static SpellDescriptor? EvolvesInto(SpellDescriptor spell)
	{
		if (!(spell.Mastery.EvolvesIntoId != Guid.Empty) || !(spell.Mastery.EvolvesIntoId != spell.Id) || !DatabaseObject<SpellDescriptor>.TryGet(spell.Mastery.EvolvesIntoId, out var databaseObject))
		{
			return null;
		}
		return databaseObject;
	}

	public static SpellDescriptor? EvolvesFrom(SpellDescriptor spell)
	{
		return DatabaseObject<SpellDescriptor>.Lookup.Select((KeyValuePair<Guid, IDatabaseObject> pair) => pair.Value).OfType<SpellDescriptor>().FirstOrDefault((SpellDescriptor other) => other.Id != spell.Id && other.Mastery.EvolvesIntoId == spell.Id);
	}

	public static List<SpellDescriptor> Lineage(SpellDescriptor spell)
	{
		SpellDescriptor spellDescriptor = spell;
		HashSet<Guid> hashSet = new HashSet<Guid> { spell.Id };
		while (true)
		{
			SpellDescriptor spellDescriptor2 = EvolvesFrom(spellDescriptor);
			if (spellDescriptor2 == null || !hashSet.Add(spellDescriptor2.Id) || hashSet.Count > 4)
			{
				break;
			}
			spellDescriptor = spellDescriptor2;
		}
		List<SpellDescriptor> list = new List<SpellDescriptor> { spellDescriptor };
		hashSet = new HashSet<Guid> { spellDescriptor.Id };
		while (list.Count < 4)
		{
			List<SpellDescriptor> list2 = list;
			SpellDescriptor spellDescriptor3 = EvolvesInto(list2[list2.Count - 1]);
			if (spellDescriptor3 == null || !hashSet.Add(spellDescriptor3.Id))
			{
				break;
			}
			list.Add(spellDescriptor3);
		}
		if (!list.Contains(spell))
		{
			int num = 1;
			List<SpellDescriptor> list3 = new List<SpellDescriptor>(num);
			CollectionsMarshal.SetCount(list3, num);
			Span<SpellDescriptor> span = CollectionsMarshal.AsSpan(list3);
			int num2 = 0;
			span[num2] = spell;
			num2++;
			list = list3;
		}
		return list;
	}

	public static TechniqueFamily FamilyOf(SpellDescriptor spell)
	{
		if (spell.Mastery.Family != TechniqueFamily.None)
		{
			return spell.Mastery.Family;
		}
		if (spell.SpellType != SpellType.CombatSpell)
		{
			return TechniqueFamily.Support;
		}
		if (spell.Combat.Effect == SpellEffect.Transform && spell.Combat.TargetType == SpellTargetType.Self)
		{
			return TechniqueFamily.Transformation;
		}
		if (spell.Combat.Friendly || spell.Combat.VitalDiff[0] <= 0)
		{
			return TechniqueFamily.Support;
		}
		if (spell.Combat.TargetType != SpellTargetType.Projectile)
		{
			return TechniqueFamily.Melee;
		}
		return TechniqueFamily.KiBlast;
	}

	public static bool IsOffensive(SpellDescriptor spell)
	{
		if (spell.SpellType == SpellType.CombatSpell && !spell.Combat.Friendly)
		{
			return spell.Combat.VitalDiff[0] > 0;
		}
		return false;
	}
}
