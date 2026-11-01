using Intersect.Enums;
using MessagePack;

namespace Intersect.Admin.Actions;

[MessagePackObject(false)]
public class SetAccessAction : AdminAction
{
	[Key(1)]
	public override Intersect.Enums.AdminAction Action { get; } = Intersect.Enums.AdminAction.SetAccess;

	[Key(2)]
	public string Name { get; set; }

	[Key(3)]
	public string Power { get; set; }

	public SetAccessAction()
	{
	}

	public SetAccessAction(string name, string power)
	{
		Name = name;
		Power = power;
	}
}
