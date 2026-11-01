using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.Quests;
using Intersect.GameObjects;
using Intersect.Models;

namespace Intersect.Client.Interface.Game;

public static class QuestRewardText
{
	public static List<string> Parts(QuestReward? reward)
	{
		List<string> list = new List<string>();
		if (reward == null || reward.IsEmpty)
		{
			return list;
		}
		Guid currencyId = Globals.Me?.CurrencyItemId ?? Guid.Empty;
		long num = ZeniOf(reward, currencyId);
		if (num > 0)
		{
			list.Add(Strings.QuestLog.RewardZeni.ToString(Amount(num)));
		}
		if (reward.Experience > 0)
		{
			list.Add(Strings.QuestLog.RewardExperience.ToString(Amount(reward.Experience)));
		}
		foreach (QuestRewardItem item in reward.Items)
		{
			if (item.Quantity > 0 && !IsCurrency(item.ItemId, currencyId) && DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var databaseObject))
			{
				list.Add(Strings.QuestLog.RewardItem.ToString(databaseObject.Name, Amount(item.Quantity)));
			}
		}
		foreach (Guid spell in reward.Spells)
		{
			if (DatabaseObject<SpellDescriptor>.TryGet(spell, out var databaseObject2))
			{
				list.Add(databaseObject2.Name);
			}
		}
		return list;
	}

	public static long ZeniOf(QuestReward reward, Guid currencyId)
	{
		return Math.Max(0L, reward.Zeni) + reward.Items.Where((QuestRewardItem item) => IsCurrency(item.ItemId, currencyId)).Sum((Func<QuestRewardItem, long>)((QuestRewardItem item) => Math.Max(0, item.Quantity)));
	}

	public static bool IsCurrency(Guid itemId, Guid currencyId)
	{
		if (currencyId != Guid.Empty)
		{
			return itemId == currencyId;
		}
		return false;
	}

	public static string Amount(long value)
	{
		return value.ToString("N0", CultureInfo.InvariantCulture).Replace(',', ' ');
	}
}
