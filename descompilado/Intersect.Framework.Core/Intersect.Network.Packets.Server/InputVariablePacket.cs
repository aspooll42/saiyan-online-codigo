using System;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class InputVariablePacket : IntersectPacket
{
	[Key(0)]
	public Guid EventId { get; set; }

	[Key(1)]
	public string Title { get; set; }

	[Key(2)]
	public string Prompt { get; set; }

	[Key(3)]
	public VariableDataType Type { get; set; }

	public InputVariablePacket()
	{
	}

	public InputVariablePacket(Guid eventId, string title, string prompt, VariableDataType type)
	{
		EventId = eventId;
		Title = title;
		Prompt = prompt;
		Type = type;
	}
}
