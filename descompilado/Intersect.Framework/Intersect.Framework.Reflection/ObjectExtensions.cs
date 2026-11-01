using System;
using System.Runtime.CompilerServices;

namespace Intersect.Framework.Reflection;

public static class ObjectExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string GetQualifiedTypeName(this object @object)
	{
		return @object.GetType().GetQualifiedName();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string GetFullishName(this object @object)
	{
		Type type = @object.GetType();
		return type.FullName ?? type.Name;
	}
}
