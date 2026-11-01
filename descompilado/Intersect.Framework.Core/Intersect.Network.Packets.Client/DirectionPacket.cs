using System.Collections.Generic;
using Intersect.Collections;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class DirectionPacket : IntersectPacket
{
	[Key(0)]
	public Direction Direction { get; set; }

	public DirectionPacket()
	{
	}

	public DirectionPacket(Direction dir)
	{
		Direction = dir;
	}

	public override Dictionary<string, SanitizedValue<object>> Sanitize()
	{
		Sanitizer sanitizer = default(Sanitizer);
		Direction = (Direction)sanitizer.Clamp("Direction", (int)Direction, 0, 7);
		return sanitizer.Sanitized;
	}
}
