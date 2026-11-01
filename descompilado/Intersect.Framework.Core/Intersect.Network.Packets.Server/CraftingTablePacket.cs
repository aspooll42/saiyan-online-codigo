using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class CraftingTablePacket : IntersectPacket
{
	[Key(0)]
	public string TableData { get; set; }

	[Key(1)]
	public bool Close { get; set; }

	[Key(2)]
	public bool JournalMode { get; set; }

	public CraftingTablePacket()
	{
	}

	public CraftingTablePacket(string tableData, bool close, bool journalMode)
	{
		TableData = tableData;
		Close = close;
		JournalMode = journalMode;
	}
}
