using Intersect.Enums;
using MessagePack;

namespace Intersect.Admin.Actions;

[MessagePackObject(false)]
public class SetKarmaAction : AdminAction
{
	[Key(1)]
	public override Intersect.Enums.AdminAction Action { get; } = Intersect.Enums.AdminAction.SetKarma;

	[Key(2)]
	public string Name { get; set; }

	[Key(3)]
	public int Amount { get; set; }

	[Key(4)]
	public bool Absolute { get; set; }

	public SetKarmaAction()
	{
	}

	public SetKarmaAction(string name, int amount, bool absolute)
	{
		Name = name;
		Amount = amount;
		Absolute = absolute;
	}
}
