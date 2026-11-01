using Intersect.Network;

namespace Intersect.Client.Framework.Plugins.Interfaces;

public interface IClientNetworkHelper
{
	bool IsConnected { get; }

	bool IsServerOnline { get; }

	IPacketSender PacketSender { get; }

	ConnectionStatistics Statistics { get; }
}
