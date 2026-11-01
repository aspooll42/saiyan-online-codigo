using System;
using System.Collections.Generic;
using System.Linq;

namespace Intersect.Framework.Core.GameObjects.Items;

public static class ReglesDeCharge
{
	public const int Base = 1000;

	public const int ParNiveau = 10;

	public const int ParNiveauDeMetier = 5;

	private static readonly int[] PoidsParEmplacement = new int[8] { 2, 8, 3, 4, 5, 0, 3, 6 };

	public static int PoidsUnitaire(ItemDescriptor objet)
	{
		if (objet.Poids >= 0)
		{
			return objet.Poids;
		}
		switch (objet.ItemType)
		{
		case ItemType.Currency:
		case ItemType.Spell:
		case ItemType.Event:
		case ItemType.Bag:
			return 0;
		case ItemType.Equipment:
			if (objet.EquipmentSlot < 0 || objet.EquipmentSlot >= PoidsParEmplacement.Length)
			{
				return 5;
			}
			return PoidsParEmplacement[objet.EquipmentSlot];
		case ItemType.Consumable:
			return 1;
		default:
			return (!string.IsNullOrWhiteSpace(objet.Folder)) ? 1 : 0;
		}
	}

	public static int BonusDeCapsule(ItemDescriptor objet)
	{
		if (objet.ItemType != ItemType.Bag)
		{
			return 0;
		}
		int rarity = objet.Rarity;
		return (rarity <= 1) ? 50 : (rarity switch
		{
			2 => 100, 
			3 => 250, 
			4 => 400, 
			_ => 600, 
		});
	}

	public static int BonusDesMetiers(IEnumerable<int> niveauxDeMetier)
	{
		return 5 * niveauxDeMetier.Sum((int niveau) => Math.Max(0, niveau - 1));
	}

	public static int Capacite(int niveau, IEnumerable<int> niveauxDeMetier, int bonusDeCapsule)
	{
		return 1000 + 10 * Math.Max(0, niveau) + BonusDesMetiers(niveauxDeMetier) + Math.Max(0, bonusDeCapsule);
	}
}
