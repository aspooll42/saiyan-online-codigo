using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Intersect.Framework.Reflection;

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
			throw new InvalidOperationException(string.Format(ReflectionStrings.AssemblyExtensions_NoMatchingSubtype, typeof(TParentType).FullName));
		}
		object obj = Activator.CreateInstance(type, args);
		if (obj is TParentType)
		{
			return (TParentType)obj;
		}
		throw new InvalidOperationException(string.Format(ReflectionStrings.AssemblyExtensions_FailedToCreateInstance, typeof(TParentType).FullName));
	}

	public static string GetMetadataVersion(this Assembly assembly, bool excludeCommitSha = false)
	{
		AssemblyName name = assembly.GetName();
		return GetMetadataVersionFrom(assembly, name, excludeCommitSha);
	}

	public static string GetMetadataVersionName(this Assembly assembly, bool excludeCommitSha = false)
	{
		return "v" + assembly.GetMetadataVersion(excludeCommitSha);
	}

	private static string GetMetadataVersionFrom(Assembly assembly, AssemblyName assemblyName, bool excludeCommitSha)
	{
		Version version = assemblyName.Version ?? new Version(0, 0, 0, 0);
		Dictionary<string, string> dictionary = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().ToDictionary((AssemblyMetadataAttribute attribute) => attribute.Key, (AssemblyMetadataAttribute attribute) => attribute.Value);
		if (dictionary.TryGetValue("BuildNumber", out var value) && int.TryParse(value, CultureInfo.InvariantCulture, out var result))
		{
			version = new Version(version.Major, version.Minor, version.Build, result);
		}
		string text = version.ToString() ?? "";
		if (dictionary.TryGetValue("VersionNameSuffix", out var value2))
		{
			text = text + "-" + value2;
		}
		if (!excludeCommitSha && dictionary.TryGetValue("CommitSha", out var value3))
		{
			text = text + "+" + value3;
		}
		return text;
	}

	public static string GetMetadataName(this Assembly assembly, bool excludeCommitSha = false)
	{
		AssemblyName name = assembly.GetName();
		return name.Name + " v" + GetMetadataVersionFrom(assembly, name, excludeCommitSha);
	}

	public static string GetVersionName(this Assembly assembly)
	{
		Version version = assembly.GetName().Version;
		if (version == null)
		{
			return "???";
		}
		string text = $"{version.Major}.{version.Minor}";
		string text2 = ((version.Major == 0) ? "-beta" : string.Empty);
		return text + text2;
	}

	public static IEnumerable<Type> FindAbstractSubtypesOf(this Assembly assembly, Type type)
	{
		return from subtype in assembly.FindSubtypesOf(type)
			where subtype.IsAbstract
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
			where subtype.IsGenericType
			select subtype;
	}

	public static IEnumerable<Type> FindGenericSubtypesOf<TParentType>(this Assembly assembly)
	{
		return assembly.FindGenericSubtypesOf(typeof(TParentType));
	}

	public static IEnumerable<Type> FindInterfaceSubtypesOf(this Assembly assembly, Type type)
	{
		return from subtype in assembly.FindSubtypesOf(type)
			where subtype.IsInterface
			select subtype;
	}

	public static IEnumerable<Type> FindInterfaceSubtypesOf<TParentType>(this Assembly assembly)
	{
		return assembly.FindInterfaceSubtypesOf(typeof(TParentType));
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
			where subtype.IsValueType
			select subtype;
	}

	public static IEnumerable<Type> FindValueSubtypesOf<TParentType>(this Assembly assembly)
	{
		return assembly.FindValueSubtypesOf(typeof(TParentType));
	}

	public static string[] FindMatchingResources(this Assembly assembly, string search)
	{
		if (!assembly.IsDynamic)
		{
			return (from name in assembly.GetManifestResourceNames()
				where name.Contains(search)
				select name).ToArray();
		}
		return Array.Empty<string>();
	}

	public static string[] FindMatchingResources(this Assembly assembly, Regex regex)
	{
		if (!assembly.IsDynamic)
		{
			return (from name in assembly.GetManifestResourceNames()
				where regex.IsMatch(name)
				select name).ToArray();
		}
		return Array.Empty<string>();
	}

	public static bool TryFindResource(this Assembly assembly, string resourceName, [NotNullWhen(true)] out string? manifestResourceName)
	{
		if (assembly.IsDynamic)
		{
			manifestResourceName = null;
			return false;
		}
		manifestResourceName = assembly.GetManifestResourceNames().FirstOrDefault((string name) => name.Contains(resourceName, StringComparison.CurrentCulture));
		return manifestResourceName != null;
	}

	public static void UnpackEmbeddedFile(this Assembly assembly, string resourceName, bool overwrite = false)
	{
		assembly.UnpackEmbeddedFileAsync(CancellationToken.None, resourceName, overwrite).Wait();
	}

	public static void UnpackEmbeddedFile(this Assembly assembly, string resourceName, FileInfo fileInfo, bool overwrite = false)
	{
		assembly.UnpackEmbeddedFileAsync(CancellationToken.None, resourceName, fileInfo, overwrite).Wait();
	}

	public static Task UnpackEmbeddedFileAsync(this Assembly assembly, CancellationToken cancellationToken, string resourceName, bool overwrite = false, string? unpackedName = null)
	{
		return assembly.UnpackEmbeddedFileAsync(cancellationToken, resourceName, new FileInfo(unpackedName ?? resourceName), overwrite);
	}

	public static async Task UnpackEmbeddedFileAsync(this Assembly assembly, CancellationToken cancellationToken, string resourceName, FileInfo fileInfo, bool overwrite = false)
	{
		if (!overwrite && fileInfo.Exists)
		{
			return;
		}
		if (!assembly.TryFindResource(resourceName, out string manifestResourceName))
		{
			throw new MissingManifestResourceException(string.Format(ReflectionStrings.UnpackEmbeddedFile_MissingManifestResourceInfo, resourceName));
		}
		await using Stream manifestResourceStream = assembly.GetManifestResourceStream(manifestResourceName);
		if (manifestResourceStream == null)
		{
			throw new MissingManifestResourceException(string.Format(ReflectionStrings.UnpackEmbeddedFile_UnableToOpenStream, manifestResourceName));
		}
		DirectoryInfo directory = fileInfo.Directory;
		if (!(directory?.Exists ?? true))
		{
			directory.Create();
		}
		await using FileStream fileStream = fileInfo.OpenWrite();
		await manifestResourceStream.CopyToAsync(fileStream, cancellationToken);
	}
}
