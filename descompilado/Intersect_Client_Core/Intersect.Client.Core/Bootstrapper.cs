using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;
using Intersect.Client.Plugins;
using Intersect.Configuration;
using Intersect.Core;
using Intersect.Factories;
using Intersect.Framework.Logging;
using Intersect.Framework.SystemInformation;
using Intersect.Network;
using Intersect.Plugins;
using Intersect.Plugins.Contexts;
using Intersect.Plugins.Helpers;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace Intersect.Client.Core;

internal static class Bootstrapper
{
	public static void Start(Assembly entryAssembly, params string[] args)
	{
		Parser parser = new Parser(delegate(ParserSettings parserSettings)
		{
			if (parserSettings == null)
			{
				throw new ArgumentNullException("parserSettings", "If this is null the CommandLineParser dependency is likely broken.");
			}
			parserSettings.AutoHelp = true;
			parserSettings.AutoVersion = true;
			parserSettings.IgnoreUnknownArguments = true;
		});
		ClientCommandLineOptions clientCommandLineOptions = parser.ParseArguments<ClientCommandLineOptions>(args).MapResult<ClientCommandLineOptions, ClientCommandLineOptions>(HandleParsedArguments, HandleParserErrors);
		LoggingLevelSwitch loggingLevelSwitch = new LoggingLevelSwitch(Debugger.IsAttached ? LogEventLevel.Debug : LogEventLevel.Information);
		(ILoggerFactory, Microsoft.Extensions.Logging.ILogger) tuple = new LoggerConfiguration().CreateLoggerForIntersect(entryAssembly, "Client", loggingLevelSwitch);
		ILoggerFactory item = tuple.Item1;
		Microsoft.Extensions.Logging.ILogger item2 = tuple.Item2;
		PlatformStatistics.Logger = item.CreateLogger<PlatformStatistics>();
		PacketTypeRegistry packetTypeRegistry = new PacketTypeRegistry(item2, typeof(IntersectPacket).Assembly);
		if (!packetTypeRegistry.TryRegisterBuiltIn())
		{
			item2.LogError("Failed to register built-in packets.");
			return;
		}
		PacketHandlerRegistry packetHandlerRegistry = new PacketHandlerRegistry(packetTypeRegistry, item2);
		PacketHelper packetHelper = new PacketHelper(packetTypeRegistry, packetHandlerRegistry);
		FactoryRegistry<IPluginBootstrapContext>.RegisterFactory(PluginBootstrapContext.CreateFactory(typeof(IClientPluginContext), args, parser, packetHelper));
		if (!string.IsNullOrWhiteSpace(clientCommandLineOptions.WorkingDirectory))
		{
			string text = clientCommandLineOptions.WorkingDirectory.Trim();
			string fullPath = Path.GetFullPath(text);
			if (Directory.Exists(fullPath))
			{
				Environment.CurrentDirectory = fullPath;
			}
			else
			{
				ApplicationContext.Context.Value?.Logger.LogWarning("Failed to set working directory to '{Path}', path does not exist: {ResolvedPath}", text, fullPath);
			}
		}
		ClientConfiguration clientConfiguration = ClientConfiguration.LoadAndSave();
		loggingLevelSwitch.MinimumLevel = LevelConvert.ToSerilogLevel(clientConfiguration.LogLevel);
		string server = clientCommandLineOptions.Server;
		if (server != null)
		{
			string[] source = server.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			string host = source.First();
			string text2 = source.Skip(1).FirstOrDefault();
			if (string.IsNullOrEmpty(text2))
			{
				text2 = "5400";
			}
			ushort port = ushort.Parse(text2);
			clientConfiguration.Host = host;
			clientConfiguration.Port = port;
		}
		if (clientCommandLineOptions.DelaiEssai != 0)
		{
			HeritageDelaiDEssai.Demander(clientCommandLineOptions.DelaiEssai, clientConfiguration.Host, item2);
		}
		clientCommandLineOptions = clientCommandLineOptions with
		{
			Server = $"{clientConfiguration.Host}:{clientConfiguration.Port}"
		};
		ClientContext clientContext = new ClientContext(entryAssembly, clientCommandLineOptions, clientConfiguration, item2, packetHelper);
		clientContext.Start();
	}

	private static ClientCommandLineOptions HandleParsedArguments(ClientCommandLineOptions clientCommandLineOptions)
	{
		return clientCommandLineOptions;
	}

	private static ClientCommandLineOptions HandleParserErrors(IEnumerable<Error> errors)
	{
		List<Error> source = errors.ToList();
		bool flag = source.Any((Error error) => error.StopsProcessing);
		string text = string.Join(", ", from error in source.ToList()
			select error.ToString());
		ArgumentException exception = new ArgumentException("Error parsing command line arguments, received the following errors: " + text);
		if (flag)
		{
			ApplicationContext.Context.Value?.Logger.LogCritical(exception, "Critical error during command line argument parsing");
		}
		else
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Error occurred during command line argument parsing");
		}
		return null;
	}
}
