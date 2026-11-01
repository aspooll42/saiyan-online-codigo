using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class WantedBoardPacket : IntersectPacket
{
	[Key(0)]
	public WantedCriminalEntryPacket[] Criminals { get; set; } = Array.Empty<WantedCriminalEntryPacket>();

	[Key(1)]
	public PoliceBountyEntryPacket[] Bounties { get; set; } = Array.Empty<PoliceBountyEntryPacket>();

	[Key(2)]
	public int Reputation { get; set; }

	[Key(3)]
	public int Contracts { get; set; }

	[Key(4)]
	public int MaxContracts { get; set; }

	[Key(5)]
	public int ResetInMinutes { get; set; }

	[Key(6)]
	public bool Behind { get; set; }

	[Key(7)]
	public Guid[] Saboteurs { get; set; } = Array.Empty<Guid>();
}
