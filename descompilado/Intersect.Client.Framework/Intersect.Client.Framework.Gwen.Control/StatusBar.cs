using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control;

public class StatusBar : Label
{
	public StatusBar(Base parent)
		: base(parent)
	{
		base.AutoSizeToContents = false;
		base.Height = 22;
		base.Dock = Pos.Bottom;
		base.Padding = Padding.Two;
		base.TextAlign = Pos.Left | Pos.CenterV;
	}

	public void AddControl(Base control, bool right)
	{
		control.Parent = this;
		control.Dock = (right ? Pos.Right : Pos.Left);
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawStatusBar(this);
	}
}
