using System;

namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class GiveExperienceCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.GiveExperience;

	public long Exp { get; set; }

	public bool UseVariable { get; set; }

	public VariableType VariableType { get; set; }

	public Guid VariableId { get; set; }

	public bool EnableLosingLevels { get; set; }
}
