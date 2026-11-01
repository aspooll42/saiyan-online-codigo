using System;
using Intersect.Framework.Core.GameObjects.Maps;

namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class WarpCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.WarpPlayer;

	public Guid MapId { get; set; }

	public byte X { get; set; }

	public byte Y { get; set; }

	public WarpDirection Direction { get; set; }

	public bool ChangeInstance { get; set; }

	public MapInstanceType InstanceType { get; set; }
}
