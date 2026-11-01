using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Diagnostics.Runtime;

namespace Intersect.Reflection;

public static class MemberInfoExtensions
{
	public static string GetFullName(this MemberInfo memberInfo)
	{
		if (memberInfo is Type type)
		{
			return type.FullName;
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
		string value = (fullyQualified ? methodInfo.ReturnType.FullName : methodInfo.ReturnType.Name);
		string value2 = (fullyQualified ? methodInfo.DeclaringType.FullName : methodInfo.DeclaringType.Name);
		IEnumerable<Type> source = from parameter in methodInfo.GetParameters()
			select parameter.ParameterType;
		IEnumerable<string> values = source.Select((Type parameterType) => (!fullyQualified) ? parameterType.Name : parameterType.FullName);
		return $"{value} {value2}.{methodInfo.Name}({string.Join(", ", values)})";
	}

	public static string GetFullSignature(this MethodInfo methodInfo)
	{
		return methodInfo.GetSignature(fullyQualified: true);
	}

	public static string GetSignature(this ClrMethod method, bool fullyQualified = false)
	{
		return method.Signature ?? string.Empty;
	}

	public static string GetFullSignature(this ClrMethod method)
	{
		return method.GetSignature(fullyQualified: true);
	}
}
