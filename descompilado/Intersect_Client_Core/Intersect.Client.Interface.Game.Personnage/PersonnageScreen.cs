using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Input;
using Intersect.Client.Framework.Items;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.DescriptionWindows;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Client.Spells;
using Intersect.Config;
using Intersect.Enums;
using Intersect.Framework.Core.Entities;
using Intersect.Framework.Core.GameObjects.Crafting;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Framework.Core.GameObjects.Ornaments;
using Intersect.Framework.Core.GameObjects.Panoplies;
using Intersect.Framework.Core.GameObjects.PlayerClass;
using Intersect.Framework.Core.GameObjects.Resources;
using Intersect.Framework.Core.GameObjects.Titles;
using Intersect.GameObjects;
using Intersect.Localization;
using Intersect.Models;

namespace Intersect.Client.Interface.Game.Personnage;

public sealed class PersonnageScreen : CharteScreen
{
	internal readonly record struct Apports(int[] Stats, long[] Vitaux, Dictionary<ItemEffect, int> Effets);

	public enum Onglet
	{
		Equipement,
		Caracteristiques,
		Personnalisation
	}

	internal readonly record struct Ligne(string Nom, string? Aide, string Valeur, long? Ecart, bool Pourcent, bool Effet);

	private readonly record struct Choix(Guid Id, string Nom, Color Couleur, string? Note, string Dossier, bool Debloque, bool Porte, string? Style, int Niveau);

	private enum Categorie
	{
		Tout,
		Equipement,
		Consommables,
		Techniques,
		Ressources,
		Quete,
		Sacs
	}

	private enum Tri
	{
		Aucun,
		Rarete,
		Nom,
		Type,
		Quantite,
		Valeur
	}

	internal static readonly Stat[] StatsMontrees = new Stat[5]
	{
		Stat.Attack,
		Stat.AbilityPower,
		Stat.Defense,
		Stat.MagicResist,
		Stat.Speed
	};

	internal static readonly ItemEffect[] EffetsMontres = new ItemEffect[7]
	{
		ItemEffect.CooldownReduction,
		ItemEffect.Lifesteal,
		ItemEffect.Tenacity,
		ItemEffect.Luck,
		ItemEffect.EXP,
		ItemEffect.Endurance,
		ItemEffect.Concentration
	};

	private static readonly int[] Pas = new int[3] { 1, 5, 2147483647 };

	private int _pas = 1;

	private Apports _avant;

	private Apports? _apres;

	private ItemDescriptor? _essai;

	private readonly Canvas _gameCanvas;

	private Onglet _onglet = Onglet.Caracteristiques;

	private int _selectionSac = -1;

	private int _selectionEmplacement = -1;

	private const float FicheX = 1148f;

	private const float FicheLargeur = 400f;

	private const float ActionsY = 802f;

	private readonly Dictionary<Guid, List<(string? Titre, string? Detail, string? Icone)>> _sources = new Dictionary<Guid, List<(string, string, string)>>();

	private Guid? _essaiTitre;

	private Guid? _essaiOrnement;

	private float _defilTitres;

	private float _defilOrnements;

	private Rectangle _zoneTitres;

	private Rectangle _zoneOrnements;

	private static readonly Color Verrouille = new Color(255, 110, 120, 145);

	private const int Colonnes = 8;

	private const int Rangees = 7;

	private const float TailleCase = 62f;

	private const float PasCase = 68f;

	private const float GrilleX = 556f;

	private const float GrilleY = 230f;

	private Categorie _categorie;

	private Tri _tri;

	private TextBox _recherche;

	private string _requete = string.Empty;

	private readonly List<(Rectangle Rect, int CaseSac)> _cases = new List<(Rectangle, int)>();

	private readonly List<(Rectangle Rect, int Raccourci)> _casesRaccourcis = new List<(Rectangle, int)>();

	private Rectangle _zoneSac;

	private int _appuiSac = -1;

	private int _appuiEmplacement = -1;

	private Point _appuiPosition;

	private bool _glisse;

	private static readonly Dictionary<string, (float X, float Y)> PlacesEmplacements = new Dictionary<string, (float, float)>
	{
		["tete"] = (76f, 140f),
		["tenue"] = (76f, 272f),
		["haut"] = (76f, 272f),
		["pantalon"] = (76f, 404f),
		["bottes"] = (76f, 536f),
		["dos"] = (392f, 140f),
		["gants"] = (392f, 272f),
		["aura"] = (392f, 404f),
		["noyau"] = (234f, 520f)
	};

	private const float SceneCentreX = 276f;

	private const float SceneSol = 496f;

	private static readonly int[] Tour = new int[4] { 0, 2, 3, 1 };

	private int _tour;

	private readonly List<(Rectangle Rect, int Emplacement)> _casesEmplacements = new List<(Rectangle, int)>();

	public Onglet OngletActif => _onglet;

	protected override string? DetailsKey => "PersonnageDetails";

	internal static Apports ApportsDe(List<(ItemDescriptor Objet, ItemProperties? Proprietes)> portes)
	{
		int[] array = new int[Enum.GetValues<Stat>().Length];
		long[] array2 = new long[Enum.GetValues<Vital>().Length];
		Dictionary<ItemEffect, int> dictionary = new Dictionary<ItemEffect, int>();
		foreach (var porte in portes)
		{
			ItemDescriptor item = porte.Objet;
			ItemProperties item2 = porte.Proprietes;
			for (int i = 0; i < array.Length; i++)
			{
				array[i] += StatDe(item, item2, i);
			}
			for (int j = 0; j < array2.Length; j++)
			{
				ref long reference = ref array2[j];
				long num = reference;
				long[] vitalsGiven = item.VitalsGiven;
				reference = num + ((vitalsGiven != null && j < vitalsGiven.Length) ? vitalsGiven[j] : 0);
			}
			foreach (KeyValuePair<ItemEffect, int> item3 in EffetsDe(item))
			{
				item3.Deconstruct(out var key, out var value);
				ItemEffect key2 = key;
				int num2 = value;
				dictionary[key2] = dictionary.GetValueOrDefault(key2) + num2;
			}
		}
		foreach (PanoplieBonus item4 in PanoplieDescriptor.BonusActifs(portes.Select<(ItemDescriptor, ItemProperties), Guid>(((ItemDescriptor Objet, ItemProperties Proprietes) entree) => entree.Objet.Id)))
		{
			for (int num3 = 0; num3 < Math.Min(array.Length, item4.Stats.Length); num3++)
			{
				array[num3] += item4.Stats[num3];
			}
			for (int num4 = 0; num4 < Math.Min(array2.Length, item4.Vitals.Length); num4++)
			{
				array2[num4] += item4.Vitals[num4];
			}
			if (item4.Effect != ItemEffect.None && item4.EffectPercentage != 0)
			{
				dictionary[item4.Effect] = dictionary.GetValueOrDefault(item4.Effect) + item4.EffectPercentage;
			}
		}
		return new Apports(array, array2, dictionary);
	}

	internal static List<(ItemDescriptor Objet, ItemProperties? Proprietes)> Portes(Player me, ItemDescriptor? remplacant = null, int caseRemplacant = -1)
	{
		List<(ItemDescriptor, ItemProperties)> list = new List<(ItemDescriptor, ItemProperties)>();
		for (int i = 0; i < me.MyEquipment.Length; i++)
		{
			if (remplacant == null || i != remplacant.EquipmentSlot)
			{
				int caseSac;
				ItemDescriptor itemDescriptor = ObjetPorte(me, i, out caseSac);
				if (itemDescriptor != null)
				{
					list.Add((itemDescriptor, ProprietesDe(me, caseSac)));
				}
			}
		}
		if (remplacant != null)
		{
			list.Add((remplacant, ProprietesDe(me, caseRemplacant)));
		}
		return list;
	}

	private void PreparerCaracteristiques(Player me)
	{
		_avant = ApportsDe(Portes(me));
		_essai = null;
		_apres = null;
		if (_selectionSac >= 0 && _selectionSac < me.Inventory.Length && DatabaseObject<ItemDescriptor>.TryGet(me.Inventory[_selectionSac]?.ItemId ?? Guid.Empty, out var databaseObject) && databaseObject.ItemType == ItemType.Equipment && databaseObject.EquipmentSlot >= 0 && databaseObject.EquipmentSlot < me.MyEquipment.Length)
		{
			_essai = databaseObject;
			_apres = ApportsDe(Portes(me, databaseObject, _selectionSac));
		}
	}

	private int EcartStat(Stat stat)
	{
		Apports? apres = _apres;
		if (!apres.HasValue)
		{
			return 0;
		}
		return apres.GetValueOrDefault().Stats[(int)stat] - _avant.Stats[(int)stat];
	}

	private static long Puissance(Player me, int[]? ecartsStats = null, long[]? ecartsVitaux = null)
	{
		int[] array = new int[me.Stat.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = me.Stat[i] + ((ecartsStats != null && i < ecartsStats.Length) ? ecartsStats[i] : 0);
		}
		return PowerLevel.Of(me.Level, array, VitalMax(0), VitalMax(1));
		long VitalMax(int vital)
		{
			return ((vital < me.MaxVital.Length) ? me.MaxVital[vital] : 0) + ((ecartsVitaux != null && vital < ecartsVitaux.Length) ? ecartsVitaux[vital] : 0);
		}
	}

	internal static Color CouleurStat(Stat stat)
	{
		return stat switch
		{
			Stat.Attack => new Color(255, 255, 154, 122), 
			Stat.AbilityPower => new Color(255, 111, 211, 255), 
			Stat.Defense => new Color(255, 111, 227, 161), 
			Stat.MagicResist => new Color(255, 255, 208, 106), 
			_ => new Color(255, 200, 155, 255), 
		};
	}

	internal static int GlypheStat(Stat stat)
	{
		return stat switch
		{
			Stat.Attack => 2, 
			Stat.AbilityPower => 0, 
			Stat.Defense => 6, 
			Stat.MagicResist => 7, 
			_ => 8, 
		};
	}

	internal static string Texte(Dictionary<int, LocalizedString> table, int index)
	{
		if (!table.TryGetValue(index, out LocalizedString value))
		{
			return string.Empty;
		}
		return value.ToString();
	}

	private void DessinerPuissance(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me)
	{
		int num = _d.X(276f);
		string text = Strings.Personnage.PowerHeading.ToString().ToUpperInvariant();
		TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(12f), text, num, _d.Y(556f), _d.Em(0.24f, 12f), A(new Color(255, 150, 225, 255)));
		long num2 = Puissance(me);
		string text2 = SaiyanHud.Amount(num2);
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(56f), text2);
		TechniqueArt.Glow(renderer, num, _d.Y(596f), _d.L(90f), A(TechniqueArt.Cyan, 0.16f));
		TechniqueArt.Styled(renderer, TechniqueArt.Display, Pt(56f), text2, num - point.X / 2 + _d.L(3f), _d.Y(598f) - point.Y / 2 + _d.L(3f), TechniqueArt.Stops.Solid(TechniqueArt.Black), 0.6f * _fade);
		TechniqueArt.Styled(renderer, TechniqueArt.Display, Pt(56f), text2, num - point.X / 2, _d.Y(598f) - point.Y / 2, new TechniqueArt.Stops(TechniqueArt.White, 0.3f, new Color(255, 191, 234, 255), 0.65f, TechniqueArt.Cyan), _fade);
		Apports? apres = _apres;
		long num3;
		if (apres.HasValue)
		{
			Apports valueOrDefault = apres.GetValueOrDefault();
			if (_essai != null)
			{
				num3 = Puissance(me, EcartsStats(valueOrDefault), EcartsVitaux(valueOrDefault)) - num2;
				goto IL_020f;
			}
		}
		num3 = 0L;
		goto IL_020f;
		IL_020f:
		long num4 = num3;
		if (num4 != 0L && _essai != null)
		{
			string text3 = Strings.Personnage.PowerChange.ToString(((num4 > 0) ? "+" : "-") + SaiyanHud.Amount(Math.Abs(num4)), _essai.Name);
			TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(16f), text3, num, _d.Y(642f), A((num4 > 0) ? TechniqueArt.Green : TechniqueArt.Red));
		}
		else
		{
			TechniqueArt.TextCenter(renderer, TechniqueArt.Body, Pt(12f), Strings.Personnage.PowerNote, num, _d.Y(642f), A(TechniqueArt.Dim));
		}
	}

	private int[] EcartsStats(Apports apres)
	{
		return apres.Stats.Select((int valeur, int index) => valeur - _avant.Stats[index]).ToArray();
	}

	private long[] EcartsVitaux(Apports apres)
	{
		return apres.Vitaux.Select((long valeur, int index) => valeur - _avant.Vitaux[index]).ToArray();
	}

	private void DessinerProgression(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, long now)
	{
		Panel(renderer, _d.R(32f, 680f, 488f, 190f));
		Heading(renderer, 50f, 692f, 452f, Strings.Personnage.ProgressionHeading, Strings.Personnage.LevelDetail.ToString(me.Level));
		long num = Math.Max(1L, me.ExperienceToNextLevel);
		float num2 = ((me.ExperienceToNextLevel <= 0) ? 1f : Math.Clamp((float)me.Experience / (float)num, 0f, 1f));
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), Strings.Personnage.ExperienceLabel.ToString().ToUpperInvariant(), _d.X(50f), _d.Y(726f), _d.Em(0.2f, 12f), A(TechniqueArt.Dim));
		string text = ((me.ExperienceToNextLevel <= 0) ? Strings.Character.ExperienceMax.ToString() : (SaiyanHud.Amount(me.Experience) + " / " + SaiyanHud.Amount(num)));
		TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(18f), text, _d.X(502f), _d.Y(726f), A(TechniqueArt.Gold));
		Rectangle rect = _d.R(50f, 740f, 452f, 10f);
		TechniqueArt.Fill(renderer, rect, A(new Color(255, 7, 10, 18)));
		Rectangle rectangle = new Rectangle(rect.X + 1, rect.Y + 1, (int)((float)(rect.Width - 2) * num2), rect.Height - 2);
		if (rectangle.Width > 0)
		{
			TechniqueArt.FillVertical(renderer, rectangle, A(new Color(255, 255, 231, 168)), A(new Color(255, 217, 160, 43)));
			float num3 = (float)(now % 2600) / 2600f;
			Rectangle previous = TechniqueArt.BeginClip(renderer, rectangle);
			TechniqueArt.Image(renderer, "tech_reflet.png", new Rectangle(rectangle.X + _d.L(-60f + 520f * num3), rectangle.Y, _d.L(60f), rectangle.Height), A(TechniqueArt.White, 0.5f));
			TechniqueArt.EndClip(renderer, previous);
		}
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line2));
		KarmaOptions karma = Options.Instance.Karma;
		KarmaTier karmaTier = karma.TierOf(me.Karma);
		Color name = KarmaDisplay.NameColors(karmaTier).Name;
		string text2 = KarmaDisplay.TierName(karmaTier).ToUpperInvariant();
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(26f), text2);
		TechniqueArt.Italic(renderer, TechniqueArt.Display, Pt(26f), text2, _d.X(50f), _d.Y(776f) - point.Y / 2, A(name));
		string text3 = ((me.Karma > 0) ? ("+" + me.Karma.ToString(CultureInfo.InvariantCulture)) : me.Karma.ToString(CultureInfo.InvariantCulture));
		Point point2 = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(22f), text3);
		TechniqueArt.Italic(renderer, TechniqueArt.Display, Pt(22f), text3, _d.X(502f) - point2.X, _d.Y(776f) - point2.Y / 2, A(TechniqueArt.Text));
		Rectangle rectangle2 = _d.R(50f, 798f, 452f, 8f);
		float num4 = Math.Max(1, karma.Maximum - karma.Minimum);
		int[] array = new int[5] { karma.Minimum, karma.CriminalThreshold, karma.SuspectThreshold, karma.HeroThreshold, karma.Maximum };
		KarmaTier[] array2 = new KarmaTier[4]
		{
			KarmaTier.Criminal,
			KarmaTier.Suspect,
			KarmaTier.Neutral,
			KarmaTier.Hero
		};
		for (int i = 0; i < array2.Length; i++)
		{
			int num5 = rectangle2.X + (int)Math.Round((float)(array[i] - karma.Minimum) / num4 * (float)rectangle2.Width);
			int num6 = rectangle2.X + (int)Math.Round((float)(array[i + 1] - karma.Minimum) / num4 * (float)rectangle2.Width);
			Color name2 = KarmaDisplay.NameColors(array2[i]).Name;
			TechniqueArt.Fill(renderer, new Rectangle(num5, rectangle2.Y, Math.Max(1, num6 - num5 - 2), rectangle2.Height), A(name2, (array2[i] == karmaTier) ? 0.95f : 0.35f));
		}
		int num7 = rectangle2.X + (int)(Math.Clamp((float)(me.Karma - karma.Minimum) / num4, 0f, 1f) * (float)rectangle2.Width);
		TechniqueArt.Glow(renderer, num7, (float)rectangle2.Y + (float)rectangle2.Height / 2f, _d.L(12f), A(TechniqueArt.White, 0.4f));
		TechniqueArt.Fill(renderer, new Rectangle(num7 - 1, rectangle2.Y - _d.L(5f), Math.Max(2, _d.L(3f)), rectangle2.Height + _d.L(10f)), A(TechniqueArt.White));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), KarmaDisplay.TierName(KarmaTier.Criminal), rectangle2.X, _d.Y(824f), A(TechniqueArt.Dim));
		TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(12f), KarmaDisplay.TierName(KarmaTier.Hero), rectangle2.Right, _d.Y(824f), A(TechniqueArt.Dim));
	}

	private Stat? DessinerStats(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me)
	{
		Panel(renderer, _d.R(540f, 96f, 568f, 774f));
		int statPoints = me.StatPoints;
		Heading(renderer, 558f, 108f, 532f, Strings.Character.StatsHeading, (statPoints > 0) ? Strings.Character.PointsToSpend.ToString(statPoints) : Strings.Character.NoPoints.ToString(), (statPoints > 0) ? TechniqueArt.Gold : TechniqueArt.Dim);
		if (_essai != null)
		{
			string[] array = Strings.Personnage.PreviewGaps.ToString("\u0001").Split('\u0001');
			int num = _d.X(558f);
			int centerY = _d.Y(143f);
			num += TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), array[0], num, centerY, A(TechniqueArt.Soft));
			num += TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(12f), _essai.Name, num, centerY, A(TechniqueArt.Text));
			if (array.Length > 1)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), array[1], num, centerY, A(TechniqueArt.Soft));
			}
		}
		DessinerPas(renderer, statPoints);
		Stat? result = null;
		Point point = LocalMouse();
		int num2 = Math.Max(60, StatsMontrees.Max((Stat stat2) => me.Stat[(int)stat2]));
		for (int num3 = 0; num3 < StatsMontrees.Length; num3++)
		{
			Stat stat = StatsMontrees[num3];
			float num4 = 168f + (float)num3 * 138f;
			Rectangle rect = _d.R(556f, num4, 536f, 129f);
			Color color = CouleurStat(stat);
			int num5 = me.Stat[(int)stat];
			int num6 = _avant.Stats[(int)stat];
			bool flag = IsHovered && rect.Contains(point.X, point.Y);
			TechniqueArt.Patch(renderer, "tech_chanfrein8.png", rect, A(new Color(225, 12, 17, 28)), 10);
			TechniqueArt.FillHorizontal(renderer, new Rectangle(rect.X + 1, rect.Y + 1, rect.Width * 3 / 5, rect.Height - 2), A(color, flag ? 0.2f : 0.13f), A(color, 0.05f), A(color, 0f));
			TechniqueArt.Patch(renderer, "tech_chanfrein8_bord.png", rect, A(flag ? TechniqueArt.Alpha(color, 0.8f) : TechniqueArt.Line), 10);
			DrawHexEmblem(renderer, _d.R(570f, num4 + 24.5f, 72f, 80f), null, GlypheStat(stat), color, 0.62f);
			string text = Texte(Strings.ItemDescription.Stats, (int)stat).TrimEnd(' ', ':').ToUpperInvariant();
			Point point2 = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(30f), text);
			TechniqueArt.Styled(renderer, TechniqueArt.Display, Pt(30f), text, _d.X(658f), _d.Y(num4 + 34f) - point2.Y / 2, new TechniqueArt.Stops(TechniqueArt.White, 0.35f, TechniqueArt.Mix(color, TechniqueArt.White, 0.6f), 0.7f, color), _fade);
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(13f), Texte(Strings.Character.StatRoles, (int)stat), _d.X(658f), _d.Y(num4 + 62f), A(TechniqueArt.Mix(color, TechniqueArt.White, 0.55f)));
			string text2 = num5.ToString(CultureInfo.InvariantCulture);
			Point point3 = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(50f), text2);
			TechniqueArt.Glow(renderer, (float)_d.X(1006f) - (float)point3.X / 2f, _d.Y(num4 + 44f), _d.L(34f), A(color, 0.16f));
			TechniqueArt.Italic(renderer, TechniqueArt.Display, Pt(50f), text2, _d.X(1006f) - point3.X, _d.Y(num4 + 44f) - point3.Y / 2, A(TechniqueArt.Text));
			int num7 = ((num6 != 0) ? TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(12.5f), Strings.Character.EquipmentShare.ToString(num6), _d.X(1006f), _d.Y(num4 + 84f), A(TechniqueArt.Cyan)) : TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(12.5f), Strings.Personnage.NoEquipmentShare, _d.X(1006f), _d.Y(num4 + 84f), A(TechniqueArt.Dim)));
			int width = Math.Min(_d.L(230f), _d.X(1006f) - num7 - _d.L(16f) - _d.X(658f));
			List<string> list = (base.Details ? TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), Texte(Strings.Character.StatDescriptions, (int)stat), width, 3) : new List<string>());
			float num9;
			int num10;
			if (list.Count <= 2)
			{
				float num8 = 17f;
				num9 = num8;
				num10 = 84;
			}
			else
			{
				float num8 = 14f;
				num9 = num8;
				num10 = 77;
			}
			for (int num11 = 0; num11 < list.Count; num11++)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), list[num11], _d.X(658f), _d.Y(num4 + (float)num10 + (float)num11 * num9), A(TechniqueArt.Dim));
			}
			int num12 = EcartStat(stat);
			if (num12 != 0)
			{
				TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(14f), Signe(num12), _d.X(1006f), _d.Y(num4 + 103f), A((num12 > 0) ? TechniqueArt.Green : TechniqueArt.Red));
			}
			Rectangle rect2 = _d.R(658f, num4 + 113f, 348f, 4f);
			TechniqueArt.Fill(renderer, rect2, A(new Color(255, 7, 10, 18)));
			int num13 = Math.Max(0, num5 - Math.Max(0, num6));
			int num14 = (int)((float)rect2.Width * Math.Clamp((float)num13 / (float)num2, 0f, 1f));
			int num15 = (int)((float)rect2.Width * Math.Clamp((float)Math.Max(0, num6) / (float)num2, 0f, 1f));
			if (num14 > 0)
			{
				TechniqueArt.FillHorizontal(renderer, new Rectangle(rect2.X, rect2.Y, num14, rect2.Height), A(TechniqueArt.Mix(color, TechniqueArt.Black, 0.4f)), A(TechniqueArt.Mix(color, TechniqueArt.Black, 0.7f)), A(color));
			}
			if (num15 > 0)
			{
				TechniqueArt.Fill(renderer, new Rectangle(rect2.X + num14, rect2.Y, Math.Min(num15, rect2.Width - num14), rect2.Height), A(TechniqueArt.Cyan, 0.85f));
			}
			DessinerPlus(renderer, stat, num3, num4, statPoints > 0 && num5 < Options.Instance.Player.MaxStat);
			if (flag)
			{
				result = stat;
			}
		}
		return result;
	}

	private void DessinerPas(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int points)
	{
		string[] array = new string[3]
		{
			Strings.Character.StatStepOne,
			Strings.Character.StatStepFive,
			Strings.Character.StatStepMax
		};
		int centerY = _d.Y(143f);
		for (int i = 0; i < Pas.Length; i++)
		{
			Rectangle rectangle = _d.R(912 + i * 62, 130f, 54f, 26f);
			int num = _d.L(3.5f);
			Rectangle rect = new Rectangle(rectangle.X - num, rectangle.Y, rectangle.Width + num * 2, rectangle.Height);
			int montant = Pas[i];
			bool flag = _pas == montant;
			string key = $"pas:{i}";
			bool flag2 = IsHot(key);
			if (flag)
			{
				TechniqueArt.Image(renderer, "tech_pip.png", rect, A(TechniqueArt.Mix(TechniqueArt.Ki, TechniqueArt.Black, 0.55f)));
				TechniqueArt.Image(renderer, "tech_pip_haut.png", rect, A(TechniqueArt.Mix(TechniqueArt.Ki, TechniqueArt.White, 0.85f)));
				TechniqueArt.Image(renderer, "tech_pip_bord.png", rect, A(TechniqueArt.Mix(TechniqueArt.Ki, TechniqueArt.White, 0.8f)));
			}
			else
			{
				TechniqueArt.Image(renderer, "tech_pip.png", rect, A(new Color(255, 8, 12, 21)));
				TechniqueArt.Image(renderer, "tech_pip_bord.png", rect, A(flag2 ? TechniqueArt.Soft : TechniqueArt.Line2));
			}
			string text = array[i];
			Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(15f), text);
			TechniqueArt.Italic(renderer, TechniqueArt.Display, Pt(15f), text, rectangle.X + (rectangle.Width - point.X) / 2, rectangle.Y + (rectangle.Height - point.Y) / 2, A(flag ? new Color(255, 20, 10, 2) : (flag2 ? TechniqueArt.Text : TechniqueArt.Dim)));
			AddHit(rect, key, delegate
			{
				_pas = montant;
			});
		}
		TechniqueArt.SpacedRight(renderer, TechniqueArt.Label, Pt(12f), Strings.Character.StepLabel.ToString().ToUpperInvariant(), _d.X(898f), centerY, _d.Em(0.2f, 12f), A((points > 0) ? TechniqueArt.Soft : TechniqueArt.Dim));
	}

	private void DessinerPlus(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Stat stat, int index, float haut, bool possible)
	{
		Rectangle rectangle = _d.R(1022f, haut + 41.5f, 54f, 46f);
		int num = _d.L(6f);
		Rectangle rect = new Rectangle(rectangle.X - num, rectangle.Y, rectangle.Width + num * 2, rectangle.Height);
		string key = $"plus:{index}";
		bool flag = possible && IsHot(key);
		if (possible)
		{
			TechniqueArt.GlowRect(renderer, new Rectangle(rect.X - _d.L(14f), rect.Y - _d.L(14f), rect.Width + _d.L(28f), rect.Height + _d.L(28f)), A(new Color(255, 255, 170, 60), flag ? 0.7f : 0.35f), additive: false);
			TechniqueArt.Image(renderer, "tech_pip.png", rect, A(flag ? new Color(255, 255, 170, 56) : new Color(255, 255, 138, 26)));
			TechniqueArt.Image(renderer, "tech_pip_haut.png", rect, A(new Color(255, 255, 241, 192)));
		}
		else
		{
			TechniqueArt.Image(renderer, "tech_pip.png", rect, A(new Color(255, 18, 24, 40)));
			TechniqueArt.Image(renderer, "tech_pip_bord.png", rect, A(TechniqueArt.Line2));
		}
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(30f), "+");
		TechniqueArt.Italic(renderer, TechniqueArt.Display, Pt(30f), "+", rectangle.X + (rectangle.Width - point.X) / 2, rectangle.Y + (rectangle.Height - point.Y) / 2, A(possible ? new Color(255, 20, 10, 2) : TechniqueArt.Dim));
		if (possible)
		{
			AddHit(rect, key, delegate
			{
				PacketSender.SendUpgradeStat((byte)stat, _pas);
			});
		}
	}

	private void DessinerInfobulleStat(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Stat? stat)
	{
		if (!stat.HasValue)
		{
			return;
		}
		Stat valueOrDefault = stat.GetValueOrDefault();
		int index = (int)valueOrDefault;
		Color color = CouleurStat(valueOrDefault);
		int num = _d.L(320f);
		int num2 = num - _d.L(24f);
		List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), Texte(Strings.Character.StatDescriptions, index), num2, 4);
		List<(string, string)> list2 = Texte(Strings.Character.StatDetails, index).Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(delegate(string detail)
		{
			int num7 = detail.IndexOf('=');
			if (num7 < 0)
			{
				return (Cle: detail, Valeur: string.Empty);
			}
			string item = detail.Substring(0, num7).Trim();
			int num8 = num7 + 1;
			return (Cle: item, Valeur: detail.Substring(num8, detail.Length - num8).Trim());
		}).ToList();
		int num3 = _d.L(52f) + list.Count * _d.L(16f) + ((list2.Count > 0) ? (_d.L(14f) + list2.Count * _d.L(18f)) : 0) + _d.L(10f);
		int num4 = Array.IndexOf(StatsMontrees, valueOrDefault);
		int y = Math.Clamp(_d.Y(168f + (float)num4 * 138f), _d.Y(100f), _d.Y(866f) - num3);
		Rectangle rect = new Rectangle(_d.X(1116f), y, num, num3);
		TechniqueArt.Fill(renderer, rect, A(new Color(248, 6, 8, 14)));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Alpha(color, 0.6f)));
		TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, Math.Max(2, _d.L(3f)), rect.Height), A(color));
		int num5 = rect.X + _d.L(14f);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(15f), Texte(Strings.ItemDescription.Stats, index).TrimEnd(' ', ':'), num5, rect.Y + _d.L(17f), A(TechniqueArt.Text));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(12f), Texte(Strings.Character.StatRoles, index), num5, rect.Y + _d.L(35f), A(TechniqueArt.Mix(color, TechniqueArt.White, 0.55f)));
		int num6 = rect.Y + _d.L(52f);
		foreach (string item2 in list)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), item2, num5, num6 + _d.L(8f), A(TechniqueArt.Soft));
			num6 += _d.L(16f);
		}
		if (list2.Count == 0)
		{
			return;
		}
		TechniqueArt.Fill(renderer, new Rectangle(num5, num6 + _d.L(6f), num2, 1), A(TechniqueArt.White, 0.08f));
		num6 += _d.L(14f);
		foreach (var (text, text2) in list2)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), text, num5, num6 + _d.L(9f), A(TechniqueArt.Soft));
			TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(12f), text2, num5 + num2, num6 + _d.L(9f), A(TechniqueArt.Text));
			num6 += _d.L(18f);
		}
	}

	private void DessinerColonneDroite(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, long now)
	{
		Panel(renderer, _d.R(1128f, 96f, 440f, 774f));
		Heading(renderer, 1146f, 108f, 404f, Strings.Character.LifeAndKi, null);
		DessinerVitaux(renderer, me, now);
		DessinerBonus(renderer);
		DessinerFormes(renderer, me);
	}

	private void DessinerVitaux(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, long now)
	{
		(string, Color, Color)[] array = new(string, Color, Color)[2]
		{
			(Strings.Personnage.Health, new Color(255, 255, 132, 104), new Color(255, 150, 20, 38)),
			(Strings.Personnage.Ki, new Color(255, 170, 216, 255), new Color(255, 30, 62, 184))
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, Color, Color) tuple = array[i];
			string item = tuple.Item1;
			Color item2 = tuple.Item2;
			Color item3 = tuple.Item3;
			float num = 1148f + (float)i * 210f;
			int num2 = _d.Y(150f);
			long num3 = ((i < me.Vital.Length) ? me.Vital[i] : 0);
			long num4 = Math.Max(1L, (i < me.MaxVital.Length) ? me.MaxVital[i] : 1);
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), item.ToUpperInvariant(), _d.X(num), num2, _d.Em(0.18f, 12f), A(TechniqueArt.Soft));
			string text = "/ " + SaiyanHud.Amount(num4);
			int num5 = TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(13f), text, _d.X(num + 194f), num2 + _d.L(3f), A(TechniqueArt.Dim));
			string text2 = SaiyanHud.Amount(num3);
			Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(26f), text2);
			TechniqueArt.Italic(renderer, TechniqueArt.Display, Pt(26f), text2, _d.X(num + 194f) - num5 - _d.L(6f) - point.X, num2 - point.Y / 2, A(TechniqueArt.Text));
			Rectangle rectangle = _d.R(num, 166f, 194f, 18f);
			int num6 = _d.L(9f);
			float num7 = Math.Clamp((float)num3 / (float)num4, 0f, 1f);
			for (int j = 0; j < rectangle.Height; j++)
			{
				float num8 = (float)j / (float)Math.Max(1, rectangle.Height - 1);
				float num9 = (float)rectangle.X + (float)num6 * (1f - num8);
				float num10 = (float)rectangle.Right - (float)num6 * num8;
				HudShapes.Span(renderer, num9, num10, rectangle.Y + j, 1, A(new Color(255, 7, 10, 18)));
				float right = num9 + (num10 - num9) * num7;
				HudShapes.Span(renderer, num9, right, rectangle.Y + j, 1, A(SaiyanHud.Lerp(item2, item3, num8)));
			}
			HudShapes.Span(renderer, rectangle.X + num6, rectangle.Right, rectangle.Y, 1, A(TechniqueArt.Line2));
			HudShapes.Span(renderer, rectangle.X, rectangle.Right - num6, rectangle.Bottom - 1, 1, A(TechniqueArt.Line2));
		}
	}

	private void DessinerBonus(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), Strings.Personnage.BonusesHeading.ToString().ToUpperInvariant(), _d.X(1148f), _d.Y(222f), _d.Em(0.2f, 12f), A(TechniqueArt.Dim));
		TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(12f), Strings.Personnage.BonusesDetail, _d.X(1552f), _d.Y(222f), A(TechniqueArt.Dim));
		for (int i = 0; i < EffetsMontres.Length; i++)
		{
			ItemEffect itemEffect = EffetsMontres[i];
			Rectangle rect = _d.R(1148f, 236f + (float)i * 48f, 404f, 44f);
			int valueOrDefault = _avant.Effets.GetValueOrDefault(itemEffect);
			TechniqueArt.Fill(renderer, rect, A(new Color(180, 7, 10, 18)));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
			LocalizedString value;
			string text = ((itemEffect == ItemEffect.EXP) ? Strings.Character.ExperienceBonus.ToString() : (Strings.ItemDescription.BonusEffects.TryGetValue((int)itemEffect, out value) ? Libelle(value) : itemEffect.ToString()));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(13f), text, rect.X + _d.L(10f), rect.Y + _d.L(15f), A((valueOrDefault != 0) ? TechniqueArt.Text : TechniqueArt.Soft));
			if (base.Details && Strings.Personnage.EffectDescriptions.TryGetValue((int)itemEffect, out LocalizedString value2))
			{
				string text2 = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), value2, rect.Width - _d.L(78f), 1).FirstOrDefault();
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), text2, rect.X + _d.L(10f), rect.Y + _d.L(31f), A(TechniqueArt.Dim));
			}
			string text3 = ((valueOrDefault != 0) ? Signe(valueOrDefault, pourcent: true) : "0 %");
			int num = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(18f), text3, rect.Right - _d.L(10f), rect.Y + rect.Height / 2, A((valueOrDefault != 0) ? TechniqueArt.Text : TechniqueArt.Dim));
			Apports? apres = _apres;
			if (apres.HasValue)
			{
				int num2 = apres.GetValueOrDefault().Effets.GetValueOrDefault(itemEffect) - valueOrDefault;
				if (num2 != 0)
				{
					TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(14f), Signe(num2, pourcent: true), rect.Right - _d.L(18f) - num, rect.Y + rect.Height / 2, A((num2 > 0) ? TechniqueArt.Green : TechniqueArt.Red));
				}
			}
		}
	}

	private static Color CouleurForme(string? nom)
	{
		string text = Normaliser(nom);
		if (text.Contains("kaioken"))
		{
			return new Color(255, 255, 70, 60);
		}
		if (text.Contains("instinct"))
		{
			return new Color(255, 214, 222, 240);
		}
		if (text.Contains("blue") || text.Contains("bleu"))
		{
			return new Color(255, 90, 210, 255);
		}
		if (text.Contains("god") || text.Contains("dieu") || text.Contains("rose"))
		{
			return new Color(255, 255, 110, 150);
		}
		if (!text.Contains(" 2") && !text.Contains(" 3"))
		{
			return new Color(255, 255, 196, 70);
		}
		return new Color(255, 255, 236, 160);
	}

	private void DessinerFormes(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me)
	{
		List<(SpellDescriptor, int)> list = new List<(SpellDescriptor, int)>();
		Spell[] spells = me.Spells;
		foreach (Spell spell in spells)
		{
			if (spell != null && spell.Id != Guid.Empty && DatabaseObject<SpellDescriptor>.TryGet(spell.Id, out var databaseObject) && SpellMastery.FamilyOf(databaseObject) == TechniqueFamily.Transformation)
			{
				list.Add((databaseObject, SpellMastery.Clamp(spell.Rank)));
			}
		}
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), Strings.Personnage.FormsHeading.ToString().ToUpperInvariant(), _d.X(1148f), _d.Y(598f), _d.Em(0.2f, 12f), A(TechniqueArt.Dim));
		TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(12f), Strings.Personnage.FormsDetail, _d.X(1552f), _d.Y(598f), A(TechniqueArt.Dim));
		if (list.Count == 0)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), Strings.Character.NoForms, _d.X(1148f), _d.Y(636f), A(TechniqueArt.Dim));
			return;
		}
		SpellDescriptor spellDescriptor = null;
		foreach (IStatus item3 in me.Status)
		{
			if (item3.Type == SpellEffect.Transform && DatabaseObject<SpellDescriptor>.TryGet(item3.SpellId, out var databaseObject2))
			{
				spellDescriptor = databaseObject2;
			}
		}
		int num = ((list.Count > 4) ? 3 : list.Count);
		for (int j = 0; j < num; j++)
		{
			(SpellDescriptor, int) tuple = list[j];
			SpellDescriptor item = tuple.Item1;
			int item2 = tuple.Item2;
			Color color = CouleurForme(item.Name);
			float num2 = 614f + (float)j * 55f;
			Rectangle rect = _d.R(1148f, num2, 404f, 50f);
			bool flag = spellDescriptor?.Id == item.Id;
			TechniqueArt.FillHorizontal(renderer, rect, A(color, 0.14f), A(new Color(200, 7, 10, 18)), A(new Color(180, 7, 10, 18)));
			TechniqueArt.Outline(renderer, rect, A(flag ? TechniqueArt.Alpha(TechniqueArt.Gold, 0.8f) : TechniqueArt.Line));
			DrawHexEmblem(renderer, _d.R(1158f, num2 + 3f, 40f, 44f), Globals.ContentManager.GetTexture(TextureType.Spell, item.Icon), -1, color, 0.62f);
			Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(19f), item.Name);
			TechniqueArt.Italic(renderer, TechniqueArt.Display, Pt(19f), item.Name, _d.X(1210f), _d.Y(num2 + 18f) - point.Y / 2, A(TechniqueArt.Text));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), Strings.Personnage.FormRank.ToString(item2, 5), _d.X(1210f), _d.Y(num2 + 36f), A(TechniqueArt.Dim));
			if (flag)
			{
				string text = Strings.Character.FormActive.ToString().ToUpperInvariant();
				float spacing = _d.Em(0.14f, 12f);
				int w = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(12f), text, spacing) + _d.L(12f);
				Rectangle rect2 = new Rectangle(_d.X(1210f) + point.X + _d.L(10f), _d.Y(num2 + 18f) - _d.L(8f), w, _d.L(16f));
				TechniqueArt.Patch(renderer, "tech_biais4.png", rect2, A(TechniqueArt.Gold), 5);
				TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), text, rect2.X + _d.L(6f), rect2.Y + rect2.Height / 2, spacing, A(new Color(255, 26, 18, 6)));
			}
			for (int k = 1; k <= 5; k++)
			{
				Rectangle rect3 = _d.R(1542 - (5 - k + 1) * 16, num2 + 22f, 13f, 6f);
				TechniqueArt.Image(renderer, "tech_pip_mini.png", rect3, A((k <= item2) ? color : new Color(255, 10, 14, 24)));
				if (k > item2)
				{
					TechniqueArt.Image(renderer, "tech_pip_mini_bord.png", rect3, A(TechniqueArt.Line2));
				}
			}
		}
		if (num < list.Count)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), Strings.Personnage.FormsMore.ToString(list.Count - num), _d.X(1148f), _d.Y(614f + (float)num * 55f + 14f), A(TechniqueArt.Soft));
		}
	}

	public PersonnageScreen(Canvas gameCanvas)
		: base(gameCanvas, "PersonnageScreen")
	{
		_gameCanvas = gameCanvas;
		BuildRecherche();
	}

	public void Open(Onglet onglet)
	{
		_onglet = ((onglet == Onglet.Equipement) ? Onglet.Caracteristiques : onglet);
		FitCanvas();
		if (base.IsHidden)
		{
			Show();
		}
	}

	public override void Show()
	{
		base.Show();
		_recherche.IsHidden = _onglet != Onglet.Equipement;
	}

	public override void Hide()
	{
		if (Globals.CanCloseInventory)
		{
			AnnulerGlisser();
			_recherche.Blur();
			base.Hide();
		}
	}

	public void Update()
	{
		if (base.IsHidden)
		{
			return;
		}
		FitCanvas();
		bool flag = _onglet == Onglet.Equipement;
		if (_recherche.IsHidden == flag)
		{
			_recherche.IsHidden = !flag;
			if (!flag)
			{
				_recherche.Blur();
			}
		}
		PlacerRecherche();
		CorrigerSelection();
	}

	protected override void DrawScreen(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		Player me = Globals.Me;
		if (me != null)
		{
			_cases.Clear();
			_casesEmplacements.Clear();
			_casesRaccourcis.Clear();
			bool flag = _onglet == Onglet.Equipement;
			DrawBackdrop(renderer, TechniqueArt.Ki);
			Gauge gauge = new Gauge(Strings.Personnage.Zeni, SaiyanHud.Amount(me.Zeni), TechniqueArt.Gold);
			string title = Strings.Personnage.Title;
			string thread = Fil(me);
			IReadOnlyList<Gauge> gauges;
			if (!flag)
			{
				IReadOnlyList<Gauge> readOnlyList = new global::_003C_003Ez__ReadOnlySingleElementList<Gauge>(gauge);
				gauges = readOnlyList;
			}
			else
			{
				IReadOnlyList<Gauge> readOnlyList = new global::_003C_003Ez__ReadOnlyArray<Gauge>(new Gauge[2]
				{
					gauge,
					new Gauge(Strings.Inventaire.ChargeLabel, Strings.Inventaire.ChargeAmount.ToString(SaiyanHud.Amount(me.LireLaCharge().Porte), SaiyanHud.Amount(me.LireLaCharge().Capacite)), me.LireLaCharge().Surcharge ? TechniqueArt.Red : TechniqueArt.Text)
				});
				gauges = readOnlyList;
			}
			DrawTopBar(renderer, title, thread, gauges);
			DessinerOnglets(renderer, me);
			if (flag)
			{
				DessinerScene(renderer, me, now);
				DessinerPanoplies(renderer, me);
				DessinerSac(renderer, me, now);
				DessinerFiche(renderer, me, now);
				DessinerGlisser(renderer, me);
			}
			else if (_onglet == Onglet.Personnalisation)
			{
				_essaiTitre = null;
				_essaiOrnement = null;
				List<Choix> titres = Titres(me);
				List<Choix> ornements = Ornements(me);
				DessinerTitres(renderer, titres);
				DessinerOrnements(renderer, me, ornements);
				DessinerScene(renderer, me, now);
				DessinerPlaquePortee(renderer, me, titres, ornements);
			}
			else
			{
				PreparerCaracteristiques(me);
				DessinerScene(renderer, me, now);
				DessinerProgression(renderer, me, now);
				Stat? stat = DessinerStats(renderer, me);
				DessinerColonneDroite(renderer, me, now);
				DessinerInfobulleStat(renderer, stat);
			}
		}
	}

	private static string Fil(Player me)
	{
		ClassDescriptor databaseObject;
		string text = (DatabaseObject<ClassDescriptor>.TryGet(me.Class, out databaseObject) ? Traduction.Contenu(databaseObject.Name) : string.Empty);
		return Strings.Personnage.Thread.ToString(me.Name, text, me.Level);
	}

	private static int PlacesOccupees(Player me)
	{
		return me.ObjetsDansLeSac();
	}

	private void DessinerOnglets(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me)
	{
		string text = Strings.Personnage.Title.ToString().ToUpperInvariant();
		string text2 = Fil(me).ToUpperInvariant();
		int num = _d.X(36f) + TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(40f), text).X + _d.L(26f) + TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(12f), text2, _d.Em(0.22f, 12f)) + _d.L(34f);
		int num2 = _d.Y(37f);
		int num3 = _d.L(36f);
		(Onglet, string, string)[] array = new(Onglet, string, string)[2]
		{
			(Onglet.Caracteristiques, Strings.Personnage.TabStats, "C"),
			(Onglet.Personnalisation, Strings.Personnage.TabCustomization, null)
		};
		(Onglet, string, string)[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			(Onglet, string, string) tuple = array2[i];
			Onglet onglet = tuple.Item1;
			string item = tuple.Item2;
			string item2 = tuple.Item3;
			string text3 = item.ToUpperInvariant();
			int num4 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(12f), text3, _d.Em(0.2f, 12f));
			int w = num4 + _d.L(36f) + ((item2 != null) ? _d.L(26f) : 0);
			Rectangle rect = new Rectangle(num, num2 - num3 / 2, w, num3);
			bool flag = onglet == _onglet;
			string key = $"onglet:{onglet}";
			bool flag2 = IsHot(key);
			TechniqueArt.Patch(renderer, "tech_chanfrein8.png", rect, A(new Color(flag ? 235 : 180, 12, 17, 28)), 10);
			if (flag)
			{
				TechniqueArt.Patch(renderer, "tech_chanfrein8_degrade.png", rect, A(TechniqueArt.Mix(TechniqueArt.Ki, new Color(255, 20, 27, 42), 0.35f)), 10);
			}
			TechniqueArt.Patch(renderer, "tech_chanfrein8_bord.png", rect, A(flag ? TechniqueArt.Alpha(TechniqueArt.Ki, 0.8f) : (flag2 ? TechniqueArt.Line2 : TechniqueArt.Line)), 10);
			Color color = (flag ? TechniqueArt.White : (flag2 ? TechniqueArt.Text : TechniqueArt.Soft));
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), text3, rect.X + _d.L(18f), num2, _d.Em(0.2f, 12f), A(color));
			if (item2 != null)
			{
				Rectangle rect2 = new Rectangle(rect.Right - _d.L(18f) - _d.L(18f), num2 - _d.L(9f), _d.L(18f), _d.L(18f));
				TechniqueArt.Outline(renderer, rect2, A(TechniqueArt.Line2));
				TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(12f), item2, rect2.X + rect2.Width / 2, rect2.Y + rect2.Height / 2, A(TechniqueArt.Soft));
			}
			if (!flag)
			{
				AddHit(rect, key, delegate
				{
					ChoisirOnglet(onglet);
				});
			}
			num = rect.Right + _d.L(8f);
		}
		DrawDetailsToggleLeft(renderer, num + _d.L(8f), num2);
	}

	private void ChoisirOnglet(Onglet onglet)
	{
		AnnulerGlisser();
		_onglet = onglet;
	}

	private void CorrigerSelection()
	{
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		if (_selectionSac < 0)
		{
			goto IL_0071;
		}
		if (_selectionSac < me.Inventory.Length)
		{
			Guid? guid = me.Inventory[_selectionSac]?.ItemId;
			if (guid.HasValue)
			{
				Guid valueOrDefault = guid.GetValueOrDefault();
				if (!(valueOrDefault == Guid.Empty))
				{
					goto IL_0071;
				}
			}
		}
		_selectionSac = -1;
		goto IL_009a;
		IL_009a:
		if (_selectionEmplacement >= 0 && ObjetPorte(me, _selectionEmplacement, out var _) == null)
		{
			_selectionEmplacement = -1;
		}
		return;
		IL_0071:
		if (_selectionSac >= 0)
		{
			int num = Array.IndexOf(me.MyEquipment, _selectionSac);
			if (num >= 0)
			{
				SelectionnerEmplacement(num);
			}
		}
		goto IL_009a;
	}

	private void Selectionner(int caseSac)
	{
		_selectionSac = caseSac;
		_selectionEmplacement = -1;
	}

	private void SelectionnerEmplacement(int emplacement)
	{
		_selectionEmplacement = emplacement;
		_selectionSac = -1;
	}

	protected override void OnMouseDown(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseDown(mouseButton, mousePosition, userAction);
		if (mouseButton == MouseButton.Left)
		{
			CommencerAppui(CanvasPosToLocal(mousePosition));
		}
	}

	protected override void OnMouseMoved(int x, int y, int dx, int dy)
	{
		base.OnMouseMoved(x, y, dx, dy);
		SuivreAppui(LocalMouse());
	}

	protected override void OnMouseClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		if (mouseButton == MouseButton.Left && _glisse)
		{
			Lacher(CanvasPosToLocal(mousePosition));
			return;
		}
		AnnulerGlisser();
		base.OnMouseClicked(mouseButton, mousePosition, userAction);
	}

	protected override void OnMouseUp(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseUp(mouseButton, mousePosition, userAction);
		if (mouseButton == MouseButton.Left && _glisse)
		{
			Lacher(CanvasPosToLocal(mousePosition));
		}
		AnnulerGlisser();
	}

	protected override void OnMouseDoubleClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseDoubleClicked(mouseButton, mousePosition, userAction);
		if (mouseButton != MouseButton.Left)
		{
			return;
		}
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		Point local = CanvasPosToLocal(mousePosition);
		int? num = CaseSacSous(local);
		if (num.HasValue)
		{
			int valueOrDefault = num.GetValueOrDefault();
			Selectionner(valueOrDefault);
			me.TryUseItem(valueOrDefault);
			return;
		}
		num = EmplacementSous(local);
		if (num.HasValue)
		{
			int valueOrDefault2 = num.GetValueOrDefault();
			if (ObjetPorte(me, valueOrDefault2, out var _) != null)
			{
				PacketSender.SendUnequipItem(valueOrDefault2);
			}
		}
	}

	protected override void OnEmptyClick(MouseButton mouseButton, Point local)
	{
		if (mouseButton != MouseButton.Right)
		{
			return;
		}
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		int? num = CaseSacSous(local);
		if (num.HasValue)
		{
			int valueOrDefault = num.GetValueOrDefault();
			Selectionner(valueOrDefault);
			return;
		}
		num = EmplacementSous(local);
		if (num.HasValue)
		{
			int valueOrDefault2 = num.GetValueOrDefault();
			if (ObjetPorte(me, valueOrDefault2, out var _) != null)
			{
				SelectionnerEmplacement(valueOrDefault2);
			}
		}
	}

	private ItemDescriptor? ObjetChoisi(Player me, out int caseSac, out int emplacement)
	{
		caseSac = -1;
		emplacement = -1;
		if (_selectionSac >= 0 && _selectionSac < me.Inventory.Length)
		{
			caseSac = _selectionSac;
			if (!DatabaseObject<ItemDescriptor>.TryGet(me.Inventory[caseSac]?.ItemId ?? Guid.Empty, out var databaseObject))
			{
				return null;
			}
			return databaseObject;
		}
		if (_selectionEmplacement >= 0)
		{
			emplacement = _selectionEmplacement;
			return ObjetPorte(me, emplacement, out caseSac);
		}
		return null;
	}

	private void DessinerFiche(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, long now)
	{
		Panel(renderer, _d.R(1128f, 96f, 440f, 774f));
		int caseSac;
		int emplacement;
		ItemDescriptor itemDescriptor = ObjetChoisi(me, out caseSac, out emplacement);
		int num = ((caseSac >= 0) ? (me.Inventory[caseSac]?.Quantity ?? 0) : 0);
		string detail = ((itemDescriptor == null) ? null : ((emplacement >= 0) ? Strings.Personnage.SheetWorn.ToString() : ((num > 1) ? $"×{num}" : null)));
		Heading(renderer, 1146f, 108f, 404f, Strings.Personnage.SheetHeading, detail);
		if (itemDescriptor == null)
		{
			TechniqueArt.TextCenter(renderer, TechniqueArt.Body, Pt(13f), Strings.Personnage.SheetEmpty, _d.X(1348f), _d.Y(460f), A(TechniqueArt.Dim));
			return;
		}
		ItemProperties itemProperties = ProprietesDe(me, caseSac);
		int rarete = JetDObjet.Rarete(itemDescriptor, itemProperties);
		Color couleur = CouleurRarete(rarete);
		float num2 = DessinerIdentite(renderer, itemDescriptor, itemProperties, rarete, couleur);
		ItemDescriptor itemDescriptor2 = null;
		int caseSac2 = -1;
		if (emplacement < 0 && itemDescriptor.ItemType == ItemType.Equipment)
		{
			ItemDescriptor itemDescriptor3 = ObjetPorte(me, itemDescriptor.EquipmentSlot, out caseSac2);
			if (itemDescriptor3 != null && caseSac2 != caseSac)
			{
				itemDescriptor2 = itemDescriptor3;
				DessinerComparaison(renderer, itemDescriptor3, num2);
				num2 += 58f;
			}
		}
		Rectangle area = _d.R(1128f, num2, 440f, 788f - num2);
		Rectangle previous = TechniqueArt.BeginClip(renderer, area);
		if (itemDescriptor.ItemType == ItemType.Equipment)
		{
			num2 = DessinerLignes(renderer, LignesEquipement(itemDescriptor, itemProperties, itemDescriptor2, ProprietesDe(me, caseSac2)), itemDescriptor2 != null, Strings.Personnage.ColumnStat, num2);
		}
		else if (itemDescriptor.ItemType == ItemType.Consumable)
		{
			num2 = DessinerLignes(renderer, LignesConsommable(itemDescriptor), avecEcart: false, Strings.Personnage.ColumnEffect, num2);
		}
		num2 = DessinerBlocPanoplie(renderer, me, itemDescriptor, itemDescriptor2, emplacement, num2);
		num2 = DessinerDescription(renderer, itemDescriptor, num2);
		if (itemDescriptor.ItemType != ItemType.Event)
		{
			DessinerSources(renderer, itemDescriptor, num2);
		}
		TechniqueArt.EndClip(renderer, previous);
		DessinerActions(renderer, me, itemDescriptor, caseSac, emplacement);
	}

	private float DessinerIdentite(Intersect.Client.Framework.Gwen.Renderer.Base renderer, ItemDescriptor objet, ItemProperties? proprietes, int rarete, Color couleur)
	{
		Rectangle outer = _d.R(1148f, 136f, 104f, 116f);
		TechniqueArt.Glow(renderer, (float)outer.X + (float)outer.Width / 2f, (float)outer.Y + (float)outer.Height / 2f, _d.L(74f), A(couleur, 0.3f));
		DrawHexEmblem(renderer, outer, Globals.ContentManager.GetTexture(TextureType.Item, objet.Icon), -1, couleur, 0.62f);
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), TypeDe(objet).ToUpperInvariant(), _d.X(1268f), _d.Y(146f), _d.Em(0.2f, 12f), A(TechniqueArt.Soft));
		List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Display, Pt(34f), objet.Name.ToUpperInvariant(), _d.L(280f), 2);
		float num = 158f;
		TechniqueArt.Stops stops = new TechniqueArt.Stops(TechniqueArt.White, 0.3f, TechniqueArt.Mix(couleur, TechniqueArt.White, 0.7f), 0.7f, couleur);
		foreach (string item in list)
		{
			TechniqueArt.Styled(renderer, TechniqueArt.Display, Pt(34f), item, _d.X(1268f), _d.Y(num), stops, _fade);
			num += 34f;
		}
		num += 22f;
		double? num2 = JetDObjet.Qualite(objet, proprietes);
		int num3 = JetDObjet.Plafond(objet);
		int x = _d.X(1268f) + TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(13f), (rarete > 0) ? NomRarete(rarete) : ((string)Strings.Personnage.Unique), _d.X(1268f), _d.Y(num), A(couleur));
		if (num2.HasValue)
		{
			double valueOrDefault = num2.GetValueOrDefault();
			if (num3 > 0)
			{
				string text = ((int)Math.Round(valueOrDefault * 100.0)).ToString(CultureInfo.InvariantCulture);
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(13f), " · " + Strings.Personnage.Roll.ToString(text), x, _d.Y(num), A(TechniqueArt.Soft));
				Rectangle rect = _d.R(1268f, num + 12f, 280f, 6f);
				TechniqueArt.Fill(renderer, rect, A(new Color(255, 7, 10, 18)));
				TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line2));
				int num4 = (int)((double)(rect.Width - 2) * Math.Clamp(valueOrDefault, 0.0, 1.0));
				if (num4 > 0)
				{
					TechniqueArt.FillHorizontal(renderer, new Rectangle(rect.X + 1, rect.Y + 1, num4, rect.Height - 2), A(TechniqueArt.Mix(couleur, TechniqueArt.Black, 0.4f)), A(TechniqueArt.Mix(couleur, TechniqueArt.Black, 0.7f)), A(couleur));
				}
				double[] array = JetDObjet.Bornes(num3);
				foreach (double num5 in array)
				{
					TechniqueArt.Fill(renderer, new Rectangle(rect.X + (int)((double)rect.Width * num5), rect.Y - _d.L(3f), 1, rect.Height + _d.L(6f)), A(TechniqueArt.White, 0.35f));
				}
				string text2 = ((objet.Price > 0) ? SaiyanHud.Amount(objet.Price) : "0");
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), Strings.Personnage.Ceiling.ToString(NomRarete(num3), text2), _d.X(1268f), _d.Y(num + 34f), A(TechniqueArt.Dim));
				num += 44f;
				goto IL_0548;
			}
		}
		string text3 = ((objet.Price > 0) ? Strings.Personnage.Price.ToString(SaiyanHud.Amount(objet.Price)) : Strings.Personnage.NoPrice.ToString());
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), text3, _d.X(1268f), _d.Y(num + 20f), A(TechniqueArt.Dim));
		num += 30f;
		goto IL_0548;
		IL_0548:
		return Math.Max(252f, num) + 14f;
	}

	private static string TypeDe(ItemDescriptor objet)
	{
		switch (objet.ItemType)
		{
		case ItemType.Equipment:
		{
			List<string> slots = Options.Instance.Equipment.Slots;
			string text = ((objet.EquipmentSlot >= 0 && objet.EquipmentSlot < slots.Count) ? slots[objet.EquipmentSlot] : string.Empty);
			return Strings.Personnage.TypeEquipment.ToString(text).TrimEnd(' ', '·');
		}
		case ItemType.Consumable:
			return Strings.Personnage.TypeConsumable;
		case ItemType.Spell:
			return Strings.Personnage.TypeTechnique;
		case ItemType.Event:
			return Strings.Personnage.TypeQuest;
		case ItemType.Bag:
			return Strings.Personnage.TypeBag;
		default:
			return Strings.Personnage.TypeResource;
		}
	}

	internal static string NomRarete(int rarete)
	{
		string rarityName;
		LocalizedString value;
		return (Options.Instance.Items.TryGetRarityName(rarete, out rarityName) && Strings.ItemDescription.Rarity.TryGetValue(rarityName, out value) && !string.IsNullOrWhiteSpace(value)) ? value : Strings.Personnage.Unique;
	}

	private void DessinerComparaison(Intersect.Client.Framework.Gwen.Renderer.Base renderer, ItemDescriptor porte, float y)
	{
		Rectangle rect = _d.R(1148f, y, 400f, 44f);
		TechniqueArt.Fill(renderer, rect, A(new Color(180, 7, 10, 18)));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
		int num = _d.L(28f);
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Item, porte.Icon);
		if (texture != null)
		{
			SaiyanHud.Draw(renderer, texture, rect.X + _d.L(10f), rect.Y + (rect.Height - num) / 2, num, num, A(porte.Color));
		}
		string[] array = Strings.Personnage.ComparedTo.ToString("\u0001").Split('\u0001');
		int num2 = rect.X + _d.L(48f);
		int centerY = rect.Y + rect.Height / 2;
		num2 += TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), array[0], num2, centerY, A(TechniqueArt.Soft));
		num2 += TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(13f), porte.Name, num2, centerY, A(TechniqueArt.Text));
		if (array.Length > 1)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), array[1], num2, centerY, A(TechniqueArt.Soft));
		}
	}

	internal static string Libelle(string? texte)
	{
		return (texte ?? string.Empty).TrimEnd(' ', ':', '\u00a0');
	}

	internal static string Signe(long valeur, bool pourcent = false)
	{
		return ((valeur > 0) ? "+" : ((valeur < 0) ? "-" : string.Empty)) + Math.Abs(valeur).ToString(CultureInfo.InvariantCulture) + (pourcent ? " %" : string.Empty);
	}

	internal static int StatDe(ItemDescriptor? objet, ItemProperties? proprietes, int stat)
	{
		if (objet != null)
		{
			int[] statsGiven = objet.StatsGiven;
			int num = ((statsGiven != null && stat < statsGiven.Length) ? statsGiven[stat] : 0);
			int[] array = proprietes?.StatModifiers;
			return num + ((array != null && stat < array.Length) ? array[stat] : 0);
		}
		return 0;
	}

	internal static Dictionary<ItemEffect, int> EffetsDe(ItemDescriptor? objet)
	{
		Dictionary<ItemEffect, int> dictionary = new Dictionary<ItemEffect, int>();
		foreach (EffectData item in objet?.Effects ?? new List<EffectData>())
		{
			if (item.Type != ItemEffect.None && item.Percentage != 0)
			{
				dictionary[item.Type] = dictionary.GetValueOrDefault(item.Type) + item.Percentage;
			}
		}
		return dictionary;
	}

	internal static List<Ligne> LignesEquipement(ItemDescriptor objet, ItemProperties? proprietes, ItemDescriptor? compare, ItemProperties? proprietesCompare)
	{
		List<Ligne> list = new List<Ligne>();
		bool flag = compare != null;
		for (int i = 0; i < Enum.GetValues<Stat>().Length; i++)
		{
			string text;
			if (!Strings.ItemDescription.Stats.TryGetValue(i, out LocalizedString value))
			{
				Stat stat = (Stat)i;
				text = stat.ToString();
			}
			else
			{
				text = Libelle(value);
			}
			string nom = text;
			int num = StatDe(objet, proprietes, i);
			int num2 = StatDe(compare, proprietesCompare, i);
			if (num != 0 || num2 != 0)
			{
				list.Add(new Ligne(nom, null, Signe(num), flag ? new long?(num - num2) : ((long?)null), Pourcent: false, Effet: false));
			}
			int[] percentageStatsGiven = objet.PercentageStatsGiven;
			int num3 = ((percentageStatsGiven != null && i < percentageStatsGiven.Length) ? percentageStatsGiven[i] : 0);
			int[] array = compare?.PercentageStatsGiven;
			int num4 = ((array != null && i < array.Length) ? array[i] : 0);
			if (num3 != 0 || num4 != 0)
			{
				list.Add(new Ligne(nom, null, Signe(num3, pourcent: true), flag ? new long?(num3 - num4) : ((long?)null), Pourcent: true, Effet: false));
			}
		}
		for (int j = 0; j < Enum.GetValues<Vital>().Length; j++)
		{
			string nom2 = ((j == 0) ? Strings.Inventaire.MaxHealth : Strings.Inventaire.MaxKi).ToString();
			long[] vitalsGiven = objet.VitalsGiven;
			long num5 = ((vitalsGiven != null && j < vitalsGiven.Length) ? vitalsGiven[j] : 0);
			long[] array2 = compare?.VitalsGiven;
			long num6 = ((array2 != null && j < array2.Length) ? array2[j] : 0);
			if (num5 != 0L || num6 != 0L)
			{
				list.Add(new Ligne(nom2, null, Signe(num5), flag ? new long?(num5 - num6) : ((long?)null), Pourcent: false, Effet: false));
			}
		}
		Dictionary<ItemEffect, int> dictionary = EffetsDe(objet);
		Dictionary<ItemEffect, int> dictionary2 = EffetsDe(compare);
		foreach (ItemEffect item in from type in dictionary.Keys.Union(dictionary2.Keys)
			orderby type
			select type)
		{
			int valueOrDefault = dictionary.GetValueOrDefault(item);
			int valueOrDefault2 = dictionary2.GetValueOrDefault(item);
			LocalizedString value2;
			string nom3 = (Strings.ItemDescription.BonusEffects.TryGetValue((int)item, out value2) ? Libelle(value2) : item.ToString());
			LocalizedString value3;
			string aide = (Strings.Personnage.EffectDescriptions.TryGetValue((int)item, out value3) ? value3.ToString() : null);
			list.Add(new Ligne(nom3, aide, Signe(valueOrDefault, pourcent: true), flag ? new long?(valueOrDefault - valueOrDefault2) : ((long?)null), Pourcent: true, Effet: true));
		}
		return list;
	}

	private static List<Ligne> LignesConsommable(ItemDescriptor objet)
	{
		List<Ligne> list = new List<Ligne>();
		ConsumableData consumable = objet.Consumable;
		if (consumable != null && (consumable.Value != 0L || consumable.Percentage != 0))
		{
			LocalizedString value;
			LocalizedString value2;
			(string, string) tuple = consumable.Type switch
			{
				ConsumableType.Health => (Strings.Personnage.RestoresHealth.ToString(), Strings.ItemDescription.Vitals.TryGetValue(0, out value) ? Libelle(value) : string.Empty), 
				ConsumableType.Mana => (Strings.Personnage.RestoresKi.ToString(), Strings.ItemDescription.Vitals.TryGetValue(1, out value2) ? Libelle(value2) : string.Empty), 
				_ => (Strings.Personnage.GivesExperience.ToString(), string.Empty), 
			};
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			List<string> list2 = new List<string>();
			if (consumable.Value != 0L)
			{
				list2.Add((item2.Length > 0) ? (Signe(consumable.Value) + " " + item2) : Signe(consumable.Value));
			}
			if (consumable.Percentage != 0)
			{
				list2.Add(Signe(consumable.Percentage, pourcent: true));
			}
			list.Add(new Ligne(item, null, string.Join(" · ", list2), null, Pourcent: false, Effet: true));
		}
		foreach (KeyValuePair<ItemEffect, int> item3 in EffetsDe(objet))
		{
			item3.Deconstruct(out var key, out var value3);
			ItemEffect key2 = key;
			int num = value3;
			LocalizedString value4;
			string nom = (Strings.ItemDescription.BonusEffects.TryGetValue((int)key2, out value4) ? Libelle(value4) : key2.ToString());
			LocalizedString value5;
			string aide = (Strings.Personnage.EffectDescriptions.TryGetValue((int)key2, out value5) ? value5.ToString() : null);
			list.Add(new Ligne(nom, aide, Signe(num, pourcent: true), null, Pourcent: true, Effet: true));
		}
		if (objet.Cooldown > 0)
		{
			list.Add(new Ligne(Strings.Personnage.Cooldown, null, Strings.Personnage.Seconds.ToString(TechniqueArt.Number((double)objet.Cooldown / 1000.0)), null, Pourcent: false, Effet: false));
		}
		return list;
	}

	private float DessinerLignes(Intersect.Client.Framework.Gwen.Renderer.Base renderer, List<Ligne> lignes, bool avecEcart, string colonne, float y)
	{
		if (lignes.Count == 0)
		{
			return y;
		}
		float num = 1548f;
		int centerY = _d.Y(y + 6f);
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), colonne.ToUpperInvariant(), _d.X(1148f), centerY, _d.Em(0.16f, 12f), A(TechniqueArt.Dim));
		if (lignes.Any((Ligne ligne2) => !ligne2.Effet || ligne2.Ecart.HasValue))
		{
			TechniqueArt.SpacedRight(renderer, TechniqueArt.Label, Pt(12f), Strings.Personnage.ColumnValue.ToString().ToUpperInvariant(), _d.X(num - (float)(avecEcart ? 70 : 0)), centerY, _d.Em(0.16f, 12f), A(TechniqueArt.Dim));
		}
		if (avecEcart)
		{
			TechniqueArt.SpacedRight(renderer, TechniqueArt.Label, Pt(12f), Strings.Personnage.ColumnGap.ToString().ToUpperInvariant(), _d.X(num), centerY, _d.Em(0.16f, 12f), A(TechniqueArt.Dim));
		}
		y += 20f;
		for (int num2 = 0; num2 < lignes.Count; num2++)
		{
			Ligne ligne = lignes[num2];
			List<string> list = ((ligne.Aide == null || !base.Details) ? new List<string>() : TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), ligne.Aide, _d.L(avecEcart ? 250 : 300), 2));
			float num3 = 32f + (float)(list.Count * 15);
			if (num2 > 0)
			{
				TechniqueArt.Fill(renderer, _d.R(1148f, y, 400f, 1f), A(TechniqueArt.White, 0.05f));
			}
			int centerY2 = _d.Y(y + ((list.Count == 0) ? (num3 / 2f) : 16f));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), ligne.Nom, _d.X(1148f), centerY2, A(TechniqueArt.Soft));
			for (int num4 = 0; num4 < list.Count; num4++)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), list[num4], _d.X(1148f), _d.Y(y + 31f + (float)(num4 * 15)), A(TechniqueArt.Dim));
			}
			int centerY3 = _d.Y(y + ((list.Count == 0) ? (num3 / 2f) : 16f));
			int right = _d.X(num - (float)(avecEcart ? 70 : 0));
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt((ligne.Effet && !ligne.Ecart.HasValue && ligne.Valeur.Length > 8) ? 17 : 20), ligne.Valeur, right, centerY3, A(ligne.Effet ? TechniqueArt.Cyan : TechniqueArt.Text));
			if (avecEcart)
			{
				long? ecart = ligne.Ecart;
				if (ecart.HasValue)
				{
					long valueOrDefault = ecart.GetValueOrDefault();
					Color color = ((valueOrDefault > 0) ? TechniqueArt.Green : ((valueOrDefault < 0) ? TechniqueArt.Red : TechniqueArt.Dim));
					TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(16f), (valueOrDefault == 0L) ? "=" : Signe(valueOrDefault, ligne.Pourcent), _d.X(num), centerY3, A(color));
				}
			}
			y += num3;
		}
		return y;
	}

	internal static bool MeilleurPartout(Player me, int caseSac, ItemDescriptor objet)
	{
		int caseSac2;
		ItemDescriptor itemDescriptor = ObjetPorte(me, objet.EquipmentSlot, out caseSac2);
		if (itemDescriptor == null)
		{
			return true;
		}
		if (itemDescriptor.Id == objet.Id && caseSac2 == caseSac)
		{
			return false;
		}
		List<Ligne> source = LignesEquipement(objet, ProprietesDe(me, caseSac), itemDescriptor, ProprietesDe(me, caseSac2));
		if (!source.All((Ligne ligne) => ligne.Ecart >= 0) || !source.Any((Ligne ligne) => ligne.Ecart > 0))
		{
			return false;
		}
		PanoplieDescriptor panoplieDescriptor = PanoplieDescriptor.DeLObjet(itemDescriptor.Id);
		if (panoplieDescriptor == null)
		{
			return true;
		}
		HashSet<Guid> hashSet = IdsPortes(me);
		HashSet<Guid> hashSet2 = new HashSet<Guid>(hashSet);
		hashSet2.Remove(itemDescriptor.Id);
		hashSet2.Add(objet.Id);
		int piecesAvant = panoplieDescriptor.PiecesPortees(hashSet);
		int piecesApres = panoplieDescriptor.PiecesPortees(hashSet2);
		return !panoplieDescriptor.Bonuses.Any((PanoplieBonus bonus) => piecesAvant >= bonus.Pieces && piecesApres < bonus.Pieces);
	}

	private float DessinerBlocPanoplie(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, ItemDescriptor objet, ItemDescriptor? compare, int emplacement, float y)
	{
		HashSet<Guid> portes = IdsPortes(me);
		HashSet<Guid> hashSet = ((emplacement < 0) ? PortesApresEchange(me) : null);
		PanoplieDescriptor panoplieDescriptor = PanoplieDescriptor.DeLObjet(objet.Id);
		if (panoplieDescriptor != null)
		{
			int num = panoplieDescriptor.PiecesPortees(portes);
			int nouveau = ((hashSet == null) ? num : panoplieDescriptor.PiecesPortees(hashSet));
			List<PanoplieBonus> paliers = panoplieDescriptor.Bonuses.OrderBy((PanoplieBonus bonus) => bonus.Pieces).ToList();
			y = DessinerCadrePanoplie(renderer, panoplieDescriptor, num, nouveau, paliers, TechniqueArt.Gold, y);
		}
		if (compare != null && hashSet != null)
		{
			PanoplieDescriptor panoplieDescriptor2 = PanoplieDescriptor.DeLObjet(compare.Id);
			if (panoplieDescriptor2 != null)
			{
				Guid id = panoplieDescriptor2.Id;
				Guid? obj = PanoplieDescriptor.DeLObjet(objet.Id)?.Id;
				if (id != obj)
				{
					int avant = panoplieDescriptor2.PiecesPortees(portes);
					int nouveau2 = panoplieDescriptor2.PiecesPortees(hashSet);
					List<PanoplieBonus> list = (from bonus in panoplieDescriptor2.Bonuses
						where avant >= bonus.Pieces && nouveau2 < bonus.Pieces
						orderby bonus.Pieces
						select bonus).ToList();
					if (list.Count > 0)
					{
						y = DessinerCadrePanoplie(renderer, panoplieDescriptor2, avant, nouveau2, list, TechniqueArt.Red, y);
					}
				}
			}
		}
		return y;
	}

	private float DessinerCadrePanoplie(Intersect.Client.Framework.Gwen.Renderer.Base renderer, PanoplieDescriptor panoplie, int avant, int nouveau, List<PanoplieBonus> paliers, Color couleur, float y)
	{
		y += 12f;
		int num = panoplie.Items.Distinct().Count();
		int num2 = 34 + paliers.Count * 20 + 6;
		Rectangle rect = _d.R(1148f, y, 400f, num2);
		TechniqueArt.FillHorizontal(renderer, rect, A(couleur, 0.08f), A(couleur, 0.03f), A(couleur, 0f));
		TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, Math.Max(1, _d.L(2f)), rect.Height), A(couleur));
		int centerY = _d.Y(y + 16f);
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), panoplie.Name.ToUpperInvariant(), _d.X(1160f), centerY, _d.Em(0.18f, 12f), A(couleur));
		string text = ((avant == nouveau) ? Strings.Personnage.SetCount.ToString(avant, num) : Strings.Personnage.SetCountChange.ToString(avant, nouveau, num));
		TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(12f), text, _d.X(1536f), centerY, A(couleur));
		float num3 = y + 40f;
		foreach (PanoplieBonus palier in paliers)
		{
			DessinerPalier(renderer, palier, avant, nouveau, 1160f, num3, 376f);
			num3 += 20f;
		}
		return y + (float)num2;
	}

	private float DessinerDescription(Intersect.Client.Framework.Gwen.Renderer.Base renderer, ItemDescriptor objet, float y)
	{
		if (string.IsNullOrWhiteSpace(objet.Description))
		{
			return y;
		}
		y += 14f;
		foreach (string item in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(13.5f), "« " + objet.Description.Trim() + " »", _d.L(400f), 4))
		{
			TechniqueArt.Italic(renderer, TechniqueArt.Body, Pt(13.5f), item, _d.X(1148f), _d.Y(y), A(new Color(255, 195, 203, 220)));
			y += 20f;
		}
		return y;
	}

	private void DessinerSources(Intersect.Client.Framework.Gwen.Renderer.Base renderer, ItemDescriptor objet, float y)
	{
		y += 16f;
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), Strings.Personnage.SourcesHeading.ToString().ToUpperInvariant(), _d.X(1148f), _d.Y(y + 6f), _d.Em(0.2f, 12f), A(TechniqueArt.Dim));
		y += 22f;
		List<(string, string, string)> list = SourcesDe(objet);
		if (list.Count == 0)
		{
			TechniqueArt.Italic(renderer, TechniqueArt.Body, Pt(12.5f), Strings.Personnage.SourceNone, _d.X(1148f), _d.Y(y), A(TechniqueArt.Dim));
			return;
		}
		for (int i = 0; i < Math.Min(3, list.Count); i++)
		{
			var (text, text2, text3) = list[i];
			if (i > 0)
			{
				TechniqueArt.Fill(renderer, _d.R(1148f, y - 4f, 400f, 1f), A(TechniqueArt.White, 0.05f));
			}
			Rectangle rect = _d.R(1148f, y, 26f, 28f);
			TechniqueArt.Image(renderer, "tech_hexa.png", rect, A(TechniqueArt.Gold, 0.1f));
			TechniqueArt.Image(renderer, "tech_hexa_bord.png", rect, A(TechniqueArt.Gold, 0.35f));
			if (text3 != null)
			{
				IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Item, text3);
				if (texture != null)
				{
					int num = _d.L(16f);
					SaiyanHud.Draw(renderer, texture, rect.X + (rect.Width - num) / 2, rect.Y + (rect.Height - num) / 2, num, num, A(TechniqueArt.White));
				}
			}
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(16f), text, _d.X(1184f), _d.Y(y + 8f), A(TechniqueArt.Text));
			if (!string.IsNullOrEmpty(text2))
			{
				string text4 = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), text2, _d.L(364f), 1).FirstOrDefault();
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), text4, _d.X(1184f), _d.Y(y + 26f), A(TechniqueArt.Dim));
			}
			y += 42f;
		}
	}

	private List<(string? Titre, string? Detail, string? Icone)> SourcesDe(ItemDescriptor objet)
	{
		if (_sources.TryGetValue(objet.Id, out List<(string, string, string)> value))
		{
			return value;
		}
		List<(string, string, string)> list = new List<(string, string, string)>();
		foreach (CraftingRecipeDescriptor recette in from craftingRecipeDescriptor in DatabaseObject<CraftingRecipeDescriptor>.Lookup.Values.OfType<CraftingRecipeDescriptor>()
			where craftingRecipeDescriptor.ItemId == objet.Id
			select craftingRecipeDescriptor)
		{
			string text = (Professions.IsValid(recette.Profession) ? Professions.Name(recette.Profession) : (DatabaseObject<CraftingTableDescriptor>.Lookup.Values.OfType<CraftingTableDescriptor>().FirstOrDefault((CraftingTableDescriptor table) => table.Crafts.Contains(recette.Id))?.Name ?? recette.Name));
			ItemDescriptor databaseObject;
			string item = string.Join(", ", from ingredient in recette.Ingredients
				where DatabaseObject<ItemDescriptor>.TryGet(ingredient.ItemId, out databaseObject)
				select $"{ingredient.Quantity} × {DatabaseObject<ItemDescriptor>.Get(ingredient.ItemId)?.Name}");
			string item2 = (Professions.IsValid(recette.Profession) ? Professions.Icon(recette.Profession) : null);
			list.Add((Strings.Personnage.SourceCraft.ToString(text, Math.Max(1, recette.ProfessionLevel)), item, item2));
		}
		IOrderedEnumerable<(string, double)> orderedEnumerable = from entree in DatabaseObject<NPCDescriptor>.Lookup.Values.OfType<NPCDescriptor>().SelectMany((NPCDescriptor pnj) => from butin in pnj.Drops ?? new List<Drop>()
				where butin.ItemId == objet.Id && butin.Chance > 0.0
				select (Name: pnj.Name, Chance: butin.Chance))
			group entree by entree.Name into groupe
			select (Nom: groupe.Key, Chance: groupe.Max(((string Name, double Chance) entree) => entree.Chance)) into entree
			orderby entree.Chance descending
			select entree;
		foreach (var (text2, value2) in orderedEnumerable)
		{
			list.Add((Strings.Personnage.SourceDrop.ToString(text2), TechniqueArt.Number(value2) + " %", "viande_dinosaure.png"));
		}
		IOrderedEnumerable<(string, int, double)> orderedEnumerable2 = from entree in DatabaseObject<ResourceDescriptor>.Lookup.Values.OfType<ResourceDescriptor>().SelectMany((ResourceDescriptor ressource) => from butin in ressource.Drops ?? new List<Drop>()
				where butin.ItemId == objet.Id && butin.Chance > 0.0
				select (Ressource: ressource, Chance: butin.Chance))
			group entree by entree.Ressource.Name into groupe
			select (Nom: groupe.Key, Metier: Professions.Of(groupe.First().Ressource), Chance: groupe.Max(((ResourceDescriptor Ressource, double Chance) entree) => entree.Chance)) into entree
			orderby entree.Chance descending
			select entree;
		foreach (var item7 in orderedEnumerable2)
		{
			string item3 = item7.Item1;
			int item4 = item7.Item2;
			double item5 = item7.Item3;
			string item6 = ((item4 >= 0) ? (Professions.Name(item4) + " · " + TechniqueArt.Number(item5) + " %") : (TechniqueArt.Number(item5) + " %"));
			list.Add((Strings.Personnage.SourceHarvest.ToString(item3), item6, (item4 >= 0) ? Professions.Icon(item4) : null));
		}
		_sources[objet.Id] = list;
		return list;
	}

	private void DessinerActions(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, ItemDescriptor objet, int caseSac, int emplacement)
	{
		Action action = null;
		bool flag = emplacement < 0 && caseSac >= 0 && objet.CanDrop;
		string text;
		if (emplacement >= 0)
		{
			text = Strings.Personnage.ActionUnequip;
			action = delegate
			{
				PacketSender.SendUnequipItem(emplacement);
			};
		}
		else
		{
			string text2;
			switch (objet.ItemType)
			{
			case ItemType.Equipment:
				text2 = Strings.Personnage.ActionEquip.ToString();
				break;
			case ItemType.Consumable:
			case ItemType.Spell:
				text2 = Strings.Personnage.ActionUse.ToString();
				break;
			case ItemType.Bag:
				text2 = Strings.Personnage.ActionOpen.ToString();
				break;
			case ItemType.Event:
				text2 = Strings.Personnage.TypeQuest.ToString();
				break;
			default:
				text2 = null;
				break;
			}
			text = text2;
			if (text != null && objet.ItemType != ItemType.Event)
			{
				action = delegate
				{
					me.TryUseItem(caseSac);
				};
			}
		}
		float num = ((flag && text != null) ? 120f : (flag ? 400f : 0f));
		if (text != null)
		{
			Rectangle rect = _d.R(1148f, 802f, 400f - ((num > 0f) ? (num + 10f) : 0f), 48f);
			bool flag2 = action != null;
			bool flag3 = flag2 && IsHot("fiche:agir");
			if (flag2)
			{
				TechniqueArt.GlowRect(renderer, new Rectangle(rect.X - _d.L(16f), rect.Y - _d.L(16f), rect.Width + _d.L(32f), rect.Height + _d.L(32f)), A(new Color(255, 255, 170, 60), flag3 ? 0.7f : 0.35f), additive: false);
			}
			TechniqueArt.Patch(renderer, (!flag2) ? "tech_bouton_eteint.png" : (flag3 ? "tech_bouton_survol.png" : "tech_bouton.png"), rect, A(TechniqueArt.White), 14);
			string text3 = text.ToUpperInvariant();
			Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(20f), text3);
			TechniqueArt.Italic(renderer, TechniqueArt.Display, Pt(20f), text3, rect.X + (rect.Width - point.X) / 2, rect.Y + (rect.Height - point.Y) / 2, A(flag2 ? new Color(255, 20, 10, 2) : TechniqueArt.Dim));
			if (action != null)
			{
				AddHit(rect, "fiche:agir", action);
			}
		}
		if (num > 0f)
		{
			Rectangle rect2 = _d.R(1548f - num, 802f, num, 48f);
			bool flag4 = IsHot("fiche:jeter");
			TechniqueArt.FillVertical(renderer, rect2, A(new Color(255, 28, 36, 54)), A(new Color(255, 18, 24, 40)));
			TechniqueArt.Outline(renderer, rect2, A(flag4 ? TechniqueArt.Red : TechniqueArt.Line2));
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Heavy, Pt(18f), Strings.Personnage.ActionDrop.ToString().ToUpperInvariant(), rect2.X + rect2.Width / 2, rect2.Y + rect2.Height / 2, _d.Em(0.08f, 18f), A(flag4 ? TechniqueArt.White : TechniqueArt.Soft));
			AddHit(rect2, "fiche:jeter", delegate
			{
				me.TryDropItem(caseSac);
			});
		}
	}

	private static List<Choix> Titres(Player me)
	{
		List<Choix> list = new List<Choix>
		{
			new Choix(Guid.Empty, Strings.Titles.None, TitleDescriptor.DefaultColor, null, string.Empty, Debloque: true, me.TitleId == Guid.Empty, null, 0)
		};
		List<TitleDescriptor> source = DatabaseObject<TitleDescriptor>.Lookup.Values.OfType<TitleDescriptor>().ToList();
		List<string> ordre = source.Select((TitleDescriptor titre) => Traduction.Contenu(titre.Folder?.Trim() ?? string.Empty)).Distinct().ToList();
		list.AddRange((from titre in source
			select new Choix(titre.Id, titre.Name, titre.Color ?? TitleDescriptor.DefaultColor, string.IsNullOrWhiteSpace(titre.Description) ? null : titre.Description, Traduction.Contenu(titre.Folder?.Trim() ?? string.Empty), me.TitleIds.Contains(titre.Id), titre.Id == me.TitleId, null, 0) into choix
			orderby (choix.Dossier.Length != 0) ? ordre.IndexOf(choix.Dossier) : (-1), choix.Debloque descending
			select choix).ThenBy<Choix, string>((Choix choix) => choix.Nom, StringComparer.CurrentCultureIgnoreCase));
		return list;
	}

	private static List<Choix> Ornements(Player me)
	{
		List<Choix> list = new List<Choix>
		{
			new Choix(Guid.Empty, Strings.Ornaments.None, TechniqueArt.Text, null, string.Empty, Debloque: true, me.OrnamentId == Guid.Empty, null, 0)
		};
		list.AddRange((from ornement in DatabaseObject<OrnamentDescriptor>.Lookup.Values.OfType<OrnamentDescriptor>()
			select new Choix(ornement.Id, ornement.Name, TechniqueArt.Text, string.IsNullOrWhiteSpace(ornement.Description) ? null : ornement.Description, Traduction.Contenu(ornement.Folder ?? string.Empty), me.OrnamentIds.Contains(ornement.Id), ornement.Id == me.OrnamentId, ornement.Style, ornement.UnlockLevel) into choix
			orderby choix.Debloque descending, choix.Niveau
			select choix).ThenBy<Choix, string>((Choix choix) => choix.Nom, StringComparer.CurrentCultureIgnoreCase));
		return list;
	}

	private NamePlate.Content PlaqueEssayee(Player me)
	{
		Guid? essaiTitre = _essaiTitre;
		object obj;
		if (essaiTitre.HasValue)
		{
			Guid valueOrDefault = essaiTitre.GetValueOrDefault();
			obj = ((valueOrDefault != Guid.Empty && DatabaseObject<TitleDescriptor>.TryGet(valueOrDefault, out var databaseObject)) ? databaseObject : null);
		}
		else
		{
			obj = me.Title;
		}
		TitleDescriptor title = (TitleDescriptor)obj;
		essaiTitre = _essaiOrnement;
		object obj2;
		if (essaiTitre.HasValue)
		{
			Guid valueOrDefault2 = essaiTitre.GetValueOrDefault();
			obj2 = ((valueOrDefault2 != Guid.Empty && DatabaseObject<OrnamentDescriptor>.TryGet(valueOrDefault2, out var databaseObject2)) ? databaseObject2 : null);
		}
		else
		{
			obj2 = me.Ornament;
		}
		OrnamentDescriptor ornament = (OrnamentDescriptor)obj2;
		return NamePlate.ContentOf(me, me.NamePlateColor())with
		{
			Title = title,
			Ornament = ornament
		};
	}

	private void DessinerPlaque(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me)
	{
		int num = Math.Max(1, (int)MathF.Round(6f * _d.S));
		int val = _d.Y(496f) - 2 - (me.NamePlateBodyHeight + 6) * num;
		Rectangle previous = TechniqueArt.BeginClip(renderer, _d.R(34f, 134f, 484f, 528f));
		NamePlate.DrawInterface(PlaqueEssayee(me), renderer, _d.X(276f), Math.Max(_d.Y(236f), val), 1.15f * _d.S);
		TechniqueArt.EndClip(renderer, previous);
	}

	private void DessinerPlaquePortee(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, List<Choix> titres, List<Choix> ornements)
	{
		Panel(renderer, _d.R(32f, 680f, 488f, 190f));
		Heading(renderer, 50f, 692f, 452f, Strings.Personnage.PlateHeading, Strings.Personnage.PlateDetail);
		Choix choix = titres.FirstOrDefault((Choix choix4) => choix4.Porte);
		Choix choix2 = ornements.FirstOrDefault((Choix choix4) => choix4.Porte);
		(string, string, Color)[] array = new(string, string, Color)[2]
		{
			(Strings.Personnage.PlateTitle, choix.Nom ?? ((string)Strings.Titles.None), (choix.Id == Guid.Empty) ? TechniqueArt.Soft : choix.Couleur),
			(Strings.Personnage.PlateOrnament, choix2.Nom ?? ((string)Strings.Ornaments.None), (choix2.Id == Guid.Empty) ? TechniqueArt.Soft : TechniqueArt.Text)
		};
		for (int num = 0; num < array.Length; num++)
		{
			(string, string, Color) tuple = array[num];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			Color item3 = tuple.Item3;
			Rectangle rect = _d.R(50f, 722f + (float)num * 46f, 452f, 38f);
			TechniqueArt.Fill(renderer, rect, A(new Color(155, 7, 10, 18)));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
			int centerY = rect.Y + rect.Height / 2;
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), item, rect.X + _d.L(10f), centerY, A(TechniqueArt.Soft));
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(18f), item2, rect.Right - _d.L(10f), centerY, A(item3));
		}
		Guid? essaiTitre = _essaiTitre;
		Choix obj;
		if (essaiTitre.HasValue)
		{
			Guid idTitre = essaiTitre.GetValueOrDefault();
			obj = titres.FirstOrDefault((Choix choix4) => choix4.Id == idTitre);
		}
		else
		{
			essaiTitre = _essaiOrnement;
			if (essaiTitre.HasValue)
			{
				Guid idOrnement = essaiTitre.GetValueOrDefault();
				obj = ornements.FirstOrDefault((Choix choix4) => choix4.Id == idOrnement);
			}
			else
			{
				obj = default(Choix);
			}
		}
		Choix choix3 = obj;
		string text = ((choix3.Nom == null) ? Strings.Personnage.TryHint.ToString() : (choix3.Debloque ? Strings.Personnage.TryUnlocked.ToString(choix3.Nom) : Strings.Personnage.TryLocked.ToString(choix3.Nom)));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), text, _d.X(50f), _d.Y(832f), A(TechniqueArt.Gold));
	}

	private void DessinerTitres(Intersect.Client.Framework.Gwen.Renderer.Base renderer, List<Choix> titres)
	{
		Panel(renderer, _d.R(540f, 96f, 568f, 774f));
		int num = titres.Count((Choix choix) => choix.Debloque && choix.Id != Guid.Empty);
		Heading(renderer, 558f, 108f, 532f, Strings.Personnage.TitlesHeading, Strings.Personnage.UnlockedCount.ToString(num, titres.Count - 1));
		_zoneTitres = _d.R(556f, 136f, 536f, 718f);
		float num2 = 0f;
		string text = null;
		foreach (Choix titre in titres)
		{
			if (titre.Dossier != text)
			{
				text = titre.Dossier;
				num2 += 28f;
			}
			num2 += 62f;
		}
		float num3 = Math.Max(0f, num2 - (float)_zoneTitres.Height / _d.S);
		_defilTitres = Math.Clamp(_defilTitres, 0f, num3);
		Rectangle previous = TechniqueArt.BeginClip(renderer, _zoneTitres);
		float num4 = 136f - _defilTitres;
		text = null;
		foreach (Choix titre2 in titres)
		{
			if (titre2.Dossier != text)
			{
				text = titre2.Dossier;
				string text2 = ((text.Length == 0) ? Strings.Personnage.NoFolder.ToString() : text).ToUpperInvariant();
				int num5 = TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), text2, _d.X(560f), _d.Y(num4 + 16f), _d.Em(0.22f, 12f), A(TechniqueArt.Dim));
				TechniqueArt.Fill(renderer, new Rectangle(_d.X(560f) + num5 + _d.L(10f), _d.Y(num4 + 16f), _d.X(1082f) - _d.X(560f) - num5 - _d.L(10f), 1), A(TechniqueArt.White, 0.07f));
				num4 += 28f;
			}
			if (num4 + 58f >= 136f && num4 <= 136f + (float)_zoneTitres.Height / _d.S)
			{
				DessinerTitre(renderer, titre2, num4, num3 > 0f);
			}
			num4 += 62f;
		}
		TechniqueArt.EndClip(renderer, previous);
		DessinerAscenseur(renderer, _zoneTitres, _defilTitres, num3, num2);
	}

	private void DessinerTitre(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Choix choix, float y, bool ascenseur)
	{
		Rectangle rect = _d.R(556f, y, ascenseur ? 526 : 536, 58f);
		string key = $"titre:{choix.Id}";
		bool flag = _zoneTitres.Contains(LocalMouse().X, LocalMouse().Y) && IsHot(key);
		if (flag)
		{
			_essaiTitre = choix.Id;
		}
		if (choix.Porte)
		{
			TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Gold, 0.12f), A(new Color(160, 7, 10, 18)), A(new Color(160, 7, 10, 18)));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Gold, 0.7f));
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, Math.Max(2, _d.L(3f)), rect.Height), A(TechniqueArt.Gold));
		}
		else
		{
			TechniqueArt.Fill(renderer, rect, A(flag ? new Color(235, 20, 27, 42) : new Color(160, 7, 10, 18)));
			TechniqueArt.Outline(renderer, rect, A(flag ? TechniqueArt.Line2 : TechniqueArt.Line));
		}
		int x = rect.X + _d.L(12f);
		Color color = (choix.Debloque ? choix.Couleur : TechniqueArt.Dim);
		string note = choix.Note;
		if (note != null)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(18f), choix.Nom, x, rect.Y + _d.L(20f), A(color));
			string text = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), note, rect.Width - _d.L(choix.Debloque ? 28 : 150), 1).FirstOrDefault();
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), text, x, rect.Y + _d.L(41f), A(TechniqueArt.Dim));
		}
		else
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(18f), choix.Nom, x, rect.Y + rect.Height / 2, A(color));
		}
		DessinerBadge(renderer, choix, rect.Right - _d.L(12f), rect.Y + rect.Height / 2);
		AddHit(rect, key, delegate
		{
			if (choix.Debloque && !choix.Porte)
			{
				PacketSender.SendSetActiveTitle(choix.Id);
			}
		});
	}

	private void DessinerBadge(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Choix choix, int droite, int centre)
	{
		string text = (choix.Porte ? Strings.Personnage.BadgeWorn.ToString() : (choix.Debloque ? null : ((choix.Niveau > 0) ? Strings.Personnage.BadgeLevel.ToString(choix.Niveau) : Strings.Personnage.BadgeLocked.ToString())));
		if (text != null)
		{
			text = text.ToUpperInvariant();
			float spacing = _d.Em(0.14f, 12f);
			int num = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(12f), text, spacing) + _d.L(16f);
			Rectangle rect = new Rectangle(droite - num, centre - _d.L(9f), num, _d.L(18f));
			if (choix.Porte)
			{
				TechniqueArt.Patch(renderer, "tech_biais4.png", rect, A(TechniqueArt.Gold), 5);
				TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), text, rect.X + _d.L(8f), centre, spacing, A(new Color(255, 26, 18, 6)));
			}
			else
			{
				TechniqueArt.Patch(renderer, "tech_biais4.png", rect, A(new Color(235, 22, 29, 46)), 5);
				TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), text, rect.X + _d.L(8f), centre, spacing, A(TechniqueArt.Dim));
			}
		}
	}

	private void DessinerOrnements(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, List<Choix> ornements)
	{
		Panel(renderer, _d.R(1128f, 96f, 440f, 774f));
		int num = ornements.Count((Choix choix3) => choix3.Debloque && choix3.Id != Guid.Empty);
		Heading(renderer, 1146f, 108f, 404f, Strings.Personnage.OrnamentsHeading, Strings.Personnage.UnlockedCount.ToString(num, ornements.Count - 1));
		_zoneOrnements = _d.R(1144f, 136f, 408f, 718f);
		int num2 = (ornements.Count + 1) / 2;
		float num3 = (float)num2 * 102f - 8f;
		float num4 = Math.Max(0f, num3 - (float)_zoneOrnements.Height / _d.S);
		_defilOrnements = Math.Clamp(_defilOrnements, 0f, num4);
		Rectangle previous = TechniqueArt.BeginClip(renderer, _zoneOrnements);
		Choix? choix = null;
		Rectangle cible = default(Rectangle);
		for (int num5 = 0; num5 < ornements.Count; num5++)
		{
			Choix choix2 = ornements[num5];
			Rectangle rectangle = _d.R(1144f + (float)(num5 % 2) * 208f, 136f + (float)(num5 / 2) * 102f - _defilOrnements, 200f, 94f);
			if (rectangle.Bottom < _zoneOrnements.Y || rectangle.Y > _zoneOrnements.Bottom)
			{
				continue;
			}
			string key = $"ornement:{choix2.Id}";
			bool flag = _zoneOrnements.Contains(LocalMouse().X, LocalMouse().Y) && IsHot(key);
			if (flag)
			{
				_essaiOrnement = choix2.Id;
				choix = choix2;
				cible = rectangle;
			}
			TechniqueArt.Fill(renderer, rectangle, A(flag ? new Color(235, 20, 27, 42) : new Color(160, 7, 10, 18)));
			Rectangle rectangle2 = new Rectangle(rectangle.X + 1, rectangle.Y + 1, rectangle.Width - 2, _d.L(64f));
			Rectangle previous2 = TechniqueArt.BeginClip(renderer, rectangle2);
			OrnamentDescriptor databaseObject;
			NamePlate.Content content = new NamePlate.Content(null, TechniqueArt.White, null, null, me.Name, me.NamePlateColor(), null, (choix2.Id != Guid.Empty && DatabaseObject<OrnamentDescriptor>.TryGet(choix2.Id, out databaseObject)) ? databaseObject : null);
			NamePlate.DrawInterface(content, renderer, rectangle2.X + rectangle2.Width / 2, rectangle2.Y + _d.L(50f), 0.8f * _d.S);
			if (!choix2.Debloque)
			{
				TechniqueArt.Fill(renderer, rectangle2, A(TechniqueArt.Black, 0.55f));
			}
			TechniqueArt.EndClip(renderer, previous2);
			if (choix2.Porte)
			{
				TechniqueArt.Outline(renderer, rectangle, A(TechniqueArt.Gold, 0.85f), Math.Max(1, _d.L(2f)));
				TechniqueArt.GlowRect(renderer, rectangle, A(TechniqueArt.Gold, 0.08f), additive: false);
			}
			else
			{
				TechniqueArt.Outline(renderer, rectangle, A(flag ? TechniqueArt.Line2 : TechniqueArt.Line));
			}
			string text = TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(15f), choix2.Nom, rectangle.Width - _d.L(16f), 1).FirstOrDefault();
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(15f), text, rectangle.X + _d.L(8f), rectangle.Bottom - _d.L(14f), A(choix2.Debloque ? TechniqueArt.Text : TechniqueArt.Dim));
			DessinerBadge(renderer, choix2, rectangle.Right - _d.L(6f), rectangle.Y + _d.L(15f));
			AddHit(rectangle, key, delegate
			{
				if (choix2.Debloque && !choix2.Porte)
				{
					PacketSender.SendSetActiveOrnament(choix2.Id);
				}
			});
		}
		TechniqueArt.EndClip(renderer, previous);
		DessinerAscenseur(renderer, _zoneOrnements, _defilOrnements, num4, num3);
		if (choix.HasValue)
		{
			Choix valueOrDefault = choix.GetValueOrDefault();
			string note = valueOrDefault.Note;
			if (note != null)
			{
				DessinerBulle(renderer, valueOrDefault.Nom, note, cible);
			}
		}
	}

	private void DessinerBulle(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string titre, string texte, Rectangle cible)
	{
		List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), texte, _d.L(260f), 3);
		int num = Math.Max(TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(13f), titre).X, (list.Count != 0) ? list.Max((string ligne) => TechniqueArt.Measure(renderer, TechniqueArt.Body, Pt(12f), ligne).X) : 0) + _d.L(20f);
		int num2 = _d.L(26 + list.Count * 16 + 6);
		int x = Math.Clamp(cible.X + cible.Width / 2 - num / 2, _d.X(1132f), _d.X(1564f) - num);
		Rectangle rect = new Rectangle(x, cible.Y - num2 - _d.L(8f), num, num2);
		if (rect.Y < _d.Y(100f))
		{
			rect.Y = cible.Bottom + _d.L(8f);
		}
		TechniqueArt.Fill(renderer, rect, A(new Color(245, 6, 8, 14)));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line2));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(13f), titre, rect.X + _d.L(10f), rect.Y + _d.L(15f), A(TechniqueArt.Text));
		for (int num3 = 0; num3 < list.Count; num3++)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), list[num3], rect.X + _d.L(10f), rect.Y + _d.L(34 + num3 * 16), A(TechniqueArt.Soft));
		}
	}

	private void DessinerAscenseur(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle zone, float defil, float maximum, float contenu)
	{
		if (!(maximum <= 0f))
		{
			Rectangle rect = new Rectangle(zone.Right - _d.L(4f), zone.Y, Math.Max(2, _d.L(4f)), zone.Height);
			TechniqueArt.Fill(renderer, rect, A(new Color(255, 7, 10, 18)));
			int num = Math.Max(_d.L(30f), (int)((float)rect.Height * ((float)zone.Height / _d.S) / Math.Max(1f, contenu)));
			int y = rect.Y + (int)((float)(rect.Height - num) * (defil / maximum));
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, y, rect.Width, num), A(TechniqueArt.Line2));
		}
	}

	protected override bool OnMouseWheeled(int delta)
	{
		if (_onglet == Onglet.Personnalisation)
		{
			Point point = LocalMouse();
			if (_zoneTitres.Contains(point.X, point.Y))
			{
				_defilTitres -= Math.Sign(delta) * 62;
				return true;
			}
			if (_zoneOrnements.Contains(point.X, point.Y))
			{
				_defilOrnements -= Math.Sign(delta) * 102;
				return true;
			}
		}
		return base.OnMouseWheeled(delta);
	}

	private void BuildRecherche()
	{
		_recherche = new TextBox(this, "Recherche")
		{
			Font = TechniqueArt.Body,
			FontSize = 11,
			ShouldDrawBackground = false,
			MaximumLength = 32
		};
		_recherche.SetTextColor(TechniqueArt.Text, ComponentState.Normal);
		_recherche.TextChanged += delegate
		{
			_requete = Normaliser(_recherche.Text);
		};
		Interface.FocusComponents.Add(_recherche);
	}

	private void PlacerRecherche()
	{
		Rectangle rectangle = _d.R(582f, 138f, 380f, 30f);
		if (_recherche.FontSize != Pt(14f))
		{
			_recherche.FontSize = Pt(14f);
		}
		if (_recherche.X != rectangle.X || _recherche.Y != rectangle.Y || _recherche.Width != rectangle.Width || _recherche.Height != rectangle.Height)
		{
			_recherche.SetBounds(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
		}
	}

	private static Categorie CategorieDe(ItemDescriptor objet)
	{
		return objet.ItemType switch
		{
			ItemType.Equipment => Categorie.Equipement, 
			ItemType.Consumable => Categorie.Consommables, 
			ItemType.Spell => Categorie.Techniques, 
			ItemType.Event => Categorie.Quete, 
			ItemType.Bag => Categorie.Sacs, 
			_ => Categorie.Ressources, 
		};
	}

	private int? CaseSacSous(Point local)
	{
		foreach (var (rectangle, value) in _cases)
		{
			if (rectangle.Contains(local.X, local.Y))
			{
				return value;
			}
		}
		return null;
	}

	private int? RaccourciSous(Point local)
	{
		foreach (var (rectangle, value) in _casesRaccourcis)
		{
			if (rectangle.Contains(local.X, local.Y))
			{
				return value;
			}
		}
		return null;
	}

	private void DessinerSac(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, long now)
	{
		_zoneSac = _d.R(540f, 96f, 568f, 774f);
		Panel(renderer, _zoneSac);
		List<(int Case, ItemDescriptor Objet, int Quantite)> list = new List<(int Case, ItemDescriptor Objet, int Quantite)>();
		List<int> list2 = new List<int>();
		for (int i = 0; i < me.Inventory.Length; i++)
		{
			IItem item = me.Inventory[i];
			if (item == null || !DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var databaseObject))
			{
				list2.Add(i);
			}
			else if (!me.MyEquipment.Contains(i))
			{
				list.Add((i, databaseObject, item.Quantity));
			}
		}
		List<(int Case, ItemDescriptor Objet, int Quantite)> list3 = (from entree in list
			where _categorie == Categorie.Tout || CategorieDe(entree.Objet) == _categorie
			where _requete.Length == 0 || Normaliser(entree.Objet.Name).Contains(_requete)
			select entree).ToList();
		List<int> list4 = (_tri switch
		{
			Tri.Rarete => list3.OrderByDescending<(int, ItemDescriptor, int), int>(((int Case, ItemDescriptor Objet, int Quantite) entree) => JetDObjet.Rarete(entree.Objet, ProprietesDe(me, entree.Case))), 
			Tri.Nom => list3.OrderBy<(int, ItemDescriptor, int), string>(((int Case, ItemDescriptor Objet, int Quantite) entree) => Normaliser(entree.Objet.Name)), 
			Tri.Type => from entree in list3
				orderby entree.Objet.ItemType, Normaliser(entree.Objet.Name)
				select entree, 
			Tri.Quantite => list3.OrderByDescending<(int, ItemDescriptor, int), int>(((int Case, ItemDescriptor Objet, int Quantite) entree) => entree.Quantite), 
			Tri.Valeur => list3.OrderByDescending<(int, ItemDescriptor, int), long>(((int Case, ItemDescriptor Objet, int Quantite) entree) => (long)entree.Objet.Price * (long)entree.Quantite), 
			_ => list3.OrderBy(entree => 0), 
		}).Select<(int, ItemDescriptor, int), int>(((int Case, ItemDescriptor Objet, int Quantite) entree) => entree.Case).ToList();
		bool flag = _categorie == Categorie.Tout && _requete.Length == 0;
		if (flag)
		{
			list4.AddRange(list2.Take(Math.Max(0, Player.CapaciteDuSac - list.Count)));
		}
		string detail = (flag ? Strings.Personnage.BagCount.ToString(list.Count) : Strings.Personnage.BagFiltered.ToString(list3.Count, list.Count));
		Heading(renderer, 558f, 108f, 532f, Strings.Personnage.BagHeading, detail);
		DessinerChampRecherche(renderer);
		DessinerTri(renderer);
		DessinerCategories(renderer, list);
		DessinerGrille(renderer, me, list4, now);
		DessinerPiedSac(renderer, me);
	}

	private void DessinerChampRecherche(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		Rectangle rect = _d.R(556f, 136f, 412f, 34f);
		TechniqueArt.Fill(renderer, rect, A(new Color(255, 7, 10, 18)));
		TechniqueArt.Outline(renderer, rect, A(_recherche.HasFocus ? TechniqueArt.Ki : TechniqueArt.Line2));
		int num = _d.L(14f);
		TechniqueArt.Image(renderer, "inv_loupe.png", new Rectangle(rect.X + _d.L(10f), rect.Y + (rect.Height - num) / 2, num, num), A(TechniqueArt.Soft));
		if (string.IsNullOrEmpty(_recherche.Text) && !_recherche.HasFocus)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), Strings.Personnage.Search, _d.X(584f), rect.Y + rect.Height / 2, A(TechniqueArt.Dim));
		}
	}

	private void DessinerTri(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		Rectangle rect = _d.R(976f, 136f, 116f, 34f);
		bool flag = IsHot("tri");
		TechniqueArt.Fill(renderer, rect, A(new Color(255, 7, 10, 18)));
		TechniqueArt.Outline(renderer, rect, A(flag ? TechniqueArt.Ki : TechniqueArt.Line2));
		string text = _tri switch
		{
			Tri.Rarete => Strings.Personnage.SortRarity, 
			Tri.Nom => Strings.Personnage.SortName, 
			Tri.Type => Strings.Personnage.SortType, 
			Tri.Quantite => Strings.Personnage.SortQuantity, 
			Tri.Valeur => Strings.Personnage.SortValue, 
			_ => Strings.Personnage.SortNone, 
		};
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(13f), text, rect.X + _d.L(10f), rect.Y + rect.Height / 2, A(TechniqueArt.Text));
		TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(12f), "↓", rect.Right - _d.L(10f), rect.Y + rect.Height / 2, A(TechniqueArt.Soft));
		AddHit(rect, "tri", delegate
		{
			_tri = (Tri)((int)(_tri + 1) % Enum.GetValues<Tri>().Length);
		});
	}

	private void DessinerCategories(Intersect.Client.Framework.Gwen.Renderer.Base renderer, List<(int Case, ItemDescriptor Objet, int Quantite)> objets)
	{
		(Categorie, string, string)[] array = new(Categorie, string, string)[7]
		{
			(Categorie.Tout, Strings.Personnage.CategoryAll, "tout"),
			(Categorie.Equipement, Strings.Personnage.CategoryEquipment, "equipement"),
			(Categorie.Consommables, Strings.Personnage.CategoryConsumables, "consommable"),
			(Categorie.Techniques, Strings.Personnage.CategoryTechniques, "technique"),
			(Categorie.Ressources, Strings.Personnage.CategoryResources, "ressource"),
			(Categorie.Quete, Strings.Personnage.CategoryQuest, "quete"),
			(Categorie.Sacs, Strings.Personnage.CategoryBags, "sac")
		};
		(Categorie, string, string) tuple = array.First(((Categorie Categorie, string Nom, string Glyphe) entree) => entree.Categorie == _categorie);
		float num = (float)(TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(12f), tuple.Item2.ToUpperInvariant(), _d.Em(0.08f, 12f)) + TechniqueArt.Measure(renderer, TechniqueArt.Heavy, Pt(15f), "00").X) / _d.S + 18f + 6f + 6f + 32f;
		float num2 = (536f - num - 6f * (float)(array.Length - 1)) / (float)(array.Length - 1);
		float num3 = 556f;
		(Categorie, string, string)[] array2 = array;
		for (int num4 = 0; num4 < array2.Length; num4++)
		{
			(Categorie, string, string) tuple2 = array2[num4];
			Categorie categorie = tuple2.Item1;
			string item = tuple2.Item2;
			string item2 = tuple2.Item3;
			int num5 = ((categorie == Categorie.Tout) ? objets.Count : objets.Count<(int, ItemDescriptor, int)>(((int Case, ItemDescriptor Objet, int Quantite) entree) => CategorieDe(entree.Objet) == categorie));
			bool flag = categorie == _categorie;
			float num6 = (flag ? num : num2);
			Rectangle rect = _d.R(num3, 180f, num6, 38f);
			string key = $"categorie:{categorie}";
			bool flag2 = IsHot(key);
			TechniqueArt.Patch(renderer, "tech_chanfrein8.png", rect, A(new Color(230, 7, 10, 18)), 10);
			if (flag)
			{
				TechniqueArt.Patch(renderer, "tech_chanfrein8_degrade.png", rect, A(TechniqueArt.Mix(TechniqueArt.Ki, new Color(255, 20, 27, 42), 0.3f)), 10);
			}
			TechniqueArt.Patch(renderer, "tech_chanfrein8_bord.png", rect, A(flag ? TechniqueArt.Alpha(TechniqueArt.Ki, 0.8f) : (flag2 ? TechniqueArt.Line2 : TechniqueArt.Line)), 10);
			float num7 = ((num5 == 0 && !flag) ? 0.45f : 1f);
			int num8 = _d.L(18f);
			string text = num5.ToString(CultureInfo.InvariantCulture);
			int x = TechniqueArt.Measure(renderer, TechniqueArt.Heavy, Pt(15f), text).X;
			int num9 = num8 + _d.L(6f) + x;
			if (flag)
			{
				num9 += TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(12f), item.ToUpperInvariant(), _d.Em(0.08f, 12f)) + _d.L(6f);
			}
			int num10 = rect.X + (rect.Width - num9) / 2;
			int num11 = rect.Y + rect.Height / 2;
			TechniqueArt.Image(renderer, "inv_glyphe_" + item2 + ".png", new Rectangle(num10, num11 - num8 / 2, num8, num8), A(TechniqueArt.White, (flag ? 1f : 0.5f) * num7));
			num10 += num8 + _d.L(6f);
			if (flag)
			{
				num10 += TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), item.ToUpperInvariant(), num10, num11, _d.Em(0.08f, 12f), A(TechniqueArt.White)) + _d.L(6f);
			}
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(15f), text, num10, num11, A(flag ? TechniqueArt.White : TechniqueArt.Soft, num7));
			AddHit(rect, key, delegate
			{
				_categorie = categorie;
			});
			num3 += num6 + 6f;
		}
	}

	private void DessinerGrille(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, List<int> montrees, long now)
	{
		int num = ((_selectionSac >= 0) ? (-1) : _selectionEmplacement);
		for (int i = 0; i < 56; i++)
		{
			Rectangle rectangle = _d.R(556f + (float)(i % 8) * 68f, 230f + (float)(i / 8) * 68f, 62f, 62f);
			if (i >= montrees.Count)
			{
				TechniqueArt.Fill(renderer, rectangle, A(new Color(140, 7, 10, 18)));
				TechniqueArt.Outline(renderer, rectangle, A(TechniqueArt.Alpha(TechniqueArt.Line, 0.5f)));
				continue;
			}
			int num2 = montrees[i];
			_cases.Add((rectangle, num2));
			IItem item = me.Inventory[num2];
			if (item == null || !DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var databaseObject))
			{
				TechniqueArt.Fill(renderer, rectangle, A(new Color(140, 7, 10, 18)));
				TechniqueArt.Outline(renderer, rectangle, A(TechniqueArt.Alpha(TechniqueArt.Line, 0.5f)));
				AddHit(rectangle, $"case:{num2}", delegate
				{
				});
				continue;
			}
			string key = $"case:{num2}";
			bool flag = IsHot(key);
			bool flag2 = num2 == _selectionSac;
			bool flag3 = _glisse && num2 == _appuiSac;
			int num3 = JetDObjet.Rarete(databaseObject, item.ItemProperties);
			Color color = CouleurRarete(num3);
			TechniqueArt.FillVertical(renderer, rectangle, A(flag ? new Color(255, 21, 32, 52) : new Color(255, 13, 19, 32)), A(new Color(255, 7, 10, 18)));
			if (num3 >= 3)
			{
				TechniqueArt.Glow(renderer, (float)rectangle.X + (float)rectangle.Width / 2f, (float)rectangle.Y + (float)rectangle.Height * 0.6f, (float)rectangle.Width * 0.55f, A(color, 0.22f));
			}
			TechniqueArt.Outline(renderer, rectangle, A(flag2 ? TechniqueArt.Gold : (flag ? TechniqueArt.Line2 : TechniqueArt.Line)));
			if (flag2)
			{
				TechniqueArt.Glow(renderer, (float)rectangle.X + (float)rectangle.Width / 2f, (float)rectangle.Y + (float)rectangle.Height / 2f, (float)rectangle.Width * 0.8f, A(TechniqueArt.Gold, 0.25f));
			}
			if (num3 > 0)
			{
				TechniqueArt.Fill(renderer, new Rectangle(rectangle.X + 1, rectangle.Bottom - _d.L(3f) - 1, rectangle.Width - 2, _d.L(3f)), A(color, 0.85f));
			}
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Item, databaseObject.Icon);
			if (texture != null)
			{
				int num4 = _d.L(48f);
				bool flag4 = me.IsItemOnCooldown(num2);
				Color color2 = (flag4 ? new Color(100, databaseObject.Color.R, databaseObject.Color.G, databaseObject.Color.B) : databaseObject.Color);
				SaiyanHud.Draw(renderer, texture, rectangle.X + (rectangle.Width - num4) / 2, rectangle.Y + (rectangle.Height - num4) / 2, num4, num4, A(color2, flag3 ? 0.35f : 1f));
				if (flag4)
				{
					string text = Math.Ceiling((double)me.GetItemRemainingCooldown(num2) / 1000.0).ToString(CultureInfo.InvariantCulture);
					TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(16f), text, rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2, A(TechniqueArt.White));
				}
			}
			if (databaseObject.IsStackable && item.Quantity > 1)
			{
				TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(15f), Strings.FormatQuantityAbbreviated(item.Quantity), rectangle.Right - _d.L(4f), rectangle.Bottom - _d.L(12f), A(TechniqueArt.White));
			}
			if (databaseObject.ItemType == ItemType.Equipment && MeilleurPartout(me, num2, databaseObject))
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(13f), "↑", rectangle.X + _d.L(4f), rectangle.Y + _d.L(9f), A(TechniqueArt.Green));
			}
			int index = num2;
			AddHit(rectangle, key, delegate
			{
				Selectionner(index);
			});
		}
	}

	private void DessinerPiedSac(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me)
	{
		Player.Charge charge = me.LireLaCharge();
		string text = Strings.Inventaire.ChargeCount.ToString(SaiyanHud.Amount(charge.Porte), SaiyanHud.Amount(charge.Capacite));
		int num = TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(13f), text, _d.X(556f), _d.Y(716f), A(charge.Surcharge ? TechniqueArt.Red : TechniqueArt.Soft));
		Rectangle rect = new Rectangle(_d.X(556f) + num + _d.L(12f), _d.Y(712f), _d.X(1092f) - (_d.X(556f) + num + _d.L(12f)), _d.L(8f));
		TechniqueArt.Fill(renderer, rect, A(new Color(255, 7, 10, 18)));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line2));
		int num2 = (int)((float)(rect.Width - 2) * Math.Min(1f, (charge.Capacite <= 0) ? 1f : ((float)charge.Porte / (float)charge.Capacite)));
		if (num2 > 0)
		{
			TechniqueArt.FillHorizontal(renderer, new Rectangle(rect.X + 1, rect.Y + 1, num2, rect.Height - 2), A(new Color(255, 178, 90, 18)), A(TechniqueArt.Ki), A(new Color(255, 255, 194, 122)));
		}
		Heading(renderer, 556f, 732f, 536f, Strings.Personnage.HotbarHeading, Strings.Personnage.HotbarDetail);
		DessinerRaccourcis(renderer, me);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), Strings.Personnage.HintDoubleClick, _d.X(556f), _d.Y(836f), A(TechniqueArt.Dim));
		TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(12f), Strings.Personnage.HintDrag, _d.X(1092f), _d.Y(836f), A(TechniqueArt.Dim));
	}

	private void DessinerRaccourcis(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me)
	{
		int num = Math.Min(10, me.Hotbar.Length);
		float num2 = (536f - 6f * (float)(num - 1)) / (float)num;
		int selection = _selectionSac;
		for (int i = 0; i < num; i++)
		{
			Rectangle rectangle = _d.R(556f + (float)i * (num2 + 6f), 756f, num2, 52f);
			_casesRaccourcis.Add((rectangle, i));
			string key = $"raccourci:{i}";
			bool flag = IsHot(key) || (_glisse && RaccourciSous(LocalMouse()) == i);
			TechniqueArt.Fill(renderer, rectangle, A(new Color(255, 7, 10, 18)));
			TechniqueArt.Outline(renderer, rectangle, A(flag ? TechniqueArt.Gold : TechniqueArt.Line));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(12f), ((i + 1) % 10).ToString(CultureInfo.InvariantCulture), rectangle.X + _d.L(4f), rectangle.Y + _d.L(8f), A(TechniqueArt.Dim));
			Guid guid = me.Hotbar[i]?.ItemOrSpellId ?? Guid.Empty;
			if (guid != Guid.Empty)
			{
				IGameTextureOrNull(guid, out IGameTexture icone, out Color teinte);
				if (icone != null)
				{
					int num3 = _d.L(32f);
					SaiyanHud.Draw(renderer, icone, rectangle.X + (rectangle.Width - num3) / 2, rectangle.Y + (rectangle.Height - num3) / 2, num3, num3, A(teinte));
				}
				if (DatabaseObject<ItemDescriptor>.TryGet(guid, out var databaseObject) && databaseObject.IsStackable)
				{
					int quantityOfItemInInventory = me.GetQuantityOfItemInInventory(guid);
					TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(12f), quantityOfItemInInventory.ToString(CultureInfo.InvariantCulture), rectangle.Right - _d.L(3f), rectangle.Bottom - _d.L(9f), A(TechniqueArt.White));
				}
			}
			int raccourci = i;
			AddHit(rectangle, key, delegate
			{
				if (selection >= 0)
				{
					me.AddToHotbar(raccourci, 0, selection);
				}
			});
		}
	}

	private static void IGameTextureOrNull(Guid contenu, out IGameTexture? icone, out Color teinte)
	{
		teinte = TechniqueArt.White;
		if (DatabaseObject<ItemDescriptor>.TryGet(contenu, out var databaseObject))
		{
			icone = Globals.ContentManager.GetTexture(TextureType.Item, databaseObject.Icon);
			teinte = databaseObject.Color;
		}
		else
		{
			icone = (DatabaseObject<SpellDescriptor>.TryGet(contenu, out var databaseObject2) ? Globals.ContentManager.GetTexture(TextureType.Spell, databaseObject2.Icon) : null);
		}
	}

	private void CommencerAppui(Point local)
	{
		AnnulerGlisser();
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		int? num = CaseSacSous(local);
		if (num.HasValue)
		{
			int valueOrDefault = num.GetValueOrDefault();
			Guid? guid = me.Inventory[valueOrDefault]?.ItemId;
			if (guid.HasValue)
			{
				Guid valueOrDefault2 = guid.GetValueOrDefault();
				if (valueOrDefault2 != Guid.Empty)
				{
					_appuiSac = valueOrDefault;
					_appuiPosition = local;
					return;
				}
			}
		}
		num = EmplacementSous(local);
		if (num.HasValue)
		{
			int valueOrDefault3 = num.GetValueOrDefault();
			if (ObjetPorte(me, valueOrDefault3, out var _) != null)
			{
				_appuiEmplacement = valueOrDefault3;
				_appuiPosition = local;
			}
		}
	}

	private void SuivreAppui(Point local)
	{
		if (!_glisse && (_appuiSac >= 0 || _appuiEmplacement >= 0) && Math.Abs(local.X - _appuiPosition.X) + Math.Abs(local.Y - _appuiPosition.Y) > _d.L(8f))
		{
			_glisse = true;
		}
	}

	private void AnnulerGlisser()
	{
		_appuiSac = -1;
		_appuiEmplacement = -1;
		_glisse = false;
	}

	private ItemDescriptor? ObjetGlisse(Player me)
	{
		if (_appuiSac >= 0)
		{
			if (!DatabaseObject<ItemDescriptor>.TryGet(me.Inventory[_appuiSac]?.ItemId ?? Guid.Empty, out var databaseObject))
			{
				return null;
			}
			return databaseObject;
		}
		if (_appuiEmplacement < 0)
		{
			return null;
		}
		int caseSac;
		return ObjetPorte(me, _appuiEmplacement, out caseSac);
	}

	private void Lacher(Point local)
	{
		Player me = Globals.Me;
		if (me == null)
		{
			AnnulerGlisser();
			return;
		}
		int? num = RaccourciSous(local);
		if (_appuiSac >= 0)
		{
			if (num.HasValue)
			{
				int valueOrDefault = num.GetValueOrDefault();
				me.AddToHotbar(valueOrDefault, 0, _appuiSac);
			}
			else
			{
				int? num2 = CaseSacSous(local);
				if (num2.HasValue)
				{
					int valueOrDefault2 = num2.GetValueOrDefault();
					if (valueOrDefault2 != _appuiSac)
					{
						me.SwapItems(_appuiSac, valueOrDefault2);
						goto IL_0149;
					}
				}
				num2 = EmplacementSous(local);
				if (num2.HasValue)
				{
					int valueOrDefault3 = num2.GetValueOrDefault();
					if (DatabaseObject<ItemDescriptor>.TryGet(me.Inventory[_appuiSac]?.ItemId ?? Guid.Empty, out var databaseObject) && databaseObject.ItemType == ItemType.Equipment && databaseObject.EquipmentSlot == valueOrDefault3)
					{
						me.TryUseItem(_appuiSac);
					}
				}
			}
		}
		else if (_appuiEmplacement >= 0)
		{
			ObjetPorte(me, _appuiEmplacement, out var caseSac);
			if (num.HasValue)
			{
				int valueOrDefault4 = num.GetValueOrDefault();
				if (caseSac >= 0)
				{
					me.AddToHotbar(valueOrDefault4, 0, caseSac);
					goto IL_0149;
				}
			}
			if (_zoneSac.Contains(local.X, local.Y))
			{
				PacketSender.SendUnequipItem(_appuiEmplacement);
			}
		}
		goto IL_0149;
		IL_0149:
		AnnulerGlisser();
	}

	private void DessinerGlisser(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me)
	{
		if (!_glisse)
		{
			return;
		}
		ItemDescriptor itemDescriptor = ObjetGlisse(me);
		if (itemDescriptor != null)
		{
			if (_appuiEmplacement >= 0)
			{
				TechniqueArt.Outline(renderer, _zoneSac, A(TechniqueArt.Alpha(TechniqueArt.Gold, 0.6f)), Math.Max(1, _d.L(2f)));
			}
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Item, itemDescriptor.Icon);
			if (texture != null)
			{
				Point point = LocalMouse();
				int num = _d.L(48f);
				SaiyanHud.Draw(renderer, texture, point.X - num / 2, point.Y - num / 2, num, num, A(itemDescriptor.Color, 0.9f));
			}
		}
	}

	internal static ItemDescriptor? ObjetPorte(Player me, int emplacement, out int caseSac)
	{
		caseSac = -1;
		if (emplacement < 0 || emplacement >= me.MyEquipment.Length)
		{
			return null;
		}
		caseSac = me.MyEquipment[emplacement];
		if (caseSac < 0 || caseSac >= me.Inventory.Length)
		{
			return null;
		}
		if (!DatabaseObject<ItemDescriptor>.TryGet(me.Inventory[caseSac]?.ItemId ?? Guid.Empty, out var databaseObject))
		{
			return null;
		}
		return databaseObject;
	}

	internal static ItemProperties? ProprietesDe(Player me, int caseSac)
	{
		if (caseSac < 0 || caseSac >= me.Inventory.Length)
		{
			return null;
		}
		return me.Inventory[caseSac]?.ItemProperties;
	}

	internal static Color CouleurRarete(int rarete)
	{
		if (!CustomColors.Items.Rarities.TryGetValue(rarete, out var value) || !(value != null))
		{
			return new Color(255, 242, 242, 242);
		}
		return value;
	}

	private int? EmplacementSous(Point local)
	{
		foreach (var (rectangle, value) in _casesEmplacements)
		{
			if (rectangle.Contains(local.X, local.Y))
			{
				return value;
			}
		}
		return null;
	}

	private void DessinerScene(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, long now)
	{
		Panel(renderer, _d.R(32f, 96f, 488f, 568f));
		if (_onglet == Onglet.Equipement)
		{
			int value = me.MyEquipment.Count((int caseSac) => caseSac >= 0 && caseSac < me.Inventory.Length && me.Inventory[caseSac]?.ItemId != Guid.Empty);
			Heading(renderer, 50f, 108f, 452f, Strings.Personnage.WornHeading, $"{value} / {Options.Instance.Equipment.Slots.Count}");
		}
		else if (_onglet == Onglet.Personnalisation)
		{
			Heading(renderer, 50f, 108f, 452f, Strings.Personnage.PreviewHeading, null);
		}
		else
		{
			Heading(renderer, 50f, 108f, 452f, me.Name, null);
		}
		TechniqueArt.GlowRect(renderer, _d.R(126f, 166f, 300f, 380f), A(new Color(255, 255, 140, 50), 0.18f), additive: false);
		DessinerDalle(renderer);
		DessinerPersonnage(renderer, me, now);
		DessinerFleches(renderer);
		switch (_onglet)
		{
		case Onglet.Equipement:
			DessinerEmplacements(renderer, me, now);
			DessinerBoules(renderer, now);
			break;
		case Onglet.Caracteristiques:
			DessinerPuissance(renderer, me);
			break;
		default:
			DessinerPlaque(renderer, me);
			DessinerBoules(renderer, now);
			break;
		}
	}

	private void DessinerDalle(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		int num = _d.X(276f);
		int num2 = _d.Y(498f);
		int num3 = _d.L(112f);
		float num4 = 0.32f;
		(float, float)[] array = new(float, float)[8];
		for (int i = 0; i < 8; i++)
		{
			float x = (float)Math.PI / 8f + (float)i * (float)Math.PI / 4f;
			array[i] = ((float)num + MathF.Cos(x) * (float)num3, (float)num2 + MathF.Sin(x) * (float)num3 * num4);
		}
		int num5 = (int)MathF.Floor(array.Min(((float X, float Y) p) => p.Y));
		int num6 = (int)MathF.Ceiling(array.Max(((float X, float Y) p) => p.Y));
		Color color = SaiyanHud.WithAlpha(new Color(255, 255, 160, 70), 0.55f * _fade);
		for (int num7 = num5; num7 < num6; num7++)
		{
			if (Traverse(array, (float)num7 + 0.5f, out var gauche, out var droite))
			{
				float num8 = ((float)num7 + 0.5f - (float)num5) / (float)Math.Max(1, num6 - num5);
				Color color2 = SaiyanHud.Lerp(new Color(255, 90, 94, 104), new Color(255, 46, 50, 60), MathF.Abs(num8 - 0.45f) * 1.6f);
				HudShapes.Span(renderer, gauche, droite, num7, 1, A(color2));
				HudShapes.Span(renderer, gauche, gauche + 2f, num7, 1, color);
				HudShapes.Span(renderer, droite - 2f, droite, num7, 1, color);
			}
		}
		HudShapes.Span(renderer, array[5].Item1, array[6].Item1, num5, 2, color);
		HudShapes.Span(renderer, array[2].Item1, array[1].Item1, num6 - 2, 2, color);
		TechniqueArt.Image(renderer, "tech_ombre.png", new Rectangle(num - _d.L(42f), num2 - _d.L(9f), _d.L(84f), _d.L(18f)), A(TechniqueArt.Black, 0.45f));
	}

	internal static bool Traverse((float X, float Y)[] points, float y, out float gauche, out float droite)
	{
		gauche = float.MaxValue;
		droite = float.MinValue;
		for (int i = 0; i < points.Length; i++)
		{
			(float, float) tuple = points[i];
			(float, float) tuple2 = points[(i + 1) % points.Length];
			if ((!(y < tuple.Item2) || !(y < tuple2.Item2)) && (!(y > tuple.Item2) || !(y > tuple2.Item2)) && !(MathF.Abs(tuple2.Item2 - tuple.Item2) < 0.0001f))
			{
				float val = tuple.Item1 + (y - tuple.Item2) / (tuple2.Item2 - tuple.Item2) * (tuple2.Item1 - tuple.Item1);
				gauche = Math.Min(gauche, val);
				droite = Math.Max(droite, val);
			}
		}
		return droite > gauche;
	}

	private void DessinerPersonnage(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, long now)
	{
		DessinerPersonnage(renderer, me, now, _d.X(276f), _d.Y(496f), Math.Max(1, (int)MathF.Round(6f * _d.S)), Tour[_tour], _fade);
	}

	internal static void DessinerPersonnage(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, long now, int centreX, int sol, int zoom, int rangee, float fondu)
	{
		if (me.Texture == null)
		{
			return;
		}
		SpriteSheets.Pose? pose = SpriteSheets.StandingBody(me.Sprite, me.Texture.Name, rangee);
		if (!pose.HasValue)
		{
			return;
		}
		SpriteSheets.Pose valueOrDefault = pose.GetValueOrDefault();
		if (valueOrDefault.IsEmpty)
		{
			return;
		}
		int num = Math.Max(1, valueOrDefault.Texture.Width / Math.Max(1, valueOrDefault.Frame.Width));
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(valueOrDefault.Texture.Name ?? string.Empty);
		bool flag = fileNameWithoutExtension.EndsWith("_idle", StringComparison.OrdinalIgnoreCase);
		int num2 = Math.Max(1, Options.Instance.Sprites.IdleFrameDuration);
		int num3 = (int)(flag ? (now % ((long)num * (long)num2) / num2) : 0);
		bool flag2 = string.IsNullOrWhiteSpace(me.TransformedSprite) || Transformation.KeepsPaperdolls(me);
		int caseSac = rangee switch
		{
			1 => 2, 
			2 => 3, 
			3 => 0, 
			_ => 1, 
		};
		int value = caseSac;
		List<string> list = Options.Instance.Equipment.Paperdoll.Directions[Math.Clamp(value, 0, Options.Instance.Equipment.Paperdoll.Directions.Length - 1)];
		List<string> slots = Options.Instance.Equipment.Slots;
		int gauche = centreX - valueOrDefault.Frame.Width * zoom / 2;
		int haut = sol - valueOrDefault.Frame.Height * zoom;
		DessinerAuras(renderer, me, valueOrDefault.Frame.Width, valueOrDefault.Frame.Height, gauche, haut, zoom, devant: false, fondu);
		foreach (string item in list)
		{
			if (item == "Player")
			{
				DessinerCase(renderer, valueOrDefault, num3, num, centreX, sol, zoom, me.Color ?? TechniqueArt.White, fondu);
				continue;
			}
			int num4 = slots.IndexOf(item);
			if (!flag2 || num4 < 0)
			{
				continue;
			}
			ItemDescriptor itemDescriptor = ObjetPorte(me, num4, out caseSac);
			if (itemDescriptor == null)
			{
				continue;
			}
			string text = Paperdolls.ForForm(me, Paperdolls.ForClass((me.Gender == Gender.Male) ? itemDescriptor.MalePaperdoll : itemDescriptor.FemalePaperdoll, me.Class));
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Paperdoll, Path.GetFileNameWithoutExtension(text) + "-" + Paperdolls.PlancheDesEnveloppes(fileNameWithoutExtension) + ".png");
			if (texture != null && texture.Width > 0 && texture.Height > 0)
			{
				DessinerEnveloppe(renderer, texture, valueOrDefault, num3, num, centreX, sol, zoom, itemDescriptor.Color, fondu);
				continue;
			}
			pose = SpriteSheets.StandingOutfit(me.Sprite, text, valueOrDefault, rangee);
			if (pose.HasValue)
			{
				SpriteSheets.Pose valueOrDefault2 = pose.GetValueOrDefault();
				if (!valueOrDefault2.IsEmpty)
				{
					int num5 = Math.Max(1, valueOrDefault2.Texture.Width / Math.Max(1, valueOrDefault2.Frame.Width));
					DessinerCase(renderer, valueOrDefault2, (num5 == num) ? num3 : 0, num5, centreX, sol, zoom, itemDescriptor.Color, fondu);
				}
			}
		}
		DessinerAuras(renderer, me, valueOrDefault.Frame.Width, valueOrDefault.Frame.Height, gauche, haut, zoom, devant: true, fondu);
	}

	private static void DessinerAuras(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, int largeur, int hauteur, int gauche, int haut, int zoom, bool devant, float fondu)
	{
		BoulesDeCristal.DessinerApercu(me, new FloatRect(0f, 0f, largeur, hauteur), devant, zoom, delegate(IGameTexture atlas, FloatRect source, FloatRect destination, Color teinte)
		{
			SaiyanHud.Draw(renderer, atlas, gauche + (int)MathF.Round(destination.X * (float)zoom), haut + (int)MathF.Round(destination.Y * (float)zoom), (int)MathF.Round(destination.Width * (float)zoom), (int)MathF.Round(destination.Height * (float)zoom), TechniqueArt.Alpha(teinte, fondu), (source.X + 0.01f) / (float)atlas.Width, (source.Y + 0.01f) / (float)atlas.Height, (source.X + source.Width - 0.01f) / (float)atlas.Width, (source.Y + source.Height - 0.01f) / (float)atlas.Height);
		});
	}

	private static void DessinerEnveloppe(Intersect.Client.Framework.Gwen.Renderer.Base renderer, IGameTexture calque, SpriteSheets.Pose corps, int image, int colonnes, int centreX, int sol, int zoom, Color teinte, float fondu)
	{
		(int X, int Y, int Width, int Height) frame = corps.Frame;
		int item = frame.X;
		int item2 = frame.Y;
		int item3 = frame.Width;
		int item4 = frame.Height;
		int num = Math.Max(1, corps.Texture.Height / Math.Max(1, item4));
		float num2 = (float)calque.Width / (float)colonnes;
		float num3 = (float)calque.Height / (float)num;
		float num4 = (num2 - (float)item3) / 2f;
		float num5 = (num3 - (float)item4) / 2f;
		float num6 = num3 - num5;
		int num7 = item / Math.Max(1, item3) + image % Math.Max(1, colonnes);
		int num8 = item2 / Math.Max(1, item4);
		float num9 = (float)num7 * num2;
		float num10 = (float)num8 * num3;
		SaiyanHud.Draw(renderer, calque, (int)MathF.Round((float)centreX - (float)(item3 * zoom) / 2f - num4 * (float)zoom), (int)MathF.Round((float)(sol - item4 * zoom) - num5 * (float)zoom), (int)MathF.Round(num2 * (float)zoom), (int)MathF.Round(num6 * (float)zoom), TechniqueArt.Alpha(teinte, fondu), (num9 + 0.01f) / (float)calque.Width, (num10 + 0.01f) / (float)calque.Height, (num9 + num2 - 0.01f) / (float)calque.Width, (num10 + num6 - 0.01f) / (float)calque.Height);
	}

	private static void DessinerCase(Intersect.Client.Framework.Gwen.Renderer.Base renderer, SpriteSheets.Pose pose, int image, int colonnes, int centreX, int sol, int zoom, Color teinte, float fondu)
	{
		(int X, int Y, int Width, int Height) frame = pose.Frame;
		int item = frame.X;
		int item2 = frame.Y;
		int item3 = frame.Width;
		int item4 = frame.Height;
		int num = item + image % Math.Max(1, colonnes) * item3;
		IGameTexture texture = pose.Texture;
		SaiyanHud.Draw(renderer, texture, centreX - item3 * zoom / 2, sol - item4 * zoom, item3 * zoom, item4 * zoom, TechniqueArt.Alpha(teinte, fondu), ((float)num + 0.01f) / (float)texture.Width, ((float)item2 + 0.01f) / (float)texture.Height, ((float)(num + item3) - 0.01f) / (float)texture.Width, ((float)(item2 + item4) - 0.01f) / (float)texture.Height);
	}

	private void DessinerFleches(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		(string, float, string, int)[] array = new(string, float, string, int)[2]
		{
			("tourner:g", 182f, "←", 3),
			("tourner:d", 336f, "→", 1)
		};
		(string, float, string, int)[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			(string, float, string, int) tuple = array2[i];
			string item = tuple.Item1;
			float item2 = tuple.Item2;
			string item3 = tuple.Item3;
			int pas = tuple.Item4;
			Rectangle rect = _d.R(item2, 482f, 34f, 34f);
			bool flag = IsHot(item);
			TechniqueArt.Image(renderer, "tech_hexa.png", rect, A(new Color(230, 12, 17, 28)));
			TechniqueArt.Image(renderer, "tech_hexa_bord.png", rect, A(flag ? TechniqueArt.Ki : TechniqueArt.Line2));
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(16f), item3, rect.X + rect.Width / 2, rect.Y + rect.Height / 2 - _d.L(1f), A(flag ? TechniqueArt.White : TechniqueArt.Soft));
			AddHit(rect, item, delegate
			{
				_tour = (_tour + pas) % Tour.Length;
			});
		}
	}

	private void DessinerEmplacements(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, long now)
	{
		int caseSac;
		int emplacement;
		ItemDescriptor itemDescriptor = ObjetChoisi(me, out caseSac, out emplacement);
		int num;
		if (_glisse)
		{
			ItemDescriptor itemDescriptor2 = ObjetGlisse(me);
			if (itemDescriptor2 != null && itemDescriptor2.ItemType == ItemType.Equipment)
			{
				num = itemDescriptor2.EquipmentSlot;
				goto IL_004f;
			}
		}
		num = -1;
		goto IL_004f;
		IL_004f:
		int num2 = num;
		float num3 = 0.5f + 0.5f * MathF.Sin((float)(now % 900) / 900f * ((float)Math.PI * 2f));
		List<string> slots = Options.Instance.Equipment.Slots;
		for (int i = 0; i < slots.Count; i++)
		{
			string text = Normaliser(slots[i]);
			if (!PlacesEmplacements.TryGetValue(text, out (float, float) value))
			{
				continue;
			}
			Rectangle rectangle = _d.R(value.Item1 + 4f, value.Item2, 76f, 84f);
			_casesEmplacements.Add((rectangle, i));
			int caseSac2;
			ItemDescriptor itemDescriptor3 = ObjetPorte(me, i, out caseSac2);
			int rarete = ((itemDescriptor3 != null) ? JetDObjet.Rarete(itemDescriptor3, ProprietesDe(me, caseSac2)) : 0);
			Color color = ((itemDescriptor3 == null) ? TechniqueArt.Line2 : CouleurRarete(rarete));
			int num4;
			if (_selectionEmplacement != i)
			{
				if (!_glisse && itemDescriptor != null && itemDescriptor.ItemType == ItemType.Equipment)
				{
					ItemDescriptor itemDescriptor4 = itemDescriptor;
					if (_selectionSac >= 0)
					{
						num4 = ((itemDescriptor4.EquipmentSlot == i) ? 1 : 0);
						goto IL_018b;
					}
				}
				num4 = 0;
			}
			else
			{
				num4 = 1;
			}
			goto IL_018b;
			IL_018b:
			bool flag = (byte)num4 != 0;
			bool flag2 = IsHot($"emplacement:{i}");
			if (flag || flag2)
			{
				TechniqueArt.Glow(renderer, (float)rectangle.X + (float)rectangle.Width / 2f, (float)rectangle.Y + (float)rectangle.Height / 2f, _d.L(54f), A(color, flag ? 0.45f : 0.25f));
			}
			if (num2 == i)
			{
				TechniqueArt.Glow(renderer, (float)rectangle.X + (float)rectangle.Width / 2f, (float)rectangle.Y + (float)rectangle.Height / 2f, _d.L(60f), A(TechniqueArt.Gold, 0.3f + 0.4f * num3));
			}
			IGameTexture icon = ((itemDescriptor3 == null) ? null : Globals.ContentManager.GetTexture(TextureType.Item, itemDescriptor3.Icon));
			DrawHexEmblem(renderer, rectangle, icon, -1, color, 0.62f);
			if (itemDescriptor3 == null)
			{
				int num5 = _d.L(30f);
				TechniqueArt.Image(renderer, "inv_emplacement_" + text + ".png", new Rectangle(rectangle.X + (rectangle.Width - num5) / 2, rectangle.Y + (rectangle.Height - num5) / 2, num5, num5), A(TechniqueArt.White, 0.3f));
			}
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(12f), slots[i].ToUpperInvariant(), rectangle.X + rectangle.Width / 2, rectangle.Bottom + _d.L(12f), _d.Em(0.18f, 12f), A(flag ? TechniqueArt.Text : TechniqueArt.Dim));
			int index = i;
			AddHit(rectangle, $"emplacement:{i}", delegate
			{
				if (ObjetPorte(me, index, out var _) != null)
				{
					SelectionnerEmplacement(index);
				}
			});
		}
	}

	private void DessinerBoules(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		int num = Math.Max(0, Options.Instance.Player.DragonBallCount);
		if (num == 0)
		{
			return;
		}
		bool[] dragonBalls = Globals.DragonBalls;
		float num2 = (float)num * 26f + (float)(num - 1) * 10f;
		float num3 = 276f - num2 / 2f;
		for (int i = 0; i < num; i++)
		{
			bool flag = i < dragonBalls.Length && dragonBalls[i];
			float num4 = (flag ? (MathF.Sin((float)(now % 2400) / 2400f * ((float)Math.PI * 2f) + (float)i * 0.9f) * 1.5f) : 0f);
			string text = (flag ? "_owned" : string.Empty);
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Image, $"dragonball{i + 1}{text}.png") ?? Globals.ContentManager.GetTexture(TextureType.Image, "dragonball1" + text + ".png");
			Rectangle rectangle = _d.R(num3 + (float)i * 36f, 624f + num4, 26f, 26f);
			if (flag)
			{
				TechniqueArt.Glow(renderer, (float)rectangle.X + (float)rectangle.Width / 2f, (float)rectangle.Y + (float)rectangle.Height / 2f, _d.L(18f), A(new Color(255, 255, 170, 60), 0.35f));
			}
			SaiyanHud.Draw(renderer, texture, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, A(TechniqueArt.White));
		}
	}

	private void DessinerPanoplies(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me)
	{
		Panel(renderer, _d.R(32f, 680f, 488f, 190f));
		Heading(renderer, 50f, 692f, 452f, Strings.Personnage.SetsHeading, Strings.Personnage.SetsDetail);
		HashSet<Guid> portes = IdsPortes(me);
		HashSet<Guid> apres = PortesApresEchange(me);
		List<(PanoplieDescriptor, int, int)> list = (from panoplie in (from panoplie in portes.Concat(apres ?? new HashSet<Guid>()).Select(PanoplieDescriptor.DeLObjet)
				where panoplie != null
				select panoplie).Distinct()
			select (Panoplie: panoplie, Avant: panoplie.PiecesPortees(portes), Apres: (apres == null) ? panoplie.PiecesPortees(portes) : panoplie.PiecesPortees(apres)) into entree
			orderby Math.Max(entree.Avant, entree.Apres) descending
			select entree).ToList();
		if (list.Count == 0)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), Strings.Personnage.NoSet, _d.X(50f), _d.Y(740f), A(TechniqueArt.Dim));
			return;
		}
		(PanoplieDescriptor, int, int) tuple = list[0];
		PanoplieDescriptor item = tuple.Item1;
		int item2 = tuple.Item2;
		int item3 = tuple.Item3;
		int num = item.Items.Distinct().Count();
		TechniqueArt.Italic(renderer, TechniqueArt.Display, Pt(22f), item.Name, _d.X(50f), _d.Y(734f) - _d.L(13f), A(TechniqueArt.Text));
		float num2 = 50f + (float)TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(22f), item.Name).X / _d.S + 14f;
		foreach (Guid item6 in item.Items.Distinct().Take(4))
		{
			Rectangle rect = _d.R(num2, 721f, 30f, 30f);
			bool flag = portes.Contains(item6);
			TechniqueArt.Fill(renderer, rect, A(new Color(255, 7, 10, 18)));
			TechniqueArt.Outline(renderer, rect, A(flag ? TechniqueArt.Alpha(TechniqueArt.Gold, 0.6f) : TechniqueArt.Line));
			if (DatabaseObject<ItemDescriptor>.TryGet(item6, out var databaseObject))
			{
				IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Item, databaseObject.Icon);
				if (texture != null)
				{
					int num3 = _d.L(3f);
					SaiyanHud.Draw(renderer, texture, rect.X + num3, rect.Y + num3, rect.Width - 2 * num3, rect.Height - 2 * num3, A(TechniqueArt.White, flag ? 1f : 0.3f));
				}
			}
			num2 += 36f;
		}
		string text = ((item2 == item3) ? Strings.Personnage.SetCount.ToString(item2, num) : Strings.Personnage.SetCountChange.ToString(item2, item3, num));
		TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(16f), text, _d.X(502f), _d.Y(736f), A(TechniqueArt.Gold));
		float num4 = 760f;
		foreach (PanoplieBonus item7 in item.Bonuses.OrderBy((PanoplieBonus bonus) => bonus.Pieces).Take(3))
		{
			DessinerPalier(renderer, item7, item2, item3, 50f, num4, 452f);
			num4 += 21f;
		}
		DessinerPieceManquante(renderer, me, item, portes, num4 + 2f);
		if (list.Count > 1)
		{
			(PanoplieDescriptor, int, int) tuple2 = list[1];
			PanoplieDescriptor item4 = tuple2.Item1;
			int avantSeconde = tuple2.Item2;
			int item5 = tuple2.Item3;
			int num5 = item4.Items.Distinct().Count();
			string text2 = ((avantSeconde == item5) ? Strings.Personnage.SetCount.ToString(avantSeconde, num5) : Strings.Personnage.SetCountChange.ToString(avantSeconde, item5, num5));
			PanoplieBonus panoplieBonus = (from bonus in item4.Bonuses
				where bonus.Pieces > avantSeconde
				orderby bonus.Pieces
				select bonus).FirstOrDefault();
			string text3 = ((panoplieBonus == null) ? Strings.Personnage.SetComplete.ToString() : Strings.Personnage.SetNextTier.ToString(panoplieBonus.Pieces - avantSeconde, string.Join(", ", ItemDescriptionWindow.Lignes(panoplieBonus))));
			int num6 = TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(15f), item4.Name, _d.X(50f), _d.Y(852f), A(TechniqueArt.Text));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), text2 + " · " + text3, _d.X(50f) + num6 + _d.L(10f), _d.Y(852f), A(TechniqueArt.Soft));
		}
	}

	private void DessinerPalier(Intersect.Client.Framework.Gwen.Renderer.Base renderer, PanoplieBonus bonus, int avant, int apres, float x, float y, float largeur)
	{
		bool flag = avant >= bonus.Pieces;
		bool flag2 = apres >= bonus.Pieces;
		Color color = ((flag && flag2) ? TechniqueArt.Green : ((!flag && flag2) ? TechniqueArt.Gold : (flag ? TechniqueArt.Red : TechniqueArt.Dim)));
		int num = _d.Y(y);
		TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(14f), bonus.Pieces.ToString(CultureInfo.InvariantCulture), _d.X(x + 8f), num, A(color));
		string text = string.Join(", ", ItemDescriptionWindow.Lignes(bonus));
		int num2 = TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), text, _d.X(x + 28f), num, A(color));
		if (flag && !flag2)
		{
			TechniqueArt.Fill(renderer, new Rectangle(_d.X(x + 28f), num, num2, Math.Max(1, _d.L(1f))), A(TechniqueArt.Red));
		}
		else if (!flag && flag2)
		{
			string text2 = Strings.Personnage.SetWithItem.ToString().ToUpperInvariant();
			int w = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(12f), text2, _d.Em(0.14f, 12f)) + _d.L(14f);
			Rectangle rect = new Rectangle(_d.X(x + 28f) + num2 + _d.L(10f), num - _d.L(8f), w, _d.L(16f));
			TechniqueArt.Fill(renderer, rect, A(TechniqueArt.Gold));
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(12f), text2, rect.X + rect.Width / 2, num, _d.Em(0.14f, 12f), A(new Color(255, 26, 18, 6)));
		}
	}

	private void DessinerPieceManquante(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, PanoplieDescriptor panoplie, HashSet<Guid> portes, float y)
	{
		Guid guid = panoplie.Items.Distinct().FirstOrDefault((Guid id) => !portes.Contains(id));
		if (guid == Guid.Empty || !DatabaseObject<ItemDescriptor>.TryGet(guid, out var databaseObject))
		{
			return;
		}
		int num = _d.Y(y + 6f);
		int num2 = _d.X(50f);
		num2 += TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), Strings.Personnage.SetMissing, num2, num, A(TechniqueArt.Soft)) + _d.L(6f);
		int caseSac = -1;
		for (int num3 = 0; num3 < me.Inventory.Length; num3++)
		{
			IItem obj = me.Inventory[num3];
			if (obj != null && obj.ItemId == guid && !me.MyEquipment.Contains(num3))
			{
				caseSac = num3;
				break;
			}
		}
		bool flag = caseSac >= 0 && IsHot("manquante");
		int num4 = TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(14f), databaseObject.Name, num2, num, A(flag ? TechniqueArt.White : TechniqueArt.Gold));
		if (caseSac >= 0)
		{
			TechniqueArt.Fill(renderer, new Rectangle(num2, num + _d.L(9f), num4, 1), A(TechniqueArt.Gold, 0.4f));
			AddHit(new Rectangle(num2, num - _d.L(9f), num4, _d.L(18f)), "manquante", delegate
			{
				_categorie = Categorie.Tout;
				Selectionner(caseSac);
			});
		}
		string text = ((caseSac >= 0) ? Strings.Personnage.SetInBag.ToString() : (SourcesDe(databaseObject).FirstOrDefault().Titre ?? string.Empty));
		if (!string.IsNullOrEmpty(text))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), "· " + text, num2 + num4 + _d.L(6f), num, A(TechniqueArt.Dim));
		}
	}

	internal static HashSet<Guid> IdsPortes(Player me)
	{
		HashSet<Guid> hashSet = new HashSet<Guid>();
		int[] myEquipment = me.MyEquipment;
		foreach (int num in myEquipment)
		{
			if (num < 0 || num >= me.Inventory.Length)
			{
				continue;
			}
			Guid? guid = me.Inventory[num]?.ItemId;
			if (guid.HasValue)
			{
				Guid valueOrDefault = guid.GetValueOrDefault();
				if (valueOrDefault != Guid.Empty)
				{
					hashSet.Add(valueOrDefault);
				}
			}
		}
		return hashSet;
	}

	private HashSet<Guid>? PortesApresEchange(Player me)
	{
		if (_selectionSac >= 0)
		{
			IItem item = me.Inventory[_selectionSac];
			if (item != null && DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var databaseObject) && databaseObject.ItemType == ItemType.Equipment)
			{
				HashSet<Guid> hashSet = new HashSet<Guid>();
				for (int i = 0; i < me.MyEquipment.Length; i++)
				{
					if (i != databaseObject.EquipmentSlot)
					{
						int caseSac;
						ItemDescriptor itemDescriptor = ObjetPorte(me, i, out caseSac);
						if (itemDescriptor != null)
						{
							hashSet.Add(itemDescriptor.Id);
						}
					}
				}
				hashSet.Add(databaseObject.Id);
				return hashSet;
			}
		}
		return null;
	}

	internal static string Normaliser(string? texte)
	{
		if (string.IsNullOrEmpty(texte))
		{
			return string.Empty;
		}
		string text = texte.Normalize(NormalizationForm.FormD);
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		string text2 = text;
		foreach (char c in text2)
		{
			if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
			{
				stringBuilder.Append(char.ToLowerInvariant(c));
			}
		}
		return stringBuilder.ToString();
	}
}
