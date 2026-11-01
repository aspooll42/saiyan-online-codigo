using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Intersect.Framework.Converters.Json;

public sealed class HandleSystemJsonConverterFactory : GenericSystemJsonConverterFactory
{
	private class HandleJsonConverter<T> : JsonConverter<Handle<T>>
	{
		public override Handle<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			return new Handle<T>(new IntPtr(reader.GetInt64()));
		}

		public override void Write(Utf8JsonWriter writer, Handle<T> value, JsonSerializerOptions options)
		{
			writer.WriteNumberValue(((IntPtr)value.Pointer).ToInt64());
		}
	}

	public HandleSystemJsonConverterFactory()
		: base(typeof(HandleJsonConverter<>), typeof(Handle<>))
	{
	}
}
