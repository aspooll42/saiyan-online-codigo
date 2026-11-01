using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class TradeRequestPacket : IntersectPacket
{
	[Key(0)]
	public Guid TargetId { get; set; }

	public TradeRequestPacket()
	{
	}

	public TradeRequestPacket(Guid targetId)
	{
		TargetId = targetId;
	}
}
