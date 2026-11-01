using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Core.Controls;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Client.Networking;
using Intersect.Client.Spells;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.PlayerClass;
using Intersect.Framework.Core.Sprites;
using Intersect.GameObjects;
using Intersect.Localization;
using Intersect.Models;

namespace Intersect.Client.Interface.Game.Techniques;

public sealed class TechniquesWindow : CharteScreen
{
	internal readonly record struct Known(int Slot, SpellDescriptor Spell, int Rank, int Experience, TechniqueFamily Family);

	internal readonly record struct DamageSplit(long Total, long Base, long Power, int Percent, string StatName, int Hits);

	private enum Row
	{
		Damage,
		Base,
		Hits,
		Ki,
		Entry,
		Cast,
		Cooldown,
		Duration,
		Leak
	}

	private sealed class Cinematic
	{
		public required int Slot { get; init; }

		public required SpellDescriptor From { get; init; }

		public required SpellDescriptor To { get; init; }

		public required TechniqueFamily Family { get; init; }

		public required long RequestedAt { get; init; }

		public long ConfirmedAt { get; set; }

		public required List<(Row Row, double Before, double After)> Gains { get; init; }

		public required (float Angle, float Speed, float Size)[] Sparks { get; init; }
	}

	private readonly MasteryToast _toast;

	private TechniqueFamily _family = TechniqueFamily.KiBlast;

	private int _slot = -1;

	private int _viewRank;

	private int _listScroll;

	private long _previewStart;

	private Rectangle _listArea;

	private bool _toutVoir;

	private bool _sequenceSeule;

	private const long EvolutionTimeoutMs = 3000L;

	private Cinematic? _cinematic;

	private static readonly Color HexCore = new Color(255, 8, 11, 19);

	private static readonly Color HexTint = new Color(255, 22, 28, 44);

	private static readonly Color CardLeft = new Color(235, 20, 27, 42);

	private static readonly Color CardRight = new Color(217, 12, 17, 28);

	private static readonly Color PipBack = new Color(255, 8, 12, 21);

	private static readonly Color BarBack = new Color(255, 7, 10, 18);

	private static readonly Color TableText = new Color(255, 201, 209, 226);

	private static readonly Color RuleText = new Color(255, 216, 200, 154);

	private static readonly Color Strength = new Color(255, 255, 154, 122);

	private static readonly Color KeyBack = new Color(255, 17, 24, 39);

	private static readonly Color RoleText = new Color(255, 195, 203, 220);

	private static readonly Color LineageOff = new Color(255, 29, 38, 64);

	private static readonly Color NodeOff = new Color(255, 17, 24, 39);

	private static readonly Color NodeEdge = new Color(255, 43, 53, 80);

	private static readonly Color NodeRoman = new Color(255, 74, 85, 112);

	private const float ColumnX = 1150f;

	private const float ColumnWidth = 420f;

	private const float Inner = 1166f;

	private new const float InnerWidth = 388f;

	private const long PreviewCycleMs = 3200L;

	private const int FacingDown = 0;

	private const int FacingRight = 2;

	private int _sceneX;

	private int _sceneY;

	private int _zoom = 2;

	private float _shake;

	private static readonly Dictionary<string, string> EffetsParNom = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["Kiai"] = "kiai",
		["Onde de choc"] = "onde",
		["Riposte"] = "riposte",
		["Explosion de ki"] = "explosion"
	};

	private const int SeuilDuTransport = 6;

	private const long RetourMs = 200L;

	private const long PoseeMs = 520L;

	private const long ApparitionMs = 120L;

	private int _appuiSlot = -1;

	private Point _appuiA;

	private int _porteSlot = -1;

	private long _porteDepuis;

	private Point _porteSouris;

	private int _retourSlot = -1;

	private Point _retourDe;

	private Point _retourVers;

	private long _retourDepuis;

	private int _poseeCase = -1;

	private long _poseeDepuis;

	private long _depotA;

	private readonly Dictionary<int, Rectangle> _cartesDessinees = new Dictionary<int, Rectangle>();

	private readonly Rectangle[] _casesDessinees = new Rectangle[10];

	private int _casesVisibles;

	protected override string? DetailsKey => "TechniquesDetails";

	protected override bool BlocksHover => _cinematic != null;

	private bool EnTransport => _porteSlot >= 0;

	private static long Maintenant => Timing.Global.MillisecondsUtc;

	public TechniquesWindow(Canvas gameCanvas)
		: base(gameCanvas, "TechniquesWindow")
	{
		_toast = new MasteryToast(gameCanvas);
	}

	internal bool JouerEvolution(int slot)
	{
		Known entry = KnownTechniques().FirstOrDefault((Known known) => known.Slot == slot);
		if (entry.Spell == null || !CanEvolve(entry, out SpellDescriptor next) || next == null)
		{
			return false;
		}
		Show();
		_slot = slot;
		_family = entry.Family;
		_sequenceSeule = true;
		StartEvolution(entry, next);
		return true;
	}

	public override void Show()
	{
		_sequenceSeule = false;
		FitCanvas();
		PickDefault();
		_previewStart = Timing.Global.MillisecondsUtc;
		_viewRank = 0;
		base.Show();
	}

	public override void Hide()
	{
		_sequenceSeule = false;
		_cinematic = null;
		OublierLeTransport();
		base.Hide();
	}

	public void Update()
	{
		_toast.Update(base.IsHidden);
		if (!base.IsHidden)
		{
			FitCanvas();
			UpdateCinematic();
			if (_sequenceSeule && _cinematic == null)
			{
				Hide();
			}
			else
			{
				MajDuTransport();
			}
		}
	}

	internal static List<Known> KnownTechniques()
	{
		List<Known> list = new List<Known>();
		Spell[] array = Globals.Me?.Spells;
		if (array == null)
		{
			return list;
		}
		for (int i = 0; i < array.Length; i++)
		{
			Spell spell = array[i];
			if (spell != null && spell.Id != Guid.Empty && DatabaseObject<SpellDescriptor>.TryGet(spell.Id, out var databaseObject))
			{
				list.Add(new Known(i, databaseObject, SpellMastery.Clamp(spell.Rank), Math.Max(0, spell.Experience), SpellMastery.FamilyOf(databaseObject)));
			}
		}
		return (from entry in list
			orderby LearnLevel(entry.Spell.Id), entry.Slot
			select entry).ToList();
	}

	private static int LearnLevel(Guid spellId)
	{
		Player me = Globals.Me;
		if (me != null && DatabaseObject<ClassDescriptor>.TryGet(me.Class, out var databaseObject))
		{
			ClassSpell classSpell = databaseObject.Spells.FirstOrDefault((ClassSpell classSpell2) => classSpell2.Id == spellId);
			if (classSpell != null)
			{
				return classSpell.Level;
			}
		}
		List<int> list = (from classSpell2 in DatabaseObject<ClassDescriptor>.Lookup.Values.OfType<ClassDescriptor>().SelectMany((ClassDescriptor descriptor) => descriptor.Spells)
			where classSpell2.Id == spellId
			select classSpell2.Level).ToList();
		if (list.Count <= 0)
		{
			return int.MaxValue;
		}
		return list.Min();
	}

	private void PickDefault()
	{
		List<Known> known = KnownTechniques();
		if (known.Count == 0)
		{
			_slot = -1;
			return;
		}
		Known known2 = known.FirstOrDefault((Known entry) => entry.Slot == _slot);
		if (known2.Spell != null)
		{
			_family = known2.Family;
			return;
		}
		Known known3 = TechniqueArt.Families.SelectMany((TechniqueFamily family) => known.Where((Known entry) => entry.Family == family)).First();
		_slot = known3.Slot;
		_family = known3.Family;
		_listScroll = 0;
	}

	private Known? Selected(List<Known> known)
	{
		foreach (Known item in known)
		{
			if (item.Slot == _slot)
			{
				return item;
			}
		}
		return null;
	}

	private void SelectFamily(TechniqueFamily family, List<Known> known)
	{
		_family = family;
		_listScroll = 0;
		_viewRank = 0;
		_previewStart = Timing.Global.MillisecondsUtc;
		Known known2 = known.FirstOrDefault((Known entry) => entry.Family == family);
		if (known2.Spell != null)
		{
			_slot = known2.Slot;
		}
	}

	private void SelectSlot(int slot)
	{
		if (slot != _slot)
		{
			_slot = slot;
			_viewRank = 0;
			_previewStart = Timing.Global.MillisecondsUtc;
		}
	}

	private static double Effective(int stat)
	{
		return (double)Math.Min(stat, 100) + (double)Math.Max(stat - 100, 0) * 0.5;
	}

	internal static int HitsOf(SpellDescriptor spell)
	{
		if (spell.Combat.TargetType != SpellTargetType.Projectile || !DatabaseObject<ProjectileDescriptor>.TryGet(spell.Combat.ProjectileId, out var databaseObject))
		{
			return 1;
		}
		return Math.Clamp(databaseObject.Quantity, 1, 5);
	}

	internal static DamageSplit? DamageOf(SpellDescriptor spell, int rank)
	{
		if (!SpellMastery.IsOffensive(spell))
		{
			return null;
		}
		int[] array = Globals.Me?.Stat;
		int num = Math.Clamp(spell.Combat.ScalingStat, 0, Enum.GetValues<Stat>().Length - 1);
		int stat = ((array != null && num < array.Length) ? array[num] : 0);
		long num2 = SpellMastery.VitalDiff(spell, Vital.Health, rank);
		long num3 = (long)Math.Round(Effective(stat) * (double)spell.Combat.Scaling / 100.0);
		LocalizedString value;
		string statName = (Strings.Combat.Stats.TryGetValue((Stat)num, out value) ? value.ToString() : string.Empty);
		return new DamageSplit(num2 + num3, num2, num3, spell.Combat.Scaling, statName, HitsOf(spell));
	}

	internal static bool IsTransformation(SpellDescriptor spell)
	{
		if (spell.SpellType == SpellType.CombatSpell && spell.Combat.Effect == SpellEffect.Transform)
		{
			return spell.Combat.TargetType == SpellTargetType.Self;
		}
		return false;
	}

	private static List<Row> RowsFor(SpellDescriptor spell, SpellDescriptor? next)
	{
		List<Row> list = new List<Row>();
		if (SpellMastery.IsOffensive(spell))
		{
			list.Add(Row.Damage);
			list.Add(Row.Base);
		}
		if (Either((SpellDescriptor technique) => HitsOf(technique) > 1))
		{
			list.Add(Row.Hits);
		}
		if (IsTransformation(spell))
		{
			list.Add(Row.Entry);
			list.Add(Row.Leak);
		}
		else if (Either((SpellDescriptor technique) => technique.VitalCost[1] > 0))
		{
			list.Add(Row.Ki);
		}
		if (Either((SpellDescriptor technique) => technique.CastDuration > 0 || technique.Mastery.CastTimePerRankMs != 0))
		{
			list.Add(Row.Cast);
		}
		if (Either((SpellDescriptor technique) => technique.CooldownDuration > 0))
		{
			list.Add(Row.Cooldown);
		}
		if (!IsTransformation(spell) && Either((SpellDescriptor technique) => technique.Combat.Duration > 0))
		{
			list.Add(Row.Duration);
		}
		return list;
		bool Either(Func<SpellDescriptor, bool> test)
		{
			if (!test(spell))
			{
				if (next != null)
				{
					return test(next);
				}
				return false;
			}
			return true;
		}
	}

	private static double ValueOf(Row row, SpellDescriptor spell, int rank)
	{
		return row switch
		{
			Row.Damage => DamageOf(spell, rank)?.Total ?? 0, 
			Row.Base => SpellMastery.VitalDiff(spell, Vital.Health, rank), 
			Row.Hits => HitsOf(spell), 
			Row.Ki => SpellMastery.VitalCost(spell, Vital.Mana, rank), 
			Row.Entry => Options.Instance.Transformation.KiCostPercent, 
			Row.Cast => (double)SpellMastery.CastDuration(spell, rank) / 1000.0, 
			Row.Cooldown => (double)SpellMastery.Cooldown(spell, rank) / 1000.0, 
			Row.Duration => (double)SpellMastery.EffectDuration(spell, rank) / 1000.0, 
			Row.Leak => (double)Options.Instance.Transformation.KiDrainPercentPerSecond * SpellMastery.LeakFactor(spell, rank), 
			_ => 0.0, 
		};
	}

	private static int Sense(Row row)
	{
		if (((uint)(row - 3) > 3u && row != Row.Leak) || 1 == 0)
		{
			return 1;
		}
		return -1;
	}

	private static string RowName(Row row)
	{
		return row switch
		{
			Row.Damage => Strings.Techniques.RowDamage, 
			Row.Base => Strings.Techniques.RowBase, 
			Row.Hits => Strings.Techniques.RowHits, 
			Row.Ki => Strings.Techniques.RowKi, 
			Row.Entry => Strings.Techniques.RowEntry, 
			Row.Cast => Strings.Techniques.RowCast, 
			Row.Cooldown => Strings.Techniques.RowCooldown, 
			Row.Duration => Strings.Techniques.RowDuration, 
			_ => Strings.Techniques.RowLeak, 
		};
	}

	internal static List<string> StepsOf(SpellDescriptor spell)
	{
		SpellMasteryDescriptor mastery = spell.Mastery;
		List<string> list = new List<string>();
		if (mastery.DamagePerRank != 0 && SpellMastery.IsOffensive(spell))
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
		if (mastery.LeakPercentPerRank != 0 && IsTransformation(spell))
		{
			list.Add(Strings.Techniques.StepLeak.ToString(TechniqueArt.Signed(mastery.LeakPercentPerRank)));
		}
		return list;
	}

	internal static bool CanEvolve(Known entry, out SpellDescriptor? next)
	{
		next = SpellMastery.EvolvesInto(entry.Spell);
		if (next != null && SpellMastery.HasMastery(entry.Spell) && entry.Rank >= 5)
		{
			return (Globals.Me?.Level ?? 0) >= entry.Spell.Mastery.EvolutionLevel;
		}
		return false;
	}

	protected override void ToggleDetails()
	{
		base.ToggleDetails();
		_toutVoir = false;
	}

	internal override bool HandleKey(Keys modifier, Keys key)
	{
		if (base.IsHidden)
		{
			return false;
		}
		if (_cinematic != null)
		{
			if ((key == Keys.Return || key == Keys.Space) ? true : false)
			{
				ClickCinematic();
				return true;
			}
			return false;
		}
		List<Known> known = KnownTechniques();
		Known? known2 = Selected(known);
		foreach (Control item in Controls.GetControlsFor(modifier, key))
		{
			int num = (int)(item - 4096 - 1);
			if (num >= 0 && num < 10 && known2.HasValue)
			{
				Known valueOrDefault = known2.GetValueOrDefault();
				Globals.Me?.AddToHotbar(num, 1, valueOrDefault.Slot);
				Audio.AddGameSound("menu_click.wav", loop: false);
				return true;
			}
		}
		if (modifier != Keys.None)
		{
			return false;
		}
		switch (key)
		{
		case Keys.Left:
			SelectNeighbourFamily(-1, known);
			return true;
		case Keys.Right:
			SelectNeighbourFamily(1, known);
			return true;
		case Keys.Up:
			SelectNeighbourSlot(-1, known);
			return true;
		case Keys.Down:
			SelectNeighbourSlot(1, known);
			return true;
		case Keys.D:
			ToggleDetails();
			return true;
		case Keys.G:
			if (base.Details)
			{
				_toutVoir = !_toutVoir;
				Audio.AddGameSound("menu_click.wav", loop: false);
			}
			return true;
		case Keys.Return:
			if (known2.HasValue)
			{
				Known valueOrDefault2 = known2.GetValueOrDefault();
				if (CanEvolve(valueOrDefault2, out SpellDescriptor next) && next != null)
				{
					StartEvolution(valueOrDefault2, next);
				}
			}
			return true;
		default:
			return false;
		}
	}

	private void SelectNeighbourFamily(int step, List<Known> known)
	{
		List<TechniqueFamily> list = TechniqueArt.Families.Where((TechniqueFamily family) => known.Any((Known entry) => entry.Family == family)).ToList();
		if (list.Count != 0)
		{
			int num = Math.Max(0, list.IndexOf(_family));
			TechniqueFamily techniqueFamily = list[(num + step + list.Count) % list.Count];
			if (techniqueFamily != _family)
			{
				SelectFamily(techniqueFamily, known);
				Audio.AddGameSound("menu_hover.wav", loop: false);
			}
		}
	}

	private void SelectNeighbourSlot(int step, List<Known> known)
	{
		List<Known> list = known.Where((Known entry) => entry.Family == _family).ToList();
		if (list.Count == 0)
		{
			return;
		}
		int num = Math.Max(0, list.FindIndex((Known entry) => entry.Slot == _slot));
		int num2 = Math.Clamp(num + step, 0, list.Count - 1);
		if (list[num2].Slot != _slot)
		{
			SelectSlot(list[num2].Slot);
			Audio.AddGameSound("menu_hover.wav", loop: false);
			int num3 = num2 * _d.L(92f);
			int num4 = num3 + _d.L(84f);
			if (_listArea.Height > _d.L(84f))
			{
				_listScroll = Math.Clamp(_listScroll, num4 - _listArea.Height, num3);
			}
		}
	}

	protected override bool InterceptClick()
	{
		if (EnTransport)
		{
			Deposer(LocalMouse());
			return true;
		}
		if (Maintenant - _depotA < 250)
		{
			return true;
		}
		if (_cinematic == null)
		{
			return false;
		}
		ClickCinematic();
		return true;
	}

	protected override bool OnMouseWheeled(int delta)
	{
		Point point = LocalMouse();
		if (_listArea.Contains(point.X, point.Y))
		{
			_listScroll = Math.Max(0, _listScroll - Math.Sign(delta) * _d.L(46f));
			return true;
		}
		return base.OnMouseWheeled(delta);
	}

	protected override void DrawScreen(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		if (_sequenceSeule)
		{
			DrawCinematic(renderer, now);
			return;
		}
		List<Known> list = KnownTechniques();
		if (list.All((Known entry) => entry.Slot != _slot))
		{
			PickDefault();
		}
		Known? known = Selected(list);
		Color accent = ((!known.HasValue) ? TechniqueArt.FamilyColor(_family) : TechniqueArt.FamilyColor(known.GetValueOrDefault().Family));
		_casesVisibles = 0;
		DrawBackdrop(renderer, accent);
		DrawTechniquesTopBar(renderer);
		DrawRail(renderer, list);
		DrawList(renderer, list);
		if (known.HasValue)
		{
			Known valueOrDefault = known.GetValueOrDefault();
			DrawPreview(renderer, valueOrDefault, now);
			DrawIdentity(renderer, valueOrDefault, now);
			DrawLineage(renderer, valueOrDefault, now);
			DrawRightColumn(renderer, valueOrDefault, now);
		}
		else
		{
			DrawEmptyCenter(renderer);
		}
		DrawTransport(renderer, now);
		if (_cinematic != null)
		{
			DrawCinematic(renderer, now);
		}
	}

	private void StartEvolution(Known entry, SpellDescriptor next)
	{
		List<(Row, double, double)> gains = (from row in RowsFor(entry.Spell, next).Where(delegate(Row row)
			{
				bool flag = ((row == Row.Hits || row == Row.Entry) ? true : false);
				return !flag;
			})
			select (Row: row, Before: ValueOf(row, entry.Spell, 5), After: ValueOf(row, next, 1)) into gain
			where Math.Abs(gain.After - gain.Before) > 0.0001
			select gain).Take(3).ToList();
		Random random = new Random(entry.Spell.Id.GetHashCode());
		(float, float, float)[] array = new(float, float, float)[160];
		for (int num = 0; num < array.Length; num++)
		{
			array[num] = ((float)(random.NextDouble() * Math.PI * 2.0), 200f + (float)random.NextDouble() * 700f, 1f + (float)random.NextDouble() * 3f);
		}
		_cinematic = new Cinematic
		{
			Slot = entry.Slot,
			From = entry.Spell,
			To = next,
			Family = entry.Family,
			RequestedAt = Timing.Global.MillisecondsUtc,
			Gains = gains,
			Sparks = array
		};
		PacketSender.SendEvolveSpell(entry.Slot);
		Audio.AddGameSound("Aura_intro.wav", loop: false);
	}

	private void UpdateCinematic()
	{
		Cinematic cinematic = _cinematic;
		if (cinematic == null || cinematic.ConfirmedAt > 0)
		{
			return;
		}
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		TechniqueProgress.Evolution? lastEvolution = TechniqueProgress.LastEvolution;
		if (lastEvolution.HasValue)
		{
			TechniqueProgress.Evolution valueOrDefault = lastEvolution.GetValueOrDefault();
			if (valueOrDefault.At >= cinematic.RequestedAt && valueOrDefault.ToId == cinematic.To.Id)
			{
				cinematic.ConfirmedAt = millisecondsUtc;
				_slot = valueOrDefault.Slot;
				_family = SpellMastery.FamilyOf(cinematic.To);
				_viewRank = 0;
				Audio.AddGameSound("ki_fatal.wav", loop: false);
				return;
			}
		}
		if (millisecondsUtc - cinematic.RequestedAt > 3000)
		{
			_cinematic = null;
		}
	}

	private void ClickCinematic()
	{
		Cinematic cinematic = _cinematic;
		if (cinematic != null && cinematic.ConfirmedAt > 0 && Timing.Global.MillisecondsUtc - cinematic.ConfirmedAt > 700)
		{
			_cinematic = null;
			_previewStart = Timing.Global.MillisecondsUtc;
		}
	}

	private static float Clamp01(float value)
	{
		return Math.Clamp(value, 0f, 1f);
	}

	private void DrawCinematic(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		Cinematic cinematic = _cinematic;
		if (cinematic == null)
		{
			return;
		}
		bool flag = cinematic.ConfirmedAt > 0;
		long num = (flag ? (900 + (now - cinematic.ConfirmedAt)) : ((now - cinematic.RequestedAt) % 900));
		int centerX = _d.X(800f);
		int centerY = _d.Y(450f);
		Rectangle rect = new Rectangle(0, 0, base.Width, base.Height);
		TechniqueArt.Image(renderer, "tech_voile_cine.png", rect, TechniqueArt.White);
		if (!flag || num < 900)
		{
			float num2 = (float)num / 900f;
			for (int i = 0; i < 3; i++)
			{
				int num3 = _d.L(40f + 300f * num2 * (1f - (float)i * 0.2f));
				int num4 = Math.Max(1, _d.L(6 - i * 2));
				TechniqueArt.Image(renderer, (num4 >= 5) ? "tech_anneau6.png" : ((num4 >= 3) ? "tech_anneau4.png" : "tech_anneau2.png"), new Rectangle(centerX - num3, centerY - num3, num3 * 2, num3 * 2), TechniqueArt.Alpha(new Color(255, 255, 190, 90), 0.5f * (1f - num2)));
			}
			int num5 = _d.L(260f * num2 + 40f);
			TechniqueArt.GlowRect(renderer, new Rectangle(centerX - num5, centerY - num5, num5 * 2, num5 * 2), TechniqueArt.Alpha(new Color(255, 255, 160, 40), 0.9f * (1f - num2)), additive: true, "tech_lueur_couleur.png");
			TechniqueArt.GlowRect(renderer, new Rectangle(centerX - num5, centerY - num5, num5 * 2, num5 * 2), TechniqueArt.Alpha(TechniqueArt.White, 0.9f * (1f - num2)), additive: true, "tech_lueur_coeur.png");
		}
		if (num >= 900 && num < 1150)
		{
			TechniqueArt.Fill(renderer, rect, TechniqueArt.Alpha(new Color(255, 255, 246, 214), 0.9f * (1f - (float)(num - 900) / 250f)));
		}
		if (num > 900)
		{
			float seconds = (float)(num - 900) / 1000f;
			float alpha = Math.Max(0f, 1f - seconds / 1.8f);
			if (alpha > 0f)
			{
				TechniqueArt.Additive(renderer, delegate
				{
					(float, float, float)[] sparks = cinematic.Sparks;
					for (int j = 0; j < sparks.Length; j++)
					{
						(float, float, float) tuple2 = sparks[j];
						float item4 = tuple2.Item1;
						float item5 = tuple2.Item2;
						float item6 = tuple2.Item3;
						int num21 = _d.L(item5 * (1f - MathF.Exp(-2.2f * seconds)));
						int num22 = Math.Max(2, _d.L(item6 * 2f));
						TechniqueArt.Fill(renderer, new Rectangle(centerX + (int)(MathF.Cos(item4) * (float)num21), centerY + (int)(MathF.Sin(item4) * (float)num21 * 0.6f), num22, num22), TechniqueArt.Alpha(new Color(255, 255, (int)(190f + item6 * 15f), 90), alpha));
					}
				});
			}
		}
		float alpha2 = Clamp01((float)num / 300f);
		float num6 = ((num < 700) ? Clamp01((float)num / 300f) : Clamp01(1f - (float)(num - 700) / 200f));
		float num7 = (flag ? Clamp01((float)(num - 900) / 350f) : 0f);
		float num8 = (flag ? Clamp01((float)(num - 1500) / 400f) : 0f);
		float num9 = (flag ? Clamp01((float)(num - 2300) / 400f) : 0f);
		float num10 = 205f + ((cinematic.Gains.Count > 0) ? 83f : 0f) + 45f;
		float num11 = 450f - num10 / 2f;
		TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(18f), Strings.Techniques.Evolution.ToString().ToUpperInvariant(), centerX, _d.Y(num11 + 13f), _d.Em(0.8f, 18f), TechniqueArt.Alpha(TechniqueArt.Gold, alpha2));
		num11 += 26f;
		if (num6 > 0f)
		{
			float num12 = ((num > 700) ? ((float)(num - 700) / 400f) : 0f);
			string text = cinematic.From.Name.ToUpperInvariant();
			Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(42f * (1f + num12 * 0.5f)), text);
			TechniqueArt.Italic(renderer, TechniqueArt.Display, Pt(42f * (1f + num12 * 0.5f)), text, centerX - point.X / 2, _d.Y(num11 + 30.5f) - point.Y / 2 - _d.L(num12 * 20f), TechniqueArt.Alpha(TechniqueArt.Soft, num6));
		}
		num11 += 61f;
		if (num7 > 0f)
		{
			float num13 = 1f - MathF.Pow(1f - num7, 3f);
			string text2 = cinematic.To.Name.ToUpperInvariant();
			float num14 = 118f * (1.6f - 0.6f * num13);
			Point point2 = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(num14), text2);
			while (point2.X > base.Width - _d.L(80f) && num14 > 40f)
			{
				num14 -= 8f;
				point2 = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(num14), text2);
			}
			int num15 = _d.Y(num11 + 59f);
			int num16 = centerX - point2.X / 2;
			int num17 = num15 - point2.Y / 2;
			TechniqueArt.GlowRect(renderer, new Rectangle(num16 - _d.L(40f), num15 - _d.L(80f), point2.X + _d.L(80f), _d.L(160f)), TechniqueArt.Alpha(new Color(255, 255, 160, 50), 0.7f * num7), additive: false);
			TechniqueArt.Styled(renderer, TechniqueArt.Display, Pt(num14), text2, num16, num17 + _d.L(4f), TechniqueArt.Stops.Solid(new Color(255, 42, 18, 4)), num7);
			TechniqueArt.Styled(renderer, TechniqueArt.Display, Pt(num14), text2, num16, num17, new TechniqueArt.Stops(TechniqueArt.White, 0.3f, new Color(255, 255, 211, 106), 0.65f, TechniqueArt.Ki), num7);
		}
		num11 += 118f;
		if (cinematic.Gains.Count > 0)
		{
			num11 += 18f;
			List<int> list = cinematic.Gains.Select(((Row Row, double Before, double After) gain) => Math.Max(TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(12f), RowName(gain.Row).ToUpperInvariant(), _d.Em(0.2f, 12f)), TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(34f), TechniqueArt.Number(gain.Before) + " » " + TechniqueArt.Number(gain.After)).X)).ToList();
			int num18 = list.Sum() + _d.L(34f) * (list.Count - 1);
			int num19 = centerX - num18 / 2;
			for (int num20 = 0; num20 < cinematic.Gains.Count; num20++)
			{
				if (!(num8 > 0f))
				{
					break;
				}
				(Row Row, double Before, double After) tuple = cinematic.Gains[num20];
				Row item = tuple.Row;
				double item2 = tuple.Before;
				double item3 = tuple.After;
				bool flag2 = (double)Sense(item) * (item3 - item2) > 0.0;
				TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), RowName(item).ToUpperInvariant(), num19, _d.Y(num11 + 8f), _d.Em(0.2f, 12f), TechniqueArt.Alpha(TechniqueArt.Soft, num8));
				string text3 = TechniqueArt.Number(item2) + " » " + TechniqueArt.Number(item3);
				Point point3 = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(34f), text3);
				TechniqueArt.Italic(renderer, TechniqueArt.Display, Pt(34f), text3, num19, _d.Y(num11 + 16f + 24.5f) - point3.Y / 2, TechniqueArt.Alpha(flag2 ? TechniqueArt.Green : TechniqueArt.Red, num8));
				num19 += list[num20] + _d.L(34f);
			}
			num11 += 65f;
		}
		TechniqueArt.TextCenter(renderer, TechniqueArt.Body, Pt(13f), Strings.Techniques.Continue, centerX, _d.Y(num11 + 26f + 9.5f), TechniqueArt.Alpha(TechniqueArt.Soft, flag ? num9 : 0f));
		if (!flag)
		{
			TechniqueArt.TextCenter(renderer, TechniqueArt.Body, Pt(13f), Strings.Techniques.Gathering, centerX, _d.Y(num11 + 26f + 9.5f), TechniqueArt.Alpha(TechniqueArt.Soft, 0.6f + 0.4f * MathF.Sin((float)now / 200f)));
		}
	}

	private void DrawEmblem(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle outer, SpellDescriptor spell, Color color, float iconRatio)
	{
		DrawHexEmblem(renderer, outer, GameContentManager.Current.GetTexture(TextureType.Spell, spell.Icon), TechniqueArt.Glyph(SpellMastery.FamilyOf(spell)), color, iconRatio);
	}

	private void DrawKeyPair(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string first, string second, int centerX, int centerY)
	{
		int num = _d.L(6f);
		int num2 = KeyChipWidth(renderer, first) + num + KeyChipWidth(renderer, second);
		int num3 = centerX - num2 / 2;
		num3 += KeyChip(renderer, first, num3, centerY) + num;
		KeyChip(renderer, second, num3, centerY);
	}

	private void DrawTechniquesTopBar(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		Player me = Globals.Me;
		int[] stats = me?.Stat;
		DrawTopBar(renderer, Strings.Techniques.Title, Strings.Techniques.Subtitle.ToString(me?.Name ?? string.Empty, me?.Level ?? 1), new global::_003C_003Ez__ReadOnlyArray<Gauge>(new Gauge[3]
		{
			new Gauge(Strings.Techniques.Level, (me?.Level ?? 1).ToString(), TechniqueArt.Gold),
			new Gauge(StatLabel(Stat.AbilityPower), StatValue(Stat.AbilityPower).ToString(), TechniqueArt.Cyan),
			new Gauge(StatLabel(Stat.Attack), StatValue(Stat.Attack).ToString(), Strength)
		}));
		static string StatLabel(Stat stat)
		{
			if (!Strings.Combat.Stats.TryGetValue(stat, out LocalizedString value))
			{
				return stat.ToString();
			}
			return value.ToString();
		}
		int StatValue(Stat stat)
		{
			if (stats == null || (int)stat >= stats.Length)
			{
				return 0;
			}
			return stats[(int)stat];
		}
	}

	private void DrawRail(Intersect.Client.Framework.Gwen.Renderer.Base renderer, List<Known> known)
	{
		for (int i = 0; i < TechniqueArt.Families.Length; i++)
		{
			TechniqueFamily family = TechniqueArt.Families[i];
			Rectangle rect = _d.R(24f, 104 + i * 100, 96f, 92f);
			int num = known.Count((Known entry) => entry.Family == family);
			bool flag = family == _family;
			Color color = TechniqueArt.FamilyColor(family);
			string key = $"famille:{family}";
			bool flag2 = num > 0 && !flag && IsHot(key);
			float alpha = ((num > 0) ? 1f : 0.45f);
			if (flag)
			{
				TechniqueArt.Patch(renderer, "tech_chanfrein10.png", rect, A(new Color(230, 12, 17, 28)), 12);
				TechniqueArt.Patch(renderer, "tech_chanfrein10_degrade.png", rect, A(color, 0.3f), 12);
				TechniqueArt.Patch(renderer, "tech_chanfrein10_bord.png", rect, A(color, 0.7f), 12);
				Rectangle rect2 = new Rectangle(rect.X, rect.Y + _d.L(14f), Math.Max(2, _d.L(3f)), rect.Height - _d.L(28f));
				TechniqueArt.Fill(renderer, rect2, A(color));
				TechniqueArt.GlowRect(renderer, new Rectangle(rect2.X - _d.L(12f), rect2.Y - _d.L(6f), rect2.Width + _d.L(24f), rect2.Height + _d.L(12f)), A(color, 0.6f));
			}
			else
			{
				TechniqueArt.Patch(renderer, "tech_chanfrein10.png", rect, A(flag2 ? new Color(217, 22, 30, 48) : new Color(179, 12, 17, 28), alpha), 12);
				TechniqueArt.Patch(renderer, "tech_chanfrein10_bord.png", rect, A(TechniqueArt.Line, alpha), 12);
			}
			string label = TechniqueArt.FamilyName(family).ToUpperInvariant();
			float num2 = _d.Em(0.06f, 12f);
			int num3 = rect.Width - _d.L(8f);
			float num4 = 12f;
			IFont labelFont = TechniqueArt.Label;
			if (Widest(num4, num2) > num3)
			{
				num2 = 0f;
			}
			while (num4 > 9.5f && Widest(num4, num2) > num3)
			{
				num4 -= 0.5f;
			}
			if (Widest(num4, num2) > num3)
			{
				labelFont = TechniqueArt.Heavy ?? TechniqueArt.Label;
				num4 = 12f;
				while (num4 > 9.5f && Widest(num4, num2) > num3)
				{
					num4 -= 0.5f;
				}
			}
			List<string> list = WrapSpaced(renderer, labelFont, Pt(num4), label, num3, num2);
			int num5 = _d.L(13.2f);
			int num6 = _d.L(30f) + _d.L(5f) + num5 * list.Count;
			int num7 = rect.Y + (rect.Height - num6) / 2;
			int num8 = _d.L(30f);
			TechniqueArt.Cell(renderer, "tech_familles.png", TechniqueArt.Glyph(family), 40, new Rectangle(rect.X + (rect.Width - num8) / 2, num7, num8, num8), A(color, alpha));
			Color color2 = (flag ? TechniqueArt.White : (flag2 ? TechniqueArt.Text : TechniqueArt.Soft));
			for (int num9 = 0; num9 < list.Count; num9++)
			{
				TechniqueArt.SpacedCenter(renderer, labelFont, Pt(num4), list[num9], rect.X + rect.Width / 2, num7 + num8 + _d.L(5f) + num5 * num9 + num5 / 2, num2, A(color2, alpha));
			}
			if (num > 0)
			{
				AddHit(rect, key, delegate
				{
					SelectFamily(family, known);
				});
			}
			int Widest(float points, float letterSpacing)
			{
				return (from word in label.Split(' ', StringSplitOptions.RemoveEmptyEntries)
					select TechniqueArt.SpacedWidth(renderer, labelFont, Pt(points), word, letterSpacing)).DefaultIfEmpty(0).Max();
			}
		}
		DrawKeyPair(renderer, "←", "→", _d.X(72f), _d.Y(104 + TechniqueArt.Families.Length * 100 + 10));
	}

	private static List<string> WrapSpaced(Intersect.Client.Framework.Gwen.Renderer.Base renderer, IFont? font, int size, string text, int width, float spacing)
	{
		List<string> list = new List<string>();
		string text2 = string.Empty;
		string[] array = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		foreach (string text3 in array)
		{
			string text4 = ((text2.Length == 0) ? text3 : (text2 + " " + text3));
			if (TechniqueArt.SpacedWidth(renderer, font, size, text4, spacing) <= width)
			{
				text2 = text4;
				continue;
			}
			if (text2.Length > 0)
			{
				list.Add(text2);
			}
			text2 = text3;
			while (TechniqueArt.SpacedWidth(renderer, font, size, text2, spacing) > width && text2.Length > 4)
			{
				int num = text2.Length - 1;
				while (num > 2 && TechniqueArt.SpacedWidth(renderer, font, size, text2.Substring(0, num) + "-", spacing) > width)
				{
					num--;
				}
				list.Add(text2.Substring(0, num) + "-");
				string text5 = text2;
				int num2 = num;
				text2 = text5.Substring(num2, text5.Length - num2);
			}
		}
		if (text2.Length > 0)
		{
			list.Add(text2);
		}
		return list;
	}

	private void DrawList(Intersect.Client.Framework.Gwen.Renderer.Base renderer, List<Known> known)
	{
		Color color = TechniqueArt.FamilyColor(_family);
		List<Known> list = known.Where((Known entry) => entry.Family == _family).ToList();
		string text = TechniqueArt.FamilyName(_family).ToUpperInvariant();
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(26f), text);
		int num = _d.Y(117f);
		TechniqueArt.Italic(renderer, TechniqueArt.Display, Pt(26f), text, _d.X(138f), num - point.Y / 2, A(TechniqueArt.Text));
		string text2 = list.Count switch
		{
			0 => Strings.Techniques.LearnedNone.ToString(), 
			1 => Strings.Techniques.LearnedOne.ToString(), 
			_ => Strings.Techniques.LearnedMany.ToString(list.Count), 
		};
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), text2, _d.X(138f) + point.X + _d.L(12f), _d.Y(121f), A(TechniqueArt.Dim));
		if (list.Count > 1)
		{
			DrawKeyPair(renderer, "↑", "↓", _d.X(457f), num);
		}
		int num2 = _d.Y(142f);
		int num3 = _d.Y(836f);
		_listArea = new Rectangle(_d.X(130f), num2, _d.L(372f), num3 - num2);
		if (list.Count == 0)
		{
			int num4 = num2 + _d.L(10f);
			{
				foreach (string item in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(14f), (known.Count == 0) ? Strings.Techniques.NoTechnique : Strings.Techniques.EmptyFamily, _d.L(332f), 4))
				{
					TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), item, _d.X(142f), num4, A(TechniqueArt.Dim));
					num4 += _d.L(20f);
				}
				return;
			}
		}
		int num5 = _d.L(92f);
		int num6 = list.Count * num5 - _d.L(8f);
		int max = Math.Max(0, num6 - _listArea.Height);
		_listScroll = Math.Clamp(_listScroll, 0, max);
		Rectangle previous = TechniqueArt.BeginClip(renderer, _listArea);
		for (int num7 = 0; num7 < list.Count; num7++)
		{
			int num8 = num2 + num7 * num5 - _listScroll;
			if (num8 + _d.L(84f) >= num2 && num8 <= num3)
			{
				DrawCard(renderer, list[num7], new Rectangle(_d.X(138f), num8, _d.L(352f), _d.L(84f)), color);
			}
		}
		TechniqueArt.EndClip(renderer, previous);
		Known? known2 = Selected(known);
		if (!known2.HasValue)
		{
			return;
		}
		Known valueOrDefault = known2.GetValueOrDefault();
		if (valueOrDefault.Family == _family)
		{
			string text3 = Strings.Techniques.Forget.ToString();
			Color color2 = (IsHot("oublier") ? TechniqueArt.Red : TechniqueArt.Soft);
			int num9 = _d.L(10f);
			int num10 = Math.Min(_d.Y(856f), num2 + num6 + _d.L(22f));
			TechniqueArt.Image(renderer, "tech_croix.png", new Rectangle(_d.X(140f), num10 - num9 / 2, num9, num9), A(color2));
			int num11 = TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), text3, _d.X(140f) + num9 + _d.L(8f), num10, A(color2));
			int slot = valueOrDefault.Slot;
			AddHit(new Rectangle(_d.X(136f), num10 - _d.L(12f), num9 + _d.L(14f) + num11, _d.L(24f)), "oublier", delegate
			{
				Globals.Me?.TryForgetSpell(slot);
			});
		}
	}

	private void DrawCard(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Known entry, Rectangle rect, Color color)
	{
		string key = $"carte:{entry.Slot}";
		bool flag = entry.Slot == _slot;
		bool flag2 = !flag && IsHot(key);
		int num = (flag ? _d.L(8f) : (flag2 ? _d.L(4f) : 0));
		Rectangle rectangle = new Rectangle(rect.X + num, rect.Y, rect.Width, rect.Height);
		if (flag)
		{
			TechniqueArt.Patch(renderer, "tech_chanfrein8.png", rectangle, A(new Color(230, 12, 17, 28)), 10);
			TechniqueArt.Patch(renderer, "tech_chanfrein8_degrade.png", rectangle, A(TechniqueArt.Mix(color, new Color(255, 18, 26, 42), 0.22f)), 10);
			TechniqueArt.Patch(renderer, "tech_chanfrein8_bord.png", rectangle, A(color, 0.8f), 10);
			Rectangle rect2 = new Rectangle(rectangle.Right - Math.Max(2, _d.L(3f)), rectangle.Y + 1, Math.Max(2, _d.L(3f)), rectangle.Height - 2);
			TechniqueArt.Fill(renderer, rect2, A(color));
			TechniqueArt.GlowRect(renderer, new Rectangle(rect2.X - _d.L(14f), rect2.Y, rect2.Width + _d.L(28f), rect2.Height), A(color, 0.55f));
		}
		else
		{
			TechniqueArt.Patch(renderer, "tech_chanfrein8.png", rectangle, A(CardRight), 10);
			TechniqueArt.Patch(renderer, "tech_chanfrein8_degrade.png", rectangle, A(CardLeft), 10);
			TechniqueArt.Patch(renderer, "tech_chanfrein8_bord.png", rectangle, A(flag2 ? TechniqueArt.Line2 : TechniqueArt.Line), 10);
		}
		Rectangle rectangle2 = new Rectangle(rectangle.X + 1, rectangle.Y + 1, rectangle.Width - 2, rectangle.Height - 2);
		DrawEmblem(renderer, new Rectangle(rectangle2.X + _d.L(10f), rectangle2.Y + _d.L(10f), _d.L(62f), _d.L(62f)), entry.Spell, color, 0.62068963f);
		int stage = SpellMastery.Lineage(entry.Spell).IndexOf(entry.Spell) + 1;
		string text = TechniqueArt.Roman(stage);
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(24f), text);
		int num2 = rectangle2.Right - _d.L(12f);
		int num3 = rectangle2.Y + rectangle2.Height / 2;
		TechniqueArt.Glow(renderer, (float)num2 - (float)point.X / 2f, num3, _d.L(16f), A(color, 0.35f));
		TechniqueArt.Italic(renderer, TechniqueArt.Display, Pt(24f), text, num2 - point.X, num3 - point.Y / 2, A(TechniqueArt.Mix(color, TechniqueArt.White, 0.85f)));
		int num4 = rectangle2.X + _d.L(84f);
		int width = num2 - point.X - _d.L(12f) - num4;
		int spacing = 0;
		string text2 = SaiyanHud.Fit(renderer, TechniqueArt.Heavy, Pt(22f), entry.Spell.Name, width, ref spacing);
		int num5 = rectangle2.Y + (rectangle2.Height - _d.L(46.4f)) / 2;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(22f), text2, num4, num5 + _d.L(11.5f), A(TechniqueArt.Text));
		int num6 = num5 + _d.L(37.7f);
		if (SpellMastery.HasMastery(entry.Spell))
		{
			for (int i = 1; i <= 5; i++)
			{
				Rectangle rect3 = new Rectangle(num4 + _d.L((i - 1) * 17), num6 - _d.L(3f), Math.Max(4, _d.L(14f)), Math.Max(3, _d.L(6f)));
				if (i <= entry.Rank)
				{
					TechniqueArt.Glow(renderer, (float)rect3.X + (float)rect3.Width / 2f, (float)rect3.Y + (float)rect3.Height / 2f, _d.L(8f), A(color, 0.35f));
					TechniqueArt.Image(renderer, "tech_pip_mini.png", rect3, A(color));
				}
				else
				{
					TechniqueArt.Image(renderer, "tech_pip_mini.png", rect3, A(new Color(255, 10, 14, 24)));
					TechniqueArt.Image(renderer, "tech_pip_mini_bord.png", rect3, A(TechniqueArt.Line2));
				}
			}
			int num7 = num4 + _d.L(90f);
			string text3 = Strings.Techniques.Rank.ToString(entry.Rank);
			num7 += TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), text3, num7, num6, A(TechniqueArt.Soft)) + _d.L(8f);
			if (CanEvolve(entry, out SpellDescriptor _))
			{
				string text4 = Strings.Techniques.Ready.ToString().ToUpperInvariant();
				float spacing2 = _d.Em(0.1f, 12f);
				int w = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(12f), text4, spacing2) + _d.L(14f);
				Rectangle rect4 = new Rectangle(num7, num6 - _d.L(9f), w, _d.L(18f));
				float num8 = 0.5f - 0.5f * MathF.Cos((float)Timing.Global.MillisecondsUtc / 1600f * ((float)Math.PI * 2f));
				TechniqueArt.GlowRect(renderer, new Rectangle(rect4.X - _d.L(14f), rect4.Y - _d.L(10f), rect4.Width + _d.L(28f), rect4.Height + _d.L(20f)), A(TechniqueArt.Gold, 0.45f * num8));
				TechniqueArt.Patch(renderer, "tech_biais4.png", rect4, A(TechniqueArt.Mix(TechniqueArt.Gold, TechniqueArt.White, 1f - 0.2f * num8)), 5);
				TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), text4, rect4.X + _d.L(7f), num6, spacing2, A(new Color(255, 26, 18, 6)));
			}
		}
		else
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), Strings.Techniques.MasteryNone, num4, num6, A(TechniqueArt.Soft));
		}
		if (entry.Slot == _porteSlot)
		{
			TechniqueArt.Patch(renderer, "tech_chanfrein8.png", rectangle, A(new Color(185, 6, 8, 14)), 10);
			TechniqueArt.Patch(renderer, "tech_chanfrein8_bord.png", rectangle, A(color, 0.55f), 10);
		}
		int slot = entry.Slot;
		_cartesDessinees[slot] = rectangle;
		AddHit(Rectangle.Intersect(rect, _listArea), key, delegate
		{
			SelectSlot(slot);
		});
	}

	private void DrawEmptyCenter(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		Rectangle rect = _d.R(510f, 104f, 640f, 300f);
		TechniqueArt.Fill(renderer, rect, A(new Color(255, 5, 7, 12)));
		TechniqueArt.TextCenter(renderer, TechniqueArt.Body, Pt(16f), Strings.Techniques.NoTechnique, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, A(TechniqueArt.Soft));
	}

	private void DrawIdentity(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Known entry, long now)
	{
		SpellDescriptor spell = entry.Spell;
		Color color = TechniqueArt.FamilyColor(entry.Family);
		List<SpellDescriptor> list = SpellMastery.Lineage(spell);
		int num = list.IndexOf(spell) + 1;
		DrawEmblem(renderer, _d.R(510f, 428f, 96f, 96f), spell, color, 0.6086956f);
		AddHit(_d.R(510f, 428f, 96f, 96f), "embleme", delegate
		{
		});
		string text = (TechniqueArt.FamilyName(entry.Family) + " · " + Strings.Techniques.Stage.ToString(TechniqueArt.Roman(num))).ToUpperInvariant();
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(13f), text, _d.X(624f), _d.Y(433.6f), _d.Em(0.18f, 13f), A(TechniqueArt.Mix(color, TechniqueArt.White, 0.9f)));
		string text2 = spell.Name.ToUpperInvariant();
		float num2 = ((text2.Length > 13) ? 46f : 58f);
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(num2), text2);
		int num3 = _d.L(404f);
		while (point.X > num3 && num2 > 30f)
		{
			num2 -= 4f;
			point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(num2), text2);
		}
		int num4 = _d.Y(471.84998f);
		int num5 = num4 - point.Y / 2;
		int num6 = _d.X(624f);
		TechniqueArt.GlowRect(renderer, new Rectangle(num6 - _d.L(20f), num4 - _d.L(46f), point.X + _d.L(40f), _d.L(92f)), A(color, 0.3f), additive: false);
		TechniqueArt.Styled(renderer, TechniqueArt.Display, Pt(num2), text2, num6, num5 + _d.L(3f), TechniqueArt.Stops.Solid(TechniqueArt.Black), 0.6f * _fade);
		TechniqueArt.Styled(renderer, TechniqueArt.Display, Pt(num2), text2, num6, num5, new TechniqueArt.Stops(TechniqueArt.White, 0.35f, TechniqueArt.Mix(color, TechniqueArt.White, 0.6f), 0.7f, color), _fade);
		string text3 = (string.IsNullOrWhiteSpace(spell.Description) ? Strings.Techniques.NoDescription.ToString() : spell.Description);
		float num7 = 517f;
		foreach (string item in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(16f), text3, num3, 2))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(16f), item, num6, _d.Y(num7), A(RoleText));
			num7 += 23.2f;
		}
		Rectangle rect = _d.R(1046f, 424f, 104f, 104f);
		float num8 = (float)rect.X + (float)rect.Width / 2f;
		float num9 = (float)rect.Y + (float)rect.Height / 2f;
		TechniqueArt.Image(renderer, "tech_sceau_cercle.png", rect, A(TechniqueArt.White, 0.08f));
		if (renderer is IntersectRenderer intersectRenderer)
		{
			IGameTexture gameTexture = TechniqueArt.Tex("tech_sceau_arc.png");
			if (gameTexture != null)
			{
				intersectRenderer.DrawColor = A(color);
				for (int num10 = 0; num10 < Math.Min(num, 4); num10++)
				{
					intersectRenderer.DrawTextureRotated(gameTexture, rect.X, rect.Y, rect.Width, rect.Height, num10 * 90);
				}
			}
		}
		for (int num11 = 0; num11 < 4; num11++)
		{
			float x = -(float)Math.PI / 2f + (float)num11 * (float)Math.PI / 2f;
			float num12 = num8 + MathF.Cos(x) * (float)_d.L(46f);
			float num13 = num9 + MathF.Sin(x) * (float)_d.L(46f);
			int num14 = Math.Max(4, _d.L(6f));
			int num15 = Math.Max(3, _d.L(4f));
			TechniqueArt.Image(renderer, "tech_point.png", new Rectangle((int)(num12 - (float)num14), (int)(num13 - (float)num14), num14 * 2, num14 * 2), A(TechniqueArt.Void));
			TechniqueArt.Image(renderer, "tech_point.png", new Rectangle((int)(num12 - (float)num15), (int)(num13 - (float)num15), num15 * 2, num15 * 2), A((num11 < num) ? color : new Color(255, 26, 34, 51)));
		}
		string text4 = TechniqueArt.Roman(num);
		Point point2 = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(44f), text4);
		TechniqueArt.Glow(renderer, num8, num9, _d.L(34f), A(color, 0.4f));
		TechniqueArt.Italic(renderer, TechniqueArt.Display, Pt(44f), text4, (int)(num8 - (float)point2.X / 2f), (int)(num9 - (float)point2.Y / 2f), A(TechniqueArt.White));
		TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(12f), Strings.Techniques.StageSeal.ToString().ToUpperInvariant(), (int)num8, _d.Y(525.5f), _d.Em(0.18f, 12f), A(TechniqueArt.Soft));
	}

	private void DrawRightColumn(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Known entry, long now)
	{
		SpellDescriptor spell = entry.Spell;
		SpellDescriptor next = SpellMastery.EvolvesInto(spell);
		DrawDetailsToggle(renderer, _d.X(1570f), _d.Y(117f));
		float top = 142f;
		top = DrawMasteryPanel(renderer, entry, top, now) + 12f;
		top = DrawDamagePanel(renderer, entry, top) + 12f;
		if (base.Details && !EnTransport)
		{
			top = DrawGainsPanel(renderer, entry, next, top) + 12f;
		}
		DrawHotbarPanel(renderer, entry, top);
	}

	private float DrawMasteryPanel(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Known entry, float top, long now)
	{
		SpellDescriptor spell = entry.Spell;
		Color color = TechniqueArt.FamilyColor(entry.Family);
		if (!SpellMastery.HasMastery(spell))
		{
			Panel(renderer, _d.R(1150f, top, 420f, 134.5f));
			Heading(renderer, 1166f, top + 14f, 388f, Strings.Techniques.Mastery, null);
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(16f), Strings.Techniques.MasteryNone, _d.X(1166f), _d.Y(top + 70f), A(TechniqueArt.Soft));
			return top + 134.5f;
		}
		List<string> list = ((!base.Details && entry.Rank < 5) ? StepsOf(spell) : new List<string>());
		string text = ((list.Count == 0) ? string.Empty : string.Join(" · ", list));
		string text2 = ((list.Count == 0) ? string.Empty : (Strings.Techniques.NextRankPrefix.ToString(entry.Rank + 1) + " "));
		int num = ((list.Count != 0) ? TechniqueArt.Measure(renderer, TechniqueArt.Body, Pt(14f), text2).X : 0);
		List<string> list2 = ((list.Count == 0) ? new List<string>() : TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(14f), text, _d.L(388f) - num, 2));
		float num2 = 124.4f + (float)((list2.Count > 0) ? (17 + list2.Count * 20) : 0) + 14f;
		Panel(renderer, _d.R(1150f, top, 420f, num2));
		int num3 = SpellMastery.ExperienceToNext(spell, entry.Rank);
		string detail = ((entry.Rank >= 5) ? Strings.Techniques.MasteryComplete.ToString() : $"{entry.Experience} / {num3}");
		Heading(renderer, 1166f, top + 14f, 388f, Strings.Techniques.Mastery, detail);
		float num4 = 72.8f;
		for (int i = 1; i <= 5; i++)
		{
			float x = 1166f + (float)(i - 1) * (num4 + 6f);
			Rectangle rectangle = _d.R(x, top + 41.4f, num4, 38f);
			int num5 = _d.L(4.74f);
			Rectangle rect = new Rectangle(rectangle.X - num5, rectangle.Y, rectangle.Width + num5 * 2, rectangle.Height);
			bool flag = i <= entry.Rank;
			bool flag2 = i == entry.Rank + 1;
			string key = $"rang:{i}";
			bool flag3 = IsHot(key);
			if (flag)
			{
				TechniqueArt.GlowRect(renderer, new Rectangle(rect.X - _d.L(12f), rect.Y - _d.L(12f), rect.Width + _d.L(24f), rect.Height + _d.L(24f)), A(color, 0.3f));
				TechniqueArt.Image(renderer, "tech_pip.png", rect, A(TechniqueArt.Mix(color, TechniqueArt.Black, 0.55f)));
				TechniqueArt.Image(renderer, "tech_pip_haut.png", rect, A(TechniqueArt.Mix(color, TechniqueArt.White, 0.85f)));
				TechniqueArt.Image(renderer, "tech_pip_bord.png", rect, A(TechniqueArt.Mix(color, TechniqueArt.White, 0.8f)));
			}
			else
			{
				TechniqueArt.Image(renderer, "tech_pip.png", rect, A(PipBack));
				TechniqueArt.Image(renderer, flag2 ? "tech_pip_tirets.png" : "tech_pip_bord.png", rect, A(flag2 ? TechniqueArt.Gold : (flag3 ? TechniqueArt.Soft : TechniqueArt.Line2)));
			}
			if (_viewRank == i)
			{
				int num6 = Math.Max(2, _d.L(3f));
				TechniqueArt.Image(renderer, "tech_pip_bord2.png", new Rectangle(rect.X - num6, rect.Y - num6, rect.Width + num6 * 2, rect.Height + num6 * 2), A(TechniqueArt.White));
			}
			string text3 = i.ToString();
			Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(19f), text3);
			Color color2 = (flag ? new Color(255, 20, 10, 2) : (flag2 ? TechniqueArt.Gold : TechniqueArt.Dim));
			TechniqueArt.Italic(renderer, TechniqueArt.Display, Pt(19f), text3, rectangle.X + (rectangle.Width - point.X) / 2, rectangle.Y + (rectangle.Height - point.Y) / 2, A(color2));
			int chosen = i;
			AddHit(rect, key, delegate
			{
				_viewRank = ((_viewRank != chosen) ? chosen : 0);
			});
		}
		Rectangle rect2 = _d.R(1166f, top + 89.4f, 388f, 9f);
		TechniqueArt.Fill(renderer, rect2, A(BarBack));
		TechniqueArt.Outline(renderer, rect2, A(TechniqueArt.Line2));
		float num7 = ((entry.Rank >= 5) ? 1f : Math.Clamp((float)entry.Experience / (float)Math.Max(1, num3), 0f, 1f));
		Rectangle rectangle2 = new Rectangle(rect2.X + 1, rect2.Y + 1, (int)((float)(rect2.Width - 2) * num7), rect2.Height - 2);
		if (rectangle2.Width > 0)
		{
			TechniqueArt.FillHorizontal(renderer, rectangle2, A(TechniqueArt.Mix(color, TechniqueArt.Black, 0.55f)), A(color), A(TechniqueArt.Mix(color, TechniqueArt.White, 0.6f)));
			float num8 = (float)(now % 2400) / 2400f;
			Rectangle rect3 = new Rectangle(rectangle2.X + _d.L(-60f + 520f * num8), rectangle2.Y, _d.L(60f), rectangle2.Height);
			Rectangle previous = TechniqueArt.BeginClip(renderer, rectangle2);
			TechniqueArt.Image(renderer, "tech_reflet.png", rect3, A(TechniqueArt.White, 0.55f));
			TechniqueArt.EndClip(renderer, previous);
		}
		int centerY = _d.Y(top + 114.4f);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), SpellMastery.IsOffensive(spell) ? Strings.Techniques.MasteryHintHit : Strings.Techniques.MasteryHintCast, _d.X(1166f), centerY, A(TechniqueArt.Soft));
		if (entry.Rank >= 5)
		{
			TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(14f), Strings.Techniques.MaxRank, _d.X(1554f), centerY, A(TechniqueArt.Gold));
		}
		else
		{
			string text4 = Math.Max(0, num3 - entry.Experience).ToString();
			int num9 = _d.X(1554f);
			num9 -= TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(14f), text4, num9, centerY, A(TechniqueArt.Text));
			TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(14f), Strings.Techniques.NextRank.ToString(entry.Rank + 1, string.Empty).TrimEnd() + " ", num9, centerY, A(TechniqueArt.Soft));
		}
		if (list2.Count > 0)
		{
			float num10 = top + 114.4f + 10f + 8f;
			TechniqueArt.Fill(renderer, _d.R(1166f, num10, 388f, 1f), A(TechniqueArt.White, 0.06f));
			float num11 = num10 + 1f + 8f + 10f;
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), text2, _d.X(1166f), _d.Y(num11), A(TechniqueArt.Soft));
			foreach (string item in list2)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(14f), item, _d.X(1166f) + num, _d.Y(num11), A(TechniqueArt.Green));
				num11 += 20f;
			}
		}
		return top + num2;
	}

	private float DrawDamagePanel(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Known entry, float top)
	{
		SpellDescriptor spell = entry.Spell;
		int num = ((_viewRank > 0) ? _viewRank : entry.Rank);
		DamageSplit? damageSplit = DamageOf(spell, num);
		if (damageSplit.HasValue)
		{
			DamageSplit valueOrDefault = damageSplit.GetValueOrDefault();
			float num2 = (base.Details ? 155.4f : 107.4f);
			Panel(renderer, _d.R(1150f, top, 420f, num2));
			Heading(renderer, 1166f, top + 14f, 388f, Strings.Techniques.Damage, ((_viewRank > 0) ? Strings.Techniques.DamageChosen : Strings.Techniques.DamageDetail).ToString(num));
			float num3 = top + 14f + 17.4f + 10f;
			string text = valueOrDefault.Total.ToString();
			Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(52f), text);
			int num4 = _d.Y(num3 + 44.72f);
			int y = num4 - (int)((float)point.Y * 0.71f);
			TechniqueArt.Italic(renderer, TechniqueArt.Display, Pt(52f), text, _d.X(1166f), y, A(TechniqueArt.Text));
			int num5 = _d.X(1166f) + point.X + _d.L(4f);
			if (valueOrDefault.Hits > 1)
			{
				string text2 = $" ×{valueOrDefault.Hits}";
				Point point2 = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(30f), text2);
				TechniqueArt.Italic(renderer, TechniqueArt.Display, Pt(30f), text2, num5, num4 - (int)((float)point2.Y * 0.71f), A(TechniqueArt.Soft));
				num5 += point2.X;
			}
			LocalizedString localizedString = ((entry.Family == TechniqueFamily.Melee) ? Strings.Techniques.PerHit : ((valueOrDefault.Hits > 1) ? Strings.Techniques.PerBall : Strings.Techniques.PerShot));
			Point point3 = TechniqueArt.Measure(renderer, TechniqueArt.Body, Pt(14f), localizedString);
			TechniqueArt.Write(renderer, TechniqueArt.Body, Pt(14f), localizedString, num5 + _d.L(10f), num4 - (int)((float)point3.Y * 0.74f), A(TechniqueArt.Soft));
			if (!base.Details)
			{
				return top + num2;
			}
			float num6 = num3 + 52f + 10f;
			Rectangle rect = _d.R(1166f, num6, 388f, 12f);
			TechniqueArt.Fill(renderer, rect, A(BarBack));
			Rectangle rectangle = new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2);
			int num7 = ((valueOrDefault.Total > 0) ? ((int)((double)(rectangle.Width * valueOrDefault.Base) / (double)valueOrDefault.Total)) : 0);
			TechniqueArt.FillVertical(renderer, new Rectangle(rectangle.X, rectangle.Y, num7, rectangle.Height), A(new Color(255, 255, 231, 168)), A(new Color(255, 217, 160, 43)));
			TechniqueArt.FillVertical(renderer, new Rectangle(rectangle.X + num7, rectangle.Y, rectangle.Width - num7, rectangle.Height), A(new Color(255, 159, 230, 255)), A(new Color(255, 47, 143, 214)));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line2));
			int centerY = _d.Y(num6 + 12f + 6f + 10f);
			int num8 = _d.X(1166f);
			num8 += TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), string.Concat(Strings.Techniques.TechniqueBase, " "), num8, centerY, A(TechniqueArt.Soft));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(14f), TechniqueArt.Number(valueOrDefault.Base), num8, centerY, A(TechniqueArt.Gold));
			int num9 = _d.X(1554f);
			num9 -= TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(14f), $"+{valueOrDefault.Power}", num9, centerY, A(TechniqueArt.Cyan));
			TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(14f), Strings.Techniques.StatShare.ToString(valueOrDefault.Percent, valueOrDefault.StatName) + " ", num9, centerY, A(TechniqueArt.Soft));
			return top + num2;
		}
		string text3 = (string.IsNullOrWhiteSpace(spell.Description) ? Strings.Techniques.NoDescription.ToString() : spell.Description);
		List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(16f), text3, _d.L(388f), 5);
		float num10 = 41.4f + (float)list.Count * 23.2f + 14f + 2f;
		Panel(renderer, _d.R(1150f, top, 420f, num10));
		Heading(renderer, 1166f, top + 14f, 388f, Strings.Techniques.Effect, null);
		float num11 = top + 14f + 17.4f + 10f + 11.6f;
		foreach (string item in list)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(16f), item, _d.X(1166f), _d.Y(num11), A(new Color(255, 213, 219, 232)));
			num11 += 23.2f;
		}
		return top + num10;
	}

	private static bool RowChanges(Row row, SpellDescriptor spell, SpellDescriptor? next)
	{
		double num = ValueOf(row, spell, 1);
		for (int i = 2; i <= 5; i++)
		{
			if (Math.Abs(ValueOf(row, spell, i) - num) > 0.0001)
			{
				return true;
			}
		}
		if (next != null)
		{
			return Math.Abs(ValueOf(row, next, 1) - ValueOf(row, spell, 5)) > 0.0001;
		}
		return false;
	}

	private float DrawGainsPanel(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Known entry, SpellDescriptor? next, float top)
	{
		SpellDescriptor spell = entry.Spell;
		Color color = TechniqueArt.FamilyColor(entry.Family);
		List<Row> list = RowsFor(spell, next);
		int num = list.Count((Row row) => row != Row.Damage && !RowChanges(row, spell, next));
		List<Row> list2 = ((_toutVoir || num == 0) ? list : list.Where((Row row) => row == Row.Damage || RowChanges(row, spell, next)).ToList());
		List<string> list3 = StepsOf(spell);
		int num2 = (SpellMastery.HasMastery(spell) ? entry.Rank : 0);
		int num3 = SpellMastery.Lineage(spell).IndexOf(spell) + 1;
		object obj;
		if (next != null)
		{
			DamageSplit? damageSplit = DamageOf(spell, 5);
			if (damageSplit.HasValue)
			{
				DamageSplit valueOrDefault = damageSplit.GetValueOrDefault();
				damageSplit = DamageOf(next, 1);
				if (damageSplit.HasValue)
				{
					DamageSplit valueOrDefault2 = damageSplit.GetValueOrDefault();
					obj = Strings.Techniques.RuleDamage.ToString(valueOrDefault.Total, next.Name, valueOrDefault2.Total);
					goto IL_0166;
				}
			}
			obj = Strings.Techniques.RuleOther.ToString(next.Name);
		}
		else
		{
			obj = null;
		}
		goto IL_0166;
		IL_0166:
		string text = (string)obj;
		List<string> list4 = ((text == null) ? new List<string>() : TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(14f), text, _d.L(366f), 3));
		float num4 = 24f + list2.Sum((Row row) => (row != Row.Damage) ? 34f : 38f);
		int num5 = ((num > 0) ? 28 : 0);
		float num6 = 41.4f + num4 + (float)num5 + 10f + 22f + (float)((list4.Count > 0) ? (26 + list4.Count * 20) : 0) + 14f + 2f;
		Panel(renderer, _d.R(1150f, top, 420f, num6));
		Heading(renderer, 1166f, top + 14f, 388f, Strings.Techniques.Gains, Strings.Techniques.Stage.ToString(TechniqueArt.Roman(num3)));
		float cellWidth = 44.4f;
		float num7 = 1274f + 5f * cellWidth;
		float num8 = top + 14f + 17.4f + 10f;
		if (num2 > 0)
		{
			TechniqueArt.Fill(renderer, _d.R(CellX(num2 - 1), num8, cellWidth, num4), A(color, 0.16f));
		}
		TechniqueArt.Fill(renderer, _d.R(num7, num8, 58f, num4), A(TechniqueArt.Gold, 0.07f));
		int centerY = _d.Y(num8 + 9f);
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), Strings.Techniques.RowRank.ToString().ToUpperInvariant(), _d.X(1166f), centerY, _d.Em(0.1f, 12f), A(TechniqueArt.Dim));
		for (int num9 = 0; num9 < 5; num9++)
		{
			Color color2 = ((num9 + 1 == num2) ? TechniqueArt.Mix(color, TechniqueArt.White, 0.85f) : TechniqueArt.Dim);
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(12f), (num9 + 1).ToString(), _d.X(CellX(num9) + cellWidth / 2f), centerY, 0f, A(color2));
		}
		string text2 = ((next != null) ? ("» " + TechniqueArt.Roman(num3 + 1)) : Strings.Techniques.Max.ToString().ToUpperInvariant());
		TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(12f), text2, _d.X(num7 + 29f), centerY, _d.Em(0.1f, 12f), A(TechniqueArt.Gold));
		float num10 = num8 + 24f;
		foreach (Row item in list2)
		{
			bool flag = item == Row.Damage;
			float num11 = (flag ? 38f : 34f);
			int centerY2 = _d.Y(num10 + num11 / 2f);
			TechniqueArt.Fill(renderer, new Rectangle(_d.X(1166f), _d.Y(num10), _d.L(388f), 1), A(TechniqueArt.White, 0.05f));
			TechniqueArt.TextMiddle(renderer, flag ? TechniqueArt.Label : TechniqueArt.Body, Pt(14f), RowName(item), _d.X(1166f), centerY2, A(flag ? TechniqueArt.Text : TechniqueArt.Soft));
			for (int num12 = 0; num12 < 5; num12++)
			{
				int num13 = num12 + 1;
				double num14 = ValueOf(item, spell, num13);
				Color color3 = TableText;
				bool flag2 = false;
				if (num2 > 0)
				{
					if (num13 == num2)
					{
						color3 = TechniqueArt.White;
						flag2 = true;
					}
					else if (num13 == num2 + 1)
					{
						color3 = TechniqueArt.Text;
						if (Math.Abs(num14 - ValueOf(item, spell, num2)) > 0.0001 && (double)Sense(item) * (num14 - ValueOf(item, spell, num2)) > 0.0)
						{
							color3 = TechniqueArt.Green;
							flag2 = true;
						}
					}
					else if (num13 > num2 + 1)
					{
						color3 = TechniqueArt.Dim;
					}
				}
				IFont font = (flag ? TechniqueArt.Heavy : (flag2 ? TechniqueArt.Label : TechniqueArt.Body));
				int size = (flag ? Pt(20f) : Pt(15f));
				TechniqueArt.TextCenter(renderer, font, size, TechniqueArt.Number(num14), _d.X(CellX(num12) + cellWidth / 2f), centerY2, A(color3));
				if (_viewRank == num13)
				{
					Rectangle rectangle = _d.R(CellX(num12), num10, cellWidth, num11);
					TechniqueArt.Fill(renderer, new Rectangle(rectangle.X, rectangle.Y, 1, rectangle.Height), A(TechniqueArt.White, 0.45f));
					TechniqueArt.Fill(renderer, new Rectangle(rectangle.Right - 1, rectangle.Y, 1, rectangle.Height), A(TechniqueArt.White, 0.45f));
				}
			}
			Color color4 = TechniqueArt.Gold;
			string text3;
			if (next != null)
			{
				double num15 = ValueOf(item, next, 1);
				text3 = TechniqueArt.Number(num15);
				if ((double)Sense(item) * (num15 - ValueOf(item, spell, 5)) < -0.0001)
				{
					color4 = TechniqueArt.Red;
				}
			}
			else
			{
				text3 = "—";
				color4 = TechniqueArt.Dim;
			}
			TechniqueArt.TextCenter(renderer, flag ? TechniqueArt.Heavy : TechniqueArt.Label, flag ? Pt(20f) : Pt(15f), text3, _d.X(num7 + 29f), centerY2, A(color4));
			num10 += num11;
		}
		if (num > 0)
		{
			float y = num10 + 8f + 10f;
			int num16 = _d.Y(y);
			bool flag3 = IsHot("toutvoir");
			int num17 = _d.X(1166f);
			int num18 = num17;
			string text4 = (_toutVoir ? Strings.Techniques.Collapse : Strings.Techniques.ShowAll).ToString();
			num17 += TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), text4, num17, num16, A(flag3 ? TechniqueArt.Text : TechniqueArt.Soft)) + _d.L(8f);
			num17 += KeyChip(renderer, "G", num17, num16, flag3) + _d.L(8f);
			string text5 = ((num == 1) ? Strings.Techniques.RowUnchanged.ToString(num) : Strings.Techniques.RowsUnchanged.ToString(num));
			num17 += TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), "· " + text5, num17, num16, A(TechniqueArt.Dim));
			AddHit(new Rectangle(num18, num16 - _d.L(12f), num17 - num18, _d.L(24f)), "toutvoir", delegate
			{
				_toutVoir = !_toutVoir;
			});
			num10 += 28f;
		}
		int num19 = _d.Y(num10 + 10f + 11f);
		int num20 = _d.X(1166f);
		num20 += TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), Strings.Techniques.EachRank, num20, num19, A(TechniqueArt.Soft)) + _d.L(6f);
		if (list3.Count == 0)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), Strings.Techniques.NoGain, num20, num19, A(TechniqueArt.Dim));
		}
		foreach (string item2 in list3)
		{
			int num21 = TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(14f), item2).X + _d.L(14f);
			if (num20 + num21 > _d.X(1554f))
			{
				break;
			}
			Rectangle rect = new Rectangle(num20, num19 - _d.L(11f), num21, _d.L(22f));
			TechniqueArt.Patch(renderer, "tech_biais5.png", rect, A(TechniqueArt.Green, 0.08f), 6);
			TechniqueArt.Patch(renderer, "tech_biais5_bord.png", rect, A(TechniqueArt.Green, 0.25f), 6);
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(14f), item2, rect.X + rect.Width / 2, num19, A(TechniqueArt.Green));
			num20 = rect.Right + _d.L(6f);
		}
		if (list4.Count > 0)
		{
			float num22 = num10 + 10f + 22f + 10f;
			int num23 = 16 + list4.Count * 20;
			Rectangle rect2 = _d.R(1166f, num22, 388f, num23);
			TechniqueArt.Image(renderer, "tech_degrade.png", rect2, A(TechniqueArt.Gold, 0.1f));
			TechniqueArt.Fill(renderer, new Rectangle(rect2.X, rect2.Y, Math.Max(1, _d.L(2f)), rect2.Height), A(TechniqueArt.Gold));
			float num24 = num22 + 8f + 10f;
			foreach (string item3 in list4)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), item3, _d.X(1176f), _d.Y(num24), A(RuleText));
				num24 += 20f;
			}
		}
		return top + num6;
		float CellX(int index)
		{
			return 1274f + (float)index * cellWidth;
		}
	}

	private void DrawHotbarPanel(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Known entry, float top)
	{
		HotbarInstance[] array = Globals.Me?.Hotbar;
		if (array == null)
		{
			return;
		}
		Color color = TechniqueArt.FamilyColor(entry.Family);
		int num = Math.Min(10, array.Length);
		int num2 = Array.FindIndex(array, (HotbarInstance hotbarInstance) => hotbarInstance?.ItemOrSpellId == entry.Spell.Id);
		float num3 = 34.3f;
		Guid[] array2 = new Guid[num];
		for (int num4 = 0; num4 < num; num4++)
		{
			array2[num4] = array[num4]?.ItemOrSpellId ?? Guid.Empty;
		}
		int cible = (EnTransport ? CaseSous(_porteSouris) : (-1));
		string text = (EnTransport ? PhraseDuTransport(array2, cible, entry.Spell.Id, KeyOf) : Strings.Techniques.HotbarDragHint.ToString(KeyOf(0), KeyOf(9)));
		List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(14f), text, _d.L(388f), 2);
		float num5 = 41.4f + num3 + 8f + (float)(list.Count * 20) + 14f + 2f;
		if (_d.Y(top + num5) > _d.Y(870f))
		{
			return;
		}
		Panel(renderer, _d.R(1150f, top, 420f, num5));
		Heading(renderer, 1166f, top + 14f, 388f, Strings.Techniques.Hotbar, (num2 >= 0) ? Strings.Techniques.HotbarKey.ToString(KeyOf(num2)) : Strings.Techniques.HotbarNone.ToString());
		float num6 = top + 14f + 17.4f + 10f;
		for (int num7 = 0; num7 < num; num7++)
		{
			Rectangle rectangle = _d.R(1166f + (float)num7 * (num3 + 5f), num6, num3, num3);
			string key = $"raccourci:{num7}";
			bool flag = IsHot(key);
			bool flag2 = num7 == num2;
			if (flag2)
			{
				TechniqueArt.GlowRect(renderer, new Rectangle(rectangle.X - _d.L(12f), rectangle.Y - _d.L(12f), rectangle.Width + _d.L(24f), rectangle.Height + _d.L(24f)), A(color, 0.4f));
			}
			TechniqueArt.Fill(renderer, rectangle, A(BarBack));
			if (flag2)
			{
				TechniqueArt.GlowRect(renderer, rectangle, A(color, 0.25f));
			}
			TechniqueArt.Outline(renderer, rectangle, A(flag2 ? color : (flag ? TechniqueArt.Soft : TechniqueArt.Line)));
			Guid guid = array[num7]?.ItemOrSpellId ?? Guid.Empty;
			IGameTexture gameTexture = null;
			ItemDescriptor databaseObject2;
			if (guid != Guid.Empty && DatabaseObject<SpellDescriptor>.TryGet(guid, out var databaseObject))
			{
				gameTexture = GameContentManager.Current.GetTexture(TextureType.Spell, databaseObject.Icon);
			}
			else if (guid != Guid.Empty && DatabaseObject<ItemDescriptor>.TryGet(guid, out databaseObject2))
			{
				gameTexture = GameContentManager.Current.GetTexture(TextureType.Item, databaseObject2.Icon);
			}
			if (gameTexture != null)
			{
				int num8 = _d.L(flag2 ? 26 : 22);
				SaiyanHud.Draw(renderer, gameTexture, rectangle.X + (rectangle.Width - num8) / 2, rectangle.Y + (rectangle.Height - num8) / 2, num8, num8, A(TechniqueArt.White, (flag2 || flag) ? 1f : 0.55f));
			}
			TechniqueArt.Write(renderer, TechniqueArt.Label, Pt(12f), KeyOf(num7), rectangle.X + _d.L(3f), rectangle.Y + _d.L(1f) - _d.L(3f), A(flag2 ? TechniqueArt.White : TechniqueArt.Dim));
			DessinerCibleDeLaBarre(renderer, rectangle, num7, cible, color, Timing.Global.MillisecondsUtc);
			_casesDessinees[num7] = rectangle;
			int slot = entry.Slot;
			int hotbarIndex = num7;
			AddHit(rectangle, key, delegate
			{
				Globals.Me?.AddToHotbar(hotbarIndex, 1, slot);
			});
		}
		_casesVisibles = num;
		float num9 = num6 + num3 + 8f + 10f;
		foreach (string item in list)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), item, _d.X(1166f), _d.Y(num9), A(EnTransport ? TechniqueArt.Mix(color, TechniqueArt.White, 0.6f) : TechniqueArt.Soft));
			num9 += 20f;
		}
		static string KeyOf(int index)
		{
			return DropdownPanel.KeyOf((Control)(4097 + index)) ?? ((index + 1) % 10).ToString();
		}
	}

	private void DrawLineage(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Known entry, long now)
	{
		SpellDescriptor spell = entry.Spell;
		Color color = TechniqueArt.FamilyColor(entry.Family);
		List<SpellDescriptor> list = SpellMastery.Lineage(spell);
		int num = list.IndexOf(spell);
		SpellDescriptor spellDescriptor = SpellMastery.EvolvesInto(spell);
		SpellDescriptor next;
		bool flag = CanEvolve(entry, out next);
		if (!base.Details)
		{
			DrawLineageSimple(renderer, entry, list.Count, spellDescriptor, flag);
			return;
		}
		Panel(renderer, _d.R(510f, 566f, 640f, 304f));
		Heading(renderer, 528f, 580f, 604f, Strings.Techniques.Lineage, null);
		float num2 = 528f + (604f - (float)list.Count * 151f) / 2f;
		float num3 = 651.4f;
		if (list.Count > 1)
		{
			float num4 = num2 + 75.5f;
			float num5 = num2 + 151f * ((float)list.Count - 0.5f);
			float value = ((float)num + ((spellDescriptor != null && SpellMastery.HasMastery(spell)) ? ((float)entry.Rank / 5f) : 0f)) / (float)(list.Count - 1);
			float num6 = num4 + (num5 - num4) * Math.Clamp(value, 0f, 1f);
			TechniqueArt.Fill(renderer, _d.R(num4, num3 - 1.5f, num5 - num4, 3f), A(LineageOff));
			TechniqueArt.Fill(renderer, _d.R(num4, num3 - 4.5f, num6 - num4, 9f), A(color, 0.25f));
			TechniqueArt.Fill(renderer, _d.R(num4, num3 - 1.5f, num6 - num4, 3f), A(color));
		}
		for (int i = 0; i < list.Count; i++)
		{
			SpellDescriptor spellDescriptor2 = list[i];
			float num7 = num2 + 151f * ((float)i + 0.5f);
			bool flag2 = i < num;
			bool flag3 = i == num;
			bool flag4 = i == num + 1;
			bool flag5 = !flag2 && !flag3 && !flag4;
			bool flag6 = flag2 || flag3;
			float num8 = (flag5 ? 0.45f : 1f);
			Rectangle rect = _d.R(num7 - 40f, 611.4f, 80f, 80f);
			if (flag4 && flag)
			{
				float num9 = 0.6f + 0.4f * MathF.Sin((float)now / 300f);
				TechniqueArt.Glow(renderer, (float)rect.X + (float)rect.Width / 2f, (float)rect.Y + (float)rect.Height / 2f, _d.L(60f), A(TechniqueArt.Gold, 0.35f * num9));
			}
			TechniqueArt.Image(renderer, "tech_noeud.png", rect, A(new Color(255, 7, 10, 18), num8));
			TechniqueArt.Image(renderer, "tech_noeud_radial.png", rect, A(flag6 ? color : NodeOff, (flag6 ? 0.55f : 1f) * num8));
			Color color2 = ((!flag4) ? (flag6 ? color : NodeEdge) : (flag ? TechniqueArt.Gold : TechniqueArt.Alpha(TechniqueArt.Gold, 0.55f)));
			string texture = ((flag4 && !flag) ? "tech_noeud_tirets.png" : (flag3 ? "tech_noeud_bord3.png" : "tech_noeud_bord2.png"));
			TechniqueArt.Image(renderer, texture, rect, A(color2, num8));
			if (flag3)
			{
				TechniqueArt.Image(renderer, "tech_noeud_halo.png", _d.R(num7 - 44f, 607.4f, 88f, 88f), A(color, 0.35f));
			}
			string text = TechniqueArt.Roman(i + 1);
			Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(30f), text);
			Color color3 = (flag6 ? TechniqueArt.White : (flag4 ? TechniqueArt.Gold : NodeRoman));
			TechniqueArt.Italic(renderer, TechniqueArt.Display, Pt(30f), text, rect.X + (rect.Width - point.X) / 2, rect.Y + (rect.Height - point.Y) / 2, A(color3, num8));
			string text2 = ((i == 0) ? Strings.Techniques.Start.ToString() : Strings.Techniques.Awakening.ToString(list[i - 1].Mastery.EvolutionLevel));
			Color color4 = (flag2 ? TechniqueArt.Green : (flag3 ? TechniqueArt.Mix(color, TechniqueArt.White, 0.85f) : (flag4 ? TechniqueArt.Gold : TechniqueArt.Dim)));
			float num10 = 705.10004f;
			string text3 = text2.ToUpperInvariant();
			float spacing = _d.Em(0.14f, 12f);
			int num11 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(12f), text3, spacing);
			int num12 = _d.X(num7) - num11 / 2;
			if (flag2)
			{
				int num13 = _d.L(10f);
				num12 += (num13 + _d.L(4f)) / 2;
				TechniqueArt.Image(renderer, "tech_coche.png", new Rectangle(num12 - num13 - _d.L(4f), _d.Y(num10) - num13 / 2, num13, num13), A(TechniqueArt.Green, num8));
			}
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), text3, num12, _d.Y(num10), spacing, A(color4, num8));
			float num14 = num10 + 8.7f + 5f;
			foreach (string item in TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(20f), spellDescriptor2.Name, _d.L(139f), 2))
			{
				TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(20f), item, _d.X(num7), _d.Y(num14 + 10.5f), A(TechniqueArt.Text, num8));
				num14 += 21f;
			}
			string text4 = NodeSummary(spellDescriptor2);
			if (!string.IsNullOrEmpty(text4))
			{
				TechniqueArt.TextCenter(renderer, TechniqueArt.Body, Pt(14f), text4, _d.X(num7), _d.Y(num14 + 5f + 9f), A(TechniqueArt.Soft, num8));
			}
		}
		TechniqueArt.Fill(renderer, _d.R(528f, 800f, 604f, 1f), A(TechniqueArt.White, 0.06f));
		if (spellDescriptor == null)
		{
			string text5 = ((list.Count > 1) ? Strings.Techniques.LineageComplete.ToString() : Strings.Techniques.LineageNone.ToString());
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), text5, _d.X(528f), _d.Y(834f), A((list.Count > 1) ? TechniqueArt.Gold : TechniqueArt.Dim));
			return;
		}
		int num15 = DrawEvolveRow(renderer, entry, spellDescriptor, flag, 834f);
		int val = num15 - _d.L(14f) - _d.X(528f);
		List<string> list2 = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(14f), Strings.Techniques.LineageHelp, Math.Min(val, _d.L(250f)), 3);
		float num16 = 834f - (float)(list2.Count - 1) * 18f / 2f;
		foreach (string item2 in list2)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), item2, _d.X(528f), _d.Y(num16), A(TechniqueArt.Dim));
			num16 += 18f;
		}
	}

	private void DrawLineageSimple(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Known entry, int stages, SpellDescriptor? next, bool ready)
	{
		Color color = TechniqueArt.Text;
		string text;
		if (next == null)
		{
			text = ((stages > 1) ? Strings.Techniques.LineageCompleteSimple.ToString() : Strings.Techniques.LineageNone.ToString());
			color = ((stages > 1) ? TechniqueArt.Gold : TechniqueArt.Dim);
		}
		else
		{
			string text2 = NodeSummary(next);
			text = ((text2.Length > 0) ? Strings.Techniques.NextStage.ToString(next.Name, text2) : Strings.Techniques.NextStageShort.ToString(next.Name));
		}
		List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(16f), text, _d.L(604f), 2);
		float num = (float)list.Count * 23.2f;
		float height = 41.4f + num + (float)((next != null) ? 56 : 0) + 14f;
		Panel(renderer, _d.R(510f, 566f, 640f, height));
		Heading(renderer, 528f, 580f, 604f, Strings.Techniques.Lineage, null);
		float num2 = 619f;
		foreach (string item in list)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(16f), item, _d.X(528f), _d.Y(num2), A(color));
			num2 += 23.2f;
		}
		if (next != null)
		{
			DrawEvolveRow(renderer, entry, next, ready, 607.4f + num + 12f + 22f);
		}
	}

	private int DrawEvolveRow(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Known entry, SpellDescriptor next, bool ready, float footerCenter)
	{
		SpellDescriptor spell = entry.Spell;
		string text = Strings.Techniques.Evolve.ToString().ToUpperInvariant();
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(20f), text);
		int num = point.X + _d.L(52f);
		int num2 = _d.L(44f);
		Rectangle rect = new Rectangle(_d.X(1132f) - num, _d.Y(footerCenter) - num2 / 2, num, num2);
		bool flag = ready && IsHot("evoluer");
		if (ready)
		{
			TechniqueArt.GlowRect(renderer, new Rectangle(rect.X - _d.L(flag ? 26 : 16), rect.Y - _d.L(flag ? 26 : 16), rect.Width + _d.L(flag ? 52 : 32), rect.Height + _d.L(flag ? 52 : 32)), A(new Color(255, 255, 170, 60), flag ? 0.8f : 0.45f), additive: false);
		}
		string texture = ((!ready) ? "tech_bouton_eteint.png" : (flag ? "tech_bouton_survol.png" : "tech_bouton.png"));
		int num3 = (flag ? 1 : 0);
		TechniqueArt.Patch(renderer, texture, new Rectangle(rect.X, rect.Y - num3, rect.Width, rect.Height), A(TechniqueArt.White), 14);
		TechniqueArt.Italic(renderer, TechniqueArt.Display, Pt(20f), text, rect.X + (rect.Width - point.X) / 2, rect.Y - num3 + (rect.Height - point.Y) / 2, A(ready ? new Color(255, 20, 10, 2) : TechniqueArt.Dim));
		if (ready)
		{
			AddHit(rect, "evoluer", delegate
			{
				StartEvolution(entry, next);
			});
		}
		int num4 = rect.X - _d.L(14f);
		if (ready)
		{
			string text2 = Strings.Keys.FormatKeyName(Keys.None, Keys.Return);
			int num5 = KeyChipWidth(renderer, text2);
			KeyChip(renderer, text2, rect.X - _d.L(10f) - num5, _d.Y(footerCenter), flag);
			num4 = rect.X - _d.L(10f) - num5 - _d.L(14f);
		}
		bool item = SpellMastery.HasMastery(spell) && entry.Rank >= 5;
		bool item2 = (Globals.Me?.Level ?? 0) >= spell.Mastery.EvolutionLevel;
		(bool, string)[] array = new(bool, string)[2]
		{
			(item, Strings.Techniques.ConditionRank.ToString()),
			(item2, Strings.Techniques.ConditionLevel.ToString(spell.Mastery.EvolutionLevel))
		};
		int num6 = array.Max(((bool Ok, string Text) condition) => TechniqueArt.Measure(renderer, TechniqueArt.Body, Pt(14f), condition.Text).X) + _d.L(16f);
		int num7 = num4 - num6;
		for (int num8 = 0; num8 < array.Length; num8++)
		{
			(bool, string) tuple = array[num8];
			bool item3 = tuple.Item1;
			string item4 = tuple.Item2;
			int num9 = _d.Y(footerCenter + (float)((num8 == 0) ? (-11) : 11));
			int num10 = _d.L(10f);
			Color color = (item3 ? TechniqueArt.Green : TechniqueArt.Red);
			TechniqueArt.Image(renderer, item3 ? "tech_coche.png" : "tech_croix.png", new Rectangle(num7, num9 - num10 / 2, num10, num10), A(color));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), item4, num7 + num10 + _d.L(6f), num9, A(color));
		}
		return num7;
	}

	private static string NodeSummary(SpellDescriptor stage)
	{
		DamageSplit? damageSplit = DamageOf(stage, 1);
		if (damageSplit.HasValue)
		{
			DamageSplit valueOrDefault = damageSplit.GetValueOrDefault();
			return Strings.Techniques.NodeDamage.ToString((valueOrDefault.Hits > 1) ? $"{valueOrDefault.Total} ×{valueOrDefault.Hits}" : valueOrDefault.Total.ToString());
		}
		if (IsTransformation(stage))
		{
			return Strings.Techniques.NodeLeak.ToString(TechniqueArt.Number((double)Options.Instance.Transformation.KiDrainPercentPerSecond * SpellMastery.LeakFactor(stage, 1)));
		}
		if (stage.Combat.Duration > 0)
		{
			return Strings.Techniques.NodeDuration.ToString(TechniqueArt.Number((double)stage.Combat.Duration / 1000.0));
		}
		if (stage.VitalCost[1] <= 0)
		{
			return string.Empty;
		}
		return Strings.Techniques.NodeCost.ToString(stage.VitalCost[1]);
	}

	private int SX(float x)
	{
		return _sceneX + (int)MathF.Round(x * (float)_zoom);
	}

	private int SY(float y)
	{
		return _sceneY + (int)MathF.Round(y * (float)_zoom);
	}

	private void SceneGlow(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float x, float y, float radius, Color color, float alpha)
	{
		if (!(radius <= 0f) && !(alpha <= 0f))
		{
			float num = radius * (float)_zoom;
			Rectangle rect = new Rectangle((int)MathF.Round((float)SX(x) - num), (int)MathF.Round((float)SY(y) - num), (int)MathF.Round(num * 2f), (int)MathF.Round(num * 2f));
			TechniqueArt.GlowRect(renderer, rect, A(color, alpha), additive: true, "tech_lueur_couleur.png");
			TechniqueArt.GlowRect(renderer, rect, A(TechniqueArt.White, alpha), additive: true, "tech_lueur_coeur.png");
		}
	}

	private void SceneFill(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float x, float y, float width, float height, Color color)
	{
		TechniqueArt.Fill(renderer, new Rectangle(SX(x), SY(y), (int)MathF.Round(width * (float)_zoom), (int)MathF.Round(height * (float)_zoom)), color);
	}

	private void DrawPreview(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Known entry, long now)
	{
		Color color = TechniqueArt.FamilyColor(entry.Family);
		Rectangle rectangle = _d.R(510f, 104f, 640f, 300f);
		TechniqueArt.Fill(renderer, rectangle, A(new Color(255, 5, 7, 12)));
		_zoom = Math.Max(1, (int)MathF.Round(2f * _d.S));
		float num = (float)rectangle.Width / (float)_zoom;
		float num2 = (float)rectangle.Height / (float)_zoom;
		long t = (now - _previewStart) % 3200;
		_shake = 0f;
		Rectangle previous = TechniqueArt.BeginClip(renderer, rectangle);
		float num3 = num / 320f;
		int num4 = 0;
		int num5 = 0;
		float num6 = ShakeAt(entry, t);
		if (num6 > 0f)
		{
			num4 = (int)MathF.Round(MathF.Sin((float)now * 0.091f) * num6) * _zoom;
			num5 = (int)MathF.Round(MathF.Cos((float)now * 0.137f) * num6 * 0.5f) * _zoom;
		}
		_sceneX = rectangle.X + num4;
		_sceneY = rectangle.Y + num5;
		DrawGround(renderer, rectangle, num, num2);
		TechniqueArt.Fill(renderer, rectangle, A(new Color(89, 5, 8, 16)));
		float num7 = 112f * num2 / 150f;
		float num8 = 70f * num3;
		float num9 = 262f * num3;
		string text = EffetCode(entry.Spell);
		if (text != null)
		{
			num9 = MannequinDe(text, t, num8);
		}
		DrawDummy(renderer, num9, num7);
		SceneEllipse(renderer, num8, num7 + 1f, 11f, 3.5f, 0.35f);
		if (text != null)
		{
			PreviewTechniqueCodee(renderer, text, t, now, num8, num9, num7, color);
		}
		else
		{
			switch (entry.Family)
			{
			case TechniqueFamily.KiBlast:
				PreviewKiBlast(renderer, entry, t, now, num8, num9, num7, color);
				break;
			case TechniqueFamily.Beam:
			case TechniqueFamily.Ultimate:
				PreviewBeam(renderer, entry, t, now, num8, num9, num7, color);
				break;
			case TechniqueFamily.Melee:
				PreviewMelee(renderer, t, num8, num9, num7, color);
				break;
			case TechniqueFamily.Transformation:
				PreviewTransformation(renderer, entry, t, now, num8, num7, rectangle);
				break;
			default:
				PreviewSupport(renderer, t, num8, num7, color, rectangle);
				break;
			}
		}
		TechniqueArt.EndClip(renderer, previous);
		TechniqueArt.Fill(renderer, _d.R(510f, 104f, 40f, 2f), A(color));
		TechniqueArt.Fill(renderer, _d.R(510f, 104f, 2f, 40f), A(color));
		TechniqueArt.Fill(renderer, _d.R(1110f, 402f, 40f, 2f), A(color));
		TechniqueArt.Fill(renderer, _d.R(1148f, 364f, 2f, 40f), A(color));
		float num10 = 0.5f - 0.5f * MathF.Cos((float)now / 1200f * ((float)Math.PI * 2f));
		Rectangle rect = _d.R(532f, 120.5f, 7f, 7f);
		TechniqueArt.Glow(renderer, (float)rect.X + (float)rect.Width / 2f, (float)rect.Y + (float)rect.Height / 2f, _d.L(8f + 6f * num10), A(new Color(255, 255, 59, 59), 0.6f));
		TechniqueArt.Image(renderer, "tech_point.png", rect, A(new Color(255, 255, 59, 59)));
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), Strings.Techniques.Preview.ToString().ToUpperInvariant(), _d.X(547f), _d.Y(124f), _d.Em(0.24f, 12f), A(TechniqueArt.White, 0.55f));
	}

	private static float ShakeAt(Known entry, long t)
	{
		switch (entry.Family)
		{
		case TechniqueFamily.Beam:
		case TechniqueFamily.Ultimate:
		{
			long num = BeamChargeMs(entry);
			if (t >= num)
			{
				return (t < num + 1100) ? 2 : 0;
			}
			return 1.2f * (float)t / (float)num;
		}
		case TechniqueFamily.Transformation:
			return (t > 650 && t < 900) ? 3 : 0;
		case TechniqueFamily.Melee:
			if (t <= 860 || t >= 1120)
			{
				return 0f;
			}
			return 2f * (1f - (float)(t - 860) / 260f);
		default:
			return 0f;
		}
	}

	private static long BeamChargeMs(Known entry)
	{
		return Math.Clamp(SpellMastery.CastDuration(entry.Spell, entry.Rank), 900, 1400);
	}

	private void DrawGround(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle area, float sceneWidth, float sceneHeight)
	{
		Player me = Globals.Me;
		int tileWidth = Options.Instance.Map.TileWidth;
		int tileHeight = Options.Instance.Map.TileHeight;
		if (me != null && MapInstance.TryGet(me.MapId, out MapInstance instance))
		{
			IGameTexture gameTexture = LocalMap.MapTexture(instance, tileWidth);
			if (gameTexture != null && gameTexture.Width > 0 && gameTexture.Height > 0)
			{
				float num = Math.Min(sceneWidth, gameTexture.Width);
				float num2 = Math.Min(sceneHeight, gameTexture.Height);
				float num3 = (float)(me.X * tileWidth) + (float)tileWidth / 2f;
				float num4 = (float)(me.Y * tileHeight) + (float)tileHeight / 2f;
				float num5 = Math.Clamp(num3 - num / 2f, 0f, (float)gameTexture.Width - num);
				float num6 = Math.Clamp(num4 - num2 * 0.6f, 0f, (float)gameTexture.Height - num2);
				SaiyanHud.Draw(renderer, gameTexture, _sceneX, _sceneY, (int)MathF.Round(num * (float)_zoom), (int)MathF.Round(num2 * (float)_zoom), A(TechniqueArt.White), num5 / (float)gameTexture.Width, num6 / (float)gameTexture.Height, (num5 + num) / (float)gameTexture.Width, (num6 + num2) / (float)gameTexture.Height);
				return;
			}
		}
		TechniqueArt.Image(renderer, "tech_sol.png", area, A(TechniqueArt.White));
	}

	private void SceneEllipse(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float centerX, float centerY, float radiusX, float radiusY, float alpha)
	{
		TechniqueArt.Image(renderer, "tech_ombre.png", new Rectangle(SX(centerX - radiusX), SY(centerY - radiusY), (int)MathF.Round(radiusX * 2f * (float)_zoom), (int)MathF.Round(radiusY * 2f * (float)_zoom)), A(TechniqueArt.Black, alpha));
	}

	private void DrawDummy(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float x, float floor)
	{
		SceneEllipse(renderer, x, floor + 1f, 10f, 3f, 0.35f);
		SceneFill(renderer, x - 2f, floor - 30f, 4f, 30f, A(new Color(255, 107, 74, 44)));
		SceneFill(renderer, x - 9f, floor - 40f, 18f, 16f, A(new Color(255, 201, 163, 106)));
		SceneFill(renderer, x - 11f, floor - 26f, 22f, 4f, A(new Color(255, 138, 106, 60)));
	}

	private void DrawBody(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string? pose, int row, int frame, float x, float floor, Color tint, string? spriteOverride = null)
	{
		string text = spriteOverride ?? Globals.Me?.Sprite;
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Entity, text);
		if (texture == null || texture.Width <= 0 || texture.Height <= 0)
		{
			return;
		}
		IGameTexture gameTexture = texture;
		SpriteSheet spriteSheet = SpriteSheets.ForEntity(text);
		int num;
		int num2;
		int num3;
		int num4;
		if (spriteSheet != null)
		{
			(num, num2, num3, num4) = spriteSheet.PreviewRect();
		}
		else
		{
			num3 = texture.Width / Math.Max(1, Options.Instance.Sprites.NormalFrames);
			num4 = texture.Height / Math.Max(1, Options.Instance.Sprites.Directions);
			IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Entity, Path.GetFileNameWithoutExtension(text) + "_" + (pose ?? "idle") + ".png");
			if (texture2 != null && texture2.Width > 0 && texture2.Height > 0)
			{
				gameTexture = texture2;
				num4 = texture2.Height / Math.Max(1, Options.Instance.Sprites.Directions);
			}
			int num5 = Math.Max(1, gameTexture.Width / Math.Max(1, num3));
			num = frame % num5 * num3;
			num2 = Math.Clamp(row, 0, Options.Instance.Sprites.Directions - 1) * num4;
		}
		SaiyanHud.Draw(renderer, gameTexture, SX(x - (float)num3 / 2f), SY(floor - (float)(num4 - 2)), num3 * _zoom, num4 * _zoom, tint, ((float)num + 0.01f) / (float)gameTexture.Width, ((float)num2 + 0.01f) / (float)gameTexture.Height, ((float)(num + num3) - 0.01f) / (float)gameTexture.Width, ((float)(num2 + num4) - 0.01f) / (float)gameTexture.Height);
	}

	private void PreviewKiBlast(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Known entry, long t, long now, float player, float target, float floor, Color color)
	{
		DrawBody(renderer, "spell", 2, 0, player, floor, A(TechniqueArt.White));
		int num = HitsOf(entry.Spell);
		float num2 = player + 14f;
		float num3 = floor - 15f;
		if (t > 250 && t < 500)
		{
			SceneGlow(renderer, num2, num3, 5f + 3f * MathF.Sin((float)now / 50f), color, 0.7f);
		}
		for (int i = 0; i < num; i++)
		{
			int num4 = 500 + i * ((num > 3) ? 170 : 260);
			long num5 = t - num4;
			if (num5 < 0)
			{
				SceneGlow(renderer, num2, num3, 5f + 3f * MathF.Sin((float)now / 50f), color, 0.7f);
				continue;
			}
			float num6 = num2 + (float)num5 * 0.32f;
			float y = num3 - (float)(i * 2) + (float)i;
			if (num6 < target)
			{
				for (int j = 0; j < 6; j++)
				{
					SceneGlow(renderer, num6 - (float)(j * 5), y, 7 - j, color, 0.5f - (float)j * 0.07f);
				}
				SceneGlow(renderer, num6, y, 6f, color, 1f);
				continue;
			}
			float num7 = (float)t - ((float)num4 + (target - num2) / 0.32f);
			if (num7 >= 0f && num7 < 260f)
			{
				float num8 = num7 / 260f;
				SceneGlow(renderer, target - 6f, floor - 18f, 22f * (0.6f + num8), color, 0.9f * (1f - num8));
				_shake = Math.Max(_shake, 2f * (1f - num8));
			}
		}
	}

	private void PreviewBeam(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Known entry, long t, long now, float player, float target, float floor, Color color)
	{
		DrawBody(renderer, "spell", 2, 0, player, floor, A(TechniqueArt.White));
		long num = BeamChargeMs(entry);
		int num2 = Math.Max(0, SpellMastery.Lineage(entry.Spell).IndexOf(entry.Spell));
		float num3 = player + 8f;
		float num4 = floor - 15f;
		if (t < num)
		{
			float num5 = (float)t / (float)num;
			SceneGlow(renderer, num3, num4, 6f + 16f * num5 + 2f * MathF.Sin((float)now / 30f), color, 0.5f + 0.5f * num5);
			for (int i = 0; i < 6; i++)
			{
				float x = (float)now / 200f + (float)i;
				float num6 = 26f * (1f - ((float)now / 400f + (float)i / 6f) % 1f);
				SceneFill(renderer, num3 + MathF.Cos(x) * num6, num4 + MathF.Sin(x) * num6, 2f, 2f, A(TechniqueArt.White, 0.8f));
			}
			return;
		}
		long num7 = t - num;
		if (num7 < 1100)
		{
			float num8 = Math.Min(target + 30f, player + 18f + (float)num7 * 0.7f);
			float num9 = (float)((entry.Family == TechniqueFamily.Ultimate) ? 17 : (13 + num2 * 2)) * ((num7 > 800) ? (1f - (float)(num7 - 800) / 300f) : 1f);
			for (int j = 0; j < 3; j++)
			{
				float num10 = num9 * (1.6f - (float)j * 0.5f) + MathF.Sin((float)now / 25f + (float)j) * 1.5f;
				Color color2 = ((j == 2) ? TechniqueArt.White : color);
				float alpha = ((j == 2) ? 0.95f : (0.35f + (float)j * 0.2f));
				Rectangle rect = new Rectangle(SX(player + 16f), SY(num4 - num10), (int)MathF.Round((num8 - player - 16f) * (float)_zoom), (int)MathF.Round(num10 * 2f * (float)_zoom));
				TechniqueArt.GlowRect(renderer, rect, A(color2, alpha), additive: true, "tech_faisceau.png");
			}
			SceneGlow(renderer, player + 14f, num4, num9 * 1.8f, color, 0.9f);
			SceneGlow(renderer, num8, num4, num9 * 2.2f, color, 0.9f);
			if (num8 >= target - 4f)
			{
				SceneGlow(renderer, target, floor - 17f, 30f + 6f * MathF.Sin((float)now / 40f), color, 0.7f);
			}
		}
	}

	private void PreviewMelee(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long t, float player, float target, float floor, Color color)
	{
		long num = t - 600;
		float num2 = target - 30f;
		float num3 = ((num < 0) ? player : ((num < 260) ? (player + (num2 - player) * ((float)num / 260f)) : ((num < 1100) ? num2 : (num2 - (num2 - player) * Math.Min(1f, (float)(num - 1100) / 500f)))));
		if (num > 0 && num < 260)
		{
			for (int i = 1; i <= 4; i++)
			{
				DrawBody(renderer, null, 2, 0, num3 - (float)(i * 18), floor, A(TechniqueArt.White, 0.25f - (float)i * 0.05f));
			}
		}
		SceneEllipse(renderer, num3, floor + 1f, 11f, 3.5f, 0.35f);
		DrawBody(renderer, null, 2, 0, num3, floor, A(TechniqueArt.White));
		long num4 = t - 860;
		if (num4 >= 0 && num4 < 260)
		{
			float num5 = (float)num4 / 260f;
			SceneGlow(renderer, target - 12f, floor - 24f, 26f * (0.6f + num5), color, 0.9f * (1f - num5));
		}
	}

	private void PreviewSupport(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long t, float player, float floor, Color color, Rectangle area)
	{
		DrawBody(renderer, "spell", 0, 0, player, floor, A(TechniqueArt.White));
		long num = t - 900;
		if ((num <= 0 || num >= 900) ? true : false)
		{
			return;
		}
		float k = (float)num / 900f;
		TechniqueArt.Fill(renderer, area, A(new Color(255, 255, 250, 220), 0.95f * (1f - k) * (1f - k)));
		IntersectRenderer rotating = renderer as IntersectRenderer;
		if (rotating == null)
		{
			return;
		}
		IGameTexture ray = TechniqueArt.Tex("tech_faisceau.png");
		if (ray == null)
		{
			return;
		}
		float length = 180f * (0.3f + k) * (float)_zoom;
		int thickness = 4 * _zoom;
		int centerX = SX(player);
		int centerY = SY(floor - 34f);
		TechniqueArt.Additive(renderer, delegate
		{
			rotating.DrawColor = A(new Color(255, 255, 240, 170), 0.5f * (1f - k));
			for (int i = 0; i < 12; i++)
			{
				float num2 = (float)i * 30f;
				float x = num2 * (float)Math.PI / 180f;
				float num3 = (float)centerX + MathF.Cos(x) * length / 2f;
				float num4 = (float)centerY + MathF.Sin(x) * length / 2f;
				rotating.DrawTextureRotated(ray, num3 - length / 2f, num4 - (float)(thickness / 2), length, thickness, num2);
			}
		});
	}

	private void PreviewTransformation(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Known entry, long t, long now, float player, float floor, Rectangle area)
	{
		Color aura = AuraColor(entry.Spell);
		bool flag = (entry.Spell.Combat.TransformSprite + " " + entry.Spell.Name).Contains("kaioken", StringComparison.OrdinalIgnoreCase);
		float rise = Math.Min(1f, (float)t / 700f);
		DrawFlames(renderer, player, floor + 1f, now, aura, 0.5f + 0.7f * rise);
		SceneGlow(renderer, player, floor - 22f, 30f * rise + 4f * MathF.Sin((float)now / 60f), aura, 0.35f * rise);
		string spriteOverride = ((t > 650) ? TransformedSprite(entry.Spell) : null);
		DrawBody(renderer, null, 0, 0, player, floor, A(TechniqueArt.White), spriteOverride);
		if (flag)
		{
			TechniqueArt.Additive(renderer, delegate
			{
				DrawBody(renderer, null, 0, 0, player, floor, A(aura, 0.35f * rise));
			});
		}
		if (t > 650 && t < 900)
		{
			TechniqueArt.Fill(renderer, area, A(new Color(255, 255, 248, 220), 0.7f * (1f - (float)(t - 650) / 250f)));
		}
	}

	private void DrawFlames(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float centerX, float floor, long now, Color aura, float force)
	{
		for (int i = 0; i < 9; i++)
		{
			float num = (float)i / 8f * 2f - 1f;
			float num2 = (34f + 18f * MathF.Sin((float)now / 90f + (float)i * 1.7f) + 10f * MathF.Sin((float)now / 37f + (float)i)) * force * (1f - MathF.Abs(num) * 0.45f);
			float num3 = 6f - MathF.Abs(num) * 2f;
			float num4 = centerX + num * 13f;
			Rectangle rect = new Rectangle(SX(num4 - num3), SY(floor - num2), (int)MathF.Round(num3 * 2f * (float)_zoom), (int)MathF.Round(num2 * (float)_zoom));
			TechniqueArt.GlowRect(renderer, rect, A(aura, 0.75f), additive: true, "tech_flamme.png");
		}
	}

	private static Color AuraColor(SpellDescriptor spell)
	{
		string text = (spell.Combat.TransformSprite + " " + spell.Name).ToLowerInvariant();
		if (text.Contains("kaioken"))
		{
			return new Color(255, 255, 50, 40);
		}
		if (text.Contains("blue") || text.Contains("ssb"))
		{
			return new Color(255, 90, 210, 255);
		}
		if (text.Contains("god") || text.Contains("ssg") || text.Contains("rose"))
		{
			return new Color(255, 255, 96, 140);
		}
		return new Color(255, 255, 196, 60);
	}

	private static string? TransformedSprite(SpellDescriptor spell)
	{
		string text = Globals.Me?.Sprite;
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(spell.Combat.TransformSprite))
		{
			return null;
		}
		string transformed = CorpsDuCreateur.Forme(text, spell.Combat.TransformSprite) ?? spell.Combat.TransformSprite;
		string text2 = Transformation.TextureNameFor(text, transformed);
		if (string.IsNullOrWhiteSpace(text2) || !(text2 != text) || Globals.ContentManager.GetTexture(TextureType.Entity, text2) == null)
		{
			return null;
		}
		return text2;
	}

	private static string? EffetCode(SpellDescriptor spell)
	{
		if (spell.SpellType != SpellType.CombatSpell || !EffetsParNom.TryGetValue(spell.Name ?? string.Empty, out string value))
		{
			return null;
		}
		return value;
	}

	private static float Adouci(float k)
	{
		return 1f - (1f - Math.Clamp(k, 0f, 1f)) * (1f - Math.Clamp(k, 0f, 1f));
	}

	private static float Recul(long t, long coup, float pixels)
	{
		if (t < coup)
		{
			return 0f;
		}
		float num = Adouci((float)(t - coup) / 220f);
		float num2 = ((t > 2500) ? Adouci((float)(t - 2500) / 500f) : 0f);
		return pixels * num * (1f - num2);
	}

	private static float MannequinDe(string effet, long t, float player)
	{
		return effet switch
		{
			"kiai" => player + 36f + Recul(t, 760L, 44f), 
			"onde" => player + 62f + Recul(t, 980L, 34f), 
			"riposte" => player + 34f + ((t >= 1150 && t < 1320) ? (-8f * MathF.Sin((float)(t - 1150) / 170f * (float)Math.PI)) : 0f) + Recul(t, 1320L, 30f), 
			_ => player + 48f + Recul(t, 1120L, 52f), 
		};
	}

	private void SceneAnneau(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float x, float y, float rayon, Color color, float alpha)
	{
		if (!(rayon <= 0f) && !(alpha <= 0f))
		{
			TechniqueArt.Additive(renderer, delegate
			{
				TechniqueArt.Image(renderer, "lieu_anneau.png", new Rectangle(SX(x - rayon), SY(y - rayon * 0.45f), (int)MathF.Round(rayon * 2f * (float)_zoom), (int)MathF.Round(rayon * 0.9f * (float)_zoom)), A(color, alpha));
			});
		}
	}

	private void SceneSonne(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float x, float floor, long now, float alpha)
	{
		for (int i = 0; i < 3; i++)
		{
			float x2 = (float)now / 140f + (float)i * ((float)Math.PI * 2f) / 3f;
			SceneGlow(renderer, x + MathF.Cos(x2) * 9f, floor - 46f + MathF.Sin(x2) * 3f, 2.2f, TechniqueArt.Gold, alpha);
		}
	}

	private void PreviewTechniqueCodee(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string effet, long t, long now, float player, float mannequin, float floor, Color color)
	{
		switch (effet)
		{
		case "kiai":
		{
			DrawBody(renderer, "spell", 0, 0, player, floor, A(TechniqueArt.White));
			long num = t - 700;
			if (num >= 0 && num < 480)
			{
				float num2 = (float)num / 480f;
				SceneAnneau(renderer, player, floor, 8f + 56f * Adouci(num2), TechniqueArt.Cyan, 0.95f * (1f - num2));
				SceneAnneau(renderer, player, floor, 4f + 40f * Adouci(num2), TechniqueArt.White, 0.7f * (1f - num2));
				SceneGlow(renderer, player, floor - 16f, 16f * (1f - num2), TechniqueArt.Cyan, 0.8f * (1f - num2));
			}
			if (t >= 900 && t < 1700)
			{
				SceneSonne(renderer, mannequin, floor, now, 0.9f);
			}
			return;
		}
		case "onde":
		{
			DrawBody(renderer, null, 2, 0, player, floor, A(TechniqueArt.White));
			long num6 = t - 700;
			if (num6 >= 0 && num6 < 200)
			{
				SceneGlow(renderer, player + 12f, floor - 4f, 9f * (1f - (float)num6 / 200f) + 3f, color, 0.9f);
			}
			if (num6 >= 0 && t < 2500)
			{
				float num7 = 76f * Adouci((float)num6 / 300f);
				float num8 = ((t > 2000) ? (1f - (float)(t - 2000) / 500f) : 1f);
				SceneFill(renderer, player + 14f, floor + 1f, num7, 2f, A(TechniqueArt.Black, 0.75f * num8));
				SceneFill(renderer, player + 14f, floor, num7, 1f, A(color, 0.6f * num8));
				if (num6 < 300)
				{
					SceneGlow(renderer, player + 14f + num7, floor - 2f, 7f, color, 0.9f);
				}
			}
			if (t >= 1000 && t < 1900)
			{
				SceneSonne(renderer, mannequin, floor, now, 0.9f);
			}
			return;
		}
		case "riposte":
		{
			DrawBody(renderer, null, 2, 0, player, floor, A(TechniqueArt.White));
			if (t >= 500 && t < 1500)
			{
				float num3 = 0.6f + 0.4f * MathF.Sin((float)now / 70f);
				SceneAnneau(renderer, player, floor, 16f, TechniqueArt.Cyan, 0.8f * num3);
				SceneGlow(renderer, player + 10f, floor - 16f, 12f, TechniqueArt.Cyan, 0.45f * num3);
			}
			long num4 = t - 1320;
			if (num4 >= 0 && num4 < 300)
			{
				float num5 = (float)num4 / 300f;
				SceneGlow(renderer, player + 20f, floor - 17f, 20f * (0.5f + num5), TechniqueArt.Gold, 0.95f * (1f - num5));
				SceneAnneau(renderer, player + 20f, floor - 15f, 6f + 22f * num5, TechniqueArt.Gold, 0.8f * (1f - num5));
			}
			return;
		}
		}
		DrawBody(renderer, "spell", 0, 0, player, floor, A(TechniqueArt.White));
		if (t >= 500 && t < 1120)
		{
			float num9 = (float)(t - 500) / 620f;
			SceneGlow(renderer, player, floor - 16f, 6f + 16f * num9, TechniqueArt.Ki, 0.5f + 0.4f * num9);
		}
		long num10 = t - 1120;
		if (num10 >= 0 && num10 < 560)
		{
			float num11 = (float)num10 / 560f;
			float num12 = 10f + 70f * Adouci(num11);
			SceneGlow(renderer, player, floor - 15f, num12 * 0.9f, TechniqueArt.Ki, 0.9f * (1f - num11));
			SceneAnneau(renderer, player, floor, num12, TechniqueArt.Ki, 1f - num11);
			SceneAnneau(renderer, player, floor, num12 * 0.72f, TechniqueArt.Gold, 0.8f * (1f - num11));
			for (int i = 0; i < 10; i++)
			{
				float x = (float)i * ((float)Math.PI * 2f) / 10f + 0.3f;
				SceneGlow(renderer, player + MathF.Cos(x) * num12, floor + MathF.Sin(x) * num12 * 0.45f - 4f, 5f * (1f - num11) + 2f, TechniqueArt.Ki, 0.9f * (1f - num11));
			}
		}
		if (t >= 1300 && t < 2000)
		{
			SceneSonne(renderer, mannequin, floor, now, 0.9f);
		}
	}

	protected override void OnMouseDown(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseDown(mouseButton, mousePosition, userAction);
		_appuiSlot = -1;
		if (mouseButton == MouseButton.Left && _cinematic == null && !EnTransport)
		{
			Point point = CanvasPosToLocal(mousePosition);
			string text = HitKeyAt(point);
			if (text != null && text.StartsWith("carte:", StringComparison.Ordinal) && int.TryParse(text.AsSpan(6), out var result))
			{
				_appuiSlot = result;
			}
			else if (text == "embleme")
			{
				_appuiSlot = _slot;
			}
			_appuiA = point;
		}
	}

	private void MajDuTransport()
	{
		Point point = LocalMouse();
		if (!Globals.InputManager.IsMouseButtonDown(MouseButton.Left))
		{
			if (EnTransport)
			{
				Deposer(point);
			}
			_appuiSlot = -1;
			return;
		}
		if (_appuiSlot >= 0 && !EnTransport && Math.Abs(point.X - _appuiA.X) + Math.Abs(point.Y - _appuiA.Y) >= 6)
		{
			_porteSlot = _appuiSlot;
			_appuiSlot = -1;
			_porteDepuis = Maintenant;
			_retourSlot = -1;
			SelectSlot(_porteSlot);
			Audio.AddGameSound("menu_hover.wav", loop: false);
		}
		if (EnTransport)
		{
			_porteSouris = point;
		}
	}

	private void Deposer(Point souris)
	{
		int porteSlot = _porteSlot;
		_porteSlot = -1;
		_appuiSlot = -1;
		_depotA = Maintenant;
		if (porteSlot >= 0)
		{
			int num = CaseSous(souris);
			if (num >= 0)
			{
				Globals.Me?.AddToHotbar(num, 1, porteSlot);
				_poseeCase = num;
				_poseeDepuis = Maintenant;
				Audio.AddGameSound("menu_click.wav", loop: false);
			}
			else
			{
				_retourSlot = porteSlot;
				_retourDe = souris;
				_retourVers = (_cartesDessinees.TryGetValue(porteSlot, out var value) ? new Point(value.X + _d.L(41f), value.Y + value.Height / 2) : souris);
				_retourDepuis = Maintenant;
			}
		}
	}

	private void OublierLeTransport()
	{
		_porteSlot = -1;
		_appuiSlot = -1;
		_retourSlot = -1;
	}

	private int CaseSous(Point souris)
	{
		int num = Math.Max(2, _d.L(3f));
		for (int i = 0; i < _casesVisibles; i++)
		{
			Rectangle rectangle = _casesDessinees[i];
			if (souris.X >= rectangle.X - num && souris.X < rectangle.Right + num && souris.Y >= rectangle.Y - num * 2 && souris.Y < rectangle.Bottom + num * 2)
			{
				return i;
			}
		}
		return -1;
	}

	private string PhraseDuTransport(Guid[] contenus, int cible, Guid transporte, Func<int, string> toucheDe)
	{
		if (cible < 0)
		{
			return Strings.Techniques.DragRelease.ToString();
		}
		Guid guid = contenus[cible];
		if (guid == Guid.Empty)
		{
			return Strings.Techniques.DragPlace.ToString(toucheDe(cible));
		}
		if (guid == transporte)
		{
			return Strings.Techniques.DragAlready.ToString(toucheDe(cible));
		}
		SpellDescriptor databaseObject;
		ItemDescriptor databaseObject2;
		string text = (DatabaseObject<SpellDescriptor>.TryGet(guid, out databaseObject) ? databaseObject.Name : (DatabaseObject<ItemDescriptor>.TryGet(guid, out databaseObject2) ? databaseObject2.Name : string.Empty));
		return Strings.Techniques.DragReplace.ToString(text, toucheDe(cible));
	}

	private static SpellDescriptor? SortDuLivre(int slot)
	{
		Spell[] array = Globals.Me?.Spells;
		if (array != null && slot >= 0 && slot < array.Length)
		{
			Spell spell = array[slot];
			if (spell != null && !(spell.Id == Guid.Empty))
			{
				if (!DatabaseObject<SpellDescriptor>.TryGet(spell.Id, out var databaseObject))
				{
					return null;
				}
				return databaseObject;
			}
		}
		return null;
	}

	private void DrawTransport(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		if (EnTransport)
		{
			SpellDescriptor spellDescriptor = SortDuLivre(_porteSlot);
			if (spellDescriptor != null)
			{
				float num = Math.Clamp((float)(now - _porteDepuis) / 120f, 0f, 1f);
				float num2 = 0.85f + 0.15f * (1f - (1f - num) * (1f - num));
				bool flag = CaseSous(_porteSouris) >= 0;
				DessinerEnMain(renderer, spellDescriptor, _porteSouris, flag ? (num2 * 0.9f) : num2, 1f, avecNom: true);
				return;
			}
		}
		if (_retourSlot >= 0)
		{
			SpellDescriptor spellDescriptor2 = SortDuLivre(_retourSlot);
			if (spellDescriptor2 != null)
			{
				float num3 = Math.Clamp((float)(now - _retourDepuis) / 200f, 0f, 1f);
				if (num3 >= 1f)
				{
					_retourSlot = -1;
					return;
				}
				float num4 = 1f - (1f - num3) * (1f - num3);
				Point centre = new Point((int)MathF.Round((float)_retourDe.X + (float)(_retourVers.X - _retourDe.X) * num4), (int)MathF.Round((float)_retourDe.Y + (float)(_retourVers.Y - _retourDe.Y) * num4));
				DessinerEnMain(renderer, spellDescriptor2, centre, 1f - 0.3f * num4, 1f - num3, avecNom: false);
				return;
			}
		}
		_retourSlot = -1;
	}

	private void DessinerEnMain(Intersect.Client.Framework.Gwen.Renderer.Base renderer, SpellDescriptor sort, Point centre, float echelle, float opacite, bool avecNom)
	{
		TechniqueFamily family = SpellMastery.FamilyOf(sort);
		Color color = TechniqueArt.FamilyColor(family);
		int num = Math.Max(24, (int)MathF.Round((float)_d.L(76f) * echelle));
		Rectangle outer = new Rectangle(centre.X - num / 2, centre.Y - num / 2, num, num);
		TechniqueArt.Glow(renderer, centre.X + _d.L(6f), centre.Y + _d.L(12f), (float)num * 0.62f, A(TechniqueArt.Black, 0.55f * opacite), additive: false);
		TechniqueArt.Glow(renderer, centre.X, centre.Y, (float)num * 0.85f, A(color, 0.45f * opacite));
		DrawHexEmblem(renderer, outer, GameContentManager.Current.GetTexture(TextureType.Spell, sort.Icon), TechniqueArt.Glyph(family), A(color, opacite), 0.6086956f);
		if (avecNom)
		{
			string text = sort.Name.ToUpperInvariant();
			float spacing = _d.Em(0.08f, 13f);
			int num2 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(13f), text, spacing) + _d.L(20f);
			Rectangle rect = new Rectangle(centre.X - num2 / 2, outer.Bottom + _d.L(4f), num2, _d.L(24f));
			TechniqueArt.Patch(renderer, "tech_biais4.png", rect, A(new Color(235, 10, 14, 24), opacite), 5);
			TechniqueArt.Outline(renderer, rect, A(color, 0.7f * opacite));
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(13f), text, rect.X + _d.L(10f), rect.Y + rect.Height / 2, spacing, A(TechniqueArt.Text, opacite));
		}
	}

	private void DessinerCibleDeLaBarre(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, int index, int cible, Color couleur, long now)
	{
		if (EnTransport)
		{
			float num = 0.5f - 0.5f * MathF.Cos((float)now / 900f * ((float)Math.PI * 2f));
			if (index == cible)
			{
				TechniqueArt.GlowRect(renderer, new Rectangle(rect.X - _d.L(14f), rect.Y - _d.L(14f), rect.Width + _d.L(28f), rect.Height + _d.L(28f)), A(couleur, 0.65f));
				TechniqueArt.Fill(renderer, rect, A(TechniqueArt.Mix(couleur, BarBack, 0.28f)));
				TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Mix(couleur, TechniqueArt.White, 0.8f)), Math.Max(2, _d.L(2f)));
				SpellDescriptor spellDescriptor = SortDuLivre(_porteSlot);
				if (spellDescriptor != null)
				{
					IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Spell, spellDescriptor.Icon);
					if (texture != null)
					{
						int num2 = _d.L(26f);
						SaiyanHud.Draw(renderer, texture, rect.X + (rect.Width - num2) / 2, rect.Y + (rect.Height - num2) / 2, num2, num2, A(TechniqueArt.White, 0.85f));
					}
				}
			}
			else
			{
				TechniqueArt.Outline(renderer, rect, A(couleur, 0.35f + 0.35f * num));
			}
		}
		if (index == _poseeCase)
		{
			float num3 = Math.Clamp((float)(now - _poseeDepuis) / 520f, 0f, 1f);
			if (num3 >= 1f)
			{
				_poseeCase = -1;
				return;
			}
			int num4 = _d.L(4f + 18f * num3);
			TechniqueArt.GlowRect(renderer, new Rectangle(rect.X - num4, rect.Y - num4, rect.Width + num4 * 2, rect.Height + num4 * 2), A(couleur, 0.7f * (1f - num3)));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Mix(TechniqueArt.White, couleur, 1f - num3), 1f - num3), Math.Max(2, _d.L(2f)));
		}
	}
}
