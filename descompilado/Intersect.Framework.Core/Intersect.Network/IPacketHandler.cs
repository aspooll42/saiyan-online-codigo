using System;
using Intersect.Framework.Reflection;

namespace Intersect.Network;

public interface IPacketHandler
{
	bool Handle(IPacketSender packetSender, IPacket packet);
}
public interface IPacketHandler<in TPacket> : IPacketHandler where TPacket : class, IPacket
{
	bool IPacketHandler.Handle(IPacketSender packetSender, IPacket packet)
	{
		if (packet is TPacket packet2)
		{
			return Handle(packetSender, packet2);
		}
		string name = typeof(TPacket).GetName(qualified: true);
		string name2 = packet.GetType().GetName(qualified: true);
		throw new ArgumentException("Expected a " + name + " but got a " + name2, "packet");
	}

	bool Handle(IPacketSender packetSender, TPacket packet);
}
