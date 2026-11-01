using System.ComponentModel;
using Intersect.Framework.Annotations;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Intersect.Config;

[RequiresRestart]
public class DatabaseOptions
{
	private const bool DefaultKillServerOnConcurrencyException = false;

	[JsonConverter(typeof(StringEnumConverter))]
	public DatabaseType Type { get; set; } = DatabaseType.SQLite;

	public string? Server { get; set; }

	public ushort Port { get; set; } = 3306;

	public string? Database { get; set; }

	public string? Username { get; set; }

	[Password]
	[PasswordPropertyText(true)]
	public string? Password { get; set; }

	[JsonConverter(typeof(StringEnumConverter))]
	[JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Populate)]
	[DefaultValue(LogLevel.Error)]
	public LogLevel LogLevel { get; set; } = LogLevel.Error;

	public bool KillServerOnConcurrencyException { get; set; }
}
