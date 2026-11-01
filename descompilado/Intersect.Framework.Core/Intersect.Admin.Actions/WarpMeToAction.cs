using Intersect.Enums;
using MessagePack;

namespace Intersect.Admin.Actions;

[MessagePackObject(false)]
public class WarpMeToAction : AdminAction
{
	[Key(1)]
	public override Intersect.Enums.AdminAction Action { get; } = Intersect.Enums.AdminAction.WarpMeTo;

	[Key(2)]
	public string Name { get; set; }

	public WarpMeToAction()
	{
	}

	public WarpMeToAction(string name)
	{
		Name = name;
	}
}
