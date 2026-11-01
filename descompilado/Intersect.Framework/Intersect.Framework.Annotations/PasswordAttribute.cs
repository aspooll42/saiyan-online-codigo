using System;
using System.Reflection;

namespace Intersect.Framework.Annotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class PasswordAttribute : CachedExistenceAttribute<PasswordAttribute>
{
	public static bool IsPassword(MemberInfo memberInfo)
	{
		return CachedExistenceAttribute<PasswordAttribute>.ExistsOn(memberInfo);
	}
}
