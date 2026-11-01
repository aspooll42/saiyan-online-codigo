using MessagePack;

namespace Intersect.Network.Packets.Editor;

[MessagePackObject(false)]
public class AddTilesetsPacket : EditorPacket
{
	[Key(0)]
	public string[] Tilesets { get; set; }

	public AddTilesetsPacket()
	{
	}

	public AddTilesetsPacket(string[] tilesets)
	{
		Tilesets = tilesets;
	}
}
