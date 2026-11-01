using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class HouseShowOutPacket : IntersectPacket
{
	[Key(0)]
	public Guid VisitorId { get; set; }

	public HouseShowOutPacket()
	{
	}

	public HouseShowOutPacket(Guid visitorId)
	{
		VisitorId = visitorId;
	}
}
