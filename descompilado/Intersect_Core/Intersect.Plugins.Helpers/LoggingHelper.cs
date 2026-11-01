using System;
using System.Diagnostics;
using System.IO;
using Intersect.Plugins.Interfaces;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;

namespace Intersect.Plugins.Helpers;

internal sealed class LoggingHelper : ILoggingHelper
{
	private static readonly string BasePluginLogPath = Path.Combine("plugins", $"{Process.GetCurrentProcess().StartTime:yyyy_MM_dd-HH_mm_ss_fff}");

	private readonly IManifestHelper mManifest;

	public Microsoft.Extensions.Logging.ILogger Application { get; }

	public Microsoft.Extensions.Logging.ILogger Plugin { get; }

	private static Logger CreateLogger(IManifestHelper manifest, CreateLoggerOptions createLoggerOptions)
	{
		string text = (string.IsNullOrWhiteSpace(createLoggerOptions.Name) ? manifest.Key : (manifest.Key + "." + createLoggerOptions.Name));
		LoggerConfiguration loggerConfiguration = new LoggerConfiguration().Enrich.FromLogContext();
		if (createLoggerOptions.File > LogLevel.None)
		{
			string text2 = Path.Combine(BasePluginLogPath, text + ".log");
			loggerConfiguration = loggerConfiguration.WriteTo.File(text2, LevelConvert.ToSerilogLevel(createLoggerOptions.File), "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}", null, retainedFileTimeLimit: TimeSpan.FromDays(30.0), fileSizeLimitBytes: 1073741824L, levelSwitch: null, buffered: false, shared: false, flushToDiskInterval: null, rollingInterval: RollingInterval.Infinite, rollOnFileSizeLimit: true, retainedFileCountLimit: 31);
		}
		if (createLoggerOptions.Console > LogLevel.None)
		{
			loggerConfiguration = loggerConfiguration.WriteTo.Console(LevelConvert.ToSerilogLevel(createLoggerOptions.File));
		}
		return loggerConfiguration.CreateLogger();
	}

	internal LoggingHelper(Microsoft.Extensions.Logging.ILogger applicationLogger, IManifestHelper manifest, PluginReference pluginReference)
	{
		mManifest = manifest;
		Application = applicationLogger;
		Plugin = CreateLogger(new CreateLoggerOptions
		{
			Console = (Debugger.IsAttached ? LogLevel.Debug : LogLevel.None),
			File = LogLevel.Information,
			ContextName = manifest.Name,
			ContextType = pluginReference.EntryType
		});
	}

	public Microsoft.Extensions.Logging.ILogger CreateLogger(CreateLoggerOptions createLoggerOptions)
	{
		SerilogLoggerFactory serilogLoggerFactory = new SerilogLoggerFactory(CreateLogger(mManifest, createLoggerOptions));
		string contextName = createLoggerOptions.ContextName;
		if (contextName != null && !string.IsNullOrWhiteSpace(contextName))
		{
			return serilogLoggerFactory.CreateLogger(contextName);
		}
		Type contextType = createLoggerOptions.ContextType;
		if ((object)contextType != null)
		{
			return serilogLoggerFactory.CreateLogger(contextType);
		}
		return serilogLoggerFactory.CreateLogger("Plugin");
	}

	public ILogger<TContext> CreateLogger<TContext>(CreateLoggerOptions createLoggerOptions)
	{
		return new SerilogLoggerFactory(CreateLogger(mManifest, createLoggerOptions)).CreateLogger<TContext>();
	}
}
