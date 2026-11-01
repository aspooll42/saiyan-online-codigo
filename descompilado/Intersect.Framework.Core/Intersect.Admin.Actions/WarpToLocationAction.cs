using System;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Admin.Actions;

[MessagePackObject(false)]
public class WarpToLocationAction : AdminAction
{
	[Key(1)]
	public override Intersect.Enums.AdminAction Action { get; } = Intersect.Enums.AdminAction.WarpTo;

	[Key(2)]
	public Guid MapId { get; set; }

	[Key(3)]
	public byte X { get; set; }

	[Key(4)]
	public byte Y { get; set; }

	public WarpToLocationAction()
	{
	}

	public WarpToLocationAction(Guid mapId, byte x, byte y)
	{
		MapId = mapId;
		X = x;
		Y = y;
	}
}
