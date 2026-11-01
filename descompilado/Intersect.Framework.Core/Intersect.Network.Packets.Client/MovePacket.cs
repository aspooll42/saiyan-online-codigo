using System;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class MovePacket : AbstractTimedPacket
{
	[Key(3)]
	public Guid MapId { get; set; }

	[Key(4)]
	public byte X { get; set; }

	[Key(5)]
	public byte Y { get; set; }

	[Key(6)]
	public Direction Dir { get; set; }

	public MovePacket()
	{
	}

	public MovePacket(Guid mapId, byte x, byte y, Direction dir)
	{
		MapId = mapId;
		X = x;
		Y = y;
		Dir = dir;
	}
}
