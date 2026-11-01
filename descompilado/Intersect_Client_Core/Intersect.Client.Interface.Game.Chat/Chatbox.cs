using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Intersect.Client.Core;
using Intersect.Client.Core.Controls;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Input;
using Intersect.Client.Framework.Items;
using Intersect.Client.Framework.Maps;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Hotbar;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Client.Networking;
using Intersect.Configuration;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Dungeons;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Framework.Core.GameObjects.WorldMap;
using Intersect.Localization;
using Intersect.Models;
using Intersect.Network.Packets.Server;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Interface.Game.Chat;

public class Chatbox
{
	private sealed class Onglet
	{
		public required Button Bouton;

		public required ImagePanel Pastille;

		public required Intersect.Client.Framework.Gwen.Control.Label Compte;

		public string Nom = string.Empty;
	}

	private sealed class LigneDeTchat
	{
		public required ListBoxRow Ligne;

		public required ImagePanel Filet;

		public ImagePanel? Voile;

		public required ChatboxMsg Message;

		public ChatboxMsg? Precedent;

		public bool Suite;

		public int Revision;

		public ImagePanel? Pastille;

		public Intersect.Client.Framework.Gwen.Control.Label? Code;

		public ImagePanel? Losange;

		public Intersect.Client.Framework.Gwen.Control.Label? Marque;

		public int LargeurMarque;

		public bool AvecLosange;

		public int Raccourcies;

		public int EtatRaccourci;
	}

	private const int LargeurPleine = 560;

	private const int LargeurEtroite = 420;

	private static int Largeur = 560;

	private const int Hauteur = 260;

	private const int BandeauHaut = 28;

	private const int BandeauBas = 34;

	private const int Marge = 8;

	private const int CanalCommerce = 6;

	private const int CanalAdmin = 4;

	private const int OpaciteRepos = 70;

	private const long ReposApresMs = 6000L;

	private const long FonduMs = 400L;

	private const long RappelDebutMs = 20000L;

	private const long RappelSurvolMs = 1500L;

	private const int BandeRappel = 46;

	private const int LignesRecentes = 5;

	private const long RecenteVisibleMs = 9000L;

	private const long RecenteFonduMs = 1500L;

	private const long GraceMenuMs = 700L;

	private static readonly Color Blanc = new Color(255, 233, 237, 246);

	private static readonly Color Gris = new Color(255, 150, 160, 182);

	private static readonly Color Nuit = new Color(255, 18, 21, 29);

	private static readonly Color Orange = new Color(255, 255, 172, 82);

	private static readonly Color GrisRappel = new Color(255, 164, 174, 194);

	private static readonly Color GrisHeure = new Color(255, 118, 128, 148);

	private readonly GameInterface _jeu;

	private readonly ImagePanel _fenetre;

	private readonly ListBox _messages;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _replie;

	private readonly Button _epingleBouton;

	private readonly Button _effacerBouton;

	private readonly Button _replierBouton;

	private readonly Button _canalBouton;

	private readonly ImagePanel _canalMenu;

	private readonly List<Button> _canalItems = new List<Button>();

	private readonly TextBox _saisie;

	private readonly Button _envoyerBouton;

	private readonly ImagePanel _aide;

	private readonly List<(Button Bouton, string Commande)> _aideItems = new List<(Button, string)>();

	private readonly Button _nouveauxBouton;

	private readonly Intersect.Client.Framework.Gwen.Control.Label[] _recentsTextes = new Intersect.Client.Framework.Gwen.Control.Label[5];

	private readonly Intersect.Client.Framework.Gwen.Control.Label[] _recentsHeures = new Intersect.Client.Framework.Gwen.Control.Label[5];

	private readonly ImagePanel[] _recentsFonds = new ImagePanel[5];

	private readonly ImagePanel _voile;

	private readonly ImagePanel _toucheParler;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _toucheParlerNom;

	private readonly ImagePanel _toucheCanal;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _toucheCanalNom;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _replieCanal;

	private readonly List<(ChatboxMsg Message, long Arrivee)> _recents = new List<(ChatboxMsg, long)>();

	private readonly Dictionary<ChatboxTab, Onglet> _onglets = new Dictionary<ChatboxTab, Onglet>();

	private readonly Dictionary<ChatboxTab, int> _enAttente = new Dictionary<ChatboxTab, int>();

	private readonly List<LigneDeTchat> _lignes = new List<LigneDeTchat>();

	private int _classes;

	private bool _commerceVu;

	private List<ChatboxMsg>? _messagesCache;

	private ChatboxTab _messagesCacheOnglet = ChatboxTab.Count;

	private long _messagesCacheA = -1L;

	private readonly IGameTexture? _fondTexture;

	private readonly IFont? _police;

	private const int Corps = 10;

	private ChatboxTab _ongletCourant;

	private ChatboxTab _ongletDessine;

	private List<ChatboxTab> _ongletsVisibles = new List<ChatboxTab>();

	private int _canal;

	private int _messageSuivant;

	private bool _aRecu;

	private bool _deploye = true;

	private bool _epingle;

	private long _deployeA;

	private long _envoyeA = -4611686018427387904L;

	private long _derniereActivite = Timing.Global.Milliseconds;

	private long _dernierTick = Timing.Global.Milliseconds;

	private int _opacite = 255;

	private int _vus = -1;

	private long _menuLoinDepuis = -1L;

	private int _recentsVus = -1;

	private ChatboxTab _recentsOnglet;

	private bool _replierPret;

	private long _rappelJusqua;

	private float _rappelOpacite = 1f;

	private bool _recentsRanges;

	private long _dernierEnvoi = -1L;

	private string _saisieVue = string.Empty;

	private bool _tabTenue;

	private int _nouveaux;

	private readonly Intersect.Client.Framework.Gwen.Control.Menu _menuContextuel;

	private readonly MenuItem _mpItem;

	private readonly MenuItem _amiItem;

	private readonly MenuItem _groupeItem;

	private readonly MenuItem _clanItem;

	private static readonly Color TeinteLocal = new Color(255, 196, 204, 220);

	private static readonly Color TeinteGlobal = new Color(255, 111, 211, 255);

	private static readonly Color TeinteGroupe = new Color(255, 176, 150, 255);

	private static readonly Color TeinteClan = new Color(255, 255, 208, 106);

	private static readonly Color TeinteMonde = new Color(255, 255, 176, 140);

	private static readonly Color OrMarque = new Color(255, 255, 208, 106);

	private static readonly Color EncrePastille = new Color(255, 227, 232, 244);

	private const int RetraitTexte = 12;

	private const int LargeurPastille = 22;

	private const int HauteurPastille = 13;

	private const int TailleMarque = 9;

	private const int LargeurLosange = 7;

	private const int ImagesDeRecalage = 4;

	private readonly ImagePanel _globalPastille;

	private readonly Intersect.Client.Framework.Gwen.Control.Label _globalCode;

	private readonly ImagePanel[] _recentsPastilles = new ImagePanel[5];

	private readonly Intersect.Client.Framework.Gwen.Control.Label[] _recentsCodes = new Intersect.Client.Framework.Gwen.Control.Label[5];

	private readonly ImagePanel[] _recentsLosanges = new ImagePanel[5];

	private readonly Intersect.Client.Framework.Gwen.Control.Label[] _recentsMarques = new Intersect.Client.Framework.Gwen.Control.Label[5];

	private int _hauteurDeLigne;

	private int _changementsVus = ChatboxMsg.Changements;

	private LigneDeTchat? _ancre;

	private int _ancreEcran;

	private bool _ancreEnBas;

	private int _ancreImages;

	private bool _ancreTenue;

	private float _ancreMontant = float.NaN;

	private const int LignePhrase = 22;

	private const int PhrasesVisibles = 4;

	private const int NomsVisibles = 6;

	private const int EntetePhrases = 34;

	private static readonly (FamilleDePhrase Famille, Func<string> Nom)[] Familles = new(FamilleDePhrase, Func<string>)[4]
	{
		(FamilleDePhrase.Groupe, () => Strings.Tchat.FamilleGroupe.ToString()),
		(FamilleDePhrase.Aide, () => Strings.Tchat.FamilleAide.ToString()),
		(FamilleDePhrase.Commerce, () => Strings.Tchat.FamilleCommerce.ToString()),
		(FamilleDePhrase.Politesse, () => Strings.Tchat.FamillePolitesse.ToString())
	};

	private static readonly Dictionary<TrouDePhrase, string[]> NomsDeRepli = new Dictionary<TrouDePhrase, string[]>
	{
		[TrouDePhrase.Lieu] = new string[3] { "Hercule City", "Kame House", "Grotte du Dragon" },
		[TrouDePhrase.Pnj] = new string[2] { "Piccolo", "Loup" },
		[TrouDePhrase.Objet] = new string[3] { "Haricot magique", "Minerai de cuivre", "Noix de coco" }
	};

	private Button _phrasesBouton;

	private ImagePanel _phrases;

	private ImagePanel _phrasesFilet;

	private readonly List<(Button Bouton, FamilleDePhrase Famille)> _famillesBoutons = new List<(Button, FamilleDePhrase)>();

	private readonly List<Button> _phrasesItems = new List<Button>();

	private Button _phrasesRetour;

	private Intersect.Client.Framework.Gwen.Control.Label _phrasesTitre;

	private ScrollControl _nomsListe;

	private readonly List<Button> _nomsItems = new List<Button>();

	private FamilleDePhrase _famille;

	private PhraseDeTchat? _phraseATrou;

	private List<(string Affiche, string Cle)> _noms = new List<(string, string)>();

	internal Intersect.Client.Framework.Gwen.Control.Base Fenetre => _fenetre;

	public bool HasFocus => _saisie.HasFocus;

	private int HauteurDeLigne
	{
		get
		{
			if (_hauteurDeLigne <= 0)
			{
				_hauteurDeLigne = Math.Max(13, Mesurer("Ag", 10).Hauteur);
			}
			return _hauteurDeLigne;
		}
	}

	private bool PhrasesOuvertes => !_phrases.IsHidden;

	public Chatbox(Canvas canvas, GameInterface jeu)
	{
		_jeu = jeu;
		_police = GameContentManager.Current?.GetFont("chakrapetch");
		_fenetre = new ImagePanel(canvas, "ChatboxWindow")
		{
			TextureFilename = "chat_fond.png",
			TextureNinePatchMargin = new Margin(8, 28, 8, 34),
			ShouldCacheToTexture = false,
			RestrictToParent = true
		};
		_fenetre.SetSize(Largeur, 260);
		_fondTexture = _fenetre.Texture;
		ChatboxTab[] values = Enum.GetValues<ChatboxTab>();
		foreach (ChatboxTab chatboxTab in values)
		{
			if (chatboxTab != ChatboxTab.Count)
			{
				LocalizedString value;
				Button button = new Button(_fenetre, $"{chatboxTab}TabButton")
				{
					UsesHeadingFace = false,
					Font = _police,
					FontSize = 10,
					Text = ((chatboxTab == ChatboxTab.World) ? Strings.Tchat.Monde : (Strings.Chatbox.ChatTabButtons.TryGetValue(chatboxTab, out value) ? value : Strings.General.None)),
					UserData = chatboxTab,
					IsHidden = true
				};
				button.SetStateTexture(ComponentState.Normal, "hud_transparent.png");
				button.SetStateTexture(ComponentState.Hovered, "chat_onglet_hover.png");
				button.SetStateTexture(ComponentState.Active, "chat_onglet_hover.png");
				button.SetStateTexture(ComponentState.Disabled, "chat_onglet_actif.png");
				button.SetTextColor(Gris, ComponentState.Normal);
				button.SetTextColor(Blanc, ComponentState.Hovered);
				button.SetTextColor(Nuit, ComponentState.Disabled);
				button.Clicked += delegate(Intersect.Client.Framework.Gwen.Control.Base sender, MouseButtonState _)
				{
					ChoisirOnglet((ChatboxTab)sender.UserData);
				};
				ImagePanel pastille = new ImagePanel(_fenetre, $"{chatboxTab}TabBadge")
				{
					MouseInputEnabled = false,
					IsHidden = true,
					TextureFilename = "chat_pastille.png",
					TextureNinePatchMargin = new Margin(7)
				};
				Intersect.Client.Framework.Gwen.Control.Label compte = new Intersect.Client.Framework.Gwen.Control.Label(_fenetre, $"{chatboxTab}TabCount")
				{
					MouseInputEnabled = false,
					IsHidden = true,
					AutoSizeToContents = false,
					Font = _police,
					FontSize = 9,
					TextColorOverride = Nuit,
					TextAlign = Pos.Center
				};
				_onglets[chatboxTab] = new Onglet
				{
					Bouton = button,
					Pastille = pastille,
					Compte = compte,
					Nom = (button.Text ?? string.Empty)
				};
				_enAttente[chatboxTab] = 0;
			}
		}
		(_globalPastille, _globalCode) = Pastille(_fenetre, "GlobalTabLanguage", Langue.Actuelle);
		_globalPastille.IsHidden = true;
		_globalCode.IsHidden = true;
		_epingleBouton = Icone("ChatboxPinButton", "chat_icone_epingle", Strings.Chatbox.ToggleLogButtonToolTip);
		_epingleBouton.Clicked += delegate
		{
			BasculerEpingle();
		};
		_effacerBouton = Icone("ChatboxClearLogButton", "chat_icone_effacer", Strings.Chatbox.ClearLogButtonToolTip);
		_effacerBouton.Clicked += delegate
		{
			EffacerJournal();
		};
		_replierBouton = Icone("ChatboxFoldButton", "chat_icone_replier", Strings.Chatbox.Fold);
		_replierBouton.Clicked += delegate
		{
			if (_epingle)
			{
				BasculerEpingle();
			}
			Replier();
		};
		_messages = new ListBox(_fenetre, "ChatMessages")
		{
			ShouldDrawBackground = false
		};
		_messages.EnableScroll(horizontal: false, vertical: true);
		_messages.SetBounds(4, 30, Largeur - 16 + 8, 192);
		_nouveauxBouton = new Button(_fenetre, "ChatboxNewMessages")
		{
			UsesHeadingFace = false,
			Font = _police,
			FontSize = 10,
			IsHidden = true
		};
		_nouveauxBouton.SetStateTexture(ComponentState.Normal, "chat_nouveaux.png");
		_nouveauxBouton.SetStateTexture(ComponentState.Hovered, "chat_nouveaux.png");
		_nouveauxBouton.SetStateTexture(ComponentState.Active, "chat_nouveaux.png");
		_nouveauxBouton.SetTextColor(Nuit, ComponentState.Normal);
		_nouveauxBouton.SetTextColor(Nuit, ComponentState.Hovered);
		_nouveauxBouton.Clicked += delegate
		{
			DescendreEnBas();
		};
		_canalBouton = new Button(_fenetre, "ChatChannelButton")
		{
			UsesHeadingFace = false,
			Font = _police,
			FontSize = 10,
			TextAlign = Pos.Center
		};
		_canalBouton.SetStateTexture(ComponentState.Normal, "chat_canal.png");
		_canalBouton.SetStateTexture(ComponentState.Hovered, "chat_canal_hover.png");
		_canalBouton.SetStateTexture(ComponentState.Active, "chat_canal_hover.png");
		_canalBouton.Clicked += delegate
		{
			BasculerMenuCanal();
		};
		_canalMenu = new ImagePanel(_fenetre, "ChatChannelMenu")
		{
			IsHidden = true,
			TextureFilename = "chat_menu.png",
			TextureNinePatchMargin = new Margin(6)
		};
		foreach (var item5 in Canaux())
		{
			string item = item5.Nom;
			int item2 = item5.Valeur;
			Button button2 = new Button(_canalMenu, $"ChatChannel{item2}")
			{
				UsesHeadingFace = false,
				Font = _police,
				FontSize = 10,
				TextAlign = (Pos.Left | Pos.CenterV),
				Padding = new Padding(10, 0, 4, 0),
				Text = item,
				UserData = item2
			};
			button2.SetStateTexture(ComponentState.Normal, "hud_transparent.png");
			button2.SetStateTexture(ComponentState.Hovered, "chat_menu_hover.png");
			button2.SetStateTexture(ComponentState.Active, "chat_menu_hover.png");
			button2.Clicked += delegate(Intersect.Client.Framework.Gwen.Control.Base sender, MouseButtonState _)
			{
				ChoisirCanal((int)sender.UserData);
				_canalMenu.IsHidden = true;
			};
			_canalItems.Add(button2);
		}
		BatirPhrases();
		_saisie = new TextBox(_fenetre, "ChatboxInputField")
		{
			Font = _police,
			FontSize = 10,
			IsTabable = false,
			TextColorOverride = Blanc
		};
		_saisie.SetMaxLength(Options.Instance.Chat.MaxChatLength);
		_saisie.SubmitPressed += delegate
		{
			EnvoyerMessage();
		};
		_saisie.Clicked += delegate
		{
			Deployer();
		};
		Interface.FocusComponents.Add(_saisie);
		_envoyerBouton = new Button(_fenetre, "ChatboxSendButton")
		{
			UsesHeadingFace = false,
			Font = _police,
			FontSize = 10,
			Text = Strings.Chatbox.Send
		};
		_envoyerBouton.SetStateTexture(ComponentState.Normal, "chat_canal.png");
		_envoyerBouton.SetStateTexture(ComponentState.Hovered, "chat_canal_hover.png");
		_envoyerBouton.SetStateTexture(ComponentState.Active, "chat_canal_hover.png");
		_envoyerBouton.SetTextColor(Orange, ComponentState.Normal);
		_envoyerBouton.SetTextColor(Blanc, ComponentState.Hovered);
		_envoyerBouton.Clicked += delegate
		{
			EnvoyerMessage();
		};
		_aide = new ImagePanel(_fenetre, "ChatboxCommands")
		{
			IsHidden = true,
			TextureFilename = "chat_menu.png",
			TextureNinePatchMargin = new Margin(6)
		};
		foreach (var item6 in Commandes())
		{
			string item3 = item6.Commande;
			string item4 = item6.Effet;
			Button button3 = new Button(_aide, "ChatCommand" + item3)
			{
				UsesHeadingFace = false,
				Font = _police,
				FontSize = 10,
				TextAlign = (Pos.Left | Pos.CenterV),
				Padding = new Padding(10, 0, 4, 0),
				Text = item3 + "   " + item4,
				UserData = item3
			};
			button3.SetStateTexture(ComponentState.Normal, "hud_transparent.png");
			button3.SetStateTexture(ComponentState.Hovered, "chat_menu_hover.png");
			button3.SetStateTexture(ComponentState.Active, "chat_menu_hover.png");
			button3.SetTextColor(Gris, ComponentState.Normal);
			button3.SetTextColor(Blanc, ComponentState.Hovered);
			button3.Clicked += delegate(Intersect.Client.Framework.Gwen.Control.Base sender, MouseButtonState _)
			{
				SetChatboxText((string)sender.UserData + " ");
			};
			_aideItems.Add((button3, item3));
		}
		_voile = new ImagePanel(_fenetre, "ChatboxFoldedVeil")
		{
			MouseInputEnabled = false,
			IsHidden = true,
			TextureFilename = "chat_voile.png"
		};
		for (int num = 0; num < 5; num++)
		{
			_recentsFonds[num] = new ImagePanel(_fenetre, $"RecentBar{num}")
			{
				MouseInputEnabled = false,
				IsHidden = true,
				TextureFilename = "chat_pixel.png"
			};
			_recentsHeures[num] = new Intersect.Client.Framework.Gwen.Control.Label(_fenetre, $"RecentHour{num}")
			{
				MouseInputEnabled = false,
				IsHidden = true,
				AutoSizeToContents = true,
				Font = _police,
				FontSize = 10
			};
			_recentsTextes[num] = new Intersect.Client.Framework.Gwen.Control.Label(_fenetre, $"RecentLine{num}")
			{
				MouseInputEnabled = false,
				IsHidden = true,
				AutoSizeToContents = true,
				Font = _police,
				FontSize = 10
			};
			ImagePanel[] recentsPastilles = _recentsPastilles;
			int num2 = num;
			Intersect.Client.Framework.Gwen.Control.Label[] recentsCodes = _recentsCodes;
			int num3 = num;
			(ImagePanel, Intersect.Client.Framework.Gwen.Control.Label) tuple2 = Pastille(_fenetre, $"RecentLanguage{num}", string.Empty);
			recentsPastilles[num2] = tuple2.Item1;
			recentsCodes[num3] = tuple2.Item2;
			recentsPastilles = _recentsLosanges;
			num3 = num;
			Intersect.Client.Framework.Gwen.Control.Label[] recentsMarques = _recentsMarques;
			num2 = num;
			(ImagePanel, Intersect.Client.Framework.Gwen.Control.Label) tuple3 = Marque(_fenetre, $"RecentMark{num}");
			recentsPastilles[num3] = tuple3.Item1;
			recentsMarques[num2] = tuple3.Item2;
			Intersect.Client.Framework.Gwen.Control.Base[] array = new Intersect.Client.Framework.Gwen.Control.Base[4]
			{
				_recentsPastilles[num],
				_recentsCodes[num],
				_recentsLosanges[num],
				_recentsMarques[num]
			};
			foreach (Intersect.Client.Framework.Gwen.Control.Base obj in array)
			{
				obj.IsHidden = true;
			}
		}
		(ImagePanel, Intersect.Client.Framework.Gwen.Control.Label) tuple4 = Capuchon("FoldedKeySpeak");
		_toucheParler = tuple4.Item1;
		_toucheParlerNom = tuple4.Item2;
		_replie = new Intersect.Client.Framework.Gwen.Control.Label(_fenetre, "ChatboxFoldedHint")
		{
			MouseInputEnabled = false,
			AutoSizeToContents = true,
			Font = _police,
			FontSize = 10,
			TextColorOverride = GrisRappel
		};
		tuple4 = Capuchon("FoldedKeyChannel");
		_toucheCanal = tuple4.Item1;
		_toucheCanalNom = tuple4.Item2;
		_replieCanal = new Intersect.Client.Framework.Gwen.Control.Label(_fenetre, "ChatboxFoldedHintChannel")
		{
			MouseInputEnabled = false,
			AutoSizeToContents = true,
			Font = _police,
			FontSize = 10,
			TextColorOverride = GrisRappel
		};
		_menuContextuel = new Intersect.Client.Framework.Gwen.Control.Menu(canvas, "ChatContextMenu")
		{
			IsHidden = true,
			IconMarginDisabled = true
		};
		_menuContextuel.ClearChildren();
		_mpItem = _menuContextuel.AddItem(Strings.ChatContextMenu.PM);
		_mpItem.Clicked += delegate(Intersect.Client.Framework.Gwen.Control.Base sender, MouseButtonState _)
		{
			SetChatboxText("/pm " + (string)sender.Parent.UserData + " ");
		};
		_amiItem = _menuContextuel.AddItem(Strings.ChatContextMenu.FriendInvite);
		_amiItem.Clicked += delegate(Intersect.Client.Framework.Gwen.Control.Base sender, MouseButtonState _)
		{
			PacketSender.SendAddFriend((string)sender.Parent.UserData);
		};
		_groupeItem = _menuContextuel.AddItem(Strings.ChatContextMenu.PartyInvite);
		_groupeItem.Clicked += delegate(Intersect.Client.Framework.Gwen.Control.Base sender, MouseButtonState _)
		{
			PacketSender.SendPartyInvite((string)sender.Parent.UserData);
		};
		_clanItem = _menuContextuel.AddItem(Strings.ChatContextMenu.GuildInvite);
		_clanItem.Clicked += delegate(Intersect.Client.Framework.Gwen.Control.Base sender, MouseButtonState _)
		{
			PacketSender.SendInviteGuild((string)sender.Parent.UserData);
		};
		_menuContextuel.LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer?.GetResolutionString() ?? string.Empty, saveOutput: false);
		if (GameClipboard.Instance == null || !GameClipboard.Instance.IsEnabled)
		{
			ChatboxMsg.AddMessage(new ChatboxMsg(Strings.Chatbox.UnableToCopy, CustomColors.Alerts.Error, ChatMessageType.Error));
		}
		RefaireOnglets();
		ChoisirCanal(0);
		Poser();
		_fenetre.BeforeLayout += delegate
		{
			RemplirMessages();
		};
	}

	private Button Icone(string nom, string texture, string infobulle)
	{
		Button button = new Button(_fenetre, nom)
		{
			UsesHeadingFace = false,
			Text = string.Empty
		};
		button.SetStateTexture(ComponentState.Normal, texture + ".png");
		button.SetStateTexture(ComponentState.Hovered, texture + "_hover.png");
		button.SetStateTexture(ComponentState.Active, texture + "_hover.png");
		button.SetStateTexture(ComponentState.Disabled, texture + "_actif.png");
		button.SetToolTipText(infobulle);
		return button;
	}

	private static List<(string Nom, int Valeur)> Canaux()
	{
		List<(string, int)> list = new List<(string, int)>
		{
			(Strings.Chatbox.Channels[0].ToString(), 0),
			(Strings.Chatbox.Channels[1].ToString(), 1),
			(Strings.Tchat.Monde.ToString(), 7),
			(Strings.Chatbox.Channels[2].ToString(), 2),
			(Strings.Chatbox.Channels[3].ToString(), 3)
		};
		list.Add((Strings.Chatbox.ChannelTrade.ToString(), 6));
		Player? me = Globals.Me;
		if (me != null && me.Type > EntityType.GlobalEntity)
		{
			list.Add((Strings.Chatbox.ChannelAdmin.ToString(), 4));
		}
		return list;
	}

	private static List<(string Commande, string Effet)> Commandes()
	{
		int num = 8;
		List<(string, string)> list = new List<(string, string)>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<(string, string)> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = ("/pm", Strings.Chatbox.CommandPrivate.ToString());
		num2++;
		span[num2] = ("/r", Strings.Chatbox.CommandReply.ToString());
		num2++;
		span[num2] = ("/party", Strings.Chatbox.CommandParty.ToString());
		num2++;
		span[num2] = ("/guild", Strings.Chatbox.CommandGuild.ToString());
		num2++;
		span[num2] = ("/global", Strings.Tchat.CommandeGlobal.ToString());
		num2++;
		span[num2] = ("/monde", Strings.Tchat.CommandeMonde.ToString());
		num2++;
		span[num2] = ("/commerce", Strings.Chatbox.CommandTrade.ToString());
		num2++;
		span[num2] = ("/local", Strings.Chatbox.CommandLocal.ToString());
		num2++;
		return list;
	}

	public void Disposer()
	{
		AdapterLaLargeur();
		Disposition.Aligner(Disposition.Piece.Tchat, _fenetre, new Alignments[2]
		{
			Alignments.Bottom,
			Alignments.Left
		}, 160, 100);
		if (_deploye)
		{
			Disposition.Occuper(Disposition.Piece.Tchat, new Rectangle(_fenetre.X, _fenetre.Y, _fenetre.Width, _fenetre.Height));
		}
	}

	private void AdapterLaLargeur()
	{
		int num = 560;
		Disposition.Reglage reglage = Disposition.De(Disposition.Piece.Tchat);
		if ((reglage == null || !reglage.Deplacee) && Interface.HasInGameUI)
		{
			HotBarWindow hotbar = Interface.GameUi.Hotbar;
			if (hotbar != null && !hotbar.IsHidden && hotbar.Width > 0 && hotbar.X > _fenetre.X && hotbar.Y < _fenetre.Y + 260 && hotbar.Y + hotbar.Height > _fenetre.Y)
			{
				num = Math.Clamp(hotbar.X - 8 - _fenetre.X, 420, 560);
			}
		}
		if (num != Largeur || _fenetre.Width != Largeur)
		{
			Largeur = num;
			_fenetre.SetSize(Largeur, 260);
			_messages.SetBounds(4, 30, Largeur - 16 + 8, 192);
			PoserLesCommandes();
			PoserLesOnglets();
			GarderLaPlace();
			_fenetre.Invalidate();
		}
	}

	private void Poser()
	{
		_fenetre.SetSize(Largeur, 260);
		_fenetre.Alignment = new Alignments[2]
		{
			Alignments.Bottom,
			Alignments.Left
		};
		_fenetre.AlignmentPadding = new Padding
		{
			Left = 4,
			Bottom = 10
		};
		PoserLesCommandes();
		_replie.SetPosition(10, 242);
	}

	private void PoserLesCommandes()
	{
		int y = 5;
		_replierBouton.SetBounds(Largeur - 8 - 18, y, 18, 18);
		_effacerBouton.SetBounds(Largeur - 8 - 36 - 4, y, 18, 18);
		_epingleBouton.SetBounds(Largeur - 8 - 54 - 8, y, 18, 18);
		int num = 233;
		_canalBouton.SetBounds(8, num, 88, 20);
		int num2 = LargeurTexte(_phrasesBouton.Text ?? string.Empty) + 18;
		_phrasesBouton.SetBounds(102, num, num2, 20);
		_envoyerBouton.SetBounds(Largeur - 8 - 64, num, 64, 20);
		int num3 = 102 + num2 + 8;
		_saisie.SetBounds(num3, num + 2, Largeur - 8 - 72 - num3, 16);
		PoserPhrases();
	}

	private void RefaireOnglets()
	{
		List<ChatboxTab> list = new List<ChatboxTab>
		{
			ChatboxTab.All,
			ChatboxTab.Local,
			ChatboxTab.Global,
			ChatboxTab.World
		};
		Player? me = Globals.Me;
		if (me != null && me.IsInParty())
		{
			list.Add(ChatboxTab.Party);
		}
		Player? me2 = Globals.Me;
		if (me2 != null && me2.IsInGuild)
		{
			list.Add(ChatboxTab.Guild);
		}
		if (_canal == 6 || _commerceVu)
		{
			list.Add(ChatboxTab.Trade);
		}
		list.Add(ChatboxTab.System);
		if (list.SequenceEqual(_ongletsVisibles))
		{
			return;
		}
		_ongletsVisibles = list;
		foreach (var (item, onglet2) in _onglets)
		{
			if (!list.Contains(item))
			{
				onglet2.Bouton.IsHidden = true;
				onglet2.Pastille.IsHidden = true;
				onglet2.Compte.IsHidden = true;
			}
		}
		PoserLesOnglets();
		if (!list.Contains(_ongletCourant))
		{
			ChoisirOnglet(ChatboxTab.All);
		}
		else
		{
			MarquerOngletActif();
		}
	}

	private void PoserLesOnglets()
	{
		List<string> noms = _ongletsVisibles.Select((ChatboxTab tab) => _onglets[tab].Nom).ToList();
		List<int> textes = noms.Select(LargeurTexte).ToList();
		bool avecLangue = _ongletsVisibles.Contains(ChatboxTab.Global);
		int num = Largeur - 6 - 70 - 10;
		int marge = 10;
		int ecart = 8;
		while (Total() > num && (marge > 4 || ecart > 3))
		{
			if (ecart > 3 && ecart >= marge - 2)
			{
				ecart--;
			}
			else
			{
				marge--;
			}
		}
		while (Total() > num)
		{
			int num2 = -1;
			for (int num3 = 0; num3 < noms.Count; num3++)
			{
				int length = noms[num3].TrimEnd('…').Length;
				if (length > 3 && (length > 4 || noms[num3].EndsWith('…')) && (num2 < 0 || textes[num3] > textes[num2]))
				{
					num2 = num3;
				}
			}
			if (num2 < 0)
			{
				break;
			}
			bool flag = !noms[num2].EndsWith('…');
			string text = noms[num2].TrimEnd('…');
			List<string> list = noms;
			int index = num2;
			string text2 = text;
			int num4 = ((!flag) ? 1 : 2);
			list[index] = text2.Substring(0, text2.Length - num4).TrimEnd() + "…";
			textes[num2] = LargeurTexte(noms[num2]);
		}
		while (Total() > num && (marge > 2 || ecart > 1))
		{
			if (ecart > 1 && ecart >= marge)
			{
				ecart--;
			}
			else
			{
				marge--;
			}
		}
		for (int num5 = 0; num5 < noms.Count; num5++)
		{
			Button bouton = _onglets[_ongletsVisibles[num5]].Bouton;
			if (bouton.Text != noms[num5])
			{
				bouton.Text = noms[num5];
			}
			bouton.SetToolTipText((noms[num5] == _onglets[_ongletsVisibles[num5]].Nom) ? null : _onglets[_ongletsVisibles[num5]].Nom);
		}
		int num6 = 6;
		int num7 = 4;
		for (int num8 = 0; num8 < _ongletsVisibles.Count; num8++)
		{
			ChatboxTab chatboxTab = _ongletsVisibles[num8];
			Onglet onglet = _onglets[chatboxTab];
			onglet.Bouton.IsHidden = !_deploye;
			int num9 = textes[num8] + 2 * marge;
			if (chatboxTab == ChatboxTab.Global)
			{
				num9 += 26;
				onglet.Bouton.TextAlign = Pos.Left | Pos.CenterV;
				onglet.Bouton.Padding = new Padding(marge, 0, 0, 0);
				int y = num7 + 3;
				_globalPastille.SetBounds(num6 + marge + textes[num8] + 4, y, 22, 13);
				_globalCode.SetBounds(num6 + marge + textes[num8] + 4, y, 22, 13);
				_globalPastille.IsHidden = !_deploye;
				_globalCode.IsHidden = !_deploye;
				_globalPastille.BringToFront();
				_globalCode.BringToFront();
			}
			onglet.Bouton.SetBounds(num6, num7, num9, 20);
			onglet.Pastille.SetBounds(num6 + num9 - 7, 2, 14, 12);
			onglet.Compte.SetBounds(num6 + num9 - 7, 2, 14, 12);
			num6 += num9 + ecart;
		}
		int Total()
		{
			return textes.Sum() + (avecLangue ? 26 : 0) + noms.Count * 2 * marge + (noms.Count - 1) * ecart;
		}
	}

	private int LargeurTexte(string texte)
	{
		return Mesurer(texte, 10).Largeur;
	}

	private void MarquerOngletActif()
	{
		foreach (var (chatboxTab2, onglet2) in _onglets)
		{
			if (chatboxTab2 == _ongletCourant)
			{
				onglet2.Bouton.Disable();
			}
			else
			{
				onglet2.Bouton.Enable();
			}
		}
	}

	private void ChoisirOnglet(ChatboxTab tab)
	{
		_ongletCourant = tab;
		_enAttente[tab] = 0;
		MarquerOngletActif();
		switch (tab)
		{
		case ChatboxTab.Local:
			ChoisirCanal(0);
			break;
		case ChatboxTab.Global:
			ChoisirCanal(1);
			break;
		case ChatboxTab.World:
			ChoisirCanal(7);
			break;
		case ChatboxTab.Party:
			ChoisirCanal(2);
			break;
		case ChatboxTab.Guild:
			ChoisirCanal(3);
			break;
		case ChatboxTab.Trade:
			ChoisirCanal(6);
			break;
		case ChatboxTab.System:
			break;
		}
	}

	private void ChoisirCanal(int canal)
	{
		_canal = canal;
		Color color = CouleurCanal(canal);
		_canalBouton.Text = Canaux().FirstOrDefault<(string, int)>(((string Nom, int Valeur) c) => c.Valeur == canal).Item1 ?? string.Empty;
		_canalBouton.RenderColor = color;
		_canalBouton.SetTextColor(color, ComponentState.Normal);
		_canalBouton.SetTextColor(Blanc, ComponentState.Hovered);
		RefaireOnglets();
	}

	private static Color CouleurCanal(int canal)
	{
		return canal switch
		{
			0 => TeinteLocal, 
			1 => TeinteGlobal, 
			2 => TeinteGroupe, 
			3 => TeinteClan, 
			4 => new Color(255, 255, 122, 26), 
			6 => new Color(255, 110, 220, 160), 
			7 => TeinteMonde, 
			_ => new Color(255, 236, 240, 248), 
		};
	}

	private static Color CouleurType(ChatMessageType type)
	{
		return type switch
		{
			ChatMessageType.Local => TeinteLocal, 
			ChatMessageType.Global => TeinteGlobal, 
			ChatMessageType.World => TeinteMonde, 
			ChatMessageType.Party => TeinteGroupe, 
			ChatMessageType.Guild => TeinteClan, 
			ChatMessageType.Trade => new Color(255, 110, 220, 160), 
			ChatMessageType.PM => new Color(255, 255, 140, 220), 
			ChatMessageType.Admin => new Color(255, 255, 122, 26), 
			ChatMessageType.Error => new Color(255, 255, 85, 102), 
			ChatMessageType.Combat => new Color(255, 255, 130, 140), 
			_ => new Color(255, 245, 190, 90), 
		};
	}

	private void RemplirMessages()
	{
		ScrollBar verticalScrollBar = _messages.GetVerticalScrollBar();
		bool flag = !(verticalScrollBar.ContentSize > (float)_messages.Height) || verticalScrollBar.ScrollAmount >= 0.99f;
		if (_ongletDessine != _ongletCourant)
		{
			_messages.Clear();
			_lignes.Clear();
			_messageSuivant = 0;
			_aRecu = true;
			_ongletDessine = _ongletCourant;
			flag = true;
		}
		List<ChatboxMsg> list = MessagesCourants();
		if (list.Count < _messageSuivant)
		{
			_messages.Clear();
			_lignes.Clear();
			_messageSuivant = 0;
		}
		int num = Math.Max(_messageSuivant, list.Count - ClientConfiguration.Instance.ChatLines);
		if (num < list.Count && _deploye)
		{
			if (flag)
			{
				ResterEnBas();
			}
			else
			{
				GarderLaPlace();
			}
		}
		for (int i = num; i < list.Count; i++)
		{
			AjouterLigne(list[i], (i > 0) ? list[i - 1] : null);
			_aRecu = true;
		}
		int val = list.Count - num;
		_messageSuivant = list.Count;
		while (_messages.RowCount > ClientConfiguration.Instance.ChatLines)
		{
			_messages.RemoveRow(0);
			if (_lignes.Count > 0)
			{
				_lignes.RemoveAt(0);
			}
		}
		if (!_aRecu)
		{
			return;
		}
		_aRecu = false;
		if (flag)
		{
			_nouveaux = 0;
			if (_deploye)
			{
				_messages.PostLayout.Enqueue(delegate
				{
					RecalerLaListe();
				}, _messages);
			}
			else
			{
				_messages.ScrollToBottom();
			}
			return;
		}
		_nouveaux += Math.Max(1, val);
		if (_deploye)
		{
			_messages.PostLayout.Enqueue(delegate
			{
				RecalerLaListe();
			}, _messages);
		}
	}

	private List<ChatboxMsg> MessagesCourants()
	{
		long milliseconds = Timing.Global.Milliseconds;
		if (_messagesCacheOnglet == _ongletCourant && _messagesCacheA == milliseconds && _messagesCache != null)
		{
			return _messagesCache;
		}
		_messagesCache = ChatboxMsg.GetMessages(_ongletCourant);
		_messagesCacheOnglet = _ongletCourant;
		_messagesCacheA = milliseconds;
		return _messagesCache;
	}

	private void AjouterLigne(ChatboxMsg message, ChatboxMsg? precedent)
	{
		bool suite;
		string label = Presenter(message, precedent, AfficherCanal(message.Type), out suite);
		ListBoxRow listBoxRow = _messages.AddRow(label, $"Message{_messages.RowCount}", message);
		listBoxRow.ShouldDrawBackground = false;
		listBoxRow.Padding = new Padding(12, 2, 4, 2);
		listBoxRow.Font = _police;
		listBoxRow.FontSize = 10;
		listBoxRow.SetTextColor(CouleurTexte(message));
		if (listBoxRow.GetCellContents(0) is Intersect.Client.Framework.Gwen.Control.Label label2)
		{
			label2.WrappingBehavior = WrappingBehavior.Wrapped;
		}
		listBoxRow.Clicked += LigneCliquee;
		ImagePanel imagePanel = new ImagePanel(listBoxRow, "Filet")
		{
			MouseInputEnabled = false,
			TextureFilename = "chat_pixel.png",
			RenderColor = CouleurType(message.Type)
		};
		imagePanel.SetBounds(2, 2, 3, 14);
		ImagePanel imagePanel2 = null;
		if (EstMentionne(message))
		{
			imagePanel2 = new ImagePanel(listBoxRow, "Mention")
			{
				MouseInputEnabled = false,
				TextureFilename = "chat_mention.png"
			};
			imagePanel2.SetBounds(0, 2, _messages.Width - 16, 14);
			imagePanel2.SendToBack();
		}
		LigneDeTchat ligneDeTchat = new LigneDeTchat
		{
			Ligne = listBoxRow,
			Filet = imagePanel,
			Voile = imagePanel2,
			Message = message,
			Precedent = precedent,
			Suite = suite,
			Revision = message.Revision
		};
		HabillerLigne(ligneDeTchat);
		_lignes.Add(ligneDeTchat);
	}

	private static bool AfficherCanal(ChatMessageType type)
	{
		return true;
	}

	private static string? NomCanal(ChatMessageType type)
	{
		return type switch
		{
			ChatMessageType.Local => Strings.Chatbox.Channels[0].ToString(), 
			ChatMessageType.Global => Strings.Chatbox.Channels[1].ToString(), 
			ChatMessageType.World => Strings.Tchat.Monde.ToString(), 
			ChatMessageType.Party => Strings.Chatbox.Channels[2].ToString(), 
			ChatMessageType.Guild => Strings.Chatbox.Channels[3].ToString(), 
			ChatMessageType.Trade => Strings.Chatbox.ChannelTrade.ToString(), 
			ChatMessageType.Admin => Strings.Chatbox.ChannelAdmin.ToString(), 
			ChatMessageType.PM => "MP", 
			_ => null, 
		};
	}

	private static string Presenter(ChatboxMsg message, ChatboxMsg? precedent, bool avecCanal)
	{
		bool suite;
		return Presenter(message, precedent, avecCanal, out suite);
	}

	private static string Presenter(ChatboxMsg message, ChatboxMsg? precedent, bool avecCanal, out bool suite)
	{
		suite = false;
		string text = message.Message ?? string.Empty;
		string text2 = (avecCanal ? NomCanal(message.Type) : null);
		string text3 = (string.IsNullOrWhiteSpace(text2) ? string.Empty : ("[" + text2 + "] "));
		string text4;
		string text6;
		if (message.CorpsDebut > 0 && message.CorpsDebut <= text.Length)
		{
			text4 = SansEtiquette(text.Substring(0, message.CorpsDebut)).TrimEnd();
			string text5;
			if (text4.EndsWith(':'))
			{
				text5 = text4;
				text4 = text5.Substring(0, text5.Length - 1).TrimEnd();
			}
			text5 = text;
			int corpsDebut = message.CorpsDebut;
			text6 = text5.Substring(corpsDebut, text5.Length - corpsDebut);
			if (text4.Length == 0)
			{
				return text3 + text6;
			}
		}
		else
		{
			text = SansEtiquette(text);
			if (string.IsNullOrWhiteSpace(message.Target))
			{
				return text3 + text;
			}
			int num = text.IndexOf(" : ", StringComparison.Ordinal);
			if (num <= 0)
			{
				return text3 + text;
			}
			text4 = text.Substring(0, num);
			string text5 = text;
			int corpsDebut = num + 3;
			text6 = text5.Substring(corpsDebut, text5.Length - corpsDebut);
		}
		return text3 + text4 + (Langue.EstSource ? " : " : ": ") + text6;
	}

	private static string SansEtiquette(string texte)
	{
		if (texte.StartsWith('['))
		{
			int num = texte.IndexOf("] ", StringComparison.Ordinal);
			if (num > 0)
			{
				int num2 = num + 2;
				return texte.Substring(num2, texte.Length - num2);
			}
		}
		return texte;
	}

	private static Color CouleurTexte(ChatboxMsg message)
	{
		ChatMessageType type = message.Type;
		if ((uint)type <= 7u)
		{
			return Eclaircie(CouleurType(message.Type), 0.5f);
		}
		return message.Color;
	}

	private static Color Eclaircie(Color couleur, float versBlanc)
	{
		return new Color(255, (int)((float)(int)couleur.R + (float)(255 - couleur.R) * versBlanc), (int)((float)(int)couleur.G + (float)(255 - couleur.G) * versBlanc), (int)((float)(int)couleur.B + (float)(255 - couleur.B) * versBlanc));
	}

	private static bool EstMentionne(ChatboxMsg message)
	{
		string text = Globals.Me?.Name;
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(message.Message))
		{
			return false;
		}
		if (message.Target == text)
		{
			return false;
		}
		return message.Message.Contains(text, StringComparison.OrdinalIgnoreCase);
	}

	private static string Heure(ChatboxMsg message)
	{
		return DateTimeOffset.FromUnixTimeMilliseconds(message.ArrivedAtUtc).ToLocalTime().ToString("HH:mm");
	}

	private void Ranger()
	{
		for (int num = _lignes.Count - 1; num >= 0; num--)
		{
			LigneDeTchat ligneDeTchat = _lignes[num];
			ListBoxRow ligne = ligneDeTchat.Ligne;
			ImagePanel filet = ligneDeTchat.Filet;
			ImagePanel voile = ligneDeTchat.Voile;
			ImagePanel imagePanel = filet;
			ListBoxRow listBoxRow = ligne;
			if (listBoxRow.Parent == null)
			{
				_lignes.RemoveAt(num);
			}
			else
			{
				int num2 = listBoxRow.Height - 4;
				if (listBoxRow.GetCellContents(0) is Intersect.Client.Framework.Gwen.Control.Label label)
				{
					num2 = label.MeasureShrinkToContents().Y;
					if (num2 > 0 && label.Height > num2)
					{
						int num3 = HashCode.Combine(ligneDeTchat.Revision, listBoxRow.Width);
						if (ligneDeTchat.EtatRaccourci != num3)
						{
							ligneDeTchat.EtatRaccourci = num3;
							ligneDeTchat.Raccourcies = 0;
						}
						if (ligneDeTchat.Raccourcies < 3)
						{
							ligneDeTchat.Raccourcies++;
							label.Height = num2;
							GarderLaPlace();
						}
					}
				}
				int num4 = Math.Max(10, Math.Min(listBoxRow.Height - 4, num2));
				if (imagePanel.Height != num4)
				{
					imagePanel.SetBounds(2, 2, 3, num4);
				}
				if (voile != null && (voile.Width != listBoxRow.Width || voile.Height != num4))
				{
					voile.SetBounds(0, 2, listBoxRow.Width, num4);
				}
				RangerLangue(ligneDeTchat, num4);
			}
		}
	}

	private void LigneCliquee(Intersect.Client.Framework.Gwen.Control.Base envoyeur, MouseButtonState arguments)
	{
		if (!(envoyeur is ListBoxRow { UserData: ChatboxMsg userData }))
		{
			return;
		}
		if (arguments.MouseButton == MouseButton.Left && userData.BasculerOriginal())
		{
			_derniereActivite = Timing.Global.Milliseconds;
		}
		string target = userData.Target;
		if (string.IsNullOrWhiteSpace(target))
		{
			return;
		}
		switch (arguments.MouseButton)
		{
		case MouseButton.Left:
		{
			GameInterface jeu = _jeu;
			if (jeu != null && jeu.IsAdminWindowOpen)
			{
				_jeu.AdminWindowSelectName(target);
			}
			break;
		}
		case MouseButton.Right:
			if (ClientConfiguration.Instance.EnableContextMenus)
			{
				OpenContextMenu(target);
			}
			else
			{
				SetChatboxText("/pm " + target + " ");
			}
			break;
		}
	}

	private void DescendreEnBas()
	{
		_nouveaux = 0;
		ResterEnBas();
		_messages.ScrollToBottom();
		_messages.PostLayout.Enqueue(delegate(ListBox liste)
		{
			liste.ScrollToBottom();
		}, _messages);
	}

	private void EffacerJournal()
	{
		ChatboxMsg.ClearMessages();
		_messages.Clear();
		_lignes.Clear();
		_messageSuivant = 0;
		_classes = 0;
		_nouveaux = 0;
		_aRecu = true;
		ChatboxTab[] array = _enAttente.Keys.ToArray();
		foreach (ChatboxTab key in array)
		{
			_enAttente[key] = 0;
		}
	}

	private void ClasserNouveaux()
	{
		List<ChatboxMsg> messages = ChatboxMsg.GetMessages();
		if (messages.Count < _classes)
		{
			_classes = 0;
			ChatboxTab[] array = _enAttente.Keys.ToArray();
			foreach (ChatboxTab key in array)
			{
				_enAttente[key] = 0;
			}
		}
		for (int j = _classes; j < messages.Count; j++)
		{
			ChatboxMsg chatboxMsg = messages[j];
			if (chatboxMsg.Type == ChatMessageType.Trade)
			{
				_commerceVu = true;
			}
			foreach (ChatboxTab item in OngletsDe(chatboxMsg.Type))
			{
				_enAttente[item] = _enAttente.GetValueOrDefault(item) + 1;
			}
		}
		_classes = messages.Count;
		if (_deploye)
		{
			_enAttente[_ongletCourant] = 0;
		}
	}

	private static IEnumerable<ChatboxTab> OngletsDe(ChatMessageType type)
	{
		yield return ChatboxTab.All;
		switch (type)
		{
		case ChatMessageType.Local:
			yield return ChatboxTab.Local;
			break;
		case ChatMessageType.Global:
			yield return ChatboxTab.Global;
			break;
		case ChatMessageType.World:
			yield return ChatboxTab.World;
			break;
		case ChatMessageType.Party:
			yield return ChatboxTab.Party;
			break;
		case ChatMessageType.Guild:
			yield return ChatboxTab.Guild;
			break;
		case ChatMessageType.Trade:
			yield return ChatboxTab.Trade;
			break;
		case ChatMessageType.PM:
			yield return ChatboxTab.Local;
			yield return ChatboxTab.Global;
			yield return ChatboxTab.World;
			yield return ChatboxTab.Party;
			yield return ChatboxTab.Guild;
			yield return ChatboxTab.Trade;
			break;
		case ChatMessageType.Admin:
			yield return ChatboxTab.Local;
			yield return ChatboxTab.Global;
			yield return ChatboxTab.World;
			yield return ChatboxTab.Party;
			yield return ChatboxTab.Guild;
			yield return ChatboxTab.Trade;
			yield return ChatboxTab.System;
			break;
		default:
			yield return ChatboxTab.System;
			break;
		}
	}

	public void Tick()
	{
		long milliseconds = Timing.Global.Milliseconds;
		long num = milliseconds - _dernierTick;
		_dernierTick = milliseconds;
		RefaireOnglets();
		MajReprises();
		Ranger();
		Point mousePosition = InputHandler.MousePosition;
		Rectangle bounds = _fenetre.Bounds;
		bool flag = mousePosition.X >= bounds.X && mousePosition.X < bounds.X + bounds.Width && mousePosition.Y >= bounds.Y && mousePosition.Y < bounds.Y + bounds.Height;
		int count = MessagesCourants().Count;
		if (count != _vus || _saisie.HasFocus || flag)
		{
			_vus = count;
			_derniereActivite = milliseconds;
		}
		if (!_replierPret)
		{
			_replierPret = true;
			_rappelJusqua = milliseconds + 20000;
			Replier();
		}
		if (!_deploye && flag && mousePosition.Y >= bounds.Y + bounds.Height - 46)
		{
			_rappelJusqua = Math.Max(_rappelJusqua, milliseconds + 1500);
		}
		ClasserNouveaux();
		SuivreRecents(milliseconds);
		if (_deploye && !_epingle && !_saisie.HasFocus && !flag && milliseconds - _deployeA > 400)
		{
			Replier();
		}
		MajRecents(milliseconds);
		MajMenuCanal(milliseconds, mousePosition.X - bounds.X, mousePosition.Y - bounds.Y);
		MajAide();
		MajPastilles();
		MajNouveaux();
		MajTouches();
		MajRappel(milliseconds, num);
		if (_fenetre.Texture != null)
		{
			int num2 = ((milliseconds - _derniereActivite < 6000) ? 255 : 70);
			if (_opacite != num2)
			{
				int num3 = (int)Math.Max(1L, num * 255 / 400);
				_opacite = ((num2 > _opacite) ? Math.Min(num2, _opacite + num3) : Math.Max(num2, _opacite - num3));
				_fenetre.RenderColor = new Color(_opacite, 255, 255, 255);
				_fenetre.Invalidate();
			}
		}
	}

	private void MajPastilles()
	{
		foreach (var (chatboxTab2, onglet2) in _onglets)
		{
			if (!_deploye || !_ongletsVisibles.Contains(chatboxTab2) || chatboxTab2 == _ongletCourant)
			{
				onglet2.Pastille.IsHidden = true;
				onglet2.Compte.IsHidden = true;
				continue;
			}
			int valueOrDefault = _enAttente.GetValueOrDefault(chatboxTab2);
			if (valueOrDefault <= 0)
			{
				onglet2.Pastille.IsHidden = true;
				onglet2.Compte.IsHidden = true;
			}
			else
			{
				onglet2.Compte.Text = ((valueOrDefault > 9) ? "9+" : valueOrDefault.ToString());
				onglet2.Pastille.IsHidden = false;
				onglet2.Compte.IsHidden = false;
			}
		}
	}

	private void MajNouveaux()
	{
		bool flag = _deploye && _nouveaux > 0 && !PhrasesOuvertes;
		_nouveauxBouton.IsHidden = !flag;
		if (flag)
		{
			_nouveauxBouton.Text = Strings.Chatbox.NewMessages.ToString(_nouveaux);
			int num = LargeurTexte(_nouveauxBouton.Text) + 24;
			_nouveauxBouton.SetBounds((Largeur - num) / 2, 200, num, 20);
			_nouveauxBouton.BringToFront();
		}
	}

	private void MajRappel(long maintenant, long ecoule)
	{
		float num = ((maintenant < _rappelJusqua) ? 1f : 0f);
		float num2 = (float)Math.Max(1L, ecoule) / 400f;
		_rappelOpacite = ((num > _rappelOpacite) ? Math.Min(num, _rappelOpacite + num2) : Math.Max(num, _rappelOpacite - num2));
		if (!_deploye)
		{
			int num3 = (int)(255f * _rappelOpacite);
			_toucheParler.RenderColor = new Color(num3, 255, 255, 255);
			_toucheCanal.RenderColor = new Color(num3, 255, 255, 255);
			_toucheParlerNom.TextColorOverride = new Color(num3, Blanc.R, Blanc.G, Blanc.B);
			_toucheCanalNom.TextColorOverride = new Color(num3, Blanc.R, Blanc.G, Blanc.B);
			_replie.TextColorOverride = new Color(num3, GrisRappel.R, GrisRappel.G, GrisRappel.B);
			_replieCanal.TextColorOverride = new Color(num3, GrisRappel.R, GrisRappel.G, GrisRappel.B);
			Intersect.Client.Framework.Gwen.Control.Base[] array = new Intersect.Client.Framework.Gwen.Control.Base[6] { _toucheParler, _toucheParlerNom, _replie, _toucheCanal, _toucheCanalNom, _replieCanal };
			foreach (Intersect.Client.Framework.Gwen.Control.Base obj in array)
			{
				obj.IsHidden = num3 <= 0;
			}
		}
	}

	private void MajTouches()
	{
		bool flag = _saisie.HasFocus && Globals.InputManager.IsKeyDown(Keys.Tab);
		if (flag && !_tabTenue)
		{
			List<(string, int)> list = Canaux();
			int num = list.FindIndex(((string Nom, int Valeur) c) => c.Valeur == _canal);
			ChoisirCanal(list[(num + 1) % list.Count].Item2);
		}
		_tabTenue = flag;
	}

	private void MajAide()
	{
		string text = _saisie.Text ?? string.Empty;
		if (text == _saisieVue)
		{
			return;
		}
		_saisieVue = text;
		if (!_deploye || !_saisie.HasFocus || !text.StartsWith('/') || text.Contains(' '))
		{
			_aide.IsHidden = true;
			return;
		}
		int num = Math.Min(Largeur - 16 - 8, Math.Max(232, _aideItems.Max<(Button, string)>(((Button Bouton, string Commande) tuple) => LargeurTexte(tuple.Bouton.Text ?? string.Empty)) + 18));
		int num2 = 6;
		int num3 = 0;
		foreach (var aideItem in _aideItems)
		{
			Button item = aideItem.Bouton;
			string item2 = aideItem.Commande;
			bool flag = item2.StartsWith(text, StringComparison.OrdinalIgnoreCase);
			item.IsHidden = !flag;
			if (flag)
			{
				item.SetBounds(4, 4 + num3 * 20, num, 20);
				num3++;
				num2 += 20;
			}
		}
		if (num3 == 0)
		{
			_aide.IsHidden = true;
			return;
		}
		FermerPhrases();
		_aide.SetBounds(8, 226 - num2 - 4, num + 8, num2);
		_aide.IsHidden = false;
		_aide.BringToFront();
	}

	private void Deployer()
	{
		_deployeA = Timing.Global.Milliseconds;
		_derniereActivite = _deployeA;
		if (_deploye)
		{
			return;
		}
		_deploye = true;
		if (_fondTexture != null)
		{
			_fenetre.Texture = _fondTexture;
		}
		else
		{
			_fenetre.TextureFilename = "chat_fond.png";
		}
		_fenetre.MouseInputEnabled = true;
		_messages.Show();
		_canalBouton.Show();
		_phrasesBouton.Show();
		_saisie.Show();
		_envoyerBouton.Show();
		_epingleBouton.Show();
		_effacerBouton.Show();
		_replierBouton.Show();
		Intersect.Client.Framework.Gwen.Control.Base[] array = new Intersect.Client.Framework.Gwen.Control.Base[6] { _toucheParler, _toucheParlerNom, _replie, _toucheCanal, _toucheCanalNom, _replieCanal };
		foreach (Intersect.Client.Framework.Gwen.Control.Base obj in array)
		{
			obj.IsHidden = true;
		}
		foreach (var (item, onglet2) in _onglets)
		{
			onglet2.Bouton.IsHidden = !_ongletsVisibles.Contains(item);
		}
		bool isHidden = !_ongletsVisibles.Contains(ChatboxTab.Global);
		_globalPastille.IsHidden = isHidden;
		_globalCode.IsHidden = isHidden;
		DescendreEnBas();
	}

	private void Replier()
	{
		_deploye = false;
		if (_saisie.HasFocus)
		{
			_saisie.Text = string.Empty;
			_messages.Focus();
		}
		_fenetre.Texture = null;
		_fenetre.MouseInputEnabled = false;
		_messages.Hide();
		_canalBouton.Hide();
		_phrasesBouton.Hide();
		FermerPhrases();
		_globalPastille.IsHidden = true;
		_globalCode.IsHidden = true;
		_saisie.Hide();
		_envoyerBouton.Hide();
		_epingleBouton.Hide();
		_effacerBouton.Hide();
		_replierBouton.Hide();
		_aide.IsHidden = true;
		_canalMenu.IsHidden = true;
		_nouveauxBouton.IsHidden = true;
		foreach (Onglet value in _onglets.Values)
		{
			value.Bouton.IsHidden = true;
			value.Pastille.IsHidden = true;
			value.Compte.IsHidden = true;
		}
		RangerRappel();
	}

	private void RangerRappel()
	{
		_toucheParlerNom.Text = ToucheParler().ToUpperInvariant();
		_toucheParlerNom.SizeToContents();
		_replie.Text = Strings.Chatbox.FoldedSpeak.ToString().ToUpperInvariant();
		_replie.SizeToContents();
		_toucheCanalNom.Text = Strings.Chatbox.FoldedTabKey.ToString().ToUpperInvariant();
		_toucheCanalNom.SizeToContents();
		_replieCanal.Text = Strings.Chatbox.FoldedChannel.ToString().ToUpperInvariant();
		_replieCanal.SizeToContents();
		int num = 241;
		int x = 10;
		x = PoserCapuchon(_toucheParler, _toucheParlerNom, x, num, 15);
		_replie.SetPosition(x, num + (15 - _replie.Height) / 2);
		x += _replie.Width + 10;
		x = PoserCapuchon(_toucheCanal, _toucheCanalNom, x, num, 15);
		_replieCanal.SetPosition(x, num + (15 - _replieCanal.Height) / 2);
		Intersect.Client.Framework.Gwen.Control.Base[] array = new Intersect.Client.Framework.Gwen.Control.Base[6] { _toucheParler, _toucheParlerNom, _replie, _toucheCanal, _toucheCanalNom, _replieCanal };
		foreach (Intersect.Client.Framework.Gwen.Control.Base obj in array)
		{
			obj.IsHidden = false;
		}
	}

	private (ImagePanel Cadre, Intersect.Client.Framework.Gwen.Control.Label Nom) Capuchon(string nom)
	{
		ImagePanel item = new ImagePanel(_fenetre, nom)
		{
			MouseInputEnabled = false,
			IsHidden = true,
			TextureFilename = "chat_touche.png",
			TextureNinePatchMargin = new Margin(4)
		};
		Intersect.Client.Framework.Gwen.Control.Label item2 = new Intersect.Client.Framework.Gwen.Control.Label(_fenetre, nom + "Label")
		{
			MouseInputEnabled = false,
			IsHidden = true,
			AutoSizeToContents = true,
			Font = _police,
			FontSize = 10,
			TextColorOverride = Blanc
		};
		return (Cadre: item, Nom: item2);
	}

	private static int PoserCapuchon(ImagePanel capuchon, Intersect.Client.Framework.Gwen.Control.Label nom, int x, int y, int hauteur)
	{
		int num = nom.Width + 12;
		capuchon.SetBounds(x, y, num, hauteur);
		nom.SetPosition(x + 6, y + (hauteur - nom.Height) / 2);
		return x + num + 6;
	}

	private static string ToucheParler()
	{
		if (Controls.ActiveControls.TryGetMappingFor(Control.Enter, out ControlMapping mapping))
		{
			Keys? keys = mapping.Bindings.FirstOrDefault(delegate(ControlBinding b)
			{
				Keys? keys2 = b?.Key;
				bool flag = ((!keys2.HasValue || keys2.GetValueOrDefault() == Keys.None) ? true : false);
				return !flag;
			})?.Key;
			if (keys.HasValue)
			{
				Keys valueOrDefault = keys.GetValueOrDefault();
				if (Strings.Keys.KeyDictionary.TryGetValue(valueOrDefault.GetKeyId().ToLowerInvariant(), out LocalizedString value))
				{
					return value.ToString();
				}
			}
		}
		return Strings.Chatbox.FoldedEnterKey.ToString();
	}

	private void BasculerEpingle()
	{
		_epingle = !_epingle;
		_epingleBouton.SetStateTexture(ComponentState.Normal, _epingle ? "chat_icone_epingle_actif.png" : "chat_icone_epingle.png");
		if (_epingle)
		{
			Deployer();
		}
	}

	private void SuivreRecents(long maintenant)
	{
		List<ChatboxMsg> list = MessagesCourants();
		if (_recentsOnglet != _ongletCourant || _recentsVus < 0 || list.Count < _recentsVus)
		{
			_recentsOnglet = _ongletCourant;
			_recents.Clear();
			_recentsVus = list.Count;
			return;
		}
		for (int i = _recentsVus; i < list.Count; i++)
		{
			_recents.Add((list[i], maintenant));
		}
		_recentsVus = list.Count;
		if (_recents.Count > 5)
		{
			_recents.RemoveRange(0, _recents.Count - 5);
		}
	}

	private void MajRecents(long maintenant)
	{
		if (_deploye)
		{
			if (_recentsRanges)
			{
				_recentsRanges = false;
				Intersect.Client.Framework.Gwen.Control.Label[] recentsTextes = _recentsTextes;
				foreach (Intersect.Client.Framework.Gwen.Control.Label label in recentsTextes)
				{
					label.IsHidden = true;
				}
				Intersect.Client.Framework.Gwen.Control.Label[] recentsHeures = _recentsHeures;
				foreach (Intersect.Client.Framework.Gwen.Control.Label label2 in recentsHeures)
				{
					label2.IsHidden = true;
				}
				ImagePanel[] recentsFonds = _recentsFonds;
				foreach (ImagePanel imagePanel in recentsFonds)
				{
					imagePanel.IsHidden = true;
				}
				for (int l = 0; l < 5; l++)
				{
					RangerLangueRecente(l, null, 0, 0, 0, 0);
				}
				_voile.IsHidden = true;
			}
			return;
		}
		_recentsRanges = true;
		int num = Largeur - 90;
		List<(ChatboxMsg, float)> list = new List<(ChatboxMsg, float)>();
		foreach (var recent in _recents)
		{
			ChatboxMsg item = recent.Message;
			long item2 = recent.Arrivee;
			long num2 = maintenant - item2;
			if (num2 < 10500)
			{
				list.Add((item, (num2 <= 9000) ? 1f : (1f - (float)(num2 - 9000) / 1500f)));
			}
		}
		int num3 = list.Count;
		int num4 = 0;
		for (int num5 = list.Count - 1; num5 >= 0; num5--)
		{
			int count = Couper(Presenter(list[num5].Item1, null, AfficherCanal(list[num5].Item1.Type)), num - PlaceDeLangue(list[num5].Item1)).Count;
			if (num4 + count > 5)
			{
				break;
			}
			num4 += count;
			num3 = num5;
		}
		if (num3 == list.Count && list.Count > 0)
		{
			num3 = list.Count - 1;
		}
		List<(string, string, Color, Color, float, ChatboxMsg, bool, bool)> list2 = new List<(string, string, Color, Color, float, ChatboxMsg, bool, bool)>();
		ChatboxMsg precedent = null;
		for (int m = num3; m < list.Count; m++)
		{
			(ChatboxMsg, float) tuple = list[m];
			ChatboxMsg item3 = tuple.Item1;
			float item4 = tuple.Item2;
			bool suite;
			List<string> list3 = Couper(Presenter(item3, precedent, AfficherCanal(item3.Type), out suite), num - PlaceDeLangue(item3));
			for (int n = 0; n < list3.Count; n++)
			{
				list2.Add(((n == 0) ? Heure(item3) : string.Empty, list3[n], CouleurType(item3.Type), CouleurTexte(item3), item4, item3, n == 0 && !suite, n == list3.Count - 1));
			}
			precedent = item3;
		}
		int num6 = Math.Min(list2.Count, 5);
		int num7 = Math.Max(16, Mesurer("Ag", 10).Hauteur + 4);
		int num8 = num7 + 2;
		int num9 = 236;
		if (num6 > 0)
		{
			int val = num9 - num6 * num8 - 26;
			_voile.SetBounds(0, Math.Max(0, val), Largeur, 260 - Math.Max(0, val));
			_voile.RenderColor = new Color(255, 255, 255, 255);
			_voile.IsHidden = false;
		}
		else
		{
			_voile.SetBounds(0, 214, Largeur, 46);
			_voile.RenderColor = new Color((int)(255f * _rappelOpacite), 255, 255, 255);
			_voile.IsHidden = _rappelOpacite <= 0f;
		}
		for (int num10 = 0; num10 < 5; num10++)
		{
			Intersect.Client.Framework.Gwen.Control.Label label3 = _recentsTextes[num10];
			Intersect.Client.Framework.Gwen.Control.Label label4 = _recentsHeures[num10];
			ImagePanel imagePanel2 = _recentsFonds[num10];
			if (num10 >= num6)
			{
				label3.IsHidden = true;
				label4.IsHidden = true;
				imagePanel2.IsHidden = true;
				RangerLangueRecente(num10, null, 0, 0, 0, 0);
				continue;
			}
			(string, string, Color, Color, float, ChatboxMsg, bool, bool) tuple2 = list2[num10];
			string item5 = tuple2.Item1;
			string item6 = tuple2.Item2;
			Color item7 = tuple2.Item3;
			Color item8 = tuple2.Item4;
			float item9 = tuple2.Item5;
			ChatboxMsg item10 = tuple2.Item6;
			bool item11 = tuple2.Item7;
			bool item12 = tuple2.Rest.Item1;
			if (label3.Text != item6)
			{
				label3.Text = item6;
				label3.SizeToContents();
			}
			if (label4.Text != item5)
			{
				label4.Text = item5;
				label4.SizeToContents();
			}
			int num11 = num9 - (num6 - num10) * num8;
			int num12 = (int)(255f * item9);
			imagePanel2.SetBounds(8, num11 + 2, 3, num7 - 4);
			imagePanel2.RenderColor = new Color(num12, item7.R, item7.G, item7.B);
			imagePanel2.IsHidden = false;
			label4.SetPosition(17, num11 + (num7 - label4.Height) / 2);
			label4.TextColorOverride = new Color(num12, GrisHeure.R, GrisHeure.G, GrisHeure.B);
			label4.IsHidden = item5.Length == 0;
			int num13 = 54 + (item10.EstDeJoueur ? 27 : 0);
			label3.SetPosition(num13, num11 + (num7 - label3.Height) / 2);
			label3.TextColorOverride = new Color(num12, item8.R, item8.G, item8.B);
			label3.IsHidden = false;
			RangerLangueRecente(num10, item10, item11 ? 54 : (-1), item12 ? (num13 + label3.Width + 8) : (-1), num11 + (num7 - 13) / 2, num12);
		}
	}

	private List<string> Couper(string texte, int largeur)
	{
		if (_police == null)
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = texte;
			num2++;
			return list;
		}
		List<string> list2 = new List<string>();
		string text = string.Empty;
		string[] array = texte.Split(' ');
		foreach (string text2 in array)
		{
			string text3 = ((text.Length == 0) ? text2 : (text + " " + text2));
			if (text.Length > 0 && Mesurer(text3, 10).Largeur > largeur)
			{
				list2.Add(text);
				text = text2;
			}
			else
			{
				text = text3;
			}
		}
		if (text.Length > 0)
		{
			list2.Add(text);
		}
		return list2;
	}

	private void BasculerMenuCanal()
	{
		if (!_canalMenu.IsHidden)
		{
			_canalMenu.IsHidden = true;
			return;
		}
		FermerPhrases();
		int num = _canalItems.Count * 22 + 8;
		_canalMenu.SetBounds(_canalBouton.X, _canalBouton.Y - num - 4, 120, num);
		for (int i = 0; i < _canalItems.Count; i++)
		{
			Button button = _canalItems[i];
			int num2 = (int)button.UserData;
			button.SetBounds(4, 4 + i * 22, 112, 22);
			button.SetTextColor((num2 == _canal) ? CouleurCanal(num2) : Gris, ComponentState.Normal);
			button.SetTextColor(CouleurCanal(num2), ComponentState.Hovered);
		}
		_menuLoinDepuis = -1L;
		_canalMenu.IsHidden = false;
		_canalMenu.BringToFront();
	}

	private void MajMenuCanal(long maintenant, int x, int y)
	{
		if (!_canalMenu.IsHidden)
		{
			if (Dedans(_canalMenu, x, y) || Dedans(_canalBouton, x, y))
			{
				_menuLoinDepuis = -1L;
			}
			else if (_menuLoinDepuis < 0)
			{
				_menuLoinDepuis = maintenant;
			}
			else if (maintenant - _menuLoinDepuis > 700)
			{
				_canalMenu.IsHidden = true;
			}
		}
		static bool Dedans(Intersect.Client.Framework.Gwen.Control.Base noeud, int px, int py)
		{
			if (px >= noeud.X && px < noeud.X + noeud.Width && py >= noeud.Y)
			{
				return py < noeud.Y + noeud.Height;
			}
			return false;
		}
	}

	public void Focus()
	{
		if (Timing.Global.Milliseconds - _envoyeA >= 200 && !_saisie.HasFocus)
		{
			Deployer();
			_saisie.Text = string.Empty;
			_saisie.Focus();
		}
	}

	public void UnFocus()
	{
		if (_saisie.HasFocus)
		{
			_saisie.Text = string.Empty;
			_messages.Focus();
			_aide.IsHidden = true;
			FermerPhrases();
			if (!_epingle)
			{
				Replier();
			}
		}
	}

	public void SetChatboxText(string message)
	{
		Deployer();
		_saisie.Text = message;
		_saisie.Focus();
		_saisie.CursorEnd = _saisie.Text.Length;
		_saisie.CursorPos = _saisie.Text.Length;
	}

	private void EnvoyerMessage()
	{
		string text = (_saisie.Text ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			_saisie.Text = string.Empty;
			_envoyeA = Timing.Global.Milliseconds;
			UnFocus();
		}
		else if (_dernierEnvoi > Timing.Global.MillisecondsUtc)
		{
			ChatboxMsg.AddMessage(new ChatboxMsg(Strings.Chatbox.TooFast, Color.Red, ChatMessageType.Error));
			_dernierEnvoi = Timing.Global.MillisecondsUtc + Options.Instance.Chat.MinIntervalBetweenChats;
		}
		else
		{
			_dernierEnvoi = Timing.Global.MillisecondsUtc + Options.Instance.Chat.MinIntervalBetweenChats;
			PacketSender.SendChatMsg(text, (byte)_canal);
			_saisie.Text = string.Empty;
			_envoyeA = Timing.Global.Milliseconds;
			UnFocus();
		}
	}

	public void OpenContextMenu(string nom)
	{
		_menuContextuel.RemoveChild(_mpItem, dispose: false);
		_menuContextuel.RemoveChild(_amiItem, dispose: false);
		_menuContextuel.RemoveChild(_groupeItem, dispose: false);
		_menuContextuel.RemoveChild(_clanItem, dispose: false);
		_menuContextuel.ClearChildren();
		if (!string.IsNullOrWhiteSpace(nom))
		{
			_menuContextuel.AddChild(_mpItem);
			_mpItem.SetText(Strings.ChatContextMenu.PM.ToString(nom));
			Player? me = Globals.Me;
			if (me != null && me.Friends.All((IFriendInstance ami) => ami.Name != nom))
			{
				_menuContextuel.AddChild(_amiItem);
				_amiItem.SetText(Strings.ChatContextMenu.FriendInvite.ToString(nom));
			}
			if (Globals.Me != null && ((Globals.Me.IsInParty() && Globals.Me.Party[0].Id == Globals.Me.Id && Globals.Me.Party.All((IPartyMember membre) => membre.Name != nom)) || !Globals.Me.IsInParty()))
			{
				_menuContextuel.AddChild(_groupeItem);
				_groupeItem.SetText(Strings.ChatContextMenu.PartyInvite.ToString(nom));
			}
			Player? me2 = Globals.Me;
			if (me2 != null && me2.IsInGuild && Globals.Me.GuildRank.Permissions.Invite && Globals.Me.GuildMembers.All((GuildMember membre) => membre.Name != nom))
			{
				_menuContextuel.AddChild(_clanItem);
				_clanItem.SetText(Strings.ChatContextMenu.GuildInvite.ToString(nom));
			}
			_menuContextuel.UserData = nom;
			_menuContextuel.SizeToChildren();
			_menuContextuel.Open(Pos.None);
		}
	}

	private (int Largeur, int Hauteur) Mesurer(string texte, int taille)
	{
		if (_police != null && !string.IsNullOrEmpty(texte))
		{
			GameRenderer renderer = Graphics.Renderer;
			if (renderer != null)
			{
				Vector2 vector = renderer.MeasureText(texte, _police, taille, 1f);
				return (Largeur: (int)Math.Ceiling(vector.X), Hauteur: (int)Math.Ceiling(vector.Y));
			}
			Intersect.Client.Framework.Gwen.Renderer.Base obj = _fenetre.Skin?.Renderer;
			if (obj != null)
			{
				Point point = obj.MeasureText(_police, taille, texte);
				return (Largeur: point.X, Hauteur: point.Y);
			}
		}
		return (Largeur: 8 * Math.Max(3, texte?.Length ?? 0), Hauteur: taille + 6);
	}

	private (ImagePanel Fond, Intersect.Client.Framework.Gwen.Control.Label Code) Pastille(Intersect.Client.Framework.Gwen.Control.Base parent, string nom, string langue)
	{
		ImagePanel imagePanel = new ImagePanel(parent, nom)
		{
			MouseInputEnabled = false,
			TextureFilename = "chat_langue.png",
			TextureNinePatchMargin = new Margin(4)
		};
		imagePanel.SetBounds(9, 2, 22, 13);
		Intersect.Client.Framework.Gwen.Control.Label label = new Intersect.Client.Framework.Gwen.Control.Label(parent, nom + "Code")
		{
			MouseInputEnabled = false,
			AutoSizeToContents = false,
			Font = _police,
			FontSize = 9,
			Text = (langue ?? string.Empty).ToUpperInvariant(),
			TextAlign = Pos.Center,
			TextColorOverride = EncrePastille
		};
		label.SetBounds(9, 2, 22, 13);
		return (Fond: imagePanel, Code: label);
	}

	private (ImagePanel Losange, Intersect.Client.Framework.Gwen.Control.Label Texte) Marque(Intersect.Client.Framework.Gwen.Control.Base parent, string nom)
	{
		ImagePanel imagePanel = new ImagePanel(parent, nom)
		{
			MouseInputEnabled = false,
			TextureFilename = "chat_traduit.png"
		};
		imagePanel.SetBounds(0, 2, 7, 7);
		Intersect.Client.Framework.Gwen.Control.Label label = new Intersect.Client.Framework.Gwen.Control.Label(parent, nom + "Label")
		{
			MouseInputEnabled = false,
			AutoSizeToContents = false,
			Font = _police,
			FontSize = 9,
			TextAlign = (Pos.Left | Pos.CenterV)
		};
		label.SetBounds(0, 2, 10, 13);
		return (Losange: imagePanel, Texte: label);
	}

	private static (string? Texte, Color Couleur, bool Losange) MarqueDe(ChatboxMsg message)
	{
		if (!message.EstDeJoueur)
		{
			return (Texte: null, Couleur: Gris, Losange: false);
		}
		switch (message.Etat)
		{
		case EtatDeTraduction.Traduit:
		case EtatDeTraduction.TraduitEnAttente:
			if (!message.MontreOriginal)
			{
				return (Texte: Strings.Tchat.Traduit.ToString().ToUpperInvariant(), Couleur: OrMarque, Losange: true);
			}
			return (Texte: Strings.Tchat.Original.ToString().ToUpperInvariant(), Couleur: Gris, Losange: true);
		case EtatDeTraduction.NonTraduit:
			return (Texte: Strings.Tchat.NonTraduit.ToString().ToUpperInvariant(), Couleur: Gris, Losange: false);
		case EtatDeTraduction.Phrase:
			if (message.Langue != Langue.Actuelle)
			{
				return (Texte: Strings.Tchat.Traduit.ToString().ToUpperInvariant(), Couleur: OrMarque, Losange: true);
			}
			break;
		}
		return (Texte: null, Couleur: Gris, Losange: false);
	}

	private int PlaceDeLangue(ChatboxMsg message)
	{
		if (!message.EstDeJoueur)
		{
			return 0;
		}
		var (text, _, flag) = MarqueDe(message);
		return 27 + ((text != null) ? (Mesurer(text, 9).Largeur + (flag ? 11 : 0) + 8) : 0);
	}

	private static string Infobulle(ChatboxMsg message)
	{
		string text = Heure(message);
		if (!message.EstDeJoueur)
		{
			return text;
		}
		text = text + " · " + NomDeLangue(message.Langue);
		if (message.AUnOriginal)
		{
			text += $" · {(message.MontreOriginal ? Strings.Tchat.VoirTraduction : Strings.Tchat.VoirOriginal)}";
		}
		return text;
	}

	private static string NomDeLangue(string code)
	{
		(string, string)[] disponibles = Langue.Disponibles;
		for (int i = 0; i < disponibles.Length; i++)
		{
			var (text, result) = disponibles[i];
			if (text == code)
			{
				return result;
			}
		}
		return code.ToUpperInvariant();
	}

	private void HabillerLigne(LigneDeTchat entree)
	{
		ChatboxMsg message = entree.Message;
		ListBoxRow ligne = entree.Ligne;
		int num = 12;
		if (message.EstDeJoueur)
		{
			num += 27;
			if (!entree.Suite && entree.Pastille == null)
			{
				(entree.Pastille, entree.Code) = Pastille(ligne, "Langue", message.Langue);
			}
		}
		int num2 = 4;
		var (text, color, flag) = MarqueDe(message);
		if (text != null)
		{
			if (entree.Marque == null || entree.Losange == null)
			{
				(entree.Losange, entree.Marque) = Marque(ligne, "Marque");
			}
			if (entree.Marque.Text != text)
			{
				entree.Marque.Text = text;
			}
			entree.Marque.TextColorOverride = color;
			entree.Marque.IsHidden = false;
			entree.Losange.RenderColor = color;
			entree.Losange.IsHidden = !flag;
			entree.AvecLosange = flag;
			entree.LargeurMarque = Mesurer(text, 9).Largeur + 2 + (flag ? 11 : 0);
			num2 += entree.LargeurMarque + 8;
		}
		else if (entree.Marque != null)
		{
			entree.Marque.IsHidden = true;
			if (entree.Losange != null)
			{
				entree.Losange.IsHidden = true;
			}
			entree.LargeurMarque = 0;
		}
		Padding padding = new Padding(num, 2, num2, 2);
		if (ligne.Padding != padding)
		{
			ligne.Padding = padding;
		}
		int num3 = ligne.Width - num - num2;
		if (num3 > 0)
		{
			Intersect.Client.Framework.Gwen.Control.Base cellContents = ligne.GetCellContents(0);
			if (cellContents != null && cellContents.Width != num3)
			{
				cellContents.Width = num3;
			}
		}
		ligne.SetToolTipText(Infobulle(message));
	}

	private void RangerLangue(LigneDeTchat entree, int hauteur)
	{
		int num = Math.Min(hauteur, HauteurDeLigne);
		ImagePanel pastille = entree.Pastille;
		if (pastille != null)
		{
			Intersect.Client.Framework.Gwen.Control.Label code = entree.Code;
			if (code != null)
			{
				int num2 = Math.Min(13, num);
				int num3 = 2 + (num - num2) / 2;
				if (pastille.Y != num3 || pastille.Height != num2 || pastille.Width != 22)
				{
					pastille.SetBounds(9, num3, 22, num2);
				}
				if (code.Y != num3 || code.Height != num2 || code.Width != 22 || code.X != 9)
				{
					code.SetBounds(9, num3, 22, num2);
				}
			}
		}
		Intersect.Client.Framework.Gwen.Control.Label marque = entree.Marque;
		if (marque == null || marque.IsHidden)
		{
			return;
		}
		int num4 = Math.Min(13, num);
		int num5 = 2 + (num - num4) / 2;
		int num6 = entree.Ligne.Width - 4 - entree.LargeurMarque;
		int num7 = num6 + (entree.AvecLosange ? 11 : 0);
		int num8 = entree.Ligne.Width - 4 - num7;
		if (marque.X != num7 || marque.Y != num5 || marque.Width != num8 || marque.Height != num4)
		{
			marque.SetBounds(num7, num5, num8, num4);
		}
		if (!entree.AvecLosange)
		{
			return;
		}
		ImagePanel losange = entree.Losange;
		if (losange != null)
		{
			int num9 = Math.Min(7, num);
			int num10 = 2 + (num - num9) / 2;
			if (losange.X != num6 || losange.Y != num10 || losange.Height != num9 || losange.Width != num9)
			{
				losange.SetBounds(num6, num10, num9, num9);
			}
		}
	}

	private void RangerLangueRecente(int place, ChatboxMsg? message, int pastilleX, int marqueX, int y, int alpha)
	{
		ImagePanel imagePanel = _recentsPastilles[place];
		Intersect.Client.Framework.Gwen.Control.Label label = _recentsCodes[place];
		ImagePanel imagePanel2 = _recentsLosanges[place];
		Intersect.Client.Framework.Gwen.Control.Label label2 = _recentsMarques[place];
		bool flag = message != null && message.EstDeJoueur && pastilleX >= 0;
		imagePanel.IsHidden = !flag;
		label.IsHidden = !flag;
		if (flag)
		{
			string text = message.Langue.ToUpperInvariant();
			if (label.Text != text)
			{
				label.Text = text;
			}
			imagePanel.SetBounds(pastilleX, y, 22, 13);
			label.SetBounds(pastilleX, y, 22, 13);
			imagePanel.RenderColor = new Color(alpha, 255, 255, 255);
			label.TextColorOverride = new Color(alpha, EncrePastille.R, EncrePastille.G, EncrePastille.B);
		}
		string text2;
		Color color;
		bool flag2;
		if (message != null && marqueX >= 0)
		{
			(text2, color, flag2) = MarqueDe(message);
		}
		else
		{
			Color gris = Gris;
			flag2 = false;
			color = gris;
			text2 = null;
		}
		label2.IsHidden = text2 == null;
		imagePanel2.IsHidden = text2 == null || !flag2;
		if (text2 != null)
		{
			if (label2.Text != text2)
			{
				label2.Text = text2;
			}
			Color color2 = new Color(alpha, color.R, color.G, color.B);
			int x = marqueX + (flag2 ? 11 : 0);
			label2.SetBounds(x, y, Mesurer(text2, 9).Largeur + 2, 13);
			label2.TextColorOverride = color2;
			if (flag2)
			{
				imagePanel2.SetBounds(marqueX, y + 3, 7, 7);
				imagePanel2.RenderColor = color2;
			}
		}
	}

	private void MajReprises()
	{
		TenirLaPlace();
		if (ChatboxMsg.Changements == _changementsVus)
		{
			return;
		}
		_changementsVus = ChatboxMsg.Changements;
		bool flag = false;
		foreach (LigneDeTchat ligne in _lignes)
		{
			if (ligne.Revision != ligne.Message.Revision && ligne.Ligne.Parent != null)
			{
				if (!flag)
				{
					flag = true;
					GarderLaPlace();
				}
				ligne.Revision = ligne.Message.Revision;
				ligne.Ligne.SetCellText(0, Presenter(ligne.Message, ligne.Precedent, AfficherCanal(ligne.Message.Type)));
				HabillerLigne(ligne);
			}
		}
		if (flag)
		{
			_derniereActivite = Timing.Global.Milliseconds;
			TenirLaPlace();
		}
	}

	private void ResterEnBas()
	{
		_ancreEnBas = true;
		_ancre = null;
		_ancreTenue = true;
		_ancreMontant = float.NaN;
		_ancreImages = 4;
	}

	private void GarderLaPlace()
	{
		if (_ancreImages > 0 && _ancreTenue)
		{
			_ancreImages = 4;
			return;
		}
		ScrollBar verticalScrollBar = _messages.GetVerticalScrollBar();
		_ancreEnBas = verticalScrollBar.ContentSize <= (float)_messages.Height || verticalScrollBar.ScrollAmount >= 0.99f;
		_ancre = null;
		if (!_ancreEnBas)
		{
			int verticalScroll = _messages.VerticalScroll;
			foreach (LigneDeTchat ligne in _lignes)
			{
				if (ligne.Ligne.Parent != null && verticalScroll + ligne.Ligne.Y + ligne.Ligne.Height > 0)
				{
					_ancre = ligne;
					_ancreEcran = verticalScroll + ligne.Ligne.Y;
					break;
				}
			}
		}
		_ancreTenue = true;
		_ancreMontant = float.NaN;
		_ancreImages = 4;
	}

	private void TenirLaPlace()
	{
		if (_ancreImages <= 0)
		{
			return;
		}
		_ancreImages--;
		if (!_deploye)
		{
			_ancreImages = 0;
			_ancre = null;
			return;
		}
		if (_ancreTenue && !float.IsNaN(_ancreMontant) && Math.Abs(_messages.GetVerticalScrollBar().ScrollAmount - _ancreMontant) > 0.0005f)
		{
			_ancreTenue = false;
		}
		if (ListeJetee())
		{
			_ancreImages = 0;
			return;
		}
		_messages.SizeToContents();
		_messages.PostLayout.Enqueue(delegate
		{
			RecalerLaListe();
		}, _messages);
	}

	private bool ListeJetee()
	{
		return !(_messages.Canvas?.IsVisibleInTree ?? false);
	}

	private void RecalerLaListe()
	{
		if (ListeJetee())
		{
			return;
		}
		try
		{
			RecalerLaListeSansGarde();
		}
		catch (ObjectDisposedException exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Tchat : recalage abandonne, interface jetee");
		}
	}

	private void RecalerLaListeSansGarde()
	{
		Ranger();
		if (!_ancreTenue)
		{
			return;
		}
		if (_ancreEnBas)
		{
			_messages.ScrollToBottom();
		}
		else
		{
			LigneDeTchat ancre = _ancre;
			if (ancre != null)
			{
				ListBoxRow ligne = ancre.Ligne;
				if (ligne != null && ligne.Parent != null)
				{
					_messages.UpdateScrollBars();
					int num = _ancreEcran - ancre.Ligne.Y;
					if (num != _messages.VerticalScroll)
					{
						_messages.ScrollToY(num);
					}
				}
			}
		}
		_ancreMontant = _messages.GetVerticalScrollBar().ScrollAmount;
	}

	private void BatirPhrases()
	{
		_phrasesBouton = new Button(_fenetre, "ChatPhrasesButton")
		{
			UsesHeadingFace = false,
			Font = _police,
			FontSize = 10,
			Text = Strings.Tchat.BoutonPhrases,
			TextAlign = Pos.Center
		};
		_phrasesBouton.SetStateTexture(ComponentState.Normal, "chat_canal.png");
		_phrasesBouton.SetStateTexture(ComponentState.Hovered, "chat_canal_hover.png");
		_phrasesBouton.SetStateTexture(ComponentState.Active, "chat_canal_hover.png");
		_phrasesBouton.SetTextColor(Gris, ComponentState.Normal);
		_phrasesBouton.SetTextColor(Blanc, ComponentState.Hovered);
		_phrasesBouton.SetToolTipText(Strings.Tchat.InfobullePhrases);
		_phrasesBouton.Clicked += delegate
		{
			BasculerPhrases();
		};
		_phrases = new ImagePanel(_fenetre, "ChatPhrases")
		{
			IsHidden = true,
			TextureFilename = "chat_menu.png",
			TextureNinePatchMargin = new Margin(6)
		};
		(FamilleDePhrase, Func<string>)[] familles = Familles;
		for (int num = 0; num < familles.Length; num++)
		{
			(FamilleDePhrase, Func<string>) tuple = familles[num];
			FamilleDePhrase item = tuple.Item1;
			Func<string> item2 = tuple.Item2;
			Button button = new Button(_phrases, $"ChatPhrasesFamily{item}")
			{
				UsesHeadingFace = false,
				Font = _police,
				FontSize = 10,
				Text = item2(),
				TextAlign = Pos.Center,
				UserData = item
			};
			button.SetStateTexture(ComponentState.Normal, "chat_canal.png");
			button.SetStateTexture(ComponentState.Hovered, "chat_canal_hover.png");
			button.SetStateTexture(ComponentState.Active, "chat_canal_hover.png");
			button.Clicked += delegate(Intersect.Client.Framework.Gwen.Control.Base sender, MouseButtonState _)
			{
				ChoisirFamille((FamilleDePhrase)sender.UserData);
			};
			_famillesBoutons.Add((button, item));
		}
		_phrasesRetour = new Button(_phrases, "ChatPhrasesBack")
		{
			UsesHeadingFace = false,
			Font = _police,
			FontSize = 10,
			Text = Strings.Tchat.Retour,
			TextAlign = Pos.Center,
			IsHidden = true
		};
		_phrasesRetour.SetStateTexture(ComponentState.Normal, "chat_canal.png");
		_phrasesRetour.SetStateTexture(ComponentState.Hovered, "chat_canal_hover.png");
		_phrasesRetour.SetStateTexture(ComponentState.Active, "chat_canal_hover.png");
		_phrasesRetour.SetTextColor(Gris, ComponentState.Normal);
		_phrasesRetour.SetTextColor(Blanc, ComponentState.Hovered);
		_phrasesRetour.Clicked += delegate
		{
			_phraseATrou = null;
			ComposerPhrases();
		};
		_phrasesTitre = new Intersect.Client.Framework.Gwen.Control.Label(_phrases, "ChatPhrasesTitle")
		{
			MouseInputEnabled = false,
			AutoSizeToContents = false,
			Font = _police,
			FontSize = 10,
			TextAlign = (Pos.Left | Pos.CenterV),
			TextColorOverride = Blanc,
			IsHidden = true
		};
		_phrasesFilet = new ImagePanel(_phrases, "ChatPhrasesRule")
		{
			MouseInputEnabled = false,
			TextureFilename = "chat_pixel.png",
			RenderColor = new Color(28, 255, 255, 255)
		};
		for (int num2 = 0; num2 < 4; num2++)
		{
			Button button2 = LigneDePanneau(_phrases, $"ChatPhrase{num2}");
			button2.Clicked += delegate(Intersect.Client.Framework.Gwen.Control.Base sender, MouseButtonState _)
			{
				if (sender.UserData is PhraseDeTchat phrase)
				{
					PhraseCliquee(phrase);
				}
			};
			_phrasesItems.Add(button2);
		}
		_nomsListe = new ScrollControl(_phrases, "ChatPhrasesNames")
		{
			IsHidden = true
		};
		_nomsListe.EnableScroll(horizontal: false, vertical: true);
	}

	private Button LigneDePanneau(Intersect.Client.Framework.Gwen.Control.Base parent, string nom)
	{
		Button button = new Button(parent, nom)
		{
			UsesHeadingFace = false,
			Font = _police,
			FontSize = 10,
			TextAlign = (Pos.Left | Pos.CenterV),
			Padding = new Padding(10, 0, 4, 0)
		};
		button.SetStateTexture(ComponentState.Normal, "hud_transparent.png");
		button.SetStateTexture(ComponentState.Hovered, "chat_menu_hover.png");
		button.SetStateTexture(ComponentState.Active, "chat_menu_hover.png");
		button.SetTextColor(Blanc, ComponentState.Normal);
		button.SetTextColor(Blanc, ComponentState.Hovered);
		return button;
	}

	private void BasculerPhrases()
	{
		if (PhrasesOuvertes)
		{
			FermerPhrases();
			return;
		}
		_canalMenu.IsHidden = true;
		_aide.IsHidden = true;
		_phraseATrou = null;
		_phrases.IsHidden = false;
		ComposerPhrases();
		_phrases.BringToFront();
		_phrasesBouton.SetTextColor(Orange, ComponentState.Normal);
		_derniereActivite = Timing.Global.Milliseconds;
	}

	private void FermerPhrases()
	{
		if (!_phrases.IsHidden)
		{
			_phrases.IsHidden = true;
			_phraseATrou = null;
			_phrasesBouton.SetTextColor(Gris, ComponentState.Normal);
		}
	}

	private void ChoisirFamille(FamilleDePhrase famille)
	{
		_famille = famille;
		_phraseATrou = null;
		ComposerPhrases();
	}

	private void PoserPhrases()
	{
		if (PhrasesOuvertes)
		{
			ComposerPhrases();
		}
	}

	private void ComposerPhrases()
	{
		int num = Largeur - 16;
		bool flag = _phraseATrou != null;
		if (flag)
		{
			_noms = NomsPour(_phraseATrou.Trou);
		}
		int num2 = (flag ? Math.Clamp(_noms.Count, 1, 6) : 4);
		int num3 = 34 + num2 * 22 + 6;
		_phrases.SetBounds(8, 226 - num3 - 4, num, num3);
		_phrasesFilet.SetBounds(8, 30, num - 16, 1);
		int num4 = 8;
		foreach (var famillesBouton in _famillesBoutons)
		{
			Button item = famillesBouton.Bouton;
			FamilleDePhrase item2 = famillesBouton.Famille;
			item.IsHidden = flag;
			if (!flag)
			{
				bool flag2 = item2 == _famille;
				Color clr = (item.RenderColor = (flag2 ? Orange : Gris));
				item.SetTextColor(clr, ComponentState.Normal);
				item.SetTextColor(flag2 ? Orange : Blanc, ComponentState.Hovered);
				int num5 = LargeurTexte(item.Text ?? string.Empty) + 20;
				item.SetBounds(num4, 6, num5, 20);
				num4 += num5 + 6;
			}
		}
		_phrasesRetour.IsHidden = !flag;
		_phrasesTitre.IsHidden = !flag;
		if (flag)
		{
			int num6 = LargeurTexte(_phrasesRetour.Text ?? string.Empty) + 20;
			_phrasesRetour.SetBounds(8, 6, num6, 20);
			_phrasesTitre.Text = TexteDe(_phraseATrou, "…");
			_phrasesTitre.SetBounds(8 + num6 + 10, 6, num - num6 - 26, 20);
		}
		List<PhraseDeTchat> list = (flag ? new List<PhraseDeTchat>() : PhrasesDeTchat.Toutes.Where((PhraseDeTchat phrase) => phrase.Famille == _famille).ToList());
		for (int num7 = 0; num7 < _phrasesItems.Count; num7++)
		{
			Button button = _phrasesItems[num7];
			if (num7 >= list.Count)
			{
				button.IsHidden = true;
				continue;
			}
			button.Text = TexteDe(list[num7], "…");
			button.UserData = list[num7];
			button.SetBounds(4, 34 + num7 * 22, num - 8, 22);
			button.IsHidden = false;
		}
		_nomsListe.IsHidden = !flag;
		if (!flag)
		{
			return;
		}
		_nomsListe.SetBounds(4, 34, num - 8, num2 * 22);
		while (_nomsItems.Count < _noms.Count)
		{
			Button button2 = LigneDePanneau(_nomsListe, $"ChatPhraseName{_nomsItems.Count}");
			button2.Clicked += delegate(Intersect.Client.Framework.Gwen.Control.Base sender, MouseButtonState _)
			{
				PhraseDeTchat phraseATrou = _phraseATrou;
				if ((object)phraseATrou != null && sender.UserData is string nom)
				{
					EnvoyerPhrase(phraseATrou, nom);
				}
			};
			_nomsItems.Add(button2);
		}
		int width = num - 8 - ((_noms.Count > 6) ? 16 : 0);
		for (int num8 = 0; num8 < _nomsItems.Count; num8++)
		{
			Button button3 = _nomsItems[num8];
			if (num8 >= _noms.Count)
			{
				button3.IsHidden = true;
				button3.SetBounds(0, 0, width, 0);
				continue;
			}
			button3.Text = _noms[num8].Affiche;
			button3.UserData = _noms[num8].Cle;
			button3.SetBounds(0, num8 * 22, width, 22);
			button3.IsHidden = false;
		}
		_nomsListe.ScrollToTop();
	}

	private static string TexteDe(PhraseDeTchat phrase, string nom)
	{
		if (!Strings.Tchat.Phrases.TryGetValue(phrase.Id, out LocalizedString value))
		{
			return phrase.Id;
		}
		if (phrase.Trou != TrouDePhrase.Aucun)
		{
			return value.ToString(nom);
		}
		return value.ToString();
	}

	private void PhraseCliquee(PhraseDeTchat phrase)
	{
		if (phrase.Trou == TrouDePhrase.Aucun)
		{
			EnvoyerPhrase(phrase, string.Empty);
			return;
		}
		_phraseATrou = phrase;
		ComposerPhrases();
	}

	private void EnvoyerPhrase(PhraseDeTchat phrase, string nom)
	{
		if (_dernierEnvoi > Timing.Global.MillisecondsUtc)
		{
			ChatboxMsg.AddMessage(new ChatboxMsg(Strings.Chatbox.TooFast, Color.Red, ChatMessageType.Error));
			_dernierEnvoi = Timing.Global.MillisecondsUtc + Options.Instance.Chat.MinIntervalBetweenChats;
			return;
		}
		_dernierEnvoi = Timing.Global.MillisecondsUtc + Options.Instance.Chat.MinIntervalBetweenChats;
		PacketSender.SendChatPhrase((byte)_canal, phrase.Id, nom);
		FermerPhrases();
		if (_saisie.HasFocus && string.IsNullOrWhiteSpace(_saisie.Text))
		{
			_envoyeA = Timing.Global.Milliseconds;
			UnFocus();
		}
	}

	private static List<(string Affiche, string Cle)> NomsPour(TrouDePhrase trou)
	{
		List<(string Affiche, string Cle)> noms = new List<(string, string)>();
		HashSet<string> vus = new HashSet<string>(StringComparer.Ordinal);
		switch (trou)
		{
		case TrouDePhrase.Lieu:
		{
			IMapInstance mapInstance = Globals.Me?.MapInstance;
			if (mapInstance != null)
			{
				Ajouter(mapInstance.Name, Traduction.NomSource(mapInstance.Id, mapInstance.Name));
			}
			foreach (DungeonDescriptor item2 in DatabaseObject<DungeonDescriptor>.Lookup.Values.OfType<DungeonDescriptor>().OrderBy<DungeonDescriptor, string>((DungeonDescriptor d) => d.Name, StringComparer.CurrentCultureIgnoreCase))
			{
				string text = Traduction.NomSource(item2.Id, item2.Name);
				if (Traduction.EstUnTexteDuJeu(text))
				{
					Ajouter(item2.Name, text);
				}
			}
			WorldMapDestination[] worldMapDestinations = Globals.WorldMapDestinations;
			foreach (WorldMapDestination worldMapDestination in worldMapDestinations)
			{
				string text2 = Traduction.Source(worldMapDestination.Name);
				if (worldMapDestination.Unlocked && Traduction.EstUnTexteDuJeu(text2))
				{
					Ajouter(worldMapDestination.Name, text2);
				}
			}
			foreach (MapInstance item3 in MapInstance.Lookup.Values.OfType<MapInstance>().OrderBy<MapInstance, string>((MapInstance c) => c.Name, StringComparer.CurrentCultureIgnoreCase))
			{
				Ajouter(item3.Name, Traduction.NomSource(item3.Id, item3.Name));
			}
			break;
		}
		case TrouDePhrase.Pnj:
			foreach (NPCDescriptor item4 in DatabaseObject<NPCDescriptor>.Lookup.Values.OfType<NPCDescriptor>().OrderBy<NPCDescriptor, string>((NPCDescriptor p) => p.Name, StringComparer.CurrentCultureIgnoreCase))
			{
				Ajouter(item4.Name, Traduction.NomSource(item4.Id, item4.Name));
			}
			break;
		case TrouDePhrase.Objet:
		{
			Player me = Globals.Me;
			if (me == null)
			{
				break;
			}
			IItem[] inventory = me.Inventory;
			foreach (IItem item in inventory)
			{
				if (item != null && item.ItemId != Guid.Empty && DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var databaseObject))
				{
					Ajouter(databaseObject.Name, Traduction.NomSource(databaseObject.Id, databaseObject.Name));
				}
			}
			break;
		}
		}
		if (noms.Count == 0 && NomsDeRepli.TryGetValue(trou, out string[] value))
		{
			string[] array = value;
			foreach (string text3 in array)
			{
				Ajouter(Traduction.Contenu(text3), text3);
			}
		}
		return noms;
		void Ajouter(string? affiche, string? cle)
		{
			if (!string.IsNullOrWhiteSpace(affiche) && !string.IsNullOrWhiteSpace(cle) && cle.Length <= 64 && vus.Add(cle))
			{
				noms.Add((affiche, cle));
			}
		}
	}
}
