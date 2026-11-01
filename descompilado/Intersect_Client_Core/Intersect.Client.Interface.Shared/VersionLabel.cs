using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Platform;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;

namespace Intersect.Client.Interface.Shared;

public class VersionLabel : Label
{
	public VersionLabel(Intersect.Client.Framework.Gwen.Control.Base parent, string name = "VersionLabel")
		: base(parent, name)
	{
		base.AutoSizeToContents = true;
		base.Dock = Pos.Top;
		base.MouseInputEnabled = true;
		base.Padding = new Padding(2, 0);
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Layout(skin);
		SizeToContents();
	}

	protected override void OnMouseClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseClicked(mouseButton, mousePosition, userAction);
		string text = Text;
		if (text != null && !string.IsNullOrWhiteSpace(text))
		{
			Neutral.SetClipboardText(text);
		}
	}
}
