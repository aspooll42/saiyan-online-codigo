using MessagePack;

namespace Intersect.Network.Packets.Editor;

[MessagePackObject(false)]
public class SaveTimeDataPacket : EditorPacket
{
	[Key(0)]
	public string TimeJson { get; set; }

	public SaveTimeDataPacket()
	{
	}

	public SaveTimeDataPacket(string timeJson)
	{
		TimeJson = timeJson;
	}
}
