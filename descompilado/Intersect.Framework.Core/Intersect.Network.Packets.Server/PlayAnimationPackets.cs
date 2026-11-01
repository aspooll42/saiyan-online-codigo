using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class PlayAnimationPackets : IntersectPacket
{
	[Key(0)]
	public PlayAnimationPacket[] Packets { get; set; }

	public PlayAnimationPackets()
	{
	}

	public PlayAnimationPackets(PlayAnimationPacket[] packets)
	{
		Packets = packets;
	}
}
