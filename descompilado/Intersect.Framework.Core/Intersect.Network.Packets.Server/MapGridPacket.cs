using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class MapGridPacket : IntersectPacket
{
	[Key(0)]
	public Guid[,] Grid { get; set; }

	[Key(1)]
	public string[,] EditorGrid { get; set; }

	[Key(2)]
	public bool ClearKnownMaps { get; set; }

	public MapGridPacket()
	{
	}

	public MapGridPacket(Guid[,] grid, string[,] editorGrid, bool clearKnownMaps)
	{
		Grid = grid;
		EditorGrid = editorGrid;
		ClearKnownMaps = clearKnownMaps;
	}
}
