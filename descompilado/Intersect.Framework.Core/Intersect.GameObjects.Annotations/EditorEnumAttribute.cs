using System;

namespace Intersect.GameObjects.Annotations;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class EditorEnumAttribute : EditorDisplayAttribute
{
	public Type EnumType { get; }

	public EditorEnumAttribute(Type enumType)
	{
		EnumType = enumType ?? throw new ArgumentNullException("enumType");
		if (!enumType.IsEnum)
		{
			throw new ArgumentException(enumType.FullName + " is not a enum type.", "enumType");
		}
	}
}
