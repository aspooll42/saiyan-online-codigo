using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class MetierNiveauPacket : IntersectPacket
{
	[Key(0)]
	public Guid EntityId { get; set; }

	[Key(1)]
	public int Metier { get; set; }

	[Key(2)]
	public int Niveau { get; set; }

	public MetierNiveauPacket()
	{
	}

	public MetierNiveauPacket(Guid entityId, int metier, int niveau)
	{
		EntityId = entityId;
		Metier = metier;
		Niveau = niveau;
	}
}
