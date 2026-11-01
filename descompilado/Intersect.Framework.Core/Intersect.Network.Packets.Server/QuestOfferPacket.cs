using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class QuestOfferPacket : IntersectPacket
{
	[Key(0)]
	public Guid QuestId { get; set; }

	public QuestOfferPacket()
	{
	}

	public QuestOfferPacket(Guid questId)
	{
		QuestId = questId;
	}
}
