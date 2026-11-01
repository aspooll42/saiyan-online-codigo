using System;
using Intersect.Framework.Core.GameObjects.Dungeons;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class DungeonStatusesPacket : IntersectPacket
{
	[Key(0)]
	public DungeonStatusInfo[] Statuses { get; set; } = Array.Empty<DungeonStatusInfo>();

	public DungeonStatusesPacket()
	{
	}

	public DungeonStatusesPacket(DungeonStatusInfo[] statuses)
	{
		Statuses = statuses;
	}
}
