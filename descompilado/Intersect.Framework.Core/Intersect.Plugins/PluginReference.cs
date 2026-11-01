using System;
using System.IO;
using System.Reflection;

namespace Intersect.Plugins;

internal sealed class PluginReference
{
	internal Assembly Assembly { get; }

	internal Type ConfigurationType { get; }

	internal Type EntryType { get; }

	internal string ConfigurationFile => Path.Combine(Directory, "config.json");

	internal string Directory => Path.GetDirectoryName(Assembly.Location) ?? throw new InvalidOperationException($"Error getting plugin directory for assembly {Assembly.FullName} ({Assembly.Location}).");

	internal PluginReference(Assembly assembly, Type configurationType, Type entryType)
	{
		Assembly = assembly;
		ConfigurationType = configurationType;
		EntryType = entryType;
	}

	internal IPluginEntry CreateInstance()
	{
		return (Activator.CreateInstance(EntryType) as IPluginEntry) ?? throw new InvalidOperationException();
	}
}
