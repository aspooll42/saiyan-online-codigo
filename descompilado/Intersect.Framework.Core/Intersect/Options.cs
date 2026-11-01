using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Intersect.Config;
using Intersect.Config.Guilds;
using Intersect.Core;
using Intersect.Framework.Annotations;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Intersect;

public record Options
{
	public static string ResourcesDirectory { get; set; } = "resources";

	public static Options Instance { get; private set; }

	public static Options? PendingChanges { get; private set; }

	public static bool IsLoaded => Instance != null;

	[Ignore]
	[JsonIgnore]
	public string OptionsData { get; private set; } = string.Empty;

	[Ignore]
	public bool SmtpValid { get; private set; }

	[Category("CategoryCore")]
	[JsonProperty(Order = -100)]
	[RequiresRestart]
	public string GameName { get; set; } = "Intersect";

	[Category("CategoryCore")]
	[JsonProperty(Order = -100)]
	[RequiresRestart]
	public ushort ServerPort { get; set; } = 5400;

	[Category("CategoryCore")]
	[JsonProperty(Order = -99)]
	[RequiresRestart]
	public ushort PortNavigateur { get; set; }

	[Category("CategoryGameAccess")]
	[JsonProperty(Order = -99)]
	public bool AdminOnly { get; set; }

	[Category("CategoryGameAccess")]
	[JsonProperty(Order = -99)]
	public bool BlockClientRegistrations { get; set; }

	[Category("CategoryGameAccess")]
	[JsonProperty(Order = -99)]
	public int MaxClientConnections { get; set; } = 100;

	[Category("CategoryGameAccess")]
	[JsonProperty(Order = -99)]
	public int MaximumLoggedInUsers { get; set; } = 50;

	[Category("CategoryNetworkVisibility")]
	[JsonProperty(Order = -91)]
	[RequiresRestart]
	public bool UPnP { get; set; } = true;

	[Category("CategoryNetworkVisibility")]
	[JsonProperty(Order = -91)]
	[RequiresRestart]
	public bool OpenPortChecker { get; set; } = true;

	[Category("CategoryNetworkVisibility")]
	[JsonProperty(Order = -91, NullValueHandling = NullValueHandling.Include)]
	[RequiresRestart]
	public string? PortCheckerUrl { get; set; }

	[Category("CategoryLoggingAndMetrics")]
	[JsonProperty(Order = -80)]
	public LoggingOptions Logging { get; set; } = new LoggingOptions();

	[Category("CategoryLoggingAndMetrics")]
	[JsonProperty(Order = -80)]
	public MetricsOptions Metrics { get; set; } = new MetricsOptions();

	[Category("CategoryDatabase")]
	[JsonProperty(Order = -70)]
	[RequiresRestart]
	public DatabaseOptions GameDatabase { get; set; } = new DatabaseOptions();

	[Category("CategoryDatabase")]
	[JsonProperty(Order = -70)]
	[RequiresRestart]
	public DatabaseOptions LoggingDatabase { get; set; } = new DatabaseOptions();

	[Category("CategoryDatabase")]
	[JsonProperty(Order = -70)]
	[RequiresRestart]
	public DatabaseOptions PlayerDatabase { get; set; } = new DatabaseOptions();

	[Category("CategorySecurity")]
	[JsonProperty(Order = -60)]
	[RequiresRestart]
	public SecurityOptions Security { get; set; } = new SecurityOptions();

	[Category("CategorySecurity")]
	[JsonProperty(Order = -60)]
	[RequiresRestart]
	public SmtpSettings SmtpSettings { get; set; } = new SmtpSettings();

	[RequiresRestart]
	public List<string> AnimatedSprites { get; set; } = new List<string>();

	[RequiresRestart]
	public PacketOptions Packets { get; set; } = new PacketOptions();

	public ChatOptions Chat { get; set; } = new ChatOptions();

	[RequiresRestart]
	public CombatOptions Combat { get; set; } = new CombatOptions();

	[RequiresRestart]
	public EquipmentOptions Equipment { get; set; } = new EquipmentOptions();

	[RequiresRestart]
	public int EventWatchdogKillThreshold { get; set; } = 5000;

	public PassabilityOptions Passability { get; set; } = new PassabilityOptions();

	public ushort ValidPasswordResetTimeMinutes { get; set; } = 30;

	public MapOptions Map { get; set; } = new MapOptions();

	public PlayerOptions Player { get; set; } = new PlayerOptions();

	public PartyOptions Party { get; set; } = new PartyOptions();

	public LootOptions Loot { get; set; } = new LootOptions();

	public ProcessingOptions Processing { get; set; } = new ProcessingOptions();

	public SpriteOptions Sprites { get; set; } = new SpriteOptions();

	public NpcOptions Npc { get; set; } = new NpcOptions();

	public QuestOptions Quest { get; set; } = new QuestOptions();

	public GuildOptions Guild { get; set; } = new GuildOptions();

	public BankOptions Bank { get; set; } = new BankOptions();

	public InstancingOptions Instancing { get; set; } = new InstancingOptions();

	public ItemOptions Items { get; set; } = new ItemOptions();

	public MarketOptions Market { get; set; } = new MarketOptions();

	public TransformationOptions Transformation { get; set; } = new TransformationOptions();

	public KarmaOptions Karma { get; set; } = new KarmaOptions();

	public FlightOptions Flight { get; set; } = new FlightOptions();

	public BeamOptions Beam { get; set; } = new BeamOptions();

	public CasseOptions Casse { get; set; } = new CasseOptions();

	public RechercheOptions Recherche { get; set; } = new RechercheOptions();

	public MaitresOptions Maitres { get; set; } = new MaitresOptions();

	private static readonly JsonSerializerSettings PrivateIndentedSerializerSettings = new JsonSerializerSettings
	{
		ContractResolver = new OptionsContractResolver(serializePrivateProperties: true, serializePublicProperties: false),
		Formatting = Formatting.Indented
	};

	private static readonly JsonSerializerSettings PrivateSerializerSettings = new JsonSerializerSettings
	{
		ContractResolver = new OptionsContractResolver(serializePrivateProperties: true, serializePublicProperties: false)
	};

	private static readonly JsonSerializerSettings PublicSerializerSettings = new JsonSerializerSettings
	{
		ContractResolver = new OptionsContractResolver(serializePrivateProperties: false, serializePublicProperties: true)
	};

	public const string DefaultGameName = "Intersect";

	public const int DefaultServerPort = 5400;

	public const string CategoryCore = "CategoryCore";

	public const string CategoryDatabase = "CategoryDatabase";

	public const string CategoryGameAccess = "CategoryGameAccess";

	public const string CategoryLoggingAndMetrics = "CategoryLoggingAndMetrics";

	public const string CategoryNetworkVisibility = "CategoryNetworkVisibility";

	public const string CategorySecurity = "CategorySecurity";

	public static event OptionsLoadedEventHandler? OptionsLoaded;

	public void FixAnimatedSprites()
	{
		for (int i = 0; i < AnimatedSprites.Count; i++)
		{
			AnimatedSprites[i] = AnimatedSprites[i].ToLower();
		}
	}

	public static bool LoadFromDisk()
	{
		Options options = EnsureCreated();
		string path = Path.Combine(ResourcesDirectory, "config.json");
		if (!Directory.Exists(ResourcesDirectory))
		{
			Directory.CreateDirectory(ResourcesDirectory);
		}
		else if (File.Exists(path))
		{
			string value = File.ReadAllText(path);
			options = (Instance = JsonConvert.DeserializeObject<Options>(value, PrivateSerializerSettings) ?? options);
		}
		options.SmtpValid = options.SmtpSettings.IsValid();
		options.FixAnimatedSprites();
		SaveToDisk();
		return true;
	}

	internal static Options EnsureCreated()
	{
		return Instance = new Options();
	}

	public static void SaveToDisk()
	{
		Options instance = Instance;
		if ((object)instance == null)
		{
			ApplicationContext.Context.Value?.Logger.LogError("Tried to save null instance to disk");
			return;
		}
		if (!Directory.Exists(ResourcesDirectory))
		{
			Directory.CreateDirectory(ResourcesDirectory);
		}
		string text = Path.Combine(ResourcesDirectory, "config.json");
		try
		{
			string contents = JsonConvert.SerializeObject(instance, PrivateIndentedSerializerSettings);
			File.WriteAllText(text, contents);
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, "Failed to save options to {OptionsPath}", text);
		}
		instance.OptionsData = JsonConvert.SerializeObject(instance, PublicSerializerSettings);
	}

	public static void LoadFromServer(string data)
	{
		try
		{
			Options options = (Instance = JsonConvert.DeserializeObject<Options>(data, PublicSerializerSettings));
			Options.OptionsLoaded?.Invoke(options);
		}
		catch (Exception exception)
		{
			ApplicationContext.CurrentContext.Logger.LogError(exception, "Failed to load options from server");
			throw;
		}
	}

	public Options DeepClone()
	{
		return JsonConvert.DeserializeObject<Options>(JsonConvert.SerializeObject(this, PrivateSerializerSettings));
	}

	[CompilerGenerated]
	protected Options(Options original)
	{
		OptionsData = original.OptionsData;
		SmtpValid = original.SmtpValid;
		GameName = original.GameName;
		ServerPort = original.ServerPort;
		PortNavigateur = original.PortNavigateur;
		AdminOnly = original.AdminOnly;
		BlockClientRegistrations = original.BlockClientRegistrations;
		MaxClientConnections = original.MaxClientConnections;
		MaximumLoggedInUsers = original.MaximumLoggedInUsers;
		UPnP = original.UPnP;
		OpenPortChecker = original.OpenPortChecker;
		PortCheckerUrl = original.PortCheckerUrl;
		Logging = original.Logging;
		Metrics = original.Metrics;
		GameDatabase = original.GameDatabase;
		LoggingDatabase = original.LoggingDatabase;
		PlayerDatabase = original.PlayerDatabase;
		Security = original.Security;
		SmtpSettings = original.SmtpSettings;
		AnimatedSprites = original.AnimatedSprites;
		Packets = original.Packets;
		Chat = original.Chat;
		Combat = original.Combat;
		Equipment = original.Equipment;
		EventWatchdogKillThreshold = original.EventWatchdogKillThreshold;
		Passability = original.Passability;
		ValidPasswordResetTimeMinutes = original.ValidPasswordResetTimeMinutes;
		Map = original.Map;
		Player = original.Player;
		Party = original.Party;
		Loot = original.Loot;
		Processing = original.Processing;
		Sprites = original.Sprites;
		Npc = original.Npc;
		Quest = original.Quest;
		Guild = original.Guild;
		Bank = original.Bank;
		Instancing = original.Instancing;
		Items = original.Items;
		Market = original.Market;
		Transformation = original.Transformation;
		Karma = original.Karma;
		Flight = original.Flight;
		Beam = original.Beam;
		Casse = original.Casse;
		Recherche = original.Recherche;
		Maitres = original.Maitres;
	}

	public Options()
	{
	}
}
