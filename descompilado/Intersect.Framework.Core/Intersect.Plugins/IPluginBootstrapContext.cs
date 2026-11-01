using System;
using Intersect.Plugins.Interfaces;

namespace Intersect.Plugins;

public interface IPluginBootstrapContext : IPluginBaseContext
{
	ICommandLineHelper CommandLine { get; }

	Type HostPluginContextType { get; }

	IPacketHelper Packet { get; }
}
