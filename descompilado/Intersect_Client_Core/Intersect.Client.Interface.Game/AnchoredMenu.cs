using System;
using System.Collections.Generic;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Framework.Core;

namespace Intersect.Client.Interface.Game;

public abstract class AnchoredMenu : Intersect.Client.Framework.Gwen.Control.Menu
{
	private const int Gap = 20;

	private const int GapBelow = 6;

	private const long RefreshMs = 500L;

	private Intersect.Client.Framework.Gwen.Control.Base? _anchor;

	private Intersect.Client.Framework.Gwen.Control.Base? _lastAnchor;

	private long _closedOnAnchorAt;

	private long _nextRefresh;

	private bool _above = true;

	protected DropdownPanel Content { get; }

	protected AnchoredMenu(Canvas gameCanvas, string name, string title, int minimumWidth)
		: base(gameCanvas, name)
	{
		base.IsHidden = true;
		base.IconMarginDisabled = true;
		base.Padding = Padding.Zero;
		base.Overflow = OverflowBehavior.Hidden;
		ClearChildren();
		Content = new DropdownPanel(this, "Content")
		{
			Title = title.ToUpperInvariant(),
			HeaderHeight = 30,
			RowHeight = 42,
			MinimumWidth = minimumWidth
		};
	}

	protected abstract IEnumerable<DropdownRow> BuildRows();

	public void ToggleHidden(Intersect.Client.Framework.Gwen.Control.Base? target)
	{
		if (target != null)
		{
			if (!base.IsHidden)
			{
				Close();
				return;
			}
			if (target == _lastAnchor && Timing.Global.Milliseconds - _closedOnAnchorAt < 1500)
			{
				_closedOnAnchorAt = 0L;
				return;
			}
			_anchor = target;
			Refresh();
			Open(Pos.None);
			SetPosition(0, 0);
			Anchor();
		}
	}

	public override void Close()
	{
		if (!base.IsHidden && _anchor != null && InputHandler.HoveredControl == _anchor)
		{
			_closedOnAnchorAt = Timing.Global.Milliseconds;
		}
		_lastAnchor = _anchor;
		_anchor = null;
		base.Close();
	}

	public override void Think()
	{
		base.Think();
		if (!base.IsHidden && Timing.Global.Milliseconds >= _nextRefresh)
		{
			Refresh();
		}
	}

	protected override void OnPositioningBeforeOpen()
	{
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
	}

	protected override void RenderUnder(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		DropdownPanel.DrawPlate(skin.Renderer, base.Width, base.Height);
	}

	protected override void RenderOver(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.RenderOver(skin);
		Intersect.Client.Framework.Gwen.Control.Base anchor = _anchor;
		if (anchor != null && _above)
		{
			int value = anchor.ToCanvas(new Point(anchor.Width / 2, 0)).X - base.X;
			DropdownPanel.DrawNotch(skin.Renderer, Math.Clamp(value, 16, Math.Max(16, base.Width - 16)), base.Height);
		}
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Layout(skin);
		Anchor();
	}

	private void Refresh()
	{
		_nextRefresh = Timing.Global.Milliseconds + 500;
		Content.SetRows(BuildRows());
		Point point = Content.Fit();
		if (base.Width != point.X || base.Height != point.Y)
		{
			SetSize(point.X, point.Y);
		}
	}

	private void Anchor()
	{
		Intersect.Client.Framework.Gwen.Control.Base anchor = _anchor;
		if (anchor != null && !base.IsHidden)
		{
			Point point = anchor.ToCanvas(new Point(0, 0));
			int num = point.X + anchor.Width / 2 - base.Width / 2;
			int num2 = point.Y - base.Height - 20;
			_above = true;
			Canvas canvas = base.Canvas;
			if (canvas != null)
			{
				num = Math.Clamp(num, 4, Math.Max(4, canvas.Width - base.Width - 4));
			}
			if (num2 < 0)
			{
				num2 = point.Y + anchor.Height + 6;
				_above = false;
			}
			if (base.X != num || base.Y != num2)
			{
				SetPosition(num, num2);
			}
		}
	}
}
