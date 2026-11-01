using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Editor;

[MessagePackObject(false)]
public class RequestOpenEditorPacket : EditorPacket
{
	[Key(0)]
	public GameObjectType Type { get; set; }

	public RequestOpenEditorPacket()
	{
	}

	public RequestOpenEditorPacket(GameObjectType type)
	{
		Type = type;
	}
}
