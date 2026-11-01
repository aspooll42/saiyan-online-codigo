using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control;

public class GroupBox : Label
{
	public GroupBox(Base parent)
		: base(parent)
	{
		base.AutoSizeToContents = false;
		base.MouseInputEnabled = true;
		base.KeyboardInputEnabled = true;
		base.Padding = new Padding(10, 0, 10, 0);
		base.TextAlign = Pos.Left | Pos.Top;
		Invalidate();
		_innerPanel = new Base(this, "_innerPanel");
		_innerPanel.Dock = Pos.Fill;
		_innerPanel.Margin = new Margin(5, base.TextHeight + 5, 5, 5);
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Layout(skin);
		if (base.AutoSizeToContents)
		{
			DoSizeToContents();
		}
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawGroupBox(this, base.TextX, base.TextHeight, base.TextWidth);
	}

	public override bool SizeToContents(out Point contentSize)
	{
		bool result = DoSizeToContents();
		contentSize = MeasureContent();
		return result;
	}

	protected virtual bool DoSizeToContents()
	{
		bool flag = _innerPanel?.SizeToChildren() ?? false;
		flag &= SizeToChildren();
		int num = base.TextWidth + base.Padding.Right + base.Padding.Left;
		if (base.Width < num)
		{
			base.Width = num;
			flag = true;
		}
		return flag;
	}
}
