using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class AcceptTradePacket : IntersectPacket
{
	[Key(0)]
	public int Revision { get; set; }

	public AcceptTradePacket()
	{
	}

	public AcceptTradePacket(int revision)
	{
		Revision = revision;
	}
}
