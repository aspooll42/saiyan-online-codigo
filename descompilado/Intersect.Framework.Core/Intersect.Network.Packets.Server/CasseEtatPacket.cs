using Intersect.Config;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class CasseEtatPacket : IntersectPacket
{
	[Key(0)]
	public int Objet { get; set; }

	[Key(1)]
	public int Partie { get; set; }

	[Key(2)]
	public CasseEtat Etat { get; set; }

	[Key(3)]
	public int Points { get; set; }

	[Key(4)]
	public int PointsMax { get; set; }

	[Key(5)]
	public int FeuMs { get; set; }

	[Key(6)]
	public bool Tranche { get; set; }

	[Key(7)]
	public sbyte Couche { get; set; }
}
