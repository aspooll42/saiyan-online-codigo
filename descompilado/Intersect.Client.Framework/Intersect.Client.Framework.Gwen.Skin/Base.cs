using System;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Renderer;

namespace Intersect.Client.Framework.Gwen.Skin;

public class Base : IDisposable
{
	protected readonly Intersect.Client.Framework.Gwen.Renderer.Base mRenderer;

	public SkinColors Colors;

	protected IFont mDefaultFont;

	public static Color DefaultTextPaddingDebugColor = Color.FromHex("79b5d2", Color.White);

	public IFont DefaultFont
	{
		get
		{
			return mDefaultFont;
		}
		set
		{
			mDefaultFont = value;
		}
	}

	public int DefaultFontSize { get; set; }

	public Intersect.Client.Framework.Gwen.Renderer.Base Renderer => mRenderer;

	protected Base(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		mDefaultFont = null;
		mRenderer = renderer;
	}

	public virtual void Dispose()
	{
		GC.SuppressFinalize(this);
	}

	public virtual void DrawSplitter(SplitterBar splitterBar)
	{
	}

	public virtual void DrawButton(Button button)
	{
	}

	public virtual void DrawPanel(Panel panel)
	{
	}

	public virtual void DrawTabButton(Intersect.Client.Framework.Gwen.Control.Base control, bool active, Pos dir)
	{
	}

	public virtual void DrawTabControl(Intersect.Client.Framework.Gwen.Control.Base control)
	{
	}

	public virtual void DrawTabTitleBar(Intersect.Client.Framework.Gwen.Control.Base control)
	{
	}

	public virtual void DrawMenuItem(Intersect.Client.Framework.Gwen.Control.Base control, bool submenuOpen, bool isChecked)
	{
	}

	public virtual void DrawMenuRightArrow(Intersect.Client.Framework.Gwen.Control.Base control)
	{
	}

	public virtual void DrawMenuStrip(Intersect.Client.Framework.Gwen.Control.Base control)
	{
	}

	public virtual void DrawMenu(Intersect.Client.Framework.Gwen.Control.Base control, bool paddingDisabled)
	{
	}

	public virtual void DrawRadioButton(Intersect.Client.Framework.Gwen.Control.Base control, bool selected, bool depressed)
	{
	}

	public virtual void DrawRadioButton(Intersect.Client.Framework.Gwen.Control.Base control, bool selected, bool hovered, bool depressed)
	{
		DrawRadioButton(control, selected, depressed);
	}

	public virtual void DrawCheckBox(Intersect.Client.Framework.Gwen.Control.Base control, bool selected, bool depressed)
	{
	}

	public virtual void DrawCheckBox(Intersect.Client.Framework.Gwen.Control.Base control, bool selected, bool hovered, bool depressed)
	{
		DrawCheckBox(control, selected, depressed);
	}

	public virtual void DrawGroupBox(Intersect.Client.Framework.Gwen.Control.Base control, int textStart, int textHeight, int textWidth)
	{
	}

	public virtual void DrawTextBox(Intersect.Client.Framework.Gwen.Control.Base control)
	{
	}

	public virtual void DrawWindow(Intersect.Client.Framework.Gwen.Control.Base control, int topHeight, bool inFocus)
	{
	}

	public virtual void DrawWindowCloseButton(CloseButton closeButton)
	{
	}

	public virtual void DrawHighlight(Intersect.Client.Framework.Gwen.Control.Base control)
	{
	}

	public virtual void DrawStatusBar(Intersect.Client.Framework.Gwen.Control.Base control)
	{
	}

	public virtual void DrawShadow(Intersect.Client.Framework.Gwen.Control.Base control)
	{
	}

	public virtual void DrawScrollBarBar(ScrollBarBar scrollBarBar)
	{
	}

	public virtual void DrawScrollBar(Intersect.Client.Framework.Gwen.Control.Base control, bool horizontal, bool depressed)
	{
	}

	public virtual void DrawScrollButton(Intersect.Client.Framework.Gwen.Control.Base control, Pos direction, bool depressed, bool hovered, bool disabled)
	{
	}

	public virtual void DrawProgressBar(Intersect.Client.Framework.Gwen.Control.Base control, bool horizontal, float progress)
	{
	}

	public virtual void DrawListBox(Intersect.Client.Framework.Gwen.Control.Base control)
	{
	}

	public virtual void DrawListBoxLine(Intersect.Client.Framework.Gwen.Control.Base control, bool selected, bool even)
	{
	}

	public virtual void DrawSlider(Intersect.Client.Framework.Gwen.Control.Base control, bool horizontal, double[]? notches, int numNotches, int barSize)
	{
	}

	public virtual void DrawSlider(Slider slider, Orientation orientation, double[]? notches, int numNotches, int barSize)
	{
		bool horizontal = (uint)orientation <= 1u;
		DrawSlider(slider, horizontal, notches, numNotches, barSize);
	}

	public virtual void DrawSliderButton(SliderBar sliderBar)
	{
	}

	public virtual void DrawComboBox(Intersect.Client.Framework.Gwen.Control.Base control, bool down, bool isMenuOpen)
	{
	}

	public virtual void DrawComboBoxArrow(Intersect.Client.Framework.Gwen.Control.Base control, bool hovered, bool depressed, bool open, bool disabled)
	{
	}

	public virtual void DrawKeyboardHighlight(Intersect.Client.Framework.Gwen.Control.Base control, Rectangle rect, int offset)
	{
	}

	public virtual void DrawToolTip(Intersect.Client.Framework.Gwen.Control.Base control)
	{
	}

	public virtual void DrawNumericUpDownButton(Intersect.Client.Framework.Gwen.Control.Base control, bool depressed, bool up)
	{
	}

	public virtual void DrawTreeButton(Intersect.Client.Framework.Gwen.Control.Base control, bool open)
	{
	}

	public virtual void DrawTreeControl(Intersect.Client.Framework.Gwen.Control.Base control)
	{
	}

	public virtual void DrawDebugOutlines(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		if (control.IsHidden)
		{
			return;
		}
		Margin margin = control.Margin;
		if (margin != default(Margin))
		{
			Rectangle rect = new Rectangle(control.Bounds.Left - margin.Left, control.Bounds.Top - margin.Top, control.Bounds.Width + margin.Right + margin.Left, control.Bounds.Height + margin.Bottom + margin.Top);
			DrawRectStroke(rect, control.MarginOutlineColor);
		}
		Padding padding = control.Padding;
		if (padding != default(Padding))
		{
			Color color = control.PaddingOutlineColor;
			if (control is ITextContainer textContainer)
			{
				color = textContainer.TextPaddingDebugColor ?? DefaultTextPaddingDebugColor;
			}
			Rectangle rect2 = new Rectangle(control.Bounds.Left + padding.Left, control.Bounds.Top + padding.Top, control.Bounds.Width - padding.Right - padding.Left, control.Bounds.Height - padding.Bottom - padding.Top);
			DrawRectStroke(rect2, color);
		}
		DrawRectStroke(control.Bounds, control.BoundsOutlineColor);
	}

	public virtual void DrawRectStroke(Rectangle rect, Color color)
	{
		DrawRect(rect, color, filled: false);
	}

	public virtual void DrawRectFill(Rectangle rect, Color color)
	{
		DrawRect(rect, color, filled: true);
	}

	public virtual void DrawRect(Rectangle rect, Color color, bool filled)
	{
		mRenderer.DrawColor = color;
		if (filled)
		{
			mRenderer.DrawFilledRect(rect);
		}
		else
		{
			mRenderer.DrawLinedRect(rect);
		}
	}

	public virtual void DrawTreeNode(Intersect.Client.Framework.Gwen.Control.Base ctrl, bool open, bool selected, int treeNodeHeight, int labelHeight, int labelWidth, int halfWay, int lastBranch, bool isRoot)
	{
		Renderer.DrawColor = Colors.Tree.Lines;
		if (!isRoot)
		{
			Renderer.DrawFilledRect(new Rectangle(8, halfWay, 7, 1));
		}
		if (open)
		{
			Renderer.DrawFilledRect(new Rectangle(21, labelHeight + 1, 1, lastBranch + halfWay - labelHeight));
		}
	}

	public virtual void DrawPropertyRow(Intersect.Client.Framework.Gwen.Control.Base control, int iWidth, bool bBeingEdited, bool hovered)
	{
		Rectangle renderBounds = control.RenderBounds;
		if (bBeingEdited)
		{
			mRenderer.DrawColor = Colors.Properties.ColumnSelected;
		}
		else if (hovered)
		{
			mRenderer.DrawColor = Colors.Properties.ColumnHover;
		}
		else
		{
			mRenderer.DrawColor = Colors.Properties.ColumnNormal;
		}
		mRenderer.DrawFilledRect(new Rectangle(0, renderBounds.Y, iWidth, renderBounds.Height));
		if (bBeingEdited)
		{
			mRenderer.DrawColor = Colors.Properties.LineSelected;
		}
		else if (hovered)
		{
			mRenderer.DrawColor = Colors.Properties.LineHover;
		}
		else
		{
			mRenderer.DrawColor = Colors.Properties.LineNormal;
		}
		mRenderer.DrawFilledRect(new Rectangle(iWidth, renderBounds.Y, 1, renderBounds.Height));
		renderBounds.Y += renderBounds.Height - 1;
		renderBounds.Height = 1;
		mRenderer.DrawFilledRect(renderBounds);
	}

	public virtual void DrawColorDisplay(Intersect.Client.Framework.Gwen.Control.Base control, Color color)
	{
	}

	public virtual void DrawModalControl(Intersect.Client.Framework.Gwen.Control.Base control)
	{
	}

	public virtual void DrawMenuDivider(Intersect.Client.Framework.Gwen.Control.Base control)
	{
	}

	public virtual void DrawCategoryHolder(Intersect.Client.Framework.Gwen.Control.Base control)
	{
	}

	public virtual void DrawCategoryInner(Intersect.Client.Framework.Gwen.Control.Base control, bool collapsed)
	{
	}

	public virtual void DrawPropertyTreeNode(Intersect.Client.Framework.Gwen.Control.Base control, int borderLeft, int borderTop)
	{
		Rectangle renderBounds = control.RenderBounds;
		mRenderer.DrawColor = Colors.Properties.Border;
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X, renderBounds.Y, borderLeft, renderBounds.Height));
		mRenderer.DrawFilledRect(new Rectangle(renderBounds.X + borderLeft, renderBounds.Y, renderBounds.Width - borderLeft, borderTop));
	}

	public virtual void DrawArrowDown(Rectangle rect)
	{
		float num = (float)rect.Width / 5f;
		float num2 = (float)rect.Height / 5f;
		mRenderer.DrawFilledRect(Util.FloatRect((float)rect.X + num * 0f, (float)rect.Y + num2 * 1f, num, num2 * 1f));
		mRenderer.DrawFilledRect(Util.FloatRect((float)rect.X + num * 1f, (float)rect.Y + num2 * 1f, num, num2 * 2f));
		mRenderer.DrawFilledRect(Util.FloatRect((float)rect.X + num * 2f, (float)rect.Y + num2 * 1f, num, num2 * 3f));
		mRenderer.DrawFilledRect(Util.FloatRect((float)rect.X + num * 3f, (float)rect.Y + num2 * 1f, num, num2 * 2f));
		mRenderer.DrawFilledRect(Util.FloatRect((float)rect.X + num * 4f, (float)rect.Y + num2 * 1f, num, num2 * 1f));
	}

	public virtual void DrawArrowUp(Rectangle rect)
	{
		float num = (float)rect.Width / 5f;
		float num2 = (float)rect.Height / 5f;
		mRenderer.DrawFilledRect(Util.FloatRect((float)rect.X + num * 0f, (float)rect.Y + num2 * 3f, num, num2 * 1f));
		mRenderer.DrawFilledRect(Util.FloatRect((float)rect.X + num * 1f, (float)rect.Y + num2 * 2f, num, num2 * 2f));
		mRenderer.DrawFilledRect(Util.FloatRect((float)rect.X + num * 2f, (float)rect.Y + num2 * 1f, num, num2 * 3f));
		mRenderer.DrawFilledRect(Util.FloatRect((float)rect.X + num * 3f, (float)rect.Y + num2 * 2f, num, num2 * 2f));
		mRenderer.DrawFilledRect(Util.FloatRect((float)rect.X + num * 4f, (float)rect.Y + num2 * 3f, num, num2 * 1f));
	}

	public virtual void DrawArrowLeft(Rectangle rect)
	{
		float num = (float)rect.Width / 5f;
		float num2 = (float)rect.Height / 5f;
		mRenderer.DrawFilledRect(Util.FloatRect((float)rect.X + num * 3f, (float)rect.Y + num2 * 0f, num * 1f, num2));
		mRenderer.DrawFilledRect(Util.FloatRect((float)rect.X + num * 2f, (float)rect.Y + num2 * 1f, num * 2f, num2));
		mRenderer.DrawFilledRect(Util.FloatRect((float)rect.X + num * 1f, (float)rect.Y + num2 * 2f, num * 3f, num2));
		mRenderer.DrawFilledRect(Util.FloatRect((float)rect.X + num * 2f, (float)rect.Y + num2 * 3f, num * 2f, num2));
		mRenderer.DrawFilledRect(Util.FloatRect((float)rect.X + num * 3f, (float)rect.Y + num2 * 4f, num * 1f, num2));
	}

	public virtual void DrawArrowRight(Rectangle rect)
	{
		float num = (float)rect.Width / 5f;
		float num2 = (float)rect.Height / 5f;
		mRenderer.DrawFilledRect(Util.FloatRect((float)rect.X + num * 1f, (float)rect.Y + num2 * 0f, num * 1f, num2));
		mRenderer.DrawFilledRect(Util.FloatRect((float)rect.X + num * 1f, (float)rect.Y + num2 * 1f, num * 2f, num2));
		mRenderer.DrawFilledRect(Util.FloatRect((float)rect.X + num * 1f, (float)rect.Y + num2 * 2f, num * 3f, num2));
		mRenderer.DrawFilledRect(Util.FloatRect((float)rect.X + num * 1f, (float)rect.Y + num2 * 3f, num * 2f, num2));
		mRenderer.DrawFilledRect(Util.FloatRect((float)rect.X + num * 1f, (float)rect.Y + num2 * 4f, num * 1f, num2));
	}

	public virtual void DrawCheck(Rectangle rect)
	{
		float num = (float)rect.Width / 5f;
		float num2 = (float)rect.Height / 5f;
		mRenderer.DrawFilledRect(Util.FloatRect((float)rect.X + num * 0f, (float)rect.Y + num2 * 3f, num * 2f, num2 * 2f));
		mRenderer.DrawFilledRect(Util.FloatRect((float)rect.X + num * 1f, (float)rect.Y + num2 * 4f, num * 2f, num2 * 2f));
		mRenderer.DrawFilledRect(Util.FloatRect((float)rect.X + num * 2f, (float)rect.Y + num2 * 3f, num * 2f, num2 * 2f));
		mRenderer.DrawFilledRect(Util.FloatRect((float)rect.X + num * 3f, (float)rect.Y + num2 * 1f, num * 2f, num2 * 2f));
		mRenderer.DrawFilledRect(Util.FloatRect((float)rect.X + num * 4f, (float)rect.Y + num2 * 0f, num * 2f, num2 * 2f));
	}

	public virtual void DrawLabel(Intersect.Client.Framework.Gwen.Control.Base control)
	{
	}
}
