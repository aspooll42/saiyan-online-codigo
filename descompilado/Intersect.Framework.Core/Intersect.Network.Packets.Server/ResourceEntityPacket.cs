using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class ResourceEntityPacket : EntityPacket
{
	[Key(24)]
	public Guid ResourceId { get; set; }

	[Key(25)]
	public bool IsDead { get; set; }
}
