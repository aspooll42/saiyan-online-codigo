using System;
using Intersect.Enums;

namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class SpawnNpcCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.SpawnNpc;

	public Guid NpcId { get; set; }

	public Direction Dir { get; set; }

	public Guid MapId { get; set; }

	public Guid EntityId { get; set; }

	public sbyte X { get; set; }

	public sbyte Y { get; set; }
}
