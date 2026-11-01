using System;
using Intersect.Config;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class CasseSillonPacket : IntersectPacket
{
	[Key(0)]
	public Guid MapId { get; set; }

	[Key(1)]
	public float X0 { get; set; }

	[Key(2)]
	public float Y0 { get; set; }

	[Key(3)]
	public float X1 { get; set; }

	[Key(4)]
	public float Y1 { get; set; }

	[Key(5)]
	public int AgeMs { get; set; }

	[Key(6)]
	public int VieMs { get; set; }

	[Key(7)]
	public float Demi { get; set; } = 40f;

	[Key(8)]
	public CasseTrace Genre { get; set; }

	[Key(9)]
	public int RevelerMs { get; set; }

	[Key(10)]
	public byte Courbe { get; set; }

	[Key(11)]
	public float K { get; set; }
}
