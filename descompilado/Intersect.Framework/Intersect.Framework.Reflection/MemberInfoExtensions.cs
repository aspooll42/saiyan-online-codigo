using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Intersect.Framework.Annotations;

namespace Intersect.Framework.Reflection;

public static class MemberInfoExtensions
{
	public static string GetFullName(this MemberInfo memberInfo)
	{
		if (memberInfo is Type type)
		{
			return type.GetName(qualified: true);
		}
		Type declaringType = memberInfo.DeclaringType;
		if (!(declaringType == null))
		{
			return declaringType.FullName + "." + memberInfo.Name;
		}
		return memberInfo.Name;
	}

	public static string GetSignature(this MethodInfo methodInfo, bool fullyQualified = false)
	{
		string name = methodInfo.ReturnType.GetName(fullyQualified);
		string value = methodInfo.DeclaringType?.GetName(fullyQualified) ?? "???";
		IEnumerable<Type> source = from parameter in methodInfo.GetParameters()
			select parameter.ParameterType;
		IEnumerable<string> values = source.Select((Type parameterType) => (!fullyQualified) ? parameterType.Name : parameterType.FullName);
		return $"{name} {value}.{methodInfo.Name}({string.Join(", ", values)})";
	}

	public static string GetFullSignature(this MethodInfo methodInfo)
	{
		return methodInfo.GetSignature(fullyQualified: true);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsIgnored(this MemberInfo memberInfo)
	{
		return IgnoreAttribute.IsIgnored(memberInfo);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsPassword(this MemberInfo memberInfo)
	{
		if (!PasswordAttribute.IsPassword(memberInfo))
		{
			return memberInfo.GetCustomAttributes<PasswordPropertyTextAttribute>().Any((PasswordPropertyTextAttribute attribute) => attribute.Password);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsReadOnly(this MemberInfo memberInfo)
	{
		return memberInfo.GetCustomAttributes<ReadOnlyAttribute>().Any((ReadOnlyAttribute attribute) => attribute.IsReadOnly);
	}
}
