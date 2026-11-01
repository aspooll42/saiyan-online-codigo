using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Intersect.Framework.Reflection;

public static class ReflectionHelper
{
	private static Assembly[]? _cachedAssemblies;

	private static Type[]? _cachedTypes;

	public static bool TryFindEmbeddedResource(string hint, [NotNullWhen(true)] out Stream? manifestResourceStream, bool ignoreCache = false, bool includeDynamic = false)
	{
		Assembly[] loadedAssemblies = GetLoadedAssemblies(ignoreCache, includeDynamic);
		Assembly[] array = loadedAssemblies;
		foreach (Assembly assembly in array)
		{
			if (assembly.TryFindResource(hint, out string manifestResourceName))
			{
				manifestResourceStream = assembly.GetManifestResourceStream(manifestResourceName);
				if (manifestResourceStream != null)
				{
					return true;
				}
			}
		}
		manifestResourceStream = null;
		return false;
	}

	public static Assembly[] GetLoadedAssemblies(bool ignoreCache = false, bool includeDynamic = false)
	{
		if (!ignoreCache && _cachedAssemblies != null)
		{
			return _cachedAssemblies;
		}
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		int num = 0;
		for (int i = 0; i < assemblies.Length; i++)
		{
			Assembly assembly = assemblies[i];
			try
			{
				_ = assembly.ExportedTypes;
			}
			catch
			{
				continue;
			}
			if (i != num)
			{
				assemblies[num] = assembly;
			}
			if (includeDynamic || !assembly.IsDynamic)
			{
				num++;
			}
		}
		_cachedAssemblies = assemblies[..num].ToArray();
		_cachedTypes = null;
		return _cachedAssemblies;
	}

	public static Type[] GetLoadedTypes(bool ignoreCache = false)
	{
		if (!ignoreCache && _cachedTypes != null)
		{
			return _cachedTypes;
		}
		Assembly[] loadedAssemblies = GetLoadedAssemblies(ignoreCache);
		_cachedTypes = loadedAssemblies.SelectMany((Assembly assembly) => assembly.ExportedTypes).ToArray();
		return _cachedTypes;
	}
}
