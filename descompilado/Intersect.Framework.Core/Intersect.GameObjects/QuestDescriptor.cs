using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Framework.Core.GameObjects.Conditions;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Framework.Core.GameObjects.Quests;
using Intersect.Framework.Core.Serialization;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.GameObjects;

public class QuestDescriptor : DatabaseObject<QuestDescriptor>, IFolderable
{
	private QuestReward? _receivedRewards;

	[NotMapped]
	[JsonIgnore]
	public Dictionary<Guid, EventDescriptor> AddEvents { get; set; } = new Dictionary<Guid, EventDescriptor>();

	[NotMapped]
	public List<Guid> RemoveEvents { get; set; } = new List<Guid>();

	[NotMapped]
	public List<QuestTaskDescriptor> Tasks { get; set; } = new List<QuestTaskDescriptor>();

	public string StartDescription { get; set; } = string.Empty;

	public string BeforeDescription { get; set; } = string.Empty;

	public string EndDescription { get; set; } = string.Empty;

	public string InProgressDescription { get; set; } = string.Empty;

	public bool LogAfterComplete { get; set; }

	public bool LogBeforeOffer { get; set; }

	public bool Quitable { get; set; }

	public bool Repeatable { get; set; }

	[Column("Requirements")]
	[JsonIgnore]
	public string JsonRequirements
	{
		get
		{
			return Requirements.Data();
		}
		set
		{
			Requirements.Load(value);
		}
	}

	[NotMapped]
	public ConditionLists Requirements { get; set; } = new ConditionLists();

	[Column("StartEvent")]
	public Guid StartEventId { get; set; }

	[NotMapped]
	[JsonIgnore]
	public EventDescriptor StartEvent
	{
		get
		{
			return DatabaseObject<EventDescriptor>.Get(StartEventId);
		}
		set
		{
			StartEventId = value.Id;
		}
	}

	[Column("EndEvent")]
	public Guid EndEventId { get; set; }

	[NotMapped]
	[JsonIgnore]
	public EventDescriptor EndEvent
	{
		get
		{
			return DatabaseObject<EventDescriptor>.Get(EndEventId);
		}
		set
		{
			EndEventId = value.Id;
		}
	}

	[NotMapped]
	public QuestReward Rewards
	{
		get
		{
			EventDescriptor eventDescriptor = ((EndEventId == Guid.Empty) ? null : EndEvent);
			if (eventDescriptor == null && EndEventId != Guid.Empty && _receivedRewards != null)
			{
				return _receivedRewards;
			}
			return QuestReward.Combine(CompletionRewards, QuestReward.FromEvent(eventDescriptor));
		}
		set
		{
			_receivedRewards = value;
		}
	}

	[NotMapped]
	public QuestReward CompletionRewards { get; set; } = new QuestReward();

	[Column("CompletionRewards")]
	[JsonIgnore]
	public string CompletionRewardsJson
	{
		get
		{
			return JsonConvert.SerializeObject(CompletionRewards);
		}
		set
		{
			CompletionRewards = (string.IsNullOrWhiteSpace(value) ? new QuestReward() : (JsonConvert.DeserializeObject<QuestReward>(value) ?? new QuestReward()));
		}
	}

	[Column("Tasks")]
	[JsonIgnore]
	public string TasksJson
	{
		get
		{
			return JsonConvert.SerializeObject(Tasks);
		}
		set
		{
			Tasks = JsonConvert.DeserializeObject<List<QuestTaskDescriptor>>(value);
		}
	}

	[NotMapped]
	public string LocalEventsJson
	{
		get
		{
			return JsonConvert.SerializeObject(AddEvents, Formatting.Indented, new JsonSerializerSettings
			{
				SerializationBinder = new IntersectTypeSerializationBinder(),
				TypeNameHandling = TypeNameHandling.Auto,
				DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
				ObjectCreationHandling = ObjectCreationHandling.Replace
			});
		}
		set
		{
			JsonConvert.PopulateObject(value, AddEvents, new JsonSerializerSettings
			{
				SerializationBinder = new IntersectTypeSerializationBinder(),
				TypeNameHandling = TypeNameHandling.Auto,
				DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
				ObjectCreationHandling = ObjectCreationHandling.Replace
			});
		}
	}

	[NotMapped]
	[JsonIgnore]
	public Dictionary<Guid, Guid> OriginalTaskEventIds { get; set; } = new Dictionary<Guid, Guid>();

	public string Folder { get; set; } = string.Empty;

	public bool DoNotShowUnlessRequirementsMet { get; set; }

	public string UnstartedCategory { get; set; } = string.Empty;

	public string InProgressCategory { get; set; } = string.Empty;

	public string CompletedCategory { get; set; } = string.Empty;

	public int OrderValue { get; set; }

	[JsonConstructor]
	public QuestDescriptor(Guid Id)
		: base(Id)
	{
		base.Name = "New Quest";
	}

	public QuestDescriptor()
	{
		base.Name = "New Quest";
	}

	public int GetTaskIndex(Guid taskId)
	{
		for (int i = 0; i < Tasks.Count; i++)
		{
			if (Tasks[i].Id == taskId)
			{
				return i;
			}
		}
		return -1;
	}

	public QuestTaskDescriptor FindTask(Guid taskId)
	{
		for (int i = 0; i < Tasks.Count; i++)
		{
			if (Tasks[i].Id == taskId)
			{
				return Tasks[i];
			}
		}
		return null;
	}
}
