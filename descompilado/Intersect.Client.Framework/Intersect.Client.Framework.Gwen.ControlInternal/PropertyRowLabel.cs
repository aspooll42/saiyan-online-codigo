using Intersect.Client.Framework.Gwen.Control;

namespace Intersect.Client.Framework.Gwen.ControlInternal;

public class PropertyRowLabel : Label
{
	private readonly PropertyRow mPropertyRow;

	public PropertyRowLabel(PropertyRow parent)
		: base(parent)
	{
		base.AutoSizeToContents = false;
		base.TextAlign = Pos.Left | Pos.CenterV;
		mPropertyRow = parent;
	}

	public override void UpdateColors()
	{
		if (IsDisabledByTree)
		{
			base.TextColor = base.Skin.Colors.Button.Disabled;
		}
		else if (mPropertyRow != null && mPropertyRow.IsEditing)
		{
			base.TextColor = base.Skin.Colors.Properties.LabelSelected;
		}
		else if (mPropertyRow != null && mPropertyRow.IsHovered)
		{
			base.TextColor = base.Skin.Colors.Properties.LabelHover;
		}
		else
		{
			base.TextColor = base.Skin.Colors.Properties.LabelNormal;
		}
	}
}
