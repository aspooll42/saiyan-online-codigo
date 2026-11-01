using System;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;

namespace Intersect.Client.Framework.Gwen.Control;

public class ColorSlider : Base
{
	private bool mDepressed;

	private int mSelectedDist;

	private IGameTexture mTexture;

	public Color SelectedColor
	{
		get
		{
			return GetColorAtHeight(mSelectedDist);
		}
		set
		{
			SetColor(value);
		}
	}

	public event GwenEventHandler<EventArgs> ColorChanged;

	public ColorSlider(Base parent)
		: base(parent)
	{
		SetSize(32, 128);
		base.MouseInputEnabled = true;
		mDepressed = false;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.Renderer.DrawColor = Color.White;
		skin.Renderer.DrawTexturedRect(skin.Renderer.GetWhiteTexture(), new Rectangle(5, 0, base.Width - 10, base.Height), Color.White);
		int num = mSelectedDist - 3;
		skin.Renderer.DrawColor = Color.Black;
		skin.Renderer.DrawFilledRect(new Rectangle(0, num + 2, base.Width, 1));
		skin.Renderer.DrawFilledRect(new Rectangle(0, num, 5, 5));
		skin.Renderer.DrawFilledRect(new Rectangle(base.Width - 5, num, 5, 5));
		skin.Renderer.DrawColor = Color.White;
		skin.Renderer.DrawFilledRect(new Rectangle(1, num + 1, 3, 3));
		skin.Renderer.DrawFilledRect(new Rectangle(base.Width - 4, num + 1, 3, 3));
		base.Render(skin);
	}

	protected override void OnMouseDown(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseDown(mouseButton, mousePosition, userAction);
		if (mouseButton == MouseButton.Left)
		{
			mDepressed = true;
			InputHandler.MouseFocus = this;
			OnMouseMoved(mousePosition.X, mousePosition.Y, 0, 0);
		}
	}

	protected override void OnMouseUp(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseUp(mouseButton, mousePosition, userAction);
		if (mouseButton == MouseButton.Left)
		{
			mDepressed = false;
			InputHandler.MouseFocus = null;
			OnMouseMoved(mousePosition.X, mousePosition.Y, 0, 0);
		}
	}

	protected override void OnMouseMoved(int x, int y, int dx, int dy)
	{
		if (mDepressed)
		{
			Point point = CanvasPosToLocal(new Point(x, y));
			if (point.Y < 0)
			{
				point.Y = 0;
			}
			if (point.Y > base.Height)
			{
				point.Y = base.Height;
			}
			mSelectedDist = point.Y;
			if (this.ColorChanged != null)
			{
				this.ColorChanged(this, EventArgs.Empty);
			}
		}
	}

	private Color GetColorAtHeight(int y)
	{
		float num = (float)y / (float)base.Height;
		return Util.HsvToColor(num * 360f, 1f, 1f);
	}

	private void SetColor(Color color)
	{
		mSelectedDist = (int)(color.ToHsv().H / 360f * (float)base.Height);
		if (this.ColorChanged != null)
		{
			this.ColorChanged(this, EventArgs.Empty);
		}
	}
}
