using System;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;

namespace Intersect.Client.Framework.Gwen.Control;

public class ColorLerpBox : Base
{
	private Point mCursorPos;

	private bool mDepressed;

	private float mHue;

	public Color SelectedColor => GetColorAt(mCursorPos.X, mCursorPos.Y);

	public event GwenEventHandler<EventArgs> ColorChanged;

	public ColorLerpBox(Base parent)
		: base(parent)
	{
		SetColor(Color.FromArgb(255, 255, 128, 0));
		SetSize(128, 128);
		base.MouseInputEnabled = true;
		mDepressed = false;
	}

	public static Color Lerp(Color toColor, Color fromColor, float amount)
	{
		Color color = toColor.Subtract(fromColor);
		color = color.Multiply(amount);
		return fromColor.Add(color);
	}

	public void SetColor(Color value, bool onlyHue = true)
	{
		Hsv hsv = value.ToHsv();
		mHue = hsv.H;
		if (!onlyHue)
		{
			mCursorPos.X = (int)(hsv.s * (float)base.Width);
			mCursorPos.Y = (int)((1f - hsv.V) * (float)base.Height);
		}
		Invalidate();
		if (this.ColorChanged != null)
		{
			this.ColorChanged(this, EventArgs.Empty);
		}
	}

	protected override void OnMouseMoved(int x, int y, int dx, int dy)
	{
		if (mDepressed)
		{
			mCursorPos = CanvasPosToLocal(new Point(x, y));
			if (mCursorPos.X < 0)
			{
				mCursorPos.X = 0;
			}
			if (mCursorPos.X > base.Width)
			{
				mCursorPos.X = base.Width;
			}
			if (mCursorPos.Y < 0)
			{
				mCursorPos.Y = 0;
			}
			if (mCursorPos.Y > base.Height)
			{
				mCursorPos.Y = base.Height;
			}
			if (this.ColorChanged != null)
			{
				this.ColorChanged(this, EventArgs.Empty);
			}
		}
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

	private Color GetColorAt(int x, int y)
	{
		float s = (float)x / (float)base.Width;
		float v = 1f - (float)y / (float)base.Height;
		return Util.HsvToColor(mHue, s, v);
	}

	public override void Invalidate()
	{
		base.Invalidate();
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.Renderer.DrawColor = Color.White;
		skin.Renderer.DrawTexturedRect(skin.Renderer.GetWhiteTexture(), base.RenderBounds, Color.White);
		skin.Renderer.DrawColor = Color.Black;
		skin.Renderer.DrawLinedRect(base.RenderBounds);
		Color selectedColor = SelectedColor;
		if ((selectedColor.R + selectedColor.G + selectedColor.B) / 3 < 170)
		{
			skin.Renderer.DrawColor = Color.White;
		}
		else
		{
			skin.Renderer.DrawColor = Color.Black;
		}
		Rectangle rect = new Rectangle(mCursorPos.X - 3, mCursorPos.Y - 3, 6, 6);
		skin.Renderer.DrawShavedCornerRect(rect);
		base.Render(skin);
	}
}
