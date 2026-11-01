using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Intersect.Core;
using Intersect.Factories;
using Intersect.Plugins.Loaders;
using Microsoft.Extensions.Logging;

namespace Intersect.Plugins;

internal sealed class PluginService : ApplicationService<IPluginService, PluginService>, IPluginService, IApplicationService, IDisposable
{
	private static readonly string BuiltInPluginDirectory = Path.Combine("resources", "plugins");

	private ConcurrentDictionary<string, Plugin> Plugins { get; }

	private ConcurrentDictionary<Plugin, PluginInstance> Instances { get; }

	private PluginLoader Loader { get; }

	public override bool IsEnabled => true;

	public IPlugin this[string pluginKey] => Plugins.GetValueOrDefault(pluginKey);

	public List<string> PluginDirectories { get; }

	public PluginService()
	{
		if (FactoryRegistry<IPluginBootstrapContext>.Factory == null)
		{
			throw new InvalidOperationException("Factory has not been registered for IPluginBootstrapContext.");
		}
		if (FactoryRegistry<IPluginContext>.Factory == null)
		{
			throw new InvalidOperationException("Factory has not been registered for IPluginContext.");
		}
		PluginDirectories = new List<string> { BuiltInPluginDirectory };
		Plugins = new ConcurrentDictionary<string, Plugin>();
		Instances = new ConcurrentDictionary<Plugin, PluginInstance>();
		Loader = new PluginLoader();
	}

	public override bool Bootstrap(IApplicationContext applicationContext)
	{
		try
		{
			PluginDirectories.AddRange(applicationContext.StartupOptions.PluginDirectories ?? Array.Empty<string>());
			applicationContext.Logger.LogDebug("Scanning for plugins in {PluginDirectoryCount} directories:\n{PluginDirectories}", PluginDirectories.Count, string.Join('\n', PluginDirectories.Select((string d) => "\t" + d)));
			IDictionary<string, Plugin> dictionary = Loader.DiscoverPlugins(applicationContext, PluginDirectories);
			applicationContext.Logger.LogInformation("Discovered {PluginCount} plugins:\n{Plugins}", dictionary.Count, string.Join('\n', dictionary.Select((KeyValuePair<string, Plugin> p) => "\t" + p.Key)));
			Loader.LoadConfigurations(applicationContext, dictionary.Values);
			RegisterPlugins(dictionary);
			CreateInstances();
			RunOnAllInstances(applicationContext, OnBootstrap);
			return true;
		}
		catch (Exception innerException)
		{
			throw new ServiceLifecycleFailureException(ServiceLifecycleStage.Bootstrap, base.Name, innerException);
		}
	}

	protected override void TaskStart(IApplicationContext applicationContext)
	{
		RunOnAllInstances(applicationContext, OnStart);
	}

	protected override void TaskStop(IApplicationContext applicationContext)
	{
		RunOnAllInstances(applicationContext, OnStop);
		Instances.Clear();
		Plugins.Clear();
	}

	public bool IsPluginEnabled(string pluginKey)
	{
		return this[pluginKey]?.IsEnabled ?? false;
	}

	public bool EnablePlugin(string pluginKey)
	{
		IPlugin plugin = this[pluginKey];
		if (plugin == null || plugin.IsEnabled)
		{
			return plugin?.IsEnabled ?? false;
		}
		return plugin.IsEnabled = true;
	}

	public bool DisablePlugin(string pluginKey)
	{
		IPlugin plugin = this[pluginKey];
		if (plugin == null || !plugin.IsEnabled)
		{
			return !(plugin?.IsEnabled ?? false);
		}
		plugin.IsEnabled = false;
		return true;
	}

	private void RegisterPlugins(IDictionary<string, Plugin> plugins)
	{
		foreach (KeyValuePair<string, Plugin> plugin in plugins)
		{
			if (!Plugins.TryAdd(plugin.Key, plugin.Value))
			{
				throw new Exception("Failed to register plugin: " + plugin.Key);
			}
		}
	}

	private void CreateInstances()
	{
		foreach (KeyValuePair<string, Plugin> plugin in Plugins)
		{
			Plugin value = plugin.Value;
			PluginInstance value2 = PluginInstance.Create(value);
			if (!Instances.TryAdd(value, value2))
			{
				throw new Exception("Failed to add plugin instance: " + value.Key);
			}
		}
	}

	private void RunOnAllInstances(IApplicationContext applicationContext, Action<KeyValuePair<Plugin, PluginInstance>> action)
	{
		Instances.Where((KeyValuePair<Plugin, PluginInstance> instance) => instance.Key?.IsEnabled ?? false).ToList().ForEach(delegate(KeyValuePair<Plugin, PluginInstance> pair)
		{
			try
			{
				action(pair);
			}
			catch (Exception exception)
			{
				applicationContext.Logger.LogError(exception, $"Failed to invoke {action.Method?.Name} for {pair.Key.Key}.");
			}
		});
	}

	private void OnBootstrap(KeyValuePair<Plugin, PluginInstance> instancePair)
	{
		PluginInstance value = instancePair.Value;
		IPluginEntry entry = value.Entry;
		entry.OnBootstrap(value.BootstrapContext);
	}

	private void OnStart(KeyValuePair<Plugin, PluginInstance> instancePair)
	{
		PluginInstance value = instancePair.Value;
		IPluginEntry entry = value.Entry;
		entry.OnStart(value.Context);
	}

	private void OnStop(KeyValuePair<Plugin, PluginInstance> instancePair)
	{
		PluginInstance value = instancePair.Value;
		IPluginEntry entry = value.Entry;
		entry.OnStop(value.Context);
	}
}
