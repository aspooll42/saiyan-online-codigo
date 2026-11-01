using System;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Renderer;

namespace Intersect.Client.Framework.Gwen.Skin;

[Obsolete]
public class Simple : Base
{
	private readonly Color mColBgDark;

	private readonly Color mColBorderColor;

	private readonly Color mColControl;

	private readonly Color mColControlBright;

	private readonly Color mColControlDark;

	private readonly Color mColControlDarker;

	private readonly Color mColControlOutlineLight;

	private readonly Color mColControlOutlineLighter;

	private readonly Color mColControlOutlineNormal;

	private readonly Color mColHighlightBg;

	private readonly Color mColHighlightBorder;

	private readonly Color mColModal;

	private readonly Color mColToolTipBackground;

	private readonly Color mColToolTipBorder;

	public Simple(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
		: base(renderer)
	{
		mColBorderColor = Color.FromArgb(255, 80, 80, 80);
		mColBgDark = Color.FromArgb(255, 235, 235, 235);
		mColControl = Color.FromArgb(255, 240, 240, 240);
		mColControlBright = Color.FromArgb(255, 255, 255, 255);
		mColControlDark = Color.FromArgb(255, 214, 214, 214);
		mColControlDarker = Color.FromArgb(255, 180, 180, 180);
		mColControlOutlineNormal = Color.FromArgb(255, 112, 112, 112);
		mColControlOutlineLight = Color.FromArgb(255, 144, 144, 144);
		mColControlOutlineLighter = Color.FromArgb(255, 210, 210, 210);
		mColHighlightBg = Color.FromArgb(255, 192, 221, 252);
		mColHighlightBorder = Color.FromArgb(255, 51, 153, 255);
		mColToolTipBackground = Color.FromArgb(255, 255, 255, 225);
		mColToolTipBorder = Color.FromArgb(255, 0, 0, 0);
		mColModal = Color.FromArgb(150, 25, 25, 25);
	}

	public override void DrawButton(Button button)
	{
		ComponentState componentState = button.ComponentState;
		DrawButton(button.Width, button.Height, componentState == ComponentState.Active, componentState == ComponentState.Hovered);
	}

	public override void DrawMenuItem(Intersect.Client.Framework.Gwen.Control.Base control, bool submenuOpen, bool isChecked)
	{
		Rectangle renderBounds = control.RenderBounds;
		if (submenuOpen || control.IsHovered)
		{
			mRenderer.DrawColor = mColHighlightBg;
			mRenderer.DrawFilledRect(renderBounds);
			mRenderer.DrawColor = mColHighlightBorder;
			mRenderer.DrawLinedRect(renderBounds);
		}
		if (isChecked)
		{
			mRenderer.DrawColor = Color.FromArgb(255, 0, 0, 0);
			Rectangle rect = new Rectangle(control.Width / 2 - 2, control.Height / 2 - 2, 5, 5);
			DrawCheck(rect);
		}
	}

	public override void DrawMenuStrip(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		int width = control.Width;
		int height = control.Height;
		mRenderer.DrawColor = Color.FromArgb(255, 246, 248, 252);
		mRenderer.DrawFilledRect(new Rectangle(0, 0, width, height));
		mRenderer.DrawColor = Color.FromArgb(150, 218, 224, 241);
		mRenderer.DrawFilledRect(Util.FloatRect(0f, (float)height * 0.4f, width, (float)height * 0.6f));
		mRenderer.DrawFilledRect(Util.FloatRect(0f, (float)height * 0.5f, width, (float)height * 0.5f));
	}

	public override void DrawMenu(Intersect.Client.Framework.Gwen.Control.Base control, bool paddingDisabled)
	{
		int width = control.Width;
		int height = control.Height;
		mRenderer.DrawColor = mColControlBright;
		mRenderer.DrawFilledRect(new Rectangle(0, 0, width, height));
		if (!paddingDisabled)
		{
			mRenderer.DrawColor = mColControl;
			mRenderer.DrawFilledRect(new Rectangle(1, 0, 22, height));
		}
		mRenderer.DrawColor = mColControlOutlineNormal;
		mRenderer.DrawLinedRect(new Rectangle(0, 0, width, height));
	}

	public override void DrawShadow(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		int width = control.Width;
		int height = control.Height;
		int num = 4;
		int num2 = 6;
		mRenderer.DrawColor = Color.FromArgb(10, 0, 0, 0);
		mRenderer.DrawFilledRect(new Rectangle(num, num2, width, height));
		num += 2;
		mRenderer.DrawFilledRect(new Rectangle(num, num2, width, height));
		num2 += 2;
		mRenderer.DrawFilledRect(new Rectangle(num, num2, width, height));
	}

	public virtual void DrawButton(int w, int h, bool depressed, bool bHovered, bool bSquared = false)
	{
		if (depressed)
		{
			mRenderer.DrawColor = mColControlDark;
		}
		else if (bHovered)
		{
			mRenderer.DrawColor = mColControlBright;
		}
		else
		{
			mRenderer.DrawColor = mColControl;
		}
		mRenderer.DrawFilledRect(new Rectangle(1, 1, w - 2, h - 2));
		if (depressed)
		{
			mRenderer.DrawColor = mColControlDark;
		}
		else if (bHovered)
		{
			mRenderer.DrawColor = mColControl;
		}
		else
		{
			mRenderer.DrawColor = mColControlDark;
		}
		mRenderer.DrawFilledRect(Util.FloatRect(1f, (float)h * 0.5f, w - 2, (float)h * 0.5f - 2f));
		if (!depressed)
		{
			mRenderer.DrawColor = mColControlBright;
		}
		else
		{
			mRenderer.DrawColor = mColControlDarker;
		}
		mRenderer.DrawShavedCornerRect(new Rectangle(1, 1, w - 2, h - 2), bSquared);
		mRenderer.DrawColor = mColControlOutlineNormal;
		mRenderer.DrawShavedCornerRect(new Rectangle(0, 0, w, h), bSquared);
	}

	public override void DrawRadioButton(Intersect.Client.Framework.Gwen.Control.Base control, bool selected, bool depressed)
	{
		Rectangle renderBounds = control.RenderBounds;
		if (control.IsHovered)
		{
			mRenderer.DrawColor = Color.FromArgb(255, 220, 242, 254);
		}
		else
		{
			mRenderer.DrawColor = mColControlBright;
		}
		mRenderer.DrawFilledRect(new Rectangle(1, 1, renderBounds.Width - 2, renderBounds.Height - 2));
		if (control.IsHovered)
		{
			mRenderer.DrawColor = Color.FromArgb(255, 85, 130, 164);
		}
		else
		{
			mRenderer.DrawColor = mColControlOutlineLight;
		}
		mRenderer.DrawShavedCornerRect(renderBounds);
		mRenderer.DrawColor = Color.FromArgb(15, 0, 50, 60);
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + 2, renderBounds.Y + 2, renderBounds.Width - 4, renderBounds.Height - 4));
		mRenderer.DrawFilledRect(Util.FloatRect(renderBounds.X + 2, renderBounds.Y + 2, (float)renderBounds.Width * 0.3f, renderBounds.Height - 4));
		mRenderer.DrawFilledRect(Util.FloatRect(renderBounds.X + 2, renderBounds.Y + 2, renderBounds.Width - 4, (float)renderBounds.Height * 0.3f));
		if (control.IsHovered)
		{
			mRenderer.DrawColor = Color.FromArgb(255, 121, 198, 249);
		}
		else
		{
			mRenderer.DrawColor = Color.FromArgb(50, 0, 50, 60);
		}
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + 2, renderBounds.Y + 3, 1, renderBounds.Height - 5));
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + 3, renderBounds.Y + 2, renderBounds.Width - 5, 1));
		if (selected)
		{
			mRenderer.DrawColor = Color.FromArgb(255, 40, 230, 30);
			mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + 2, renderBounds.Y + 2, renderBounds.Width - 4, renderBounds.Height - 4));
		}
	}

	public override void DrawCheckBox(Intersect.Client.Framework.Gwen.Control.Base control, bool selected, bool depressed)
	{
		Rectangle renderBounds = control.RenderBounds;
		if (control.IsHovered)
		{
			mRenderer.DrawColor = Color.FromArgb(255, 220, 242, 254);
		}
		else
		{
			mRenderer.DrawColor = mColControlBright;
		}
		mRenderer.DrawFilledRect(renderBounds);
		if (control.IsHovered)
		{
			mRenderer.DrawColor = Color.FromArgb(255, 85, 130, 164);
		}
		else
		{
			mRenderer.DrawColor = mColControlOutlineLight;
		}
		mRenderer.DrawLinedRect(renderBounds);
		mRenderer.DrawColor = Color.FromArgb(15, 0, 50, 60);
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + 2, renderBounds.Y + 2, renderBounds.Width - 4, renderBounds.Height - 4));
		mRenderer.DrawFilledRect(Util.FloatRect(renderBounds.X + 2, renderBounds.Y + 2, (float)renderBounds.Width * 0.3f, renderBounds.Height - 4));
		mRenderer.DrawFilledRect(Util.FloatRect(renderBounds.X + 2, renderBounds.Y + 2, renderBounds.Width - 4, (float)renderBounds.Height * 0.3f));
		if (control.IsHovered)
		{
			mRenderer.DrawColor = Color.FromArgb(255, 121, 198, 249);
		}
		else
		{
			mRenderer.DrawColor = Color.FromArgb(50, 0, 50, 60);
		}
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + 2, renderBounds.Y + 2, 1, renderBounds.Height - 4));
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + 2, renderBounds.Y + 2, renderBounds.Width - 4, 1));
		if (depressed)
		{
			mRenderer.DrawColor = Color.FromArgb(255, 100, 100, 100);
			Rectangle rect = new Rectangle(control.Width / 2 - 2, control.Height / 2 - 2, 5, 5);
			DrawCheck(rect);
		}
		else if (selected)
		{
			mRenderer.DrawColor = Color.FromArgb(255, 0, 0, 0);
			Rectangle rect2 = new Rectangle(control.Width / 2 - 2, control.Height / 2 - 2, 5, 5);
			DrawCheck(rect2);
		}
	}

	public override void DrawGroupBox(Intersect.Client.Framework.Gwen.Control.Base control, int textStart, int textHeight, int textWidth)
	{
		Rectangle renderBounds = control.RenderBounds;
		renderBounds.Y += (int)((float)textHeight * 0.5f);
		renderBounds.Height -= (int)((float)textHeight * 0.5f);
		Color drawColor = Color.FromArgb(50, 0, 50, 60);
		Color drawColor2 = Color.FromArgb(150, 255, 255, 255);
		mRenderer.DrawColor = drawColor2;
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + 1, renderBounds.Y + 1, textStart - 3, 1));
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + 1 + textStart + textWidth, renderBounds.Y + 1, renderBounds.Width - textStart + textWidth - 2, 1));
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + 1, renderBounds.Y + renderBounds.Height - 1, renderBounds.Width - 2, 1));
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + 1, renderBounds.Y + 1, 1, renderBounds.Height));
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + renderBounds.Width - 2, renderBounds.Y + 1, 1, renderBounds.Height - 1));
		mRenderer.DrawColor = drawColor;
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + 1, renderBounds.Y, textStart - 3, 1));
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + 1 + textStart + textWidth, renderBounds.Y, renderBounds.Width - textStart - textWidth - 2, 1));
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + 1, renderBounds.Y + renderBounds.Height - 1, renderBounds.Width - 2, 1));
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X, renderBounds.Y + 1, 1, renderBounds.Height - 1));
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + renderBounds.Width - 1, renderBounds.Y + 1, 1, renderBounds.Height - 1));
	}

	public override void DrawTextBox(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		Rectangle renderBounds = control.RenderBounds;
		bool hasFocus = control.HasFocus;
		mRenderer.DrawColor = Color.FromArgb(255, 255, 255, 255);
		mRenderer.DrawFilledRect(new Rectangle(1, 1, renderBounds.Width - 2, renderBounds.Height - 2));
		mRenderer.DrawColor = mColControlOutlineLight;
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + 1, renderBounds.Y, renderBounds.Width - 2, 1));
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X, renderBounds.Y + 1, 1, renderBounds.Height - 2));
		mRenderer.DrawColor = mColControlOutlineLighter;
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + 1, renderBounds.Y + renderBounds.Height - 1, renderBounds.Width - 2, 1));
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + renderBounds.Width - 1, renderBounds.Y + 1, 1, renderBounds.Height - 2));
		if (hasFocus)
		{
			mRenderer.DrawColor = Color.FromArgb(150, 50, 200, 255);
			mRenderer.DrawLinedRect(renderBounds);
		}
	}

	public override void DrawTabButton(Intersect.Client.Framework.Gwen.Control.Base control, bool active, Pos dir)
	{
		Rectangle renderBounds = control.RenderBounds;
		bool isHovered = control.IsHovered;
		if (isHovered)
		{
			mRenderer.DrawColor = mColControlBright;
		}
		else
		{
			mRenderer.DrawColor = mColControl;
		}
		mRenderer.DrawFilledRect(new Rectangle(1, 1, renderBounds.Width - 2, renderBounds.Height - 1));
		if (isHovered)
		{
			mRenderer.DrawColor = mColControl;
		}
		else
		{
			mRenderer.DrawColor = mColControlDark;
		}
		mRenderer.DrawFilledRect(Util.FloatRect(1f, (float)renderBounds.Height * 0.5f, renderBounds.Width - 2, (float)renderBounds.Height * 0.5f - 1f));
		mRenderer.DrawColor = mColControlBright;
		mRenderer.DrawShavedCornerRect(new Rectangle(1, 1, renderBounds.Width - 2, renderBounds.Height));
		mRenderer.DrawColor = mColBorderColor;
		mRenderer.DrawShavedCornerRect(new Rectangle(0, 0, renderBounds.Width, renderBounds.Height));
	}

	public override void DrawTabControl(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		Rectangle renderBounds = control.RenderBounds;
		mRenderer.DrawColor = mColControl;
		mRenderer.DrawFilledRect(renderBounds);
		mRenderer.DrawColor = mColBorderColor;
		mRenderer.DrawLinedRect(renderBounds);
	}

	public override void DrawWindow(Intersect.Client.Framework.Gwen.Control.Base control, int topHeight, bool inFocus)
	{
		Rectangle renderBounds = control.RenderBounds;
		if (inFocus)
		{
			mRenderer.DrawColor = Color.FromArgb(230, 87, 164, 232);
		}
		else
		{
			mRenderer.DrawColor = Color.FromArgb(230, 60, 114, 162);
		}
		int num = 5;
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + 1, renderBounds.Y + 1, renderBounds.Width - 2, topHeight - 1));
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + 1, renderBounds.Y + topHeight - 1, num, renderBounds.Height - 2 - topHeight));
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + renderBounds.Width - num, renderBounds.Y + topHeight - 1, num, renderBounds.Height - 2 - topHeight));
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + 1, renderBounds.Y + renderBounds.Height - num, renderBounds.Width - 2, num));
		mRenderer.DrawColor = Color.FromArgb(230, mColControlDark.R, mColControlDark.G, mColControlDark.B);
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + num + 1, renderBounds.Y + topHeight, renderBounds.Width - num * 2 - 2, renderBounds.Height - topHeight - num - 1));
		mRenderer.DrawColor = Color.FromArgb(100, 255, 255, 255);
		mRenderer.DrawShavedCornerRect(new Rectangle(renderBounds.X + 1, renderBounds.Y + 1, renderBounds.Width - 2, renderBounds.Height - 2));
		mRenderer.DrawColor = mColBorderColor;
		mRenderer.DrawColor = mColControlOutlineNormal;
		mRenderer.DrawLinedRect(new Rectangle(renderBounds.X + num, renderBounds.Y + topHeight - 1, renderBounds.Width - 10, renderBounds.Height - topHeight - (num - 1)));
		mRenderer.DrawColor = mColBorderColor;
		mRenderer.DrawShavedCornerRect(new Rectangle(renderBounds.X, renderBounds.Y, renderBounds.Width, renderBounds.Height));
	}

	public override void DrawWindowCloseButton(CloseButton closeButton)
	{
		ComponentState componentState = closeButton.ComponentState;
		DrawButton(closeButton.Width, closeButton.Height, componentState == ComponentState.Active, componentState == ComponentState.Hovered);
	}

	public override void DrawHighlight(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		Rectangle renderBounds = control.RenderBounds;
		mRenderer.DrawColor = Color.FromArgb(255, 255, 100, 255);
		mRenderer.DrawFilledRect(renderBounds);
	}

	public override void DrawScrollBar(Intersect.Client.Framework.Gwen.Control.Base control, bool horizontal, bool depressed)
	{
		Rectangle renderBounds = control.RenderBounds;
		if (depressed)
		{
			mRenderer.DrawColor = mColControlDarker;
		}
		else
		{
			mRenderer.DrawColor = mColControlBright;
		}
		mRenderer.DrawFilledRect(renderBounds);
	}

	public override void DrawScrollBarBar(ScrollBarBar scrollBarBar)
	{
		ComponentState componentState = scrollBarBar.ComponentState;
		DrawButton(scrollBarBar.Width, scrollBarBar.Height, componentState == ComponentState.Active, componentState == ComponentState.Hovered);
	}

	public override void DrawTabTitleBar(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		Rectangle renderBounds = control.RenderBounds;
		mRenderer.DrawColor = Color.FromArgb(255, 177, 193, 214);
		mRenderer.DrawFilledRect(renderBounds);
		mRenderer.DrawColor = mColBorderColor;
		renderBounds.Height++;
		mRenderer.DrawLinedRect(renderBounds);
	}

	public override void DrawProgressBar(Intersect.Client.Framework.Gwen.Control.Base control, bool horizontal, float progress)
	{
		Rectangle renderBounds = control.RenderBounds;
		Color drawColor = Color.FromArgb(255, 0, 211, 40);
		if (horizontal)
		{
			mRenderer.DrawColor = mColControlDark;
			mRenderer.DrawFilledRect(new Rectangle(1, 1, renderBounds.Width - 2, renderBounds.Height - 2));
			mRenderer.DrawColor = drawColor;
			mRenderer.DrawFilledRect(Util.FloatRect(1f, 1f, (float)renderBounds.Width * progress - 2f, renderBounds.Height - 2));
			mRenderer.DrawColor = Color.FromArgb(150, 255, 255, 255);
			mRenderer.DrawFilledRect(Util.FloatRect(1f, 1f, renderBounds.Width - 2, (float)renderBounds.Height * 0.45f));
		}
		else
		{
			mRenderer.DrawColor = mColControlDark;
			mRenderer.DrawFilledRect(new Rectangle(1, 1, renderBounds.Width - 2, renderBounds.Height - 2));
			mRenderer.DrawColor = drawColor;
			mRenderer.DrawFilledRect(Util.FloatRect(1f, 1f + (float)renderBounds.Height * (1f - progress), renderBounds.Width - 2, (float)renderBounds.Height * progress - 2f));
			mRenderer.DrawColor = Color.FromArgb(150, 255, 255, 255);
			mRenderer.DrawFilledRect(Util.FloatRect(1f, 1f, (float)renderBounds.Width * 0.45f, renderBounds.Height - 2));
		}
		mRenderer.DrawColor = Color.FromArgb(150, 255, 255, 255);
		mRenderer.DrawShavedCornerRect(new Rectangle(1, 1, renderBounds.Width - 2, renderBounds.Height - 2));
		mRenderer.DrawColor = Color.FromArgb(70, 255, 255, 255);
		mRenderer.DrawShavedCornerRect(new Rectangle(2, 2, renderBounds.Width - 4, renderBounds.Height - 4));
		mRenderer.DrawColor = mColBorderColor;
		mRenderer.DrawShavedCornerRect(renderBounds);
	}

	public override void DrawListBox(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		Rectangle renderBounds = control.RenderBounds;
		mRenderer.DrawColor = mColControlBright;
		mRenderer.DrawFilledRect(renderBounds);
		mRenderer.DrawColor = mColBorderColor;
		mRenderer.DrawLinedRect(renderBounds);
	}

	public override void DrawListBoxLine(Intersect.Client.Framework.Gwen.Control.Base control, bool selected, bool even)
	{
		Rectangle renderBounds = control.RenderBounds;
		if (selected)
		{
			mRenderer.DrawColor = mColHighlightBorder;
			mRenderer.DrawFilledRect(renderBounds);
		}
		else if (control.IsHovered)
		{
			mRenderer.DrawColor = mColHighlightBg;
			mRenderer.DrawFilledRect(renderBounds);
		}
	}

	public override void DrawSlider(Intersect.Client.Framework.Gwen.Control.Base control, bool horizontal, double[] notches, int numNotches, int barSize)
	{
		Rectangle renderBounds = control.RenderBounds;
		Rectangle rectangle = renderBounds;
		if (horizontal)
		{
			renderBounds.Y += (int)((float)renderBounds.Height * 0.4f);
			renderBounds.Height -= (int)((float)renderBounds.Height * 0.8f);
		}
		else
		{
			renderBounds.X += (int)((float)renderBounds.Width * 0.4f);
			renderBounds.Width -= (int)((float)renderBounds.Width * 0.8f);
		}
		mRenderer.DrawColor = mColBgDark;
		mRenderer.DrawFilledRect(renderBounds);
		mRenderer.DrawColor = mColControlDarker;
		mRenderer.DrawLinedRect(renderBounds);
	}

	public override void DrawComboBox(Intersect.Client.Framework.Gwen.Control.Base control, bool down, bool open)
	{
		DrawTextBox(control);
	}

	public override void DrawKeyboardHighlight(Intersect.Client.Framework.Gwen.Control.Base control, Rectangle r, int iOffset)
	{
		Rectangle rectangle = r;
		rectangle.X += iOffset;
		rectangle.Y += iOffset;
		rectangle.Width -= iOffset * 2;
		rectangle.Height -= iOffset * 2;
		bool flag = true;
		for (int i = 0; (double)i < (double)rectangle.Width * 0.5; i++)
		{
			mRenderer.DrawColor = Color.FromArgb(255, 0, 0, 0);
			if (!flag)
			{
				mRenderer.DrawPixel(rectangle.X + i * 2, rectangle.Y);
				mRenderer.DrawPixel(rectangle.X + i * 2, rectangle.Y + rectangle.Height - 1);
			}
			else
			{
				flag = false;
			}
		}
		for (int j = 0; (double)j < (double)rectangle.Height * 0.5; j++)
		{
			mRenderer.DrawColor = Color.FromArgb(255, 0, 0, 0);
			mRenderer.DrawPixel(rectangle.X, rectangle.Y + j * 2);
			mRenderer.DrawPixel(rectangle.X + rectangle.Width - 1, rectangle.Y + j * 2);
		}
	}

	public override void DrawToolTip(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		Rectangle renderBounds = control.RenderBounds;
		renderBounds.X -= 3;
		renderBounds.Y -= 3;
		renderBounds.Width += 6;
		renderBounds.Height += 6;
		mRenderer.DrawColor = mColToolTipBackground;
		mRenderer.DrawFilledRect(renderBounds);
		mRenderer.DrawColor = mColToolTipBorder;
		mRenderer.DrawLinedRect(renderBounds);
	}

	public override void DrawScrollButton(Intersect.Client.Framework.Gwen.Control.Base control, Pos direction, bool depressed, bool hovered, bool disabled)
	{
		ComponentState componentState = control.ComponentState;
		DrawButton(control.Width, control.Height, componentState == ComponentState.Active, componentState == ComponentState.Hovered);
		mRenderer.DrawColor = Color.FromArgb(240, 0, 0, 0);
		Rectangle rect = new Rectangle(control.Width / 2 - 2, control.Height / 2 - 2, 5, 5);
		switch (direction)
		{
		case Pos.Top:
			DrawArrowUp(rect);
			break;
		case Pos.Bottom:
			DrawArrowDown(rect);
			break;
		case Pos.Left:
			DrawArrowLeft(rect);
			break;
		default:
			DrawArrowRight(rect);
			break;
		}
	}

	public override void DrawComboBoxArrow(Intersect.Client.Framework.Gwen.Control.Base control, bool hovered, bool depressed, bool open, bool disabled)
	{
		mRenderer.DrawColor = Color.FromArgb(240, 0, 0, 0);
		Rectangle rect = new Rectangle(control.Width / 2 - 2, control.Height / 2 - 2, 5, 5);
		DrawArrowDown(rect);
	}

	public override void DrawNumericUpDownButton(Intersect.Client.Framework.Gwen.Control.Base control, bool depressed, bool up)
	{
		mRenderer.DrawColor = Color.FromArgb(240, 0, 0, 0);
		Rectangle rect = new Rectangle(control.Width / 2 - 2, control.Height / 2 - 2, 5, 5);
		if (up)
		{
			DrawArrowUp(rect);
		}
		else
		{
			DrawArrowDown(rect);
		}
	}

	public override void DrawTreeButton(Intersect.Client.Framework.Gwen.Control.Base control, bool open)
	{
		Rectangle renderBounds = control.RenderBounds;
		renderBounds.X += 2;
		renderBounds.Y += 2;
		renderBounds.Width -= 4;
		renderBounds.Height -= 4;
		mRenderer.DrawColor = mColControlBright;
		mRenderer.DrawFilledRect(renderBounds);
		mRenderer.DrawColor = mColBorderColor;
		mRenderer.DrawLinedRect(renderBounds);
		mRenderer.DrawColor = mColBorderColor;
		if (!open)
		{
			mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + renderBounds.Width / 2, renderBounds.Y + 2, 1, renderBounds.Height - 4));
		}
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + 2, renderBounds.Y + renderBounds.Height / 2, renderBounds.Width - 4, 1));
	}

	public override void DrawTreeControl(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		Rectangle renderBounds = control.RenderBounds;
		mRenderer.DrawColor = mColControlBright;
		mRenderer.DrawFilledRect(renderBounds);
		mRenderer.DrawColor = mColBorderColor;
		mRenderer.DrawLinedRect(renderBounds);
	}

	public override void DrawTreeNode(Intersect.Client.Framework.Gwen.Control.Base ctrl, bool open, bool selected, int treeNodeHeight, int labelHeight, int labelWidth, int halfWay, int lastBranch, bool isRoot)
	{
		if (selected)
		{
			base.Renderer.DrawColor = Color.FromArgb(100, 0, 150, 255);
			base.Renderer.DrawFilledRect(new Rectangle(17, 0, labelWidth + 2, treeNodeHeight - 1));
			base.Renderer.DrawColor = Color.FromArgb(200, 0, 150, 255);
			base.Renderer.DrawLinedRect(new Rectangle(17, 0, labelWidth + 2, treeNodeHeight - 1));
		}
		base.DrawTreeNode(ctrl, open, selected, treeNodeHeight, labelHeight, labelWidth, halfWay, lastBranch, isRoot);
	}

	public override void DrawStatusBar(Intersect.Client.Framework.Gwen.Control.Base control)
	{
	}

	public override void DrawColorDisplay(Intersect.Client.Framework.Gwen.Control.Base control, Color color)
	{
		Rectangle renderBounds = control.RenderBounds;
		if (color.A != byte.MaxValue)
		{
			base.Renderer.DrawColor = Color.FromArgb(255, 255, 255, 255);
			base.Renderer.DrawFilledRect(renderBounds);
			base.Renderer.DrawColor = Color.FromArgb(128, 128, 128, 128);
			base.Renderer.DrawFilledRect(Util.FloatRect(0f, 0f, (float)renderBounds.Width * 0.5f, (float)renderBounds.Height * 0.5f));
			base.Renderer.DrawFilledRect(Util.FloatRect((float)renderBounds.Width * 0.5f, (float)renderBounds.Height * 0.5f, (float)renderBounds.Width * 0.5f, (float)renderBounds.Height * 0.5f));
		}
		base.Renderer.DrawColor = color;
		base.Renderer.DrawFilledRect(renderBounds);
		base.Renderer.DrawColor = Color.FromArgb(255, 0, 0, 0);
		base.Renderer.DrawLinedRect(renderBounds);
	}

	public override void DrawModalControl(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		if (control.ShouldDrawBackground)
		{
			Rectangle renderBounds = control.RenderBounds;
			base.Renderer.DrawColor = mColModal;
			base.Renderer.DrawFilledRect(renderBounds);
		}
	}

	public override void DrawMenuDivider(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		Rectangle renderBounds = control.RenderBounds;
		base.Renderer.DrawColor = mColBgDark;
		base.Renderer.DrawFilledRect(renderBounds);
		base.Renderer.DrawColor = mColControlDarker;
		base.Renderer.DrawLinedRect(renderBounds);
	}

	public override void DrawMenuRightArrow(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		DrawArrowRight(control.RenderBounds);
	}

	public override void DrawSliderButton(SliderBar sliderBar)
	{
		ComponentState componentState = sliderBar.ComponentState;
		DrawButton(sliderBar.Width, sliderBar.Height, componentState == ComponentState.Active, componentState == ComponentState.Hovered);
	}

	public override void DrawCategoryHolder(Intersect.Client.Framework.Gwen.Control.Base control)
	{
	}

	public override void DrawCategoryInner(Intersect.Client.Framework.Gwen.Control.Base control, bool collapsed)
	{
	}
}
