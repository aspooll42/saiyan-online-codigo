using Intersect.Enums;
using MessagePack;

namespace Intersect.Admin.Actions;

[MessagePackObject(false)]
public class SetZeniAction : AdminAction
{
	[Key(1)]
	public override Intersect.Enums.AdminAction Action { get; } = Intersect.Enums.AdminAction.SetZeni;

	[Key(2)]
	public string Name { get; set; }

	[Key(3)]
	public long Amount { get; set; }

	[Key(4)]
	public bool Absolute { get; set; }

	public SetZeniAction()
	{
	}

	public SetZeniAction(string name, long amount, bool absolute)
	{
		Name = name;
		Amount = amount;
		Absolute = absolute;
	}
}
