namespace Intersect.Network;

public abstract class AbstractPacketHandler<TPacket> : IPacketHandler<TPacket>, IPacketHandler where TPacket : class, IPacket
{
	public abstract bool Handle(IPacketSender packetSender, TPacket packet);

	public virtual bool Handle(IPacketSender packetSender, IPacket packet)
	{
		if (packet is TPacket packet2)
		{
			return Handle(packetSender, packet2);
		}
		return false;
	}
}
