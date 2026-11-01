using System;
using MessagePack;

namespace Intersect.Network.Packets.Editor;

[MessagePackObject(false)]
public class EnterMapPacket : EditorPacket
{
	[Key(0)]
	public Guid MapId { get; set; }

	public EnterMapPacket()
	{
	}

	public EnterMapPacket(Guid mapId)
	{
		MapId = mapId;
	}
}
