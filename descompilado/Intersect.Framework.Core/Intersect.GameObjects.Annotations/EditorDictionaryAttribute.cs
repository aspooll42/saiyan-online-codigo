using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Intersect.Core;
using Intersect.Framework.Reflection;
using Intersect.Localization;
using Microsoft.Extensions.Logging;

namespace Intersect.GameObjects.Annotations;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class EditorDictionaryAttribute : EditorDisplayAttribute
{
	private delegate string? GetStringFromLocaleDictionaryGeneric(object? dictionaryObject, Enum genericKey);

	private static readonly Dictionary<Type, GetStringFromLocaleDictionaryGeneric> _cachedFormatters = new Dictionary<Type, GetStringFromLocaleDictionaryGeneric>();

	private static readonly MethodInfo _methodInfoCreateWeaklyTypedDelegate = typeof(EditorDictionaryAttribute).GetMethod("CreateWeaklyTypedDelegate", BindingFlags.Static | BindingFlags.NonPublic);

	public string Group { get; }

	public string Name { get; }

	public EditorDictionaryAttribute(string name)
	{
		Group = null;
		if (string.IsNullOrEmpty(name))
		{
			throw new ArgumentNullException("name");
		}
		Name = name;
	}

	public EditorDictionaryAttribute(string group, string name)
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
		FieldInfo fieldInfo = type2.GetMember(Name).FirstOrDefault((MemberInfo member) => member.MemberType == MemberTypes.Field) as FieldInfo;
		if (fieldInfo == null)
		{
			InvalidOperationException exception = new InvalidOperationException(type2.FullName + "." + Name + " does not exist.");
			ApplicationContext.Context.Value?.Logger.LogError(exception, "{ParentTypeName}.{Name} does not exist.", type2.GetName(qualified: true), Name);
			return base.Format(stringsType, value);
		}
		object value2 = fieldInfo.GetValue(value);
		if (!(value2 is Dictionary<int, LocalizedString> dictionary))
		{
			if (value2 is Dictionary<string, LocalizedString> dictionary2)
			{
				if (!(value is string text))
				{
					throw new ArgumentException("Expected an int but received a " + value?.GetType()?.FullName, "value");
				}
				if (!dictionary2.TryGetValue(text, out var value3))
				{
					throw new ArgumentOutOfRangeException("Key missing: '" + text + "'");
				}
				return value3;
			}
			if (typeof(LocaleDictionary<, >).ExtendedBy(value2?.GetType()) && value is Enum key)
			{
				return FormatLocaleDictionary(value2, key);
			}
			throw new InvalidOperationException("Unsupported type: " + fieldInfo.FieldType.FullName);
		}
		try
		{
			int num = Convert.ToInt32(value, CultureInfo.InvariantCulture);
			if (!dictionary.TryGetValue(num, out var value4))
			{
				value4 = $"KEY_MISSING=\"{num}\"";
			}
			return value4;
		}
		catch (Exception innerException)
		{
			throw new ArgumentException("Expected an int or int-like but received a " + value?.GetType()?.FullName, "value", innerException);
		}
	}

	private static GetStringFromLocaleDictionaryGeneric CreateWeaklyTypedDelegate<TKey>()
	{
		return delegate(object? dictionaryObject, Enum genericKey)
		{
			if (!(dictionaryObject is LocaleDictionary<TKey, LocalizedString> dictionary))
			{
				throw new ArgumentException("Dictionary was a " + dictionaryObject?.GetType().Name + " but expected " + typeof(LocaleDictionary<TKey, LocalizedString>).Name, "dictionaryObject");
			}
			if (!(genericKey is TKey key))
			{
				throw new ArgumentException($"Key was a {genericKey.GetFullishName()} but expected {typeof(TKey).Name}.");
			}
			return Format(dictionary, key);
		};
	}

	private static string? FormatLocaleDictionary(object? dictionaryObject, Enum key)
	{
		if (dictionaryObject == null)
		{
			return null;
		}
		Type type = dictionaryObject.GetType();
		if (!_cachedFormatters.TryGetValue(type, out var value))
		{
			object obj = _methodInfoCreateWeaklyTypedDelegate.MakeGenericMethod(key.GetType()).Invoke(null, Array.Empty<object>());
			if (!(obj is GetStringFromLocaleDictionaryGeneric getStringFromLocaleDictionaryGeneric))
			{
				throw new InvalidOperationException();
			}
			value = getStringFromLocaleDictionaryGeneric;
		}
		return value(dictionaryObject, key);
	}

	private static string? Format<TKey>(LocaleDictionary<TKey, LocalizedString> dictionary, TKey key)
	{
		if (!dictionary.TryGetValue(key, out var value))
		{
			throw new ArgumentOutOfRangeException($"Key missing: '{key}'");
		}
		return value;
	}
}
