using Intersect.Enums;

namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class ChangeGenderCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.ChangeGender;

	public Gender Gender { get; set; }
}
