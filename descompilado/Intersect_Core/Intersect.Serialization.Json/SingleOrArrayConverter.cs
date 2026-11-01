using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Intersect.Serialization.Json;

public class SingleOrArrayConverter<T> : JsonConverter
{
	public override bool CanWrite => false;

	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(List<T>);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		JToken jToken = JToken.Load(reader);
		if (jToken == null)
		{
			return new List<T>();
		}
		if (jToken.Type != JTokenType.Array)
		{
			return new List<T> { jToken.ToObject<T>() };
		}
		return jToken.ToObject<List<T>>();
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}
}
