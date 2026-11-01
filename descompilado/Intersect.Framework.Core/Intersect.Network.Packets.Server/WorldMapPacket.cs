using System;
using Intersect.Framework.Core.GameObjects.WorldMap;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class WorldMapPacket : IntersectPacket
{
	[Key(0)]
	public WorldMapDestination[] Destinations { get; set; } = Array.Empty<WorldMapDestination>();

	[Key(1)]
	public Guid[] VisitedMaps { get; set; } = Array.Empty<Guid>();

	[Key(2)]
	public bool RetourDEnma { get; set; }

	public WorldMapPacket()
	{
	}

	public WorldMapPacket(WorldMapDestination[] destinations, Guid[] visitedMaps, bool retourDEnma = false)
	{
		Destinations = destinations;
		VisitedMaps = visitedMaps;
		RetourDEnma = retourDEnma;
	}
}
