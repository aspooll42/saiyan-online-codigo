#if !ANDROID
using System;
using System.Linq;
using System.Reflection;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.MonoGame.NativeInterop;
using Intersect.Core;
using Microsoft.Extensions.Logging;
using Steamworks;

namespace Intersect.Client.ThirdParty;

public static class Steam
{
	[AttributeUsage(AttributeTargets.Assembly)]
	public sealed class SupportedAttribute : Attribute
	{
		public static bool IsPresent(Assembly? assembly = null)
		{
			if (!(assembly == null))
			{
				return assembly.GetCustomAttribute<SupportedAttribute>() != null;
			}
			return AppDomain.CurrentDomain.GetAssemblies().Any(IsPresent);
		}
	}

	private delegate bool SteamAPI_InitSafe_d();

	private delegate bool CSteamAPIContext_Init_d();

	private delegate void CallbackDispatcher_Initialize_d();

	private static readonly FunctionLoader Loader;

	private static readonly SteamAPI_InitSafe_d SteamAPI_InitSafe;

	private static readonly CSteamAPIContext_Init_d CSteamAPIContext_Init;

	private static readonly CallbackDispatcher_Initialize_d CallbackDispatcher_Initialize;

	private static Callback<GamepadTextInputDismissed_t>? _callbackGamepadTextInputDismissed;

	private static Callback<FloatingGamepadTextInputDismissed_t>? _callbackFloatingGamepadTextInputDismissed;

	public static bool Initialized { get; }

	public static string Language { get; }

	public static string[] Languages { get; }

	public static bool SteamDeck { get; }

	static Steam()
	{
		Loader = new FunctionLoader(KnownLibrary.steam_api);
		SteamAPI_InitSafe = Loader.LoadFunction<SteamAPI_InitSafe_d>("SteamAPI_InitSafe");
		CSteamAPIContext_Init = typeof(SteamAPI).Assembly.GetType("Steamworks.CSteamAPIContext")?.GetMethod("Init", BindingFlags.Static | BindingFlags.NonPublic)?.CreateDelegate<CSteamAPIContext_Init_d>() ?? throw new MissingMethodException("Steamworks.CSteamAPIContext", "Init");
		CallbackDispatcher_Initialize = typeof(CallbackDispatcher).GetMethod("Initialize", BindingFlags.Static | BindingFlags.NonPublic)?.CreateDelegate<CallbackDispatcher_Initialize_d>() ?? throw new MissingMethodException(typeof(CallbackDispatcher).FullName ?? "CallbackDispatcher", "Initialize");
		Language = string.Empty;
		Languages = Array.Empty<string>();
		if (!SupportedAttribute.IsPresent())
		{
			return;
		}
		try
		{
			bool flag = SteamAPI_InitSafe();
			flag &= CSteamAPIContext_Init();
			if (flag)
			{
				CallbackDispatcher_Initialize();
			}
			Language = SteamApps.GetCurrentGameLanguage();
			Languages = SteamApps.GetAvailableGameLanguages().Split(',');
			SteamDeck = SteamUtils.IsSteamRunningOnSteamDeck();
			Initialized = flag;
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, "Error during Steam integration initialization");
			Initialized = false;
		}
	}

	public static bool ShowKeyboard(Action<string?> inputHandler, string description, string? existingInput, bool password, uint maxLength = 1024u, Rectangle? inputBounds = null)
	{
		if (!Initialized)
		{
			return true;
		}
		if (inputBounds.HasValue)
		{
			Rectangle value = inputBounds.Value;
			if (!SteamUtils.ShowFloatingGamepadTextInput(EFloatingGamepadTextInputMode.k_EFloatingGamepadTextInputModeModeSingleLine, value.X, value.Y, value.Width, value.Height))
			{
				return false;
			}
			_callbackFloatingGamepadTextInputDismissed = Callback<FloatingGamepadTextInputDismissed_t>.Create(delegate
			{
				_callbackFloatingGamepadTextInputDismissed = null;
				uint enteredGamepadTextLength = SteamUtils.GetEnteredGamepadTextLength();
				if (enteredGamepadTextLength != 0 && SteamUtils.GetEnteredGamepadTextInput(out var pchText, enteredGamepadTextLength))
				{
					inputHandler(pchText);
				}
				else
				{
					inputHandler(null);
				}
			});
		}
		else
		{
			if (!SteamUtils.ShowGamepadTextInput(password ? EGamepadTextInputMode.k_EGamepadTextInputModePassword : EGamepadTextInputMode.k_EGamepadTextInputModeNormal, EGamepadTextInputLineMode.k_EGamepadTextInputLineModeSingleLine, description, Math.Max(1u, maxLength), existingInput ?? string.Empty))
			{
				return false;
			}
			_callbackGamepadTextInputDismissed = Callback<GamepadTextInputDismissed_t>.Create(delegate(GamepadTextInputDismissed_t data)
			{
				_callbackGamepadTextInputDismissed = null;
				if (data.m_bSubmitted)
				{
					uint enteredGamepadTextLength = SteamUtils.GetEnteredGamepadTextLength();
					if (SteamUtils.GetEnteredGamepadTextInput(out var pchText, enteredGamepadTextLength))
					{
						inputHandler(pchText);
						return;
					}
				}
				inputHandler(null);
			});
		}
		return true;
	}

	public static void PumpEvents()
	{
		if (Initialized)
		{
			SteamAPI.RunCallbacks();
		}
	}
}
#else
using System;
using System.Reflection;
using Intersect.Client.Framework.GenericClasses;

namespace Intersect.Client.ThirdParty;

public static class Steam
{
	[AttributeUsage(AttributeTargets.Assembly)]
	public sealed class SupportedAttribute : Attribute
	{
		public static bool IsPresent(Assembly? assembly = null) => false;
	}

	public static bool Initialized => false;

	public static string Language => string.Empty;

	public static string[] Languages => Array.Empty<string>();

	public static bool SteamDeck => false;

	public static bool ShowKeyboard(Action<string?> inputHandler, string description, string? existingInput, bool password, uint maxLength = 1024u, Rectangle? inputBounds = null) => false;

	public static void PumpEvents()
	{
	}
}
#endif
