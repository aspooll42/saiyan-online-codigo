using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Intersect.Framework.Annotations;

public abstract class CachedExistenceAttribute<TAttribute> : Attribute where TAttribute : CachedExistenceAttribute<TAttribute>
{
	private static readonly ConcurrentDictionary<MemberInfo, bool> CachedExistence = new ConcurrentDictionary<MemberInfo, bool>();

	protected static bool ExistsOn(MemberInfo memberInfo, bool allowInheritedFromDeclaringType = true)
	{
		if (CachedExistence.TryGetValue(memberInfo, out var value))
		{
			return value;
		}
		value = memberInfo.GetCustomAttribute<TAttribute>() != null;
		if (!value)
		{
			if (!(memberInfo is PropertyInfo propertyInfo))
			{
				if (memberInfo is FieldInfo fieldInfo)
				{
					value |= ExistsOn(fieldInfo.FieldType);
				}
			}
			else
			{
				value |= ExistsOn(propertyInfo.PropertyType);
			}
		}
		if (!value && allowInheritedFromDeclaringType)
		{
			Type declaringType = memberInfo.DeclaringType;
			if ((object)declaringType != null)
			{
				value = ExistsOn(declaringType);
			}
		}
		CachedExistence[memberInfo] = value;
		return value;
	}
}
