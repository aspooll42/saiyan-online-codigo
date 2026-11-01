using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class WantedCriminalEntryPacket : IntersectPacket
{
	[Key(0)]
	public Guid Id { get; set; }

	[Key(1)]
	public string Name { get; set; } = string.Empty;

	[Key(2)]
	public int Level { get; set; }

	[Key(3)]
	public string ClassName { get; set; } = string.Empty;

	[Key(4)]
	public string Sprite { get; set; } = string.Empty;

	[Key(5)]
	public string Face { get; set; } = string.Empty;

	[Key(6)]
	public long Bounty { get; set; }

	[Key(7)]
	public bool Online { get; set; }

	[Key(8)]
	public int OfflineMinutes { get; set; } = -1;

	[Key(9)]
	public Guid SightingMapId { get; set; }

	[Key(10)]
	public int SightingMinutes { get; set; } = -1;

	[Key(11)]
	public int Murders { get; set; }

	[Key(12)]
	public int Hunters { get; set; }

	[Key(13)]
	public bool Taken { get; set; }
}
