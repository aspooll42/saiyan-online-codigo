using System;
using System.Collections.Generic;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Interface.Game.DescriptionWindows.Components;
using Intersect.Client.Localization;
using Intersect.Client.Spells;
using Intersect.Enums;
using Intersect.GameObjects;
using Intersect.Localization;
using Intersect.Models;
using Intersect.Utilities;

namespace Intersect.Client.Interface.Game.DescriptionWindows;

public class SpellDescriptionWindow : DescriptionWindowBase
{
	private SpellDescriptor? _spellDescriptor;

	private int Rank
	{
		get
		{
			if (_spellDescriptor != null)
			{
				return TechniqueProgress.RankOf(_spellDescriptor.Id);
			}
			return 1;
		}
	}

	public SpellDescriptionWindow()
		: base(Interface.GameUi.GameCanvas, "DescriptionWindow")
	{
	}

	public void Show(Guid spellId, ItemDescriptionWindow? itemDecriptionContainer = null)
	{
		_spellDescriptor = DatabaseObject<SpellDescriptor>.Get(spellId);
		if (_spellDescriptor != null && itemDecriptionContainer == null)
		{
			base.Texture = null;
			base.TextureFilename = null;
			base.Padding = Padding.Zero;
			TechniqueTooltip techniqueTooltip = new TechniqueTooltip(this, _spellDescriptor);
			techniqueTooltip.SetPosition(0, 0);
			SetSize(techniqueTooltip.Width, techniqueTooltip.Height);
			PlaceCard();
			base.Show();
		}
		else
		{
			SetupDescriptionWindow();
			if (itemDecriptionContainer != null)
			{
				SetPosition(itemDecriptionContainer.X, itemDecriptionContainer.Y + itemDecriptionContainer.Height);
				SetSize(itemDecriptionContainer.Width, itemDecriptionContainer.Height);
			}
			else
			{
				PositionToHoveredControl();
			}
			base.Show();
		}
	}

	private void PlaceCard()
	{
		Base hoveredControl = InputHandler.HoveredControl;
		if (base.Canvas == null || hoveredControl == null)
		{
			PositionToHoveredControl();
			return;
		}
		Point point = hoveredControl.ToCanvas(new Point(0, 0));
		if ((float)(point.Y + hoveredControl.Height / 2) < (float)base.Canvas.Height * 0.6f)
		{
			PositionToHoveredControl();
			return;
		}
		int x = Math.Clamp(point.X + hoveredControl.Width / 2 - base.Width / 2, 0, Math.Max(0, base.Canvas.Width - base.Width));
		int y = Math.Max(0, point.Y - base.Height - 12);
		MoveTo(x, y);
	}

	public override void Hide()
	{
		if (Interface.GameUi.SpellDescriptionWindow == this)
		{
			Interface.GameUi.GameCanvas.RemoveChild(Interface.GameUi.SpellDescriptionWindow, dispose: true);
			Interface.GameUi.SpellDescriptionWindow = null;
		}
	}

	protected void SetupDescriptionWindow()
	{
		if (_spellDescriptor != null)
		{
			SetupHeader();
			if (!string.IsNullOrWhiteSpace(_spellDescriptor.Description))
			{
				SetupDescription();
			}
			SetupSpellInfo();
			switch (_spellDescriptor.SpellType)
			{
			case SpellType.CombatSpell:
			case SpellType.WarpTo:
				SetupCombatInfo();
				break;
			case SpellType.Dash:
				SetupDashInfo();
				break;
			}
			SetupExtraInfo();
			FinalizeWindow();
		}
	}

	protected void SetupHeader()
	{
		if (_spellDescriptor == null)
		{
			return;
		}
		HeaderComponent headerComponent = AddHeader();
		IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Spell, _spellDescriptor.Icon);
		if (texture != null)
		{
			headerComponent.SetIcon(texture, Color.White);
		}
		headerComponent.SetTitle(_spellDescriptor.Name);
		List<string> list = new List<string>();
		Strings.SpellDescription.SpellTypes.TryGetValue((int)_spellDescriptor.SpellType, out LocalizedString value);
		SpellType spellType = _spellDescriptor.SpellType;
		if ((spellType == SpellType.CombatSpell || spellType == SpellType.WarpTo) ? true : false)
		{
			LocalizedString value2;
			string text = ((_spellDescriptor.Combat.Effect != SpellEffect.None && Strings.SpellDescription.Effects.TryGetValue((int)_spellDescriptor.Combat.Effect, out value2) && !string.IsNullOrWhiteSpace(value2)) ? value2.ToString() : string.Empty);
			value = ((text.Length > 0) ? ((LocalizedString)text) : (_spellDescriptor.Combat.Friendly ? Strings.SpellDescription.Friendly : Strings.SpellDescription.Unfriendly));
		}
		if (!string.IsNullOrWhiteSpace(value))
		{
			list.Add(value);
		}
		if (_spellDescriptor.SpellType == SpellType.CombatSpell)
		{
			string text2 = ((_spellDescriptor.Combat.TargetType == SpellTargetType.Projectile) ? Strings.SpellDescription.TargetTypes[(int)_spellDescriptor.Combat.TargetType].ToString(DatabaseObject<ProjectileDescriptor>.Get(_spellDescriptor.Combat.ProjectileId)?.Range ?? 0, _spellDescriptor.Combat.HitRadius) : Strings.SpellDescription.TargetTypes[(int)_spellDescriptor.Combat.TargetType].ToString(_spellDescriptor.Combat.CastRange, _spellDescriptor.Combat.HitRadius));
			if (!string.IsNullOrWhiteSpace(text2))
			{
				list.Add(text2);
			}
		}
		if (SpellMastery.HasMastery(_spellDescriptor) && TechniqueProgress.SlotOf(_spellDescriptor.Id) >= 0)
		{
			list.Add(Strings.Techniques.Rank.ToString(Rank));
		}
		headerComponent.SetSubtitle(string.Join(Strings.ItemDescription.ClassificationSeparator, list));
		headerComponent.SetDescription(string.Empty);
		headerComponent.SizeToChildren(resizeX: true, resizeY: false);
	}

	protected void SetupSpellInfo()
	{
		if (_spellDescriptor == null)
		{
			return;
		}
		AddDivider();
		RowContainerComponent rowContainerComponent = AddRowContainer();
		LocalizedString localizedString = Strings.SpellDescription.Instant;
		int num = SpellMastery.CastDuration(_spellDescriptor, Rank);
		if (num > 0)
		{
			localizedString = TimeSpan.FromMilliseconds(num).WithSuffix();
		}
		rowContainerComponent.AddKeyValueRow(Strings.SpellDescription.CastTime, localizedString);
		for (int i = 0; i < Enum.GetValues<Vital>().Length; i++)
		{
			long num2 = SpellMastery.VitalCost(_spellDescriptor, (Vital)i, Rank);
			if (num2 != 0L)
			{
				rowContainerComponent.AddKeyValueRow(Strings.SpellDescription.VitalCosts[i], num2.ToString());
			}
		}
		int num3 = SpellMastery.Cooldown(_spellDescriptor, Rank);
		if (num3 > 0)
		{
			rowContainerComponent.AddKeyValueRow(Strings.SpellDescription.Cooldown, TimeSpan.FromMilliseconds(num3).WithSuffix());
		}
		if (!string.IsNullOrWhiteSpace(_spellDescriptor.CooldownGroup))
		{
			rowContainerComponent.AddKeyValueRow(Strings.SpellDescription.CooldownGroup, _spellDescriptor.CooldownGroup);
		}
		if (Options.Instance.Combat.EnableGlobalCooldowns && _spellDescriptor.IgnoreGlobalCooldown)
		{
			rowContainerComponent.AddKeyValueRow(Strings.SpellDescription.IgnoreGlobalCooldown, string.Empty);
		}
		if (_spellDescriptor.IgnoreCooldownReduction)
		{
			rowContainerComponent.AddKeyValueRow(Strings.SpellDescription.IgnoreCooldownReduction, string.Empty);
		}
		rowContainerComponent.SizeToChildren();
	}

	protected void SetupDescription()
	{
		if (_spellDescriptor != null)
		{
			AddDivider();
			DescriptionComponent descriptionComponent = AddDescription();
			descriptionComponent.AddText(Strings.ItemDescription.Description.ToString(_spellDescriptor.Description));
		}
	}

	protected void SetupCombatInfo()
	{
		if (_spellDescriptor == null)
		{
			return;
		}
		AddDivider();
		RowContainerComponent rowContainerComponent = AddRowContainer();
		bool flag = false;
		bool flag2 = false;
		for (int i = 0; i < Enum.GetValues<Vital>().Length; i++)
		{
			long num = SpellMastery.VitalDiff(_spellDescriptor, (Vital)i, Rank);
			if (num < 0)
			{
				rowContainerComponent.AddKeyValueRow(Strings.SpellDescription.VitalRecovery[i], Math.Abs(num).ToString());
				flag = true;
			}
			else if (num > 0)
			{
				rowContainerComponent.AddKeyValueRow(Strings.SpellDescription.VitalDamage[i], num.ToString());
				flag2 = true;
			}
		}
		if (flag2)
		{
			Strings.SpellDescription.DamageTypes.TryGetValue(_spellDescriptor.Combat.DamageType, out LocalizedString value);
			rowContainerComponent.AddKeyValueRow(Strings.SpellDescription.DamageType, value);
		}
		if (_spellDescriptor.Combat.Scaling > 0)
		{
			Strings.SpellDescription.Stats.TryGetValue(_spellDescriptor.Combat.ScalingStat, out LocalizedString value2);
			rowContainerComponent.AddKeyValueRow(Strings.SpellDescription.ScalingStat, value2);
			rowContainerComponent.AddKeyValueRow(Strings.SpellDescription.ScalingPercentage, Strings.SpellDescription.Percentage.ToString(_spellDescriptor.Combat.Scaling));
		}
		if (_spellDescriptor.Combat.CritChance > 0)
		{
			rowContainerComponent.AddKeyValueRow(Strings.SpellDescription.CritChance, Strings.SpellDescription.Percentage.ToString(_spellDescriptor.Combat.CritChance));
			rowContainerComponent.AddKeyValueRow(Strings.SpellDescription.CritMultiplier, Strings.SpellDescription.Multiplier.ToString(_spellDescriptor.Combat.CritMultiplier));
		}
		bool flag3 = false;
		bool flag4 = false;
		for (int j = 0; j < Enum.GetValues<Stat>().Length; j++)
		{
			Tuple<string, string> tuple = null;
			if (_spellDescriptor.Combat.StatDiff[j] != 0 && _spellDescriptor.Combat.PercentageStatDiff[j] != 0)
			{
				tuple = new Tuple<string, string>(Strings.SpellDescription.StatCounts[j], Strings.SpellDescription.RegularAndPercentage.ToString(_spellDescriptor.Combat.StatDiff[j], _spellDescriptor.Combat.PercentageStatDiff[j]));
			}
			else if (_spellDescriptor.Combat.StatDiff[j] != 0)
			{
				tuple = new Tuple<string, string>(Strings.SpellDescription.StatCounts[j], _spellDescriptor.Combat.StatDiff[j].ToString());
			}
			else if (_spellDescriptor.Combat.PercentageStatDiff[j] != 0)
			{
				tuple = new Tuple<string, string>(Strings.SpellDescription.StatCounts[j], Strings.ItemDescription.Percentage.ToString(_spellDescriptor.Combat.PercentageStatDiff[j]));
			}
			if (tuple != null)
			{
				if (!flag4)
				{
					rowContainerComponent.AddKeyValueRow(Strings.SpellDescription.StatBuff, string.Empty);
					flag4 = true;
				}
				rowContainerComponent.AddKeyValueRow(tuple.Item1, tuple.Item2);
				flag3 = true;
			}
		}
		if (_spellDescriptor.Combat.HoTDoT)
		{
			flag3 = true;
			if (flag)
			{
				rowContainerComponent.AddKeyValueRow(Strings.SpellDescription.HoT, string.Empty);
			}
			else if (flag2)
			{
				rowContainerComponent.AddKeyValueRow(Strings.SpellDescription.DoT, string.Empty);
			}
			rowContainerComponent.AddKeyValueRow(Strings.SpellDescription.Tick, TimeSpan.FromMilliseconds(_spellDescriptor.Combat.HotDotInterval).WithSuffix());
		}
		if (_spellDescriptor.Combat.Effect != SpellEffect.None)
		{
			flag3 = true;
		}
		if (flag3)
		{
			rowContainerComponent.AddKeyValueRow(Strings.SpellDescription.Duration, TimeSpan.FromMilliseconds(SpellMastery.EffectDuration(_spellDescriptor, Rank)).WithSuffix("0.#"));
		}
		rowContainerComponent.SizeToChildren();
	}

	protected void SetupDashInfo()
	{
		if (_spellDescriptor != null)
		{
			AddDivider();
			RowContainerComponent rowContainerComponent = AddRowContainer();
			rowContainerComponent.AddKeyValueRow(Strings.SpellDescription.Distance, Strings.SpellDescription.Tiles.ToString(_spellDescriptor.Combat.CastRange));
			if (_spellDescriptor.Dash.IgnoreMapBlocks)
			{
				rowContainerComponent.AddKeyValueRow(Strings.SpellDescription.IgnoreMapBlock, string.Empty);
			}
			if (_spellDescriptor.Dash.IgnoreActiveResources)
			{
				rowContainerComponent.AddKeyValueRow(Strings.SpellDescription.IgnoreResourceBlock, string.Empty);
			}
			if (_spellDescriptor.Dash.IgnoreInactiveResources)
			{
				rowContainerComponent.AddKeyValueRow(Strings.SpellDescription.IgnoreConsumedResourceBlock, string.Empty);
			}
			if (Options.Instance.Map.ZDimensionVisible && _spellDescriptor.Dash.IgnoreZDimensionAttributes)
			{
				rowContainerComponent.AddKeyValueRow(Strings.SpellDescription.IgnoreZDimension, string.Empty);
			}
			rowContainerComponent.SizeToChildren();
		}
	}

	protected void SetupExtraInfo()
	{
		if (_spellDescriptor != null && _spellDescriptor.Bound)
		{
			AddDivider();
			RowContainerComponent rowContainerComponent = AddRowContainer();
			rowContainerComponent.AddKeyValueRow(Strings.SpellDescription.Bound, string.Empty);
			rowContainerComponent.SizeToChildren();
		}
	}
}
