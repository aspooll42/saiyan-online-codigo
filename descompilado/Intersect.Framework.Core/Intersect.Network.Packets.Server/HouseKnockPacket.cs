using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class HouseKnockPacket : IntersectPacket
{
	[Key(0)]
	public Guid VisitorId { get; set; }

	[Key(1)]
	public string VisitorName { get; set; } = string.Empty;

	public HouseKnockPacket()
	{
	}

	public HouseKnockPacket(Guid visitorId, string visitorName)
	{
		VisitorId = visitorId;
		VisitorName = visitorName;
	}
}
