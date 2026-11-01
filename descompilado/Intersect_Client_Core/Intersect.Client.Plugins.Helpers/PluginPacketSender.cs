using System;
using System.Linq;
using Intersect.Client.Networking;
using Intersect.Core;
using Intersect.Network;
using Intersect.Plugins.Interfaces;

namespace Intersect.Client.Plugins.Helpers;

public sealed class PluginPacketSender : IPacketSender
{
	private readonly IPacketHelper mPluginPluginPacketHelper;

	private static IPacketSender? VirtualSender => Intersect.Client.Networking.Network.PacketHandler?.VirtualSender;

	public IApplicationContext ApplicationContext => VirtualSender?.ApplicationContext;

	public INetwork Network => VirtualSender?.Network;

	public PluginPacketSender(IPacketHelper pluginPacketHelper)
	{
		mPluginPluginPacketHelper = pluginPacketHelper;
	}

	public bool Send(IPacket packet)
	{
		if (packet == null)
		{
			throw new ArgumentNullException("packet");
		}
		Type type = packet.GetType();
		if (mPluginPluginPacketHelper.AllPluginPacketTypes.Contains(type))
		{
			return VirtualSender?.Send(packet) ?? false;
		}
		return false;
	}
}
