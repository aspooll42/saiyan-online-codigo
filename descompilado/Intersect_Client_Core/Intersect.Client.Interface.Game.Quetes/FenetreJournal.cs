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
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Chat;
using Intersect.Client.Interface.Game.Dungeons;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Housing;
using Intersect.Client.Interface.Game.Inventaire;
using Intersect.Client.Interface.Game.Personnage;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Config;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Framework.Core.GameObjects.Quests;
using Intersect.GameObjects;
using Intersect.Localization;
using Intersect.Models;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game.Quetes;

internal sealed class FenetreJournal : FenetreCharte
{
	private enum Categorie
	{
		EnCours,
		Histoire,
		Enseignements,
		Annexes,
		Quotidiennes,
		Contrats,
		AFaire,
		Terminees
	}

	private enum Etat
	{
		EnCours,
		AFaire,
		Terminee
	}

	private enum Genre
	{
		Histoire,
		Enseignement,
		Annexe,
		Quotidienne
	}

	private enum Sorte
	{
		Quete,
		Prime,
		Criminel,
		Ruban
	}

	private sealed record Entree(string Cle, Sorte Sorte, string Cherche)
	{
		public QuestDescriptor? Quete { get; init; }

		public QuestProgress? Progres { get; init; }

		public Etat Etat { get; init; }

		public Genre Genre { get; init; }

		public WantedContractEntryPacket? Contrat { get; init; }

		public RibbonContractEntryPacket? Ruban { get; init; }

		public bool EstUnContrat => Sorte != Sorte.Quete;
	}

	private readonly record struct Element(Entree? Entree, string Titre, string Detail, Color Couleur)
	{
		public bool EstUnGroupe => Entree == null;
	}

	private sealed record Pastille(IGameTexture? Icone, string Valeur, string Unite, Color Couleur, ItemDescriptor? Objet, bool Piece = false);

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

	private const float FiltresLargeur = 200f;

	private const float ChampLargeur = 420f;

	private const float TitreTop = 112f;

	private const float TitreHeight = 30f;

	private const float EnteteTop = 144f;

	private const float EnteteHeight = 24f;

	private const float ListeTop = 170f;

	private const float LigneWidth = 614f;

	private const float LigneHeight = 54f;

	private const float Pas = 4f;

	private const float GroupeHauteur = 26f;

	private const float PiedHeight = 30f;

	private const float PiedTop = 704f;

	private const float ListeBas = 694f;

	private const float ColonneNom = 64f;

	private const float ColonneOu = 280f;

	private const float ColonneOuLargeur = 170f;

	private const float RecompenseDroite = 574f;

	private const float ColonnePunaise = 581f;

	private const float HistoireHauteur = 156f;

	private const float VitrineTop = 72f;

	private const float VitrineHauteur = 118f;

	private const float BoutonHeight = 40f;

	private const float BoutonTop = 680f;

	private static readonly Color Muted = new Color(255, 125, 136, 159);

	private static readonly Color Eteint = new Color(255, 74, 85, 108);

	private static readonly Color Rouge = new Color(255, 255, 138, 150);

	private static readonly Color KiClair = new Color(255, 255, 172, 82);

	private static readonly Color Case = new Color(255, 10, 14, 24);

	private static readonly Color Violet = new Color(255, 199, 155, 255);

	private static readonly Color BleuPolice = new Color(255, 111, 182, 255);

	private static readonly Color Brun = new Color(255, 214, 150, 96);

	private Categorie _categorie;

	private bool _suiviesSeulement;

	private string _requete = string.Empty;

	private string _choisie = string.Empty;

	private int _premier;

	private int _premierMax;

	private Rectangle _liste;

	private string _vueAffichee = "-";

	private int _signatureEntrees = int.MinValue;

	private List<Entree> _entrees = new List<Entree>();

	private readonly TextBox _recherche;

	private readonly List<(Rectangle Rect, string Cle, Func<Bulle> Lire)> _zonesBulles = new List<(Rectangle, string, Func<Bulle>)>();

	private string? _bulleCle;

	private Bulle? _bulle;

	protected override Rectangle Place => new Rectangle(180, 76, 1240, 748);

	public FenetreJournal(Canvas gameCanvas)
		: base(gameCanvas, "FenetreJournal")
	{
		_recherche = new TextBox(this, "JournalRecherche")
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
	}

	public void Update(bool quetesChangees)
	{
		if (quetesChangees)
		{
			_signatureEntrees = int.MinValue;
		}
	}

	public override void Show()
	{
		bool isHidden = base.IsHidden;
		base.Show();
		if (isHidden)
		{
			PlacerRecherche();
		}
	}

	public override void Hide()
	{
		_recherche.Blur();
		base.Hide();
	}

	public void ShowQuest(Guid questId)
	{
		Ouvrir();
		if (DatabaseObject<QuestDescriptor>.TryGet(questId, out var databaseObject))
		{
			Player me = Globals.Me;
			if (me != null)
			{
				_categorie = EtatDe(me, databaseObject, out QuestProgress _) switch
				{
					Etat.Terminee => Categorie.Terminees, 
					Etat.AFaire => Categorie.AFaire, 
					_ => Categorie.EnCours, 
				};
			}
		}
		_choisie = $"q:{questId}";
		_vueAffichee = "-";
	}

	public void ShowContract(string cle)
	{
		Ouvrir();
		_categorie = Categorie.Contrats;
		_choisie = cle;
		_vueAffichee = "-";
	}

	private void Ouvrir()
	{
		if (_requete.Length > 0)
		{
			_recherche.Text = string.Empty;
			_requete = string.Empty;
		}
		_suiviesSeulement = false;
		if (base.IsHidden)
		{
			Show();
		}
		BringToFront();
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
		Rectangle rectangle = _d.R(296f, 63f, 348f, 30f);
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

	private static Etat? EtatDe(Player me, QuestDescriptor quete, out QuestProgress? progres)
	{
		progres = null;
		if (me.QuestProgress.TryGetValue(quete.Id, out QuestProgress value))
		{
			progres = value;
			if (value.TaskId != Guid.Empty)
			{
				return Etat.EnCours;
			}
			if (value.Completed)
			{
				if (!quete.LogAfterComplete)
				{
					return null;
				}
				return Etat.Terminee;
			}
		}
		if (!quete.LogBeforeOffer || me.HiddenQuests.Contains(quete.Id))
		{
			return null;
		}
		return Etat.AFaire;
	}

	private static Genre GenreDe(QuestDescriptor quete)
	{
		if (StoryThread.IsStoryQuest(quete))
		{
			return Genre.Histoire;
		}
		if (quete.Repeatable)
		{
			return Genre.Quotidienne;
		}
		if (!string.Equals(quete.Folder?.Trim(), "Enseignements", StringComparison.OrdinalIgnoreCase))
		{
			return Genre.Annexe;
		}
		return Genre.Enseignement;
	}

	private static int NumeroDeChapitre(QuestDescriptor quete)
	{
		string s = new string((quete.Folder ?? string.Empty).Where(char.IsDigit).ToArray());
		if (!int.TryParse(s, out var result))
		{
			return 0;
		}
		return result;
	}

	private static int Signature(Player me)
	{
		HashCode hashCode = default(HashCode);
		foreach (var (value, questProgress2) in me.QuestProgress)
		{
			hashCode.Add(value);
			hashCode.Add(questProgress2.TaskId);
			hashCode.Add(questProgress2.TaskProgress);
			hashCode.Add(questProgress2.Completed);
		}
		hashCode.Add(me.HiddenQuests.Length);
		hashCode.Add(DatabaseObject<QuestDescriptor>.Lookup.Count);
		hashCode.Add(Globals.WantedContracts);
		hashCode.Add(Globals.WantedContractsAt);
		hashCode.Add(Globals.RibbonContract);
		hashCode.Add(Globals.RibbonContractAt);
		return hashCode.ToHashCode();
	}

	private void Actualiser(Player me)
	{
		int num = Signature(me);
		if (num == _signatureEntrees)
		{
			return;
		}
		_signatureEntrees = num;
		List<Entree> list = new List<Entree>();
		foreach (QuestDescriptor item in DatabaseObject<QuestDescriptor>.Lookup.Values.OfType<QuestDescriptor>())
		{
			QuestProgress progres;
			Etat? etat = EtatDe(me, item, out progres);
			if (etat.HasValue)
			{
				Etat valueOrDefault = etat.GetValueOrDefault();
				string value = string.Join(" ", item.Tasks.Select((QuestTaskDescriptor tache) => QuestTrackerPanel.ObjectiveOf(tache)));
				string text = item.Folder?.Trim() ?? string.Empty;
				string cherche = PersonnageScreen.Normaliser($"{item.Name} {Traduction.Contenu(text)} {StoryThread.ChapterTitle(text)} {value}");
				list.Add(new Entree($"q:{item.Id}", Sorte.Quete, cherche)
				{
					Quete = item,
					Progres = progres,
					Etat = valueOrDefault,
					Genre = GenreDe(item)
				});
			}
		}
		HashSet<(WantedContractKind, Guid)> hashSet = new HashSet<(WantedContractKind, Guid)>();
		WantedContractEntryPacket[] array = Globals.WantedContracts ?? Array.Empty<WantedContractEntryPacket>();
		foreach (WantedContractEntryPacket wantedContractEntryPacket in array)
		{
			if (wantedContractEntryPacket != null && hashSet.Add((wantedContractEntryPacket.Kind, wantedContractEntryPacket.TargetId)))
			{
				Sorte sorte = ((wantedContractEntryPacket.Kind != WantedContractKind.Criminal) ? Sorte.Prime : Sorte.Criminel);
				string cherche2 = PersonnageScreen.Normaliser($"{wantedContractEntryPacket.Name} {wantedContractEntryPacket.Place} {WantedPlaces.ZoneOf(wantedContractEntryPacket.MapId)} {Strings.Journal.CatContrats}");
				list.Add(new Entree($"c:{wantedContractEntryPacket.Kind}:{wantedContractEntryPacket.TargetId}", sorte, cherche2)
				{
					Contrat = wantedContractEntryPacket,
					Etat = Etat.EnCours
				});
			}
		}
		RibbonContractEntryPacket ribbonContract = Globals.RibbonContract;
		if (ribbonContract != null)
		{
			string cherche3 = PersonnageScreen.Normaliser($"{ribbonContract.Name} {ribbonContract.Place} {WantedPlaces.ZoneOf(ribbonContract.MapId)} {Strings.Journal.GroupeRuban} {Strings.Journal.CatContrats}");
			list.Add(new Entree("r:" + ribbonContract.Key, Sorte.Ruban, cherche3)
			{
				Ruban = ribbonContract,
				Etat = Etat.EnCours
			});
		}
		_entrees = list;
	}

	private IEnumerable<Entree> EnCours(Genre? genre)
	{
		return (from entree in _entrees
			where entree.Sorte == Sorte.Quete && entree.Etat == Etat.EnCours && (!genre.HasValue || entree.Genre == genre)
			orderby entree.Genre, entree.Quete.OrderValue
			select entree).ThenBy<Entree, string>((Entree entree) => entree.Quete.Name, StringComparer.CurrentCultureIgnoreCase);
	}

	private IEnumerable<Entree> Contrats()
	{
		return from entree in _entrees
			where entree.EstUnContrat
			orderby entree.Sorte, entree.Contrat?.Completed ?? false
			select entree;
	}

	private int Nombre(Categorie categorie)
	{
		return categorie switch
		{
			Categorie.EnCours => EnCours(null).Count() + Contrats().Count(), 
			Categorie.Histoire => EnCours(Genre.Histoire).Count(), 
			Categorie.Enseignements => EnCours(Genre.Enseignement).Count(), 
			Categorie.Annexes => EnCours(Genre.Annexe).Count(), 
			Categorie.Quotidiennes => EnCours(Genre.Quotidienne).Count(), 
			Categorie.Contrats => Contrats().Count(), 
			Categorie.AFaire => _entrees.Count((Entree entree) => entree.Etat == Etat.AFaire), 
			_ => _entrees.Count((Entree entree) => entree.Etat == Etat.Terminee), 
		};
	}

	private bool Garder(Entree entree)
	{
		if (_suiviesSeulement && !entree.EstUnContrat)
		{
			if (entree.Quete != null && entree.Etat == Etat.EnCours)
			{
				return TrackedQuests.IsPinned(entree.Quete.Id);
			}
			return false;
		}
		return true;
	}

	private List<Element> Vue()
	{
		List<Element> list = new List<Element>();
		if (_requete.Length > 0)
		{
			List<Entree> list2 = _entrees.Where((Entree entree) => entree.Cherche.Contains(_requete, StringComparison.Ordinal) && Garder(entree)).ToList();
			Grouper(list, Strings.Journal.CatEnCours, string.Empty, TechniqueArt.Ki, from entree in list2
				where !entree.EstUnContrat && entree.Etat == Etat.EnCours
				orderby entree.Genre, entree.Quete.OrderValue
				select entree);
			Grouper(list, Strings.Journal.CatContrats, string.Empty, KiClair, list2.Where((Entree entree) => entree.EstUnContrat));
			Grouper(list, Strings.Journal.CatAFaire, string.Empty, TechniqueArt.Dim, from entree in list2
				where entree.Etat == Etat.AFaire
				orderby entree.Quete.OrderValue
				select entree);
			Grouper(list, Strings.Journal.CatTerminees, string.Empty, TechniqueArt.Green, Terminees(list2));
			return list;
		}
		switch (_categorie)
		{
		case Categorie.EnCours:
		{
			GrouperEnCours(list, Genre.Histoire);
			GrouperEnCours(list, Genre.Enseignement);
			GrouperEnCours(list, Genre.Annexe);
			GrouperEnCours(list, Genre.Quotidienne);
			List<Entree> list3 = Contrats().Where(Garder).ToList();
			int num2 = Math.Max(1, Options.Instance.Karma.WantedMaxContracts);
			int num3 = list3.Count((Entree entree) => entree.Sorte != Sorte.Ruban);
			Grouper(list, Strings.Journal.CatContrats, Strings.Journal.DetailContrats.ToString(num3, num2), KiClair, list3);
			break;
		}
		case Categorie.Histoire:
			GrouperEnCours(list, Genre.Histoire);
			break;
		case Categorie.Enseignements:
			GrouperEnCours(list, Genre.Enseignement);
			break;
		case Categorie.Annexes:
			GrouperEnCours(list, Genre.Annexe);
			break;
		case Categorie.Quotidiennes:
			GrouperEnCours(list, Genre.Quotidienne);
			break;
		case Categorie.Contrats:
		{
			List<Entree> source2 = Contrats().Where(Garder).ToList();
			Grouper(list, Strings.Journal.GroupePrimes, Strings.Journal.DetailPrimes, BleuPolice, source2.Where((Entree entree) => entree.Sorte == Sorte.Prime));
			Grouper(list, Strings.Journal.GroupeCriminels, Strings.Journal.DetailCriminels, TechniqueArt.Red, source2.Where((Entree entree) => entree.Sorte == Sorte.Criminel));
			Grouper(list, Strings.Journal.GroupeRuban, Strings.Journal.DetailRuban, Brun, source2.Where((Entree entree) => entree.Sorte == Sorte.Ruban));
			break;
		}
		case Categorie.AFaire:
			Grouper(list, Strings.Journal.CatAFaire, string.Empty, TechniqueArt.Dim, from entree in _entrees
				where entree.Etat == Etat.AFaire && Garder(entree)
				orderby entree.Quete.OrderValue
				select entree);
			break;
		default:
		{
			List<Entree> source = Terminees(_entrees.Where((Entree entree) => entree.Etat == Etat.Terminee && Garder(entree))).ToList();
			foreach (IGrouping<string, Entree> item in from entree in source
				where entree.Genre == Genre.Histoire
				group entree by entree.Quete.Folder?.Trim() ?? string.Empty)
			{
				string dossier = item.Key;
				int num = DatabaseObject<QuestDescriptor>.Lookup.Values.OfType<QuestDescriptor>().Count((QuestDescriptor quete) => string.Equals(quete.Folder?.Trim(), dossier, StringComparison.OrdinalIgnoreCase));
				string text = StoryThread.ChapterTitle(dossier);
				string titre = ((text != null && text.Length > 0) ? (Traduction.Contenu(dossier) + " · " + text) : Traduction.Contenu(dossier));
				Grouper(list, titre, Strings.QuestLog.Progress.ToString(item.Count(), num), TechniqueArt.Gold, item);
			}
			Grouper(list, Strings.Journal.CatEnseignements, string.Empty, Violet, source.Where((Entree entree) => entree.Genre == Genre.Enseignement));
			Grouper(list, Strings.Journal.CatAnnexes, string.Empty, TechniqueArt.Green, source.Where(delegate(Entree entree)
			{
				Genre genre = entree.Genre;
				return (uint)(genre - 2) <= 1u;
			}));
			break;
		}
		}
		return list;
	}

	private static IEnumerable<Entree> Terminees(IEnumerable<Entree> entrees)
	{
		return from entree in entrees
			where entree.Etat == Etat.Terminee
			orderby (entree.Genre != Genre.Histoire) ? 1 : 0, NumeroDeChapitre(entree.Quete) descending, entree.Quete.OrderValue descending
			select entree;
	}

	private void GrouperEnCours(List<Element> elements, Genre genre)
	{
		List<Entree> list = EnCours(genre).Where(Garder).ToList();
		if (list.Count != 0)
		{
			Color couleur;
			string titre;
			string detail;
			switch (genre)
			{
			case Genre.Histoire:
			{
				string text6 = list[0].Quete.Folder?.Trim() ?? string.Empty;
				string text7 = Strings.Journal.CatHistoire;
				couleur = TechniqueArt.Gold;
				titre = text7;
				string text8 = StoryThread.ChapterTitle(text6);
				detail = ((text8 != null && text8.Length > 0) ? (Traduction.Contenu(text6) + " · " + text8) : Traduction.Contenu(text6));
				break;
			}
			case Genre.Enseignement:
			{
				string text = Strings.Journal.CatEnseignements;
				string text5 = Strings.Journal.DetailNEnCours.ToString(list.Count);
				couleur = Violet;
				detail = text5;
				titre = text;
				break;
			}
			case Genre.Quotidienne:
			{
				string text3 = Strings.Journal.CatQuotidiennes;
				string text4 = Strings.Journal.DetailQuotidienne;
				couleur = TechniqueArt.Cyan;
				detail = text4;
				titre = text3;
				break;
			}
			default:
			{
				string text = Strings.Journal.CatAnnexes;
				string text2 = Strings.Journal.DetailNEnCours.ToString(list.Count);
				couleur = TechniqueArt.Green;
				detail = text2;
				titre = text;
				break;
			}
			}
			Grouper(elements, titre, detail, couleur, list);
		}
	}

	private static void Grouper(List<Element> elements, string titre, string detail, Color couleur, IEnumerable<Entree> entrees)
	{
		List<Entree> list = entrees.ToList();
		if (list.Count != 0)
		{
			elements.Add(new Element(null, titre, detail, couleur));
			elements.AddRange(list.Select((Entree entree) => new Element(entree, string.Empty, string.Empty, couleur)));
		}
	}

	private void SuivreLaVue(List<Element> elements)
	{
		string text = $"{((_requete.Length > 0) ? ("?" + _requete) : _categorie.ToString())}|{_suiviesSeulement}";
		int num = elements.FindIndex((Element element) => element.Entree?.Cle == _choisie);
		if (text != _vueAffichee)
		{
			_vueAffichee = text;
			if (num < 0)
			{
				string guidee = $"q:{QuestMarkers.GuidedQuest()}";
				num = elements.FindIndex((Element element) => element.Entree?.Cle == guidee);
				if (num < 0)
				{
					num = elements.FindIndex((Element element) => !element.EstUnGroupe);
				}
				_choisie = ((num >= 0) ? elements[num].Entree.Cle : string.Empty);
			}
			_premier = ((num >= 0) ? Math.Max(0, num - ((num > 0 && elements[num - 1].EstUnGroupe) ? 1 : 0)) : 0);
			if (_premier <= 2)
			{
				_premier = 0;
			}
		}
		else if (num < 0 && _choisie.Length > 0)
		{
			_choisie = elements.FirstOrDefault((Element element) => !element.EstUnGroupe).Entree?.Cle ?? string.Empty;
		}
	}

	private void ChoisirCategorie(Categorie categorie)
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

	private static void Punaiser(Guid questId)
	{
		if (!TrackedQuests.Toggle(questId))
		{
			ChatboxMsg.AddMessage(new ChatboxMsg(Strings.QuestLog.TrackFull.ToString(5), CustomColors.Alerts.Error, ChatMessageType.Notice));
		}
	}

	private static void Flecher(QuestDescriptor quete)
	{
		if (TrackedQuests.IsStory(quete.Id))
		{
			TrackedQuests.FollowStory();
			return;
		}
		if (TrackedQuests.Followed == quete.Id)
		{
			TrackedQuests.FollowStory();
			return;
		}
		if (!TrackedQuests.IsPinned(quete.Id) && !TrackedQuests.IsFull)
		{
			TrackedQuests.Toggle(quete.Id);
		}
		TrackedQuests.Follow(quete.Id);
	}

	private static void DemanderAbandon(QuestDescriptor quete)
	{
		Guid id = quete.Id;
		ConfirmCardWindow.Open(new ConfirmCardRequest
		{
			Title = Strings.ConfirmCard.AbandonTitle,
			Heading = quete.Name,
			Prompt = Strings.ConfirmCard.AbandonPrompt,
			Detail = Strings.ConfirmCard.AbandonDetail,
			ConfirmLabel = Strings.ConfirmCard.Abandon,
			Danger = true,
			Emblem = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_embleme_quete.png")
		}, delegate
		{
			PacketSender.SendAbandonQuest(id);
		});
	}

	private static void DemanderAbandon(Entree entree)
	{
		LocalizedString localizedString = entree.Sorte switch
		{
			Sorte.Ruban => Strings.Journal.AbandonContratRuban, 
			Sorte.Criminel => Strings.Journal.AbandonContratCriminel, 
			_ => Strings.Journal.AbandonContratPrime, 
		};
		RibbonContractEntryPacket ruban = entree.Ruban;
		Action onConfirm;
		if (ruban != null)
		{
			string cle = ruban.Key;
			onConfirm = delegate
			{
				PacketSender.SendRibbonContract(cle, take: false);
			};
		}
		else
		{
			WantedContractEntryPacket contrat = entree.Contrat;
			if (contrat == null)
			{
				return;
			}
			WantedContractKind kind = contrat.Kind;
			Guid targetId = contrat.TargetId;
			Guid cible = targetId;
			WantedContractKind sorte = kind;
			onConfirm = delegate
			{
				WantedBoardWindow.IgnorerLeProchainPanneau();
				PacketSender.SendWantedContract(sorte, cible, take: false);
			};
		}
		ConfirmCardWindow.Open(new ConfirmCardRequest
		{
			Title = Strings.Journal.AbandonContratTitre,
			Heading = TitreDe(entree),
			Prompt = Strings.Journal.AbandonContratQuestion,
			Detail = localizedString,
			ConfirmLabel = Strings.ConfirmCard.Abandon,
			Danger = true,
			Emblem = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_embleme_quete.png")
		}, onConfirm);
	}

	protected override bool OnMouseWheeled(int delta)
	{
		Point point = LocalMouse();
		if (!base.IsHidden && _liste.Contains(point.X, point.Y))
		{
			_premier = Math.Clamp(_premier + ((delta <= 0) ? 1 : (-1)), 0, _premierMax);
			return true;
		}
		return base.OnMouseWheeled(delta);
	}

	private static (QuestTaskDescriptor? Tache, int Fait, int Quantite, int Rang) OuEnEst(Entree entree)
	{
		QuestDescriptor quete = entree.Quete;
		QuestProgress progres = entree.Progres;
		int num = ((progres != null) ? quete.Tasks.FindIndex((QuestTaskDescriptor tache) => tache.Id == progres.TaskId) : (-1));
		QuestTaskDescriptor questTaskDescriptor = ((num >= 0) ? quete.Tasks[num] : null);
		int num2 = ((questTaskDescriptor != null && questTaskDescriptor.Objective != QuestObjective.EventDriven && questTaskDescriptor.Quantity > 0) ? questTaskDescriptor.Quantity : 0);
		int item = ((num2 > 0) ? Math.Clamp(entree.Progres.TaskProgress, 0, num2) : 0);
		return (Tache: questTaskDescriptor, Fait: item, Quantite: num2, Rang: num);
	}

	private static string TitreDe(Entree entree)
	{
		QuestDescriptor quete = entree.Quete;
		if (quete != null)
		{
			return quete.Name;
		}
		RibbonContractEntryPacket ruban = entree.Ruban;
		if (ruban != null)
		{
			return ruban.Kind switch
			{
				RibbonContractKind.Smuggling => Strings.RubanRouge.TrackerSmuggling.ToString(ruban.Name), 
				RibbonContractKind.Sabotage => Strings.Recherche.SuiviSabotage.ToString(ruban.Name), 
				_ => Strings.RubanRouge.TrackerHead.ToString(ruban.Name), 
			};
		}
		WantedContractEntryPacket contrat = entree.Contrat;
		if (contrat.Kind != WantedContractKind.Criminal)
		{
			if (!contrat.Completed)
			{
				return Strings.WantedBoard.TrackerPolice.ToString(contrat.Name);
			}
			return Strings.WantedBoard.TrackerCompleted.ToString(contrat.Name);
		}
		return Strings.WantedBoard.TrackerCriminal.ToString(contrat.Name);
	}

	private static string SousTitreDe(Entree entree)
	{
		QuestDescriptor quete = entree.Quete;
		if (quete != null)
		{
			switch (entree.Genre)
			{
			case Genre.Histoire:
			{
				string text = StoryThread.ChapterTitle(quete.Folder);
				return (text != null && text.Length > 0) ? (Traduction.Contenu(quete.Folder.Trim()) + " · " + text) : Traduction.Contenu(quete.Folder.Trim());
			}
			case Genre.Enseignement:
				return Strings.Journal.SousEnseignement;
			case Genre.Quotidienne:
				return Strings.Journal.SousQuotidienne;
			default:
				return Strings.Journal.SousAnnexe;
			}
		}
		RibbonContractEntryPacket ruban = entree.Ruban;
		if (ruban != null)
		{
			return Strings.Journal.SousRuban.ToString(NomDuGenre(ruban.Kind));
		}
		WantedContractEntryPacket contrat = entree.Contrat;
		if (contrat.Kind == WantedContractKind.Criminal)
		{
			return Strings.Journal.SousCriminel;
		}
		if (!contrat.Completed)
		{
			return Strings.Journal.SousPrime.ToString(Math.Max(1, contrat.Level));
		}
		return Strings.Journal.SousPrimeRemplie.ToString(WantedPlaces.Elapsed(Math.Max(0, contrat.Minutes) + Vieillesse(Globals.WantedContractsAt)));
	}

	private static string TexteDEtape(QuestTaskDescriptor tache)
	{
		string text = QuestTrackerPanel.ObjectiveOf(tache);
		QuestObjective objective = tache.Objective;
		bool flag = (uint)(objective - 1) <= 1u;
		if (!flag || tache.Quantity <= 1 || !string.IsNullOrWhiteSpace(tache.Description))
		{
			return text;
		}
		return $"{text} ({tache.Quantity})";
	}

	private static string NomDuGenre(RibbonContractKind genre)
	{
		return genre switch
		{
			RibbonContractKind.Smuggling => Strings.RubanRouge.KindSmuggling, 
			RibbonContractKind.Sabotage => Strings.RubanRouge.KindSabotage, 
			_ => Strings.RubanRouge.KindHead, 
		};
	}

	private static int Vieillesse(long recuA)
	{
		return Math.Max(0, (int)((Timing.Global.Milliseconds - recuA) / 60000));
	}

	private static string OuDe(Entree entree)
	{
		RibbonContractEntryPacket ruban = entree.Ruban;
		if (ruban != null)
		{
			string text = WantedPlaces.ZoneOf(ruban.MapId);
			object obj;
			if (text.Length <= 0)
			{
				string text2 = ruban.Place?.Trim();
				obj = ((text2 != null && text2.Length > 0) ? text2 : Strings.WantedBoard.UnknownPlace.ToString());
			}
			else
			{
				obj = text;
			}
			string text3 = (string)obj;
			switch (ruban.Kind)
			{
			case RibbonContractKind.Smuggling:
				return Strings.RubanRouge.TrackerDeliver.ToString(text3);
			case RibbonContractKind.Sabotage:
			{
				LocalizedString suiviSabotageOu = Strings.Recherche.SuiviSabotageOu;
				object[] array = new object[1];
				string text4 = ruban.Place?.Trim();
				array[0] = ((text4 != null && text4.Length > 0) ? text4 : text3);
				return suiviSabotageOu.ToString(array);
			}
			default:
				return Strings.RubanRouge.HeadSeen.ToString(text3, WantedPlaces.Elapsed(Math.Max(0, ruban.Minutes) + Vieillesse(Globals.RibbonContractAt)));
			}
		}
		WantedContractEntryPacket contrat = entree.Contrat;
		if (contrat.Kind == WantedContractKind.Criminal)
		{
			string text5 = WantedPlaces.ZoneOf(contrat.MapId);
			if (contrat.Minutes >= 0 && !(contrat.MapId == Guid.Empty))
			{
				return Strings.WantedBoard.LastSeen.ToString((text5.Length > 0) ? text5 : Strings.WantedBoard.UnknownPlace.ToString(), WantedPlaces.Elapsed(contrat.Minutes + Vieillesse(Globals.WantedContractsAt)));
			}
			return Strings.WantedBoard.NeverSeen;
		}
		if (contrat.Completed)
		{
			return Strings.WantedBoard.TrackerGoBack;
		}
		return Strings.WantedBoard.Spotted.ToString(LieuDePrime(contrat));
	}

	private static string LieuDePrime(WantedContractEntryPacket contrat)
	{
		string text = WantedPlaces.ZoneOf(contrat.MapId);
		string text2 = contrat.Place?.Trim() ?? string.Empty;
		string text3 = ((text.Length == 0) ? text2 : ((text2.Length != 0 && !text2.Contains(text, StringComparison.OrdinalIgnoreCase)) ? (text + ", " + text2) : ((text2.Length == 0) ? text : text2)));
		if (text3.Length <= 0)
		{
			return Strings.WantedBoard.UnknownPlace.ToString();
		}
		return text3;
	}

	private static string Presence(WantedContractEntryPacket contrat)
	{
		if (!contrat.Online)
		{
			if (contrat.OfflineMinutes < 0)
			{
				return Strings.WantedBoard.Offline.ToString();
			}
			return Strings.WantedBoard.OfflineSince.ToString(WantedPlaces.Elapsed(contrat.OfflineMinutes + Vieillesse(Globals.WantedContractsAt)));
		}
		return Strings.WantedBoard.Online.ToString();
	}

	private static Color CouleurDe(Entree entree)
	{
		return entree.Sorte switch
		{
			Sorte.Prime => entree.Contrat.Completed ? TechniqueArt.Gold : BleuPolice, 
			Sorte.Criminel => TechniqueArt.Red, 
			Sorte.Ruban => Brun, 
			_ => (entree.Etat != Etat.Terminee) ? (entree.Genre switch
			{
				Genre.Histoire => TechniqueArt.Gold, 
				Genre.Enseignement => Violet, 
				Genre.Quotidienne => TechniqueArt.Cyan, 
				_ => TechniqueArt.Green, 
			}) : TechniqueArt.Green, 
		};
	}

	private static string Montant(long valeur)
	{
		return MarketWindow.FormatAmount(valeur);
	}

	protected override void Dessiner(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		DessinerCadre(renderer);
		int croix = DessinerBarre(renderer, Strings.QuestLog.Title);
		_zonesBulles.Clear();
		_liste = default(Rectangle);
		Player me = Globals.Me;
		if (me != null)
		{
			Actualiser(me);
			List<Element> elements = Vue();
			SuivreLaVue(elements);
			DessinerEntete(renderer, croix);
			DessinerGauche(renderer, me);
			DessinerCentre(renderer, elements);
			DessinerFiche(renderer, me, elements);
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
				if (_bulle == null || _bulleCle != text)
				{
					_bulleCle = text;
					_bulle = func();
				}
				DessinerBulle(renderer, _bulle, cible);
				break;
			}
		}
	}

	private void DessinerEntete(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int croix)
	{
		int num = _d.Y(23f);
		int num2 = croix - _d.L(18f);
		string text = $"{TrackedQuests.Count} / {5}";
		num2 -= TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(16f), text, num2, num, A(TechniqueArt.Gold)) + _d.L(10f);
		num2 -= Etiquette(renderer, Strings.Journal.EnteteSuivies, num2, num) + _d.L(26f);
		if (DatabaseObject<QuestDescriptor>.TryGet(Globals.StoryQuestId, out var databaseObject))
		{
			(int, int)? tuple = StoryThread.RankOf(databaseObject);
			if (tuple.HasValue)
			{
				(int, int) valueOrDefault = tuple.GetValueOrDefault();
				string text2 = Strings.Journal.EnteteHistoireValeur.ToString(Traduction.Contenu(databaseObject.Folder.Trim()), valueOrDefault.Item1, valueOrDefault.Item2);
				num2 -= TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(16f), text2, num2, num, A(TechniqueArt.Text)) + _d.L(10f);
				Etiquette(renderer, Strings.Journal.CatHistoire, num2, num);
			}
		}
	}

	private int Etiquette(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string texte, int droite, int centre)
	{
		string text = texte.ToUpperInvariant();
		int num = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), text, _d.Em(0.16f, 10f));
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), text, droite - num, centre + _d.L(1f), _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
		return num;
	}

	private void DessinerGauche(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me)
	{
		Panneau(renderer, _d.R(14f, 58f, 232f, 676f));
		DessinerHistoire(renderer, me);
		float bas = DessinerAEcran(renderer);
		DessinerCategories(renderer, 214f, bas);
	}

	private void DessinerHistoire(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me)
	{
		Rectangle rect = _d.R(15f, 59f, 230f, 155f);
		TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Gold, 0.13f), A(TechniqueArt.Gold, 0.05f), A(TechniqueArt.Gold, 0f));
		TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Gold));
		TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Bottom, rect.Width, 1), A(TechniqueArt.Line));
		Rectangle rect2 = _d.R(30f, 72f, 48f, 48f);
		TechniqueArt.Fill(renderer, rect2, A(Case));
		TechniqueArt.Outline(renderer, rect2, A(TechniqueArt.Line2));
		int num = _d.L(36f);
		Lisser("saiyan_embleme_quete.png", new Rectangle(rect2.X + (rect2.Width - num) / 2, rect2.Y + (rect2.Height - num) / 2, num, num), A(TechniqueArt.White));
		int num2 = _d.X(90f);
		int num3 = _d.X(230f) - num2;
		if (!DatabaseObject<QuestDescriptor>.TryGet(Globals.StoryQuestId, out var quete) || !StoryThread.IsStoryQuest(quete))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(20f), Strings.Journal.CatHistoire.ToString().ToUpperInvariant(), num2, _d.Y(84f), A(TechniqueArt.Text));
			float num4 = 142f;
			{
				foreach (string item in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), Strings.Journal.CarteFin, _d.L(200f), 3))
				{
					TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), item, _d.X(30f), _d.Y(num4), A(TechniqueArt.Dim));
					num4 += 16f;
				}
				return;
			}
		}
		string dossier = quete.Folder.Trim();
		string text = Traduction.Contenu(dossier).ToUpperInvariant();
		int size = Ajuster(renderer, TechniqueArt.Heavy, text, num3, 20f, 17f, 15f);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, size, TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, size, text, num3, 1).FirstOrDefault(), num2, _d.Y(84f), A(TechniqueArt.Text));
		float num5 = 102f;
		foreach (string item2 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), StoryThread.ChapterTitle(dossier) ?? string.Empty, num3, 2))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), item2, num2, _d.Y(num5), A(TechniqueArt.Dim));
			num5 += 14f;
		}
		(int, int) tuple = StoryThread.RankOf(quete) ?? (1, 1);
		int num6 = _d.Y(145f);
		int num7 = _d.X(30f);
		num7 += TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), Strings.Journal.CarteQuete.ToString().ToUpperInvariant(), num7, num6 + _d.L(5f), _d.Em(0.16f, 10f), A(TechniqueArt.Dim)) + _d.L(8f);
		num7 += TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(34f), $"{tuple.Item1}", num7, num6, A(TechniqueArt.Gold)) + _d.L(6f);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(15f), $"/ {tuple.Item2}", num7, num6 + _d.L(5f), A(Muted));
		List<QuestDescriptor> list = (from autre in DatabaseObject<QuestDescriptor>.Lookup.Values.OfType<QuestDescriptor>()
			where string.Equals(autre.Folder?.Trim(), dossier, StringComparison.OrdinalIgnoreCase)
			select autre).ToList();
		QuestProgress value;
		int num8 = list.Count((QuestDescriptor autre) => me.QuestProgress.TryGetValue(autre.Id, out value) && value.Completed && value.TaskId == Guid.Empty);
		Rectangle rect3 = _d.R(30f, 170f, 200f, 6f);
		TechniqueArt.Fill(renderer, rect3, A(new Color(255, 7, 10, 18)));
		TechniqueArt.Outline(renderer, rect3, A(TechniqueArt.Line));
		int num9 = (int)Math.Round((double)(rect3.Width - 2) * Math.Clamp((double)num8 / (double)Math.Max(1, list.Count), 0.0, 1.0));
		if (num9 > 0)
		{
			TechniqueArt.FillHorizontal(renderer, new Rectangle(rect3.X + 1, rect3.Y + 1, num9, rect3.Height - 2), A(new Color(255, 184, 97, 15)), A(TechniqueArt.Ki), A(TechniqueArt.Gold));
		}
		string text2;
		if (me.QuestProgress.TryGetValue(quete.Id, out QuestProgress progresHistoire) && progresHistoire.TaskId != Guid.Empty)
		{
			text2 = Strings.Journal.CarteSuite.ToString(QuestTrackerPanel.ObjectiveOf(quete.Tasks.FirstOrDefault((QuestTaskDescriptor tache) => tache.Id == progresHistoire.TaskId)));
		}
		else if (Globals.StoryGiver.Length > 0)
		{
			text2 = Strings.Story.Talk.ToString(Globals.StoryGiver);
			if (Globals.StoryPlace.Length > 0)
			{
				text2 = text2 + " · " + Globals.StoryPlace;
			}
		}
		else
		{
			text2 = Strings.Story.Find;
		}
		int num10 = rect3.Bottom + _d.L(13f);
		foreach (string item3 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), text2, _d.L(200f), 2))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), item3, _d.X(30f), num10, A(TechniqueArt.Soft));
			num10 += _d.L(16f);
		}
		AddHit(rect, "histoire", delegate
		{
			_choisie = $"q:{quete.Id}";
			ChoisirCategorie(Categorie.EnCours);
			_vueAffichee = "-";
		});
		Rectangle rectangle = _d.R(204f, 132f, 26f, 26f);
		bool storyShown = TrackedQuests.StoryShown;
		bool flag = IsHot("punaise:histoire");
		DessinerPunaise(renderer, rectangle, storyShown, flag);
		AddHit(rectangle, "punaise:histoire", TrackedQuests.ToggleStory);
		if (flag)
		{
			Etiqueter("punaise:histoire", rectangle, AidePunaise(quete.Id, storyShown));
		}
	}

	private void DessinerCategories(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float haut, float bas)
	{
		List<(Categorie, string)> list = new List<(Categorie, string)> { (Categorie.EnCours, Strings.Journal.CatEnCours) };
		(Categorie, string)[] array = new(Categorie, string)[6]
		{
			(Categorie.Histoire, Strings.Journal.CatHistoire),
			(Categorie.Enseignements, Strings.Journal.CatEnseignements),
			(Categorie.Annexes, Strings.Journal.CatAnnexes),
			(Categorie.Quotidiennes, Strings.Journal.CatQuotidiennes),
			(Categorie.Contrats, Strings.Journal.CatContrats),
			(Categorie.AFaire, Strings.Journal.CatAFaire)
		};
		for (int i = 0; i < array.Length; i++)
		{
			var (categorie, item) = array[i];
			if (Nombre(categorie) > 0 || _categorie == categorie)
			{
				list.Add((categorie, item));
			}
		}
		int val = list.Count + 1;
		float num = haut + 32f;
		float num2 = Math.Min(34f, (bas - 10f - num - 26f) / (float)Math.Max(1, val));
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), Strings.Journal.CatEnCours.ToString().ToUpperInvariant(), _d.X(32f), _d.Y(haut + 19f), _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
		float num3 = num;
		foreach (var (categorie2, nom) in list)
		{
			DessinerCategorie(renderer, categorie2, nom, num3, num2);
			num3 += num2;
		}
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), Strings.Journal.RubriqueFini.ToString().ToUpperInvariant(), _d.X(32f), _d.Y(num3 + 12f), _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
		DessinerCategorie(renderer, Categorie.Terminees, Strings.Journal.CatTerminees, num3 + 26f, num2);
	}

	private void DessinerCategorie(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Categorie categorie, string nom, float y, float pas)
	{
		Rectangle rect = _d.R(24f, y, 212f, pas - 2f);
		string cle = $"categorie:{categorie}";
		bool flag = _requete.Length == 0 && _categorie == categorie;
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
		int num = _d.L(16f);
		Rectangle rectangle = new Rectangle(rect.X + _d.L(10f), rect.Y + (rect.Height - num) / 2, num, num);
		Color couleur = A(flag ? TechniqueArt.Ki : (flag2 ? TechniqueArt.Soft : TechniqueArt.Dim));
		switch (categorie)
		{
		case Categorie.EnCours:
			Lisser("inv_glyphe_tout.png", rectangle, couleur);
			break;
		case Categorie.Contrats:
			GlypheCible(renderer, rectangle, couleur);
			break;
		case Categorie.Terminees:
			Lisser("saiyan_suivi_coche.png", rectangle, couleur);
			break;
		default:
			GlypheLosange(renderer, rectangle, couleur, categorie == Categorie.Histoire);
			break;
		}
		int centerY = rect.Y + rect.Height / 2;
		int num2 = TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(13f), $"{Nombre(categorie)}", rect.Right - _d.L(10f), centerY, A(Muted));
		int width = rect.Width - _d.L(36f) - _d.L(10f) - num2 - _d.L(8f);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(15f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(15f), nom, width, 1).FirstOrDefault(), rect.X + _d.L(36f), centerY, A(flag ? TechniqueArt.Text : TechniqueArt.Soft));
		AddHit(rect, cle, delegate
		{
			ChoisirCategorie(categorie);
		});
	}

	private float DessinerAEcran(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		QuestDescriptor databaseObject;
		QuestDescriptor questDescriptor = ((DatabaseObject<QuestDescriptor>.TryGet(Globals.StoryQuestId, out databaseObject) && StoryThread.IsStoryQuest(databaseObject)) ? databaseObject : null);
		QuestDescriptor databaseObject2;
		List<string> list = (from id in TrackedQuests.Pinned
			where !TrackedQuests.IsStory(id)
			select (!DatabaseObject<QuestDescriptor>.TryGet(id, out databaseObject2)) ? null : databaseObject2.Name into nom
			where nom != null
			select nom).Cast<string>().ToList();
		int num = _entrees.Count((Entree entree) => entree.EstUnContrat);
		List<string> list2 = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), Strings.Journal.AEcranAide.ToString(5), _d.L(204f), 2);
		int num2 = ((questDescriptor != null) ? 1 : 0) + list.Count + ((num > 0) ? 1 : 0);
		int num3 = 34 + num2 * 21 + list2.Count * 15 + 12;
		float num4 = 734f - (float)num3;
		TechniqueArt.Fill(renderer, _d.R(15f, num4, 230f, 1f), A(TechniqueArt.Line));
		int num5 = _d.X(28f);
		int num6 = _d.X(232f);
		float num7 = num4 + 14f + 8f;
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), Strings.Journal.AEcran.ToString().ToUpperInvariant(), num5, _d.Y(num7), _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
		int num8 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(15f), $"/ {5}", num6, _d.Y(num7), A(Muted));
		TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(15f), $"{list.Count} ", num6 - num8, _d.Y(num7), A(TechniqueArt.Text));
		num7 += 22f;
		int width = _d.L(184f);
		if (questDescriptor != null)
		{
			int num9 = _d.L(14f);
			bool storyShown = TrackedQuests.StoryShown;
			Lisser("saiyan_embleme_quete.png", new Rectangle(num5, _d.Y(num7) - num9 / 2, num9, num9), A(TechniqueArt.White, storyShown ? 1f : 0.35f));
			string text = (storyShown ? $"{Strings.Journal.CatHistoire} · {questDescriptor.Name}" : Strings.Journal.AEcranHistoireRetiree.ToString());
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), text, width, 1).FirstOrDefault(), num5 + _d.L(20f), _d.Y(num7), A(storyShown ? TechniqueArt.Gold : Muted));
			num7 += 21f;
		}
		foreach (string item in list)
		{
			int num10 = _d.L(13f);
			Lisser("saiyan_epingle.png", new Rectangle(num5, _d.Y(num7) - num10 / 2, num10, num10), A(TechniqueArt.Ki));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), item, width, 1).FirstOrDefault(), num5 + _d.L(20f), _d.Y(num7), A(TechniqueArt.Soft));
			num7 += 21f;
		}
		if (num > 0)
		{
			int num11 = _d.L(14f);
			GlypheOeil(renderer, new Rectangle(num5, _d.Y(num7) - num11 / 2, num11, num11), A(TechniqueArt.Dim));
			string text2 = ((num == 1) ? Strings.Journal.AEcranContratUn.ToString() : Strings.Journal.AEcranContrats.ToString(num));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), text2, width, 1).FirstOrDefault(), num5 + _d.L(20f), _d.Y(num7), A(TechniqueArt.Dim));
			num7 += 21f;
		}
		num7 += 2f;
		foreach (string item2 in list2)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), item2, num5, _d.Y(num7), A(Muted));
			num7 += 15f;
		}
		return num4;
	}

	private void DessinerCentre(Intersect.Client.Framework.Gwen.Renderer.Base renderer, List<Element> elements)
	{
		DessinerRecherche(renderer);
		DessinerFiltres(renderer, _d.R(688f, 58f, 200f, 40f));
		List<Entree> list = (from element2 in elements
			where !element2.EstUnGroupe
			select element2.Entree).ToList();
		int num = list.Count((Entree entree) => !entree.EstUnContrat);
		int num2 = list.Count - num;
		string titre;
		string complement;
		if (_requete.Length > 0)
		{
			string text = _recherche.Text?.Trim() ?? string.Empty;
			titre = Strings.Journal.TitreRecherche;
			complement = ((list.Count == 1) ? Strings.Journal.ResultatUn.ToString(text) : Strings.Journal.ResultatsN.ToString(list.Count, text));
		}
		else
		{
			titre = _categorie switch
			{
				Categorie.Histoire => Strings.Journal.CatHistoire, 
				Categorie.Enseignements => Strings.Journal.CatEnseignements, 
				Categorie.Annexes => Strings.Journal.CatAnnexes, 
				Categorie.Quotidiennes => Strings.Journal.CatQuotidiennes, 
				Categorie.Contrats => Strings.Journal.CatContrats, 
				Categorie.AFaire => Strings.Journal.CatAFaire, 
				Categorie.Terminees => Strings.Journal.CatTerminees, 
				_ => Strings.Journal.CatEnCours.ToString(), 
			};
			List<string> list2 = new List<string>();
			if (_categorie == Categorie.Contrats)
			{
				list2.Add(Strings.Journal.SousTitreContrats.ToString(list.Count((Entree entree) => entree.Sorte != Sorte.Ruban), Math.Max(1, Options.Instance.Karma.WantedMaxContracts)));
			}
			else
			{
				if (num > 0 || num2 == 0)
				{
					list2.Add((num == 1) ? Strings.Journal.QuetesUne.ToString() : Strings.Journal.QuetesN.ToString(num));
				}
				if (num2 > 0)
				{
					list2.Add((num2 == 1) ? Strings.Journal.ContratsUn.ToString() : Strings.Journal.ContratsN.ToString(num2));
				}
				if (_categorie == Categorie.Terminees)
				{
					list2.Add(Strings.Journal.RelireHistoire);
				}
			}
			complement = string.Join(" · ", list2);
		}
		Titre(renderer, titre, complement);
		Colonnes(renderer);
		_liste = _d.R(258f, 170f, 630f, 524f);
		_premierMax = PremierMax(elements);
		_premier = Math.Clamp(_premier, 0, _premierMax);
		if (elements.Count == 0)
		{
			string text2;
			if (_requete.Length > 0)
			{
				text2 = Strings.Journal.VideRecherche.ToString(_recherche.Text?.Trim() ?? string.Empty);
			}
			else
			{
				string text3 = ((!_suiviesSeulement) ? (_categorie switch
				{
					Categorie.EnCours => (_entrees.Count == 0) ? Strings.QuestLog.Empty.ToString() : Strings.Journal.VideEnCours.ToString(), 
					Categorie.Contrats => Strings.Journal.VideContrats.ToString(), 
					Categorie.Terminees => Strings.Journal.VideTerminees.ToString(), 
					_ => Strings.Journal.VideCategorie.ToString(), 
				}) : Strings.Journal.VideSuivies.ToString());
				text2 = text3;
			}
			string text4 = text2;
			float num3 = 190f;
			foreach (string item in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(13f), text4, _d.L(590f), 3))
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), item, _d.X(270f), _d.Y(num3), A(TechniqueArt.Dim));
				num3 += 19f;
			}
		}
		float num4 = 170f;
		for (int num5 = _premier; num5 < elements.Count; num5++)
		{
			Element element = elements[num5];
			float num6 = (element.EstUnGroupe ? 26f : 58f);
			if (num4 + num6 - (element.EstUnGroupe ? 0f : 4f) > 694.5f)
			{
				break;
			}
			if (element.EstUnGroupe)
			{
				DessinerGroupe(renderer, element, num4);
			}
			else
			{
				DessinerLigne(renderer, element.Entree, num4);
			}
			num4 += num6;
		}
		if (_premierMax > 0)
		{
			Rectangle rect = new Rectangle(_d.X(882f), _liste.Y, _d.L(4f), _liste.Height);
			TechniqueArt.Fill(renderer, rect, A(new Color(255, 10, 14, 24)));
			int num7 = Math.Max(_d.L(24f), rect.Height * (elements.Count - _premierMax) / Math.Max(1, elements.Count));
			int y = rect.Y + (rect.Height - num7) * _premier / Math.Max(1, _premierMax);
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, y, rect.Width, num7), A(TechniqueArt.Line2));
		}
		int centerY = _d.Y(719f);
		int num8 = _d.X(888f);
		if (_premierMax > 0)
		{
			num8 -= TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(13f), Strings.Atelier.Molette, num8, centerY, A(Muted)) + _d.L(16f);
		}
		int num9 = _d.X(258f);
		string text5;
		if (_categorie == Categorie.Contrats && _requete.Length == 0)
		{
			text5 = Strings.Journal.PiedContrats;
		}
		else if (_categorie == Categorie.Terminees && _requete.Length == 0)
		{
			int num10 = list.Count((Entree entree) => entree.Genre == Genre.Histoire);
			text5 = Strings.Journal.PiedTerminees.ToString(num10, list.Count - num10);
		}
		else
		{
			text5 = Strings.Journal.PiedSuivies.ToString(TrackedQuests.Count, 5);
		}
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(13f), text5, Math.Max(_d.L(40f), num8 - num9), 1).FirstOrDefault(), num9, centerY, A(Muted));
	}

	private static int PremierMax(List<Element> elements)
	{
		float num = 524f;
		float num2 = 0f;
		for (int num3 = elements.Count - 1; num3 >= 0; num3--)
		{
			float num4 = (elements[num3].EstUnGroupe ? 26f : 58f);
			if (num2 + num4 - ((num3 == elements.Count - 1 && !elements[num3].EstUnGroupe) ? 4f : 0f) > num)
			{
				return num3 + 1;
			}
			num2 += num4;
		}
		return 0;
	}

	private void DessinerRecherche(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		Rectangle rect = _d.R(258f, 58f, 420f, 40f);
		bool flag = !string.IsNullOrEmpty(_recherche.Text);
		TechniqueArt.Fill(renderer, rect, A(new Color(255, 10, 14, 24)));
		TechniqueArt.Outline(renderer, rect, A((flag || InputHandler.KeyboardFocus == _recherche) ? TechniqueArt.Ki : TechniqueArt.Line2));
		int num = _d.L(15f);
		Lisser("inv_loupe.png", new Rectangle(rect.X + _d.L(14f), rect.Y + (rect.Height - num) / 2, num, num), A(TechniqueArt.Dim));
		AddHit(rect, "recherche", delegate
		{
			_recherche.Focus();
		}, string.Empty);
		if (!flag)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(15f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(15f), Strings.Journal.RechercheIndice, rect.Width - _d.L(52f), 1).FirstOrDefault(), rect.X + _d.L(40f), rect.Y + rect.Height / 2, A(Muted));
			return;
		}
		Rectangle rect2 = new Rectangle(rect.Right - _d.L(32f), rect.Y + (rect.Height - _d.L(26f)) / 2, _d.L(26f), _d.L(26f));
		bool flag2 = IsHot("effacer");
		int num2 = _d.L(11f);
		Lisser("tech_croix.png", new Rectangle(rect2.X + (rect2.Width - num2) / 2, rect2.Y + (rect2.Height - num2) / 2, num2, num2), A(flag2 ? TechniqueArt.White : TechniqueArt.Dim));
		AddHit(rect2, "effacer", Effacer);
		if (flag2)
		{
			Etiqueter("effacer", rect2, Strings.Inventaire.CloseSearch);
		}
	}

	private void DessinerFiltres(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle zone)
	{
		(bool, string)[] array = new(bool, string)[2]
		{
			(false, Strings.Journal.FiltreToutes.ToString().ToUpperInvariant()),
			(true, Strings.Journal.FiltreSuivies.ToString().ToUpperInvariant())
		};
		float espacement = _d.Em(0.1f, 12f);
		int taille;
		for (taille = Pt(12f); array.Sum(((bool Suivies, string Nom) filtre) => TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, taille, filtre.Nom, espacement) + _d.L(16f)) > zone.Width && taille > 9; taille--)
		{
		}
		int num = zone.X;
		for (int num2 = 0; num2 < array.Length; num2++)
		{
			int num3 = ((num2 == array.Length - 1) ? (zone.Right - num) : (zone.Width / array.Length));
			Rectangle rect = new Rectangle(num, zone.Y, num3, zone.Height);
			(bool, string) tuple = array[num2];
			bool item = tuple.Item1;
			string item2 = tuple.Item2;
			string cle = (item ? "filtre:suivies" : "filtre:toutes");
			bool flag = _suiviesSeulement == item;
			bool flag2 = IsHot(cle);
			if (flag)
			{
				TechniqueArt.FillVertical(renderer, rect, A(new Color(255, 255, 194, 122)), A(new Color(255, 224, 117, 26)));
			}
			else
			{
				TechniqueArt.Fill(renderer, rect, A(flag2 ? new Color(255, 16, 22, 36) : new Color(255, 10, 14, 24)));
			}
			if (num2 > 0)
			{
				TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, 1, rect.Height), A(TechniqueArt.Line2));
			}
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, taille, item2, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, espacement, A(flag ? new Color(255, 23, 11, 2) : (flag2 ? TechniqueArt.Text : TechniqueArt.Dim)));
			bool choix = item;
			AddHit(rect, cle, delegate
			{
				_suiviesSeulement = choix;
			});
			num += num3;
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
		Colonne(Strings.Journal.ColonneQuete, 64f);
		Colonne(Strings.Journal.ColonneOu, 280f);
		TechniqueArt.SpacedRight(renderer, TechniqueArt.Label, Pt(10f), Strings.Journal.ColonneRecompense.ToString().ToUpperInvariant(), _d.X(832f), centre, _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
		void Colonne(string texte, float colonne)
		{
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), texte.ToUpperInvariant(), _d.X(258f + colonne), centre, _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
		}
	}

	private void DessinerGroupe(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Element element, float y)
	{
		int num = _d.Y(y + 26f - 8f);
		int num2 = _d.X(260f);
		num2 += TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(11f), element.Titre.ToUpperInvariant(), num2, num, _d.Em(0.18f, 11f), A(element.Couleur)) + _d.L(12f);
		int num3 = _d.X(872f);
		if (element.Detail.Length > 0)
		{
			num2 += TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(11.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(11.5f), element.Detail, Math.Max(_d.L(20f), num3 - num2 - _d.L(30f)), 1).FirstOrDefault(), num2, num, A(Muted)) + _d.L(12f);
		}
		if (num3 > num2)
		{
			TechniqueArt.Fill(renderer, new Rectangle(num2, num, num3 - num2, 1), A(TechniqueArt.Line));
		}
	}

	private void DessinerLigne(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Entree entree, float y)
	{
		Rectangle rect = _d.R(258f, y, 614f, 54f);
		string cle = "ligne:" + entree.Cle;
		bool flag = entree.Cle == _choisie;
		bool flag2 = IsHot(cle);
		WantedContractEntryPacket contrat = entree.Contrat;
		bool flag3 = contrat != null && contrat.Completed && entree.Sorte == Sorte.Prime;
		if (flag)
		{
			TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Ki, 0.16f), A(TechniqueArt.Ki, 0.06f), A(TechniqueArt.Ki, 0.02f));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Ki));
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Ki));
		}
		else if (flag3)
		{
			TechniqueArt.FillHorizontal(renderer, rect, A(TechniqueArt.Gold, 0.1f), A(TechniqueArt.Gold, 0.04f), A(TechniqueArt.Gold, 0f));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Gold, flag2 ? 0.7f : 0.45f));
		}
		else
		{
			TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, (entree.Etat == Etat.Terminee) ? 0.015f : (flag2 ? 0.055f : 0.03f)));
			TechniqueArt.Outline(renderer, rect, A(flag2 ? TechniqueArt.Line2 : TechniqueArt.Line));
		}
		int num = rect.Y + rect.Height / 2;
		Rectangle carre = _d.R(266f, y + 6f, 42f, 42f);
		DessinerCase(renderer, carre, entree);
		int x = _d.X(322f);
		int width = _d.L(204f);
		Color color = (flag3 ? TechniqueArt.Gold : ((entree.Etat == Etat.Terminee) ? TechniqueArt.Soft : TechniqueArt.Text));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(19f), TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(19f), TitreDe(entree), width, 1).FirstOrDefault(), x, num - _d.L(9f), A(color));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), SousTitreDe(entree), width, 1).FirstOrDefault(), x, num + _d.L(11f), A(TechniqueArt.Dim));
		int x2 = _d.X(538f);
		int num2 = _d.L(170f);
		QuestDescriptor quete = entree.Quete;
		if (quete != null)
		{
			Color color2 = TechniqueArt.Soft;
			string text;
			if (entree.Etat == Etat.EnCours)
			{
				(QuestTaskDescriptor? Tache, int Fait, int Quantite, int Rang) tuple = OuEnEst(entree);
				QuestTaskDescriptor item = tuple.Tache;
				int item2 = tuple.Fait;
				int item3 = tuple.Quantite;
				int item4 = tuple.Rang;
				text = QuestTrackerPanel.ObjectiveOf(item);
				int num5;
				int num6;
				if (item3 <= 0)
				{
					int num3 = Math.Max(0, item4) + 1;
					int num4 = Math.Max(1, quete.Tasks.Count);
					num5 = num4;
					num6 = num3;
				}
				else
				{
					int num4 = item3;
					num5 = num4;
					num6 = item2;
				}
				Jauge(renderer, x2, num, num2, (item3 > 0) ? ((float)num6 / (float)num5) : ((float)Math.Max(0, item4) / (float)Math.Max(1, quete.Tasks.Count)), TechniqueArt.Ki, $"{num6} / {num5}");
			}
			else if (entree.Etat == Etat.Terminee)
			{
				string text2 = Strings.Journal.OuTerminee;
				color2 = TechniqueArt.Green;
				text = text2;
				Jauge(renderer, x2, num, num2, 1f, TechniqueArt.Green, $"{quete.Tasks.Count} / {quete.Tasks.Count}");
			}
			else
			{
				text = Strings.Journal.OuAFaire;
				Jauge(renderer, x2, num, num2, 0f, TechniqueArt.Ki, $"0 / {quete.Tasks.Count}");
			}
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), text, num2, 1).FirstOrDefault(), x2, num - _d.L(9f), A(color2));
		}
		else
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), OuDe(entree), num2, 1).FirstOrDefault(), x2, num - _d.L(9f), A(TechniqueArt.Soft));
			if (flag3)
			{
				Puce(renderer, Strings.WantedBoard.ToCashIn, x2, num + _d.L(10f), TechniqueArt.Gold);
			}
			else
			{
				WantedContractEntryPacket contrat2 = entree.Contrat;
				if (contrat2 != null && contrat2.Kind == WantedContractKind.Criminal)
				{
					TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(12f), "• " + Presence(contrat2), x2, num + _d.L(10f), A(contrat2.Online ? TechniqueArt.Green : TechniqueArt.Dim));
				}
				else
				{
					Jauge(renderer, x2, num, num2, 0f, TechniqueArt.Ki, "0 / 1");
				}
			}
		}
		DessinerRecompenseCourte(renderer, entree, rect.X + _d.L(574f), num);
		Rectangle rectangle = _d.R(839f, y + 14f, 26f, 26f);
		if (entree.EstUnContrat)
		{
			int num7 = _d.L(15f);
			GlypheOeil(renderer, new Rectangle(rectangle.X + (rectangle.Width - num7) / 2, rectangle.Y + (rectangle.Height - num7) / 2, num7, num7), A(Muted));
			if (IsHot("oeil:" + entree.Cle))
			{
				Etiqueter("oeil:" + entree.Cle, rectangle, Strings.Journal.OeilAide);
			}
			AddHit(rect, cle, delegate
			{
				_choisie = entree.Cle;
			});
			AddHit(rectangle, "oeil:" + entree.Cle, delegate
			{
				_choisie = entree.Cle;
			});
			return;
		}
		AddHit(rect, cle, delegate
		{
			_choisie = entree.Cle;
		});
		if (entree.Etat == Etat.EnCours && entree.Quete != null)
		{
			Guid id = entree.Quete.Id;
			bool punaisee = TrackedQuests.IsPinned(id);
			string cle2 = "punaise:" + entree.Cle;
			bool flag4 = IsHot(cle2);
			DessinerPunaise(renderer, rectangle, punaisee, flag4);
			AddHit(rectangle, cle2, delegate
			{
				Punaiser(id);
			});
			if (flag4)
			{
				Etiqueter(cle2, rectangle, AidePunaise(id, punaisee));
			}
		}
	}

	private void DessinerPunaise(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle boite, bool punaisee, bool chaud)
	{
		if (punaisee)
		{
			TechniqueArt.FillVertical(renderer, boite, A(new Color(255, 255, 194, 122)), A(new Color(255, 224, 117, 26)));
		}
		else
		{
			TechniqueArt.Fill(renderer, boite, A(Case));
			TechniqueArt.Outline(renderer, boite, A(chaud ? TechniqueArt.Soft : TechniqueArt.Line2));
		}
		int num = _d.L(13f);
		Lisser(punaisee ? "saiyan_epingle.png" : "saiyan_epingle_vide.png", new Rectangle(boite.X + (boite.Width - num) / 2, boite.Y + (boite.Height - num) / 2, num, num), A(punaisee ? new Color(255, 23, 11, 2) : (chaud ? TechniqueArt.Text : Muted)));
	}

	private static string AidePunaise(Guid questId, bool punaisee)
	{
		if (TrackedQuests.IsStory(questId))
		{
			if (!punaisee)
			{
				return Strings.Journal.HistoireRemettre.ToString(5);
			}
			return Strings.Journal.HistoireRetirer.ToString();
		}
		if (!punaisee)
		{
			if (!TrackedQuests.IsFull)
			{
				return Strings.QuestLog.TrackTooltipPin.ToString();
			}
			return Strings.QuestLog.TrackFull.ToString(5);
		}
		return Strings.QuestLog.TrackTooltipUnpin.ToString();
	}

	private void DessinerCase(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle carre, Entree entree)
	{
		TechniqueArt.Fill(renderer, carre, A(Case));
		TechniqueArt.Outline(renderer, carre, A(TechniqueArt.Line2));
		Color color = CouleurDe(entree);
		int num = carre.X + carre.Width / 2;
		int num2 = carre.Y + carre.Height / 2;
		bool flag = false;
		WantedContractEntryPacket contrat = entree.Contrat;
		if (contrat != null && contrat.Kind == WantedContractKind.Police && DatabaseObject<NPCDescriptor>.TryGet(contrat.TargetId, out var databaseObject))
		{
			flag = Planche(renderer, databaseObject.Sprite, carre, recadrer: true);
		}
		else
		{
			RibbonContractEntryPacket ruban = entree.Ruban;
			if (ruban != null)
			{
				if (!string.IsNullOrEmpty(ruban.Face))
				{
					IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Face, ruban.Face);
					if (gameTexture != null)
					{
						int num3 = carre.Width - _d.L(8f);
						Lisser(gameTexture, new Rectangle(num - num3 / 2, num2 - num3 / 2, num3, num3), A(TechniqueArt.White));
						flag = true;
						goto IL_0194;
					}
				}
				if (!string.IsNullOrEmpty(ruban.Icon))
				{
					IGameTexture gameTexture2 = Globals.ContentManager?.GetTexture(TextureType.Item, ruban.Icon);
					if (gameTexture2 != null)
					{
						int num4 = TailleIcone(gameTexture2.Width);
						SaiyanHud.Draw(renderer, gameTexture2, num - num4 / 2, num2 - num4 / 2, num4, num4, A(TechniqueArt.White));
						flag = true;
					}
				}
			}
		}
		goto IL_0194;
		IL_0194:
		if (!flag)
		{
			if (entree.EstUnContrat)
			{
				GlypheCible(renderer, new Rectangle(num - _d.L(11f), num2 - _d.L(11f), _d.L(22f), _d.L(22f)), A(color));
			}
			else
			{
				int num5 = _d.L(16f);
				TechniqueArt.Glow(renderer, num, num2, _d.L(16f), A(color, 0.18f));
				DungeonHud.Diamond(renderer, num - num5 / 2, num2, num5, A(color));
			}
		}
		int num6 = Math.Max(1, _d.L(2f));
		TechniqueArt.Fill(renderer, new Rectangle(carre.X + 2, carre.Bottom - 2 - num6, carre.Width - 4, num6), A(color, (entree.Etat == Etat.Terminee) ? 0.6f : 1f));
	}

	private bool Planche(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string? nom, Rectangle cadre, bool recadrer)
	{
		if (!string.IsNullOrEmpty(nom))
		{
			IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Entity, nom);
			if (gameTexture != null)
			{
				int num = gameTexture.Width / 4;
				int num2 = gameTexture.Height / 4;
				int num3 = Math.Max(1, TailleIcone() / 32);
				if (!recadrer)
				{
					num3 = Math.Max(num3, Math.Min(cadre.Width / Math.Max(1, num), cadre.Height / Math.Max(1, num2)));
				}
				int num4 = (recadrer ? Math.Min(num, (cadre.Width - 2) / num3) : num);
				int num5 = (recadrer ? Math.Min(num2, (cadre.Height - 2) / num3) : num2);
				float num6 = (float)(num - num4) / 2f / (float)gameTexture.Width;
				float v = (float)(num2 - num5) / (float)gameTexture.Height;
				float u = num6 + (float)num4 / (float)gameTexture.Width;
				float v2 = (float)num2 / (float)gameTexture.Height;
				int x = cadre.X + (cadre.Width - num4 * num3) / 2;
				int y = (recadrer ? (cadre.Bottom - 1 - num5 * num3) : (cadre.Y + (cadre.Height - num5 * num3) / 2));
				SaiyanHud.Draw(renderer, gameTexture, x, y, num4 * num3, num5 * num3, A(TechniqueArt.White), num6, v, u, v2);
				return true;
			}
		}
		return false;
	}

	private void Jauge(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int x, int milieu, int largeur, float part, Color couleur, string compte)
	{
		int num = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(14f), compte, x + largeur, milieu + _d.L(10f), A(TechniqueArt.Text));
		Rectangle rect = new Rectangle(x, milieu + _d.L(9f), Math.Max(_d.L(20f), largeur - num - _d.L(8f)), Math.Max(2, _d.L(4f)));
		TechniqueArt.Fill(renderer, rect, A(new Color(255, 7, 10, 18)));
		int num2 = (int)Math.Round((float)rect.Width * Math.Clamp(part, 0f, 1f));
		if (num2 > 0)
		{
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, num2, rect.Height), A(couleur));
		}
	}

	private void Puce(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string texte, int x, int milieu, Color couleur)
	{
		string text = texte.ToUpperInvariant();
		int w = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), text, _d.Em(0.14f, 10f)) + _d.L(14f);
		Rectangle rect = new Rectangle(x, milieu - _d.L(9f), w, _d.L(18f));
		TechniqueArt.Fill(renderer, rect, A(couleur));
		TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(10f), text, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, _d.Em(0.14f, 10f), A(new Color(255, 23, 11, 2)));
	}

	private void DessinerRecompenseCourte(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Entree entree, int droite, int milieu)
	{
		List<string> list = new List<string>();
		List<ItemDescriptor> list2 = new List<ItemDescriptor>();
		QuestDescriptor quete = entree.Quete;
		long num;
		if (quete != null)
		{
			QuestReward rewards = quete.Rewards;
			Guid monnaie = Globals.Me?.CurrencyItemId ?? Guid.Empty;
			num = QuestRewardText.ZeniOf(rewards, monnaie);
			if (rewards.Experience > 0)
			{
				list.Add(Strings.Journal.RecompenseExp.ToString(Montant(rewards.Experience)));
			}
			list2.AddRange((from item in rewards.Items
				where !QuestRewardText.IsCurrency(item.ItemId, monnaie)
				select DatabaseObject<ItemDescriptor>.Get(item.ItemId) into objet
				where objet != null
				select objet).Cast<ItemDescriptor>().Take(3));
		}
		else
		{
			RibbonContractEntryPacket ruban = entree.Ruban;
			if (ruban != null)
			{
				num = Math.Max(0L, ruban.Zeni);
				list.Add(Strings.Journal.RecompenseRep.ToString(Math.Max(0, ruban.Reputation)));
			}
			else
			{
				WantedContractEntryPacket contrat = entree.Contrat;
				num = Math.Max(0L, contrat.Zeni);
				if (contrat.Kind == WantedContractKind.Criminal)
				{
					list.Add(Strings.Journal.RecompensePrime);
				}
				else
				{
					list.Add(Strings.Journal.RecompenseExp.ToString(Montant(contrat.Experience)));
					if (contrat.Completed)
					{
						list.Add(Strings.Journal.RecompenseRep.ToString(Math.Max(0, Options.Instance.Karma.ReputationPoliceBounty)));
					}
				}
			}
		}
		if (num > 0)
		{
			int num2 = _d.L(14f);
			HouseArt.Zeni(renderer, new Rectangle(droite - num2, milieu - _d.L(9f) - num2 / 2, num2, num2), 0, _fade);
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(17f), "+" + Montant(num), droite - num2 - _d.L(5f), milieu - _d.L(9f), A(TechniqueArt.Gold));
		}
		int num3 = droite;
		int num4 = _d.L(18f);
		for (int num5 = list2.Count - 1; num5 >= 0; num5--)
		{
			IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Item, list2[num5].Icon);
			if (gameTexture != null)
			{
				num3 -= num4;
				Lisser(gameTexture, new Rectangle(num3, milieu + _d.L(11f) - num4 / 2, num4, num4), A(TechniqueArt.White));
				num3 -= _d.L(2f);
			}
		}
		if (list.Count > 0)
		{
			TechniqueArt.TextRight(renderer, TechniqueArt.Body, Pt(11.5f), string.Join(" · ", list), num3 - ((list2.Count > 0) ? _d.L(4f) : 0), milieu + _d.L(11f), A(TechniqueArt.Dim));
		}
	}

	private void DessinerFiche(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, List<Element> elements)
	{
		Panneau(renderer, _d.R(900f, 58f, 326f, 676f));
		Entree entree = elements.FirstOrDefault((Element element) => element.Entree?.Cle == _choisie).Entree ?? _entrees.FirstOrDefault((Entree autre) => autre.Cle == _choisie);
		if (entree == null)
		{
			float num = 98f;
			{
				foreach (string item in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(13f), Strings.QuestLog.PickOne, _d.L(290f), 3))
				{
					TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), item, _d.X(918f), _d.Y(num), A(TechniqueArt.Soft));
					num += 18f;
				}
				return;
			}
		}
		if (entree.Quete != null)
		{
			DessinerFicheQuete(renderer, me, entree);
		}
		else
		{
			DessinerFicheContrat(renderer, entree);
		}
	}

	private Rectangle Vitrine(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Color couleur, string puce, string filigrane, string etiquette, string valeur, string? complement)
	{
		Rectangle rectangle = _d.R(914f, 72f, 298f, 118f);
		TechniqueArt.FillVertical(renderer, rectangle, A(new Color(255, 10, 16, 28)), A(new Color(255, 13, 20, 34)));
		TechniqueArt.Glow(renderer, (float)rectangle.X + (float)rectangle.Width * 0.72f, (float)rectangle.Y + (float)rectangle.Height * 0.55f, _d.L(90f), A(couleur, 0.2f));
		TechniqueArt.Outline(renderer, rectangle, A(TechniqueArt.Line));
		if (filigrane.Length > 0)
		{
			int num = _d.L(76f);
			int num2 = rectangle.Right - _d.L(22f) - num;
			int num3 = rectangle.Y + (rectangle.Height - num) / 2 - ((filigrane != "-") ? _d.L(8f) : 0);
			Lisser("saiyan_embleme_quete.png", new Rectangle(num2, num3, num, num), A(TechniqueArt.Mix(couleur, TechniqueArt.White, 0.6f), 0.5f));
			if (filigrane != "-")
			{
				TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(22f), filigrane, num2 + num / 2, num3 + num + _d.L(8f), A(couleur, 0.75f));
			}
		}
		Puce(renderer, puce, rectangle.X + _d.L(10f), rectangle.Y + _d.L(19f), couleur);
		if (valeur.Length > 0)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(12f), etiquette, rectangle.X + _d.L(12f), rectangle.Bottom - _d.L(44f), A(KiClair));
			int num4 = TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(30f), valeur, rectangle.X + _d.L(12f), rectangle.Bottom - _d.L(20f), A(TechniqueArt.Text));
			if (!string.IsNullOrEmpty(complement))
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(18f), complement, rectangle.X + _d.L(12f) + num4 + _d.L(6f), rectangle.Bottom - _d.L(18f), A(Muted));
			}
		}
		return rectangle;
	}

	private void DessinerFicheQuete(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Player me, Entree entree)
	{
		QuestDescriptor quete = entree.Quete;
		Color couleur = CouleurDe(entree);
		QuestTaskDescriptor questTaskDescriptor;
		int num;
		int num2;
		int num3;
		if (entree.Etat == Etat.EnCours)
		{
			(questTaskDescriptor, num, num2, num3) = OuEnEst(entree);
		}
		else
		{
			questTaskDescriptor = null;
			num = 0;
			num2 = 0;
			num3 = -1;
		}
		string puce = entree.Etat switch
		{
			Etat.Terminee => Strings.QuestLog.ChipCompleted.ToString(), 
			Etat.AFaire => Strings.QuestLog.ChipNotStarted.ToString(), 
			_ => entree.Genre switch
			{
				Genre.Histoire => Strings.Journal.CatHistoire.ToString(), 
				Genre.Enseignement => Strings.Journal.ChipEnseignement.ToString(), 
				Genre.Quotidienne => Strings.Journal.ChipQuotidienne.ToString(), 
				_ => Strings.Journal.ChipAnnexe.ToString(), 
			}, 
		};
		object obj;
		if (entree.Genre == Genre.Histoire)
		{
			string text = quete.Folder?.Trim();
			if (text != null && text.Length > 0)
			{
				obj = Traduction.Contenu(text).ToUpperInvariant();
				goto IL_0147;
			}
		}
		obj = "-";
		goto IL_0147;
		IL_0147:
		string filigrane = (string)obj;
		int value = Math.Max(1, quete.Tasks.Count);
		if (entree.Etat == Etat.EnCours)
		{
			Vitrine(renderer, couleur, puce, filigrane, Strings.Journal.VitrineEtape, $"{Math.Max(0, num3) + 1}", $"/ {value}");
		}
		else
		{
			Vitrine(renderer, couleur, puce, filigrane, Strings.Journal.VitrineEtapes, (entree.Etat == Etat.Terminee) ? $"{value}" : "0", $"/ {value}");
		}
		int x = _d.X(918f);
		int num4 = _d.L(290f);
		float num5 = 216f;
		foreach (string item in TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(27f), quete.Name, num4, 2))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(27f), item, x, _d.Y(num5), A(TechniqueArt.Text));
			num5 += 28f;
		}
		num5 -= 6f;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(13f), SousTitreDe(entree), num4, 1).FirstOrDefault(), x, _d.Y(num5), A(TechniqueArt.Dim));
		num5 += 22f;
		string text2 = entree.Etat switch
		{
			Etat.Terminee => quete.EndDescription, 
			Etat.AFaire => quete.BeforeDescription, 
			_ => string.IsNullOrWhiteSpace(quete.InProgressDescription) ? quete.StartDescription : quete.InProgressDescription, 
		};
		bool flag = quete.Tasks.Count >= 5;
		foreach (string item2 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(13f), text2?.Trim() ?? string.Empty, num4, flag ? 2 : 3))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), item2, x, _d.Y(num5), A(TechniqueArt.Soft));
			num5 += 18f;
		}
		if (questTaskDescriptor != null)
		{
			num5 += 8f;
			num5 = Etiquette(renderer, Strings.QuestLog.Objective, num5);
			List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(14f), QuestTrackerPanel.ObjectiveOf(questTaskDescriptor), num4 - _d.L(24f), 2);
			int num6 = 10 + list.Count * 19 + ((num2 > 0) ? 18 : 0) + 6;
			Rectangle rect = _d.R(918f, num5, 290f, num6);
			TechniqueArt.Fill(renderer, rect, A(TechniqueArt.Ki, 0.07f));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Ki, 0.45f));
			TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(TechniqueArt.Ki));
			float num7 = num5 + 10f + 9f;
			foreach (string item3 in list)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), item3, rect.X + _d.L(14f), _d.Y(num7), A(TechniqueArt.Text));
				num7 += 19f;
			}
			if (num2 > 0)
			{
				string text3 = $"{num} / {num2}";
				int num8 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(17f), text3, rect.Right - _d.L(12f), _d.Y(num7 + 2f), A(TechniqueArt.Text));
				Rectangle rect2 = new Rectangle(rect.X + _d.L(14f), _d.Y(num7 + 2f) - Math.Max(2, _d.L(5f)) / 2, rect.Width - _d.L(14f) - _d.L(12f) - num8 - _d.L(10f), Math.Max(2, _d.L(5f)));
				TechniqueArt.Fill(renderer, rect2, A(new Color(255, 7, 10, 18)));
				int num9 = (int)Math.Round((double)rect2.Width * Math.Clamp((double)num / (double)num2, 0.0, 1.0));
				if (num9 > 0)
				{
					TechniqueArt.FillHorizontal(renderer, new Rectangle(rect2.X, rect2.Y, num9, rect2.Height), A(new Color(255, 184, 97, 15)), A(TechniqueArt.Ki), A(TechniqueArt.Gold));
				}
			}
			num5 += (float)(num6 + 6);
			List<string> list2 = QuestRewardText.Parts(questTaskDescriptor.Rewards);
			if (list2.Count > 0)
			{
				string text4 = Strings.QuestLog.StepReward.ToString(string.Join(" · ", list2));
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), text4, num4, 1).FirstOrDefault(), x, _d.Y(num5 + 6f), A(KiClair));
				num5 += 18f;
			}
		}
		bool flag2 = entree.Etat == Etat.EnCours && quete.Quitable;
		float num10 = ((entree.Etat == Etat.EnCours) ? (680f - (float)(flag2 ? 40 : 0)) : 688f);
		QuestReward rewards = quete.Rewards;
		List<Pastille> list3 = Pastilles(renderer, rewards);
		int num11 = ((list3.Count != 0) ? (18 + LignesDePastilles(renderer, list3, num4) * 40) : 0);
		float num12 = num10 - 12f - (float)num11 - (float)((num11 > 0) ? 8 : 0);
		num5 += 10f;
		num5 = DessinerEtapes(renderer, quete, (entree.Etat == Etat.Terminee) ? quete.Tasks.Count : ((entree.Etat == Etat.AFaire) ? (-1) : num3), num5, num12);
		if (list3.Count > 0)
		{
			float y = Math.Max(num5 + 8f, num12 + 8f);
			y = Etiquette(renderer, (entree.Etat == Etat.Terminee) ? Strings.Journal.RecompensesRecues : Strings.QuestLog.Rewards, y);
			DessinerPastilles(renderer, list3, y, num4);
		}
		if (entree.Etat == Etat.Terminee)
		{
			Note(renderer, Strings.Journal.NoteTerminee);
			return;
		}
		if (entree.Etat == Etat.AFaire)
		{
			Note(renderer, Strings.Journal.NoteAFaire);
			return;
		}
		if (flag2)
		{
			Rectangle rect3 = _d.R(914f, 640f, 298f, 32f);
			BoutonCreux(renderer, rect3, Strings.Journal.BoutonAbandonQuete, "abandon", Rouge, actif: true, null);
			AddHit(rect3, "abandon", delegate
			{
				DemanderAbandon(quete);
			});
		}
		bool flag3 = QuestMarkers.GuidedQuest() == quete.Id;
		bool flag4 = TrackedQuests.IsStory(quete.Id);
		float num13 = 145f;
		Rectangle rect4 = _d.R(914f, 680f, num13 + 18f, 40f);
		Rectangle rect5 = _d.R(914f + num13 + 18f + 8f, 680f, num13 - 18f, 40f);
		BoutonCreux(renderer, rect4, flag3 ? Strings.QuestTracker.Following : Strings.QuestTracker.Follow, "fleche", flag3 ? KiClair : null, actif: true, "losange");
		AddHit(rect4, "fleche", delegate
		{
			Flecher(quete);
		});
		if (IsHot("fleche"))
		{
			Etiqueter("fleche", rect4, (!flag3) ? Strings.QuestTracker.Follow.ToString() : ((TrackedQuests.Followed == quete.Id && !flag4) ? Strings.QuestTracker.FollowingBack.ToString() : Strings.QuestTracker.Following.ToString()));
		}
		bool flag5 = TrackedQuests.IsPinned(quete.Id);
		bool flag6 = flag5 || flag4 || !TrackedQuests.IsFull;
		BoutonCreux(renderer, rect5, flag5 ? Strings.QuestLog.TrackUnpin : Strings.QuestLog.TrackTooltipPin, "suivi", null, flag6, flag5 ? "saiyan_epingle.png" : "saiyan_epingle_vide.png");
		Guid id = quete.Id;
		AddHit(rect5, "suivi", delegate
		{
			Punaiser(id);
		});
		if ((flag4 || !flag6) && IsHot("suivi"))
		{
			Etiqueter("suivi", rect5, AidePunaise(id, flag5));
		}
	}

	private float DessinerEtapes(Intersect.Client.Framework.Gwen.Renderer.Base renderer, QuestDescriptor quete, int courante, float y, float fin)
	{
		if (quete.Tasks.Count == 0 || y + 34f > fin)
		{
			return y;
		}
		y = Etiquette(renderer, Strings.Journal.Etapes, y);
		int num = _d.X(918f);
		int width = _d.L(272f);
		for (int i = 0; i < quete.Tasks.Count; i++)
		{
			List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(13f), TexteDEtape(quete.Tasks[i]), width, 2);
			int num2 = list.Count * 17 + 5;
			int num3 = quete.Tasks.Count - i;
			if (y + (float)num2 > fin && num3 > 0)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), Strings.Journal.EtapesEnPlus.ToString(num3), num + _d.L(18f), _d.Y(y + 8f), A(Muted));
				return y + 18f;
			}
			bool flag = courante >= 0 && i < courante;
			bool flag2 = i == courante;
			int num4 = _d.L(10f);
			int centerY = _d.Y(y + 8f);
			if (flag || flag2)
			{
				DungeonHud.Diamond(renderer, num, centerY, num4, A(flag ? TechniqueArt.Green : TechniqueArt.Ki));
			}
			else
			{
				DungeonHud.Diamond(renderer, num, centerY, num4, A(TechniqueArt.Line2));
				DungeonHud.Diamond(renderer, num + Math.Max(1, _d.L(2f)), centerY, num4 - Math.Max(2, _d.L(4f)), A(new Color(255, 14, 20, 33)));
			}
			Color color = (flag ? TechniqueArt.Dim : (flag2 ? TechniqueArt.Text : Muted));
			float num5 = y + 8f;
			foreach (string item in list)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), item, num + _d.L(18f), _d.Y(num5), A(color));
				num5 += 17f;
			}
			y += (float)num2;
		}
		return y;
	}

	private void DessinerFicheContrat(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Entree entree)
	{
		Color color = CouleurDe(entree);
		WantedContractEntryPacket contrat = entree.Contrat;
		RibbonContractEntryPacket ruban = entree.Ruban;
		bool flag = contrat != null && contrat.Completed && contrat.Kind == WantedContractKind.Police;
		string puce = ((ruban != null) ? Strings.Journal.GroupeRuban.ToString() : ((contrat.Kind == WantedContractKind.Criminal) ? Strings.WantedBoard.Criminal.ToString() : (flag ? Strings.WantedBoard.ToCashIn.ToString() : Strings.WantedBoard.PoliceBounty.ToString())));
		int num = ((ruban == null) ? ((contrat.Kind == WantedContractKind.Police) ? contrat.Level : 0) : ((ruban.Kind == RibbonContractKind.HeroHead) ? ruban.Level : 0));
		Rectangle rectangle = Vitrine(renderer, color, puce, string.Empty, Strings.Journal.VitrineNiveau, (num > 0) ? $"{num}" : string.Empty, null);
		if (contrat != null && contrat.Kind == WantedContractKind.Police && DatabaseObject<NPCDescriptor>.TryGet(contrat.TargetId, out var databaseObject))
		{
			Planche(cadre: new Rectangle(rectangle.Right - _d.L(130f), rectangle.Y + _d.L(8f), _d.L(118f), rectangle.Height - _d.L(14f)), renderer: renderer, nom: databaseObject.Sprite, recadrer: false);
		}
		else
		{
			if (ruban != null && !string.IsNullOrEmpty(ruban.Face))
			{
				IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Face, ruban.Face);
				if (gameTexture != null)
				{
					int num2 = rectangle.Height - _d.L(24f);
					Lisser(gameTexture, new Rectangle(rectangle.Right - _d.L(18f) - num2, rectangle.Y + _d.L(12f), num2, num2), A(TechniqueArt.White));
					goto IL_02b0;
				}
			}
			int num3 = _d.L(56f);
			GlypheCible(renderer, new Rectangle(rectangle.Right - _d.L(40f) - num3, rectangle.Y + (rectangle.Height - num3) / 2, num3, num3), A(color, 0.85f));
		}
		goto IL_02b0;
		IL_02b0:
		int num4 = _d.X(918f);
		int num5 = _d.L(290f);
		float num6 = 216f;
		foreach (string item3 in TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(27f), TitreDe(entree), num5, 2))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(27f), item3, num4, _d.Y(num6), A(flag ? TechniqueArt.Gold : TechniqueArt.Text));
			num6 += 28f;
		}
		num6 -= 6f;
		LocalizedString localizedString = ((ruban != null) ? Strings.Journal.FicheRubanSous : ((contrat.Kind == WantedContractKind.Criminal) ? Strings.Journal.FicheCriminelSous : Strings.Journal.FichePrimeSous));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(13f), localizedString, num5, 1).FirstOrDefault(), num4, _d.Y(num6), A(TechniqueArt.Dim));
		num6 += 26f;
		string text;
		if (ruban == null)
		{
			text = ((contrat.Kind != WantedContractKind.Criminal) ? (flag ? Strings.WantedBoard.TrackerGoBack.ToString() : Strings.Journal.ObjVaincre.ToString(contrat.Name)) : Strings.Journal.ObjNeutraliser.ToString(contrat.Name));
		}
		else
		{
			RibbonContractKind kind = ruban.Kind;
			string text2 = ((kind != RibbonContractKind.Smuggling && kind != RibbonContractKind.Sabotage) ? Strings.Journal.ObjAbattre.ToString(ruban.Name) : OuDe(entree));
			text = text2;
		}
		num6 = Etiquette(renderer, Strings.QuestLog.Objective, num6);
		List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(14f), text, num5 - _d.L(24f), 2);
		bool flag2 = contrat != null && contrat.Kind == WantedContractKind.Police && !flag;
		int num7 = 10 + list.Count * 19 + (flag2 ? 18 : 0) + 6;
		Rectangle rect = _d.R(918f, num6, 290f, num7);
		Color color2 = (flag ? TechniqueArt.Green : TechniqueArt.Ki);
		TechniqueArt.Fill(renderer, rect, A(color2, 0.07f));
		TechniqueArt.Outline(renderer, rect, A(color2, 0.45f));
		TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(color2));
		float num8 = num6 + 10f + 9f;
		foreach (string item4 in list)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), item4, rect.X + _d.L(14f), _d.Y(num8), A(TechniqueArt.Text));
			num8 += 19f;
		}
		if (flag2)
		{
			int num9 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(17f), "0 / 1", rect.Right - _d.L(12f), _d.Y(num8 + 2f), A(TechniqueArt.Text));
			Rectangle rect2 = new Rectangle(rect.X + _d.L(14f), _d.Y(num8 + 2f) - Math.Max(2, _d.L(5f)) / 2, rect.Width - _d.L(14f) - _d.L(12f) - num9 - _d.L(10f), Math.Max(2, _d.L(5f)));
			TechniqueArt.Fill(renderer, rect2, A(new Color(255, 7, 10, 18)));
		}
		num6 += (float)(num7 + 12);
		num6 = Etiquette(renderer, Strings.Journal.LeContrat, num6);
		List<(string, string)> list2 = new List<(string, string)>();
		if (ruban != null)
		{
			string text3 = WantedPlaces.ZoneOf(ruban.MapId);
			list2.Add((Strings.Journal.RegleOu, (text3.Length > 0) ? text3 : (ruban.Place?.Trim() ?? Strings.WantedBoard.UnknownPlace.ToString())));
			list2.Add((Strings.Journal.ReglePaye, Strings.RubanRouge.TrackerPay.ToString(Montant(Math.Max(0L, ruban.Zeni)), Math.Max(0, ruban.Reputation))));
			list2.Add((Strings.Journal.RegleJusqua, Strings.Journal.RegleRubanMinuit));
		}
		else if (contrat.Kind == WantedContractKind.Criminal)
		{
			list2.Add((Strings.Journal.RegleVu, OuDe(entree)));
			list2.Add((Strings.WantedBoard.Online, Presence(contrat)));
			list2.Add((Strings.Journal.ReglePrime, Strings.Journal.RegleCriminelPrime.ToString(Montant(Math.Max(0L, contrat.Zeni)))));
		}
		else if (flag)
		{
			list2.Add((Strings.Journal.RegleRepere, LieuDePrime(contrat)));
			list2.Add((Strings.Journal.RegleEncaisser, Strings.Journal.RegleEncaisserTexte));
		}
		else
		{
			int num10 = _entrees.Count((Entree autre) => autre.Contrat != null);
			list2.Add((Strings.Journal.RegleRepere, LieuDePrime(contrat)));
			list2.Add((Strings.Journal.ReglePrime, Strings.Journal.ReglePrimeTexte.ToString(Math.Max(0, Options.Instance.Karma.PoliceBountyMinSharePercent))));
			list2.Add((Strings.Journal.RegleJusqua, Strings.Journal.RegleMinuit.ToString(num10, Math.Max(1, Options.Instance.Karma.WantedMaxContracts))));
		}
		int num11 = _d.L(84f);
		int width = num5 - num11;
		foreach (var item5 in list2)
		{
			string item = item5.Item1;
			string item2 = item5.Item2;
			List<string> list3 = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(13f), item2, width, 4);
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), item.ToUpperInvariant(), num4, _d.Y(num6 + 8f), _d.Em(0.14f, 10f), A(Muted));
			float num12 = num6 + 8f;
			foreach (string item6 in list3)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), item6, num4 + num11, _d.Y(num12), A(TechniqueArt.Soft));
				num12 += 17f;
			}
			num6 += (float)(Math.Max(1, list3.Count) * 17 + 5);
		}
		QuestReward questReward = new QuestReward();
		if (ruban != null)
		{
			questReward.Zeni = Math.Max(0L, ruban.Zeni);
		}
		else
		{
			questReward.Zeni = Math.Max(0L, contrat.Zeni);
			questReward.Experience = ((contrat.Kind == WantedContractKind.Police) ? Math.Max(0L, contrat.Experience) : 0);
		}
		List<Pastille> list4 = Pastilles(renderer, questReward);
		if (ruban != null && ruban.Reputation > 0)
		{
			list4.Add(new Pastille(null, Strings.Journal.RecompenseRep.ToString(ruban.Reputation), string.Empty, Brun, null));
		}
		else if (flag && Options.Instance.Karma.ReputationPoliceBounty > 0)
		{
			list4.Add(new Pastille(null, Strings.Journal.RecompenseRep.ToString(Options.Instance.Karma.ReputationPoliceBounty), string.Empty, BleuPolice, null));
		}
		if (list4.Count > 0)
		{
			num6 += 8f;
			num6 = Etiquette(renderer, Strings.QuestLog.Rewards, num6);
			DessinerPastilles(renderer, list4, num6, num5);
		}
		if (flag)
		{
			Note(renderer, Strings.Journal.NoteRemplie);
			return;
		}
		Rectangle rect3 = _d.R(914f, 680f, 298f, 40f);
		BoutonCreux(renderer, rect3, Strings.Journal.BoutonAbandonContrat, "abandon-contrat", Rouge, actif: true, null);
		AddHit(rect3, "abandon-contrat", delegate
		{
			DemanderAbandon(entree);
		});
	}

	private float Etiquette(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string texte, float y)
	{
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), texte.ToUpperInvariant(), _d.X(918f), _d.Y(y + 7f), _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
		return y + 18f;
	}

	private void Note(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string texte)
	{
		List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), texte, _d.L(290f), 3);
		float num = 718f - (float)list.Count * 17f;
		foreach (string item in list)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), item, _d.X(918f), _d.Y(num + 8f), A(Muted));
			num += 17f;
		}
	}

	private void BoutonCreux(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, string texte, string cle, Color? couleur, bool actif, string? picto)
	{
		bool flag = actif && IsHot(cle);
		TechniqueArt.Fill(renderer, rect, A(HouseArt.ButtonBack));
		TechniqueArt.Fill(renderer, rect, A(couleur ?? TechniqueArt.White, flag ? 0.14f : 0.06f));
		TechniqueArt.Outline(renderer, rect, A(couleur ?? (flag ? TechniqueArt.Soft : TechniqueArt.Line2), actif ? 1f : 0.5f));
		Color color = A((!actif) ? Eteint : (couleur ?? (flag ? TechniqueArt.Text : TechniqueArt.Soft)));
		int num = Pt(13f);
		int num2 = ((picto != null) ? (_d.L(13f) + _d.L(8f)) : 0);
		while (num > 9 && TechniqueArt.Measure(renderer, TechniqueArt.Label, num, texte).X + num2 > rect.Width - _d.L(14f))
		{
			num--;
		}
		int x = TechniqueArt.Measure(renderer, TechniqueArt.Label, num, texte).X;
		int num3 = rect.X + (rect.Width - x - num2) / 2;
		int num4 = rect.Y + rect.Height / 2;
		if (picto == "losange")
		{
			DungeonHud.Diamond(renderer, num3, num4, _d.L(12f), color);
		}
		else if (picto != null)
		{
			int num5 = _d.L(13f);
			Lisser(picto, new Rectangle(num3, num4 - num5 / 2, num5, num5), color);
		}
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, num, texte, num3 + num2, num4, color);
	}

	private static List<Pastille> Pastilles(Intersect.Client.Framework.Gwen.Renderer.Base renderer, QuestReward recompenses)
	{
		List<Pastille> list = new List<Pastille>();
		Guid currencyId = Globals.Me?.CurrencyItemId ?? Guid.Empty;
		long num = QuestRewardText.ZeniOf(recompenses, currencyId);
		if (num > 0)
		{
			list.Add(new Pastille(null, "+" + Montant(num), string.Empty, TechniqueArt.Gold, null, Piece: true));
		}
		if (recompenses.Experience > 0)
		{
			list.Add(new Pastille(null, "+" + Montant(recompenses.Experience), "exp", TechniqueArt.Cyan, null));
		}
		foreach (QuestRewardItem item in recompenses.Items)
		{
			if (!QuestRewardText.IsCurrency(item.ItemId, currencyId))
			{
				ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(item.ItemId);
				if (itemDescriptor != null)
				{
					list.Add(new Pastille(Globals.ContentManager?.GetTexture(TextureType.Item, itemDescriptor.Icon), itemDescriptor.Name, "×" + Montant(item.Quantity), TechniqueArt.Text, itemDescriptor));
				}
			}
		}
		foreach (Guid spell in recompenses.Spells)
		{
			if (DatabaseObject<SpellDescriptor>.TryGet(spell, out var databaseObject))
			{
				list.Add(new Pastille(Globals.ContentManager?.GetTexture(TextureType.Spell, databaseObject.Icon), databaseObject.Name, string.Empty, TechniqueArt.Text, null));
			}
		}
		return list;
	}

	private int LargeurPastille(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Pastille pastille)
	{
		int num = _d.L(8f) + TechniqueArt.Measure(renderer, TechniqueArt.Heavy, Pt(16f), pastille.Valeur).X + _d.L(10f);
		if (pastille.Icone != null || pastille.Piece)
		{
			num += _d.L(20f) + _d.L(7f);
		}
		if (pastille.Unite.Length > 0)
		{
			num += _d.L(6f) + TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), pastille.Unite.ToUpperInvariant(), _d.Em(0.14f, 10f));
		}
		return num;
	}

	private int LignesDePastilles(Intersect.Client.Framework.Gwen.Renderer.Base renderer, List<Pastille> pastilles, int largeur)
	{
		int num = 1;
		int num2 = 0;
		foreach (Pastille pastille in pastilles)
		{
			int num3 = Math.Min(largeur, LargeurPastille(renderer, pastille));
			if (num2 > 0 && num2 + num3 > largeur)
			{
				num++;
				num2 = 0;
			}
			num2 += num3 + _d.L(6f);
		}
		return num;
	}

	private void DessinerPastilles(Intersect.Client.Framework.Gwen.Renderer.Base renderer, List<Pastille> pastilles, float y, int largeur)
	{
		int num = _d.X(918f);
		int num2 = 0;
		int num3 = _d.Y(y);
		int num4 = _d.L(34f);
		foreach (Pastille pastille in pastilles)
		{
			int num5 = Math.Min(largeur, LargeurPastille(renderer, pastille));
			if (num2 > 0 && num2 + num5 > largeur)
			{
				num2 = 0;
				num3 += num4 + _d.L(6f);
			}
			Rectangle rect = new Rectangle(num + num2, num3, num5, num4);
			TechniqueArt.Fill(renderer, rect, A(Case));
			TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line2));
			int num6 = rect.X + _d.L(8f);
			int num7 = rect.Y + rect.Height / 2;
			if (pastille.Piece)
			{
				int num8 = _d.L(20f);
				HouseArt.Zeni(renderer, new Rectangle(num6, num7 - num8 / 2, num8, num8), 0, _fade);
				num6 += num8 + _d.L(7f);
			}
			else
			{
				IGameTexture icone = pastille.Icone;
				if (icone != null)
				{
					int num9 = _d.L(20f);
					Lisser(icone, new Rectangle(num6, num7 - num9 / 2, num9, num9), A(TechniqueArt.White));
					num6 += num9 + _d.L(7f);
				}
			}
			int val = rect.Right - _d.L(8f) - num6 - ((pastille.Unite.Length > 0) ? (TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(10f), pastille.Unite.ToUpperInvariant(), _d.Em(0.14f, 10f)) + _d.L(6f)) : 0);
			num6 += TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(16f), TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(16f), pastille.Valeur, Math.Max(_d.L(20f), val), 1).FirstOrDefault(), num6, num7, A(pastille.Couleur));
			if (pastille.Unite.Length > 0)
			{
				TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(10f), pastille.Unite.ToUpperInvariant(), num6 + _d.L(6f), num7 + _d.L(1f), _d.Em(0.14f, 10f), A(Muted));
			}
			ItemDescriptor objet = pastille.Objet;
			if (objet != null)
			{
				BulleSur(rect, $"objet:{objet.Id}", () => BulleObjet(objet));
			}
			num2 += num5 + _d.L(6f);
		}
	}

	private static Bulle BulleObjet(ItemDescriptor objet)
	{
		int num = JetDObjet.Rarete(objet, null);
		Color color = PersonnageScreen.CouleurRarete(num);
		return new Bulle
		{
			Icone = Globals.ContentManager.GetTexture(TextureType.Item, objet.Icon),
			Teinte = objet.Color,
			Nom = objet.Name,
			Couleur = color,
			CouleurNom = TechniqueArt.Mix(color, TechniqueArt.White, 0.55f),
			Sorte = ((num > 0) ? (FenetreInventaire.SorteDe(objet) + " · " + PersonnageScreen.NomRarete(num)) : FenetreInventaire.SorteDe(objet)),
			Description = (string.IsNullOrWhiteSpace(objet.Description) ? null : objet.Description.Trim())
		};
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

	private void GlypheLosange(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle carre, Color couleur, bool plein)
	{
		int num = carre.Width - 2;
		int num2 = carre.X + 1;
		int centerY = carre.Y + carre.Height / 2;
		DungeonHud.Diamond(renderer, num2, centerY, num, couleur);
		if (!plein)
		{
			int num3 = Math.Max(2, _d.L(3f));
			DungeonHud.Diamond(renderer, num2 + num3, centerY, num - num3 * 2, A(new Color(255, 14, 20, 33)));
		}
	}

	private void GlypheCible(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle carre, Color couleur)
	{
		int num = Math.Max(1, _d.L(2f));
		int num2 = carre.X + carre.Width / 2;
		int num3 = carre.Y + carre.Height / 2;
		Rectangle rect = new Rectangle(carre.X + carre.Width / 6, carre.Y + carre.Height / 6, carre.Width * 2 / 3, carre.Height * 2 / 3);
		TechniqueArt.Outline(renderer, rect, couleur, num);
		int num4 = Math.Max(2, carre.Width / 6);
		TechniqueArt.Fill(renderer, new Rectangle(num2 - num4 / 2, num3 - num4 / 2, num4, num4), couleur);
		TechniqueArt.Fill(renderer, new Rectangle(num2 - num / 2, carre.Y, num, carre.Height / 6 + num), couleur);
		TechniqueArt.Fill(renderer, new Rectangle(num2 - num / 2, carre.Bottom - carre.Height / 6 - num, num, carre.Height / 6 + num), couleur);
		TechniqueArt.Fill(renderer, new Rectangle(carre.X, num3 - num / 2, carre.Width / 6 + num, num), couleur);
		TechniqueArt.Fill(renderer, new Rectangle(carre.Right - carre.Width / 6 - num, num3 - num / 2, carre.Width / 6 + num, num), couleur);
	}

	private static void GlypheOeil(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle carre, Color couleur)
	{
		float num = (float)carre.Height * 0.62f;
		float num2 = carre.Width;
		float x = (float)carre.Y + ((float)carre.Height - num) / 2f;
		float num3 = (float)carre.X + num2 / 2f;
		int num4 = (int)MathF.Ceiling(num);
		float num5 = num * 0.36f;
		for (int i = 0; i < num4; i++)
		{
			float num6 = ((float)i + 0.5f) / num * 2f - 1f;
			float num7 = num2 / 2f * MathF.Sqrt(Math.Max(0f, 1f - num6 * num6)) * 0.98f;
			int y = (int)MathF.Round(x) + i;
			float num8 = Math.Max(0f, num7 - 1.6f);
			if (i == 0 || i == num4 - 1 || num8 <= 0.5f)
			{
				TechniqueArt.Fill(renderer, new Rectangle((int)MathF.Round(num3 - num7), y, Math.Max(1, (int)MathF.Round(num7 * 2f)), 1), couleur);
				continue;
			}
			TechniqueArt.Fill(renderer, new Rectangle((int)MathF.Round(num3 - num7), y, Math.Max(1, (int)MathF.Round(num7 - num8)), 1), couleur);
			TechniqueArt.Fill(renderer, new Rectangle((int)MathF.Round(num3 + num8), y, Math.Max(1, (int)MathF.Round(num7 - num8)), 1), couleur);
			float num9 = (float)i + 0.5f - num / 2f;
			if (Math.Abs(num9) < num5)
			{
				float num10 = MathF.Sqrt(num5 * num5 - num9 * num9);
				TechniqueArt.Fill(renderer, new Rectangle((int)MathF.Round(num3 - num10), y, Math.Max(1, (int)MathF.Round(num10 * 2f)), 1), couleur);
			}
		}
	}
}
