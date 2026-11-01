using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class WorldMapTeleportPacket : IntersectPacket
{
	[Key(0)]
	public string DestinationId { get; set; } = string.Empty;

	public WorldMapTeleportPacket()
	{
	}

	public WorldMapTeleportPacket(string destinationId)
	{
		DestinationId = destinationId;
	}
}
