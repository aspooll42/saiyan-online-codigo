using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Items;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.DescriptionWindows.Components;
using Intersect.Client.Interface.Game.Inventaire;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.Panoplies;
using Intersect.GameObjects.Ranges;
using Intersect.Localization;
using Intersect.Models;
using Intersect.Utilities;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Interface.Game.DescriptionWindows;

public class ItemDescriptionWindow : DescriptionWindowBase
{
	private ItemDescriptor? _itemDescriptor;

	private ItemProperties? _itemProperties;

	private int _amount;

	private string? _valueLabel;

	private static readonly Color PanoplieTitre = new Color(255, 245, 190, 90);

	private static readonly Color PanoplieActif = new Color(255, 142, 214, 124);

	private static readonly Color PanoplieEteint = new Color(255, 120, 126, 140);

	public ItemDescriptionWindow()
		: base(Interface.GameUi.GameCanvas, "DescriptionWindow")
	{
	}

	public void Show(ItemDescriptor item, int amount, ItemProperties? itemProperties = null, string valueLabel = "")
	{
		_itemDescriptor = item;
		_amount = amount;
		_itemProperties = itemProperties;
		_valueLabel = valueLabel;
		SetupDescriptionWindow();
		PositionToHoveredControl();
		if (_itemDescriptor.ItemType == ItemType.Spell && _itemDescriptor.SpellId != Guid.Empty)
		{
			if (base.Canvas == null)
			{
				return;
			}
			Base hoveredControl = InputHandler.HoveredControl;
			if (hoveredControl == null)
			{
				return;
			}
			SpellDescriptionWindow spellDescriptionWindow = Interface.GameUi.SpellDescriptionWindow;
			spellDescriptionWindow.Show(_itemDescriptor.SpellId);
			Point point = InputHandler.HoveredControl.ToCanvas(new Point(0, 0));
			int num = 0;
			int y = base.Y;
			if (base.Y + spellDescriptionWindow.Height > base.Canvas.Height)
			{
				y = base.Canvas.Height - spellDescriptionWindow.Height;
			}
			if (base.X >= point.X + hoveredControl.Width)
			{
				num = point.X - spellDescriptionWindow.Width;
				if (num < 0)
				{
					num = base.X + base.Width;
				}
			}
			else
			{
				num = point.X + hoveredControl.Width;
				if (num + spellDescriptionWindow.Width > base.Canvas.Width)
				{
					num = base.X - spellDescriptionWindow.Width;
				}
			}
			spellDescriptionWindow.SetPosition(num, y);
		}
		base.Show();
	}

	public override void Hide()
	{
		if (Interface.GameUi.ItemDescriptionWindow == this)
		{
			Interface.GameUi.GameCanvas.RemoveChild(Interface.GameUi.ItemDescriptionWindow, dispose: true);
			Interface.GameUi.ItemDescriptionWindow = null;
		}
		if (Interface.GameUi.SpellDescriptionWindow != null)
		{
			Interface.GameUi.GameCanvas.RemoveChild(Interface.GameUi.SpellDescriptionWindow, dispose: true);
			Interface.GameUi.SpellDescriptionWindow = null;
		}
	}

	protected void SetupDescriptionWindow()
	{
		if (_itemDescriptor != null)
		{
			SetupHeader();
			SetupLevelRequirement();
			if (!string.IsNullOrWhiteSpace(_itemDescriptor.Description))
			{
				SetupDescription();
			}
			SetupItemLimits();
			switch (_itemDescriptor.ItemType)
			{
			case ItemType.Equipment:
				SetupEquipmentInfo();
				SetupPanoplieInfo();
				break;
			case ItemType.Consumable:
				SetupConsumableInfo();
				break;
			case ItemType.Spell:
				SetupSpellInfo();
				break;
			case ItemType.Bag:
				SetupBagInfo();
				break;
			}
			SetupExtraInfo();
			FinalizeWindow();
		}
	}

	protected void SetupHeader()
	{
		if (_itemDescriptor == null)
		{
			return;
		}
		HeaderComponent headerComponent = AddHeader();
		IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Item, _itemDescriptor.Icon);
		if (texture != null)
		{
			headerComponent.SetIcon(texture, _itemDescriptor.Color);
		}
		int num = JetDObjet.Rarete(_itemDescriptor, _itemProperties);
		int num2 = ((!JetDObjet.Qualite(_itemDescriptor, _itemProperties).HasValue) ? JetDObjet.Plafond(_itemDescriptor) : 0);
		CustomColors.Items.Rarities.TryGetValue(num, out var value);
		headerComponent.SetTitle(_itemDescriptor.Name, value);
		List<string> list = new List<string>();
		Strings.ItemDescription.ItemTypes.TryGetValue((int)_itemDescriptor.ItemType, out LocalizedString value2);
		if (!string.IsNullOrWhiteSpace(value2))
		{
			list.Add(value2);
		}
		if (_itemDescriptor.ItemType == ItemType.Equipment)
		{
			string text = Options.Instance.Equipment.Slots[_itemDescriptor.EquipmentSlot];
			string text2 = ((_itemDescriptor.EquipmentSlot == Options.Instance.Equipment.WeaponSlot && _itemDescriptor.TwoHanded) ? $"{Strings.ItemDescription.TwoHand} {text}" : text);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				list.Add(text2);
			}
		}
		try
		{
			if (num > 0 && Options.Instance.Items.TryGetRarityName(num, out string rarityName) && Strings.ItemDescription.Rarity.TryGetValue(rarityName, out LocalizedString value3) && !string.IsNullOrWhiteSpace(value3))
			{
				if (num2 >= 2 && Options.Instance.Items.TryGetRarityName(1, out string rarityName2) && Strings.ItemDescription.Rarity.TryGetValue(rarityName2, out LocalizedString value4) && !string.IsNullOrWhiteSpace(value4))
				{
					list.Add(Strings.ItemDescription.RarityRange.ToString(value4, value3));
				}
				else
				{
					list.Add(value3);
				}
			}
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, "Error setting rarity description for rarity {Rarity}", _itemDescriptor.Rarity);
			throw;
		}
		headerComponent.SetSubtitle(string.Join(Strings.ItemDescription.ClassificationSeparator, list));
		headerComponent.SetDescription(string.Empty);
		headerComponent.SizeToChildren(resizeX: true, resizeY: false);
	}

	protected void SetupLevelRequirement()
	{
		if (_itemDescriptor != null)
		{
			int num = FenetreInventaire.NiveauRequis(_itemDescriptor);
			if (num > 1)
			{
				Player me = Globals.Me;
				bool flag = me != null && me.Level < num;
				AddDescription().AddText(Strings.Inventaire.LevelRequired.ToString(num), flag ? TechniqueArt.Red : TechniqueArt.Soft);
			}
		}
	}

	protected void SetupItemLimits()
	{
		if (_itemDescriptor != null)
		{
			List<string> list = new List<string>();
			if (!_itemDescriptor.CanBank)
			{
				list.Add(Strings.ItemDescription.Banked);
			}
			if (!_itemDescriptor.CanGuildBank)
			{
				list.Add(Strings.ItemDescription.GuildBanked);
			}
			if (!_itemDescriptor.CanBag)
			{
				list.Add(Strings.ItemDescription.Bagged);
			}
			if (!_itemDescriptor.CanTrade)
			{
				list.Add(Strings.ItemDescription.Traded);
			}
			if (!_itemDescriptor.CanDrop)
			{
				list.Add(Strings.ItemDescription.Dropped);
			}
			if (!_itemDescriptor.CanSell)
			{
				list.Add(Strings.ItemDescription.Sold);
			}
			if (list.Count > 0)
			{
				AddDivider();
				DescriptionComponent descriptionComponent = AddDescription();
				descriptionComponent.AddText(Strings.ItemDescription.ItemLimits.ToString(string.Join(", ", list)));
			}
		}
	}

	protected void SetupDescription()
	{
		if (_itemDescriptor != null)
		{
			AddDivider();
			DescriptionComponent descriptionComponent = AddDescription();
			descriptionComponent.AddText(Strings.ItemDescription.Description.ToString(_itemDescriptor.Description));
		}
	}

	protected void SetupEquipmentInfo()
	{
		if (_itemDescriptor == null)
		{
			return;
		}
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		AddDivider();
		RowContainerComponent rowContainerComponent = AddRowContainer();
		if (_itemDescriptor.EquipmentSlot == Options.Instance.Equipment.WeaponSlot)
		{
			rowContainerComponent.AddKeyValueRow(Strings.ItemDescription.BaseDamage, _itemDescriptor.Damage.ToString());
			Strings.ItemDescription.DamageTypes.TryGetValue(_itemDescriptor.DamageType, out LocalizedString value);
			rowContainerComponent.AddKeyValueRow(Strings.ItemDescription.BaseDamageType, value);
			if (_itemDescriptor.Scaling > 0)
			{
				Strings.ItemDescription.Stats.TryGetValue(_itemDescriptor.ScalingStat, out LocalizedString value2);
				rowContainerComponent.AddKeyValueRow(Strings.ItemDescription.ScalingStat, value2);
				rowContainerComponent.AddKeyValueRow(Strings.ItemDescription.ScalingPercentage, Strings.ItemDescription.Percentage.ToString(_itemDescriptor.Scaling));
			}
			if (_itemDescriptor.CritChance > 0)
			{
				rowContainerComponent.AddKeyValueRow(Strings.ItemDescription.CritChance, Strings.ItemDescription.Percentage.ToString(_itemDescriptor.CritChance));
				rowContainerComponent.AddKeyValueRow(Strings.ItemDescription.CritMultiplier, Strings.ItemDescription.Multiplier.ToString(_itemDescriptor.CritMultiplier));
			}
			if (_itemDescriptor.AttackSpeedModifier == 0)
			{
				int num = me.Stat[4];
				int num2 = me.MyEquipment[Options.Instance.Equipment.WeaponSlot];
				if (num2 != -1)
				{
					int[] statModifiers = me.Inventory[num2].ItemProperties.StatModifiers;
					ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(me.Inventory[num2].ItemId);
					if (itemDescriptor != null && statModifiers != null)
					{
						num = (int)Math.Round((float)num / ((float)(100 + itemDescriptor.PercentageStatsGiven[4]) / 100f));
						num -= itemDescriptor.StatsGiven[4];
						num -= statModifiers[4];
					}
				}
				if (_itemProperties?.StatModifiers != null)
				{
					num += _itemDescriptor.StatsGiven[4];
					num += _itemProperties.StatModifiers[4];
					num += (int)Math.Floor((float)num * ((float)_itemDescriptor.PercentageStatsGiven[4] / 100f));
				}
				rowContainerComponent.AddKeyValueRow(Strings.ItemDescription.AttackSpeed, TimeSpan.FromMilliseconds(me.CalculateAttackTime(num)).WithSuffix());
			}
			else if (_itemDescriptor.AttackSpeedModifier == 1)
			{
				rowContainerComponent.AddKeyValueRow(Strings.ItemDescription.AttackSpeed, TimeSpan.FromMilliseconds(_itemDescriptor.AttackSpeedValue).WithSuffix());
			}
			else if (_itemDescriptor.AttackSpeedModifier == 2)
			{
				rowContainerComponent.AddKeyValueRow(Strings.ItemDescription.AttackSpeed, Strings.ItemDescription.Percentage.ToString(_itemDescriptor.AttackSpeedValue));
			}
		}
		if (_itemDescriptor.EquipmentSlot == Options.Instance.Equipment.ShieldSlot)
		{
			if (_itemDescriptor.BlockChance > 0)
			{
				rowContainerComponent.AddKeyValueRow(Strings.ItemDescription.BlockChance, Strings.ItemDescription.Percentage.ToString(_itemDescriptor.BlockChance));
			}
			if (_itemDescriptor.BlockAmount > 0)
			{
				rowContainerComponent.AddKeyValueRow(Strings.ItemDescription.BlockAmount, Strings.ItemDescription.Percentage.ToString(_itemDescriptor.BlockAmount));
			}
			if (_itemDescriptor.BlockAbsorption > 0)
			{
				rowContainerComponent.AddKeyValueRow(Strings.ItemDescription.BlockAbsorption, Strings.ItemDescription.Percentage.ToString(_itemDescriptor.BlockAbsorption));
			}
		}
		for (int i = 0; i < Enum.GetValues<Vital>().Length; i++)
		{
			string text = Strings.ItemDescription.Vitals[i].ToString();
			string text2 = text;
			int length = text.TrimEnd(' ', ':', '\u00a0').Length;
			string value3 = text2.Substring(length, text2.Length - length);
			string key = $"{((i == 0) ? Strings.Inventaire.MaxHealth : Strings.Inventaire.MaxKi)}{value3}";
			if (_itemDescriptor.VitalsGiven[i] != 0L && _itemDescriptor.PercentageVitalsGiven[i] != 0)
			{
				rowContainerComponent.AddKeyValueRow(key, Strings.ItemDescription.RegularAndPercentage.ToString(_itemDescriptor.VitalsGiven[i], _itemDescriptor.PercentageVitalsGiven[i]));
			}
			else if (_itemDescriptor.VitalsGiven[i] != 0L)
			{
				rowContainerComponent.AddKeyValueRow(key, _itemDescriptor.VitalsGiven[i].ToString());
			}
			else if (_itemDescriptor.PercentageVitalsGiven[i] != 0)
			{
				rowContainerComponent.AddKeyValueRow(key, Strings.ItemDescription.Percentage.ToString(_itemDescriptor.PercentageVitalsGiven[i]));
			}
		}
		for (int j = 0; j < Enum.GetValues<Vital>().Length; j++)
		{
			if (_itemDescriptor.VitalsRegen[j] != 0L)
			{
				rowContainerComponent.AddKeyValueRow(Strings.ItemDescription.VitalsRegen[j], Strings.ItemDescription.Percentage.ToString(_itemDescriptor.VitalsRegen[j]));
			}
		}
		int[] array = _itemProperties?.StatModifiers;
		Dictionary<Stat, (Stat, int, int)> dictionary = JetDObjet.Fourchettes(_itemDescriptor).ToDictionary(((Stat Stat, int Bas, int Haut) entree) => entree.Stat);
		for (int num3 = 0; num3 < Enum.GetValues<Stat>().Length; num3++)
		{
			Stat stat = (Stat)num3;
			LocalizedString localizedString = Strings.ItemDescription.StatCounts[num3];
			ItemRange range = null;
			int num4 = _itemDescriptor.PercentageStatsGiven[num3];
			ItemRange range2;
			if (array != null || !_itemDescriptor.TryGetRangeFor(stat, out range) || range.LowRange == range.HighRange)
			{
				int num5 = _itemDescriptor.StatsGiven[num3];
				if (array != null)
				{
					num5 += array[num3];
				}
				num5 += range?.LowRange ?? 0;
				if (array != null && dictionary.TryGetValue(stat, out var value4))
				{
					int num6 = _itemDescriptor.StatsGiven[num3];
					KeyValueRowComponent keyValueRowComponent = rowContainerComponent.AddKeyValueRow(Strings.ItemDescription.RolledStat.ToString(Strings.ItemDescription.Stats[num3].ToString(), Strings.ItemDescription.RolledRange.ToString(num6 + value4.Item2, num6 + value4.Item3)), (num4 != 0) ? Strings.ItemDescription.RegularAndPercentage.ToString(num5, num4) : num5.ToString());
					int num7 = array[num3];
					if (num7 >= value4.Item3)
					{
						keyValueRowComponent.SetValueTextColor(MarketWindow.RollColor(1.0));
					}
					else if (num7 <= value4.Item2)
					{
						keyValueRowComponent.SetValueTextColor(MarketWindow.RollColor(0.0));
					}
				}
				else if (num5 != 0 && num4 != 0)
				{
					rowContainerComponent.AddKeyValueRow(localizedString, Strings.ItemDescription.RegularAndPercentage.ToString(num5, num4));
				}
				else if (num5 != 0)
				{
					rowContainerComponent.AddKeyValueRow(localizedString, num5.ToString());
				}
				else if (num4 != 0)
				{
					rowContainerComponent.AddKeyValueRow(localizedString, Strings.ItemDescription.Percentage.ToString(num4));
				}
			}
			else if (_itemDescriptor.TryGetRangeFor(stat, out range2))
			{
				int num8 = _itemDescriptor.StatsGiven[num3];
				int num9 = num4;
				int num10 = num8 + range2.LowRange;
				int num11 = num8 + range2.HighRange;
				string text3 = Strings.ItemDescription.StatGrowthRange.ToString(num10, num11);
				if (num9 != 0)
				{
					text3 = Strings.ItemDescription.RegularAndPercentage.ToString(text3, num9);
				}
				rowContainerComponent.AddKeyValueRow(localizedString, text3);
			}
		}
		double? num12 = JetDObjet.Qualite(_itemDescriptor, _itemProperties);
		if (num12.HasValue)
		{
			double valueOrDefault = num12.GetValueOrDefault();
			string text4 = MarketWindow.RollTier(valueOrDefault) switch
			{
				3 => Strings.ItemDescription.RollPerfect, 
				2 => Strings.ItemDescription.RollGood, 
				1 => Strings.ItemDescription.RollAverage, 
				_ => Strings.ItemDescription.RollWeak, 
			};
			KeyValueRowComponent keyValueRowComponent2 = rowContainerComponent.AddKeyValueRow(Strings.ItemDescription.Roll, Strings.ItemDescription.RollValue.ToString(MarketWindow.RollPercent(valueOrDefault), text4));
			keyValueRowComponent2.SetValueTextColor(MarketWindow.RollColor(valueOrDefault));
		}
		foreach (EffectData effect in _itemDescriptor.Effects)
		{
			if (effect.Type != ItemEffect.None && effect.Percentage != 0)
			{
				rowContainerComponent.AddKeyValueRow(Strings.ItemDescription.BonusEffects[(int)effect.Type], Strings.ItemDescription.Percentage.ToString(effect.Percentage));
			}
		}
		rowContainerComponent.SizeToChildren();
	}

	protected void SetupConsumableInfo()
	{
		if (_itemDescriptor == null)
		{
			return;
		}
		AddDivider();
		RowContainerComponent rowContainerComponent = AddRowContainer();
		if (_itemDescriptor.Consumable != null)
		{
			if (_itemDescriptor.Consumable.Value > 0 && _itemDescriptor.Consumable.Percentage > 0)
			{
				rowContainerComponent.AddKeyValueRow(Strings.ItemDescription.ConsumableTypes[(int)_itemDescriptor.Consumable.Type], Strings.ItemDescription.RegularAndPercentage.ToString(_itemDescriptor.Consumable.Value, _itemDescriptor.Consumable.Percentage));
			}
			else if (_itemDescriptor.Consumable.Value > 0)
			{
				rowContainerComponent.AddKeyValueRow(Strings.ItemDescription.ConsumableTypes[(int)_itemDescriptor.Consumable.Type], _itemDescriptor.Consumable.Value.ToString());
			}
			else if (_itemDescriptor.Consumable.Percentage > 0)
			{
				rowContainerComponent.AddKeyValueRow(Strings.ItemDescription.ConsumableTypes[(int)_itemDescriptor.Consumable.Type], Strings.ItemDescription.Percentage.ToString(_itemDescriptor.Consumable.Percentage));
			}
		}
		rowContainerComponent.SizeToChildren();
	}

	protected void SetupSpellInfo()
	{
		if (_itemDescriptor == null)
		{
			return;
		}
		AddDivider();
		RowContainerComponent rowContainerComponent = AddRowContainer();
		if (_itemDescriptor.Spell != null)
		{
			if (_itemDescriptor.QuickCast)
			{
				rowContainerComponent.AddKeyValueRow(Strings.ItemDescription.CastSpell.ToString(_itemDescriptor.Spell.Name), string.Empty);
			}
			else
			{
				rowContainerComponent.AddKeyValueRow(Strings.ItemDescription.TeachSpell.ToString(_itemDescriptor.Spell.Name), string.Empty);
			}
			if (_itemDescriptor.SingleUse)
			{
				rowContainerComponent.AddKeyValueRow(Strings.ItemDescription.SingleUse, string.Empty);
			}
		}
		rowContainerComponent.SizeToChildren();
	}

	protected void SetupBagInfo()
	{
		if (_itemDescriptor != null)
		{
			AddDivider();
			RowContainerComponent rowContainerComponent = AddRowContainer();
			rowContainerComponent.AddKeyValueRow(Strings.ItemDescription.BagSlots, _itemDescriptor.SlotCount.ToString());
			rowContainerComponent.SizeToChildren();
		}
	}

	protected void SetupExtraInfo()
	{
		if (_itemDescriptor == null)
		{
			return;
		}
		List<Tuple<string, string>> list = new List<Tuple<string, string>>();
		if (_itemDescriptor.IsStackable && _amount > 1)
		{
			list.Add(new Tuple<string, string>(Strings.ItemDescription.Amount, _amount.ToString("N0").Replace(",", Strings.Numbers.Comma)));
		}
		if (_itemDescriptor.DropChanceOnDeath > 0)
		{
			list.Add(new Tuple<string, string>(Strings.ItemDescription.DropOnDeath, Strings.ItemDescription.Percentage.ToString(_itemDescriptor.DropChanceOnDeath)));
		}
		if (!string.IsNullOrWhiteSpace(_valueLabel))
		{
			list.Add(new Tuple<string, string>(_valueLabel, string.Empty));
		}
		else if (_itemDescriptor.Price > 0)
		{
			list.Add(new Tuple<string, string>(Strings.ItemDescription.EstimatedValue, MarketWindow.FormatAmount(_itemDescriptor.Price)));
		}
		if (list.Count <= 0)
		{
			return;
		}
		AddDivider();
		RowContainerComponent rowContainerComponent = AddRowContainer();
		foreach (Tuple<string, string> item in list)
		{
			rowContainerComponent.AddKeyValueRow(item.Item1, item.Item2);
		}
		rowContainerComponent.SizeToChildren();
	}

	protected void SetupPanoplieInfo()
	{
		if (_itemDescriptor == null)
		{
			return;
		}
		PanoplieDescriptor panoplieDescriptor = PanoplieDescriptor.DeLObjet(_itemDescriptor.Id);
		if (panoplieDescriptor == null)
		{
			return;
		}
		HashSet<Guid> hashSet = new HashSet<Guid>();
		Player me = Globals.Me;
		if (me != null)
		{
			int[] myEquipment = me.MyEquipment;
			foreach (int num in myEquipment)
			{
				if (num >= 0 && num < me.Inventory.Length)
				{
					IItem item = me.Inventory[num];
					if (item != null && item.ItemId != Guid.Empty)
					{
						hashSet.Add(item.ItemId);
					}
				}
			}
		}
		int num2 = panoplieDescriptor.PiecesPortees(hashSet);
		int num3 = panoplieDescriptor.Items.Distinct().Count();
		AddDivider();
		RowContainerComponent rowContainerComponent = AddRowContainer();
		KeyValueRowComponent keyValueRowComponent = rowContainerComponent.AddKeyValueRow(panoplieDescriptor.Name, Strings.ItemDescription.SetPieces.ToString(num2, num3));
		keyValueRowComponent.SetKeyTextColor(PanoplieTitre);
		keyValueRowComponent.SetValueTextColor((num2 >= 2) ? PanoplieActif : PanoplieEteint);
		foreach (PanoplieBonus item2 in panoplieDescriptor.Bonuses.OrderBy((PanoplieBonus bonus) => bonus.Pieces))
		{
			Color color = ((item2.Pieces <= num2) ? PanoplieActif : PanoplieEteint);
			bool flag = true;
			foreach (string item3 in Lignes(item2))
			{
				KeyValueRowComponent keyValueRowComponent2 = rowContainerComponent.AddKeyValueRow(flag ? Strings.ItemDescription.SetBonusPieces.ToString(item2.Pieces) : string.Empty, item3);
				keyValueRowComponent2.SetKeyTextColor(color);
				keyValueRowComponent2.SetValueTextColor(color);
				flag = false;
			}
		}
		rowContainerComponent.SizeToChildren();
	}

	internal static IEnumerable<string> Lignes(PanoplieBonus bonus)
	{
		for (int i = 0; i < bonus.Stats.Length; i++)
		{
			if (bonus.Stats[i] != 0 && Strings.ItemDescription.StatCounts.TryGetValue(i, out LocalizedString value))
			{
				yield return $"{Libelle(value)} {bonus.Stats[i]:+0;-0}";
			}
		}
		for (int i = 0; i < bonus.Vitals.Length; i++)
		{
			if (bonus.Vitals[i] != 0L)
			{
				yield return $"{((i == 0) ? Strings.Inventaire.MaxHealth : Strings.Inventaire.MaxKi)} {bonus.Vitals[i]:+0;-0}";
			}
		}
		if (bonus.Effect != ItemEffect.None && bonus.EffectPercentage != 0 && Strings.ItemDescription.BonusEffects.TryGetValue((int)bonus.Effect, out LocalizedString value2))
		{
			yield return $"{Libelle(value2)} +{bonus.EffectPercentage} %";
		}
		static string Libelle(string texte)
		{
			return texte.TrimEnd(' ', ':', '\u00a0');
		}
	}
}
