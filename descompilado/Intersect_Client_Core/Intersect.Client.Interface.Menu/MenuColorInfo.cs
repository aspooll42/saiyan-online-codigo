using Newtonsoft.Json;

namespace Intersect.Client.Interface.Menu;

internal sealed class MenuColorInfo
{
	[JsonProperty("Nom")]
	public string Name { get; set; } = string.Empty;

	[JsonProperty("Tissu")]
	public string Cloth { get; set; } = string.Empty;

	[JsonProperty("Second")]
	public string Second { get; set; } = string.Empty;
}
