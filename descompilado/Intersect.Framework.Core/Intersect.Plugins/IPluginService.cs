using System;
using System.Collections.Generic;
using Intersect.Core;

namespace Intersect.Plugins;

public interface IPluginService : IApplicationService, IDisposable
{
	IPlugin this[string pluginKey] { get; }

	List<string> PluginDirectories { get; }

	bool IsPluginEnabled(string pluginKey);

	bool EnablePlugin(string pluginKey);

	bool DisablePlugin(string pluginKey);
}
