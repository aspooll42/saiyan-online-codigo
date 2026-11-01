using System;
using System.Globalization;
using System.Reflection;
using CommandLine;
using Intersect.Factories;
using Intersect.Framework.Reflection;
using Intersect.Plugins.Helpers;
using Intersect.Plugins.Interfaces;
using Intersect.Properties;

namespace Intersect.Plugins.Contexts;

public sealed class PluginBootstrapContext : IPluginBootstrapContext, IPluginBaseContext
{
	private sealed class Factory : IFactory<IPluginBootstrapContext>
	{
		private readonly Type _hostPluginContextType;

		private readonly string[] _args;

		private readonly Parser _parser;

		private readonly IPacketHelper _packetHelper;

		public Factory(Type hostPluginContextType, string[] args, Parser parser, IPacketHelper packetHelper)
		{
			_hostPluginContextType = hostPluginContextType;
			_args = args;
			_parser = parser;
			_packetHelper = packetHelper;
		}

		public IPluginBootstrapContext Create(params object[] args)
		{
			if (args.Length != 1)
			{
				string name = typeof(Plugin).GetName(qualified: true);
				throw new ArgumentException($"`{"args"}` should have 1 exactly argument (a {name}).", "args");
			}
			if (!(args[0] is Plugin plugin))
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentUICulture, ExceptionMessages.PluginBootstrapContextMissingPluginArgument, "Plugin"), "args");
			}
			return new PluginBootstrapContext(_hostPluginContextType, _args, _parser, plugin, _packetHelper);
		}
	}

	private Plugin Plugin { get; }

	public ICommandLineHelper CommandLine { get; }

	public IPacketHelper Packet { get; }

	public Assembly Assembly => Plugin.Reference.Assembly;

	public PluginConfiguration Configuration => Plugin.Configuration;

	public IEmbeddedResourceHelper EmbeddedResources { get; }

	public Type HostPluginContextType { get; }

	public ILoggingHelper Logging => Plugin.Logging;

	public IManifestHelper Manifest => Plugin.Manifest;

	public static IFactory<IPluginBootstrapContext> CreateFactory(Type hostPluginContextType, string[] args, Parser parser, IPacketHelper packetHelper)
	{
		return new Factory(hostPluginContextType, args, parser, packetHelper);
	}

	private PluginBootstrapContext(Type hostPluginContextType, string[] args, Parser parser, Plugin plugin, IPacketHelper parentPacketHelper)
	{
		HostPluginContextType = hostPluginContextType;
		Plugin = plugin;
		CommandLine = new CommandLineHelper(plugin.Logging.Plugin, args, parser);
		EmbeddedResources = new EmbeddedResourceHelper(Assembly);
		Packet = new PacketHelper(parentPacketHelper);
	}

	public TConfiguration GetTypedConfiguration<TConfiguration>() where TConfiguration : PluginConfiguration
	{
		return Configuration as TConfiguration;
	}
}
