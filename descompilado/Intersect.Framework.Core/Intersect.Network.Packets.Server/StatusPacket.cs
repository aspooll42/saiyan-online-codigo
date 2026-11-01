using System;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class StatusPacket : IntersectPacket
{
	[Key(0)]
	public Guid SpellId { get; set; }

	[Key(1)]
	public SpellEffect Type { get; set; }

	[Key(2)]
	public string TransformSprite { get; set; }

	[Key(3)]
	public long TimeRemaining { get; set; }

	[Key(4)]
	public long TotalDuration { get; set; }

	[Key(5)]
	public long[] VitalShields { get; set; }

	[Key(6)]
	public int Sequence { get; set; }

	public StatusPacket()
	{
	}

	public StatusPacket(Guid spellId, SpellEffect type, string transformSprite, long timeRemaining, long totalDuration, long[] vitalShields, int sequence = 0)
	{
		SpellId = spellId;
		Type = type;
		TransformSprite = transformSprite;
		TimeRemaining = timeRemaining;
		TotalDuration = totalDuration;
		VitalShields = vitalShields;
		Sequence = sequence;
	}
}
