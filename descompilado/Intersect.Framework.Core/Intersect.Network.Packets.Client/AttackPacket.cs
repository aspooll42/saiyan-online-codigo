using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class AttackPacket : AbstractTimedPacket
{
	[Key(3)]
	public Guid Target { get; set; }

	[Key(4)]
	public bool Ruee { get; set; }

	[Key(5)]
	public GesteDeCoup Geste { get; set; }

	[Key(6)]
	public byte Coup { get; set; }

	[Key(7)]
	public int Cercle { get; set; }

	[Key(8)]
	public int EcartMs { get; set; }

	public AttackPacket()
	{
	}

	public AttackPacket(Guid target, bool ruee = false, GesteDeCoup geste = GesteDeCoup.Ordinaire, byte coup = 0)
	{
		Target = target;
		Ruee = ruee;
		Geste = geste;
		Coup = coup;
	}
}
