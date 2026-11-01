using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class HouseKnockAnswerPacket : IntersectPacket
{
	[Key(0)]
	public Guid VisitorId { get; set; }

	[Key(1)]
	public bool Open { get; set; }

	public HouseKnockAnswerPacket()
	{
	}

	public HouseKnockAnswerPacket(Guid visitorId, bool open)
	{
		VisitorId = visitorId;
		Open = open;
	}
}
