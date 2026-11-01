using System;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Editor;

[MessagePackObject(false)]
public class MapListUpdatePacket : EditorPacket
{
	[Key(0)]
	public MapListUpdate UpdateType { get; set; }

	[Key(1)]
	public int TargetType { get; set; }

	[Key(2)]
	public Guid TargetId { get; set; }

	[Key(3)]
	public int ParentType { get; set; }

	[Key(4)]
	public Guid ParentId { get; set; }

	[Key(5)]
	public string Name { get; set; }

	public MapListUpdatePacket()
	{
	}

	public MapListUpdatePacket(MapListUpdate updateType, int targetType, Guid targetId, int parentType, Guid parentId, string name)
	{
		UpdateType = updateType;
		TargetType = targetType;
		TargetId = targetId;
		ParentType = parentType;
		ParentId = parentId;
		Name = name;
	}
}
