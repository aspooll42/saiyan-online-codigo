using System;
using Intersect.Config;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class WantedContractEntryPacket : IntersectPacket
{
	[Key(0)]
	public WantedContractKind Kind { get; set; }

	[Key(1)]
	public Guid TargetId { get; set; }

	[Key(2)]
	public string Name { get; set; } = string.Empty;

	[Key(3)]
	public Guid MapId { get; set; }

	[Key(4)]
	public int Minutes { get; set; } = -1;

	[Key(5)]
	public bool Online { get; set; }

	[Key(6)]
	public long Zeni { get; set; }

	[Key(7)]
	public long Experience { get; set; }

	[Key(8)]
	public int Level { get; set; }

	[Key(9)]
	public string Place { get; set; } = string.Empty;

	[Key(10)]
	public int OfflineMinutes { get; set; } = -1;

	[Key(11)]
	public bool Completed { get; set; }
}
