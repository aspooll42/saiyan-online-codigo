using Intersect.Enums;
using MessagePack;

namespace Intersect.Admin.Actions;

[MessagePackObject(false)]
public class KickAction : AdminAction
{
	[Key(1)]
	public override Intersect.Enums.AdminAction Action { get; } = Intersect.Enums.AdminAction.Kick;

	[Key(2)]
	public string Name { get; set; }

	public KickAction()
	{
	}

	public KickAction(string name)
	{
		Name = name;
	}
}
