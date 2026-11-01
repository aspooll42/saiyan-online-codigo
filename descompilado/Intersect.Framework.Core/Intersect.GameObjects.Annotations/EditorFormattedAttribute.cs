using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Intersect.Core;
using Intersect.Framework.Reflection;
using Intersect.Localization;
using Microsoft.Extensions.Logging;

namespace Intersect.GameObjects.Annotations;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class EditorFormattedAttribute : EditorDisplayAttribute
{
	public string Group { get; }

	public string Name { get; }

	public EditorFormattedAttribute(string name)
	{
		Group = null;
		if (string.IsNullOrEmpty(name))
		{
			throw new ArgumentNullException("name");
		}
		Name = name;
	}

	public EditorFormattedAttribute(string group, string name)
		: this(name)
	{
		if (string.IsNullOrEmpty(group))
		{
			throw new ArgumentNullException("group");
		}
		Group = group;
	}

	[Obsolete("We want to re-implement strings to be object-oriented.")]
	public override string Format(Type stringsType, object value)
	{
		if (stringsType == null)
		{
			throw new ArgumentNullException("stringsType");
		}
		Type type = ((Group == null) ? null : stringsType.GetNestedTypes(BindingFlags.Static | BindingFlags.Public).FirstOrDefault((Type type3) => type3.Name == Group));
		Type type2 = type ?? stringsType;
		MemberInfo[] member = type2.GetMember(Name);
		if (member.Length == 0)
		{
			InvalidOperationException exception = new InvalidOperationException(type2.FullName + "." + Name + " does not exist.");
			ApplicationContext.Context.Value?.Logger.LogError(exception, "{ParentTypeName}.{Name} does not exist.", type2.GetName(qualified: true), Name);
			return base.Format(stringsType, value);
		}
		MemberInfo memberInfo = member.First();
		if (!(memberInfo is MethodInfo))
		{
			if (memberInfo is FieldInfo fieldInfo)
			{
				if (fieldInfo.FieldType != typeof(LocalizedString))
				{
					throw new InvalidOperationException("Expected " + typeof(LocalizedString).FullName + " but the field is " + fieldInfo.FieldType.FullName);
				}
				return ((fieldInfo.GetValue(null) as LocalizedString) ?? throw new InvalidOperationException()).ToString(value);
			}
			throw new InvalidOperationException($"Unsupported member type {memberInfo.MemberType}: {memberInfo.DeclaringType.FullName}.{memberInfo.Name}");
		}
		return InvokeFormatterMethod(member.Cast<MethodInfo>(), value)?.ToString();
	}

	private static object InvokeFormatterMethod(IEnumerable<MethodInfo> methodInfos, object value)
	{
		MethodInfo methodInfo = methodInfos.OrderBy((MethodInfo methodInfo3) => methodInfo3.GetParameters().Length).FirstOrDefault((MethodInfo methodInfo3) => methodInfo3.GetParameters().Length == 1 || methodInfo3.GetParameters().Count((ParameterInfo parameter) => !parameter.HasDefaultValue) == 1);
		if (methodInfo == null)
		{
			MethodInfo methodInfo2 = methodInfos.First();
			throw new InvalidOperationException("No override that has 1 required parameter exists for " + methodInfo2.DeclaringType.FullName + "." + methodInfo2.Name);
		}
		if (methodInfo.GetParameters().Length == 1)
		{
			return methodInfo.Invoke(null, new object[1] { value });
		}
		return methodInfo.Invoke(null, new object[1] { value }.Concat<object>(from parameter in methodInfo.GetParameters()
			where parameter.HasDefaultValue
			select parameter.RawDefaultValue).ToArray());
	}
}
