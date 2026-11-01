using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class WantedTurnInPacket : IntersectPacket
{
	[Key(0)]
	public WantedTurnInEntryPacket[] Entries { get; set; } = Array.Empty<WantedTurnInEntryPacket>();

	[Key(1)]
	public int Reputation { get; set; }
}
