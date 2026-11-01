using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Intersect.Core;
using Intersect.Plugins.Interfaces;
using Intersect.Plugins.Manifests;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Intersect.Plugins.Loaders;

public static class ManifestLoader
{
	public delegate IManifestHelper ManifestLoaderDelegate(Assembly assembly);

	private static readonly Type ManifestType = typeof(IManifestHelper);

	public static readonly List<ManifestLoaderDelegate> ManifestLoaderDelegates;

	public static IManifestHelper FindManifest(Assembly assembly)
	{
		if (ManifestLoaderDelegates.Count < 1)
		{
			throw new InvalidOperationException("ManifestLoaderDelegates was initialized with no pre-registered delegates, or the pre-defined delegates were removed and no alternatives were added.");
		}
		try
		{
			foreach (ManifestLoaderDelegate manifestLoaderDelegate in ManifestLoaderDelegates)
			{
				IManifestHelper manifestHelper = manifestLoaderDelegate?.Invoke(assembly);
				if (manifestHelper != null)
				{
					return manifestHelper;
				}
			}
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, "Exception thrown by manifest loader delegate");
		}
		return null;
	}

	public static IManifestHelper LoadJsonManifestFrom(Assembly assembly)
	{
		try
		{
			ManifestResourceInfo manifestResourceInfo = assembly.GetManifestResourceInfo("manifest.json");
			if (manifestResourceInfo == null)
			{
				return null;
			}
			using Stream stream = assembly.GetManifestResourceStream("manifest.json");
			if (stream == null)
			{
				throw new InvalidDataException("Manifest resource stream null when info exists.");
			}
			using StreamReader streamReader = new StreamReader(stream);
			string value = streamReader.ReadToEnd();
			if (string.IsNullOrWhiteSpace(value))
			{
				throw new InvalidDataException("Manifest is empty or failed to load and is null.");
			}
			return JsonConvert.DeserializeObject<JsonManifest>(value);
		}
		catch (Exception innerException)
		{
			throw new Exception("Failed to load manifest.json from " + assembly.FullName + ".", innerException);
		}
	}

	internal static bool IsVirtualManifestType(Type type)
	{
		if (type.IsAbstract || type.IsInterface || type.IsGenericType)
		{
			return false;
		}
		if (!ManifestType.IsAssignableFrom(type))
		{
			Type type2 = type.GetInterfaces().FirstOrDefault((Type interfaceType) => string.Equals(ManifestType.FullName, interfaceType.FullName, StringComparison.Ordinal));
			if (type2 != null)
			{
				ApplicationContext.Context.Value?.Logger.LogDebug("Expected: " + ManifestType.AssemblyQualifiedName + " in " + ManifestType.Assembly.Location);
				ApplicationContext.Context.Value?.Logger.LogDebug("Loaded: " + type2.AssemblyQualifiedName + " in " + type2.Assembly.Location);
				if (!string.Equals(ManifestType.Assembly.Location, type2.Assembly.Location, StringComparison.Ordinal))
				{
					ApplicationContext.Context.Value?.Logger.LogWarning($"Manifest loaded the core library from the wrong location.\n\tExpected: {ManifestType.Assembly.Location}\n\t  Actual: {type2.Assembly.Location}");
				}
			}
			return false;
		}
		if (type.IsValueType)
		{
			return true;
		}
		ConstructorInfo constructor = type.GetConstructor(Array.Empty<Type>());
		if (constructor != null)
		{
			return true;
		}
		ApplicationContext.Context.Value?.Logger.LogDebug("'" + type.Name + "' is missing a default constructor.");
		return false;
	}

	public static IManifestHelper LoadVirtualManifestFrom(Assembly assembly)
	{
		ArgumentNullException.ThrowIfNull(assembly, "assembly");
		try
		{
			Type[] types = assembly.GetTypes();
			Type type = types.FirstOrDefault(IsVirtualManifestType);
			if (null != type)
			{
				return Activator.CreateInstance(type) as IManifestHelper;
			}
		}
		catch (Exception innerException)
		{
			Debugger.Break();
			throw new Exception("Failed to load virtual manifest from " + assembly.FullName + ".", innerException);
		}
		return null;
	}

	static ManifestLoader()
	{
		int num = 2;
		List<ManifestLoaderDelegate> list = new List<ManifestLoaderDelegate>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<ManifestLoaderDelegate> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = LoadJsonManifestFrom;
		num2++;
		span[num2] = LoadVirtualManifestFrom;
		num2++;
		ManifestLoaderDelegates = list;
	}
}
