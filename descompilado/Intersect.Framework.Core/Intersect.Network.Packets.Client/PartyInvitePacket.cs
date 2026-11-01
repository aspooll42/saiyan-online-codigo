using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class PartyInvitePacket : IntersectPacket
{
	[Key(0)]
	public Guid TargetId { get; set; }

	[Key(1)]
	public string Target { get; set; }

	public PartyInvitePacket()
	{
	}

	public PartyInvitePacket(Guid targetId)
	{
		TargetId = targetId;
	}

	public PartyInvitePacket(string target)
	{
		Target = target;
	}
}
