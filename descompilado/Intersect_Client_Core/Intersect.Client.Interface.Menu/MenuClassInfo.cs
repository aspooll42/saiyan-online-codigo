using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

namespace Intersect.Client.Interface.Menu;

internal sealed class MenuClassInfo
{
	[JsonProperty("Nom")]
	public string Name { get; set; } = string.Empty;

	[JsonProperty("Classe")]
	public string ClassName { get; set; } = string.Empty;

	[JsonIgnore]
	public IEnumerable<string> ClassNames => ClassName.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

	[JsonProperty("Titre")]
	public string Title { get; set; } = string.Empty;

	[JsonProperty("Texte")]
	public string Text { get; set; } = string.Empty;

	[JsonProperty("Technique")]
	public string Technique { get; set; } = string.Empty;

	[JsonProperty("Aura")]
	public string Aura { get; set; } = "bleue";

	[JsonProperty("Sprite")]
	public string Sprite { get; set; } = string.Empty;

	[JsonProperty("Passif")]
	public string Passive { get; set; } = string.Empty;

	[JsonProperty("Couleurs")]
	public List<MenuColorInfo> Colors { get; set; } = new List<MenuColorInfo>();

	public bool Matches(string? className)
	{
		if (!string.IsNullOrWhiteSpace(className))
		{
			return ClassNames.Any((string name) => SameName(name, className));
		}
		return false;
	}

	public static bool SameName(string a, string b)
	{
		return string.Compare(a.Trim(), b.Trim(), CultureInfo.InvariantCulture, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0;
	}
}
