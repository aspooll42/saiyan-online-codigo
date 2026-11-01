using System;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Editor;

[MessagePackObject(false)]
public class DeleteGameObjectPacket : EditorPacket
{
	[Key(0)]
	public GameObjectType Type { get; set; }

	[Key(1)]
	public Guid Id { get; set; }

	public DeleteGameObjectPacket()
	{
	}

	public DeleteGameObjectPacket(GameObjectType type, Guid id)
	{
		Type = type;
		Id = id;
	}
}
