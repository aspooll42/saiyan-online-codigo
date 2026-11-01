using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Intersect.Reflection;

public static class AssemblyExtensions
{
	public static TParentType CreateInstanceOf<TParentType>(this Assembly assembly, params object[] args)
	{
		return assembly.CreateInstanceOf<TParentType>((Type _) => true, args);
	}

	public static TParentType CreateInstanceOf<TParentType>(this Assembly assembly, Func<Type, bool> predicate, params object[] args)
	{
		IEnumerable<Type> source = assembly.FindDefinedSubtypesOf<TParentType>();
		Type type = source.FirstOrDefault(predicate);
		if (type == null)
		{
			throw new InvalidOperationException("Found no matching subtype of " + typeof(TParentType).FullName + " that can be created.");
		}
		object obj = Activator.CreateInstance(type, args);
		if (obj is TParentType)
		{
			return (TParentType)obj;
		}
		throw new InvalidOperationException("Failed to create instance of " + typeof(TParentType).FullName + ".");
	}

	public static string GetVersionName(this Assembly assembly)
	{
		Version version = assembly.GetName().Version;
		string text = $"{version.Major}.{version.Minor}";
		string text2 = ((version.Major == 0) ? "-beta" : string.Empty);
		return text + text2;
	}

	public static IEnumerable<Type> FindAbstractSubtypesOf(this Assembly assembly, Type type)
	{
		return from subtype in assembly.FindSubtypesOf(type)
			where subtype?.IsAbstract ?? false
			select subtype;
	}

	public static IEnumerable<Type> FindAbstractSubtypesOf<TParentType>(this Assembly assembly)
	{
		return assembly.FindAbstractSubtypesOf(typeof(TParentType));
	}

	public static IEnumerable<Type> FindDefinedSubtypesOf(this Assembly assembly, Type type)
	{
		return from subtype in assembly.FindSubtypesOf(type)
			where !(subtype == null) && !subtype.IsAbstract && !subtype.IsGenericType && !subtype.IsInterface
			select subtype;
	}

	public static IEnumerable<Type> FindDefinedSubtypesOf<TParentType>(this Assembly assembly)
	{
		return assembly.FindDefinedSubtypesOf(typeof(TParentType));
	}

	public static IEnumerable<Type> FindGenericSubtypesOf(this Assembly assembly, Type type)
	{
		return from subtype in assembly.FindSubtypesOf(type)
			where subtype?.IsGenericType ?? false
			select subtype;
	}

	public static IEnumerable<Type> FindGenericSubtypesOf<TParentType>(this Assembly assembly)
	{
		return assembly.FindGenericSubtypesOf(typeof(TParentType));
	}

	public static IEnumerable<Type> FindInterfaceSubtypesOf(this Assembly assembly, Type type)
	{
		return from subtype in assembly.FindSubtypesOf(type)
			where subtype?.IsInterface ?? false
			select subtype;
	}

	public static IEnumerable<Type> FindInterfaceSubtypesOf<TParentType>(this Assembly assembly)
	{
		return assembly.FindInterfaceSubtypesOf(typeof(Type));
	}

	public static IEnumerable<Type> FindSubtypesOf(this Assembly assembly, Type type)
	{
		return assembly.GetTypes().Where(type.IsAssignableFrom);
	}

	public static IEnumerable<Type> FindSubtypesOf<TParentType>(this Assembly assembly)
	{
		return assembly.FindGenericSubtypesOf(typeof(TParentType));
	}

	public static IEnumerable<Type> FindValueSubtypesOf(this Assembly assembly, Type type)
	{
		return from subtype in assembly.FindSubtypesOf(type)
			where subtype?.IsValueType ?? false
			select subtype;
	}

	public static IEnumerable<Type> FindValueSubtypesOf<TParentType>(this Assembly assembly)
	{
		return assembly.FindValueSubtypesOf(typeof(Type));
	}

	public static bool TryFindResource(this Assembly assembly, string resourceName, out string? manifestResourceName)
	{
		if (assembly.IsDynamic)
		{
			manifestResourceName = null;
			return false;
		}
		manifestResourceName = assembly.GetManifestResourceNames().FirstOrDefault((string name) => name?.Contains(resourceName, StringComparison.CurrentCulture) ?? false);
		return manifestResourceName != null;
	}
}
