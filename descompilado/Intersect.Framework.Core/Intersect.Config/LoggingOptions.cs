using Intersect.Framework.Core.Serialization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Core;
using Serilog.Extensions.Logging;

namespace Intersect.Config;

public class LoggingOptions
{
	private const LogLevel DefaultLogLevel = LogLevel.Information;

	public static readonly LoggingLevelSwitch LoggingLevelSwitch = new LoggingLevelSwitch(LevelConvert.ToSerilogLevel(LogLevel.Information));

	private LogLevel _level = LogLevel.Information;

	private bool _showSensitiveData;

	public bool ShowSensitiveData
	{
		get
		{
			return _showSensitiveData;
		}
		set
		{
			_showSensitiveData = value;
		}
	}

	[JsonConverter(typeof(SafeStringEnumConverter))]
	public LogLevel Level
	{
		get
		{
			return _level;
		}
		set
		{
			if (value != _level)
			{
				_level = value;
				LoggingLevelSwitch.MinimumLevel = LevelConvert.ToSerilogLevel(_level);
			}
		}
	}

	public bool Chat { get; set; } = true;

	public bool GuildActivity { get; set; } = true;

	public bool Trade { get; set; } = true;

	public bool UserActivity { get; set; } = true;
}
