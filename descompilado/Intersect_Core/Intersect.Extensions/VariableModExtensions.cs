using System;
using System.Reflection;
using Intersect.Attributes;
using Intersect.Framework.Core.GameObjects.Events;

namespace Intersect.Extensions;

public static class VariableModExtensions
{
	public static VariableType GetRelatedVariableType(this VariableModType value)
	{
		if (!Enum.IsDefined(value))
		{
			throw new ArgumentOutOfRangeException("value", value, "Invalid VariableMod enum");
		}
		string name = Enum.GetName(value);
		if (name == null)
		{
			throw new ArgumentOutOfRangeException("value", value, "Missing enum name");
		}
		FieldInfo field = typeof(VariableModType).GetField(name);
		if (field == null)
		{
			throw new MissingFieldException($"Reflection failed for VariableType enum, value was {value}", "value");
		}
		RelatedVariableTypeAttribute customAttribute = field.GetCustomAttribute<RelatedVariableTypeAttribute>();
		if (customAttribute == null)
		{
			throw new ArgumentException($"Failed to get RelatedVariableType attribute for VariableType enum, value was {value}", "value");
		}
		return customAttribute.VariableType;
	}
}
