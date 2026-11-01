using Intersect.Enums;
using MessagePack;

namespace Intersect.Admin.Actions;

[MessagePackObject(false)]
public class WarpToMeAction : AdminAction
{
	[Key(1)]
	public override Intersect.Enums.AdminAction Action { get; } = Intersect.Enums.AdminAction.WarpToMe;

	[Key(2)]
	public string Name { get; set; }

	public WarpToMeAction()
	{
	}

	public WarpToMeAction(string name)
	{
		Name = name;
	}
}
