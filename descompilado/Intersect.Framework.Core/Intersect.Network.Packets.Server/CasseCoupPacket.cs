using System;
using Intersect.Config;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class CasseCoupPacket : IntersectPacket
{
	[Key(0)]
	public Guid MapId { get; set; }

	[Key(1)]
	public int Objet { get; set; }

	[Key(2)]
	public int Partie { get; set; }

	[Key(3)]
	public CasseCoup Coup { get; set; }

	[Key(4)]
	public float X { get; set; }

	[Key(5)]
	public float Y { get; set; }

	[Key(6)]
	public CasseEtat Etat { get; set; }

	[Key(7)]
	public int Points { get; set; }

	[Key(8)]
	public int PointsMax { get; set; }

	[Key(9)]
	public int Graine { get; set; }

	[Key(10)]
	public int DelaiMs { get; set; }

	[Key(11)]
	public bool Reflet { get; set; }

	[Key(12)]
	public float Dx { get; set; }

	[Key(13)]
	public float Dy { get; set; }

	[Key(14)]
	public sbyte Couche { get; set; }
}
