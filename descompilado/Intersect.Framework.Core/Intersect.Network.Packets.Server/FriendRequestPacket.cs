using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class FriendRequestPacket : IntersectPacket
{
	[Key(0)]
	public Guid FriendId { get; set; }

	[Key(1)]
	public string FriendName { get; set; }

	public FriendRequestPacket()
	{
	}

	public FriendRequestPacket(Guid friendId, string friendName)
	{
		FriendId = friendId;
		FriendName = friendName;
	}
}
