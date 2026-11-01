using System;

namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class OpenCraftingTableCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.OpenCraftingTable;

	public Guid CraftingTableId { get; set; }

	public bool JournalMode { get; set; }
}
