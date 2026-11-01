using System;
using Newtonsoft.Json;

namespace Intersect.GameObjects;

public class QuestProgress
{
	public bool Completed;

	public Guid TaskId;

	public int TaskProgress;

	public QuestProgress(string data)
	{
		JsonConvert.PopulateObject(data, this);
	}
}
