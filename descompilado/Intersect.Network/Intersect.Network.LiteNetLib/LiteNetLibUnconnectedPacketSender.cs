using System.Net;
using Intersect.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Network.LiteNetLib;

public sealed class LiteNetLibUnconnectedPacketSender : IPacketSender
{
	private readonly IPEndPoint _endPoint;

	private readonly LiteNetLibInterface _networkLayerInterface;

	public IApplicationContext ApplicationContext { get; }

	public INetwork Network { get; }

	public LiteNetLibUnconnectedPacketSender(LiteNetLibInterface networkLayerInterface, IPEndPoint endPoint, INetwork network)
	{
		_networkLayerInterface = networkLayerInterface;
		_endPoint = endPoint;
		ApplicationContext = network.ApplicationContext;
		Network = network;
	}

	public bool Send(IPacket packet)
	{
		if (packet is UnconnectedPacket packet2)
		{
			return _networkLayerInterface.SendUnconnectedPacket(_endPoint, packet2);
		}
		ApplicationContext.Logger.LogError("Expected a UnconnectedPacket but tried to send a " + packet.GetType().FullName);
		return false;
	}
}
