using Intersect.Enums;
using MessagePack;

namespace Intersect.Admin.Actions;

[MessagePackObject(false)]
public class SetLevelAction : AdminAction
{
	[Key(1)]
	public override Intersect.Enums.AdminAction Action { get; } = Intersect.Enums.AdminAction.SetLevel;

	[Key(2)]
	public string Name { get; set; }

	[Key(3)]
	public int Amount { get; set; }

	[Key(4)]
	public bool Absolute { get; set; }

	public SetLevelAction()
	{
	}

	public SetLevelAction(string name, int amount, bool absolute)
	{
		Name = name;
		Amount = amount;
		Absolute = absolute;
	}
}
