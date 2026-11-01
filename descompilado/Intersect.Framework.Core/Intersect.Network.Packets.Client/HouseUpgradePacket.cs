using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class HouseUpgradePacket : IntersectPacket
{
	[Key(0)]
	public Guid CapsuleId { get; set; }

	[Key(1)]
	public int OffsetX { get; set; } = -1;

	[Key(2)]
	public int OffsetY { get; set; } = -1;

	public HouseUpgradePacket()
	{
	}

	public HouseUpgradePacket(Guid capsuleId, int offsetX, int offsetY)
	{
		CapsuleId = capsuleId;
		OffsetX = offsetX;
		OffsetY = offsetY;
	}
}
