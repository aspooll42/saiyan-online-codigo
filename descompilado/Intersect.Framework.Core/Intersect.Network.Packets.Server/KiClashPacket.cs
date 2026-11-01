using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class KiClashPacket : IntersectPacket
{
	[Key(0)]
	public Guid MapId { get; set; }

	[Key(1)]
	public float X { get; set; }

	[Key(2)]
	public float Y { get; set; }

	[Key(3)]
	public Guid FirstProjectileId { get; set; }

	[Key(4)]
	public int FirstSpawn { get; set; }

	[Key(5)]
	public Guid SecondProjectileId { get; set; }

	[Key(6)]
	public int SecondSpawn { get; set; }

	[Key(7)]
	public KiClashOutcome Outcome { get; set; }

	[Key(8)]
	public float WinnerStrength { get; set; }

	public KiClashPacket()
	{
	}

	public KiClashPacket(Guid mapId, float x, float y, Guid firstProjectileId, int firstSpawn, Guid secondProjectileId, int secondSpawn, KiClashOutcome outcome, float winnerStrength)
	{
		MapId = mapId;
		X = x;
		Y = y;
		FirstProjectileId = firstProjectileId;
		FirstSpawn = firstSpawn;
		SecondProjectileId = secondProjectileId;
		SecondSpawn = secondSpawn;
		Outcome = outcome;
		WinnerStrength = winnerStrength;
	}
}
