using System;
using Intersect.Network;
using MessagePack;

namespace Saiyan.Paquets;

[MessagePackObject(false)]
public class VoisinsPacket : IntersectPacket
{
	[Key(0)]
	public Guid Carte { get; set; }

	[Key(1)]
	public Guid[] Ids { get; set; } = Array.Empty<Guid>();

	public VoisinsPacket()
	{
	}

	public VoisinsPacket(Guid carte, Guid[] ids)
	{
		Carte = carte;
		Ids = ids;
	}
}
