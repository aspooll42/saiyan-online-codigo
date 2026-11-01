using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;

namespace Intersect.Client.Interface.Game.DescriptionWindows.Components;

public class HeaderComponent : ComponentBase
{
	private readonly ImagePanel _icon;

	private readonly Label _title;

	private readonly Label _subtitle;

	private readonly Label _description;

	public HeaderComponent(Base parent, string name)
		: base(parent, name)
	{
		ImagePanel parent2 = new ImagePanel(this, "IconContainer");
		_icon = new ImagePanel(parent2, "Icon");
		_title = new Label(this, "Title");
		_subtitle = new Label(this, "Subtitle");
		_description = new Label(this, "Description");
	}

	public void SetIcon(IGameTexture texture, Color color)
	{
		_icon.Texture = texture;
		_icon.RenderColor = color;
		_icon.SizeToContents();
		Align.Center(_icon);
	}

	public void HideIcon()
	{
		Base parent = _icon.Parent;
		if (parent != null)
		{
			parent.IsHidden = true;
		}
		_title.SetPosition(0, _title.Y);
		_subtitle.SetPosition(0, _subtitle.Y);
		_description.SetPosition(0, _description.Y);
	}

	public void SetTitle(string title, Color? color = null)
	{
		_title.SetText(title);
		if ((object)color != null)
		{
			_title.SetTextColor(color, ComponentState.Normal);
		}
	}

	public void SetSubtitle(string subtitle, Color? color = null)
	{
		_subtitle.SetText(subtitle);
		if ((object)color != null)
		{
			_subtitle.SetTextColor(color, ComponentState.Normal);
		}
	}

	public void SetDescription(string description, Color? color = null)
	{
		_description.SetText(description);
		if ((object)color != null)
		{
			_description.SetTextColor(color, ComponentState.Normal);
		}
	}
}
