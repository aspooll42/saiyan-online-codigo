using Newtonsoft.Json;

namespace Intersect.Plugins;

public class PluginConfiguration
{
	[JsonProperty]
	public bool IsEnabled { get; internal set; }
}
