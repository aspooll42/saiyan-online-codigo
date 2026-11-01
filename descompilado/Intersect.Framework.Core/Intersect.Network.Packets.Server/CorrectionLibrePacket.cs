using System;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class CorrectionLibrePacket : IntersectPacket
{
	[Key(0)]
	public byte Epoque { get; set; }

	[Key(1)]
	public Guid MapId { get; set; }

	[Key(2)]
	public float X { get; set; }

	[Key(3)]
	public float Y { get; set; }

	[Key(4)]
	public Direction Direction { get; set; }
}
