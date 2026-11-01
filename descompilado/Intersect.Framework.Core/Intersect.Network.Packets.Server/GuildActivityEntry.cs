using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class GuildActivityEntry
{
	[Key(0)]
	public long TimeStamp;

	[Key(1)]
	public int Type;

	[Key(2)]
	public string Actor = string.Empty;

	[Key(3)]
	public string Target = string.Empty;

	[Key(4)]
	public string Meta = string.Empty;

	public GuildActivityEntry()
	{
	}

	public GuildActivityEntry(long timeStamp, int type, string actor, string target, string meta)
	{
		TimeStamp = timeStamp;
		Type = type;
		Actor = actor ?? string.Empty;
		Target = target ?? string.Empty;
		Meta = meta ?? string.Empty;
	}
}
