using Intersect.Enums;
using MessagePack;

namespace Intersect.Admin.Actions;

[MessagePackObject(false)]
public class UnmuteAction : AdminAction
{
	[Key(1)]
	public override Intersect.Enums.AdminAction Action { get; } = Intersect.Enums.AdminAction.UnMute;

	[Key(2)]
	public string Name { get; set; }

	public UnmuteAction()
	{
	}

	public UnmuteAction(string name)
	{
		Name = name;
	}
}
