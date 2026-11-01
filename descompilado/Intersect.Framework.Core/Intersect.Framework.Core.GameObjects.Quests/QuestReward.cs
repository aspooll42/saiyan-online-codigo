using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Framework.Core.GameObjects.Events.Commands;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Quests;

public class QuestReward
{
	public long Zeni { get; set; }

	public long Experience { get; set; }

	public List<QuestRewardItem> Items { get; set; } = new List<QuestRewardItem>();

	public List<Guid> Spells { get; set; } = new List<Guid>();

	[JsonIgnore]
	public bool IsEmpty
	{
		get
		{
			if (Zeni <= 0 && Experience <= 0 && Items.All((QuestRewardItem item) => item.Quantity <= 0))
			{
				return Spells.Count == 0;
			}
			return false;
		}
	}

	public QuestReward Clone()
	{
		QuestReward obj = new QuestReward
		{
			Zeni = Zeni,
			Experience = Experience,
			Items = Items.Select((QuestRewardItem item) => new QuestRewardItem
			{
				ItemId = item.ItemId,
				Quantity = item.Quantity
			}).ToList()
		};
		List<Guid> spells = Spells;
		int count = spells.Count;
		List<Guid> list = new List<Guid>(count);
		CollectionsMarshal.SetCount(list, count);
		Span<Guid> span = CollectionsMarshal.AsSpan(list);
		int num = 0;
		Span<Guid> span2 = CollectionsMarshal.AsSpan(spells);
		span2.CopyTo(span.Slice(num, span2.Length));
		num += span2.Length;
		obj.Spells = list;
		return obj;
	}

	public static QuestReward Combine(params QuestReward?[] parts)
	{
		QuestReward questReward = new QuestReward();
		foreach (QuestReward questReward2 in parts)
		{
			if (questReward2 == null)
			{
				continue;
			}
			questReward.Zeni += Math.Max(0L, questReward2.Zeni);
			questReward.Experience += Math.Max(0L, questReward2.Experience);
			foreach (QuestRewardItem item in questReward2.Items)
			{
				if (!(item.ItemId == Guid.Empty) && item.Quantity > 0)
				{
					QuestRewardItem questRewardItem = questReward.Items.FirstOrDefault((QuestRewardItem questRewardItem2) => questRewardItem2.ItemId == item.ItemId);
					if (questRewardItem == null)
					{
						questReward.Items.Add(new QuestRewardItem
						{
							ItemId = item.ItemId,
							Quantity = item.Quantity
						});
					}
					else
					{
						questRewardItem.Quantity += item.Quantity;
					}
				}
			}
			foreach (Guid spell in questReward2.Spells)
			{
				if (!questReward.Spells.Contains(spell))
				{
					questReward.Spells.Add(spell);
				}
			}
		}
		return questReward;
	}

	public static QuestReward FromEvent(EventDescriptor? descriptor)
	{
		QuestReward questReward = new QuestReward();
		EventPage eventPage = descriptor?.Pages?.FirstOrDefault();
		if (eventPage?.CommandLists == null || eventPage.CommandLists.Count == 0)
		{
			return questReward;
		}
		if (!eventPage.CommandLists.TryGetValue(eventPage.CommandLists.Keys.First(), out var value))
		{
			return questReward;
		}
		foreach (EventCommand item in value)
		{
			EventCommand eventCommand = item;
			if (!(eventCommand is GiveExperienceCommand giveExperienceCommand))
			{
				ChangeItemsCommand changeItemsCommand = eventCommand as ChangeItemsCommand;
				if (changeItemsCommand == null)
				{
					if (eventCommand is ChangeSpellsCommand { Add: not false } changeSpellsCommand && changeSpellsCommand.SpellId != Guid.Empty && !questReward.Spells.Contains(changeSpellsCommand.SpellId))
					{
						questReward.Spells.Add(changeSpellsCommand.SpellId);
					}
				}
				else if (changeItemsCommand.Add && !changeItemsCommand.UseVariable && changeItemsCommand.ItemId != Guid.Empty && changeItemsCommand.Quantity > 0)
				{
					QuestRewardItem questRewardItem = questReward.Items.FirstOrDefault((QuestRewardItem i) => i.ItemId == changeItemsCommand.ItemId);
					if (questRewardItem == null)
					{
						questReward.Items.Add(new QuestRewardItem
						{
							ItemId = changeItemsCommand.ItemId,
							Quantity = changeItemsCommand.Quantity
						});
					}
					else
					{
						questRewardItem.Quantity += changeItemsCommand.Quantity;
					}
				}
			}
			else if (!giveExperienceCommand.UseVariable && giveExperienceCommand.Exp > 0)
			{
				questReward.Experience += giveExperienceCommand.Exp;
			}
		}
		return questReward;
	}
}
