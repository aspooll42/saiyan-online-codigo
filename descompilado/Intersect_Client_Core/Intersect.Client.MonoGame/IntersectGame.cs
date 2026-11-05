using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Intersect.Client.Core;
using Intersect.Client.Framework.Database;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface;
using Intersect.Client.Interface.Menu;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.MonoGame.File_Management;
using Intersect.Client.MonoGame.Graphics;
using Intersect.Client.MonoGame.Input;
using Intersect.Client.MonoGame.NativeInterop;
using Intersect.Client.MonoGame.NativeInterop.OpenGL;
using Intersect.Client.MonoGame.Network;
using Intersect.Client.Networking;
using Intersect.Client.Security;
using Intersect.Client.ThirdParty;
using Intersect.Configuration;
using Intersect.Core;
using Intersect.Framework.Core;
using Intersect.Framework.Core.AssetManagement;
using Intersect.Framework.Core.IO;
using Intersect.Framework.SystemInformation;
using Intersect.Framework.Utilities;
using Intersect.Network;
using Intersect.Network.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Intersect.Client.MonoGame;

internal class IntersectGame : Game
{
	internal class MonoGameRunner : IPlatformRunner
	{
		private delegate void SdlInit(int flags);

		public void Start(IClientContext context, Action postStartupAction)
		{
			try
			{
				DoSdlInit(12832);
			}
			catch (Exception exception)
			{
				ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Error occurred while trying to initialize SDL");
			}
#if !ANDROID
			using IntersectGame intersectGame = new IntersectGame(context, postStartupAction);
			try
			{
				intersectGame.Run();
			}
#else
			IntersectGame intersectGame = new IntersectGame(context, postStartupAction);
			try
			{
				Intersect.Client.Android.MainActivity.Actual.MostrarJuego(intersectGame);
			}
#endif
			catch (Exception exception2)
			{
				context.Logger.LogCritical(exception2, "Game is crashing due to an exception");
				throw;
			}
		}

		private static void DoSdlInit(int flags)
		{
			if (PlatformHelper.CurrentPlatform == Platform.Linux)
			{
				SdlInit sdlInit = ((AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly assembly) => assembly.FullName?.StartsWith("MonoGame.Framework") ?? false)?.GetType("Sdl"))?.GetMethod("Init"))?.CreateDelegate<SdlInit>();
				if (sdlInit == null)
				{
					throw new InvalidOperationException("Missing Sdl.Init() from MonoGame");
				}
				sdlInit(flags);
				if (!Sdl2.SDL_SetHint("SDL_VIDEO_X11_NET_WM_BYPASS_COMPOSITOR", value: false))
				{
					ApplicationContext.Context.Value?.Logger.LogWarning("Failed to set X11 Compositor hint");
				}
			}
		}
	}

	private bool mInitialized;

	private double mLastUpdateTime;

	private readonly GameRenderer _gameRenderer;

	private GraphicsDeviceManager mGraphics;

	private Updater? _updater;

	private Texture2D? updaterBackground;

	private SpriteFont? updaterFont;

	private SpriteFont? updaterFontSmall;

	private Texture2D? updaterProgressBar;

	private SpriteBatch? updateBatch;

	private bool updaterGraphicsReset;

	public static bool _isShowingExitConfirmation;

	private TimeSpan _elapsedSincePlatformStatisticsRefresh;

	private bool _restartFailed;

	private IClientContext Context { get; }

	private Action PostStartupAction { get; }

	private IntersectGame(IClientContext context, Action postStartupAction)
	{
		Context = context;
		PostStartupAction = postStartupAction;
		try
		{
			Strings.Load();
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, "Error occurred loading strings for client");
			throw;
		}
		mGraphics = new GraphicsDeviceManager(this)
		{
			PreferredBackBufferWidth = 800,
			PreferredBackBufferHeight = 480,
			PreferHalfPixelOffset = true,
			PreferMultiSampling = true
		};
		mGraphics.PreparingDeviceSettings += delegate(object? _, PreparingDeviceSettingsEventArgs args)
		{
			args.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
			args.GraphicsDeviceInformation.PresentationParameters.MultiSampleCount = ((!OperatingSystem.IsMacOS()) ? 8 : 0);
		};
		string path = context.StartupOptions.WorkingDirectory ?? string.Empty;
		string fullPath = Path.GetFullPath(path, Environment.CurrentDirectory);
		base.Content = new ContenuDuCoffre(base.Services);
		LectureDesRessources.Existe = Coffre.Existe;
		LectureDesRessources.LireTexte = Coffre.LireTexte;
		LectureDesRessources.DateUtc = Coffre.DateUtc;
		base.Content.RootDirectory = fullPath;
		base.IsMouseVisible = true;
		Globals.ContentManager = new MonoContentManager();
		Globals.Database = new JsonDatabase();
		AntiOutils.Verifier();
#if !ANDROID
		Globals.Database.LoadPreferences();
#else
		Globals.Database.LoadPreferences();
		if (string.IsNullOrEmpty(Globals.Database.LoadPreference("ModePerformances")))
		{
			Globals.Database.ModePerformances = true;
		}
#endif
#if !ANDROID
		base.Window.IsBorderless = Context.StartupOptions.BorderlessWindow;
#endif
		_gameRenderer = new MonoRenderer(mGraphics, base.Content, this)
		{
			OverrideResolution = Context.StartupOptions.ScreenResolution
		};
		Globals.InputManager = new MonoInput(this);
		GameClipboard.Instance = new MonoClipboard();
		Intersect.Client.Core.Graphics.Renderer = _gameRenderer;
		Intersect.Client.Interface.Interface.GwenRenderer = new IntersectRenderer(null, Intersect.Client.Core.Graphics.Renderer);
		Intersect.Client.Interface.Interface.GwenInput = new IntersectInput();
#if !ANDROID
		base.Window.Position = new Microsoft.Xna.Framework.Point(_gameRenderer.ScreenWidth - _gameRenderer.ActiveResolution.X, _gameRenderer.ScreenHeight - _gameRenderer.ActiveResolution.Y) / new Microsoft.Xna.Framework.Point(2);
		base.Window.AllowAltF4 = false;
#endif
		string text = ClientConfiguration.Instance.UpdateUrl;
		if (OperatingSystem.IsMacOS())
		{
			ApplicationContext.Context.Value?.Logger.LogInformation("Version Mac : sans anticrenelage ni Steam, certificat ordinaire (28/10/2026)");
		}
		if (OperatingSystem.IsMacOS() && text != null && text.EndsWith("update-mac.json", StringComparison.Ordinal))
		{
			string text2 = text;
			int length = "update-mac.json".Length;
			text = text2.Substring(0, text2.Length - length) + ((RuntimeInformation.ProcessArchitecture == Architecture.Arm64) ? "update-mac-apple.json" : "update-mac-intel.json");
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			if (_updater == null)
			{
				_updater = new Updater(text, "client/update.json", "version.client.json", 7);
			}
			_updater.Start();
		}
	}

	protected override void Initialize()
	{
		base.Initialize();
#if !ANDROID
		PlatformStatistics.GPUStatisticsProvider = GL.CreateGPUStatisticsProvider();
#endif
		if (_updater != null)
		{
			mGraphics.PreferredBackBufferWidth = 800;
			mGraphics.PreferredBackBufferHeight = 480;
		}
		if (!OperatingSystem.IsMacOS() && !OperatingSystem.IsAndroid() && Steam.SteamDeck)
		{
			base.Window.IsBorderless = true;
			Resolution resolution = new Resolution(base.GraphicsDevice.DisplayMode.Width, base.GraphicsDevice.DisplayMode.Height);
			_gameRenderer.PreferredResolution = resolution;
			_gameRenderer.OverrideResolution = resolution;
			mGraphics.PreferredBackBufferWidth = resolution.X;
			mGraphics.PreferredBackBufferHeight = resolution.Y;
		}
		mGraphics.ApplyChanges();
	}

	private void IntersectInit()
	{
		(Intersect.Client.Core.Graphics.Renderer as MonoRenderer)?.Init(base.GraphicsDevice);
		Intersect.Client.Networking.Network.Socket = new MonoSocket(Context);
		Intersect.Client.Networking.Network.Socket.Connected += delegate(INetworkLayerInterface _, ConnectionEventArgs connectionEventArgs)
		{
			MainMenu.SetNetworkStatus(connectionEventArgs.NetworkStatus);
		};
		Intersect.Client.Networking.Network.Socket.ConnectionFailed += delegate(INetworkLayerInterface _, ConnectionEventArgs connectionEventArgs, bool _)
		{
			MainMenu.SetNetworkStatus(connectionEventArgs.NetworkStatus);
		};
		Intersect.Client.Networking.Network.Socket.Disconnected += delegate(INetworkLayerInterface _, ConnectionEventArgs connectionEventArgs)
		{
			MainMenu.SetNetworkStatus(connectionEventArgs.NetworkStatus, resetStatusCheck: true);
		};
		Main.Start(Context);
		mInitialized = true;
		PostStartupAction();
	}

	protected override void Update(GameTime gameTime)
	{
		ChronoImage.NouvelleImage(base.IsActive);
		_elapsedSincePlatformStatisticsRefresh += gameTime.ElapsedGameTime;
		if (_elapsedSincePlatformStatisticsRefresh.TotalSeconds > 1.0)
		{
			_elapsedSincePlatformStatisticsRefresh = default(TimeSpan);
			PlatformStatistics.Refresh();
		}
		if (_updater == null)
		{
			if (_updater == null)
			{
				if (!mInitialized)
				{
					IntersectInit();
				}
				if (Globals.IsRunning)
				{
					if (mLastUpdateTime < gameTime.TotalGameTime.TotalMilliseconds)
					{
						lock (Globals.GameLock)
						{
							using (ChronoImage.Etape("mise a jour", principale: true))
							{
								Main.Update(gameTime.ElapsedGameTime);
							}
						}
					}
				}
				else
				{
					Main.DestroyGame();
					Exit();
				}
			}
		}
		else
		{
			if (_updater.CheckUpdaterContentLoaded())
			{
				LoadUpdaterContent();
			}
			switch (_updater.Status)
			{
			case UpdateStatus.None:
			case UpdateStatus.UpdateCompleted:
				if (updaterGraphicsReset)
				{
					IntersectInit();
					_updater = null;
				}
				break;
			case UpdateStatus.Restart:
				if (!_restartFailed && !ProcessHelper.TryRelaunch())
				{
					ApplicationContext.CurrentContext.Logger.LogWarning("Failed to restart automatically");
					_restartFailed = true;
				}
				break;
			}
		}
		base.Update(gameTime);
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		if (_updater != null)
		{
			LoadUpdaterContent();
		}
	}

	protected override void Draw(GameTime gameTime)
	{
		base.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
		base.GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Black);
		Updater updater = _updater;
		if (updater != null)
		{
			UpdateStatus status = updater.Status;
			if ((status == UpdateStatus.None || status == UpdateStatus.UpdateCompleted) ? true : false)
			{
				if (!updaterGraphicsReset)
				{
					if (Intersect.Client.Core.Graphics.Renderer is MonoRenderer monoRenderer)
					{
						monoRenderer.Init(base.GraphicsDevice);
						monoRenderer.Init();
						monoRenderer.Begin();
						monoRenderer.End();
					}
					updaterGraphicsReset = true;
				}
			}
			else
			{
				DrawUpdater(updater);
			}
		}
		else if (Globals.IsRunning && mInitialized)
		{
			lock (Globals.GameLock)
			{
				using (ChronoImage.Etape("dessin", principale: true))
				{
					Intersect.Client.Core.Graphics.Render(gameTime.ElapsedGameTime, gameTime.TotalGameTime);
				}
			}
		}
		base.Draw(gameTime);
	}

	protected override void OnExiting(object sender, ExitingEventArgs args)
	{
		if (!Globals.IsRunning || !mInitialized)
		{
			ApplicationContext.Context.Value?.Logger.LogInformation("System window closing (due to user interaction most likely).");
			TryExit(sender, args);
			return;
		}
		args.Cancel = true;
		bool inCombat = Globals.Me != null && Globals.Me.CombatTimer > Timing.Global?.Milliseconds && Globals.GameState == GameStates.InGame;
		if (_isShowingExitConfirmation)
		{
			return;
		}
		ConfirmCardWindow.OpenQuit(inCombat, delegate
		{
			if (Globals.Me != null)
			{
				Globals.Me.CombatTimer = 0L;
			}
			_isShowingExitConfirmation = false;
			Globals.IsRunning = false;
		}, delegate
		{
			_isShowingExitConfirmation = false;
		});
		_isShowingExitConfirmation = true;
	}

	private void TryExit(object sender, ExitingEventArgs args)
	{
		try
		{
			_updater?.Stop();
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Exception thrown while stopping the updater on game close");
		}
		try
		{
			Intersect.Client.Interface.Interface.DestroyGwen();
		}
		catch (Exception exception2)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception2, "Exception thrown while destroying GWEN on game close");
		}
		try
		{
			Intersect.Client.Networking.Network.Close("quitting");
		}
		catch (Exception exception3)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception3, "Exception thrown while closing the network on game close");
		}
		base.OnExiting(sender, args);
	}

	private void DrawUpdater(Updater updater)
	{
		if (updateBatch == null)
		{
			updateBatch = new SpriteBatch(base.GraphicsDevice);
		}
		updateBatch.Begin(SpriteSortMode.Immediate);
		if (updaterBackground != null)
		{
			updateBatch.Draw(updaterBackground, new Rectangle(0, 0, 800, 480), new Rectangle(0, 0, updaterBackground.Width, updaterBackground.Height), Microsoft.Xna.Framework.Color.White);
		}
		string text = string.Empty;
		float num = 0f;
		string text2 = string.Empty;
		string text3 = string.Empty;
		string text4 = string.Empty;
		switch (updater.Status)
		{
		case UpdateStatus.DownloadingManifest:
			text = Strings.Update.Checking;
			break;
		case UpdateStatus.UpdateInProgress:
			text = Strings.Update.Updating;
			num = updater.Progress / 100f;
			text2 = Strings.Update.PercentComplete.ToString((int)updater.Progress);
			text3 = Strings.Update.FilesRemaining.ToString(updater.FilesRemaining);
			text4 = Strings.Update.RemainingSize.ToString(Updater.GetHumanReadableFileSize(updater.SizeRemaining));
			break;
		case UpdateStatus.Restart:
			text = Strings.Update.Restart.ToString(Strings.Main.GameName);
			num = 100f;
			text2 = Strings.Update.PercentComplete.ToString(100);
			break;
		case UpdateStatus.UpdateCompleted:
			text = Strings.Update.Done;
			num = 100f;
			text2 = Strings.Update.PercentComplete.ToString(100);
			break;
		case UpdateStatus.Error:
			text = Strings.Update.Error;
			text2 = updater.Exception?.Message ?? "";
			num = 100f;
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case UpdateStatus.None:
			break;
		}
		if (updaterFont != null)
		{
			Vector2 vector = updaterFont.MeasureString(text);
			updateBatch.DrawString(updaterFont, text, new Vector2(400f - vector.X / 2f, 360f), Microsoft.Xna.Framework.Color.White);
		}
		if (updaterProgressBar != null)
		{
			updateBatch.Draw(updaterProgressBar, new Rectangle(100, 400, (int)(600f * num), 32), new Rectangle(0, 0, (int)((float)updaterProgressBar.Width * num), updaterProgressBar.Height), Microsoft.Xna.Framework.Color.White);
		}
		if (updaterFontSmall != null)
		{
			Vector2 vector2 = updaterFontSmall.MeasureString(text2);
			updateBatch.DrawString(updaterFontSmall, text2, new Vector2(400f - vector2.X / 2f, 405f), Microsoft.Xna.Framework.Color.White);
			updateBatch.DrawString(updaterFontSmall, text3, new Vector2(100f, 440f), Microsoft.Xna.Framework.Color.White);
			vector2 = updaterFontSmall.MeasureString(text4);
			updateBatch.DrawString(updaterFontSmall, text4, new Vector2(700f - vector2.X, 440f), Microsoft.Xna.Framework.Color.White);
		}
		updateBatch.End();
	}

	private void LoadUpdaterContent()
	{
		if (Coffre.Existe(Path.Combine(ClientConfiguration.ResourcesDirectory, "updater", "background.png")))
		{
			using Stream stream = Coffre.Ouvrir(Path.Combine(ClientConfiguration.ResourcesDirectory, "updater", "background.png"));
			updaterBackground = Texture2D.FromStream(base.GraphicsDevice, stream);
		}
		if (Coffre.Existe(Path.Combine(ClientConfiguration.ResourcesDirectory, "updater", "progressbar.png")))
		{
			using Stream stream2 = Coffre.Ouvrir(Path.Combine(ClientConfiguration.ResourcesDirectory, "updater", "progressbar.png"));
			updaterProgressBar = Texture2D.FromStream(base.GraphicsDevice, stream2);
		}
		if (Coffre.Existe(Path.Combine(ClientConfiguration.ResourcesDirectory, "updater", "font.xnb")))
		{
			updaterFont = base.Content.Load<SpriteFont>(Path.Combine(ClientConfiguration.ResourcesDirectory, "updater", "font"));
		}
		if (Coffre.Existe(Path.Combine(ClientConfiguration.ResourcesDirectory, "updater", "fontsmall.xnb")))
		{
			updaterFontSmall = base.Content.Load<SpriteFont>(Path.Combine(ClientConfiguration.ResourcesDirectory, "updater", "fontsmall"));
		}
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			base.Dispose(disposing);
		}
		catch (NullReferenceException)
		{
			throw;
		}
		if (disposing && !Context.IsDisposed)
		{
			Context.Dispose();
		}
	}
}
