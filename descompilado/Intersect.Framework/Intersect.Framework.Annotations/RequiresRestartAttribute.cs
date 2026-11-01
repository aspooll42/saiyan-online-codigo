using System;
using System.Reflection;

namespace Intersect.Framework.Annotations;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field)]
public sealed class RequiresRestartAttribute : CachedExistenceAttribute<IgnoreAttribute>
{
	public static bool RequiresRestart(MemberInfo memberInfo)
	{
		return CachedExistenceAttribute<IgnoreAttribute>.ExistsOn(memberInfo, !DoesNotRequireRestartAttribute.DoesNotRequireRestart(memberInfo));
	}
}
