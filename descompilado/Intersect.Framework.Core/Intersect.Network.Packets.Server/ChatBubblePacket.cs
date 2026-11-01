using System;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class ChatBubblePacket : IntersectPacket
{
	[Key(0)]
	public Guid EntityId { get; set; }

	[Key(1)]
	public EntityType Type { get; set; }

	[Key(2)]
	public Guid MapId { get; set; }

	[Key(3)]
	public string Text { get; set; }

	public ChatBubblePacket()
	{
	}

	public ChatBubblePacket(Guid entityId, EntityType type, Guid mapId, string text)
	{
		EntityId = entityId;
		Type = type;
		MapId = mapId;
		Text = text;
	}
}
