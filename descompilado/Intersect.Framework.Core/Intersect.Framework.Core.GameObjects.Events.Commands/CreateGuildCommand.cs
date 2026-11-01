using System;
using System.Collections.Generic;

namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class CreateGuildCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.CreateGuild;

	public Guid VariableId { get; set; }

	public Guid[] BranchIds { get; set; } = new Guid[2];

	public CreateGuildCommand()
	{
	}

	public CreateGuildCommand(Dictionary<Guid, List<EventCommand>> commandLists)
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
}
