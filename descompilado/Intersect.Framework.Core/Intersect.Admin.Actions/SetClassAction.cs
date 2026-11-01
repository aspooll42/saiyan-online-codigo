using System;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Admin.Actions;

[MessagePackObject(false)]
public class SetClassAction : AdminAction
{
	[Key(1)]
	public override Intersect.Enums.AdminAction Action { get; } = Intersect.Enums.AdminAction.SetClass;

	[Key(2)]
	public string Name { get; set; }

	[Key(3)]
	public Guid ClassId { get; set; }

	public SetClassAction()
	{
	}

	public SetClassAction(string name, Guid classId)
	{
		Name = name;
		ClassId = classId;
	}
}
