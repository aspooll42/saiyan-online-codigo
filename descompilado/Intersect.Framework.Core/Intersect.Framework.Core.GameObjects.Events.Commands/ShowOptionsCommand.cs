using System;
using System.Collections.Generic;

namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class ShowOptionsCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.ShowOptions;

	public string Text { get; set; } = string.Empty;

	public string[] Options { get; set; } = new string[4];

	public Guid[] BranchIds { get; set; } = new Guid[4];

	public string Face { get; set; } = string.Empty;

	public ShowOptionsCommand()
	{
	}

	public ShowOptionsCommand(Dictionary<Guid, List<EventCommand>> commandLists)
	{
		for (int i = 0; i < BranchIds.Length; i++)
		{
			BranchIds[i] = Guid.NewGuid();
			commandLists.Add(BranchIds[i], new List<EventCommand>());
		}
	}

	public override string GetCopyData(Dictionary<Guid, List<EventCommand>> commandLists, Dictionary<Guid, List<EventCommand>> copyLists)
	{
		Guid[] branchIds = BranchIds;
		foreach (Guid guid in branchIds)
		{
			if (!(guid != Guid.Empty) || !commandLists.ContainsKey(guid))
			{
				continue;
			}
			copyLists.Add(guid, commandLists[guid]);
			foreach (EventCommand item in commandLists[guid])
			{
				item.GetCopyData(commandLists, copyLists);
			}
		}
		return base.GetCopyData(commandLists, copyLists);
	}

	public override void FixBranchIds(Dictionary<Guid, Guid> idDict)
	{
		for (int i = 0; i < BranchIds.Length; i++)
		{
			if (idDict.ContainsKey(BranchIds[i]))
			{
				BranchIds[i] = idDict[BranchIds[i]];
			}
		}
	}
}
