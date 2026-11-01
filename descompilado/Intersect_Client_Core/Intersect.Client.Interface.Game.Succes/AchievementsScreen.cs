using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Dungeons;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Housing;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Framework.Core.GameObjects.Achievements;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.Ornaments;
using Intersect.Framework.Core.GameObjects.Titles;
using Intersect.Models;

namespace Intersect.Client.Interface.Game.Succes;

internal sealed class AchievementsScreen : CharteScreen
{
	private sealed class Carte
	{
		public readonly List<AchievementProgress> Paliers = new List<AchievementProgress>();

		public AchievementProgress Courant
		{
			get
			{
				AchievementProgress achievementProgress = Paliers.FirstOrDefault((AchievementProgress palier) => !palier.Completed);
				if (achievementProgress == null)
				{
					List<AchievementProgress> paliers = Paliers;
					achievementProgress = paliers[paliers.Count - 1];
				}
				return achievementProgress;
			}
		}

		public bool Complete => Paliers.All((AchievementProgress palier) => palier.Completed);

		public string Categorie => Paliers[0].Category?.Trim() ?? string.Empty;
	}

	private readonly record struct Gain(string Sorte, IGameTexture? Icone, string Court, string Long, Color Couleur, Guid Titre);

	private const float BodyTop = 96f;

	private const float BodyHeight = 768f;

	private const float RailLeft = 36f;

	private const float RailWidth = 250f;

	private const float CenterLeft = 298f;

	private const float CenterWidth = 930f;

	private const float FicheLeft = 1240f;

	private const float FicheWidth = 324f;

	private const float RankHeight = 118f;

	private const float CardWidth = 438f;

	private const float CardHeight = 96f;

	private const float CardGap = 10f;

	private const int VisibleRows = 5;

	private static readonly Color Silver = new Color(255, 207, 216, 230);

	private static readonly Color Bronze = new Color(255, 224, 160, 112);

	private static readonly Color Violet = new Color(255, 185, 140, 255);

	private static readonly Color Prism = new Color(255, 154, 208, 255);

	private static readonly Color BodyLight = new Color(255, 196, 204, 220);

	private static readonly (int Seuil, int Matiere)[] Rangs = new(int, int)[5]
	{
		(0, 0),
		(20, 1),
		(45, 2),
		(70, 1),
		(100, 3)
	};

	private string? _categorie;

	private int _filtre;

	private string _choisi = string.Empty;

	private int _scroll;

	private int _rangees;

	private Rectangle _grille;

	private AchievementProgress[]? _source;

	private List<Carte> _cartes = new List<Carte>();

	public override bool IsBlockingInput => !base.IsHidden;

	public AchievementsScreen(Canvas gameCanvas)
		: base(gameCanvas, "AchievementsScreen")
	{
		Interface.InputBlockingComponents.Add(this);
	}

	public void Toggle()
	{
		if (IsVisible())
		{
			Hide();
		}
		else
		{
			Show();
		}
	}

	public void Update()
	{
		if (!base.IsHidden)
		{
			FitCanvas();
		}
	}

	protected override bool OnMouseWheeled(int delta)
	{
		Point point = LocalMouse();
		if (!base.IsHidden && _grille.Contains(point.X, point.Y))
		{
			_scroll = Math.Clamp(_scroll + ((delta <= 0) ? 1 : (-1)), 0, Math.Max(0, _rangees - 5));
			return true;
		}
		return base.OnMouseWheeled(delta);
	}

	private List<Carte> Cartes()
	{
		AchievementProgress[] array = Globals.Achievements ?? Array.Empty<AchievementProgress>();
		if (array == _source)
		{
			return _cartes;
		}
		_source = array;
		List<Carte> list = new List<Carte>();
		Dictionary<string, Carte> dictionary = new Dictionary<string, Carte>();
		AchievementProgress[] array2 = array;
		foreach (AchievementProgress achievementProgress in array2)
		{
			if (string.IsNullOrEmpty(achievementProgress.Chain))
			{
				Carte carte = new Carte();
				carte.Paliers.Add(achievementProgress);
				list.Add(carte);
				continue;
			}
			if (!dictionary.TryGetValue(achievementProgress.Chain, out var value))
			{
				value = (dictionary[achievementProgress.Chain] = new Carte());
				list.Add(value);
			}
			value.Paliers.Add(achievementProgress);
		}
		foreach (Carte item in list)
		{
			item.Paliers.Sort((AchievementProgress a, AchievementProgress b) => (a.Points == b.Points) ? a.Objective.CompareTo(b.Objective) : a.Points.CompareTo(b.Points));
		}
		return _cartes = list;
	}

	private static string Texte(string? contenu)
	{
		return Traduction.Contenu(contenu ?? string.Empty);
	}

	private static string NomDeChaine(AchievementProgress succes)
	{
		string text;
		if (!succes.Chain.StartsWith("variable:", StringComparison.Ordinal))
		{
			text = succes.ChainName;
			if (text == null)
			{
				return string.Empty;
			}
		}
		else
		{
			text = string.Empty;
		}
		return text;
	}

	private static string Nombre(long valeur)
	{
		return MarketWindow.FormatAmount(valeur);
	}

	private static float Part(AchievementProgress succes)
	{
		if (!succes.Completed)
		{
			return Math.Clamp((float)succes.Value / (float)Math.Max(1L, succes.Objective), 0f, 1f);
		}
		return 1f;
	}

	private static string NomDeCategorie(string categorie)
	{
		if (categorie.Length != 0)
		{
			return Texte(categorie);
		}
		return Strings.Succes.Other.ToString();
	}

	private static Color Teinte(string categorie)
	{
		string text = categorie.ToLowerInvariant();
		if (text.StartsWith("hist"))
		{
			return TechniqueArt.Ki;
		}
		if (text.StartsWith("explo"))
		{
			return TechniqueArt.Cyan;
		}
		if (text.StartsWith("comb"))
		{
			return TechniqueArt.Red;
		}
		if (text.StartsWith("donj"))
		{
			return Violet;
		}
		if (text.StartsWith("mét") || text.StartsWith("met"))
		{
			return TechniqueArt.Green;
		}
		if (text.StartsWith("karma"))
		{
			return TechniqueArt.Gold;
		}
		Color[] array = new Color[6]
		{
			TechniqueArt.Ki,
			TechniqueArt.Cyan,
			Violet,
			TechniqueArt.Green,
			TechniqueArt.Gold,
			TechniqueArt.Red
		};
		int num = 0;
		string text2 = text;
		foreach (char c in text2)
		{
			num += c;
		}
		if (text.Length != 0)
		{
			return array[num % array.Length];
		}
		return Silver;
	}

	private static int Matiere(int points)
	{
		if (points < 100)
		{
			if (points < 30)
			{
				return (points >= 15) ? 1 : 0;
			}
			return 2;
		}
		return 3;
	}

	private static (Color Clair, Color Sombre) Metal(int matiere)
	{
		return matiere switch
		{
			1 => (Clair: new Color(255, 230, 237, 247), Sombre: new Color(255, 125, 138, 160)), 
			2 => (Clair: new Color(255, 255, 227, 154), Sombre: new Color(255, 201, 134, 28)), 
			3 => (Clair: new Color(255, 184, 243, 255), Sombre: new Color(255, 138, 92, 255)), 
			_ => (Clair: new Color(255, 240, 185, 138), Sombre: new Color(255, 154, 90, 48)), 
		};
	}

	private static string NomDuRang(int rang)
	{
		return (rang switch
		{
			1 => Strings.Succes.RankSilver, 
			2 => Strings.Succes.RankGold, 
			3 => Strings.Succes.RankPlatinum, 
			4 => Strings.Succes.RankLegend, 
			_ => Strings.Succes.RankBronze, 
		}).ToString();
	}

	private AchievementProgress? Choisi()
	{
		AchievementProgress[] source = _source ?? Array.Empty<AchievementProgress>();
		AchievementProgress achievementProgress = source.FirstOrDefault((AchievementProgress succes) => succes.Id == _choisi);
		if (achievementProgress != null)
		{
			return achievementProgress;
		}
		AchievementProgress achievementProgress2 = source.Where((AchievementProgress succes) => !succes.Completed).OrderByDescending(Part).FirstOrDefault() ?? source.FirstOrDefault();
		_choisi = achievementProgress2?.Id ?? string.Empty;
		return achievementProgress2;
	}

	private void Choisir(string id)
	{
		_choisi = id;
	}

	private void Ouvrir(string? categorie)
	{
		_categorie = categorie;
		_scroll = 0;
	}

	protected override void DrawScreen(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		DrawBackdrop(renderer, TechniqueArt.Gold);
		List<Carte> list = Cartes();
		AchievementProgress[] array = _source ?? Array.Empty<AchievementProgress>();
		int num = array.Where((AchievementProgress succes) => succes.Completed).Sum((AchievementProgress succes) => succes.Points);
		int num2 = array.Sum((AchievementProgress succes) => succes.Points);
		DrawTopBar(renderer, Strings.Achievements.Title, Strings.Succes.Thread, new global::_003C_003Ez__ReadOnlyArray<Gauge>(new Gauge[2]
		{
			new Gauge(Strings.Succes.GaugeCount, $"{array.Count((AchievementProgress succes) => succes.Completed)} / {array.Length}", TechniqueArt.Text),
			new Gauge(Strings.Succes.GaugePoints, Nombre(num) + " / " + Nombre(num2), TechniqueArt.Gold)
		}));
		if (_categorie != null && list.All((Carte carte) => !string.Equals(carte.Categorie, _categorie, StringComparison.OrdinalIgnoreCase)))
		{
			_categorie = null;
		}
		DrawRail(renderer, list, array);
		DrawCentre(renderer, list, array, num, num2);
		DrawFiche(renderer, list);
	}

	private void Jauge(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, float part, Color clair, Color sombre)
	{
		if (rect.Width > 0 && rect.Height > 0)
		{
			TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, 0.07f));
			int num = (int)MathF.Round((float)rect.Width * Math.Clamp(part, 0f, 1f));
			if (num > 0)
			{
				TechniqueArt.FillHorizontal(renderer, new Rectangle(rect.X, rect.Y, num, rect.Height), A(sombre), A(TechniqueArt.Mix(clair, sombre, 0.5f)), A(clair));
			}
		}
	}

	private void Medaille(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle outer, int matiere, bool faite, string texte, bool petiteLegende, float corps)
	{
		var (color, b) = Metal(matiere);
		if (faite)
		{
			TechniqueArt.Glow(renderer, (float)outer.X + (float)outer.Width / 2f, (float)outer.Y + (float)outer.Height / 2f, (float)outer.Width * 0.72f, A(color, 0.28f));
		}
		TechniqueArt.Image(renderer, "tech_hexa_bord.png", outer, A(faite ? TechniqueArt.Mix(color, TechniqueArt.White, 0.6f) : TechniqueArt.Mix(color, TechniqueArt.Line2, 0.45f)));
		int num = Math.Max(1, (int)MathF.Round((float)outer.Width * 2.4f / 62f));
		Rectangle rect = new Rectangle(outer.X + num, outer.Y + num, outer.Width - num * 2, outer.Height - num * 2);
		if (faite)
		{
			TechniqueArt.Image(renderer, "tech_hexa.png", rect, A(TechniqueArt.Mix(color, b, 0.55f)));
			TechniqueArt.Image(renderer, "tech_hexa_radial.png", rect, A(TechniqueArt.Mix(TechniqueArt.White, color, 0.5f), 0.55f));
		}
		else
		{
			TechniqueArt.Image(renderer, "tech_hexa.png", rect, A(new Color(255, 8, 11, 19)));
			TechniqueArt.Image(renderer, "tech_hexa_radial.png", rect, A(TechniqueArt.Mix(color, new Color(255, 22, 28, 44), 0.22f)));
		}
		int centerX = outer.X + outer.Width / 2;
		int num2 = outer.Y + outer.Height / 2;
		Color color2 = (faite ? HouseArt.Dark : color);
		if (petiteLegende)
		{
			TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(corps), texte, centerX, num2 - _d.L(corps * 0.2f), A(color2));
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(corps * 0.4f), Strings.Achievements.PointsShort, centerX, num2 + _d.L(corps * 0.48f), _d.Em(0.14f, corps * 0.4f), A(faite ? HouseArt.Dark : TechniqueArt.Dim, faite ? 0.75f : 1f));
		}
		else
		{
			TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(corps), texte, centerX, num2, A(color2));
		}
	}

	private void MedailleDe(Intersect.Client.Framework.Gwen.Renderer.Base renderer, AchievementProgress succes, Rectangle outer, float corps)
	{
		Medaille(renderer, outer, Matiere(succes.Points), succes.Completed, $"{succes.Points}", petiteLegende: true, corps);
	}

	private void DrawRail(Intersect.Client.Framework.Gwen.Renderer.Base renderer, List<Carte> cartes, AchievementProgress[] tous)
	{
		Rectangle rect = _d.R(36f, 96f, 250f, 768f);
		Panel(renderer, rect);
		float y = 110f;
		y = EntreeDuRail(renderer, y, null, Strings.Succes.Overview, TechniqueArt.Text, tous.Count((AchievementProgress succes) => succes.Completed), tous.Length, jauge: false);
		y += 8f;
		int num = _d.Y(y + 8f);
		int num2 = TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), Strings.Succes.Categories.ToString().ToUpperInvariant(), _d.X(54f), num, _d.Em(0.18f, 12f), A(TechniqueArt.Dim));
		TechniqueArt.Fill(renderer, new Rectangle(_d.X(62f) + num2, num, _d.X(272f) - _d.X(62f) - num2, 1), A(TechniqueArt.Line));
		y += 22f;
		foreach (string categorie in cartes.Select((Carte carte) => carte.Categorie).Distinct<string>(StringComparer.OrdinalIgnoreCase).OrderBy<string, string>((string nom) => (nom.Length != 0) ? nom : "~", StringComparer.CurrentCultureIgnoreCase))
		{
			AchievementProgress[] array = tous.Where((AchievementProgress succes) => string.Equals(succes.Category?.Trim() ?? string.Empty, categorie, StringComparison.OrdinalIgnoreCase)).ToArray();
			y = EntreeDuRail(renderer, y, categorie, NomDeCategorie(categorie), Teinte(categorie), array.Count((AchievementProgress succes) => succes.Completed), array.Length, jauge: true);
		}
		List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), Strings.Succes.RailHint, _d.L(222f), 3);
		int num3 = _d.Y(848f);
		for (int num4 = list.Count - 1; num4 >= 0; num4--)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), list[num4], _d.X(50f), num3 - _d.L(7f), A(TechniqueArt.Dim));
			num3 -= _d.L(15f);
		}
	}

	private float EntreeDuRail(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float y, string? categorie, string nom, Color teinte, int faits, int total, bool jauge)
	{
		float num = (jauge ? 44f : 36f);
		Rectangle rect = _d.R(46f, y, 230f, num);
		bool flag = ((categorie == null) ? (_categorie == null) : string.Equals(categorie, _categorie, StringComparison.OrdinalIgnoreCase));
		string key = "rail-" + (categorie ?? "*");
		if (flag)
		{
			TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Ki, 0.18f), A(TechniqueArt.Ki, 0.08f), A(TechniqueArt.Ki, 0.02f));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Ki, 0.45f));
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Ki));
		}
		else if (IsHot(key))
		{
			TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, 0.04f));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
		}
		int centerY = rect.Y + _d.L(jauge ? 16 : 18);
		DungeonHud.Diamond(renderer, rect.X + _d.L(12f), centerY, _d.L(7f), A(teinte, 0.9f));
		bool flag2 = total > 0 && faits == total;
		string text = $"{faits} / {total}";
		int num2 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(14f), text, rect.Right - _d.L(10f), centerY, A(flag2 ? TechniqueArt.Gold : (flag ? TechniqueArt.Text : TechniqueArt.Dim)));
		string text2 = TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(13.5f), nom, rect.Right - _d.L(18f) - num2 - (rect.X + _d.L(26f)), 1).FirstOrDefault();
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(13.5f), text2, rect.X + _d.L(26f), centerY, A(flag ? TechniqueArt.Text : BodyLight));
		if (jauge)
		{
			Jauge(renderer, new Rectangle(rect.X + _d.L(26f), rect.Y + _d.L(31f), rect.Width - _d.L(36f), _d.L(3f)), (total == 0) ? 0f : ((float)faits / (float)total), TechniqueArt.Mix(teinte, TechniqueArt.White, 0.8f), TechniqueArt.Mix(teinte, TechniqueArt.Black, 0.7f));
		}
		AddHit(rect, key, delegate
		{
			Ouvrir(categorie);
		});
		return y + num + 2f;
	}

	private void DrawCentre(Intersect.Client.Framework.Gwen.Renderer.Base renderer, List<Carte> cartes, AchievementProgress[] tous, int gagnes, int total)
	{
		Rectangle rect = _d.R(298f, 96f, 930f, 768f);
		Panel(renderer, rect);
		float num = 320f;
		_grille = default(Rectangle);
		_rangees = 0;
		DrawRang(renderer, num, 112f, tous, gagnes, total);
		if (tous.Length == 0)
		{
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(14f), Strings.Succes.Empty, _d.X(763f), _d.Y(496f), A(TechniqueArt.Soft));
			return;
		}
		float num2 = 248f;
		if (_categorie == null)
		{
			List<Carte> list = (from carte in cartes
				where !carte.Complete
				orderby Part(carte.Courant) descending
				select carte).Take(4).ToList();
			if (list.Count > 0)
			{
				num2 = Section(renderer, num, num2, Strings.Succes.WithinReach, TechniqueArt.Ki);
				num2 = Grille(renderer, num, num2, list, 0, 2) + 18f;
			}
			List<Carte> list2 = (from carte in cartes
				where carte.Complete
				orderby carte.Courant.Points descending
				select carte).Take((list.Count > 2) ? 4 : 6).ToList();
			num2 = Section(renderer, num, num2, Strings.Succes.Finest, TechniqueArt.Gold);
			if (list2.Count == 0)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), Strings.Succes.NothingYet, _d.X(num), _d.Y(num2 + 14f), A(TechniqueArt.Soft));
			}
			else
			{
				Grille(renderer, num, num2, list2, 0, 3);
			}
			return;
		}
		List<Carte> source = cartes.Where((Carte carte) => string.Equals(carte.Categorie, _categorie, StringComparison.OrdinalIgnoreCase)).ToList();
		AchievementProgress[] array = source.SelectMany((Carte carte) => carte.Paliers).ToArray();
		Color color = Teinte(_categorie);
		int num3 = _d.Y(num2 + 20f);
		DungeonHud.Diamond(renderer, _d.X(num), num3, _d.L(11f), A(color));
		string text = NomDeCategorie(_categorie).ToUpperInvariant();
		int num4 = TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(28f), text, _d.X(num + 20f), num3, A(TechniqueArt.Text));
		string text2 = Strings.Succes.CategoryCount.ToString(array.Count((AchievementProgress succes) => succes.Completed), array.Length, Nombre(array.Where((AchievementProgress succes) => succes.Completed).Sum((AchievementProgress succes) => succes.Points)));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(15f), text2, _d.X(num + 20f) + num4 + _d.L(14f), num3 + _d.L(2f), A(TechniqueArt.Dim));
		string[] array2 = new string[3]
		{
			Strings.Succes.FilterAll.ToString(),
			Strings.Succes.FilterOngoing.ToString(),
			Strings.Succes.FilterDone.ToString()
		};
		int num5 = _d.X(1206f);
		for (int num6 = array2.Length - 1; num6 >= 0; num6--)
		{
			int num7 = TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(12.5f), array2[num6]).X + _d.L(24f);
			Rectangle rect2 = new Rectangle(num5 - num7, num3 - _d.L(15f), num7, _d.L(30f));
			bool flag = _filtre == num6;
			string key = $"filtre-{num6}";
			TechniqueArt.Fill(renderer, rect2, flag ? A(TechniqueArt.Gold, 0.12f) : A(TechniqueArt.White, 0.03f));
			TechniqueArt.Outline(renderer, rect2, A(flag ? TechniqueArt.Gold : (IsHot(key) ? TechniqueArt.Line2 : TechniqueArt.Line)));
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(12.5f), array2[num6], rect2.X + rect2.Width / 2, num3, A((flag || IsHot(key)) ? TechniqueArt.Text : TechniqueArt.Soft));
			int choix = num6;
			AddHit(rect2, key, delegate
			{
				_filtre = choix;
				_scroll = 0;
			});
			num5 -= num7 + _d.L(6f);
		}
		num2 += 52f;
		List<Carte> list3 = source.Where((Carte carte) => _filtre == 0 || ((_filtre != 2) ? (!carte.Complete) : carte.Complete)).ToList();
		if (list3.Count == 0)
		{
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(14f), Strings.Succes.NothingHere, _d.X(763f), _d.Y(num2 + 120f), A(TechniqueArt.Soft));
			return;
		}
		_rangees = (list3.Count + 1) / 2;
		_scroll = Math.Clamp(_scroll, 0, Math.Max(0, _rangees - 5));
		_grille = _d.R(num, num2, 886f, 520f);
		Rectangle previous = TechniqueArt.BeginClip(renderer, _grille);
		Grille(renderer, num, num2, list3, _scroll * 2, 5);
		TechniqueArt.EndClip(renderer, previous);
		if (_rangees > 5)
		{
			Rectangle rect3 = new Rectangle(_grille.Right + _d.L(6f), _grille.Y, _d.L(3f), _grille.Height);
			TechniqueArt.Fill(renderer, rect3, A(TechniqueArt.White, 0.05f));
			int num8 = Math.Max(_d.L(24f), rect3.Height * 5 / _rangees);
			int y = rect3.Y + (rect3.Height - num8) * _scroll / Math.Max(1, _rangees - 5);
			TechniqueArt.Fill(renderer, new Rectangle(rect3.X, y, rect3.Width, num8), A(TechniqueArt.Line2));
		}
	}

	private float Section(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float gauche, float y, string titre, Color teinte)
	{
		int num = _d.Y(y + 8f);
		DungeonHud.Diamond(renderer, _d.X(gauche), num, _d.L(8f), A(teinte));
		int num2 = TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), titre.ToUpperInvariant(), _d.X(gauche + 16f), num, _d.Em(0.18f, 12f), A(TechniqueArt.Dim));
		TechniqueArt.Fill(renderer, new Rectangle(_d.X(gauche + 26f) + num2, num, _d.X(1206f) - _d.X(gauche + 26f) - num2, 1), A(TechniqueArt.Line));
		return y + 26f;
	}

	private float Grille(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float gauche, float y, List<Carte> cartes, int depuis, int rangees)
	{
		float result = y;
		for (int i = depuis; i < cartes.Count && i - depuis < rangees * 2; i++)
		{
			int num = i - depuis;
			float x = gauche + (float)(num % 2) * 448f;
			float num2 = y + (float)(num / 2) * 106f;
			DrawCarte(renderer, cartes[i], x, num2);
			result = num2 + 96f;
		}
		return result;
	}

	private void DrawRang(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float gauche, float y, AchievementProgress[] tous, int gagnes, int total)
	{
		Rectangle rect = _d.R(gauche, y, 886f, 118f);
		float num = ((total == 0) ? 0f : (100f * (float)gagnes / (float)total));
		int i;
		for (i = 0; i + 1 < Rangs.Length && num >= (float)Rangs[i + 1].Seuil; i++)
		{
		}
		var (color, sombre) = Metal(Rangs[i].Matiere);
		TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Gold, 0.13f), A(TechniqueArt.Gold, 0.05f), A(TechniqueArt.Gold, 0.02f));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Gold, 0.28f));
		Medaille(renderer, _d.R(gauche + 18f, y + 17f, 84f, 84f), Rangs[i].Matiere, faite: true, (new string[5] { "I", "II", "III", "IV", "V" })[i], petiteLegende: false, 30f);
		int value = tous.Count((AchievementProgress succes) => succes.Completed && succes.TitleRewardId != Guid.Empty);
		(string, string, string)[] array = new(string, string, string)[3]
		{
			(Strings.Succes.GaugePoints.ToString(), Nombre(gagnes), "/ " + Nombre(total)),
			(Strings.Succes.GaugeCount.ToString(), $"{tous.Count((AchievementProgress succes) => succes.Completed)}", $"/ {tous.Length}"),
			(Strings.Succes.TitlesWon.ToString(), $"{value}", string.Empty)
		};
		int num2 = _d.X(gauche + 930f - 44f - 20f);
		for (int num3 = array.Length - 1; num3 >= 0; num3--)
		{
			(string, string, string) tuple2 = array[num3];
			string item = tuple2.Item1;
			string item2 = tuple2.Item2;
			string item3 = tuple2.Item3;
			int num4 = ((item3.Length != 0) ? (TechniqueArt.Measure(renderer, TechniqueArt.Body, Pt(13f), item3).X + _d.L(6f)) : 0);
			int x = TechniqueArt.Measure(renderer, TechniqueArt.Heavy, Pt(24f), item2).X;
			int val = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), item.ToUpperInvariant(), _d.Em(0.14f, 10f));
			int num5 = Math.Max(val, x + num4);
			int num6 = num2 - num5;
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), item.ToUpperInvariant(), num6, _d.Y(y + 44f), _d.Em(0.14f, 10f), A(TechniqueArt.Dim));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(24f), item2, num6, _d.Y(y + 68f), A(TechniqueArt.Text));
			if (item3.Length > 0)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), item3, num6 + x + _d.L(6f), _d.Y(y + 70f), A(TechniqueArt.Dim));
			}
			num2 = num6 - _d.L(18f);
			TechniqueArt.Fill(renderer, new Rectangle(num2, _d.Y(y + 34f), 1, _d.L(50f)), A((num3 == 0) ? TechniqueArt.Line2 : TechniqueArt.Line));
			num2 -= _d.L(18f);
		}
		float x2 = gauche + 122f;
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(11f), Strings.Succes.CollectorRank.ToString().ToUpperInvariant(), _d.X(x2), _d.Y(y + 24f), _d.Em(0.18f, 11f), A(TechniqueArt.Dim));
		TechniqueArt.Styled(renderer, TechniqueArt.Display, Pt(34f), NomDuRang(i).ToUpperInvariant(), _d.X(x2), _d.Y(y + 34f), TechniqueArt.Stops.Solid(color), _fade);
		int num7 = ((i + 1 < Rangs.Length) ? Rangs[i + 1].Seuil : (-1));
		float part = ((num7 < 0) ? 1f : ((num - (float)Rangs[i].Seuil) / (float)Math.Max(1, num7 - Rangs[i].Seuil)));
		Jauge(renderer, new Rectangle(_d.X(x2), _d.Y(y + 80f), Math.Min(_d.L(330f), num2 - _d.X(x2) - _d.L(8f)), _d.L(8f)), part, color, sombre);
		string text = ((num7 < 0) ? Strings.Succes.TopRank.ToString() : Strings.Succes.NextRank.ToString(Nombre(Math.Max(1L, (long)MathF.Ceiling((float)(total * num7) / 100f) - gagnes)), NomDuRang(i + 1)));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), text, _d.X(x2), _d.Y(y + 102f), A(TechniqueArt.Soft));
	}

	private void DrawCarte(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Carte carte, float x, float y)
	{
		Rectangle rect = _d.R(x, y, 438f, 96f);
		AchievementProgress courant = carte.Courant;
		bool complete = carte.Complete;
		bool flag = carte.Paliers.Any((AchievementProgress palier) => palier.Id == _choisi);
		string key = "carte-" + courant.Id;
		bool flag2 = IsHot(key);
		if (flag)
		{
			TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Ki, 0.14f), A(TechniqueArt.Ki, 0.06f), A(TechniqueArt.Ki, 0.02f));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Ki));
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Ki));
		}
		else if (complete)
		{
			TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Gold, flag2 ? 0.13f : 0.09f), A(TechniqueArt.Gold, 0.04f), A(TechniqueArt.Gold, 0.015f));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Gold, 0.34f));
		}
		else
		{
			TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, flag2 ? 0.05f : 0.025f));
			TechniqueArt.Outline(renderer, rect, A(flag2 ? TechniqueArt.Line2 : TechniqueArt.Line));
		}
		MedailleDe(renderer, courant, _d.R(x + 14f, y + 18f, 60f, 60f), 23f);
		List<Gain> list = (from gain in carte.Paliers.SelectMany(Gains)
			group gain by gain.Sorte into groupe
			select groupe.Last()).Take(2).ToList();
		int num = rect.Right - _d.L(14f);
		int num2 = rect.Y + _d.L((list.Count > 1) ? 23 : 37);
		foreach (Gain item in list)
		{
			int num3 = Puce(renderer, item, rect.Right - _d.L(14f), num2);
			num = Math.Min(num, rect.Right - _d.L(14f) - num3 - _d.L(12f));
			num2 += _d.L(27f);
		}
		int num4 = rect.X + _d.L(88f);
		int num5 = num - num4;
		bool flag3 = carte.Paliers.Count > 1;
		string text = ((flag3 && NomDeChaine(courant).Length > 0) ? Texte(NomDeChaine(courant)) : Texte(courant.Name));
		string text2 = TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(19f), text, num5, 1).FirstOrDefault();
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(19f), text2, num4, rect.Y + _d.L(22f), A(TechniqueArt.Text));
		string text3 = ((flag3 && NomDeChaine(courant).Length > 0) ? (Texte(courant.Name) + " : " + Texte(courant.Description)) : Texte(courant.Description));
		List<string> list2 = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), text3, num5, 2);
		for (int num6 = 0; num6 < list2.Count; num6++)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), list2[num6], num4, rect.Y + _d.L(43 + num6 * 16), A(TechniqueArt.Soft));
		}
		string text4 = (complete ? (flag3 ? Strings.Succes.Complete : Strings.Succes.StateDone).ToString() : ((courant.Objective <= 1) ? Strings.Succes.ToDo.ToString() : (Nombre(Math.Min(courant.Value, courant.Objective)) + " / " + Nombre(courant.Objective))));
		int num7 = rect.Y + _d.L(80f);
		int num8 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(14f), text4, num, num7, A(complete ? TechniqueArt.Gold : BodyLight));
		Rectangle rect2 = new Rectangle(num4, num7 - _d.L(3f), num5 - num8 - _d.L(12f), _d.L(6f));
		var (clair, sombre) = Metal(Matiere(courant.Points));
		if (flag3)
		{
			int num9 = _d.L(3f);
			int num10 = (rect2.Width - num9 * (carte.Paliers.Count - 1)) / carte.Paliers.Count;
			for (int num11 = 0; num11 < carte.Paliers.Count; num11++)
			{
				var (clair2, sombre2) = Metal(Matiere(carte.Paliers[num11].Points));
				Jauge(renderer, new Rectangle(rect2.X + num11 * (num10 + num9), rect2.Y, num10, rect2.Height), Part(carte.Paliers[num11]), clair2, sombre2);
			}
		}
		else
		{
			Jauge(renderer, rect2, Part(courant), clair, sombre);
		}
		string id = courant.Id;
		AddHit(rect, key, delegate
		{
			Choisir(id);
		});
	}

	private static List<Gain> Gains(AchievementProgress succes)
	{
		List<Gain> list = new List<Gain>();
		if (succes.ExperienceReward > 0)
		{
			list.Add(new Gain("exp", TechniqueArt.Tex("saiyan_orbe_xp.png"), Nombre(succes.ExperienceReward), Strings.Succes.RewardExperience.ToString(Nombre(succes.ExperienceReward)), TechniqueArt.Cyan, Guid.Empty));
		}
		Guid guid = Globals.Me?.CurrencyItemId ?? Guid.Empty;
		foreach (AchievementRewardItem item in succes.ItemRewards ?? new List<AchievementRewardItem>())
		{
			if (!(item.ItemId == Guid.Empty) && DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var databaseObject))
			{
				IGameTexture icone = Globals.ContentManager?.GetTexture(TextureType.Item, databaseObject.Icon);
				string text = Texte(databaseObject.Name);
				list.Add((item.ItemId == guid) ? new Gain("monnaie", icone, Nombre(item.Quantity), Strings.Succes.RewardCurrency.ToString(Nombre(item.Quantity), text), TechniqueArt.Gold, Guid.Empty) : new Gain($"objet-{item.ItemId:N}", icone, $"×{item.Quantity}", Strings.Succes.RewardItem.ToString(text, item.Quantity), BodyLight, Guid.Empty));
			}
		}
		if (succes.TitleRewardId != Guid.Empty && DatabaseObject<TitleDescriptor>.TryGet(succes.TitleRewardId, out var databaseObject2))
		{
			list.Add(new Gain("titre", null, Strings.Succes.TitleChip, Texte(databaseObject2.Name), databaseObject2.Color, databaseObject2.Id));
		}
		if (succes.OrnamentRewardId != Guid.Empty && DatabaseObject<OrnamentDescriptor>.TryGet(succes.OrnamentRewardId, out var databaseObject3))
		{
			list.Add(new Gain("ornement", null, Strings.Succes.OrnamentChip, Strings.Succes.RewardOrnament.ToString(Texte(databaseObject3.Name)), Violet, Guid.Empty));
		}
		return list;
	}

	private int Puce(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Gain gain, int droite, int haut)
	{
		int h = _d.L(22f);
		int num = _d.L(18f);
		int x = TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(12f), gain.Court).X;
		int num2 = ((gain.Icone != null) ? (num + _d.L(5f)) : _d.L(12f));
		int num3 = _d.L(6f) + num2 + x + _d.L(8f);
		Rectangle rect = new Rectangle(droite - num3, haut, num3, h);
		TechniqueArt.Fill(renderer, rect, A(new Color(178, 6, 8, 14)));
		TechniqueArt.Outline(renderer, rect, A((gain.Icone != null) ? TechniqueArt.Line : gain.Couleur, (gain.Icone != null) ? 1f : 0.45f));
		int num4 = rect.Y + rect.Height / 2;
		if (gain.Icone != null)
		{
			SaiyanHud.Draw(renderer, gain.Icone, rect.X + _d.L(4f), num4 - num / 2, num, num, A(TechniqueArt.White));
		}
		else
		{
			DungeonHud.Diamond(renderer, rect.X + _d.L(7f), num4, _d.L(7f), A(gain.Couleur));
		}
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(12f), gain.Court, rect.X + _d.L(6f) + num2, num4, A((gain.Icone != null) ? BodyLight : gain.Couleur));
		return num3;
	}

	private void DrawFiche(Intersect.Client.Framework.Gwen.Renderer.Base renderer, List<Carte> cartes)
	{
		Rectangle rect = _d.R(1240f, 96f, 324f, 768f);
		Panel(renderer, rect);
		AchievementProgress succes = Choisi();
		if (succes == null)
		{
			return;
		}
		Carte carte = cartes.FirstOrDefault((Carte candidate) => candidate.Paliers.Contains(succes));
		int matiere = Matiere(succes.Points);
		(Color Clair, Color Sombre) tuple = Metal(matiere);
		Color item = tuple.Clair;
		Color item2 = tuple.Sombre;
		string categorie = succes.Category?.Trim() ?? string.Empty;
		Rectangle rect2 = _d.R(1254f, 110f, 296f, 196f);
		TechniqueArt.FillVertical(renderer, rect2, A(new Color(255, 10, 18, 32)), A(new Color(255, 13, 21, 36)));
		TechniqueArt.Glow(renderer, (float)rect2.X + (float)rect2.Width / 2f, (float)rect2.Y + (float)rect2.Height * 0.55f, _d.L(130f), A(item, 0.26f));
		TechniqueArt.Outline(renderer, rect2, A(TechniqueArt.Line));
		int num = _d.L(124f);
		MedailleDe(renderer, succes, new Rectangle(rect2.X + (rect2.Width - num) / 2, rect2.Y + (rect2.Height - num) / 2 + _d.L(6f), num, num), 48f);
		string text = (succes.Completed ? Strings.Succes.StateDone : Strings.Succes.StateOngoing).ToString().ToUpperInvariant();
		int w = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(11f), text, _d.Em(0.14f, 11f)) + _d.L(14f);
		Rectangle rect3 = new Rectangle(rect2.X + _d.L(10f), rect2.Y + _d.L(10f), w, _d.L(22f));
		if (succes.Completed)
		{
			TechniqueArt.Fill(renderer, rect3, A(TechniqueArt.Gold));
		}
		else
		{
			TechniqueArt.Fill(renderer, rect3, A(new Color(217, 6, 8, 14)));
			TechniqueArt.Outline(renderer, rect3, A(TechniqueArt.Line2));
		}
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(11f), text, rect3.X + _d.L(7f), rect3.Y + rect3.Height / 2, _d.Em(0.14f, 11f), A(succes.Completed ? HouseArt.Dark : TechniqueArt.Text));
		TechniqueArt.SpacedRight(renderer, TechniqueArt.Label, Pt(11f), NomDeCategorie(categorie).ToUpperInvariant(), rect2.Right - _d.L(12f), rect3.Y + rect3.Height / 2, _d.Em(0.12f, 11f), A(Teinte(categorie)));
		int num2 = _d.X(1258f);
		int num3 = _d.L(288f);
		float num4 = 332f;
		foreach (string item3 in TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(27f), Texte(succes.Name), num3, 2))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(27f), item3, num2, _d.Y(num4), A(TechniqueArt.Text));
			num4 += 28f;
		}
		num4 -= 4f;
		foreach (string item4 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(13.5f), Texte(succes.Description), num3, 4))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13.5f), item4, num2, _d.Y(num4), A(BodyLight));
			num4 += 18f;
		}
		num4 += 12f;
		if (carte != null)
		{
			List<AchievementProgress> paliers = carte.Paliers;
			if (paliers != null && paliers.Count > 1)
			{
				string text2 = ((NomDeChaine(succes).Length == 0) ? Strings.Succes.StepsPlain.ToString() : Strings.Succes.Steps.ToString(Texte(NomDeChaine(succes))));
				string text3 = TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(11f), text2.ToUpperInvariant(), num3, 1).FirstOrDefault();
				TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(11f), text3, num2, _d.Y(num4), _d.Em(0.14f, 11f), A(TechniqueArt.Dim));
				num4 += 14f;
				foreach (AchievementProgress palier in carte.Paliers)
				{
					Rectangle rect4 = _d.R(1258f, num4, 288f, 30f);
					bool flag = palier.Id == succes.Id;
					string key = "palier-" + palier.Id;
					TechniqueArt.Fill(renderer, rect4, flag ? A(TechniqueArt.Ki, 0.12f) : A(TechniqueArt.White, IsHot(key) ? 0.05f : 0.025f));
					int centerY = rect4.Y + rect4.Height / 2;
					DungeonHud.Diamond(renderer, rect4.X + _d.L(8f), centerY, _d.L(8f), A(palier.Completed ? TechniqueArt.Gold : TechniqueArt.Line2));
					string text4 = ((palier.Objective <= 1) ? (palier.Completed ? Strings.Succes.Done : Strings.Succes.ToDo).ToString() : (Nombre(Math.Min(palier.Value, palier.Objective)) + " / " + Nombre(palier.Objective)));
					int num5 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(13f), text4, rect4.Right - _d.L(8f), centerY, A(palier.Completed ? TechniqueArt.Gold : TechniqueArt.Dim));
					string text5 = TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(12.5f), Texte(palier.Name), rect4.Width - _d.L(40f) - num5, 1).FirstOrDefault();
					TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(12.5f), text5, rect4.X + _d.L(24f), centerY, A(flag ? TechniqueArt.Text : BodyLight));
					string id = palier.Id;
					AddHit(rect4, key, delegate
					{
						Choisir(id);
					});
					num4 += 31f;
				}
				goto IL_0a39;
			}
		}
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(11f), Strings.Succes.Progression.ToString().ToUpperInvariant(), num2, _d.Y(num4), _d.Em(0.14f, 11f), A(TechniqueArt.Dim));
		num4 += 22f;
		string text6 = (succes.Completed ? Strings.Succes.Finished.ToString() : Strings.Succes.Missing.ToString(Nombre(Math.Max(0L, succes.Objective - succes.Value))));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), text6, num2, _d.Y(num4), A(TechniqueArt.Soft));
		TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(21f), Nombre(Math.Min(succes.Value, succes.Objective)) + " / " + Nombre(succes.Objective), num2 + num3, _d.Y(num4 - 2f), A(TechniqueArt.Text));
		num4 += 16f;
		Jauge(renderer, _d.R(1258f, num4, 288f, 8f), Part(succes), item, item2);
		num4 += 8f;
		goto IL_0a39;
		IL_0a39:
		num4 += 18f;
		List<Gain> list = Gains(succes);
		if (list.Count > 0)
		{
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(11f), Strings.Succes.Rewards.ToString().ToUpperInvariant(), num2, _d.Y(num4), _d.Em(0.14f, 11f), A(TechniqueArt.Dim));
			num4 += 14f;
			foreach (Gain item5 in list)
			{
				num4 = ((item5.Titre != Guid.Empty) ? PlaqueDeTitre(renderer, item5, num4) : LigneDeGain(renderer, item5, num4));
			}
		}
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), Strings.Succes.Worth.ToString(succes.Points), num2, _d.Y(840f), A(TechniqueArt.Dim));
	}

	private float LigneDeGain(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Gain gain, float y)
	{
		Rectangle rect = _d.R(1258f, y, 288f, 34f);
		TechniqueArt.Fill(renderer, rect, A(new Color(153, 6, 8, 14)));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
		int num = rect.Y + rect.Height / 2;
		int num2 = _d.L(24f);
		if (gain.Icone != null)
		{
			SaiyanHud.Draw(renderer, gain.Icone, rect.X + _d.L(8f), num - num2 / 2, num2, num2, A(TechniqueArt.White));
		}
		else
		{
			DungeonHud.Diamond(renderer, rect.X + _d.L(15f), num, _d.L(9f), A(gain.Couleur));
		}
		string text = TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(13.5f), gain.Long, rect.Width - _d.L(50f), 1).FirstOrDefault();
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(13.5f), text, rect.X + _d.L(42f), num, A(TechniqueArt.Text));
		return y + 40f;
	}

	private float PlaqueDeTitre(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Gain gain, float y)
	{
		Rectangle rect = _d.R(1258f, y, 288f, 62f);
		TechniqueArt.FillHorizontal(renderer, rect, A(gain.Couleur, 0.12f), A(gain.Couleur, 0.04f), A(gain.Couleur, 0f));
		TechniqueArt.Outline(renderer, rect, A(gain.Couleur, 0.45f));
		Rectangle rect2 = new Rectangle(rect.X + _d.L(9f), rect.Y + _d.L(9f), _d.L(44f), _d.L(44f));
		TechniqueArt.Fill(renderer, rect2, A(new Color(255, 10, 18, 32)));
		TechniqueArt.Outline(renderer, rect2, A(TechniqueArt.Line));
		Player me = Globals.Me;
		if (me != null)
		{
			(IGameTexture, float, float, float, float)? tuple = DungeonEntryWindow.StandingFrame(me.Sprite);
			if (tuple.HasValue)
			{
				(IGameTexture, float, float, float, float) valueOrDefault = tuple.GetValueOrDefault();
				(IGameTexture, float, float, float, float) tuple2 = valueOrDefault;
				IGameTexture item = tuple2.Item1;
				float item2 = tuple2.Item2;
				float item3 = tuple2.Item3;
				float item4 = tuple2.Item4;
				float item5 = tuple2.Item5;
				float num = item4 * 0.56f;
				float num2 = (item2 + item4 * 0.22f) / (float)item.Width;
				float num3 = (item3 + ((item5 > item4) ? (item5 * 0.33f) : (item5 * 0.1f))) / (float)item.Height;
				SaiyanHud.Draw(renderer, item, rect2.X + 1, rect2.Y + 1, rect2.Width - 2, rect2.Height - 2, A(TechniqueArt.White), num2, num3, num2 + num / (float)item.Width, num3 + num / (float)item.Height);
			}
		}
		int num4 = rect2.Right + _d.L(10f);
		int width = rect.Right - num4 - _d.L(8f);
		string text = TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(17f), Globals.Me?.Name ?? string.Empty, width, 1).FirstOrDefault();
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(17f), text, num4, rect.Y + _d.L(17f), A(TechniqueArt.Text));
		string text2 = TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(12.5f), gain.Long, width, 1).FirstOrDefault();
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(12.5f), text2, num4, rect.Y + _d.L(34f), A(gain.Couleur));
		string text3 = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11f), Strings.Succes.TitleCaption, width, 1).FirstOrDefault();
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11f), text3, num4, rect.Y + _d.L(49f), A(TechniqueArt.Dim));
		return y + 68f;
	}
}
