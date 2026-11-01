using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class GameDataPacket : IntersectPacket
{
	[Key(0)]
	public GameObjectPacket[] GameObjects { get; set; }

	[Key(1)]
	public string ColorsJson { get; set; }

	public GameDataPacket()
	{
	}

	public GameDataPacket(GameObjectPacket[] gameObjects, string colorsJson)
	{
		GameObjects = gameObjects;
		ColorsJson = colorsJson;
	}
}
