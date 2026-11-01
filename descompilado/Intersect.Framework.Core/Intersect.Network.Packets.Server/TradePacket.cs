using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class TradePacket : IntersectPacket
{
	[Key(0)]
	public string TradePartner { get; set; }

	public TradePacket()
	{
	}

	public TradePacket(string partnerName)
	{
		TradePartner = partnerName;
	}
}
