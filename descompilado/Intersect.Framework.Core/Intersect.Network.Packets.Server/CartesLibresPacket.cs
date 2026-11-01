using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class CartesLibresPacket : IntersectPacket
{
	[Key(0)]
	public Guid[] Cartes { get; set; } = Array.Empty<Guid>();

	[Key(1)]
	public Guid[] CartesMonstres { get; set; } = Array.Empty<Guid>();

	public CartesLibresPacket()
	{
	}

	public CartesLibresPacket(Guid[] cartes, Guid[] cartesMonstres)
	{
		Cartes = cartes;
		CartesMonstres = cartesMonstres;
	}
}
