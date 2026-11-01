using System;
using System.Linq;
using Intersect.Enums;

namespace Intersect.Client.Framework.Database;

public abstract class GameDatabase
{
	private const string CleAffichageCompatible = "AffichageCompatible";

	private const bool AffichageCompatibleParDefaut = false;

	public const float DefaultWorldZoom = 1.5f;

	private static readonly float[] AnciensZoomsParDefaut = new float[2] { 1.875f, 1.75f };

	public bool FullScreen { get; set; }

	public bool HideOthersOnWindowOpen { get; set; }

	public bool AutoToggleChatLog { get; set; }

	public bool TargetAccountDirection { get; set; }

	public int MusicVolume { get; set; }

	public int SoundVolume { get; set; }

	public bool ShowFPSCounter { get; set; }

	public bool ShowPingCounter { get; set; }

	public int TargetFps { get; set; }

	public int TargetResolution { get; set; }

	public bool EnableLighting { get; set; }

	public bool ShowBlood { get; set; } = true;

	public bool CameraEffects { get; set; } = true;

	public bool AffichageCompatible { get; set; }

	public bool ModePerformances { get; set; }

	public bool ConseilDePerformancesDonne { get; set; }

	public bool TraduireLeTchat { get; set; } = true;

	public bool PresenceDiscord { get; set; } = true;

	public bool StickyTarget { get; set; }

	public bool HeritageDirectionTenue { get; set; }

	public bool AutoTurnToTarget { get; set; }

	public bool AutoSoftRetargetOnSelfCast { get; set; }

	public bool FriendOverheadInfo { get; set; }

	public bool FriendOverheadHpBar { get; set; }

	public bool GuildMemberOverheadInfo { get; set; }

	public bool GuildMemberOverheadHpBar { get; set; }

	public bool MyOverheadInfo { get; set; }

	public bool MyOverheadHpBar { get; set; }

	public bool NpcOverheadInfo { get; set; }

	public bool NpcOverheadHpBar { get; set; }

	public bool PartyMemberOverheadInfo { get; set; }

	public bool PartyMemberOverheadHpBar { get; set; }

	public bool PlayerOverheadInfo { get; set; }

	public bool PlayerOverheadHpBar { get; set; }

	public bool ShowExperienceAsPercentage { get; set; }

	public bool ShowHealthAsPercentage { get; set; }

	public bool ShowManaAsPercentage { get; set; }

	public bool SimplifiedEscapeMenu { get; set; }

	public TypewriterBehavior TypewriterBehavior { get; set; }

	public bool TypewriterEnabled => TypewriterBehavior != TypewriterBehavior.Off;

	public float UIScale { get; set; } = 1f;

	public bool EnableScrollingWorldZoom { get; set; }

	public float WorldZoom { get; set; } = 1.5f;

	public abstract void DeletePreference(string key);

	public abstract bool HasPreference(string key);

	public abstract void SavePreference<TValue>(string key, TValue value);

	public abstract string LoadPreference(string key);

	public T LoadPreference<T>(string key, T defaultValue)
	{
		string value = LoadPreference(key);
		if (string.IsNullOrEmpty(value))
		{
			return defaultValue;
		}
		Type typeFromHandle = typeof(T);
		if (typeFromHandle.IsEnum)
		{
			try
			{
				object obj = Enum.Parse(typeFromHandle, value);
				return (T)obj;
			}
			catch
			{
				return defaultValue;
			}
		}
		return (T)Convert.ChangeType(value, typeof(T));
	}

	public virtual void LoadPreferences()
	{
		MusicVolume = LoadPreference("MusicVolume", 10);
		SoundVolume = LoadPreference("SoundVolume", 15);
		TargetResolution = LoadPreference("TargetResolution", -1);
		ShowFPSCounter = LoadPreference("ShowFPSCounter", defaultValue: false);
		ShowPingCounter = LoadPreference("ShowPingCounter", defaultValue: false);
		TargetFps = LoadPreference("TargetFps", 0);
		FullScreen = LoadPreference("FullScreen", defaultValue: false);
		EnableLighting = LoadPreference("EnableLighting", defaultValue: true);
		ShowBlood = LoadPreference("ShowBlood", defaultValue: true);
		CameraEffects = LoadPreference("CameraEffects", defaultValue: true);
		AffichageCompatible = LoadPreference("AffichageCompatible", defaultValue: false);
		ModePerformances = LoadPreference("ModePerformances", defaultValue: false);
		ConseilDePerformancesDonne = LoadPreference("ConseilDePerformancesDonne", defaultValue: false);
		TraduireLeTchat = LoadPreference("TraduireLeTchat", defaultValue: true);
		PresenceDiscord = LoadPreference("PresenceDiscord", defaultValue: true);
		HideOthersOnWindowOpen = LoadPreference("HideOthersOnWindowOpen", defaultValue: true);
		AutoToggleChatLog = LoadPreference("AutoToggleChatLog", defaultValue: false);
		TargetAccountDirection = LoadPreference("TargetAccountDirection", defaultValue: false);
		StickyTarget = LoadPreference("StickyTarget", defaultValue: false);
		HeritageDirectionTenue = LoadPreference("HeritageDirectionTenue", defaultValue: false);
		AutoTurnToTarget = LoadPreference("AutoTurnToTarget", defaultValue: false);
		AutoSoftRetargetOnSelfCast = LoadPreference("AutoSoftRetargetOnSelfCast", defaultValue: false);
		FriendOverheadInfo = LoadPreference("FriendOverheadInfo", defaultValue: false);
		FriendOverheadHpBar = LoadPreference("FriendOverheadHpBar", defaultValue: false);
		GuildMemberOverheadInfo = LoadPreference("GuildMemberOverheadInfo", defaultValue: false);
		GuildMemberOverheadHpBar = LoadPreference("GuildMemberOverheadHpBar", defaultValue: false);
		MyOverheadInfo = LoadPreference("MyOverheadInfo", defaultValue: false);
		MyOverheadHpBar = LoadPreference("MyOverheadHpBar", defaultValue: false);
		NpcOverheadInfo = LoadPreference("NpcOverheadInfo", defaultValue: false);
		NpcOverheadHpBar = LoadPreference("NpcOverheadHpBar", defaultValue: false);
		PartyMemberOverheadInfo = LoadPreference("PartyMemberOverheadInfo", defaultValue: false);
		PartyMemberOverheadHpBar = LoadPreference("PartyMemberOverheadHpBar", defaultValue: false);
		PlayerOverheadInfo = LoadPreference("PlayerOverheadInfo", defaultValue: false);
		PlayerOverheadHpBar = LoadPreference("PlayerOverheadHpBar", defaultValue: false);
		ShowExperienceAsPercentage = LoadPreference("ShowExperienceAsPercentage", defaultValue: true);
		ShowHealthAsPercentage = LoadPreference("ShowHealthAsPercentage", defaultValue: false);
		ShowManaAsPercentage = LoadPreference("ShowManaAsPercentage", defaultValue: false);
		SimplifiedEscapeMenu = LoadPreference("SimplifiedEscapeMenu", defaultValue: false);
		TypewriterBehavior = LoadPreference("TypewriterBehavior", TypewriterBehavior.Word);
		UIScale = LoadPreference("UIScale", 1f);
		EnableScrollingWorldZoom = LoadPreference("EnableScrollingWorldZoom", defaultValue: false);
		WorldZoom = LoadPreference("WorldZoom", 1.5f);
		if (AnciensZoomsParDefaut.Any((float ancien) => MathF.Abs(WorldZoom - ancien) < 0.001f))
		{
			WorldZoom = 1.5f;
		}
	}

	public virtual void SavePreferences()
	{
		SavePreference("MusicVolume", MusicVolume);
		SavePreference("SoundVolume", SoundVolume);
		SavePreference("TargetResolution", TargetResolution);
		SavePreference("TargetFps", TargetFps);
		SavePreference("FullScreen", FullScreen);
		SavePreference("ShowFPSCounter", ShowFPSCounter);
		SavePreference("ShowPingCounter", ShowPingCounter);
		SavePreference("EnableLighting", EnableLighting);
		SavePreference("ShowBlood", ShowBlood);
		SavePreference("CameraEffects", CameraEffects);
		SavePreference("AffichageCompatible", AffichageCompatible);
		SavePreference("ModePerformances", ModePerformances);
		SavePreference("ConseilDePerformancesDonne", ConseilDePerformancesDonne);
		SavePreference("TraduireLeTchat", TraduireLeTchat);
		SavePreference("PresenceDiscord", PresenceDiscord);
		SavePreference("HideOthersOnWindowOpen", HideOthersOnWindowOpen);
		SavePreference("AutoToggleChatLog", AutoToggleChatLog);
		SavePreference("TargetAccountDirection", TargetAccountDirection);
		SavePreference("StickyTarget", StickyTarget);
		if (HeritageDirectionTenue || HasPreference("HeritageDirectionTenue"))
		{
			SavePreference("HeritageDirectionTenue", HeritageDirectionTenue);
		}
		SavePreference("AutoTurnToTarget", AutoTurnToTarget);
		SavePreference("AutoSoftRetargetOnSelfCast", AutoSoftRetargetOnSelfCast);
		SavePreference("FriendOverheadInfo", FriendOverheadInfo);
		SavePreference("FriendOverheadHpBar", FriendOverheadHpBar);
		SavePreference("GuildMemberOverheadInfo", GuildMemberOverheadInfo);
		SavePreference("GuildMemberOverheadHpBar", GuildMemberOverheadHpBar);
		SavePreference("MyOverheadInfo", MyOverheadInfo);
		SavePreference("MyOverheadHpBar", MyOverheadHpBar);
		SavePreference("NpcOverheadInfo", NpcOverheadInfo);
		SavePreference("NpcOverheadHpBar", NpcOverheadHpBar);
		SavePreference("PartyMemberOverheadInfo", PartyMemberOverheadInfo);
		SavePreference("PartyMemberOverheadHpBar", PartyMemberOverheadHpBar);
		SavePreference("PlayerOverheadInfo", PlayerOverheadInfo);
		SavePreference("PlayerOverheadHpBar", PlayerOverheadHpBar);
		SavePreference("ShowExperienceAsPercentage", ShowExperienceAsPercentage);
		SavePreference("ShowHealthAsPercentage", ShowHealthAsPercentage);
		SavePreference("ShowManaAsPercentage", ShowManaAsPercentage);
		SavePreference("SimplifiedEscapeMenu", SimplifiedEscapeMenu);
		SavePreference("TypewriterBehavior", TypewriterBehavior);
		SavePreference("UIScale", UIScale);
		SavePreference("EnableScrollingWorldZoom", EnableScrollingWorldZoom);
		SavePreference("WorldZoom", WorldZoom);
	}

	public abstract bool LoadConfig();
}
