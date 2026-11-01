using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Intersect.Client.Entities;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Configuration;
using Intersect.Framework.Core.GameObjects.Conditions;
using Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.GameObjects;
using Intersect.Models;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Interface.Game;

public static class StoryThread
{
	private static Guid _characterId;

	private static Dictionary<string, string>? _chapterTitles;

	private static readonly Dictionary<Guid, (int Index, int Count)?> Ranks = new Dictionary<Guid, (int, int)?>();

	private static int _rankedQuests = -1;

	public static int NiveauRequis(QuestDescriptor quest)
	{
		return ((from condition in quest.Requirements?.Lists?.SelectMany((ConditionList liste) => liste.Conditions ?? new List<Condition>()).OfType<LevelOrStatCondition>()
			where condition.ComparingLevel && !condition.Negated && condition.Comparator == VariableComparator.GreaterOrEqual
			select condition.Value).DefaultIfEmpty(0).Max());
	}

	public static bool IsStoryQuest(QuestDescriptor? quest)
	{
		if (quest == null)
		{
			return false;
		}
		return quest.Folder?.Trim().StartsWith("Chapitre", StringComparison.OrdinalIgnoreCase) == true;
	}

	public static string ChapterLabel(QuestDescriptor quest)
	{
		if (!IsStoryQuest(quest))
		{
			return string.Empty;
		}
		return Traduction.Contenu(quest.Folder.Trim()).ToUpperInvariant();
	}

	public static (int Index, int Count)? RankOf(QuestDescriptor quest)
	{
		if (!IsStoryQuest(quest))
		{
			return null;
		}
		int count = DatabaseObject<QuestDescriptor>.Lookup.Count;
		if (count != _rankedQuests)
		{
			Ranks.Clear();
			_rankedQuests = count;
		}
		if (Ranks.TryGetValue(quest.Id, out (int, int)? value))
		{
			return value;
		}
		List<QuestDescriptor> list = Siblings(quest);
		int num = list.FindIndex((QuestDescriptor sibling) => sibling.Id == quest.Id);
		(int, int)? tuple = ((num < 0) ? (((int, int)?)null) : new(int, int)?((num + 1, list.Count)));
		Ranks[quest.Id] = tuple;
		return tuple;
	}

	public static bool IsLastOfChapter(QuestDescriptor quest)
	{
		(int, int)? tuple = RankOf(quest);
		if (tuple.HasValue)
		{
			(int, int) valueOrDefault = tuple.GetValueOrDefault();
			return valueOrDefault.Item1 == valueOrDefault.Item2;
		}
		return false;
	}

	private static List<QuestDescriptor> Siblings(QuestDescriptor quest)
	{
		return (from other in DatabaseObject<QuestDescriptor>.Lookup.Values.OfType<QuestDescriptor>()
			where string.Equals(other.Folder?.Trim(), quest.Folder?.Trim(), StringComparison.OrdinalIgnoreCase)
			orderby other.OrderValue, other.Name
			select other).ToList();
	}

	public static string? ChapterTitle(string? folder)
	{
		if (string.IsNullOrWhiteSpace(folder))
		{
			return null;
		}
		if (_chapterTitles == null)
		{
			_chapterTitles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			try
			{
				string chemin = Path.Combine(ClientConfiguration.ResourcesDirectory, "histoire.json");
				if (Coffre.Existe(chemin) && JObject.Parse(Coffre.LireTexte(chemin))["Chapitres"] is JObject jObject)
				{
					foreach (var (text2, jToken2) in jObject)
					{
						if (jToken2 != null && jToken2.Type == JTokenType.String && !string.IsNullOrWhiteSpace((string?)jToken2))
						{
							_chapterTitles[text2.Trim()] = ((string?)jToken2).Trim();
						}
					}
				}
			}
			catch
			{
			}
		}
		if (!_chapterTitles.TryGetValue(folder.Trim(), out string value))
		{
			return null;
		}
		return Traduction.Contenu(value);
	}

	public static void Observe(Guid previous, Guid current, string giver, string place)
	{
		Player me = Globals.Me;
		if (me != null)
		{
			QuestProgress value;
			if (me.Id != _characterId)
			{
				_characterId = me.Id;
			}
			else if (!(previous == Guid.Empty) && !(previous == current) && me.QuestProgress.TryGetValue(previous, out value) && value.Completed)
			{
				Annonces.Histoire(previous, current, giver, place);
			}
		}
	}
}
