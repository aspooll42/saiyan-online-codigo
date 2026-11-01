using System;
using Intersect.Framework.Core.GameObjects.Dungeons;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class DungeonTelegraphPacket : IntersectPacket
{
	[Key(0)]
	public Guid MapId { get; set; }

	[Key(1)]
	public Guid BossId { get; set; }

	[Key(2)]
	public byte Kind { get; set; }

	[Key(3)]
	public int DurationMs { get; set; }

	[Key(4)]
	public DungeonTelegraphShape[] Shapes { get; set; } = Array.Empty<DungeonTelegraphShape>();

	[Key(5)]
	public Guid TargetId { get; set; }

	[Key(6)]
	public bool Locked { get; set; }
}
