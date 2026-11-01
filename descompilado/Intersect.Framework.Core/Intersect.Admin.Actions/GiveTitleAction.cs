using System;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Admin.Actions;

[MessagePackObject(false)]
public class GiveTitleAction : AdminAction
{
	[Key(1)]
	public override Intersect.Enums.AdminAction Action { get; } = Intersect.Enums.AdminAction.GiveTitle;

	[Key(2)]
	public string Name { get; set; }

	[Key(3)]
	public Guid TitleId { get; set; }

	[Key(4)]
	public bool Remove { get; set; }

	public GiveTitleAction()
	{
	}

	public GiveTitleAction(string name, Guid titleId, bool remove)
	{
		Name = name;
		TitleId = titleId;
		Remove = remove;
	}
}
