using Intersect.Enums;
using MessagePack;

namespace Intersect.Admin.Actions;

[MessagePackObject(false)]
public class UnbanAction : AdminAction
{
	[Key(1)]
	public override Intersect.Enums.AdminAction Action { get; } = Intersect.Enums.AdminAction.UnBan;

	[Key(2)]
	public string Name { get; set; }

	public UnbanAction()
	{
	}

	public UnbanAction(string name)
	{
		Name = name;
	}
}
