using System;
using MessagePack;

namespace Intersect.Network.Packets.Editor;

[MessagePackObject(false)]
public class MapUpdatePacket : EditorPacket
{
	[Key(0)]
	public Guid MapId { get; set; }

	[Key(1)]
	public string JsonData { get; set; }

	[Key(2)]
	public byte[] TileData { get; set; }

	[Key(3)]
	public byte[] AttributeData { get; set; }

	public MapUpdatePacket()
	{
	}

	public MapUpdatePacket(Guid mapId, string jsonData, byte[] tileData, byte[] attributeData)
	{
		MapId = mapId;
		JsonData = jsonData;
		TileData = tileData;
		AttributeData = attributeData;
	}
}
