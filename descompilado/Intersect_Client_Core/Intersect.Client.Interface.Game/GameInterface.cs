using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Dungeons;
using Intersect.Client.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Maps;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Admin;
using Intersect.Client.Interface.Game.Bag;
using Intersect.Client.Interface.Game.Bank;
using Intersect.Client.Interface.Game.Boss;
using Intersect.Client.Interface.Game.Chat;
using Intersect.Client.Interface.Game.Crafting;
using Intersect.Client.Interface.Game.DescriptionWindows;
using Intersect.Client.Interface.Game.Dungeons;
using Intersect.Client.Interface.Game.Enseignements;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Hdv;
using Intersect.Client.Interface.Game.Hotbar;
using Intersect.Client.Interface.Game.Housing;
using Intersect.Client.Interface.Game.Inventory;
using Intersect.Client.Interface.Game.Metiers;
using Intersect.Client.Interface.Game.Rankings;
using Intersect.Client.Interface.Game.Recompenses;
using Intersect.Client.Interface.Game.Shop;
using Intersect.Client.Interface.Game.Succes;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Interface.Game.Trades;
using Intersect.Client.Interface.Menu;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Config;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.GameObjects;
using Intersect.Models;
using Intersect.Network.Packets.Server;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Interface.Game;

public class GameInterface : MutableInterface
{
	public bool FocusChat;

	public bool UnfocusChat;

	public HotBarWindow Hotbar;

	private AdminWindow? mAdminWindow;

	private WorldMapWindow? _worldMapWindow;

	private BagWindow _bagWindow;

	private BankWindow? _bankWindow;

	private Chatbox mChatBox;

	private FenetreAtelier? mCraftingWindow;

	private PictureWindow mPictureWindow;

	private QuestOfferWindow mQuestOfferWindow;

	private ShopWindow _shopWindow;

	private MapItemWindow mMapItemWindow;

	private SettingsWindow? _settingsWindow;

	private ItemDescriptionWindow? _itemDescriptionWindow;

	private SpellDescriptionWindow? _spellDescriptionWindow;

	private bool mShouldCloseBag;

	private bool _shouldCloseBank;

	private bool mShouldCloseCraftingTable;

	private bool mShouldCloseShop;

	private bool mShouldCloseTrading;

	private bool mShouldOpenAdminWindow;

	private bool mShouldOpenBag;

	private bool mShouldOpenBank;

	private bool mShouldOpenCraftingTable;

	private bool mShouldOpenShop;

	private bool mShouldOpenTrading;

	private bool mShouldUpdateQuestLog = true;

	private bool mShouldUpdateFriendsList;

	private bool mShouldUpdateGuildList;

	private bool mShouldHideGuildWindow;

	private string mTradingTarget;

	private TradingWindow? mTradingWindow;

	public EntityBox PlayerBox;

	public PlayerStatusWindow PlayerStatusWindow;

	private PartyFrames? _partyFrames;

	private EscapeMenuWindow? _escapeMenu;

	private TargetContextMenu? _targetContextMenu;

	private bool _hudEscamote;

	private Annonces? _annonces;

	private HellPanel? _hellPanel;

	private RecherchePanel? _recherche;

	private BossHud? _bossHud;

	private DispositionEditeur? _dispositionEditeur;

	private string? _lastMapName;

	private MapZone? _lastZone;

	private int _lastLevel;

	private static readonly Dictionary<Guid, HashSet<string>> LieuxConnus = new Dictionary<Guid, HashSet<string>>();

	private FenetreSucces? _achievementsWindow;

	private FenetreMetiers? _professionsWindow;

	private CalendarWindow? _calendarWindow;

	private FenetreEnseignements? _teachingsScreen;

	private FenetreClassements? _rankingsScreen;

	private QuestTrackerPanel? _questTracker;

	private MiniMapPanel? _miniMap;

	private readonly List<Rectangle> _piecesOccupees = new List<Rectangle>();

	private FenetreRecompenses? _dailyRewardWindow;

	private bool _dailyRewardPending;

	private long _dailyRewardQuietSince;

	private const long DailyRewardQuietMs = 2000L;

	private FenetreHdv? _marketWindow;

	private RoueDesMenus? _roueDesMenus;

	public bool ChatFocussed => mChatBox.HasFocus;

	private bool mCraftJournal { get; set; }

	public Canvas GameCanvas { get; }

	public EscapeMenuWindow EscapeMenu
	{
		get
		{
			EscapeMenuWindow escapeMenuWindow = _escapeMenu;
			if (escapeMenuWindow == null)
			{
				EscapeMenuWindow obj = new EscapeMenuWindow(GameCanvas, GetOrCreateSettingsWindow)
				{
					IsHidden = true
				};
				EscapeMenuWindow escapeMenuWindow2 = obj;
				_escapeMenu = obj;
				escapeMenuWindow = escapeMenuWindow2;
			}
			return escapeMenuWindow;
		}
	}

	public TargetContextMenu TargetContextMenu
	{
		get
		{
			TargetContextMenu targetContextMenu = _targetContextMenu;
			if (targetContextMenu == null)
			{
				TargetContextMenu obj = new TargetContextMenu(GameCanvas)
				{
					IsHidden = true
				};
				TargetContextMenu targetContextMenu2 = obj;
				_targetContextMenu = obj;
				targetContextMenu = targetContextMenu2;
			}
			return targetContextMenu;
		}
	}

	public ItemDescriptionWindow? ItemDescriptionWindow
	{
		get
		{
			return _itemDescriptionWindow ?? (_itemDescriptionWindow = new ItemDescriptionWindow());
		}
		set
		{
			_itemDescriptionWindow = value;
		}
	}

	public SpellDescriptionWindow? SpellDescriptionWindow
	{
		get
		{
			return _spellDescriptionWindow ?? (_spellDescriptionWindow = new SpellDescriptionWindow());
		}
		set
		{
			_spellDescriptionWindow = value;
		}
	}

	public MenuContainer GameMenu { get; private set; }

	public bool IsWorldMapOpen => _worldMapWindow?.IsOpen ?? false;

	public bool IsAdminWindowOpen
	{
		get
		{
			AdminWindow? adminWindow = mAdminWindow;
			if (adminWindow == null)
			{
				return false;
			}
			return !adminWindow.IsHidden;
		}
	}

	public bool IsAnnouncementShowing => _annonces?.IsShowing ?? false;

	public bool IsAchievementsOpen
	{
		get
		{
			FenetreSucces achievementsWindow = _achievementsWindow;
			if (achievementsWindow != null)
			{
				return !achievementsWindow.IsHidden;
			}
			return false;
		}
	}

	public bool IsTeachingsOpen
	{
		get
		{
			FenetreEnseignements teachingsScreen = _teachingsScreen;
			if (teachingsScreen != null)
			{
				return !teachingsScreen.IsHidden;
			}
			return false;
		}
	}

	public bool IsRankingsOpen
	{
		get
		{
			FenetreClassements rankingsScreen = _rankingsScreen;
			if (rankingsScreen != null)
			{
				return !rankingsScreen.IsHidden;
			}
			return false;
		}
	}

	public bool IsProgressionOpen
	{
		get
		{
			FenetreSucces achievementsWindow = _achievementsWindow;
			if (achievementsWindow == null || achievementsWindow.IsHidden)
			{
				FenetreRecompenses dailyRewardWindow = _dailyRewardWindow;
				if (dailyRewardWindow == null || !dailyRewardWindow.IsOpen)
				{
					FenetreMetiers professionsWindow = _professionsWindow;
					if (professionsWindow == null || !professionsWindow.IsOpen)
					{
						FenetreClassements rankingsScreen = _rankingsScreen;
						if (rankingsScreen != null)
						{
							return !rankingsScreen.IsHidden;
						}
						return false;
					}
				}
			}
			return true;
		}
	}

	internal FenetreRecompenses? DailyRewardWindow => _dailyRewardWindow;

	public bool IsMarketOpen => _marketWindow?.IsOpen ?? false;

	public RoueDesMenus RoueDesMenus => _roueDesMenus ?? (_roueDesMenus = new RoueDesMenus(GameCanvas, this));

	private SettingsWindow GetOrCreateSettingsWindow()
	{
		if (_settingsWindow == null)
		{
			_settingsWindow = new SettingsWindow(GameCanvas)
			{
				IsVisibleInTree = false
			};
		}
		return _settingsWindow;
	}

	public GameInterface(Canvas canvas)
		: base(canvas)
	{
		GameCanvas = canvas;
		InitGameGui();
	}

	public void Annoncer(string message, long dureeMs)
	{
		_annonces?.Serveur(message, dureeMs);
	}

	public void InitGameGui()
	{
		mChatBox = new Chatbox(GameCanvas, this);
		GameMenu = new MenuContainer(GameCanvas);
		Hotbar = new HotBarWindow(GameCanvas);
		PlayerBox = new EntityBox(GameCanvas, EntityType.Player, Globals.Me, isPlayerBox: true);
		PlayerBox.SetEntity(Globals.Me);
		PlayerStatusWindow = new PlayerStatusWindow(GameCanvas);
		_partyFrames = new PartyFrames(GameCanvas);
		if (mPictureWindow == null)
		{
			mPictureWindow = new PictureWindow(GameCanvas);
		}
		mQuestOfferWindow = new QuestOfferWindow(GameCanvas);
		mMapItemWindow = new MapItemWindow(GameCanvas);
		_miniMap = new MiniMapPanel(GameCanvas);
		_miniMap.Update();
		_miniMap.SendToBack();
		mMapItemWindow.Panel.AlignmentPadding = new Padding
		{
			Top = 332,
			Right = 4
		};
		mMapItemWindow.Panel.ProcessAlignments();
		_questTracker = new QuestTrackerPanel(GameCanvas, _miniMap, mMapItemWindow.Panel);
		_questTracker.SendToBack();
		_annonces = new Annonces(GameCanvas);
		_bossHud = new BossHud(GameCanvas);
		_lastMapName = null;
		_lastZone = null;
		_lastLevel = 0;
	}

	public void SetChatboxText(string msg)
	{
		mChatBox.SetChatboxText(msg);
	}

	public void NotifyUpdateFriendsList()
	{
		mShouldUpdateFriendsList = true;
	}

	public void NotifyUpdateGuildList()
	{
		mShouldUpdateGuildList = true;
	}

	public void HideGuildWindow()
	{
		mShouldHideGuildWindow = true;
	}

	public void NotifyOpenAdminWindow()
	{
		mShouldOpenAdminWindow = true;
	}

	public void ToggleWorldMapWindow()
	{
		WorldMapWindow worldMapWindow = _worldMapWindow;
		if ((worldMapWindow == null || !worldMapWindow.IsOpen) && WorldMapWindow.AuRoyaumeDesMorts())
		{
			ChatboxMsg.AddMessage(new ChatboxMsg(Strings.WorldMap.NotFromAfterlife, CustomColors.Alerts.Declined, ChatMessageType.Notice));
			return;
		}
		if (_worldMapWindow == null)
		{
			_worldMapWindow = new WorldMapWindow(GameCanvas);
		}
		_worldMapWindow.SetDestinations(Globals.WorldMapDestinations);
		_worldMapWindow.Toggle();
	}

	public void OuvrirCarteDuRetour()
	{
		if (_worldMapWindow == null)
		{
			_worldMapWindow = new WorldMapWindow(GameCanvas);
		}
		_worldMapWindow.SetDestinations(Globals.WorldMapDestinations);
		_worldMapWindow.OuvrirPourLeRetour();
	}

	public void RefreshWorldMapDestinations()
	{
		_worldMapWindow?.SetDestinations(Globals.WorldMapDestinations);
	}

	public bool ToggleAdminWindow()
	{
		mShouldOpenAdminWindow = false;
		if (mAdminWindow == null)
		{
			if (mAdminWindow == null)
			{
				mAdminWindow = new AdminWindow(GameCanvas);
			}
			mAdminWindow.X = Math.Max(0, (GameCanvas.Width - mAdminWindow.OuterWidth) / 2);
			mAdminWindow.Y = Math.Max(0, (GameCanvas.Height - mAdminWindow.OuterHeight) / 2);
		}
		else if (IsAdminWindowOpen)
		{
			mAdminWindow.Hide();
		}
		else
		{
			mAdminWindow.Show();
		}
		return mAdminWindow.IsVisibleInParent;
	}

	public void NotifyOpenShop()
	{
		mShouldOpenShop = true;
	}

	public void NotifyCloseShop()
	{
		mShouldCloseShop = true;
	}

	public void OpenShop()
	{
		_shopWindow = new ShopWindow(GameCanvas)
		{
			DeleteOnClose = true
		};
		mShouldOpenShop = false;
	}

	public void NotifyOpenBank()
	{
		mShouldOpenBank = true;
	}

	public void NotifyCloseBank()
	{
		_shouldCloseBank = true;
	}

	public void OpenBank()
	{
		_bankWindow?.Detach();
		_bankWindow = new BankWindow(GameCanvas);
		mShouldOpenBank = false;
		Globals.InBank = true;
	}

	public void NotifyOpenBag()
	{
		mShouldOpenBag = true;
	}

	public void NotifyCloseBag()
	{
		mShouldCloseBag = true;
	}

	public void OpenBag()
	{
		_bagWindow = new BagWindow(GameCanvas)
		{
			DeleteOnClose = true
		};
		mShouldOpenBag = false;
		Globals.InBag = true;
	}

	public BagWindow GetBagWindow()
	{
		return _bagWindow;
	}

	public BankWindow? GetBankWindow()
	{
		return _bankWindow;
	}

	public void NotifyOpenCraftingTable(bool journalMode)
	{
		mShouldOpenCraftingTable = true;
		mCraftJournal = journalMode;
	}

	public void NotifyCloseCraftingTable()
	{
		mShouldCloseCraftingTable = true;
		mCraftJournal = false;
	}

	public void OpenCraftingTable()
	{
		if (mCraftingWindow != null)
		{
			mCraftingWindow.Close();
			mCraftingWindow.Detach();
		}
		mCraftingWindow = new FenetreAtelier(GameCanvas, mCraftJournal);
		mShouldOpenCraftingTable = false;
		Globals.InCraft = true;
	}

	public void NotifyQuestsUpdated()
	{
		mShouldUpdateQuestLog = true;
		GameMenu?.Notify(MenuBadge.Quests);
	}

	public void NotifyOpenTrading(string traderName)
	{
		mShouldOpenTrading = true;
		mTradingTarget = traderName;
	}

	public void NotifyCloseTrading()
	{
		mShouldCloseTrading = true;
	}

	public void OpenTrading()
	{
		mTradingWindow?.Close();
		mTradingWindow = new TradingWindow(GameCanvas, mTradingTarget);
		mShouldOpenTrading = false;
		Globals.InTrade = true;
	}

	public void AdminWindowSelectName(string playerName)
	{
		AdminWindow adminWindow = mAdminWindow;
		if (adminWindow != null)
		{
			adminWindow.PlayerName = playerName;
		}
	}

	public void Update(TimeSpan elapsed, TimeSpan total)
	{
		if (Globals.Me != null && PlayerBox?.MyEntity != Globals.Me)
		{
			PlayerBox?.SetEntity(Globals.Me);
		}
		ChronoImage.DebutDesTours();
		GameMenu?.Update(mShouldUpdateQuestLog);
		mShouldUpdateQuestLog = false;
		ChronoImage.Tour("interface : menu du jeu");
		Hotbar?.Update();
		KeepBottomBarClear();
		DeplacementDesFenetres.Update();
		if (GameMenu != null)
		{
			Disposition.Aligner(Disposition.Piece.Menus, GameMenu, new Alignments[2]
			{
				Alignments.Bottom,
				Alignments.Right
			});
		}
		if (Hotbar != null)
		{
			Disposition.Aligner(Disposition.Piece.Raccourcis, Hotbar, new Alignments[2]
			{
				Alignments.Bottom,
				Alignments.CenterH
			});
		}
		mChatBox?.Disposer();
		_dispositionEditeur?.Update();
		ChronoImage.Tour("interface : barre de raccourcis");
		_miniMap?.Update();
		ChronoImage.Tour("interface : mini-carte");
		_questTracker?.Update();
		ChronoImage.Tour("interface : suivi des quetes");
		mChatBox?.Tick();
		ChronoImage.Tour("interface : tchat");
		UpdateBanners();
		_bossHud?.Update();
		if (_hellPanel == null)
		{
			_hellPanel = new HellPanel(GameCanvas);
		}
		_hellPanel.Update();
		if (_recherche == null)
		{
			_recherche = new RecherchePanel(GameCanvas);
		}
		_recherche.Update();
		EscapeMenu.Update();
		ChronoImage.Tour("interface : bandeaux, boss, enfer, echap");
		PlayerBox?.Update();
		_partyFrames?.Update();
		ChronoImage.Tour("interface : cadre du joueur et groupe");
		if (PlayerStatusWindow != null && PlayerBox != null)
		{
			Point point = PlayerBox.LieuDesEffets(PlayerStatusWindow.Height);
			Point point2 = Disposition.Poser(Disposition.Piece.Effets, PlayerStatusWindow.Width, PlayerStatusWindow.Height, Math.Max(0, point.X), Math.Max(0, point.Y), GameCanvas, PlayerStatusWindow.IsVisibleInTree);
			if (PlayerStatusWindow.X != point2.X || PlayerStatusWindow.Y != point2.Y)
			{
				PlayerStatusWindow.SetPosition(point2.X, point2.Y);
			}
		}
		PlayerStatusWindow?.Update();
		PlayerStatusWindow playerStatusWindow = PlayerStatusWindow;
		if (playerStatusWindow != null && playerStatusWindow.IsVisibleInTree && Disposition.Masquee(Disposition.Piece.Effets))
		{
			PlayerStatusWindow.IsVisibleInTree = false;
		}
		mMapItemWindow.Update();
		mPictureWindow?.Update();
		Guid guid = Globals.QuestOffers.FirstOrDefault();
		QuestDescriptor databaseObject;
		if (guid == default(Guid))
		{
			if (mQuestOfferWindow.IsVisible())
			{
				mQuestOfferWindow.Hide();
			}
		}
		else if (DatabaseObject<QuestDescriptor>.TryGet(guid, out databaseObject))
		{
			mQuestOfferWindow.Update(databaseObject);
		}
		else
		{
			ApplicationContext.CurrentContext.Logger.LogWarning("Failed to get quest {QuestId}", guid);
		}
		if (Globals.Picture != null)
		{
			if (mPictureWindow.Picture != Globals.Picture.Picture || mPictureWindow.Size != Globals.Picture.Size || mPictureWindow.Clickable != Globals.Picture.Clickable)
			{
				mPictureWindow.Setup(Globals.Picture.Picture, Globals.Picture.Size, Globals.Picture.Clickable);
			}
		}
		else if (mPictureWindow != null)
		{
			mPictureWindow.Close();
		}
		EventWindow.ShowOrUpdateDialog(GameCanvas);
		OpenPendingDailyRewards();
		if (mShouldOpenAdminWindow)
		{
			ToggleAdminWindow();
		}
		if (mShouldOpenShop)
		{
			OpenShop();
			GameMenu.OpenInventory();
			_shopWindow?.DockWith(GameMenu.GetInventoryWindow());
		}
		if (_shopWindow != null && (!_shopWindow.IsVisibleInTree || mShouldCloseShop))
		{
			CloseShop();
		}
		mShouldCloseShop = false;
		if (mShouldOpenBank)
		{
			OpenBank();
			GameMenu?.OpenInventory();
			InventoryWindow inventoryWindow = GameMenu?.GetInventoryWindow();
			if (inventoryWindow != null)
			{
				_bankWindow?.DockWith(inventoryWindow);
			}
		}
		else if (_shouldCloseBank)
		{
			CloseBank();
		}
		else
		{
			_bankWindow?.Update();
		}
		if (mShouldOpenBag)
		{
			OpenBag();
		}
		if (_bagWindow != null)
		{
			if (!_bagWindow.IsVisibleInTree || mShouldCloseBag)
			{
				CloseBagWindow();
			}
			else
			{
				_bagWindow.Update();
			}
		}
		mShouldCloseBag = false;
		if (mShouldOpenCraftingTable)
		{
			OpenCraftingTable();
		}
		if (mCraftingWindow != null && (!mCraftingWindow.IsVisibleInTree || mShouldCloseCraftingTable))
		{
			CloseCraftingTable();
		}
		mShouldCloseCraftingTable = false;
		if (mShouldOpenTrading)
		{
			OpenTrading();
		}
		if (mTradingWindow != null)
		{
			if (mShouldCloseTrading)
			{
				CloseTrading();
				mShouldCloseTrading = false;
			}
			else if (!mTradingWindow.IsVisible())
			{
				CloseTrading();
			}
			else
			{
				mTradingWindow.Update();
			}
		}
		if (mShouldUpdateFriendsList)
		{
			GameMenu.UpdateFriendsList();
			mShouldUpdateFriendsList = false;
		}
		if (mShouldUpdateGuildList)
		{
			GameMenu.UpdateGuildList();
			mShouldUpdateGuildList = false;
		}
		if (mShouldHideGuildWindow)
		{
			GameMenu.HideGuildWindow();
			mShouldHideGuildWindow = false;
		}
		mShouldCloseTrading = false;
		if (FocusChat)
		{
			mChatBox.Focus();
			FocusChat = false;
		}
		if (UnfocusChat)
		{
			mChatBox.UnFocus();
			UnfocusChat = false;
		}
		_worldMapWindow?.Update();
		_achievementsWindow?.Update();
		_professionsWindow?.Update();
		_calendarWindow?.Update();
		_teachingsScreen?.Update();
		_rankingsScreen?.Update();
		_dailyRewardWindow?.Update();
		_marketWindow?.Update();
		HouseInterface.Update();
		DungeonInterface.Update();
		TrialInterface.Update();
		BandesDeCinema.Update();
		EscamoterLeHud(BandesDeCinema.InterfaceEscamotee);
	}

	private void EscamoterLeHud(bool escamote)
	{
		if (escamote && !_hudEscamote)
		{
			UnfocusChat = true;
		}
		_hudEscamote = escamote;
		Escamoter(mChatBox?.Fenetre, escamote);
		Escamoter(GameMenu, escamote);
		Escamoter(Hotbar, escamote);
		Escamoter(PlayerBox?.EntityWindow, escamote);
		Escamoter(Globals.Me?.TargetBox?.EntityWindow, escamote);
		Escamoter(PlayerStatusWindow, escamote);
		Escamoter(_partyFrames?.Fenetre, escamote);
		Escamoter(_miniMap, escamote);
		Escamoter(_questTracker, escamote);
		Escamoter(mMapItemWindow?.Panel, escamote);
		Escamoter(_bossHud, escamote);
		Escamoter(_hellPanel?.Fenetre, escamote);
		Escamoter(_recherche, escamote);
		Escamoter(DungeonInterface.Hud, escamote);
		Escamoter(DungeonInterface.Hud?.BoutonAbandon, escamote);
		Escamoter(DungeonInterface.Overlay, escamote);
		Escamoter(TrialInterface.Hud, escamote);
	}

	private static void Escamoter(Base? piece, bool escamote)
	{
		if (piece != null)
		{
			piece.Escamote = escamote;
		}
	}

	public void OuvrirLaDisposition()
	{
		if (_dispositionEditeur == null)
		{
			_dispositionEditeur = new DispositionEditeur(GameCanvas);
		}
		_dispositionEditeur.Ouvrir();
	}

	private void KeepBottomBarClear()
	{
		HotBarWindow hotbar = Hotbar;
		if (hotbar == null || hotbar.IsHidden || hotbar.Width <= 0)
		{
			return;
		}
		MenuContainer gameMenu = GameMenu;
		if (gameMenu == null || gameMenu.IsHidden || gameMenu.Width <= 0)
		{
			return;
		}
		Canvas gameCanvas = GameCanvas;
		if (gameCanvas == null || gameCanvas.Width <= 0)
		{
			return;
		}
		Disposition.Reglage reglage = Disposition.De(Disposition.Piece.Raccourcis);
		if (reglage != null && reglage.Deplacee)
		{
			return;
		}
		reglage = Disposition.De(Disposition.Piece.Menus);
		if (reglage == null || !reglage.Deplacee)
		{
			int num = (gameCanvas.Width - hotbar.Width) / 2;
			int num2 = Math.Max(4, Math.Min(num, gameMenu.X - 8 - hotbar.Width));
			if (hotbar.AlignmentTranslation.X != num2 - num)
			{
				hotbar.AlignmentTranslation = new Point(num2 - num, hotbar.AlignmentTranslation.Y);
			}
		}
	}

	private void UpdateBanners()
	{
		Player me = Globals.Me;
		if (me != null)
		{
			IMapInstance mapInstance = me.MapInstance;
			string text = mapInstance?.Name;
			if (mapInstance != null && !string.IsNullOrWhiteSpace(text) && (text != _lastMapName || mapInstance.ZoneType != _lastZone))
			{
				_lastMapName = text;
				_lastZone = mapInstance.ZoneType;
				ShowPlace(me.Id, mapInstance, text);
			}
			if (_lastLevel > 0 && me.Level > _lastLevel)
			{
				_annonces?.Evenement(Strings.Banners.LevelUp.ToString(), Strings.Banners.LevelReached.ToString(me.Level), null, TechniqueArt.Ki, 2600L);
			}
			_lastLevel = me.Level;
		}
		if (Dungeon.InRun || MasterTrial.InTrial)
		{
			_annonces?.AnnulerLieu();
		}
		_annonces?.Update();
	}

	private void ShowPlace(Guid playerId, IMapInstance map, string mapName)
	{
		if (Dungeon.InRun || MasterTrial.InTrial)
		{
			_annonces?.AnnulerLieu();
			return;
		}
		string text = Traduction.NomSource(map.Id, mapName).Trim();
		int num = text.IndexOf(" : ", StringComparison.Ordinal);
		string texte;
		if (num <= 0)
		{
			texte = text;
		}
		else
		{
			string text2 = text;
			int num2 = num + 3;
			texte = text2.Substring(num2, text2.Length - num2);
		}
		string titre = Traduction.Contenu(texte);
		string text3 = (map as MapDescriptor)?.Region?.Trim() ?? string.Empty;
		if (text3.StartsWith("NON RELI", StringComparison.OrdinalIgnoreCase))
		{
			text3 = string.Empty;
		}
		string region = Traduction.Contenu((text3.Length > 0) ? text3 : ((num > 0) ? text.Substring(0, num) : string.Empty));
		Calendrier.Date maintenant = Calendrier.Maintenant;
		string heure = Strings.Banners.PlaceTime.ToString(maintenant.Annee, Calendrier.TexteHeure(maintenant.Heure));
		_annonces?.Lieu(region, titre, map.ZoneType != MapZone.Safe, DiscoverPlace(playerId, text), heure);
	}

	private static bool DiscoverPlace(Guid playerId, string placeName)
	{
		string key = $"LieuxDecouverts_{playerId:N}";
		bool result = true;
		if (!LieuxConnus.TryGetValue(playerId, out HashSet<string> value))
		{
			result = Globals.Database.HasPreference(key);
			value = Globals.Database.LoadPreference(key).Split('|', StringSplitOptions.RemoveEmptyEntries).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
			LieuxConnus[playerId] = value;
		}
		if (!value.Add(placeName))
		{
			return false;
		}
		Globals.Database.SavePreference(key, string.Join('|', value));
		return result;
	}

	public void ShowAchievementBanner(string name, int points)
	{
		_annonces?.Evenement(Strings.Banners.AchievementUnlocked.ToString(), name, Strings.Banners.AchievementPoints.ToString(points), TechniqueArt.Gold, 3000L);
	}

	public void ShowKarmaTierBanner(KarmaTier tier, int karma)
	{
		string titre = ((tier == KarmaTier.Neutral) ? Strings.Karma.BecameNeutral.ToString() : Strings.Karma.BecameTier.ToString(KarmaDisplay.TierName(tier).ToUpperInvariant()));
		_annonces?.Evenement(Strings.Banners.KarmaTag.ToString(), titre, Strings.Karma.BannerValue.ToString(karma), KarmaDisplay.NameColors(tier).Name, 3000L);
	}

	public void ToggleAchievementsWindow()
	{
		if (_achievementsWindow == null)
		{
			_achievementsWindow = new FenetreSucces(GameCanvas);
		}
		_achievementsWindow.Toggle();
	}

	public void ToggleProfessionsWindow()
	{
		if (_professionsWindow == null)
		{
			_professionsWindow = new FenetreMetiers(GameCanvas);
		}
		_professionsWindow.Toggle();
	}

	public void ToggleCalendarWindow()
	{
		if (_calendarWindow == null)
		{
			_calendarWindow = new CalendarWindow(GameCanvas);
		}
		_calendarWindow.Toggle();
	}

	public void ToggleTeachingsScreen()
	{
		if (_teachingsScreen == null)
		{
			_teachingsScreen = new FenetreEnseignements(GameCanvas);
		}
		_teachingsScreen.Toggle();
	}

	public void RefreshAchievementsWindow()
	{
	}

	public void ToggleRankingsScreen()
	{
		if (_rankingsScreen == null)
		{
			_rankingsScreen = new FenetreClassements(GameCanvas);
		}
		_rankingsScreen.Toggle();
	}

	public void ZonesDeLInterface(List<FloatRect> zones)
	{
		zones.Clear();
		if (GameCanvas.IsHidden)
		{
			return;
		}
		Disposition.Occupees(_piecesOccupees);
		float num = GameCanvas.Scale * Graphics.EchelleDeLInterface / Graphics.EchelleDuMonde;
		foreach (Rectangle piecesOccupee in _piecesOccupees)
		{
			zones.Add(new FloatRect((float)piecesOccupee.X * num, (float)piecesOccupee.Y * num, (float)piecesOccupee.Width * num, (float)piecesOccupee.Height * num));
		}
	}

	public void ToggleDailyRewardWindow()
	{
		if (_dailyRewardWindow == null)
		{
			_dailyRewardWindow = new FenetreRecompenses(GameCanvas);
		}
		_dailyRewardWindow.Toggle();
	}

	public void OnDailyRewards(bool canClaim)
	{
		if (canClaim)
		{
			GameMenu?.Notify(MenuBadge.Progression);
			if (_dailyRewardWindow == null)
			{
				_dailyRewardWindow = new FenetreRecompenses(GameCanvas);
			}
			_dailyRewardPending = true;
			_dailyRewardQuietSince = 0L;
			_dailyRewardWindow.Refresh();
		}
		else
		{
			_dailyRewardWindow?.Refresh();
		}
	}

	private void OpenPendingDailyRewards()
	{
		if (!_dailyRewardPending)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		if (EventWindow.Current != null || Globals.EventDialogs.Count > 0)
		{
			_dailyRewardQuietSince = 0L;
		}
		else if (_dailyRewardQuietSince == 0L)
		{
			_dailyRewardQuietSince = milliseconds;
		}
		else
		{
			if (milliseconds - _dailyRewardQuietSince < 2000)
			{
				return;
			}
			_dailyRewardPending = false;
			if (Globals.DailyRewardCanClaim)
			{
				if (_dailyRewardWindow == null)
				{
					_dailyRewardWindow = new FenetreRecompenses(GameCanvas);
				}
				_dailyRewardWindow.Open();
			}
		}
	}

	public void OpenMarketWindow()
	{
		if (_marketWindow == null)
		{
			_marketWindow = new FenetreHdv(GameCanvas);
		}
		if (!_marketWindow.IsOpen)
		{
			_marketWindow.Toggle();
		}
	}

	public void CloseMarketWindow(bool tooFar)
	{
		FenetreHdv marketWindow = _marketWindow;
		if (marketWindow != null && marketWindow.IsOpen)
		{
			_marketWindow.Toggle();
			if (tooFar)
			{
				ChatboxMsg.AddMessage(new ChatboxMsg(Strings.Market.ClosedTooFar, new Color(255, 255, 172, 82), ChatMessageType.Notice));
			}
		}
	}

	public void OnMarketListings(MarketListingsPacket packet)
	{
		_marketWindow?.OnListings(packet);
	}

	public void OnMarketMyListings(MarketMyListingsResultPacket packet)
	{
		_marketWindow?.OnMyListings(packet);
	}

	public void OnMarketResult(MarketResultPacket packet, string text, bool success)
	{
		_marketWindow?.OnResult(packet, text, success);
	}

	public void UpdateAdminWindowMapList()
	{
		mAdminWindow?.UpdateMapList();
	}

	public void Draw(TimeSpan elapsed, TimeSpan total)
	{
		GameCanvas.RenderCanvas(elapsed, total);
	}

	private void CloseShop()
	{
		Globals.GameShop = null;
		_shopWindow?.Hide();
		_shopWindow = null;
		GameMenu?.CloseInventory();
		PacketSender.SendCloseShop();
	}

	private void CloseBank()
	{
		bool flag = _bankWindow != null;
		_bankWindow?.Detach();
		_bankWindow = null;
		Globals.InBank = false;
		if (flag)
		{
			GameMenu?.CloseInventory();
		}
		PacketSender.SendCloseBank();
		_shouldCloseBank = false;
	}

	private void CloseBagWindow()
	{
		_bagWindow?.Hide();
		_bagWindow = null;
		Globals.InBag = false;
		PacketSender.SendCloseBag();
	}

	private void CloseCraftingTable()
	{
		mCraftingWindow?.Close();
		mCraftingWindow?.Detach();
		mCraftingWindow = null;
		Globals.InCraft = false;
		PacketSender.SendCloseCrafting();
	}

	private void CloseTrading()
	{
		mTradingWindow?.Close();
		mTradingWindow = null;
		Globals.InTrade = false;
		PacketSender.SendDeclineTrade();
	}

	public bool CloseAllWindows()
	{
		ConfirmCardWindow current = ConfirmCardWindow.Current;
		if (current != null)
		{
			current.Cancel();
			return true;
		}
		DispositionEditeur dispositionEditeur = _dispositionEditeur;
		if (dispositionEditeur != null && !dispositionEditeur.IsHidden)
		{
			dispositionEditeur.Fermer();
			return true;
		}
		SettingsWindow settingsWindow = _settingsWindow;
		if (settingsWindow != null && !settingsWindow.IsHidden && !settingsWindow.IsListening)
		{
			settingsWindow.Hide();
			return true;
		}
		RequestCardWindow current2 = RequestCardWindow.Current;
		if (current2 != null)
		{
			current2.Decline();
			return true;
		}
		QuantityWindow current3 = QuantityWindow.Current;
		if (current3 != null)
		{
			current3.Cancel();
			return true;
		}
		HellBailWindow current4 = HellBailWindow.Current;
		if (current4 != null)
		{
			current4.Cancel();
			return true;
		}
		TrackingRadarWindow current5 = TrackingRadarWindow.Current;
		if (current5 != null)
		{
			current5.CloseRadar();
			return true;
		}
		WantedBoardWindow current6 = WantedBoardWindow.Current;
		if (current6 != null)
		{
			current6.CloseBoard();
			return true;
		}
		WantedTurnInWindow current7 = WantedTurnInWindow.Current;
		if (current7 != null)
		{
			current7.CloseTurnIn();
			return true;
		}
		RubanRougeWindow current8 = RubanRougeWindow.Current;
		if (current8 != null)
		{
			current8.CloseWindow();
			return true;
		}
		if (HouseInterface.CloseOnEscape())
		{
			return true;
		}
		if (DungeonInterface.CloseOnEscape())
		{
			return true;
		}
		bool result = false;
		WorldMapWindow worldMapWindow = _worldMapWindow;
		if (worldMapWindow != null && !worldMapWindow.IsHidden)
		{
			_worldMapWindow.Close();
			result = true;
		}
		FenetreSucces achievementsWindow = _achievementsWindow;
		if (achievementsWindow != null && !achievementsWindow.IsHidden)
		{
			_achievementsWindow.Hide();
			result = true;
		}
		FenetreMetiers professionsWindow = _professionsWindow;
		if (professionsWindow != null && !professionsWindow.IsHidden)
		{
			_professionsWindow.Close();
			result = true;
		}
		CalendarWindow calendarWindow = _calendarWindow;
		if (calendarWindow != null && !calendarWindow.IsHidden)
		{
			_calendarWindow.Close();
			result = true;
		}
		FenetreEnseignements teachingsScreen = _teachingsScreen;
		if (teachingsScreen != null && !teachingsScreen.IsHidden)
		{
			_teachingsScreen.Hide();
			result = true;
		}
		FenetreClassements rankingsScreen = _rankingsScreen;
		if (rankingsScreen != null && !rankingsScreen.IsHidden)
		{
			_rankingsScreen.Hide();
			result = true;
		}
		FenetreRecompenses dailyRewardWindow = _dailyRewardWindow;
		if (dailyRewardWindow != null && dailyRewardWindow.IsOpen)
		{
			_dailyRewardWindow.Toggle();
			result = true;
		}
		FenetreHdv marketWindow = _marketWindow;
		if (marketWindow != null && marketWindow.IsOpen)
		{
			_marketWindow.Toggle();
			result = true;
		}
		if (IsAdminWindowOpen)
		{
			mAdminWindow?.Hide();
			result = true;
		}
		if (_bagWindow != null && _bagWindow.IsVisibleInTree)
		{
			CloseBagWindow();
			result = true;
		}
		if (mTradingWindow != null && mTradingWindow.IsVisible())
		{
			CloseTrading();
			result = true;
		}
		BankWindow bankWindow = _bankWindow;
		if (bankWindow != null && bankWindow.IsVisibleInTree)
		{
			CloseBank();
			result = true;
		}
		FenetreAtelier fenetreAtelier = mCraftingWindow;
		if (fenetreAtelier != null && fenetreAtelier.IsVisibleInTree && !fenetreAtelier.IsCrafting)
		{
			CloseCraftingTable();
			result = true;
		}
		ShopWindow shopWindow = _shopWindow;
		if (shopWindow != null && shopWindow.IsVisibleInTree)
		{
			CloseShop();
			result = true;
		}
		if (GameMenu != null && GameMenu.HasWindowsOpen())
		{
			GameMenu.CloseAllWindows();
			result = true;
		}
		if (TargetContextMenu.IsVisibleInTree)
		{
			TargetContextMenu.ToggleHidden();
			result = true;
		}
		return result;
	}

	public void Dispose()
	{
		CloseBagWindow();
		CloseBank();
		CloseCraftingTable();
		CloseShop();
		CloseTrading();
		GameCanvas.Dispose();
	}

	public bool HasOpenWindows()
	{
		if (QuantityWindow.Current == null && ConfirmCardWindow.Current == null && HellBailWindow.Current == null && TrackingRadarWindow.Current == null && WantedBoardWindow.Current == null && WantedTurnInWindow.Current == null && RubanRougeWindow.Current == null)
		{
			EscapeMenuWindow escapeMenu = _escapeMenu;
			if (escapeMenu == null || escapeMenu.IsHidden)
			{
				SettingsWindow settingsWindow = _settingsWindow;
				if (settingsWindow == null || !settingsWindow.IsVisibleInTree)
				{
					WorldMapWindow worldMapWindow = _worldMapWindow;
					if (worldMapWindow == null || worldMapWindow.IsHidden)
					{
						FenetreSucces achievementsWindow = _achievementsWindow;
						if (achievementsWindow == null || achievementsWindow.IsHidden)
						{
							FenetreMetiers professionsWindow = _professionsWindow;
							if (professionsWindow == null || professionsWindow.IsHidden)
							{
								CalendarWindow calendarWindow = _calendarWindow;
								if (calendarWindow == null || calendarWindow.IsHidden)
								{
									FenetreRecompenses dailyRewardWindow = _dailyRewardWindow;
									if (dailyRewardWindow == null || !dailyRewardWindow.IsOpen)
									{
										FenetreHdv marketWindow = _marketWindow;
										if (marketWindow == null || !marketWindow.IsOpen)
										{
											BagWindow bagWindow = _bagWindow;
											if (bagWindow == null || !bagWindow.IsVisibleInTree)
											{
												TradingWindow? tradingWindow = mTradingWindow;
												if (tradingWindow == null || !tradingWindow.IsVisible())
												{
													BankWindow bankWindow = _bankWindow;
													if (bankWindow == null || !bankWindow.IsVisibleInTree)
													{
														FenetreAtelier fenetreAtelier = mCraftingWindow;
														if (fenetreAtelier == null || !fenetreAtelier.IsVisibleInTree)
														{
															ShopWindow shopWindow = _shopWindow;
															if (shopWindow == null || !shopWindow.IsVisibleInTree)
															{
																TargetContextMenu targetContextMenu = _targetContextMenu;
																if ((targetContextMenu == null || !targetContextMenu.IsVisibleInTree) && !IsAdminWindowOpen)
																{
																	MenuContainer gameMenu = GameMenu;
																	if (gameMenu == null || !gameMenu.HasWindowsOpen())
																	{
																		return Interface.InputBlockingComponents.Any((Base composant) => !(composant is EventWindow) && composant.IsVisibleInTree && composant.IsBlockingInput);
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
		return true;
	}
}
