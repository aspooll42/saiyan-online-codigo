using System;
using Intersect.Client.Framework.Plugins.Interfaces;
using Intersect.Client.MonoGame.Network;
using Intersect.Network;
using Intersect.Plugins.Interfaces;

namespace Intersect.Client.Plugins.Helpers;

public sealed class ClientNetworkHelper : IClientNetworkHelper
{
	private static IClient Client => MonoSocket.Instance.Network;

	private IPacketHelper PacketHelper { get; }

	public bool IsConnected => Client?.IsConnected ?? false;

	public bool IsServerOnline => Client?.IsServerOnline ?? false;

	public IPacketSender PacketSender { get; }

	public ConnectionStatistics Statistics => Client?.Connection?.Statistics;

	public ClientNetworkHelper(IPacketHelper packetHelper)
	{
		PacketHelper = packetHelper ?? throw new ArgumentNullException("packetHelper");
		PacketSender = new PluginPacketSender(PacketHelper);
	}
}
