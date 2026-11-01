using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Intersect.Core;
using Intersect.Framework.Core.Serialization;
using Intersect.Plugins.Interfaces;
using Intersect.Properties;
using Intersect.Reflection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Intersect.Plugins.Loaders;

internal sealed class PluginLoader
{
	private sealed class PluginLoadContext(FileInfo pluginFileInfo) : AssemblyLoadContext()
	{
		private readonly AssemblyDependencyResolver _dependencyResolver = new AssemblyDependencyResolver(pluginFileInfo.FullName);

		protected override Assembly? Load(AssemblyName assemblyName)
		{
			string text = _dependencyResolver.ResolveAssemblyToPath(assemblyName);
			if (text != null)
			{
				try
				{
					return LoadFromAssemblyPath(text);
				}
				catch (Exception exception)
				{
					ApplicationContext.CurrentContext.Logger.LogError(exception, "Failed to resolve '{AssemblyName}' from plugin: {PluginPath}", assemblyName, Path.GetRelativePath(Environment.CurrentDirectory, pluginFileInfo.FullName));
				}
			}
			try
			{
				Assembly result = Assembly.Load(assemblyName);
				ApplicationContext.CurrentContext.Logger.LogDebug("Found '{AssemblyName}' using fallback resolution", assemblyName);
				return result;
			}
			catch (Exception exception2)
			{
				ApplicationContext.CurrentContext.Logger.LogError(exception2, "Failed to resolve '{AssemblyName}' using fallback resolution", assemblyName);
				return null;
			}
		}

		protected override nint LoadUnmanagedDll(string nativeLibraryName)
		{
			string text = _dependencyResolver.ResolveUnmanagedDllToPath(nativeLibraryName);
			if (text != null)
			{
				return LoadUnmanagedDllFromPath(text);
			}
			ApplicationContext.CurrentContext.Logger.LogError("Failed to resolve '{AssemblyName}' from plugin: {PluginPath}", nativeLibraryName, Path.GetRelativePath(Environment.CurrentDirectory, pluginFileInfo.FullName));
			return 0;
		}
	}

	internal Plugin LoadFrom(IApplicationContext applicationContext, string assemblyPath)
	{
		try
		{
			FileInfo pluginFileInfo = new FileInfo(assemblyPath);
			PluginLoadContext pluginLoadContext = new PluginLoadContext(pluginFileInfo);
			AssemblyName assemblyName = AssemblyName.GetAssemblyName(assemblyPath);
			Assembly assembly = pluginLoadContext.LoadFromAssemblyName(assemblyName);
			return LoadFrom(applicationContext, assembly);
		}
		catch (Exception exception)
		{
			applicationContext.Logger.LogError(exception, "Error loading plugin from {AssemblyPath}", assemblyPath);
			return null;
		}
	}

	internal Plugin LoadFrom(IApplicationContext applicationContext, Assembly assembly)
	{
		IManifestHelper manifestHelper = ManifestLoader.FindManifest(assembly);
		if (manifestHelper == null)
		{
			applicationContext.Logger.LogWarning($"Unable to find a manifest in '{assembly.FullName}' ({assembly.Location})");
			return null;
		}
		applicationContext.Logger.LogInformation($"Loading plugin {manifestHelper.Name} v{manifestHelper.Version} ({manifestHelper.Key}).");
		PluginReference pluginReference = CreatePluginReference(assembly);
		if (pluginReference != null)
		{
			return Plugin.Create(applicationContext, manifestHelper, pluginReference);
		}
		applicationContext.Logger.LogError($"Unable to find a plugin entry point in '{assembly.FullName}' ({assembly.Location})");
		return null;
	}

	internal void LoadConfigurations(IApplicationContext applicationContext, IEnumerable<Plugin> plugins)
	{
		plugins.ToList().ForEach(delegate(Plugin plugin)
		{
			if (plugin != null)
			{
				plugin.Configuration = LoadConfiguration(applicationContext, plugin);
			}
		});
	}

	internal PluginConfiguration LoadConfiguration(IApplicationContext applicationContext, Plugin plugin)
	{
		string configurationFile = plugin.Reference.ConfigurationFile;
		PluginConfiguration pluginConfiguration = Activator.CreateInstance(plugin.Reference.ConfigurationType) as PluginConfiguration;
		pluginConfiguration.IsEnabled = true;
		string text = null;
		try
		{
			try
			{
				if (File.Exists(configurationFile))
				{
					text = File.ReadAllText(configurationFile, Encoding.UTF8);
					pluginConfiguration = (JsonConvert.DeserializeObject(text, plugin.Reference.ConfigurationType, new JsonSerializerSettings
					{
						DefaultValueHandling = DefaultValueHandling.Include,
						SerializationBinder = new IntersectTypeSerializationBinder()
					}) as PluginConfiguration) ?? pluginConfiguration;
				}
			}
			catch (Exception exception)
			{
				applicationContext.Logger.LogWarning(exception, "Failed to load plugin configuration from '" + configurationFile + "', using default values.");
			}
			string text2 = JsonConvert.SerializeObject(pluginConfiguration, Formatting.Indented);
			string text3 = Path.Combine(Path.GetDirectoryName(configurationFile), $"{Path.GetFileNameWithoutExtension(configurationFile)}.{DateTime.Now:yyyyMMddHHmmss}.backup.json");
			try
			{
				if (text == null || !string.Equals(text, text2, StringComparison.Ordinal))
				{
					File.Copy(configurationFile, text3);
				}
			}
			catch (Exception exception2)
			{
				applicationContext.Logger.LogWarning(exception2, "Failed to backup existing configuration from " + configurationFile + " to " + text3);
			}
			File.WriteAllText(configurationFile, text2, Encoding.UTF8);
		}
		catch (Exception exception3)
		{
			applicationContext.Logger.LogWarning(exception3, "Failed to save plugin configuration to '" + configurationFile + "'.");
		}
		return pluginConfiguration;
	}

	internal IDictionary<string, Plugin> DiscoverPlugins(IApplicationContext applicationContext, IEnumerable<string> pluginDirectories)
	{
		IEnumerable<Plugin> enumerable = pluginDirectories.SelectMany((string pluginDirectory) => DiscoverPlugins(applicationContext, pluginDirectory) ?? Array.Empty<Plugin>());
		Dictionary<string, Plugin> dictionary = new Dictionary<string, Plugin>();
		foreach (Plugin item in enumerable)
		{
			string key = item.Manifest.Key;
			if (!dictionary.TryGetValue(item.Manifest.Key, out var value) || value == null || value.Manifest.Version.ComparePrecedenceTo(item.Manifest.Version) < 0)
			{
				dictionary[key] = item;
			}
		}
		return dictionary;
	}

	internal IEnumerable<Plugin> DiscoverPlugins(IApplicationContext applicationContext, string pluginDirectory)
	{
		if (Directory.Exists(pluginDirectory))
		{
			List<string> list = new List<string>();
			list.AddRange((from directoryPath in Directory.EnumerateDirectories(pluginDirectory)
				select new DirectoryInfo(directoryPath) into directoryInfo
				select Path.Combine(directoryInfo.FullName, directoryInfo.Name + ".dll")).Where(File.Exists));
			return from file in list
				select LoadFrom(applicationContext, file) into plugin
				where plugin != null
				select plugin;
		}
		applicationContext.Logger.LogWarning("Directory was specified as a plugin directory but does not exist: '" + pluginDirectory + "'");
		return null;
	}

	internal static bool IsPluginEntryType(Type type)
	{
		if (type.IsAbstract || type.IsInterface || type.IsGenericType)
		{
			return false;
		}
		if (!typeof(IPluginEntry).IsAssignableFrom(type))
		{
			return false;
		}
		ConstructorInfo constructor = type.GetConstructor(Array.Empty<Type>());
		return constructor != null;
	}

	internal static PluginReference CreatePluginReference(Assembly assembly)
	{
		Type[] types = assembly.GetTypes();
		Type type = types.FirstOrDefault(IsPluginEntryType);
		if (type == null)
		{
			throw new MissingPluginEntryException(assembly);
		}
		ConstructorInfo constructor = type.GetConstructor(Array.Empty<Type>());
		if (constructor == null)
		{
			throw new ArgumentException(ExceptionMessages.FoundPluginEntryTypeMissingDefaultConstructor, "assembly");
		}
		Type typeFromHandle = typeof(PluginConfiguration);
		Type configurationType = assembly.FindDefinedSubtypesOf(typeFromHandle).FirstOrDefault() ?? typeFromHandle;
		return new PluginReference(assembly, configurationType, type);
	}
}
