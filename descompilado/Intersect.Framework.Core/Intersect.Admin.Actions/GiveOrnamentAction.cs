using System;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Admin.Actions;

[MessagePackObject(false)]
public class GiveOrnamentAction : AdminAction
{
	[Key(1)]
	public override Intersect.Enums.AdminAction Action { get; } = Intersect.Enums.AdminAction.GiveOrnament;

	[Key(2)]
	public string Name { get; set; }

	[Key(3)]
	public Guid OrnamentId { get; set; }

	[Key(4)]
	public bool Remove { get; set; }

	public GiveOrnamentAction()
	{
	}

	public GiveOrnamentAction(string name, Guid ornamentId, bool remove)
	{
		Name = name;
		OrnamentId = ornamentId;
		Remove = remove;
	}
}
