using System;
using Intersect.Framework.Core.GameObjects.Dungeons;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class DungeonRunPacket : IntersectPacket
{
	[Key(0)]
	public Guid RunId { get; set; }

	[Key(1)]
	public Guid DungeonId { get; set; }

	[Key(2)]
	public int RoomIndex { get; set; } = -1;

	[Key(3)]
	public bool[] Cleared { get; set; } = Array.Empty<bool>();

	[Key(4)]
	public int BeansLeft { get; set; }

	[Key(5)]
	public int BeansMax { get; set; }

	[Key(6)]
	public long ElapsedMs { get; set; }

	[Key(7)]
	public int Remaining { get; set; }

	[Key(8)]
	public int Players { get; set; }

	[Key(9)]
	public bool Finished { get; set; }

	[Key(10)]
	public DungeonDifficulty Difficulty { get; set; }
}
