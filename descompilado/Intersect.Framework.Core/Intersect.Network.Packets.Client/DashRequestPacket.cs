using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class DashRequestPacket : IntersectPacket
{
	[Key(0)]
	public float SensX { get; set; }

	[Key(1)]
	public float SensY { get; set; }

	[Key(2)]
	public Guid CibleId { get; set; }

	public DashRequestPacket()
	{
	}

	public DashRequestPacket(float sensX, float sensY, Guid cibleId)
	{
		SensX = sensX;
		SensY = sensY;
		CibleId = cibleId;
	}
}
