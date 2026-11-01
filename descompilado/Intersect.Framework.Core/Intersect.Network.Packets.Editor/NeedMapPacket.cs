using System;
using MessagePack;

namespace Intersect.Network.Packets.Editor;

[MessagePackObject(false)]
public class NeedMapPacket : EditorPacket
{
	[Key(0)]
	public Guid MapId { get; set; }

	public NeedMapPacket()
	{
	}

	public NeedMapPacket(Guid mapId)
	{
		MapId = mapId;
	}
}
