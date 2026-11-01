using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class UpdateFriendsPacket : IntersectPacket
{
	[Key(0)]
	public string Name { get; set; }

	[Key(1)]
	public bool Adding { get; set; }

	public UpdateFriendsPacket()
	{
	}

	public UpdateFriendsPacket(string name, bool adding)
	{
		Name = name;
		Adding = adding;
	}
}
