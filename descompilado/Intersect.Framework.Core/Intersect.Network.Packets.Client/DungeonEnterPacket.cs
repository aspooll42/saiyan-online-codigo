using System;
using Intersect.Framework.Core.GameObjects.Dungeons;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class DungeonEnterPacket : IntersectPacket
{
	[Key(0)]
	public Guid DungeonId { get; set; }

	[Key(1)]
	public DungeonDifficulty Difficulty { get; set; }

	public DungeonEnterPacket()
	{
	}

	public DungeonEnterPacket(Guid dungeonId, DungeonDifficulty difficulty)
	{
		DungeonId = dungeonId;
		Difficulty = difficulty;
	}
}
