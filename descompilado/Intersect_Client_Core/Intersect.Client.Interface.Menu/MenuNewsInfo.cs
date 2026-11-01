using Newtonsoft.Json;

namespace Intersect.Client.Interface.Menu;

internal sealed class MenuNewsInfo
{
	[JsonProperty("Icone")]
	public string Icon { get; set; } = string.Empty;

	[JsonProperty("Titre")]
	public string Title { get; set; } = string.Empty;

	[JsonProperty("Texte")]
	public string Text { get; set; } = string.Empty;
}
