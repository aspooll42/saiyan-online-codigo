using System;
using Newtonsoft.Json.Linq;

namespace Intersect.Framework.Serialization;

public static class JObjectExtensions
{
	public static bool TryGet<TEnum>(this JObject @object, string propertyName, out TEnum propertyValue) where TEnum : struct, Enum
	{
		return @object.TryGet(propertyName, default(TEnum), out propertyValue);
	}

	public static bool TryGet<TEnum>(this JObject @object, string propertyName, TEnum defaultValue, out TEnum propertyValue) where TEnum : struct, Enum
	{
		if (!@object.TryGetValue(propertyName, out JToken value))
		{
			propertyValue = defaultValue;
			return false;
		}
		if (!(value is JValue { Type: JTokenType.String } jValue))
		{
			propertyValue = defaultValue;
			return false;
		}
		string value2 = jValue.Value<string>();
		if (Enum.TryParse<TEnum>(value2, out propertyValue))
		{
			return true;
		}
		propertyValue = defaultValue;
		return false;
	}
}
