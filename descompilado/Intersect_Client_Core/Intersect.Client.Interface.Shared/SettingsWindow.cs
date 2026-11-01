using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Intersect.Client.Core;
using Intersect.Client.Core.Controls;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Database;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Chat;
using Intersect.Client.Localization;
using Intersect.Client.Utilities;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.Entities;
using Intersect.Localization;

namespace Intersect.Client.Interface.Shared;

public class SettingsWindow : Window
{
	private enum Page
	{
		Game,
		Interface,
		Overhead,
		Graphics,
		Sound,
		Keys
	}

	private sealed class Draft
	{
		public bool HideOthers;

		public bool AutoToggleChatLog;

		public bool HealthAsPercentage;

		public bool ManaAsPercentage;

		public bool ExperienceAsPercentage;

		public bool ShowFps;

		public bool ShowPing;

		public bool StickyTarget;

		public bool AutoTurnToTarget;

		public bool AutoSoftRetarget;

		public bool Typewriter;

		public bool HeritageDirectionTenue;

		public bool MyInfo;

		public bool MyBar;

		public bool PlayerInfo;

		public bool PlayerBar;

		public bool FriendInfo;

		public bool FriendBar;

		public bool PartyInfo;

		public bool PartyBar;

		public bool GuildInfo;

		public bool GuildBar;

		public bool NpcInfo;

		public bool NpcBar;

		public bool FullScreen;

		public bool Lighting;

		public bool Blood;

		public bool CameraEffects;

		public bool ScrollZoom;

		public bool AffichageCompatible;

		public bool ModePerformances;

		public int TargetFps;

		public int LangueIndice;

		public bool TraduireLeTchat = true;

		public bool PresenceDiscord = true;

		public void LoadFrom(GameDatabase database)
		{
			HideOthers = database.HideOthersOnWindowOpen;
			AutoToggleChatLog = database.AutoToggleChatLog;
			HealthAsPercentage = database.ShowHealthAsPercentage;
			ManaAsPercentage = database.ShowManaAsPercentage;
			ExperienceAsPercentage = database.ShowExperienceAsPercentage;
			ShowFps = database.ShowFPSCounter;
			ShowPing = database.ShowPingCounter;
			StickyTarget = database.StickyTarget;
			HeritageDirectionTenue = database.HeritageDirectionTenue;
			AutoTurnToTarget = database.AutoTurnToTarget;
			AutoSoftRetarget = database.AutoSoftRetargetOnSelfCast;
			Typewriter = database.TypewriterBehavior == TypewriterBehavior.Word;
			MyInfo = database.MyOverheadInfo;
			MyBar = database.MyOverheadHpBar;
			PlayerInfo = database.PlayerOverheadInfo;
			PlayerBar = database.PlayerOverheadHpBar;
			FriendInfo = database.FriendOverheadInfo;
			FriendBar = database.FriendOverheadHpBar;
			PartyInfo = database.PartyMemberOverheadInfo;
			PartyBar = database.PartyMemberOverheadHpBar;
			GuildInfo = database.GuildMemberOverheadInfo;
			GuildBar = database.GuildMemberOverheadHpBar;
			NpcInfo = database.NpcOverheadInfo;
			NpcBar = database.NpcOverheadHpBar;
			FullScreen = database.FullScreen;
			Lighting = database.EnableLighting;
			Blood = database.ShowBlood;
			CameraEffects = database.CameraEffects;
			AffichageCompatible = database.AffichageCompatible;
			ModePerformances = database.ModePerformances;
			ScrollZoom = database.EnableScrollingWorldZoom;
			TargetFps = database.TargetFps;
			TraduireLeTchat = database.TraduireLeTchat;
			PresenceDiscord = database.PresenceDiscord;
			LangueIndice = Math.Max(0, Langue.Indice(Langue.Choisie));
		}

		public void ApplyTo(GameDatabase database)
		{
			database.HideOthersOnWindowOpen = HideOthers;
			database.AutoToggleChatLog = AutoToggleChatLog;
			database.SimplifiedEscapeMenu = false;
			database.ShowHealthAsPercentage = HealthAsPercentage;
			database.ShowManaAsPercentage = ManaAsPercentage;
			database.ShowExperienceAsPercentage = ExperienceAsPercentage;
			database.ShowFPSCounter = ShowFps;
			database.ShowPingCounter = ShowPing;
			database.StickyTarget = StickyTarget;
			database.HeritageDirectionTenue = HeritageDirectionTenue;
			database.AutoTurnToTarget = AutoTurnToTarget;
			database.AutoSoftRetargetOnSelfCast = AutoSoftRetarget;
			database.TypewriterBehavior = (Typewriter ? TypewriterBehavior.Word : TypewriterBehavior.Off);
			database.MyOverheadInfo = MyInfo;
			database.MyOverheadHpBar = MyBar;
			database.PlayerOverheadInfo = PlayerInfo;
			database.PlayerOverheadHpBar = PlayerBar;
			database.FriendOverheadInfo = FriendInfo;
			database.FriendOverheadHpBar = FriendBar;
			database.PartyMemberOverheadInfo = PartyInfo;
			database.PartyMemberOverheadHpBar = PartyBar;
			database.GuildMemberOverheadInfo = GuildInfo;
			database.GuildMemberOverheadHpBar = GuildBar;
			database.NpcOverheadInfo = NpcInfo;
			database.NpcOverheadHpBar = NpcBar;
			database.EnableLighting = Lighting;
			database.ShowBlood = Blood;
			database.CameraEffects = CameraEffects;
			database.AffichageCompatible = AffichageCompatible;
			database.ModePerformances = ModePerformances;
			database.EnableScrollingWorldZoom = ScrollZoom;
			database.TraduireLeTchat = TraduireLeTchat;
			database.PresenceDiscord = PresenceDiscord;
		}
	}

	private const int WindowWidth = 760;

	private const int WindowHeight = 600;

	private const int ContentX = 216;

	private const int ContentWidth = 528;

	private const int CardTop = 126;

	private const int CardHeight = 412;

	private const int FooterY = 548;

	private const int ScrollbarRoom = 14;

	private const long ListeningMs = 5000L;

	private const long NoticeMs = 7000L;

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private static readonly Color Entry = new Color(255, 196, 204, 220);

	private static readonly Color OrangeLight = new Color(255, 255, 172, 82);

	private static readonly Color Dim = new Color(255, 150, 160, 182);

	private static readonly Control[] MovementKeys = new Control[6]
	{
		Control.MoveUp,
		Control.MoveDown,
		Control.MoveLeft,
		Control.MoveRight,
		Control.Sprint,
		Control.TurnAround
	};

	private static readonly Control[] CombatKeys = new Control[9]
	{
		Control.AttackInteract,
		Control.Block,
		Control.Dodge,
		Control.Dash,
		Control.ChargeKi,
		Control.AutoTarget,
		Control.HoldToSoftRetargetOnSelfCast,
		Control.ToggleAutoSoftRetargetOnSelfCast,
		Control.PickUp
	};

	private static readonly Control[] WindowKeys = new Control[18]
	{
		Control.OpenMenu,
		Control.OpenInventory,
		Control.OpenSpells,
		Control.OpenCharacterInfo,
		Control.OpenQuests,
		Control.OpenWorldMap,
		Control.ShowLocalMap,
		Control.OpenAchievements,
		Control.OpenDailyRewards,
		Control.OpenProfessions,
		Control.OpenCalendar,
		Control.OpenTeachings,
		Control.OpenRankings,
		Control.OpenMarket,
		Control.OpenFriends,
		Control.OpenParties,
		Control.OpenGuild,
		Control.OpenSettings
	};

	private static readonly Control[] SystemKeys = new Control[10]
	{
		Control.Enter,
		Control.Screenshot,
		Control.ToggleFullscreen,
		Control.ToggleGui,
		Control.ToggleZoomIn,
		Control.ToggleZoomOut,
		Control.HoldToZoomIn,
		Control.HoldToZoomOut,
		Control.OpenAdminPanel,
		Control.OpenDebugger
	};

	private readonly Label _title;

	private readonly Button _close;

	private readonly OptionsSidebar _sidebar;

	private readonly OptionsHeader _header;

	private readonly ImagePanel _card;

	private readonly ImagePanel _previewCard;

	private readonly OverheadPreview _preview;

	private readonly OptionsKeyColumns _keyColumns;

	private readonly OptionsKeyNotice _keyNotice;

	private readonly Dictionary<Page, (ScrollControl Scroll, OptionsSheet Sheet)> _pages = new Dictionary<Page, (ScrollControl, OptionsSheet)>();

	private readonly Button _restoreButton;

	private readonly Button _cancelButton;

	private readonly Button _applyButton;

	private readonly ComboBox _resolutionList;

	private readonly Slider _worldScale;

	private readonly Slider _musicSlider;

	private readonly Slider _soundSlider;

	private readonly Draft _draft = new Draft();

	private readonly HashSet<Keys> _keysDown = new HashSet<Keys>();

	private Keys _pressedMouseKey = Keys.LButton;

	private MenuItem? _customResolutionMenuItem;

	private Controls? _keybindingEditControls;

	private (Control Control, int Slot)? _listening;

	private (Keys Modifier, Keys Key) _listeningPrevious;

	private long _listeningUntil;

	private long _noticeUntil;

	private int _previousMusicVolume;

	private int _previousSoundVolume;

	private int _laidOutTitleBarHeight = -1;

	private Page _page;

	private Base? _returnTo;

	public override bool IsBlockingInput => _listening.HasValue;

	public bool IsListening => _listening.HasValue;

	public SettingsWindow(Base parent)
		: base(parent, Strings.Settings.Title, modal: false, "SettingsWindow")
	{
		Interface.InputBlockingComponents.Add(this);
		DisableResizing();
		base.IsClosable = false;
		base.Titlebar.MouseInputEnabled = false;
		base.Titlebar.ShouldDrawBackground = false;
		base.TitleLabel.IsHidden = true;
		base.MinimumSize = new Point(760, 600);
		SetSize(760, 600);
		base.Alignment = new Alignments[1] { Alignments.Center };
		IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_window_marche.png");
		SetImage(texture, "saiyan_window_marche.png", ControlState.Active);
		SetImage(texture, "saiyan_window_marche.png", ControlState.Inactive);
		_title = new Label(this, "OptionsTitle")
		{
			Font = GameContentManager.Current.GetFont("refuel"),
			FontSize = 20,
			AutoSizeToContents = false,
			MouseInputEnabled = false,
			TextAlign = (Pos.Left | Pos.CenterV),
			Text = Strings.Settings.Title.ToString().ToUpperInvariant()
		};
		_title.SetTextColor(OptionsStyle.White, ComponentState.Normal);
		WindowTitle.Apply(_title);
		_close = new Button(this, "OptionsClose", disableText: true);
		_close.SetStateTexture(ComponentState.Normal, "saiyan_close.png");
		_close.SetStateTexture(ComponentState.Hovered, "saiyan_close_hover.png");
		_close.SetStateTexture(ComponentState.Active, "saiyan_close_clicked.png");
		_close.Clicked += delegate
		{
			CancelPendingChanges();
		};
		_sidebar = new OptionsSidebar(this, "OptionsSidebar")
		{
			Heading = Strings.OptionsWindow.Categories,
			Items = new(string, string)[6]
			{
				(Strings.OptionsWindow.CategoryGame, "jeu"),
				(Strings.OptionsWindow.CategoryInterface, "interface"),
				(Strings.OptionsWindow.CategoryOverhead, "tetes"),
				(Strings.OptionsWindow.CategoryGraphics, "graphismes"),
				(Strings.OptionsWindow.CategorySound, "son"),
				(Strings.OptionsWindow.CategoryKeys, "touches")
			},
			Picked = delegate(int index)
			{
				ShowPage((Page)index);
			}
		};
		_header = new OptionsHeader(this, "OptionsHeader");
		_card = MakeCard("OptionsCard");
		_previewCard = MakeCard("OptionsPreviewCard");
		_preview = new OverheadPreview(_previewCard, "OptionsPreview")
		{
			Heading = Strings.OptionsWindow.Preview,
			State = PreviewState,
			HoveredCaption = (string text) => Strings.OptionsWindow.PreviewHovered.ToString(text),
			VisibilityCaption = delegate(bool name, bool bar)
			{
				LocalizedString localizedString = (name ? ((!bar) ? Strings.OptionsWindow.PreviewName : Strings.OptionsWindow.PreviewBoth) : ((!bar) ? Strings.OptionsWindow.PreviewNothing : Strings.OptionsWindow.PreviewBar));
				return localizedString;
			}
		};
		Page[] values = Enum.GetValues<Page>();
		foreach (Page page in values)
		{
			ScrollControl scrollControl = new ScrollControl(this, $"Options{page}Scroll");
			StyleScroll(scrollControl);
			OptionsSheet item = new OptionsSheet(scrollControl, $"Options{page}Sheet");
			_pages[page] = (scrollControl, item);
		}
		OptionsSheet item2 = _pages[Page.Keys].Sheet;
		item2.KeyLabel = KeyLabel;
		item2.IsListening = delegate(Control control, int slot)
		{
			(Control, int)? listening = _listening;
			if (listening.HasValue)
			{
				(Control, int) valueOrDefault = listening.GetValueOrDefault();
				if (valueOrDefault.Item1 == control)
				{
					return valueOrDefault.Item2 == slot;
				}
			}
			return false;
		};
		item2.KeyPicked = OnKeyChipClicked;
		_keyColumns = new OptionsKeyColumns(this, "OptionsKeyColumns")
		{
			Action = Strings.OptionsWindow.KeysAction,
			Primary = Strings.OptionsWindow.KeysPrimary,
			Secondary = Strings.OptionsWindow.KeysSecondary
		};
		_keyNotice = new OptionsKeyNotice(this, "OptionsKeyNotice")
		{
			Hint = Strings.OptionsWindow.KeysHint
		};
		OptionsSheet item3 = _pages[Page.Graphics].Sheet;
		_resolutionList = new ComboBox(item3, "OptionsResolution");
		string[] array = Graphics.Renderer?.ValidVideoModes?.ToArray() ?? Array.Empty<string>();
		for (int num2 = 0; num2 < array.Length; num2++)
		{
			string[] source = array[num2].Split(',');
			string label = Strings.Settings.FormatResolution.ToString(source.Cast<object>().ToArray());
			MenuItem menuItem = _resolutionList.AddItem(label, null, num2);
			menuItem.TextAlign = Pos.Left;
		}
		_worldScale = MakeSlider(item3, "OptionsWorldScale");
		_worldScale.SnapToNotches = true;
		OptionsSheet item4 = _pages[Page.Sound].Sheet;
		_musicSlider = MakeSlider(item4, "OptionsMusic");
		_musicSlider.SetRange(0.0, 100.0);
		_musicSlider.ValueChanged += delegate(Base _, ValueChangedEventArgs<double> args)
		{
			Globals.Database.MusicVolume = (int)args.Value;
			Audio.UpdateGlobalVolume();
		};
		_soundSlider = MakeSlider(item4, "OptionsSound");
		_soundSlider.SetRange(0.0, 100.0);
		_soundSlider.ValueChanged += delegate(Base _, ValueChangedEventArgs<double> args)
		{
			Globals.Database.SoundVolume = (int)args.Value;
			Audio.UpdateGlobalVolume();
		};
		_restoreButton = MakeButton("OptionsRestore", "saiyan_btn_176x34", Entry);
		_restoreButton.Clicked += delegate
		{
			RestoreDefaults();
		};
		_cancelButton = MakeButton("OptionsCancel", "saiyan_btn_128x34", Entry);
		_cancelButton.Text = Strings.Settings.Cancel;
		_cancelButton.Clicked += delegate
		{
			CancelPendingChanges();
		};
		_applyButton = MakeButton("OptionsApply", "saiyan_cta_180x34", Night);
		_applyButton.Font = OptionsStyle.Strong;
		_applyButton.FontSize = 13;
		_applyButton.SetTextColor(Night, ComponentState.Hovered);
		_applyButton.Text = Strings.Settings.Apply.ToString().ToUpperInvariant();
		_applyButton.Clicked += delegate
		{
			ApplyPendingChanges();
		};
		Input.KeyDown = (Input.HandleKeyEvent)Delegate.Combine(Input.KeyDown, new Input.HandleKeyEvent(OnKeyDown));
		Input.MouseDown = (Input.HandleKeyEvent)Delegate.Combine(Input.MouseDown, new Input.HandleKeyEvent(OnKeyDown));
		Input.KeyUp = (Input.HandleKeyEvent)Delegate.Combine(Input.KeyUp, new Input.HandleKeyEvent(OnKeyUp));
		Input.MouseUp = (Input.HandleKeyEvent)Delegate.Combine(Input.MouseUp, new Input.HandleKeyEvent(OnKeyUp));
		BuildPages();
		Layout();
		ShowPage(_page);
	}

	protected override void EnsureInitialized()
	{
		Layout();
		SaiyanDropdowns.Apply(this);
	}

	public override void Think()
	{
		base.Think();
		if (!base.IsHidden)
		{
			if (base.TitleBarBounds.Height != _laidOutTitleBarHeight)
			{
				Layout();
			}
			Update();
		}
	}

	public void Update()
	{
		if (!base.IsHidden)
		{
			if (_listening.HasValue && Timing.Global.MillisecondsUtc > _listeningUntil)
			{
				StopListening(restore: true);
			}
			if (_keyNotice.Notice != null && Timing.Global.MillisecondsUtc > _noticeUntil)
			{
				_keyNotice.Notice = null;
			}
		}
	}

	public override void Show()
	{
		Show(null);
	}

	public void Show(Base? returnTo)
	{
		if (Globals.GameState == GameStates.InGame)
		{
			MakeModal(dim: true);
			Canvas canvas = base.Canvas;
			if (canvas != null)
			{
				SetPosition(Math.Max(0, (canvas.Width - 760) / 2), Math.Max(0, (canvas.Height - 600) / 2));
			}
		}
		_draft.LoadFrom(Globals.Database);
		_previousMusicVolume = Globals.Database.MusicVolume;
		_previousSoundVolume = Globals.Database.SoundVolume;
		_musicSlider.SetValue(Globals.Database.MusicVolume, skipEvents: true);
		_soundSlider.SetValue(Globals.Database.SoundVolume, skipEvents: true);
		if ((Graphics.Renderer?.ValidVideoModes?.Count).GetValueOrDefault() > 0)
		{
			if (Graphics.Renderer.HasOverrideResolution)
			{
				if (_customResolutionMenuItem == null)
				{
					_customResolutionMenuItem = _resolutionList.AddItem(Strings.Settings.ResolutionCustom, null, -1);
				}
				_customResolutionMenuItem.Show();
				_resolutionList.SelectedItem = _customResolutionMenuItem;
			}
			else
			{
				_resolutionList.SelectByUserData(Globals.Database.TargetResolution);
			}
		}
		_keybindingEditControls = new Controls(Controls.ActiveControls);
		_listening = null;
		_keyNotice.Notice = null;
		UpdateWorldScaleControls();
		BuildPages();
		base.Show();
		Layout();
		ShowPage(_page);
		SaiyanDropdowns.Apply(this);
		_returnTo = returnTo;
	}

	public override void Hide()
	{
		StopListening(restore: true);
		base.Hide();
		RemoveModal();
		_returnTo?.Show();
		_returnTo = null;
	}

	private void BuildPages()
	{
		SetSheet(Page.Game, GameRows());
		SetSheet(Page.Interface, InterfaceRows());
		SetSheet(Page.Overhead, OverheadRows());
		SetSheet(Page.Graphics, GraphicsRows());
		SetSheet(Page.Sound, SoundRows());
		SetSheet(Page.Keys, KeyRows());
	}

	private void SetSheet(Page page, List<OptionRow> rows)
	{
		(ScrollControl Scroll, OptionsSheet Sheet) tuple = _pages[page];
		ScrollControl item = tuple.Scroll;
		OptionsSheet item2 = tuple.Sheet;
		(int X, int Y, int Width, int Height) tuple2 = ScrollBounds(page);
		int item3 = tuple2.Width;
		int item4 = tuple2.Height;
		bool flag = OptionsSheet.MeasureHeight(rows) > item4;
		item2.SetRows(rows, flag ? (item3 - 14) : item3);
		item2.SetPosition(0, 0);
		if (page == Page.Keys)
		{
			_keyColumns.SheetWidth = item2.Width;
		}
		item.UpdateScrollBars();
	}

	private List<OptionRow> GameRows()
	{
		List<OptionRow> list = new List<OptionRow>();
		list.Add(Section(Strings.OptionsWindow.SectionLanguage));
		list.Add(LanguageRow(() => _draft.LangueIndice, delegate(int value)
		{
			_draft.LangueIndice = value;
		}));
		list.Add(Toggle(Strings.Tchat.OptionTraduire, () => _draft.TraduireLeTchat, delegate(bool value)
		{
			_draft.TraduireLeTchat = value;
		}, Strings.Tchat.OptionTraduireAide));
		list.Add(Toggle(Strings.PresenceDiscord.Option, () => _draft.PresenceDiscord, delegate(bool value)
		{
			_draft.PresenceDiscord = value;
		}, Strings.PresenceDiscord.OptionAide));
		list.Add(Section(Strings.OptionsWindow.SectionTargeting));
		list.Add(Toggle(Strings.Settings.StickyTarget, () => _draft.StickyTarget, delegate(bool value)
		{
			_draft.StickyTarget = value;
		}, Strings.OptionsWindow.StickyTargetHelp));
		list.Add(Toggle(Strings.Settings.AutoTurnToTarget, () => _draft.AutoTurnToTarget, delegate(bool value)
		{
			_draft.AutoTurnToTarget = value;
		}, null, () => ServerReason(Options.IsLoaded && Options.Instance?.Player?.EnableAutoTurnToTarget == true)));
		list.Add(Toggle(Strings.Settings.AutoSoftRetargetOnSelfCast, () => _draft.AutoSoftRetarget, delegate(bool value)
		{
			_draft.AutoSoftRetarget = value;
		}, Strings.OptionsWindow.AutoSoftRetargetHelp, () => ServerReason(Options.IsLoaded && Options.Instance?.Combat?.EnableAutoSelfCastFriendlySpellsWhenTargetingHostile == true)));
		list.AddRange(LignesHeritage());
		list.Add(Section(Strings.OptionsWindow.SectionDialogues));
		list.Add(Toggle(Strings.Settings.TypewriterText, () => _draft.Typewriter, delegate(bool value)
		{
			_draft.Typewriter = value;
		}));
		return list;
	}

	private List<OptionRow> InterfaceRows()
	{
		int num = 10;
		List<OptionRow> list = new List<OptionRow>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<OptionRow> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = Section(Strings.OptionsWindow.SectionWindows);
		num2++;
		span[num2] = Toggle(Strings.Settings.AutoCloseWindows, () => _draft.HideOthers, delegate(bool value)
		{
			_draft.HideOthers = value;
		}, Strings.OptionsWindow.AutoCloseWindowsHelp);
		num2++;
		span[num2] = Toggle(Strings.Settings.AutoToggleChatLog, () => _draft.AutoToggleChatLog, delegate(bool value)
		{
			_draft.AutoToggleChatLog = value;
		});
		num2++;
		span[num2] = Section(Strings.OptionsWindow.SectionGauges);
		num2++;
		span[num2] = Toggle(Strings.Settings.ShowHealthAsPercentage, () => _draft.HealthAsPercentage, delegate(bool value)
		{
			_draft.HealthAsPercentage = value;
		});
		num2++;
		span[num2] = Toggle(Strings.Settings.ShowManaAsPercentage, () => _draft.ManaAsPercentage, delegate(bool value)
		{
			_draft.ManaAsPercentage = value;
		});
		num2++;
		span[num2] = Toggle(Strings.Settings.ShowExperienceAsPercentage, () => _draft.ExperienceAsPercentage, delegate(bool value)
		{
			_draft.ExperienceAsPercentage = value;
		});
		num2++;
		span[num2] = Section(Strings.OptionsWindow.SectionCounters);
		num2++;
		span[num2] = Toggle(Strings.Settings.ShowFPSCounter, () => _draft.ShowFps, delegate(bool value)
		{
			_draft.ShowFps = value;
			Interface.ShowStatisticsPanelFPS = value;
		});
		num2++;
		span[num2] = Toggle(Strings.Settings.ShowPingCounter, () => _draft.ShowPing, delegate(bool value)
		{
			_draft.ShowPing = value;
			Interface.ShowStatisticsPanelPing = value;
		});
		num2++;
		return list;
	}

	private List<OptionRow> OverheadRows()
	{
		int num = 7;
		List<OptionRow> list = new List<OptionRow>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<OptionRow> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = new OptionRow
		{
			Kind = OptionRowKind.PairHeader,
			Label = Strings.OptionsWindow.OverheadName,
			Help = Strings.OptionsWindow.OverheadBar,
			Height = 28
		};
		num2++;
		span[num2] = Pair(Strings.OptionsWindow.OverheadMe, Strings.OptionsWindow.OverheadMeHelp, OptionsStyle.OrangeLight, () => _draft.MyInfo, delegate(bool value)
		{
			_draft.MyInfo = value;
		}, () => _draft.MyBar, delegate(bool value)
		{
			_draft.MyBar = value;
		});
		num2++;
		span[num2] = Pair(Strings.OptionsWindow.OverheadPlayers, Strings.OptionsWindow.OverheadPlayersHelp, OptionsStyle.White, () => _draft.PlayerInfo, delegate(bool value)
		{
			_draft.PlayerInfo = value;
		}, () => _draft.PlayerBar, delegate(bool value)
		{
			_draft.PlayerBar = value;
		});
		num2++;
		span[num2] = Pair(Strings.OptionsWindow.OverheadFriends, null, new Color(255, 140, 235, 170), () => _draft.FriendInfo, delegate(bool value)
		{
			_draft.FriendInfo = value;
		}, () => _draft.FriendBar, delegate(bool value)
		{
			_draft.FriendBar = value;
		});
		num2++;
		span[num2] = Pair(Strings.OptionsWindow.OverheadParty, null, new Color(255, 140, 235, 170), () => _draft.PartyInfo, delegate(bool value)
		{
			_draft.PartyInfo = value;
		}, () => _draft.PartyBar, delegate(bool value)
		{
			_draft.PartyBar = value;
		});
		num2++;
		span[num2] = Pair(Strings.OptionsWindow.OverheadGuild, null, new Color(255, 245, 190, 90), () => _draft.GuildInfo, delegate(bool value)
		{
			_draft.GuildInfo = value;
		}, () => _draft.GuildBar, delegate(bool value)
		{
			_draft.GuildBar = value;
		});
		num2++;
		span[num2] = Pair(Strings.OptionsWindow.OverheadNpcs, Strings.OptionsWindow.OverheadNpcsHelp, new Color(255, 255, 120, 110), () => _draft.NpcInfo, delegate(bool value)
		{
			_draft.NpcInfo = value;
		}, () => _draft.NpcBar, delegate(bool value)
		{
			_draft.NpcBar = value;
		});
		num2++;
		return list;
	}

	private List<OptionRow> GraphicsRows()
	{
		int num = 12;
		List<OptionRow> list = new List<OptionRow>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<OptionRow> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = Section(Strings.OptionsWindow.SectionScreen);
		num2++;
		span[num2] = new OptionRow
		{
			Kind = OptionRowKind.Combo,
			Label = Strings.Settings.Resolution,
			AppliesOnConfirm = true,
			Widget = _resolutionList,
			WidgetWidth = 172
		};
		num2++;
		span[num2] = Toggle(Strings.Settings.Fullscreen, () => _draft.FullScreen, delegate(bool value)
		{
			_draft.FullScreen = value;
		}, null, null, confirm: true);
		num2++;
		span[num2] = Toggle(Strings.OptionsWindow.CompatibleRendering, () => _draft.AffichageCompatible, delegate(bool value)
		{
			_draft.AffichageCompatible = value;
		}, Strings.OptionsWindow.CompatibleRenderingHelp);
		num2++;
		span[num2] = Toggle(Strings.OptionsWindow.PerformanceMode, () => _draft.ModePerformances, delegate(bool value)
		{
			_draft.ModePerformances = value;
		}, Strings.OptionsWindow.PerformanceModeHelp);
		num2++;
		span[num2] = Section(Strings.OptionsWindow.SectionEffects);
		num2++;
		span[num2] = Toggle(Strings.Settings.EnableLighting, () => _draft.Lighting, delegate(bool value)
		{
			_draft.Lighting = value;
		});
		num2++;
		span[num2] = Toggle(Strings.Settings.ShowBlood, () => _draft.Blood, delegate(bool value)
		{
			_draft.Blood = value;
		});
		num2++;
		span[num2] = Toggle(Strings.OptionsWindow.CameraEffects, () => _draft.CameraEffects, delegate(bool value)
		{
			_draft.CameraEffects = value;
		}, Strings.OptionsWindow.CameraEffectsHelp);
		num2++;
		span[num2] = Section(Strings.OptionsWindow.SectionCamera);
		num2++;
		span[num2] = Toggle(Strings.OptionsWindow.ScrollZoom, () => _draft.ScrollZoom, delegate(bool value)
		{
			_draft.ScrollZoom = value;
		}, Strings.OptionsWindow.ScrollZoomHelp);
		num2++;
		span[num2] = new OptionRow
		{
			Kind = OptionRowKind.Slider,
			Label = Strings.Settings.WorldScale,
			Height = 40,
			Widget = _worldScale,
			WidgetWidth = 180,
			Unavailable = () => (!Options.IsLoaded) ? Strings.OptionsWindow.AvailableOnceConnected.ToString() : null,
			Value = () => Strings.Settings.FormatZoom.ToString(_worldScale.Value),
			Notches = () => _worldScale.Notches
		};
		num2++;
		return list;
	}

	private List<OptionRow> SoundRows()
	{
		int num = 3;
		List<OptionRow> list = new List<OptionRow>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<OptionRow> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = Section(Strings.Settings.SectionVolume.ToString().ToUpperInvariant());
		num2++;
		span[num2] = new OptionRow
		{
			Kind = OptionRowKind.Slider,
			Label = Strings.Settings.VolumeMusic,
			Height = 52,
			Widget = _musicSlider,
			WidgetWidth = 260,
			Value = () => Strings.OptionsWindow.VolumeValue.ToString((int)Math.Round(_musicSlider.Value))
		};
		num2++;
		span[num2] = new OptionRow
		{
			Kind = OptionRowKind.Slider,
			Label = Strings.Settings.VolumeSoundEffects,
			Height = 52,
			Widget = _soundSlider,
			WidgetWidth = 260,
			Value = () => Strings.OptionsWindow.VolumeValue.ToString((int)Math.Round(_soundSlider.Value))
		};
		num2++;
		return list;
	}

	private List<OptionRow> KeyRows()
	{
		IControlSet keybindingEditControls = _keybindingEditControls;
		IReadOnlyDictionary<Control, ControlMapping> mappings = (keybindingEditControls ?? Controls.ActiveControls).Mappings;
		HashSet<Control> placed = new HashSet<Control>();
		List<OptionRow> rows = new List<OptionRow>();
		Group(Strings.OptionsWindow.SectionMovement, MovementKeys);
		Group(Strings.OptionsWindow.SectionCombat, CombatKeys);
		Group(Strings.OptionsWindow.SectionWindows, WindowKeys);
		Group(Strings.OptionsWindow.SectionHotkeys, from control in mappings.Keys
			where control > Control.HotkeyOffset
			orderby control
			select control);
		Group(Strings.OptionsWindow.SectionSystem, SystemKeys);
		Group(Strings.OptionsWindow.SectionOther, mappings.Keys.OrderBy((Control control) => control));
		return rows;
		void Group(string title, IEnumerable<Control> controls)
		{
			List<Control> list = controls.Where((Control control) => mappings.ContainsKey(control) && placed.Add(control)).ToList();
			if (list.Count != 0)
			{
				rows.Add(Section(title));
				rows.AddRange(list.Select((Control control) => new OptionRow
				{
					Kind = OptionRowKind.Keys,
					Label = ControlName(control),
					KeyControl = control,
					Height = 34
				}));
			}
		}
	}

	private void ShowPage(Page page)
	{
		StopListening(restore: true);
		_page = page;
		_sidebar.Selected = (int)page;
		foreach (KeyValuePair<Page, (ScrollControl, OptionsSheet)> page3 in _pages)
		{
			page3.Deconstruct(out var key, out var value);
			(ScrollControl, OptionsSheet) tuple = value;
			Page page2 = key;
			ScrollControl item = tuple.Item1;
			item.IsHidden = page2 != page;
		}
		bool flag = page == Page.Overhead;
		_previewCard.IsHidden = !flag;
		_keyColumns.IsHidden = page != Page.Keys;
		_keyNotice.IsHidden = page != Page.Keys;
		string item2 = _sidebar.Items[(int)page].Label;
		_header.Title = item2.ToUpperInvariant();
		_header.DescriptionEnd = string.Empty;
		OptionsHeader header = _header;
		header.Description = page switch
		{
			Page.Game => Strings.OptionsWindow.DescriptionGame, 
			Page.Interface => Strings.OptionsWindow.DescriptionInterface, 
			Page.Overhead => Strings.OptionsWindow.DescriptionOverhead, 
			Page.Graphics => Strings.OptionsWindow.DescriptionGraphics, 
			Page.Sound => Strings.OptionsWindow.DescriptionSound, 
			_ => Strings.OptionsWindow.DescriptionKeys, 
		};
		if (page == Page.Graphics)
		{
			_header.DescriptionEnd = Strings.OptionsWindow.DescriptionGraphicsEnd;
		}
		_restoreButton.Text = ((page == Page.Keys) ? Strings.OptionsWindow.RestoreKeys : Strings.Settings.Restore);
		Layout();
	}

	private new int Y(int windowY)
	{
		return windowY - _laidOutTitleBarHeight;
	}

	private (int X, int Y, int Width, int Height) ScrollBounds(Page page)
	{
		return page switch
		{
			Page.Overhead => (X: 218, Y: 130, Width: 318, Height: 404), 
			Page.Keys => (X: 218, Y: 162, Width: 524, Height: 326), 
			_ => (X: 218, Y: 130, Width: 524, Height: 404), 
		};
	}

	private void Layout()
	{
		_laidOutTitleBarHeight = base.TitleBarBounds.Height;
		_title.SetSize(400, 38);
		_title.SetPosition(26, Y(10));
		_close.SetSize(24, 24);
		_close.SetPosition(720, Y(16));
		_sidebar.SetBounds(16, Y(72), 184, 512);
		_header.SetBounds(218, Y(70), 526, 50);
		bool flag = _page == Page.Overhead;
		_card.SetBounds(216, Y(126), flag ? 322 : 528, 412);
		_previewCard.SetBounds(546, Y(126), 198, 412);
		_preview.SetBounds(0, 0, 198, 412);
		foreach (KeyValuePair<Page, (ScrollControl, OptionsSheet)> page2 in _pages)
		{
			page2.Deconstruct(out var key, out var value);
			(ScrollControl, OptionsSheet) tuple = value;
			Page page = key;
			var (scrollControl, optionsSheet) = tuple;
			var (x, windowY, width, height) = ScrollBounds(page);
			scrollControl.SetBounds(x, Y(windowY), width, height);
			optionsSheet.PlaceWidgets();
		}
		_keyColumns.SetBounds(218, Y(134), 524, 24);
		_keyNotice.SetBounds(224, Y(494), 512, 36);
		_restoreButton.SetPosition(216, Y(548));
		_cancelButton.SetPosition(426, Y(548));
		_applyButton.SetPosition(564, Y(548));
		_card.SendToBack();
		_previewCard.SendToBack();
	}

	private void UpdateWorldScaleControls()
	{
		if (!Options.IsLoaded)
		{
			_worldScale.IsHidden = true;
			return;
		}
		_worldScale.IsHidden = false;
		List<double> list = (from n in new double[3] { 1.0, 2.0, 4.0 }
			select n * (double)(Options.Instance?.Map?.MinimumWorldScale ?? 1f) into n
			where n >= (double)Graphics.MinimumWorldScale
			select n).ToList();
		if (list.Count == 0)
		{
			list.Add(Graphics.MinimumWorldScale);
		}
		double item = 1.5;
		if (!list.Contains(item))
		{
			list.Add(item);
			list.Sort();
		}
		while (list.Last() < (double)Graphics.MaximumWorldScale)
		{
			list.Add(list.Last() * 2.0);
		}
		Globals.Database.WorldZoom = (float)MathHelper.Clamp(Globals.Database.WorldZoom, list.Min(), list.Max());
		_worldScale.SetRange(list.Min(), list.Max());
		_worldScale.Notches = list.ToArray();
		_worldScale.SetValue(Globals.Database.WorldZoom, skipEvents: true);
	}

	private void ApplyPendingChanges()
	{
		StopListening(restore: true);
		GameDatabase database = Globals.Database;
		bool flag = false;
		_draft.ApplyTo(database);
		ChatboxMsg.AppliquerLeReglage();
		(string, string)[] disponibles = Langue.Disponibles;
		string item = disponibles[Math.Clamp(_draft.LangueIndice, 0, disponibles.Length - 1)].Item1;
		bool flag2 = item != Langue.Choisie;
		if (flag2)
		{
			Langue.Choisir(item);
		}
		if (Options.IsLoaded && !_worldScale.IsHidden)
		{
			database.WorldZoom = (float)_worldScale.Value;
		}
		int num = (_resolutionList.SelectedItem?.UserData as int?) ?? (-1);
		if (num > -1)
		{
			flag = database.TargetResolution != num || (Graphics.Renderer?.HasOverrideResolution ?? false);
			database.TargetResolution = num;
		}
		if (database.FullScreen != _draft.FullScreen)
		{
			database.FullScreen = _draft.FullScreen;
			flag = true;
		}
		if (database.TargetFps != _draft.TargetFps)
		{
			database.TargetFps = _draft.TargetFps;
			flag = true;
		}
		database.MusicVolume = (int)_musicSlider.Value;
		database.SoundVolume = (int)_soundSlider.Value;
		Audio.UpdateGlobalVolume();
		IControlSet keybindingEditControls = _keybindingEditControls;
		IControlSet controlSet = (Controls.ActiveControls = keybindingEditControls ?? Controls.ActiveControls);
		controlSet.TrySave();
		database.SavePreferences();
		if (flag && Graphics.Renderer != null)
		{
			_customResolutionMenuItem?.Hide();
			Graphics.Renderer.OverrideResolution = Resolution.Empty;
			Graphics.Renderer.Init();
		}
		Hide();
		if (flag2 && item != Langue.Actuelle)
		{
			Interface.ShowAlert(Strings.OptionsWindow.LanguageRestart, Strings.OptionsWindow.Language, AlertType.Information);
		}
	}

	private void CancelPendingChanges()
	{
		StopListening(restore: true);
		Globals.Database.MusicVolume = _previousMusicVolume;
		Globals.Database.SoundVolume = _previousSoundVolume;
		Interface.ShowStatisticsPanelFPS = Globals.Database.ShowFPSCounter;
		Interface.ShowStatisticsPanelPing = Globals.Database.ShowPingCounter;
		Audio.UpdateGlobalVolume();
		_keybindingEditControls = new Controls(Controls.ActiveControls);
		Hide();
	}

	private void RestoreDefaults()
	{
		StopListening(restore: false);
		switch (_page)
		{
		case Page.Game:
			_draft.StickyTarget = false;
			_draft.HeritageDirectionTenue = false;
			_draft.AutoTurnToTarget = false;
			_draft.AutoSoftRetarget = false;
			_draft.Typewriter = true;
			_draft.TraduireLeTchat = true;
			_draft.PresenceDiscord = true;
			break;
		case Page.Interface:
			_draft.HideOthers = true;
			_draft.AutoToggleChatLog = false;
			_draft.HealthAsPercentage = false;
			_draft.ManaAsPercentage = false;
			_draft.ExperienceAsPercentage = true;
			_draft.ShowFps = false;
			_draft.ShowPing = false;
			Interface.ShowStatisticsPanelFPS = false;
			Interface.ShowStatisticsPanelPing = false;
			break;
		case Page.Overhead:
			_draft.MyInfo = (_draft.PlayerInfo = (_draft.FriendInfo = (_draft.PartyInfo = (_draft.GuildInfo = (_draft.NpcInfo = false)))));
			_draft.MyBar = (_draft.PlayerBar = (_draft.FriendBar = (_draft.PartyBar = (_draft.GuildBar = (_draft.NpcBar = false)))));
			break;
		case Page.Graphics:
			_draft.FullScreen = false;
			_draft.TargetFps = 0;
			_draft.Lighting = true;
			_draft.Blood = true;
			_draft.CameraEffects = true;
			_draft.AffichageCompatible = false;
			_draft.ModePerformances = false;
			_draft.ScrollZoom = false;
			if (Options.IsLoaded)
			{
				_worldScale.SetValue(1.5);
			}
			break;
		case Page.Sound:
			_musicSlider.SetValue(10.0);
			_soundSlider.SetValue(15.0);
			break;
		case Page.Keys:
			if (_keybindingEditControls == null)
			{
				_keybindingEditControls = new Controls(Controls.ActiveControls);
			}
			_keybindingEditControls.ResetDefaults();
			_keyNotice.Notice = null;
			break;
		}
	}

	private string ControlName(Control control)
	{
		string controlId = control.GetControlId();
		if (Strings.Controls.KeyDictionary.TryGetValue(controlId?.ToLower() ?? string.Empty, out LocalizedString value))
		{
			return Trim(value);
		}
		int num = Options.Instance?.Player.HotbarSlotCount ?? 10;
		int num2 = (int)(control - 4096);
		string text;
		if (0 >= num2 || num2 > num)
		{
			text = controlId;
			if (text == null)
			{
				return control.ToString();
			}
		}
		else
		{
			text = Trim(Strings.Controls.HotkeyXLabel.ToString(num2));
		}
		return text;
		static string Trim(string label)
		{
			return label.TrimEnd().TrimEnd(':').TrimEnd();
		}
	}

	private (string Text, bool Empty) KeyLabel(Control control, int slot)
	{
		(Control, int)? listening = _listening;
		if (listening.HasValue)
		{
			(Control, int) valueOrDefault = listening.GetValueOrDefault();
			if (valueOrDefault.Item1 == control && valueOrDefault.Item2 == slot)
			{
				return (Text: Strings.OptionsWindow.KeyListening, Empty: false);
			}
		}
		IControlSet keybindingEditControls = _keybindingEditControls;
		IControlSet controlSet = keybindingEditControls ?? Controls.ActiveControls;
		if (!controlSet.Mappings.TryGetValue(control, out ControlMapping value) || slot >= value.Bindings.Count || value.Bindings[slot].Key == Keys.None)
		{
			return (Text: Strings.OptionsWindow.KeyEmpty, Empty: true);
		}
		ControlBinding controlBinding = value.Bindings[slot];
		return (Text: Strings.Keys.FormatKeyName(controlBinding.Modifier, controlBinding.Key), Empty: false);
	}

	private void StartListening(Control control, int slot)
	{
		(Control, int)? listening = _listening;
		if (listening.HasValue)
		{
			(Control, int) valueOrDefault = listening.GetValueOrDefault();
			bool flag = valueOrDefault.Item1 == control && valueOrDefault.Item2 == slot;
			StopListening(restore: true);
			if (flag)
			{
				return;
			}
		}
		if (_keybindingEditControls == null)
		{
			_keybindingEditControls = new Controls(Controls.ActiveControls);
		}
		if (_keybindingEditControls.Mappings.TryGetValue(control, out ControlMapping value) && slot < value.Bindings.Count)
		{
			_listeningPrevious = (Modifier: value.Bindings[slot].Modifier, Key: value.Bindings[slot].Key);
			_listening = (control, slot);
			_listeningUntil = Timing.Global.MillisecondsUtc + 5000;
			_keysDown.Clear();
			Interface.GwenInput.HandleInput = false;
		}
	}

	private void StopListening(bool restore)
	{
		(Control, int)? listening = _listening;
		if (listening.HasValue)
		{
			(Control, int) valueOrDefault = listening.GetValueOrDefault();
			if (restore)
			{
				_keybindingEditControls?.UpdateControl(valueOrDefault.Item1, valueOrDefault.Item2, _listeningPrevious.Modifier, _listeningPrevious.Key);
			}
			_listening = null;
			_keysDown.Clear();
			Interface.GwenInput.HandleInput = true;
		}
	}

	private void OnKeyChipClicked(Control control, int slot)
	{
		switch (_pressedMouseKey)
		{
		case Keys.LButton:
			StartListening(control, slot);
			break;
		case Keys.RButton:
			if (!_listening.HasValue)
			{
				ClearKey(control, slot);
			}
			break;
		}
	}

	private void ClearKey(Control control, int slot)
	{
		StopListening(restore: true);
		if (_keybindingEditControls == null)
		{
			_keybindingEditControls = new Controls(Controls.ActiveControls);
		}
		_keybindingEditControls.UpdateControl(control, slot, Keys.None, Keys.None);
	}

	private void OnKeyDown(Keys modifier, Keys key)
	{
		if (((uint)(key - 1) <= 1u || (uint)(key - 4) <= 2u) ? true : false)
		{
			_pressedMouseKey = key;
		}
		if (_listening.HasValue)
		{
			_keysDown.Add(key);
		}
	}

	private void OnKeyUp(Keys modifier, Keys key)
	{
		(Control, int)? listening = _listening;
		if (!listening.HasValue)
		{
			return;
		}
		(Control, int) valueOrDefault = listening.GetValueOrDefault();
		Controls keybindingEditControls = _keybindingEditControls;
		if (keybindingEditControls == null || key == Keys.None || !_keysDown.Remove(key))
		{
			return;
		}
		keybindingEditControls.UpdateControl(valueOrDefault.Item1, valueOrDefault.Item2, modifier, key);
		foreach (KeyValuePair<Control, ControlMapping> mapping in keybindingEditControls.Mappings)
		{
			mapping.Deconstruct(out var key2, out var value);
			Control control = key2;
			ControlMapping controlMapping = value;
			for (int i = 0; i < controlMapping.Bindings.Count; i++)
			{
				if (control != valueOrDefault.Item1 || i != valueOrDefault.Item2)
				{
					ControlBinding controlBinding = controlMapping.Bindings[i];
					if (controlBinding.Modifier == modifier && controlBinding.Key == key)
					{
						keybindingEditControls.UpdateControl(control, i, Keys.None, Keys.None);
						_keyNotice.Notice = Strings.OptionsWindow.KeyMoved.ToString(Strings.Keys.FormatKeyName(modifier, key), ControlName(control));
						_noticeUntil = Timing.Global.MillisecondsUtc + 7000;
					}
				}
			}
		}
		_listening = null;
		_keysDown.Clear();
		Interface.GwenInput.HandleInput = true;
	}

	private (string Label, Color Tint, bool Name, bool Bar) PreviewState()
	{
		OptionRow lastHoveredRow = _pages[Page.Overhead].Sheet.LastHoveredRow;
		if (lastHoveredRow == null)
		{
			return (Label: Strings.OptionsWindow.OverheadMe, Tint: OptionsStyle.OrangeLight, Name: _draft.MyInfo, Bar: _draft.MyBar);
		}
		return (Label: lastHoveredRow.Label, Tint: lastHoveredRow.Dot ?? OptionsStyle.White, Name: lastHoveredRow.Get?.Invoke() ?? false, Bar: lastHoveredRow.GetSecond?.Invoke() ?? false);
	}

	private static string? ServerReason(bool enabled)
	{
		if (!enabled)
		{
			if (!Options.IsLoaded)
			{
				return Strings.OptionsWindow.AvailableOnceConnected.ToString();
			}
			return Strings.OptionsWindow.DisabledOnServer.ToString();
		}
		return null;
	}

	private static OptionRow Section(string title)
	{
		return new OptionRow
		{
			Kind = OptionRowKind.Section,
			Label = title,
			Height = 28
		};
	}

	private static OptionRow LanguageRow(Func<int> get, Action<int> set)
	{
		return new OptionRow
		{
			Kind = OptionRowKind.Segments,
			Label = Strings.OptionsWindow.Language,
			Help = Strings.OptionsWindow.LanguageHelp,
			Choices = Langue.Disponibles.Select(((string Code, string Nom) langue) => langue.Nom).ToArray(),
			GetChoice = get,
			SetChoice = set,
			Height = 56
		};
	}

	private static OptionRow Toggle(string label, Func<bool> get, Action<bool> set, string? help = null, Func<string?>? unavailable = null, bool confirm = false)
	{
		return new OptionRow
		{
			Kind = OptionRowKind.Toggle,
			Label = label,
			Help = help,
			Get = get,
			Set = set,
			Unavailable = unavailable,
			AppliesOnConfirm = confirm,
			Height = ((help == null && unavailable == null) ? 36 : 44)
		};
	}

	private static OptionRow Pair(string label, string? help, Color dot, Func<bool> getName, Action<bool> setName, Func<bool> getBar, Action<bool> setBar)
	{
		return new OptionRow
		{
			Kind = OptionRowKind.Pair,
			Label = label,
			Help = help,
			Dot = dot,
			Get = getName,
			Set = setName,
			GetSecond = getBar,
			SetSecond = setBar,
			Height = 52
		};
	}

	private ImagePanel MakeCard(string name)
	{
		return new ImagePanel(this, name)
		{
			Texture = GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_card.png"),
			TextureNinePatchMargin = new Margin(8),
			MouseInputEnabled = false
		};
	}

	private static Slider MakeSlider(Base parent, string name)
	{
		Slider slider = new Slider(parent, name)
		{
			BackgroundImageName = "saiyan_vide.png",
			NotchCount = 0,
			SnapToNotches = false
		};
		slider.DraggerSize = new Point(20, 20);
		slider.SetDraggerImage(GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_curseur.png"), ComponentState.Normal);
		slider.SetDraggerImage(GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_curseur_hover.png"), ComponentState.Hovered);
		slider.SetDraggerImage(GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_curseur_clicked.png"), ComponentState.Active);
		slider.SetDraggerImage(GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_curseur.png"), ComponentState.Disabled);
		return slider;
	}

	private Button MakeButton(string name, string key, Color ink)
	{
		string[] array = key.Split('_')[^1].Split('x');
		Button button = new Button(this, name)
		{
			Font = OptionsStyle.Body,
			FontSize = 11,
			TextAlign = Pos.Center,
			UsesHeadingFace = false
		};
		button.SetStateTexture(ComponentState.Normal, key + ".png");
		button.SetStateTexture(ComponentState.Hovered, key + "_hover.png");
		button.SetStateTexture(ComponentState.Active, key + "_clicked.png");
		button.SetStateTexture(ComponentState.Disabled, key + "_disabled.png");
		button.SetTextColor(ink, ComponentState.Normal);
		button.SetTextColor(OrangeLight, ComponentState.Hovered);
		button.SetTextColor(Night, ComponentState.Active);
		button.SetTextColor(Dim, ComponentState.Disabled);
		button.SetSize(int.Parse(array[0]), int.Parse(array[1]));
		return button;
	}

	private static void StyleScroll(ScrollControl scroll)
	{
		scroll.EnableScroll(horizontal: false, vertical: true);
		scroll.VerticalScrollBar.SetBackgroundTemplate(null, "saiyan_scroll_bg.png");
		IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_scroll_bar.png");
		IGameTexture texture2 = GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_scroll_bar_hover.png");
		scroll.VerticalScrollBar.SetScrollBarImage(texture, "saiyan_scroll_bar.png", ComponentState.Normal);
		scroll.VerticalScrollBar.SetScrollBarImage(texture2, "saiyan_scroll_bar_hover.png", ComponentState.Hovered);
		scroll.VerticalScrollBar.SetScrollBarImage(texture2, "saiyan_scroll_bar_hover.png", ComponentState.Active);
	}

	private List<OptionRow> LignesHeritage()
	{
		if (!HeritageRegles.Actif)
		{
			return new List<OptionRow>();
		}
		int num = 2;
		List<OptionRow> list = new List<OptionRow>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<OptionRow> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = Section(Strings.Heritage.SectionCombat);
		num2++;
		span[num2] = Toggle(Strings.Heritage.DirectionTenue, () => _draft.HeritageDirectionTenue, delegate(bool value)
		{
			_draft.HeritageDirectionTenue = value;
		}, Strings.Heritage.DirectionTenueAide);
		num2++;
		return list;
	}
}
