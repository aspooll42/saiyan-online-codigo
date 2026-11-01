using Intersect.Enums;
using MessagePack;

namespace Intersect.Admin.Actions;

[MessagePackObject(false)]
public class KillAction : AdminAction
{
	[Key(1)]
	public override Intersect.Enums.AdminAction Action { get; } = Intersect.Enums.AdminAction.Kill;

	[Key(2)]
	public string Name { get; set; }

	public KillAction()
	{
	}

	public KillAction(string name)
	{
		Name = name;
	}
}
