using System;
using System.Linq;
using System.Numerics;
using Intersect.Admin.Actions;
using Intersect.Client.Core.Controls;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.DragDrop;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Housing;
using Intersect.Client.Interface;
using Intersect.Client.Interface.Game;
using Intersect.Client.Interface.Game.Dungeons;
using Intersect.Client.Interface.Game.Housing;
using Intersect.Client.Interface.Game.Recompenses;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Maps;
using Intersect.Client.Networking;
using Intersect.Configuration;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Events;

namespace Intersect.Client.Core;

public static class Input
{
	public delegate void HandleKeyEvent(Keys modifier, Keys key);

	private static HandleKeyEvent? _keyDown;

	private static HandleKeyEvent? _keyUp;

	private static HandleKeyEvent? _mouseDown;

	private static HandleKeyEvent? _mouseUp;

	public static HandleKeyEvent? KeyDown
	{
		get
		{
			return _keyDown;
		}
		set
		{
			_keyDown = value;
		}
	}

	public static HandleKeyEvent? KeyUp
	{
		get
		{
			return _keyUp;
		}
		set
		{
			_keyUp = value;
		}
	}

	public static HandleKeyEvent? MouseDown
	{
		get
		{
			return _mouseDown;
		}
		set
		{
			_mouseDown = value;
		}
	}

	public static HandleKeyEvent? MouseUp
	{
		get
		{
			return _mouseUp;
		}
		set
		{
			_mouseUp = value;
		}
	}

	public static event MouseButtonEventInterceptor? MouseDownIntercept;

	public static event MouseButtonEventInterceptor? MouseUpIntercept;

	public static void HandleZoomOut(bool wrap = true)
	{
		float num = Globals.Database.WorldZoom / 2f;
		if (num < Graphics.MinimumWorldScale)
		{
			num = (wrap ? Graphics.MaximumWorldScale : Graphics.MinimumWorldScale);
		}
		Globals.Database.WorldZoom = num;
	}

	public static void HandleZoomIn(bool wrap = true)
	{
		float num = Globals.Database.WorldZoom * 2f;
		if (num > Graphics.MaximumWorldScale)
		{
			num = (wrap ? Graphics.MinimumWorldScale : Graphics.MaximumWorldScale);
		}
		Globals.Database.WorldZoom = num;
	}

	private static void MarkPlayerActive()
	{
		Player me = Globals.Me;
		if (me != null)
		{
			me.LastInputTime = Timing.Global.Milliseconds;
		}
	}

	private static int DialogNumber(Keys key)
	{
		switch (key)
		{
		case Keys.D1:
		case Keys.NumPad1:
			return 1;
		case Keys.D2:
		case Keys.NumPad2:
			return 2;
		case Keys.D3:
		case Keys.NumPad3:
			return 3;
		case Keys.D4:
		case Keys.NumPad4:
			return 4;
		default:
			return 0;
		}
	}

	public static void OnKeyPressed(Keys modifier, Keys key)
	{
		if (key == Keys.None)
		{
			return;
		}
		MarkPlayerActive();
		GameStates gameState = Globals.GameState;
		bool flag = false;
		bool flag2 = true;
		KeyDown?.Invoke(modifier, key);
		switch (key)
		{
		case Keys.Escape:
			if (gameState == GameStates.Intro)
			{
				Fade.FadeIn(ClientConfiguration.Instance.FadeDurationMs);
				Globals.GameState = GameStates.Menu;
				return;
			}
			break;
		case Keys.Return:
		{
			QuantityWindow current2 = QuantityWindow.Current;
			if (current2 != null)
			{
				current2.Confirm();
				flag = true;
				flag2 = false;
				break;
			}
			if (gameState == GameStates.InGame && Intersect.Client.Interface.Interface.HasInGameUI)
			{
				FenetreRecompenses dailyRewardWindow = Intersect.Client.Interface.Interface.GameUi.DailyRewardWindow;
				if (dailyRewardWindow != null && dailyRewardWindow.Reclamer())
				{
					flag = true;
					flag2 = false;
					break;
				}
			}
			ConfirmCardWindow current3 = ConfirmCardWindow.Current;
			if (current3 != null)
			{
				current3.Confirm();
				flag = true;
				flag2 = false;
				break;
			}
			RequestCardWindow current4 = RequestCardWindow.Current;
			if (current4 != null)
			{
				current4.Accept();
				flag = true;
				flag2 = false;
				break;
			}
			HellBailWindow current5 = HellBailWindow.Current;
			if (current5 != null)
			{
				current5.Confirm();
				flag = true;
				flag2 = false;
				break;
			}
			if (HouseInterface.ConfirmOnEnter())
			{
				flag = true;
				flag2 = false;
				break;
			}
			if (DungeonInterface.ConfirmOnEnter())
			{
				flag = true;
				flag2 = false;
				break;
			}
			Base[] array = Intersect.Client.Interface.Interface.InputBlockingComponents.ToArray();
			for (int num = array.Length - 1; num >= 0; num--)
			{
				Base obj = array[num];
				try
				{
					if (obj is InputBox inputBox && !obj.IsHidden)
					{
						inputBox.SubmitInput();
						flag = true;
						flag2 = false;
						break;
					}
				}
				catch
				{
				}
				try
				{
					if (obj is EventWindow eventWindow && !obj.IsHidden && Globals.EventDialogs.Count > 0)
					{
						eventWindow.CloseEventResponse(EventResponseType.OneOption);
						flag = true;
						flag2 = false;
						break;
					}
				}
				catch
				{
				}
			}
			if (flag)
			{
			}
			break;
		}
		case Keys.D1:
		case Keys.D2:
		case Keys.D3:
		case Keys.D4:
		case Keys.NumPad1:
		case Keys.NumPad2:
		case Keys.NumPad3:
		case Keys.NumPad4:
		{
			EventWindow current = EventWindow.Current;
			if (current != null && !current.IsHidden && Globals.EventDialogs.Count > 0 && current.ChooseByNumber(DialogNumber(key)))
			{
				flag = true;
				flag2 = false;
			}
			break;
		}
		}
		if (Intersect.Client.Core.Controls.Controls.ControlHasKey(Control.OpenMenu, modifier, key) || (modifier != Keys.None && Intersect.Client.Core.Controls.Controls.ControlHasKey(Control.OpenMenu, Keys.None, key)))
		{
			if (gameState != GameStates.InGame || !Intersect.Client.Interface.Interface.HasInGameUI || DragAndDrop.IsDragging)
			{
				return;
			}
			GameInterface gameUi = Intersect.Client.Interface.Interface.GameUi;
			if (gameUi.ChatFocussed)
			{
				gameUi.UnfocusChat = true;
			}
			else if (!gameUi.CloseAllWindows())
			{
				Player me = Globals.Me;
				if (me != null && me.TargetId != default(Guid) && me.Status.All((IStatus s) => s.Type != SpellEffect.Taunt))
				{
					me.ClearTarget();
				}
				else
				{
					gameUi.EscapeMenu.ToggleHidden();
				}
			}
		}
		if (gameState == GameStates.InGame && Intersect.Client.Interface.Interface.HasInGameUI && !DragAndDrop.IsDragging)
		{
			Base keyboardFocus = InputHandler.KeyboardFocus;
			if (!(keyboardFocus is TextBox) || !keyboardFocus.IsVisibleInTree)
			{
				GameInterface gameUi2 = Intersect.Client.Interface.Interface.GameUi;
				if (gameUi2.IsTeachingsOpen && Intersect.Client.Core.Controls.Controls.ControlHasKey(Control.OpenTeachings, modifier, key))
				{
					gameUi2.ToggleTeachingsScreen();
					return;
				}
				if (gameUi2.IsRankingsOpen && Intersect.Client.Core.Controls.Controls.ControlHasKey(Control.OpenRankings, modifier, key))
				{
					gameUi2.ToggleRankingsScreen();
					return;
				}
				if (gameUi2.IsAchievementsOpen && Intersect.Client.Core.Controls.Controls.ControlHasKey(Control.OpenAchievements, modifier, key))
				{
					gameUi2.ToggleAchievementsWindow();
					return;
				}
				CharteScreen ouvert = CharteScreen.Ouvert;
				if (ouvert != null && ouvert.IsVisibleInTree && ouvert.HandleKey(modifier, key))
				{
					return;
				}
				keyboardFocus = InputHandler.HoveredControl;
				if (keyboardFocus is FenetreCharte fenetreCharte && keyboardFocus.IsVisibleInTree && fenetreCharte.HandleKey(modifier, key))
				{
					return;
				}
			}
		}
		if (Intersect.Client.Interface.Interface.HasInputFocus())
		{
			return;
		}
		foreach (Control item in Intersect.Client.Core.Controls.Controls.GetControlsFor(modifier, key))
		{
			if (flag)
			{
				continue;
			}
			switch (item)
			{
			case Control.Screenshot:
				Graphics.Renderer?.RequestScreenshot();
				break;
			case Control.ToggleGui:
				if (gameState == GameStates.InGame && !DragAndDrop.IsDragging)
				{
					Intersect.Client.Interface.Interface.HideUi = !Intersect.Client.Interface.Interface.HideUi;
				}
				break;
			case Control.ToggleZoomIn:
			case Control.HoldToZoomIn:
				HandleZoomIn();
				break;
			case Control.ToggleZoomOut:
			case Control.HoldToZoomOut:
				HandleZoomOut();
				break;
			case Control.ToggleFullscreen:
				if (Graphics.Renderer != null)
				{
					Globals.Database.FullScreen = !Globals.Database.FullScreen;
					Globals.Database.SavePreferences();
					Graphics.Renderer.OverrideResolution = Resolution.Empty;
					Graphics.Renderer.Init();
				}
				break;
			case Control.OpenDebugger:
				Intersect.Client.Interface.Interface.CurrentInterface.ToggleDebug();
				break;
			}
			switch (gameState)
			{
			case GameStates.InGame:
				if (!Intersect.Client.Interface.Interface.HasInGameUI || DragAndDrop.IsDragging)
				{
					break;
				}
				switch (item)
				{
				case Control.Block:
					Globals.Me?.TryBlock();
					break;
				case Control.AutoTarget:
					Globals.Me?.AutoTarget();
					break;
				case Control.HoldToSoftRetargetOnSelfCast:
					Globals.HoldToSoftRetargetOnSelfCast = true;
					break;
				case Control.ToggleAutoSoftRetargetOnSelfCast:
					Globals.Database.AutoSoftRetargetOnSelfCast = !Globals.Database.AutoSoftRetargetOnSelfCast;
					break;
				case Control.PickUp:
					if (Globals.Me != null && Globals.Me.MapInstance != null)
					{
						var (mapId, tileIndex) = Globals.Me.CaseDeRamassage();
						Player.TryPickupItem(mapId, tileIndex);
					}
					break;
				case Control.Enter:
					if (flag2)
					{
						Intersect.Client.Interface.Interface.GameUi.FocusChat = true;
						flag = true;
					}
					break;
				case Control.OpenInventory:
					Intersect.Client.Interface.Interface.GameUi.GameMenu?.ToggleInventoryWindow();
					break;
				case Control.OpenQuests:
					Intersect.Client.Interface.Interface.GameUi.GameMenu?.ToggleQuestsWindow();
					break;
				case Control.OpenCharacterInfo:
					Intersect.Client.Interface.Interface.GameUi.GameMenu?.ToggleCharacterWindow();
					break;
				case Control.OpenParties:
					Intersect.Client.Interface.Interface.GameUi.GameMenu?.TogglePartyWindow();
					break;
				case Control.OpenSpells:
					Intersect.Client.Interface.Interface.GameUi.GameMenu?.ToggleSpellsWindow();
					break;
				case Control.OpenFriends:
					Intersect.Client.Interface.Interface.GameUi.GameMenu?.ToggleFriendsWindow();
					break;
				case Control.OpenSettings:
					Intersect.Client.Interface.Interface.GameUi.EscapeMenu?.OpenSettingsWindow();
					break;
				case Control.OpenAdminPanel:
				{
					Access? access = Globals.Me?.AccessLevel;
					bool flag3 = ((!access.HasValue || access.GetValueOrDefault() == Access.None) ? true : false);
					if (!flag3 && Intersect.Client.Interface.Interface.GameUi.ToggleAdminWindow())
					{
						PacketSender.SendOpenAdminWindow();
					}
					break;
				}
				case Control.OpenGuild:
					Intersect.Client.Interface.Interface.GameUi.GameMenu?.ToggleGuildWindow();
					break;
				case Control.OpenWorldMap:
					Intersect.Client.Interface.Interface.GameUi.ToggleWorldMapWindow();
					break;
				case Control.OpenAchievements:
					Intersect.Client.Interface.Interface.GameUi.ToggleAchievementsWindow();
					break;
				case Control.OpenMarket:
					if (Houses.CanDecorateHere)
					{
						Decoration.Toggle();
					}
					break;
				case Control.OpenDailyRewards:
					Intersect.Client.Interface.Interface.GameUi.ToggleDailyRewardWindow();
					break;
				case Control.OpenProfessions:
					Intersect.Client.Interface.Interface.GameUi.ToggleProfessionsWindow();
					break;
				case Control.OpenCalendar:
					Intersect.Client.Interface.Interface.GameUi.ToggleCalendarWindow();
					break;
				case Control.OpenTeachings:
					Intersect.Client.Interface.Interface.GameUi.ToggleTeachingsScreen();
					break;
				case Control.OpenRankings:
					Intersect.Client.Interface.Interface.GameUi.ToggleRankingsScreen();
					break;
				}
				break;
			default:
				throw new NotImplementedException($"{"GameStates"} '{gameState}' not yet implemented");
			case GameStates.Intro:
			case GameStates.Menu:
			case GameStates.Loading:
			case GameStates.Error:
				break;
			}
		}
	}

	public static void OnKeyReleased(Keys modifier, Keys key)
	{
		KeyUp?.Invoke(modifier, key);
		if (!Intersect.Client.Interface.Interface.HasInputFocus())
		{
			if (Intersect.Client.Core.Controls.Controls.ControlHasKey(Control.HoldToZoomIn, modifier, key))
			{
				HandleZoomOut();
			}
			if (Intersect.Client.Core.Controls.Controls.ControlHasKey(Control.HoldToZoomOut, modifier, key))
			{
				HandleZoomIn();
			}
			if (Intersect.Client.Core.Controls.Controls.ControlHasKey(Control.HoldToSoftRetargetOnSelfCast, modifier, key))
			{
				Globals.HoldToSoftRetargetOnSelfCast = false;
			}
			_ = Globals.Me;
		}
	}

	private static bool InvokeMouseButtonInterceptors(MulticastDelegate? multicastDelegate, Keys modifier, MouseButton mouseButton)
	{
		Delegate[] source = multicastDelegate?.GetInvocationList() ?? Array.Empty<Delegate>();
		MouseButtonEventInterceptor[] source2 = source.OfType<MouseButtonEventInterceptor>().ToArray();
		return source2.Any((MouseButtonEventInterceptor interceptor) => interceptor(modifier, mouseButton));
	}

	public static bool TestInterceptMouse(Keys modifier, MouseButton mouseButton, bool down)
	{
		if (modifier != Keys.Alt)
		{
			return InvokeMouseButtonInterceptors(down ? Input.MouseDownIntercept : Input.MouseUpIntercept, modifier, mouseButton);
		}
		return false;
	}

	public static void OnMouseDown(Keys modifier, MouseButton mouseButton)
	{
		MarkPlayerActive();
		Keys keys = Keys.None;
		switch (mouseButton)
		{
		case MouseButton.Left:
			keys = Keys.LButton;
			break;
		case MouseButton.Right:
			keys = Keys.RButton;
			break;
		case MouseButton.Middle:
			keys = Keys.MButton;
			break;
		case MouseButton.X1:
			keys = Keys.XButton1;
			break;
		case MouseButton.X2:
			keys = Keys.XButton2;
			break;
		}
		MouseDown?.Invoke(modifier, keys);
		if (InputHandler.KeyboardFocus is TextBox textBox && Globals.GameState == GameStates.InGame)
		{
			GameInterface gameUi = Intersect.Client.Interface.Interface.GameUi;
			if ((gameUi == null || !gameUi.ChatFocussed) && !Intersect.Client.Interface.Interface.DoesMouseHitInterface())
			{
				textBox.Blur();
			}
		}
		if (Intersect.Client.Interface.Interface.HasInputFocus() || Globals.GameState != GameStates.InGame || Globals.Me == null || Intersect.Client.Interface.Interface.DoesMouseHitInterface() || Globals.Me == null || (mouseButton == MouseButton.Left && Globals.Me.TryFireBeamOnClick()))
		{
			return;
		}
		bool flag = mouseButton == MouseButton.Left && Intersect.Client.Core.Controls.Controls.EstUnClicOrdinaire(modifier, keys);
		if ((flag && Globals.Me.TryParlerAuCurseur()) || (flag && Globals.Me.TryMenuDuJoueurAuCurseur()) || ((!Heritage.Actif || !Heritage.Cible.EnCombat(Globals.Me)) && flag && Globals.Me.TryRecolterAuClic()) || (flag && Globals.Me.TryTarget()))
		{
			return;
		}
		if (Intersect.Client.Core.Controls.Controls.ControlHasKey(Control.PickUp, modifier, keys))
		{
			var (mapId, tileIndex) = Globals.Me.CaseDeRamassage();
			if (Globals.Me.MapInstance != null && Player.TryPickupItem(mapId, tileIndex, Guid.Empty, firstOnly: true))
			{
				return;
			}
			if (!Globals.Me.IsAttacking)
			{
				Globals.Me.AttackTimer = Timing.Global.Milliseconds + Globals.Me.CalculateAttackTime();
			}
		}
		if ((!Intersect.Client.Core.Controls.Controls.ControlHasKey(Control.Block, modifier, keys) || !Globals.Me.TryBlock()) && keys != Keys.None)
		{
			OnKeyPressed(modifier, keys);
		}
	}

	public static void OnMouseUp(Keys modifier, MouseButton mouseButton)
	{
		Keys key = Keys.LButton;
		switch (mouseButton)
		{
		case MouseButton.Right:
			key = Keys.RButton;
			break;
		case MouseButton.Middle:
			key = Keys.MButton;
			break;
		case MouseButton.X1:
			key = Keys.XButton1;
			break;
		case MouseButton.X2:
			key = Keys.XButton2;
			break;
		}
		MouseUp?.Invoke(modifier, key);
		if (Intersect.Client.Interface.Interface.HasInputFocus())
		{
			return;
		}
		if (Intersect.Client.Core.Controls.Controls.ControlHasKey(Control.HoldToZoomIn, modifier, key))
		{
			HandleZoomOut();
		}
		if (Intersect.Client.Core.Controls.Controls.ControlHasKey(Control.HoldToZoomOut, modifier, key))
		{
			HandleZoomIn();
		}
		if (Globals.Me == null || mouseButton != MouseButton.Right || !Globals.InputManager.IsKeyDown(Keys.Shift) || !Globals.InputManager.IsKeyDown(Keys.Control))
		{
			return;
		}
		Vector2 vector = Graphics.ConvertToWorldPoint(Globals.InputManager.GetMousePosition());
		int num = (int)vector.X;
		int num2 = (int)vector.Y;
		foreach (MapInstance item in MapInstance.Lookup.Values.Cast<MapInstance>())
		{
			if (num >= item.X && num <= item.X + Options.Instance.Map.MapWidth * Options.Instance.Map.TileWidth && num2 >= item.Y && num2 <= item.Y + Options.Instance.Map.MapHeight * Options.Instance.Map.TileHeight)
			{
				num -= item.X;
				num2 -= item.Y;
				num /= Options.Instance.Map.TileWidth;
				num2 /= Options.Instance.Map.TileHeight;
				Guid mapId = item.Id;
				if (Globals.Me.TryGetRealLocation(ref num, ref num2, ref mapId))
				{
					PacketSender.SendAdminAction(new WarpToLocationAction(item.Id, (byte)num, (byte)num2));
				}
				break;
			}
		}
	}

	public static bool IsModifier(Keys key)
	{
		switch (key)
		{
		case Keys.ShiftKey:
		case Keys.ControlKey:
		case Keys.LShiftKey:
		case Keys.RShiftKey:
		case Keys.LControlKey:
		case Keys.RControlKey:
		case Keys.Shift:
		case Keys.Control:
		case Keys.Alt:
			return true;
		default:
			return false;
		}
	}
}
