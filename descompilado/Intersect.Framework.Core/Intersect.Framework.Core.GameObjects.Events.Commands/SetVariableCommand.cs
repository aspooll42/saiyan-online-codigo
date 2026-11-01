using System;

namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class SetVariableCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.SetVariable;

	public VariableType VariableType { get; set; }

	public Guid VariableId { get; set; }

	public bool SyncParty { get; set; }

	public VariableMod Modification { get; set; }
}
