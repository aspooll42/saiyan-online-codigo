using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class PartyInviteResponsePacket : IntersectPacket
{
	[Key(0)]
	public Guid PartyId { get; set; }

	[Key(1)]
	public bool AcceptingInvite { get; set; }

	public PartyInviteResponsePacket()
	{
	}

	public PartyInviteResponsePacket(Guid partyId, bool accepting)
	{
		PartyId = partyId;
		AcceptingInvite = accepting;
	}
}
