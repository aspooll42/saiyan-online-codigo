using System;

namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class PlayAnimationCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.PlayAnimation;

	public Guid AnimationId { get; set; }

	public byte Dir { get; set; }

	public Guid MapId { get; set; }

	public Guid EntityId { get; set; }

	public sbyte X { get; set; }

	public sbyte Y { get; set; }

	public bool InstanceToPlayer { get; set; }
}
