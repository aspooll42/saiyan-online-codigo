using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class SetActiveOrnamentPacket : IntersectPacket
{
	[Key(0)]
	public Guid OrnamentId { get; set; }

	public SetActiveOrnamentPacket()
	{
	}

	public SetActiveOrnamentPacket(Guid ornamentId)
	{
		OrnamentId = ornamentId;
	}
}
