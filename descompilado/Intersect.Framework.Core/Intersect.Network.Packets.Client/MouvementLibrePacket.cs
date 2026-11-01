using System;
using System.Collections.Generic;
using Intersect.Collections;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class MouvementLibrePacket : IntersectPacket
{
	[Key(0)]
	public byte Epoque { get; set; }

	[Key(1)]
	public ushort Sequence { get; set; }

	[Key(2)]
	public Guid MapId { get; set; }

	[Key(3)]
	public float X { get; set; }

	[Key(4)]
	public float Y { get; set; }

	[Key(5)]
	public Direction Direction { get; set; }

	[Key(6)]
	public bool EnMouvement { get; set; }

	public override Dictionary<string, SanitizedValue<object>> Sanitize()
	{
		Sanitizer sanitizer = default(Sanitizer);
		Direction = (Direction)sanitizer.Clamp("Direction", (int)Direction, 0, 7);
		if (!float.IsFinite(X))
		{
			X = sanitizer.Is("X", X, -100000f);
		}
		if (!float.IsFinite(Y))
		{
			Y = sanitizer.Is("Y", Y, -100000f);
		}
		return sanitizer.Sanitized;
	}
}
