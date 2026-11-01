using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class EventDialogPacket : IntersectPacket
{
	[Key(0)]
	public Guid EventId { get; set; }

	[Key(1)]
	public string Prompt { get; set; }

	[Key(2)]
	public string Face { get; set; }

	[Key(3)]
	public byte Type { get; set; }

	[Key(4)]
	public string[] Responses { get; set; }

	public EventDialogPacket()
	{
	}

	public EventDialogPacket(Guid eventId, string prompt, string face, byte type, string[] responses)
	{
		EventId = eventId;
		Prompt = prompt;
		Face = face;
		Type = type;
		Responses = responses;
	}
}
