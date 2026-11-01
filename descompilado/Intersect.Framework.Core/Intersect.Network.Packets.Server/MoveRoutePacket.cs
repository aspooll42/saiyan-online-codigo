using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class MoveRoutePacket : IntersectPacket
{
	[Key(0)]
	public bool Active { get; set; }

	public MoveRoutePacket()
	{
	}

	public MoveRoutePacket(bool active)
	{
		Active = active;
	}
}
