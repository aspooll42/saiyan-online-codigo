using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.General;
using Intersect.Client.Interface.Game;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Interface.Menu;
using Intersect.Client.Interface.Shared;
using Intersect.Configuration;

namespace Intersect.Client.Interface;

public static class Interface
{
	private static StatisticsPanel? _statisticsPanel;

	private static readonly ConcurrentQueue<Alert> PendingErrorMessages = new ConcurrentQueue<Alert>();

	private static bool _initialized;

	private static Canvas? _canvasInGame;

	private static Canvas? _canvasMainMenu;

	private static readonly Queue<Action> PendingActionsForInGameInterface = new Queue<Action>();

	public static readonly HashSet<Intersect.Client.Framework.Gwen.Control.Base> FocusComponents = new HashSet<Intersect.Client.Framework.Gwen.Control.Base>();

	public static readonly HashSet<Intersect.Client.Framework.Gwen.Control.Base> InputBlockingComponents = new HashSet<Intersect.Client.Framework.Gwen.Control.Base>();

	private static GameInterface? _uiInGame;

	private static MenuGuiBase? _uiMainMenu;

	private static TexturedBase? _skin;

	private static bool _showStatisticsPanelFPS;

	private static bool _showStatisticsPanelPing;

	private static long _imageDuSurvol = -1L;

	private static Point _pointDuSurvol;

	private static bool _survolDeLInterface;

	public static InputBase GwenInput { get; set; } = null;

	public static Intersect.Client.Framework.Gwen.Renderer.Base GwenRenderer { get; set; } = null;

	public static bool HideUi { get; set; }

	public static bool SetupHandlers { get; set; }

	public static bool HasInGameUI => _uiInGame != null;

	public static bool HasMainMenuUI => _uiMainMenu != null;

	public static GameInterface GameUi
	{
		get
		{
			if (_uiInGame == null)
			{
				throw new InvalidOperationException("In-game UI not initialized");
			}
			return _uiInGame;
		}
		private set
		{
			_uiInGame = value;
		}
	}

	public static MenuGuiBase MenuUi
	{
		get
		{
			if (_uiMainMenu == null)
			{
				throw new InvalidOperationException("Menu UI not initialized");
			}
			return _uiMainMenu;
		}
		private set
		{
			_uiMainMenu = value;
		}
	}

	public static MenuGuiBase? MenuUiIfReady => _uiMainMenu;

	private static MutableInterface? NullableCurrentInterface
	{
		get
		{
			object obj = _uiInGame;
			if (obj == null)
			{
				MenuGuiBase? uiMainMenu = _uiMainMenu;
				if (uiMainMenu == null)
				{
					return null;
				}
				obj = uiMainMenu.MainMenu;
			}
			return (MutableInterface?)obj;
		}
	}

	public static MutableInterface CurrentInterface => NullableCurrentInterface ?? throw new InvalidOperationException("No current UI initialized");

	public static bool ShowStatisticsPanelFPS
	{
		get
		{
			return _showStatisticsPanelFPS;
		}
		set
		{
			if (_showStatisticsPanelFPS != value)
			{
				_showStatisticsPanelFPS = value;
				StatisticsPanel statisticsPanel = _statisticsPanel;
				if (statisticsPanel != null)
				{
					statisticsPanel.IsFPSEnabled = value;
				}
			}
		}
	}

	public static bool ShowStatisticsPanelPing
	{
		get
		{
			return _showStatisticsPanelPing;
		}
		set
		{
			if (_showStatisticsPanelPing != value)
			{
				_showStatisticsPanelPing = value;
				StatisticsPanel statisticsPanel = _statisticsPanel;
				if (statisticsPanel != null)
				{
					statisticsPanel.IsPingEnabled = value;
				}
			}
		}
	}

	private static bool HasCurrentInterface => NullableCurrentInterface != null;

	public static TexturedBase Skin
	{
		get
		{
			return _skin ?? throw new InvalidOperationException("No skin");
		}
		set
		{
			_skin = value;
		}
	}

	public static void ShowAlert(string message, string? title = null, AlertType alertType = AlertType.Error)
	{
		PendingErrorMessages.Enqueue(new Alert(message, title ?? string.Empty, alertType));
	}

	public static void EnqueueInGame(Action action)
	{
		if (_uiInGame != null)
		{
			action();
		}
		else
		{
			PendingActionsForInGameInterface.Enqueue(action);
		}
	}

	public static void EnqueueInGame(Action<GameInterface> action)
	{
		GameInterface uiInGame = _uiInGame;
		if (uiInGame != null)
		{
			action(uiInGame);
			return;
		}
		PendingActionsForInGameInterface.Enqueue(delegate
		{
			action(GameUi);
		});
	}

	public static void EnqueueInGame<TArg0, TArg1>(Action<GameInterface> action, Action<TArg0, TArg1> onDeferred, TArg0 arg0, TArg1 arg1)
	{
		GameInterface uiInGame = _uiInGame;
		if (uiInGame != null)
		{
			action(uiInGame);
			return;
		}
		PendingActionsForInGameInterface.Enqueue(delegate
		{
			action(GameUi);
		});
		onDeferred(arg0, arg1);
	}

	public static Intersect.Client.Framework.Gwen.Control.Base? FindComponentUnderCursor(NodeFilter filters = NodeFilter.None)
	{
		Point point = new Point(InputHandler.MousePosition.X, InputHandler.MousePosition.Y);
		return NullableCurrentInterface?.Root.GetComponentAt(point, filters);
	}

	public static void InitGwen()
	{
		MutableInterface.DetachDebugWindow();
		if (_skin == null)
		{
			_skin = TexturedBase.FindSkin(GwenRenderer, GameContentManager.Current, ClientConfiguration.Instance.UiSkin);
			_skin.DefaultFont = Graphics.UIFont ?? _skin.DefaultFont;
			if (Graphics.UIFontSize != 0)
			{
				_skin.DefaultFontSize = Graphics.UIFontSize;
			}
		}
		_uiMainMenu?.Dispose();
		_uiInGame?.Dispose();
		_canvasMainMenu = new Canvas(Skin, "MainMenu")
		{
			Scale = 1f
		};
		var (num, num2) = Graphics.TailleReelleDeLEcran;
		_canvasMainMenu.SetSize((int)((float)num / _canvasMainMenu.Scale), (int)((float)num2 / _canvasMainMenu.Scale));
		_canvasMainMenu.ShouldDrawBackground = false;
		_canvasMainMenu.BackgroundColor = Color.FromArgb(255, 150, 170, 170);
		_canvasMainMenu.KeyboardInputEnabled = true;
		_canvasInGame = new Canvas(Skin, "InGame");
		var (num3, num4) = Graphics.TailleDeLInterface(num, num2);
		_canvasInGame.SetSize((int)((float)num3 / _canvasInGame.Scale), (int)((float)num4 / _canvasInGame.Scale));
		_canvasInGame.ShouldDrawBackground = false;
		_canvasInGame.BackgroundColor = Color.FromArgb(255, 150, 170, 170);
		_canvasInGame.KeyboardInputEnabled = true;
		if (Globals.GameState == GameStates.Intro || Globals.GameState == GameStates.Menu)
		{
			GwenInput.Initialize(_canvasMainMenu);
			MutableInterface.ReparentDebugWindow(_canvasMainMenu);
		}
		else
		{
			GwenInput.Initialize(_canvasInGame);
			MutableInterface.ReparentDebugWindow(_canvasInGame);
		}
		FocusComponents.Clear();
		InputBlockingComponents.Clear();
		if (Globals.GameState == GameStates.Intro || Globals.GameState == GameStates.Menu)
		{
			MenuUi = new MenuGuiBase(_canvasMainMenu);
			_uiInGame = null;
		}
		else
		{
			GameUi = new GameInterface(_canvasInGame);
			_uiMainMenu = null;
		}
		_showStatisticsPanelFPS = Globals.Database?.ShowFPSCounter ?? false;
		_showStatisticsPanelPing = Globals.Database?.ShowPingCounter ?? false;
		_statisticsPanel = new StatisticsPanel(CurrentInterface.Root)
		{
			IsFPSEnabled = _showStatisticsPanelFPS,
			IsPingEnabled = _showStatisticsPanelPing
		};
		Globals.EmitLifecycleChangedState();
		_initialized = true;
		Action result;
		while (_uiInGame != null && PendingActionsForInGameInterface.TryDequeue(out result))
		{
			result();
		}
	}

	public static void DestroyGwen(bool exiting = false)
	{
		if (!exiting)
		{
			MutableInterface.DetachDebugWindow();
		}
		MenuGuiBase uiMainMenu = _uiMainMenu;
		if (uiMainMenu != null)
		{
			uiMainMenu.Dispose();
		}
		else
		{
			_canvasMainMenu?.Dispose();
		}
		_canvasMainMenu = null;
		_uiMainMenu = null;
		if (Globals.Me != null)
		{
			Globals.Me.ClearTarget();
			Globals.Me.TargetBox?.Dispose();
			Globals.Me.TargetBox = null;
		}
		GameInterface uiInGame = _uiInGame;
		if (uiInGame != null)
		{
			uiInGame.Dispose();
		}
		else
		{
			_canvasInGame?.Dispose();
		}
		_canvasInGame = null;
		_uiInGame = null;
		_initialized = false;
	}

	public static bool HasInputFocus()
	{
		Intersect.Client.Framework.Gwen.Control.Base keyboardFocus = InputHandler.KeyboardFocus;
		if (keyboardFocus is TextBox && keyboardFocus.IsVisibleInTree)
		{
			return true;
		}
		if (!FocusComponents.Any((Intersect.Client.Framework.Gwen.Control.Base component) => component != null && component.MouseInputEnabled && component.HasFocus))
		{
			return InputBlockingComponents.Any((Intersect.Client.Framework.Gwen.Control.Base component) => component != null && component.IsVisibleInTree && component.IsBlockingInput);
		}
		return true;
	}

	public static void DrawGui(TimeSpan elapsed, TimeSpan total)
	{
		if (!_initialized)
		{
			InitGwen();
		}
		if (Globals.GameState == GameStates.Menu)
		{
			MenuUi.Update(elapsed, total);
		}
		else if (Globals.GameState == GameStates.InGame)
		{
			using (ChronoImage.Etape("interface : mise a jour"))
			{
				GameUi.Update(elapsed, total);
			}
		}
		int num;
		if (!Globals.InCraft && !Globals.InBank && !Globals.InShop && !Globals.InTrade && !Globals.InBag && Globals.EventDialogs.Count <= 0 && !HasInputFocus())
		{
			GameInterface uiInGame = _uiInGame;
			if (uiInGame != null)
			{
				EscapeMenuWindow escapeMenu = uiInGame.EscapeMenu;
				if (escapeMenu != null)
				{
					num = (escapeMenu.IsVisibleInTree ? 1 : 0);
					goto IL_00ab;
				}
			}
			num = 0;
		}
		else
		{
			num = 1;
		}
		goto IL_00ab;
		IL_00ab:
		bool flag = (byte)num != 0;
		AlertWindow.OpenPendingAlertWindowsFrom(PendingErrorMessages);
		Canvas canvasInGame = _canvasInGame;
		if (canvasInGame != null)
		{
			canvasInGame.RestrictToParent = false;
		}
		if (Globals.GameState == GameStates.Menu)
		{
			MenuUi.Draw(elapsed, total);
		}
		else
		{
			if (Globals.GameState != GameStates.InGame)
			{
				return;
			}
			Canvas canvasInGame2;
			if (!HideUi || flag)
			{
				canvasInGame2 = _canvasInGame;
				if (canvasInGame2 != null && !canvasInGame2.IsVisibleInTree)
				{
					_canvasInGame.Show();
				}
				using (ChronoImage.Etape("interface : dessin des fenetres"))
				{
					GameUi.Draw(elapsed, total);
					return;
				}
			}
			canvasInGame2 = _canvasInGame;
			if (canvasInGame2 != null && canvasInGame2.IsVisibleInTree)
			{
				_canvasInGame.Hide();
			}
		}
	}

	public static void SetHandleInput(bool val)
	{
		GwenInput.HandleInput = val;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsInBounds(int x, int y)
	{
		float echelleDeLInterface = Graphics.EchelleDeLInterface;
		if (echelleDeLInterface != 1f)
		{
			x = (int)((float)x / echelleDeLInterface);
			y = (int)((float)y / echelleDeLInterface);
		}
		if (HasCurrentInterface)
		{
			return CurrentInterface.Root.Bounds.Contains(x, y);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsInBounds(Point point)
	{
		if (HasCurrentInterface)
		{
			return CurrentInterface.Root.Bounds.Contains(point);
		}
		return false;
	}

	public static bool DoesMouseHitInterface()
	{
		Intersect.Client.Framework.Gwen.Control.Base mouseFocus = InputHandler.MouseFocus;
		bool flag = ((mouseFocus is FenetreCharte fenetreCharte && (fenetreCharte.TientUnObjet || fenetreCharte.EnDeplacement)) ? true : false);
		if (flag || DeplacementDesFenetres.EnCours)
		{
			return true;
		}
		long image = ChronoImage.Image;
		Point mousePosition = InputHandler.MousePosition;
		if (_imageDuSurvol == image && _pointDuSurvol == mousePosition)
		{
			return _survolDeLInterface;
		}
		_imageDuSurvol = image;
		_pointDuSurvol = mousePosition;
		_survolDeLInterface = DoesMouseHitComponentOrChildren(_canvasInGame);
		return _survolDeLInterface;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool DoesMouseHitComponentOrChildren(Intersect.Client.Framework.Gwen.Control.Base? component)
	{
		return DoesComponentOrChildrenContainMousePoint(component, InputHandler.MousePosition);
	}

	public static bool DoesComponentOrChildrenContainMousePoint(Intersect.Client.Framework.Gwen.Control.Base? component, Point position)
	{
		if (component == null)
		{
			return false;
		}
		if (component.IsHidden)
		{
			return false;
		}
		if (!component.Bounds.Contains(position))
		{
			return false;
		}
		if (component.MouseInputEnabled)
		{
			return true;
		}
		Point position2 = position;
		position2.X -= component.X;
		position2.Y -= component.Y;
		IReadOnlyList<Intersect.Client.Framework.Gwen.Control.Base> children = component.Children;
		for (int i = 0; i < children.Count; i++)
		{
			if (DoesComponentOrChildrenContainMousePoint(children[i], position2))
			{
				return true;
			}
		}
		return false;
	}
}
