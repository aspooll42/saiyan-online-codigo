using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class BlockPacket : IntersectPacket
{
	[Key(0)]
	public bool Blocking { get; set; }

	public BlockPacket()
	{
	}

	public BlockPacket(bool blocking)
	{
		Blocking = blocking;
	}
}
