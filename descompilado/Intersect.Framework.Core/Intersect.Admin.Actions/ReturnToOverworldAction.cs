using Intersect.Enums;
using MessagePack;

namespace Intersect.Admin.Actions;

[MessagePackObject(false)]
public class ReturnToOverworldAction : AdminAction
{
	[Key(1)]
	public override Intersect.Enums.AdminAction Action { get; } = Intersect.Enums.AdminAction.ReturnToOverworld;

	[Key(2)]
	public string PlayerName { get; set; }

	public ReturnToOverworldAction()
	{
	}

	public ReturnToOverworldAction(string name)
	{
		PlayerName = name;
	}
}
