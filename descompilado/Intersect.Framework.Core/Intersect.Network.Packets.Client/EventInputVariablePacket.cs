using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class EventInputVariablePacket : IntersectPacket
{
	[Key(0)]
	public Guid EventId { get; set; }

	[Key(1)]
	public bool BooleanValue { get; set; }

	[Key(2)]
	public string? StringValue { get; set; }

	[Key(3)]
	public int Value { get; set; }

	[Key(4)]
	public bool Canceled { get; set; }

	public EventInputVariablePacket()
	{
	}

	public EventInputVariablePacket(Guid eventId, bool booleanValue, int value, string stringValue = "", bool canceled = false)
	{
		EventId = eventId;
		BooleanValue = booleanValue;
		Value = value;
		StringValue = stringValue;
		Canceled = canceled;
	}
}
