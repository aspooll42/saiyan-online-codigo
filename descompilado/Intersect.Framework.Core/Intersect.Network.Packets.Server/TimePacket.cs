using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class TimePacket : IntersectPacket
{
	[Key(0)]
	public DateTime Time { get; set; }

	[Key(1)]
	public float Rate { get; set; }

	[Key(2)]
	public Color Color { get; set; }

	public TimePacket()
	{
	}

	public TimePacket(DateTime time, float rate, Color color)
	{
		Time = time;
		Rate = rate;
		Color = color;
	}
}
