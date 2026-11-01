using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Database;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Chat;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Configuration;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.PlayerClass;
using Intersect.Localization;
using Intersect.Models;
using Intersect.Network;
using Intersect.Network.Events;
using Intersect.Network.Packets.Server;
using Intersect.Security;
using Intersect.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;

namespace Intersect.Client.Interface.Menu;

internal sealed class MenuScene : IDisposable
{
	private enum Panel
	{
		Login,
		Register,
		Forgot,
		Credits
	}

	private sealed record CreditsRow(string Text, MenuFont Font, float Px, float LineHeight, float SpacingEm, Color Color, MenuAlign Align, float Before);

	private sealed class MenuField : TextBox
	{
		private bool _masked;

		public bool Masked
		{
			get
			{
				return _masked;
			}
			set
			{
				if (_masked != value)
				{
					_masked = value;
					ApplyMask();
				}
			}
		}

		public (float X, float Y, float Width, float Alpha)? Placement { get; set; }

		public int AppliedPaddingLeft { get; set; } = -1;

		public MenuField(Base parent, bool masked, int maximumLength)
			: base(parent)
		{
			_masked = masked;
			base.ShouldDrawBackground = false;
			base.IsHidden = true;
			base.IsTabable = true;
			base.TextAlign = Pos.Left | Pos.CenterV;
			base.Padding = Padding.Zero;
			base.MaximumLength = maximumLength;
			Font = GameContentManager.Current.GetFont("chakrapetchmedium") ?? GameContentManager.Current.GetFont("chakrapetch");
			FontSize = 15;
			base.TextColorOverride = new Color(255, 242, 242, 242);
		}

		private void ApplyMask()
		{
			base.TextOverride = (_masked ? new string('•', Text?.Length ?? 0) : null);
		}

		protected override void OnTextChanged()
		{
			ApplyMask();
			base.OnTextChanged();
		}

		protected override bool OnKeyReturn(bool down)
		{
			return true;
		}
	}

	private sealed record CreationClass(MenuClassInfo Info, ClassDescriptor? Descriptor, int SpriteIndex, string? Sprite, IReadOnlyList<(int Index, string Sprite)> Outfits, IReadOnlyList<CreatorBody> Bodies)
	{
		public bool HasSprite => Sprite != null;

		public bool Available
		{
			get
			{
				if (Descriptor != null && SpriteIndex >= 0)
				{
					return Sprite != null;
				}
				return false;
			}
		}
	}

	private sealed record CreatorChoice(string Key, Func<string> Name, string Swatch = "");

	private readonly record struct CreatorBody(int Hairstyle, int HairColor, int Skin, int Index, string Sprite);

	private enum ModalKind
	{
		None,
		Quit,
		Delete,
		Bienvenue
	}

	private const float PanelLeft = 96f;

	private const float PanelWidth = 600f;

	private const float PanelInner = 508f;

	private const float PanelPadX = 46f;

	private const float FieldHeight = 56f;

	private const float CreditsViewport = 420f;

	private const string SavedPasswordMask = "****************";

	private readonly SceneTween[] _panelAlpha = new SceneTween[4]
	{
		new SceneTween(0f),
		new SceneTween(0f),
		new SceneTween(0f),
		new SceneTween(0f)
	};

	private readonly SceneTween[] _panelSlide = new SceneTween[4]
	{
		new SceneTween(0f),
		new SceneTween(0f),
		new SceneTween(0f),
		new SceneTween(0f)
	};

	private readonly string[] _panelErrors = new string[4]
	{
		string.Empty,
		string.Empty,
		string.Empty,
		string.Empty
	};

	private readonly long[] _panelShakeAt = new long[4] { -2305843009213693952L, -2305843009213693952L, -2305843009213693952L, -2305843009213693952L };

	private readonly List<MenuField> _fields = new List<MenuField>();

	private MenuField _loginName;

	private MenuField _loginPassword;

	private MenuField _registerName;

	private MenuField _registerEmail;

	private MenuField _registerPassword;

	private MenuField _registerConfirm;

	private MenuField _forgotName;

	private MenuField _characterName;

	private MenuField? _focusRequest;

	private bool _rememberMe;

	private bool _useSavedPassword;

	private bool _loadingCredentials;

	private string _savedPasswordHash = string.Empty;

	private MenuScreen? _busyScreen;

	private long _busySince;

	private float _busyDuration = 1500f;

	private string _busyText = string.Empty;

	private Action? _pendingAction;

	private volatile bool _configurationArrived;

	private volatile bool _connectionFailed;

	private bool _networkEventsAttached;

	private bool _cameFromRegistration;

	private static List<CreditsRow>? _creditsRows;

	private static float _creditsHeight;

	private float _creditsOffset;

	private long _creditsHoldUntil;

	private bool _creditsRewind;

	private long _creditsLastUpdate;

	private int _lastWheel = int.MinValue;

	private FloatRect _creditsViewportRect;

	private const string VersionBienvenue = "alpha-1";

	private const string PreferenceBienvenue = "BienvenueAlpha";

	private static bool _bienvenueFaite;

	private bool _bienvenueNePlus;

	private const float StageGround = 830f;

	private static CharacterSelectionPreviewMetadata[] _previews = Array.Empty<CharacterSelectionPreviewMetadata>();

	private static string _sessionUsername = string.Empty;

	private static bool _forceCreation;

	private readonly SceneTween _stageAlpha = new SceneTween(0f);

	private readonly SceneTween _headerAlpha = new SceneTween(0f);

	private readonly SceneTween _headerSlide = new SceneTween(0f);

	private readonly SceneTween _selectionAlpha = new SceneTween(0f);

	private readonly SceneTween _ficheSlide = new SceneTween(0f);

	private readonly SceneTween _creationAlpha = new SceneTween(0f);

	private readonly SceneTween _turnAlpha = new SceneTween(0f);

	private readonly SceneTween _comingAlpha = new SceneTween(0f);

	private readonly SceneTween _classBandY = new SceneTween(9f);

	private readonly SceneTween _classBandWidth = new SceneTween(200f);

	private SceneTween[] _classShift = Array.Empty<SceneTween>();

	private List<CreationClass> _classes = new List<CreationClass>();

	private int _previewIndex;

	private int _classIndex;

	private int _outfitIndex;

	private MenuScreen _lastCharacterScreen = MenuScreen.Selection;

	private long _materializedAt = -2305843009213693952L;

	private float _stageX = 960f;

	private long _stageLastUpdate;

	private MenuCharacterLook? _stageLook;

	private bool _stageGreen;

	private string _creationError = string.Empty;

	private long _creationShakeAt = -2305843009213693952L;

	private string? _pendingDeleteName;

	private Guid _pendingDeleteId;

	private string? _deleteMessage;

	private const float SwatchSize = 60f;

	private const float SwatchGap = 14f;

	private const float SwatchCut = 12f;

	private static readonly CreatorChoice[] CreatorHairstyles = new CreatorChoice[9]
	{
		new CreatorChoice("criniere", () => Strings.MenuScene.CoiffureCriniere),
		new CreatorChoice("pointes", () => Strings.MenuScene.CoiffurePointes),
		new CreatorChoice("flamme", () => Strings.MenuScene.CoiffureFlamme),
		new CreatorChoice("meches", () => Strings.MenuScene.CoiffureMeches),
		new CreatorChoice("carre", () => Strings.MenuScene.CoiffureCarre),
		new CreatorChoice("meche", () => Strings.MenuScene.CoiffureMeche),
		new CreatorChoice("ebouriffee", () => Strings.MenuScene.CoiffureEbouriffee),
		new CreatorChoice("herisson", () => Strings.MenuScene.CoiffureHerisson),
		new CreatorChoice("broussaille", () => Strings.MenuScene.CoiffureBroussaille)
	};

	private static readonly CreatorChoice[] CreatorHairColors = new CreatorChoice[2]
	{
		new CreatorChoice("noir", () => Strings.MenuScene.CheveuxNoir, "#1A1A1E"),
		new CreatorChoice("brun", () => Strings.MenuScene.CheveuxBrun, "#64401E")
	};

	private static readonly CreatorChoice[] CreatorSkins = new CreatorChoice[4]
	{
		new CreatorChoice("claire", () => Strings.MenuScene.PeauClaire, "#D39F6D"),
		new CreatorChoice("doree", () => Strings.MenuScene.PeauDoree, "#D59C47"),
		new CreatorChoice("mate", () => Strings.MenuScene.PeauMate, "#A4723A"),
		new CreatorChoice("foncee", () => Strings.MenuScene.PeauFoncee, "#744D36")
	};

	private int _hairstyle;

	private int _hairColor;

	private int _skin;

	private static bool _introPlayed;

	private static string? _pendingToast;

	private static MenuScreen _lastScreen = MenuScreen.Intro;

	private static bool _sessionActive;

	private readonly MainMenu _mainMenu;

	private readonly Canvas _canvas;

	private readonly MenuBackdrop _backdrop = new MenuBackdrop();

	private readonly MenuLogo _logo = new MenuLogo();

	private readonly MenuCharacterStage _stage = new MenuCharacterStage();

	private readonly Random _random = new Random();

	private readonly List<(string Id, FloatRect Rect)> _hits = new List<(string, FloatRect)>();

	private readonly List<(string Id, FloatRect Rect)> _drawnHits = new List<(string, FloatRect)>();

	private bool _hitsEnabled = true;

	private string? _hovered;

	private string? _pressed;

	private long _hoveredSince;

	private MenuScreen _screen;

	private long _screenSince;

	private long _introStart;

	private float _mouseX;

	private float _mouseY;

	private bool _mouseInside;

	private bool _disposed;

	private readonly SceneTween _veil = new SceneTween(0.42f);

	private readonly SceneTween _blur = new SceneTween(0f);

	private readonly SceneTween _spotlight = new SceneTween(0f);

	private readonly SceneTween _focusX = new SceneTween(960f);

	private readonly SceneTween _logoPose = new SceneTween(0f);

	private readonly SceneTween _logoAlpha = new SceneTween(1f);

	private readonly SceneTween _invite = new SceneTween(0f);

	private readonly SceneTween _chips = new SceneTween(0f);

	private readonly SceneTween _version = new SceneTween(0f);

	private readonly SceneTween _keys = new SceneTween(0f);

	private readonly SceneTween _black = new SceneTween(1f);

	private readonly SceneTween _farewell = new SceneTween(0f);

	private long _flashAt = -2305843009213693952L;

	private float _flashStrength;

	private const float NavLeft = 96f;

	private const float NavTop = 392f;

	private const float NavItemHeight = 72f;

	private readonly SceneTween[] _navItems = new SceneTween[5]
	{
		new SceneTween(0f),
		new SceneTween(0f),
		new SceneTween(0f),
		new SceneTween(0f),
		new SceneTween(0f)
	};

	private readonly SceneTween[] _navShift = new SceneTween[5]
	{
		new SceneTween(0f),
		new SceneTween(0f),
		new SceneTween(0f),
		new SceneTween(0f),
		new SceneTween(0f)
	};

	private readonly SceneTween _bandY = new SceneTween(0f);

	private readonly SceneTween _bandWidth = new SceneTween(320f);

	private readonly SceneTween _bandAlpha = new SceneTween(0f);

	private readonly SceneTween _navHint = new SceneTween(0f);

	private readonly SceneTween _newsAlpha = new SceneTween(0f);

	private readonly SceneTween _articleIn = new SceneTween(1f);

	private int _navIndex;

	private int _newsPage;

	private long _newsPageSince;

	private long _newsPausedAt = -1L;

	private readonly SceneTween _modalAlpha = new SceneTween(0f);

	private readonly SceneTween _modalLift = new SceneTween(0f);

	private readonly SceneTween _toastAlpha = new SceneTween(0f);

	private ModalKind _modal;

	private ModalKind _modalShown;

	private string _modalName = string.Empty;

	private string _toastText = string.Empty;

	private long _toastAt = -2305843009213693952L;

	private bool _quitting;

	private long _quitAt;

	private static readonly Color StrokeLight = MenuDraw.Rgba(255, 255, 255, 0.14f);

	private const float LabelPx = 16f;

	private bool AnyFieldFocused => _fields.Any((MenuField field) => field.HasFocus);

	private float ClassRowHeight
	{
		get
		{
			if (ListRows <= 8)
			{
				return 78f;
			}
			return MathF.Max(52f, 632f / (float)ListRows);
		}
	}

	private int ListRows
	{
		get
		{
			if (!CreatorMode)
			{
				return _classes.Count;
			}
			return CreatorHairstyles.Length;
		}
	}

	private CharacterSelectionPreviewMetadata? CurrentPreview
	{
		get
		{
			if (_previews.Length == 0)
			{
				return null;
			}
			CharacterSelectionPreviewMetadata value = _previews[Math.Clamp(_previewIndex, 0, _previews.Length - 1)];
			if (!(value.Id == Guid.Empty))
			{
				return value;
			}
			return null;
		}
	}

	private bool CreatorMode
	{
		get
		{
			if (_classIndex < _classes.Count)
			{
				return _classes[_classIndex].Bodies.Count > 0;
			}
			return false;
		}
	}

	private static long Now => MenuDraw.Maintenant;

	public MenuScreen Screen => _screen;

	private bool EngineWindowOpen
	{
		get
		{
			if (!_canvas.Children.OfType<WindowControl>().Any((WindowControl window) => window.IsVisibleInTree))
			{
				return _mainMenu.IsEngineWindowOpen;
			}
			return true;
		}
	}

	private static (string Label, string Hint)[] NavEntries => new(string, string)[5]
	{
		(Strings.MenuScene.Play, Strings.MenuScene.PlayHint),
		(Strings.MenuScene.Register, Strings.MenuScene.RegisterHint),
		(Strings.MenuScene.Options, Strings.MenuScene.OptionsHint),
		(Strings.MenuScene.Credits, Strings.MenuScene.CreditsHint),
		(Strings.MenuScene.Quit, Strings.MenuScene.QuitHint)
	};

	private float Middle => MenuDraw.MiddleOffset;

	private static IGameTexture? KiRamp => MenuTextures.Ramp("ki", (0f, MenuDraw.Ki), (1f, MenuDraw.KiLight));

	private static IGameTexture? BandRamp => MenuTextures.Ramp("band", (0f, MenuDraw.KiDark), (0.25f, MenuDraw.Ki), (1f, MenuDraw.KiLight));

	private static IGameTexture? GreyRamp => MenuTextures.Ramp("grey", (0f, new Color(255, 96, 100, 110)), (1f, new Color(255, 120, 124, 134)));

	private static IGameTexture? SheenRamp => MenuTextures.Ramp("sheen", (0f, new Color(0, 255, 255, 255)), (0.5f, new Color(153, 255, 255, 255)), (1f, new Color(0, 255, 255, 255)));

	private static IGameTexture? FadeRightRamp => MenuTextures.Ramp("fade-right", (0f, new Color(56, 255, 255, 255)), (1f, new Color(0, 255, 255, 255)));

	private static IGameTexture? LineInRamp => MenuTextures.Ramp("line-in", (0f, new Color(0, 255, 172, 82)), (1f, MenuDraw.KiLight));

	private static IGameTexture? LineOutRamp => MenuTextures.Ramp("line-out", (0f, MenuDraw.KiLight), (1f, new Color(0, 255, 172, 82)));

	private static IGameTexture? SlotRamp => MenuTextures.RampVertical("slot", (0f, MenuDraw.Rgba(36, 42, 62, 0.92f)), (1f, MenuDraw.Rgba(15, 19, 31, 0.92f)));

	private MenuField Field(bool masked = false, int maximumLength = 64)
	{
		MenuField menuField = new MenuField(_canvas, masked, maximumLength);
		_fields.Add(menuField);
		return menuField;
	}

	private void CreateFields()
	{
		_loginName = Field();
		_loginPassword = Field(masked: true);
		_registerName = Field();
		_registerEmail = Field(masked: false, 128);
		_registerPassword = Field(masked: true);
		_registerConfirm = Field(masked: true);
		_forgotName = Field(masked: false, 128);
		_characterName = Field(masked: false, 16);
		LoadCredentials();
		_loginPassword.TextChanged += delegate
		{
			if (!_loadingCredentials)
			{
				bool useSavedPassword = _useSavedPassword;
				_useSavedPassword = false;
				if (useSavedPassword)
				{
					string text = _loginPassword.Text ?? string.Empty;
					string text2;
					if (!text.StartsWith("****************", StringComparison.Ordinal))
					{
						text2 = text.Trim('*');
					}
					else
					{
						string text3 = text;
						int length = "****************".Length;
						text2 = text3.Substring(length, text3.Length - length);
					}
					string text4 = text2;
					if (text4 != text)
					{
						_loadingCredentials = true;
						try
						{
							_loginPassword.Text = text4;
							_loginPassword.CursorPos = text4.Length;
						}
						finally
						{
							_loadingCredentials = false;
						}
					}
				}
			}
		};
	}

	private void DisposeFields()
	{
		foreach (MenuField field in _fields)
		{
			if (field.HasFocus)
			{
				field.Blur();
			}
			field.IsHidden = true;
		}
	}

	private void LoadCredentials()
	{
		_loadingCredentials = true;
		try
		{
			string text = Globals.Database?.LoadPreference("Username");
			if (!string.IsNullOrEmpty(text))
			{
				_loginName.Text = text;
				string text2 = Globals.Database?.LoadPreference("Password");
				if (!string.IsNullOrEmpty(text2))
				{
					_loginPassword.Text = "****************";
					_loginPassword.CursorPos = "****************".Length;
					_savedPasswordHash = text2;
					_useSavedPassword = true;
					_rememberMe = true;
				}
			}
		}
		finally
		{
			_loadingCredentials = false;
		}
	}

	private void SaveCredentials(string name, string hash)
	{
		GameDatabase database = Globals.Database;
		if (database != null)
		{
			database.SavePreference("Username", _rememberMe ? name : null);
			database.SavePreference("Password", _rememberMe ? hash : null);
		}
	}

	private void LayoutFields(long now)
	{
		bool flag = _modal != ModalKind.None || _quitting || MenuLoadingScreen.IsActive || _black.Value(now) > 0.35f;
		float scale = MenuDraw.Scale;
		foreach (MenuField field in _fields)
		{
			if (!flag)
			{
				(float, float, float, float)? placement = field.Placement;
				if (placement.HasValue)
				{
					(float, float, float, float) valueOrDefault = placement.GetValueOrDefault();
					if (!(valueOrDefault.Item4 <= 0.02f))
					{
						bool flag2 = field == _loginPassword || field == _registerPassword;
						int num = (int)MathF.Round(valueOrDefault.Item1 * scale);
						int num2 = (int)MathF.Round(valueOrDefault.Item2 * scale);
						int num3 = (int)MathF.Round((valueOrDefault.Item3 - (float)(flag2 ? 52 : 0)) * scale);
						int num4 = (int)MathF.Round(56f * scale);
						if (field.X != num || field.Y != num2 || field.Width != num3 || field.Height != num4)
						{
							field.SetBounds(num, num2, num3, num4);
						}
						int num5 = (int)MathF.Round(18f * scale);
						if (field.AppliedPaddingLeft != num5)
						{
							field.AppliedPaddingLeft = num5;
							field.Padding = new Padding(num5, 0, (!flag2) ? num5 : 0, 0);
						}
						int num6 = Math.Clamp((int)MathF.Round(20f * scale * 0.75f), 8, 32);
						if (field.FontSize != num6)
						{
							field.FontSize = num6;
						}
						field.TextColorOverride = new Color(Math.Max(1, (int)(255f * valueOrDefault.Item4)), 242, 242, 242);
						if (field.IsHidden)
						{
							field.IsHidden = false;
						}
						if (_focusRequest == field && valueOrDefault.Item4 > 0.5f && _modal == ModalKind.None)
						{
							field.Focus();
							_focusRequest = null;
						}
						continue;
					}
				}
			}
			if (field.HasFocus)
			{
				field.Blur();
			}
			if (!field.IsHidden)
			{
				field.IsHidden = true;
			}
		}
	}

	private static Panel? PanelOf(MenuScreen screen)
	{
		return screen switch
		{
			MenuScreen.Login => Panel.Login, 
			MenuScreen.Register => Panel.Register, 
			MenuScreen.Forgot => Panel.Forgot, 
			MenuScreen.Credits => Panel.Credits, 
			_ => null, 
		};
	}

	private void OnAccountScreen(MenuScreen previous, MenuScreen screen, long now)
	{
		Panel? panel = PanelOf(screen);
		for (int i = 0; i < _panelAlpha.Length; i++)
		{
			bool flag = panel == (Panel?)i;
			_panelAlpha[i].Set(flag ? 1 : 0, now, 280f, flag ? 140 : 0);
			_panelSlide[i].Set(flag ? 1 : 0, now, 460f, flag ? 140 : 0, MenuDraw.EaseSlide);
		}
		if (previous == screen)
		{
			return;
		}
		if (_busyScreen.HasValue && _busyScreen != screen)
		{
			ClearBusy();
		}
		foreach (MenuField field in _fields)
		{
			if (field.HasFocus && !FieldShownOn(field, screen))
			{
				field.Blur();
			}
		}
		if (!panel.HasValue)
		{
			_focusRequest = ((screen == MenuScreen.Creation) ? _focusRequest : null);
			return;
		}
		Array.Fill(_panelErrors, string.Empty);
		_focusRequest = screen switch
		{
			MenuScreen.Login => string.IsNullOrWhiteSpace(_loginName.Text) ? _loginName : _loginPassword, 
			MenuScreen.Register => _registerName, 
			MenuScreen.Forgot => _forgotName, 
			_ => null, 
		};
		if (screen == MenuScreen.Credits)
		{
			_creditsOffset = 0f;
			_creditsRewind = false;
			_creditsHoldUntil = now + 1800;
		}
	}

	private bool FieldShownOn(MenuField field, MenuScreen screen)
	{
		return screen switch
		{
			MenuScreen.Login => field == _loginName || field == _loginPassword, 
			MenuScreen.Register => field == _registerName || field == _registerEmail || field == _registerPassword || field == _registerConfirm, 
			MenuScreen.Forgot => field == _forgotName, 
			MenuScreen.Creation => field == _characterName, 
			_ => false, 
		};
	}

	private void OpenAccountScreen(MenuScreen screen, long now)
	{
		Go(screen, now);
		EnsureConnection();
	}

	private void BackToMain(long now)
	{
		Sound("menu_back.wav");
		_pendingAction = null;
		ClearBusy();
		Go(MenuScreen.Main, now, sound: false);
		Intersect.Client.Networking.Network.DebounceClose("returning_to_main_menu");
	}

	private void StartBusy(MenuScreen screen, long now, string text, float durationMs)
	{
		_busyScreen = screen;
		_busySince = now;
		_busyText = text;
		_busyDuration = durationMs;
	}

	private void ClearBusy()
	{
		_busyScreen = null;
	}

	private float BusyCharge(long now)
	{
		return 0.92f * MenuDraw.EaseOut((float)(now - _busySince) / _busyDuration);
	}

	private void AttachNetworkEvents()
	{
		if (!_networkEventsAttached)
		{
			_networkEventsAttached = true;
			MainMenu.ReceivedConfiguration += OnReceivedConfiguration;
			Intersect.Client.Networking.Network.Socket.ConnectionFailed += OnConnectionFailed;
		}
	}

	private void DetachNetworkEvents()
	{
		if (_networkEventsAttached)
		{
			_networkEventsAttached = false;
			MainMenu.ReceivedConfiguration -= OnReceivedConfiguration;
			Intersect.Client.Networking.Network.Socket.ConnectionFailed -= OnConnectionFailed;
		}
	}

	private void OnReceivedConfiguration(object? sender, EventArgs arguments)
	{
		_configurationArrived = true;
	}

	private void OnConnectionFailed(INetworkLayerInterface sender, ConnectionEventArgs arguments, bool denied)
	{
		_connectionFailed = true;
	}

	private static void EnsureConnection()
	{
		if (!Intersect.Client.Networking.Network.InterruptDisconnectsIfConnected())
		{
			Intersect.Client.Networking.Network.TryConnect();
		}
	}

	private void WhenConnected(Action action)
	{
		AttachNetworkEvents();
		if (Intersect.Client.Networking.Network.InterruptDisconnectsIfConnected() && !Globals.WaitingOnServer)
		{
			action();
			return;
		}
		_pendingAction = action;
		_connectionFailed = false;
		Intersect.Client.Networking.Network.TryConnect();
	}

	private void UpdateAccount(long now)
	{
		if (_configurationArrived)
		{
			_configurationArrived = false;
			Action pendingAction = _pendingAction;
			if (pendingAction != null && Intersect.Client.Networking.Network.IsConnected)
			{
				_pendingAction = null;
				try
				{
					pendingAction();
				}
				catch (Exception exception)
				{
					ApplicationContext.Context.Value?.Logger.LogError(exception, "Menu action after connecting failed");
					ClearBusy();
				}
			}
		}
		if (_connectionFailed)
		{
			_connectionFailed = false;
			if (_pendingAction != null)
			{
				_pendingAction = null;
				ClearBusy();
				Panel? panel = PanelOf(_screen);
				if (panel.HasValue)
				{
					Panel valueOrDefault = panel.GetValueOrDefault();
					Fail(valueOrDefault, Strings.MenuScene.ServerUnavailable, now);
				}
				else if (_screen == MenuScreen.Creation)
				{
					FailCreation(Strings.MenuScene.ServerUnavailable, now);
				}
			}
		}
		if (_busyScreen.HasValue && _pendingAction == null && !Globals.WaitingOnServer && now - _busySince > 250)
		{
			ClearBusy();
		}
		UpdateCredits(now);
	}

	private void Fail(Panel panel, string message, long now)
	{
		_panelErrors[(int)panel] = message;
		_panelShakeAt[(int)panel] = now;
		ClearBusy();
		Sound("menu_back.wav");
	}

	private void SubmitLogin(long now)
	{
		if (_busyScreen.HasValue)
		{
			return;
		}
		string name = _loginName.Text?.Trim() ?? string.Empty;
		string password = _loginPassword.Text ?? string.Empty;
		if (name.Length == 0)
		{
			Fail(Panel.Login, Strings.MenuScene.EnterUsername, now);
			return;
		}
		if (password.Length == 0)
		{
			Fail(Panel.Login, Strings.MenuScene.EnterPassword, now);
			return;
		}
		if (!FieldChecking.IsValidUsername(name, Strings.Regex.Username))
		{
			Fail(Panel.Login, Strings.Errors.UsernameInvalid, now);
			return;
		}
		if (!_useSavedPassword && !FieldChecking.IsValidPassword(password, Strings.Regex.Password))
		{
			Fail(Panel.Login, Strings.Errors.PasswordInvalid, now);
			return;
		}
		_panelErrors[0] = string.Empty;
		Sound("menu_click.wav");
		StartBusy(MenuScreen.Login, now, Strings.MenuScene.LoginBusy, 1500f);
		_sessionUsername = name;
		_cameFromRegistration = false;
		bool useSaved = _useSavedPassword;
		string savedHash = _savedPasswordHash;
		WhenConnected(delegate
		{
			string text = (useSaved ? savedHash : PasswordUtils.ComputePasswordHash(password.Trim()));
			Globals.WaitingOnServer = true;
			PacketSender.SendLogin(name, text);
			SaveCredentials(name, text);
			ChatboxMsg.ClearMessages();
		});
	}

	private void SubmitRegistration(long now)
	{
		if (_busyScreen.HasValue)
		{
			return;
		}
		string username = _registerName.Text?.Trim() ?? string.Empty;
		string email = _registerEmail.Text?.Trim() ?? string.Empty;
		string password = _registerPassword.Text?.Trim() ?? string.Empty;
		string b = _registerConfirm.Text?.Trim() ?? string.Empty;
		if (!FieldChecking.IsValidUsername(username, Strings.Regex.Username))
		{
			Fail(Panel.Register, Strings.Errors.UsernameInvalid, now);
			return;
		}
		if (!FieldChecking.IsWellformedEmailAddress(email, Strings.Regex.Email))
		{
			Fail(Panel.Register, Strings.MenuScene.EmailInvalid, now);
			return;
		}
		if (!FieldChecking.IsValidPassword(password, Strings.Regex.Password))
		{
			Fail(Panel.Register, Strings.Errors.PasswordInvalid, now);
			return;
		}
		if (MotDePasse.TropCourt(password))
		{
			Fail(Panel.Register, Strings.Errors.PasswordTooShort.ToString(8), now);
			return;
		}
		if (!string.Equals(password, b, StringComparison.Ordinal))
		{
			Fail(Panel.Register, Strings.MenuScene.PasswordMismatch, now);
			return;
		}
		_panelErrors[1] = string.Empty;
		Sound("menu_click.wav");
		StartBusy(MenuScreen.Register, now, Strings.MenuScene.RegisterBusy, 1200f);
		_sessionUsername = username;
		_cameFromRegistration = true;
		WhenConnected(delegate
		{
			PacketSender.SendUserRegistration(username, PasswordUtils.ComputePasswordHash(password), email);
			Globals.WaitingOnServer = true;
			ChatboxMsg.ClearMessages();
		});
	}

	private void SubmitForgot(long now)
	{
		if (_busyScreen.HasValue)
		{
			return;
		}
		string identifier = _forgotName.Text?.Trim() ?? string.Empty;
		if (!FieldChecking.IsValidUsername(identifier, Strings.Regex.Username) && !FieldChecking.IsWellformedEmailAddress(identifier, Strings.Regex.Email))
		{
			Fail(Panel.Forgot, Strings.Errors.UsernameInvalid, now);
			return;
		}
		_panelErrors[2] = string.Empty;
		Sound("menu_click.wav");
		StartBusy(MenuScreen.Forgot, now, Strings.MenuScene.ForgotBusy, 900f);
		WhenConnected(delegate
		{
			ClearBusy();
			_mainMenu.OpenPasswordChangeWindow(identifier, PasswordChangeMode.ResetToken, null);
			PacketSender.SendRequestPasswordReset(identifier);
			Go(MenuScreen.Overlay, Now, sound: false);
		});
	}

	private bool OnAccountClick(string id, long now)
	{
		switch (id)
		{
		case "login:submit":
			SubmitLogin(now);
			return true;
		case "login:remember":
			_rememberMe = !_rememberMe;
			return true;
		case "login:forgot":
			Sound("menu_click.wav");
			Go(MenuScreen.Forgot, now, sound: false);
			return true;
		case "login:register":
			if (Options.IsLoaded && Options.Instance.BlockClientRegistrations)
			{
				Toast(Strings.MenuScene.RegistrationsClosed);
				return true;
			}
			Go(MenuScreen.Register, now);
			return true;
		case "register:login":
			Go(MenuScreen.Login, now);
			return true;
		case "credits:back":
		case "register:back":
		case "login:back":
			BackToMain(now);
			return true;
		case "forgot:back":
			Sound("menu_back.wav");
			_pendingAction = null;
			ClearBusy();
			Go(MenuScreen.Login, now, sound: false);
			return true;
		case "register:submit":
			SubmitRegistration(now);
			return true;
		case "forgot:submit":
			SubmitForgot(now);
			return true;
		case "eye:login":
			_loginPassword.Masked = !_loginPassword.Masked;
			return true;
		case "eye:register":
			_registerPassword.Masked = !_registerPassword.Masked;
			return true;
		default:
			return false;
		}
	}

	private bool OnAccountKey(Intersect.Client.Framework.GenericClasses.Keys key, long now)
	{
		Panel? panel = PanelOf(_screen);
		if (panel.HasValue)
		{
			Panel valueOrDefault = panel.GetValueOrDefault();
			switch (key)
			{
			case Intersect.Client.Framework.GenericClasses.Keys.Escape:
				if (valueOrDefault == Panel.Forgot)
				{
					OnAccountClick("forgot:back", now);
				}
				else
				{
					BackToMain(now);
				}
				return true;
			case Intersect.Client.Framework.GenericClasses.Keys.Return:
				switch (valueOrDefault)
				{
				case Panel.Login:
					SubmitLogin(now);
					break;
				case Panel.Register:
					SubmitRegistration(now);
					break;
				case Panel.Forgot:
					SubmitForgot(now);
					break;
				}
				return true;
			default:
				return false;
			}
		}
		return false;
	}

	private bool AccountGlassVisible(long now)
	{
		return _panelAlpha.Any((SceneTween alpha) => alpha.Value(now) > 0.001f);
	}

	private (float X, float Top, float Alpha) PanelFrame(Panel panel, float top, long now)
	{
		float item = _panelAlpha[(int)panel].Value(now);
		float num = _panelSlide[(int)panel].Value(now);
		return (X: 96f + (1f - num) * -46f + ShakeOffset(_panelShakeAt[(int)panel], now), Top: top + Middle, Alpha: item);
	}

	private static float ShakeOffset(long at, long now)
	{
		float num = (float)(now - at) / 360f;
		if ((num < 0f || num >= 1f) ? true : false)
		{
			return 0f;
		}
		float[] array = new float[5] { 0f, -12f, 9f, -5f, 0f };
		float num2 = MenuDraw.EaseOut(num) * 4f;
		int num3 = Math.Min(3, (int)num2);
		return array[num3] + (array[num3 + 1] - array[num3]) * (num2 - (float)num3);
	}

	private void DrawAccount(long now)
	{
		foreach (MenuField field in _fields)
		{
			field.Placement = null;
		}
		DrawLoginPanel(now);
		DrawRegisterPanel(now);
		DrawForgotPanel(now);
		DrawCreditsPanel(now);
		_hitsEnabled = true;
	}

	private static void PanelTitle(string text, float x, float y, float maxWidth, float alpha, float px = 58f)
	{
		string text2 = MenuDraw.Upper(text);
		float item = MenuDraw.Measure(text2, MenuFont.Saira, px, 0.01f).Width;
		float px2 = ((item > maxWidth) ? (px * maxWidth / item) : px);
		MenuDraw.Text(text2, MenuFont.Saira, px2, x, y, MenuDraw.Alpha(MenuDraw.White, alpha), MenuAlign.Left, 0.01f, px * 0.95f);
	}

	private float FieldBlock(MenuField field, string label, float x, float y, float width, float alpha, bool active, string placeholder = "", string? eyeId = null)
	{
		Label(label, x, y, alpha);
		FieldBox(field, x, y + 23f, width, alpha, active, placeholder, eyeId);
		return y + 23f + 56f + 20f;
	}

	private void FieldBox(MenuField field, float x, float y, float width, float alpha, bool active, string placeholder = "", string? eyeId = null)
	{
		bool flag = active && field.HasFocus;
		MenuDraw.Rect(x, y, width, 56f, MenuDraw.Rgba(4, 6, 11, (flag ? 0.72f : 0.55f) * alpha));
		if (flag)
		{
			OutlineRect(x - 1f, y - 1f, width + 2f, 58f, MenuDraw.Rgba(255, 172, 82, 0.3f * alpha), 1f);
			OutlineRect(x, y, width, 56f, MenuDraw.Rgba(255, 172, 82, 0.75f * alpha), 1f);
			MenuDraw.Rect(x + 1f, y + 56f - 3f, width - 2f, 2f, MenuDraw.Alpha(MenuDraw.Ki, alpha));
		}
		else
		{
			OutlineRect(x, y, width, 56f, MenuDraw.Rgba(255, 255, 255, 0.13f * alpha), 1f);
		}
		if (string.IsNullOrEmpty(field.Text) && placeholder.Length > 0)
		{
			MenuDraw.Text(placeholder, MenuFont.ChakraMedium, 20f, x + 18f, y, MenuDraw.Rgba(150, 160, 182, 0.7f * alpha), MenuAlign.Left, 0f, 56f);
		}
		if (eyeId != null)
		{
			EyeButton(eyeId, x + width - 8f - 21f, y + 28f, !field.Masked, alpha);
		}
		field.Placement = (active ? new(float, float, float, float)?((x, y, width, alpha)) : (((float, float, float, float)?)null));
	}

	private static float ErrorHeight(IReadOnlyCollection<string> lines)
	{
		return MathF.Max(26f, (float)lines.Count * 23.8f);
	}

	private static List<string> ErrorLines(string error, float width)
	{
		if (!string.IsNullOrEmpty(error))
		{
			return MenuDraw.Wrap(error, MenuFont.ChakraMedium, 17f, width);
		}
		return new List<string>();
	}

	private static void DrawErrorLines(List<string> lines, float x, float y, float alpha)
	{
		for (int i = 0; i < lines.Count; i++)
		{
			MenuDraw.Text(lines[i], MenuFont.ChakraMedium, 17f, x, y + (float)i * 23.8f, MenuDraw.Alpha(MenuDraw.Red, alpha), MenuAlign.Left, 0f, 23.8f);
		}
	}

	private void QuestionAndLink(string question, string link, string linkId, float right, float y, float alpha)
	{
		float item = MenuDraw.Measure(link, MenuFont.Chakra, 17f).Width;
		float num = right - item;
		Link(linkId, link, num, y + 0.8f, alpha);
		float item2 = MenuDraw.Measure(question, MenuFont.ChakraMedium, 17f).Width;
		MenuDraw.Text(question, MenuFont.ChakraMedium, 17f, num - 5f - item2, y, MenuDraw.Alpha(MenuDraw.Steel, alpha), MenuAlign.Left, 0f, 22.1f);
	}

	private void DrawLoginPanel(long now)
	{
		var (num, num2, num3) = PanelFrame(Panel.Login, 364f, now);
		if (!(num3 <= 0.001f))
		{
			bool active = (_hitsEnabled = _screen == MenuScreen.Login);
			float num4 = num + 46f;
			List<string> lines = ErrorLines(_panelErrors[0], 508f);
			float num5 = ErrorHeight(lines);
			float height = 389.1f + num5 + 10f + 64f + 22f + 22.1f + 34f;
			GlassPanel(num, num2, 600f, height, 26f, 0.6f, num3);
			float num6 = num2 + 42f;
			Kicker(Strings.MenuScene.LoginKicker, num4, num6, num3);
			num6 += 24f;
			PanelTitle(Strings.MenuScene.LoginTitle, num4, num6, 508f, num3);
			num6 += 85.1f;
			num6 = FieldBlock(_loginName, Strings.MenuScene.Username, num4, num6, 508f, num3, active);
			num6 = FieldBlock(_loginPassword, Strings.MenuScene.Password, num4, num6, 508f, num3, active, "", _useSavedPassword ? null : "eye:login");
			Checkbox("login:remember", num4, num6, _rememberMe, Strings.MenuScene.RememberMe, num3);
			if (!Options.IsLoaded || Options.Instance.SmtpValid)
			{
				string text = Strings.MenuScene.ForgotPassword.ToString();
				float item = MenuDraw.Measure(text, MenuFont.Chakra, 17f).Width;
				Link("login:forgot", text, num4 + 508f - item, num6 + 0.8f, num3);
			}
			num6 += 40f;
			DrawErrorLines(lines, num4, num6, num3);
			num6 += num5 + 10f;
			bool flag = _busyScreen == MenuScreen.Login;
			Cta("login:submit", num4, num6, 508f, 64f, flag ? _busyText : ((string)Strings.MenuScene.LoginButton), 30f, num3, now, enabled: true, flag ? BusyCharge(now) : 0f);
			num6 += 86f;
			BackButton("login:back", Strings.MenuScene.Back, num4, num6 + 3f, num3);
			QuestionAndLink(Strings.MenuScene.NoAccount, Strings.MenuScene.CreateAccountLink, "login:register", num4 + 508f, num6, num3);
		}
	}

	private void DrawRegisterPanel(long now)
	{
		var (num, num2, num3) = PanelFrame(Panel.Register, 340f, now);
		if (!(num3 <= 0.001f))
		{
			bool active = (_hitsEnabled = _screen == MenuScreen.Register);
			float num4 = num + 46f;
			List<string> lines = ErrorLines(_panelErrors[1], 508f);
			float num5 = ErrorHeight(lines);
			float height = 448.1f + num5 + 10f + 64f + 22f + 22.1f + 34f;
			GlassPanel(num, num2, 600f, height, 26f, 0.6f, num3);
			float num6 = num2 + 42f;
			Kicker(Strings.MenuScene.RegisterKicker, num4, num6, num3);
			num6 += 24f;
			PanelTitle(Strings.MenuScene.RegisterTitle, num4, num6, 508f, num3);
			num6 += 85.1f;
			num6 = FieldBlock(_registerName, Strings.MenuScene.Username, num4, num6, 508f, num3, active, Strings.MenuScene.UsernamePlaceholder);
			num6 = FieldBlock(_registerEmail, Strings.MenuScene.Email, num4, num6, 508f, num3, active, Strings.MenuScene.EmailPlaceholder);
			FieldBlock(_registerPassword, Strings.MenuScene.Password, num4, num6, 246f, num3, active, Strings.MenuScene.PasswordPlaceholder.ToString(8), "eye:register");
			num6 = FieldBlock(_registerConfirm, Strings.MenuScene.ConfirmPassword, num4 + 246f + 16f, num6, 246f, num3, active, Strings.MenuScene.ConfirmPlaceholder);
			DrawErrorLines(lines, num4, num6, num3);
			num6 += num5 + 10f;
			bool flag = _busyScreen == MenuScreen.Register;
			Cta("register:submit", num4, num6, 508f, 64f, flag ? _busyText : ((string)Strings.MenuScene.RegisterButton), 30f, num3, now, enabled: true, flag ? BusyCharge(now) : 0f);
			num6 += 86f;
			BackButton("register:back", Strings.MenuScene.Back, num4, num6 + 3f, num3);
			QuestionAndLink(Strings.MenuScene.HaveAccount, Strings.MenuScene.LoginLink, "register:login", num4 + 508f, num6, num3);
		}
	}

	private void DrawForgotPanel(long now)
	{
		var (num, num2, num3) = PanelFrame(Panel.Forgot, 364f, now);
		if (!(num3 <= 0.001f))
		{
			bool active = (_hitsEnabled = _screen == MenuScreen.Forgot);
			float x = num + 46f;
			List<string> list = MenuDraw.Wrap(Strings.MenuScene.ForgotText, MenuFont.ChakraMedium, 20f, 508f);
			List<string> lines = ErrorLines(_panelErrors[2], 508f);
			float num4 = ErrorHeight(lines);
			float height = 139.1f + (float)(list.Count * 29) + 26f + 99f + num4 + 10f + 64f + 22f + 22.1f + 34f;
			GlassPanel(num, num2, 600f, height, 26f, 0.6f, num3);
			float num5 = num2 + 42f;
			Kicker(Strings.MenuScene.ForgotKicker, x, num5, num3);
			num5 += 24f;
			PanelTitle(Strings.MenuScene.ForgotTitle, x, num5, 508f, num3);
			num5 += 73.1f;
			for (int i = 0; i < list.Count; i++)
			{
				MenuDraw.Text(list[i], MenuFont.ChakraMedium, 20f, x, num5 + (float)(i * 29), MenuDraw.Alpha(MenuDraw.Mist, num3), MenuAlign.Left, 0f, 29f);
			}
			num5 += (float)(list.Count * 29 + 26);
			num5 = FieldBlock(_forgotName, Strings.MenuScene.UsernameOrEmail, x, num5, 508f, num3, active);
			DrawErrorLines(lines, x, num5, num3);
			num5 += num4 + 10f;
			bool flag = _busyScreen == MenuScreen.Forgot;
			Cta("forgot:submit", x, num5, 508f, 64f, flag ? _busyText : ((string)Strings.MenuScene.ForgotButton), 30f, num3, now, enabled: true, flag ? BusyCharge(now) : 0f);
			num5 += 86f;
			BackButton("forgot:back", Strings.MenuScene.Back, x, num5 + 3f, num3);
		}
	}

	private void DrawCreditsPanel(long now)
	{
		var (num, num2, num3) = PanelFrame(Panel.Credits, 340f, now);
		if (num3 <= 0.001f)
		{
			return;
		}
		_hitsEnabled = _screen == MenuScreen.Credits;
		EnsureCredits();
		float num4 = MathF.Min(_creditsHeight, 420f);
		float num5 = num + 46f;
		float height = 151.1f + num4 + 22f + 22.1f + 34f;
		GlassPanel(num, num2, 600f, height, 26f, 0.6f, num3);
		float num6 = num2 + 42f;
		Kicker(Strings.MenuScene.CreditsKicker, num5, num6, num3);
		num6 += 24f;
		PanelTitle(Strings.MenuScene.CreditsTitle, num5, num6, 508f, num3);
		num6 += 85.1f;
		_creditsViewportRect = new FloatRect(num5, num6, 508f, num4);
		bool flag = _creditsHeight > 420f;
		float num7 = num6 - (flag ? _creditsOffset : 0f);
		foreach (CreditsRow creditsRow in _creditsRows)
		{
			num7 += creditsRow.Before;
			float num8 = num7;
			num7 += creditsRow.LineHeight;
			if (creditsRow.Text.Length != 0 && !(num8 + creditsRow.LineHeight < num6) && !(num8 > num6 + num4))
			{
				float num9 = num3;
				if (flag)
				{
					float num10 = num8 + creditsRow.LineHeight / 2f;
					num9 *= MenuDraw.Clamp01((num10 - num6) / 30f) * MenuDraw.Clamp01((num6 + num4 - num10) / 30f);
				}
				MenuAlign align = creditsRow.Align;
				MenuDraw.Text(creditsRow.Text, creditsRow.Font, creditsRow.Px, align switch
				{
					MenuAlign.Center => num5 + 254f, 
					MenuAlign.Right => num5 + 508f, 
					_ => num5, 
				}, num8, MenuDraw.Alpha(creditsRow.Color, num9), creditsRow.Align, creditsRow.SpacingEm, creditsRow.LineHeight);
			}
		}
		num6 += num4 + 22f;
		BackButton("credits:back", Strings.MenuScene.Back, num5, num6 + 3f, num3);
	}

	private static void EnsureCredits()
	{
		if (_creditsRows != null)
		{
			return;
		}
		List<CreditsRow> list = new List<CreditsRow>();
		try
		{
			string chemin = Path.Combine(ClientConfiguration.ResourcesDirectory, "credits.json");
			foreach (Credits.CreditsLine item in (Coffre.Existe(chemin) ? JsonConvert.DeserializeObject<Credits>(Coffre.LireTexte(chemin)) : null)?.Lines ?? new List<Credits.CreditsLine>())
			{
				string text = item.Text?.Trim() ?? string.Empty;
				Color textColor = item.TextColor;
				Color color = (((object)textColor != null) ? new Color(255, textColor.R, textColor.G, textColor.B) : MenuDraw.Mist);
				string alignment = item.Alignment;
				MenuAlign menuAlign = ((alignment == "center") ? MenuAlign.Center : ((alignment == "right") ? MenuAlign.Right : MenuAlign.Left));
				MenuAlign align = menuAlign;
				if (text.Length == 0)
				{
					list.Add(new CreditsRow(string.Empty, MenuFont.ChakraMedium, 17f, 14f, 0f, color, align, 0f));
					continue;
				}
				string font = item.Font;
				if (font != null && font.Contains("refuel", StringComparison.OrdinalIgnoreCase))
				{
					bool flag = item.Size >= 20;
					list.Add(new CreditsRow(MenuDraw.Upper(text), MenuFont.Saira, flag ? 40 : 26, flag ? 44 : 30, flag ? 0.02f : 0.04f, color, align, (!flag) ? 6 : 0));
					continue;
				}
				int num = ((item.Size <= 11) ? 15 : 18);
				float lineHeight = (float)num * 1.45f;
				foreach (string item2 in MenuDraw.Wrap(text, MenuFont.ChakraMedium, num, 508f))
				{
					list.Add(new CreditsRow(item2, MenuFont.ChakraMedium, num, lineHeight, 0f, color, align, 0f));
				}
			}
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Could not read the credits");
		}
		_creditsRows = list;
		_creditsHeight = list.Sum((CreditsRow row) => row.Before + row.LineHeight);
	}

	private void UpdateCredits(long now)
	{
		long num = ((_creditsLastUpdate == 0L) ? 0 : Math.Min(100L, now - _creditsLastUpdate));
		_creditsLastUpdate = now;
		int scrollWheelValue = Mouse.GetState().ScrollWheelValue;
		int num2 = ((_lastWheel != int.MinValue) ? (scrollWheelValue - _lastWheel) : 0);
		_lastWheel = scrollWheelValue;
		if (_screen != MenuScreen.Credits || _creditsRows == null || _creditsHeight <= 420f)
		{
			return;
		}
		float num3 = _creditsHeight - 420f;
		if (num2 != 0 && _creditsViewportRect.Contains(_mouseX, _mouseY))
		{
			_creditsOffset = Math.Clamp(_creditsOffset - (float)num2 * 0.4f, 0f, num3);
			_creditsHoldUntil = now + 2500;
			_creditsRewind = false;
		}
		else
		{
			if (now < _creditsHoldUntil)
			{
				return;
			}
			if (_creditsRewind)
			{
				_creditsRewind = false;
				_creditsOffset = 0f;
				_creditsHoldUntil = now + 1800;
				return;
			}
			_creditsOffset += (float)num * 0.024f;
			if (_creditsOffset >= num3)
			{
				_creditsOffset = num3;
				_creditsRewind = true;
				_creditsHoldUntil = now + 3000;
			}
		}
	}

	private void OuvrirBienvenueSiBesoin(long now)
	{
		if (!_bienvenueFaite)
		{
			_bienvenueFaite = true;
			if (!string.Equals(Globals.Database?.LoadPreference("BienvenueAlpha"), "alpha-1", StringComparison.Ordinal))
			{
				_bienvenueNePlus = false;
				OpenModal(ModalKind.Bienvenue, now, sound: false);
			}
		}
	}

	private void RetenirBienvenue()
	{
		if (_bienvenueNePlus)
		{
			Globals.Database?.SavePreference("BienvenueAlpha", "alpha-1");
		}
	}

	private void DrawBienvenue(long now, float alpha)
	{
		List<List<string>> list = new LocalizedString[4]
		{
			Strings.BienvenueAlpha.Point1,
			Strings.BienvenueAlpha.Point2,
			Strings.BienvenueAlpha.Point3,
			Strings.BienvenueAlpha.Point4
		}.Select((LocalizedString point) => MenuDraw.Wrap(point.ToString(), MenuFont.ChakraMedium, 20f, 692f)).ToList();
		float num = list.Sum((List<string> lignes) => (float)lignes.Count * 29f) + (float)(list.Count - 1) * 14f;
		float num2 = 121.1f + num + 30f + 22f + 30f + 64f + 42f;
		float num3 = MenuDraw.Width / 2f - 410f;
		float num4 = MenuDraw.Height / 2f - num2 / 2f + (1f - _modalLift.Value(now)) * 18f;
		GlassPanel(num3, num4, 820f, num2, 26f, 0.8f, alpha);
		float num5 = num3 + 50f;
		float num6 = num4 + 46f;
		PanelTitle(Strings.BienvenueAlpha.Titre, num5, num6, 720f, alpha);
		num6 += 75.1f;
		foreach (List<string> item in list)
		{
			Diamond(num5 + 7f, num6 + 14.5f, 9f, MenuDraw.Alpha(MenuDraw.Ki, alpha));
			for (int num7 = 0; num7 < item.Count; num7++)
			{
				MenuDraw.Text(item[num7], MenuFont.ChakraMedium, 20f, num5 + 28f, num6 + (float)num7 * 29f, MenuDraw.Alpha(MenuDraw.Mist, alpha), MenuAlign.Left, 0f, 29f);
			}
			num6 += (float)item.Count * 29f + 14f;
		}
		num6 += 16f;
		Checkbox("modal:bienvenue:case", num5, num6, _bienvenueNePlus, Strings.BienvenueAlpha.NePlusAfficher, alpha);
		num6 += 52f;
		Hollow("modal:discord", num5, num6, 352f, 64f, Strings.BienvenueAlpha.OuvrirDiscord, 30f, alpha, 16f, danger: false);
		Cta("modal:cancel", num5 + 352f + 16f, num6, 352f, 64f, Strings.BienvenueAlpha.Jouer, 30f, alpha, now);
	}

	private static MenuClassInfo? ClassInfoFor(string? className)
	{
		if (!string.IsNullOrWhiteSpace(className))
		{
			return MenuContent.Current.Classes.FirstOrDefault((MenuClassInfo info) => info.Matches(className));
		}
		return null;
	}

	private static string TierOf(int level)
	{
		LocalizedString localizedString = ((level >= 45) ? ((level < 75) ? Strings.MenuScene.TierMastery : Strings.MenuScene.TierSummit) : ((level < 20) ? Strings.MenuScene.TierBorrowedBody : Strings.MenuScene.TierFirstAwakening));
		return localizedString;
	}

	private string RandomTip()
	{
		List<string> tips = MenuContent.Current.Tips;
		if (tips.Count != 0)
		{
			return tips[_random.Next(tips.Count)];
		}
		return string.Empty;
	}

	private static MenuCharacterLook LookOf(CharacterSelectionPreviewMetadata preview)
	{
		return new MenuCharacterLook(preview.Sprite ?? string.Empty, (preview.Equipment ?? Array.Empty<EquipmentFragment>()).Select((EquipmentFragment fragment) => (Paperdolls.ForClassNamed(fragment?.Name ?? string.Empty, preview.Class), fragment?.RenderColor ?? Color.White)).ToList());
	}

	public void ShowCharacters(IReadOnlyList<CharacterSelectionPreviewMetadata> previews, string? username)
	{
		long now = Now;
		Guid? guid = CurrentPreview?.Id;
		int num = 0;
		CharacterSelectionPreviewMetadata[] array = new CharacterSelectionPreviewMetadata[previews.Count];
		foreach (CharacterSelectionPreviewMetadata preview in previews)
		{
			array[num] = preview;
			num++;
		}
		_previews = array;
		if (!string.IsNullOrWhiteSpace(username))
		{
			_sessionUsername = username;
		}
		_sessionActive = true;
		_forceCreation = false;
		_pendingAction = null;
		ClearBusy();
		if (!Globals.JoiningGame)
		{
			MenuLoadingScreen.Stop();
			OuvrirBienvenueSiBesoin(now);
		}
		string pendingDeleteName = _pendingDeleteName;
		if (pendingDeleteName != null)
		{
			bool flag = _previews.Any((CharacterSelectionPreviewMetadata preview) => preview.Id == _pendingDeleteId);
			Toast((flag && _deleteMessage != null) ? _deleteMessage : Strings.MenuScene.Deleted.ToString(pendingDeleteName));
			_pendingDeleteName = null;
			_deleteMessage = null;
		}
		_previewIndex = Math.Max(0, Array.FindIndex(_previews, (CharacterSelectionPreviewMetadata preview) => preview.Id != Guid.Empty));
		if (_screen == MenuScreen.Selection)
		{
			CharacterSelectionPreviewMetadata? currentPreview = CurrentPreview;
			if (currentPreview.HasValue && currentPreview.GetValueOrDefault().Id != guid)
			{
				Materialize(now);
			}
			return;
		}
		MenuScreen screen = _screen;
		if ((uint)(screen - 3) <= 1u)
		{
			Flash(0.22f, now);
		}
		Sound("menu_open.wav");
		Go(MenuScreen.Selection, now, sound: false);
	}

	public void ShowCreation(bool force)
	{
		long now = Now;
		_forceCreation = force;
		_sessionActive = true;
		_pendingAction = null;
		ClearBusy();
		OuvrirBienvenueSiBesoin(now);
		MenuScreen screen = _screen;
		if ((uint)(screen - 3) <= 1u)
		{
			Flash(0.22f, now);
		}
		if (_cameFromRegistration)
		{
			_cameFromRegistration = false;
			Toast(Strings.MenuScene.AccountCreated);
		}
		Sound("menu_open.wav");
		Go(MenuScreen.Creation, now, sound: false);
	}

	private void OnCharacterScreen(MenuScreen previous, MenuScreen screen, long now)
	{
		bool flag = screen == MenuScreen.Selection;
		bool flag2 = screen == MenuScreen.Creation;
		bool flag3 = flag || flag2;
		bool flag4 = flag3;
		bool flag5 = flag4;
		if (flag5)
		{
			bool flag6 = (uint)(previous - 7) <= 1u;
			flag5 = !flag6;
		}
		bool flag7 = flag5;
		_stageAlpha.Set(flag3 ? 1 : 0, now, 500f);
		_headerAlpha.Set(flag3 ? 1 : 0, now, 350f, flag7 ? 200 : 0);
		_headerSlide.Set(flag3 ? 1 : 0, now, 500f, flag7 ? 200 : 0, MenuDraw.EaseSlide);
		_selectionAlpha.Set(flag ? 1 : 0, now, 350f);
		_ficheSlide.Set(flag ? 1 : 0, now, 550f, 0f, MenuDraw.EaseSlide);
		_creationAlpha.Set(flag2 ? 1 : 0, now, 350f);
		if (previous == screen || !flag3)
		{
			return;
		}
		_lastCharacterScreen = screen;
		Materialize(now);
		if (flag2)
		{
			_classes = BuildClasses();
			_classIndex = Math.Clamp(_classIndex, 0, Math.Max(0, _classes.Count - 1));
			_classShift = (from _ in Enumerable.Range(0, ListRows)
				select new SceneTween(0f)).ToArray();
			_characterName.Text = string.Empty;
			_creationError = string.Empty;
			SelectClass(_classIndex, now, sound: false, force: true);
		}
	}

	private void Materialize(long now)
	{
		_materializedAt = now;
		_stage.Direction = 0;
	}

	private static List<CreationClass> BuildClasses()
	{
		List<ClassDescriptor> source = (from descriptor in DatabaseObject<ClassDescriptor>.Lookup.Values.OfType<ClassDescriptor>()
			where !descriptor.Locked
			select descriptor).ToList();
		HashSet<Guid> used = new HashSet<Guid>();
		List<CreationClass> list = new List<CreationClass>();
		foreach (MenuClassInfo @class in MenuContent.Current.Classes)
		{
			ClassDescriptor classDescriptor = null;
			foreach (string name in @class.ClassNames)
			{
				foreach (ClassDescriptor item in source.Where((ClassDescriptor candidate) => MenuClassInfo.SameName(candidate.Name, name)))
				{
					if (classDescriptor == null)
					{
						classDescriptor = item;
					}
					used.Add(item.Id);
				}
			}
			list.Add(Describe(@class, classDescriptor));
		}
		foreach (ClassDescriptor item2 in source.Where((ClassDescriptor candidate) => !used.Contains(candidate.Id)))
		{
			list.Add(Describe(new MenuClassInfo
			{
				Name = Traduction.Contenu(item2.Name),
				ClassName = item2.Name
			}, item2));
		}
		CreationClass creationClass = list.FirstOrDefault((CreationClass entry) => entry.Bodies.Count > 0);
		if ((object)creationClass == null)
		{
			return list;
		}
		int num = 1;
		List<CreationClass> list2 = new List<CreationClass>(num);
		CollectionsMarshal.SetCount(list2, num);
		Span<CreationClass> span = CollectionsMarshal.AsSpan(list2);
		int num2 = 0;
		span[num2] = creationClass;
		num2++;
		return list2;
	}

	private static CreationClass Describe(MenuClassInfo info, ClassDescriptor? descriptor)
	{
		List<CreatorBody> list = CreatorBodies(descriptor);
		if (list != null && list.Count > 0)
		{
			return new CreationClass(info, descriptor, list[0].Index, list[0].Sprite, Array.Empty<(int, string)>(), list);
		}
		int num = -1;
		if (descriptor != null)
		{
			num = descriptor.Sprites.FindIndex((ClassSprite sprite) => sprite.Gender == Gender.Male && !string.IsNullOrEmpty(sprite.Sprite));
			if (num < 0 && descriptor.Sprites.Count > 0)
			{
				num = 0;
			}
		}
		string text = ((num >= 0) ? descriptor.Sprites[num].Sprite : null);
		if (string.IsNullOrEmpty(text) && !string.IsNullOrWhiteSpace(info.Sprite))
		{
			text = info.Sprite;
		}
		if (string.IsNullOrEmpty(text) || GameContentManager.Current.GetTexture(TextureType.Entity, text) == null)
		{
			text = null;
		}
		List<(int, string)> list2 = new List<(int, string)>();
		if (descriptor != null && num >= 0 && text != null)
		{
			list2.Add((num, text));
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text);
			string extension = Path.GetExtension(text);
			for (int num2 = 1; num2 < info.Colors.Count; num2++)
			{
				string name = $"{fileNameWithoutExtension}-c{num2}{extension}";
				int num3 = descriptor.Sprites.FindIndex((ClassSprite candidate) => string.Equals(candidate?.Sprite, name, StringComparison.OrdinalIgnoreCase));
				if (num3 < 0 || GameContentManager.Current.GetTexture(TextureType.Entity, name) == null)
				{
					break;
				}
				list2.Add((num3, name));
			}
		}
		return new CreationClass(info, descriptor, num, text, list2, Array.Empty<CreatorBody>());
	}

	private (int Index, string Sprite)? CurrentOutfit(CreationClass entry)
	{
		if (entry.Outfits.Count != 0)
		{
			return entry.Outfits[Math.Clamp(_outfitIndex, 0, entry.Outfits.Count - 1)];
		}
		return null;
	}

	private (int Index, string Sprite)? CurrentLook(CreationClass entry)
	{
		CreatorBody? creatorBody = CurrentBody(entry);
		if (creatorBody.HasValue)
		{
			CreatorBody valueOrDefault = creatorBody.GetValueOrDefault();
			return (valueOrDefault.Index, valueOrDefault.Sprite);
		}
		return CurrentOutfit(entry);
	}

	private string ClassSmallText(CreationClass entry)
	{
		return (!entry.HasSprite) ? Strings.MenuScene.SpriteComing : (entry.Available ? ((LocalizedString)entry.Info.Technique) : Strings.MenuScene.ClassComing);
	}

	private void SelectClass(int index, long now, bool sound, bool force = false)
	{
		if (_classes.Count == 0)
		{
			return;
		}
		int num = (index % _classes.Count + _classes.Count) % _classes.Count;
		if (num != _classIndex || force)
		{
			_classIndex = num;
			_outfitIndex = 0;
			CreationClass creationClass = _classes[num];
			if (creationClass.Bodies.Count > 0)
			{
				StartCreator(now, force);
			}
			else
			{
				MoveBand(num, creationClass.Info.Name, ClassSmallText(creationClass), now, force);
			}
			_creationError = (creationClass.Available ? string.Empty : ((string)Strings.MenuScene.ClassComingError));
			Materialize(now);
			if (sound)
			{
				Sound("menu_hover.wav");
			}
		}
	}

	private void MoveBand(int row, string name, string small, long now, bool force)
	{
		for (int i = 0; i < _classShift.Length; i++)
		{
			_classShift[i].Set((i == row) ? 10 : 0, now, 220f, 0f, MenuDraw.EaseSlide);
		}
		float item = MenuDraw.Measure(MenuDraw.Upper(name), MenuFont.Saira, 34f, 0.01f).Width;
		float item2 = MenuDraw.Measure(MenuDraw.Upper(small), MenuFont.Chakra, 13f, 0.14f).Width;
		float num = (float)row * ClassRowHeight + (ClassRowHeight - 60f) / 2f;
		float num2 = 14f + MathF.Max(item, item2) + 64f;
		_classBandY.Set(num, now, 220f, 0f, MenuDraw.EaseSlide);
		_classBandWidth.Set(num2, now, 220f, 0f, MenuDraw.EaseSlide);
		if (force && _creationAlpha.Value(now) < 0.01f)
		{
			_classBandY.Snap(num);
			_classBandWidth.Snap(num2);
			SceneTween[] classShift = _classShift;
			foreach (SceneTween sceneTween in classShift)
			{
				sceneTween.Snap(sceneTween.Target);
			}
		}
	}

	private void SelectOutfit(int index, long now)
	{
		if (_classes.Count != 0)
		{
			CreationClass creationClass = _classes[_classIndex];
			if (index >= 0 && index < creationClass.Outfits.Count && index != _outfitIndex)
			{
				_outfitIndex = index;
				_materializedAt = now;
				Sound("menu_hover.wav");
			}
		}
	}

	private void Turn(int sense)
	{
		_stage.Turn(sense);
		Sound("menu_hover.wav");
	}

	private void CyclePreview(int sense, long now)
	{
		if (_previews.Length > 1)
		{
			_previewIndex = ((_previewIndex + sense) % _previews.Length + _previews.Length) % _previews.Length;
			Materialize(now);
			Sound("menu_hover.wav");
		}
	}

	private void SelectSlot(int index, long now)
	{
		if (index >= 0 && index < _previews.Length && !Globals.WaitingOnServer)
		{
			if (_previews[index].Id == Guid.Empty)
			{
				_previewIndex = index;
				RequestCreation(now);
			}
			else if (index != _previewIndex)
			{
				_previewIndex = index;
				Materialize(now);
				Sound("menu_hover.wav");
			}
		}
	}

	private void Play(long now)
	{
		CharacterSelectionPreviewMetadata? currentPreview = CurrentPreview;
		if (currentPreview.HasValue)
		{
			CharacterSelectionPreviewMetadata valueOrDefault = currentPreview.GetValueOrDefault();
			if (!Globals.WaitingOnServer && !MenuLoadingScreen.IsActive)
			{
				Sound("menu_open.wav");
				MenuLoadingScreen.Start(MenuLastPlaces.Get(valueOrDefault.Id), RandomTip());
				ChatboxMsg.ClearMessages();
				PacketSender.SendSelectCharacter(valueOrDefault.Id);
				Globals.WaitingOnServer = true;
			}
		}
	}

	private void RequestCreation(long now)
	{
		if (!Globals.WaitingOnServer)
		{
			Sound("menu_click.wav");
			StartBusy(MenuScreen.Selection, now, Strings.MenuScene.CreateWarrior, 900f);
			PacketSender.SendNewCharacter();
			Globals.WaitingOnServer = true;
		}
	}

	private void ConfirmDelete(long now)
	{
		CloseModal(now, sound: false);
		Sound("menu_back.wav");
		CharacterSelectionPreviewMetadata? currentPreview = CurrentPreview;
		if (currentPreview.HasValue)
		{
			CharacterSelectionPreviewMetadata valueOrDefault = currentPreview.GetValueOrDefault();
			if (!Globals.WaitingOnServer)
			{
				_pendingDeleteName = valueOrDefault.Name;
				_pendingDeleteId = valueOrDefault.Id;
				_deleteMessage = null;
				PacketSender.SendDeleteCharacter(valueOrDefault.Id);
				Globals.WaitingOnServer = true;
			}
		}
	}

	private void Logout(long now)
	{
		Sound("menu_back.wav");
		_pendingToast = Strings.MenuScene.LoggedOut;
		_sessionActive = false;
		_bienvenueFaite = false;
		_previews = Array.Empty<CharacterSelectionPreviewMetadata>();
		_pendingAction = null;
		ClearBusy();
		Main.Logout(characterSelect: false, skipFade: true);
	}

	private void BackFromCreation(long now)
	{
		if (_forceCreation || !_previews.Any((CharacterSelectionPreviewMetadata preview) => preview.Id != Guid.Empty))
		{
			Logout(now);
			return;
		}
		Sound("menu_back.wav");
		ClearBusy();
		Go(MenuScreen.Selection, now, sound: false);
	}

	private void SubmitCreation(long now)
	{
		if (!_busyScreen.HasValue && !Globals.WaitingOnServer && _classes.Count != 0)
		{
			CreationClass creationClass = _classes[_classIndex];
			string text = _characterName.Text?.Trim() ?? string.Empty;
			if (!creationClass.Available)
			{
				FailCreation(Strings.MenuScene.ClassComingError, now);
				return;
			}
			if (!FieldChecking.IsValidUsername(text, Strings.Regex.Username))
			{
				FailCreation(Strings.MenuScene.CharacterNameInvalid, now);
				return;
			}
			_creationError = string.Empty;
			Sound("menu_click.wav");
			StartBusy(MenuScreen.Creation, now, "…", 1300f);
			ChatboxMsg.ClearMessages();
			PacketSender.SendCreateCharacter(text, creationClass.Descriptor.Id, CurrentLook(creationClass)?.Index ?? creationClass.SpriteIndex);
			Globals.WaitingOnServer = true;
		}
	}

	private void FailCreation(string message, long now)
	{
		_creationError = message;
		_creationShakeAt = now;
		ClearBusy();
		Sound("menu_back.wav");
	}

	private void UpdateCharacters(long now)
	{
		float num = ((_stageLastUpdate == 0L) ? 0f : Math.Min(0.1f, (float)(now - _stageLastUpdate) / 1000f));
		_stageLastUpdate = now;
		float num2 = ((_screen == MenuScreen.Creation) ? 960f : 760f);
		_stageX += (num2 - _stageX) * (1f - MathF.Exp((0f - num) * 6f));
		string text;
		if (Globals.JoiningGame)
		{
			_sessionActive = false;
			if (!MenuLoadingScreen.IsActive)
			{
				if (_screen == MenuScreen.Selection)
				{
					CharacterSelectionPreviewMetadata? currentPreview = CurrentPreview;
					if (currentPreview.HasValue)
					{
						text = MenuLastPlaces.Get(currentPreview.GetValueOrDefault().Id);
						goto IL_00be;
					}
				}
				text = string.Empty;
				goto IL_00be;
			}
		}
		else
		{
			MenuScreen screen = _screen;
			bool flag = (uint)(screen - 7) <= 1u;
			if (flag && !Intersect.Client.Networking.Network.IsConnected && !MenuLoadingScreen.IsActive)
			{
				_sessionActive = false;
				_bienvenueFaite = false;
				_previews = Array.Empty<CharacterSelectionPreviewMetadata>();
				ClearBusy();
				Go(MenuScreen.Main, now, sound: false);
			}
		}
		goto IL_011d;
		IL_011d:
		bool flag2 = _classes.Count > 0 && _classIndex < _classes.Count;
		bool flag3 = _screen switch
		{
			MenuScreen.Selection => CurrentPreview.HasValue, 
			MenuScreen.Creation => flag2 && _classes[_classIndex].HasSprite, 
			_ => false, 
		};
		_turnAlpha.Set(flag3 ? 1 : 0, now, 300f);
		_comingAlpha.Set((_screen == MenuScreen.Creation && flag2 && !_classes[_classIndex].HasSprite) ? 1 : 0, now, 250f);
		return;
		IL_00be:
		string destination = text;
		MenuLoadingScreen.Start(destination, RandomTip());
		goto IL_011d;
	}

	private bool OnCharacterClick(string id, long now)
	{
		if (id.StartsWith("slot:", StringComparison.Ordinal))
		{
			string text = id;
			if (int.TryParse(text.Substring(5, text.Length - 5), out var result))
			{
				if (_screen == MenuScreen.Selection)
				{
					SelectSlot(result, now);
				}
				return true;
			}
		}
		if (id.StartsWith("class:", StringComparison.Ordinal))
		{
			string text = id;
			if (int.TryParse(text.Substring(6, text.Length - 6), out var result2))
			{
				if (_screen == MenuScreen.Creation)
				{
					SelectClass(result2, now, sound: true);
				}
				return true;
			}
		}
		if (id.StartsWith("outfit:", StringComparison.Ordinal))
		{
			string text = id;
			if (int.TryParse(text.Substring(7, text.Length - 7), out var result3))
			{
				if (_screen == MenuScreen.Creation)
				{
					SelectOutfit(result3, now);
				}
				return true;
			}
		}
		if (OnCreatorClick(id, now))
		{
			return true;
		}
		switch (id)
		{
		case "select:play":
			Play(now);
			return true;
		case "select:create":
			RequestCreation(now);
			return true;
		case "select:delete":
			if (CurrentPreview.HasValue && !Globals.WaitingOnServer)
			{
				OpenModal(ModalKind.Delete, now);
			}
			return true;
		case "select:logout":
			Logout(now);
			return true;
		case "turn:left":
			Turn(-1);
			return true;
		case "turn:right":
			Turn(1);
			return true;
		case "create:submit":
			SubmitCreation(now);
			return true;
		case "create:back":
			BackFromCreation(now);
			return true;
		default:
			return false;
		}
	}

	private bool OnCharacterKey(Intersect.Client.Framework.GenericClasses.Keys key, long now)
	{
		if (_screen == MenuScreen.Selection)
		{
			switch (key)
			{
			case Intersect.Client.Framework.GenericClasses.Keys.Left:
			case Intersect.Client.Framework.GenericClasses.Keys.Right:
				if (CurrentPreview.HasValue)
				{
					Turn((key != Intersect.Client.Framework.GenericClasses.Keys.Left) ? 1 : (-1));
				}
				return true;
			case Intersect.Client.Framework.GenericClasses.Keys.Up:
			case Intersect.Client.Framework.GenericClasses.Keys.Down:
				CyclePreview((key == Intersect.Client.Framework.GenericClasses.Keys.Down) ? 1 : (-1), now);
				return true;
			case Intersect.Client.Framework.GenericClasses.Keys.Return:
				if (CurrentPreview.HasValue)
				{
					Play(now);
				}
				else
				{
					RequestCreation(now);
				}
				return true;
			case Intersect.Client.Framework.GenericClasses.Keys.Escape:
				Logout(now);
				return true;
			default:
				return false;
			}
		}
		if (_screen != MenuScreen.Creation)
		{
			return false;
		}
		bool anyFieldFocused = AnyFieldFocused;
		bool creatorMode = CreatorMode;
		int num;
		switch (key)
		{
		case Intersect.Client.Framework.GenericClasses.Keys.D1:
		case Intersect.Client.Framework.GenericClasses.Keys.NumPad1:
			num = 0;
			break;
		case Intersect.Client.Framework.GenericClasses.Keys.D2:
		case Intersect.Client.Framework.GenericClasses.Keys.NumPad2:
			num = 1;
			break;
		case Intersect.Client.Framework.GenericClasses.Keys.D3:
		case Intersect.Client.Framework.GenericClasses.Keys.NumPad3:
			num = 2;
			break;
		case Intersect.Client.Framework.GenericClasses.Keys.D4:
		case Intersect.Client.Framework.GenericClasses.Keys.NumPad4:
			num = 3;
			break;
		default:
			num = -1;
			break;
		}
		int num2 = num;
		if (num2 >= 0 && !anyFieldFocused)
		{
			if (creatorMode)
			{
				Choose(ref _skin, num2, CreatorSkins.Length, now);
			}
			else
			{
				SelectOutfit(num2, now);
			}
			return true;
		}
		switch (key)
		{
		case Intersect.Client.Framework.GenericClasses.Keys.Escape:
			BackFromCreation(now);
			return true;
		case Intersect.Client.Framework.GenericClasses.Keys.Return:
			SubmitCreation(now);
			return true;
		case Intersect.Client.Framework.GenericClasses.Keys.C:
			if (creatorMode && !anyFieldFocused)
			{
				Choose(ref _hairColor, _hairColor + 1, CreatorHairColors.Length, now);
				return true;
			}
			break;
		case Intersect.Client.Framework.GenericClasses.Keys.Up:
		case Intersect.Client.Framework.GenericClasses.Keys.Down:
			if (!anyFieldFocused)
			{
				if (creatorMode)
				{
					SelectHairstyle(_hairstyle + ((key == Intersect.Client.Framework.GenericClasses.Keys.Down) ? 1 : (-1)), now);
				}
				else
				{
					SelectClass(_classIndex + ((key == Intersect.Client.Framework.GenericClasses.Keys.Down) ? 1 : (-1)), now, sound: true);
				}
				return true;
			}
			break;
		case Intersect.Client.Framework.GenericClasses.Keys.Left:
		case Intersect.Client.Framework.GenericClasses.Keys.Right:
			if (!anyFieldFocused)
			{
				if (_classes.Count > 0 && _classes[_classIndex].HasSprite)
				{
					Turn((key != Intersect.Client.Framework.GenericClasses.Keys.Left) ? 1 : (-1));
				}
				return true;
			}
			break;
		}
		return false;
	}

	private void DrawStage(long now)
	{
		float num = _stageAlpha.Value(now);
		if (_screen == MenuScreen.Selection)
		{
			CharacterSelectionPreviewMetadata? currentPreview = CurrentPreview;
			if (currentPreview.HasValue)
			{
				CharacterSelectionPreviewMetadata valueOrDefault = currentPreview.GetValueOrDefault();
				_stageLook = LookOf(valueOrDefault);
				_stageGreen = string.Equals(ClassInfoFor(valueOrDefault.Class)?.Aura, "verte", StringComparison.OrdinalIgnoreCase);
			}
			else
			{
				_stageLook = null;
			}
		}
		else if (_screen == MenuScreen.Creation && _classes.Count > 0)
		{
			CreationClass creationClass = _classes[_classIndex];
			_stageLook = new MenuCharacterLook(CurrentLook(creationClass)?.Sprite ?? creationClass.Sprite ?? string.Empty, Array.Empty<(string, Color)>());
			_stageGreen = string.Equals(creationClass.Info.Aura, "verte", StringComparison.OrdinalIgnoreCase);
		}
		if (!(num <= 0.001f) && !(_stageLook == null))
		{
			_stage.Draw(now, _stageX, 830f + Middle, _stageLook, num, _materializedAt, _stageGreen);
			float num2 = _comingAlpha.Value(now) * num;
			if (num2 > 0.001f)
			{
				MenuDraw.Text(MenuDraw.Upper(Strings.MenuScene.SpriteComing), MenuFont.Saira, 44f, 960f, 610f + Middle, MenuDraw.Alpha(MenuDraw.White, num2), MenuAlign.Center, 0.12f, 44f);
				MenuDraw.Text(Strings.MenuScene.SpriteComingText, MenuFont.ChakraMedium, 19f, 960f, 664f + Middle, MenuDraw.Alpha(MenuDraw.Mist, num2), MenuAlign.Center, 0f, 26.6f);
			}
		}
	}

	private void DrawCharacters(long now)
	{
		DrawCharacterHeader(now);
		DrawTurnButtons(now);
		DrawSelection(now);
		DrawCreation(now);
		_hitsEnabled = true;
	}

	private void DrawCharacterHeader(long now)
	{
		float num = _headerAlpha.Value(now);
		if (!(num <= 0.001f))
		{
			float x = 96f + (1f - _headerSlide.Value(now)) * -30f;
			bool flag = _lastCharacterScreen == MenuScreen.Creation;
			string text = (flag ? Strings.MenuScene.NewCharacter.ToString() : (string.IsNullOrWhiteSpace(_sessionUsername) ? Strings.MenuScene.LoginKicker.ToString() : Strings.MenuScene.WelcomeBack.ToString(_sessionUsername)));
			Kicker(text, x, 64f, num);
			MenuDraw.Text(MenuDraw.Upper(flag ? Strings.MenuScene.ChooseWarrior : Strings.MenuScene.SelectionTitle), MenuFont.Saira, 64f, x, 88f, MenuDraw.Alpha(MenuDraw.White, num), MenuAlign.Left, 0.01f, 60.8f);
			if (flag)
			{
				MenuDraw.Paragraph(Strings.MenuScene.CreationText, MenuFont.ChakraMedium, 20f, x, 162.8f, 660f, 29f, MenuDraw.Alpha(MenuDraw.Mist, num));
			}
		}
	}

	private void DrawTurnButtons(long now)
	{
		float num = _turnAlpha.Value(now);
		if (!(num <= 0.001f))
		{
			MenuScreen screen = _screen;
			bool hitsEnabled = (uint)(screen - 7) <= 1u;
			_hitsEnabled = hitsEnabled;
			float y = 650f + Middle;
			RoundButton("turn:left", _stageX - 270f, y, left: true, num);
			RoundButton("turn:right", _stageX + 214f, y, left: false, num);
			_hitsEnabled = true;
		}
	}

	private void RoundButton(string id, float x, float y, bool left, float alpha)
	{
		bool flag = IsHovered(id);
		MenuDraw.Bevel(x, y, 56f, 56f, 12f, MenuDraw.Rgba(10, 13, 22, 0.55f * alpha));
		MenuDraw.BevelOutline(x, y, 56f, 56f, 12f, flag ? MenuDraw.Rgba(255, 172, 82, 0.6f * alpha) : MenuDraw.Rgba(255, 255, 255, 0.2f * alpha), 1f);
		Chevron(x + 28f, y + 28f, 22f, left, MenuDraw.Alpha(flag ? MenuDraw.KiLight : MenuDraw.White, alpha), 2.4f);
		Hit(id, x, y, 56f, 56f);
	}

	private static float Fact(string label, string value, float x, float y, float alpha)
	{
		MenuDraw.Rect(x, y, 2f, 48.2f, MenuDraw.Rgba(255, 172, 82, 0.7f * alpha));
		string text = MenuDraw.Upper(label);
		float item = MenuDraw.Measure(text, MenuFont.Chakra, 16f, 0.18f).Width;
		Label(label, x + 16f, y, alpha);
		float y2 = MenuDraw.Text(value, MenuFont.Chakra, 21f, x + 16f, y + 23f, MenuDraw.Alpha(MenuDraw.White, alpha), MenuAlign.Left, 0f, 25.2f);
		return 16f + MathF.Max(item + 2.88f, y2);
	}

	private void DrawSelection(long now)
	{
		float num = _selectionAlpha.Value(now);
		if (num <= 0.001f)
		{
			return;
		}
		_hitsEnabled = _screen == MenuScreen.Selection;
		float num2 = 1060f + (1f - _ficheSlide.Value(now)) * 46f;
		float num3 = 404f + Middle;
		bool flag = _busyScreen == MenuScreen.Selection;
		DrawSlots(num2, num3 - 104f, num);
		CharacterSelectionPreviewMetadata? currentPreview = CurrentPreview;
		if (currentPreview.HasValue)
		{
			CharacterSelectionPreviewMetadata valueOrDefault = currentPreview.GetValueOrDefault();
			MenuClassInfo menuClassInfo = ClassInfoFor(valueOrDefault.Class);
			Kicker(Strings.MenuScene.YourWarrior, num2, num3, num);
			float num4 = ((valueOrDefault.Name.Length > 9) ? 96f : 132f);
			MenuDraw.Text(MenuDraw.Upper(valueOrDefault.Name), MenuFont.Saira, num4, num2, num3 + 24f, MenuDraw.Alpha(MenuDraw.White, num), MenuAlign.Left, 0.005f, num4 * 0.88f);
			float num5 = num3 + 24f + num4 * 0.88f + 8f;
			float num6 = MenuDraw.Text(MenuDraw.Upper(menuClassInfo?.Name ?? valueOrDefault.Class), MenuFont.Saira, 34f, num2, num5 + 0.5f, MenuDraw.Alpha(MenuDraw.Mist, num), MenuAlign.Left, 0.06f, 34f);
			string text = Strings.MenuScene.LevelShort.ToString(valueOrDefault.Level);
			float item = MenuDraw.Measure(text, MenuFont.Saira, 22f, 0.08f).Width;
			float num7 = num2 + num6 + 2.04f + 16f;
			MenuDraw.Bevel(num7, num5, item + 24f + 1.76f, 35f, 8f, MenuDraw.Alpha(MenuDraw.Gold, num));
			MenuDraw.Text(text, MenuFont.Saira, 22f, num7 + 12f, num5 + 7f, MenuDraw.Alpha(MenuDraw.Night, num), MenuAlign.Left, 0.08f, 22f);
			float num8 = num5 + 35f + 32f;
			string text2 = MenuLastPlaces.Get(valueOrDefault.Id);
			string value = (string.IsNullOrWhiteSpace(text2) ? Strings.MenuScene.UnknownPlace.ToString() : text2);
			float num9 = num2;
			num9 += Fact(Strings.MenuScene.LastPosition, value, num9, num8, num) + 38f;
			num9 += Fact(Strings.MenuScene.Tier, TierOf(valueOrDefault.Level), num9, num8, num) + 38f;
			if (!string.IsNullOrWhiteSpace(menuClassInfo?.Technique))
			{
				Fact(Strings.MenuScene.SignatureTechnique, menuClassInfo.Technique, num9, num8, num);
			}
			float num10 = num8 + 48.2f + 40f;
			Cta("select:play", num2, num10, 460f, 76f, Strings.MenuScene.Play, 38f, num, now, !Globals.WaitingOnServer);
			float num11 = num10 + 76f + 5f + 24f;
			string text3 = MenuDraw.Upper(Strings.MenuScene.Delete);
			float item2 = MenuDraw.Measure(text3, MenuFont.Saira, 22f, 0.06f).Width;
			float num12 = item2 + 48f + 1.3199999f + 2f;
			Hollow("select:delete", num2, num11, num12, 48f, Strings.MenuScene.Delete, 22f, num, 12f);
			BackButton("select:logout", Strings.MenuScene.Logout, num2 + num12 + 30f, num11 + 16f, num);
		}
		else
		{
			Kicker(Strings.MenuScene.EmptyKicker, num2, num3, num);
			MenuDraw.Text(MenuDraw.Upper(Strings.MenuScene.EmptyTitle), MenuFont.Saira, 104f, num2, num3 + 24f, MenuDraw.Alpha(MenuDraw.White, num), MenuAlign.Left, 0.005f, 91.52f);
			float num13 = num3 + 24f + 91.52f + 8f;
			num13 += MenuDraw.Paragraph(Strings.MenuScene.EmptyText, MenuFont.ChakraMedium, 21f, num2, num13, 560f, 30.45f, MenuDraw.Alpha(MenuDraw.Mist, num)) + 36f;
			Cta("select:create", num2, num13, 460f, 76f, Strings.MenuScene.CreateWarrior, 38f, num, now, !Globals.WaitingOnServer || flag, flag ? BusyCharge(now) : 0f);
			num13 += 105f;
			BackButton("select:logout", Strings.MenuScene.Logout, num2, num13 + 1f, num);
		}
	}

	private void DrawSlots(float x, float y, float alpha)
	{
		if (_previews.Length <= 1 || alpha <= 0.001f)
		{
			return;
		}
		float num = (460f - 10f * (float)(_previews.Length - 1)) / (float)_previews.Length;
		int num2 = Math.Clamp(_previewIndex, 0, _previews.Length - 1);
		Label(Strings.MenuScene.SlotsLabel, x, y - 24f, alpha);
		for (int i = 0; i < _previews.Length; i++)
		{
			CharacterSelectionPreviewMetadata characterSelectionPreviewMetadata = _previews[i];
			string id = $"slot:{i}";
			float num3 = x + (float)i * (num + 10f);
			bool flag = i == num2;
			bool flag2 = IsHovered(id);
			MenuDraw.Bevel(num3, y, num, 64f, 10f, flag ? MenuDraw.Rgba(255, 122, 26, 0.16f * alpha) : MenuDraw.Rgba(10, 13, 22, 0.55f * alpha));
			MenuDraw.BevelOutline(num3, y, num, 64f, 10f, flag ? MenuDraw.Alpha(MenuDraw.KiLight, alpha) : (flag2 ? MenuDraw.Rgba(255, 172, 82, 0.6f * alpha) : MenuDraw.Rgba(255, 255, 255, 0.18f * alpha)), 1f);
			if (characterSelectionPreviewMetadata.Id == Guid.Empty)
			{
				MenuDraw.Text(MenuDraw.Upper(Strings.MenuScene.SlotCreate), MenuFont.Saira, 20f, num3 + num / 2f, y, MenuDraw.Alpha(flag2 ? MenuDraw.KiLight : MenuDraw.Steel, alpha), MenuAlign.Center, 0.06f, 64f);
			}
			else
			{
				string text = MenuDraw.Upper(characterSelectionPreviewMetadata.Name);
				float num4 = 22f;
				while (num4 > 14f && MenuDraw.Measure(text, MenuFont.Saira, num4, 0.02f).Width > num - 28f)
				{
					num4 -= 2f;
				}
				MenuDraw.Text(text, MenuFont.Saira, num4, num3 + 14f, y + 12f, MenuDraw.Alpha((flag || flag2) ? MenuDraw.White : MenuDraw.Mist, alpha), MenuAlign.Left, 0.02f, 22f);
				MenuDraw.Text(Strings.MenuScene.LevelShort.ToString(characterSelectionPreviewMetadata.Level), MenuFont.Chakra, 16f, num3 + 14f, y + 38f, MenuDraw.Alpha(flag ? MenuDraw.Gold : MenuDraw.Mist, alpha), MenuAlign.Left, 0f, 16f);
			}
			Hit(id, num3, y, num, 64f);
		}
	}

	private void DrawCreation(long now)
	{
		float num = _creationAlpha.Value(now);
		if (!(num <= 0.001f) && _classes.Count != 0)
		{
			_hitsEnabled = _screen == MenuScreen.Creation;
			DrawClassList(now, num);
			DrawClassDetails(num);
			if (CreatorMode)
			{
				DrawCreatorChoices(num);
			}
			else
			{
				DrawOutfits(num);
			}
			DrawCreationValidation(now, num);
			BackButton("create:back", Strings.MenuScene.Back, 96f, 950f + Middle + 1f, num);
		}
	}

	private void DrawClassList(long now, float alpha)
	{
		float num = 300f + Middle;
		KiBand(178f, num + _classBandY.Value(now), _classBandWidth.Value(now), 60f, alpha, now, 1);
		bool creatorMode = CreatorMode;
		if (creatorMode)
		{
			Label(Strings.MenuScene.Coiffure, 96f, num - 26f, alpha);
		}
		int listRows = ListRows;
		string text2;
		string text3;
		string id;
		bool flag;
		float num2;
		float x;
		Color color;
		float num4;
		for (int i = 0; i < listRows; x = 192f + ((i < _classShift.Length) ? _classShift[i].Value(now) : 0f), color = (flag ? MenuDraw.Alpha(MenuDraw.Night, alpha) : (IsHovered(id) ? MenuDraw.Alpha(MenuDraw.White, alpha) : MenuDraw.Rgba(242, 242, 242, 0.76f * alpha))), num4 = num2 + (ClassRowHeight - (float)((text3.Length > 0) ? 52 : 34)) / 2f, MenuDraw.Text(MenuDraw.Upper(text2), MenuFont.Saira, 34f, x, num4, color, MenuAlign.Left, 0.01f, 34f), MenuDraw.Text(MenuDraw.Upper(text3), MenuFont.Chakra, 13f, x, num4 + 39f, flag ? MenuDraw.Rgba(11, 14, 23, 0.72f * alpha) : MenuDraw.Alpha(MenuDraw.Steel, alpha), MenuAlign.Left, 0.14f, 13f), Hit(id, 96f, num2, 560f, ClassRowHeight), i++)
		{
			string text;
			if (creatorMode)
			{
				(text, text2, text3, id, flag) = HairstyleRow(i);
			}
			else
			{
				(text, text2, text3, id, flag) = ClassRow(i);
			}
			num2 = num + (float)i * ClassRowHeight;
			float num3 = num2 + (ClassRowHeight - 64f) / 2f;
			IGameTexture slotRamp = SlotRamp;
			if (slotRamp != null)
			{
				MenuDraw.Renderer.DrawTexture(slotRamp, 0f, 0f, 1f, 256f, 96f * MenuDraw.Scale, num3 * MenuDraw.Scale, 64f * MenuDraw.Scale, 64f * MenuDraw.Scale, MenuDraw.Alpha(Color.White, alpha));
			}
			if (flag)
			{
				OutlineRect(95f, num3 - 1f, 66f, 66f, MenuDraw.Rgba(255, 172, 82, 0.45f * alpha), 1f);
				OutlineRect(96f, num3, 64f, 64f, MenuDraw.Alpha(MenuDraw.KiLight, alpha), 1f);
			}
			else
			{
				OutlineRect(96f, num3, 64f, 64f, MenuDraw.Rgba(255, 255, 255, 0.14f * alpha), 1f);
			}
			if (text != null)
			{
				(IGameTexture, int, int, int)? tuple3 = MenuCharacterStage.HeadCrop(text);
				if (tuple3.HasValue)
				{
					(IGameTexture, int, int, int) valueOrDefault = tuple3.GetValueOrDefault();
					DrawPixelArt(valueOrDefault.Item1, 101f, num3 + 5f, 54f, 54f, alpha, valueOrDefault.Item2, valueOrDefault.Item3, valueOrDefault.Item4, valueOrDefault.Item4);
					continue;
				}
			}
			MenuDraw.Text("?", MenuFont.Saira, 30f, 128f, num3, MenuDraw.Alpha(MenuDraw.Steel, alpha), MenuAlign.Center, 0f, 64f);
		}
	}

	private (string? Sprite, string Name, string Small, string Id, bool Active) ClassRow(int row)
	{
		return (Sprite: _classes[row].Sprite, Name: _classes[row].Info.Name, Small: ClassSmallText(_classes[row]), Id: $"class:{row}", Active: row == _classIndex);
	}

	private void DrawClassDetails(float alpha)
	{
		float num = 290f + Middle;
		CreationClass creationClass = _classes[_classIndex];
		MenuClassInfo info = creationClass.Info;
		if (!string.IsNullOrWhiteSpace(info.Title))
		{
			Kicker(info.Title, 1296f, num, alpha);
		}
		float num2 = ((info.Name.Length > 12) ? 76f : 96f);
		MenuDraw.Text(MenuDraw.Upper(info.Name), MenuFont.Saira, num2, 1296f, num + 24f, MenuDraw.Alpha(MenuDraw.White, alpha), MenuAlign.Left, 0.005f, num2 * 0.92f);
		float num3 = num + 24f + num2 * 0.92f + 14f;
		List<string> list = MenuDraw.Wrap(info.Text, MenuFont.ChakraMedium, 20f, 560f);
		for (int i = 0; i < list.Count; i++)
		{
			MenuDraw.Text(list[i], MenuFont.ChakraMedium, 20f, 1296f, num3 + (float)(i * 29), MenuDraw.Alpha(MenuDraw.Mist, alpha), MenuAlign.Left, 0f, 29f);
		}
		num3 += MathF.Max(87f, list.Count * 29) + 24f;
		MenuDraw.HorizontalRamp(FadeRightRamp, 1296f, num3, 560f, 1f, MenuDraw.Alpha(Color.White, alpha));
		num3 += 25f;
		if (!string.IsNullOrWhiteSpace(info.Technique))
		{
			Label(Strings.MenuScene.SignatureTechnique, 1296f, num3, alpha, null, 0.2f);
			num3 += 23f;
			MenuDraw.Text(MenuDraw.Upper(info.Technique), MenuFont.Saira, 44f, 1296f, num3, MenuDraw.Alpha(MenuDraw.Gold, alpha), MenuAlign.Left, 0.02f, 44f);
			num3 += 70f;
		}
		if (!string.IsNullOrWhiteSpace(info.Passive))
		{
			Label(Strings.MenuScene.Passive, 1296f, num3, alpha, null, 0.2f);
			num3 += 23f;
			List<string> list2 = MenuDraw.Wrap(info.Passive, MenuFont.ChakraMedium, 20f, 560f);
			for (int j = 0; j < list2.Count; j++)
			{
				MenuDraw.Text(list2[j], MenuFont.ChakraMedium, 20f, 1296f, num3 + (float)(j * 29), MenuDraw.Alpha(MenuDraw.White, alpha), MenuAlign.Left, 0f, 29f);
			}
		}
	}

	private void DrawOutfits(float alpha)
	{
		CreationClass creationClass = _classes[_classIndex];
		int num = Math.Min(creationClass.Outfits.Count, creationClass.Info.Colors.Count);
		if (num >= 2)
		{
			(Color, Color, string)[] swatches = (from color in creationClass.Info.Colors.Take(num)
				select (SwatchColor(color.Cloth, alpha), SwatchColor(color.Second, alpha), Name: color.Name)).ToArray();
			SwatchRow(Strings.MenuScene.Outfit, "outfit", swatches, Math.Clamp(_outfitIndex, 0, num - 1), numbered: true, _stageX, 862f + Middle, alpha);
		}
	}

	private static float SwatchRowWidth(int count)
	{
		return (float)count * 60f + (float)(count - 1) * 14f;
	}

	private void SwatchRow(string label, string idPrefix, IReadOnlyList<(Color First, Color Second, string Name)> swatches, int chosenIndex, bool numbered, float centerX, float top, float alpha)
	{
		MenuDraw.Text(MenuDraw.Upper(label), MenuFont.Chakra, 16f, centerX, top, MenuDraw.Alpha(MenuDraw.Mist, alpha), MenuAlign.Center, 0.2f, 16f);
		float num = top + 26f;
		float num2 = centerX - SwatchRowWidth(swatches.Count) / 2f;
		for (int i = 0; i < swatches.Count; i++)
		{
			string id = $"{idPrefix}:{i}";
			bool flag = i == chosenIndex;
			float num3 = num2 + (float)i * 74f;
			float num4 = num - (float)(flag ? 4 : 0);
			if (flag)
			{
				IGameTexture radial = MenuTextures.Radial;
				if (radial != null)
				{
					MenuDraw.Texture(radial, num3 - 18f, num4 - 18f, 96f, 96f, MenuDraw.Rgba(255, 122, 26, 0.55f * alpha));
				}
			}
			Color color = (flag ? MenuDraw.Alpha(MenuDraw.KiLight, alpha) : (IsHovered(id) ? MenuDraw.Rgba(255, 172, 82, 0.6f * alpha) : MenuDraw.Rgba(255, 255, 255, 0.24f * alpha)));
			MenuDraw.Bevel(num3, num4, 60f, 60f, 12f, color);
			SplitSwatch(num3 + 2f, num4 + 2f, 56f, 56f, 11f, swatches[i].First, swatches[i].Second);
			if (numbered)
			{
				MenuDraw.Text((i + 1).ToString(CultureInfo.InvariantCulture), MenuFont.Chakra, 12f, num3 + 60f - 7f, num4 + 60f - 17f, MenuDraw.Rgba(255, 255, 255, 0.85f * alpha), MenuAlign.Right, 0f, 12f);
			}
			Hit(id, num3, num - 4f, 60f, 64f);
		}
		MenuDraw.Text(MenuDraw.Upper(swatches[chosenIndex].Name), MenuFont.Saira, 30f, centerX, num + 60f + 12f, MenuDraw.Alpha(MenuDraw.White, alpha), MenuAlign.Center, 0.06f, 30f);
	}

	private static void SplitSwatch(float x, float y, float width, float height, float cut, Color first, Color second)
	{
		float num = MathF.Round(x * MenuDraw.Scale);
		float num2 = MathF.Round(y * MenuDraw.Scale);
		float num3 = MathF.Round((x + width) * MenuDraw.Scale);
		float num4 = MathF.Round((y + height) * MenuDraw.Scale);
		int num5 = (int)(num4 - num2);
		int num6 = (int)Math.Min(MathF.Round(cut * MenuDraw.Scale), num5 / 2);
		for (int i = 0; i < num5; i++)
		{
			float num7 = num + (float)((i >= num5 - num6) ? (i - (num5 - num6) + 1) : 0);
			float num8 = num3 - (float)((i < num6) ? (num6 - i) : 0);
			float num9 = Math.Clamp(MathF.Round(num + (num3 - num) * (1f - ((float)i + 0.5f) / (float)num5)), num7, num8);
			if (num9 > num7)
			{
				MenuDraw.ScreenRect(num7, num2 + (float)i, num9 - num7, 1f, first);
			}
			if (num8 > num9)
			{
				MenuDraw.ScreenRect(num9, num2 + (float)i, num8 - num9, 1f, second);
			}
		}
	}

	private static Color SwatchColor(string? hex, float alpha)
	{
		string text = (hex ?? string.Empty).Trim().TrimStart('#');
		if (text.Length != 6 || !int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result))
		{
			return MenuDraw.Rgba(128, 128, 128, alpha);
		}
		return MenuDraw.Rgba((result >> 16) & 0xFF, (result >> 8) & 0xFF, result & 0xFF, alpha);
	}

	private void DrawCreationValidation(long now, float alpha)
	{
		float num = 1296f + ShakeOffset(_creationShakeAt, now);
		float num2 = 858f + Middle;
		bool active = _screen == MenuScreen.Creation;
		CreationClass creationClass = _classes[_classIndex];
		Label(Strings.MenuScene.CharacterName, num, num2, alpha);
		FieldBox(_characterName, num, num2 + 23f, 354f, alpha, active, Strings.MenuScene.CharacterNamePlaceholder);
		bool flag = _busyScreen == MenuScreen.Creation;
		Cta("create:submit", num + 354f + 16f, num2 + 23f + 56f - 64f, 190f, 64f, flag ? _busyText : ((string)Strings.MenuScene.Create), 30f, alpha, now, creationClass.Available, flag ? BusyCharge(now) : 0f);
		DrawErrorLines(ErrorLines(_creationError, 560f), num, num2 + 23f + 56f + 10f, alpha);
	}

	private static List<CreatorBody> CreatorBodies(ClassDescriptor? descriptor)
	{
		if (descriptor == null)
		{
			return new List<CreatorBody>();
		}
		List<CreatorBody> list = new List<CreatorBody>();
		HashSet<(int, int, int)> hashSet = new HashSet<(int, int, int)>();
		for (int i = 0; i < descriptor.Sprites.Count; i++)
		{
			string sprite = descriptor.Sprites[i]?.Sprite;
			if (CorpsDuCreateur.TryLire(sprite, out string coiffure, out string couleur, out string peau))
			{
				CreatorBody item = new CreatorBody(Find(CreatorHairstyles, coiffure), Find(CreatorHairColors, couleur), Find(CreatorSkins, peau), i, sprite);
				if (item.Hairstyle >= 0 && item.HairColor >= 0 && item.Skin >= 0 && GameContentManager.Current.GetTexture(TextureType.Entity, item.Sprite) != null && hashSet.Add((item.Hairstyle, item.HairColor, item.Skin)))
				{
					list.Add(item);
				}
			}
		}
		list.Sort((CreatorBody a, CreatorBody b) => (a.Hairstyle, a.HairColor, a.Skin).CompareTo((b.Hairstyle, b.HairColor, b.Skin)));
		return list;
		static int Find(CreatorChoice[] choices, string key)
		{
			return Array.FindIndex(choices, (CreatorChoice choice) => string.Equals(choice.Key, key, StringComparison.OrdinalIgnoreCase));
		}
	}

	private static CreatorBody? NearestBody(CreationClass entry, int hairstyle, int hairColor, int skin)
	{
		if (entry.Bodies.Count != 0)
		{
			return entry.Bodies.MinBy((CreatorBody body) => Math.Abs(body.Hairstyle - hairstyle) * 100 + Math.Abs(body.HairColor - hairColor) * 10 + Math.Abs(body.Skin - skin));
		}
		return null;
	}

	private CreatorBody? CurrentBody(CreationClass entry)
	{
		return NearestBody(entry, _hairstyle, _hairColor, _skin);
	}

	private void StartCreator(long now, bool force)
	{
		_hairstyle = 0;
		_hairColor = 0;
		_skin = 1;
		MoveBand(_hairstyle, CreatorHairstyles[_hairstyle].Name(), string.Empty, now, force);
	}

	private bool Choose(ref int choice, int index, int count, long now)
	{
		int num = (index % count + count) % count;
		if (num == choice)
		{
			return false;
		}
		choice = num;
		_materializedAt = now;
		Sound("menu_hover.wav");
		return true;
	}

	private void SelectHairstyle(int index, long now)
	{
		if (Choose(ref _hairstyle, index, CreatorHairstyles.Length, now))
		{
			MoveBand(_hairstyle, CreatorHairstyles[_hairstyle].Name(), string.Empty, now, force: false);
		}
	}

	private bool OnCreatorClick(string id, long now)
	{
		int num = id.IndexOf(':');
		bool flag = num < 0;
		bool flag2 = flag;
		if (!flag2)
		{
			bool flag3;
			switch (id.Substring(0, num))
			{
			case "coiffure":
			case "cheveux":
			case "peau":
				flag3 = true;
				break;
			default:
				flag3 = false;
				break;
			}
			flag2 = !flag3;
		}
		if (!flag2)
		{
			int num2 = num + 1;
			if (int.TryParse(id.Substring(num2, id.Length - num2), out var result))
			{
				if (_screen != MenuScreen.Creation || !CreatorMode)
				{
					return true;
				}
				switch (id.Substring(0, num))
				{
				case "coiffure":
					SelectHairstyle(result, now);
					break;
				case "cheveux":
					Choose(ref _hairColor, result, CreatorHairColors.Length, now);
					break;
				case "peau":
					Choose(ref _skin, result, CreatorSkins.Length, now);
					break;
				}
				return true;
			}
		}
		return false;
	}

	private (string? Sprite, string Name, string Small, string Id, bool Active) HairstyleRow(int row)
	{
		CreatorBody? creatorBody = NearestBody(_classes[_classIndex], row, _hairColor, _skin);
		object item;
		if (creatorBody.HasValue)
		{
			CreatorBody valueOrDefault = creatorBody.GetValueOrDefault();
			if (valueOrDefault.Hairstyle == row)
			{
				item = valueOrDefault.Sprite;
				goto IL_0049;
			}
		}
		item = null;
		goto IL_0049;
		IL_0049:
		return (Sprite: (string)item, Name: CreatorHairstyles[row].Name(), Small: string.Empty, Id: $"coiffure:{row}", Active: row == _hairstyle);
	}

	private void DrawCreatorChoices(float alpha)
	{
		float num = SwatchRowWidth(CreatorHairColors.Length);
		float num2 = SwatchRowWidth(CreatorSkins.Length);
		float num3 = _stageX - (num + 56f + num2) / 2f;
		float top = 862f + Middle;
		SwatchRow(Strings.MenuScene.Cheveux, "cheveux", Shades(CreatorHairColors, alpha), _hairColor, numbered: false, num3 + num / 2f, top, alpha);
		SwatchRow(Strings.MenuScene.Peau, "peau", Shades(CreatorSkins, alpha), _skin, numbered: true, num3 + num + 56f + num2 / 2f, top, alpha);
	}

	private static (Color First, Color Second, string Name)[] Shades(CreatorChoice[] choices, float alpha)
	{
		return choices.Select((CreatorChoice choice) => (SwatchColor(choice.Swatch, alpha), SwatchColor(choice.Swatch, alpha), choice.Name())).ToArray();
	}

	public MenuScene(MainMenu mainMenu, Canvas canvas)
	{
		_mainMenu = mainMenu;
		_canvas = canvas;
		long now = Now;
		CreateFields();
		Input.KeyDown = (Input.HandleKeyEvent)Delegate.Combine(Input.KeyDown, new Input.HandleKeyEvent(OnKeyDown));
		if (!Globals.JoiningGame)
		{
			MenuLoadingScreen.Stop();
		}
		if (_introPlayed)
		{
			_logo.ShowSettled(now);
			_black.Snap(0f);
			_backdrop.IntroStart = now - 5000;
			MenuScreen lastScreen = _lastScreen;
			if ((uint)(lastScreen - 7) <= 1u && _sessionActive && Intersect.Client.Networking.Network.IsConnected)
			{
				Go(lastScreen, now, sound: false);
				SnapTweens();
			}
			else
			{
				_sessionActive = false;
				_previews = Array.Empty<CharacterSelectionPreviewMetadata>();
				Go(((uint)(lastScreen - 7) <= 1u) ? lastScreen : MenuScreen.Main, now, sound: false);
				SnapTweens();
				Go(MenuScreen.Main, now, sound: false);
			}
			string pendingToast = _pendingToast;
			if (pendingToast != null)
			{
				Toast(pendingToast);
				_pendingToast = null;
			}
		}
		else
		{
			_introStart = now;
			_backdrop.IntroStart = now;
			_black.Snap(1f);
			_black.Set(0f, now + 100, 1300f);
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			Input.KeyDown = (Input.HandleKeyEvent)Delegate.Remove(Input.KeyDown, new Input.HandleKeyEvent(OnKeyDown));
			DetachNetworkEvents();
			DisposeFields();
		}
	}

	private void SnapTweens()
	{
		FieldInfo[] fields = typeof(MenuScene).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
		foreach (FieldInfo fieldInfo in fields)
		{
			object value = fieldInfo.GetValue(this);
			if (!(value is SceneTween sceneTween))
			{
				if (value is SceneTween[] array)
				{
					SceneTween[] array2 = array;
					foreach (SceneTween sceneTween2 in array2)
					{
						sceneTween2.Snap(sceneTween2.Target);
					}
				}
			}
			else
			{
				sceneTween.Snap(sceneTween.Target);
			}
		}
	}

	public void Go(MenuScreen screen, long now, bool sound = true)
	{
		MenuScreen screen2 = _screen;
		_screen = screen;
		_screenSince = now;
		_lastScreen = screen;
		bool flag = (((uint)(screen - 2) <= 4u || screen == MenuScreen.Overlay) ? true : false);
		bool flag2 = flag;
		flag = (uint)(screen - 7) <= 1u;
		bool flag3 = flag;
		SceneTween veil = _veil;
		flag = (uint)screen <= 1u;
		veil.Set(flag ? 0.42f : 1f, now, 700f);
		_blur.Set(flag3 ? 1 : 0, now, 700f);
		_spotlight.Set(flag3 ? 1 : 0, now, 700f);
		_focusX.Set((screen == MenuScreen.Creation) ? 960 : 760, now, 600f, 0f, MenuDraw.EaseSlide);
		SceneTween logoPose = _logoPose;
		flag = (uint)screen <= 1u;
		logoPose.Set((!flag) ? 1 : 0, now, 780f, 0f, MenuDraw.EaseSlide);
		_logoAlpha.Set((!flag3) ? 1 : 0, now, 400f);
		_invite.Set((screen == MenuScreen.Title) ? 1 : 0, now, 450f);
		_chips.Set((screen != MenuScreen.Intro) ? 1 : 0, now, 500f);
		SceneTween version = _version;
		flag = ((screen == MenuScreen.Intro || screen == MenuScreen.Creation) ? true : false);
		version.Set((!flag) ? 1 : 0, now, 500f);
		_keys.Set(((flag2 && screen != MenuScreen.Overlay) || flag3) ? 1 : 0, now, 400f, (screen == MenuScreen.Main && screen2 == MenuScreen.Title) ? 500 : 0);
		OnMainScreen(screen2, screen, now);
		OnAccountScreen(screen2, screen, now);
		OnCharacterScreen(screen2, screen, now);
		bool flag4 = sound && screen2 != screen;
		bool flag5 = flag4;
		if (flag5)
		{
			flag = (uint)(screen - 3) <= 3u;
			flag5 = flag;
		}
		if (flag5)
		{
			Sound("menu_open.wav");
		}
	}

	private void Sound(string file)
	{
		Audio.AddGameSound(file, loop: false);
	}

	private void Flash(float strength, long now)
	{
		_flashAt = now;
		_flashStrength = strength;
	}

	public void Update(float dt)
	{
		long now = Now;
		if (_screen == MenuScreen.Intro)
		{
			long num = now - _introStart;
			if (num >= 950 && !_logo.IsVisible)
			{
				RevealLogo(now);
			}
			if (num >= 1950)
			{
				_introPlayed = true;
				Go(MenuScreen.Title, now);
			}
		}
		Vector2 mousePosition = Globals.InputManager.GetMousePosition();
		GameRenderer renderer = MenuDraw.Renderer;
		_mouseInside = mousePosition.X >= 0f && mousePosition.Y >= 0f && mousePosition.X < (float)renderer.ScreenWidth && mousePosition.Y < (float)renderer.ScreenHeight;
		_mouseX = mousePosition.X / MenuDraw.Scale;
		_mouseY = mousePosition.Y / MenuDraw.Scale;
		_backdrop.Update(dt, _mouseX, _mouseY, _mouseInside);
		UpdateMouse(now);
		UpdateMain(now);
		UpdateAccount(now);
		UpdateCharacters(now);
		UpdateOverlays(now);
	}

	private void RevealLogo(long now)
	{
		_logo.Appear(now);
		_backdrop.Shake(now);
		Flash(0.28f, now);
		Sound("menu_open.wav");
	}

	private void SkipIntro(long now)
	{
		if (!_logo.IsVisible)
		{
			RevealLogo(now - 400);
		}
		_introStart = Math.Min(_introStart, now - 1950);
		_black.Snap(0f);
		_backdrop.IntroStart = Math.Min(_backdrop.IntroStart, now - 1950);
	}

	private void UpdateMouse(long now)
	{
		_hits.Clear();
		_hits.AddRange(_drawnHits);
		GameInput inputManager = Globals.InputManager;
		bool flag = inputManager.IsMouseButtonDown(MouseButton.Left);
		bool flag2 = inputManager.WasMouseButtonDown(MouseButton.Left);
		if (EngineWindowOpen || MenuLoadingScreen.IsActive || _quitting)
		{
			SetHovered(null, now);
			_pressed = null;
			return;
		}
		string text = null;
		for (int num = _hits.Count - 1; num >= 0; num--)
		{
			if (_hits[num].Rect.Contains(_mouseX, _mouseY))
			{
				text = _hits[num].Id;
				break;
			}
		}
		SetHovered(text, now);
		if (flag && !flag2)
		{
			_pressed = text;
			if (_screen == MenuScreen.Intro)
			{
				SkipIntro(now);
			}
			else if (_screen == MenuScreen.Title && text == null)
			{
				EnterMenu(now);
			}
		}
		else if (!flag && flag2)
		{
			if (_pressed != null && _pressed == text)
			{
				OnClick(_pressed, now);
			}
			_pressed = null;
		}
	}

	private void SetHovered(string? id, long now)
	{
		if (!(id == _hovered))
		{
			_hovered = id;
			_hoveredSince = now;
			if (id != null)
			{
				OnHover(id, now);
			}
		}
	}

	private bool IsHovered(string id)
	{
		return _hovered == id;
	}

	private bool IsPressed(string id)
	{
		if (_pressed == id)
		{
			return _hovered == id;
		}
		return false;
	}

	private void Hit(string id, float x, float y, float width, float height)
	{
		if (_hitsEnabled)
		{
			_drawnHits.Add((id, new FloatRect(x, y, width, height)));
		}
	}

	private void OnHover(string id, long now)
	{
		if (id.StartsWith("nav:", StringComparison.Ordinal) && _screen == MenuScreen.Main)
		{
			if (int.TryParse(id.Substring(4, id.Length - 4), out var result))
			{
				SelectNav(result, now, sound: true);
			}
		}
	}

	private void OnClick(string id, long now)
	{
		if (!OnMainClick(id, now) && !OnAccountClick(id, now) && !OnCharacterClick(id, now))
		{
			OnOverlayClick(id, now);
		}
	}

	private void OnKeyDown(Intersect.Client.Framework.GenericClasses.Keys modifier, Intersect.Client.Framework.GenericClasses.Keys key)
	{
		bool disposed = _disposed;
		bool flag = disposed;
		bool flag2;
		if (!flag)
		{
			flag2 = (((uint)key <= 2u || key == Intersect.Client.Framework.GenericClasses.Keys.MButton) ? true : false);
			flag = flag2;
		}
		if (flag || Globals.GameState != GameStates.Menu)
		{
			return;
		}
		switch (key)
		{
		case Intersect.Client.Framework.GenericClasses.Keys.ShiftKey:
		case Intersect.Client.Framework.GenericClasses.Keys.ControlKey:
		case Intersect.Client.Framework.GenericClasses.Keys.Menu:
		case Intersect.Client.Framework.GenericClasses.Keys.CapsLock:
		case Intersect.Client.Framework.GenericClasses.Keys.LWin:
		case Intersect.Client.Framework.GenericClasses.Keys.RWin:
		case Intersect.Client.Framework.GenericClasses.Keys.LShiftKey:
		case Intersect.Client.Framework.GenericClasses.Keys.RShiftKey:
		case Intersect.Client.Framework.GenericClasses.Keys.LControlKey:
		case Intersect.Client.Framework.GenericClasses.Keys.RControlKey:
		case Intersect.Client.Framework.GenericClasses.Keys.LMenu:
		case Intersect.Client.Framework.GenericClasses.Keys.RMenu:
			flag2 = true;
			break;
		default:
			flag2 = false;
			break;
		}
		if (flag2)
		{
			return;
		}
		long now = Now;
		if (MenuLoadingScreen.IsActive || EngineWindowOpen)
		{
			return;
		}
		switch (_screen)
		{
		case MenuScreen.Intro:
			SkipIntro(now);
			return;
		case MenuScreen.Title:
			EnterMenu(now);
			return;
		}
		if (!OnOverlayKey(key, now))
		{
			if (OnMainKey(key, now) || OnAccountKey(key, now))
			{
				_ = 1;
			}
			else
				OnCharacterKey(key, now);
		}
	}

	public void Draw()
	{
		if (!_disposed)
		{
			long now = Now;
			MenuDraw.Begin();
			MenuTextures.EnsureSourceTextures();
			_drawnHits.Clear();
			_hitsEnabled = true;
			float veil = _veil.Value(now);
			float blur = _blur.Value(now);
			if (NeedsGlass(now))
			{
				_backdrop.PaintGlass(now, veil, blur);
			}
			else
			{
				MenuDraw.GlassReady = false;
			}
			_backdrop.Draw(now, veil, blur, _spotlight.Value(now), _focusX.Value(now));
			DrawStage(now);
			float k = _logoPose.Value(now);
			MenuLogo logo = _logo;
			long now2 = now;
			float originX = MenuDraw.Lerp(50f, 36f, k);
			float originY = MenuDraw.Lerp(26f, -14f, k);
			float scale = MenuDraw.Lerp(1f, 0.6f, k);
			float alpha = _logoAlpha.Value(now);
			MenuScreen screen = _screen;
			bool flag = (uint)(screen - 7) <= 1u;
			logo.Draw(now2, originX, originY, scale, alpha, !flag);
			DrawTitle(now);
			DrawNav(now);
			DrawAccount(now);
			DrawCharacters(now);
			DrawChips(now);
			DrawNews(now);
			DrawFooter(now);
			DrawOverlays(now);
			float num = (float)(now - _flashAt) / 400f;
			if (num >= 0f && num < 1f)
			{
				MenuDraw.Rect(0f, 0f, MenuDraw.Width, MenuDraw.Height, MenuDraw.Rgba(255, 255, 255, _flashStrength * (1f - MenuDraw.EaseOut(num))));
			}
			float num2 = _black.Value(now);
			if (num2 > 0.001f)
			{
				MenuDraw.Rect(0f, 0f, MenuDraw.Width, MenuDraw.Height, MenuDraw.Rgba(0, 0, 0, num2));
			}
			float num3 = _farewell.Value(now);
			if (num3 > 0.001f)
			{
				MenuDraw.Text(MenuDraw.Upper(Strings.MenuScene.Farewell), MenuFont.Saira, 42f, MenuDraw.Width / 2f, MenuDraw.Height / 2f - 25f, MenuDraw.Alpha(MenuDraw.White, num3), MenuAlign.Center, 0.3f, 50f);
			}
			MenuDraw.End();
			MenuLoadingScreen.Draw();
			LayoutFields(now);
		}
	}

	private bool NeedsGlass(long now)
	{
		if (!(_chips.Value(now) > 0.001f) && !AccountGlassVisible(now) && !NewsVisible(now))
		{
			return OverlaysVisible(now);
		}
		return true;
	}

	private void OnMainScreen(MenuScreen previous, MenuScreen screen, long now)
	{
		bool flag = screen == MenuScreen.Main;
		for (int i = 0; i < _navItems.Length; i++)
		{
			_navItems[i].Set(flag ? 1 : 0, now, flag ? 340 : 260, flag ? (i * 60 + 240) : 0);
		}
		_bandAlpha.Set(flag ? 1 : 0, now, 300f, flag ? 460 : 0);
		_navHint.Set(flag ? 1 : 0, now, 300f, flag ? 620 : 0);
		_newsAlpha.Set(flag ? 1 : 0, now, 450f, flag ? 520 : 0);
		if (flag)
		{
			SelectNav(_navIndex, now, sound: false, force: true);
			_newsPageSince = now;
		}
	}

	private void EnterMenu(long now)
	{
		if (_screen == MenuScreen.Title)
		{
			Sound("menu_click.wav");
			Go(MenuScreen.Main, now);
		}
	}

	private void SelectNav(int index, long now, bool sound, bool force = false)
	{
		int num = (index + _navItems.Length) % _navItems.Length;
		if (num != _navIndex || force)
		{
			_navIndex = num;
			for (int i = 0; i < _navShift.Length; i++)
			{
				_navShift[i].Set((i == num) ? 18 : 0, now, 240f, 0f, MenuDraw.EaseSlide);
			}
			float item = MenuDraw.Measure(MenuDraw.Upper(NavEntries[num].Label), MenuFont.Saira, 60f, 0.012f).Width;
			_bandY.Set((float)num * 72f + 5f, now, 220f, 0f, MenuDraw.EaseSlide);
			_bandWidth.Set(item + 120f, now, 220f, 0f, MenuDraw.EaseSlide);
			if (force && _bandAlpha.Value(now) < 0.01f)
			{
				_bandY.Snap((float)num * 72f + 5f);
				_bandWidth.Snap(item + 120f);
			}
			if (sound)
			{
				Sound("menu_hover.wav");
			}
		}
	}

	private void ActivateNav(int index, long now)
	{
		switch (index)
		{
		case 0:
			OpenAccountScreen(MenuScreen.Login, now);
			break;
		case 1:
			if (Options.IsLoaded && Options.Instance.BlockClientRegistrations)
			{
				Sound("menu_back.wav");
				Toast(Strings.MenuScene.RegistrationsClosed);
			}
			else
			{
				OpenAccountScreen(MenuScreen.Register, now);
			}
			break;
		case 2:
			Sound("menu_click.wav");
			_mainMenu.OpenSettings();
			Go(MenuScreen.Overlay, now, sound: false);
			break;
		case 3:
			Go(MenuScreen.Credits, now);
			break;
		case 4:
			OpenModal(ModalKind.Quit, now);
			break;
		}
	}

	private void UpdateMain(long now)
	{
		if (_screen == MenuScreen.Overlay && !EngineWindowOpen)
		{
			Go(MenuScreen.Main, now, sound: false);
		}
		List<MenuNewsInfo> news = MenuContent.Current.News;
		if (_screen == MenuScreen.Main && news.Count > 1 && _newsPausedAt < 0 && now - _newsPageSince >= 7000)
		{
			ShowNews(_newsPage + 1, now);
		}
		bool flag;
		switch (_hovered)
		{
		case "news:card":
		case "news:prev":
		case "news:next":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		bool flag2 = flag;
		if (flag2 && _newsPausedAt < 0)
		{
			_newsPausedAt = now;
		}
		else if (!flag2 && _newsPausedAt >= 0)
		{
			_newsPageSince += now - _newsPausedAt;
			_newsPausedAt = -1L;
		}
	}

	private void ShowNews(int page, long now)
	{
		int count = MenuContent.Current.News.Count;
		if (count != 0)
		{
			_newsPage = (page % count + count) % count;
			_newsPageSince = now;
			_articleIn.Snap(0f);
			_articleIn.Set(1f, now, 380f, 0f, MenuDraw.EaseSlide);
		}
	}

	private bool OnMainClick(string id, long now)
	{
		if (id.StartsWith("nav:", StringComparison.Ordinal))
		{
			if (int.TryParse(id.Substring(4, id.Length - 4), out var result))
			{
				if (_screen == MenuScreen.Main)
				{
					SelectNav(result, now, sound: false);
					ActivateNav(result, now);
				}
				return true;
			}
		}
		switch (id)
		{
		case "news:prev":
			ShowNews(_newsPage - 1, now);
			return true;
		case "news:next":
			ShowNews(_newsPage + 1, now);
			return true;
		case "news:card":
			return true;
		case "chip:discord":
			Sound("menu_click.wav");
			OpenLink(MenuContent.Current.Discord);
			return true;
		default:
			return false;
		}
	}

	private static void OpenLink(string url)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			return;
		}
		try
		{
			Process.Start(new ProcessStartInfo(url)
			{
				UseShellExecute = true
			});
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Could not open {Url}", url);
		}
	}

	private bool OnMainKey(Intersect.Client.Framework.GenericClasses.Keys key, long now)
	{
		if (_screen != MenuScreen.Main)
		{
			return false;
		}
		switch (key)
		{
		case Intersect.Client.Framework.GenericClasses.Keys.Down:
			SelectNav(_navIndex + 1, now, sound: true);
			return true;
		case Intersect.Client.Framework.GenericClasses.Keys.Up:
			SelectNav(_navIndex - 1, now, sound: true);
			return true;
		case Intersect.Client.Framework.GenericClasses.Keys.Return:
		case Intersect.Client.Framework.GenericClasses.Keys.Space:
			ActivateNav(_navIndex, now);
			return true;
		case Intersect.Client.Framework.GenericClasses.Keys.Escape:
			OpenModal(ModalKind.Quit, now);
			return true;
		default:
			return false;
		}
	}

	private void DrawTitle(long now)
	{
		float num = _invite.Value(now);
		if (num <= 0.001f)
		{
			return;
		}
		if (_screen == MenuScreen.Title)
		{
			float num2 = (float)(now - _screenSince - 600) / 2800f;
			if (num2 > 0f)
			{
				num *= 0.725f + 0.275f * MathF.Cos(num2 * (float)Math.PI * 2f);
			}
		}
		string text = MenuDraw.Upper(Strings.MenuScene.PressAnyKey);
		float item = MenuDraw.Measure(text, MenuFont.Saira, 30f, 0.36f).Width;
		float num3 = 240f + item;
		float num4 = 600f - num3 / 2f;
		float num5 = 640f + Middle;
		IGameTexture radial = MenuTextures.Radial;
		if (radial != null)
		{
			MenuDraw.Texture(radial, num4 + 96f, num5 - 22f, item + 48f, 80f, MenuDraw.Rgba(0, 0, 0, 0.42f * num));
		}
		MenuDraw.HorizontalRamp(LineInRamp, num4, num5 + 14f, 96f, 2f, MenuDraw.Alpha(Color.White, num));
		MenuDraw.Text(text, MenuFont.Saira, 30f, num4 + 96f + 24f, num5, MenuDraw.Alpha(MenuDraw.White, num), MenuAlign.Left, 0.36f, 30f);
		MenuDraw.HorizontalRamp(LineOutRamp, num4 + 96f + 48f + item, num5 + 14f, 96f, 2f, MenuDraw.Alpha(Color.White, num));
	}

	private void DrawNav(long now)
	{
		float num = 392f + Middle;
		(string, string)[] navEntries = NavEntries;
		float num2 = _bandAlpha.Value(now);
		if (num2 > 0.001f)
		{
			KiBand(96f, num + _bandY.Value(now), _bandWidth.Value(now), 62f, num2, now);
		}
		for (int i = 0; i < navEntries.Length; i++)
		{
			float num3 = _navItems[i].Value(now);
			if (!(num3 <= 0.001f))
			{
				float num4 = (1f - num3) * -48f;
				bool flag = i == _navIndex && num2 > 0.5f;
				float num5 = num + (float)i * 72f;
				float x = 124f + num4 + _navShift[i].Value(now);
				string text = MenuDraw.Upper(navEntries[i].Item1);
				MenuDraw.Text(text, MenuFont.Saira, 60f, x, num5, flag ? MenuDraw.Alpha(MenuDraw.Night, num3) : MenuDraw.Rgba(242, 242, 242, 0.7f * num3), MenuAlign.Left, 0.012f, 72f);
				if (_screen == MenuScreen.Main && num3 > 0.5f)
				{
					Hit($"nav:{i}", 96f, num5, 820f, 72f);
				}
			}
		}
		float num6 = _navHint.Value(now);
		if (num6 > 0.001f)
		{
			float num7 = num + (float)navEntries.Length * 72f + 24f;
			MenuDraw.Rect(124f, num7 + 14f, 36f, 2f, MenuDraw.Alpha(MenuDraw.KiLight, num6));
			MenuDraw.Text(navEntries[_navIndex].Item2, MenuFont.ChakraMedium, 21f, 174f, num7, MenuDraw.Alpha(MenuDraw.Mist, num6), MenuAlign.Left, 0f, 29f);
		}
	}

	private void DrawChips(long now)
	{
		float num = _chips.Value(now);
		if (!(num <= 0.001f))
		{
			float num2 = MenuDraw.Width - 64f;
			float num3 = 48f - (1f - num) * 12f;
			string discord = MenuContent.Current.Discord;
			if (!string.IsNullOrWhiteSpace(discord))
			{
				string text = MenuDraw.Upper(Strings.MenuScene.Discord);
				float item = MenuDraw.Measure(text, MenuFont.Chakra, 15f, 0.16f).Width;
				float num4 = 54f + item + 22f;
				float num5 = num2 - num4;
				Chip(num5, num3, num4, 48f, num, IsHovered("chip:discord"));
				DrawSpeechIcon(num5 + 22f, num3 + 14f, MenuDraw.Alpha(MenuDraw.White, num));
				MenuDraw.Text(text, MenuFont.Chakra, 15f, num5 + 22f + 20f + 12f, num3, MenuDraw.Alpha(MenuDraw.White, num), MenuAlign.Left, 0.16f, 48f);
				Hit("chip:discord", num5, num3, num4, 48f);
				num2 = num5 - 12f;
			}
			(string, Color, bool) tuple;
			switch (MainMenu.ActiveNetworkStatus)
			{
			case NetworkStatus.Online:
				tuple = (Strings.MenuScene.ServerOnline.ToString(), MenuDraw.Green, true);
				break;
			case NetworkStatus.Connecting:
				tuple = (Strings.MenuScene.ServerConnecting.ToString(), MenuDraw.Ki, true);
				break;
			case NetworkStatus.ServerFull:
				tuple = (Strings.MenuScene.ServerFull.ToString(), MenuDraw.Ki, false);
				break;
			case NetworkStatus.VersionMismatch:
				tuple = (Strings.MenuScene.ServerVersion.ToString(), MenuDraw.Red, false);
				break;
			case NetworkStatus.Offline:
			case NetworkStatus.Failed:
			case NetworkStatus.HandshakeFailure:
				tuple = (Strings.MenuScene.ServerOffline.ToString(), MenuDraw.Red, false);
				break;
			default:
				tuple = (Strings.MenuScene.ServerConnecting.ToString(), MenuDraw.Steel, true);
				break;
			}
			var (text2, color, flag) = tuple;
			if (Globals.WaitingOnServer && MainMenu.ActiveNetworkStatus != NetworkStatus.Online)
			{
				text2 = Strings.MenuScene.ServerConnecting;
				color = MenuDraw.Ki;
			}
			bool flag2 = !Globals.WaitingOnServer && MainMenu.ServerAdminOnly;
			bool flag3 = flag2;
			if (flag3)
			{
				NetworkStatus activeNetworkStatus = MainMenu.ActiveNetworkStatus;
				bool flag4 = ((activeNetworkStatus == NetworkStatus.Online || activeNetworkStatus == NetworkStatus.ServerFull) ? true : false);
				flag3 = flag4;
			}
			bool flag5 = flag3;
			if (flag5)
			{
				text2 = Strings.MenuScene.ServerReserved;
				color = MenuDraw.Ki;
				flag = false;
			}
			bool flag6 = !flag5 && !Globals.WaitingOnServer && MainMenu.ServerMaxCount > 0;
			bool flag7 = flag6;
			if (flag7)
			{
				NetworkStatus activeNetworkStatus = MainMenu.ActiveNetworkStatus;
				bool flag4 = ((activeNetworkStatus == NetworkStatus.Online || activeNetworkStatus == NetworkStatus.ServerFull) ? true : false);
				flag7 = flag4;
			}
			if (flag7)
			{
				text2 = $"{text2} — {MainMenu.ServerOnlineCount} / {MainMenu.ServerMaxCount}";
			}
			string text3 = MenuDraw.Upper(text2);
			float item2 = MenuDraw.Measure(text3, MenuFont.Chakra, 15f, 0.16f).Width;
			float num6 = 44f + item2 + 22f;
			float num7 = num2 - num6;
			Chip(num7, num3, num6, 48f, num, hovered: false);
			float num8 = (flag ? (0.85f + 0.15f * MathF.Cos((float)now / 2400f * (float)Math.PI * 2f)) : 1f);
			IGameTexture radial = MenuTextures.Radial;
			if (radial != null)
			{
				MenuDraw.Texture(radial, num7 + 22f + 5f - 12f, num3 + 24f - 12f, 24f, 24f, MenuDraw.Alpha(color, 0.55f * num * num8));
			}
			Diamond(num7 + 22f + 5f, num3 + 24f, 10f, MenuDraw.Alpha(color, num * num8));
			MenuDraw.Text(text3, MenuFont.Chakra, 15f, num7 + 22f + 10f + 12f, num3, MenuDraw.Alpha(MenuDraw.White, num), MenuAlign.Left, 0.16f, 48f);
		}
	}

	private static void Chip(float x, float y, float width, float height, float alpha, bool hovered)
	{
		MenuDraw.Glass(x, y, width, height, 0f, MenuDraw.Rgba(10, 13, 22, 0.55f), alpha);
		OutlineRect(x, y, width, height, hovered ? MenuDraw.Rgba(255, 172, 82, 0.55f * alpha) : MenuDraw.Rgba(255, 255, 255, 0.12f * alpha), 1f);
	}

	private static void DrawSpeechIcon(float x, float y, Color color)
	{
		MenuDraw.Line(x + 3f, y + 3f, x + 17f, y + 3f, 2f, color);
		MenuDraw.Line(x + 17f, y + 3f, x + 17f, y + 14f, 2f, color);
		MenuDraw.Line(x + 17f, y + 14f, x + 8f, y + 14f, 2f, color);
		MenuDraw.Line(x + 8f, y + 14f, x + 3f, y + 18f, 2f, color);
		MenuDraw.Line(x + 3f, y + 18f, x + 3f, y + 3f, 2f, color);
		MenuDraw.Rect(x + 7f, y + 7f, 2f, 2f, color);
		MenuDraw.Rect(x + 12f, y + 7f, 2f, 2f, color);
	}

	private bool NewsVisible(long now)
	{
		if (_newsAlpha.Value(now) > 0.001f)
		{
			return MenuContent.Current.News.Count > 0;
		}
		return false;
	}

	private void DrawNews(long now)
	{
		List<MenuNewsInfo> news = MenuContent.Current.News;
		float num = _newsAlpha.Value(now);
		if (num <= 0.001f || news.Count == 0)
		{
			return;
		}
		MenuNewsInfo menuNewsInfo = news[Math.Clamp(_newsPage, 0, news.Count - 1)];
		float maxWidth = 374f;
		List<string> list = MenuDraw.Wrap(menuNewsInfo.Text, MenuFont.ChakraMedium, 17f, maxWidth);
		float num2 = 63f + (float)list.Count * 24.65f;
		float num3 = Math.Max(92f, num2);
		float num4 = 72f + num3 + 28f;
		float num5 = MenuDraw.Width - 64f - 540f;
		float num6 = MenuDraw.Height - 104f - num4 + (1f - num) * 26f;
		GlassPanel(num5, num6, 540f, num4, 20f, 0.58f, num, accent: false);
		Hit("news:card", num5, num6, 540f, num4);
		Diamond(num5 + 26f + 4f, num6 + 22f + 17f, 8f, MenuDraw.Alpha(MenuDraw.Gold, num));
		MenuDraw.Text(MenuDraw.Upper(Strings.MenuScene.News), MenuFont.Saira, 19f, num5 + 26f + 20f, num6 + 22f, MenuDraw.Alpha(MenuDraw.White, num), MenuAlign.Left, 0.2f, 34f);
		string text = $"{_newsPage + 1} / {news.Count}";
		float item = MenuDraw.Measure(text, MenuFont.Chakra, 15f, 0.08f).Width;
		float num7 = num5 + 540f - 26f - 34f;
		float num8 = num7 - 8f - item;
		float x = num8 - 8f - 34f;
		ArrowButton("news:prev", x, num6 + 22f, left: true, num);
		MenuDraw.Text(text, MenuFont.Chakra, 15f, num8, num6 + 22f, MenuDraw.Alpha(MenuDraw.Steel, num), MenuAlign.Left, 0.08f, 34f);
		ArrowButton("news:next", num7, num6 + 22f, left: false, num);
		float num9 = _articleIn.Value(now);
		float num10 = num * num9;
		float num11 = (1f - num9) * 16f;
		float num12 = num6 + 22f + 34f + 16f;
		float num13 = num12 + (num3 - 92f) / 2f;
		MenuDraw.Renderer.DrawTexture(SlotRamp, 0f, 0f, 1f, 256f, (num5 + 26f + num11) * MenuDraw.Scale, num13 * MenuDraw.Scale, 92f * MenuDraw.Scale, 92f * MenuDraw.Scale, MenuDraw.Alpha(Color.White, num10));
		OutlineRect(num5 + 26f + num11, num13, 92f, 92f, MenuDraw.Rgba(255, 255, 255, 0.13f * num10), 1f);
		IGameTexture gameTexture = ((menuNewsInfo.Icon == "meteo") ? MenuTextures.WeatherIcon : GameContentManager.Current.GetTexture(TextureType.Item, menuNewsInfo.Icon));
		if (gameTexture != null)
		{
			DrawPixelArt(gameTexture, num5 + 26f + num11 + 14f, num13 + 14f, 64f, 64f, num10);
		}
		float x2 = num5 + 140f + num11;
		float num14 = num12 + (num3 - num2) / 2f;
		MenuDraw.Text(MenuDraw.Upper(Strings.MenuScene.NewsTag), MenuFont.Chakra, 13f, x2, num14, MenuDraw.Alpha(MenuDraw.Gold, num10), MenuAlign.Left, 0.22f, 13f);
		MenuDraw.Text(MenuDraw.Upper(menuNewsInfo.Title), MenuFont.Saira, 34f, x2, num14 + 21f, MenuDraw.Alpha(MenuDraw.White, num10), MenuAlign.Left, 0.01f, 34f);
		for (int i = 0; i < list.Count; i++)
		{
			MenuDraw.Text(list[i], MenuFont.ChakraMedium, 17f, x2, num14 + 63f + (float)i * 24.65f, MenuDraw.Alpha(MenuDraw.Mist, num10), MenuAlign.Left, 0f, 24.65f);
		}
		float num15 = MenuDraw.Clamp01((float)((_newsPausedAt >= 0) ? (_newsPausedAt - _newsPageSince) : (now - _newsPageSince)) / 7000f);
		MenuDraw.Rect(num5 + 18f, num6 + num4 - 3f, 522f, 3f, MenuDraw.Rgba(255, 255, 255, 0.07f * num));
		float num16 = 540f * num15 - 18f;
		if (num16 > 0f)
		{
			IGameTexture kiRamp = KiRamp;
			if (kiRamp != null)
			{
				float num17 = 18f / (540f * num15) * 255f;
				MenuDraw.Renderer.DrawTexture(kiRamp, num17, 0f, Math.Max(1f, 255f - num17), 1f, (num5 + 18f) * MenuDraw.Scale, (num6 + num4 - 3f) * MenuDraw.Scale, num16 * MenuDraw.Scale, 3f * MenuDraw.Scale, MenuDraw.Alpha(Color.White, num));
			}
		}
	}

	private void ArrowButton(string id, float x, float y, bool left, float alpha)
	{
		bool flag = IsHovered(id);
		MenuDraw.Rect(x, y, 34f, 34f, MenuDraw.Rgba(255, 255, 255, 0.05f * alpha));
		OutlineRect(x, y, 34f, 34f, flag ? MenuDraw.Rgba(255, 172, 82, 0.6f * alpha) : MenuDraw.Rgba(255, 255, 255, 0.14f * alpha), 1f);
		Chevron(x + 17f, y + 17f, 16f, left, MenuDraw.Alpha(flag ? MenuDraw.KiLight : MenuDraw.White, alpha), 2.6f);
		Hit(id, x, y, 34f, 34f);
	}

	private static void DrawPixelArt(IGameTexture texture, float x, float y, float width, float height, float alpha, float sx = 0f, float sy = 0f, float sw = -1f, float sh = -1f)
	{
		GameRenderer renderer = MenuDraw.Renderer;
		renderer.LinearFiltering = false;
		float sw2 = ((sw > 0f) ? sw : ((float)Math.Min(texture.Width, texture.Height)));
		float sh2 = ((sh > 0f) ? sh : ((float)texture.Height));
		renderer.DrawTexture(texture, sx, sy, sw2, sh2, MathF.Round(x * MenuDraw.Scale), MathF.Round(y * MenuDraw.Scale), MathF.Round(width * MenuDraw.Scale), MathF.Round(height * MenuDraw.Scale), MenuDraw.Alpha(Color.White, alpha));
		renderer.LinearFiltering = true;
	}

	private void DrawFooter(long now)
	{
		float num = MenuDraw.Height - 34f;
		float num2 = _version.Value(now);
		if (num2 > 0.001f)
		{
			float num3 = num - 30f + 7.5f;
			string text = MenuDraw.Upper(Strings.Main.GameName);
			float num4 = MenuDraw.Text(text, MenuFont.Chakra, 15f, 96f, num3, MenuDraw.Alpha(MenuDraw.Mist, num2), MenuAlign.Left, 0.16f, 15f);
			Diamond(96f + num4 + 12f + 2.5f, num3 + 7.5f, 5f, MenuDraw.Alpha(MenuDraw.Ki, num2));
			MenuDraw.Text(MenuDraw.Upper(MenuContent.Current.Version), MenuFont.Chakra, 15f, 96f + num4 + 29f, num3, MenuDraw.Alpha(MenuDraw.Steel, num2), MenuAlign.Left, 0.16f, 15f);
		}
		float keys = _keys.Value(now);
		(string[] Caps, string Label)[] hints;
		float y;
		if (!(keys <= 0.001f))
		{
			(string[], string)[] array;
			switch (_screen)
			{
			case MenuScreen.Login:
			case MenuScreen.Register:
			case MenuScreen.Forgot:
			case MenuScreen.Credits:
				array = new(string[], string)[3]
				{
					(new string[1] { Strings.MenuScene.KeyTab }, Strings.MenuScene.HintNextField),
					(new string[1] { Strings.MenuScene.KeyEnter }, Strings.MenuScene.HintConfirm),
					(new string[1] { Strings.MenuScene.KeyEscape }, Strings.MenuScene.HintBack)
				};
				break;
			case MenuScreen.Selection:
				array = ((_previews.Length <= 1) ? new(string[], string)[3]
				{
					(new string[2] { "←", "→" }, Strings.MenuScene.HintTurn),
					(new string[1] { Strings.MenuScene.KeyEnter }, Strings.MenuScene.HintPlay),
					(new string[1] { Strings.MenuScene.KeyEscape }, Strings.MenuScene.HintLogout)
				} : new(string[], string)[4]
				{
					(new string[2] { "↑", "↓" }, Strings.MenuScene.HintCharacters),
					(new string[2] { "←", "→" }, Strings.MenuScene.HintTurn),
					(new string[1] { Strings.MenuScene.KeyEnter }, Strings.MenuScene.HintPlay),
					(new string[1] { Strings.MenuScene.KeyEscape }, Strings.MenuScene.HintLogout)
				});
				break;
			case MenuScreen.Creation:
				array = ((!CreatorMode) ? new(string[], string)[4]
				{
					(new string[2] { "↑", "↓" }, Strings.MenuScene.HintClass),
					(new string[1] { "1-4" }, Strings.MenuScene.HintOutfit),
					(new string[2] { "←", "→" }, Strings.MenuScene.HintTurn),
					(new string[1] { Strings.MenuScene.KeyEscape }, Strings.MenuScene.HintBack)
				} : new(string[], string)[5]
				{
					(new string[2] { "↑", "↓" }, Strings.MenuScene.HintCoiffure),
					(new string[1] { "C" }, Strings.MenuScene.HintCheveux),
					(new string[1] { "1-4" }, Strings.MenuScene.HintPeau),
					(new string[2] { "←", "→" }, Strings.MenuScene.HintTurn),
					(new string[1] { Strings.MenuScene.KeyEscape }, Strings.MenuScene.HintBack)
				});
				break;
			default:
				array = new(string[], string)[3]
				{
					(new string[2] { "↑", "↓" }, Strings.MenuScene.HintNavigate),
					(new string[1] { Strings.MenuScene.KeyEnter }, Strings.MenuScene.HintConfirm),
					(new string[1] { Strings.MenuScene.KeyEscape }, Strings.MenuScene.HintQuit)
				};
				break;
			}
			hints = array;
			y = num - 30f;
			float num5 = Lay(draw: false);
			MenuDraw.Bevel(num5 - 16f, y - 6f, MenuDraw.Width - 64f - num5 + 32f, 42f, 8f, MenuDraw.Alpha(MenuDraw.Night, 0.68f * keys));
			Lay(draw: true);
		}
		float Lay(bool draw)
		{
			float num6 = MenuDraw.Width - 64f;
			for (int num7 = hints.Length - 1; num7 >= 0; num7--)
			{
				(string[] Caps, string Label) tuple = hints[num7];
				string[] item = tuple.Caps;
				string item2 = tuple.Label;
				string text2 = MenuDraw.Upper(item2);
				float item3 = MenuDraw.Measure(text2, MenuFont.Chakra, 14f, 0.14f).Width;
				num6 -= item3;
				if (draw)
				{
					MenuDraw.Text(text2, MenuFont.Chakra, 14f, num6, y, MenuDraw.Alpha(MenuDraw.Mist, keys), MenuAlign.Left, 0.14f, 30f);
				}
				num6 -= 10f;
				for (int num8 = item.Length - 1; num8 >= 0; num8--)
				{
					num6 -= KeyCapWidth(item[num8]);
					if (draw)
					{
						KeyCap(item[num8], num6, y, keys);
					}
					if (num8 > 0)
					{
						num6 -= 6f;
					}
				}
				if (num7 > 0)
				{
					num6 -= 28f;
				}
			}
			return num6;
		}
	}

	private void Toast(string message)
	{
		long now = Now;
		_toastText = message;
		_toastAt = now;
		_toastAlpha.Set(1f, now, 250f);
	}

	private void OpenModal(ModalKind kind, long now, bool sound = true)
	{
		_modal = kind;
		_modalShown = kind;
		_modalName = CurrentPreview?.Name ?? string.Empty;
		_modalAlpha.Set(1f, now, 250f);
		_modalLift.Set(1f, now, 350f, 0f, MenuDraw.EaseSlide);
		if (sound)
		{
			Sound("menu_open.wav");
		}
		foreach (MenuField field in _fields)
		{
			if (field.HasFocus)
			{
				field.Blur();
			}
		}
	}

	private void CloseModal(long now, bool sound = true)
	{
		if (_modal != ModalKind.None)
		{
			if (sound)
			{
				Sound("menu_back.wav");
			}
			if (_modal == ModalKind.Bienvenue)
			{
				RetenirBienvenue();
			}
			_modal = ModalKind.None;
			_modalAlpha.Set(0f, now, 250f);
			_modalLift.Set(0f, now, 350f, 0f, MenuDraw.EaseSlide);
		}
	}

	private void StartQuit(long now)
	{
		CloseModal(now, sound: false);
		Sound("menu_back.wav");
		_quitting = true;
		_quitAt = now + 2000;
		_black.Set(1f, now, 700f);
		_farewell.Set(1f, now, 600f, 300f);
	}

	private void UpdateOverlays(long now)
	{
		if (_toastAlpha.Target > 0f && now - _toastAt > 3200)
		{
			_toastAlpha.Set(0f, now, 250f);
		}
		if (_quitting && now >= _quitAt)
		{
			Globals.IsRunning = false;
		}
	}

	private bool OnOverlayClick(string id, long now)
	{
		switch (id)
		{
		case "modal:backdrop":
			return true;
		case "modal:cancel":
			CloseModal(now);
			return true;
		case "modal:bienvenue:case":
			Sound("menu_click.wav");
			_bienvenueNePlus = !_bienvenueNePlus;
			return true;
		case "modal:discord":
			Sound("menu_click.wav");
			OpenLink(MenuContent.Current.Discord);
			return true;
		case "modal:confirm":
			switch (_modal)
			{
			case ModalKind.Quit:
				StartQuit(now);
				break;
			case ModalKind.Delete:
				ConfirmDelete(now);
				break;
			}
			return true;
		default:
			return false;
		}
	}

	private bool OnOverlayKey(Intersect.Client.Framework.GenericClasses.Keys key, long now)
	{
		if (_quitting)
		{
			return true;
		}
		if (_modal == ModalKind.None)
		{
			return false;
		}
		if ((key == Intersect.Client.Framework.GenericClasses.Keys.Return || key == Intersect.Client.Framework.GenericClasses.Keys.Escape || key == Intersect.Client.Framework.GenericClasses.Keys.Space) ? true : false)
		{
			CloseModal(now);
		}
		return true;
	}

	private bool OverlaysVisible(long now)
	{
		if (!(_modalAlpha.Value(now) > 0.001f))
		{
			return _toastAlpha.Value(now) > 0.001f;
		}
		return true;
	}

	private void DrawOverlays(long now)
	{
		DrawToast(now);
		DrawModal(now);
	}

	private void DrawToast(long now)
	{
		float num = _toastAlpha.Value(now);
		if (!(num <= 0.001f) && _toastText.Length != 0)
		{
			float item = MenuDraw.Measure(_toastText, MenuFont.ChakraMedium, 19f).Width;
			float num2 = 25f + item + 26f;
			float num3 = MenuDraw.Width / 2f - num2 / 2f;
			float num4 = MenuDraw.Height - 128f - 58.6f + (1f - num) * 16f;
			MenuDraw.Glass(num3, num4, num2, 58.6f, 0f, MenuDraw.Rgba(10, 13, 22, 0.82f), num);
			MenuDraw.Rect(num3, num4, 3f, 58.6f, MenuDraw.Alpha(MenuDraw.KiLight, num));
			MenuDraw.Text(_toastText, MenuFont.ChakraMedium, 19f, num3 + 25f, num4 + 16f, MenuDraw.Alpha(MenuDraw.White, num), MenuAlign.Left, 0f, 26.6f);
		}
	}

	private void DrawModal(long now)
	{
		float num = _modalAlpha.Value(now);
		if (num <= 0.001f)
		{
			return;
		}
		_hitsEnabled = _modal != ModalKind.None;
		MenuDraw.Rect(0f, 0f, MenuDraw.Width, MenuDraw.Height, MenuDraw.Rgba(4, 6, 11, 0.5f * num));
		Hit("modal:backdrop", 0f, 0f, MenuDraw.Width, MenuDraw.Height);
		if (_modalShown == ModalKind.Bienvenue)
		{
			DrawBienvenue(now, num);
			_hitsEnabled = true;
			return;
		}
		bool flag = _modalShown == ModalKind.Delete;
		string text = (flag ? Strings.MenuScene.DeleteTitle.ToString(_modalName) : Strings.MenuScene.QuitTitle.ToString());
		string text2 = (flag ? Strings.MenuScene.DeleteText.ToString() : Strings.MenuScene.QuitText.ToString());
		string text3 = (flag ? Strings.MenuScene.Delete.ToString() : Strings.MenuScene.QuitConfirm.ToString());
		string text4 = (flag ? Strings.MenuScene.Keep.ToString() : Strings.MenuScene.Stay.ToString());
		List<string> list = MenuDraw.Wrap(text2, MenuFont.ChakraMedium, 20f, 540f);
		float num2 = 115.1f + (float)(list.Count * 29) + 32f + 64f + 42f;
		float num3 = MenuDraw.Width / 2f - 320f;
		float num4 = MenuDraw.Height / 2f - num2 / 2f + (1f - _modalLift.Value(now)) * 18f;
		GlassPanel(num3, num4, 640f, num2, 26f, 0.8f, num);
		float num5 = num3 + 50f;
		float num6 = num4 + 46f;
		PanelTitle(text, num5, num6, 540f, num);
		num6 += 69.1f;
		for (int i = 0; i < list.Count; i++)
		{
			MenuDraw.Text(list[i], MenuFont.ChakraMedium, 20f, num5, num6 + (float)(i * 29), MenuDraw.Alpha(MenuDraw.Mist, num), MenuAlign.Left, 0f, 29f);
		}
		num6 += (float)(list.Count * 29 + 32);
		Hollow("modal:confirm", num5, num6, 262f, 64f, text3, 30f, num);
		Cta("modal:cancel", num5 + 262f + 16f, num6, 262f, 64f, text4, 30f, num, now);
		_hitsEnabled = true;
	}

	public bool TryShowErrorInline(string message)
	{
		long now = Now;
		_pendingAction = null;
		ClearBusy();
		if (MenuLoadingScreen.IsActive && !Globals.JoiningGame)
		{
			MenuLoadingScreen.Stop();
			return false;
		}
		if (_pendingDeleteName != null && _screen == MenuScreen.Selection)
		{
			_deleteMessage = message;
			return true;
		}
		if (_screen == MenuScreen.Overlay && _mainMenu.IsPasswordChangeOpen)
		{
			Go(MenuScreen.Forgot, now, sound: false);
			Fail(Panel.Forgot, message, now);
			return true;
		}
		switch (_screen)
		{
		case MenuScreen.Login:
			Fail(Panel.Login, message, now);
			return true;
		case MenuScreen.Register:
			Fail(Panel.Register, message, now);
			return true;
		case MenuScreen.Forgot:
			Fail(Panel.Forgot, message, now);
			return true;
		case MenuScreen.Creation:
			FailCreation(message, now);
			return true;
		default:
			return false;
		}
	}

	public void OnServerReset()
	{
		_pendingAction = null;
		ClearBusy();
		if (!Globals.JoiningGame)
		{
			MenuLoadingScreen.Stop();
		}
	}

	public void ShowMain()
	{
		long now = Now;
		MenuScreen screen = _screen;
		if ((uint)screen > 1u)
		{
			_pendingAction = null;
			ClearBusy();
			Go(MenuScreen.Main, now, sound: false);
		}
	}

	public void ShowLogin()
	{
		long now = Now;
		MenuScreen screen = _screen;
		if ((uint)screen <= 1u)
		{
			_introPlayed = true;
			_black.Snap(0f);
			if (!_logo.IsVisible)
			{
				_logo.ShowSettled(now);
			}
		}
		Go(MenuScreen.Login, now, sound: false);
	}

	public void ShowForgot()
	{
		Go(MenuScreen.Forgot, Now);
	}

	public void ShowRegister()
	{
		Go(MenuScreen.Register, Now);
	}

	public void ShowCredits()
	{
		Go(MenuScreen.Credits, Now);
	}

	public void ShowSelectionAgain()
	{
		if (_previews.Length != 0)
		{
			Go(MenuScreen.Selection, Now, sound: false);
		}
		else
		{
			ShowMain();
		}
	}

	private static void GlassPanel(float x, float y, float width, float height, float cut, float tintAlpha, float alpha, bool accent = true)
	{
		MenuDraw.Glass(x, y, width, height, cut, MenuDraw.Rgba(10, 13, 22, tintAlpha), alpha);
		MenuDraw.BevelOutline(x, y, width, height, cut, MenuDraw.Alpha(StrokeLight, alpha));
		if (accent)
		{
			MenuDraw.HorizontalRamp(KiRamp, x, y, 88f, 4f, MenuDraw.Alpha(Color.White, alpha));
		}
	}

	private void Cta(string id, float x, float y, float width, float height, string text, float fontPx, float alpha, long now, bool enabled = true, float charge = 0f)
	{
		if (alpha <= 0.001f)
		{
			return;
		}
		IGameTexture gameTexture = (enabled ? KiRamp : GreyRamp);
		GameRenderer renderer = MenuDraw.Renderer;
		float num = MathF.Round(x * MenuDraw.Scale);
		float num2 = width * MenuDraw.Scale;
		foreach (var (num3, num4, num5, num6) in MenuDraw.BevelRows(x, y, width, height, 16f))
		{
			if (gameTexture == null)
			{
				break;
			}
			float num7 = (num3 - num) / num2 * 255f;
			float num8 = (num3 + num5 - num) / num2 * 255f;
			renderer.DrawTexture(gameTexture, num7, 0f, Math.Max(1f, num8 - num7), 1f, num3, num4, num5, num6, MenuDraw.Alpha(Color.White, alpha));
			if (charge > 0.001f)
			{
				float val = num + num2 * Math.Clamp(charge, 0f, 1f);
				float width2 = Math.Min(num3 + num5, val) - num3;
				MenuDraw.ScreenRect(num3, num4, width2, num6, MenuDraw.Rgba(255, 255, 255, 0.34f * alpha));
			}
		}
		float num9 = (float)(now - _hoveredSince) / 700f;
		if (enabled && IsHovered(id) && num9 >= 0f && num9 < 1f)
		{
			IGameTexture sheenRamp = SheenRamp;
			if (sheenRamp != null)
			{
				float num10 = x - 160f + MenuDraw.EaseOut(num9) * (width + 220f);
				foreach (var item5 in MenuDraw.BevelRows(x, y, width, height, 16f))
				{
					float item = item5.X;
					float item2 = item5.Y;
					float item3 = item5.Width;
					float item4 = item5.Height;
					float num11 = item2 / MenuDraw.Scale - y;
					float num12 = (height / 2f - num11) * 0.364f;
					float num13 = Math.Max(item, (num10 + num12) * MenuDraw.Scale);
					float num14 = Math.Min(item + item3, (num10 + num12 + 110f) * MenuDraw.Scale);
					if (num14 > num13)
					{
						float num15 = (num13 - (num10 + num12) * MenuDraw.Scale) / (110f * MenuDraw.Scale) * 255f;
						float num16 = (num14 - (num10 + num12) * MenuDraw.Scale) / (110f * MenuDraw.Scale) * 255f;
						renderer.DrawTexture(sheenRamp, num15, 0f, Math.Max(1f, num16 - num15), 1f, num13, item2, num14 - num13, item4, MenuDraw.Alpha(Color.White, alpha));
					}
				}
			}
		}
		bool flag = IsPressed(id) && enabled;
		MenuDraw.Text(MenuDraw.Upper(text), MenuFont.Saira, fontPx, x + width / 2f, y + (float)(flag ? 1 : 0), MenuDraw.Alpha(MenuDraw.Night, alpha), MenuAlign.Center, 0.06f, height);
		if (enabled)
		{
			Hit(id, x, y, width, height);
		}
	}

	private void Hollow(string id, float x, float y, float width, float height, string text, float fontPx, float alpha, float cut = 16f, bool danger = true)
	{
		if (!(alpha <= 0.001f))
		{
			bool flag = IsHovered(id);
			Color color = (danger ? MenuDraw.Red : MenuDraw.KiLight);
			Color color2 = (flag ? color : MenuDraw.White);
			MenuDraw.Bevel(x, y, width, height, cut, MenuDraw.Rgba(255, 255, 255, 0.03f * alpha));
			MenuDraw.BevelOutline(x, y, width, height, cut, flag ? MenuDraw.Alpha(color, alpha) : MenuDraw.Rgba(255, 255, 255, 0.26f * alpha), 1f);
			MenuDraw.Text(MenuDraw.Upper(text), MenuFont.Saira, fontPx, x + width / 2f, y, MenuDraw.Alpha(color2, alpha), MenuAlign.Center, 0.06f, height);
			Hit(id, x, y, width, height);
		}
	}

	private float Link(string id, string text, float x, float y, float alpha, float fontPx = 17f)
	{
		float item = MenuDraw.Measure(text, MenuFont.Chakra, fontPx).Width;
		MenuDraw.Text(text, MenuFont.Chakra, fontPx, x, y, MenuDraw.Alpha(MenuDraw.KiLight, alpha), MenuAlign.Left, 0f, fontPx * 1.2f);
		if (IsHovered(id))
		{
			MenuDraw.Rect(x, y + fontPx * 1.2f, item, 1f, MenuDraw.Alpha(MenuDraw.KiLight, alpha));
		}
		Hit(id, x - 4f, y - 4f, item + 8f, fontPx * 1.2f + 8f);
		return item;
	}

	private float BackButton(string id, string text, float x, float y, float alpha)
	{
		Color color = MenuDraw.Alpha(IsHovered(id) ? MenuDraw.White : MenuDraw.Mist, alpha);
		Chevron(x + 9f, y + 8f, 18f, left: true, color, 2.4f);
		float num = MenuDraw.Text(MenuDraw.Upper(text), MenuFont.Chakra, 16f, x + 28f, y, color, MenuAlign.Left, 0.14f, 16f);
		Hit(id, x - 6f, y - 8f, num + 40f, 32f);
		return 28f + num;
	}

	private static void Chevron(float centerX, float centerY, float size, bool left, Color color, float thickness)
	{
		float num = size * 0.29f;
		int num2 = ((!left) ? 1 : (-1));
		MenuDraw.Line(centerX + (float)num2 * num * -0.5f, centerY - size * 0.29f, centerX + (float)num2 * num * 0.5f, centerY, thickness, color);
		MenuDraw.Line(centerX + (float)num2 * num * 0.5f, centerY, centerX + (float)num2 * num * -0.5f, centerY + size * 0.29f, thickness, color);
	}

	private static void Diamond(float centerX, float centerY, float size, Color color)
	{
		MenuDraw.Renderer.DrawTexture(MenuDraw.Pixel, 0f, 0f, 1f, 1f, (centerX - size / 2f) * MenuDraw.Scale, (centerY - size / 2f) * MenuDraw.Scale, size * MenuDraw.Scale, size * MenuDraw.Scale, color, null, GameBlendModes.None, null, 45f);
	}

	private void Checkbox(string id, float x, float y, bool isChecked, string label, float alpha)
	{
		float num = 22f;
		MenuDraw.Rect(x, y, num, num, MenuDraw.Rgba(4, 6, 11, 0.55f * alpha));
		OutlineRect(x, y, num, num, isChecked ? MenuDraw.Alpha(MenuDraw.KiLight, alpha) : MenuDraw.Rgba(255, 255, 255, 0.3f * alpha), 1f);
		if (isChecked)
		{
			Diamond(x + num / 2f, y + num / 2f, 10f, MenuDraw.Alpha(MenuDraw.Ki, alpha));
		}
		float num2 = MenuDraw.Text(label, MenuFont.ChakraMedium, 17f, x + num + 12f, y, MenuDraw.Alpha(MenuDraw.Mist, alpha), MenuAlign.Left, 0f, num);
		Hit(id, x - 4f, y - 4f, num + 12f + num2 + 8f, num + 8f);
	}

	private static void OutlineRect(float x, float y, float width, float height, Color color, float thickness)
	{
		MenuDraw.Rect(x, y, width, thickness, color);
		MenuDraw.Rect(x, y + height - thickness, width, thickness, color);
		MenuDraw.Rect(x, y + thickness, thickness, height - 2f * thickness, color);
		MenuDraw.Rect(x + width - thickness, y + thickness, thickness, height - 2f * thickness, color);
	}

	private void EyeButton(string id, float centerX, float centerY, bool active, float alpha)
	{
		Color color = MenuDraw.Alpha((active || IsHovered(id)) ? MenuDraw.KiLight : MenuDraw.Steel, alpha);
		for (int i = 0; i < 14; i++)
		{
			float x = (float)i / 14f * (float)Math.PI * 2f;
			float x2 = (float)(i + 1) / 14f * (float)Math.PI * 2f;
			MenuDraw.Line(centerX + MathF.Cos(x) * 11f, centerY + MathF.Sin(x) * 6.5f * (float)((MathF.Abs(MathF.Sin(x)) < 0.2f) ? 1 : 1), centerX + MathF.Cos(x2) * 11f, centerY + MathF.Sin(x2) * 6.5f, 2f, color);
		}
		for (int j = 0; j < 10; j++)
		{
			float x3 = (float)j / 10f * (float)Math.PI * 2f;
			float x4 = (float)(j + 1) / 10f * (float)Math.PI * 2f;
			MenuDraw.Line(centerX + MathF.Cos(x3) * 3f, centerY + MathF.Sin(x3) * 3f, centerX + MathF.Cos(x4) * 3f, centerY + MathF.Sin(x4) * 3f, 2f, color);
		}
		Hit(id, centerX - 21f, centerY - 21f, 42f, 42f);
	}

	private static float KeyCapWidth(string text)
	{
		return Math.Max(32f, (ArrowOf(text).HasValue ? 12f : MenuDraw.Measure(text, MenuFont.Chakra, 14f).Width) + 16f);
	}

	private static float KeyCap(string text, float x, float y, float alpha)
	{
		(float, float)? tuple = ArrowOf(text);
		float num = KeyCapWidth(text);
		MenuDraw.Rect(x, y, num, 30f, MenuDraw.Rgba(255, 255, 255, 0.06f * alpha));
		Color color = MenuDraw.Rgba(255, 255, 255, 0.24f * alpha);
		MenuDraw.Rect(x, y, num, 1f, color);
		MenuDraw.Rect(x, y + 27f, num, 3f, color);
		MenuDraw.Rect(x, y + 1f, 1f, 26f, color);
		MenuDraw.Rect(x + num - 1f, y + 1f, 1f, 26f, color);
		if (tuple.HasValue)
		{
			(float, float) valueOrDefault = tuple.GetValueOrDefault();
			Arrow(x + num / 2f, y + 14f, valueOrDefault, MenuDraw.Alpha(MenuDraw.White, alpha));
		}
		else
		{
			MenuDraw.Text(text, MenuFont.Chakra, 14f, x + num / 2f, y, MenuDraw.Alpha(MenuDraw.White, alpha), MenuAlign.Center, 0f, 28f);
		}
		return num;
	}

	private static (float X, float Y)? ArrowOf(string text)
	{
		return text switch
		{
			"↑" => (0f, -1f), 
			"↓" => (0f, 1f), 
			"←" => (-1f, 0f), 
			"→" => (1f, 0f), 
			_ => null, 
		};
	}

	private static void Arrow(float centerX, float centerY, (float X, float Y) direction, Color color)
	{
		(float X, float Y) tuple = direction;
		float item = tuple.X;
		float item2 = tuple.Y;
		float num = centerX + item * 6f;
		float num2 = centerY + item2 * 6f;
		MenuDraw.Line(centerX - item * 6f, centerY - item2 * 6f, num, num2, 1.6f, color);
		MenuDraw.Line(num, num2, num - item * 4f - item2 * 4f, num2 - item2 * 4f + item * 4f, 1.6f, color);
		MenuDraw.Line(num, num2, num - item * 4f + item2 * 4f, num2 - item2 * 4f - item * 4f, 1.6f, color);
	}

	private static void Label(string text, float x, float y, float alpha, Color? color = null, float spacing = 0.18f)
	{
		MenuDraw.Text(MenuDraw.Upper(text), MenuFont.Chakra, 16f, x, y, MenuDraw.Alpha(color ?? MenuDraw.Mist, alpha), MenuAlign.Left, spacing, 16f);
	}

	private static void Kicker(string text, float x, float y, float alpha)
	{
		MenuDraw.Text(MenuDraw.Upper(text), MenuFont.Chakra, 16f, x, y, MenuDraw.Alpha(MenuDraw.KiLight, alpha), MenuAlign.Left, 0.24f, 16f);
	}

	private static void KiBand(float x, float y, float width, float height, float alpha, long now, int phase = 0)
	{
		if (alpha <= 0.001f)
		{
			return;
		}
		IGameTexture bandRamp = BandRamp;
		if (bandRamp == null)
		{
			return;
		}
		(float, float)[][] array = new(float, float)[3][]
		{
			new(float, float)[7]
			{
				(72f, 0f),
				(40f, 0.16f),
				(58f, 0.3f),
				(4f, 0.46f),
				(46f, 0.6f),
				(20f, 0.8f),
				(64f, 1f)
			},
			new(float, float)[7]
			{
				(64f, 0f),
				(28f, 0.12f),
				(52f, 0.32f),
				(12f, 0.52f),
				(50f, 0.66f),
				(30f, 0.86f),
				(70f, 1f)
			},
			new(float, float)[7]
			{
				(78f, 0f),
				(46f, 0.2f),
				(62f, 0.34f),
				(0f, 0.42f),
				(40f, 0.58f),
				(16f, 0.76f),
				(58f, 1f)
			}
		};
		(float, float)[] array2 = array[(int)((now / 90 + phase) % 3)];
		IGameTexture radial = MenuTextures.Radial;
		if (radial != null)
		{
			MenuDraw.Texture(radial, x - 30f, y - 34f, width + 60f, height + 68f, MenuDraw.Rgba(255, 122, 26, 0.3f * alpha));
		}
		GameRenderer renderer = MenuDraw.Renderer;
		float scale = MenuDraw.Scale;
		int num = Math.Max(1, (int)MathF.Round(height * scale));
		float num2 = MathF.Round(y * scale);
		for (int i = 0; i < num; i++)
		{
			float num3 = ((float)i + 0.5f) / (float)num;
			float num4 = (0.5f - num3) * height * 0.2126f;
			float num5 = 0f;
			for (int j = 1; j < array2.Length; j++)
			{
				if (num3 <= array2[j].Item2)
				{
					float num6 = array2[j].Item2 - array2[j - 1].Item2;
					float num7 = ((num6 > 0f) ? ((num3 - array2[j - 1].Item2) / num6) : 0f);
					num5 = array2[j - 1].Item1 + (array2[j].Item1 - array2[j - 1].Item1) * num7;
					break;
				}
			}
			float num8 = (x + num4) * scale;
			float num9 = (width - num5) * scale;
			float val = (width - num5) / width * 255f;
			renderer.DrawTexture(bandRamp, 0f, 0f, Math.Max(1f, val), 1f, num8, num2 + (float)i, num9, 1f, MenuDraw.Alpha(Color.White, alpha));
			if (i == 0)
			{
				MenuDraw.ScreenRect(num8, num2, num9, 1f, MenuDraw.Rgba(255, 255, 255, 0.4f * alpha));
			}
		}
	}
}
