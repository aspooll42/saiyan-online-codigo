using System;
using System.Collections.Generic;
using Intersect.Framework.Core.GameObjects.Items;

namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class ChangeItemsCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.ChangeItems;

	public Guid ItemId { get; set; }

	public bool Add { get; set; }

	public ItemHandling ItemHandling { get; set; }

	public bool UseVariable { get; set; }

	public VariableType VariableType { get; set; }

	public Guid VariableId { get; set; }

	public int Quantity { get; set; }

	public Guid[] BranchIds { get; set; } = new Guid[2];

	public ChangeItemsCommand()
	{
	}

	public ChangeItemsCommand(Dictionary<Guid, List<EventCommand>> commandLists)
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
