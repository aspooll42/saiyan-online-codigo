using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Items;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Dungeons;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Housing;
using Intersect.Client.Interface.Game.Personnage;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Framework.Core.GameObjects.Crafting;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Framework.Core.GameObjects.Resources;
using Intersect.Models;

namespace Intersect.Client.Interface.Game.Metiers;

internal sealed class FenetreMetiers : FenetreCharte
{
	private sealed class Entree
	{
		public string Id = string.Empty;

		public string Nom = string.Empty;

		public string Icone = string.Empty;

		public int NiveauRequis = 1;

		public string Detail = string.Empty;

		public ResourceDescriptor? Ressource;

		public CraftingRecipeDescriptor? Recette;

		public NPCDescriptor? Gibier;

		private string? _cherchable;

		public string Cherchable => _cherchable ?? (_cherchable = PersonnageScreen.Normaliser(Nom + " " + Detail));

		public bool Ouverte(int niveau)
		{
			return NiveauRequis <= niveau;
		}
	}

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

	private const float HeaderHeight = 100f;

	private const float BonusTop = 186f;

	private const float BonusHeight = 56f;

	private const float ToolsTop = 256f;

	private const float ToolsHeight = 34f;

	private const float GridTop = 302f;

	private const float CardWidth = 329f;

	private const float CardHeight = 92f;

	private const float CardGap = 10f;

	private const int VisibleRows = 4;

	private static readonly Color BodyLight = new Color(255, 196, 204, 220);

	private int _metier;

	private int _filtre;

	private int _tri;

	private string _choisie = string.Empty;

	private int _scroll;

	private int _rangees;

	private Rectangle _grille;

	private string _signature = string.Empty;

	private List<Entree> _entrees = new List<Entree>();

	private TextBox _recherche;

	private string _requete = string.Empty;

	private bool _cherche;

	private const float ChampLargeur = 292f;

	protected override Rectangle Place => new Rectangle(180, 76, 1240, 748);

	public bool IsOpen => !base.IsHidden;

	private string NomDuTri => ((_tri == 1) ? Strings.Professions.SortName : Strings.Professions.SortLevel).ToString();

	private string TitreDeLaListe => (Professions.IsCrafting(_metier) ? Strings.Professions.Recipes : ((_metier == 4) ? Strings.Professions.Game : Strings.Professions.Resources)).ToString();

	public FenetreMetiers(Canvas gameCanvas)
		: base(gameCanvas, "FenetreMetiers")
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

	public void Close()
	{
		Hide();
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
			_scroll = Math.Clamp(_scroll + ((delta <= 0) ? 1 : (-1)), 0, Math.Max(0, _rangees - 4));
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
			Rectangle rectangle = _d.R(296f, 259f, 162f, 28f);
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

	private static string Texte(string? contenu)
	{
		return Traduction.Contenu(contenu ?? string.Empty);
	}

	private static string Nombre(long valeur)
	{
		return MarketWindow.FormatAmount(valeur);
	}

	private static double Ratio(Player me, int metier)
	{
		long num = me.ProfessionNextLevel[metier];
		if (num > 0)
		{
			return Math.Clamp((double)me.ProfessionExperience[metier] / (double)num, 0.0, 1.0);
		}
		return 1.0;
	}

	private static int DansLeSac(Player me, Guid objet)
	{
		return me.Inventory.Where((IItem item) => item != null && item.ItemId == objet).Sum((IItem item) => item.Quantity);
	}

	private static IEnumerable<ItemDescriptor> ObjetsDe(IEnumerable<Drop> drops)
	{
		ItemDescriptor databaseObject;
		return (from drop in drops
			select (!DatabaseObject<ItemDescriptor>.TryGet(drop.ItemId, out databaseObject)) ? null : databaseObject into item
			where item != null
			select item).Distinct();
	}

	private List<Entree> Entrees()
	{
		string text = $"{_metier}:{DatabaseObject<ResourceDescriptor>.Lookup.Count}:{DatabaseObject<NPCDescriptor>.Lookup.Count}:{DatabaseObject<CraftingRecipeDescriptor>.Lookup.Count}:{DatabaseObject<ItemDescriptor>.Lookup.Count}";
		if (text == _signature)
		{
			return _entrees;
		}
		_signature = text;
		List<Entree> list = new List<Entree>();
		if (Professions.IsCrafting(_metier))
		{
			foreach (CraftingRecipeDescriptor item in from recette in DatabaseObject<CraftingRecipeDescriptor>.Lookup.Values.OfType<CraftingRecipeDescriptor>()
				where recette.Profession == _metier
				select recette)
			{
				ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(item.ItemId);
				list.Add(new Entree
				{
					Id = "recette:" + item.Id,
					Nom = Texte(itemDescriptor?.Name ?? item.Name),
					Icone = (itemDescriptor?.Icon ?? Professions.Icon(_metier)),
					NiveauRequis = Math.Max(1, item.ProfessionLevel),
					Detail = string.Join(" · ", from ingredient in item.Ingredients
						select (Item: DatabaseObject<ItemDescriptor>.Get(ingredient.ItemId), Quantity: ingredient.Quantity) into e
						where e.Item != null
						select $"{e.Quantity} {Texte(e.Item.Name)}"),
					Recette = item
				});
			}
		}
		else if (_metier == 4)
		{
			foreach (NPCDescriptor item2 in from npc in DatabaseObject<NPCDescriptor>.Lookup.Values.OfType<NPCDescriptor>()
				where npc.IsGame
				select npc)
			{
				List<Drop> list2 = item2.Drops.Where((Drop drop) => drop.Hunting).ToList();
				List<ItemDescriptor> source = ObjetsDe((list2.Count > 0) ? list2 : item2.Drops).ToList();
				list.Add(new Entree
				{
					Id = "gibier:" + item2.Id,
					Nom = Texte(item2.Name),
					Icone = (source.FirstOrDefault()?.Icon ?? Professions.Icon(4)),
					NiveauRequis = Math.Max(1, item2.HunterLevel),
					Detail = string.Join(" · ", source.Select((ItemDescriptor item) => Texte(item.Name))),
					Gibier = item2
				});
			}
		}
		else
		{
			foreach (ResourceDescriptor item3 in from ressource in DatabaseObject<ResourceDescriptor>.Lookup.Values.OfType<ResourceDescriptor>()
				where Professions.Of(ressource) == _metier
				select ressource)
			{
				List<ItemDescriptor> source2 = ObjetsDe(item3.Drops).ToList();
				list.Add(new Entree
				{
					Id = "ressource:" + item3.Id,
					Nom = Texte(item3.Name),
					Icone = (source2.FirstOrDefault()?.Icon ?? Professions.Icon(_metier)),
					NiveauRequis = Math.Max(1, item3.ProfessionLevel),
					Detail = string.Join(" · ", source2.Select((ItemDescriptor item) => Texte(item.Name))),
					Ressource = item3
				});
			}
		}
		return _entrees = list;
	}

	private List<Entree> FiltrerEtTrier(IEnumerable<Entree> entrees, int niveau)
	{
		IEnumerable<Entree> source = entrees.Where((Entree entree) => _filtre == 0 || ((_filtre != 1) ? (!entree.Ouverte(niveau)) : entree.Ouverte(niveau)));
		if (_requete.Length > 0)
		{
			string[] mots = _requete.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			source = source.Where((Entree entree) => mots.All((string mot) => entree.Cherchable.Contains(mot, StringComparison.Ordinal)));
		}
		StringComparer currentCultureIgnoreCase = StringComparer.CurrentCultureIgnoreCase;
		return ((_tri == 1) ? source.OrderBy<Entree, string>((Entree entree) => entree.Nom, currentCultureIgnoreCase) : source.OrderBy((Entree entree) => entree.NiveauRequis).ThenBy<Entree, string>((Entree entree) => entree.Nom, currentCultureIgnoreCase)).ToList();
	}

	private Entree? Choisie()
	{
		List<Entree> source = Entrees();
		return source.FirstOrDefault((Entree entree) => entree.Id == _choisie) ?? source.OrderBy((Entree entree) => entree.NiveauRequis).FirstOrDefault();
	}

	private void Ouvrir(int metier)
	{
		if (metier != _metier)
		{
			_metier = metier;
			_choisie = string.Empty;
			_scroll = 0;
			FermerRecherche();
		}
	}

	private static List<string> Ateliers(int metier)
	{
		return (from atelier in DatabaseObject<CraftingTableDescriptor>.Lookup.Values.OfType<CraftingTableDescriptor>()
			where MetiersArtisanat.MetierDeLAtelier(atelier) == metier
			select Texte(atelier.Name)).OrderBy<string, string>((string nom) => nom, StringComparer.CurrentCultureIgnoreCase).ToList();
	}

	protected override void Dessiner(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		DessinerCadre(renderer);
		int num = DessinerBarre(renderer, Strings.Professions.Title);
		Player me = Globals.Me;
		if (me != null)
		{
			int num2 = _d.Y(23f);
			int value = Enumerable.Range(0, 9).Sum((Func<int, int>)me.ProfessionLevel);
			int num3 = num - _d.L(18f);
			int num4 = TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(12f), $"/ {9 * me.ProfessionMaximumLevel}", num3, num2 + _d.L(1f), A(TechniqueArt.Dim));
			num3 -= num4 + _d.L(5f);
			int num5 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(16f), $"{value}", num3, num2, A(TechniqueArt.Gold));
			num3 -= num5 + _d.L(8f);
			string text = Strings.Professions.TotalLevel.ToString().ToUpperInvariant();
			int num6 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), text, _d.Em(0.16f, 10f));
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), text, num3 - num6, num2 + _d.L(1f), _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
			DrawRail(renderer, me);
			DrawCentre(renderer, me);
			DrawFiche(renderer, me);
		}
	}

	protected override void DessinerDessus(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		DessinerEtiquette(renderer, now);
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

	private IGameTexture? IconeDe(string icone)
	{
		return Globals.ContentManager?.GetTexture(TextureType.Item, icone);
	}

	private void Icone(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string icone, Rectangle carre, float alpha = 1f)
	{
		IGameTexture gameTexture = IconeDe(icone);
		if (gameTexture != null)
		{
			int num = Math.Min(carre.Width, TailleIcone(gameTexture.Width));
			SaiyanHud.Draw(renderer, gameTexture, carre.X + (carre.Width - num) / 2, carre.Y + (carre.Height - num) / 2, num, num, A(TechniqueArt.White, alpha));
		}
	}

	private void DrawRail(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me)
	{
		Panel(renderer, _d.R(14f, 58f, 224f, 676f));
		float y = 70f;
		y = SectionDuRail(renderer, y, Strings.Professions.TabGathering);
		for (int i = 0; i < 5; i++)
		{
			y = EntreeDuRail(renderer, y, i, me);
		}
		y += 8f;
		y = SectionDuRail(renderer, y, Strings.Professions.TabCrafting);
		for (int j = 5; j < 9; j++)
		{
			y = EntreeDuRail(renderer, y, j, me);
		}
		List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), Strings.Professions.RailHint, _d.L(200f), 3);
		int num = _d.Y(722f);
		for (int num2 = list.Count - 1; num2 >= 0; num2--)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), list[num2], _d.X(26f), num - _d.L(7f), A(TechniqueArt.Dim));
			num -= _d.L(14f);
		}
	}

	private float SectionDuRail(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float y, string titre)
	{
		int num = _d.Y(y + 8f);
		int num2 = TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(11f), titre.ToUpperInvariant(), _d.X(30f), num, _d.Em(0.18f, 11f), A(TechniqueArt.Dim));
		TechniqueArt.Fill(renderer, new Rectangle(_d.X(38f) + num2, num, _d.X(226f) - _d.X(38f) - num2, 1), A(TechniqueArt.Line));
		return y + 22f;
	}

	private float EntreeDuRail(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float y, int metier, Player me)
	{
		Rectangle rect = _d.R(22f, y, 208f, 50f);
		bool flag = metier == _metier;
		string cle = $"rail-{metier}";
		Color color = Professions.Tint(metier);
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
		int num = _d.L(30f);
		Icone(renderer, Professions.Icon(metier), new Rectangle(rect.X + _d.L(9f), rect.Y + (rect.Height - num) / 2 - _d.L(3f), num, num));
		int centerY = rect.Y + _d.L(17f);
		string text = Strings.Professions.Level.ToString(me.ProfessionLevel(metier));
		int num2 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(12.5f), text, rect.Right - _d.L(10f), centerY, A(flag ? TechniqueArt.Text : color));
		string text2 = TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(13f), Professions.Name(metier), rect.Right - _d.L(16f) - num2 - (rect.X + _d.L(46f)), 1).FirstOrDefault();
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(13f), text2, rect.X + _d.L(46f), centerY, A(flag ? TechniqueArt.Text : BodyLight));
		Jauge(renderer, new Rectangle(rect.X + _d.L(46f), rect.Y + _d.L(32f), rect.Width - _d.L(56f), _d.L(4f)), (float)Ratio(me, metier), TechniqueArt.Mix(color, TechniqueArt.White, 0.8f), TechniqueArt.Mix(color, TechniqueArt.Black, 0.7f));
		int choix = metier;
		AddHit(rect, cle, delegate
		{
			Ouvrir(choix);
		});
		return y + 50f + 2f;
	}

	private void DrawCentre(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me)
	{
		Panel(renderer, _d.R(248f, 58f, 700f, 676f));
		float num = 264f;
		_grille = default(Rectangle);
		_rangees = 0;
		DrawHeader(renderer, me, num);
		DrawBonus(renderer, me, num);
		int niveau = me.ProfessionLevel(_metier);
		List<Entree> list = Entrees();
		List<Entree> list2 = FiltrerEtTrier(list, niveau);
		int num2 = list.Count((Entree entree) => entree.Ouverte(niveau));
		string compte = ((_metier == 4) ? Strings.Professions.GameUnlockedCount : Strings.Professions.UnlockedCount).ToString(num2, list.Count);
		DessinerOutils(renderer, (_requete.Length > 0) ? Strings.Professions.Results.ToString(list2.Count) : TitreDeLaListe, compte, Professions.Tint(_metier));
		if (list.Count == 0)
		{
			string text = (Professions.IsCrafting(_metier) ? Strings.Professions.NoRecipe : ((_metier == 4) ? Strings.Professions.NoGame : Strings.Professions.NoResource)).ToString();
			{
				foreach (var (text2, num3) in TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(13.5f), text, _d.L(668f), 3).Select((string ligne, int index) => (ligne: ligne, index: index)))
				{
					TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(13.5f), text2, _d.X(598f), _d.Y(392f + (float)(num3 * 18)), A(TechniqueArt.Soft));
				}
				return;
			}
		}
		if (list2.Count == 0)
		{
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(14f), (_requete.Length > 0) ? Strings.Professions.NoResult : Strings.Professions.NothingHere, _d.X(598f), _d.Y(392f), A(TechniqueArt.Soft));
			return;
		}
		_rangees = (list2.Count + 1) / 2;
		_scroll = Math.Clamp(_scroll, 0, Math.Max(0, _rangees - 4));
		_grille = _d.R(num, 302f, 668f, 398f);
		Rectangle previous = TechniqueArt.BeginClip(renderer, _grille);
		for (int num4 = _scroll * 2; num4 < list2.Count && num4 - _scroll * 2 < 8; num4++)
		{
			int num5 = num4 - _scroll * 2;
			DrawCarte(renderer, list2[num4], num + (float)(num5 % 2) * 339f, 302f + (float)(num5 / 2) * 102f, niveau);
		}
		TechniqueArt.EndClip(renderer, previous);
		if (_rangees > 4)
		{
			Rectangle rect = new Rectangle(_grille.Right + _d.L(5f), _grille.Y, _d.L(3f), _grille.Height);
			TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, 0.05f));
			int num6 = Math.Max(_d.L(24f), rect.Height * 4 / _rangees);
			int y = rect.Y + (rect.Height - num6) * _scroll / Math.Max(1, _rangees - 4);
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, y, rect.Width, num6), A(TechniqueArt.Line2));
		}
	}

	private void DrawHeader(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, float gauche)
	{
		Rectangle rect = _d.R(gauche, 74f, 668f, 100f);
		Color color = Professions.Tint(_metier);
		TechniqueArt.FillHorizontal(renderer, rect, A(color, 0.14f), A(color, 0.05f), A(color, 0.02f));
		TechniqueArt.Outline(renderer, rect, A(color, 0.3f));
		Rectangle hex = _d.R(gauche + 14f, 86f, 56f, 56f);
		Hexagone(renderer, hex, color, IconeDe(Professions.Icon(_metier)), TechniqueArt.White, vide: false);
		int x = _d.X(gauche + 84f);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(24f), Professions.Name(_metier).ToUpperInvariant(), x, _d.Y(100f), A(TechniqueArt.Text));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), Professions.Subtitle(_metier), x, _d.Y(122f), A(TechniqueArt.Soft));
		int num = me.ProfessionLevel(_metier);
		int num2 = rect.Right - _d.L(16f);
		TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(30f), $"{num}", num2, _d.Y(110f), A(TechniqueArt.Gold));
		int x2 = TechniqueArt.Measure(renderer, TechniqueArt.Heavy, Pt(30f), $"{num}").X;
		TechniqueArt.SpacedRight(renderer, TechniqueArt.Label, Pt(10f), Strings.Professions.LevelCaps.ToString().ToUpperInvariant(), num2 - x2 - _d.L(10f), _d.Y(112f), _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
		long num3 = me.ProfessionExperience[_metier];
		long num4 = me.ProfessionNextLevel[_metier];
		int num5 = _d.Y(158f);
		Jauge(renderer, new Rectangle(rect.X + _d.L(14f), num5, rect.Width - _d.L(28f), _d.L(7f)), (float)Ratio(me, _metier), TechniqueArt.Mix(color, TechniqueArt.White, 0.7f), TechniqueArt.Mix(color, TechniqueArt.Black, 0.6f));
		string text = ((num4 > 0) ? Strings.Professions.ExperienceLeft.ToString(Nombre(num4 - num3), num + 1) : Strings.Professions.MaximumLevel.ToString());
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), text, rect.X + _d.L(14f), num5 - _d.L(11f), A(BodyLight));
		if (num4 > 0)
		{
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(12f), Strings.Professions.ExperienceValue.ToString(Nombre(num3), Nombre(num4)), rect.Right - _d.L(14f), num5 - _d.L(11f), A(TechniqueArt.Text));
		}
	}

	private void DrawBonus(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, float gauche)
	{
		Rectangle rect = _d.R(gauche, 186f, 668f, 56f);
		TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, 0.025f));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
		int niveau = me.ProfessionLevel(_metier);
		List<Entree> list = Entrees();
		List<(string, string)> list3;
		if (!Professions.IsCrafting(_metier))
		{
			List<(string, string)> list2 = new List<(string, string)>();
			list2.Add((Professions.SpeedLabel(_metier), Strings.Professions.Bonus.ToString(Professions.Percent((double)niveau * me.ProfessionFirstBonusPerLevel(_metier)))));
			list2.Add((Strings.Professions.RareChance.ToString(), Strings.Professions.Bonus.ToString(Professions.Percent((double)niveau * me.ProfessionRarityPerLevel))));
			list3 = list2;
		}
		else
		{
			List<(string, string)> list2 = new List<(string, string)>();
			list2.Add((Professions.SpeedLabel(_metier), Professions.Percent(me.PreciseChance(_metier)) + " %"));
			list2.Add((Strings.Professions.RecipesUnlocked.ToString(), Strings.Professions.Ratio.ToString(list.Count((Entree entree) => entree.Ouverte(niveau)), list.Count)));
			list3 = list2;
		}
		List<(string, string)> list4 = list3;
		if (Professions.IsCrafting(_metier))
		{
			List<string> list5 = Ateliers(_metier);
			if (list5 != null && list5.Count > 0)
			{
				list4.Add((((list5.Count > 1) ? Strings.Professions.Workshops : Strings.Professions.Workshop).ToString(), string.Join(", ", list5)));
			}
		}
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), Strings.Professions.WhatYourLevelGives.ToString().ToUpperInvariant(), rect.X + _d.L(14f), rect.Y + _d.L(14f), _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
		int num = (rect.Width - _d.L(28f)) / list4.Count;
		for (int num2 = 0; num2 < list4.Count; num2++)
		{
			(string, string) tuple = list4[num2];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			int num3 = rect.X + _d.L(14f) + num2 * num;
			int num4 = TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(15f), TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(15f), item2, num - _d.L(16f), 1).FirstOrDefault(), num3, rect.Y + _d.L(37f), A((num2 == 0) ? TechniqueArt.Gold : TechniqueArt.Text));
			int num5 = num - num4 - _d.L(24f);
			if (num5 > _d.L(40f))
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), item, num5, 1).FirstOrDefault(), num3 + num4 + _d.L(8f), rect.Y + _d.L(38f), A(BodyLight));
			}
			if (num2 > 0)
			{
				TechniqueArt.Fill(renderer, new Rectangle(num3 - _d.L(10f), rect.Y + _d.L(10f), 1, rect.Height - _d.L(20f)), A(TechniqueArt.Line));
			}
		}
	}

	private void DessinerOutils(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string titre, string compte, Color teinte)
	{
		int num = _d.Y(273f);
		int num2 = _d.X(264f);
		int num3 = _d.X(932f);
		int num4 = _d.L(34f);
		int num5 = _d.Y(256f);
		string[] array = new string[3]
		{
			Strings.Professions.FilterAll.ToString(),
			Strings.Professions.FilterUnlocked.ToString(),
			Strings.Professions.FilterLocked.ToString()
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
		string text = $"{Strings.Professions.SortTool} : {NomDuTri}";
		int num8 = TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(12f), text).X + _d.L(34f);
		Rectangle rect2 = new Rectangle(num3 - num8, num5 + (num4 - _d.L(28f)) / 2, num8, _d.L(28f));
		bool flag2 = IsHot("outil:tri");
		TechniqueArt.Fill(renderer, rect2, A(new Color(255, 10, 14, 24)));
		TechniqueArt.Outline(renderer, rect2, A(flag2 ? TechniqueArt.Line2 : TechniqueArt.Line));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(12f), text, rect2.X + _d.L(10f), num, A(flag2 ? TechniqueArt.Text : TechniqueArt.Soft));
		FenetreCharte.Triangle(renderer, rect2.Right - _d.L(18f), num, _d.L(9f), _d.L(5f), versLeHaut: false, A(flag2 ? TechniqueArt.Ki : TechniqueArt.Soft));
		if (flag2)
		{
			Etiqueter("outil:tri", rect2, Strings.Professions.SortHint);
		}
		AddHit(rect2, "outil:tri", delegate
		{
			_tri = (_tri + 1) % 2;
			_scroll = 0;
		});
		num3 -= num8 + _d.L(7f);
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
		Lisser(_cherche ? "tech_croix.png" : "inv_loupe.png", new Rectangle(rect3.X + (rect3.Width - num9) / 2, rect3.Y + (rect3.Height - num9) / 2, num9, num9), couleur);
		if (flag3)
		{
			Etiqueter("outil:loupe", rect3, _cherche ? Strings.Inventaire.CloseSearch : Strings.Professions.SearchTool);
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
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13.5f), Strings.Professions.SearchPlaceholder, rect4.X + _d.L(32f), num, A(TechniqueArt.Dim));
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
			string text2 = TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(13f), compte, num3 - (num2 + _d.L(20f) + num10 + _d.L(12f)), 1).FirstOrDefault();
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(13f), text2, num2 + _d.L(20f) + num10 + _d.L(12f), num + _d.L(2f), A(TechniqueArt.Dim));
		}
	}

	private void DrawCarte(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Entree entree, float x, float y, int niveau)
	{
		Rectangle rect = _d.R(x, y, 329f, 92f);
		bool flag = entree.Ouverte(niveau);
		bool flag2 = entree.Id == _choisie;
		string cle = "carte-" + entree.Id;
		bool flag3 = IsHot(cle);
		if (flag2)
		{
			TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Ki, 0.14f), A(TechniqueArt.Ki, 0.06f), A(TechniqueArt.Ki, 0.02f));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Ki));
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Ki));
		}
		else if (flag)
		{
			TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Green, flag3 ? 0.12f : 0.07f), A(TechniqueArt.Green, 0.03f), A(TechniqueArt.Green, 0.01f));
			TechniqueArt.Outline(renderer, rect, A(flag3 ? TechniqueArt.Line2 : TechniqueArt.Line));
		}
		else
		{
			TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, flag3 ? 0.05f : 0.025f));
			TechniqueArt.Outline(renderer, rect, A(flag3 ? TechniqueArt.Line2 : TechniqueArt.Line));
		}
		Color color = Professions.Tint(_metier);
		Rectangle hex = _d.R(x + 10f, y + 19f, 54f, 54f);
		Hexagone(renderer, hex, flag ? color : TechniqueArt.Line2, IconeDe(entree.Icone), flag ? TechniqueArt.White : TechniqueArt.Alpha(TechniqueArt.White, 0.4f), !flag);
		int num = rect.X + _d.L(74f);
		int centerY = rect.Y + _d.L(21f);
		string text = (flag ? ((_metier == 4) ? Strings.Professions.GameUnlocked : Strings.Professions.Unlocked).ToString() : Strings.Professions.Level.ToString(entree.NiveauRequis));
		int num2 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(12.5f), text, rect.Right - _d.L(12f), centerY, A(flag ? TechniqueArt.Green : TechniqueArt.Text));
		int width = rect.Right - _d.L(12f) - num2 - _d.L(10f) - num;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(16.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(16.5f), entree.Nom, width, 1).FirstOrDefault(), num, centerY, A(flag ? TechniqueArt.Text : BodyLight));
		if (!flag)
		{
			TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(11f), Strings.Professions.LevelsLeft.ToString(entree.NiveauRequis - niveau), rect.Right - _d.L(12f), rect.Y + _d.L(40f), A(TechniqueArt.Dim));
		}
		List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), entree.Detail, rect.Right - _d.L(12f) - num - ((!flag) ? _d.L(90f) : 0), 2);
		for (int i = 0; i < list.Count; i++)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), list[i], num, rect.Y + _d.L(41 + i * 15), A(TechniqueArt.Soft));
		}
		int centerY2 = rect.Y + _d.L(77f);
		CraftingRecipeDescriptor recette = entree.Recette;
		object obj;
		if (recette != null)
		{
			obj = ((recette.ProfessionExperience > 0) ? Strings.Workshop.TimeAndExperience.ToString(Math.Max(1, recette.Time / 1000), recette.ProfessionExperience) : Strings.Workshop.TimeOnly.ToString(Math.Max(1, recette.Time / 1000)));
		}
		else
		{
			NPCDescriptor gibier = entree.Gibier;
			if (gibier == null)
			{
				ResourceDescriptor ressource = entree.Ressource;
				obj = ((ressource != null && ressource.ProfessionExperience > 0) ? Strings.Professions.ResourceExperience.ToString(ressource.ProfessionExperience) : string.Empty);
			}
			else
			{
				obj = Strings.Professions.MonsterLevel.ToString(gibier.Level) + ((gibier.HunterExperience > 0) ? (" · " + Strings.Professions.HunterExperience.ToString(gibier.HunterExperience)) : string.Empty);
			}
		}
		string text2 = (string)obj;
		if (text2.Length > 0)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(11f), TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(11f), text2, rect.Right - _d.L(12f) - num, 1).FirstOrDefault(), num, centerY2, A(TechniqueArt.Dim));
		}
		string id = entree.Id;
		AddHit(rect, cle, delegate
		{
			_choisie = id;
		});
	}

	private void DrawFiche(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me)
	{
		Panel(renderer, _d.R(958f, 58f, 268f, 676f));
		int num = _d.X(974f);
		int num2 = _d.L(236f);
		Entree entree = Choisie();
		if (entree == null)
		{
			string text = ((_metier == 4) ? Strings.Professions.OtherMonsters : Strings.Professions.PickHint).ToString();
			float num3 = 98f;
			{
				foreach (string item in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), text, num2, 6))
				{
					TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), item, num, _d.Y(num3), A(BodyLight));
					num3 += 16f;
				}
				return;
			}
		}
		int num4 = me.ProfessionLevel(_metier);
		bool flag = entree.Ouverte(num4);
		Color color = Professions.Tint(_metier);
		Rectangle rect = _d.R(970f, 70f, 244f, 150f);
		TechniqueArt.FillVertical(renderer, rect, A(new Color(255, 10, 18, 32)), A(new Color(255, 13, 21, 36)));
		TechniqueArt.Glow(renderer, (float)rect.X + (float)rect.Width / 2f, (float)rect.Y + (float)rect.Height * 0.55f, _d.L(100f), A(flag ? color : TechniqueArt.Line2, 0.26f));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
		int num5 = _d.L(92f);
		Hexagone(renderer, new Rectangle(rect.X + (rect.Width - num5) / 2, rect.Y + (rect.Height - num5) / 2 + _d.L(6f), num5, num5), flag ? color : TechniqueArt.Line2, IconeDe(entree.Icone), flag ? TechniqueArt.White : TechniqueArt.Alpha(TechniqueArt.White, 0.45f), !flag);
		string text2 = (flag ? Strings.Professions.Unlocked.ToString() : Strings.Professions.Level.ToString(entree.NiveauRequis)).ToUpperInvariant();
		int num6 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), text2, _d.Em(0.14f, 10f)) + _d.L(12f);
		Rectangle rect2 = new Rectangle(rect.X + _d.L(8f), rect.Y + _d.L(8f), num6, _d.L(20f));
		if (flag)
		{
			TechniqueArt.Fill(renderer, rect2, A(TechniqueArt.Green));
		}
		else
		{
			TechniqueArt.Fill(renderer, rect2, A(new Color(217, 6, 8, 14)));
			TechniqueArt.Outline(renderer, rect2, A(TechniqueArt.Line2));
		}
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), text2, rect2.X + _d.L(6f), rect2.Y + rect2.Height / 2, _d.Em(0.14f, 10f), A(flag ? HouseArt.Dark : TechniqueArt.Text));
		string text3 = TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(10f), Professions.Name(_metier).ToUpperInvariant(), rect.Width - num6 - _d.L(30f), 1).FirstOrDefault() ?? string.Empty;
		TechniqueArt.SpacedRight(renderer, TechniqueArt.Label, Pt(10f), text3, rect.Right - _d.L(10f), rect2.Y + rect2.Height / 2, _d.Em(0.12f, 10f), A(color));
		float num7 = 242f;
		foreach (string item2 in TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(22f), entree.Nom, num2, 2))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(22f), item2, num, _d.Y(num7), A(TechniqueArt.Text));
			num7 += 24f;
		}
		num7 -= 2f;
		object obj;
		if (entree.Recette == null)
		{
			NPCDescriptor gibier = entree.Gibier;
			obj = ((gibier != null) ? (Strings.Professions.MonsterLevel.ToString(gibier.Level) + " · " + Strings.Professions.HuntedWith.ToString(entree.NiveauRequis)) : Strings.Professions.HarvestedWith.ToString(Professions.Name(_metier), entree.NiveauRequis));
		}
		else
		{
			obj = (flag ? Strings.Professions.CraftedWith : Strings.Professions.CraftedWithLocked).ToString(Professions.Name(_metier), entree.NiveauRequis);
		}
		string text4 = (string)obj;
		foreach (string item3 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), text4, num2, 3))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), item3, num, _d.Y(num7), A(BodyLight));
			num7 += 16f;
		}
		if (!flag)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), Strings.Professions.LevelsLeft.ToString(entree.NiveauRequis - num4), num, _d.Y(num7), A(TechniqueArt.Ki));
			num7 += 16f;
		}
		num7 += 10f;
		CraftingRecipeDescriptor recette = entree.Recette;
		if (recette != null)
		{
			num7 = Rubrique(renderer, num7, Strings.Workshop.Ingredients, num);
			TechniqueArt.SpacedRight(renderer, TechniqueArt.Label, Pt(10f), Strings.Workshop.BagAndNeeded.ToString().ToUpperInvariant(), num + num2, _d.Y(num7 - 12f + 8f), _d.Em(0.12f, 10f), A(TechniqueArt.Dim));
			foreach (CraftingRecipeIngredient ingredient in recette.Ingredients)
			{
				ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(ingredient.ItemId);
				if (itemDescriptor != null)
				{
					int num8 = DansLeSac(me, ingredient.ItemId);
					num7 = LigneObjet(renderer, num7, itemDescriptor, Strings.Workshop.OwnedOfNeeded.ToString(Nombre(num8), ingredient.Quantity), (num8 >= ingredient.Quantity) ? TechniqueArt.Green : TechniqueArt.Text);
				}
			}
			num7 += 10f;
			ItemDescriptor itemDescriptor2 = DatabaseObject<ItemDescriptor>.Get(recette.ItemId);
			if (itemDescriptor2 != null)
			{
				num7 = Rubrique(renderer, num7, Strings.Professions.Produces, num);
				num7 = LigneObjet(renderer, num7, itemDescriptor2, $"×{recette.Quantity}", TechniqueArt.Text);
			}
			string text5 = ((recette.ProfessionExperience > 0) ? Strings.Workshop.TimeAndExperience.ToString(Math.Max(1, recette.Time / 1000), recette.ProfessionExperience) : Strings.Workshop.TimeOnly.ToString(Math.Max(1, recette.Time / 1000)));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), text5, num2, 1).FirstOrDefault(), num, _d.Y(num7 + 6f), A(TechniqueArt.Dim));
			num7 += 22f;
			List<string> list = Ateliers(_metier);
			if (list != null && list.Count > 0)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), $"{((list.Count > 1) ? Strings.Professions.Workshops : Strings.Professions.Workshop)} : {string.Join(", ", list)}", num2, 2).FirstOrDefault(), num, _d.Y(num7), A(TechniqueArt.Dim));
			}
			return;
		}
		NPCDescriptor gibier2 = entree.Gibier;
		if (gibier2 != null)
		{
			List<Drop> list2 = gibier2.Drops.Where((Drop drop) => drop.Hunting).ToList();
			num7 = Rubrique(renderer, num7, (list2.Count > 0) ? Strings.Professions.HuntingLoot : Strings.Professions.Drops, num);
			foreach (Drop item4 in ((list2.Count > 0) ? list2 : gibier2.Drops).Take(6))
			{
				if (DatabaseObject<ItemDescriptor>.TryGet(item4.ItemId, out var databaseObject))
				{
					num7 = LigneObjet(renderer, num7, databaseObject, Professions.Percent(item4.Chance) + " %", BodyLight);
				}
			}
			if (gibier2.HunterExperience > 0)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), Strings.Professions.HunterExperience.ToString(gibier2.HunterExperience), num, _d.Y(num7 + 6f), A(TechniqueArt.Dim));
				num7 += 22f;
			}
			{
				foreach (string item5 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11f), Strings.Professions.OtherMonsters, num2, 3))
				{
					TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11f), item5, num, _d.Y(num7), A(TechniqueArt.Dim));
					num7 += 14f;
				}
				return;
			}
		}
		ResourceDescriptor ressource = entree.Ressource;
		if (ressource == null)
		{
			return;
		}
		num7 = Rubrique(renderer, num7, Strings.Professions.Drops, num);
		foreach (Drop item6 in ressource.Drops.Take(6))
		{
			if (DatabaseObject<ItemDescriptor>.TryGet(item6.ItemId, out var databaseObject2))
			{
				num7 = LigneObjet(renderer, num7, databaseObject2, Professions.Percent(item6.Chance) + " %", BodyLight);
			}
		}
		if (ressource.ProfessionExperience > 0)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), Strings.Professions.ResourceExperience.ToString(ressource.ProfessionExperience), num, _d.Y(num7 + 6f), A(TechniqueArt.Dim));
		}
	}

	private float Rubrique(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float y, string titre, int gauche)
	{
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10.5f), titre.ToUpperInvariant(), gauche, _d.Y(y), _d.Em(0.14f, 10.5f), A(TechniqueArt.Dim));
		return y + 12f;
	}

	private float LigneObjet(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float y, ItemDescriptor objet, string droite, Color couleur)
	{
		Rectangle rect = _d.R(974f, y, 236f, 31f);
		TechniqueArt.Fill(renderer, rect, A(new Color(153, 6, 8, 14)));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
		int num = rect.Y + rect.Height / 2;
		int num2 = _d.L(24f);
		Icone(renderer, objet.Icon, new Rectangle(rect.X + _d.L(6f), num - num2 / 2, num2, num2));
		int num3 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(12f), droite, rect.Right - _d.L(8f), num, A(couleur));
		string text = TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(12.5f), Texte(objet.Name), rect.Width - _d.L(48f) - num3, 1).FirstOrDefault();
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(12.5f), text, rect.X + _d.L(38f), num, A(TechniqueArt.Text));
		return y + 34f;
	}
}
