using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class EntityMovementPackets : IntersectPacket
{
	[Key(0)]
	public EntityMovePacket[] GlobalMovements { get; set; }

	[Key(1)]
	public EntityMovePacket[] LocalMovements { get; set; }

	public EntityMovementPackets()
	{
	}

	public EntityMovementPackets(EntityMovePacket[] globalMovements, EntityMovePacket[] localMovements)
	{
		GlobalMovements = globalMovements;
		LocalMovements = localMovements;
	}
}
