using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Intersect.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum DisplayDirection
{
	StartToEnd,
	EndToStart,
	TopToBottom,
	BottomToTop
}
