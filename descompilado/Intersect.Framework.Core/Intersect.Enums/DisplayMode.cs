using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Intersect.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum DisplayMode
{
	Default,
	Center,
	Stretch,
	FitHeight,
	FitWidth,
	Fit,
	Cover
}
