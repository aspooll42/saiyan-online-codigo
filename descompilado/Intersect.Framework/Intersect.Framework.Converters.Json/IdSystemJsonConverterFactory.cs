using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Intersect.Framework.Converters.Json;

public sealed class IdSystemJsonConverterFactory : GenericSystemJsonConverterFactory
{
	private class IdJsonConverter<T> : JsonConverter<Id<T>>
	{
		public override Id<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			return new Id<T>(reader.GetGuid());
		}

		public override void Write(Utf8JsonWriter writer, Id<T> value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.Guid);
		}
	}

	public IdSystemJsonConverterFactory()
		: base(typeof(IdJsonConverter<>), typeof(Id<>))
	{
	}
}
