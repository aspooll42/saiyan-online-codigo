using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class BossMondialPacket : IntersectPacket
{
	[Key(0)]
	public int Etat { get; set; }

	[Key(1)]
	public string Nom { get; set; } = string.Empty;

	[Key(2)]
	public string Zone { get; set; } = string.Empty;

	[Key(3)]
	public Guid CarteId { get; set; }

	[Key(4)]
	public long RestantMs { get; set; }

	[Key(5)]
	public long Vie { get; set; }

	[Key(6)]
	public long VieMax { get; set; } = 1L;

	[Key(7)]
	public int Joueurs { get; set; }

	[Key(8)]
	public string Difficulte { get; set; } = string.Empty;

	[Key(9)]
	public int Cristaux { get; set; }

	[Key(10)]
	public int CristauxTotal { get; set; }

	[Key(11)]
	public int Phase { get; set; }

	[Key(12)]
	public string Annonce { get; set; } = string.Empty;

	[Key(13)]
	public string Sous { get; set; } = string.Empty;
}
