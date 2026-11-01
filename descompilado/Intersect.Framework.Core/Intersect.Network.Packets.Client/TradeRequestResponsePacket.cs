using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class TradeRequestResponsePacket : IntersectPacket
{
	[Key(0)]
	public Guid TradeId { get; set; }

	[Key(1)]
	public bool AcceptingInvite { get; set; }

	public TradeRequestResponsePacket()
	{
	}

	public TradeRequestResponsePacket(Guid tradeId, bool accepting)
	{
		TradeId = tradeId;
		AcceptingInvite = accepting;
	}
}
