using System;
using System.Reflection;

namespace Intersect.Framework.Annotations;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field)]
public sealed class IgnoreAttribute : CachedExistenceAttribute<IgnoreAttribute>
{
	public static bool IsIgnored(MemberInfo memberInfo)
	{
		return CachedExistenceAttribute<IgnoreAttribute>.ExistsOn(memberInfo);
	}
}
