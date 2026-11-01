using Intersect.Client.Framework.Gwen.Control;

namespace Intersect.Client.Framework.Gwen.ControlInternal;

public class TreeNodeLabel : Button
{
	public TreeNodeLabel(Base parent)
		: base(parent)
	{
		base.TextAlign = Pos.Left | Pos.CenterV;
		base.ShouldDrawBackground = false;
		base.Height = 16;
		base.Padding = new Padding(3, 0, 3, 0);
	}

	public override void UpdateColors()
	{
		if (IsDisabledByTree)
		{
			base.TextColor = base.Skin.Colors.Button.Disabled;
		}
		else if (base.IsActive || base.ToggleState)
		{
			base.TextColor = base.Skin.Colors.Tree.Selected;
		}
		else if (IsHovered)
		{
			base.TextColor = base.Skin.Colors.Tree.Hover;
		}
		else
		{
			base.TextColor = base.Skin.Colors.Tree.Normal;
		}
	}
}
