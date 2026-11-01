using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class EchangePacket : IntersectPacket
{
	[Key(0)]
	public EchangeEvent Event { get; set; }

	[Key(1)]
	public Guid EntityId { get; set; }

	[Key(2)]
	public Guid AutreId { get; set; }

	[Key(3)]
	public int DureeMs { get; set; }

	[Key(4)]
	public int Valeur { get; set; }

	[Key(5)]
	public byte Index { get; set; }

	[Key(6)]
	public float X { get; set; }

	[Key(7)]
	public float Y { get; set; }

	[Key(8)]
	public int Valeur2 { get; set; }

	[Key(9)]
	public int Valeur3 { get; set; }

	public EchangePacket()
	{
	}

	public EchangePacket(EchangeEvent evenement, Guid entityId, Guid autreId = default(Guid), int dureeMs = 0, int valeur = 0, byte index = 0)
	{
		Event = evenement;
		EntityId = entityId;
		AutreId = autreId;
		DureeMs = dureeMs;
		Valeur = valeur;
		Index = index;
	}
}
