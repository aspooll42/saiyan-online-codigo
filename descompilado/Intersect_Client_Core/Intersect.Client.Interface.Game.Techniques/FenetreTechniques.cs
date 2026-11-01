using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Core.Controls;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Localization;
using Intersect.Client.Spells;
using Intersect.Config;
using Intersect.Enums;
using Intersect.GameObjects;

namespace Intersect.Client.Interface.Game.Techniques;

internal sealed class FenetreTechniques : FenetreCharte
{
	private const float ListeX = 16f;

	private const float ListeY = 98f;

	private const float LargeurRangee = 298f;

	private const float HauteurRangee = 34f;

	private const float PasRangee = 37f;

	private const float PasColonne = 306f;

	private const int Colonnes = 2;

	private const int RangeesVisibles = 9;

	private const float Bas = 462f;

	private const int Crans = 5;

	private readonly TechniquesWindow _ecran;

	private List<TechniquesWindow.Known> _connues = new List<TechniquesWindow.Known>();

	private readonly List<TechniquesWindow.Known> _montrees = new List<TechniquesWindow.Known>();

	private readonly int[] _comptes = new int[TechniqueArt.Families.Length];

	private int _signature;

	private bool _lu;

	private TechniqueFamily? _famille;

	private int _defilement;

	private readonly List<(Rectangle Rect, int Slot)> _rangees = new List<(Rectangle, int)>();

	private readonly List<Rectangle> _boutons = new List<Rectangle>();

	private readonly List<Puce> _puces = new List<Puce>();

	private readonly float[] _crans = new float[5];

	private int _appuiSlot = -1;

	private Point _appuiPosition;

	private bool _glisse;

	private bool _appuiDroit;

	private (int Slot, bool Details) _bulleCle;

	private Bulle? _bulle;

	private bool _attendLaSequence;

	protected override Rectangle Place => new Rectangle(924, 196, 636, 476);

	public override bool TientUnObjet
	{
		get
		{
			if (_appuiSlot >= 0 && !_appuiDroit)
			{
				return Globals.InputManager.IsMouseButtonDown(MouseButton.Left);
			}
			return false;
		}
	}

	public override bool ObjetEnVol
	{
		get
		{
			if (_glisse)
			{
				return TientUnObjet;
			}
			return false;
		}
	}

	public override Color CouleurEnVol
	{
		get
		{
			TechniquesWindow.Known? known = Tenue();
			if (known.HasValue)
			{
				TechniquesWindow.Known valueOrDefault = known.GetValueOrDefault();
				if (valueOrDefault.Spell != null)
				{
					return TechniqueArt.FamilyColor(valueOrDefault.Family);
				}
			}
			return TechniqueArt.Soft;
		}
	}

	protected override bool SurvolSuspendu => _glisse;

	protected override bool PrendLesRaccourcis => RangeeSurvolee().HasValue;

	public FenetreTechniques(Canvas gameCanvas, TechniquesWindow ecran)
		: base(gameCanvas, "FenetreTechniques")
	{
		_ecran = ecran;
	}

	public override void Show()
	{
		_lu = false;
		_defilement = 0;
		base.Show();
	}

	public override void Hide()
	{
		_attendLaSequence = false;
		AnnulerGlisser();
		base.Hide();
	}

	public void Update()
	{
		if (_attendLaSequence)
		{
			if (!CharteScreen.EstOuvert)
			{
				_attendLaSequence = false;
				Show();
			}
		}
		else if (!base.IsHidden)
		{
			if (CharteScreen.EstOuvert)
			{
				Hide();
			}
			else
			{
				Placer();
			}
		}
	}

	private void Lire(Player me)
	{
		HashCode hashCode = default(HashCode);
		Spell[] spells = me.Spells;
		foreach (Spell spell in spells)
		{
			hashCode.Add(spell?.Id ?? Guid.Empty);
			hashCode.Add(spell?.Rank ?? 0);
			hashCode.Add(spell?.Experience ?? 0);
		}
		HotbarInstance[] hotbar = me.Hotbar;
		for (int j = 0; j < hotbar.Length; j++)
		{
			hashCode.Add(hotbar[j]?.ItemOrSpellId ?? Guid.Empty);
		}
		int[] stat = me.Stat;
		foreach (int value in stat)
		{
			hashCode.Add(value);
		}
		hashCode.Add(me.Level);
		int num = hashCode.ToHashCode();
		if (_lu && num == _signature)
		{
			return;
		}
		_lu = true;
		_signature = num;
		_bulle = null;
		List<TechniquesWindow.Known> connues = TechniquesWindow.KnownTechniques();
		_connues = TechniqueArt.Families.SelectMany((TechniqueFamily techniqueFamily) => connues.Where((TechniquesWindow.Known entree) => entree.Family == techniqueFamily)).ToList();
		Array.Clear(_comptes);
		foreach (TechniquesWindow.Known connue in _connues)
		{
			int num2 = Array.IndexOf(TechniqueArt.Families, connue.Family);
			if (num2 >= 0)
			{
				_comptes[num2]++;
			}
		}
		TechniqueFamily? famille = _famille;
		if (famille.HasValue)
		{
			TechniqueFamily valueOrDefault = famille.GetValueOrDefault();
			if (_comptes[Math.Max(0, Array.IndexOf(TechniqueArt.Families, valueOrDefault))] == 0)
			{
				_famille = null;
			}
		}
	}

	private void Composer()
	{
		_montrees.Clear();
		foreach (TechniquesWindow.Known connue in _connues)
		{
			if (!_famille.HasValue || connue.Family == _famille)
			{
				_montrees.Add(connue);
			}
		}
		int num = (_montrees.Count + 2 - 1) / 2;
		_defilement = Math.Clamp(_defilement, 0, Math.Max(0, num - 9));
	}

	private TechniquesWindow.Known? Trouver(int slot)
	{
		foreach (TechniquesWindow.Known connue in _connues)
		{
			if (connue.Slot == slot)
			{
				return connue;
			}
		}
		return null;
	}

	private TechniquesWindow.Known? Tenue()
	{
		if (_appuiSlot < 0)
		{
			return null;
		}
		return Trouver(_appuiSlot);
	}

	private static string ToucheDe(int index)
	{
		return DropdownPanel.KeyOf((Control)(4097 + index)) ?? ((index + 1) % 10).ToString(CultureInfo.InvariantCulture);
	}

	private static List<int> CasesDe(Player me, Guid technique)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < me.Hotbar.Length; i++)
		{
			HotbarInstance obj = me.Hotbar[i];
			if (obj != null && obj.ItemOrSpellId == technique)
			{
				list.Add(i);
			}
		}
		return list;
	}

	private float[] CransDe(TechniquesWindow.Known entree)
	{
		int num = SpellMastery.ExperienceToNext(entree.Spell, entree.Rank);
		for (int i = 0; i < 5; i++)
		{
			_crans[i] = ((i < entree.Rank) ? 1f : ((i == entree.Rank && num > 0) ? Math.Clamp((float)entree.Experience / (float)num, 0f, 1f) : 0f));
		}
		return _crans;
	}

	protected override void Dessiner(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		_rangees.Clear();
		_boutons.Clear();
		DessinerCadre(renderer);
		int croix = DessinerBarre(renderer, Strings.Techniques.Title);
		Player me = Globals.Me;
		if (me != null)
		{
			Lire(me);
			Composer();
			DessinerCompte(renderer, croix);
			if (_connues.Count == 0)
			{
				TechniqueArt.TextCenter(renderer, TechniqueArt.Body, Pt(14f), Strings.Techniques.NoTechnique, base.Width / 2, _d.Y(250f), A(TechniqueArt.Dim));
				return;
			}
			DessinerFamilles(renderer);
			DessinerListe(renderer, me);
			DessinerPied(renderer);
		}
	}

	protected override void DessinerDessus(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		Player me = Globals.Me;
		if (me != null)
		{
			if (_glisse)
			{
				DessinerEnMain(renderer);
				return;
			}
			DessinerEtiquette(renderer, now);
			DessinerBulleDeLaTechnique(renderer, me);
		}
	}

	private void DessinerCompte(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int croix)
	{
		int num = _d.Y(23f);
		int num2 = croix - _d.L(16f);
		string text = ((_connues.Count > 1) ? Strings.Techniques.WindowLearnedMany : Strings.Techniques.WindowLearnedOne).ToString().ToUpperInvariant();
		int num3 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(11f), text, _d.Em(0.14f, 11f));
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(11f), text, num2 - num3, num + _d.L(2f), _d.Em(0.14f, 11f), A(TechniqueArt.Dim));
		num2 -= num3 + _d.L(5f);
		TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(19f), _connues.Count.ToString(CultureInfo.InvariantCulture), num2, num, A(TechniqueArt.Text));
	}

	private void DessinerFamilles(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		_puces.Clear();
		_puces.Add(new Puce("famille:tout", Strings.Personnage.CategoryAll, _connues.Count.ToString(CultureInfo.InvariantCulture), !_famille.HasValue, delegate
		{
			Choisir(null);
		}, delegate(Rectangle rect, Color encre)
		{
			Lisser("inv_glyphe_tout.png", rect, encre);
		}));
		for (int num = 0; num < TechniqueArt.Families.Length; num++)
		{
			if (_comptes[num] != 0)
			{
				TechniqueFamily famille = TechniqueArt.Families[num];
				_puces.Add(new Puce($"famille:{famille}", TechniqueArt.FamilyName(famille), _comptes[num].ToString(CultureInfo.InvariantCulture), _famille == famille, delegate
				{
					Choisir(famille);
				}, delegate(Rectangle rect, Color encre)
				{
					Pictogramme(famille, rect, encre);
				}, TechniqueArt.FamilyColor(famille)));
			}
		}
		DessinerPuces(renderer, _d.R(16f, 58f, 604f, 30f), _puces);
	}

	private void Choisir(TechniqueFamily? famille)
	{
		_famille = famille;
		_defilement = 0;
	}

	private void Pictogramme(TechniqueFamily famille, Rectangle rect, Color encre)
	{
		IGameTexture gameTexture = TechniqueArt.Tex("tech_familles.png");
		if (gameTexture != null && gameTexture.Width > 0)
		{
			int num = TechniqueArt.Glyph(famille);
			Lisser(gameTexture, rect, encre, ((float)(num * 40) + 0.5f) / (float)gameTexture.Width, 0f, ((float)((num + 1) * 40) - 0.5f) / (float)gameTexture.Width);
		}
	}

	private void DessinerListe(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me)
	{
		Point point = LocalMouse();
		bool survolee = base.Survolee;
		int num = _defilement * 2;
		int num2 = Math.Min(_montrees.Count, num + 18);
		for (int i = num; i < num2; i++)
		{
			TechniquesWindow.Known entree = _montrees[i];
			Rectangle rectangle = _d.R(16f + (float)(i % 2) * 306f, 98f + (float)(i / 2 - _defilement) * 37f, 298f, 34f);
			_rangees.Add((rectangle, entree.Slot));
			DessinerRangee(renderer, me, rectangle, entree, survolee && rectangle.Contains(point.X, point.Y), _glisse && entree.Slot == _appuiSlot);
		}
		int num3 = (_montrees.Count + 2 - 1) / 2;
		if (num3 > 9)
		{
			Rectangle rect = _d.R(624f, 98f, 3f, 330f);
			TechniqueArt.Fill(renderer, rect, A(TechniqueArt.Line, 0.6f));
			int num4 = Math.Max(_d.L(24f), rect.Height * 9 / num3);
			int num5 = rect.Height - num4;
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y + num5 * _defilement / (num3 - 9), rect.Width, num4), A(TechniqueArt.Line2));
		}
	}

	private void DessinerRangee(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, Rectangle rect, TechniquesWindow.Known entree, bool chaud, bool tenue)
	{
		SpellDescriptor spell = entree.Spell;
		Color color = TechniqueArt.FamilyColor(entree.Family);
		SpellDescriptor next;
		bool flag = TechniquesWindow.CanEvolve(entree, out next);
		float alpha = (tenue ? 0.4f : 1f);
		if (flag)
		{
			FenetreCharte.Degrade(renderer, rect, A(TechniqueArt.Mix(TechniqueArt.Gold, new Color(255, 15, 22, 38), 0.16f)), A(new Color(255, 11, 16, 27)), 6);
		}
		else if (chaud)
		{
			FenetreCharte.Degrade(renderer, rect, A(new Color(255, 24, 35, 58)), A(new Color(255, 11, 16, 27)), 6);
		}
		else
		{
			FenetreCharte.Degrade(renderer, rect, A(new Color(255, 15, 22, 38)), A(new Color(255, 10, 14, 24)), 6);
		}
		TechniqueArt.Outline(renderer, rect, A(flag ? TechniqueArt.Gold : (chaud ? new Color(255, 90, 108, 148) : TechniqueArt.Line)));
		TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, Math.Max(2, _d.L(2f)), rect.Height), A(flag ? TechniqueArt.Gold : color, (chaud || flag) ? 1f : 0.6f));
		int num = rect.X + _d.L(6f);
		int num2 = TailleIcone();
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Spell, spell.Icon);
		if (texture != null)
		{
			int num3 = Math.Min(num2, rect.Height - Math.Max(2, _d.L(4f)));
			Rectangle rect2 = new Rectangle(num + (num2 - num3) / 2, rect.Y + (rect.Height - num3) / 2, num3, num3);
			if (num3 == num2)
			{
				SaiyanHud.Draw(renderer, texture, rect2.X, rect2.Y, num3, num3, A(TechniqueArt.White, alpha));
			}
			else
			{
				Lisser(texture, rect2, A(TechniqueArt.White, alpha));
			}
		}
		int num4 = rect.Y + rect.Height / 2;
		int num5 = rect.Right - _d.L(7f);
		List<int> list = CasesDe(me, spell.Id);
		for (int num6 = Math.Min(list.Count, 2) - 1; num6 >= 0; num6--)
		{
			string texte = ToucheDe(list[num6]);
			num5 -= LargeurTouche(renderer, texte);
			Touche(renderer, texte, num5, num4);
			num5 -= _d.L(4f);
		}
		if (flag)
		{
			num5 = DessinerEvoluer(renderer, entree, num5, num4);
		}
		int num7 = num + num2 + _d.L(10f);
		int width = Math.Max(_d.L(40f), num5 - _d.L(4f) - num7);
		List<string> list2 = TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(17f), spell.Name, width, 1);
		string text = ((list2.Count > 0) ? list2[0] : spell.Name);
		if (!SpellMastery.HasMastery(spell))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(17f), text, num7, num4, A(TechniqueArt.Text, alpha));
			return;
		}
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(17f), text, num7, rect.Y + _d.L(13f), A(TechniqueArt.Text, alpha));
		Jauge(renderer, num7, rect.Y + _d.L(25.5f), _d.L(16f), Math.Max(3, _d.L(4f)), CransDe(entree), TechniqueArt.Alpha(color, alpha));
	}

	private int DessinerEvoluer(Intersect.Client.Framework.Gwen.Renderer.Base renderer, TechniquesWindow.Known entree, int droite, int centre)
	{
		string text = Strings.Techniques.EvolveShort.ToString().ToUpperInvariant();
		int num = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(14f), text).X + _d.L(18f);
		int num2 = _d.L(22f);
		Rectangle rectangle = new Rectangle(droite - num, centre - num2 / 2, num, num2);
		string cle = $"evoluer:{entree.Slot}";
		bool flag = IsHot(cle);
		TechniqueArt.FillVertical(renderer, rectangle, A(flag ? TechniqueArt.White : new Color(255, 255, 241, 192)), A(new Color(255, 255, 138, 26)));
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(14f), text);
		TechniqueArt.Italic(renderer, TechniqueArt.Display, Pt(14f), text, rectangle.X + (rectangle.Width - point.X) / 2, rectangle.Y + (rectangle.Height - point.Y) / 2, A(new Color(255, 20, 10, 2)));
		_boutons.Add(rectangle);
		int slot = entree.Slot;
		AddHit(rectangle, cle, delegate
		{
			Evoluer(slot);
		});
		return rectangle.X - _d.L(4f);
	}

	private void DessinerPied(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		int num = _d.Y(453f);
		int num2 = _d.X(16f);
		num2 += Touche(renderer, Strings.Techniques.KeyDrag, num2, num) + _d.L(6f);
		num2 += TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), Strings.Techniques.HintDrag, num2, num, A(TechniqueArt.Dim)) + _d.L(6f);
		num2 += Touche(renderer, ToucheDe(0), num2, num) + _d.L(5f);
		num2 += TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), Strings.Techniques.HintTo, num2, num, A(TechniqueArt.Dim)) + _d.L(5f);
		num2 += Touche(renderer, ToucheDe(9), num2, num) + _d.L(6f);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), Strings.Techniques.HintPoint, num2, num, A(TechniqueArt.Dim));
		int num3 = _d.X(620f);
		num3 -= TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(12f), Strings.Inventaire.HintDetails, num3, num, A(TechniqueArt.Dim)) + _d.L(6f);
		Touche(renderer, Strings.Inventaire.KeyShift, num3 - LargeurTouche(renderer, Strings.Inventaire.KeyShift), num);
	}

	private void DessinerEnMain(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		TechniquesWindow.Known? known = Tenue();
		if (!known.HasValue)
		{
			return;
		}
		TechniquesWindow.Known valueOrDefault = known.GetValueOrDefault();
		if (valueOrDefault.Spell != null)
		{
			Point point = LocalMouse();
			int num = TailleIcone();
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Spell, valueOrDefault.Spell.Icon);
			if (texture != null)
			{
				SaiyanHud.Draw(renderer, texture, point.X - num / 2, point.Y - num / 2, num, num, TechniqueArt.Alpha(TechniqueArt.White, 0.92f));
			}
			string text = valueOrDefault.Spell.Name.ToUpperInvariant();
			float spacing = _d.Em(0.08f, 11f);
			int num2 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(11f), text, spacing) + _d.L(16f);
			Rectangle rect = new Rectangle(point.X - num2 / 2, point.Y + num / 2 + _d.L(4f), num2, _d.L(20f));
			TechniqueArt.Fill(renderer, rect, new Color(235, 10, 14, 24));
			TechniqueArt.Outline(renderer, rect, TechniqueArt.FamilyColor(valueOrDefault.Family));
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(11f), text, rect.X + _d.L(8f), rect.Y + rect.Height / 2, spacing, TechniqueArt.Text);
		}
	}

	private void DessinerBulleDeLaTechnique(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me)
	{
		int? num = RangeeSurvolee();
		if (!num.HasValue)
		{
			return;
		}
		int valueOrDefault = num.GetValueOrDefault();
		TechniquesWindow.Known? known = Trouver(valueOrDefault);
		if (!known.HasValue)
		{
			return;
		}
		TechniquesWindow.Known valueOrDefault2 = known.GetValueOrDefault();
		Rectangle cible = default(Rectangle);
		foreach (var (rectangle, num2) in _rangees)
		{
			if (num2 == valueOrDefault)
			{
				cible = rectangle;
				break;
			}
		}
		(int, bool) tuple2 = (valueOrDefault, Globals.InputManager.IsKeyDown(Keys.Shift));
		if (_bulle != null)
		{
			(int, bool) bulleCle = _bulleCle;
			(int, bool) tuple3 = tuple2;
			if (bulleCle.Item1 == tuple3.Item1 && bulleCle.Item2 == tuple3.Item2)
			{
				goto IL_00eb;
			}
		}
		_bulleCle = tuple2;
		_bulle = BulleDe(me, valueOrDefault2, tuple2.Item2);
		goto IL_00eb;
		IL_00eb:
		DessinerBulle(renderer, _bulle, cible);
	}

	private Bulle BulleDe(Player me, TechniquesWindow.Known entree, bool details)
	{
		SpellDescriptor spell = entree.Spell;
		Color color = TechniqueArt.FamilyColor(entree.Family);
		bool flag = SpellMastery.HasMastery(spell);
		string text = TechniqueArt.FamilyName(entree.Family);
		Bulle bulle = new Bulle
		{
			Icone = Globals.ContentManager.GetTexture(TextureType.Spell, spell.Icon),
			Nom = spell.Name,
			Couleur = color,
			CouleurNom = color,
			Sorte = (flag ? Strings.Techniques.KindRank.ToString(text, TechniqueArt.Roman(entree.Rank)) : text)
		};
		TechniquesWindow.DamageSplit? damageSplit = TechniquesWindow.DamageOf(spell, entree.Rank);
		if (damageSplit.HasValue)
		{
			TechniquesWindow.DamageSplit valueOrDefault = damageSplit.GetValueOrDefault();
			string text2 = valueOrDefault.Total.ToString(CultureInfo.InvariantCulture);
			Ligne(Strings.Techniques.RowDamage, (valueOrDefault.Hits > 1) ? Strings.Techniques.ValueHits.ToString(text2, valueOrDefault.Hits) : text2);
		}
		if (TechniquesWindow.IsTransformation(spell))
		{
			TransformationOptions transformation = Options.Instance.Transformation;
			Ligne(Strings.Techniques.LineEntry, Strings.Techniques.ValueEntry.ToString(TechniqueArt.Number(transformation.KiCostPercent)));
			Ligne(Strings.Techniques.LineLeak, Strings.Techniques.ValueLeak.ToString(TechniqueArt.Number((double)transformation.KiDrainPercentPerSecond * SpellMastery.LeakFactor(spell, entree.Rank))));
		}
		else
		{
			long num = SpellMastery.VitalCost(spell, Vital.Mana, entree.Rank);
			if (num > 0)
			{
				Ligne(Strings.Techniques.RowKi, num.ToString(CultureInfo.InvariantCulture));
			}
		}
		int num2 = SpellMastery.CastDuration(spell, entree.Rank);
		if (num2 >= 50)
		{
			Ligne(Strings.Techniques.LineCast, Secondes(num2));
		}
		int num3 = SpellMastery.Cooldown(spell, entree.Rank);
		if (num3 >= 50)
		{
			Ligne(Strings.Techniques.LineCooldown, Secondes(num3));
		}
		if (!TechniquesWindow.IsTransformation(spell))
		{
			int num4 = SpellMastery.EffectDuration(spell, entree.Rank);
			if (num4 >= 50)
			{
				Ligne(Strings.Techniques.LineDuration, Secondes(num4));
			}
		}
		if (flag)
		{
			int num5 = SpellMastery.ExperienceToNext(spell, entree.Rank);
			bulle.Crans = new ReadOnlySpan<float>(CransDe(entree)).ToArray();
			if (num5 > 0)
			{
				bulle.Jauge = Strings.Techniques.Mastery;
				bulle.Compte = $"{entree.Experience} / {num5}";
			}
			else
			{
				bulle.Jauge = Strings.Techniques.MasteryFull;
				bulle.JaugePleine = true;
			}
		}
		SpellDescriptor next;
		bool flag2 = TechniquesWindow.CanEvolve(entree, out next) && next != null;
		if (flag2)
		{
			bulle.Annonce = Strings.Techniques.Ready;
			bulle.Paliers.Add((Strings.Techniques.ReadyBecomes.ToString(next.Name), TechniqueArt.Soft));
			bulle.Paliers.Add(($"{Strings.Techniques.ConditionRank} · {Strings.Techniques.ConditionLevel.ToString(spell.Mastery.EvolutionLevel)}", TechniqueArt.Green));
		}
		List<int> list = CasesDe(me, spell.Id);
		if (list.Count == 0)
		{
			bulle.Verdict.Add((Strings.Techniques.NotOnBar, TechniqueArt.Soft));
		}
		else
		{
			string text3 = ToucheDe(list[0]);
			for (int i = 1; i < list.Count; i++)
			{
				text3 = ((i == list.Count - 1) ? Strings.Techniques.KeysAnd.ToString(text3, ToucheDe(list[i])) : (text3 + ", " + ToucheDe(list[i])));
			}
			bulle.Verdict.Add((((list.Count > 1) ? Strings.Techniques.OnBarMany : Strings.Techniques.OnBarOne).ToString(text3), TechniqueArt.Text));
		}
		if (details)
		{
			bulle.Description = (string.IsNullOrWhiteSpace(spell.Description) ? null : spell.Description.Trim());
			List<string> list2 = TechniquesWindow.StepsOf(spell);
			if (list2.Count > 0)
			{
				bulle.Notes.Add(Strings.Techniques.EachRankLine.ToString(string.Join(", ", list2)));
			}
			if (flag)
			{
				bulle.Notes.Add(SpellMastery.IsOffensive(spell) ? Strings.Techniques.RisesOnHit : Strings.Techniques.RisesOnUse);
			}
			if (!spell.Bound)
			{
				bulle.Notes.Add(Strings.Techniques.HelpForget);
			}
		}
		string text4 = (flag2 ? Strings.Techniques.HelpEvolve : Strings.Techniques.HelpDrag).ToString();
		bulle.Aides.Add(details ? text4 : $"{text4} · {Strings.Inventaire.HelpDetails}");
		return bulle;
		void Ligne(string nom, string valeur)
		{
			bulle.Lignes.Add(new LigneBulle(nom, valeur, 0L, null));
		}
		static string Secondes(double millisecondes)
		{
			return Strings.Techniques.ValueSeconds.ToString(TechniqueArt.Number(millisecondes / 1000.0));
		}
	}

	private int? RangeeSous(Point local)
	{
		foreach (var (rectangle, value) in _rangees)
		{
			if (rectangle.Contains(local.X, local.Y))
			{
				return value;
			}
		}
		return null;
	}

	private int? RangeeSurvolee()
	{
		if (!base.Survolee)
		{
			return null;
		}
		return RangeeSous(LocalMouse());
	}

	private bool SurUnBouton(Point local)
	{
		foreach (Rectangle bouton in _boutons)
		{
			if (bouton.Contains(local.X, local.Y))
			{
				return true;
			}
		}
		return false;
	}

	private void AnnulerGlisser()
	{
		_appuiSlot = -1;
		_appuiDroit = false;
		_glisse = false;
	}

	private void Lacher()
	{
		int appuiSlot = _appuiSlot;
		AnnulerGlisser();
		Point point = LocalMouse();
		if ((point.X >= 0 && point.Y >= 0 && point.X < base.Width && point.Y < base.Height) || appuiSlot < 0)
		{
			return;
		}
		Player me = Globals.Me;
		if (me != null)
		{
			int? num = FenetreCharte.RaccourciSousLeCurseur();
			if (num.HasValue)
			{
				int valueOrDefault = num.GetValueOrDefault();
				me.AddToHotbar(valueOrDefault, 1, appuiSlot);
				Audio.AddGameSound("menu_click.wav", loop: false);
			}
		}
	}

	private static void Oublier(TechniquesWindow.Known entree)
	{
		if (!entree.Spell.Bound)
		{
			Globals.Me?.TryForgetSpell(entree.Slot);
		}
	}

	private void Evoluer(int slot)
	{
		if (_ecran.JouerEvolution(slot))
		{
			Hide();
			_attendLaSequence = true;
		}
	}

	protected override void OnMouseDown(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseDown(mouseButton, mousePosition, userAction);
		AnnulerGlisser();
		Point point = CanvasPosToLocal(mousePosition);
		int? num = RangeeSous(point);
		if (num.HasValue)
		{
			int valueOrDefault = num.GetValueOrDefault();
			if (!SurUnBouton(point))
			{
				_appuiSlot = valueOrDefault;
				_appuiPosition = point;
				_appuiDroit = Globals.InputManager.IsMouseButtonDown(MouseButton.Right);
			}
		}
	}

	protected override void OnMouseMoved(int x, int y, int dx, int dy)
	{
		base.OnMouseMoved(x, y, dx, dy);
		if (!_glisse && _appuiSlot >= 0 && !_appuiDroit)
		{
			Point point = LocalMouse();
			if (Math.Abs(point.X - _appuiPosition.X) + Math.Abs(point.Y - _appuiPosition.Y) > _d.L(8f))
			{
				_glisse = true;
				Audio.AddGameSound("menu_hover.wav", loop: false);
			}
		}
	}

	protected override void OnMouseClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		if (_glisse)
		{
			Lacher();
		}
		else if (_appuiDroit)
		{
			int appuiSlot = _appuiSlot;
			AnnulerGlisser();
			if (appuiSlot >= 0 && RangeeSous(CanvasPosToLocal(mousePosition)) == appuiSlot)
			{
				TechniquesWindow.Known? known = Trouver(appuiSlot);
				if (known.HasValue)
				{
					TechniquesWindow.Known valueOrDefault = known.GetValueOrDefault();
					Oublier(valueOrDefault);
				}
			}
		}
		else
		{
			AnnulerGlisser();
			base.OnMouseClicked(mouseButton, mousePosition, userAction);
		}
	}

	protected override void OnMouseUp(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseUp(mouseButton, mousePosition, userAction);
		if (_glisse)
		{
			Lacher();
		}
		if (!_appuiDroit)
		{
			AnnulerGlisser();
		}
	}

	protected override bool OnMouseWheeled(int delta)
	{
		int num = (_montrees.Count + 2 - 1) / 2;
		_defilement = Math.Clamp(_defilement - Math.Sign(delta), 0, Math.Max(0, num - 9));
		return true;
	}

	internal override bool HandleKey(Keys modifier, Keys key)
	{
		Player me = Globals.Me;
		if (me != null)
		{
			int? num = RangeeSurvolee();
			if (num.HasValue)
			{
				int valueOrDefault = num.GetValueOrDefault();
				foreach (Control item in Controls.GetControlsFor(modifier, key))
				{
					int num2 = (int)(item - 4096 - 1);
					if (num2 >= 0 && num2 < me.Hotbar.Length)
					{
						me.AddToHotbar(num2, 1, valueOrDefault);
						FenetreCharte.RetenirLaTouche(item);
						Audio.AddGameSound("menu_click.wav", loop: false);
						return true;
					}
				}
				return false;
			}
		}
		return false;
	}
}
