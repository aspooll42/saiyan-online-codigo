using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Framework.Core.GameObjects.Quests;
using Intersect.Localization;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.GameObjects;

public class QuestTaskDescriptor
{
	[NotMapped]
	[JsonIgnore]
	[Obsolete("Windows Editor only")]
	public EventDescriptor EditingEvent { get; set; }

	public Guid Id { get; set; }

	public Guid CompletionEventId { get; set; }

	[JsonIgnore]
	public EventDescriptor CompletionEvent
	{
		get
		{
			return DatabaseObject<EventDescriptor>.Get(CompletionEventId);
		}
		set
		{
			CompletionEventId = value.Id;
		}
	}

	public QuestObjective Objective { get; set; }

	public Guid TargetId { get; set; }

	public int Quantity { get; set; }

	public string Description { get; set; } = string.Empty;

	public QuestReward Rewards { get; set; } = new QuestReward();

	public QuestTaskDescriptor(Guid id)
	{
		Id = id;
	}

	public string GetTaskString(Dictionary<int, LocalizedString> descriptions)
	{
		string result = string.Empty;
		switch (Objective)
		{
		case QuestObjective.EventDriven:
			result = descriptions[(int)Objective].ToString(Description);
			break;
		case QuestObjective.GatherItems:
			result = descriptions[(int)Objective].ToString(DatabaseObject<ItemDescriptor>.GetName(TargetId), Quantity, Description);
			break;
		case QuestObjective.KillNpcs:
			result = descriptions[(int)Objective].ToString(DatabaseObject<NPCDescriptor>.GetName(TargetId), Quantity, Description);
			break;
		}
		return result;
	}
}
