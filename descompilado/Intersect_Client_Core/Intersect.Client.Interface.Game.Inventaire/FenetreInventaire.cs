using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Core.Sounds;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Input;
using Intersect.Client.Framework.Items;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.DescriptionWindows;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Personnage;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.Entities;
using Intersect.Framework.Core.GameObjects.Conditions;
using Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.Ornaments;
using Intersect.Framework.Core.GameObjects.Panoplies;
using Intersect.Framework.Core.GameObjects.PlayerClass;
using Intersect.Framework.Core.GameObjects.Titles;
using Intersect.Localization;
using Intersect.Models;

namespace Intersect.Client.Interface.Game.Inventaire;

internal sealed class FenetreInventaire : FenetreCharte
{
	private readonly record struct Parure(Guid Id, string Nom, Color Couleur, string? Description, string Dossier, bool Debloque, bool Porte, int Niveau);

	private enum Pointe
	{
		Rien,
		Sac,
		Porte,
		Vide,
		Panoplie,
		Autre
	}

	private sealed class CasePoints : TextBox
	{
		private readonly FenetreInventaire _fenetre;

		public Stat Stat { get; }

		public CasePoints(FenetreInventaire fenetre, Stat stat)
			: base(fenetre, $"CasePoints{stat}")
		{
			_fenetre = fenetre;
			Stat = stat;
		}

		protected override bool IsTextAllowed(string text, int position)
		{
			if (!string.IsNullOrEmpty(text) && text.All(char.IsAsciiDigit))
			{
				return base.IsTextAllowed(text, position);
			}
			return false;
		}

		protected override void OnKeyboardFocus()
		{
			base.OnKeyboardFocus();
			_fenetre.CaseEnMain(this);
		}

		protected override void OnLostKeyboardFocus()
		{
			base.OnLostKeyboardFocus();
			_fenetre.CaseLachee(this);
		}

		protected override bool OnKeyReturn(bool down)
		{
			if (!down)
			{
				Blur();
			}
			return true;
		}

		protected override bool OnKeyEscape(bool down)
		{
			if (down)
			{
				_fenetre.CaseEchappee(this);
				Blur();
			}
			return true;
		}
	}

	internal enum Onglet
	{
		Sac,
		Caracteristiques,
		Apparence
	}

	private enum Categorie
	{
		Tout,
		Equipement,
		Consommables,
		Ressources,
		Sacs,
		Quete,
		Techniques
	}

	private readonly record struct Entree(int Case, ItemDescriptor Objet, int Quantite, int Rarete, Categorie Categorie, bool Meilleur, int NiveauRequis);

	private readonly record struct Ecart(long Valeur, Color Couleur);

	private const float HauteurParure = 26f;

	private const float EcartParure = 3f;

	private Guid? _essaiTitre;

	private Guid? _essaiOrnement;

	private int _defilTitres;

	private int _defilOrnements;

	private Rectangle _zoneTitres;

	private Rectangle _zoneOrnements;

	private const char Repere = '\u0001';

	private (Pointe Sorte, int Numero, Guid Panoplie, bool Details, string? Autre) _bulleCle;

	private Bulle? _bulle;

	private readonly List<(Rectangle Rect, string Cle, Func<Bulle> Lire)> _zonesBulles = new List<(Rectangle, string, Func<Bulle>)>();

	private const int VieParPointDeResistance = 5;

	private const int KiParPointDeKi = 2;

	private const double CritiqueParPointDeMaitrise = 0.1;

	private const double DegatsCritiquesParPointDeMaitrise = 0.3;

	private static readonly int[] Pas = new int[3] { 1, 10, 2147483647 };

	private int _pas = 1;

	private readonly int[] _attente = new int[Enum.GetValues<Stat>().Length];

	private Stat? _plusTenu;

	private long _plusDepuis;

	private long _plusProchain;

	private readonly List<(Rectangle Rect, Stat Stat)> _boutonsPlus = new List<(Rectangle, Stat)>();

	private long _valideA;

	private const long ReinitialiserConfirmationMs = 4000L;

	private long _reinitialiserDemandeA;

	private readonly List<CasePoints> _casesPoints = new List<CasePoints>();

	private int _avantSaisie;

	private long _echapA;

	private bool _majCase;

	private readonly List<Entree> _objets = new List<Entree>();

	private readonly Dictionary<int, Entree> _parCase = new Dictionary<int, Entree>();

	private readonly List<int> _vides = new List<int>();

	private readonly List<int> _montrees = new List<int>();

	private readonly int[] _comptes = new int[Enum.GetValues<Categorie>().Length];

	private readonly List<(PanoplieDescriptor Panoplie, int Pieces, int Total)> _panopliesPortees = new List<(PanoplieDescriptor, int, int)>();

	private int _signature;

	private bool _lu;

	private bool _vueSale = true;

	private Onglet _onglet;

	private int _appuiSac = -1;

	private int _appuiEmplacement = -1;

	private Point _appuiPosition;

	private bool _glisse;

	private int _refusEmplacement = -1;

	private long _refusA;

	private string? _avis;

	private long _avisA;

	private const float GuerrierX = 16f;

	private const float GuerrierY = 98f;

	private const float GuerrierLargeur = 256f;

	private const float SceneHauteur = 290f;

	private static readonly Dictionary<string, (float X, float Y)> PlacesEmplacements = new Dictionary<string, (float, float)>
	{
		["tete"] = (0f, 10f),
		["tenue"] = (0f, 78f),
		["haut"] = (0f, 78f),
		["pantalon"] = (0f, 146f),
		["bottes"] = (0f, 214f),
		["dos"] = (204f, 10f),
		["gants"] = (204f, 78f),
		["aura"] = (204f, 146f),
		["noyau"] = (204f, 214f)
	};

	private static readonly int[] TourGauche = new int[4] { 1, 3, 0, 2 };

	private static readonly int[] TourDroite = new int[4] { 2, 0, 3, 1 };

	private static readonly Color BordVide = new Color(255, 43, 55, 83);

	private static readonly Color DessinVide = new Color(255, 77, 93, 130);

	private static readonly Color Rouge = new Color(255, 255, 90, 110);

	private static readonly Color RougePale = new Color(255, 255, 154, 165);

	private readonly List<(Rectangle Rect, int Emplacement)> _hexagones = new List<(Rectangle, int)>();

	private static int _rangee;

	private int _essai = -1;

	private bool _apportsLus;

	private PersonnageScreen.Apports _apports;

	private int[]? _equipementLu;

	private int _eclatEmplacement = -1;

	private int _eclatCase = -1;

	private long _eclatA;

	private int _filtreEmplacement = -1;

	private bool _clicApresDouble;

	private const float PanneauX = 288f;

	private const float PanneauY = 98f;

	private const float PanneauLargeur = 376f;

	private const float PanneauHauteur = 430f;

	private const float GrilleX = 294f;

	private const float GrilleY = 138f;

	private const int Colonnes = 8;

	private const int Rangees = 7;

	private const float TailleCase = 42f;

	private const float PasCase = 46f;

	private const float BasDuSacY = 464f;

	private static readonly (Categorie Categorie, string Glyphe)[] Puces = new(Categorie, string)[7]
	{
		(Categorie.Tout, "tout"),
		(Categorie.Equipement, "equipement"),
		(Categorie.Consommables, "consommable"),
		(Categorie.Ressources, "ressource"),
		(Categorie.Sacs, "sac"),
		(Categorie.Quete, "quete"),
		(Categorie.Techniques, "technique")
	};

	private static bool _range;

	private Categorie _categorie;

	private TextBox _recherche;

	private string _requete = string.Empty;

	private bool _cherche;

	private readonly List<(Rectangle Rect, int CaseSac)> _cases = new List<(Rectangle, int)>();

	private readonly List<(Rectangle Rect, PanoplieDescriptor Panoplie)> _lignesPanoplies = new List<(Rectangle, PanoplieDescriptor)>();

	private Rectangle _zoneSac;

	private readonly List<Puce> _puces = new List<Puce>();

	private int _rangeeHaut;

	private string? _survolSon;

	private long _survolSonA;

	private int _montee;

	private long _monteeA;

	private int PointsEnAttente => _attente.Sum();

	protected override Rectangle Place => new Rectangle(900, 146, 680, 544);

	protected override string SonOuverture => FichierSon("ouvrir");

	internal Onglet OngletActif => _onglet;

	public override bool TientUnObjet
	{
		get
		{
			if (_appuiSac >= 0 || _appuiEmplacement >= 0)
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

	protected override bool SurvolSuspendu => _glisse;

	private static List<Parure> Titres(Player me)
	{
		IOrderedEnumerable<Parure> collection = (from titre in DatabaseObject<TitleDescriptor>.Lookup.Values.OfType<TitleDescriptor>()
			select new Parure(titre.Id, titre.Name, titre.Color ?? TitleDescriptor.DefaultColor, string.IsNullOrWhiteSpace(titre.Description) ? null : titre.Description.Trim(), Traduction.Contenu(titre.Folder?.Trim() ?? string.Empty), me.TitleIds.Contains(titre.Id), titre.Id == me.TitleId, 0) into titre
			orderby titre.Debloque descending
			select titre).ThenBy<Parure, string>((Parure titre) => titre.Nom, StringComparer.CurrentCultureIgnoreCase);
		List<Parure> list = new List<Parure>();
		list.Add(new Parure(Guid.Empty, Strings.Titles.None, TechniqueArt.Soft, null, string.Empty, Debloque: true, me.TitleId == Guid.Empty, 0));
		list.AddRange(collection);
		return list;
	}

	private static List<Parure> Ornements(Player me)
	{
		IOrderedEnumerable<Parure> collection = (from ornement in DatabaseObject<OrnamentDescriptor>.Lookup.Values.OfType<OrnamentDescriptor>()
			select new Parure(ornement.Id, ornement.Name, TechniqueArt.Text, string.IsNullOrWhiteSpace(ornement.Description) ? null : ornement.Description.Trim(), Traduction.Contenu(ornement.Folder ?? string.Empty), me.OrnamentIds.Contains(ornement.Id), ornement.Id == me.OrnamentId, ornement.UnlockLevel) into ornement
			orderby ornement.Debloque descending, ornement.Niveau
			select ornement).ThenBy<Parure, string>((Parure ornement) => ornement.Nom, StringComparer.CurrentCultureIgnoreCase);
		List<Parure> list = new List<Parure>();
		list.Add(new Parure(Guid.Empty, Strings.Ornaments.None, TechniqueArt.Text, null, string.Empty, Debloque: true, me.OrnamentId == Guid.Empty, 0));
		list.AddRange(collection);
		return list;
	}

	private void DessinerApparence(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me)
	{
		_essaiTitre = null;
		_essaiOrnement = null;
		List<Parure> list = Titres(me);
		SousTitre(renderer, 108f, Strings.Personnage.TitlesHeading, list.Count((Parure titre) => titre.Debloque && titre.Id != Guid.Empty), list.Count - 1);
		_zoneTitres = _d.R(288f, 122f, 376f, 236f);
		_defilTitres = DessinerParures(renderer, _zoneTitres, list, _defilTitres, titre: true);
		List<Parure> list2 = Ornements(me);
		SousTitre(renderer, 376f, Strings.Personnage.OrnamentsHeading, list2.Count((Parure ornement) => ornement.Debloque && ornement.Id != Guid.Empty), list2.Count - 1);
		_zoneOrnements = _d.R(288f, 390f, 376f, 88f);
		_defilOrnements = DessinerParures(renderer, _zoneOrnements, list2, _defilOrnements, titre: false);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), Strings.Personnage.TryHint, _d.X(288f), _d.Y(510f), A(TechniqueArt.Dim));
	}

	private void SousTitre(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float centreY, string nom, int debloques, int total)
	{
		int centerY = _d.Y(centreY);
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(11f), nom.ToUpperInvariant(), _d.X(288f), centerY, _d.Em(0.18f, 11f), A(TechniqueArt.Ki));
		TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(11f), debloques.ToString(CultureInfo.InvariantCulture) + " / " + total.ToString(CultureInfo.InvariantCulture), _d.X(664f), centerY, A(TechniqueArt.Dim));
	}

	private int DessinerParures(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle zone, List<Parure> parures, int defil, bool titre)
	{
		int num = (parures.Count + 1) / 2;
		int num2 = Math.Max(1, (int)(((float)zone.Height / _d.S + 3f) / 29f));
		defil = Math.Clamp(defil, 0, Math.Max(0, num - num2));
		bool flag = num > num2;
		float num3 = ((flag ? 368f : 376f) - 6f) / 2f;
		Point point = LocalMouse();
		Rectangle previous = TechniqueArt.BeginClip(renderer, zone);
		for (int i = defil * 2; i < Math.Min(parures.Count, (defil + num2) * 2); i++)
		{
			Parure parure = parures[i];
			int num4 = i / 2 - defil;
			Rectangle rect = new Rectangle(_d.X(288f + (float)(i % 2) * (num3 + 6f)), zone.Y + _d.L((float)num4 * 29f), _d.L(num3), _d.L(26f));
			string cle = $"{(titre ? "titre" : "ornement")}:{parure.Id}";
			bool flag2 = base.Survolee && IsHot(cle) && zone.Contains(point.X, point.Y);
			if (flag2 && parure.Debloque)
			{
				if (titre)
				{
					_essaiTitre = parure.Id;
				}
				else
				{
					_essaiOrnement = parure.Id;
				}
			}
			DessinerParure(renderer, rect, parure, flag2);
			Parure choix = parure;
			AddHit(rect, cle, delegate
			{
				Porter(choix, titre);
			});
			BulleSur(rect, cle, () => BulleParure(choix, titre));
		}
		TechniqueArt.EndClip(renderer, previous);
		if (flag)
		{
			Rectangle rect2 = new Rectangle(zone.Right - _d.L(3f), zone.Y, Math.Max(2, _d.L(3f)), zone.Height);
			TechniqueArt.Fill(renderer, rect2, A(new Color(255, 10, 14, 24)));
			int num5 = Math.Max(_d.L(16f), rect2.Height * num2 / num);
			int num6 = rect2.Height - num5;
			TechniqueArt.Fill(renderer, new Rectangle(rect2.X, rect2.Y + num6 * defil / Math.Max(1, num - num2), rect2.Width, num5), A(TechniqueArt.Line2));
		}
		return defil;
	}

	private void DessinerParure(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, Parure parure, bool chaud)
	{
		TechniqueArt.Fill(renderer, rect, A(chaud ? new Color(235, 20, 27, 42) : new Color(153, 7, 10, 18)));
		TechniqueArt.Outline(renderer, rect, A(parure.Porte ? TechniqueArt.Gold : (chaud ? TechniqueArt.Line2 : TechniqueArt.Line)));
		int num = rect.Y + rect.Height / 2;
		int num2 = rect.X + _d.L(8f);
		int num3 = rect.Right - _d.L(8f);
		if (!parure.Debloque)
		{
			int num4 = _d.L(9f);
			int num5 = _d.L(11f);
			Lisser("inv_cadenas.png", new Rectangle(num2, num - num5 / 2, num4, num5), A(new Color(255, 59, 70, 96)));
			num2 += num4 + _d.L(6f);
		}
		string text = (parure.Porte ? Strings.Inventaire.Worn.ToString() : ((!parure.Debloque && parure.Niveau > 0) ? Strings.Inventaire.LevelShortLower.ToString(parure.Niveau) : null));
		if (text != null)
		{
			string text2 = text.ToUpperInvariant();
			num3 -= TechniqueArt.SpacedRight(renderer, TechniqueArt.Label, Pt(9f), text2, num3, num, _d.Em(0.12f, 9f), A(parure.Porte ? TechniqueArt.Gold : TechniqueArt.Dim)) + _d.L(6f);
		}
		Color color = (parure.Debloque ? parure.Couleur : new Color(255, 77, 88, 115));
		string text3 = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), parure.Nom, num3 - num2, 1).FirstOrDefault();
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), text3, num2, num, A(color));
	}

	private static void Porter(Parure parure, bool titre)
	{
		if (parure.Debloque && !parure.Porte)
		{
			if (titre)
			{
				PacketSender.SendSetActiveTitle(parure.Id);
			}
			else
			{
				PacketSender.SendSetActiveOrnament(parure.Id);
			}
		}
	}

	private void DefilerApparence(int delta)
	{
		Point point = LocalMouse();
		if (_zoneTitres.Contains(point.X, point.Y))
		{
			_defilTitres = Math.Max(0, _defilTitres - Math.Sign(delta));
		}
		else if (_zoneOrnements.Contains(point.X, point.Y))
		{
			_defilOrnements = Math.Max(0, _defilOrnements - Math.Sign(delta));
		}
	}

	private void BulleSur(Rectangle rect, string cle, Func<Bulle> lire)
	{
		_zonesBulles.Add((rect, cle, lire));
	}

	internal static string SorteDe(ItemDescriptor objet)
	{
		switch (objet.ItemType)
		{
		case ItemType.Equipment:
		{
			List<string> slots = Options.Instance.Equipment.Slots;
			if (objet.EquipmentSlot < 0 || objet.EquipmentSlot >= slots.Count)
			{
				return Strings.Personnage.CategoryEquipment.ToString();
			}
			return Traduction.Contenu(slots[objet.EquipmentSlot]);
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

	private static Bulle BulleObjet(Player me, int caseSac, bool depuisSac, bool details)
	{
		Bulle bulle = new Bulle();
		IItem item = me.Inventory[caseSac];
		if (item == null || !DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var databaseObject))
		{
			return bulle;
		}
		ItemProperties itemProperties = item.ItemProperties;
		int rarete = JetDObjet.Rarete(databaseObject, itemProperties);
		Color color = PersonnageScreen.CouleurRarete(rarete);
		bulle.Icone = Globals.ContentManager.GetTexture(TextureType.Item, databaseObject.Icon);
		bulle.Teinte = databaseObject.Color;
		bulle.Nom = databaseObject.Name;
		bulle.Couleur = color;
		bulle.CouleurNom = color;
		bulle.Sorte = SorteDe(databaseObject) + " · " + PersonnageScreen.NomRarete(rarete);
		int num = NiveauRequis(databaseObject);
		if (num > 1)
		{
			bulle.Exigence = (Strings.Inventaire.LevelRequired.ToString(num), (me.Level < num) ? TechniqueArt.Red : TechniqueArt.Soft);
		}
		if (databaseObject.ItemType == ItemType.Equipment)
		{
			ItemDescriptor itemDescriptor = null;
			int caseSac2 = -1;
			if (depuisSac)
			{
				itemDescriptor = PersonnageScreen.ObjetPorte(me, databaseObject.EquipmentSlot, out caseSac2);
			}
			List<PersonnageScreen.Ligne> list = PersonnageScreen.LignesEquipement(databaseObject, itemProperties, itemDescriptor, PersonnageScreen.ProprietesDe(me, caseSac2));
			bulle.Ecarts = itemDescriptor != null;
			foreach (PersonnageScreen.Ligne item3 in list)
			{
				string valeur = item3.Valeur;
				bool flag = ((valeur == "0" || valeur == "0 %") ? true : false);
				bool flag2 = flag;
				long valueOrDefault = item3.Ecart.GetValueOrDefault();
				bulle.Lignes.Add(new LigneBulle(item3.Nom, flag2 ? "–" : item3.Valeur, valueOrDefault, (valueOrDefault == 0L) ? null : Math.Abs(valueOrDefault).ToString(CultureInfo.InvariantCulture)));
			}
			PanoplieDescriptor panoplieDescriptor = PanoplieDescriptor.DeLObjet(databaseObject.Id);
			if (panoplieDescriptor != null)
			{
				HashSet<Guid> hashSet = PersonnageScreen.IdsPortes(me);
				if (depuisSac)
				{
					if (itemDescriptor != null)
					{
						hashSet.Remove(itemDescriptor.Id);
					}
					hashSet.Add(databaseObject.Id);
				}
				int num2 = panoplieDescriptor.PiecesPortees(hashSet);
				int num3 = panoplieDescriptor.Items.Distinct().Count();
				bulle.Annonce = panoplieDescriptor.Name;
				LocalizedString localizedString = ((!depuisSac) ? ((num2 > 1) ? Strings.Inventaire.SetPiecesWorn : Strings.Inventaire.SetPieceWorn) : ((num2 > 1) ? Strings.Inventaire.SetPiecesWith : Strings.Inventaire.SetPieceWith));
				bulle.Paliers.Add((localizedString.ToString(num2, num3), TechniqueArt.Soft));
				if (details)
				{
					AjouterPaliers(bulle, panoplieDescriptor, num2);
				}
			}
			if (itemDescriptor != null)
			{
				int num4 = list.Count((PersonnageScreen.Ligne ligne) => ligne.Ecart > 0);
				int num5 = list.Count((PersonnageScreen.Ligne ligne) => ligne.Ecart < 0);
				string text;
				Color item2;
				if (num4 > 0 && num5 == 0)
				{
					text = Strings.Inventaire.VerdictBetter.ToString('\u0001');
					item2 = TechniqueArt.Green;
				}
				else if (num5 > 0 && num4 == 0)
				{
					text = Strings.Inventaire.VerdictWorse.ToString('\u0001');
					item2 = TechniqueArt.Red;
				}
				else if (num4 == 0)
				{
					text = Strings.Inventaire.VerdictSame.ToString('\u0001');
					item2 = TechniqueArt.Soft;
				}
				else
				{
					text = Strings.Inventaire.VerdictMixed.ToString(num4, num5, '\u0001');
					item2 = TechniqueArt.Soft;
				}
				string[] array = text.Split('\u0001');
				bulle.Verdict.Add((array[0], item2));
				if (array.Length > 1)
				{
					bulle.Verdict.Add((itemDescriptor.Name, TechniqueArt.Text));
					bulle.Verdict.Add((array[1], item2));
				}
			}
		}
		else if (databaseObject.ItemType == ItemType.Consumable)
		{
			ConsumableData consumable = databaseObject.Consumable;
			if (consumable != null)
			{
				if (consumable.Percentage != 0)
				{
					LocalizedString localizedString2 = consumable.Type switch
					{
						ConsumableType.Health => Strings.Inventaire.RestoresHealthPercent, 
						ConsumableType.Mana => Strings.Inventaire.RestoresKiPercent, 
						_ => Strings.Inventaire.RestoresExperiencePercent, 
					};
					bulle.Lignes.Add(new LigneBulle(Strings.Inventaire.Restores, localizedString2.ToString(consumable.Percentage), 0L, null));
				}
				if (consumable.Value != 0L)
				{
					LocalizedString localizedString3 = consumable.Type switch
					{
						ConsumableType.Health => Strings.Inventaire.RestoresHealth, 
						ConsumableType.Mana => Strings.Inventaire.RestoresKi, 
						_ => Strings.Inventaire.RestoresExperience, 
					};
					bulle.Lignes.Add(new LigneBulle(Strings.Inventaire.Restores, localizedString3.ToString(SaiyanHud.Amount(consumable.Value)), 0L, null));
				}
			}
			foreach (KeyValuePair<ItemEffect, int> item4 in PersonnageScreen.EffetsDe(databaseObject))
			{
				item4.Deconstruct(out var key, out var value);
				ItemEffect key2 = key;
				int num6 = value;
				LocalizedString value2;
				string nom = (Strings.ItemDescription.BonusEffects.TryGetValue((int)key2, out value2) ? PersonnageScreen.Libelle(value2) : key2.ToString());
				bulle.Lignes.Add(new LigneBulle(nom, PersonnageScreen.Signe(num6, pourcent: true), 0L, null));
			}
			if (databaseObject.Cooldown > 0)
			{
				bulle.Lignes.Add(new LigneBulle(Strings.Personnage.Cooldown, Strings.Personnage.Seconds.ToString(TechniqueArt.Number((double)databaseObject.Cooldown / 1000.0)), 0L, null));
			}
		}
		AjouterLePoids(bulle, me, databaseObject, item.Quantity, depuisSac);
		if (details)
		{
			bulle.Description = (string.IsNullOrWhiteSpace(databaseObject.Description) ? null : databaseObject.Description.Trim());
			string text2 = ((databaseObject.Price > 0) ? Strings.Inventaire.SellsFor.ToString(SaiyanHud.Amount(databaseObject.Price)) : Strings.Inventaire.NoSale.ToString());
			if (item.Quantity > 1)
			{
				text2 = text2 + " · " + Strings.Inventaire.StackOf.ToString(item.Quantity);
			}
			bulle.Notes.Add(text2);
		}
		string text3;
		if (!depuisSac)
		{
			text3 = $"{Strings.Inventaire.SlotFilterHint} · {Strings.Inventaire.HelpUnequip}";
		}
		else
		{
			string valeur;
			switch (databaseObject.ItemType)
			{
			case ItemType.Equipment:
				valeur = ((NiveauRequis(databaseObject) <= me.Level) ? Strings.Inventaire.HelpEquip.ToString() : Strings.Inventaire.NotYet.ToString());
				break;
			case ItemType.Consumable:
			case ItemType.Spell:
				valeur = Strings.Inventaire.HelpUse.ToString();
				break;
			case ItemType.Bag:
				valeur = Strings.Inventaire.CapsuleOnlyBest.ToString();
				break;
			default:
				valeur = null;
				break;
			}
			text3 = valeur;
		}
		string text4 = text3;
		if (!details)
		{
			text4 = ((text4 == null) ? Strings.Inventaire.HelpDetails.ToString() : $"{text4} · {Strings.Inventaire.HelpDetails}");
		}
		if (text4 != null)
		{
			bulle.Aides.Add(text4);
		}
		if (details && depuisSac && databaseObject.CanDrop)
		{
			bulle.Aides.Add(Strings.Inventaire.HelpDrop);
		}
		return bulle;
	}

	private static void AjouterLePoids(Bulle bulle, Player me, ItemDescriptor objet, int quantite, bool depuisSac)
	{
		if (objet.ItemType == ItemType.Bag)
		{
			int num = ReglesDeCharge.BonusDeCapsule(objet);
			bulle.Lignes.Add(new LigneBulle(Strings.Inventaire.CapsuleExtra, Strings.Inventaire.KilosMore.ToString(SaiyanHud.Amount(num)), 0L, null));
			bool flag = me.LireLaCharge().Capsule == objet.Id;
			bulle.Verdict.Add((flag ? Strings.Inventaire.CapsuleActive.ToString(SaiyanHud.Amount(num)) : Strings.Inventaire.CapsuleOutdone.ToString(), flag ? TechniqueArt.Green : TechniqueArt.Soft));
			return;
		}
		if (!depuisSac)
		{
			bulle.Lignes.Add(new LigneBulle(Strings.Inventaire.WeightLine, Strings.Inventaire.WeightWorn, 0L, null));
			return;
		}
		int num2 = ReglesDeCharge.PoidsUnitaire(objet);
		bulle.Lignes.Add(new LigneBulle(Strings.Inventaire.WeightLine, Strings.Inventaire.Kilos.ToString(SaiyanHud.Amount(num2)), 0L, null));
		if (quantite > 1 && num2 > 0)
		{
			bulle.Lignes.Add(new LigneBulle(Strings.Inventaire.WeightInBag, Strings.Inventaire.WeightInBagValue.ToString(SaiyanHud.Amount(quantite), SaiyanHud.Amount((long)num2 * (long)quantite)), 0L, null));
		}
		else if (objet.ItemType == ItemType.Equipment && num2 > 0)
		{
			bulle.Lignes.Add(new LigneBulle(Strings.Inventaire.WeightOnceWorn, Strings.Inventaire.Kilos.ToString(SaiyanHud.Amount(0L)), 0L, null));
		}
	}

	private static Bulle BulleCharge(Player me)
	{
		Player.Charge charge = me.LireLaCharge();
		Bulle bulle = new Bulle()
		{
			Nom = Strings.Inventaire.ChargeTitle,
			Couleur = (charge.Surcharge ? TechniqueArt.Red : (charge.PresquePleine ? TechniqueArt.Gold : new Color(255, 90, 168, 255))),
			CouleurNom = TechniqueArt.Text,
			Sorte = Strings.Inventaire.ChargeSummary.ToString(SaiyanHud.Amount(charge.Porte), SaiyanHud.Amount(charge.Capacite), charge.Pourcent),
			Texte = (charge.Surcharge ? Strings.Inventaire.ChargeOverloadedExplained.ToString(SaiyanHud.Amount(charge.Porte - charge.Capacite), SaiyanHud.Amount(charge.Capacite)) : (charge.PresquePleine ? Strings.Inventaire.ChargeAlmostExplained.ToString(SaiyanHud.Amount(charge.Capacite - charge.Porte)) : Strings.Inventaire.ChargeExplained.ToString())),
		};
		Bulle bulle2 = bulle;
		bulle2.Lignes.Add(new LigneBulle(Strings.Inventaire.ChargeBase, Strings.Inventaire.Kilos.ToString(SaiyanHud.Amount(1000L)), 0L, null));
		bulle2.Lignes.Add(new LigneBulle(Strings.Inventaire.ChargeLevel.ToString(me.Level), Strings.Inventaire.KilosMore.ToString(SaiyanHud.Amount(charge.ParLeNiveau)), 0L, null));
		bulle2.Lignes.Add(new LigneBulle(Strings.Inventaire.ChargeJobs.ToString(charge.NiveauxDeMetier), Strings.Inventaire.KilosMore.ToString(SaiyanHud.Amount(charge.ParLesMetiers)), 0L, null));
		if (charge.ParLaCapsule > 0 && DatabaseObject<ItemDescriptor>.TryGet(charge.Capsule, out var databaseObject))
		{
			bulle2.Lignes.Add(new LigneBulle(databaseObject.Name, Strings.Inventaire.KilosMore.ToString(SaiyanHud.Amount(charge.ParLaCapsule)), 0L, null));
		}
		bulle2.Lignes.Add(new LigneBulle(Strings.Inventaire.ChargeTotal, Strings.Inventaire.Kilos.ToString(SaiyanHud.Amount(charge.Capacite)), 0L, null));
		bulle2.Aides.Add(Strings.Inventaire.ChargeWornWeighsNothing);
		return bulle2;
	}

	internal static int NiveauRequis(ItemDescriptor objet)
	{
		List<ConditionList> list = objet.UsageRequirements?.Lists;
		if (list == null || list.Count == 0)
		{
			return 0;
		}
		int num = int.MaxValue;
		foreach (ConditionList item in list)
		{
			int num2 = 0;
			foreach (Condition condition in item.Conditions)
			{
				if (condition is LevelOrStatCondition { ComparingLevel: not false } levelOrStatCondition && !condition.Negated)
				{
					int val = num2;
					int val2;
					switch (levelOrStatCondition.Comparator)
					{
					case VariableComparator.Equal:
					case VariableComparator.GreaterOrEqual:
						val2 = levelOrStatCondition.Value;
						break;
					case VariableComparator.Greater:
						val2 = levelOrStatCondition.Value + 1;
						break;
					default:
						val2 = 0;
						break;
					}
					num2 = Math.Max(val, val2);
				}
			}
			num = Math.Min(num, num2);
		}
		if (num != int.MaxValue)
		{
			return num;
		}
		return 0;
	}

	private static void AjouterPaliers(Bulle bulle, PanoplieDescriptor panoplie, int pieces)
	{
		foreach (PanoplieBonus item2 in panoplie.Bonuses.OrderBy((PanoplieBonus bonus) => bonus.Pieces))
		{
			string item = Strings.Inventaire.SetTier.ToString(item2.Pieces, string.Join(", ", ItemDescriptionWindow.Lignes(item2)));
			bulle.Paliers.Add((item, (pieces >= item2.Pieces) ? TechniqueArt.Green : TechniqueArt.Soft));
		}
	}

	private static Bulle BulleEmplacement(string nom)
	{
		Bulle bulle = new Bulle
		{
			Nom = Traduction.Contenu(nom),
			Sorte = Strings.Inventaire.EmptySlot
		};
		bulle.Aides.Add(Strings.Inventaire.EmptySlotHint);
		bulle.Aides.Add(Strings.Inventaire.SlotFilterHint);
		return bulle;
	}

	private static Bulle BullePanoplie(Player me, PanoplieDescriptor panoplie)
	{
		HashSet<Guid> portes = PersonnageScreen.IdsPortes(me);
		int num = panoplie.PiecesPortees(portes);
		List<Guid> list = panoplie.Items.Distinct().ToList();
		Bulle bulle = new Bulle()
		{
			Nom = panoplie.Name,
			Couleur = TechniqueArt.Gold,
			CouleurNom = TechniqueArt.Gold,
			Sorte = ((num > 1) ? Strings.Inventaire.SetPiecesCount : Strings.Inventaire.SetPieceCount).ToString(num, list.Count),
		};
		Bulle bulle2 = bulle;
		AjouterPaliers(bulle2, panoplie, num);
		ItemDescriptor databaseObject;
		List<string> list2 = (from id in list
			where !portes.Contains(id) && DatabaseObject<ItemDescriptor>.TryGet(id, out databaseObject)
			select DatabaseObject<ItemDescriptor>.Get(id)?.Name into nom
			where !string.IsNullOrEmpty(nom)
			select nom).ToList();
		if (list2.Count > 0)
		{
			bulle2.Aides.Add(Strings.Inventaire.SetMissing.ToString(string.Join(", ", list2)));
		}
		return bulle2;
	}

	private static Bulle BullePuissance(Player me)
	{
		return new Bulle
		{
			Nom = Strings.Personnage.PowerHeading,
			Couleur = TechniqueArt.Cyan,
			CouleurNom = TechniqueArt.Text,
			Sorte = Strings.Inventaire.PowerScouter,
			Texte = Strings.Inventaire.PowerExplained
		};
	}

	private static Bulle BulleVital(Player me, int vital)
	{
		long value = VitalMax(me, vital);
		long value2 = 0L;
		string text = "0";
		if (DatabaseObject<ClassDescriptor>.TryGet(me.Class, out var databaseObject))
		{
			value2 = ((vital < databaseObject.BaseVital.Length) ? databaseObject.BaseVital[vital] : 0);
			long value3 = ((vital < databaseObject.VitalIncrease.Length) ? databaseObject.VitalIncrease[vital] : 0);
			text = (databaseObject.IncreasePercentage ? $"{value3} %" : SaiyanHud.Amount(value3));
		}
		bool flag = vital == 0;
		Stat index = ((!flag) ? Stat.AbilityPower : Stat.Defense);
		Bulle bulle = new Bulle()
		{
			Nom = (flag ? Strings.Inventaire.MaxHealth : Strings.Inventaire.MaxKi),
			Couleur = (flag ? new Color(255, 255, 90, 95) : new Color(255, 90, 168, 255)),
			CouleurNom = TechniqueArt.Text,
			Sorte = (flag ? Strings.Inventaire.HealthPoints : Strings.Inventaire.KiPoints).ToString(SaiyanHud.Amount(value)),
			Texte = Strings.Inventaire.VitalExplained.ToString(SaiyanHud.Amount(value2), text, flag ? 5 : 2, PersonnageScreen.Texte(Strings.ItemDescription.Stats, (int)index).TrimEnd(' ', ':', ' ')),
		};
		return bulle;
	}

	private static Bulle BulleNiveau(Player me)
	{
		int num = 0;
		long value = 0L;
		long value2 = 0L;
		if (DatabaseObject<ClassDescriptor>.TryGet(me.Class, out var databaseObject))
		{
			num = databaseObject.PointIncrease;
			value = ((databaseObject.VitalIncrease.Length != 0) ? databaseObject.VitalIncrease[0] : 0);
			value2 = ((databaseObject.VitalIncrease.Length > 1) ? databaseObject.VitalIncrease[1] : 0);
		}
		long experienceToNextLevel = me.ExperienceToNextLevel;
		Bulle bulle = new Bulle()
		{
			Nom = Strings.Inventaire.LevelTitle.ToString(me.Level),
			Couleur = TechniqueArt.Ki,
			CouleurNom = TechniqueArt.Text,
			Sorte = ((experienceToNextLevel <= 0) ? Strings.Character.ExperienceMax.ToString() : Strings.Inventaire.ExperienceOf.ToString(SaiyanHud.Amount(me.Experience), SaiyanHud.Amount(experienceToNextLevel))),
			Texte = Strings.Inventaire.LevelExplained.ToString(num, SaiyanHud.Amount(value), SaiyanHud.Amount(value2)),
		};
		return bulle;
	}

	private Bulle BulleStat(Player me, Stat stat)
	{
		Color color = PersonnageScreen.CouleurStat(stat);
		Bulle bulle = new Bulle()
		{
			Nom = PersonnageScreen.Texte(Strings.ItemDescription.Stats, (int)stat).TrimEnd(' ', ':', ' '),
			Couleur = color,
			CouleurNom = TechniqueArt.Mix(color, TechniqueArt.White, 0.55f),
			Sorte = PersonnageScreen.Texte(Strings.Character.StatRoles, (int)stat),
			Texte = PersonnageScreen.Texte(Strings.Character.StatDescriptions, (int)stat),
		};
		Bulle bulle2 = bulle;
		string[] array = PersonnageScreen.Texte(Strings.Character.StatDetails, (int)stat).Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		foreach (string text in array)
		{
			int num = text.IndexOf('=');
			List<LigneBulle> lignes = bulle2.Lignes;
			LigneBulle item;
			if (num < 0)
			{
				item = new LigneBulle(text, string.Empty, 0L, null);
			}
			else
			{
				string nom = text.Substring(0, num).Trim();
				string text2 = text;
				int num2 = num + 1;
				item = new LigneBulle(nom, text2.Substring(num2, text2.Length - num2).Trim(), 0L, null);
			}
			lignes.Add(item);
		}
		int num3 = me.Stat[(int)stat];
		int num4 = Math.Clamp(((int)stat < _apports.Stats.Length) ? _apports.Stats[(int)stat] : 0, 0, num3);
		int num5 = Math.Clamp(BaseDe(me, stat), 0, num3 - num4);
		int num6 = Math.Max(0, num3 - num4 - num5);
		List<string> list = new List<string>();
		list.Add(Strings.Inventaire.StatBase.ToString(num5));
		list.Add(((num6 > 1) ? Strings.Inventaire.StatPlacedMany : Strings.Inventaire.StatPlacedOne).ToString(num6));
		list.Add(Strings.Inventaire.StatFromGear.ToString(num4));
		List<string> list2 = list;
		if (_attente[(int)stat] > 0)
		{
			list2.Add(Strings.Inventaire.StatPending.ToString(_attente[(int)stat]));
		}
		bulle2.Verdict.Add((string.Join(", ", list2), TechniqueArt.Soft));
		return bulle2;
	}

	private Bulle BulleBonus()
	{
		Bulle bulle = new Bulle
		{
			Nom = Strings.Inventaire.BonusTitle,
			Couleur = TechniqueArt.Cyan,
			CouleurNom = TechniqueArt.Text,
			Sorte = Strings.Inventaire.BonusKind
		};
		List<(string, int)> list = BonusPortes();
		foreach (var (nom, num) in list)
		{
			bulle.Lignes.Add(new LigneBulle(nom, PersonnageScreen.Signe(num, pourcent: true), 0L, null));
		}
		if (list.Count == 0)
		{
			bulle.Texte = Strings.Inventaire.NoBonusExplained;
		}
		return bulle;
	}

	private static Bulle BulleParure(Parure parure, bool titre)
	{
		string text = (parure.Debloque ? Strings.Inventaire.Unlocked.ToString() : ((parure.Niveau > 0) ? Strings.Inventaire.AtLevel.ToString(parure.Niveau) : Strings.Inventaire.Locked.ToString()));
		Bulle bulle = new Bulle
		{
			Nom = parure.Nom,
			Couleur = (titre ? parure.Couleur : TechniqueArt.Gold),
			CouleurNom = ((titre && parure.Debloque) ? parure.Couleur : TechniqueArt.Text),
			Sorte = ((parure.Dossier.Length > 0) ? (text + " · " + parure.Dossier) : text),
			Texte = parure.Description
		};
		bulle.Aides.Add(parure.Porte ? Strings.Inventaire.WornAlready.ToString() : (parure.Debloque ? Strings.Inventaire.ClickToWear.ToString() : Strings.Inventaire.NotUnlocked.ToString()));
		return bulle;
	}

	private void DessinerBulleDuSac(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me)
	{
		if (!base.Survolee)
		{
			return;
		}
		Point point = LocalMouse();
		bool flag = Globals.InputManager.IsKeyDown(Keys.Shift);
		Rectangle cible = default(Rectangle);
		Pointe pointe = Pointe.Rien;
		int num = -1;
		PanoplieDescriptor panoplieDescriptor = null;
		foreach (var (rectangle, num2) in _cases)
		{
			if (rectangle.Contains(point.X, point.Y) && _parCase.ContainsKey(num2))
			{
				Rectangle rectangle2 = rectangle;
				num = num2;
				pointe = Pointe.Sac;
				cible = rectangle2;
				break;
			}
		}
		if (pointe == Pointe.Rien)
		{
			foreach (var (rectangle3, num3) in _hexagones)
			{
				if (rectangle3.Contains(point.X, point.Y))
				{
					int caseSac;
					bool flag2 = PersonnageScreen.ObjetPorte(me, num3, out caseSac) != null;
					Rectangle rectangle4 = rectangle3;
					int num4 = (flag2 ? 2 : 3);
					num = num3;
					pointe = (Pointe)num4;
					cible = rectangle4;
					break;
				}
			}
		}
		if (pointe == Pointe.Rien)
		{
			foreach (var (rectangle5, panoplieDescriptor2) in _lignesPanoplies)
			{
				if (rectangle5.Contains(point.X, point.Y))
				{
					Rectangle rectangle6 = rectangle5;
					panoplieDescriptor = panoplieDescriptor2;
					pointe = Pointe.Panoplie;
					cible = rectangle6;
					break;
				}
			}
		}
		string item = null;
		Func<Bulle> func = null;
		if (pointe == Pointe.Rien)
		{
			for (int num5 = _zonesBulles.Count - 1; num5 >= 0; num5--)
			{
				(Rectangle, string, Func<Bulle>) tuple4 = _zonesBulles[num5];
				if (tuple4.Item1.Contains(point.X, point.Y))
				{
					Rectangle item2 = tuple4.Item1;
					func = tuple4.Item3;
					pointe = Pointe.Autre;
					cible = item2;
					item = $"{tuple4.Item2}#{_signature}#{PointsEnAttente}#{me.StatPoints}#{me.Experience}#{me.TitleId}#{me.OrnamentId}";
					break;
				}
			}
		}
		if (pointe == Pointe.Rien)
		{
			return;
		}
		(Pointe, int, Guid, bool, string) tuple5 = (pointe, num, panoplieDescriptor?.Id ?? Guid.Empty, flag, item);
		if (_bulle != null)
		{
			(Pointe, int, Guid, bool, string) bulleCle = _bulleCle;
			(Pointe, int, Guid, bool, string) tuple6 = tuple5;
			if (bulleCle.Item1 == tuple6.Item1 && bulleCle.Item2 == tuple6.Item2 && !(bulleCle.Item3 != tuple6.Item3) && bulleCle.Item4 == tuple6.Item4 && !(bulleCle.Item5 != tuple6.Item5))
			{
				goto IL_03cb;
			}
		}
		_bulleCle = tuple5;
		switch (pointe)
		{
		case Pointe.Autre:
			_bulle = func();
			break;
		case Pointe.Sac:
			_bulle = BulleObjet(me, num, depuisSac: true, flag);
			break;
		case Pointe.Porte:
		{
			PersonnageScreen.ObjetPorte(me, num, out var caseSac2);
			_bulle = BulleObjet(me, caseSac2, depuisSac: false, flag);
			break;
		}
		case Pointe.Vide:
			_bulle = BulleEmplacement(Options.Instance.Equipment.Slots[num]);
			break;
		default:
			_bulle = BullePanoplie(me, panoplieDescriptor);
			break;
		}
		goto IL_03cb;
		IL_03cb:
		DessinerBulle(renderer, _bulle, cible);
	}

	private int PointsPlaces(Player me)
	{
		int num = 0;
		Stat[] statsMontrees = PersonnageScreen.StatsMontrees;
		foreach (Stat stat in statsMontrees)
		{
			int num2 = (int)stat;
			int num3 = me.Stat[num2];
			int num4 = Math.Clamp((num2 < _apports.Stats.Length) ? _apports.Stats[num2] : 0, 0, num3);
			num += Math.Max(0, num3 - num4 - Math.Clamp(BaseDe(me, stat), 0, num3 - num4));
		}
		return num;
	}

	private static long PrixPourReinitialiser(Player me)
	{
		return (long)Math.Max(0, Options.Instance.Player.ZenisParNiveauPourReprendreLesPoints) * (long)Math.Max(1, me.Level);
	}

	private long VitauxEnAttente(int vital)
	{
		return vital switch
		{
			0 => (long)_attente[2] * 5L, 
			1 => (long)_attente[1] * 2L, 
			_ => 0L, 
		};
	}

	private void AnnulerPoints()
	{
		Array.Clear(_attente);
		_plusTenu = null;
	}

	private int PointsAAjouter(Player me, Stat stat)
	{
		int val = me.StatPoints - PointsEnAttente;
		int val2 = Options.Instance.Player.MaxStat - me.Stat[(int)stat] - _attente[(int)stat];
		return Math.Max(0, Math.Min(Math.Min(_pas, val), val2));
	}

	private bool Ajouter(Player me, Stat stat)
	{
		int num = PointsAAjouter(me, stat);
		if (num <= 0)
		{
			return false;
		}
		_attente[(int)stat] += num;
		SonDuPoint();
		return true;
	}

	private void CommencerPlus(Point local)
	{
		if (_onglet != Onglet.Caracteristiques)
		{
			return;
		}
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		foreach (var (rectangle, stat) in _boutonsPlus)
		{
			if (rectangle.Contains(local.X, local.Y))
			{
				if (Ajouter(me, stat))
				{
					long millisecondsUtc = Timing.Global.MillisecondsUtc;
					_plusTenu = stat;
					_plusDepuis = millisecondsUtc;
					_plusProchain = millisecondsUtc + 380;
				}
				break;
			}
		}
	}

	private void LacherPlus()
	{
		_plusTenu = null;
	}

	private void SuivreLesPoints()
	{
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		if (PointsEnAttente > me.StatPoints)
		{
			AnnulerPoints();
		}
		Stat? plusTenu = _plusTenu;
		if (!plusTenu.HasValue)
		{
			return;
		}
		Stat valueOrDefault = plusTenu.GetValueOrDefault();
		if (_onglet != Onglet.Caracteristiques || !Globals.InputManager.IsMouseButtonDown(MouseButton.Left))
		{
			_plusTenu = null;
			return;
		}
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		if (millisecondsUtc >= _plusProchain)
		{
			if (!Ajouter(me, valueOrDefault))
			{
				_plusTenu = null;
			}
			else
			{
				_plusProchain = millisecondsUtc + ((millisecondsUtc - _plusDepuis > 1500) ? 45 : 110);
			}
		}
	}

	private void Valider(Player me)
	{
		bool flag = false;
		Stat[] statsMontrees = PersonnageScreen.StatsMontrees;
		foreach (Stat stat in statsMontrees)
		{
			int num = _attente[(int)stat];
			if (num > 0)
			{
				PacketSender.SendUpgradeStat((byte)stat, num);
				flag = true;
			}
		}
		AnnulerPoints();
		if (flag)
		{
			_valideA = Timing.Global.MillisecondsUtc;
			Son("valider");
		}
	}

	private static int BaseDe(Player me, Stat stat)
	{
		if (!DatabaseObject<ClassDescriptor>.TryGet(me.Class, out var databaseObject))
		{
			return 0;
		}
		int num = (((int)stat < databaseObject.BaseStat.Length) ? databaseObject.BaseStat[(int)stat] : 0);
		int num2 = (((int)stat < databaseObject.StatIncrease.Length) ? databaseObject.StatIncrease[(int)stat] : 0);
		if (!databaseObject.IncreasePercentage)
		{
			return num + num2 * Math.Max(0, me.Level - 1);
		}
		return (int)((double)num * Math.Pow(1.0 + (double)num2 / 100.0, Math.Max(0, me.Level - 1)));
	}

	private static string Pourcent(double valeur)
	{
		return Math.Round(valeur, 1).ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',') + " %";
	}

	private void DessinerCaracteristiques(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, long now)
	{
		_boutonsPlus.Clear();
		int statPoints = me.StatPoints;
		int pointsEnAttente = PointsEnAttente;
		DessinerBanniere(renderer, me, statPoints, pointsEnAttente, now);
		float num = 120f;
		Stat[] statsMontrees = PersonnageScreen.StatsMontrees;
		foreach (Stat stat in statsMontrees)
		{
			num = Math.Max(num, (float)(me.Stat[(int)stat] + _attente[(int)stat]) * 1.12f);
		}
		bool possible = statPoints - pointsEnAttente > 0;
		for (int j = 0; j < PersonnageScreen.StatsMontrees.Length; j++)
		{
			DessinerStat(renderer, me, PersonnageScreen.StatsMontrees[j], j, _d.R(288f, 154f + (float)(j * 50), 376f, 46f), num, possible);
		}
		DessinerDerives(renderer, me);
		DessinerBonusPortes(renderer);
		DessinerActions(renderer, me, statPoints, pointsEnAttente);
	}

	private void DessinerBanniere(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, int libres, int enAttente, long now)
	{
		Rectangle rectangle = _d.R(288f, 98f, 376f, 50f);
		int num = rectangle.Y + rectangle.Height / 2;
		int w = Math.Max(2, _d.L(2f));
		if (libres <= 0)
		{
			TechniqueArt.Fill(renderer, rectangle, A(new Color(153, 7, 10, 18)));
			TechniqueArt.Fill(renderer, new Rectangle(rectangle.X, rectangle.Y, w, rectangle.Height), A(TechniqueArt.Line2));
			ClassDescriptor databaseObject;
			int num2 = (DatabaseObject<ClassDescriptor>.TryGet(me.Class, out databaseObject) ? databaseObject.PointIncrease : 0);
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(11f), Strings.Inventaire.NoPointsTitle.ToString().ToUpperInvariant(), rectangle.X + _d.L(12f), num - _d.L(8f), _d.Em(0.16f, 11f), A(TechniqueArt.Text));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11f), Strings.Inventaire.NoPointsHint.ToString(num2), rectangle.X + _d.L(12f), num + _d.L(8f), A(TechniqueArt.Soft));
			return;
		}
		TechniqueArt.FillHorizontal(renderer, rectangle, A(TechniqueArt.Gold, 0.16f), A(new Color(92, 10, 14, 24)), A(new Color(77, 10, 14, 24)));
		TechniqueArt.Fill(renderer, new Rectangle(rectangle.X, rectangle.Y, w, rectangle.Height), A(TechniqueArt.Gold));
		string text = SaiyanHud.Amount(Math.Max(0, libres - enAttente));
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(32f), text);
		int num3 = rectangle.X + _d.L(12f);
		float num4 = ((enAttente == 0) ? (0.5f - 0.5f * MathF.Cos((float)(now % 2400) / 2400f * ((float)Math.PI * 2f))) : 0f);
		TechniqueArt.Glow(renderer, (float)num3 + (float)point.X / 2f, num, _d.L(30f), A(TechniqueArt.Gold, 0.14f + 0.22f * num4));
		TechniqueArt.Italic(renderer, TechniqueArt.Display, Pt(32f), text, num3, num - point.Y / 2, A(TechniqueArt.Gold));
		num3 += point.X + _d.L(12f);
		string text2 = ((libres - enAttente == 1) ? Strings.Inventaire.PointToPlace : Strings.Inventaire.PointsToPlace).ToString().ToUpperInvariant();
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(11f), text2, num3, num - _d.L(7f), _d.Em(0.16f, 11f), A(TechniqueArt.Text));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11f), Strings.Inventaire.PointsFinal, num3, num + _d.L(8f), A(TechniqueArt.Soft));
		DessinerPas(renderer, rectangle);
	}

	private void DessinerPas(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle banniere)
	{
		string[] array = new string[3]
		{
			Strings.Character.StatStepOne,
			Strings.Inventaire.StatStepTen,
			Strings.Character.StatStepMax
		};
		int[] array2 = new int[Pas.Length];
		int num = 0;
		for (int i = 0; i < Pas.Length; i++)
		{
			array2[i] = Math.Max(_d.L(38f), TechniqueArt.Measure(renderer, TechniqueArt.Heavy, Pt(14f), array[i]).X + _d.L(16f));
			num += array2[i];
		}
		int num2 = _d.L(28f);
		int num3 = banniere.Right - _d.L(10f) - num;
		int num4 = banniere.Y + (banniere.Height - num2) / 2;
		TechniqueArt.Outline(renderer, new Rectangle(num3 - 1, num4 - 1, num + 2, num2 + 2), A(TechniqueArt.Line2));
		for (int j = 0; j < Pas.Length; j++)
		{
			Rectangle rect = new Rectangle(num3, num4, array2[j], num2);
			int montant = Pas[j];
			bool flag = _pas == montant;
			string cle = $"pas:{j}";
			bool flag2 = IsHot(cle);
			if (flag)
			{
				TechniqueArt.FillVertical(renderer, rect, A(new Color(255, 255, 224, 138)), A(new Color(255, 255, 176, 46)));
			}
			else
			{
				TechniqueArt.Fill(renderer, rect, A(new Color(255, 10, 14, 24)));
			}
			if (j > 0)
			{
				TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, 1, rect.Height), A(TechniqueArt.Line2));
			}
			TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(14f), array[j], rect.X + rect.Width / 2, rect.Y + rect.Height / 2, A(flag ? new Color(255, 26, 18, 6) : (flag2 ? TechniqueArt.White : TechniqueArt.Soft)));
			AddHit(rect, cle, delegate
			{
				_pas = montant;
			});
			num3 += array2[j];
		}
	}

	private void DessinerStat(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, Stat stat, int rang, Rectangle ligne, float echelle, bool possible)
	{
		int num = (int)stat;
		Color color = PersonnageScreen.CouleurStat(stat);
		Point point = LocalMouse();
		if (base.Survolee && ligne.Contains(point.X, point.Y))
		{
			TechniqueArt.Fill(renderer, ligne, A(TechniqueArt.White, 0.03f));
		}
		TechniqueArt.Fill(renderer, new Rectangle(ligne.X, ligne.Bottom - 1, ligne.Width, 1), A(TechniqueArt.White, 0.05f));
		int num2 = ligne.Y + ligne.Height / 2;
		Rectangle rect = new Rectangle(ligne.X + _d.L(4f), num2 - _d.L(19f), _d.L(34f), _d.L(38f));
		TechniqueArt.Image(renderer, "tech_hexa.png", rect, A(TechniqueArt.Mix(color, new Color(255, 10, 14, 24), 0.22f)));
		Pictogramme(renderer, PersonnageScreen.GlypheStat(stat), new Rectangle(rect.X + (rect.Width - _d.L(18f)) / 2, num2 - _d.L(9f), _d.L(18f), _d.L(18f)), A(color));
		int x = ligne.X + _d.L(50f);
		int num3 = _d.L(160f);
		string text = PersonnageScreen.Texte(Strings.ItemDescription.Stats, num).TrimEnd(' ', ':', ' ');
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(18f), text, x, ligne.Y + _d.L(12f), A(TechniqueArt.Text));
		string text2 = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), PersonnageScreen.Texte(Strings.Character.StatRoles, num), num3, 1).FirstOrDefault();
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), text2, x, ligne.Y + _d.L(28f), A(TechniqueArt.Dim));
		int num4 = me.Stat[num];
		int num5 = Math.Clamp((num < _apports.Stats.Length) ? _apports.Stats[num] : 0, 0, num4);
		int num6 = Math.Clamp(BaseDe(me, stat), 0, num4 - num5);
		int item = Math.Max(0, num4 - num5 - num6);
		int num7 = _attente[num];
		Rectangle rect2 = new Rectangle(x, ligne.Y + _d.L(37f), num3, Math.Max(4, _d.L(6f)));
		TechniqueArt.Fill(renderer, rect2, A(new Color(255, 7, 10, 18)));
		int num8 = rect2.X + 1;
		int num9 = rect2.Width - 2;
		(int, Color)[] array = new(int, Color)[4]
		{
			(num6, new Color(255, 75, 86, 115)),
			(item, TechniqueArt.Ki),
			(num5, TechniqueArt.Cyan),
			(num7, TechniqueArt.Gold)
		};
		for (int i = 0; i < array.Length; i++)
		{
			(int, Color) tuple = array[i];
			int item2 = tuple.Item1;
			Color item3 = tuple.Item2;
			int val = (int)MathF.Round((float)num9 * Math.Clamp((float)item2 / echelle, 0f, 1f));
			val = Math.Min(val, rect2.X + 1 + num9 - num8);
			if (val <= 0)
			{
				continue;
			}
			Rectangle rect3 = new Rectangle(num8, rect2.Y + 1, val, rect2.Height - 2);
			TechniqueArt.Fill(renderer, rect3, A(item3));
			if (item3 == TechniqueArt.Gold)
			{
				for (int j = rect3.X + 3; j < rect3.Right; j += 6)
				{
					TechniqueArt.Fill(renderer, new Rectangle(j, rect3.Y, Math.Min(3, rect3.Right - j), rect3.Height), A(new Color(255, 201, 144, 42)));
				}
			}
			num8 += val;
		}
		TechniqueArt.Outline(renderer, rect2, A(TechniqueArt.Line));
		int right = ligne.X + _d.L(280f);
		TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(24f), SaiyanHud.Amount(num4 + num7), right, (num7 > 0) ? (ligne.Y + _d.L(19f)) : num2, A(TechniqueArt.Text));
		if (num7 > 0)
		{
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(13f), "+" + SaiyanHud.Amount(num7), right, ligne.Y + _d.L(37f), A(TechniqueArt.Gold));
		}
		Rectangle rectangle = new Rectangle(ligne.X + _d.L(342f), num2 - _d.L(14f), _d.L(28f), _d.L(28f));
		bool flag = possible && num4 + num7 < Options.Instance.Player.MaxStat;
		DessinerCase(renderer, rang, flag || num7 > 0);
		BoutonPlus(renderer, rectangle, $"plus:{num}", flag);
		if (flag)
		{
			_boutonsPlus.Add((rectangle, stat));
		}
		BulleSur(ligne, $"stat:{num}", () => BulleStat(me, stat));
	}

	private void BoutonPlus(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle bouton, string cle, bool possible)
	{
		int biais = _d.L(5f);
		bool flag = possible && IsHot(cle);
		if (possible)
		{
			Biais(renderer, bouton, biais, flag ? new Color(255, 255, 236, 164) : new Color(255, 255, 224, 138), flag ? new Color(255, 255, 190, 70) : new Color(255, 255, 176, 46));
		}
		else
		{
			Biais(renderer, bouton, biais, new Color(255, 26, 33, 49), new Color(255, 26, 33, 49));
		}
		int num = Math.Max(2, _d.L(3f));
		int num2 = _d.L(6f);
		Color color = A(possible ? new Color(255, 26, 18, 6) : new Color(255, 62, 74, 100));
		int num3 = bouton.X + bouton.Width / 2;
		int num4 = bouton.Y + bouton.Height / 2;
		TechniqueArt.Fill(renderer, new Rectangle(num3 - num2, num4 - num / 2, num2 * 2, num), color);
		TechniqueArt.Fill(renderer, new Rectangle(num3 - num / 2, num4 - num2, num, num2 * 2), color);
		AddHit(bouton, cle, null);
	}

	private void Biais(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, int biais, Color haut, Color bas)
	{
		for (int i = 0; i < rect.Height; i++)
		{
			float num = (float)i / (float)Math.Max(1, rect.Height - 1);
			HudShapes.Span(renderer, (float)rect.X + (float)biais * (1f - num), (float)rect.Right - (float)biais * num, rect.Y + i, 1, A(SaiyanHud.Lerp(haut, bas, num)));
		}
	}

	private void Pictogramme(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int index, Rectangle rect, Color encre)
	{
		IGameTexture gameTexture = TechniqueArt.Tex("tech_familles.png");
		if (gameTexture != null && gameTexture.Width > 0)
		{
			Lisser(gameTexture, rect, encre, ((float)(index * 40) + 0.5f) / (float)gameTexture.Width, 0f, ((float)((index + 1) * 40) - 0.5f) / (float)gameTexture.Width);
		}
	}

	private void DessinerDerives(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me)
	{
		int num = me.Stat[3];
		int num2 = _attente[3];
		(string, string, string, string, Func<Bulle>)[] array = new(string, string, string, string, Func<Bulle>)[4]
		{
			(Strings.Inventaire.MaxHealth, SaiyanHud.Amount(VitalMax(me, 0)), (VitauxEnAttente(0) > 0) ? ("+" + SaiyanHud.Amount(VitauxEnAttente(0))) : null, "derive:vie", () => BulleVital(me, 0)),
			(Strings.Inventaire.MaxKi, SaiyanHud.Amount(VitalMax(me, 1)), (VitauxEnAttente(1) > 0) ? ("+" + SaiyanHud.Amount(VitauxEnAttente(1))) : null, "derive:ki", () => BulleVital(me, 1)),
			(Strings.Inventaire.CritChance, Pourcent((double)num * 0.1), (num2 > 0) ? ("+" + Pourcent((double)num2 * 0.1).Replace(" %", string.Empty)) : null, "derive:critique", () => BulleStat(me, Stat.MagicResist)),
			(Strings.Inventaire.CritDamage, "+" + Pourcent((double)num * 0.3), (num2 > 0) ? ("+" + Pourcent((double)num2 * 0.3).Replace(" %", string.Empty)) : null, "derive:degats", () => BulleStat(me, Stat.MagicResist))
		};
		float num3 = 91f;
		for (int num4 = 0; num4 < array.Length; num4++)
		{
			(string, string, string, string, Func<Bulle>) tuple = array[num4];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			string item3 = tuple.Item3;
			string item4 = tuple.Item4;
			Func<Bulle> item5 = tuple.Item5;
			Rectangle rect = _d.R(288f + (float)num4 * (num3 + 4f), 406f, num3, 38f);
			TechniqueArt.Fill(renderer, rect, A(new Color(179, 7, 10, 18)));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
			int num5 = rect.X + _d.L(8f);
			string text = item.ToUpperInvariant();
			float spacing = _d.Em(0.14f, 9.5f);
			if (TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(9.5f), text, spacing) > rect.Width - _d.L(14f))
			{
				spacing = _d.Em(0.04f, 9.5f);
			}
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(9.5f), text, num5, rect.Y + _d.L(10f), spacing, A(TechniqueArt.Dim));
			int num6 = num5 + TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(17f), item2, num5, rect.Y + _d.L(26f), A(TechniqueArt.Text));
			if (item3 != null)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(13f), item3, num6 + _d.L(3f), rect.Y + _d.L(27f), A(TechniqueArt.Gold));
			}
			BulleSur(rect, item4, item5);
		}
	}

	private void DessinerBonusPortes(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		Rectangle rectangle = _d.R(288f, 450f, 376f, 44f);
		int hauteur = _d.L(20f);
		int num = _d.L(24f);
		int num2 = _d.L(4f);
		int centerY = rectangle.Y + hauteur / 2;
		List<(string, int)> list = BonusPortes();
		if (list.Count == 0)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), Strings.Inventaire.NoBonusFromGear, rectangle.X, centerY, A(TechniqueArt.Dim));
			BulleSur(new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, hauteur), "bonus", BulleBonus);
			return;
		}
		List<string> list2 = list.Select<(string, int), string>(((string Nom, int Pourcent) effet) => effet.Nom + " " + PersonnageScreen.Signe(effet.Pourcent, pourcent: true)).ToList();
		List<int> list3 = list2.Select((string texte) => TechniqueArt.Measure(renderer, TechniqueArt.Body, Pt(12f), texte).X + _d.L(16f)).ToList();
		int num3 = rectangle.X + TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), Strings.Inventaire.BonusFromGear, rectangle.X, centerY, A(TechniqueArt.Dim)) + _d.L(6f);
		int num4 = 0;
		List<(int, int, int)> list4 = new List<(int, int, int)>();
		int num5 = 0;
		for (int num6 = 0; num6 < list2.Count; num6++)
		{
			if (num3 + list3[num6] > rectangle.Right)
			{
				num4++;
				num3 = rectangle.X;
			}
			if (num4 > 1)
			{
				num5 = list2.Count - num6;
				break;
			}
			list4.Add((num6, num3, num4));
			num3 += list3[num6] + num2;
		}
		int gauche = rectangle.X;
		if (num5 > 0)
		{
			while (list4.Count > 0)
			{
				if (list4[list4.Count - 1].Item3 != 1)
				{
					break;
				}
				(int, int, int) tuple = list4[list4.Count - 1];
				int num7 = tuple.Item2 + list3[tuple.Item1] + num2;
				if (num7 + LargeurReste(num5) <= rectangle.Right)
				{
					gauche = num7;
					break;
				}
				list4.RemoveAt(list4.Count - 1);
				num5++;
			}
		}
		foreach (var (index, gauche2, num8) in list4)
		{
			Pastille(list2[index], gauche2, rectangle.Y + num8 * num, list3[index]);
		}
		if (num5 > 0)
		{
			Pastille("+" + num5.ToString(CultureInfo.InvariantCulture), gauche, rectangle.Y + num, LargeurReste(num5));
		}
		BulleSur(new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, (num4 > 0 || num5 > 0) ? (num + hauteur) : hauteur), "bonus", BulleBonus);
		int LargeurReste(int nombre)
		{
			return TechniqueArt.Measure(renderer, TechniqueArt.Body, Pt(12f), "+" + nombre.ToString(CultureInfo.InvariantCulture)).X + _d.L(16f);
		}
		void Pastille(string texte, int x, int haut, int largeur)
		{
			Rectangle rect = new Rectangle(x, haut, largeur, hauteur);
			TechniqueArt.Fill(renderer, rect, A(TechniqueArt.Cyan, 0.1f));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Cyan, 0.35f));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), texte, rect.X + _d.L(8f), rect.Y + hauteur / 2, A(new Color(255, 207, 238, 255)));
		}
	}

	private List<(string Nom, int Pourcent)> BonusPortes()
	{
		List<(string, int)> list = new List<(string, int)>();
		ItemEffect[] effetsMontres = PersonnageScreen.EffetsMontres;
		for (int i = 0; i < effetsMontres.Length; i++)
		{
			ItemEffect itemEffect = effetsMontres[i];
			int valueOrDefault = _apports.Effets.GetValueOrDefault(itemEffect);
			if (valueOrDefault != 0)
			{
				LocalizedString value;
				string item = ((itemEffect == ItemEffect.EXP) ? Strings.Character.ExperienceBonus.ToString() : (Strings.ItemDescription.BonusEffects.TryGetValue((int)itemEffect, out value) ? PersonnageScreen.Libelle(value) : itemEffect.ToString()));
				list.Add((item, valueOrDefault));
			}
		}
		return list;
	}

	private void DessinerActions(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, int libres, int enAttente)
	{
		Rectangle zone = _d.R(288f, 498f, 376f, 30f);
		int num = zone.Y + zone.Height / 2;
		if (enAttente <= 0)
		{
			string text = ((libres > 0) ? Strings.Inventaire.TypeOrHold.ToString() : Strings.Inventaire.PointAStat.ToString());
			int width = zone.Width;
			Color color = TechniqueArt.Dim;
			if (PointsPlaces(me) > 0)
			{
				(int Gauche, string? Explication, bool EnOr) tuple = DessinerReinitialiser(renderer, me, zone, num);
				int item = tuple.Gauche;
				string item2 = tuple.Explication;
				bool item3 = tuple.EnOr;
				width = item - _d.L(8f) - zone.X;
				if (item2 != null)
				{
					text = item2;
					color = (item3 ? TechniqueArt.Gold : TechniqueArt.Soft);
				}
			}
			string text2 = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), text, width, 1).FirstOrDefault();
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), text2, zone.X, num, A(color));
			return;
		}
		string text3 = Strings.Inventaire.Validate.ToString().ToUpperInvariant();
		int num2 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Display, Pt(17f), text3, _d.Em(0.08f, 17f)) + _d.L(36f);
		Rectangle rect = new Rectangle(zone.Right - num2, num - _d.L(14f), num2, _d.L(28f));
		bool flag = IsHot("valider");
		Biais(renderer, rect, _d.L(8f), flag ? TechniqueArt.White : new Color(255, 255, 241, 192), flag ? new Color(255, 255, 150, 40) : new Color(255, 255, 138, 26));
		TechniqueArt.SpacedCenter(renderer, TechniqueArt.Display, Pt(17f), text3, rect.X + rect.Width / 2, num, _d.Em(0.08f, 17f), A(new Color(255, 20, 10, 2)));
		AddHit(rect, "valider", delegate
		{
			Valider(me);
		}, string.Empty);
		string text4 = Strings.Inventaire.Cancel.ToString().ToUpperInvariant();
		int num3 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(12f), text4, _d.Em(0.12f, 12f)) + _d.L(28f);
		Rectangle rect2 = new Rectangle(rect.X - _d.L(8f) - num3, num - _d.L(14f), num3, _d.L(28f));
		bool flag2 = IsHot("annuler");
		TechniqueArt.Fill(renderer, rect2, A(new Color(255, 18, 26, 42)));
		TechniqueArt.Outline(renderer, rect2, A(flag2 ? TechniqueArt.Soft : TechniqueArt.Line2));
		TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(12f), text4, rect2.X + rect2.Width / 2, num, _d.Em(0.12f, 12f), A(flag2 ? TechniqueArt.White : TechniqueArt.Soft));
		AddHit(rect2, "annuler", delegate
		{
			AnnulerPoints();
			Son("retirer");
		}, string.Empty);
		string text5 = ((enAttente > 1) ? Strings.Inventaire.PendingMany : Strings.Inventaire.PendingOne).ToString(enAttente);
		int width2 = rect2.X - _d.L(8f) - zone.X;
		string text6 = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), text5, width2, 1).FirstOrDefault();
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), text6, zone.X, num, A(TechniqueArt.Gold));
	}

	private (int Gauche, string? Explication, bool EnOr) DessinerReinitialiser(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, Rectangle zone, int centre)
	{
		long num = PrixPourReinitialiser(me);
		string text = SaiyanHud.Amount(num);
		bool assez = me.Zeni >= num;
		bool demande = assez && _reinitialiserDemandeA != 0L && Timing.Global.MillisecondsUtc - _reinitialiserDemandeA < 4000;
		string text2 = (demande ? Strings.Inventaire.ResetConfirm.ToString(text) : Strings.Inventaire.ResetPoints.ToString()).ToUpperInvariant();
		int num2 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(12f), text2, _d.Em(0.12f, 12f)) + _d.L(28f);
		Rectangle rect = new Rectangle(zone.Right - num2, centre - _d.L(14f), num2, _d.L(28f));
		bool flag = IsHot("reinitialiser");
		if (demande)
		{
			Biais(renderer, rect, _d.L(8f), flag ? TechniqueArt.White : new Color(255, 255, 241, 192), flag ? new Color(255, 255, 150, 40) : new Color(255, 255, 138, 26));
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(12f), text2, rect.X + rect.Width / 2, centre, _d.Em(0.12f, 12f), A(new Color(255, 20, 10, 2)));
		}
		else
		{
			TechniqueArt.Fill(renderer, rect, A(new Color(255, 18, 26, 42)));
			TechniqueArt.Outline(renderer, rect, A((flag && assez) ? TechniqueArt.Soft : TechniqueArt.Line2));
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(12f), text2, rect.X + rect.Width / 2, centre, _d.Em(0.12f, 12f), A((!assez) ? TechniqueArt.Dim : (flag ? TechniqueArt.White : TechniqueArt.Soft)));
		}
		AddHit(rect, "reinitialiser", delegate
		{
			if (!assez)
			{
				Son("refus");
			}
			else if (!demande)
			{
				_reinitialiserDemandeA = Timing.Global.MillisecondsUtc;
				Son("point");
			}
			else
			{
				_reinitialiserDemandeA = 0L;
				AnnulerPoints();
				PacketSender.SendReinitialiserLesPoints();
				Son("retirer");
			}
		}, string.Empty);
		if (demande)
		{
			return (Gauche: rect.X, Explication: Strings.Inventaire.ResetConfirmHint.ToString(text), EnOr: true);
		}
		if (flag)
		{
			return (Gauche: rect.X, Explication: (assez ? Strings.Inventaire.ResetHint : Strings.Inventaire.ResetNeedZeni).ToString(text), EnOr: false);
		}
		return (Gauche: rect.X, Explication: null, EnOr: false);
	}

	private void ConstruireCasesPoints()
	{
		Stat[] statsMontrees = PersonnageScreen.StatsMontrees;
		foreach (Stat stat in statsMontrees)
		{
			CasePoints champ = new CasePoints(this, stat)
			{
				Font = TechniqueArt.Heavy,
				FontSize = 12,
				ShouldDrawBackground = false,
				MaximumLength = 4,
				IsHidden = true,
				TextAlign = Pos.Center,
				SelectAllOnFocus = true
			};
			champ.SetTextColor(TechniqueArt.Gold, ComponentState.Normal);
			champ.TextChanged += delegate
			{
				CaseTapee(champ);
			};
			Interface.FocusComponents.Add(champ);
			_casesPoints.Add(champ);
		}
	}

	private Rectangle RectCase(int rang)
	{
		return _d.R(580f, 154f + (float)(rang * 50) + 9f, 46f, 28f);
	}

	private void PlacerCasesPoints()
	{
		bool flag = _onglet == Onglet.Caracteristiques && Globals.Me != null;
		for (int i = 0; i < _casesPoints.Count; i++)
		{
			CasePoints casePoints = _casesPoints[i];
			if (!flag)
			{
				if (casePoints.HasFocus)
				{
					casePoints.Blur();
				}
				if (!casePoints.IsHidden)
				{
					casePoints.IsHidden = true;
				}
				continue;
			}
			if (casePoints.IsHidden)
			{
				casePoints.IsHidden = false;
			}
			Rectangle rectangle = RectCase(i);
			if (casePoints.FontSize != Pt(16f))
			{
				casePoints.FontSize = Pt(16f);
			}
			if (casePoints.X != rectangle.X || casePoints.Y != rectangle.Y || casePoints.Width != rectangle.Width || casePoints.Height != rectangle.Height)
			{
				casePoints.SetBounds(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
			}
			int num = _attente[(int)casePoints.Stat];
			if (!casePoints.HasFocus)
			{
				string text = ((num > 0) ? num.ToString(CultureInfo.InvariantCulture) : string.Empty);
				if (casePoints.Text != text)
				{
					Ecrire(casePoints, text);
				}
			}
			bool flag2 = num > 0 || Globals.Me.StatPoints - PointsEnAttente > 0;
			if (casePoints.MouseInputEnabled != flag2)
			{
				casePoints.MouseInputEnabled = flag2;
			}
		}
	}

	private void DessinerCase(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int rang, bool active)
	{
		if (rang >= 0 && rang < _casesPoints.Count)
		{
			CasePoints casePoints = _casesPoints[rang];
			Rectangle rect = RectCase(rang);
			bool hasFocus = casePoints.HasFocus;
			int num = _attente[(int)casePoints.Stat];
			float alpha = (active ? 1f : 0.45f);
			if (hasFocus)
			{
				TechniqueArt.GlowRect(renderer, new Rectangle(rect.X - _d.L(6f), rect.Y - _d.L(6f), rect.Width + _d.L(12f), rect.Height + _d.L(12f)), A(TechniqueArt.Gold, 0.25f), additive: false);
			}
			TechniqueArt.Fill(renderer, rect, A(new Color(255, 7, 10, 18), alpha));
			TechniqueArt.Outline(renderer, rect, A(hasFocus ? TechniqueArt.Gold : ((num > 0) ? TechniqueArt.Alpha(TechniqueArt.Gold, 0.6f) : TechniqueArt.Line2), alpha));
			if (!hasFocus && num == 0)
			{
				TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(16f), "0", rect.X + rect.Width / 2, rect.Y + rect.Height / 2, A(new Color(255, 62, 74, 100), alpha));
			}
		}
	}

	private void Ecrire(CasePoints champ, string texte)
	{
		_majCase = true;
		try
		{
			champ.Text = texte;
			champ.CursorPos = texte.Length;
			champ.CursorEnd = texte.Length;
		}
		finally
		{
			_majCase = false;
		}
	}

	private void CaseTapee(CasePoints champ)
	{
		if (_majCase)
		{
			return;
		}
		Player me = Globals.Me;
		if (me != null)
		{
			int stat = (int)champ.Stat;
			int num = PointsEnAttente - _attente[stat];
			int num2 = Math.Max(0, Math.Min(me.StatPoints - num, Options.Instance.Player.MaxStat - me.Stat[stat]));
			string text = new string((champ.Text ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
			int result;
			int val = ((text.Length != 0) ? (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out result) ? result : num2) : 0);
			int num3 = Math.Min(val, num2);
			_attente[stat] = num3;
			string text2 = ((text.Length == 0) ? string.Empty : num3.ToString(CultureInfo.InvariantCulture));
			if (champ.Text != text2)
			{
				Ecrire(champ, text2);
			}
		}
	}

	private void CaseEnMain(CasePoints champ)
	{
		_avantSaisie = _attente[(int)champ.Stat];
	}

	private void CaseLachee(CasePoints champ)
	{
		if (_attente[(int)champ.Stat] != _avantSaisie)
		{
			Son("point");
		}
		_avantSaisie = _attente[(int)champ.Stat];
	}

	private void CaseEchappee(CasePoints champ)
	{
		_attente[(int)champ.Stat] = _avantSaisie;
		Ecrire(champ, (_avantSaisie > 0) ? _avantSaisie.ToString(CultureInfo.InvariantCulture) : string.Empty);
		_echapA = Timing.Global.MillisecondsUtc;
	}

	private void LacherLesCases()
	{
		foreach (CasePoints casesPoint in _casesPoints)
		{
			if (casesPoint.HasFocus)
			{
				casesPoint.Blur();
			}
		}
	}

	public FenetreInventaire(Canvas gameCanvas)
		: base(gameCanvas, "FenetreInventaire")
	{
		ConstruireRecherche();
		ConstruireCasesPoints();
	}

	public void Basculer(Onglet onglet)
	{
		if (IsVisible() && _onglet == onglet)
		{
			Hide();
		}
		else
		{
			Ouvrir(onglet);
		}
	}

	public void Ouvrir(Onglet onglet)
	{
		if (!IsVisible())
		{
			_onglet = onglet;
			Show();
		}
		else
		{
			ChangerOnglet(onglet);
		}
	}

	private void ChangerOnglet(Onglet onglet)
	{
		if (onglet != _onglet)
		{
			AnnulerGlisser();
			LacherLesCases();
			if (onglet != Onglet.Sac)
			{
				FermerRecherche();
			}
			_onglet = onglet;
			_bulle = null;
			_essaiTitre = null;
			_essaiOrnement = null;
			Son("onglet");
		}
	}

	public override void Show()
	{
		_lu = false;
		_equipementLu = null;
		if (base.IsHidden)
		{
			_filtreEmplacement = -1;
		}
		base.Show();
	}

	public override void Hide()
	{
		if (_echapA > 0 && Timing.Global.MillisecondsUtc - _echapA < 150)
		{
			_echapA = 0L;
			return;
		}
		if (IsVisible())
		{
			Son("fermer");
		}
		AnnulerGlisser();
		LacherLesCases();
		FermerRecherche();
		AnnulerPoints();
		_essaiTitre = null;
		_essaiOrnement = null;
		base.Hide();
	}

	public void Update()
	{
		if (!base.IsHidden)
		{
			if (CharteScreen.EstOuvert)
			{
				Hide();
				return;
			}
			Placer();
			PlacerRecherche();
			PlacerCasesPoints();
			SuivreLesPoints();
		}
	}

	protected override void Dessiner(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		_cases.Clear();
		_hexagones.Clear();
		_lignesPanoplies.Clear();
		_zonesBulles.Clear();
		DessinerCadre(renderer);
		Player me = Globals.Me;
		if (me != null)
		{
			LireLeSac(me);
			ComposerLaVue();
			if (!_apportsLus)
			{
				SuivreLEquipement(me);
			}
			DessinerTete(renderer, me, now);
			DessinerOnglets(renderer, me);
			switch (_onglet)
			{
			case Onglet.Caracteristiques:
				DessinerCaracteristiques(renderer, me, now);
				break;
			case Onglet.Apparence:
				DessinerApparence(renderer, me);
				break;
			default:
				DessinerOutils(renderer);
				DessinerGrille(renderer, me, now);
				DessinerBasDuSac(renderer, me);
				break;
			}
			_essai = CaseEssayee();
			DessinerGuerrier(renderer, me, now);
			SonnerLeSurvol();
		}
	}

	private int CaseEssayee()
	{
		if (_onglet != Onglet.Sac || !base.Survolee)
		{
			return -1;
		}
		Point point = LocalMouse();
		foreach (var (rectangle, num) in _cases)
		{
			Entree value;
			if (rectangle.Contains(point.X, point.Y))
			{
				return (_parCase.TryGetValue(num, out value) && value.Objet.ItemType == ItemType.Equipment) ? num : (-1);
			}
		}
		return -1;
	}

	protected override void DessinerDessus(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		Player me = Globals.Me;
		if (me != null)
		{
			DessinerAvis(renderer, now);
			if (_glisse)
			{
				DessinerObjetTenu(renderer, me);
				return;
			}
			DessinerEtiquette(renderer, now);
			DessinerBulleDuSac(renderer, me);
		}
	}

	private void DessinerTete(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, long now)
	{
		int num = _d.Y(25f);
		TechniqueArt.Fill(renderer, new Rectangle(1, _d.Y(50f), base.Width - 2, 1), A(TechniqueArt.White, 0.04f));
		string text = (me.Name ?? string.Empty).ToUpperInvariant();
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(28f), text);
		int num2 = _d.X(22f);
		TechniqueArt.Styled(renderer, TechniqueArt.Display, Pt(28f), text, num2, num - point.Y / 2, new TechniqueArt.Stops(TechniqueArt.White, 0.35f, new Color(255, 255, 194, 122), 0.8f, TechniqueArt.Ki), _fade);
		num2 += point.X + _d.L(14f);
		ClassDescriptor databaseObject;
		string text2 = (DatabaseObject<ClassDescriptor>.TryGet(me.Class, out databaseObject) ? Traduction.Contenu(databaseObject.Name) : string.Empty);
		string text3 = (string.IsNullOrEmpty(text2) ? Strings.Inventaire.LevelOnly.ToString(me.Level) : Strings.Inventaire.ClassAndLevel.ToString(text2, me.Level)).ToUpperInvariant();
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(11f), text3, num2, num + _d.L(3f), _d.Em(0.18f, 11f), A(TechniqueArt.Dim));
		Rectangle rect = _d.R(Place.Width - 41, 10f, 30f, 30f);
		bool flag = IsHot("fermer");
		if (flag)
		{
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line2));
		}
		int num3 = _d.L(14f);
		Lisser("tech_croix.png", new Rectangle(rect.X + (rect.Width - num3) / 2, rect.Y + (rect.Height - num3) / 2, num3, num3), A(flag ? TechniqueArt.White : TechniqueArt.Soft));
		AddHit(rect, "fermer", Hide, string.Empty);
		DessinerBourse(renderer, me, rect.X, now);
	}

	private void DessinerBourse(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, int croix, long now)
	{
		int num = _d.Y(25f);
		int num2 = croix - _d.L(14f);
		num2 -= TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(19f), SaiyanHud.Amount(me.Zeni), num2, num, A(TechniqueArt.Gold));
		num2 -= _d.L(6f);
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Item, "zeni_anime.png");
		if (texture != null && texture.Width > 0 && texture.Height > 0)
		{
			long num3 = now % 1900;
			int num4 = ((num3 >= 1615) ? ((num3 < 1710) ? 1 : ((num3 < 1805) ? 2 : 3)) : 0);
			int height = texture.Height;
			int num5 = _d.L(20f);
			Lisser(texture, new Rectangle(num2 - num5, num - num5 / 2, num5, num5), A(TechniqueArt.White), ((float)(num4 * height) + 0.5f) / (float)texture.Width, 0f, ((float)((num4 + 1) * height) - 0.5f) / (float)texture.Width);
		}
	}

	private void DessinerOnglets(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me)
	{
		int num = _d.Y(50f);
		int num2 = _d.Y(86f);
		TechniqueArt.Fill(renderer, new Rectangle(_d.X(16f), num2 - 1, _d.L(Place.Width - 32), 1), A(TechniqueArt.White, 0.07f));
		int num3 = _d.X(16f);
		float spacing = _d.Em(0.16f, 12f);
		(Onglet, string, Control?)[] array = new(Onglet, string, Control?)[3]
		{
			(Onglet.Sac, Strings.Inventaire.TabBag, Control.OpenInventory),
			(Onglet.Caracteristiques, Strings.Inventaire.TabStats, Control.OpenCharacterInfo),
			(Onglet.Apparence, Strings.Inventaire.TabLook, null)
		};
		int num4 = Math.Max(0, me.StatPoints - PointsEnAttente);
		Player.Charge charge = me.LireLaCharge();
		(Onglet, string, Control?)[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			(Onglet, string, Control?) tuple = array2[i];
			Onglet item = tuple.Item1;
			string item2 = tuple.Item2;
			Control? item3 = tuple.Item3;
			string text = item2.ToUpperInvariant();
			string text2;
			if (item3.HasValue)
			{
				Control valueOrDefault = item3.GetValueOrDefault();
				text2 = ToucheCourte(valueOrDefault);
			}
			else
			{
				text2 = string.Empty;
			}
			string text3 = text2;
			int num5 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(12f), text, spacing);
			int num6 = _d.L(14f) + num5;
			if (text3.Length > 0)
			{
				num6 += _d.L(8f) + LargeurTouche(renderer, text3);
			}
			bool flag = item == Onglet.Sac && charge.Surcharge;
			string text4 = ((item == Onglet.Caracteristiques && num4 > 0) ? num4.ToString(CultureInfo.InvariantCulture) : (flag ? $"{charge.Pourcent} %" : null));
			int num7 = ((text4 != null) ? (TechniqueArt.Measure(renderer, TechniqueArt.Heavy, Pt(13f), text4).X + _d.L(14f)) : 0);
			if (text4 != null)
			{
				num6 += _d.L(8f) + num7;
			}
			num6 += _d.L(14f);
			Rectangle rect = new Rectangle(num3, num, num6, num2 - num);
			string cle = $"onglet:{item}";
			bool flag2 = item == _onglet;
			bool flag3 = IsHot(cle);
			if (flag2)
			{
				TechniqueArt.FillVertical(renderer, rect, A(new Color(0, 255, 122, 26)), A(new Color(255, 255, 122, 26), 0.16f));
				Rectangle rect2 = new Rectangle(rect.X + _d.L(6f), num2 - 2, rect.Width - _d.L(12f), 2);
				TechniqueArt.FillHorizontal(renderer, rect2, A(TechniqueArt.Ki, 0.2f), A(new Color(255, 255, 194, 122)), A(TechniqueArt.Ki, 0.2f));
				TechniqueArt.GlowRect(renderer, new Rectangle(rect2.X, rect2.Y - _d.L(3f), rect2.Width, _d.L(8f)), A(TechniqueArt.Ki, 0.45f));
			}
			int num8 = num + (num2 - num) / 2;
			int num9 = rect.X + _d.L(14f);
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), text, num9, num8, spacing, A(flag2 ? TechniqueArt.White : (flag3 ? TechniqueArt.Text : TechniqueArt.Dim)));
			num9 += num5;
			if (text3.Length > 0)
			{
				num9 += _d.L(8f);
				num9 += Touche(renderer, text3, num9, num8);
			}
			if (text4 != null)
			{
				num9 += _d.L(8f);
				Rectangle rect3 = new Rectangle(num9, num8 - _d.L(9f), num7, _d.L(18f));
				if (flag)
				{
					TechniqueArt.Fill(renderer, rect3, A(new Color(255, 74, 18, 32)));
					TechniqueArt.Outline(renderer, rect3, A(new Color(255, 255, 100, 116)));
					TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(13f), text4, rect3.X + rect3.Width / 2, num8, A(new Color(255, 255, 214, 218)));
				}
				else
				{
					TechniqueArt.FillVertical(renderer, rect3, A(new Color(255, 255, 224, 138)), A(new Color(255, 255, 176, 46)));
					TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(13f), text4, rect3.X + rect3.Width / 2, num8, A(new Color(255, 26, 18, 6)));
				}
			}
			Onglet choix = item;
			AddHit(rect, cle, delegate
			{
				ChangerOnglet(choix);
			}, string.Empty);
			num3 += num6 + _d.L(4f);
		}
	}

	private static string ToucheCourte(Control controle)
	{
		string text = DropdownPanel.KeyOf(controle);
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}
		text = text.Trim().ToUpperInvariant();
		if (text.Length <= 5)
		{
			return text;
		}
		return string.Empty;
	}

	private bool RefuserSiTropBas(Player me, Entree entree)
	{
		if (entree.NiveauRequis <= me.Level)
		{
			return false;
		}
		_refusEmplacement = entree.Objet.EquipmentSlot;
		_refusA = Timing.Global.MillisecondsUtc;
		Annoncer(Strings.Inventaire.CannotWear.ToString(Strings.Inventaire.LevelRequired.ToString(entree.NiveauRequis), entree.Objet.Name));
		Son("refus");
		return true;
	}

	private void Annoncer(string texte)
	{
		_avis = texte;
		_avisA = Timing.Global.MillisecondsUtc;
	}

	private void AnnoncerRaccourci(string nom, int raccourci)
	{
		Son("raccourci");
		string text = ((raccourci >= 0 && raccourci < 10) ? DropdownPanel.KeyOf((Control)(4097 + raccourci)) : null);
		Annoncer(string.IsNullOrWhiteSpace(text) ? Strings.Inventaire.PutOnBar.ToString(nom) : Strings.Inventaire.PutOnKey.ToString(nom, text.Trim().ToUpperInvariant()));
	}

	private void DessinerAvis(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		string avis = _avis;
		if (avis != null)
		{
			long num = now - _avisA;
			if (num > 1600)
			{
				_avis = null;
				return;
			}
			float alpha = ((num < 1300) ? 1f : (1f - (float)(num - 1300) / 300f));
			Point point = TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(13f), avis);
			Rectangle rect = new Rectangle(base.Width / 2 - point.X / 2 - _d.L(14f), base.Height + _d.L(6f), point.X + _d.L(28f), _d.L(28f));
			TechniqueArt.Fill(renderer, rect, A(new Color(242, 8, 11, 19), alpha));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Gold, alpha));
			TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(13f), avis, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, A(TechniqueArt.Text, alpha));
		}
	}

	private static Categorie CategorieDe(ItemDescriptor objet)
	{
		return objet.ItemType switch
		{
			ItemType.Equipment => Categorie.Equipement, 
			ItemType.Consumable => Categorie.Consommables, 
			ItemType.Bag => Categorie.Sacs, 
			ItemType.Event => Categorie.Quete, 
			ItemType.Spell => Categorie.Techniques, 
			_ => Categorie.Ressources, 
		};
	}

	private void LireLeSac(Player me)
	{
		IItem[] inventory = me.Inventory;
		HashCode hashCode = default(HashCode);
		hashCode.Add(me.Level);
		foreach (IItem item in inventory)
		{
			hashCode.Add(item?.ItemId ?? Guid.Empty);
			hashCode.Add(item?.Quantity ?? 0);
			int[] array = item?.ItemProperties?.StatModifiers;
			if (array != null)
			{
				int[] array2 = array;
				foreach (int value in array2)
				{
					hashCode.Add(value);
				}
			}
		}
		int[] myEquipment = me.MyEquipment;
		foreach (int value2 in myEquipment)
		{
			hashCode.Add(value2);
		}
		int num = hashCode.ToHashCode();
		if (_lu && num == _signature)
		{
			return;
		}
		_lu = true;
		_signature = num;
		_vueSale = true;
		_bulle = null;
		_apportsLus = false;
		_objets.Clear();
		_parCase.Clear();
		_vides.Clear();
		Array.Clear(_comptes);
		for (int l = 0; l < inventory.Length; l++)
		{
			IItem item2 = inventory[l];
			if (item2 == null || !DatabaseObject<ItemDescriptor>.TryGet(item2.ItemId, out var databaseObject))
			{
				_vides.Add(l);
			}
			else if (Array.IndexOf(me.MyEquipment, l) < 0)
			{
				Categorie categorie = CategorieDe(databaseObject);
				int num2 = NiveauRequis(databaseObject);
				Entree entree = new Entree(l, databaseObject, item2.Quantity, JetDObjet.Rarete(databaseObject, item2.ItemProperties), categorie, databaseObject.ItemType == ItemType.Equipment && num2 <= me.Level && PersonnageScreen.MeilleurPartout(me, l, databaseObject), num2);
				_objets.Add(entree);
				_parCase[l] = entree;
				_comptes[(int)categorie]++;
			}
		}
		_comptes[0] = _objets.Count;
		if (_comptes[(int)_categorie] == 0)
		{
			_categorie = Categorie.Tout;
		}
		_panopliesPortees.Clear();
		HashSet<Guid> hashSet = PersonnageScreen.IdsPortes(me);
		foreach (Guid item3 in hashSet)
		{
			PanoplieDescriptor panoplie = PanoplieDescriptor.DeLObjet(item3);
			if (panoplie != null && !_panopliesPortees.Exists(((PanoplieDescriptor Panoplie, int Pieces, int Total) tuple) => tuple.Panoplie == panoplie))
			{
				_panopliesPortees.Add((panoplie, panoplie.PiecesPortees(hashSet), panoplie.Items.Distinct().Count()));
			}
		}
		_panopliesPortees.Sort(((PanoplieDescriptor Panoplie, int Pieces, int Total) a, (PanoplieDescriptor Panoplie, int Pieces, int Total) b) => b.Pieces.CompareTo(a.Pieces));
	}

	private void ComposerLaVue()
	{
		if (!_vueSale)
		{
			return;
		}
		_vueSale = false;
		_montrees.Clear();
		foreach (Entree objet in _objets)
		{
			if (_filtreEmplacement >= 0)
			{
				if (objet.Objet.ItemType != ItemType.Equipment || objet.Objet.EquipmentSlot != _filtreEmplacement)
				{
					continue;
				}
			}
			else if (_categorie != Categorie.Tout && objet.Categorie != _categorie)
			{
				continue;
			}
			if (_requete.Length <= 0 || PersonnageScreen.Normaliser(objet.Objet.Name).Contains(_requete))
			{
				_montrees.Add(objet.Case);
			}
		}
		if (_range)
		{
			_montrees.Sort(ComparerRanges);
		}
		if (_categorie == Categorie.Tout && _requete.Length == 0 && _filtreEmplacement < 0)
		{
			int num = Math.Max(56, (_montrees.Count / 8 + 1) * 8);
			int num2 = Math.Min(_vides.Count, Math.Min(Math.Max(0, Player.CapaciteDuSac - _objets.Count), num - _montrees.Count));
			for (int i = 0; i < num2; i++)
			{
				_montrees.Add(_vides[i]);
			}
		}
	}

	private int ComparerRanges(int premiere, int seconde)
	{
		Entree entree = _parCase[premiere];
		Entree entree2 = _parCase[seconde];
		int num = ((int)entree.Categorie).CompareTo((int)entree2.Categorie);
		if (num != 0)
		{
			return num;
		}
		num = entree2.Rarete.CompareTo(entree.Rarete);
		if (num != 0)
		{
			return num;
		}
		num = string.Compare(PersonnageScreen.Normaliser(entree.Objet.Name), PersonnageScreen.Normaliser(entree2.Objet.Name), StringComparison.Ordinal);
		if (num == 0)
		{
			return entree2.Quantite.CompareTo(entree.Quantite);
		}
		return num;
	}

	private int? CaseSacSous(Point local)
	{
		if (_onglet != Onglet.Sac)
		{
			return null;
		}
		foreach (var (rectangle, value) in _cases)
		{
			if (rectangle.Contains(local.X, local.Y))
			{
				return value;
			}
		}
		return null;
	}

	private int? EmplacementSous(Point local)
	{
		foreach (var (rectangle, value) in _hexagones)
		{
			if (rectangle.Contains(local.X, local.Y))
			{
				return value;
			}
		}
		return null;
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
			if (_parCase.ContainsKey(valueOrDefault))
			{
				_appuiSac = valueOrDefault;
				_appuiPosition = local;
				Son("prendre");
				return;
			}
		}
		num = EmplacementSous(local);
		if (num.HasValue)
		{
			int valueOrDefault2 = num.GetValueOrDefault();
			if (PersonnageScreen.ObjetPorte(me, valueOrDefault2, out var _) != null)
			{
				_appuiEmplacement = valueOrDefault2;
				_appuiPosition = local;
				Son("prendre");
			}
		}
	}

	private void SuivreAppui(Point local)
	{
		if (!_glisse && (_appuiSac >= 0 || _appuiEmplacement >= 0) && Math.Abs(local.X - _appuiPosition.X) + Math.Abs(local.Y - _appuiPosition.Y) > _d.L(8f))
		{
			_glisse = true;
			_essai = -1;
		}
	}

	private void AnnulerGlisser()
	{
		_appuiSac = -1;
		_appuiEmplacement = -1;
		_glisse = false;
	}

	private ItemDescriptor? ObjetTenu(Player me)
	{
		if (_appuiSac >= 0)
		{
			if (!_parCase.TryGetValue(_appuiSac, out var value))
			{
				return null;
			}
			return value.Objet;
		}
		if (_appuiEmplacement < 0)
		{
			return null;
		}
		int caseSac;
		return PersonnageScreen.ObjetPorte(me, _appuiEmplacement, out caseSac);
	}

	private void Equiper(Player me, Entree entree)
	{
		if (!RefuserSiTropBas(me, entree))
		{
			me.TryUseItem(entree.Case);
			Son("equiper");
		}
	}

	private void Retirer(int emplacement)
	{
		PacketSender.SendUnequipItem(emplacement);
		Son("retirer");
	}

	private void Lacher(Point local)
	{
		int appuiSac = _appuiSac;
		int appuiEmplacement = _appuiEmplacement;
		AnnulerGlisser();
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		bool flag = local.X >= 0 && local.Y >= 0 && local.X < base.Width && local.Y < base.Height;
		if (appuiSac >= 0)
		{
			if (!_parCase.TryGetValue(appuiSac, out var value))
			{
				return;
			}
			int? num;
			if (!flag)
			{
				num = FenetreCharte.RaccourciSousLeCurseur();
				if (num.HasValue)
				{
					int valueOrDefault = num.GetValueOrDefault();
					me.AddToHotbar(valueOrDefault, 0, appuiSac);
					AnnoncerRaccourci(value.Objet.Name, valueOrDefault);
				}
				else if (value.Objet.CanDrop && !me.IsBusy && !Interface.DoesMouseHitInterface())
				{
					me.TryDropItem(appuiSac);
				}
				return;
			}
			num = CaseSacSous(local);
			if (num.HasValue)
			{
				int valueOrDefault2 = num.GetValueOrDefault();
				if (valueOrDefault2 != appuiSac)
				{
					me.SwapItems(appuiSac, valueOrDefault2);
				}
				return;
			}
			num = EmplacementSous(local);
			if (num.HasValue)
			{
				int valueOrDefault3 = num.GetValueOrDefault();
				if (value.Objet.ItemType == ItemType.Equipment && value.Objet.EquipmentSlot == valueOrDefault3)
				{
					Equiper(me, value);
				}
			}
		}
		else
		{
			if (appuiEmplacement < 0)
			{
				return;
			}
			int caseSac;
			ItemDescriptor itemDescriptor = PersonnageScreen.ObjetPorte(me, appuiEmplacement, out caseSac);
			if (itemDescriptor == null)
			{
				return;
			}
			if (!flag)
			{
				if (caseSac >= 0)
				{
					int? num = FenetreCharte.RaccourciSousLeCurseur();
					if (num.HasValue)
					{
						int valueOrDefault4 = num.GetValueOrDefault();
						me.AddToHotbar(valueOrDefault4, 0, caseSac);
						AnnoncerRaccourci(itemDescriptor.Name, valueOrDefault4);
					}
				}
			}
			else if (_onglet == Onglet.Sac && _zoneSac.Contains(local.X, local.Y))
			{
				Retirer(appuiEmplacement);
			}
		}
	}

	protected override void OnMouseDown(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseDown(mouseButton, mousePosition, userAction);
		LacherLesCases();
		if (mouseButton == MouseButton.Left)
		{
			Point local = CanvasPosToLocal(mousePosition);
			CommencerAppui(local);
			CommencerPlus(local);
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
		bool clicApresDouble = _clicApresDouble;
		_clicApresDouble = false;
		if (mouseButton == MouseButton.Left && !clicApresDouble)
		{
			int? num = EmplacementSous(CanvasPosToLocal(mousePosition));
			if (num.HasValue)
			{
				int valueOrDefault = num.GetValueOrDefault();
				BasculerFiltre(valueOrDefault);
				return;
			}
		}
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
		LacherPlus();
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
		AnnulerGlisser();
		_clicApresDouble = true;
		int? num = CaseSacSous(local);
		if (num.HasValue)
		{
			int valueOrDefault = num.GetValueOrDefault();
			if (_parCase.TryGetValue(valueOrDefault, out var value))
			{
				if (value.Objet.ItemType == ItemType.Equipment)
				{
					Equiper(me, value);
				}
				else
				{
					me.TryUseItem(valueOrDefault);
				}
			}
			return;
		}
		num = EmplacementSous(local);
		if (num.HasValue)
		{
			int valueOrDefault2 = num.GetValueOrDefault();
			if (PersonnageScreen.ObjetPorte(me, valueOrDefault2, out var _) != null)
			{
				Retirer(valueOrDefault2);
			}
		}
	}

	protected override bool OnMouseWheeled(int delta)
	{
		if (_onglet == Onglet.Apparence)
		{
			DefilerApparence(delta);
			return true;
		}
		if (_onglet == Onglet.Sac && DefilerLeSac(delta))
		{
			return true;
		}
		return base.OnMouseWheeled(delta);
	}

	private void BasculerFiltre(int emplacement)
	{
		_filtreEmplacement = ((_filtreEmplacement == emplacement && _onglet == Onglet.Sac) ? (-1) : emplacement);
		ChangerOnglet(Onglet.Sac);
		_rangeeHaut = 0;
		_vueSale = true;
		Son("onglet");
	}

	private Entree? PiecePrise(Player me)
	{
		if (_appuiSac < 0 || !Globals.InputManager.IsMouseButtonDown(MouseButton.Left) || !_parCase.TryGetValue(_appuiSac, out var value))
		{
			return null;
		}
		ItemDescriptor objet = value.Objet;
		if (objet.ItemType != ItemType.Equipment || objet.EquipmentSlot < 0 || objet.EquipmentSlot >= me.MyEquipment.Length)
		{
			return null;
		}
		return value;
	}

	private Entree? PieceEssayee(Player me)
	{
		if (_essai < 0 || !_parCase.TryGetValue(_essai, out var value))
		{
			return null;
		}
		ItemDescriptor objet = value.Objet;
		if (objet.ItemType != ItemType.Equipment || objet.EquipmentSlot < 0 || objet.EquipmentSlot >= me.MyEquipment.Length)
		{
			return null;
		}
		return value;
	}

	private void SuivreLEquipement(Player me)
	{
		int[] myEquipment = me.MyEquipment;
		int[] equipementLu = _equipementLu;
		if (equipementLu != null && equipementLu.Length == myEquipment.Length)
		{
			for (int i = 0; i < myEquipment.Length; i++)
			{
				if (myEquipment[i] != equipementLu[i])
				{
					_eclatA = Timing.Global.MillisecondsUtc;
					if (myEquipment[i] >= 0)
					{
						_eclatEmplacement = i;
					}
					else
					{
						_eclatCase = equipementLu[i];
					}
				}
			}
		}
		_equipementLu = (int[])myEquipment.Clone();
		_apports = PersonnageScreen.ApportsDe(PersonnageScreen.Portes(me));
		_apportsLus = true;
	}

	private float Eclat(long now, bool concerne)
	{
		if (!concerne || now - _eclatA >= 450)
		{
			return 0f;
		}
		return 1f - (float)(now - _eclatA) / 450f;
	}

	private void DessinerGuerrier(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, long now)
	{
		int num = _d.X(144f);
		if (_onglet == Onglet.Apparence)
		{
			TechniqueArt.GlowRect(renderer, _d.R(34f, 112f, 220f, 276f), A(new Color(255, 255, 140, 50), 0.18f), additive: false);
			DessinerDalle(renderer, num, _d.Y(365f), _d.L(75f));
			int zoom = Math.Max(1, (int)MathF.Round(5f * _d.S));
			int sol = _d.Y(367f);
			PersonnageScreen.DessinerPersonnage(renderer, me, now, num, sol, zoom, _rangee, _fade);
			DessinerPlaqueTete(renderer, me, zoom, sol);
			DessinerTourner(renderer, _d.R(28f, 354f, 28f, 22f), _d.R(232f, 354f, 28f, 22f));
		}
		else
		{
			TechniqueArt.GlowRect(renderer, _d.R(54f, 104f, 180f, 250f), A(new Color(255, 255, 140, 50), 0.18f), additive: false);
			DessinerDalle(renderer, num, _d.Y(313f), _d.L(62f));
			int zoom2 = Math.Max(1, (int)MathF.Round(4f * _d.S));
			PersonnageScreen.DessinerPersonnage(renderer, me, now, num, _d.Y(315f), zoom2, _rangee, _fade);
			DessinerTourner(renderer, _d.R(112f, 340f, 28f, 22f), _d.R(148f, 340f, 28f, 22f));
			DessinerEmplacements(renderer, me, now);
		}
		DessinerReperes(renderer, me, now);
	}

	private void DessinerDalle(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int cx, int cy, int rayon)
	{
		Octogone(renderer, cx, cy, rayon, new Color(255, 26, 31, 44), new Color(255, 26, 31, 44), null);
		Octogone(renderer, cx, cy, rayon - _d.L(6f), new Color(255, 90, 94, 104), new Color(255, 46, 50, 60), new Color(255, 255, 160, 70));
		int num = rayon * 30 / 72;
		TechniqueArt.Image(renderer, "tech_ombre.png", new Rectangle(cx - num, cy - num / 5, num * 2, num * 2 / 5), A(TechniqueArt.Black, 0.45f));
	}

	private void Octogone(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int cx, int cy, int rayon, Color clair, Color sombre, Color? lisere)
	{
		(float, float)[] array = new(float, float)[8];
		for (int i = 0; i < 8; i++)
		{
			float x = (float)Math.PI / 8f + (float)i * (float)Math.PI / 4f;
			array[i] = ((float)cx + MathF.Cos(x) * (float)rayon, (float)cy + MathF.Sin(x) * (float)rayon * 0.3f);
		}
		int num = (int)MathF.Floor(array.Min(((float X, float Y) point) => point.Y));
		int num2 = (int)MathF.Ceiling(array.Max(((float X, float Y) point) => point.Y));
		object obj;
		if ((object)lisere != null)
		{
			obj = A(lisere, 0.5f);
		}
		else
		{
			obj = null;
		}
		Color color = (Color)obj;
		for (int num3 = num; num3 < num2; num3++)
		{
			if (PersonnageScreen.Traverse(array, (float)num3 + 0.5f, out var gauche, out var droite))
			{
				float num4 = ((float)num3 + 0.5f - (float)num) / (float)Math.Max(1, num2 - num);
				HudShapes.Span(renderer, gauche, droite, num3, 1, A(SaiyanHud.Lerp(clair, sombre, MathF.Abs(num4 - 0.45f) * 1.6f)));
				if (color != null)
				{
					HudShapes.Span(renderer, gauche, gauche + 2f, num3, 1, color);
					HudShapes.Span(renderer, droite - 2f, droite, num3, 1, color);
				}
			}
		}
		if (color != null)
		{
			HudShapes.Span(renderer, array[5].Item1, array[6].Item1, num, 2, color);
			HudShapes.Span(renderer, array[2].Item1, array[1].Item1, num2 - 2, 2, color);
		}
	}

	private void DessinerTourner(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle gauche, Rectangle droite)
	{
		BoutonTourner(renderer, gauche, "tourner:gauche", versLaGauche: true, delegate
		{
			_rangee = TourGauche[Math.Clamp(_rangee, 0, 3)];
		});
		BoutonTourner(renderer, droite, "tourner:droite", versLaGauche: false, delegate
		{
			_rangee = TourDroite[Math.Clamp(_rangee, 0, 3)];
		});
	}

	private void BoutonTourner(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, string cle, bool versLaGauche, Action tourner)
	{
		bool flag = IsHot(cle);
		TechniqueArt.Fill(renderer, rect, A(new Color(255, 10, 14, 24)));
		TechniqueArt.Outline(renderer, rect, A(flag ? TechniqueArt.Line2 : TechniqueArt.Line));
		int num = Math.Max(3, _d.L(6f));
		FlecheLaterale(renderer, rect.X + (rect.Width - num) / 2, rect.Y + rect.Height / 2, num, Math.Max(5, _d.L(10f)), versLaGauche, A(flag ? TechniqueArt.White : TechniqueArt.Soft));
		AddHit(rect, cle, tourner);
	}

	private static void FlecheLaterale(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int gauche, int centreY, int largeur, int hauteur, bool versLaGauche, Color couleur)
	{
		for (int i = 0; i < largeur; i++)
		{
			int num = (versLaGauche ? i : (largeur - 1 - i));
			int num2 = Math.Max(1, (int)MathF.Round((float)hauteur / 2f * (float)(num + 1) / (float)largeur));
			TechniqueArt.Fill(renderer, new Rectangle(gauche + i, centreY - num2, 1, num2 * 2), couleur);
		}
	}

	private void DessinerEmplacements(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, long now)
	{
		Point point = LocalMouse();
		bool survolee = base.Survolee;
		List<string> slots = Options.Instance.Equipment.Slots;
		Entree? entree = PiecePrise(me);
		int num = entree?.Objet.EquipmentSlot ?? (-1);
		bool flag = entree.HasValue && entree.GetValueOrDefault().NiveauRequis > me.Level;
		int num2;
		if (!entree.HasValue && !_glisse)
		{
			Entree? entree2 = PieceEssayee(me);
			if (entree2.HasValue)
			{
				num2 = entree2.GetValueOrDefault().Objet.EquipmentSlot;
				goto IL_00ac;
			}
		}
		num2 = -1;
		goto IL_00ac;
		IL_00ac:
		int num3 = num2;
		float num4 = 0.5f - 0.5f * MathF.Cos((float)(now % 1400) / 1400f * ((float)Math.PI * 2f));
		float num5 = 0.5f - 0.5f * MathF.Cos((float)(now % 1800) / 1800f * ((float)Math.PI * 2f));
		for (int i = 0; i < slots.Count; i++)
		{
			if (!PlacesEmplacements.TryGetValue(PersonnageScreen.Normaliser(slots[i]), out (float, float) value))
			{
				continue;
			}
			Rectangle rectangle = _d.R(16f + value.Item1, 98f + value.Item2, 52f, 58f);
			_hexagones.Add((rectangle, i));
			int caseSac;
			ItemDescriptor itemDescriptor = PersonnageScreen.ObjetPorte(me, i, out caseSac);
			bool flag2 = survolee && rectangle.Contains(point.X, point.Y);
			bool flag3 = i == num;
			bool flag4 = num >= 0 && !flag3;
			bool flag5 = flag3 && !flag && _glisse && rectangle.Contains(point.X, point.Y);
			bool flag6 = i == num3;
			bool flag7 = i == _filtreEmplacement && _onglet == Onglet.Sac;
			Rectangle hex = rectangle;
			if (i == _refusEmplacement && now - _refusA < 450)
			{
				hex.X -= (int)MathF.Round(MathF.Sin((float)(now - _refusA) / 450f * ((float)Math.PI * 2f) * 2f) * (float)_d.L(4f));
			}
			if (flag5)
			{
				int num6 = rectangle.Width * 6 / 100;
				int num7 = rectangle.Height * 6 / 100;
				hex = new Rectangle(rectangle.X - num6, rectangle.Y - num7, rectangle.Width + num6 * 2, rectangle.Height + num7 * 2);
			}
			float num8 = (float)hex.X + (float)hex.Width / 2f;
			float num9 = (float)hex.Y + (float)hex.Height / 2f;
			float num10 = (flag4 ? 0.3f : 1f);
			Color color;
			Color reflet;
			if (flag3)
			{
				color = (flag ? Rouge : (flag5 ? TechniqueArt.Mix(TechniqueArt.White, TechniqueArt.Gold, 0.45f) : TechniqueArt.Mix(new Color(255, 255, 241, 192), new Color(255, 255, 176, 46), 0.35f + 0.4f * num4)));
				reflet = (flag ? TechniqueArt.Mix(Rouge, new Color(255, 11, 15, 25), 0.3f) : TechniqueArt.Mix(TechniqueArt.Gold, new Color(255, 11, 15, 25), 0.32f));
				TechniqueArt.Glow(renderer, num8, num9, _d.L(46f), A(flag ? Rouge : TechniqueArt.Gold, flag ? 0.45f : (0.4f + 0.3f * num4)));
			}
			else if (itemDescriptor != null)
			{
				Color color2 = PersonnageScreen.CouleurRarete(JetDObjet.Rarete(itemDescriptor, PersonnageScreen.ProprietesDe(me, caseSac)));
				if (flag4)
				{
					color2 = TechniqueArt.Mix(color2, new Color(255, 120, 126, 140), 0.2f);
				}
				color = ((flag2 || flag6) ? TechniqueArt.Mix(color2, TechniqueArt.White, flag6 ? (0.75f - 0.35f * num5) : 0.7f) : TechniqueArt.Mix(color2, TechniqueArt.White, 0.95f));
				reflet = TechniqueArt.Mix(color2, new Color(255, 24, 32, 51), 0.24f);
				if (flag2)
				{
					TechniqueArt.Glow(renderer, num8, num9, _d.L(40f), A(color2, 0.28f));
				}
			}
			else
			{
				color = ((flag2 || flag6) ? TechniqueArt.Mix(BordVide, TechniqueArt.White, 0.7f) : BordVide);
				reflet = null;
			}
			if (flag7 && !flag3)
			{
				TechniqueArt.Glow(renderer, num8, num9, _d.L(44f), A(TechniqueArt.Gold, 0.35f * num10));
				color = TechniqueArt.Mix(TechniqueArt.White, TechniqueArt.Gold, 0.35f);
			}
			if (flag6 && !flag3)
			{
				TechniqueArt.Glow(renderer, num8, num9, _d.L(44f), A(TechniqueArt.Gold, 0.12f + 0.3f * num5));
			}
			float num11 = Eclat(now, i == _eclatEmplacement);
			if (num11 > 0f)
			{
				TechniqueArt.Glow(renderer, num8, num9, _d.L(50f), A(TechniqueArt.Gold, 0.8f * num11));
				color = TechniqueArt.Mix(TechniqueArt.White, color, num11);
			}
			Alveole(renderer, hex, color, reflet, num10);
			if (itemDescriptor != null)
			{
				bool flag8 = _glisse && _appuiEmplacement == i;
				IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Item, itemDescriptor.Icon);
				if (texture != null)
				{
					int num12 = TailleIcone(texture.Width);
					SaiyanHud.Draw(renderer, texture, (int)num8 - num12 / 2, (int)num9 - num12 / 2, num12, num12, A(itemDescriptor.Color, (flag8 ? 0.35f : 1f) * num10));
				}
			}
			else
			{
				Color color3 = ((!flag3) ? (flag7 ? TechniqueArt.Gold : ((flag2 || flag6) ? TechniqueArt.Mix(DessinVide, TechniqueArt.White, 0.7f) : DessinVide)) : (flag ? RougePale : TechniqueArt.Gold));
				int num13 = _d.L(24f);
				Lisser("inv_emplacement_" + PersonnageScreen.Normaliser(slots[i]) + ".png", new Rectangle((int)num8 - num13 / 2, (int)num9 - num13 / 2, num13, num13), A(color3, num10));
			}
			if (flag2 || flag3 || flag7)
			{
				string text;
				if (flag3 && flag && entree.HasValue)
				{
					Entree valueOrDefault = entree.GetValueOrDefault();
					text = Strings.Inventaire.LevelShort.ToString(valueOrDefault.NiveauRequis);
				}
				else
				{
					text = Traduction.Contenu(slots[i]);
				}
				string text2 = text;
				Color color4 = ((!flag3) ? (flag7 ? TechniqueArt.Gold : TechniqueArt.Soft) : (flag ? RougePale : TechniqueArt.Gold));
				TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(9.5f), text2.ToUpperInvariant(), rectangle.X + rectangle.Width / 2, rectangle.Bottom + _d.L(7f), _d.Em(0.16f, 9.5f), A(color4));
			}
		}
	}

	private void Alveole(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle hex, Color bord, Color? reflet, float opacite)
	{
		TechniqueArt.Image(renderer, "tech_hexa_bord.png", hex, A(bord, opacite));
		int num = Math.Max(1, _d.L(2f));
		Rectangle rect = new Rectangle(hex.X + num, hex.Y + num, hex.Width - num * 2, hex.Height - num * 2);
		TechniqueArt.Image(renderer, "tech_hexa.png", rect, A(new Color(255, 8, 12, 21)));
		if ((object)reflet != null)
		{
			TechniqueArt.Image(renderer, "tech_hexa_radial.png", rect, A(reflet, opacite));
		}
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

	private void DessinerPlaqueTete(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, int zoom, int sol)
	{
		NamePlate.Content content = PlaqueEssayee(me);
		float scale = 0.95f * _d.S;
		Point point = NamePlate.MeasureInterface(content, renderer, scale);
		Rectangle area = _d.R(16f, 98f, 256f, 290f);
		int val = sol - 2 - (me.NamePlateBodyHeight + 6) * zoom;
		val = Math.Max(val, area.Y + _d.L(24f) + point.Y);
		Rectangle previous = TechniqueArt.BeginClip(renderer, area);
		NamePlate.DrawInterface(content, renderer, _d.X(144f), val, scale);
		TechniqueArt.EndClip(renderer, previous);
		if (_essaiTitre.HasValue || _essaiOrnement.HasValue)
		{
			string text = Strings.Inventaire.Trying.ToString().ToUpperInvariant();
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(10f), text, _d.X(144f), val - point.Y - _d.L(10f), _d.Em(0.16f, 10f), A(TechniqueArt.Gold));
		}
	}

	private (Ecart? Puissance, Ecart? Vie, Ecart? Ki, string Sous) Ecarts(Player me)
	{
		string item = Strings.Inventaire.PowerScouter.ToString();
		Entree? entree = PieceEssayee(me);
		bool plusTard;
		if (entree.HasValue)
		{
			Entree valueOrDefault = entree.GetValueOrDefault();
			if (_onglet != Onglet.Apparence)
			{
				PersonnageScreen.Apports apres = PersonnageScreen.ApportsDe(PersonnageScreen.Portes(me, valueOrDefault.Objet, valueOrDefault.Case));
				int[] array = new int[apres.Stats.Length];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = apres.Stats[i] - _apports.Stats[i];
				}
				long num = VitalEcart(apres, 0);
				long num2 = VitalEcart(apres, 1);
				long valeur = Puissance(me, array, num + VitauxEnAttente(0), num2 + VitauxEnAttente(1)) - Puissance(me, null, VitauxEnAttente(0), VitauxEnAttente(1));
				plusTard = valueOrDefault.NiveauRequis > me.Level;
				return new ValueTuple<Ecart?, Ecart?, Ecart?, string>(item4: (!plusTard) ? Strings.Inventaire.PowerIfEquipped.ToString() : Strings.Inventaire.PowerAtLevel.ToString(valueOrDefault.NiveauRequis), item1: Teinter(valeur), item2: Teinter(num), item3: Teinter(num2));
			}
		}
		if (PointsEnAttente > 0)
		{
			int[] array2 = new int[me.Stat.Length];
			for (int j = 0; j < Math.Min(array2.Length, _attente.Length); j++)
			{
				array2[j] = _attente[j];
			}
			long num3 = VitauxEnAttente(0);
			long num4 = VitauxEnAttente(1);
			long valeur2 = Puissance(me, array2, num3, num4) - Puissance(me, null, 0L, 0L);
			return (Puissance: Dorer(valeur2), Vie: Dorer(num3), Ki: Dorer(num4), Sous: item);
		}
		return (Puissance: null, Vie: null, Ki: null, Sous: item);
		static Ecart? Dorer(long num5)
		{
			if (num5 != 0L)
			{
				return new Ecart(num5, TechniqueArt.Gold);
			}
			return null;
		}
		Ecart? Teinter(long num5)
		{
			if (num5 != 0L)
			{
				return new Ecart(num5, plusTard ? TechniqueArt.Dim : ((num5 > 0) ? TechniqueArt.Green : TechniqueArt.Red));
			}
			return null;
		}
	}

	private long VitalEcart(PersonnageScreen.Apports apres, int vital)
	{
		if (vital >= apres.Vitaux.Length || vital >= _apports.Vitaux.Length)
		{
			return 0L;
		}
		return apres.Vitaux[vital] - _apports.Vitaux[vital];
	}

	private static long Puissance(Player me, int[]? ecartsStats = null, long ecartVie = 0L, long ecartKi = 0L)
	{
		int[] array = new int[me.Stat.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = me.Stat[i] + ((ecartsStats != null && i < ecartsStats.Length) ? ecartsStats[i] : 0);
		}
		long maxHealth = ((me.MaxVital.Length != 0) ? me.MaxVital[0] : 0) + ecartVie;
		long maxKi = ((me.MaxVital.Length > 1) ? me.MaxVital[1] : 0) + ecartKi;
		return PowerLevel.Of(me.Level, array, maxHealth, maxKi);
	}

	private void DessinerReperes(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, long now)
	{
		(Ecart? Puissance, Ecart? Vie, Ecart? Ki, string Sous) tuple = Ecarts(me);
		Ecart? item = tuple.Puissance;
		Ecart? item2 = tuple.Vie;
		Ecart? item3 = tuple.Ki;
		string item4 = tuple.Sous;
		Rectangle rect = _d.R(16f, 390f, 256f, 46f);
		TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Cyan, 0.12f), A(new Color(102, 10, 14, 24)), A(new Color(102, 10, 14, 24)));
		TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, Math.Max(2, _d.L(2f)), rect.Height), A(TechniqueArt.Cyan));
		if (_valideA > 0 && now - _valideA < 450)
		{
			TechniqueArt.GlowRect(renderer, rect, A(TechniqueArt.Cyan, 0.5f * (1f - (float)(now - _valideA) / 450f)), additive: false);
		}
		int num = rect.X + _d.L(12f);
		string text = Strings.Personnage.PowerHeading.ToString().ToUpperInvariant();
		int val = TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10.5f), text, num, rect.Y + _d.L(16f), _d.Em(0.2f, 10.5f), A(TechniqueArt.Soft));
		int val2 = TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(10f), item4, num, rect.Y + _d.L(30f), A(TechniqueArt.Dim));
		int num2 = rect.Right - _d.L(12f);
		string text2 = SaiyanHud.Amount(Puissance(me, null, 0L, 0L));
		int num3 = num2 - num - Math.Max(val, val2) - _d.L(10f);
		string text3 = ((!item.HasValue) ? null : EcrireEcart(item.GetValueOrDefault().Valeur));
		int size = TailleQuiTient(renderer, TechniqueArt.Display, new float[4] { 32f, 28f, 24f, 20f }, text2, num3 - ((text3 != null) ? (TechniqueArt.Measure(renderer, TechniqueArt.Heavy, Pt(14f), text3).X + _d.L(6f)) : 0));
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, size, text2);
		int num4 = rect.Y + rect.Height / 2;
		TechniqueArt.Glow(renderer, (float)num2 - (float)point.X / 2f, num4, _d.L(28f), A(TechniqueArt.Cyan, 0.14f));
		TechniqueArt.Italic(renderer, TechniqueArt.Display, size, text2, num2 - point.X, num4 - point.Y / 2, A(TechniqueArt.Cyan));
		if (text3 != null && item.HasValue)
		{
			Ecart valueOrDefault = item.GetValueOrDefault();
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(14f), text3, num2 - point.X - _d.L(6f), num4 + _d.L(2f), A(valueOrDefault.Couleur));
		}
		BulleSur(rect, "repere:puissance", () => BullePuissance(me));
		float num5 = 125f;
		DessinerVital(renderer, _d.R(16f, 444f, num5, 34f), Strings.Inventaire.MaxHealth, VitalMax(me, 0), item2, new Color(255, 255, 210, 212), "repere:vie", () => BulleVital(me, 0));
		DessinerVital(renderer, _d.R(16f + num5 + 6f, 444f, num5, 34f), Strings.Inventaire.MaxKi, VitalMax(me, 1), item3, new Color(255, 207, 230, 255), "repere:ki", () => BulleVital(me, 1));
		DessinerNiveau(renderer, me, _d.R(16f, 488f, 256f, 30f));
	}

	private static long VitalMax(Player me, int vital)
	{
		if (vital >= me.MaxVital.Length)
		{
			return 0L;
		}
		return me.MaxVital[vital];
	}

	private static string EcrireEcart(long valeur)
	{
		return ((valeur > 0) ? "+" : "-") + SaiyanHud.Amount(Math.Abs(valeur));
	}

	private int TailleQuiTient(Intersect.Client.Framework.Gwen.Renderer.Base renderer, IFont? police, float[] tailles, string texte, int largeur)
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

	private void DessinerVital(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle cadre, string nom, long valeur, Ecart? ecart, Color encre, string cle, Func<Bulle> bulle)
	{
		TechniqueArt.Fill(renderer, cadre, A(new Color(179, 7, 10, 18)));
		TechniqueArt.Outline(renderer, cadre, A(TechniqueArt.Line));
		int num = cadre.Y + cadre.Height / 2;
		int num2 = cadre.X + _d.L(10f);
		int num3 = cadre.Right - _d.L(10f);
		int num4 = TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), nom.ToUpperInvariant(), num2, num, _d.Em(0.14f, 10f), A(TechniqueArt.Dim));
		string text = ((!ecart.HasValue) ? null : EcrireEcart(ecart.GetValueOrDefault().Valeur));
		int num5 = ((text != null) ? (TechniqueArt.Measure(renderer, TechniqueArt.Heavy, Pt(14f), text).X + _d.L(4f)) : 0);
		string text2 = SaiyanHud.Amount(valeur);
		int size = TailleQuiTient(renderer, TechniqueArt.Heavy, new float[3] { 20f, 17f, 15f }, text2, num3 - num2 - num4 - _d.L(6f) - num5);
		if (text != null && ecart.HasValue)
		{
			Ecart valueOrDefault = ecart.GetValueOrDefault();
			num3 -= TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(14f), text, num3, num + _d.L(1f), A(valueOrDefault.Couleur)) + _d.L(4f);
		}
		TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, size, text2, num3, num, A(encre));
		BulleSur(cadre, cle, bulle);
	}

	private void DessinerNiveau(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, Rectangle zone)
	{
		int centerY = zone.Y + _d.L(8f);
		int x = zone.X;
		x += TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10.5f), Strings.Inventaire.Level.ToString().ToUpperInvariant(), x, centerY, _d.Em(0.16f, 10.5f), A(TechniqueArt.Soft)) + _d.L(6f);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(15f), me.Level.ToString(CultureInfo.InvariantCulture), x, centerY, A(TechniqueArt.Text));
		long experienceToNextLevel = me.ExperienceToNextLevel;
		string text = ((experienceToNextLevel <= 0) ? Strings.Character.ExperienceMax.ToString() : (SaiyanHud.Amount(me.Experience) + " / " + SaiyanHud.Amount(experienceToNextLevel)));
		TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(11f), text, zone.Right, centerY, A(TechniqueArt.Dim));
		Rectangle rect = new Rectangle(zone.X, zone.Y + _d.L(19f), zone.Width, Math.Max(4, _d.L(6f)));
		TechniqueArt.Fill(renderer, rect, A(new Color(255, 7, 10, 18)));
		float num = ((experienceToNextLevel <= 0) ? 1f : Math.Clamp((float)me.Experience / (float)experienceToNextLevel, 0f, 1f));
		int num2 = (int)((float)(rect.Width - 2) * num);
		if (num2 > 0)
		{
			TechniqueArt.FillHorizontal(renderer, new Rectangle(rect.X + 1, rect.Y + 1, num2, rect.Height - 2), A(new Color(255, 184, 97, 15)), A(TechniqueArt.Ki), A(TechniqueArt.Gold));
		}
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
		BulleSur(zone, "repere:niveau", () => BulleNiveau(me));
	}

	private static string NomDe(Categorie categorie)
	{
		return categorie switch
		{
			Categorie.Equipement => Strings.Personnage.CategoryEquipment, 
			Categorie.Consommables => Strings.Personnage.CategoryConsumables, 
			Categorie.Ressources => Strings.Personnage.CategoryResources, 
			Categorie.Sacs => Strings.Personnage.CategoryBags, 
			Categorie.Quete => Strings.Personnage.CategoryQuest, 
			Categorie.Techniques => Strings.Personnage.CategoryTechniques, 
			_ => Strings.Personnage.CategoryAll, 
		};
	}

	private void ConstruireRecherche()
	{
		_recherche = new TextBox(this, "Recherche")
		{
			Font = TechniqueArt.Body,
			FontSize = 11,
			ShouldDrawBackground = false,
			MaximumLength = 32,
			IsHidden = true
		};
		_recherche.SetTextColor(TechniqueArt.Text, ComponentState.Normal);
		_recherche.TextChanged += delegate
		{
			_requete = PersonnageScreen.Normaliser(_recherche.Text);
			_vueSale = true;
		};
		Interface.FocusComponents.Add(_recherche);
	}

	private void PlacerRecherche()
	{
		if (_cherche)
		{
			Rectangle rectangle = _d.R(320f, 100f, 300f, 26f);
			if (_recherche.FontSize != Pt(14f))
			{
				_recherche.FontSize = Pt(14f);
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
			_vueSale = true;
		}
	}

	private void DessinerOutils(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		int num = _d.L(15f);
		if (_cherche)
		{
			DessinerChamp(renderer, _d.R(288f, 98f, 342f, 30f));
			Rectangle rect = _d.R(634f, 98f, 30f, 30f);
			Color couleur = FondOutil(renderer, rect, "outil:loupe", enfonce: false, Strings.Inventaire.CloseSearch);
			int num2 = _d.L(13f);
			Lisser("tech_croix.png", new Rectangle(rect.X + (rect.Width - num2) / 2, rect.Y + (rect.Height - num2) / 2, num2, num2), couleur);
			AddHit(rect, "outil:loupe", FermerRecherche);
			return;
		}
		Rectangle rect2 = _d.R(600f, 98f, 30f, 30f);
		Rectangle rect3 = _d.R(634f, 98f, 30f, 30f);
		Color couleur2 = FondOutil(renderer, rect2, "outil:loupe", enfonce: false, Strings.Inventaire.ToolSearch);
		Lisser("inv_loupe.png", new Rectangle(rect2.X + (rect2.Width - num) / 2, rect2.Y + (rect2.Height - num) / 2, num, num), couleur2);
		AddHit(rect2, "outil:loupe", BasculerRecherche);
		couleur2 = FondOutil(renderer, rect3, "outil:ranger", _range, Strings.Inventaire.ToolSort);
		int num3 = Math.Max(2, _d.L(2f));
		int x = rect3.X + (rect3.Width - num) / 2 + num / 8;
		int num4 = rect3.Y + (rect3.Height - num) / 2;
		for (int i = 0; i < 3; i++)
		{
			TechniqueArt.Fill(renderer, new Rectangle(x, num4 + num * (1 + i) / 4 - num3 / 2, num * (3 - i) / 4, num3), couleur2);
		}
		AddHit(rect3, "outil:ranger", delegate
		{
			_range = !_range;
			_vueSale = true;
		});
		DessinerPuces(renderer, _d.R(288f, 98f, 308f, 30f));
	}

	private Color FondOutil(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, string cle, bool enfonce, string nom)
	{
		bool flag = IsHot(cle);
		if (enfonce)
		{
			TechniqueArt.FillVertical(renderer, rect, A(new Color(255, 255, 194, 122)), A(new Color(255, 224, 117, 26)));
			TechniqueArt.Outline(renderer, rect, A(new Color(255, 255, 194, 122)));
		}
		else
		{
			TechniqueArt.Fill(renderer, rect, A(new Color(255, 10, 14, 24)));
			TechniqueArt.Outline(renderer, rect, A(flag ? TechniqueArt.Line2 : TechniqueArt.Line));
		}
		if (flag)
		{
			Etiqueter(cle, rect, nom);
		}
		return A(enfonce ? new Color(255, 23, 11, 2) : (flag ? TechniqueArt.White : TechniqueArt.Soft));
	}

	private void DessinerPuces(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle zone)
	{
		_puces.Clear();
		(Categorie, string)[] puces = Puces;
		for (int i = 0; i < puces.Length; i++)
		{
			(Categorie, string) tuple = puces[i];
			Categorie item = tuple.Item1;
			string item2 = tuple.Item2;
			if (item == Categorie.Tout || _comptes[(int)item] != 0)
			{
				Categorie choix = item;
				string glyphe = item2;
				_puces.Add(new Puce($"puce:{item}", NomDe(item), Compte(item), item == _categorie && _filtreEmplacement < 0, delegate
				{
					_categorie = choix;
					_filtreEmplacement = -1;
					_vueSale = true;
				}, delegate(Rectangle rect, Color encre)
				{
					Lisser("inv_glyphe_" + glyphe + ".png", rect, encre);
				}));
			}
		}
		DessinerPuces(renderer, zone, _puces);
	}

	private string Compte(Categorie categorie)
	{
		return _comptes[(int)categorie].ToString(CultureInfo.InvariantCulture);
	}

	private void DessinerChamp(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle zone)
	{
		TechniqueArt.Fill(renderer, zone, A(new Color(255, 7, 10, 18)));
		TechniqueArt.Outline(renderer, zone, A(TechniqueArt.Ki));
		int num = _d.L(14f);
		Lisser("inv_loupe.png", new Rectangle(zone.X + _d.L(10f), zone.Y + (zone.Height - num) / 2, num, num), A(TechniqueArt.Soft));
		if (string.IsNullOrEmpty(_recherche.Text))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), Strings.Inventaire.SearchPlaceholder, zone.X + _d.L(32f), zone.Y + zone.Height / 2, A(TechniqueArt.Dim));
		}
	}

	private void DessinerGrille(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, long now)
	{
		_zoneSac = _d.R(288f, 98f, 376f, 430f);
		Point point = LocalMouse();
		bool survolee = base.Survolee;
		Rectangle rect = _d.R(289f, 133f, 374f, 328f);
		if (_appuiEmplacement >= 0 && Globals.InputManager.IsMouseButtonDown(MouseButton.Left))
		{
			if (_glisse && _zoneSac.Contains(point.X, point.Y))
			{
				TechniqueArt.Fill(renderer, rect, A(TechniqueArt.Gold, 0.06f));
				TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Gold, 0.7f), Math.Max(1, _d.L(2f)));
			}
			else
			{
				Pointilles(renderer, rect, A(TechniqueArt.Gold, 0.7f));
			}
		}
		_rangeeHaut = Math.Clamp(_rangeeHaut, 0, RangeesDeTrop());
		int num = _rangeeHaut * 8;
		for (int i = 0; i < Math.Min(_montrees.Count - num, 56); i++)
		{
			Rectangle rectangle = _d.R(294f + (float)(i % 8) * 46f, 138f + (float)(i / 8) * 46f, 42f, 42f);
			int num2 = _montrees[num + i];
			_cases.Add((rectangle, num2));
			bool flag = rectangle.Contains(point.X, point.Y);
			if (!_parCase.TryGetValue(num2, out var value))
			{
				bool flag2 = _glisse && _appuiSac >= 0 && flag;
				TechniqueArt.Fill(renderer, rectangle, A(new Color(128, 7, 10, 18)));
				TechniqueArt.Outline(renderer, rectangle, A(flag2 ? TechniqueArt.Gold : TechniqueArt.Alpha(TechniqueArt.Line, 0.55f)));
			}
			else
			{
				bool chaud = survolee && flag;
				bool flag3 = _glisse && num2 == _appuiSac;
				bool cible = _glisse && _appuiSac >= 0 && !flag3 && flag;
				DessinerCase(renderer, me, rectangle, value, chaud, flag3, cible, Eclat(now, num2 == _eclatCase));
			}
		}
		if (_filtreEmplacement >= 0 && _montrees.Count == 0 && _filtreEmplacement < Options.Instance.Equipment.Slots.Count)
		{
			string text = Traduction.Contenu(Options.Instance.Equipment.Slots[_filtreEmplacement]);
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), Strings.Inventaire.SlotFilterEmpty.ToString(text), rect.X + _d.L(12f), rect.Y + _d.L(24f), A(TechniqueArt.Soft));
		}
		DessinerDefilement(renderer);
	}

	private int RangeesDeTrop()
	{
		return Math.Max(0, (_montrees.Count + 8 - 1) / 8 - 7);
	}

	private bool DefilerLeSac(int cran)
	{
		int num = RangeesDeTrop();
		if (num == 0)
		{
			return false;
		}
		_rangeeHaut = Math.Clamp(_rangeeHaut - Math.Sign(cran), 0, num);
		_bulle = null;
		return true;
	}

	private void DessinerDefilement(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		int num = RangeesDeTrop();
		if (num != 0)
		{
			int num2 = 7 + num;
			Rectangle rect = _d.R(660f, 138f, 3f, 318f);
			TechniqueArt.Fill(renderer, rect, A(TechniqueArt.Alpha(TechniqueArt.Line, 0.7f)));
			int num3 = Math.Max(_d.L(16f), rect.Height * 7 / num2);
			int y = rect.Y + (rect.Height - num3) * _rangeeHaut / Math.Max(1, num);
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, y, rect.Width, num3), A(TechniqueArt.Soft));
		}
	}

	private void Pointilles(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, Color couleur)
	{
		int num = Math.Max(1, _d.L(2f));
		int num2 = _d.L(6f);
		int num3 = num2 + _d.L(4f);
		for (int i = rect.X; i < rect.Right; i += num3)
		{
			int w = Math.Min(num2, rect.Right - i);
			TechniqueArt.Fill(renderer, new Rectangle(i, rect.Y, w, num), couleur);
			TechniqueArt.Fill(renderer, new Rectangle(i, rect.Bottom - num, w, num), couleur);
		}
		for (int j = rect.Y + num3; j < rect.Bottom - num; j += num3)
		{
			int h = Math.Min(num2, rect.Bottom - num - j);
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, j, num, h), couleur);
			TechniqueArt.Fill(renderer, new Rectangle(rect.Right - num, j, num, h), couleur);
		}
	}

	private void DessinerCase(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, Rectangle rect, Entree entree, bool chaud, bool tenue, bool cible, float eclat)
	{
		ItemDescriptor objet = entree.Objet;
		bool flag = entree.NiveauRequis > me.Level;
		if (chaud || cible)
		{
			FenetreCharte.Degrade(renderer, rect, A(new Color(255, 24, 35, 58)), A(new Color(255, 11, 16, 27)), 8);
		}
		else
		{
			FenetreCharte.Degrade(renderer, rect, A(new Color(255, 15, 22, 38)), A(new Color(255, 8, 12, 21)), 8);
		}
		TechniqueArt.Outline(renderer, rect, A(cible ? TechniqueArt.Gold : (chaud ? new Color(255, 90, 108, 148) : TechniqueArt.Line)));
		if (entree.Rarete >= 2)
		{
			int num = Math.Max(2, _d.L(2f));
			TechniqueArt.Fill(renderer, new Rectangle(rect.X + 1, rect.Bottom - 1 - num, rect.Width - 2, num), A(PersonnageScreen.CouleurRarete(entree.Rarete)));
		}
		bool flag2 = me.IsItemOnCooldown(entree.Case);
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Item, objet.Icon);
		if (texture != null)
		{
			int num2 = TailleIcone(texture.Width);
			Color color = (flag2 ? new Color(100, objet.Color.R, objet.Color.G, objet.Color.B) : objet.Color);
			if (flag)
			{
				int num3 = (color.R + color.G + color.B) / 3;
				color = new Color(color.A, (color.R + num3) * 7 / 20, (color.G + num3) * 7 / 20, (color.B + num3) * 7 / 20);
			}
			SaiyanHud.Draw(renderer, texture, rect.X + (rect.Width - num2) / 2, rect.Y + (rect.Height - num2) / 2, num2, num2, A(color, tenue ? 0.35f : 1f));
		}
		if (flag)
		{
			TechniqueArt.Fill(renderer, new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2), A(new Color(36, 255, 70, 90)));
			int num4 = _d.L(10f);
			int h = _d.L(12f);
			Lisser("inv_cadenas.png", new Rectangle(rect.Right - _d.L(2f) - num4, rect.Y + _d.L(2f), num4, h), A(new Color(255, 255, 138, 150)));
		}
		if (flag2)
		{
			string text = Math.Ceiling((double)me.GetItemRemainingCooldown(entree.Case) / 1000.0).ToString(CultureInfo.InvariantCulture);
			TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(16f), text, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, A(TechniqueArt.White));
		}
		if (objet.IsStackable && entree.Quantite > 1)
		{
			string text2 = Strings.FormatQuantityAbbreviated(entree.Quantite);
			int num5 = rect.Right - _d.L(3f);
			int num6 = rect.Bottom - _d.L(8f);
			Color color2 = A(TechniqueArt.Black, 0.85f);
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(13f), text2, num5 + 1, num6, color2);
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(13f), text2, num5 - 1, num6, color2);
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(13f), text2, num5, num6 + 1, color2);
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(13f), text2, num5, num6, A(TechniqueArt.White));
		}
		if (entree.Meilleur)
		{
			int num7 = _d.L(11f);
			for (int i = 0; i < num7; i++)
			{
				TechniqueArt.Fill(renderer, new Rectangle(rect.X + 1, rect.Y + 1 + i, num7 - i, 1), A(TechniqueArt.Green));
			}
		}
		if (eclat > 0f)
		{
			TechniqueArt.GlowRect(renderer, new Rectangle(rect.X - _d.L(9f), rect.Y - _d.L(9f), rect.Width + _d.L(18f), rect.Height + _d.L(18f)), A(TechniqueArt.Gold, 0.7f * eclat), additive: false);
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Gold, eclat), Math.Max(1, _d.L(2f)));
		}
	}

	private void DessinerBasDuSac(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me)
	{
		Player.Charge charge = me.LireLaCharge();
		DessinerCharge(renderer, me, charge);
		if (charge.Surcharge)
		{
			DessinerSurcharge(renderer, charge);
		}
		else
		{
			DessinerPanoplie(renderer);
		}
	}

	private void DessinerCharge(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, Player.Charge charge)
	{
		Rectangle rect = _d.R(294f, 464f, 364f, 18f);
		int num = rect.Y + rect.Height / 2;
		bool surcharge = charge.Surcharge;
		bool presquePleine = charge.PresquePleine;
		Color color = (surcharge ? new Color(255, 255, 138, 150) : (presquePleine ? TechniqueArt.Gold : TechniqueArt.Soft));
		string text = (surcharge ? Strings.Inventaire.ChargeOverloaded : (presquePleine ? Strings.Inventaire.ChargeAlmostFull : Strings.Inventaire.ChargeLabel)).ToString().ToUpperInvariant();
		float spacing = _d.Em(0.16f, 11f);
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(11f), text, rect.X, num, spacing, A(color));
		int num2 = rect.X + TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(11f), text, spacing) + _d.L(10f);
		int right = rect.Right;
		string text2 = Strings.Inventaire.ChargeAmount.ToString(SaiyanHud.Amount(charge.Porte), SaiyanHud.Amount(charge.Capacite));
		string text3 = SaiyanHud.Amount(charge.Porte);
		object obj;
		if (!text2.StartsWith(text3, StringComparison.Ordinal))
		{
			obj = null;
		}
		else
		{
			string text4 = text2;
			int length = text3.Length;
			obj = text4.Substring(length, text4.Length - length);
		}
		string text5 = (string)obj;
		if (text5 != null)
		{
			right -= TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(15f), text5, right, num, A(TechniqueArt.Dim));
			right -= TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(15f), text3, right, num, A(surcharge ? color : (presquePleine ? TechniqueArt.Gold : TechniqueArt.Text)));
		}
		else
		{
			right -= TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(15f), text2, right, num, A(surcharge ? color : TechniqueArt.Text));
		}
		Rectangle rect2 = new Rectangle(num2, num - _d.L(5f), Math.Max(_d.L(20f), right - _d.L(10f) - num2), _d.L(10f));
		TechniqueArt.Fill(renderer, rect2, A(new Color(255, 7, 10, 18)));
		Rectangle rectangle = new Rectangle(rect2.X + 1, rect2.Y + 1, rect2.Width - 2, rect2.Height - 2);
		int num3 = ((charge.Capacite <= 0) ? rectangle.Width : ((int)((double)rectangle.Width * Math.Min(1.0, (double)charge.Porte / (double)charge.Capacite))));
		if (num3 > 0)
		{
			Rectangle rect3 = new Rectangle(rectangle.X, rectangle.Y, num3, rectangle.Height);
			if (surcharge)
			{
				TechniqueArt.GlowRect(renderer, new Rectangle(rect2.X - _d.L(4f), rect2.Y - _d.L(4f), rect2.Width + _d.L(8f), rect2.Height + _d.L(8f)), A(new Color(255, 255, 100, 116), 0.45f), additive: false);
				TechniqueArt.Fill(renderer, rect3, A(new Color(255, 194, 50, 70)));
				int num4 = Math.Max(2, _d.L(6f));
				for (int i = rect3.X; i < rect3.Right; i += num4 * 2)
				{
					TechniqueArt.Fill(renderer, new Rectangle(i, rect3.Y, Math.Min(num4, rect3.Right - i), rect3.Height), A(new Color(255, 255, 100, 116)));
				}
			}
			else if (presquePleine)
			{
				TechniqueArt.FillHorizontal(renderer, rect3, A(new Color(255, 184, 97, 15)), A(TechniqueArt.Ki), A(TechniqueArt.Gold));
			}
			else
			{
				TechniqueArt.FillHorizontal(renderer, rect3, A(new Color(255, 31, 95, 158)), A(new Color(255, 90, 168, 255)), A(new Color(255, 159, 216, 255)));
			}
		}
		TechniqueArt.Outline(renderer, rect2, A(surcharge ? new Color(179, 255, 100, 116) : TechniqueArt.Line));
		int x = rectangle.X + rectangle.Width * 9 / 10;
		TechniqueArt.Fill(renderer, new Rectangle(x, rect2.Y - _d.L(3f), 1, rect2.Height + _d.L(6f)), A(TechniqueArt.Gold, 0.55f));
		BulleSur(rect, "charge", () => BulleCharge(me));
	}

	private void DessinerSurcharge(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player.Charge charge)
	{
		Rectangle rect = _d.R(294f, 488f, 364f, 28f);
		TechniqueArt.Fill(renderer, rect, A(new Color(31, 255, 70, 90)));
		TechniqueArt.Outline(renderer, rect, A(new Color(115, 255, 100, 116)));
		int num = rect.X + _d.L(10f);
		string text = Strings.Inventaire.ChargeOverloadedAlert.ToString(SaiyanHud.Amount(Math.Max(1, charge.Porte - charge.Capacite)));
		List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), text, rect.Right - _d.L(10f) - num, 1);
		if (list.Count > 0)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), list[0], num, rect.Y + rect.Height / 2, A(new Color(255, 255, 179, 187)));
		}
	}

	private void DessinerPanoplie(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		if (_panopliesPortees.Count != 0)
		{
			(PanoplieDescriptor Panoplie, int Pieces, int Total) tuple = _panopliesPortees[0];
			PanoplieDescriptor item = tuple.Panoplie;
			int item2 = tuple.Pieces;
			int item3 = tuple.Total;
			Rectangle rectangle = _d.R(294f, 488f, 364f, 28f);
			_lignesPanoplies.Add((rectangle, item));
			Point point = LocalMouse();
			bool flag = base.Survolee && rectangle.Contains(point.X, point.Y);
			bool flag2 = item2 >= item3;
			TechniqueArt.Fill(renderer, rectangle, A(new Color(179, 7, 10, 18)));
			TechniqueArt.Outline(renderer, rectangle, A(flag ? TechniqueArt.Line2 : TechniqueArt.Line));
			int num = rectangle.Y + rectangle.Height / 2;
			int num2 = rectangle.Right - _d.L(10f);
			num2 -= TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(14f), $"{item2} / {item3}", num2, num, A(flag2 ? TechniqueArt.Gold : TechniqueArt.Soft));
			num2 -= _d.L(8f);
			int num3 = _d.L(11f);
			int num4 = num3 + _d.L(2f);
			int num5 = Math.Min(item3, 8);
			num2 -= num5 * num4 - _d.L(2f);
			for (int i = 0; i < num5; i++)
			{
				Losange(renderer, num2 + i * num4 + num3 / 2, num, num3, (i < item2) ? A(TechniqueArt.Gold) : A(TechniqueArt.Line2), i < item2);
			}
			num2 -= _d.L(8f);
			int num6 = rectangle.X + _d.L(10f);
			List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(15f), NomCourt(item.Name), num2 - num6, 1);
			if (list.Count > 0)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(15f), list[0], num6, num, A(TechniqueArt.Text));
			}
		}
	}

	private static string NomCourt(string nom)
	{
		string[] array = new string[8] { "Panoplie du ", "Panoplie de la ", "Panoplie de l'", "Panoplie de l’", "Panoplie des ", "Panoplie d'", "Panoplie d’", "Panoplie " };
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (nom.StartsWith(text, StringComparison.OrdinalIgnoreCase) && nom.Length > text.Length)
			{
				string text2 = nom;
				int length = text.Length;
				string text3 = text2.Substring(length, text2.Length - length);
				ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(char.ToUpperInvariant(text3[0]));
				text2 = text3;
				return string.Concat(readOnlySpan, text2.Substring(1, text2.Length - 1));
			}
		}
		return nom;
	}

	private static void Losange(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int cx, int cy, int taille, Color couleur, bool plein)
	{
		int num = taille / 2;
		for (int i = -num; i <= num; i++)
		{
			int num2 = num - Math.Abs(i);
			if (plein || num2 == 0)
			{
				TechniqueArt.Fill(renderer, new Rectangle(cx - num2, cy + i, num2 * 2 + 1, 1), couleur);
				continue;
			}
			TechniqueArt.Fill(renderer, new Rectangle(cx - num2, cy + i, 1, 1), couleur);
			TechniqueArt.Fill(renderer, new Rectangle(cx + num2, cy + i, 1, 1), couleur);
		}
	}

	private void DessinerObjetTenu(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me)
	{
		ItemDescriptor itemDescriptor = ObjetTenu(me);
		if (itemDescriptor != null)
		{
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Item, itemDescriptor.Icon);
			if (texture != null)
			{
				Point point = LocalMouse();
				Rectangle rect = new Rectangle(point.X - _d.L(23f), point.Y - _d.L(23f), _d.L(46f), _d.L(46f));
				TechniqueArt.GlowRect(renderer, new Rectangle(rect.X - _d.L(10f), rect.Y - _d.L(10f), rect.Width + _d.L(20f), rect.Height + _d.L(20f)), A(TechniqueArt.Gold, 0.35f), additive: false);
				TechniqueArt.Fill(renderer, rect, A(new Color(224, 8, 11, 19)));
				TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Gold));
				int num = TailleIcone(texture.Width);
				SaiyanHud.Draw(renderer, texture, point.X - num / 2, point.Y - num / 2, num, num, TechniqueArt.Alpha(itemDescriptor.Color, 0.95f));
			}
		}
	}

	private static string FichierSon(string geste)
	{
		return "ui_" + geste + ".wav";
	}

	private static void Son(string geste, float hauteur = 0f)
	{
		Sound sound = Audio.AddGameSound(FichierSon(geste), loop: false);
		if (sound != null && hauteur != 0f)
		{
			sound.Pitch = Math.Clamp(hauteur, -1f, 1f);
		}
	}

	private void SonDuPoint()
	{
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		_montee = ((millisecondsUtc - _monteeA < 450) ? Math.Min(_montee + 1, 18) : 0);
		_monteeA = millisecondsUtc;
		Son("point", MathF.Log2(1f + (float)_montee * 0.025f));
	}

	private void SonnerLeSurvol()
	{
		string text = null;
		if (base.Survolee)
		{
			Point point = LocalMouse();
			foreach (var (rectangle, num) in _cases)
			{
				if (rectangle.Contains(point.X, point.Y) && _parCase.ContainsKey(num))
				{
					text = $"c{num}";
					break;
				}
			}
			if (text == null)
			{
				foreach (var (rectangle2, value) in _hexagones)
				{
					if (rectangle2.Contains(point.X, point.Y))
					{
						text = $"e{value}";
						break;
					}
				}
			}
		}
		if (!(text == _survolSon))
		{
			_survolSon = text;
			long millisecondsUtc = Timing.Global.MillisecondsUtc;
			if (text != null && millisecondsUtc - _survolSonA > 45)
			{
				_survolSonA = millisecondsUtc;
				Son("survol");
			}
		}
	}
}
