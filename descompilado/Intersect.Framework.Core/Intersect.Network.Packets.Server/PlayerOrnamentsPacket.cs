using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class PlayerOrnamentsPacket : IntersectPacket
{
	[Key(0)]
	public Guid[] OrnamentIds { get; set; } = Array.Empty<Guid>();

	[Key(1)]
	public Guid ActiveOrnamentId { get; set; }

	public PlayerOrnamentsPacket()
	{
	}

	public PlayerOrnamentsPacket(Guid[] ornamentIds, Guid activeOrnamentId)
	{
		OrnamentIds = ornamentIds;
		ActiveOrnamentId = activeOrnamentId;
	}
}
