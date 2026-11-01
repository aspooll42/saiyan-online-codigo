using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control;

public class MenuStrip : Menu
{
	protected override bool ShouldHoverOpenMenu => IsMenuOpen();

	public MenuStrip(Base parent)
		: base(parent)
	{
		SetBounds(0, 0, 200, 22);
		base.Dock = Pos.Top;
		_innerPanel.Padding = new Padding(5, 0, 0, 0);
	}

	public override void Close()
	{
	}

	protected override void RenderUnder(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawMenuStrip(this);
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
	}

	protected override void OnAddItem(MenuItem item)
	{
		item.Dock = Pos.Left;
		item.Padding = new Padding(10, 0, 10, 0);
		item.SizeToContents();
		item.IsOnStrip = true;
		item.HoverEnter += OnHoverItem;
	}
}
