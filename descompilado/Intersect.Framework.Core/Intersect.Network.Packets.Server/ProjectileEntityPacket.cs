using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class ProjectileEntityPacket : EntityPacket
{
	[Key(24)]
	public Guid ProjectileId { get; set; }

	[Key(25)]
	public byte ProjectileDirection { get; set; }

	[Key(26)]
	public Guid TargetId { get; set; }

	[Key(27)]
	public Guid OwnerId { get; set; }

	[Key(28)]
	public int RangeBonus { get; set; }
}
