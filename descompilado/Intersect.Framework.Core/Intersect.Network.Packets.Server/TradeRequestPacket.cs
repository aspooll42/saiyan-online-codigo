using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class TradeRequestPacket : IntersectPacket
{
	[Key(0)]
	public Guid PartnerId { get; set; }

	[Key(1)]
	public string PartnerName { get; set; }

	public TradeRequestPacket()
	{
	}

	public TradeRequestPacket(Guid partnerId, string partnerName)
	{
		PartnerId = partnerId;
		PartnerName = partnerName;
	}
}
