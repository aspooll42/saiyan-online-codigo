using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class PlayerDeathPacket : IntersectPacket
{
	[Key(0)]
	public Guid PlayerId { get; set; }

	[Key(1)]
	public byte Etape { get; set; }

	public PlayerDeathPacket()
	{
	}

	public PlayerDeathPacket(Guid playerId, byte etape = 0)
	{
		PlayerId = playerId;
		Etape = etape;
	}
}
