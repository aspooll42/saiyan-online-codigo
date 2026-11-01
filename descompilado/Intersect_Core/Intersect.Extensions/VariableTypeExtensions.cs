using System;
using System.Reflection;
using Intersect.Attributes;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Events;

namespace Intersect.Extensions;

public static class VariableTypeExtensions
{
	public static GameObjectType GetRelatedTable(this VariableType value)
	{
		if (!Enum.IsDefined(value))
		{
			throw new ArgumentOutOfRangeException("value", value, "Invalid VariableType enum");
		}
		string name = Enum.GetName(value);
		if (name == null)
		{
			throw new ArgumentOutOfRangeException("value", value, "Missing enum name");
		}
		FieldInfo field = typeof(VariableType).GetField(name);
		if (field == null)
		{
			throw new MissingFieldException($"Reflection failed for VariableType enum, value was {value}", "value");
		}
		RelatedTableAttribute customAttribute = field.GetCustomAttribute<RelatedTableAttribute>();
		if (customAttribute == null)
		{
			throw new ArgumentException($"Failed to get RelatedTable attribute for VariableType enum, value was {value}", "value");
		}
		return customAttribute.TableType;
	}
}
