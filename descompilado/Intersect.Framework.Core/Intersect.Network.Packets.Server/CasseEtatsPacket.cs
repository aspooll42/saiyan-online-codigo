using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class CasseEtatsPacket : IntersectPacket
{
	[Key(0)]
	public Guid MapId { get; set; }

	[Key(1)]
	public CasseEtatPacket[] Etats { get; set; } = Array.Empty<CasseEtatPacket>();

	[Key(2)]
	public string Genere { get; set; } = string.Empty;

	[Key(3)]
	public CasseSillonPacket[] Sillons { get; set; } = Array.Empty<CasseSillonPacket>();
}
