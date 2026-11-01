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
using Intersect.Client.Interface.Game.Personnage;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Framework.Core.GameObjects.Achievements;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.Ornaments;
using Intersect.Framework.Core.GameObjects.Titles;
using Intersect.Models;

namespace Intersect.Client.Interface.Game.Succes;

internal sealed class FenetreSucces : FenetreCharte
{
	private sealed class Carte
	{
		public readonly List<AchievementProgress> Paliers = new List<AchievementProgress>();

		private string? _cherchable;

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

		public int Points => Paliers.Sum((AchievementProgress palier) => palier.Points);

		public string Titre
		{
			get
			{
				if (Paliers.Count <= 1 || NomDeChaine(Courant).Length <= 0)
				{
					return Texte(Courant.Name);
				}
				return Texte(NomDeChaine(Courant));
			}
		}

		public string Cherchable => _cherchable ?? (_cherchable = PersonnageScreen.Normaliser(string.Join(" ", Paliers.SelectMany((AchievementProgress palier) => new string[3]
		{
			Texte(palier.Name),
			Texte(palier.Description),
			Texte(palier.ChainName ?? string.Empty)
		}).Append(NomDeCategorie(Categorie)))));
	}

	private readonly record struct Gain(string Sorte, IGameTexture? Icone, string Court, string Long, Color Couleur, Guid Titre);

	private const float Largeur = 1240f;

	private const float Hauteur = 748f;

	private const float BodyTop = 58f;

	private const float BodyHeight = 676f;

	private const float RailLeft = 14f;

	private const float RailWidth = 224f;

	private const float CenterLeft = 248f;

	private const float CenterWidth = 700f;

	private const float Marge = 16f;

	private const float FicheLeft = 958f;

	private const float FicheWidth = 268f;

	private const float RankHeight = 80f;

	private const float ToolsTop = 168f;

	private const float ToolsHeight = 34f;

	private const float GridTop = 214f;

	private const float CardWidth = 329f;

	private const float CardHeight = 92f;

	private const float CardGap = 10f;

	private const int VisibleRows = 5;

	private static readonly Color Silver = new Color(255, 207, 216, 230);

	private static readonly Color Violet = new Color(255, 185, 140, 255);

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

	private int _tri;

	private string _choisi = string.Empty;

	private int _scroll;

	private int _rangees;

	private Rectangle _grille;

	private AchievementProgress[]? _source;

	private List<Carte> _cartes = new List<Carte>();

	private TextBox _recherche;

	private string _requete = string.Empty;

	private bool _cherche;

	private const float ChampLargeur = 292f;

	protected override Rectangle Place => new Rectangle(180, 76, 1240, 748);

	private string NomDuTri => (_tri switch
	{
		1 => Strings.Succes.SortPoints, 
		2 => Strings.Succes.SortProgress, 
		_ => Strings.Succes.SortName, 
	}).ToString();

	public FenetreSucces(Canvas gameCanvas)
		: base(gameCanvas, "FenetreSucces")
	{
		ConstruireRecherche();
	}

	public void Toggle()
	{
		if (base.IsHidden)
		{
			Show();
		}
		else
		{
			Hide();
		}
	}

	public override void Hide()
	{
		FermerRecherche();
		base.Hide();
	}

	public void Update()
	{
		if (!base.IsHidden)
		{
			Placer();
			PlacerRecherche();
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

	private void ConstruireRecherche()
	{
		_recherche = new TextBox(this, "Recherche")
		{
			Font = TechniqueArt.Body,
			FontSize = 11,
			ShouldDrawBackground = false,
			MaximumLength = 40,
			IsHidden = true
		};
		_recherche.SetTextColor(TechniqueArt.Text, ComponentState.Normal);
		_recherche.TextChanged += delegate
		{
			_requete = PersonnageScreen.Normaliser(_recherche.Text);
			_scroll = 0;
		};
		Interface.FocusComponents.Add(_recherche);
	}

	private void PlacerRecherche()
	{
		if (_cherche)
		{
			Rectangle rectangle = _d.R(296f, 171f, 162f, 28f);
			if (_recherche.FontSize != Pt(13.5f))
			{
				_recherche.FontSize = Pt(13.5f);
			}
			if (_recherche.X != rectangle.X || _recherche.Y != rectangle.Y || _recherche.Width != rectangle.Width || _recherche.Height != rectangle.Height)
			{
				_recherche.SetBounds(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
			}
		}
	}

	private void BasculerRecherche()
	{
		if (_cherche)
		{
			FermerRecherche();
			return;
		}
		_cherche = true;
		_recherche.IsHidden = false;
		PlacerRecherche();
		_recherche.Focus();
	}

	private void FermerRecherche()
	{
		if (_cherche)
		{
			_cherche = false;
			_recherche.Blur();
			_recherche.Text = string.Empty;
			_recherche.IsHidden = true;
			_requete = string.Empty;
			_scroll = 0;
		}
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
		if (text.StartsWith("comb") || text.StartsWith("best"))
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

	private List<Carte> FiltrerEtTrier(IEnumerable<Carte> cartes)
	{
		IEnumerable<Carte> source = cartes.Where((Carte carte) => _filtre == 0 || ((_filtre != 2) ? (!carte.Complete) : carte.Complete));
		StringComparer currentCultureIgnoreCase = StringComparer.CurrentCultureIgnoreCase;
		return (_tri switch
		{
			1 => source.OrderByDescending((Carte carte) => carte.Points).ThenBy<Carte, string>((Carte carte) => carte.Titre, currentCultureIgnoreCase), 
			2 => (from carte in source
				orderby carte.Complete ? 1 : 0, Part(carte.Courant) descending
				select carte).ThenBy<Carte, string>((Carte carte) => carte.Titre, currentCultureIgnoreCase), 
			_ => source.OrderBy<Carte, string>((Carte carte) => carte.Titre, currentCultureIgnoreCase), 
		}).ToList();
	}

	private IEnumerable<Carte> Trouvees(List<Carte> cartes)
	{
		string[] mots = _requete.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		return cartes.Where((Carte carte) => mots.All((string mot) => carte.Cherchable.Contains(mot, StringComparison.Ordinal)));
	}

	protected override void Dessiner(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		DessinerCadre(renderer);
		List<Carte> list = Cartes();
		AchievementProgress[] array = _source ?? Array.Empty<AchievementProgress>();
		int gagnes = array.Where((AchievementProgress succes) => succes.Completed).Sum((AchievementProgress succes) => succes.Points);
		int total = array.Sum((AchievementProgress succes) => succes.Points);
		int croix = DessinerBarre(renderer, Strings.Achievements.Title);
		DessinerCompteurs(renderer, croix, array, gagnes, total);
		if (_categorie != null && list.All((Carte carte) => !string.Equals(carte.Categorie, _categorie, StringComparison.OrdinalIgnoreCase)))
		{
			_categorie = null;
		}
		DrawRail(renderer, list, array);
		DrawCentre(renderer, list, array, gagnes, total);
		DrawFiche(renderer, list);
	}

	protected override void DessinerDessus(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		DessinerEtiquette(renderer, now);
	}

	private void DessinerCompteurs(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int croix, AchievementProgress[] tous, int gagnes, int total)
	{
		int num = _d.Y(23f);
		int num2 = croix - _d.L(18f);
		(string, string, Color)[] array = new(string, string, Color)[2]
		{
			(Strings.Succes.GaugePoints.ToString(), Nombre(gagnes) + " / " + Nombre(total), TechniqueArt.Gold),
			(Strings.Succes.GaugeCount.ToString(), $"{tous.Count((AchievementProgress succes) => succes.Completed)} / {tous.Length}", TechniqueArt.Text)
		};
		for (int num3 = 0; num3 < array.Length; num3++)
		{
			(string, string, Color) tuple = array[num3];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			Color item3 = tuple.Item3;
			int num4 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(16f), item2, num2, num, A(item3));
			num2 -= num4 + _d.L(8f);
			int num5 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), item.ToUpperInvariant(), _d.Em(0.16f, 10f));
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), item.ToUpperInvariant(), num2 - num5, num + _d.L(1f), _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
			num2 -= num5 + _d.L(22f);
		}
	}

	private void Panel(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect)
	{
		TechniqueArt.Patch(renderer, "tech_panneau.png", rect, A(TechniqueArt.White), 12);
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
		Rectangle rect = _d.R(14f, 58f, 224f, 676f);
		Panel(renderer, rect);
		float y = 70f;
		y = EntreeDuRail(renderer, y, null, Strings.Succes.Overview, TechniqueArt.Text, tous.Count((AchievementProgress succes) => succes.Completed), tous.Length, jauge: false);
		y += 6f;
		int num = _d.Y(y + 8f);
		int num2 = TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(11f), Strings.Succes.Categories.ToString().ToUpperInvariant(), _d.X(30f), num, _d.Em(0.18f, 11f), A(TechniqueArt.Dim));
		TechniqueArt.Fill(renderer, new Rectangle(_d.X(38f) + num2, num, _d.X(226f) - _d.X(38f) - num2, 1), A(TechniqueArt.Line));
		y += 20f;
		foreach (string categorie in cartes.Select((Carte carte) => carte.Categorie).Distinct<string>(StringComparer.OrdinalIgnoreCase).OrderBy<string, string>((string nom) => (nom.Length != 0) ? nom : "~", StringComparer.CurrentCultureIgnoreCase))
		{
			AchievementProgress[] array = tous.Where((AchievementProgress succes) => string.Equals(succes.Category?.Trim() ?? string.Empty, categorie, StringComparison.OrdinalIgnoreCase)).ToArray();
			y = EntreeDuRail(renderer, y, categorie, NomDeCategorie(categorie), Teinte(categorie), array.Count((AchievementProgress succes) => succes.Completed), array.Length, jauge: true);
		}
		List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), Strings.Succes.RailHint, _d.L(200f), 3);
		int num3 = _d.Y(722f);
		for (int num4 = list.Count - 1; num4 >= 0; num4--)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), list[num4], _d.X(26f), num3 - _d.L(7f), A(TechniqueArt.Dim));
			num3 -= _d.L(14f);
		}
	}

	private float EntreeDuRail(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float y, string? categorie, string nom, Color teinte, int faits, int total, bool jauge)
	{
		float num = (jauge ? 42f : 34f);
		Rectangle rect = _d.R(22f, y, 208f, num);
		bool flag = ((categorie == null) ? (_categorie == null) : string.Equals(categorie, _categorie, StringComparison.OrdinalIgnoreCase));
		string cle = "rail-" + (categorie ?? "*");
		if (flag)
		{
			TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Ki, 0.18f), A(TechniqueArt.Ki, 0.08f), A(TechniqueArt.Ki, 0.02f));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Ki, 0.45f));
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Ki));
		}
		else if (IsHot(cle))
		{
			TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, 0.04f));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
		}
		int centerY = rect.Y + _d.L(jauge ? 15 : 17);
		DungeonHud.Diamond(renderer, rect.X + _d.L(12f), centerY, _d.L(7f), A(teinte, 0.9f));
		bool flag2 = total > 0 && faits == total;
		string text = $"{faits} / {total}";
		int num2 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(13f), text, rect.Right - _d.L(10f), centerY, A(flag2 ? TechniqueArt.Gold : (flag ? TechniqueArt.Text : TechniqueArt.Dim)));
		string text2 = TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(13f), nom, rect.Right - _d.L(18f) - num2 - (rect.X + _d.L(26f)), 1).FirstOrDefault();
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(13f), text2, rect.X + _d.L(26f), centerY, A(flag ? TechniqueArt.Text : BodyLight));
		if (jauge)
		{
			Jauge(renderer, new Rectangle(rect.X + _d.L(26f), rect.Y + _d.L(29f), rect.Width - _d.L(36f), _d.L(3f)), (total == 0) ? 0f : ((float)faits / (float)total), TechniqueArt.Mix(teinte, TechniqueArt.White, 0.8f), TechniqueArt.Mix(teinte, TechniqueArt.Black, 0.7f));
		}
		AddHit(rect, cle, delegate
		{
			FermerRecherche();
			Ouvrir(categorie);
		});
		return y + num + 2f;
	}

	private void DrawCentre(Intersect.Client.Framework.Gwen.Renderer.Base renderer, List<Carte> cartes, AchievementProgress[] tous, int gagnes, int total)
	{
		Rectangle rect = _d.R(248f, 58f, 700f, 676f);
		Panel(renderer, rect);
		float num = 264f;
		_grille = default(Rectangle);
		_rangees = 0;
		DrawRang(renderer, num, 74f, tous, gagnes, total);
		if (tous.Length == 0)
		{
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(14f), Strings.Succes.Empty, _d.X(598f), _d.Y(418f), A(TechniqueArt.Soft));
			return;
		}
		bool flag = _cherche && _requete.Length > 0;
		List<Carte> list;
		if (flag)
		{
			list = FiltrerEtTrier(Trouvees(cartes));
			DessinerOutils(renderer, Strings.Succes.Results.ToString(list.Count), string.Empty, TechniqueArt.Ki);
		}
		else
		{
			if (_categorie == null)
			{
				DessinerOutils(renderer, Strings.Succes.Overview, string.Empty, TechniqueArt.Text, sansFiltres: true);
				float y = 214f;
				List<Carte> list2 = (from carte in cartes
					where !carte.Complete
					orderby Part(carte.Courant) descending
					select carte).Take(4).ToList();
				if (list2.Count > 0)
				{
					y = Section(renderer, num, y, Strings.Succes.WithinReach, TechniqueArt.Ki);
					y = Grille(renderer, num, y, list2, 0, 2) + 16f;
				}
				List<Carte> list3 = (from carte in cartes
					where carte.Complete
					orderby carte.Courant.Points descending
					select carte).Take((list2.Count > 2) ? 4 : 6).ToList();
				y = Section(renderer, num, y, Strings.Succes.Finest, TechniqueArt.Gold);
				if (list3.Count == 0)
				{
					TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), Strings.Succes.NothingYet, _d.X(num), _d.Y(y + 14f), A(TechniqueArt.Soft));
				}
				else
				{
					Grille(renderer, num, y, list3, 0, (list2.Count > 2) ? 2 : 3);
				}
				return;
			}
			List<Carte> list4 = cartes.Where((Carte carte) => string.Equals(carte.Categorie, _categorie, StringComparison.OrdinalIgnoreCase)).ToList();
			AchievementProgress[] array = list4.SelectMany((Carte carte) => carte.Paliers).ToArray();
			string compte = Strings.Succes.CategoryCount.ToString(array.Count((AchievementProgress succes) => succes.Completed), array.Length, Nombre(array.Where((AchievementProgress succes) => succes.Completed).Sum((AchievementProgress succes) => succes.Points)));
			DessinerOutils(renderer, NomDeCategorie(_categorie), compte, Teinte(_categorie));
			list = FiltrerEtTrier(list4);
		}
		if (list.Count == 0)
		{
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(14f), flag ? Strings.Succes.NoResult : Strings.Succes.NothingHere, _d.X(598f), _d.Y(324f), A(TechniqueArt.Soft));
			return;
		}
		_rangees = (list.Count + 1) / 2;
		_scroll = Math.Clamp(_scroll, 0, Math.Max(0, _rangees - 5));
		_grille = _d.R(num, 214f, 668f, 500f);
		Rectangle previous = TechniqueArt.BeginClip(renderer, _grille);
		Grille(renderer, num, 214f, list, _scroll * 2, 5);
		TechniqueArt.EndClip(renderer, previous);
		if (_rangees > 5)
		{
			Rectangle rect2 = new Rectangle(_grille.Right + _d.L(5f), _grille.Y, _d.L(3f), _grille.Height);
			TechniqueArt.Fill(renderer, rect2, A(TechniqueArt.White, 0.05f));
			int num2 = Math.Max(_d.L(24f), rect2.Height * 5 / _rangees);
			int y2 = rect2.Y + (rect2.Height - num2) * _scroll / Math.Max(1, _rangees - 5);
			TechniqueArt.Fill(renderer, new Rectangle(rect2.X, y2, rect2.Width, num2), A(TechniqueArt.Line2));
		}
	}

	private void DessinerOutils(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string titre, string compte, Color teinte, bool sansFiltres = false)
	{
		int num = _d.Y(185f);
		int num2 = _d.X(264f);
		int num3 = _d.X(932f);
		int num4 = _d.L(34f);
		int num5 = _d.Y(168f);
		if (!sansFiltres)
		{
			string[] array = new string[3]
			{
				Strings.Succes.FilterAll.ToString(),
				Strings.Succes.FilterOngoing.ToString(),
				Strings.Succes.FilterDone.ToString()
			};
			for (int num6 = array.Length - 1; num6 >= 0; num6--)
			{
				int num7 = TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(12f), array[num6]).X + _d.L(20f);
				Rectangle rect = new Rectangle(num3 - num7, num5 + (num4 - _d.L(28f)) / 2, num7, _d.L(28f));
				bool flag = _filtre == num6;
				string cle = $"filtre-{num6}";
				TechniqueArt.Fill(renderer, rect, flag ? A(TechniqueArt.Gold, 0.12f) : A(TechniqueArt.White, 0.03f));
				TechniqueArt.Outline(renderer, rect, A(flag ? TechniqueArt.Gold : (IsHot(cle) ? TechniqueArt.Line2 : TechniqueArt.Line)));
				TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(12f), array[num6], rect.X + rect.Width / 2, num, A((flag || IsHot(cle)) ? TechniqueArt.Text : TechniqueArt.Soft));
				int choix = num6;
				AddHit(rect, cle, delegate
				{
					_filtre = choix;
					_scroll = 0;
				});
				num3 -= num7 + _d.L(5f);
			}
			num3 -= _d.L(7f);
			string text = $"{Strings.Succes.SortTool} : {NomDuTri}";
			int num8 = TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(12f), text).X + _d.L(34f);
			Rectangle rect2 = new Rectangle(num3 - num8, num5 + (num4 - _d.L(28f)) / 2, num8, _d.L(28f));
			bool flag2 = IsHot("outil:tri");
			TechniqueArt.Fill(renderer, rect2, A(new Color(255, 10, 14, 24)));
			TechniqueArt.Outline(renderer, rect2, A(flag2 ? TechniqueArt.Line2 : TechniqueArt.Line));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(12f), text, rect2.X + _d.L(10f), num, A(flag2 ? TechniqueArt.Text : TechniqueArt.Soft));
			FenetreCharte.Triangle(renderer, rect2.Right - _d.L(18f), num, _d.L(9f), _d.L(5f), versLeHaut: false, A(flag2 ? TechniqueArt.Ki : TechniqueArt.Soft));
			if (flag2)
			{
				Etiqueter("outil:tri", rect2, Strings.Succes.SortHint);
			}
			AddHit(rect2, "outil:tri", delegate
			{
				_tri = (_tri + 1) % 3;
				_scroll = 0;
			});
			num3 -= num8 + _d.L(7f);
		}
		Rectangle rect3 = new Rectangle(num3 - _d.L(30f), num5 + (num4 - _d.L(30f)) / 2, _d.L(30f), _d.L(30f));
		bool flag3 = IsHot("outil:loupe");
		if (_cherche)
		{
			TechniqueArt.FillVertical(renderer, rect3, A(new Color(255, 255, 194, 122)), A(new Color(255, 224, 117, 26)));
			TechniqueArt.Outline(renderer, rect3, A(new Color(255, 255, 194, 122)));
		}
		else
		{
			TechniqueArt.Fill(renderer, rect3, A(new Color(255, 10, 14, 24)));
			TechniqueArt.Outline(renderer, rect3, A(flag3 ? TechniqueArt.Line2 : TechniqueArt.Line));
		}
		Color couleur = A(_cherche ? new Color(255, 23, 11, 2) : (flag3 ? TechniqueArt.White : TechniqueArt.Soft));
		int num9 = _d.L(14f);
		if (_cherche)
		{
			Lisser("tech_croix.png", new Rectangle(rect3.X + (rect3.Width - num9) / 2, rect3.Y + (rect3.Height - num9) / 2, num9, num9), couleur);
		}
		else
		{
			Lisser("inv_loupe.png", new Rectangle(rect3.X + (rect3.Width - num9) / 2, rect3.Y + (rect3.Height - num9) / 2, num9, num9), couleur);
		}
		if (flag3)
		{
			Etiqueter("outil:loupe", rect3, _cherche ? Strings.Inventaire.CloseSearch : Strings.Succes.SearchTool);
		}
		AddHit(rect3, "outil:loupe", BasculerRecherche);
		num3 -= _d.L(30f) + _d.L(10f);
		if (_cherche)
		{
			Rectangle rect4 = new Rectangle(num2, num5, Math.Min(_d.L(292f), num3 - num2), num4);
			TechniqueArt.Fill(renderer, rect4, A(new Color(255, 7, 10, 18)));
			TechniqueArt.Outline(renderer, rect4, A(TechniqueArt.Ki));
			Lisser("inv_loupe.png", new Rectangle(rect4.X + _d.L(10f), rect4.Y + (rect4.Height - num9) / 2, num9, num9), A(TechniqueArt.Soft));
			if (string.IsNullOrEmpty(_recherche.Text))
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13.5f), Strings.Succes.SearchPlaceholder, rect4.X + _d.L(32f), num, A(TechniqueArt.Dim));
			}
			if (_requete.Length > 0)
			{
				TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(12f), titre, rect4.Right - _d.L(10f), num, A(teinte));
			}
		}
		else
		{
			DungeonHud.Diamond(renderer, num2 + _d.L(6f), num, _d.L(10f), A(teinte));
			int num10 = TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(22f), TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(22f), titre.ToUpperInvariant(), num3 - num2 - _d.L(24f), 1).FirstOrDefault(), num2 + _d.L(20f), num, A(TechniqueArt.Text));
			if (compte.Length > 0)
			{
				string text2 = TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(13f), compte, num3 - (num2 + _d.L(20f) + num10 + _d.L(12f)), 1).FirstOrDefault();
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(13f), text2, num2 + _d.L(20f) + num10 + _d.L(12f), num + _d.L(2f), A(TechniqueArt.Dim));
			}
		}
	}

	private float Section(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float gauche, float y, string titre, Color teinte)
	{
		int num = _d.Y(y + 8f);
		DungeonHud.Diamond(renderer, _d.X(gauche), num, _d.L(8f), A(teinte));
		int num2 = TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(11.5f), titre.ToUpperInvariant(), _d.X(gauche + 16f), num, _d.Em(0.18f, 11.5f), A(TechniqueArt.Dim));
		TechniqueArt.Fill(renderer, new Rectangle(_d.X(gauche + 26f) + num2, num, _d.X(932f) - _d.X(gauche + 26f) - num2, 1), A(TechniqueArt.Line));
		return y + 24f;
	}

	private float Grille(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float gauche, float y, List<Carte> cartes, int depuis, int rangees)
	{
		float result = y;
		for (int i = depuis; i < cartes.Count && i - depuis < rangees * 2; i++)
		{
			int num = i - depuis;
			float x = gauche + (float)(num % 2) * 339f;
			float num2 = y + (float)(num / 2) * 102f;
			DrawCarte(renderer, cartes[i], x, num2);
			result = num2 + 92f;
		}
		return result;
	}

	private void DrawRang(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float gauche, float y, AchievementProgress[] tous, int gagnes, int total)
	{
		Rectangle rect = _d.R(gauche, y, 668f, 80f);
		float num = ((total == 0) ? 0f : (100f * (float)gagnes / (float)total));
		int i;
		for (i = 0; i + 1 < Rangs.Length && num >= (float)Rangs[i + 1].Seuil; i++)
		{
		}
		var (color, sombre) = Metal(Rangs[i].Matiere);
		TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Gold, 0.13f), A(TechniqueArt.Gold, 0.05f), A(TechniqueArt.Gold, 0.02f));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Gold, 0.28f));
		Medaille(renderer, _d.R(gauche + 14f, y + 12f, 56f, 56f), Rangs[i].Matiere, faite: true, (new string[5] { "I", "II", "III", "IV", "V" })[i], petiteLegende: false, 21f);
		int value = tous.Count((AchievementProgress succes) => succes.Completed && succes.TitleRewardId != Guid.Empty);
		(string, string, string)[] array = new(string, string, string)[3]
		{
			(Strings.Succes.GaugePoints.ToString(), Nombre(gagnes), "/ " + Nombre(total)),
			(Strings.Succes.GaugeCount.ToString(), $"{tous.Count((AchievementProgress succes) => succes.Completed)}", $"/ {tous.Length}"),
			(Strings.Succes.TitlesWon.ToString(), $"{value}", string.Empty)
		};
		int num2 = _d.X(gauche + 700f - 32f - 16f);
		for (int num3 = array.Length - 1; num3 >= 0; num3--)
		{
			(string, string, string) tuple2 = array[num3];
			string item = tuple2.Item1;
			string item2 = tuple2.Item2;
			string item3 = tuple2.Item3;
			int num4 = ((item3.Length != 0) ? (TechniqueArt.Measure(renderer, TechniqueArt.Body, Pt(12f), item3).X + _d.L(5f)) : 0);
			int x = TechniqueArt.Measure(renderer, TechniqueArt.Heavy, Pt(19f), item2).X;
			int val = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(9.5f), item.ToUpperInvariant(), _d.Em(0.14f, 9.5f));
			int num5 = Math.Max(val, x + num4);
			int num6 = num2 - num5;
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(9.5f), item.ToUpperInvariant(), num6, _d.Y(y + 28f), _d.Em(0.14f, 9.5f), A(TechniqueArt.Dim));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(19f), item2, num6, _d.Y(y + 50f), A(TechniqueArt.Text));
			if (item3.Length > 0)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), item3, num6 + x + _d.L(5f), _d.Y(y + 52f), A(TechniqueArt.Dim));
			}
			num2 = num6 - _d.L(14f);
			TechniqueArt.Fill(renderer, new Rectangle(num2, _d.Y(y + 22f), 1, _d.L(36f)), A((num3 == 0) ? TechniqueArt.Line2 : TechniqueArt.Line));
			num2 -= _d.L(14f);
		}
		float x2 = gauche + 84f;
		int num7 = Math.Min(_d.L(280f), num2 - _d.X(x2) - _d.L(8f));
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), Strings.Succes.CollectorRank.ToString().ToUpperInvariant(), _d.X(x2), _d.Y(y + 19f), _d.Em(0.18f, 10f), A(TechniqueArt.Dim));
		TechniqueArt.Styled(renderer, TechniqueArt.Display, Pt(26f), NomDuRang(i).ToUpperInvariant(), _d.X(x2), _d.Y(y + 26f), TechniqueArt.Stops.Solid(color), _fade);
		int num8 = ((i + 1 < Rangs.Length) ? Rangs[i + 1].Seuil : (-1));
		float part = ((num8 < 0) ? 1f : ((num - (float)Rangs[i].Seuil) / (float)Math.Max(1, num8 - Rangs[i].Seuil)));
		Jauge(renderer, new Rectangle(_d.X(x2), _d.Y(y + 58f), num7, _d.L(6f)), part, color, sombre);
		string text = ((num8 < 0) ? Strings.Succes.TopRank.ToString() : Strings.Succes.NextRank.ToString(Nombre(Math.Max(1L, (long)MathF.Ceiling((float)(total * num8) / 100f) - gagnes)), NomDuRang(i + 1)));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), text, num7, 1).FirstOrDefault(), _d.X(x2), _d.Y(y + 71f), A(TechniqueArt.Soft));
	}

	private void DrawCarte(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Carte carte, float x, float y)
	{
		Rectangle rect = _d.R(x, y, 329f, 92f);
		AchievementProgress courant = carte.Courant;
		bool complete = carte.Complete;
		bool flag = carte.Paliers.Any((AchievementProgress palier) => palier.Id == _choisi);
		string cle = "carte-" + courant.Id;
		bool flag2 = IsHot(cle);
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
		MedailleDe(renderer, courant, _d.R(x + 10f, y + 19f, 54f, 54f), 20f);
		List<Gain> list = (from gain in carte.Paliers.SelectMany(Gains)
			group gain by gain.Sorte into groupe
			select groupe.Last()).Take(2).ToList();
		int num = rect.Right - _d.L(12f);
		int num2 = rect.Y + _d.L((list.Count > 1) ? 21 : 35);
		foreach (Gain item in list)
		{
			int num3 = Puce(renderer, item, rect.Right - _d.L(12f), num2);
			num = Math.Min(num, rect.Right - _d.L(12f) - num3 - _d.L(10f));
			num2 += _d.L(26f);
		}
		int num4 = rect.X + _d.L(74f);
		int num5 = num - num4;
		bool flag3 = carte.Paliers.Count > 1;
		string text = TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(16.5f), carte.Titre, num5, 1).FirstOrDefault();
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(16.5f), text, num4, rect.Y + _d.L(21f), A(TechniqueArt.Text));
		string text2 = ((flag3 && NomDeChaine(courant).Length > 0) ? (Texte(courant.Name) + " : " + Texte(courant.Description)) : Texte(courant.Description));
		List<string> list2 = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), text2, num5, 2);
		for (int num6 = 0; num6 < list2.Count; num6++)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), list2[num6], num4, rect.Y + _d.L(41 + num6 * 15), A(TechniqueArt.Soft));
		}
		string text3 = (complete ? (flag3 ? Strings.Succes.Complete : Strings.Succes.StateDone).ToString() : ((courant.Objective <= 1) ? Strings.Succes.ToDo.ToString() : (Nombre(Math.Min(courant.Value, courant.Objective)) + " / " + Nombre(courant.Objective))));
		int num7 = rect.Y + _d.L(77f);
		int num8 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(13f), text3, num, num7, A(complete ? TechniqueArt.Gold : BodyLight));
		Rectangle rect2 = new Rectangle(num4, num7 - _d.L(3f), num5 - num8 - _d.L(10f), _d.L(6f));
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
		AddHit(rect, cle, delegate
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
		int h = _d.L(21f);
		int num = _d.L(17f);
		int x = TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(11.5f), gain.Court).X;
		int num2 = ((gain.Icone != null) ? (num + _d.L(4f)) : _d.L(12f));
		int num3 = _d.L(6f) + num2 + x + _d.L(7f);
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
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(11.5f), gain.Court, rect.X + _d.L(6f) + num2, num4, A((gain.Icone != null) ? BodyLight : gain.Couleur));
		return num3;
	}

	private void DrawFiche(Intersect.Client.Framework.Gwen.Renderer.Base renderer, List<Carte> cartes)
	{
		Rectangle rect = _d.R(958f, 58f, 268f, 676f);
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
		Rectangle rect2 = _d.R(970f, 70f, 244f, 150f);
		TechniqueArt.FillVertical(renderer, rect2, A(new Color(255, 10, 18, 32)), A(new Color(255, 13, 21, 36)));
		TechniqueArt.Glow(renderer, (float)rect2.X + (float)rect2.Width / 2f, (float)rect2.Y + (float)rect2.Height * 0.55f, _d.L(100f), A(item, 0.26f));
		TechniqueArt.Outline(renderer, rect2, A(TechniqueArt.Line));
		int num = _d.L(92f);
		MedailleDe(renderer, succes, new Rectangle(rect2.X + (rect2.Width - num) / 2, rect2.Y + (rect2.Height - num) / 2 + _d.L(6f), num, num), 36f);
		string text = (succes.Completed ? Strings.Succes.StateDone : Strings.Succes.StateOngoing).ToString().ToUpperInvariant();
		int num2 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), text, _d.Em(0.14f, 10f)) + _d.L(12f);
		Rectangle rect3 = new Rectangle(rect2.X + _d.L(8f), rect2.Y + _d.L(8f), num2, _d.L(20f));
		if (succes.Completed)
		{
			TechniqueArt.Fill(renderer, rect3, A(TechniqueArt.Gold));
		}
		else
		{
			TechniqueArt.Fill(renderer, rect3, A(new Color(217, 6, 8, 14)));
			TechniqueArt.Outline(renderer, rect3, A(TechniqueArt.Line2));
		}
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), text, rect3.X + _d.L(6f), rect3.Y + rect3.Height / 2, _d.Em(0.14f, 10f), A(succes.Completed ? HouseArt.Dark : TechniqueArt.Text));
		string text2 = TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(10f), NomDeCategorie(categorie).ToUpperInvariant(), rect2.Width - num2 - _d.L(30f), 1).FirstOrDefault() ?? string.Empty;
		TechniqueArt.SpacedRight(renderer, TechniqueArt.Label, Pt(10f), text2, rect2.Right - _d.L(10f), rect3.Y + rect3.Height / 2, _d.Em(0.12f, 10f), A(Teinte(categorie)));
		int num3 = _d.X(974f);
		int num4 = _d.L(236f);
		float num5 = 242f;
		foreach (string item3 in TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(22f), Texte(succes.Name), num4, 2))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(22f), item3, num3, _d.Y(num5), A(TechniqueArt.Text));
			num5 += 24f;
		}
		num5 -= 2f;
		foreach (string item4 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), Texte(succes.Description), num4, 4))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), item4, num3, _d.Y(num5), A(BodyLight));
			num5 += 16f;
		}
		num5 += 10f;
		if (carte != null)
		{
			List<AchievementProgress> paliers = carte.Paliers;
			if (paliers != null && paliers.Count > 1)
			{
				string text3 = ((NomDeChaine(succes).Length == 0) ? Strings.Succes.StepsPlain.ToString() : Strings.Succes.Steps.ToString(Texte(NomDeChaine(succes))));
				string text4 = TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(10.5f), text3.ToUpperInvariant(), num4, 1).FirstOrDefault();
				TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10.5f), text4, num3, _d.Y(num5), _d.Em(0.14f, 10.5f), A(TechniqueArt.Dim));
				num5 += 12f;
				foreach (AchievementProgress palier in carte.Paliers)
				{
					Rectangle rect4 = _d.R(974f, num5, 236f, 27f);
					bool flag = palier.Id == succes.Id;
					string cle = "palier-" + palier.Id;
					TechniqueArt.Fill(renderer, rect4, flag ? A(TechniqueArt.Ki, 0.12f) : A(TechniqueArt.White, IsHot(cle) ? 0.05f : 0.025f));
					int centerY = rect4.Y + rect4.Height / 2;
					DungeonHud.Diamond(renderer, rect4.X + _d.L(8f), centerY, _d.L(8f), A(palier.Completed ? TechniqueArt.Gold : TechniqueArt.Line2));
					string text5 = ((palier.Objective <= 1) ? (palier.Completed ? Strings.Succes.Done : Strings.Succes.ToDo).ToString() : (Nombre(Math.Min(palier.Value, palier.Objective)) + " / " + Nombre(palier.Objective)));
					int num6 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(12f), text5, rect4.Right - _d.L(8f), centerY, A(palier.Completed ? TechniqueArt.Gold : TechniqueArt.Dim));
					string text6 = TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(12f), Texte(palier.Name), rect4.Width - _d.L(38f) - num6, 1).FirstOrDefault();
					TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(12f), text6, rect4.X + _d.L(22f), centerY, A(flag ? TechniqueArt.Text : BodyLight));
					string id = palier.Id;
					AddHit(rect4, cle, delegate
					{
						Choisir(id);
					});
					num5 += 28f;
				}
				goto IL_0a7d;
			}
		}
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10.5f), Strings.Succes.Progression.ToString().ToUpperInvariant(), num3, _d.Y(num5), _d.Em(0.14f, 10.5f), A(TechniqueArt.Dim));
		num5 += 20f;
		string text7 = (succes.Completed ? Strings.Succes.Finished.ToString() : Strings.Succes.Missing.ToString(Nombre(Math.Max(0L, succes.Objective - succes.Value))));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), text7, num3, _d.Y(num5), A(TechniqueArt.Soft));
		TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(18f), Nombre(Math.Min(succes.Value, succes.Objective)) + " / " + Nombre(succes.Objective), num3 + num4, _d.Y(num5 - 2f), A(TechniqueArt.Text));
		num5 += 14f;
		Jauge(renderer, _d.R(974f, num5, 236f, 7f), Part(succes), item, item2);
		num5 += 7f;
		goto IL_0a7d;
		IL_0a7d:
		num5 += 16f;
		List<Gain> list = Gains(succes);
		if (list.Count > 0)
		{
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10.5f), Strings.Succes.Rewards.ToString().ToUpperInvariant(), num3, _d.Y(num5), _d.Em(0.14f, 10.5f), A(TechniqueArt.Dim));
			num5 += 12f;
			foreach (Gain item5 in list)
			{
				if (num5 > 664f)
				{
					break;
				}
				num5 = ((item5.Titre != Guid.Empty) ? PlaqueDeTitre(renderer, item5, num5) : LigneDeGain(renderer, item5, num5));
			}
		}
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), Strings.Succes.Worth.ToString(succes.Points), num3, _d.Y(716f), A(TechniqueArt.Dim));
	}

	private float LigneDeGain(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Gain gain, float y)
	{
		Rectangle rect = _d.R(974f, y, 236f, 31f);
		TechniqueArt.Fill(renderer, rect, A(new Color(153, 6, 8, 14)));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
		int num = rect.Y + rect.Height / 2;
		int num2 = _d.L(22f);
		if (gain.Icone != null)
		{
			SaiyanHud.Draw(renderer, gain.Icone, rect.X + _d.L(7f), num - num2 / 2, num2, num2, A(TechniqueArt.White));
		}
		else
		{
			DungeonHud.Diamond(renderer, rect.X + _d.L(14f), num, _d.L(9f), A(gain.Couleur));
		}
		string text = TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(12.5f), gain.Long, rect.Width - _d.L(46f), 1).FirstOrDefault();
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(12.5f), text, rect.X + _d.L(38f), num, A(TechniqueArt.Text));
		return y + 36f;
	}

	private float PlaqueDeTitre(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Gain gain, float y)
	{
		Rectangle rect = _d.R(974f, y, 236f, 58f);
		TechniqueArt.FillHorizontal(renderer, rect, A(gain.Couleur, 0.12f), A(gain.Couleur, 0.04f), A(gain.Couleur, 0f));
		TechniqueArt.Outline(renderer, rect, A(gain.Couleur, 0.45f));
		Rectangle rect2 = new Rectangle(rect.X + _d.L(8f), rect.Y + _d.L(8f), _d.L(42f), _d.L(42f));
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
		int num4 = rect2.Right + _d.L(9f);
		int width = rect.Right - num4 - _d.L(8f);
		string text = TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(15f), Globals.Me?.Name ?? string.Empty, width, 1).FirstOrDefault();
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(15f), text, num4, rect.Y + _d.L(15f), A(TechniqueArt.Text));
		string text2 = TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(12f), gain.Long, width, 1).FirstOrDefault();
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(12f), text2, num4, rect.Y + _d.L(31f), A(gain.Couleur));
		string text3 = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(10.5f), Strings.Succes.TitleCaption, width, 1).FirstOrDefault();
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(10.5f), text3, num4, rect.Y + _d.L(46f), A(TechniqueArt.Dim));
		return y + 64f;
	}
}
