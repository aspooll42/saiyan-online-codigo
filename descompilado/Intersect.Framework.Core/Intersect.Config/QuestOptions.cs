using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Intersect.Config;

public class QuestOptions
{
	public List<string> Categories { get; set; }

	[OnDeserializing]
	internal void OnDeserializingMethod(StreamingContext context)
	{
		Categories.Clear();
	}

	public QuestOptions()
	{
		int num = 3;
		List<string> list = new List<string>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<string> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = "Main Quests";
		num2++;
		span[num2] = "Side Quests";
		num2++;
		span[num2] = "Completed Quests";
		num2++;
		Categories = list;
	}
}
