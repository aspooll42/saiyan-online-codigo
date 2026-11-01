using System;
using System.Collections.Generic;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class QuestProgressPacket : IntersectPacket
{
	[Key(0)]
	public Dictionary<Guid, string> Quests { get; set; }

	[Key(1)]
	public Guid[] HiddenQuests { get; set; }

	[Key(2)]
	public Guid[] TargetQuests { get; set; } = Array.Empty<Guid>();

	[Key(3)]
	public Guid[] TargetMaps { get; set; } = Array.Empty<Guid>();

	[Key(4)]
	public int[] TargetXs { get; set; } = Array.Empty<int>();

	[Key(5)]
	public int[] TargetYs { get; set; } = Array.Empty<int>();

	[Key(6)]
	public Guid StoryQuestId { get; set; }

	[Key(7)]
	public string StoryGiver { get; set; } = string.Empty;

	[Key(8)]
	public string StoryPlace { get; set; } = string.Empty;

	public QuestProgressPacket()
	{
	}

	public QuestProgressPacket(Dictionary<Guid, string> quests, Guid[] hiddenQuests)
	{
		Quests = quests;
		HiddenQuests = hiddenQuests;
	}
}
