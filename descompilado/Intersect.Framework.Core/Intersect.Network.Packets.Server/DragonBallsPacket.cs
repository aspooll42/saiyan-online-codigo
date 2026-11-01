using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class DragonBallsPacket : IntersectPacket
{
	[Key(0)]
	public bool[] Owned { get; set; } = Array.Empty<bool>();

	public DragonBallsPacket()
	{
	}

	public DragonBallsPacket(bool[] owned)
	{
		Owned = owned;
	}
}
