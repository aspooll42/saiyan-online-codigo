using System;
using Intersect.Client.General;
using Intersect.Framework.Core;
using Intersect.GameObjects;
using Intersect.Models;

namespace Intersect.Client.Spells;

public static class TechniqueProgress
{
	public readonly record struct RankUp(int Slot, Guid SpellId, int Rank, long At);

	public readonly record struct Evolution(int Slot, Guid FromId, Guid ToId, long At);

	public static RankUp? LastRankUp { get; private set; }

	public static Evolution? LastEvolution { get; private set; }

	public static void Observe(int slot, Guid previousId, int previousRank, Spell now)
	{
		if (previousId == Guid.Empty || now.Id == Guid.Empty)
		{
			return;
		}
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		SpellDescriptor databaseObject;
		if (previousId == now.Id)
		{
			if (now.Rank > previousRank)
			{
				LastRankUp = new RankUp(slot, now.Id, now.Rank, millisecondsUtc);
			}
		}
		else if (DatabaseObject<SpellDescriptor>.TryGet(previousId, out databaseObject) && databaseObject.Mastery.EvolvesIntoId == now.Id)
		{
			LastEvolution = new Evolution(slot, previousId, now.Id, millisecondsUtc);
		}
	}

	public static int SlotOf(Guid spellId)
	{
		Spell[] array = Globals.Me?.Spells;
		if (array == null || spellId == Guid.Empty)
		{
			return -1;
		}
		for (int i = 0; i < array.Length; i++)
		{
			Spell obj = array[i];
			if (obj != null && obj.Id == spellId)
			{
				return i;
			}
		}
		return -1;
	}

	public static int RankOf(Guid spellId)
	{
		int num = SlotOf(spellId);
		if (num >= 0)
		{
			return SpellMastery.Clamp(Globals.Me.Spells[num].Rank);
		}
		return 1;
	}
}
