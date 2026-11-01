using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class BossMondialResultPacket : IntersectPacket
{
	[Key(0)]
	public string Nom { get; set; } = string.Empty;

	[Key(1)]
	public string Zone { get; set; } = string.Empty;

	[Key(2)]
	public bool Victoire { get; set; }

	[Key(3)]
	public long DureeMs { get; set; }

	[Key(4)]
	public int Joueurs { get; set; }

	[Key(5)]
	public string Difficulte { get; set; } = string.Empty;

	[Key(6)]
	public long Degats { get; set; }

	[Key(7)]
	public long Soins { get; set; }

	[Key(8)]
	public long Aggro { get; set; }

	[Key(9)]
	public long Points { get; set; }

	[Key(10)]
	public int Rang { get; set; }

	[Key(11)]
	public int Total { get; set; }

	[Key(12)]
	public int Jetons { get; set; }

	[Key(13)]
	public int Tirages { get; set; }

	[Key(14)]
	public string[] Butin { get; set; } = Array.Empty<string>();
}
