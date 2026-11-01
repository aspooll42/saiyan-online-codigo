using Intersect.Admin.Actions;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class AdminActionPacket : IntersectPacket
{
	[Key(0)]
	public AdminAction Action { get; set; }

	public AdminActionPacket()
	{
	}

	public AdminActionPacket(AdminAction action)
	{
		Action = action;
	}
}
