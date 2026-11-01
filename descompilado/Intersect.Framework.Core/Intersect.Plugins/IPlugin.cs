using Intersect.Plugins.Interfaces;

namespace Intersect.Plugins;

public interface IPlugin
{
	IManifestHelper Manifest { get; }

	ILoggingHelper Logging { get; }

	PluginConfiguration Configuration { get; }

	bool IsEnabled { get; internal set; }

	string Key { get; }

	new int GetHashCode();
}
