using System;
using System.Linq;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Skin;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Framework.Gwen.Control;

public class ScrollControl : Base
{
	protected readonly ScrollPanel _scrollPanel;

	private bool _updatingScrollbars;

	private OverflowBehavior _overflowX;

	private OverflowBehavior _overflowY;

	private float _pixelsPerScrollAmountX;

	private float _pixelsPerScrollAmountY;

	public Padding InnerPanelPadding
	{
		get
		{
			return _scrollPanel.Padding;
		}
		set
		{
			_scrollPanel.Padding = value;
		}
	}

	public int VerticalScroll
	{
		get
		{
			return _scrollPanel.Y;
		}
		set
		{
			VerticalScrollBar.ScrollAmount = (float)value / (float)_scrollPanel.Height;
		}
	}

	public int HorizontalScroll
	{
		get
		{
			return _scrollPanel.X;
		}
		set
		{
			_scrollPanel.X = value;
		}
	}

	public Base InnerPanel => _scrollPanel;

	public bool CanScrollH
	{
		get
		{
			OverflowBehavior overflowX = _overflowX;
			if (overflowX == OverflowBehavior.Auto || overflowX == OverflowBehavior.Scroll)
			{
				return true;
			}
			return false;
		}
	}

	public bool CanScrollV
	{
		get
		{
			OverflowBehavior overflowY = _overflowY;
			if (overflowY == OverflowBehavior.Auto || overflowY == OverflowBehavior.Scroll)
			{
				return true;
			}
			return false;
		}
	}

	public bool AutoHideBars
	{
		get
		{
			if (_overflowX != OverflowBehavior.Auto)
			{
				return _overflowY == OverflowBehavior.Auto;
			}
			return true;
		}
		set
		{
			SetOverflow(_overflowX.SetAutoHide(value), _overflowY.SetAutoHide(value));
		}
	}

	public ScrollBar VerticalScrollBar { get; }

	public ScrollBar HorizontalScrollBar { get; }

	public OverflowBehavior Overflow
	{
		set
		{
			SetOverflow(value, value);
		}
	}

	public OverflowBehavior OverflowX
	{
		get
		{
			return _overflowX;
		}
		set
		{
			SetOverflow(value, _overflowY);
		}
	}

	public OverflowBehavior OverflowY
	{
		get
		{
			return _overflowY;
		}
		set
		{
			SetOverflow(_overflowX, value);
		}
	}

	public ScrollControl(Base parent, string? name = null)
		: base(parent, name ?? "ScrollControl")
	{
		VerticalScrollBar = new VerticalScrollBar(this)
		{
			Dock = Pos.Right,
			NudgeAmount = 30f
		};
		VerticalScrollBar.BarMoved += VBarMoved;
		HorizontalScrollBar = new HorizontalScrollBar(this)
		{
			Dock = Pos.Bottom,
			NudgeAmount = 30f
		};
		HorizontalScrollBar.BarMoved += HBarMoved;
		_scrollPanel = new ScrollPanel(this, "_scrollPanel");
		_scrollPanel.SendToBack();
		_innerPanel = _scrollPanel;
		_overflowX = OverflowBehavior.Hidden;
		_overflowY = OverflowBehavior.Auto;
		base.MouseInputEnabled = false;
	}

	public void SetOverflow(OverflowBehavior overflowX, OverflowBehavior overflowY)
	{
		if (_overflowX != overflowX || _overflowY != overflowY)
		{
			bool horizontal = overflowX.AllowsScrolling();
			bool vertical = overflowY.AllowsScrolling();
			_overflowX = overflowX;
			_overflowY = overflowY;
			EnableScroll(horizontal, vertical, internalSet: true);
			Invalidate();
		}
	}

	public override JObject? GetJson(bool isRoot = false, bool onlySerializeIfNotEmpty = false)
	{
		JObject json = base.GetJson(isRoot, onlySerializeIfNotEmpty);
		if (json == null)
		{
			return null;
		}
		json.Add("OverflowX", _overflowX.ToString());
		json.Add("OverflowY", _overflowY.ToString());
		json.Add("HorizontalScrollBar", HorizontalScrollBar.GetJson());
		json.Add("VerticalScrollBar", VerticalScrollBar.GetJson());
		return base.FixJson(json);
	}

	public override void LoadJson(JToken token, bool isRoot = false)
	{
		base.LoadJson(token, isRoot);
		if (token is JObject jObject)
		{
			OverflowBehavior result = _overflowX;
			if (jObject.TryGetValue("OverflowX", out JToken value) && value is JValue { Type: JTokenType.String } jValue)
			{
				Enum.TryParse<OverflowBehavior>(jValue.Value<string>(), out result);
			}
			OverflowBehavior result2 = _overflowY;
			if (jObject.TryGetValue("OverflowY", out JToken value2) && value2 is JValue { Type: JTokenType.String } jValue2)
			{
				Enum.TryParse<OverflowBehavior>(jValue2.Value<string>(), out result2);
			}
			SetOverflow(result, result2);
			JToken jToken = jObject["HorizontalScrollBar"];
			if (jToken != null)
			{
				HorizontalScrollBar.LoadJson(jToken);
			}
			JToken jToken2 = jObject["VerticalScrollBar"];
			if (jToken2 != null)
			{
				VerticalScrollBar.LoadJson(jToken2);
			}
		}
	}

	public void EnableScroll(bool horizontal, bool vertical)
	{
		EnableScroll(horizontal, vertical, internalSet: false);
	}

	protected virtual void EnableScroll(bool horizontal, bool vertical, bool internalSet)
	{
		if (!internalSet)
		{
			_overflowX = _overflowX.SetAllowScrolling(horizontal);
			_overflowY = _overflowY.SetAllowScrolling(vertical);
			Invalidate();
		}
		UpdateScrollBars();
	}

	public virtual void SetInnerSize(int width, int height)
	{
		_scrollPanel.SetSize(width, height);
	}

	protected virtual void VBarMoved(Base control, EventArgs args)
	{
		Invalidate();
	}

	protected virtual void HBarMoved(Base control, EventArgs args)
	{
		Invalidate();
	}

	protected override void OnChildBoundsChanged(Base child, Rectangle oldChildBounds, Rectangle newChildBounds)
	{
		if (_innerPanel != null)
		{
			UpdateScrollBars();
		}
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		UpdateScrollBars();
		base.Layout(skin);
	}

	protected override bool OnMouseWheeled(int delta)
	{
		if (CanScrollV && VerticalScrollBar.IsVisibleInTree)
		{
			float value = VerticalScrollBar.ScrollAmount - VerticalScrollBar.NudgeAmount * ((float)delta / 60f);
			if (VerticalScrollBar.SetScrollAmount(value))
			{
				return true;
			}
		}
		if (!CanScrollH || !HorizontalScrollBar.IsVisibleInTree)
		{
			return false;
		}
		return HorizontalScrollBar.SetScrollAmount(HorizontalScrollBar.ScrollAmount - HorizontalScrollBar.NudgeAmount * ((float)delta / 60f), forceUpdate: true);
	}

	protected override bool OnMouseHWheeled(int delta)
	{
		if (!CanScrollH || !HorizontalScrollBar.IsVisibleInTree)
		{
			return false;
		}
		float value = HorizontalScrollBar.ScrollAmount - HorizontalScrollBar.NudgeAmount * ((float)delta / 60f);
		return HorizontalScrollBar.SetScrollAmount(value, forceUpdate: true);
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
	}

	private static void UpdateScrollbar(ScrollBar scrollBar, bool show, float ratio)
	{
		if (show)
		{
			scrollBar.IsDisabled = ratio > 1f;
			if (!scrollBar.IsVisibleInTree)
			{
				scrollBar.IsVisibleInTree = true;
				scrollBar.SetScrollAmount(0f, forceUpdate: true);
			}
		}
		else
		{
			scrollBar.IsVisibleInTree = false;
			scrollBar.IsDisabled = true;
		}
	}

	public virtual void UpdateScrollBars()
	{
		if (!_updatingScrollbars)
		{
			_updatingScrollbars = true;
			int num = ((base.Children.Count > 0) ? base.Children.Max((Base obj) => obj.Right) : 0);
			int num2 = ((base.Children.Count > 0) ? base.Children.Max((Base obj) => obj.Bottom) : 0);
			bool canScrollH = CanScrollH;
			bool canScrollV = CanScrollV;
			int num3 = ((!VerticalScrollBar.IsHidden) ? VerticalScrollBar.Width : 0);
			int num4 = base.Width - num3;
			int num5 = ((!HorizontalScrollBar.IsHidden) ? HorizontalScrollBar.Height : 0);
			int num6 = base.Height - num5;
			int num7 = (canScrollH ? Math.Max(base.InnerWidth, num) : num4);
			int num8 = (canScrollV ? Math.Max(base.InnerHeight, num2) : num6);
			Margin margin = _scrollPanel.Margin;
			int width = num7 - (margin.Left + margin.Right);
			int height = num8 - (margin.Top + margin.Bottom);
			_scrollPanel.SetSize(width, height);
			float num9 = (float)base.Width / (float)(num + num3);
			float num10 = (float)base.Height / (float)(num2 + num5);
			VerticalScrollBar.ContentSize = _scrollPanel.Height;
			VerticalScrollBar.ViewableContentSize = num6;
			HorizontalScrollBar.ContentSize = _scrollPanel.Width;
			HorizontalScrollBar.ViewableContentSize = num4;
			bool flag = _overflowX == OverflowBehavior.Scroll || (_overflowX == OverflowBehavior.Auto && num9 < 1f);
			UpdateScrollbar(HorizontalScrollBar, flag, num9);
			bool flag2 = _overflowY == OverflowBehavior.Scroll || (_overflowY == OverflowBehavior.Auto && num10 < 1f);
			UpdateScrollbar(VerticalScrollBar, flag2, num10);
			int x = 0;
			int y = 0;
			_pixelsPerScrollAmountY = -(_scrollPanel.Height - base.Height + num5);
			if (flag2)
			{
				y = (int)(_pixelsPerScrollAmountY * VerticalScrollBar.ScrollAmount);
			}
			_pixelsPerScrollAmountX = -(_scrollPanel.Width - base.Width + num3);
			if (flag)
			{
				x = (int)(_pixelsPerScrollAmountX * HorizontalScrollBar.ScrollAmount);
			}
			_scrollPanel.SetPosition(x, y);
			_updatingScrollbars = false;
		}
	}

	public void ScrollToX(int x)
	{
		float scrollAmount = (float)x / _pixelsPerScrollAmountX;
		HorizontalScrollBar.ScrollAmount = scrollAmount;
	}

	public void ScrollToY(int y)
	{
		float scrollAmount = (float)y / _pixelsPerScrollAmountY;
		VerticalScrollBar.ScrollAmount = scrollAmount;
	}

	protected override void OnBoundsChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.OnBoundsChanged(oldBounds, newBounds);
	}

	public override Point GetChildrenSize()
	{
		return base.GetChildrenSize();
	}

	public override void Invalidate()
	{
		base.Invalidate();
	}

	public virtual void ScrollToBottom()
	{
		if (CanScrollV)
		{
			UpdateScrollBars();
			VerticalScrollBar.ScrollToBottom();
		}
	}

	public virtual void ScrollToTop()
	{
		if (CanScrollV)
		{
			UpdateScrollBars();
			VerticalScrollBar.ScrollToTop();
		}
	}

	public virtual void ScrollToLeft()
	{
		if (CanScrollH)
		{
			UpdateScrollBars();
			VerticalScrollBar.ScrollToLeft();
		}
	}

	public virtual void ScrollToRight()
	{
		if (CanScrollH)
		{
			UpdateScrollBars();
			VerticalScrollBar.ScrollToRight();
		}
	}

	public virtual void DeleteAll()
	{
		_scrollPanel.DeleteAllChildren();
	}
}
