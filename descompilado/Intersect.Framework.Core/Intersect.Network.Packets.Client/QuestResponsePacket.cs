using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class QuestResponsePacket : IntersectPacket
{
	[Key(0)]
	public Guid QuestId { get; set; }

	[Key(1)]
	public bool AcceptingQuest { get; set; }

	public QuestResponsePacket()
	{
	}

	public QuestResponsePacket(Guid questId, bool accepting)
	{
		QuestId = questId;
		AcceptingQuest = accepting;
	}
}
