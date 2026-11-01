using Intersect.Enums;
using MessagePack;

namespace Intersect.Admin.Actions;

[MessagePackObject(false)]
public class SetFaceAction : AdminAction
{
	[Key(1)]
	public override Intersect.Enums.AdminAction Action { get; } = Intersect.Enums.AdminAction.SetFace;

	[Key(2)]
	public string Name { get; set; }

	[Key(3)]
	public string Face { get; set; }

	public SetFaceAction()
	{
	}

	public SetFaceAction(string name, string face)
	{
		Name = name;
		Face = face;
	}
}
