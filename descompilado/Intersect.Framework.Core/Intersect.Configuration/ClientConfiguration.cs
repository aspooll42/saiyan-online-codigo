using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Intersect.Enums;
using Intersect.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Intersect.Configuration;

public sealed class ClientConfiguration : IConfiguration<ClientConfiguration>
{
	public const string DefaultHost = "localhost";

	public const int DefaultPort = 5400;

	public const string DefaultFont = "sourcesansproblack";

	public const string DefaultUIFont = "sourcesanspro,10";

	public const int DefaultChatLines = 100;

	public const float DefaultFadeDurationMs = 3000f;

	public const DisplayMode DefaultMenuBackgroundDisplayMode = DisplayMode.Default;

	public const long DefaultMenuBackgroundFrameInterval = 50L;

	public const string DefaultMenuMusic = "RPG-Theme_v001_Looping.ogg";

	public const bool DefaultTypewriterEnabled = true;

	public const long DefaultTypewriterFullStopDelay = 400L;

	public const long DefaultTypewriterPartDelay = 6L;

	public const long DefaultTypewriterPauseDelay = 80L;

	public const long DefaultTypewriterResponseDelay = 600L;

	public const int DefaultTypewriterSoundFrequency = 5;

	private const bool DefaultDimmedEventWindowBackground = true;

	public static string ResourcesDirectory { get; set; } = "resources";

	public static string DefaultPath => Path.Combine(ResourcesDirectory, "config.json");

	public static List<DisplayDirection> DefaultEntityBarDirections => (from _ in Enumerable.Range(0, 1 + Enum.GetValues<Vital>().Length)
		select DisplayDirection.StartToEnd).ToList();

	public static HashSet<string> DefaultTypewriterFullStops => new HashSet<string> { ".", "!", "?", ":" };

	public static HashSet<string> DefaultTypewriterPauses => new HashSet<string> { ",", ";", "-" };

	public static List<string> DefaultTypewriterSounds
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "octave-beep-tapped.wav";
			num2++;
			return list;
		}
	}

	public static ClientConfiguration Instance { get; } = new ClientConfiguration();

	public string Host { get; set; } = "localhost";

	public ushort Port { get; set; } = 5400;

	public string GameFont { get; set; } = "sourcesansproblack";

	public string EntityNameFont { get; set; } = "sourcesansproblack";

	public string ChatBubbleFont { get; set; } = "sourcesansproblack";

	public string ActionMsgFont { get; set; } = "sourcesansproblack";

	public string UIFont { get; set; } = "sourcesanspro,10";

	public int ChatLines { get; set; } = 100;

	public string MenuMusic { get; set; } = "RPG-Theme_v001_Looping.ogg";

	public List<string> MenuBackground { get; set; }

	public DisplayMode MenuBackgroundDisplayMode { get; set; }

	public long MenuBackgroundFrameInterval { get; set; }

	public List<string> IntroImages { get; set; }

	public string? UpdateUrl { get; set; }

	public string? MouseCursor { get; set; }

	public int MusicFadeTimer { get; set; }

	public float FadeDurationMs { get; set; }

	public bool EnableContextMenus { get; set; }

	public bool EnableZebraStripedRows { get; set; }

	public string UiSkin { get; set; }

	[JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
	public List<DisplayDirection> EntityBarDirections { get; set; }

	public bool TypewriterEnabled { get; set; }

	public HashSet<string> TypewriterFullStops { get; set; }

	public long TypewriterFullStopDelay { get; set; }

	public long TypewriterPartDelay { get; set; }

	public HashSet<string> TypewriterPauses { get; set; }

	public long TypewriterPauseDelay { get; set; }

	public long TypewriterResponseDelay { get; set; }

	public int TypewriterSoundFrequency { get; set; }

	public List<string> TypewriterSounds { get; set; }

	public bool DimmedEventWindowBackground { get; set; }

	[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
	[JsonConverter(typeof(StringEnumConverter))]
	[DefaultValue(LogLevel.Information)]
	public LogLevel LogLevel { get; set; }

	public ClientConfiguration()
	{
		int num = 1;
		List<string> list = new List<string>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<string> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = "background.png";
		num2++;
		MenuBackground = list;
		MenuBackgroundFrameInterval = 50L;
		IntroImages = new List<string>();
		MusicFadeTimer = 1500;
		FadeDurationMs = 3000f;
		EnableContextMenus = true;
		EnableZebraStripedRows = true;
		UiSkin = "IntersectSkin";
		EntityBarDirections = (from _ in Enumerable.Range(0, 1 + Enum.GetValues<Vital>().Length)
			select DisplayDirection.StartToEnd).ToList();
		TypewriterEnabled = true;
		TypewriterFullStops = DefaultTypewriterFullStops;
		TypewriterFullStopDelay = 400L;
		TypewriterPartDelay = 6L;
		TypewriterPauses = DefaultTypewriterPauses;
		TypewriterPauseDelay = 80L;
		TypewriterResponseDelay = 600L;
		TypewriterSoundFrequency = 5;
		TypewriterSounds = DefaultTypewriterSounds;
		DimmedEventWindowBackground = true;
		LogLevel = LogLevel.Information;
		ConfigurationHelper.CacheName = $"{Host}.{Port}";
	}

	public ClientConfiguration Load(string? filePath = null, bool failQuietly = false)
	{
		return ConfigurationHelper.Load(this, filePath ?? DefaultPath, failQuietly);
	}

	public ClientConfiguration Save(string? filePath = null, bool failQuietly = false)
	{
		return ConfigurationHelper.Save(this, filePath ?? DefaultPath, failQuietly);
	}

	public static ClientConfiguration LoadAndSave(string? filePath = null)
	{
		return ConfigurationHelper.LoadSafely(Instance, filePath ?? DefaultPath);
	}

	public void Validate()
	{
		ChatLines = MathHelper.Clamp(ChatLines, 10, 500);
		EntityBarDirections = DefaultEntityBarDirections.Select((DisplayDirection direction, int index) => (EntityBarDirections.Count <= index) ? direction : EntityBarDirections[index]).ToList();
		GameFont = (string.IsNullOrWhiteSpace(GameFont) ? "sourcesansproblack" : GameFont.Trim());
		Host = (string.IsNullOrWhiteSpace(Host) ? "localhost" : Host.Trim());
		List<string> list = new List<string>();
		list.AddRange(IntroImages?.Distinct() ?? new List<string>());
		IntroImages = list;
		List<string> list2 = new List<string>();
		list2.AddRange(MenuBackground?.Distinct() ?? new List<string> { "background.png" });
		MenuBackground = list2;
		Port = Math.Min(Math.Max(Port, (ushort)1), ushort.MaxValue);
		if (TypewriterFullStops == null)
		{
			HashSet<string> hashSet = (TypewriterFullStops = new HashSet<string>());
		}
		if (TypewriterPauses == null)
		{
			HashSet<string> hashSet = (TypewriterPauses = new HashSet<string>());
		}
		List<string> list3 = new List<string>();
		list3.AddRange(TypewriterSounds?.Distinct() ?? new List<string>());
		TypewriterSounds = list3;
		UIFont = (string.IsNullOrWhiteSpace(UIFont) ? "sourcesanspro,10" : UIFont.Trim());
	}

	[OnDeserializing]
	internal void OnDeserializing(StreamingContext context)
	{
		MenuBackground?.Clear();
		IntroImages?.Clear();
	}

	[OnDeserialized]
	internal void OnDeserialized(StreamingContext context)
	{
		Validate();
	}
}
