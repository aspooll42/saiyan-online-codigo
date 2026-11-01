using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class OpenEditorPacket : IntersectPacket
{
	[Key(0)]
	public GameObjectType Type { get; set; }

	public OpenEditorPacket()
	{
	}

	public OpenEditorPacket(GameObjectType type)
	{
		Type = type;
	}
}
