using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Intersect.Framework.Core.GameObjects.Conditions;

public class ConditionListsSerializer : JsonConverter
{
	public override bool CanConvert(Type objectType)
	{
		return true;
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		JObject jObject = JObject.Load(reader);
		List<JProperty> list = jObject.Properties().ToList();
		ConditionLists conditionLists = ((existingValue != null) ? ((ConditionLists)existingValue) : new ConditionLists());
		conditionLists.Load((string?)list[0].Value);
		return conditionLists;
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		writer.WriteStartObject();
		writer.WritePropertyName("Lists");
		serializer.Serialize(writer, ((ConditionLists)value).Data());
		writer.WriteEndObject();
	}
}
