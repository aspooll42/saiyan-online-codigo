using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Intersect.Framework.Converters.Json;

public abstract class GenericSystemJsonConverterFactory : JsonConverterFactory
{
	private readonly Type _converterTypeDefinition;

	private readonly Type _serializedGenericTypeDefinition;

	protected GenericSystemJsonConverterFactory(Type converterTypeDefinition, Type serializedGenericTypeDefinition)
	{
		_converterTypeDefinition = converterTypeDefinition ?? throw new ArgumentNullException("converterTypeDefinition");
		_serializedGenericTypeDefinition = serializedGenericTypeDefinition ?? throw new ArgumentNullException("serializedGenericTypeDefinition");
	}

	public override bool CanConvert(Type typeToConvert)
	{
		if (typeToConvert.IsGenericType)
		{
			return typeToConvert.GetGenericTypeDefinition() == _serializedGenericTypeDefinition;
		}
		return false;
	}

	public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		Type type = typeToConvert.GetGenericArguments()[0];
		Type type2 = _converterTypeDefinition.MakeGenericType(type);
		return Activator.CreateInstance(type2) as JsonConverter;
	}
}
