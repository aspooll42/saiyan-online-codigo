using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Client.Framework.Items;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Dungeons;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Housing;
using Intersect.Client.Interface.Game.Inventaire;
using Intersect.Client.Interface.Game.Personnage;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Crafting;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.Market;
using Intersect.Localization;
using Intersect.Models;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game.Crafting;

internal sealed class FenetreAtelier : FenetreCharte
{
	private enum Filtre
	{
		Toutes,
		ANiveau,
		Faisables
	}

	private sealed record Ligne(CraftingRecipeDescriptor Recette, ItemDescriptor? Objet, string Categorie, int Niveau, bool Fermee, int Faisables, int Manquants, long Cout, long Prix, string Cherche)
	{
		public long Benefice => Prix - Cout;

		public bool Ouverte
		{
			get
			{
				if (!Fermee)
				{
					return Faisables > 0;
				}
				return false;
			}
		}
	}

	private sealed record Categorie(string Cle, string Nom, int Ordre, string? Glyphe, int Nombre);

	private const float Largeur = 1240f;

	private const float Hauteur = 748f;

	private const float BodyTop = 58f;

	private const float BodyHeight = 676f;

	private const float BodyBas = 734f;

	private const float RailLeft = 14f;

	private const float RailWidth = 232f;

	private const float CenterLeft = 258f;

	private const float CenterWidth = 630f;

	private const float FicheLeft = 900f;

	private const float FicheWidth = 326f;

	private const float ChampTop = 58f;

	private const float ChampHeight = 40f;

	private const float FiltresLargeur = 300f;

	private const float ChampLargeur = 320f;

	private const float TitreTop = 112f;

	private const float TitreHeight = 30f;

	private const float EnteteTop = 144f;

	private const float EnteteHeight = 24f;

	private const float ListeTop = 170f;

	private const float LigneWidth = 614f;

	private const float LigneHeight = 54f;

	private const float Pas = 4f;

	private const float PiedHeight = 30f;

	private const float PiedTop = 704f;

	private const float ColonneNom = 64f;

	private const float ColonneNiveau = 282f;

	private const float ColonneEtat = 352f;

	private const float MetierHauteur = 156f;

	private const float CaseSac = 42f;

	private const float EcartSac = 6f;

	private const float VitrineTop = 72f;

	private const float VitrineHauteur = 128f;

	private const float BoutonHeight = 54f;

	private const float BoutonTop = 666f;

	private const float QuantiteTop = 622f;

	private const float EcoTop = 535f;

	private const long Attente = 1500L;

	private static readonly Color Rouge = new Color(255, 255, 138, 150);

	private static readonly Color Muted = new Color(255, 125, 136, 159);

	private static readonly Color Eteint = new Color(255, 74, 85, 108);

	private static readonly Color KiClair = new Color(255, 255, 172, 82);

	private static readonly Color Case = new Color(255, 10, 14, 24);

	private static readonly string[] OrdreDuCorps = new string[6] { "Tête", "Haut", "Pantalon", "Gants", "Bottes", "Dos" };

	private readonly bool _journal;

	private readonly List<(int Metier, List<CraftingRecipeDescriptor> Recettes)> _groupes = new List<(int, List<CraftingRecipeDescriptor>)>();

	private int _groupe;

	private string _categorie = string.Empty;

	private Filtre _filtre;

	private string _requete = string.Empty;

	private Guid _recette;

	private int _quantite = 1;

	private int _quantiteMax = 1;

	private long _demandeA = -10000L;

	private Guid _recetteLancee;

	private int _quantiteLancee = 1;

	private int _defilement;

	private int _visibles = 1;

	private int _rangees;

	private Rectangle _liste;

	private Rectangle _rangeeQuantite;

	private bool _quantiteLibre;

	private string _vueAffichee = "-";

	private int _versionPrix;

	private readonly Dictionary<Guid, int> _possede = new Dictionary<Guid, int>();

	private int _signatureSac = int.MinValue;

	private string _signatureLignes = string.Empty;

	private List<Ligne> _lignes = new List<Ligne>();

	private List<Categorie> _categories = new List<Categorie>();

	private readonly TextBox _recherche;

	private readonly List<(Rectangle Rect, string Cle, Func<Bulle> Lire)> _zonesBulles = new List<(Rectangle, string, Func<Bulle>)>();

	private string? _bulleCle;

	private Bulle? _bulle;

	protected override Rectangle Place => new Rectangle(180, 76, 1240, 748);

	public bool IsCrafting => false;

	private (int Metier, List<CraftingRecipeDescriptor> Recettes) Groupe
	{
		get
		{
			if (_groupe < 0 || _groupe >= _groupes.Count)
			{
				return (Metier: -1, Recettes: new List<CraftingRecipeDescriptor>());
			}
			return _groupes[_groupe];
		}
	}

	public FenetreAtelier(Canvas gameCanvas, bool journal)
		: base(gameCanvas, "FenetreAtelier")
	{
		_journal = journal;
		_recherche = new TextBox(this, "AtelierRecherche")
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
			_requete = PersonnageScreen.Normaliser(_recherche.Text?.Trim());
		};
		Interface.FocusComponents.Add(_recherche);
		ConstruireLesGroupes();
		MarketPrices.Received += OnPrix;
		DemanderLesPrix();
		Show();
		PlacerRecherche();
	}

	public void Close()
	{
		Hide();
	}

	public void Detach()
	{
		MarketPrices.Received -= OnPrix;
		_recherche.Blur();
		Interface.FocusComponents.Remove(_recherche);
		DelayedDelete();
	}

	public override void Hide()
	{
		_recherche.Blur();
		base.Hide();
	}

	private void OnPrix(MarketPricesResultPacket _)
	{
		_versionPrix++;
	}

	protected override void DoPrelayout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.DoPrelayout(skin);
		if (!base.IsHidden)
		{
			Placer();
			PlacerRecherche();
		}
	}

	private void PlacerRecherche()
	{
		Rectangle rectangle = _d.R(296f, 63f, 248f, 30f);
		if (_recherche.IsHidden)
		{
			_recherche.IsHidden = false;
		}
		if (_recherche.FontSize != Pt(14f))
		{
			_recherche.FontSize = Pt(14f);
		}
		if (_recherche.X != rectangle.X || _recherche.Y != rectangle.Y || _recherche.Width != rectangle.Width || _recherche.Height != rectangle.Height)
		{
			_recherche.SetBounds(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
		}
	}

	private void ConstruireLesGroupes()
	{
		_groupes.Clear();
		DbList<CraftingRecipeDescriptor> dbList = Globals.ActiveCraftingTable?.Crafts;
		if (dbList == null)
		{
			return;
		}
		List<CraftingRecipeDescriptor> source = (from recette in dbList.Select(DatabaseObject<CraftingRecipeDescriptor>.Get)
			where recette != null
			select recette).Cast<CraftingRecipeDescriptor>().ToList();
		foreach (int metier in from num in source.Select(MetierDe).Distinct()
			orderby (num >= 0) ? num : 99
			select num)
		{
			_groupes.Add((metier, (from recette in source
				where MetierDe(recette) == metier
				orderby Math.Max(1, recette.ProfessionLevel)
				select recette).ThenBy<CraftingRecipeDescriptor, string>((CraftingRecipeDescriptor recette) => DatabaseObject<ItemDescriptor>.Get(recette.ItemId)?.Name ?? recette.Name, StringComparer.CurrentCultureIgnoreCase).ToList()));
		}
		_groupe = 0;
	}

	private static int MetierDe(CraftingRecipeDescriptor recette)
	{
		if (!MetiersArtisanat.EstArtisanat(recette.Profession))
		{
			return -1;
		}
		return recette.Profession;
	}

	private void DemanderLesPrix()
	{
		Guid[] array = _groupes.SelectMany<(int, List<CraftingRecipeDescriptor>), CraftingRecipeDescriptor>(((int Metier, List<CraftingRecipeDescriptor> Recettes) groupe) => groupe.Recettes).SelectMany((CraftingRecipeDescriptor recette) => recette.Ingredients.Select((CraftingRecipeIngredient ingredient) => ingredient.ItemId).Append(recette.ItemId)).Distinct()
			.ToArray();
		if (array.Length != 0)
		{
			MarketPrices.Request(array);
		}
	}

	private static int SignatureDuSac(Player me)
	{
		HashCode hashCode = default(HashCode);
		IItem[] inventory = me.Inventory;
		foreach (IItem item in inventory)
		{
			hashCode.Add(item?.ItemId ?? Guid.Empty);
			hashCode.Add(item?.Quantity ?? 0);
		}
		return hashCode.ToHashCode();
	}

	private int Possede(Player me, Guid objet)
	{
		if (!_possede.TryGetValue(objet, out var value))
		{
			value = me.GetQuantityOfItemInInventory(objet);
			_possede[objet] = value;
		}
		return value;
	}

	private static long PrixDe(Guid objet)
	{
		if (MarketPrices.TryGet(objet, out MarketPriceInfo info) && info.Median > 0)
		{
			return info.Median;
		}
		return DatabaseObject<ItemDescriptor>.Get(objet)?.Price ?? 0;
	}

	private void Actualiser(Player me)
	{
		int num = SignatureDuSac(me);
		if (num != _signatureSac)
		{
			_signatureSac = num;
			_possede.Clear();
		}
		string text = $"{_groupe}:{me.ProfessionsVersion}:{_signatureSac}:{_versionPrix}";
		if (!(text == _signatureLignes))
		{
			_signatureLignes = text;
			_lignes = Groupe.Recettes.Select((CraftingRecipeDescriptor recette) => Calculer(me, recette)).ToList();
			_categories = (from categorie in (from ligne in _lignes
					group ligne by ligne.Categorie).Select(delegate(IGrouping<string, Ligne> groupe)
				{
					(string Cle, string Nom, int Ordre, string? Glyphe) tuple = CategorieDe(groupe.First().Objet);
					string item = tuple.Nom;
					int item2 = tuple.Ordre;
					string item3 = tuple.Glyphe;
					return new Categorie(groupe.Key, item, item2, item3, groupe.Count());
				})
				orderby categorie.Ordre
				select categorie).ThenBy<Categorie, string>((Categorie categorie) => categorie.Nom, StringComparer.CurrentCultureIgnoreCase).ToList();
			if (_categorie.Length > 0 && _categories.All((Categorie categorie) => categorie.Cle != _categorie))
			{
				_categorie = string.Empty;
			}
		}
	}

	private Ligne Calculer(Player me, CraftingRecipeDescriptor recette)
	{
		ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(recette.ItemId);
		int num = Math.Max(1, recette.ProfessionLevel);
		bool fermee = MetiersArtisanat.EstArtisanat(recette.Profession) && num > me.ProfessionLevel(recette.Profession);
		int faisables = ((recette.Ingredients.Count == 0) ? 999 : recette.Ingredients.Min((CraftingRecipeIngredient ingredient) => Possede(me, ingredient.ItemId) / Math.Max(1, ingredient.Quantity)));
		int manquants = recette.Ingredients.Count((CraftingRecipeIngredient ingredient) => Possede(me, ingredient.ItemId) < ingredient.Quantity);
		long cout = recette.Ingredients.Sum((CraftingRecipeIngredient ingredient) => PrixDe(ingredient.ItemId) * ingredient.Quantity);
		long prix = PrixDe(recette.ItemId) * Math.Max(1, recette.Quantity);
		(string Cle, string Nom, int Ordre, string? Glyphe) tuple = CategorieDe(itemDescriptor);
		string item = tuple.Cle;
		string item2 = tuple.Nom;
		string cherche = PersonnageScreen.Normaliser((itemDescriptor?.Name ?? recette.Name) + " " + item2);
		return new Ligne(recette, itemDescriptor, item, num, fermee, faisables, manquants, cout, prix, cherche);
	}

	private static (string Cle, string Nom, int Ordre, string? Glyphe) CategorieDe(ItemDescriptor? objet)
	{
		switch (objet?.ItemType)
		{
		case ItemType.Equipment:
		{
			List<string> slots = Options.Instance.Equipment.Slots;
			if (objet.EquipmentSlot >= 0 && objet.EquipmentSlot < slots.Count)
			{
				string text = slots[objet.EquipmentSlot];
				int num = Array.IndexOf(OrdreDuCorps, text);
				return (Cle: $"emplacement:{objet.EquipmentSlot}", Nom: Traduction.Contenu(text), Ordre: (num >= 0) ? num : (10 + objet.EquipmentSlot), Glyphe: null);
			}
			return (Cle: "equipement", Nom: Strings.Personnage.CategoryEquipment, Ordre: 30, Glyphe: "inv_glyphe_equipement.png");
		}
		case ItemType.Consumable:
			return (Cle: "consommables", Nom: Strings.Market.CategoryConsumables, Ordre: 40, Glyphe: "inv_glyphe_consommable.png");
		case ItemType.Bag:
			return (Cle: "capsules", Nom: Strings.Atelier.Capsules, Ordre: 50, Glyphe: "inv_glyphe_sac.png");
		case ItemType.None:
			return (Cle: "materiaux", Nom: Strings.Atelier.Materiaux, Ordre: 60, Glyphe: "inv_glyphe_ressource.png");
		default:
			return (Cle: "autres", Nom: Strings.Atelier.Autres, Ordre: 70, Glyphe: "inv_glyphe_tout.png");
		}
	}

	private List<Ligne> Vue(bool filtree)
	{
		IEnumerable<Ligne> enumerable = _lignes;
		if (_requete.Length > 0)
		{
			enumerable = enumerable.Where((Ligne ligne) => ligne.Cherche.Contains(_requete, StringComparison.Ordinal));
		}
		else if (_categorie.Length > 0)
		{
			enumerable = enumerable.Where((Ligne ligne) => ligne.Categorie == _categorie);
		}
		if (filtree)
		{
			enumerable = _filtre switch
			{
				Filtre.ANiveau => enumerable.Where((Ligne ligne) => !ligne.Fermee), 
				Filtre.Faisables => enumerable.Where((Ligne ligne) => ligne.Ouverte), 
				_ => enumerable, 
			};
		}
		return enumerable.ToList();
	}

	private void SuivreLaVue(List<Ligne> vue)
	{
		string text = $"{_groupe}|{((_requete.Length > 0) ? ("?" + _requete) : _categorie)}|{_filtre}";
		if (text == _vueAffichee)
		{
			return;
		}
		_vueAffichee = text;
		int num = vue.FindIndex((Ligne ligne) => ligne.Recette.Id == _recette);
		if (num < 0)
		{
			if (vue.Count > 0)
			{
				Choisir(vue[0].Recette.Id);
			}
			_defilement = 0;
		}
		else
		{
			_defilement = ((num >= _visibles) ? (num - _visibles / 2) : 0);
		}
	}

	private void Choisir(Guid recette)
	{
		if (!(_recette == recette))
		{
			_recette = recette;
			_quantite = 1;
		}
	}

	private void ChoisirCategorie(string categorie)
	{
		if (_requete.Length > 0)
		{
			_recherche.Text = string.Empty;
			_requete = string.Empty;
		}
		_categorie = categorie;
	}

	private void Effacer()
	{
		_recherche.Text = string.Empty;
		_requete = string.Empty;
		_recherche.Focus();
	}

	private void MetierSuivant()
	{
		if (_groupes.Count >= 2)
		{
			_groupe = (_groupe + 1) % _groupes.Count;
			_categorie = string.Empty;
		}
	}

	private void ChangerLaQuantite(int sens)
	{
		GameInput inputManager = Globals.InputManager;
		int num = ((inputManager == null || !inputManager.IsKeyDown(Keys.Shift)) ? 1 : 10);
		_quantite = Math.Clamp(_quantite + sens * num, 1, _quantiteMax);
	}

	private void Fabriquer(Guid recette)
	{
		PacketSender.SendCraftItem(recette, _quantite);
		_demandeA = Timing.Global.Milliseconds;
		_recetteLancee = recette;
		_quantiteLancee = _quantite;
	}

	private void Arreter(Player me)
	{
		if (Artisanat.Progression(me.Id, out var _, out var _, out var _) && Timing.Global.Milliseconds - _demandeA >= 600)
		{
			PacketSender.SendCraftItem(default(Guid), 0);
			_demandeA = -10000L;
		}
	}

	protected override bool OnMouseWheeled(int delta)
	{
		Point point = LocalMouse();
		if (!base.IsHidden && _liste.Contains(point.X, point.Y))
		{
			_defilement = Math.Clamp(_defilement + ((delta <= 0) ? 1 : (-1)), 0, Math.Max(0, _rangees - _visibles));
			return true;
		}
		if (!base.IsHidden && _quantiteLibre && _rangeeQuantite.Contains(point.X, point.Y))
		{
			ChangerLaQuantite((delta > 0) ? 1 : (-1));
			return true;
		}
		return base.OnMouseWheeled(delta);
	}

	protected override void Dessiner(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		DessinerCadre(renderer);
		int croix = DessinerBarre(renderer, Strings.Workshop.Title);
		_zonesBulles.Clear();
		_liste = default(Rectangle);
		_rangeeQuantite = default(Rectangle);
		_quantiteLibre = false;
		Player me = Globals.Me;
		if (me != null)
		{
			Actualiser(me);
			List<Ligne> tout = Vue(filtree: false);
			List<Ligne> vue = Vue(filtree: true);
			_visibles = Math.Max(1, 9);
			SuivreLaVue(vue);
			float avancee;
			int index;
			int nombre;
			bool flag = Artisanat.Progression(me.Id, out avancee, out index, out nombre);
			bool flag2 = !flag && Timing.Global.Milliseconds - _demandeA < 1500;
			DessinerEntete(renderer, me, croix, now);
			DessinerGauche(renderer, me, tout);
			DessinerCentre(renderer, me, tout, vue, flag || flag2);
			DessinerFiche(renderer, me, flag, flag2, avancee, index, nombre);
		}
	}

	protected override void DessinerDessus(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		DessinerEtiquette(renderer, now);
		DessinerLaBulle(renderer);
	}

	private void Panneau(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect)
	{
		TechniqueArt.Patch(renderer, "tech_panneau.png", rect, A(TechniqueArt.White), 12);
	}

	private void BulleSur(Rectangle rect, string cle, Func<Bulle> lire)
	{
		_zonesBulles.Add((rect, cle, lire));
	}

	private void DessinerLaBulle(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		if (!base.Survolee)
		{
			return;
		}
		Point point = LocalMouse();
		for (int num = _zonesBulles.Count - 1; num >= 0; num--)
		{
			var (cible, text, func) = _zonesBulles[num];
			if (cible.Contains(point.X, point.Y))
			{
				string text2 = text + "#" + _signatureLignes;
				if (_bulle == null || _bulleCle != text2)
				{
					_bulleCle = text2;
					_bulle = func();
				}
				DessinerBulle(renderer, _bulle, cible);
				break;
			}
		}
	}

	private void DessinerEntete(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, int croix, long now)
	{
		int num = _d.Y(23f);
		int num2 = croix - _d.L(18f);
		num2 -= TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(16f), MarketWindow.FormatAmount(me.Zeni), num2, num, A(TechniqueArt.Gold)) + _d.L(6f);
		int num3 = _d.L(20f);
		HouseArt.Zeni(renderer, new Rectangle(num2 - num3, num - num3 / 2, num3, num3), ImagePiece(now), _fade);
		num2 -= num3 + _d.L(10f);
		num2 -= Etiquette(renderer, MonnaieNom(), num2, num) + _d.L(26f);
		if (!_journal)
		{
			num2 -= TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(11.5f), $"• {Strings.Workshop.InReach}", num2, num + _d.L(1f), A(TechniqueArt.Green)) + _d.L(8f);
		}
		string text = Globals.ActiveCraftingTable?.Name ?? string.Empty;
		int num4 = num2 - _d.X(260f);
		text = TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(16f), text, Math.Max(_d.L(80f), num4 - _d.L(90f)), 1).FirstOrDefault() ?? string.Empty;
		num2 -= TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(16f), text, num2, num, A(TechniqueArt.Text)) + _d.L(10f);
		Etiquette(renderer, Strings.Workshop.WorkshopCaps, num2, num);
	}

	private int Etiquette(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string texte, int droite, int centre)
	{
		string text = texte.ToUpperInvariant();
		int num = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), text, _d.Em(0.16f, 10f));
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), text, droite - num, centre + _d.L(1f), _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
		return num;
	}

	private static int ImagePiece(long now)
	{
		long num = now % 1870;
		if (num >= 1600)
		{
			return Math.Min(3, 1 + (int)((num - 1600) / 90));
		}
		return 0;
	}

	private static string MonnaieNom()
	{
		if (MarketWindow.KnownCurrencyId != Guid.Empty)
		{
			ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(MarketWindow.KnownCurrencyId);
			if (itemDescriptor != null)
			{
				return itemDescriptor.Name;
			}
		}
		return "Zeni";
	}

	private void DessinerGauche(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, List<Ligne> tout)
	{
		Panneau(renderer, _d.R(14f, 58f, 232f, 676f));
		DessinerMetier(renderer, me);
		float bas = DessinerSac(renderer, me, tout);
		DessinerCategories(renderer, 214f, bas);
	}

	private void DessinerMetier(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me)
	{
		(int Metier, List<CraftingRecipeDescriptor> Recettes) groupe = Groupe;
		int item = groupe.Metier;
		List<CraftingRecipeDescriptor> item2 = groupe.Recettes;
		bool flag = MetiersArtisanat.EstArtisanat(item);
		Rectangle rect = _d.R(15f, 59f, 230f, 155f);
		TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Ki, 0.14f), A(TechniqueArt.Ki, 0.06f), A(TechniqueArt.Ki, 0f));
		TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Ki));
		TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Bottom, rect.Width, 1), A(TechniqueArt.Line));
		Rectangle rect2 = _d.R(30f, 72f, 48f, 48f);
		TechniqueArt.Fill(renderer, rect2, A(Case));
		TechniqueArt.Outline(renderer, rect2, A(TechniqueArt.Line2));
		string text = (flag ? Professions.Icon(item) : DatabaseObject<ItemDescriptor>.Get(item2.FirstOrDefault()?.ItemId ?? Guid.Empty)?.Icon);
		if (!string.IsNullOrEmpty(text))
		{
			IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Item, text);
			if (gameTexture != null)
			{
				int num = TailleIcone(gameTexture.Width);
				SaiyanHud.Draw(renderer, gameTexture, rect2.X + (rect2.Width - num) / 2, rect2.Y + (rect2.Height - num) / 2, num, num, A(TechniqueArt.White));
			}
		}
		int num2 = _d.X(90f);
		int num3 = _d.X(230f) - num2;
		string text2 = (flag ? Professions.Name(item) : (Globals.ActiveCraftingTable?.Name ?? string.Empty)).ToUpperInvariant();
		int size = Ajuster(renderer, TechniqueArt.Heavy, text2, num3, 20f, 17f, 15f);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, size, TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, size, text2, num3, 1).FirstOrDefault(), num2, _d.Y(84f), A(TechniqueArt.Text));
		string text3 = (flag ? Professions.Subtitle(item) : Strings.Workshop.RecipeCount.ToString(item2.Count));
		float num4 = 102f;
		foreach (string item3 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), text3, num3, 2))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), item3, num2, _d.Y(num4), A(TechniqueArt.Dim));
			num4 += 14f;
		}
		if (_groupes.Count > 1)
		{
			TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(10.5f), $"{_groupe + 1} / {_groupes.Count}", _d.X(236f), _d.Y(70f), A(TechniqueArt.Dim));
			AddHit(rect, "metier", MetierSuivant);
			if (IsHot("metier"))
			{
				Etiqueter("metier", rect, Strings.Atelier.MetierSuivant.ToString(_groupe + 1, _groupes.Count));
			}
		}
		if (!flag)
		{
			return;
		}
		int num5 = me.ProfessionLevel(item);
		int num6 = _d.Y(145f);
		int num7 = _d.X(30f);
		num7 += TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), Strings.Professions.LevelCaps.ToString().ToUpperInvariant(), num7, num6 + _d.L(5f), _d.Em(0.16f, 10f), A(TechniqueArt.Dim)) + _d.L(8f);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(34f), $"{num5}", num7, num6, A(TechniqueArt.Gold));
		long num8 = me.ProfessionExperience[item];
		long num9 = me.ProfessionNextLevel[item];
		int num10 = _d.X(230f);
		if (num9 > 0)
		{
			string text4 = " / " + MarketWindow.FormatAmount(num9);
			int num11 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(15f), text4, num10, num6 + _d.L(5f), A(Muted));
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(15f), MarketWindow.FormatAmount(num8), num10 - num11, num6 + _d.L(5f), A(TechniqueArt.Text));
		}
		Rectangle rect3 = _d.R(30f, 170f, 200f, 6f);
		TechniqueArt.Fill(renderer, rect3, A(new Color(255, 7, 10, 18)));
		TechniqueArt.Outline(renderer, rect3, A(TechniqueArt.Line));
		double num12 = ((num9 <= 0) ? 1.0 : Math.Clamp((double)num8 / (double)num9, 0.0, 1.0));
		int num13 = (int)Math.Round((double)(rect3.Width - 2) * num12);
		if (num13 > 0)
		{
			TechniqueArt.FillHorizontal(renderer, new Rectangle(rect3.X + 1, rect3.Y + 1, num13, rect3.Height - 2), A(new Color(255, 184, 97, 15)), A(TechniqueArt.Ki), A(TechniqueArt.Gold));
		}
		string text5 = ((num9 > 0) ? Strings.Professions.ExperienceLeft.ToString(MarketWindow.FormatAmount(num9 - num8), num5 + 1) : Strings.Professions.MaximumLevel.ToString());
		List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), text5, _d.L(200f), 2);
		int num14 = rect3.Bottom + _d.L((list.Count > 1) ? 9 : 14);
		foreach (string item4 in list)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), item4, _d.X(30f), num14, A(TechniqueArt.Dim));
			num14 += _d.L(14f);
		}
	}

	private void DessinerCategories(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float haut, float bas)
	{
		string text = (_categories.Any((Categorie categorie) => categorie.Cle.StartsWith("emplacement:", StringComparison.Ordinal)) ? Strings.Atelier.Emplacements : Strings.Atelier.Categories).ToString().ToUpperInvariant();
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), text, _d.X(32f), _d.Y(haut + 19f), _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
		List<(string, string, string, int)> list = new List<(string, string, string, int)> { (string.Empty, Strings.Market.CategoryAll, "inv_glyphe_tout.png", _lignes.Count) };
		list.AddRange(_categories.Select((Categorie categorie) => (Cle: categorie.Cle, Nom: categorie.Nom, Glyphe: categorie.Glyphe, Nombre: categorie.Nombre)));
		float num = haut + 32f;
		float num2 = Math.Min(34f, (bas - 10f - num) / (float)Math.Max(1, list.Count));
		float num3 = num;
		foreach (var item5 in list)
		{
			string item = item5.Item1;
			string item2 = item5.Item2;
			string item3 = item5.Item3;
			int item4 = item5.Item4;
			Rectangle rect = _d.R(24f, num3, 212f, num2 - 2f);
			string cle = "categorie:" + item;
			bool flag = ((_requete.Length > 0) ? (item.Length == 0) : (_categorie == item));
			bool flag2 = IsHot(cle);
			if (flag)
			{
				TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, 0.05f));
				TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line2));
			}
			else if (flag2)
			{
				TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, 0.03f));
			}
			int num4 = _d.L(16f);
			Rectangle rectangle = new Rectangle(rect.X + _d.L(10f), rect.Y + (rect.Height - num4) / 2, num4, num4);
			Color couleur = A(flag ? TechniqueArt.Ki : (flag2 ? TechniqueArt.Soft : TechniqueArt.Dim));
			if (item3 == null)
			{
				GlypheHexagone(renderer, rectangle, couleur, Math.Max(1, _d.L(1.8f)));
			}
			else
			{
				Lisser(item3, rectangle, couleur);
			}
			int centerY = rect.Y + rect.Height / 2;
			int num5 = TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(13f), $"{item4}", rect.Right - _d.L(10f), centerY, A(Muted));
			int width = rect.Width - _d.L(36f) - _d.L(10f) - num5 - _d.L(8f);
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(15f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(15f), item2, width, 1).FirstOrDefault(), rect.X + _d.L(36f), centerY, A(flag ? TechniqueArt.Text : TechniqueArt.Soft));
			string choix = item;
			AddHit(rect, cle, delegate
			{
				ChoisirCategorie(choix);
			});
			num3 += num2;
		}
	}

	private static void GlypheHexagone(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle carre, Color couleur, int trait)
	{
		float num = (float)carre.Height * 0.86f;
		float num2 = num * 0.86f;
		float num3 = (float)carre.X + (float)carre.Width / 2f;
		float x = (float)carre.Y + ((float)carre.Height - num) / 2f;
		float num4 = (float)trait * 1.15f;
		float num5 = num - 2f * num4;
		float largeur = num2 - (float)(2 * trait);
		int num6 = (int)MathF.Ceiling(num);
		for (int i = 0; i < num6; i++)
		{
			float num7 = (float)i + 0.5f;
			float num8 = DemiLargeur(num7, num2, num);
			if (!(num8 <= 0.15f))
			{
				int num9 = (int)MathF.Round(num3 - num8);
				int num10 = (int)MathF.Round(num3 + num8);
				int y = (int)MathF.Round(x) + i;
				float num11 = num7 - num4;
				float num12 = ((num11 > 0f && num11 < num5) ? DemiLargeur(num11, largeur, num5) : 0f);
				if (num12 <= 0.5f)
				{
					TechniqueArt.Fill(renderer, new Rectangle(num9, y, Math.Max(1, num10 - num9), 1), couleur);
					continue;
				}
				int num13 = (int)MathF.Round(num3 - num12);
				int num14 = (int)MathF.Round(num3 + num12);
				TechniqueArt.Fill(renderer, new Rectangle(num9, y, Math.Max(1, num13 - num9), 1), couleur);
				TechniqueArt.Fill(renderer, new Rectangle(num14, y, Math.Max(1, num10 - num14), 1), couleur);
			}
		}
	}

	private static float DemiLargeur(float y, float largeur, float hauteur)
	{
		float num = hauteur / 4f;
		if (y < num)
		{
			return largeur / 2f * (y / num);
		}
		if (!(y > hauteur - num))
		{
			return largeur / 2f;
		}
		return largeur / 2f * ((hauteur - y) / num);
	}

	private float DessinerSac(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, List<Ligne> tout)
	{
		List<Guid> list = tout.SelectMany((Ligne ligne) => ligne.Recette.Ingredients.Select((CraftingRecipeIngredient ingredient) => ingredient.ItemId)).Distinct().Take(12)
			.ToList();
		if (list.Count == 0)
		{
			return 734f;
		}
		int num = (list.Count + 3) / 4;
		float num2 = 720f - ((float)num * 42f + (float)(num - 1) * 6f);
		float num3 = num2 - 15f;
		float num4 = num3 - 19f;
		TechniqueArt.Fill(renderer, _d.R(15f, num4, 230f, 1f), A(TechniqueArt.Line));
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), Strings.Workshop.InYourBag.ToString().ToUpperInvariant(), _d.X(32f), _d.Y(num3), _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
		for (int num5 = 0; num5 < list.Count; num5++)
		{
			ItemDescriptor objet = DatabaseObject<ItemDescriptor>.Get(list[num5]);
			if (objet != null)
			{
				Rectangle rect = _d.R(28f + (float)(num5 % 4) * 48f, num2 + (float)(num5 / 4) * 48f, 42f, 42f);
				int num6 = Possede(me, objet.Id);
				CaseObjet(renderer, rect, objet, num6, eteinte: false, num6 <= 0, toujoursLeCompte: true);
				BulleSur(rect, $"materiau:{objet.Id}", () => BulleMateriau(me, objet));
			}
		}
		return num4;
	}

	private void DessinerCentre(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, List<Ligne> tout, List<Ligne> vue, bool fabrication)
	{
		DessinerRecherche(renderer);
		DessinerFiltres(renderer, _d.R(588f, 58f, 300f, 40f));
		string titre;
		string complement;
		if (_requete.Length > 0)
		{
			string text = _recherche.Text?.Trim() ?? string.Empty;
			titre = Strings.Atelier.Recherche;
			complement = ((vue.Count == 1) ? Strings.Atelier.ResultatsUn.ToString(text) : Strings.Atelier.Resultats.ToString(vue.Count, text));
		}
		else
		{
			titre = ((_categorie.Length == 0) ? Strings.Market.CategoryAll.ToString() : (_categories.FirstOrDefault((Categorie categorie) => categorie.Cle == _categorie)?.Nom ?? string.Empty));
			int num = tout.Count((Ligne ligne) => !ligne.Fermee);
			complement = ((tout.Count == 1) ? Strings.Atelier.SousTitreUne.ToString(num) : Strings.Atelier.SousTitre.ToString(tout.Count, num));
		}
		Titre(renderer, titre, complement);
		Colonnes(renderer);
		_rangees = vue.Count;
		_defilement = Math.Clamp(_defilement, 0, Math.Max(0, _rangees - _visibles));
		_liste = _d.R(258f, 170f, 630f, (float)_visibles * 58f - 4f);
		if (vue.Count == 0)
		{
			string text2 = ((_requete.Length > 0) ? Strings.Atelier.AucunResultat.ToString(_recherche.Text?.Trim() ?? string.Empty) : ((_lignes.Count == 0) ? Strings.Workshop.NoRecipes.ToString() : ((_filtre == Filtre.Faisables) ? Strings.Workshop.NothingCraftable.ToString() : Strings.Atelier.AucuneANiveau.ToString())));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), text2, _d.X(270f), _d.Y(190f), A(TechniqueArt.Dim));
		}
		for (int num2 = 0; num2 < _visibles && _defilement + num2 < vue.Count; num2++)
		{
			DessinerLigne(renderer, me, vue[_defilement + num2], 170f + (float)num2 * 58f, fabrication);
		}
		if (_rangees > _visibles)
		{
			Rectangle rect = new Rectangle(_d.X(882f), _liste.Y, _d.L(4f), _liste.Height);
			TechniqueArt.Fill(renderer, rect, A(new Color(255, 10, 14, 24)));
			int num3 = Math.Max(_d.L(24f), rect.Height * _visibles / _rangees);
			int y = rect.Y + (rect.Height - num3) * _defilement / Math.Max(1, _rangees - _visibles);
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, y, rect.Width, num3), A(TechniqueArt.Line2));
		}
		int centerY = _d.Y(719f);
		int num4 = tout.Count((Ligne ligne) => ligne.Ouverte);
		int num5 = _d.X(258f);
		num5 += TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(13f), $"{num4} ", num5, centerY, A(TechniqueArt.Soft));
		string text3 = $"{((num4 == 1) ? Strings.Atelier.PiedFaisable : Strings.Atelier.PiedFaisables)} · {Strings.Atelier.PiedPrix}";
		int num6 = _d.X(888f);
		if (_rangees > _visibles)
		{
			num6 -= TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(13f), Strings.Atelier.Molette, num6, centerY, A(Muted)) + _d.L(16f);
		}
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(13f), text3, Math.Max(_d.L(40f), num6 - num5), 1).FirstOrDefault(), num5, centerY, A(Muted));
	}

	private void DessinerRecherche(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		Rectangle rect = _d.R(258f, 58f, 320f, 40f);
		bool flag = !string.IsNullOrEmpty(_recherche.Text);
		TechniqueArt.Fill(renderer, rect, A(new Color(255, 10, 14, 24)));
		TechniqueArt.Outline(renderer, rect, A((flag || InputHandler.KeyboardFocus == _recherche) ? TechniqueArt.Ki : TechniqueArt.Line2));
		int num = _d.L(15f);
		Lisser("inv_loupe.png", new Rectangle(rect.X + _d.L(14f), rect.Y + (rect.Height - num) / 2, num, num), A(TechniqueArt.Dim));
		if (!flag)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(15f), Strings.Atelier.RechercheIndice, rect.X + _d.L(40f), rect.Y + rect.Height / 2, A(Muted));
			AddHit(rect, "recherche", delegate
			{
				_recherche.Focus();
			}, string.Empty);
			return;
		}
		Rectangle rect2 = new Rectangle(rect.Right - _d.L(32f), rect.Y + (rect.Height - _d.L(26f)) / 2, _d.L(26f), _d.L(26f));
		bool flag2 = IsHot("effacer");
		int num2 = _d.L(11f);
		Lisser("tech_croix.png", new Rectangle(rect2.X + (rect2.Width - num2) / 2, rect2.Y + (rect2.Height - num2) / 2, num2, num2), A(flag2 ? TechniqueArt.White : TechniqueArt.Dim));
		AddHit(rect, "recherche", delegate
		{
			_recherche.Focus();
		}, string.Empty);
		AddHit(rect2, "effacer", Effacer);
		if (flag2)
		{
			Etiqueter("effacer", rect2, Strings.Inventaire.CloseSearch);
		}
	}

	private void DessinerFiltres(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle zone)
	{
		(Filtre, string)[] array = new(Filtre, string)[3]
		{
			(Filtre.Toutes, Strings.Workshop.All.ToString().ToUpperInvariant()),
			(Filtre.ANiveau, Strings.Atelier.FiltreANiveau.ToString().ToUpperInvariant()),
			(Filtre.Faisables, Strings.Workshop.Craftable.ToString().ToUpperInvariant())
		};
		float espacement = _d.Em(0.1f, 12f);
		int taille = Pt(12f);
		int[] array2 = array.Select(((Filtre Filtre, string Nom) filtre) => TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, taille, filtre.Nom, espacement) + _d.L(24f)).ToArray();
		while (array2.Sum() > zone.Width && taille > 9)
		{
			taille--;
			array2 = array.Select(((Filtre Filtre, string Nom) filtre) => TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, taille, filtre.Nom, espacement) + _d.L(16f)).ToArray();
		}
		int num = zone.Width - array2.Sum();
		int num2 = zone.X;
		for (int num3 = 0; num3 < array.Length; num3++)
		{
			int num4 = ((num3 == array.Length - 1) ? (zone.Right - num2) : (array2[num3] + num / array.Length));
			Rectangle rect = new Rectangle(num2, zone.Y, num4, zone.Height);
			(Filtre, string) tuple = array[num3];
			Filtre item = tuple.Item1;
			string item2 = tuple.Item2;
			string cle = $"filtre:{item}";
			bool flag = _filtre == item;
			bool flag2 = IsHot(cle);
			if (flag)
			{
				TechniqueArt.FillVertical(renderer, rect, A(new Color(255, 255, 194, 122)), A(new Color(255, 224, 117, 26)));
			}
			else
			{
				TechniqueArt.Fill(renderer, rect, A(flag2 ? new Color(255, 16, 22, 36) : new Color(255, 10, 14, 24)));
			}
			if (num3 > 0)
			{
				TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, 1, rect.Height), A(TechniqueArt.Line2));
			}
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, taille, item2, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, espacement, A(flag ? new Color(255, 23, 11, 2) : (flag2 ? TechniqueArt.Text : TechniqueArt.Dim)));
			Filtre choix = item;
			AddHit(rect, cle, delegate
			{
				_filtre = choix;
			});
			num2 += num4;
		}
		TechniqueArt.Outline(renderer, zone, A(TechniqueArt.Line2));
	}

	private void Titre(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string titre, string complement)
	{
		int num = _d.Y(127f);
		int num2 = _d.X(258f);
		DungeonHud.Diamond(renderer, num2 + _d.L(2f), num, _d.L(10f), A(TechniqueArt.Green));
		int num3 = TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(26f), titre.ToUpperInvariant(), num2 + _d.L(20f), num, A(TechniqueArt.Text));
		int num4 = num2 + _d.L(20f) + num3 + _d.L(12f);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(16f), TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(16f), complement, _d.X(888f) - num4, 1).FirstOrDefault(), num4, num + _d.L(2f), A(TechniqueArt.Dim));
	}

	private void Colonnes(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		int centre = _d.Y(156f);
		Colonne(Strings.Atelier.ColonneRecette, 64f);
		Colonne(Strings.Atelier.ColonneNiveau, 282f);
		Colonne(Strings.Atelier.ColonneEtat, 352f);
		TechniqueArt.SpacedRight(renderer, TechniqueArt.Label, Pt(10f), Strings.Workshop.Profit.ToString().ToUpperInvariant(), _d.X(864f), centre, _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
		void Colonne(string texte, float colonne)
		{
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), texte.ToUpperInvariant(), _d.X(258f + colonne), centre, _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
		}
	}

	private void DessinerLigne(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, Ligne ligne, float y, bool fabrication)
	{
		CraftingRecipeDescriptor recette = ligne.Recette;
		Rectangle rect = _d.R(258f, y, 614f, 54f);
		string cle = $"recette:{recette.Id}";
		bool flag = recette.Id == _recette;
		bool flag2 = IsHot(cle);
		if (flag)
		{
			TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Ki, 0.16f), A(TechniqueArt.Ki, 0.06f), A(TechniqueArt.Ki, 0.02f));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Ki));
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Ki));
		}
		else
		{
			TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, ligne.Fermee ? 0.015f : (flag2 ? 0.055f : 0.03f)));
			TechniqueArt.Outline(renderer, rect, A(flag2 ? TechniqueArt.Line2 : TechniqueArt.Line));
		}
		ItemDescriptor objet = ligne.Objet;
		int num = rect.Y + rect.Height / 2;
		Rectangle rect2 = _d.R(266f, y + 6f, 42f, 42f);
		CaseObjet(renderer, rect2, objet, Math.Max(1, recette.Quantity), ligne.Fermee);
		if (objet != null)
		{
			BulleSur(rect2, $"produit:{recette.Id}", () => BulleProduit(me, objet));
		}
		int x = _d.X(322f);
		int width = _d.L(206f);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(19f), TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(19f), objet?.Name ?? recette.Name, width, 1).FirstOrDefault(), x, num - _d.L(9f), A(ligne.Fermee ? Muted : TechniqueArt.Text));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), (objet == null) ? string.Empty : SorteComplete(objet), width, 1).FirstOrDefault(), x, num + _d.L(11f), A(TechniqueArt.Dim));
		int x2 = _d.X(540f);
		if (MetiersArtisanat.EstArtisanat(recette.Profession))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(19f), $"{ligne.Niveau}", x2, num, A(ligne.Fermee ? Rouge : TechniqueArt.Soft));
		}
		else
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), "—", x2, num, A(Muted));
		}
		Color color;
		string text2;
		if (fabrication && recette.Id == ((_recetteLancee != Guid.Empty) ? _recetteLancee : _recette))
		{
			string text = Strings.Atelier.EnCours;
			color = KiClair;
			text2 = text;
		}
		else if (ligne.Fermee)
		{
			string text3 = Strings.Workshop.StateLevel.ToString(ligne.Niveau);
			color = Muted;
			text2 = text3;
		}
		else if (ligne.Faisables > 0)
		{
			string text4 = Strings.Workshop.StateCraftable.ToString(Math.Min(999, ligne.Faisables));
			color = TechniqueArt.Green;
			text2 = text4;
		}
		else
		{
			string obj = ((ligne.Manquants == 1) ? Strings.Workshop.StateMissingOne.ToString() : Strings.Workshop.StateMissing.ToString(ligne.Manquants));
			color = Rouge;
			text2 = obj;
		}
		int width2 = _d.L(150f);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(13f), TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(13f), text2, width2, 1).FirstOrDefault(), _d.X(610f), num, A(color));
		int num2 = rect.Right - _d.L(8f);
		if (ligne.Prix <= 0)
		{
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(19f), "—", num2, num, A(Muted));
		}
		else
		{
			int num3 = _d.L(15f);
			HouseArt.Zeni(renderer, new Rectangle(num2 - num3, num - num3 / 2, num3, num3), 0, _fade);
			long benefice = ligne.Benefice;
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(19f), Signe(benefice), num2 - num3 - _d.L(6f), num, A((benefice > 0) ? TechniqueArt.Green : ((benefice < 0) ? Rouge : Muted)));
		}
		Guid id = recette.Id;
		AddHit(rect, cle, delegate
		{
			Choisir(id);
		});
	}

	private void DessinerFiche(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, bool enCours, bool attente, float avancee, int index, int nombre)
	{
		Panneau(renderer, _d.R(900f, 58f, 326f, 676f));
		Ligne ligne = _lignes.FirstOrDefault((Ligne entree) => entree.Recette.Id == _recette);
		ItemDescriptor objet = ligne?.Objet;
		if (objet == null)
		{
			string text = ((_lignes.Count == 0) ? Strings.Workshop.NoRecipes.ToString() : string.Empty);
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), text, _d.X(918f), _d.Y(98f), A(TechniqueArt.Soft));
			return;
		}
		CraftingRecipeDescriptor recette = ligne.Recette;
		int rarete = JetDObjet.Rarete(objet, null);
		Color color = TechniqueArt.Mix(PersonnageScreen.CouleurRarete(rarete), TechniqueArt.White, 0.55f);
		(Rectangle Vitrine, int BordPuce) tuple = Vitrine(renderer, objet, rarete, color);
		Rectangle item = tuple.Vitrine;
		int item2 = tuple.BordPuce;
		string text2 = (MetiersArtisanat.EstArtisanat(recette.Profession) ? Strings.Workshop.TimeAndExperience.ToString(Professions.Percent((double)recette.Time / 1000.0), Experience(me, recette)) : Strings.Workshop.TimeOnly.ToString(Professions.Percent((double)recette.Time / 1000.0)));
		bool flag = item.Right - _d.L(10f) - TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(12f), text2).X < item2 + _d.L(8f);
		TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(12f), text2, item.Right - _d.L(10f), flag ? (item.Bottom - _d.L(16f)) : (item.Y + _d.L(20f)), A(KiClair));
		BulleSur(item, $"produit:{recette.Id}", () => BulleProduit(me, objet));
		int num = _d.X(918f);
		int num2 = _d.L(290f);
		int size = Ajuster(renderer, TechniqueArt.Heavy, objet.Name, num2, 28f, 24f, 21f);
		float num3 = 227f;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, size, TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, size, objet.Name, num2, 1).FirstOrDefault(), num, _d.Y(num3), A(color));
		num3 += 22f;
		int num4 = FenetreInventaire.NiveauRequis(objet);
		string text3 = ((num4 > 1) ? (SorteComplete(objet) + " · " + Strings.Hdv.NiveauCourt.ToString(num4)) : SorteComplete(objet));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(13f), text3, num2, 1).FirstOrDefault(), num, _d.Y(num3), A(TechniqueArt.Dim));
		num3 += 22f;
		List<CraftingRecipeIngredient> ingredients = recette.Ingredients;
		bool flag2 = ingredients.Count >= 4;
		if (!string.IsNullOrWhiteSpace(objet.Description))
		{
			foreach (string item3 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(13f), objet.Description.Trim(), num2, flag2 ? 2 : 3))
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), item3, num, _d.Y(num3), A(TechniqueArt.Soft));
				num3 += 18f;
			}
		}
		List<string> list = (from entree in LignesBonus(objet)
			select entree.Nom + " " + entree.Valeur).ToList();
		if (list.Count > 0)
		{
			num3 += 4f;
			foreach (string item4 in Rangees(renderer, TechniqueArt.Body, Pt(13f), list, num2, flag2 ? 1 : 2))
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), item4, num, _d.Y(num3), A(TechniqueArt.Text));
				num3 += 18f;
			}
		}
		num3 += 8f;
		int num5 = _d.X(1208f);
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), Strings.Workshop.Ingredients.ToString().ToUpperInvariant(), num, _d.Y(num3), _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
		TechniqueArt.SpacedRight(renderer, TechniqueArt.Label, Pt(10f), Strings.Workshop.BagAndNeeded.ToString().ToUpperInvariant(), num5, _d.Y(num3), _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
		float num6 = num3 + 12f;
		float num7 = ((ingredients.Count == 0) ? 38f : Math.Clamp((527f - num6) / (float)ingredients.Count, 30f, 38f));
		for (int num8 = 0; num8 < ingredients.Count; num8++)
		{
			CraftingRecipeIngredient craftingRecipeIngredient = ingredients[num8];
			ItemDescriptor materiau = DatabaseObject<ItemDescriptor>.Get(craftingRecipeIngredient.ItemId);
			float y = num6 + (float)num8 * num7;
			Rectangle rect = _d.R(914f, y, 298f, num7 - 2f);
			int num9 = rect.Y + rect.Height / 2;
			if (materiau != null)
			{
				IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Item, materiau.Icon);
				if (gameTexture != null)
				{
					int num10 = TailleIcone(gameTexture.Width);
					while (num10 > rect.Height + _d.L(4f) && num10 > gameTexture.Width)
					{
						num10 -= gameTexture.Width;
					}
					SaiyanHud.Draw(renderer, gameTexture, num, num9 - num10 / 2, num10, num10, A(TechniqueArt.White));
				}
			}
			int num11 = Possede(me, craftingRecipeIngredient.ItemId);
			string text4 = Strings.Workshop.OwnedOfNeeded.ToString(num11, craftingRecipeIngredient.Quantity);
			int num12 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(19f), text4, num5, num9, A((num11 >= craftingRecipeIngredient.Quantity) ? TechniqueArt.Green : Rouge));
			int num13 = num + _d.L(42f);
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(14f), materiau?.Name ?? "?", num5 - num12 - _d.L(10f) - num13, 1).FirstOrDefault(), num13, num9, A(TechniqueArt.Soft));
			if (materiau != null)
			{
				BulleSur(rect, $"ingredient:{materiau.Id}", () => BulleMateriau(me, materiau));
			}
		}
		DessinerEconomie(renderer, ligne, objet);
		DessinerFabrication(renderer, me, ligne, enCours, attente, avancee, index, nombre);
	}

	private void DessinerEconomie(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Ligne ligne, ItemDescriptor objet)
	{
		int x = _d.X(918f);
		int num = _d.X(1208f);
		TechniqueArt.Fill(renderer, _d.R(918f, 535f, 290f, 1f), A(TechniqueArt.Line));
		float num2 = 553f;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), Strings.Workshop.IngredientsCost, x, _d.Y(num2), A(TechniqueArt.Soft));
		TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(17f), MarketWindow.FormatAmount(ligne.Cout), num, _d.Y(num2), A(TechniqueArt.Text));
		num2 += 21f;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), Strings.Atelier.PrixHdv, x, _d.Y(num2), A(TechniqueArt.Soft));
		int num3 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(17f), (ligne.Prix > 0) ? MarketWindow.FormatAmount(ligne.Prix) : "—", num, _d.Y(num2), A((ligne.Prix > 0) ? TechniqueArt.Gold : Muted));
		bool flag = JetDObjet.ADesJets(objet);
		if (flag && MarketPrices.TryGet(objet.Id, out MarketPriceInfo info) && info.PerfectRollMedian > 0)
		{
			TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(12f), Strings.Workshop.PerfectRoll.ToString(MarketWindow.FormatAmount(info.PerfectRollMedian)), num - num3 - _d.L(8f), _d.Y(num2), A(Muted));
		}
		num2 += 21f;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), (flag ? Strings.Workshop.AverageProfit : Strings.Workshop.Profit).ToString(), x, _d.Y(num2), A(TechniqueArt.Soft));
		long benefice = ligne.Benefice;
		TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(17f), (ligne.Prix > 0) ? Signe(benefice) : "—", num, _d.Y(num2), A((ligne.Prix <= 0) ? Muted : ((benefice >= 0) ? TechniqueArt.Green : Rouge)));
	}

	private void DessinerFabrication(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, Ligne ligne, bool enCours, bool attente, float avancee, int index, int nombre)
	{
		CraftingRecipeDescriptor recette = ligne.Recette;
		bool flag = !ligne.Fermee && ligne.Faisables > 0 && !enCours && !attente && !_journal;
		int num = ((!ligne.Fermee) ? Math.Max(0, Math.Min(999, ligne.Faisables)) : 0);
		_quantiteMax = Math.Max(1, num);
		_quantite = Math.Clamp(_quantite, 1, _quantiteMax);
		_quantiteLibre = flag;
		float x = 914f;
		int num2 = Math.Max(_d.L(66f), TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(11f), Strings.Workshop.Max.ToString(num).ToUpperInvariant(), _d.Em(0.14f, 11f)) + _d.L(20f));
		Rectangle rectangle = _d.R(x, 622f, 34f, 34f);
		Rectangle rect = new Rectangle(_d.X(1212f) - num2, _d.Y(622f), num2, _d.L(34f));
		Rectangle rectangle2 = new Rectangle(rect.X - _d.L(8f) - _d.L(34f), rect.Y, _d.L(34f), _d.L(34f));
		Rectangle rect2 = new Rectangle(rectangle.Right + _d.L(8f), rectangle.Y, rectangle2.X - _d.L(8f) - rectangle.Right - _d.L(8f), rectangle.Height);
		_rangeeQuantite = new Rectangle(rectangle.X, rectangle.Y, rect.Right - rectangle.X, rectangle.Height);
		PetitBouton(renderer, rectangle, "–", "quantite:moins", flag && _quantite > 1, delegate
		{
			ChangerLaQuantite(-1);
		});
		TechniqueArt.Fill(renderer, rect2, A(Case));
		TechniqueArt.Outline(renderer, rect2, A(TechniqueArt.Line2));
		int value = (enCours ? Math.Max(1, nombre) : (attente ? _quantiteLancee : _quantite));
		TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(20f), $"{value}", rect2.X + rect2.Width / 2, rect2.Y + rect2.Height / 2, A(flag ? TechniqueArt.Text : Eteint));
		PetitBouton(renderer, rectangle2, "+", "quantite:plus", flag && _quantite < _quantiteMax, delegate
		{
			ChangerLaQuantite(1);
		});
		PetitBouton(renderer, rect, Strings.Workshop.Max.ToString(num).ToUpperInvariant(), "quantite:max", flag && _quantite < _quantiteMax, delegate
		{
			_quantite = _quantiteMax;
		});
		if (flag && (IsHot("quantite:moins") || IsHot("quantite:plus")))
		{
			Etiqueter(IsHot("quantite:moins") ? "quantite:moins" : "quantite:plus", IsHot("quantite:moins") ? rectangle : rectangle2, Strings.Atelier.MajDix);
		}
		Rectangle rect3 = _d.R(914f, 666f, 298f, 54f);
		if (_journal)
		{
			Bouton(renderer, rect3, Strings.Workshop.Journal, actif: false, chaud: false);
			AddHit(rect3, "fabriquer", null);
		}
		else if (enCours || attente)
		{
			TechniqueArt.Patch(renderer, "tech_bouton_eteint.png", rect3, A(TechniqueArt.White), 14);
			float num3 = (enCours ? Math.Clamp(((float)index + avancee) / (float)Math.Max(1, nombre), 0f, 1f) : 0f);
			int num4 = (int)Math.Round((float)rect3.Width * num3);
			if (num4 > 0)
			{
				Rectangle previous = TechniqueArt.BeginClip(renderer, new Rectangle(rect3.X, rect3.Y, num4, rect3.Height));
				TechniqueArt.Patch(renderer, "tech_bouton.png", rect3, A(TechniqueArt.White), 14);
				TechniqueArt.EndClip(renderer, previous);
			}
			int num5 = (enCours ? Math.Max(1, nombre) : _quantiteLancee);
			string text = Strings.Workshop.CraftingProgress.ToString(Math.Min(index + 1, num5), num5).ToUpperInvariant();
			int num6 = rect3.X + rect3.Width / 2;
			int num7 = rect3.Y + rect3.Height / 2 - _d.L(7f);
			int size = Pt(22f);
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Heavy, size, text, num6 + 1, num7 + 1, _d.Em(0.06f, 22f), A(TechniqueArt.Black, 0.6f));
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Heavy, size, text, num6, num7, _d.Em(0.06f, 22f), A(TechniqueArt.White));
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(11f), Strings.Atelier.CliquePourArreter.ToString().ToUpperInvariant(), num6, rect3.Y + rect3.Height / 2 + _d.L(14f), _d.Em(0.14f, 11f), A(TechniqueArt.White, 0.85f));
			AddHit(rect3, "arreter", delegate
			{
				Arreter(me);
			});
		}
		else if (ligne.Fermee)
		{
			Bouton(renderer, rect3, Strings.Workshop.LevelRequired.ToString(ligne.Niveau), actif: false, chaud: false);
			AddHit(rect3, "fabriquer", null);
		}
		else if (ligne.Faisables <= 0)
		{
			Bouton(renderer, rect3, Strings.Workshop.MissingIngredients, actif: false, chaud: false);
			AddHit(rect3, "fabriquer", null);
		}
		else
		{
			Bouton(renderer, rect3, Strings.Workshop.CraftMany.ToString(_quantite), actif: true, IsHot("fabriquer"));
			Guid id = recette.Id;
			AddHit(rect3, "fabriquer", delegate
			{
				Fabriquer(id);
			});
		}
	}

	private void Bouton(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, string texte, bool actif, bool chaud)
	{
		string texture = ((!actif) ? "tech_bouton_eteint.png" : (chaud ? "tech_bouton_survol.png" : "tech_bouton.png"));
		TechniqueArt.Patch(renderer, texture, rect, A(TechniqueArt.White), 14);
		string text = texte.ToUpperInvariant();
		float num = 24f;
		while (num > 13f && TechniqueArt.SpacedWidth(renderer, TechniqueArt.Heavy, Pt(num), text, _d.Em(0.06f, num)) > rect.Width - _d.L(28f))
		{
			num -= 1f;
		}
		TechniqueArt.SpacedCenter(renderer, TechniqueArt.Heavy, Pt(num), text, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, _d.Em(0.06f, num), A(actif ? HouseArt.Dark : new Color(255, 111, 122, 147)));
	}

	private void PetitBouton(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, string texte, string cle, bool possible, Action action)
	{
		bool flag = possible && IsHot(cle);
		TechniqueArt.Fill(renderer, rect, A(Case));
		TechniqueArt.Outline(renderer, rect, A(flag ? TechniqueArt.Soft : TechniqueArt.Line2));
		if (texte.Length > 1)
		{
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(11f), texte, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, _d.Em(0.14f, 11f), A((!possible) ? Eteint : (flag ? TechniqueArt.Text : TechniqueArt.Soft)));
		}
		else
		{
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(18f), texte, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, A((!possible) ? Eteint : (flag ? TechniqueArt.Text : TechniqueArt.Soft)));
		}
		AddHit(rect, cle, possible ? action : null);
	}

	private (Rectangle Vitrine, int BordPuce) Vitrine(Intersect.Client.Framework.Gwen.Renderer.Base renderer, ItemDescriptor objet, int rarete, Color couleurRarete)
	{
		Rectangle rectangle = _d.R(914f, 72f, 298f, 128f);
		TechniqueArt.FillVertical(renderer, rectangle, A(new Color(255, 10, 16, 28)), A(new Color(255, 13, 20, 34)));
		TechniqueArt.Glow(renderer, (float)rectangle.X + (float)rectangle.Width / 2f, (float)rectangle.Y + (float)rectangle.Height * 0.56f, _d.L(84f), A(PersonnageScreen.CouleurRarete(rarete), 0.22f));
		TechniqueArt.Outline(renderer, rectangle, A(TechniqueArt.Line));
		IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Item, objet.Icon);
		if (gameTexture != null)
		{
			int num = Math.Max(2, _d.L(79.36f) / Math.Max(1, gameTexture.Width));
			int num2 = gameTexture.Width * num;
			SaiyanHud.Draw(renderer, gameTexture, rectangle.X + (rectangle.Width - num2) / 2, rectangle.Y + (rectangle.Height - num2) / 2 + _d.L(4f), num2, num2, A(TechniqueArt.White));
		}
		if (rarete <= 0)
		{
			return (Vitrine: rectangle, BordPuce: rectangle.X);
		}
		string text = PersonnageScreen.NomRarete(rarete).ToUpperInvariant();
		int w = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10.5f), text, _d.Em(0.16f, 10.5f)) + _d.L(16f);
		Rectangle rect = new Rectangle(rectangle.X + _d.L(10f), rectangle.Y + _d.L(10f), w, _d.L(20f));
		TechniqueArt.Fill(renderer, rect, A(couleurRarete));
		TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(10.5f), text, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, _d.Em(0.16f, 10.5f), A(new Color(255, 10, 14, 24)));
		return (Vitrine: rectangle, BordPuce: rect.Right);
	}

	private static Bulle BulleProduit(Player me, ItemDescriptor objet)
	{
		int num = JetDObjet.Rarete(objet, null);
		Color color = PersonnageScreen.CouleurRarete(num);
		Bulle bulle = new Bulle
		{
			Icone = Globals.ContentManager.GetTexture(TextureType.Item, objet.Icon),
			Teinte = objet.Color,
			Nom = objet.Name,
			Couleur = color,
			CouleurNom = TechniqueArt.Mix(color, TechniqueArt.White, 0.55f),
			Sorte = ((num > 0) ? (SorteComplete(objet) + " · " + PersonnageScreen.NomRarete(num)) : SorteComplete(objet))
		};
		int num2 = FenetreInventaire.NiveauRequis(objet);
		if (num2 > 1)
		{
			bulle.Exigence = (Strings.Inventaire.LevelRequired.ToString(num2), (me.Level < num2) ? TechniqueArt.Red : TechniqueArt.Soft);
		}
		foreach (var (nom, valeur) in LignesBonus(objet))
		{
			bulle.Lignes.Add(new LigneBulle(nom, valeur, 0L, null));
		}
		bulle.Lignes.Add(new LigneBulle(Strings.Inventaire.WeightLine, Strings.Inventaire.Kilos.ToString(SaiyanHud.Amount(ReglesDeCharge.PoidsUnitaire(objet))), 0L, null));
		bulle.Description = (string.IsNullOrWhiteSpace(objet.Description) ? null : objet.Description.Trim());
		return bulle;
	}

	private Bulle BulleMateriau(Player me, ItemDescriptor objet)
	{
		int num = JetDObjet.Rarete(objet, null);
		Color color = PersonnageScreen.CouleurRarete(num);
		Bulle bulle = new Bulle
		{
			Icone = Globals.ContentManager.GetTexture(TextureType.Item, objet.Icon),
			Teinte = objet.Color,
			Nom = objet.Name,
			Couleur = color,
			CouleurNom = TechniqueArt.Mix(color, TechniqueArt.White, 0.55f),
			Sorte = ((num > 0) ? (FenetreInventaire.SorteDe(objet) + " · " + PersonnageScreen.NomRarete(num)) : FenetreInventaire.SorteDe(objet))
		};
		bulle.Lignes.Add(new LigneBulle(Strings.Atelier.DansLeSac, SaiyanHud.Amount(Possede(me, objet.Id)), 0L, null));
		bulle.Lignes.Add(new LigneBulle(Strings.Inventaire.WeightLine, Strings.Inventaire.Kilos.ToString(SaiyanHud.Amount(ReglesDeCharge.PoidsUnitaire(objet))), 0L, null));
		if (MarketPrices.TryGet(objet.Id, out MarketPriceInfo info) && info.Median > 0)
		{
			bulle.Lignes.Add(new LigneBulle(Strings.Atelier.PrixHdv, MarketWindow.FormatAmount(info.Median), 0L, null));
		}
		bulle.Description = (string.IsNullOrWhiteSpace(objet.Description) ? null : objet.Description.Trim());
		int num2 = _lignes.Count((Ligne ligne) => ligne.Recette.Ingredients.Any((CraftingRecipeIngredient ingredient) => ingredient.ItemId == objet.Id));
		if (num2 > 0)
		{
			bulle.Aides.Add((num2 == 1) ? Strings.Atelier.SertAUne.ToString() : Strings.Atelier.SertA.ToString(num2));
		}
		return bulle;
	}

	private static List<(string Nom, string Valeur)> LignesBonus(ItemDescriptor objet)
	{
		List<(string, string)> list = new List<(string, string)>();
		Dictionary<Stat, (Stat, int, int)> dictionary = JetDObjet.Fourchettes(objet).ToDictionary(((Stat Stat, int Bas, int Haut) entree) => entree.Stat);
		for (int num = 0; num < Enum.GetValues<Stat>().Length; num++)
		{
			Stat key = (Stat)num;
			int num2 = ((num < objet.StatsGiven.Length) ? objet.StatsGiven[num] : 0);
			int num3 = ((num < objet.PercentageStatsGiven.Length) ? objet.PercentageStatsGiven[num] : 0);
			(Stat, int, int) value;
			bool flag = dictionary.TryGetValue(key, out value);
			if (num2 != 0 || num3 != 0 || flag)
			{
				LocalizedString value2;
				string item = (Strings.ItemDescription.Stats.TryGetValue(num, out value2) ? value2.ToString().TrimEnd(' ', ':', ' ') : key.ToString());
				List<string> list2 = new List<string>();
				if (flag)
				{
					list2.Add(Strings.Workshop.Range.ToString(num2 + value.Item2, num2 + value.Item3));
				}
				else if (num2 != 0)
				{
					list2.Add(Signe(num2));
				}
				if (num3 != 0)
				{
					list2.Add(Signe(num3) + " %");
				}
				list.Add((item, string.Join(" ", list2)));
			}
		}
		for (int num4 = 0; num4 < objet.VitalsGiven.Length; num4++)
		{
			if (objet.VitalsGiven[num4] != 0L)
			{
				string item2 = ((num4 == 0) ? Strings.Inventaire.MaxHealth : Strings.Inventaire.MaxKi).ToString();
				list.Add((item2, Signe(objet.VitalsGiven[num4])));
			}
		}
		if (objet.ItemType == ItemType.Consumable)
		{
			ConsumableData consumable = objet.Consumable;
			if (consumable != null)
			{
				if (consumable.Value != 0L)
				{
					LocalizedString localizedString = consumable.Type switch
					{
						ConsumableType.Health => Strings.Inventaire.RestoresHealth, 
						ConsumableType.Mana => Strings.Inventaire.RestoresKi, 
						_ => Strings.Inventaire.RestoresExperience, 
					};
					list.Add((Strings.Inventaire.Restores, localizedString.ToString(SaiyanHud.Amount(consumable.Value))));
				}
				if (consumable.Percentage != 0)
				{
					LocalizedString localizedString2 = consumable.Type switch
					{
						ConsumableType.Health => Strings.Inventaire.RestoresHealthPercent, 
						ConsumableType.Mana => Strings.Inventaire.RestoresKiPercent, 
						_ => Strings.Inventaire.RestoresExperiencePercent, 
					};
					list.Add((Strings.Inventaire.Restores, localizedString2.ToString(consumable.Percentage)));
				}
			}
		}
		if (objet.ItemType == ItemType.Bag)
		{
			list.Add((Strings.Inventaire.CapsuleExtra, Strings.Inventaire.KilosMore.ToString(SaiyanHud.Amount(ReglesDeCharge.BonusDeCapsule(objet)))));
		}
		return list;
	}

	private void CaseObjet(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, ItemDescriptor? objet, int quantite, bool eteinte, bool pale = false, bool toujoursLeCompte = false)
	{
		TechniqueArt.Fill(renderer, rect, A(Case));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line2));
		if (objet == null)
		{
			return;
		}
		IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Item, objet.Icon);
		if (gameTexture != null)
		{
			int num = TailleIcone(gameTexture.Width);
			while (num > rect.Width - 4 && num > gameTexture.Width)
			{
				num -= gameTexture.Width;
			}
			float alpha = (eteinte ? 0.55f : (pale ? 0.35f : 1f));
			SaiyanHud.Draw(renderer, gameTexture, rect.X + (rect.Width - num) / 2, rect.Y + (rect.Height - num) / 2, num, num, A(eteinte ? new Color(255, 110, 116, 128) : TechniqueArt.White, alpha));
		}
		int num2 = JetDObjet.Rarete(objet, null);
		if (num2 > 0)
		{
			TechniqueArt.Fill(renderer, new Rectangle(rect.X + 2, rect.Bottom - 2 - Math.Max(1, _d.L(2f)), rect.Width - 4, Math.Max(1, _d.L(2f))), A(PersonnageScreen.CouleurRarete(num2), (eteinte || pale) ? 0.5f : 1f));
		}
		if (quantite > 1 || toujoursLeCompte)
		{
			string text = MarketWindow.FormatAmount(Math.Max(0, quantite));
			int num3 = rect.Right - _d.L(3f);
			int num4 = rect.Bottom - _d.L(9f);
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(13f), text, num3 + 1, num4 + 1, A(TechniqueArt.Black, 0.9f));
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(13f), text, num3, num4, A((quantite > 0) ? TechniqueArt.White : TechniqueArt.Dim));
		}
		if (eteinte)
		{
			int num5 = _d.L(12f);
			Lisser("inv_cadenas.png", new Rectangle(rect.Right - num5 - _d.L(3f), rect.Y + _d.L(3f), num5, num5), A(Rouge));
		}
	}

	private static List<string> Rangees(Intersect.Client.Framework.Gwen.Renderer.Base renderer, IFont? police, int taille, List<string> morceaux, int largeur, int maximum)
	{
		List<string> list = new List<string>();
		string text = string.Empty;
		for (int i = 0; i < morceaux.Count; i++)
		{
			string text2 = ((text.Length == 0) ? morceaux[i] : (text + " · " + morceaux[i]));
			if (text.Length == 0 || TechniqueArt.Measure(renderer, police, taille, text2).X <= largeur)
			{
				text = text2;
				continue;
			}
			if (list.Count == maximum - 1)
			{
				text = string.Join(" · ", new string[1] { text }.Concat(morceaux.Skip(i)));
				break;
			}
			list.Add(text);
			text = morceaux[i];
		}
		if (text.Length > 0)
		{
			list.AddRange(TechniqueArt.Wrap(renderer, police, taille, text, largeur, 1));
		}
		return list.Take(maximum).ToList();
	}

	private int Ajuster(Intersect.Client.Framework.Gwen.Renderer.Base renderer, IFont? police, string texte, int largeur, params float[] tailles)
	{
		foreach (float cssPixels in tailles)
		{
			if (TechniqueArt.Measure(renderer, police, Pt(cssPixels), texte).X <= largeur)
			{
				return Pt(cssPixels);
			}
		}
		return Pt(tailles[^1]);
	}

	private static string SorteComplete(ItemDescriptor objet)
	{
		if (objet.ItemType != ItemType.Equipment || objet.EquipmentSlot < 0 || objet.EquipmentSlot >= Options.Instance.Equipment.Slots.Count)
		{
			return FenetreInventaire.SorteDe(objet);
		}
		return $"{Strings.Personnage.CategoryEquipment} · {FenetreInventaire.SorteDe(objet)}";
	}

	private static long Experience(Player me, CraftingRecipeDescriptor recette)
	{
		if (recette.ProfessionExperience <= 0)
		{
			return Math.Max(1L, (long)Math.Round((double)recette.Time / 1000.0 * me.ProfessionCraftExperiencePerSecond));
		}
		return recette.ProfessionExperience;
	}

	private static string Signe(long valeur)
	{
		if (valeur <= 0)
		{
			if (valeur >= 0)
			{
				return "0";
			}
			return "–" + MarketWindow.FormatAmount(-valeur);
		}
		return "+" + MarketWindow.FormatAmount(valeur);
	}
}
