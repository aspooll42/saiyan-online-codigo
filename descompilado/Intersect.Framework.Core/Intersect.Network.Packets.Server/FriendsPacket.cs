using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class FriendsPacket : IntersectPacket
{
	[Key(0)]
	public FriendEntryPacket[] Friends { get; set; } = Array.Empty<FriendEntryPacket>();

	public FriendsPacket()
	{
	}

	public FriendsPacket(FriendEntryPacket[] friends)
	{
		Friends = friends;
	}
}
