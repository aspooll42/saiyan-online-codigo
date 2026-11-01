using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Intersect.Framework.Reflection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace Intersect.Framework.Logging;

public static class LoggerConfigurationExtensions
{
	public static (ILoggerFactory, Microsoft.Extensions.Logging.ILogger) CreateLoggerForIntersect(this LoggerConfiguration loggerConfiguration, Assembly forAssembly, string categoryName, LoggingLevelSwitch? parentLoggingLevelSwitch = null)
	{
		Process currentProcess = Process.GetCurrentProcess();
		DateTime startTime = currentProcess.StartTime;
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(currentProcess.MainModule?.FileName ?? forAssembly.GetName().Name);
		LoggingLevelSwitch loggingLevelSwitch = new LoggingLevelSwitch(Debugger.IsAttached ? LogEventLevel.Debug : LogEventLevel.Information);
		if (parentLoggingLevelSwitch != null)
		{
			parentLoggingLevelSwitch.MinimumLevelChanged += delegate(object? _, LoggingLevelSwitchChangedEventArgs args)
			{
				loggingLevelSwitch.MinimumLevel = args.NewLevel;
			};
			loggingLevelSwitch.MinimumLevel = parentLoggingLevelSwitch.MinimumLevel;
		}
		LoggingLevelSwitch loggingLevelSwitch2 = new LoggingLevelSwitch();
		Logger logger = loggerConfiguration.MinimumLevel.ControlledBy(loggingLevelSwitch).Enrich.FromLogContext().WriteTo.Console().WriteTo.File(Path.Combine("logs", $"{fileNameWithoutExtension}-{startTime:yyyy_MM_dd-HH_mm_ss_fff}.log"), LogEventLevel.Verbose, "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}", null, retainedFileTimeLimit: TimeSpan.FromDays(30.0), fileSizeLimitBytes: 1073741824L, levelSwitch: null, buffered: false, shared: false, flushToDiskInterval: null, rollingInterval: RollingInterval.Infinite, rollOnFileSizeLimit: true, retainedFileCountLimit: 31).WriteTo.File(Path.Combine("logs", "errors-" + fileNameWithoutExtension + ".log"), LogEventLevel.Verbose, "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}", null, levelSwitch: loggingLevelSwitch2, retainedFileTimeLimit: TimeSpan.FromDays(30.0), fileSizeLimitBytes: 1073741824L, buffered: false, shared: false, flushToDiskInterval: null, rollingInterval: RollingInterval.Infinite, rollOnFileSizeLimit: true, retainedFileCountLimit: 31).CreateLogger();
		SerilogLoggerFactory serilogLoggerFactory = new SerilogLoggerFactory(logger, dispose: true);
		Microsoft.Extensions.Logging.ILogger logger2 = serilogLoggerFactory.CreateLogger(categoryName);
		logger2.LogInformation("Starting {AssemblyName} v{AssemblyVersion}", forAssembly.GetName().Name, forAssembly.GetMetadataVersion());
		loggingLevelSwitch2.MinimumLevel = LogEventLevel.Error;
		return (serilogLoggerFactory, logger2);
	}
}
