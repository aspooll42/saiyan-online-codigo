using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class AchievementUnlockedPacket : IntersectPacket
{
	[Key(0)]
	public string Id { get; set; } = string.Empty;

	[Key(1)]
	public string Name { get; set; } = string.Empty;

	[Key(2)]
	public int Points { get; set; }

	public AchievementUnlockedPacket()
	{
	}

	public AchievementUnlockedPacket(string id, string name, int points)
	{
		Id = id;
		Name = name;
		Points = points;
	}
}
