using System;
using System.Collections.Generic;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class ProjectileDeadPacket : IntersectPacket
{
	[Key(0)]
	public Guid[] ProjectileDeaths { get; set; }

	[Key(1)]
	public KeyValuePair<Guid, int>[] SpawnDeaths { get; set; }

	[Key(2)]
	public Guid[]? SpawnHits { get; set; }

	public ProjectileDeadPacket()
	{
	}

	public ProjectileDeadPacket(Guid[] projectileDeaths, KeyValuePair<Guid, int>[] spawnIndices, Guid[]? spawnHits = null)
	{
		ProjectileDeaths = projectileDeaths;
		SpawnDeaths = spawnIndices;
		SpawnHits = spawnHits;
	}
}
