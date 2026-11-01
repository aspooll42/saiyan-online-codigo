using Intersect.Client.Framework.Audio;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Input;
using Intersect.Client.Framework.Maps;
using Intersect.Client.Framework.Plugins.Interfaces;
using Intersect.Client.Plugins.Interfaces;
using Intersect.Plugins;

namespace Intersect.Client.Plugins;

public interface IClientPluginContext : IPluginContext<IClientPluginContext, IClientLifecycleHelper>, IPluginContext<IClientPluginContext>, IPluginContext, IPluginBaseContext
{
	IContentManager ContentManager { get; }

	IClientNetworkHelper Network { get; }

	IGameRenderer Graphics { get; }

	IAudioManager Audio { get; }

	IGameInput Input { get; }

	IMapGrid MapGrid { get; }

	Options Options { get; }
}
