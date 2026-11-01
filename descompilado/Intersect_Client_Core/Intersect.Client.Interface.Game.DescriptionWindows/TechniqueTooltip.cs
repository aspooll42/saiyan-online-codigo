using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Client.Spells;
using Intersect.Enums;
using Intersect.GameObjects;
using Intersect.Localization;

namespace Intersect.Client.Interface.Game.DescriptionWindows;

public sealed class TechniqueTooltip : Intersect.Client.Framework.Gwen.Control.Base
{
	private const int CardWidth = 344;

	private const int Pad = 14;

	private readonly SpellDescriptor _spell;

	private readonly int _rank;

	private readonly int _experience;

	private readonly bool _known;

	private readonly Color _color;

	private readonly List<string> _description;

	private readonly List<(string Name, string Value, Color Color)> _rows = new List<(string, string, Color)>();

	private readonly (long Total, long Base, long Power, int Percent, string Stat)? _damage;

	private readonly string _steps;

	private readonly int _descriptionTop;

	private readonly int _rowsTop;

	private readonly int _masteryTop;

	private readonly int _stepsTop;

	public TechniqueTooltip(Intersect.Client.Framework.Gwen.Control.Base parent, SpellDescriptor spell)
		: base(parent, "TechniqueTooltip")
	{
		base.MouseInputEnabled = false;
		_spell = spell;
		_color = TechniqueArt.FamilyColor(SpellMastery.FamilyOf(spell));
		int num = TechniqueProgress.SlotOf(spell.Id);
		_known = num >= 0;
		_rank = TechniqueProgress.RankOf(spell.Id);
		_experience = (_known ? Math.Max(0, Globals.Me.Spells[num].Experience) : 0);
		_description = WrapText(TechniqueArt.Body, 10, string.IsNullOrWhiteSpace(spell.Description) ? string.Empty : spell.Description, 316, 5);
		if (SpellMastery.IsOffensive(spell))
		{
			int[] array = Globals.Me?.Stat;
			int num2 = Math.Clamp(spell.Combat.ScalingStat, 0, Enum.GetValues<Stat>().Length - 1);
			int num3 = ((array != null && num2 < array.Length) ? array[num2] : 0);
			double num4 = (double)Math.Min(num3, 100) + (double)Math.Max(num3 - 100, 0) * 0.5;
			long num5 = SpellMastery.VitalDiff(spell, Vital.Health, _rank);
			long num6 = (long)Math.Round(num4 * (double)spell.Combat.Scaling / 100.0);
			LocalizedString value;
			string item = (Strings.Combat.Stats.TryGetValue((Stat)num2, out value) ? value.ToString() : string.Empty);
			_damage = (num5 + num6, num5, num6, spell.Combat.Scaling, item);
		}
		bool flag = spell.SpellType == SpellType.CombatSpell && spell.Combat.Effect == SpellEffect.Transform;
		if (flag)
		{
			_rows.Add((Strings.Techniques.RowEntry, TechniqueArt.Number(Options.Instance.Transformation.KiCostPercent), TechniqueArt.Text));
			_rows.Add((Strings.Techniques.RowLeak, TechniqueArt.Number((double)Options.Instance.Transformation.KiDrainPercentPerSecond * SpellMastery.LeakFactor(spell, _rank)), TechniqueArt.Text));
		}
		else if (SpellMastery.VitalCost(spell, Vital.Mana, _rank) > 0)
		{
			_rows.Add((Strings.Techniques.RowKi, SpellMastery.VitalCost(spell, Vital.Mana, _rank).ToString(), TechniqueArt.Cyan));
		}
		int num7 = SpellMastery.CastDuration(spell, _rank);
		if (num7 > 0)
		{
			_rows.Add((Strings.Techniques.RowCast, TechniqueArt.Number((double)num7 / 1000.0), TechniqueArt.Text));
		}
		int num8 = SpellMastery.Cooldown(spell, _rank);
		if (num8 > 0)
		{
			_rows.Add((Strings.Techniques.RowCooldown, TechniqueArt.Number((double)num8 / 1000.0), TechniqueArt.Text));
		}
		if (!flag && spell.Combat.Duration > 0)
		{
			_rows.Add((Strings.Techniques.RowDuration, TechniqueArt.Number((double)SpellMastery.EffectDuration(spell, _rank) / 1000.0), TechniqueArt.Text));
		}
		List<string> list = new List<string>();
		SpellMasteryDescriptor mastery = spell.Mastery;
		if (SpellMastery.HasMastery(spell))
		{
			if (mastery.DamagePerRank != 0 && _damage.HasValue)
			{
				list.Add(Strings.Techniques.StepDamage.ToString(TechniqueArt.Signed(mastery.DamagePerRank)));
			}
			if (mastery.KiCostPerRank != 0 && spell.VitalCost[1] > 0)
			{
				list.Add(Strings.Techniques.StepKi.ToString(TechniqueArt.Signed(mastery.KiCostPerRank)));
			}
			if (mastery.CastTimePerRankMs != 0)
			{
				list.Add(Strings.Techniques.StepCast.ToString(TechniqueArt.Signed((double)mastery.CastTimePerRankMs / 1000.0)));
			}
			if (mastery.CooldownPerRankMs != 0)
			{
				list.Add(Strings.Techniques.StepCooldown.ToString(TechniqueArt.Signed((double)mastery.CooldownPerRankMs / 1000.0)));
			}
			if (mastery.DurationPerRankMs != 0 && spell.Combat.Duration > 0)
			{
				list.Add(Strings.Techniques.StepDuration.ToString(TechniqueArt.Signed((double)mastery.DurationPerRankMs / 1000.0)));
			}
			if (mastery.LeakPercentPerRank != 0 && flag)
			{
				list.Add(Strings.Techniques.StepLeak.ToString(TechniqueArt.Signed(mastery.LeakPercentPerRank)));
			}
		}
		_steps = ((list.Count == 0) ? string.Empty : $"{Strings.Techniques.EachRank} {string.Join("  ·  ", list)}");
		int num9 = (_masteryTop = (_rowsTop = (_descriptionTop = 76) + (_description.Count * 17 + ((_description.Count > 0) ? 8 : 0))) + (7 + (_damage.HasValue ? 50 : 0) + _rows.Count * 22));
		if (_known && SpellMastery.HasMastery(spell))
		{
			num9 += 48;
		}
		_stepsTop = num9;
		if (_steps.Length > 0)
		{
			num9 += 26;
		}
		SetSize(344, num9 + 14);
	}

	private static List<string> WrapText(IFont? font, int size, string text, int width, int maxLines)
	{
		List<string> list = new List<string>();
		GameRenderer renderer;
		if (font != null && !string.IsNullOrWhiteSpace(text))
		{
			renderer = Graphics.Renderer;
			if (renderer != null)
			{
				string text2 = string.Empty;
				string[] array = text.Replace("\r", string.Empty).Replace('\n', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
				foreach (string text3 in array)
				{
					string text4 = ((text2.Length == 0) ? text3 : (text2 + " " + text3));
					if (text2.Length > 0 && Measure(text4) > width)
					{
						list.Add(text2);
						text2 = text3;
					}
					else
					{
						text2 = text4;
					}
				}
				if (text2.Length > 0)
				{
					list.Add(text2);
				}
				if (list.Count > maxLines)
				{
					list = list.Take(maxLines).ToList();
					List<string> list2 = list;
					int index = list2.Count - 1;
					List<string> list3 = list;
					list2[index] = list3[list3.Count - 1].TrimEnd('.', ',') + "…";
				}
				return list;
			}
		}
		return list;
		int Measure(string value)
		{
			return (int)renderer.MeasureText(value, font, size, 1f).X;
		}
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		int width = base.Width;
		TechniqueArt.Patch(renderer, "tech_panneau.png", new Rectangle(0, 0, width, base.Height), TechniqueArt.White, 12);
		TechniqueArt.Fill(renderer, new Rectangle(12, 0, width - 24, 2), _color);
		TechniqueArt.GlowRect(renderer, new Rectangle(-40, -40, 180, 150), TechniqueArt.Alpha(_color, 0.16f), additive: false);
		Rectangle rect = new Rectangle(14, 14, 52, 52);
		TechniqueArt.Image(renderer, "tech_hexa_bord.png", rect, TechniqueArt.Mix(_color, TechniqueArt.White, 0.95f));
		Rectangle rect2 = new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4);
		TechniqueArt.Image(renderer, "tech_hexa.png", rect2, new Color(255, 8, 11, 19));
		TechniqueArt.Image(renderer, "tech_hexa_radial.png", rect2, TechniqueArt.Mix(_color, new Color(255, 22, 28, 44), 0.3f));
		IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Spell, _spell.Icon);
		if (texture != null)
		{
			TechniqueArt.Glow(renderer, (float)rect2.X + (float)rect2.Width / 2f, (float)rect2.Y + (float)rect2.Height / 2f, 22f, TechniqueArt.Alpha(_color, 0.4f));
			SaiyanHud.Draw(renderer, texture, rect2.X + (rect2.Width - 32) / 2, rect2.Y + (rect2.Height - 32) / 2, 32, 32, TechniqueArt.White);
		}
		int num = rect.Right + 12;
		List<SpellDescriptor> list = SpellMastery.Lineage(_spell);
		int stage = list.IndexOf(_spell) + 1;
		string text = (TechniqueArt.FamilyName(SpellMastery.FamilyOf(_spell)) + " · " + Strings.Techniques.Stage.ToString(TechniqueArt.Roman(stage))).ToUpperInvariant();
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, 8, text, num, 21, 1.5f, TechniqueArt.Mix(_color, TechniqueArt.White, 0.85f));
		string text2 = _spell.Name.ToUpperInvariant();
		int num2 = 20;
		while (num2 > 13 && TechniqueArt.Measure(renderer, TechniqueArt.Display, num2, text2).X > width - num - 14)
		{
			num2 -= 2;
		}
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, num2, text2);
		TechniqueArt.Styled(renderer, TechniqueArt.Display, num2, text2, num, 44 - point.Y / 2, new TechniqueArt.Stops(TechniqueArt.White, 0.35f, TechniqueArt.Mix(_color, TechniqueArt.White, 0.6f), 0.75f, _color));
		int num3 = 65;
		if (SpellMastery.HasMastery(_spell))
		{
			for (int i = 1; i <= 5; i++)
			{
				Rectangle rect3 = new Rectangle(num + (i - 1) * 14, num3 - 2, 11, 5);
				TechniqueArt.Image(renderer, "tech_pip_mini.png", rect3, (i <= _rank) ? _color : new Color(255, 18, 24, 38));
			}
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, 9, Strings.Techniques.Rank.ToString(_rank), num + 70 + 4, num3, TechniqueArt.Soft);
		}
		else
		{
			string text3 = (_spell.Combat.Friendly ? Strings.SpellDescription.Friendly.ToString() : Strings.SpellDescription.Unfriendly.ToString());
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, 9, text3, num, num3, TechniqueArt.Dim);
		}
		int num4 = _descriptionTop;
		foreach (string item in _description)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, 10, item, 14, num4 + 8, new Color(255, 195, 203, 220));
			num4 += 17;
		}
		num4 = _rowsTop;
		TechniqueArt.Fill(renderer, new Rectangle(14, num4, width - 28, 1), TechniqueArt.Alpha(TechniqueArt.White, 0.06f));
		num4 += 7;
		(long, long, long, int, string)? damage = _damage;
		if (damage.HasValue)
		{
			(long, long, long, int, string) valueOrDefault = damage.GetValueOrDefault();
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, 10, Strings.Techniques.RowDamage, 14, num4 + 12, TechniqueArt.Text);
			string text4 = valueOrDefault.Item1.ToString();
			Point point2 = TechniqueArt.Measure(renderer, TechniqueArt.Display, 20, text4);
			TechniqueArt.Italic(renderer, TechniqueArt.Display, 20, text4, width - 14 - point2.X, num4 + 12 - point2.Y / 2, TechniqueArt.White);
			Rectangle rect4 = new Rectangle(14, num4 + 28, width - 28, 6);
			TechniqueArt.Fill(renderer, rect4, new Color(255, 7, 10, 18));
			int num5 = ((valueOrDefault.Item1 > 0) ? ((int)((double)((rect4.Width - 2) * valueOrDefault.Item2) / (double)valueOrDefault.Item1)) : 0);
			TechniqueArt.FillVertical(renderer, new Rectangle(rect4.X + 1, rect4.Y + 1, num5, rect4.Height - 2), new Color(255, 255, 231, 168), new Color(255, 217, 160, 43));
			TechniqueArt.FillVertical(renderer, new Rectangle(rect4.X + 1 + num5, rect4.Y + 1, rect4.Width - 2 - num5, rect4.Height - 2), new Color(255, 159, 230, 255), new Color(255, 47, 143, 214));
			TechniqueArt.Outline(renderer, rect4, TechniqueArt.Line2);
			int centerY = num4 + 43;
			int x = 14 + TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, 9, string.Concat(Strings.Techniques.TechniqueBase, " "), 14, centerY, TechniqueArt.Soft);
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, 9, TechniqueArt.Number(valueOrDefault.Item2), x, centerY, TechniqueArt.Gold);
			int right = width - 14 - TechniqueArt.TextRight(renderer, TechniqueArt.Label, 9, $"+{valueOrDefault.Item3}", width - 14, centerY, TechniqueArt.Cyan);
			TechniqueArt.TextRight(renderer, TechniqueArt.Body, 9, Strings.Techniques.StatShare.ToString(valueOrDefault.Item4, valueOrDefault.Item5) + " ", right, centerY, TechniqueArt.Soft);
			num4 += 50;
		}
		foreach (var (text5, text6, color) in _rows)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, 10, text5, 14, num4 + 11, TechniqueArt.Soft);
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, 13, text6, width - 14, num4 + 11, color);
			num4 += 22;
		}
		if (_known && SpellMastery.HasMastery(_spell))
		{
			num4 = _masteryTop + 8;
			int num6 = SpellMastery.ExperienceToNext(_spell, _rank);
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, 8, Strings.Techniques.Mastery.ToString().ToUpperInvariant(), 14, num4 + 6, 1.5f, TechniqueArt.Soft);
			string text7 = ((_rank >= 5) ? Strings.Techniques.MaxRank.ToString() : $"{_experience} / {num6}");
			TechniqueArt.TextRight(renderer, TechniqueArt.Label, 9, text7, width - 14, num4 + 6, (_rank >= 5) ? TechniqueArt.Gold : TechniqueArt.Dim);
			Rectangle rect5 = new Rectangle(14, num4 + 16, width - 28, 6);
			TechniqueArt.Fill(renderer, rect5, new Color(255, 7, 10, 18));
			float num7 = ((_rank >= 5) ? 1f : Math.Clamp((float)_experience / (float)Math.Max(1, num6), 0f, 1f));
			Rectangle rect6 = new Rectangle(rect5.X + 1, rect5.Y + 1, (int)((float)(rect5.Width - 2) * num7), rect5.Height - 2);
			TechniqueArt.FillHorizontal(renderer, rect6, TechniqueArt.Mix(_color, TechniqueArt.Black, 0.55f), _color, TechniqueArt.Mix(_color, TechniqueArt.White, 0.6f));
			TechniqueArt.Outline(renderer, rect5, TechniqueArt.Line2);
			LocalizedString localizedString = (SpellMastery.IsOffensive(_spell) ? Strings.Techniques.MasteryHintHit : Strings.Techniques.MasteryHintCast);
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, 9, localizedString, 14, num4 + 32, TechniqueArt.Dim);
		}
		if (_steps.Length > 0)
		{
			TechniqueArt.Fill(renderer, new Rectangle(14, _stepsTop + 2, width - 28, 1), TechniqueArt.Alpha(TechniqueArt.White, 0.06f));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, 9, _steps, 14, _stepsTop + 16, TechniqueArt.Green);
		}
	}
}
