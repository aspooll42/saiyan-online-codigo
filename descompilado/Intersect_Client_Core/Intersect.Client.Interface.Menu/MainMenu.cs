using System;
using System.Collections.Generic;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Interface.Shared;
using Intersect.Framework.Core;
using Intersect.Network;

namespace Intersect.Client.Interface.Menu;

public class MainMenu : MutableInterface
{
	public delegate void NetworkStatusHandler();

	private readonly Canvas _menuCanvas;

	private readonly MenuScene _scene;

	private PasswordChangeWindow? _passwordChangeWindow;

	private SettingsWindow? _settingsWindow;

	public static NetworkStatus ActiveNetworkStatus { get; set; }

	public static int ServerOnlineCount { get; set; }

	public static int ServerMaxCount { get; set; }

	public static bool ServerAdminOnly { get; set; }

	public static long LastNetworkStatusChangeTime { get; private set; }

	private SettingsWindow SettingsWindow => _settingsWindow ?? (_settingsWindow = new SettingsWindow(_menuCanvas)
	{
		Alignment = new Alignments[1] { Alignments.Center },
		IsVisibleInTree = false
	});

	internal bool IsEngineWindowOpen
	{
		get
		{
			SettingsWindow settingsWindow = _settingsWindow;
			if (settingsWindow == null || !settingsWindow.IsVisibleInTree)
			{
				return _passwordChangeWindow?.IsVisibleInTree ?? false;
			}
			return true;
		}
	}

	internal bool IsPasswordChangeOpen => _passwordChangeWindow != null;

	public static event NetworkStatusHandler? NetworkStatusChanged;

	internal static event EventHandler? ReceivedConfiguration;

	public MainMenu(Canvas menuCanvas)
		: base(menuCanvas)
	{
		_menuCanvas = menuCanvas;
		_scene = new MenuScene(this, menuCanvas);
	}

	public static void HandleReceivedConfiguration()
	{
		MainMenu.ReceivedConfiguration?.Invoke(null, EventArgs.Empty);
	}

	public void Update(TimeSpan elapsed, TimeSpan total)
	{
		_scene.Update((float)Math.Min(elapsed.TotalSeconds, 0.1));
		SettingsWindow settingsWindow = _settingsWindow;
		if (settingsWindow != null && settingsWindow.IsVisibleInTree)
		{
			settingsWindow.Update();
		}
	}

	internal void DrawScene()
	{
		_scene.Draw();
	}

	internal void DisposeScene()
	{
		_scene.Dispose();
	}

	public bool TryShowErrorInline(string message)
	{
		return _scene.TryShowErrorInline(message);
	}

	public void Reset()
	{
		_passwordChangeWindow?.DelayedDelete();
		_passwordChangeWindow = null;
		_scene.OnServerReset();
	}

	public void Show()
	{
		_scene.ShowMain();
	}

	internal void ReturnToMain()
	{
		_scene.ShowMain();
	}

	internal void ReturnToCharacterSelection()
	{
		_scene.ShowSelectionAgain();
	}

	internal void ReturnToWindow(Base window)
	{
		_scene.ShowLogin();
	}

	public void NotifyOpenCharacterSelection(List<CharacterSelectionPreviewMetadata> characterSelectionPreviews, string username)
	{
		_scene.ShowCharacters(characterSelectionPreviews, username);
	}

	public void NotifyOpenForgotPassword()
	{
		_scene.ShowForgot();
	}

	public void NotifyOpenLogin()
	{
		_passwordChangeWindow?.DelayedDelete();
		_passwordChangeWindow = null;
		_scene.ShowLogin();
	}

	public void OpenPasswordChangeWindow(string? identifier, PasswordChangeMode changeMode, Window? previousWindow)
	{
		_passwordChangeWindow?.DelayedDelete();
		_passwordChangeWindow = new PasswordChangeWindow(_menuCanvas, this, previousWindow, changeMode)
		{
			Target = identifier
		};
		_passwordChangeWindow.Disposed += PasswordChangeWindowOnDisposed;
	}

	private void PasswordChangeWindowOnDisposed(Base sender, EventArgs _)
	{
		if (sender == _passwordChangeWindow)
		{
			_passwordChangeWindow = null;
		}
	}

	public void NotifyOpenCharacterCreation(bool force = false)
	{
		_scene.ShowCreation(force);
	}

	internal void SettingsButton_Clicked()
	{
		OpenSettings();
	}

	internal void OpenSettings()
	{
		SettingsWindow settingsWindow = SettingsWindow;
		settingsWindow.Show(null);
	}

	public static void SetNetworkStatus(NetworkStatus networkStatus, bool resetStatusCheck = false)
	{
		if (ActiveNetworkStatus != networkStatus)
		{
			ActiveNetworkStatus = networkStatus;
			MainMenu.NetworkStatusChanged?.Invoke();
		}
		LastNetworkStatusChangeTime = (resetStatusCheck ? (-1) : Timing.Global.MillisecondsUtc);
	}
}
