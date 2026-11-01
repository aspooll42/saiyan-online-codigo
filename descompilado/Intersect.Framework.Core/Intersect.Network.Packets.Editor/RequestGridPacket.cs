using System;
using MessagePack;

namespace Intersect.Network.Packets.Editor;

[MessagePackObject(false)]
public class RequestGridPacket : EditorPacket
{
	[Key(0)]
	public Guid MapId { get; set; }

	public RequestGridPacket()
	{
	}

	public RequestGridPacket(Guid mapId)
	{
		MapId = mapId;
	}
}
