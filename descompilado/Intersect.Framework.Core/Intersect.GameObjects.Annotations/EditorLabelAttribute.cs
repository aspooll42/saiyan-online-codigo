using System;
using System.Reflection;

namespace Intersect.GameObjects.Annotations;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class EditorLabelAttribute : Attribute
{
	public string Group { get; }

	public string Name { get; }

	public EditorLabelAttribute(string name)
	{
		Group = null;
		if (string.IsNullOrEmpty(name))
		{
			throw new ArgumentNullException("name");
		}
		Name = name;
	}

	public EditorLabelAttribute(string group, string name)
		: this(name)
	{
		if (string.IsNullOrEmpty(group))
		{
			throw new ArgumentNullException("group");
		}
		Group = group;
	}

	[Obsolete("We want to re-implement strings to be object-oriented.")]
	public string Evaluate(Type stringsType)
	{
		if (stringsType == null)
		{
			throw new ArgumentNullException("stringsType");
		}
		Type nestedType = stringsType.GetNestedType(Group, BindingFlags.Static | BindingFlags.Public);
		if (nestedType == null)
		{
			throw new InvalidOperationException($"'{stringsType.FullName}.{Group}' does not exist.");
		}
		FieldInfo field = nestedType.GetField(Name);
		if (field == null)
		{
			throw new InvalidOperationException($"'{nestedType.FullName}.{Name}' does not exist.");
		}
		return field.GetValue(null)?.ToString();
	}
}
