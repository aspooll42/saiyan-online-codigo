using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Items;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Dungeons;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Housing;
using Intersect.Client.Interface.Game.Inventaire;
using Intersect.Client.Interface.Game.Personnage;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.Market;
using Intersect.Localization;
using Intersect.Models;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game.Hdv;

internal sealed class FenetreHdv : FenetreCharte
{
	private enum Mode
	{
		Acheter,
		Vendre,
		Ventes
	}

	private enum TriSac
	{
		Aucun,
		Valeur,
		Rarete,
		Type,
		Nom,
		Quantite
	}

	private const float Largeur = 1240f;

	private const float Hauteur = 748f;

	private const float BodyTop = 58f;

	private const float BodyHeight = 676f;

	private const float BodyBas = 734f;

	private const float RailLeft = 14f;

	private const float RailWidth = 224f;

	private const float CenterLeft = 248f;

	private const float CenterWidth = 640f;

	private const float FicheLeft = 898f;

	private const float FicheWidth = 328f;

	private const float Marge = 16f;

	private const float Gauche = 264f;

	private const float LigneWidth = 608f;

	private const float ChampTop = 74f;

	private const float ChampHeight = 36f;

	private const float TriWidth = 196f;

	private const float ChampWidth = 402f;

	private const float TitreTop = 122f;

	private const float TitreHeight = 34f;

	private const float EnteteTop = 158f;

	private const float EnteteHeight = 20f;

	private const float ListeTop = 182f;

	private const float LigneHeight = 52f;

	private const float Pas = 4f;

	private const float PiedHeight = 34f;

	private const float PiedTop = 684f;

	private const float ColonneNom = 58f;

	private const float ColonneJet = 300f;

	private const float ColonneVendeur = 404f;

	private const int SacColonnes = 10;

	private const float SacCase = 54f;

	private const float SacEcart = 6f;

	private const float VitrineTop = 70f;

	private const float QuantiteTop = 294f;

	private const float PrixTop = 358f;

	private const float IndicesTop = 424f;

	private const float BoutonHeight = 46f;

	private const float BoutonTop = 674f;

	private const long DelaiRecherche = 350L;

	private static readonly Color Rouge = new Color(255, 255, 120, 110);

	private static readonly Color Case = new Color(255, 10, 14, 24);

	private static readonly Color Eteint = new Color(255, 110, 116, 128);

	private static readonly MarketSort[] Tris = new MarketSort[5]
	{
		MarketSort.PriceAscending,
		MarketSort.PriceDescending,
		MarketSort.Newest,
		MarketSort.Name,
		MarketSort.BestRoll
	};

	private Mode _mode;

	private MarketCategory _categorie;

	private MarketSort _tri;

	private int _page;

	private int _pages = 1;

	private int _total;

	private bool _repondu;

	private long _rechercheA;

	private string _rechercheEnvoyee = string.Empty;

	private MarketListingInfo[] _annonces = Array.Empty<MarketListingInfo>();

	private MarketListingInfo[] _miennes = Array.Empty<MarketListingInfo>();

	private int _maxAnnonces = 20;

	private int _taxe = 5;

	private Guid _annonceChoisie;

	private Guid _venteChoisie;

	private int _caseChoisie = -1;

	private Guid _objetChoisi;

	private MarketPriceInfo? _indices;

	private string _message = string.Empty;

	private Color _couleurMessage = TechniqueArt.Dim;

	private int _defilement;

	private int _rangees;

	private int _visibles = 1;

	private Rectangle _liste;

	private int _signature;

	private static readonly TriSac[] TrisSac = new TriSac[6]
	{
		TriSac.Aucun,
		TriSac.Valeur,
		TriSac.Rarete,
		TriSac.Type,
		TriSac.Nom,
		TriSac.Quantite
	};

	private TriSac _triSac;

	private readonly TextBox _recherche;

	private readonly TextBoxNumeric _quantite;

	private readonly TextBoxNumeric _prix;

	protected override Rectangle Place => new Rectangle(180, 76, 1240, 748);

	public bool IsOpen => !base.IsHidden;

	public FenetreHdv(Canvas gameCanvas)
		: base(gameCanvas, "FenetreHdv")
	{
		_recherche = new TextBox(this, "HdvRecherche")
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
			_rechercheA = Timing.Global.Milliseconds + 350;
		};
		_recherche.SubmitPressed += delegate
		{
			Rechercher(0);
		};
		Interface.FocusComponents.Add(_recherche);
		_quantite = Nombre("HdvQuantite");
		_prix = Nombre("HdvPrix");
		_prix.SubmitPressed += delegate
		{
			MettreEnVente();
		};
		MarketPrices.Received += OnPrix;
	}

	private TextBoxNumeric Nombre(string nom)
	{
		TextBoxNumeric textBoxNumeric = new TextBoxNumeric(this, nom)
		{
			Font = TechniqueArt.Heavy,
			FontSize = 13,
			ShouldDrawBackground = false,
			MaximumLength = 12,
			IsHidden = true
		};
		textBoxNumeric.SetTextColor(TechniqueArt.Text, ComponentState.Normal);
		Interface.FocusComponents.Add(textBoxNumeric);
		return textBoxNumeric;
	}

	public void Toggle()
	{
		if (IsOpen)
		{
			Fermer();
			return;
		}
		_message = string.Empty;
		Show();
		Placer();
		PlacerChamps();
		Rechercher(_page);
		PacketSender.SendMarketMyListings();
		DemanderIndices();
	}

	public override void Hide()
	{
		_recherche.Blur();
		_quantite.Blur();
		_prix.Blur();
		base.Hide();
	}

	protected override void Dispose(bool disposing)
	{
		MarketPrices.Received -= OnPrix;
		base.Dispose(disposing);
	}

	public void OnListings(MarketListingsPacket packet)
	{
		_annonces = packet.Listings ?? Array.Empty<MarketListingInfo>();
		_page = packet.Page;
		_pages = Math.Max(1, packet.PageCount);
		_total = packet.TotalCount;
		_taxe = packet.SaleTaxPercent;
		_repondu = true;
		MarketWindow.RememberCurrency(packet.CurrencyItemId);
		if (_annonces.All((MarketListingInfo annonce) => annonce.Id != _annonceChoisie))
		{
			_annonceChoisie = _annonces.FirstOrDefault()?.Id ?? Guid.Empty;
		}
	}

	public void OnMyListings(MarketMyListingsResultPacket packet)
	{
		_miennes = packet.Listings ?? Array.Empty<MarketListingInfo>();
		_maxAnnonces = packet.MaxListings;
		_taxe = packet.SaleTaxPercent;
		MarketWindow.RememberCurrency(packet.CurrencyItemId);
		if (_miennes.All((MarketListingInfo annonce) => annonce.Id != _venteChoisie))
		{
			_venteChoisie = _miennes.FirstOrDefault()?.Id ?? Guid.Empty;
		}
		_defilement = Math.Clamp(_defilement, 0, Math.Max(0, _miennes.Length - 1));
	}

	public void OnResult(MarketResultPacket packet, string text, bool success)
	{
		if (packet.Result == MarketResult.Bought)
		{
			Audio.AddGameSound("buy.wav", loop: false);
		}
		if (!base.IsHidden)
		{
			Message((packet.Result == MarketResult.NoCurrency) ? string.Empty : text, success ? TechniqueArt.Green : Rouge);
			switch (packet.Result)
			{
			default:
				return;
			case MarketResult.Listed:
				_caseChoisie = -1;
				_objetChoisi = Guid.Empty;
				break;
			case MarketResult.Bought:
			case MarketResult.Cancelled:
			case MarketResult.Collected:
			case MarketResult.ItemsReturned:
			case MarketResult.SoldNotice:
			case MarketResult.ListingUnavailable:
				break;
			}
			if (_mode == Mode.Acheter)
			{
				Rechercher(_page);
			}
			PacketSender.SendMarketMyListings();
		}
	}

	private void OnPrix(MarketPricesResultPacket packet)
	{
		if (!base.IsHidden && packet.Slot >= 0 && packet.Slot == _caseChoisie && !(_objetChoisi == Guid.Empty))
		{
			_indices = packet.Prices?.FirstOrDefault((MarketPriceInfo info) => info?.ItemId == _objetChoisi) ?? new MarketPriceInfo
			{
				ItemId = _objetChoisi
			};
		}
	}

	private void DemanderIndices()
	{
		_indices = null;
		if (_caseChoisie >= 0 && _objetChoisi != Guid.Empty)
		{
			MarketPrices.Request(new Guid[1] { _objetChoisi }, _caseChoisie);
		}
	}

	private void Rechercher(int page)
	{
		_rechercheA = 0L;
		_rechercheEnvoyee = _recherche.Text?.Trim() ?? string.Empty;
		PacketSender.SendMarketSearch(_rechercheEnvoyee, _categorie, _tri, Math.Max(0, page));
	}

	private void Message(string texte, Color couleur)
	{
		_message = texte;
		_couleurMessage = couleur;
	}

	public void Update()
	{
		if (base.IsHidden)
		{
			return;
		}
		Placer();
		PlacerChamps();
		if (_rechercheA > 0 && Timing.Global.Milliseconds >= _rechercheA)
		{
			Rechercher(0);
		}
		int num = Signature();
		if (num != _signature)
		{
			_signature = num;
			if (_caseChoisie >= 0 && !Choisi(out IItem _, out ItemDescriptor _))
			{
				_caseChoisie = -1;
				_objetChoisi = Guid.Empty;
				_indices = null;
			}
		}
	}

	private void PlacerChamps()
	{
		ItemDescriptor objet = null;
		IItem item;
		bool flag = _mode == Mode.Vendre && Choisi(out item, out objet);
		bool visible = flag && (objet?.Stackable ?? false);
		Poser(_recherche, _mode == Mode.Acheter, _d.R(298f, 78f, 358f, 28f), Pt(13.5f));
		Poser(_quantite, visible, _d.R(962f, 316f, 140f, 26f), Pt(16f));
		Poser(_prix, flag, _d.R(924f, 380f, 248f, 28f), Pt(17f));
		static void Poser(TextBox champ, bool flag2, Rectangle zone, int taille)
		{
			if (champ.IsHidden != !flag2)
			{
				champ.IsHidden = !flag2;
				if (!flag2)
				{
					champ.Blur();
				}
			}
			if (flag2)
			{
				if (champ.FontSize != taille)
				{
					champ.FontSize = taille;
				}
				if (champ.X != zone.X || champ.Y != zone.Y || champ.Width != zone.Width || champ.Height != zone.Height)
				{
					champ.SetBounds(zone.X, zone.Y, zone.Width, zone.Height);
				}
			}
		}
	}

	private static int Signature()
	{
		HashCode hashCode = default(HashCode);
		Player me = Globals.Me;
		if (me == null)
		{
			return 0;
		}
		IItem[] inventory = me.Inventory;
		foreach (IItem item in inventory)
		{
			hashCode.Add(item?.ItemId ?? Guid.Empty);
			hashCode.Add(item?.Quantity ?? 0);
		}
		int[] myEquipment = me.MyEquipment;
		foreach (int value in myEquipment)
		{
			hashCode.Add(value);
		}
		hashCode.Add(me.Zeni);
		return hashCode.ToHashCode();
	}

	protected override bool OnMouseWheeled(int delta)
	{
		Point point = LocalMouse();
		if (!base.IsHidden && _mode == Mode.Ventes && _liste.Contains(point.X, point.Y))
		{
			_defilement = Math.Clamp(_defilement + ((delta <= 0) ? 1 : (-1)), 0, Math.Max(0, _rangees - _visibles));
			return true;
		}
		return base.OnMouseWheeled(delta);
	}

	protected override void Dessiner(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		DessinerCadre(renderer);
		int croix = DessinerBarre(renderer, Strings.Market.Title);
		DessinerEntete(renderer, croix, now);
		DessinerRail(renderer);
		Panneau(renderer, _d.R(248f, 58f, 640f, 676f));
		Panneau(renderer, _d.R(898f, 58f, 328f, 676f));
		_liste = default(Rectangle);
		switch (_mode)
		{
		case Mode.Vendre:
			DessinerSac(renderer);
			DessinerFormulaire(renderer, now);
			break;
		case Mode.Ventes:
			DessinerMesVentes(renderer, now);
			DessinerFicheVente(renderer, now);
			break;
		default:
			DessinerAchat(renderer, now);
			DessinerFicheAnnonce(renderer, now);
			break;
		}
	}

	protected override void DessinerDessus(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		DessinerEtiquette(renderer, now);
	}

	private void Panneau(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect)
	{
		TechniqueArt.Patch(renderer, "tech_panneau.png", rect, A(TechniqueArt.White), 12);
	}

	private void DessinerEntete(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int croix, long now)
	{
		int num = _d.Y(23f);
		int droite = croix - _d.L(18f);
		int value = _miennes.Count((MarketListingInfo annonce) => annonce.State == MarketListingState.Active && annonce.Quantity > 0);
		droite = Info(renderer, droite, num, Strings.Hdv.MesAnnonces, $"{value} / {_maxAnnonces}", TechniqueArt.Text) - _d.L(26f);
		droite = Info(renderer, droite, num, null, MarketWindow.FormatAmount(Globals.Me?.Zeni ?? 0), TechniqueArt.Gold);
		int num2 = _d.L(20f);
		HouseArt.Zeni(renderer, new Rectangle(droite - _d.L(6f) - num2, num - num2 / 2, num2, num2), ImagePiece(now), _fade);
		droite -= num2 + _d.L(14f);
		string text = MonnaieNom().ToUpperInvariant();
		int num3 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), text, _d.Em(0.16f, 10f));
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), text, droite - num3, num + _d.L(1f), _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
	}

	private int Info(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int droite, int centre, string? intitule, string valeur, Color couleur)
	{
		int num = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(16f), valeur, droite, centre, A(couleur));
		droite -= num + _d.L(8f);
		if (string.IsNullOrEmpty(intitule))
		{
			return droite;
		}
		string text = intitule.ToUpperInvariant();
		int num2 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), text, _d.Em(0.16f, 10f));
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), text, droite - num2, centre + _d.L(1f), _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
		return droite - num2;
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

	private void DessinerRail(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		Panneau(renderer, _d.R(14f, 58f, 224f, 676f));
		float y = 66f;
		y = ModeRail(renderer, Mode.Acheter, Strings.Market.TabBuy, null, y);
		if (_mode == Mode.Acheter)
		{
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), Strings.Market.Categories.ToString().ToUpperInvariant(), _d.X(36f), _d.Y(y + 14f), _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
			y += 26f;
			(MarketCategory, string, string)[] array = new(MarketCategory, string, string)[4]
			{
				(MarketCategory.All, Strings.Market.CategoryAll, "inv_glyphe_tout.png"),
				(MarketCategory.Equipment, Strings.Market.CategoryEquipment, "inv_glyphe_equipement.png"),
				(MarketCategory.Consumables, Strings.Market.CategoryConsumables, "inv_glyphe_consommable.png"),
				(MarketCategory.Resources, Strings.Market.CategoryResources, "inv_glyphe_ressource.png")
			};
			for (int i = 0; i < array.Length; i++)
			{
				(MarketCategory, string, string) tuple = array[i];
				MarketCategory item = tuple.Item1;
				string item2 = tuple.Item2;
				string item3 = tuple.Item3;
				Rectangle rect = _d.R(24f, y, 204f, 32f);
				string cle = $"categorie:{item}";
				bool flag = _categorie == item;
				bool flag2 = IsHot(cle);
				if (flag || flag2)
				{
					TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, flag ? 0.06f : 0.035f));
					TechniqueArt.Outline(renderer, rect, A(flag ? TechniqueArt.Line2 : TechniqueArt.Line));
				}
				int num = _d.L(15f);
				Lisser(item3, new Rectangle(rect.X + _d.L(10f), rect.Y + (rect.Height - num) / 2, num, num), A(flag ? TechniqueArt.Ki : TechniqueArt.Dim));
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), item2, rect.X + _d.L(34f), rect.Y + rect.Height / 2, A(flag ? TechniqueArt.Text : TechniqueArt.Soft));
				if (flag && _repondu)
				{
					TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(11f), MarketWindow.FormatAmount(_total), rect.Right - _d.L(10f), rect.Y + rect.Height / 2, A(TechniqueArt.Dim));
				}
				MarketCategory choix = item;
				AddHit(rect, cle, delegate
				{
					ChoisirCategorie(choix);
				});
				y += 34f;
			}
			y += 6f;
		}
		y = ModeRail(renderer, Mode.Vendre, Strings.Market.TabSell, null, y);
		int actives = _miennes.Count((MarketListingInfo annonce) => annonce.State == MarketListingState.Active && annonce.Quantity > 0);
		ModeRail(renderer, Mode.Ventes, Strings.Market.MineHeading, (_miennes.Length != 0) ? $"{_miennes.Length}" : null, y);
		DessinerGains(renderer, actives);
	}

	private float ModeRail(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Mode mode, string texte, string? compte, float y)
	{
		Rectangle rect = _d.R(15f, y, 222f, 44f);
		string cle = $"mode:{mode}";
		bool flag = _mode == mode;
		if (flag)
		{
			TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Ki, 0.18f), A(TechniqueArt.Ki, 0.07f), A(TechniqueArt.Ki, 0f));
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Ki));
		}
		else if (IsHot(cle))
		{
			TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, 0.04f));
		}
		TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), A(TechniqueArt.Line));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(19f), texte.ToUpperInvariant(), rect.X + _d.L(18f), rect.Y + rect.Height / 2, A(flag ? TechniqueArt.Text : TechniqueArt.Soft));
		if (!string.IsNullOrEmpty(compte))
		{
			TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(11f), compte, rect.Right - _d.L(14f), rect.Y + rect.Height / 2, A(TechniqueArt.Dim));
		}
		AddHit(rect, cle, delegate
		{
			ChoisirMode(mode);
		});
		return y + 46f;
	}

	private void DessinerGains(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int actives)
	{
		long num = _miennes.Sum((MarketListingInfo annonce) => annonce.PendingProceeds);
		bool flag = _miennes.Any((MarketListingInfo annonce) => annonce.State == MarketListingState.Expired);
		float num2 = 596f;
		TechniqueArt.Fill(renderer, _d.R(15f, num2, 222f, 1f), A(TechniqueArt.Line));
		int x = _d.X(32f);
		int right = _d.X(220f);
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), Strings.Market.MineHeading.ToString().ToUpperInvariant(), x, _d.Y(num2 + 18f), _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), Strings.Hdv.MesAnnonces, x, _d.Y(num2 + 42f), A(TechniqueArt.Soft));
		TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(14f), $"{actives} / {_maxAnnonces}", right, _d.Y(num2 + 42f), A(TechniqueArt.Text));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), Strings.Hdv.AEncaisser, x, _d.Y(num2 + 66f), A(TechniqueArt.Soft));
		TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(14f), (num > 0) ? ("+" + MarketWindow.FormatAmount(num)) : Strings.Hdv.Rien.ToString(), right, _d.Y(num2 + 66f), A((num > 0) ? TechniqueArt.Gold : TechniqueArt.Dim));
		Rectangle rect = _d.R(30f, 688f, 192f, 34f);
		bool flag2 = num > 0 || flag;
		HouseArt.Hollow(renderer, _d, rect, Strings.Market.CollectAll, flag2 && IsHot("encaisser"), flag2 ? _fade : (_fade * 0.45f));
		AddHit(rect, "encaisser", flag2 ? new Action(PacketSender.SendMarketCollect) : null);
	}

	private void ChoisirMode(Mode mode)
	{
		_mode = mode;
		Message(string.Empty, TechniqueArt.Dim);
		_defilement = 0;
		switch (mode)
		{
		case Mode.Acheter:
			Rechercher(_page);
			break;
		case Mode.Ventes:
			PacketSender.SendMarketMyListings();
			break;
		default:
			DemanderIndices();
			break;
		}
	}

	private void ChoisirCategorie(MarketCategory categorie)
	{
		_categorie = categorie;
		_annonceChoisie = Guid.Empty;
		Rechercher(0);
	}

	private void DessinerAchat(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		Rectangle rect = _d.R(264f, 74f, 402f, 36f);
		TechniqueArt.Fill(renderer, rect, A(new Color(255, 7, 10, 18)));
		TechniqueArt.Outline(renderer, rect, A((InputHandler.KeyboardFocus == _recherche) ? TechniqueArt.Ki : TechniqueArt.Line2));
		int num = _d.L(14f);
		Lisser("inv_loupe.png", new Rectangle(rect.X + _d.L(12f), rect.Y + (rect.Height - num) / 2, num, num), A(TechniqueArt.Soft));
		if (string.IsNullOrEmpty(_recherche.Text))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13.5f), Strings.Market.SearchHint, rect.X + _d.L(34f), rect.Y + rect.Height / 2, A(TechniqueArt.Dim));
		}
		BoutonTri(renderer, _d.R(676f, 74f, 196f, 36f), "tri", NomTri(_tri), TriSuivant);
		if (_repondu && _total == 0 && _rechercheEnvoyee.Length == 0 && _categorie == MarketCategory.All)
		{
			DessinerAccueil(renderer);
			return;
		}
		Titre(renderer, 122f, NomCategorie(_categorie), _repondu ? (NombreAnnonces(_total) + " · " + OrdreTri(_tri)) : null);
		Colonnes(renderer, Strings.Market.ColumnRoll, Strings.Market.ColumnSeller, Strings.Hdv.ColonnePrix);
		if (_annonces.Length == 0)
		{
			if (_repondu)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), Strings.Market.Empty, _d.X(276f), _d.Y(202f), A(TechniqueArt.Dim));
			}
		}
		else
		{
			for (int i = 0; i < _annonces.Length && i < 8; i++)
			{
				LigneAnnonce(renderer, _annonces[i], 182f + (float)i * 56f, mine: false);
			}
		}
		Rectangle rectangle = _d.R(264f, 684f, 608f, 34f);
		string text = ((_message.Length > 0) ? _message : ((_pages > 1) ? Strings.Market.PageOf.ToString(_page + 1, _pages) : string.Empty));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), text, _d.L(438f), 1).FirstOrDefault(), rectangle.X, rectangle.Y + rectangle.Height / 2, A((_message.Length > 0) ? _couleurMessage : TechniqueArt.Dim));
		int num2 = _d.L(34f);
		Rectangle rect2 = new Rectangle(rectangle.Right - num2, rectangle.Y, num2, num2);
		string text2 = $"{_page + 1} / {_pages}";
		int num3 = Math.Max(_d.L(56f), TechniqueArt.Measure(renderer, TechniqueArt.Heavy, Pt(15f), text2).X + _d.L(16f));
		Rectangle rect3 = new Rectangle(rect2.X - num3 - num2, rectangle.Y, num2, num2);
		Fleche(renderer, rect3, "<", "page:precedente", _page > 0, delegate
		{
			Rechercher(_page - 1);
		});
		TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(15f), text2, rect3.Right + num3 / 2, rectangle.Y + rectangle.Height / 2, A(TechniqueArt.Text));
		Fleche(renderer, rect2, ">", "page:suivante", _page < _pages - 1, delegate
		{
			Rechercher(_page + 1);
		});
	}

	private void Fleche(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, string signe, string cle, bool possible, Action action)
	{
		bool flag = possible && IsHot(cle);
		TechniqueArt.Fill(renderer, rect, A(new Color(255, 7, 10, 18)));
		TechniqueArt.Outline(renderer, rect, A(flag ? TechniqueArt.Soft : TechniqueArt.Line2));
		TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(15f), signe, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, A((!possible) ? Eteint : (flag ? TechniqueArt.Text : TechniqueArt.Soft)));
		AddHit(rect, cle, possible ? action : null);
	}

	private void BoutonTri(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle tri, string cle, string valeur, Action suivant)
	{
		bool flag = IsHot(cle);
		TechniqueArt.Fill(renderer, tri, A(new Color(255, 7, 10, 18)));
		TechniqueArt.Outline(renderer, tri, A(flag ? TechniqueArt.Soft : TechniqueArt.Line2));
		int num = TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11f), Strings.Hdv.Tri, tri.X + _d.L(12f), tri.Y + tri.Height / 2, A(TechniqueArt.Dim));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), valeur, tri.X + _d.L(18f) + num, tri.Y + tri.Height / 2, A(flag ? TechniqueArt.Text : TechniqueArt.Soft));
		int num2 = _d.L(10f);
		Lisser("inv_chevron.png", new Rectangle(tri.Right - _d.L(12f) - num2, tri.Y + (tri.Height - num2) / 2, num2, num2), A(TechniqueArt.Soft));
		AddHit(tri, cle, suivant);
		Etiqueter(cle, tri, Strings.Market.Sort.ToString(valeur.ToLowerInvariant()));
	}

	private void TriSuivant()
	{
		int num = Array.IndexOf(Tris, _tri);
		_tri = Tris[(num + 1) % Tris.Length];
		Rechercher(0);
	}

	private void Titre(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float top, string titre, string? complement)
	{
		int num = _d.Y(top + 17f);
		int num2 = _d.X(264f);
		DungeonHud.Diamond(renderer, num2 + _d.L(2f), num, _d.L(10f), A(TechniqueArt.Green));
		int num3 = TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(22f), titre.ToUpperInvariant(), num2 + _d.L(18f), num, A(TechniqueArt.Text));
		if (!string.IsNullOrEmpty(complement))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(13f), complement, num2 + _d.L(18f) + num3 + _d.L(12f), num + _d.L(2f), A(TechniqueArt.Dim));
		}
	}

	private void Colonnes(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string jet, string vendeur, string prix, float haut = 158f)
	{
		int centre = _d.Y(haut + 10f);
		float x = 264f;
		Colonne(Strings.Market.ColumnItem, 58f);
		Colonne(jet, 300f);
		Colonne(vendeur, 404f);
		string text = prix.ToUpperInvariant();
		TechniqueArt.SpacedRight(renderer, TechniqueArt.Label, Pt(9.5f), text, _d.X(x + 608f - 14f), centre, _d.Em(0.16f, 9.5f), A(TechniqueArt.Dim));
		void Colonne(string texte, float colonne)
		{
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(9.5f), texte.ToUpperInvariant(), _d.X(x + colonne), centre, _d.Em(0.16f, 9.5f), A(TechniqueArt.Dim));
		}
	}

	private void LigneAnnonce(Intersect.Client.Framework.Gwen.Renderer.Base renderer, MarketListingInfo annonce, float y, bool mine)
	{
		Rectangle rect = _d.R(264f, y, 608f, 52f);
		string cle = $"{(mine ? "vente" : "annonce")}:{annonce.Id}";
		bool flag = (mine ? (annonce.Id == _venteChoisie) : (annonce.Id == _annonceChoisie));
		bool flag2 = IsHot(cle);
		if (flag)
		{
			TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Ki, 0.15f), A(TechniqueArt.Ki, 0.06f), A(TechniqueArt.Ki, 0.02f));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Ki));
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Ki));
		}
		else
		{
			TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, flag2 ? 0.055f : 0.03f));
			TechniqueArt.Outline(renderer, rect, A(flag2 ? TechniqueArt.Line2 : TechniqueArt.Line));
		}
		ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(annonce.ItemId);
		int num = rect.Y + rect.Height / 2;
		CaseObjet(renderer, _d.R(272f, y + 6f, 40f, 40f), itemDescriptor, Math.Max(0, annonce.Quantity), annonce.Properties, choisie: false, eteinte: false);
		int x = _d.X(322f);
		int width = _d.L(232f);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(15f), TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(15f), itemDescriptor?.Name ?? "?", width, 1).FirstOrDefault(), x, num - _d.L(8f), A(TechniqueArt.Text));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11f), (itemDescriptor == null) ? string.Empty : Sorte(itemDescriptor), width, 1).FirstOrDefault(), x, num + _d.L(10f), A(TechniqueArt.Dim));
		int x2 = _d.X(564f);
		double? num2 = JetDObjet.Qualite(itemDescriptor, annonce.Properties);
		if (num2.HasValue)
		{
			double valueOrDefault = num2.GetValueOrDefault();
			Color color = MarketWindow.RollColor(valueOrDefault);
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(14f), Strings.Market.RollPercent.ToString(MarketWindow.RollPercent(valueOrDefault)), x2, num - _d.L(8f), A(color));
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(9f), MotJet(valueOrDefault).ToUpperInvariant(), x2, num + _d.L(10f), _d.Em(0.14f, 9f), A(color));
		}
		else
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), "—", x2, num, A(Eteint));
		}
		int x3 = _d.X(668f);
		int width2 = _d.L(84f);
		if (mine)
		{
			var (text, color2) = Etat(annonce);
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), text, width2, 1).FirstOrDefault(), x3, num - ((annonce.PendingProceeds > 0) ? _d.L(8f) : 0), A(color2));
			if (annonce.PendingProceeds > 0)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(11f), Strings.Market.Pending.ToString(MarketWindow.FormatAmount(annonce.PendingProceeds)), x3, num + _d.L(10f), A(TechniqueArt.Gold));
			}
		}
		else
		{
			string text2 = (annonce.IsMine ? Strings.Hdv.AToi.ToString() : annonce.SellerName);
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), text2, width2, 1).FirstOrDefault(), x3, num, A(annonce.IsMine ? TechniqueArt.Ki : TechniqueArt.Soft));
		}
		Prix(renderer, annonce.UnitPrice, rect.Right - _d.L(14f), num, Pt(17f), _d.L(16f));
		Guid id = annonce.Id;
		AddHit(rect, cle, mine ? ((Action)delegate
		{
			_venteChoisie = id;
		}) : ((Action)delegate
		{
			_annonceChoisie = id;
		}));
	}

	private int Prix(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long montant, int droite, int milieu, int taille, int piece)
	{
		HouseArt.Zeni(renderer, new Rectangle(droite - piece, milieu - piece / 2, piece, piece), 0, _fade);
		int num = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, taille, MarketWindow.FormatAmount(montant), droite - piece - _d.L(6f), milieu, A(TechniqueArt.Gold));
		return droite - piece - _d.L(6f) - num;
	}

	private void CaseObjet(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, ItemDescriptor? objet, int quantite, ItemProperties? proprietes, bool choisie, bool eteinte, bool chaude = false)
	{
		TechniqueArt.Fill(renderer, rect, A(Case));
		TechniqueArt.Outline(renderer, rect, A(choisie ? TechniqueArt.Ki : (chaude ? TechniqueArt.Soft : TechniqueArt.Line2)));
		if (choisie)
		{
			TechniqueArt.Outline(renderer, new Rectangle(rect.X - 1, rect.Y - 1, rect.Width + 2, rect.Height + 2), A(TechniqueArt.Ki, 0.5f));
		}
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
			SaiyanHud.Draw(renderer, gameTexture, rect.X + (rect.Width - num) / 2, rect.Y + (rect.Height - num) / 2, num, num, A(eteinte ? Eteint : TechniqueArt.White, eteinte ? 0.55f : 1f));
		}
		int num2 = JetDObjet.Rarete(objet, proprietes);
		if (num2 > 0)
		{
			TechniqueArt.Fill(renderer, new Rectangle(rect.X + 2, rect.Bottom - 2 - Math.Max(1, _d.L(2f)), rect.Width - 4, Math.Max(1, _d.L(2f))), A(PersonnageScreen.CouleurRarete(num2), eteinte ? 0.5f : 1f));
		}
		if (quantite > 1)
		{
			string text = MarketWindow.FormatAmount(quantite);
			int num3 = rect.Right - _d.L(3f);
			int num4 = rect.Bottom - _d.L(9f);
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(12f), text, num3 + 1, num4 + 1, A(TechniqueArt.Black, 0.9f));
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(12f), text, num3, num4, A(TechniqueArt.White));
		}
		if (eteinte)
		{
			int num5 = _d.L(12f);
			Lisser("inv_cadenas.png", new Rectangle(rect.Right - num5 - _d.L(3f), rect.Y + _d.L(3f), num5, num5), A(TechniqueArt.Soft));
		}
	}

	private void DessinerAccueil(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		Rectangle rect = _d.R(264f, 122f, 608f, 596f);
		TechniqueArt.Glow(renderer, (float)rect.X + (float)rect.Width / 2f, (float)rect.Y + (float)rect.Height * 0.38f, _d.L(220f), A(TechniqueArt.Ki, 0.1f));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
		int num = rect.X + rect.Width / 2;
		int num2 = _d.L(84f);
		int num3 = rect.Y + (int)((float)rect.Height * 0.24f);
		Lisser("saiyan_carte_icone_echange.png", new Rectangle(num - num2 / 2, num3, num2, num2), A(new Color(255, 255, 172, 82)));
		int num4 = num3 + num2 + _d.L(30f);
		TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(25f), Strings.Hdv.VideTitre.ToString().ToUpperInvariant(), num, num4, A(TechniqueArt.Text));
		num4 += _d.L(34f);
		foreach (string item in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(13.5f), Strings.Hdv.VideTexte, _d.L(430f), 3))
		{
			TechniqueArt.TextCenter(renderer, TechniqueArt.Body, Pt(13.5f), item, num, num4, A(TechniqueArt.Soft));
			num4 += _d.L(22f);
		}
		Rectangle rect2 = new Rectangle(num - _d.L(140f), num4 + _d.L(16f), _d.L(280f), _d.L(50f));
		HouseArt.Cta(renderer, _d, rect2, Strings.Hdv.VideBouton, enabled: true, IsHot("accueil:vendre"), _fade);
		AddHit(rect2, "accueil:vendre", delegate
		{
			ChoisirMode(Mode.Vendre);
		});
	}

	private void DessinerFicheAnnonce(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		if (_repondu && _total == 0 && _rechercheEnvoyee.Length == 0 && _categorie == MarketCategory.All)
		{
			DessinerCommentCaMarche(renderer);
			return;
		}
		MarketListingInfo marketListingInfo = _annonces.FirstOrDefault((MarketListingInfo annonce) => annonce.Id == _annonceChoisie);
		if (marketListingInfo == null)
		{
			Conseil(renderer, Strings.Hdv.ChoisisAnnonce);
		}
		else
		{
			DessinerObjet(renderer, marketListingInfo, now, mine: false);
		}
	}

	private void Conseil(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string texte)
	{
		float num = 98f;
		foreach (string item in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), texte, _d.L(292f), 6))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), item, _d.X(916f), _d.Y(num), A(TechniqueArt.Soft));
			num += 18f;
		}
	}

	private void DessinerCommentCaMarche(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		float num = 916f;
		int width = _d.L(246f);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(20f), Strings.Hdv.CommentTitre.ToString().ToUpperInvariant(), _d.X(num), _d.Y(92f), A(TechniqueArt.Text));
		float num2 = 120f;
		int num3 = Math.Max(1, (int)Math.Round((double)(Options.Instance?.Market?.ListingDurationHours ?? 72) / 24.0));
		(string, string)[] array = new(string, string)[3]
		{
			(Strings.Hdv.Etape1Titre, Strings.Hdv.Etape1),
			(Strings.Hdv.Etape2Titre.ToString(num3), Strings.Hdv.Etape2.ToString(_taxe)),
			(Strings.Hdv.Etape3Titre, Strings.Hdv.Etape3)
		};
		for (int i = 0; i < array.Length; i++)
		{
			Rectangle rect = _d.R(num, num2, 32f, 32f);
			TechniqueArt.Fill(renderer, rect, A(Case));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line2));
			TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(18f), $"{i + 1}", rect.X + rect.Width / 2, rect.Y + rect.Height / 2, A(TechniqueArt.Ki));
			int x = _d.X(num + 46f);
			float num4 = num2 + 8f;
			foreach (string item in TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(12.5f), array[i].Item1, width, 2))
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(12.5f), item, x, _d.Y(num4), A(TechniqueArt.Text));
				num4 += 18f;
			}
			foreach (string item2 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), array[i].Item2, width, 4))
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), item2, x, _d.Y(num4), A(TechniqueArt.Soft));
				num4 += 18f;
			}
			num2 = Math.Max(num2 + 44f, num4 + 12f);
		}
	}

	private void DessinerObjet(Intersect.Client.Framework.Gwen.Renderer.Base renderer, MarketListingInfo annonce, long now, bool mine)
	{
		ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(annonce.ItemId);
		int num = JetDObjet.Rarete(itemDescriptor, annonce.Properties);
		Color color = TechniqueArt.Mix(PersonnageScreen.CouleurRarete(num), TechniqueArt.White, 0.55f);
		Vitrine(renderer, itemDescriptor, annonce.Properties, num, color, 150f);
		int x = _d.X(916f);
		int width = _d.L(292f);
		float num2 = 246f;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(22f), TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(22f), itemDescriptor?.Name ?? "?", width, 1).FirstOrDefault(), x, _d.Y(num2), A(TechniqueArt.Gold));
		num2 += 22f;
		if (itemDescriptor != null)
		{
			string text = ((num > 0) ? (FenetreInventaire.SorteDe(itemDescriptor) + " · " + PersonnageScreen.NomRarete(num)) : FenetreInventaire.SorteDe(itemDescriptor));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), text, width, 1).FirstOrDefault(), x, _d.Y(num2), A((num > 0) ? color : TechniqueArt.Soft));
			num2 += 22f;
			foreach (string item in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), itemDescriptor.Description, width, 3))
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), item, x, _d.Y(num2), A(TechniqueArt.Soft));
				num2 += 16f;
			}
			num2 += 8f;
			string text2 = Bonus(itemDescriptor, annonce.Properties);
			if (text2.Length > 0)
			{
				num2 = Valeur(renderer, Strings.Hdv.Bonus, text2, TechniqueArt.Text, num2, 2);
			}
			double? num3 = JetDObjet.Qualite(itemDescriptor, annonce.Properties);
			if (num3.HasValue)
			{
				double valueOrDefault = num3.GetValueOrDefault();
				num2 = Valeur(renderer, Strings.Market.ColumnRoll, Strings.Market.RollPercent.ToString(MarketWindow.RollPercent(valueOrDefault)) + " · " + MotJet(valueOrDefault), MarketWindow.RollColor(valueOrDefault), num2, 1);
			}
			int num4 = FenetreInventaire.NiveauRequis(itemDescriptor);
			if (num4 > 1)
			{
				num2 = Valeur(renderer, Strings.Hdv.NiveauRequis, $"{num4}", ((Globals.Me?.Level ?? 0) < num4) ? TechniqueArt.Red : TechniqueArt.Text, num2, 1);
			}
		}
		string valeur = (mine ? Etat(annonce).Texte : ((annonce.IsMine ? Strings.Hdv.AToi.ToString() : annonce.SellerName) + " · " + Strings.Hdv.Encore.ToString(TempsRestant(annonce.ExpiresAt))));
		Valeur(renderer, mine ? Strings.Market.ColumnState : Strings.Market.ColumnSeller, valeur, mine ? Etat(annonce).Couleur : TechniqueArt.Text, num2, 2);
		Rectangle rect = _d.R(912f, 674f, 300f, 46f);
		int num5 = _d.Y(640f);
		TechniqueArt.Fill(renderer, _d.R(912f, 612f, 300f, 1f), A(TechniqueArt.Line));
		int num6 = Prix(renderer, annonce.UnitPrice, _d.X(916f) + TechniqueArt.Measure(renderer, TechniqueArt.Heavy, Pt(28f), MarketWindow.FormatAmount(annonce.UnitPrice)).X + _d.L(6f) + _d.L(22f), num5, Pt(28f), _d.L(22f));
		if (annonce.Quantity > 1)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11f), $"{Strings.Hdv.Unite} · {Strings.Hdv.Lot.ToString(MarketWindow.FormatAmount(annonce.Quantity))}", _d.X(916f), num5 + _d.L(22f), A(TechniqueArt.Dim));
		}
		if (mine)
		{
			bool flag = annonce.State != MarketListingState.Sold && annonce.Quantity > 0;
			HouseArt.Hollow(renderer, _d, rect, Strings.Market.WithdrawTitle, flag && IsHot("retirer"), flag ? _fade : (_fade * 0.45f), flag ? Rouge : null);
			AddHit(rect, "retirer", flag ? ((Action)delegate
			{
				DemanderRetrait(annonce);
			}) : null);
			return;
		}
		long num7 = Globals.Me?.Zeni ?? 0;
		long num8 = num7 - annonce.UnitPrice;
		int right = _d.X(1208f);
		TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(11f), ((num8 >= 0) ? Strings.Hdv.IlTeRestera : Strings.Hdv.IlTeManque).ToString(), right, num5 - _d.L(8f), A(TechniqueArt.Dim));
		TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(12f), MarketWindow.Money(Math.Abs(num8)), right, num5 + _d.L(9f), A((num8 >= 0) ? TechniqueArt.Soft : TechniqueArt.Red));
		bool flag2 = !annonce.IsMine && num8 >= 0;
		HouseArt.Cta(renderer, _d, rect, Strings.Market.Buy, flag2, flag2 && IsHot("acheter"), _fade);
		AddHit(rect, "acheter", flag2 ? ((Action)delegate
		{
			DemanderAchat(annonce);
		}) : null);
	}

	private void Vitrine(Intersect.Client.Framework.Gwen.Renderer.Base renderer, ItemDescriptor? objet, ItemProperties? proprietes, int rarete, Color couleurRarete, float hauteur)
	{
		Rectangle rect = _d.R(910f, 70f, 304f, hauteur);
		TechniqueArt.FillVertical(renderer, rect, A(new Color(255, 10, 16, 28)), A(new Color(255, 13, 20, 34)));
		TechniqueArt.Glow(renderer, (float)rect.X + (float)rect.Width / 2f, (float)rect.Y + (float)rect.Height * 0.55f, _d.L(90f), A(PersonnageScreen.CouleurRarete(rarete), 0.22f));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
		if (objet != null)
		{
			IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Item, objet.Icon);
			if (gameTexture != null)
			{
				int num = Math.Max(2, _d.L(hauteur * 0.62f) / Math.Max(1, gameTexture.Width));
				int num2 = gameTexture.Width * num;
				SaiyanHud.Draw(renderer, gameTexture, rect.X + (rect.Width - num2) / 2, rect.Y + (rect.Height - num2) / 2, num2, num2, A(TechniqueArt.White));
			}
		}
		if (rarete > 0)
		{
			string text = PersonnageScreen.NomRarete(rarete).ToUpperInvariant();
			int w = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), text, _d.Em(0.12f, 10f)) + _d.L(14f);
			Rectangle rect2 = new Rectangle(rect.X + _d.L(10f), rect.Y + _d.L(10f), w, _d.L(20f));
			TechniqueArt.Fill(renderer, rect2, A(couleurRarete));
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(10f), text, rect2.X + rect2.Width / 2, rect2.Y + rect2.Height / 2, _d.Em(0.12f, 10f), A(HouseArt.Dark));
		}
	}

	private float Valeur(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string intitule, string valeur, Color couleur, float y, int lignes)
	{
		int x = _d.X(916f);
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(9.5f), intitule.ToUpperInvariant(), x, _d.Y(y), _d.Em(0.16f, 9.5f), A(TechniqueArt.Dim));
		y += 16f;
		foreach (string item in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), valeur, _d.L(292f), lignes))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), item, x, _d.Y(y), A(couleur));
			y += 18f;
		}
		return y + 8f;
	}

	private static string Bonus(ItemDescriptor objet, ItemProperties? proprietes)
	{
		List<string> list = new List<string>();
		int[] array = proprietes?.StatModifiers;
		Dictionary<Stat, (Stat Stat, int Bas, int Haut)> dictionary = JetDObjet.Fourchettes(objet).ToDictionary(((Stat Stat, int Bas, int Haut) entree) => entree.Stat);
		for (int num = 0; num < Enum.GetValues<Stat>().Length; num++)
		{
			Stat key = (Stat)num;
			int num2 = ((num < objet.StatsGiven.Length) ? objet.StatsGiven[num] : 0) + ((array != null && num < array.Length) ? array[num] : 0);
			int num3 = ((num < objet.PercentageStatsGiven.Length) ? objet.PercentageStatsGiven[num] : 0);
			if (num2 != 0 || num3 != 0)
			{
				LocalizedString value;
				string text = (Strings.ItemDescription.Stats.TryGetValue(num, out value) ? value.ToString() : key.ToString());
				string text2 = ((num2 != 0) ? $"{text} {((num2 > 0) ? "+" : string.Empty)}{num2}" : text);
				if (num3 != 0)
				{
					text2 += $" {((num3 > 0) ? "+" : string.Empty)}{num3} %";
				}
				if (array != null && dictionary.TryGetValue(key, out var value2))
				{
					int num4 = ((num < objet.StatsGiven.Length) ? objet.StatsGiven[num] : 0);
					text2 = text2 + " (" + Strings.ItemDescription.RolledRange.ToString(num4 + value2.Item2, num4 + value2.Item3) + ")";
				}
				list.Add(text2);
			}
		}
		for (int num5 = 0; num5 < objet.VitalsGiven.Length; num5++)
		{
			if (objet.VitalsGiven[num5] != 0L)
			{
				string value3 = ((num5 == 0) ? Strings.Inventaire.MaxHealth : Strings.Inventaire.MaxKi).ToString();
				list.Add($"{value3} {((objet.VitalsGiven[num5] > 0) ? "+" : string.Empty)}{objet.VitalsGiven[num5]}");
			}
		}
		return string.Join(" · ", list);
	}

	private void DessinerSac(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		Titre(renderer, 72f, Strings.Hdv.TonSac, Strings.Hdv.TonSacComplement);
		BoutonTri(renderer, _d.R(676f, 73f, 196f, 32f), "tri-sac", NomTriSac(_triSac), delegate
		{
			_triSac = TrisSac[(Array.IndexOf(TrisSac, _triSac) + 1) % TrisSac.Length];
		});
		IItem[] sac = Globals.Me?.Inventory;
		if (sac == null)
		{
			return;
		}
		List<(int Index, IItem Item, ItemDescriptor Objet)> list = (from index in Enumerable.Range(0, sac.Length)
			select (Index: index, Item: sac[index], Objet: (sac[index] == null || sac[index].ItemId == Guid.Empty) ? null : DatabaseObject<ItemDescriptor>.Get(sac[index].ItemId))).ToList();
		if (_triSac != TriSac.Aucun)
		{
			List<(int Index, IItem Item, ItemDescriptor Objet)> source = list.Where<(int Index, IItem Item, ItemDescriptor Objet)>(((int Index, IItem Item, ItemDescriptor Objet) entree) => entree.Objet != null).ToList();
			StringComparer currentCultureIgnoreCase = StringComparer.CurrentCultureIgnoreCase;
			list = (_triSac switch
			{
				TriSac.Valeur => source.OrderByDescending<(int, IItem, ItemDescriptor), long>(((int Index, IItem Item, ItemDescriptor Objet) entree) => ValeurConseillee(entree.Objet, entree.Item.ItemProperties)).ThenBy<(int, IItem, ItemDescriptor), string>(((int Index, IItem Item, ItemDescriptor Objet) entree) => entree.Objet.Name, currentCultureIgnoreCase), 
				TriSac.Rarete => from entree in source
					orderby JetDObjet.Rarete(entree.Objet, entree.Item.ItemProperties) descending, ValeurConseillee(entree.Objet, entree.Item.ItemProperties) descending
					select entree, 
				TriSac.Type => source.OrderBy<(int, IItem, ItemDescriptor), int>(((int Index, IItem Item, ItemDescriptor Objet) entree) => RangType(entree.Objet)).ThenBy<(int, IItem, ItemDescriptor), string>(((int Index, IItem Item, ItemDescriptor Objet) entree) => FenetreInventaire.SorteDe(entree.Objet), currentCultureIgnoreCase).ThenBy<(int, IItem, ItemDescriptor), string>(((int Index, IItem Item, ItemDescriptor Objet) entree) => entree.Objet.Name, currentCultureIgnoreCase), 
				TriSac.Nom => source.OrderBy<(int, IItem, ItemDescriptor), string>(((int Index, IItem Item, ItemDescriptor Objet) entree) => entree.Objet.Name, currentCultureIgnoreCase), 
				_ => source.OrderByDescending<(int, IItem, ItemDescriptor), int>(((int Index, IItem Item, ItemDescriptor Objet) entree) => entree.Item.Quantity).ThenBy<(int, IItem, ItemDescriptor), string>(((int Index, IItem Item, ItemDescriptor Objet) entree) => entree.Objet.Name, currentCultureIgnoreCase), 
			}).ToList();
		}
		float num = 116f;
		float num2 = 0f;
		for (int num3 = 0; num3 < list.Count; num3++)
		{
			int num4 = num3 / 10;
			int num5 = num3 % 10;
			float num6 = num + (float)num4 * 60f;
			if (num6 + 54f > 654f)
			{
				break;
			}
			num2 = num6 + 54f;
			Rectangle rect = _d.R(264f + (float)num5 * 60f, num6, 54f, 54f);
			var (num7, item, itemDescriptor) = list[num3];
			if (itemDescriptor == null)
			{
				TechniqueArt.Fill(renderer, rect, A(new Color(255, 9, 13, 22)));
				TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
				continue;
			}
			string blocage = Blocage(num7, item, itemDescriptor);
			string cle = $"sac:{num7}";
			CaseObjet(renderer, rect, itemDescriptor, item.Quantity, item.ItemProperties, num7 == _caseChoisie, blocage != null, IsHot(cle));
			int caseIndex = num7;
			ItemDescriptor objetIndex = itemDescriptor;
			AddHit(rect, cle, delegate
			{
				if (blocage != null)
				{
					Message(blocage, Rouge);
				}
				else
				{
					ChoisirCase(caseIndex, item, objetIndex);
				}
			});
		}
		foreach (string item2 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), Strings.Hdv.Grises, _d.L(608f), 2))
		{
			num2 += 22f;
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), item2, _d.X(264f), _d.Y(num2), A(TechniqueArt.Dim));
		}
	}

	private static string? Blocage(int index, IItem item, ItemDescriptor objet)
	{
		Player? me = Globals.Me;
		if (me != null && me.MyEquipment.Contains(index))
		{
			return Strings.Market.LockedEquipped;
		}
		bool flag = !objet.CanTrade;
		bool flag2 = flag;
		if (!flag2)
		{
			ItemType itemType = objet.ItemType;
			bool flag3 = ((itemType == ItemType.Currency || itemType == ItemType.Bag) ? true : false);
			flag2 = flag3;
		}
		if (!flag2 && !(objet.Id == MarketWindow.KnownCurrencyId))
		{
			Guid? bagId = item.BagId;
			if (bagId.HasValue)
			{
				Guid valueOrDefault = bagId.GetValueOrDefault();
				if (valueOrDefault != Guid.Empty)
				{
					goto IL_0088;
				}
			}
			return null;
		}
		goto IL_0088;
		IL_0088:
		return Strings.Market.LockedNotTradable;
	}

	private void ChoisirCase(int index, IItem item, ItemDescriptor objet)
	{
		_caseChoisie = index;
		_objetChoisi = item.ItemId;
		_quantite.SetValue((!objet.Stackable) ? 1 : item.Quantity, skipEvents: true);
		_prix.SetValue(ValeurConseillee(objet, item.ItemProperties), skipEvents: true);
		Message(string.Empty, TechniqueArt.Dim);
		DemanderIndices();
	}

	private bool Choisi(out IItem item, out ItemDescriptor? objet)
	{
		item = null;
		objet = null;
		IItem[] array = Globals.Me?.Inventory;
		if (array == null || _caseChoisie < 0 || _caseChoisie >= array.Length)
		{
			return false;
		}
		item = array[_caseChoisie];
		if (item != null && !(item.ItemId != _objetChoisi))
		{
			ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(item.ItemId);
			if (itemDescriptor != null)
			{
				objet = itemDescriptor;
				return true;
			}
		}
		return false;
	}

	private (int Quantite, long Prix) Saisie(IItem item, ItemDescriptor objet)
	{
		int item2 = ((!objet.Stackable) ? 1 : ((int)Math.Clamp(Math.Floor(_quantite.Value), 0.0, item.Quantity)));
		long item3 = (long)Math.Max(0.0, Math.Floor(_prix.Value));
		return (Quantite: item2, Prix: item3);
	}

	private void DessinerFormulaire(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		if (!Choisi(out IItem item, out ItemDescriptor objet) || objet == null)
		{
			Conseil(renderer, Strings.Market.SellHint);
			Pied(renderer);
			return;
		}
		int rarete = JetDObjet.Rarete(objet, item.ItemProperties);
		Color color = TechniqueArt.Mix(PersonnageScreen.CouleurRarete(rarete), TechniqueArt.White, 0.55f);
		Vitrine(renderer, objet, item.ItemProperties, rarete, color, 112f);
		int x = _d.X(916f);
		int width = _d.L(292f);
		float num = 206f;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(21f), TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(21f), objet.Name, width, 1).FirstOrDefault(), x, _d.Y(num), A(TechniqueArt.Gold));
		num += 21f;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), FenetreInventaire.SorteDe(objet) + " · " + Strings.Market.Owned.ToString(MarketWindow.FormatAmount(item.Quantity)), width, 1).FirstOrDefault(), x, _d.Y(num), A(color));
		double? num2 = JetDObjet.Qualite(objet, item.ItemProperties);
		if (num2.HasValue)
		{
			double valueOrDefault = num2.GetValueOrDefault();
			num += 19f;
			string text = $"{Strings.Market.ColumnRoll} {Strings.Market.RollPercent.ToString(MarketWindow.RollPercent(valueOrDefault))} · {MotJet(valueOrDefault)}";
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), text, x, _d.Y(num), A(MarketWindow.RollColor(valueOrDefault)));
		}
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(9.5f), Strings.Market.Quantity.ToString().ToUpperInvariant(), x, _d.Y(301f), _d.Em(0.16f, 9.5f), A(TechniqueArt.Dim));
		float y = 312f;
		bool stackable = objet.Stackable;
		(int Quantite, long Prix) tuple = Saisie(item, objet);
		int quantite = tuple.Item1;
		long item2 = tuple.Item2;
		Rectangle rect = _d.R(914f, y, 34f, 34f);
		Rectangle rect2 = _d.R(954f, y, 156f, 34f);
		Rectangle rect3 = _d.R(1116f, y, 34f, 34f);
		Rectangle rect4 = _d.R(1156f, y, 54f, 34f);
		PetitBouton(renderer, rect, "-", "quantite:moins", stackable && quantite > 1, delegate
		{
			_quantite.SetValue(Math.Max(1, quantite - 1), skipEvents: true);
		});
		TechniqueArt.Fill(renderer, rect2, A(new Color(255, 7, 10, 18)));
		TechniqueArt.Outline(renderer, rect2, A((InputHandler.KeyboardFocus == _quantite) ? TechniqueArt.Ki : TechniqueArt.Line2));
		if (!stackable)
		{
			TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(16f), "1", rect2.X + rect2.Width / 2, rect2.Y + rect2.Height / 2, A(TechniqueArt.Text));
		}
		PetitBouton(renderer, rect3, "+", "quantite:plus", stackable && quantite < item.Quantity, delegate
		{
			_quantite.SetValue(Math.Min(item.Quantity, quantite + 1), skipEvents: true);
		});
		PetitBouton(renderer, rect4, Strings.Hdv.Max.ToString().ToUpperInvariant(), "quantite:max", stackable && quantite < item.Quantity, delegate
		{
			_quantite.SetValue(item.Quantity, skipEvents: true);
		});
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(9.5f), Strings.Market.UnitPriceLabel.ToString().ToUpperInvariant(), x, _d.Y(365f), _d.Em(0.16f, 9.5f), A(TechniqueArt.Dim));
		Rectangle rect5 = _d.R(914f, 376f, 296f, 36f);
		TechniqueArt.Fill(renderer, rect5, A(new Color(255, 7, 10, 18)));
		TechniqueArt.Outline(renderer, rect5, A((InputHandler.KeyboardFocus == _prix) ? TechniqueArt.Ki : TechniqueArt.Line2));
		int num3 = _d.L(18f);
		HouseArt.Zeni(renderer, new Rectangle(rect5.Right - _d.L(12f) - num3, rect5.Y + (rect5.Height - num3) / 2, num3, num3), ImagePiece(now), _fade);
		float y2 = 424f;
		Indice(renderer, ref y2, "indice:conseillee", Strings.Hdv.ValeurConseillee, ValeurConseillee(objet, item.ItemProperties), moinsUn: false);
		Indice(renderer, ref y2, "indice:moinscher", Strings.Market.HintCheapest, (_indices == null) ? ((long?)null) : new long?(_indices.Cheapest), moinsUn: true);
		Indice(renderer, ref y2, "indice:courant", Strings.Hdv.PrixCourant, (_indices == null) ? ((long?)null) : new long?(_indices.Median), moinsUn: false);
		if (JetDObjet.ADesJets(objet))
		{
			Indice(renderer, ref y2, "indice:jet", Strings.Market.HintSameRoll, (_indices == null) ? ((long?)null) : new long?(_indices.CheapestSameRollOrBetter), moinsUn: true);
		}
		long num4 = quantite * item2;
		long num5 = num4 * _taxe / 100;
		float num6 = 570f;
		TechniqueArt.Fill(renderer, _d.R(912f, num6 - 12f, 300f, 1f), A(TechniqueArt.Line));
		int num7 = _d.X(1208f);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), Strings.Market.Total, x, _d.Y(num6), A(TechniqueArt.Soft));
		TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(12.5f), MarketWindow.FormatAmount(num4), num7, _d.Y(num6), A(TechniqueArt.Text));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), $"{Strings.Market.TaxLine.ToString(_taxe)}, {Strings.Market.TaxHint}", x, _d.Y(num6 + 20f), A(TechniqueArt.Soft));
		TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(12.5f), (num5 > 0) ? ("- " + MarketWindow.FormatAmount(num5)) : "0", num7, _d.Y(num6 + 20f), A(TechniqueArt.Text));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), Strings.Market.SellNetLabel, x, _d.Y(num6 + 46f), A(TechniqueArt.Text));
		Prix(renderer, num4 - num5, num7, _d.Y(num6 + 46f), Pt(24f), _d.L(20f));
		if (_message.Length > 0)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), _message, width, 1).FirstOrDefault(), x, _d.Y(660f), A(_couleurMessage));
		}
		Rectangle rect6 = _d.R(912f, 674f, 300f, 46f);
		bool flag = quantite >= 1 && item2 >= 1;
		HouseArt.Cta(renderer, _d, rect6, Strings.Market.SellButton, flag, flag && IsHot("vendre"), _fade);
		AddHit(rect6, "vendre", flag ? new Action(MettreEnVente) : null);
	}

	private void Pied(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		if (_message.Length == 0)
		{
			return;
		}
		foreach (var item in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), _message, _d.L(292f), 3).Select((string texte, int rang) => (texte: texte, rang: rang)))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), item.texte, _d.X(916f), _d.Y(178f + (float)(item.rang * 18)), A(_couleurMessage));
		}
	}

	private void PetitBouton(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, string texte, string cle, bool possible, Action action)
	{
		bool flag = possible && IsHot(cle);
		TechniqueArt.Fill(renderer, rect, A(new Color(255, 7, 10, 18)));
		TechniqueArt.Outline(renderer, rect, A(flag ? TechniqueArt.Soft : TechniqueArt.Line2));
		TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt((texte.Length > 1) ? 10.5f : 15f), texte, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, A((!possible) ? Eteint : (flag ? TechniqueArt.Text : TechniqueArt.Soft)));
		AddHit(rect, cle, possible ? action : null);
	}

	private void Indice(Intersect.Client.Framework.Gwen.Renderer.Base renderer, ref float y, string cle, string intitule, long? montant, bool moinsUn)
	{
		Rectangle rect = _d.R(914f, y, 296f, 28f);
		bool flag = montant > 0;
		bool flag2 = flag && IsHot(cle);
		TechniqueArt.Fill(renderer, rect, A(new Color(255, 7, 10, 18)));
		TechniqueArt.Outline(renderer, rect, A(flag2 ? TechniqueArt.Soft : TechniqueArt.Line));
		int centerY = rect.Y + rect.Height / 2;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), intitule, rect.X + _d.L(10f), centerY, A(flag2 ? new Color(255, 255, 172, 82) : TechniqueArt.Soft));
		string text;
		if (montant.HasValue)
		{
			long valueOrDefault = montant.GetValueOrDefault();
			text = ((valueOrDefault <= 0) ? Strings.Market.HintNone.ToString() : MarketWindow.FormatAmount(montant.Value));
		}
		else
		{
			text = "...";
		}
		string text2 = text;
		TechniqueArt.TextRight(renderer, flag ? TechniqueArt.Heavy : TechniqueArt.Body, Pt(flag ? 14f : 11.5f), text2, rect.Right - _d.L(10f), centerY, A(flag ? TechniqueArt.Gold : TechniqueArt.Dim));
		if (flag)
		{
			long proposition = (moinsUn ? Math.Max(1L, montant.Value - 1) : montant.Value);
			AddHit(rect, cle, delegate
			{
				_prix.SetValue(proposition, skipEvents: true);
			});
		}
		y += 32f;
	}

	private void MettreEnVente()
	{
		if (Choisi(out IItem item, out ItemDescriptor objet) && objet != null)
		{
			var (num, num2) = Saisie(item, objet);
			if (num < 1)
			{
				Message(Strings.Market.ResultInvalidQuantity, Rouge);
				return;
			}
			if (num2 < 1)
			{
				Message(Strings.Market.ResultInvalidPrice, Rouge);
				return;
			}
			PacketSender.SendMarketSell(_caseChoisie, num, num2);
			Message(Strings.Market.Selling, TechniqueArt.Dim);
		}
	}

	private void DessinerMesVentes(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		long num = _miennes.Sum((MarketListingInfo annonce) => annonce.PendingProceeds);
		int num2 = _miennes.Count((MarketListingInfo annonce) => annonce.State == MarketListingState.Active && annonce.Quantity > 0);
		string text = Strings.Market.ListingsCount.ToString(num2, _maxAnnonces);
		if (num > 0)
		{
			text = text + " · " + Strings.Market.PendingTotal.ToString(MarketWindow.FormatAmount(num));
		}
		Titre(renderer, 72f, Strings.Market.MineHeading, text);
		float num3 = 112f;
		float num4 = num3 + 20f + 4f;
		Colonnes(renderer, Strings.Market.ColumnRoll, Strings.Market.ColumnState, Strings.Hdv.ColonnePrix, num3);
		if (_miennes.Length == 0)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), Strings.Market.MineEmpty, _d.X(276f), _d.Y(num4 + 20f), A(TechniqueArt.Dim));
			return;
		}
		_liste = _d.R(264f, num4, 608f, 676f - num4);
		_rangees = _miennes.Length;
		_visibles = Math.Max(1, (int)((676f - num4 + 4f) / 56f));
		_defilement = Math.Clamp(_defilement, 0, Math.Max(0, _rangees - _visibles));
		for (int num5 = 0; num5 < _visibles && _defilement + num5 < _miennes.Length; num5++)
		{
			LigneAnnonce(renderer, _miennes[_defilement + num5], num4 + (float)num5 * 56f, mine: true);
		}
		if (_rangees > _visibles)
		{
			Rectangle rect = new Rectangle(_liste.Right + _d.L(5f), _liste.Y, _d.L(3f), _liste.Height);
			TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, 0.05f));
			int num6 = Math.Max(_d.L(24f), rect.Height * _visibles / _rangees);
			int y = rect.Y + (rect.Height - num6) * _defilement / Math.Max(1, _rangees - _visibles);
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, y, rect.Width, num6), A(TechniqueArt.Line2));
		}
		if (_message.Length > 0)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), _message, _d.L(608f), 1).FirstOrDefault(), _d.X(264f), _d.Y(701f), A(_couleurMessage));
		}
	}

	private void DessinerFicheVente(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		MarketListingInfo marketListingInfo = _miennes.FirstOrDefault((MarketListingInfo annonce) => annonce.Id == _venteChoisie);
		if (marketListingInfo == null)
		{
			DessinerCommentCaMarche(renderer);
		}
		else
		{
			DessinerObjet(renderer, marketListingInfo, now, mine: true);
		}
	}

	private (string Texte, Color Couleur) Etat(MarketListingInfo annonce)
	{
		return annonce.State switch
		{
			MarketListingState.Active => (Texte: Strings.Market.StateActive.ToString(MarketWindow.FormatAmount(annonce.Quantity), TempsRestant(annonce.ExpiresAt)), Couleur: TechniqueArt.Soft), 
			MarketListingState.Expired => (Texte: Strings.Market.StateExpired.ToString(MarketWindow.FormatAmount(annonce.Quantity)), Couleur: Rouge), 
			_ => (Texte: Strings.Market.StateSold.ToString(), Couleur: TechniqueArt.Green), 
		};
	}

	private void DemanderAchat(MarketListingInfo annonce)
	{
		string heading = DatabaseObject<ItemDescriptor>.Get(annonce.ItemId)?.Name ?? "?";
		if (annonce.Quantity > 1 && DatabaseObject<ItemDescriptor>.TryGet(annonce.ItemId, out var databaseObject))
		{
			ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(MarketWindow.KnownCurrencyId);
			int num = (int)((annonce.UnitPrice > 0) ? Math.Min(2147483647L, (Globals.Me?.Zeni ?? 0) / annonce.UnitPrice) : annonce.Quantity);
			int num2 = PlaceDansLeSac(databaseObject);
			int num3 = Math.Min(annonce.Quantity, Math.Min(num, num2));
			string limitHint = null;
			if (num < annonce.Quantity && num <= num2)
			{
				limitHint = Strings.Quantity.LimitedBy.ToString(itemDescriptor?.Name ?? string.Empty);
			}
			else if (num2 < annonce.Quantity && num2 < num)
			{
				limitHint = Strings.Quantity.LimitedByBag;
			}
			if (num3 < 1)
			{
				PacketSender.SendMarketBuy(annonce.Id, 1, annonce.UnitPrice);
				return;
			}
			new QuantityWindow(Strings.Market.BuyTitle, databaseObject, Strings.Market.Buy, 1, num3, delegate(int quantite)
			{
				PacketSender.SendMarketBuy(annonce.Id, quantite, annonce.UnitPrice);
			}, (itemDescriptor != null) ? new QuantityPricing(itemDescriptor, annonce.UnitPrice, Earning: false) : null, limitHint, () => !base.IsHidden);
		}
		else
		{
			ConfirmCardWindow.Open(new ConfirmCardRequest
			{
				Title = Strings.ConfirmCard.BuyTitle,
				Icon = "saiyan_carte_icone_echange.png",
				Heading = heading,
				Prompt = Strings.ConfirmCard.BuyPrompt,
				Chip = MarketWindow.Money(annonce.UnitPrice),
				ConfirmLabel = Strings.ConfirmCard.Buy,
				Emblem = IconeAnnonce(annonce)
			}, delegate
			{
				PacketSender.SendMarketBuy(annonce.Id, 1, annonce.UnitPrice);
			});
		}
	}

	private static void DemanderRetrait(MarketListingInfo annonce)
	{
		string heading = DatabaseObject<ItemDescriptor>.Get(annonce.ItemId)?.Name ?? "?";
		ConfirmCardRequest confirmCardRequest = new ConfirmCardRequest()
		{
			Title = Strings.ConfirmCard.WithdrawTitle,
			Icon = "saiyan_carte_icone_echange.png",
			Heading = heading,
			Prompt = Strings.ConfirmCard.WithdrawPrompt,
			Chip = Strings.ConfirmCard.Quantity.ToString(annonce.Quantity),
			ConfirmLabel = Strings.ConfirmCard.Withdraw,
			Emblem = IconeAnnonce(annonce),
		};
		ConfirmCardWindow.Open(confirmCardRequest, delegate
		{
			PacketSender.SendMarketCancel(annonce.Id);
		});
	}

	private static int PlaceDansLeSac(ItemDescriptor objet)
	{
		Player me = Globals.Me;
		if (me == null)
		{
			return 0;
		}
		int num = ((!objet.IsStackable) ? 1 : Math.Max(1, objet.MaxInventoryStack));
		int num2 = me.Inventory.Count((IItem slot) => slot.ItemId == default(Guid));
		int num3 = me.Inventory.Count((IItem slot) => slot.ItemId == objet.Id);
		long value = (long)num2 * (long)num + (long)num3 * (long)num - me.GetQuantityOfItemInInventory(objet.Id);
		return (int)Math.Clamp(value, 0L, 2147483647L);
	}

	private static IGameTexture? IconeAnnonce(MarketListingInfo annonce)
	{
		ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(annonce.ItemId);
		if (itemDescriptor == null)
		{
			return null;
		}
		return Globals.ContentManager.GetTexture(TextureType.Item, itemDescriptor.Icon);
	}

	internal static long ValeurConseillee(ItemDescriptor objet, ItemProperties? proprietes)
	{
		double num = Math.Max(1, objet.Price);
		double? num2 = JetDObjet.Qualite(objet, proprietes);
		if (num2.HasValue)
		{
			double valueOrDefault = num2.GetValueOrDefault();
			num *= 0.75 + 0.5 * valueOrDefault;
		}
		return Math.Max(1L, (long)Math.Round(num));
	}

	private static int RangType(ItemDescriptor objet)
	{
		return objet.ItemType switch
		{
			ItemType.Equipment => 0, 
			ItemType.Consumable => 1, 
			ItemType.Spell => 2, 
			ItemType.None => 3, 
			_ => 4, 
		};
	}

	private static string NomTriSac(TriSac tri)
	{
		return tri switch
		{
			TriSac.Valeur => Strings.Personnage.SortValue, 
			TriSac.Rarete => Strings.Personnage.SortRarity, 
			TriSac.Type => Strings.Personnage.SortType, 
			TriSac.Nom => Strings.Personnage.SortName, 
			TriSac.Quantite => Strings.Personnage.SortQuantity, 
			_ => Strings.Personnage.SortNone, 
		};
	}

	private static string Sorte(ItemDescriptor objet)
	{
		string text = FenetreInventaire.SorteDe(objet);
		int num = FenetreInventaire.NiveauRequis(objet);
		if (num <= 1)
		{
			return text;
		}
		return text + " · " + Strings.Hdv.NiveauCourt.ToString(num);
	}

	private static string MotJet(double qualite)
	{
		return MarketWindow.RollTier(qualite) switch
		{
			3 => Strings.Market.RollPerfect, 
			2 => Strings.Market.RollGood, 
			1 => Strings.Market.RollAverage, 
			_ => Strings.Market.RollWeak, 
		};
	}

	private static string NombreAnnonces(int nombre)
	{
		if (nombre != 1)
		{
			return Strings.Hdv.NAnnonces.ToString(MarketWindow.FormatAmount(nombre));
		}
		return Strings.Hdv.UneAnnonce.ToString();
	}

	private static string NomCategorie(MarketCategory categorie)
	{
		return categorie switch
		{
			MarketCategory.Equipment => Strings.Market.CategoryEquipment, 
			MarketCategory.Consumables => Strings.Market.CategoryConsumables, 
			MarketCategory.Resources => Strings.Market.CategoryResources, 
			_ => Strings.Market.CategoryAll, 
		};
	}

	private static string NomTri(MarketSort tri)
	{
		string text = tri switch
		{
			MarketSort.PriceDescending => Strings.Market.SortPriceDescending, 
			MarketSort.Newest => Strings.Market.SortNewest, 
			MarketSort.Name => Strings.Market.SortName, 
			MarketSort.BestRoll => Strings.Market.SortBestRoll, 
			_ => Strings.Market.SortPriceAscending, 
		};
		if (!string.IsNullOrEmpty(text))
		{
			ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(char.ToUpperInvariant(text[0]));
			string text2 = text;
			return string.Concat(readOnlySpan, text2.Substring(1, text2.Length - 1));
		}
		return text;
	}

	private static string OrdreTri(MarketSort tri)
	{
		return tri switch
		{
			MarketSort.PriceDescending => Strings.Hdv.OrdrePrixDecroissant, 
			MarketSort.Newest => Strings.Hdv.OrdreRecentes, 
			MarketSort.Name => Strings.Hdv.OrdreNom, 
			MarketSort.BestRoll => Strings.Hdv.OrdreJet, 
			_ => Strings.Hdv.OrdrePrixCroissant, 
		};
	}

	private static string TempsRestant(long fin)
	{
		TimeSpan timeSpan = DateTimeOffset.FromUnixTimeMilliseconds(fin) - DateTimeOffset.UtcNow;
		if (timeSpan <= TimeSpan.Zero)
		{
			return Strings.Market.TimeMinutes.ToString(0);
		}
		if (timeSpan.TotalDays >= 1.0)
		{
			return Strings.Market.TimeDays.ToString((int)timeSpan.TotalDays, timeSpan.Hours);
		}
		if (!(timeSpan.TotalHours >= 1.0))
		{
			return Strings.Market.TimeMinutes.ToString(Math.Max(1, timeSpan.Minutes));
		}
		return Strings.Market.TimeHours.ToString((int)timeSpan.TotalHours, timeSpan.Minutes);
	}
}
