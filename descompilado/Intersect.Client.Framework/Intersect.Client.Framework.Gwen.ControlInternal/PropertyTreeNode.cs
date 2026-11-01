using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.ControlInternal;

public class PropertyTreeNode : TreeNode
{
	public PropertyTreeNode(Intersect.Client.Framework.Gwen.Control.Base parent)
		: base(parent)
	{
		base._trigger.TextColorOverride = base.Skin.Colors.Properties.Title;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawPropertyTreeNode(this, _innerPanel.X, _innerPanel.Y);
	}
}
