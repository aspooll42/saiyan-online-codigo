using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class PartyKickPacket : IntersectPacket
{
	[Key(0)]
	public Guid TargetId { get; set; }

	public PartyKickPacket()
	{
	}

	public PartyKickPacket(Guid targetId)
	{
		TargetId = targetId;
	}
}
