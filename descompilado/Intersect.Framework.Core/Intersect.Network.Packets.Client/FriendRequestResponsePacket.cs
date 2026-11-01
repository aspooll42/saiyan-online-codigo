using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class FriendRequestResponsePacket : IntersectPacket
{
	[Key(0)]
	public Guid FriendId { get; set; }

	[Key(1)]
	public bool AcceptingRequest { get; set; }

	public FriendRequestResponsePacket()
	{
	}

	public FriendRequestResponsePacket(Guid friendId, bool accepting)
	{
		FriendId = friendId;
		AcceptingRequest = accepting;
	}
}
