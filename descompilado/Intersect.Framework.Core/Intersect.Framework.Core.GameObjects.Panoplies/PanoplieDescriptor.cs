using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Panoplies;

public class PanoplieDescriptor : DatabaseObject<PanoplieDescriptor>, IFolderable
{
	[NotMapped]
	[JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
	public List<Guid> Items { get; set; } = new List<Guid>();

	[Column("Items")]
	[JsonIgnore]
	public string ItemsJson
	{
		get
		{
			return JsonConvert.SerializeObject(Items);
		}
		set
		{
			Items = (string.IsNullOrWhiteSpace(value) ? new List<Guid>() : (JsonConvert.DeserializeObject<List<Guid>>(value) ?? new List<Guid>()));
		}
	}

	[NotMapped]
	[JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
	public List<PanoplieBonus> Bonuses { get; set; } = new List<PanoplieBonus>();

	[Column("Bonuses")]
	[JsonIgnore]
	public string BonusesJson
	{
		get
		{
			return JsonConvert.SerializeObject(Bonuses);
		}
		set
		{
			Bonuses = (string.IsNullOrWhiteSpace(value) ? new List<PanoplieBonus>() : (JsonConvert.DeserializeObject<List<PanoplieBonus>>(value) ?? new List<PanoplieBonus>()));
		}
	}

	public string Folder { get; set; } = string.Empty;

	[JsonConstructor]
	public PanoplieDescriptor(Guid id)
		: base(id)
	{
		base.Name = "Nouvelle panoplie";
	}

	public PanoplieDescriptor()
	{
		base.Name = "Nouvelle panoplie";
	}

	public int PiecesPortees(IReadOnlySet<Guid> portes)
	{
		return Items.Distinct().Count(portes.Contains);
	}

	public IEnumerable<PanoplieBonus> BonusAtteints(int pieces)
	{
		return Bonuses.Where((PanoplieBonus bonus) => bonus.Pieces >= 2 && bonus.Pieces <= pieces);
	}

	public static PanoplieDescriptor? DeLObjet(Guid itemId)
	{
		if (!(itemId == Guid.Empty))
		{
			return DatabaseObject<PanoplieDescriptor>.Lookup.Values.OfType<PanoplieDescriptor>().FirstOrDefault((PanoplieDescriptor set) => set.Items.Contains(itemId));
		}
		return null;
	}

	public static IEnumerable<PanoplieBonus> BonusActifs(IEnumerable<Guid> portes)
	{
		HashSet<Guid> ids = portes.Where((Guid id) => id != Guid.Empty).ToHashSet();
		if (ids.Count < 2)
		{
			return Array.Empty<PanoplieBonus>();
		}
		return DatabaseObject<PanoplieDescriptor>.Lookup.Values.OfType<PanoplieDescriptor>().SelectMany((PanoplieDescriptor set) => set.BonusAtteints(set.PiecesPortees(ids))).ToList();
	}

	public static int BonusDeStat(IEnumerable<Guid> portes, Stat stat)
	{
		return BonusActifs(portes).Sum((PanoplieBonus bonus) => ((int)stat < bonus.Stats.Length) ? bonus.Stats[(int)stat] : 0);
	}

	public static long BonusDeVital(IEnumerable<Guid> portes, int vital)
	{
		return BonusActifs(portes).Sum((PanoplieBonus bonus) => (vital < 0 || vital >= bonus.Vitals.Length) ? 0 : bonus.Vitals[vital]);
	}

	public static int BonusDEffet(IEnumerable<Guid> portes, ItemEffect effect)
	{
		if (effect != ItemEffect.None)
		{
			return (from bonus in BonusActifs(portes)
				where bonus.Effect == effect
				select bonus).Sum((PanoplieBonus bonus) => bonus.EffectPercentage);
		}
		return 0;
	}
}
