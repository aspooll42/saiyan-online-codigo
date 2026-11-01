using System;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Admin.Actions;

[MessagePackObject(false)]
public class WarpToMapAction : AdminAction
{
	[Key(1)]
	public override Intersect.Enums.AdminAction Action { get; } = Intersect.Enums.AdminAction.WarpTo;

	[Key(2)]
	public Guid MapId { get; set; }

	public WarpToMapAction()
	{
	}

	public WarpToMapAction(Guid mapId)
	{
		MapId = mapId;
	}
}
