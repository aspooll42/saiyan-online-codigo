using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class ActionMsgPackets : IntersectPacket
{
	[Key(0)]
	public ActionMsgPacket[] Packets { get; set; }

	public ActionMsgPackets()
	{
	}

	public ActionMsgPackets(ActionMsgPacket[] packets)
	{
		Packets = packets;
	}
}
