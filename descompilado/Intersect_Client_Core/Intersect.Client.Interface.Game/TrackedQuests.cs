using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Database;
using Intersect.Client.General;
using Intersect.Framework.Core;
using Intersect.GameObjects;
using Intersect.Models;

namespace Intersect.Client.Interface.Game;

public static class TrackedQuests
{
	public const int MaxPinned = 5;

	public const long FarewellMs = 4000L;

	private const string PinsPreference = "TrackedQuests_";

	private const string CollapsedPreference = "TrackedQuestsCollapsed";

	private const string FollowedPreference = "TrackedQuestsFollowed_";

	private const string StoryPreference = "TrackedQuestsStory_";

	private static readonly List<Guid> Pins = new List<Guid>();

	private static Guid _followed;

	private static readonly Dictionary<Guid, long> FinishedAt = new Dictionary<Guid, long>();

	private static readonly Dictionary<Guid, Guid> KnownTasks = new Dictionary<Guid, Guid>();

	private static Guid _characterId;

	public static IReadOnlyList<Guid> Pinned => Pins;

	public static int Version { get; private set; }

	public static bool Collapsed { get; private set; }

	public static bool StoryShown { get; private set; } = true;

	public static int Count => Pins.Count((Guid questId) => !IsStory(questId));

	public static bool IsFull => Count >= 5;

	public static Guid Followed => _followed;

	public static bool IsPinned(Guid questId)
	{
		if (!IsStory(questId))
		{
			return Pins.Contains(questId);
		}
		return StoryShown;
	}

	public static bool IsStory(Guid questId)
	{
		if (questId != Guid.Empty)
		{
			return questId == Globals.StoryQuestId;
		}
		return false;
	}

	public static bool TryGetFinishedAt(Guid questId, out long finishedAt)
	{
		return FinishedAt.TryGetValue(questId, out finishedAt);
	}

	public static void Follow(Guid questId)
	{
		SetFollowed((_followed == questId) ? Guid.Empty : questId);
	}

	public static void FollowStory()
	{
		SetFollowed(Guid.Empty);
	}

	private static void SetFollowed(Guid questId)
	{
		if (!(_followed == questId))
		{
			_followed = questId;
			Version++;
			if (_characterId != Guid.Empty)
			{
				Globals.Database?.SavePreference("TrackedQuestsFollowed_" + _characterId, (questId == Guid.Empty) ? string.Empty : questId.ToString());
			}
		}
	}

	private static void Unfollow(Guid questId)
	{
		if (_followed == questId)
		{
			SetFollowed(Guid.Empty);
		}
	}

	public static bool Toggle(Guid questId)
	{
		if (IsStory(questId))
		{
			ToggleStory();
			return true;
		}
		if (Pins.Remove(questId))
		{
			FinishedAt.Remove(questId);
			Unfollow(questId);
			Changed();
			return true;
		}
		if (IsFull)
		{
			return false;
		}
		Pins.Add(questId);
		Changed();
		return true;
	}

	public static void ToggleStory()
	{
		StoryShown = !StoryShown;
		Guid storyQuestId = Globals.StoryQuestId;
		if (!StoryShown)
		{
			Pins.Remove(storyQuestId);
			FinishedAt.Remove(storyQuestId);
		}
		else if (InProgress(storyQuestId) && !Pins.Contains(storyQuestId))
		{
			Pins.Add(storyQuestId);
		}
		if (_characterId != Guid.Empty)
		{
			Globals.Database?.SavePreference("TrackedQuestsStory_" + _characterId, StoryShown.ToString());
		}
		Changed();
	}

	public static void ToggleCollapsed()
	{
		Collapsed = !Collapsed;
		Globals.Database?.SavePreference("TrackedQuestsCollapsed", Collapsed.ToString());
		Version++;
	}

	public static void Prune(long now)
	{
		List<Guid> list = (from pair in FinishedAt
			where now - pair.Value >= 4000
			select pair.Key).ToList();
		if (list.Count == 0)
		{
			return;
		}
		foreach (Guid item in list)
		{
			FinishedAt.Remove(item);
			Pins.Remove(item);
			Unfollow(item);
		}
		Changed();
	}

	public static void Observe()
	{
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		if (me.Id != _characterId)
		{
			Load(me.Id);
			Remember();
			return;
		}
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		bool flag = false;
		foreach (KeyValuePair<Guid, QuestProgress> item in me.QuestProgress)
		{
			item.Deconstruct(out var key, out var value);
			Guid guid = key;
			QuestProgress questProgress = value;
			Guid value2;
			bool flag2 = KnownTasks.TryGetValue(guid, out value2) && value2 != Guid.Empty;
			bool flag3 = questProgress.TaskId != Guid.Empty;
			if (!flag2 && flag3)
			{
				FinishedAt.Remove(guid);
				if (!Pins.Contains(guid) && (IsStory(guid) ? StoryShown : (!IsFull)))
				{
					Pins.Add(guid);
				}
				flag = true;
			}
			else if (flag2 && !flag3 && Pins.Contains(guid))
			{
				Unfollow(guid);
				if (questProgress.Completed)
				{
					FinishedAt[guid] = millisecondsUtc;
				}
				else
				{
					Pins.Remove(guid);
				}
				flag = true;
			}
		}
		foreach (Guid key2 in KnownTasks.Keys)
		{
			if (!me.QuestProgress.ContainsKey(key2) && Pins.Remove(key2))
			{
				FinishedAt.Remove(key2);
				Unfollow(key2);
				flag = true;
			}
		}
		if (_followed != Guid.Empty && (!me.QuestProgress.TryGetValue(_followed, out QuestProgress value3) || !(value3.TaskId != Guid.Empty)))
		{
			SetFollowed(Guid.Empty);
		}
		Remember();
		if (flag)
		{
			Changed();
		}
	}

	private static void Load(Guid characterId)
	{
		_characterId = characterId;
		Pins.Clear();
		FinishedAt.Clear();
		KnownTasks.Clear();
		Player me = Globals.Me;
		GameDatabase database = Globals.Database;
		if (me == null || database == null)
		{
			return;
		}
		string key = "TrackedQuests_" + characterId;
		StoryShown = database.LoadPreference("TrackedQuestsStory_" + characterId) != bool.FalseString;
		if (database.HasPreference(key))
		{
			string[] array = database.LoadPreference(key).Split(',', StringSplitOptions.RemoveEmptyEntries);
			foreach (string input in array)
			{
				if (Guid.TryParse(input, out var result) && InProgress(result) && !Pins.Contains(result) && (IsStory(result) ? StoryShown : (!IsFull)))
				{
					Pins.Add(result);
				}
			}
		}
		else
		{
			QuestDescriptor databaseObject;
			IEnumerable<QuestDescriptor> source = (from quest in (from questId in me.QuestProgress.Keys
					where InProgress(questId) && !IsStory(questId)
					select (!DatabaseObject<QuestDescriptor>.TryGet(questId, out databaseObject)) ? null : databaseObject).OfType<QuestDescriptor>()
				orderby quest.OrderValue, quest.Name
				select quest).Take(5);
			Pins.AddRange(source.Select((QuestDescriptor quest) => quest.Id));
			if (StoryShown && InProgress(Globals.StoryQuestId))
			{
				Pins.Add(Globals.StoryQuestId);
			}
		}
		Collapsed = database.LoadPreference("TrackedQuestsCollapsed") == bool.TrueString;
		_followed = Guid.Empty;
		if (Guid.TryParse(database.LoadPreference("TrackedQuestsFollowed_" + characterId), out var result2) && InProgress(result2) && Pins.Contains(result2))
		{
			_followed = result2;
		}
		Changed();
	}

	private static bool InProgress(Guid questId)
	{
		Player me = Globals.Me;
		if (me != null && me.QuestProgress.TryGetValue(questId, out QuestProgress value))
		{
			return value.TaskId != Guid.Empty;
		}
		return false;
	}

	private static void Remember()
	{
		KnownTasks.Clear();
		if (Globals.Me == null)
		{
			return;
		}
		foreach (var (key, questProgress2) in Globals.Me.QuestProgress)
		{
			KnownTasks[key] = questProgress2.TaskId;
		}
	}

	private static void Changed()
	{
		Version++;
		if (_characterId != Guid.Empty)
		{
			Globals.Database?.SavePreference("TrackedQuests_" + _characterId, string.Join(",", Pins));
		}
	}
}
