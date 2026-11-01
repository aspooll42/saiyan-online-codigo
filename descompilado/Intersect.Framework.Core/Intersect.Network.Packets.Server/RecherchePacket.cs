using Intersect.Config;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class RecherchePacket : IntersectPacket
{
	[Key(0)]
	public float Chaleur { get; set; }

	[Key(1)]
	public byte Cran { get; set; }

	[Key(2)]
	public RechercheEtat Etat { get; set; }

	[Key(3)]
	public int Temoins { get; set; }

	[Key(4)]
	public long Montant { get; set; }

	[Key(5)]
	public float Gain { get; set; }

	[Key(6)]
	public CasseCarteGenre Lieu { get; set; }
}
