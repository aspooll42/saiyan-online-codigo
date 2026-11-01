using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Skin;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Framework.Gwen.Control;

public class TabControl : Base
{
	private sealed class TabControlRenderOrderComparer : IComparer<Base>
	{
		public int Compare(Base? x, Base? y)
		{
			if (!(x is TabButton tabButton))
			{
				bool flag = ((y == null || !(y is TabButton)) ? true : false);
				return (!flag) ? 1 : 0;
			}
			if (!(y is TabButton))
			{
				return -1;
			}
			if (!tabButton.IsTabActive)
			{
				return -1;
			}
			return 1;
		}
	}

	private readonly ScrollBarButton[] _scrollbarButtons;

	private readonly TabStrip _tabStrip;

	private TabButton? _activeButton;

	private int _scrollOffset;

	private IFont? _font;

	private int _fontSize = 10;

	private static readonly TabControlRenderOrderComparer RenderOrderComparer = new TabControlRenderOrderComparer();

	private readonly List<Base> _ordreDeDessin = new List<Base>();

	public bool AllowReorder
	{
		get
		{
			return _tabStrip.AllowReorder;
		}
		set
		{
			_tabStrip.AllowReorder = value;
		}
	}

	public TabButton SelectedTab => _activeButton;

	public Pos TabStripPosition
	{
		get
		{
			return _tabStrip.StripPosition;
		}
		set
		{
			_tabStrip.StripPosition = value;
		}
	}

	public TabStrip TabStrip => _tabStrip;

	public int TabCount => _tabStrip.Children.Count;

	public int FontSize
	{
		get
		{
			return _fontSize;
		}
		set
		{
			if (value != _fontSize)
			{
				_fontSize = value;
				TabButton[] array = _tabStrip.Children.OfType<TabButton>().ToArray();
				TabButton[] array2 = array;
				foreach (TabButton tabButton in array2)
				{
					tabButton.FontSize = _fontSize;
				}
			}
		}
	}

	public IFont? Font
	{
		get
		{
			return _font;
		}
		set
		{
			if (value != _font)
			{
				_font = value;
				TabButton[] array = _tabStrip.Children.OfType<TabButton>().ToArray();
				TabButton[] array2 = array;
				foreach (TabButton tabButton in array2)
				{
					tabButton.Font = _font;
					tabButton.FontSize = _fontSize;
				}
			}
		}
	}

	public event GwenEventHandler<EventArgs>? TabAdded;

	public event GwenEventHandler<EventArgs>? TabRemoved;

	public event GwenEventHandler<TabChangeEventArgs>? TabChanged;

	public TabControl(Base parent, string? name = null)
		: base(parent, name)
	{
		_scrollbarButtons = new ScrollBarButton[2];
		_scrollOffset = 0;
		_tabStrip = new TabStrip(this);
		_tabStrip.StripPosition = Pos.Top;
		_scrollbarButtons[0] = new ScrollBarButton(this);
		_scrollbarButtons[0].SetDirectionLeft();
		_scrollbarButtons[0].Clicked += ScrollPressedLeft;
		_scrollbarButtons[0].SetSize(14, 16);
		_scrollbarButtons[1] = new ScrollBarButton(this);
		_scrollbarButtons[1].SetDirectionRight();
		_scrollbarButtons[1].Clicked += ScrollPressedRight;
		_scrollbarButtons[1].SetSize(14, 16);
		_innerPanel = new TabControlInner(this, "_innerPanel")
		{
			Dock = Pos.Fill
		};
		_innerPanel.SendToBack();
		base.IsTabable = false;
	}

	public TabButton AddPage(string label, string? tabName, Base? page = null)
	{
		if (page == null)
		{
			page = new Base(this, tabName)
			{
				Dock = Pos.Fill
			};
		}
		else
		{
			page.Name = tabName;
			page.Parent = this;
		}
		TabButton button = new TabButton(_tabStrip)
		{
			Font = _font,
			FontSize = _fontSize,
			IsTabable = false,
			Page = page,
			Text = label
		};
		return AddPage(button);
	}

	public TabButton AddPage(string label, Base? page = null)
	{
		return AddPage(label, null, page);
	}

	public TabButton AddPage(TabButton button)
	{
		Base page = button.Page;
		page.Parent = this;
		page.IsHidden = true;
		page.Margin = Margin.Four;
		page.Dock = Pos.Fill;
		button.Parent = _tabStrip;
		button.Dock = Pos.Left;
		button.SizeToContents();
		if (button.TabControl != this)
		{
			TabControl tabControl = button.TabControl;
			if (tabControl != null)
			{
				button.Clicked -= tabControl.OnTabPressed;
			}
			button.TabControl = this;
			button.Clicked += OnTabPressed;
		}
		if (_activeButton == null)
		{
			_activeButton = button;
			button.Page.IsVisibleInTree = true;
		}
		this.TabAdded?.Invoke(this, EventArgs.Empty);
		Invalidate();
		return button;
	}

	public override JObject? GetJson(bool isRoot = false, bool onlySerializeIfNotEmpty = false)
	{
		JObject json = base.GetJson(isRoot, onlySerializeIfNotEmpty);
		if (json == null)
		{
			return null;
		}
		json["Font"] = Font?.Name;
		json["FontSize"] = FontSize;
		return json;
	}

	public override void LoadJson(JToken token, bool isRoot = false)
	{
		base.LoadJson(token, isRoot);
		if (token is JObject jObject)
		{
			if (jObject.TryGetValue("Font", out JToken value) && value != null && value.Type == JTokenType.String)
			{
				Font = GameContentManager.Current.GetFont(value.Value<string>());
			}
			if (jObject.TryGetValue("FontSize", out JToken value2) && value2 != null && value2.Type == JTokenType.Integer)
			{
				FontSize = value2.Value<int>();
			}
		}
	}

	internal virtual void OnTabPressed(Base control, EventArgs args)
	{
		if (!(control is TabButton tabButton))
		{
			return;
		}
		Base page = tabButton.Page;
		if (page == null || _activeButton == tabButton)
		{
			return;
		}
		TabButton tabButton2 = _activeButton;
		if (tabButton2 != null)
		{
			Base page2 = _activeButton.Page;
			if (page2 != null)
			{
				page2.IsVisibleInTree = false;
			}
			_activeButton.Redraw();
		}
		else
		{
			tabButton2 = null;
		}
		_activeButton = tabButton;
		tabButton.InvalidateDock();
		tabButton.Redraw();
		page.IsVisibleInTree = true;
		this.TabChanged?.Invoke(control, new TabChangeEventArgs
		{
			PreviousTab = tabButton2,
			ActiveTab = tabButton
		});
		_tabStrip.Invalidate();
		Invalidate();
	}

	protected override void DoPostlayout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.DoPostlayout(skin);
		HandleOverflow();
	}

	internal virtual void OnLoseTab(TabButton button)
	{
		if (_activeButton == button)
		{
			_activeButton = null;
		}
		if (this.TabRemoved != null)
		{
			this.TabRemoved(this, EventArgs.Empty);
		}
		Invalidate();
	}

	private void HandleOverflow()
	{
		Point childrenSize = _tabStrip.GetChildrenSize();
		bool flag = childrenSize.X > base.Width && _tabStrip.Dock == Pos.Top;
		_scrollbarButtons[0].IsHidden = !flag;
		_scrollbarButtons[1].IsHidden = !flag;
		if (flag)
		{
			_scrollOffset = Util.Clamp(_scrollOffset, 0, childrenSize.X - base.Width + 32);
			_tabStrip.Margin = new Margin(_scrollOffset * -1, 0, 0, 0);
			_scrollbarButtons[0].SetPosition(base.Width - 30, 5);
			_scrollbarButtons[1].SetPosition(_scrollbarButtons[0].Right, 5);
		}
	}

	protected virtual void ScrollPressedLeft(Base control, EventArgs args)
	{
		_scrollOffset -= 120;
	}

	protected virtual void ScrollPressedRight(Base control, EventArgs args)
	{
		_scrollOffset += 120;
	}

	protected override IReadOnlyList<Base> OrderChildrenForRendering(IReadOnlyList<Base> children)
	{
		_ordreDeDessin.Clear();
		_ordreDeDessin.AddRange(children.Order(RenderOrderComparer));
		return _ordreDeDessin;
	}
}
