using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Editor;

[MessagePackObject(false)]
public class CreateGameObjectPacket : EditorPacket
{
	[Key(0)]
	public GameObjectType Type { get; set; }

	public CreateGameObjectPacket()
	{
	}

	public CreateGameObjectPacket(GameObjectType type)
	{
		Type = type;
	}
}
