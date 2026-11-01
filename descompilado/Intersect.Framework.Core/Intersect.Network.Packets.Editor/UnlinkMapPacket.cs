using System;
using MessagePack;

namespace Intersect.Network.Packets.Editor;

[MessagePackObject(false)]
public class UnlinkMapPacket : EditorPacket
{
	[Key(0)]
	public Guid MapId { get; set; }

	[Key(1)]
	public Guid CurrentMapId { get; set; }

	public UnlinkMapPacket()
	{
	}

	public UnlinkMapPacket(Guid unlinkMapId, Guid currentMapId)
	{
		MapId = unlinkMapId;
		CurrentMapId = currentMapId;
	}
}
