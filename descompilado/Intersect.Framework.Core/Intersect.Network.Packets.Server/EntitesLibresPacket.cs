using System;
using Intersect.Framework.Core.Entities;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class EntitesLibresPacket : IntersectPacket
{
	[Key(0)]
	public long Temps { get; set; }

	[Key(1)]
	public EntiteLibre[] Entites { get; set; } = Array.Empty<EntiteLibre>();
}
