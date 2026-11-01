using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class PartyInvitePacket : IntersectPacket
{
	[Key(0)]
	public string LeaderName { get; set; }

	[Key(1)]
	public Guid LeaderId { get; set; }

	public PartyInvitePacket()
	{
	}

	public PartyInvitePacket(string leaderName, Guid leaderId)
	{
		LeaderName = leaderName;
		LeaderId = leaderId;
	}
}
