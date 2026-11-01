using System;
using System.Reflection;
using Intersect.Localization;

namespace Intersect.GameObjects.Annotations;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class EditorBooleanAttribute : EditorDisplayAttribute
{
	public BooleanStyle Style { get; set; } = BooleanStyle.YesNo;

	[Obsolete("We want to re-implement strings to be object-oriented.")]
	public override string Format(Type stringsType, object value)
	{
		if (stringsType == null)
		{
			throw new ArgumentNullException("stringsType");
		}
		MethodInfo method = stringsType.GetMethod("FormatBoolean", BindingFlags.Static | BindingFlags.Public);
		if (method == null)
		{
			throw new InvalidOperationException(stringsType.FullName + ".stringsType is missing.");
		}
		return method.Invoke(null, new object[2] { value, Style })?.ToString();
	}
}
