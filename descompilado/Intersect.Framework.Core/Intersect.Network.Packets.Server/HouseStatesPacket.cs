using System;
using Intersect.Framework.Core.GameObjects.Housing;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class HouseStatesPacket : IntersectPacket
{
	[Key(0)]
	public HouseStateInfo[] States { get; set; } = Array.Empty<HouseStateInfo>();

	[Key(1)]
	public Guid FoldedCapsuleId { get; set; }

	public HouseStatesPacket()
	{
	}

	public HouseStatesPacket(HouseStateInfo[] states)
	{
		States = states;
	}
}
