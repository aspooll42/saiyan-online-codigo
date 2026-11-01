using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Framework.Reflection;
using Newtonsoft.Json;

namespace Intersect.Framework.Converters.Json;

public abstract class GenericFactoryNewtonsoftJsonConverter : JsonConverter
{
	private static readonly Dictionary<Type, JsonConverter> Converters = new Dictionary<Type, JsonConverter>();

	private readonly Type _converterTypeDefinition;

	private readonly Type _serializedGenericTypeDefinition;

	protected GenericFactoryNewtonsoftJsonConverter(Type converterTypeDefinition, Type serializedGenericTypeDefinition)
	{
		_converterTypeDefinition = converterTypeDefinition ?? throw new ArgumentNullException("converterTypeDefinition");
		_serializedGenericTypeDefinition = serializedGenericTypeDefinition ?? throw new ArgumentNullException("serializedGenericTypeDefinition");
	}

	public override bool CanConvert(Type objectType)
	{
		if (objectType.IsGenericType)
		{
			return objectType.GetGenericTypeDefinition() == _serializedGenericTypeDefinition;
		}
		return false;
	}

	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		if (reader.Value == null)
		{
			return reader.Value;
		}
		JsonConverter jsonConverter = CreateActualConverter(_converterTypeDefinition, _serializedGenericTypeDefinition, objectType);
		return jsonConverter.ReadJson(reader, objectType, existingValue, serializer);
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		if (value == null)
		{
			writer.WriteNull();
			return;
		}
		JsonConverter jsonConverter = CreateActualConverter(_converterTypeDefinition, _serializedGenericTypeDefinition, value.GetType());
		jsonConverter.WriteJson(writer, value, serializer);
	}

	private static JsonConverter CreateActualConverter(Type converterTypeDefinition, Type serializedGenericTypeDefinition, Type objectType)
	{
		if (Converters.TryGetValue(objectType, out JsonConverter value))
		{
			return value;
		}
		Type type = objectType.FindGenericTypeParameters(typeof(Nullable<>), throwOnNonGeneric: false).FirstOrDefault() ?? objectType;
		Type[] typeArguments = type.FindGenericTypeParameters(serializedGenericTypeDefinition);
		Type type2 = converterTypeDefinition.MakeGenericType(typeArguments);
		value = Activator.CreateInstance(type2) as JsonConverter;
		Converters[objectType] = value ?? throw new InvalidOperationException();
		return value;
	}
}
