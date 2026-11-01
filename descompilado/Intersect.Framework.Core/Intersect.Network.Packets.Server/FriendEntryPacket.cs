using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class FriendEntryPacket : IntersectPacket
{
	[Key(0)]
	public string Name { get; set; } = string.Empty;

	[Key(1)]
	public bool Online { get; set; }

	[Key(2)]
	public string Map { get; set; } = string.Empty;

	[Key(3)]
	public string Sprite { get; set; } = string.Empty;

	[Key(4)]
	public string Face { get; set; } = string.Empty;

	[Key(5)]
	public int Level { get; set; }

	[Key(6)]
	public int LastSeenMinutes { get; set; } = -1;

	public FriendEntryPacket()
	{
	}

	public FriendEntryPacket(string name, bool online, string map, string sprite, string face, int level, int lastSeenMinutes)
	{
		Name = name;
		Online = online;
		Map = map;
		Sprite = sprite;
		Face = face;
		Level = level;
		LastSeenMinutes = lastSeenMinutes;
	}
}
