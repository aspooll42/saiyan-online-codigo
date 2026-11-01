using Intersect.Enums;
using MessagePack;

namespace Intersect.Admin.Actions;

[MessagePackObject(false)]
public class SetSpriteAction : AdminAction
{
	[Key(1)]
	public override Intersect.Enums.AdminAction Action { get; } = Intersect.Enums.AdminAction.SetSprite;

	[Key(2)]
	public string Name { get; set; }

	[Key(3)]
	public string Sprite { get; set; }

	public SetSpriteAction()
	{
	}

	public SetSpriteAction(string name, string sprite)
	{
		Name = name;
		Sprite = sprite;
	}
}
