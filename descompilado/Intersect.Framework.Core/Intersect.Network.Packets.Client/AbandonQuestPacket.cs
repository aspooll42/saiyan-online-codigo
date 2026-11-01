using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class AbandonQuestPacket : IntersectPacket
{
	[Key(0)]
	public Guid QuestId { get; set; }

	public AbandonQuestPacket()
	{
	}

	public AbandonQuestPacket(Guid questId)
	{
		QuestId = questId;
	}
}
