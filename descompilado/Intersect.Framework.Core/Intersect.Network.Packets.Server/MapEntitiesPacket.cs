using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class MapEntitiesPacket : IntersectPacket
{
	[Key(0)]
	public EntityPacket[] MapEntities { get; set; }

	public MapEntitiesPacket()
	{
	}

	public MapEntitiesPacket(EntityPacket[] mapEntities)
	{
		MapEntities = mapEntities;
	}
}
