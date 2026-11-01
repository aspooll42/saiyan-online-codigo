using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class VanishRequestPacket : IntersectPacket
{
	[Key(0)]
	public sbyte SensX { get; set; }

	[Key(1)]
	public sbyte SensY { get; set; }

	public VanishRequestPacket()
	{
	}

	public VanishRequestPacket(sbyte sensX, sbyte sensY)
	{
		SensX = sensX;
		SensY = sensY;
	}
}
