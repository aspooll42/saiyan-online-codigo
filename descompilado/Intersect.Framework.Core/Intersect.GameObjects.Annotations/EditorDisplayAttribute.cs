using System;
using System.Reflection;

namespace Intersect.GameObjects.Annotations;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class EditorDisplayAttribute : Attribute
{
	public EmptyBehavior EmptyBehavior { get; set; }

	public EditorFieldType FieldType { get; set; }

	public StringBehavior StringBehavior { get; set; }

	[Obsolete("We want to re-implement strings to be object-oriented.")]
	public virtual string Format(Type stringsType, object value)
	{
		string text2;
		if (value is string text)
		{
			text2 = text;
			StringBehavior stringBehavior = StringBehavior;
			if (stringBehavior != StringBehavior.NoChange && stringBehavior == StringBehavior.Trim)
			{
				text2 = text.Trim();
			}
		}
		else
		{
			text2 = value?.ToString();
		}
		switch (EmptyBehavior)
		{
		case EmptyBehavior.ShowNoneOnEmpty:
			if (text2 != null && string.IsNullOrEmpty(text2))
			{
				text2 = GetNone(stringsType);
			}
			break;
		case EmptyBehavior.ShowNoneOnNull:
			if (text2 == null)
			{
				text2 = GetNone(stringsType);
			}
			break;
		case EmptyBehavior.ShowNoneOnNullOrEmpty:
			if (string.IsNullOrEmpty(text2))
			{
				text2 = GetNone(stringsType);
			}
			break;
		}
		return text2;
	}

	protected static string GetNone(Type stringsType)
	{
		if (stringsType == null)
		{
			throw new ArgumentNullException("stringsType");
		}
		FieldInfo fieldInfo = stringsType.GetNestedType("General")?.GetField("None");
		if (fieldInfo == null)
		{
			throw new InvalidOperationException();
		}
		return fieldInfo?.GetValue(null)?.ToString();
	}
}
