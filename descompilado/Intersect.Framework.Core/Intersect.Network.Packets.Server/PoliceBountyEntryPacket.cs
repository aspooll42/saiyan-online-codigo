using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class PoliceBountyEntryPacket : IntersectPacket
{
	[Key(0)]
	public Guid NpcId { get; set; }

	[Key(1)]
	public string Name { get; set; } = string.Empty;

	[Key(2)]
	public string Sprite { get; set; } = string.Empty;

	[Key(3)]
	public int Level { get; set; }

	[Key(4)]
	public Guid MapId { get; set; }

	[Key(5)]
	public long Zeni { get; set; }

	[Key(6)]
	public long Experience { get; set; }

	[Key(7)]
	public bool Done { get; set; }

	[Key(8)]
	public bool Taken { get; set; }

	[Key(9)]
	public string Place { get; set; } = string.Empty;
}
