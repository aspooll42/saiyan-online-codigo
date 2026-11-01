using System;
using Newtonsoft.Json;

namespace Intersect;

public class ColorConverter : JsonConverter<Color>
{
	public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
	{
		writer.WriteValue($"{value?.A ?? 0},{value?.R ?? 0},{value?.G ?? 0},{value?.B ?? 0}");
	}

	public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		return Color.FromString(reader.Value as string, Color.Black);
	}
}
