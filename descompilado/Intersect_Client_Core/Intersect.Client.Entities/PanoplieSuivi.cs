using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Framework.Items;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.DescriptionWindows;
using Intersect.Client.Localization;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.Panoplies;
using Intersect.Models;

namespace Intersect.Client.Entities;

public static class PanoplieSuivi
{
	private const long SilenceALaConnexionMs = 5000L;

	private static Guid _joueur;

	private static long _depuis;

	private static Dictionary<Guid, int> _pieces = new Dictionary<Guid, int>();

	public static void ApresEquipement()
	{
		Verifier(annoncer: true);
	}

	public static void ApresSacComplet()
	{
		Verifier(annoncer: false);
	}

	private static void Verifier(bool annoncer)
	{
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		HashSet<Guid> portes = new HashSet<Guid>();
		int[] myEquipment = me.MyEquipment;
		foreach (int num in myEquipment)
		{
			if (num >= 0 && num < me.Inventory.Length)
			{
				IItem item = me.Inventory[num];
				if (item != null && item.ItemId != Guid.Empty)
				{
					portes.Add(item.ItemId);
				}
			}
		}
		Dictionary<Guid, int> dictionary = DatabaseObject<PanoplieDescriptor>.Lookup.Values.OfType<PanoplieDescriptor>().ToDictionary((PanoplieDescriptor set) => set.Id, (PanoplieDescriptor set) => set.PiecesPortees(portes));
		long milliseconds = Timing.Global.Milliseconds;
		if (_joueur != me.Id)
		{
			_joueur = me.Id;
			_depuis = milliseconds;
		}
		if (milliseconds - _depuis < 5000)
		{
			annoncer = false;
		}
		if (annoncer)
		{
			foreach (KeyValuePair<Guid, int> item2 in dictionary)
			{
				item2.Deconstruct(out var key, out var value);
				Guid guid = key;
				int pieces = value;
				int avant = _pieces.GetValueOrDefault(guid);
				if (pieces <= avant)
				{
					continue;
				}
				PanoplieDescriptor panoplieDescriptor = DatabaseObject<PanoplieDescriptor>.Get(guid);
				if (panoplieDescriptor == null)
				{
					continue;
				}
				string icon = panoplieDescriptor.Items.Select(DatabaseObject<ItemDescriptor>.Get).FirstOrDefault((ItemDescriptor descriptor) => descriptor != null)?.Icon ?? string.Empty;
				foreach (PanoplieBonus item3 in from bonus in panoplieDescriptor.Bonuses
					where bonus.Pieces > avant && bonus.Pieces <= pieces
					orderby bonus.Pieces
					select bonus)
				{
					string text = string.Join(" · ", ItemDescriptionWindow.Lignes(item3));
					LootFeedback.OnPanoplie(panoplieDescriptor.Name, icon, Strings.ItemDescription.SetBonusActive.ToString(item3.Pieces, text).ToUpperInvariant());
				}
			}
		}
		_pieces = dictionary;
	}
}
