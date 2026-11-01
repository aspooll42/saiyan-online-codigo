using Intersect.Core;
using Intersect.Plugins.Helpers;
using Intersect.Plugins.Interfaces;

namespace Intersect.Plugins;

public sealed class Plugin : IPlugin
{
	public IManifestHelper Manifest { get; }

	public ILoggingHelper Logging { get; }

	public PluginConfiguration Configuration { get; internal set; }

	internal PluginReference Reference { get; }

	internal bool IsEnabled
	{
		get
		{
			return Configuration.IsEnabled;
		}
		set
		{
			Configuration.IsEnabled = value;
		}
	}

	bool IPlugin.IsEnabled
	{
		get
		{
			return IsEnabled;
		}
		set
		{
			IsEnabled = value;
		}
	}

	public string Key => Manifest.Key;

	internal static Plugin Create(IApplicationContext applicationContext, IManifestHelper manifest, PluginReference reference)
	{
		return new Plugin(manifest, new LoggingHelper(applicationContext.Logger, manifest, reference), reference);
	}

	private Plugin(IManifestHelper manifest, ILoggingHelper logging, PluginReference reference)
	{
		Manifest = manifest;
		Logging = logging;
		Reference = reference;
	}

	public override int GetHashCode()
	{
		return Manifest.Key.GetHashCode();
	}
}
