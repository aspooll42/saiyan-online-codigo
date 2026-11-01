using System.Reflection;
using Intersect.Plugins.Interfaces;

namespace Intersect.Plugins;

public interface IPluginBaseContext
{
	Assembly Assembly { get; }

	PluginConfiguration Configuration { get; }

	IEmbeddedResourceHelper EmbeddedResources { get; }

	ILoggingHelper Logging { get; }

	IManifestHelper Manifest { get; }

	TConfiguration GetTypedConfiguration<TConfiguration>() where TConfiguration : PluginConfiguration;
}
