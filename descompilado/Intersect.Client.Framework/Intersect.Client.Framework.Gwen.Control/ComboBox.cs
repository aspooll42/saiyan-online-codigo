using System;
using System.Linq;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Core;
using Intersect.Framework.Reflection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Framework.Gwen.Control;

public class ComboBox : Button
{
	private readonly Base _arrowIcon;

	private readonly Menu _menu;

	private Point _itemMaximumSize;

	private string mCloseMenuSound;

	private string mHoverItemSound;

	private string mHoverMenuSound;

	private bool _positionMenuAbove;

	private string mOpenMenuSound;

	private MenuItem? mSelectedItem;

	private string mSelectItemSound;

	public bool IsOpen => _menu.IsVisibleInTree;

	public MenuItem? SelectedItem
	{
		get
		{
			return mSelectedItem;
		}
		set
		{
			if (value != mSelectedItem)
			{
				if (value == null)
				{
					mSelectedItem = null;
					return;
				}
				if (value.Parent == _menu)
				{
					mSelectedItem = value;
					OnItemSelected(mSelectedItem, new ItemSelectedEventArgs(value, automated: true, mSelectedItem.UserData));
					return;
				}
				ApplicationContext.CurrentContext.Logger.LogWarning("Tried to set selected item of {ComponentTypeName} '{ComponentName}' to '{SelectionName}' ({SelectionType})", GetType().GetName(qualified: true), base.ParentQualifiedName, value.Name, value.GetType().GetName(qualified: true));
				mSelectedItem = null;
			}
		}
	}

	internal override bool IsMenuComponent => true;

	public bool OpenMenuAbove
	{
		get
		{
			return _positionMenuAbove;
		}
		set
		{
			Base.SetAndDoIfChanged(ref _positionMenuAbove, value, UpdatePositionIfOpen, base.Parent, this);
		}
	}

	public override int FontSize
	{
		get
		{
			return base.FontSize;
		}
		set
		{
			base.FontSize = value;
			if (_menu != null)
			{
				_itemMaximumSize = default(Point);
				MenuItem[] menuItems = _menu.MenuItems;
				foreach (MenuItem menuItem in menuItems)
				{
					menuItem.FontSize = value;
					UpdateItemMaximumSize(menuItem.Text ?? string.Empty);
				}
			}
		}
	}

	public event GwenEventHandler<ItemSelectedEventArgs>? ItemSelected;

	public ComboBox(Base parent, string? name = null)
		: base(parent, name)
	{
		base.Size = new Point(100, 20);
		_menu = new Menu(this, "_menu")
		{
			FontProvider = this,
			IsHidden = true,
			IconMarginDisabled = true,
			IsTabable = false,
			MaximumSize = new Point(100, 200)
		};
		_arrowIcon = new DownArrow(this, "_arrowIcon")
		{
			Margin = new Margin(4, 0, 0, 0)
		};
		base.TextAlign = Pos.Left | Pos.CenterV;
		Text = string.Empty;
		base.AutoSizeToContents = true;
		base.IsTabable = true;
		base.KeyboardInputEnabled = true;
	}

	private static void UpdatePositionIfOpen(Base? parent, ComboBox @this)
	{
		if (@this.IsOpen)
		{
			Open(@this, parent);
		}
	}

	public override JObject? GetJson(bool isRoot = false, bool onlySerializeIfNotEmpty = false)
	{
		JObject json = base.GetJson(isRoot, onlySerializeIfNotEmpty);
		if (json == null)
		{
			return null;
		}
		json.Add("MenuAbove", _positionMenuAbove);
		json.Add("DropDownButton", _arrowIcon.GetJson());
		json.Add("OpenMenuSound", mOpenMenuSound);
		json.Add("CloseMenuSound", mCloseMenuSound);
		json.Add("HoverMenuSound", mHoverMenuSound);
		json.Add("ItemHoverSound", mHoverItemSound);
		json.Add("SelectItemSound", mSelectItemSound);
		return base.FixJson(json);
	}

	public override void LoadJson(JToken obj, bool isRoot = false)
	{
		base.LoadJson(obj, isRoot: false);
		if (obj["MenuAbove"] != null)
		{
			_positionMenuAbove = (bool)obj["MenuAbove"];
		}
		if (obj["Menu"] != null)
		{
			_menu.LoadJson(obj["Menu"]);
		}
		if (obj["DropDownButton"] != null)
		{
			_arrowIcon.LoadJson(obj["DropDownButton"]);
		}
		if (obj["OpenMenuSound"] != null)
		{
			mOpenMenuSound = (string?)obj["OpenMenuSound"];
		}
		if (obj["CloseMenuSound"] != null)
		{
			mCloseMenuSound = (string?)obj["CloseMenuSound"];
		}
		if (obj["HoverMenuSound"] != null)
		{
			mHoverMenuSound = (string?)obj["HoverMenuSound"];
		}
		if (obj["ItemHoverSound"] != null)
		{
			mHoverItemSound = (string?)obj["ItemHoverSound"];
		}
		if (obj["SelectItemSound"] != null)
		{
			mSelectItemSound = (string?)obj["SelectItemSound"];
		}
		foreach (Base child in base.Children)
		{
			if (!(child is Menu menu))
			{
				continue;
			}
			foreach (Base child2 in menu.Children)
			{
				if (child2 is MenuItem menuItem)
				{
					menuItem.SetSound(ButtonSoundState.Hover, mHoverItemSound);
				}
			}
		}
	}

	public virtual MenuItem AddItem(string label, string? name = null, object? userData = null)
	{
		MenuItem menuItem = _menu.AddItem(label, null, "", "", Font);
		menuItem.Name = name;
		menuItem.Selected += OnItemSelected;
		menuItem.UserData = userData;
		menuItem.SetTextColor(GetTextColor(ComponentState.Normal), ComponentState.Normal);
		menuItem.SetTextColor(GetTextColor(ComponentState.Hovered), ComponentState.Hovered);
		menuItem.SetTextColor(GetTextColor(ComponentState.Active), ComponentState.Active);
		menuItem.SetTextColor(GetTextColor(ComponentState.Disabled), ComponentState.Disabled);
		menuItem.SetSound(ButtonSoundState.Hover, mHoverItemSound);
		UpdateItemMaximumSize(label);
		if (mSelectedItem == null)
		{
			ItemSelectedEventArgs args = new ItemSelectedEventArgs(menuItem, automated: true, menuItem.UserData);
			OnItemSelected(menuItem, args);
		}
		return menuItem;
	}

	private void UpdateItemMaximumSize(string item)
	{
		Point point = base.Skin.Renderer.MeasureText(Font, FontSize, item);
		_itemMaximumSize.X = Math.Max(_itemMaximumSize.X, point.X);
		_itemMaximumSize.Y = Math.Max(_itemMaximumSize.Y, point.Y);
	}

	protected override void OnFontChanged(Base sender, IFont? oldFont, IFont? newFont)
	{
		if (_menu != null)
		{
			_itemMaximumSize = default(Point);
			MenuItem[] menuItems = _menu.MenuItems;
			foreach (MenuItem menuItem in menuItems)
			{
				menuItem.Font = newFont;
				menuItem.FontSize = FontSize;
				UpdateItemMaximumSize(menuItem.Text ?? string.Empty);
			}
		}
	}

	public override void Disable()
	{
		base.Disable();
		base.Canvas?.CloseMenus();
	}

	protected override void OnMouseClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		Canvas canvas = base.Canvas;
		if (IsOpen)
		{
			canvas?.CloseMenus();
			return;
		}
		bool isHidden = _menu.IsHidden;
		canvas?.CloseMenus();
		if (isHidden)
		{
			Open();
		}
		base.OnMouseClicked(mouseButton, mousePosition, userAction);
	}

	public virtual void DeleteAll()
	{
		_itemMaximumSize = default(Point);
		_menu.DeleteAll();
	}

	protected virtual void OnItemSelected(Base control, ItemSelectedEventArgs args)
	{
		if (!IsDisabledByTree && control is MenuItem menuItem)
		{
			mSelectedItem = menuItem;
			Text = mSelectedItem.Text;
			_menu.IsHidden = true;
			this.ItemSelected?.Invoke(this, args);
			if (!args.Automated)
			{
				PlaySound(mSelectItemSound);
			}
			Focus();
			Invalidate();
		}
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		_arrowIcon.Position(Pos.Right | Pos.CenterV, 4);
		base.Layout(skin);
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawComboBox(this, base.IsActive, IsOpen);
	}

	protected override void OnLostKeyboardFocus()
	{
		if (GetTextColor(ComponentState.Normal) != null)
		{
			base.TextColor = GetTextColor(ComponentState.Normal);
		}
		else
		{
			UpdateColors();
		}
	}

	protected override void OnKeyboardFocus()
	{
		if (GetTextColor(ComponentState.Normal) != null)
		{
			base.TextColor = GetTextColor(ComponentState.Normal);
		}
		else
		{
			UpdateColors();
		}
	}

	public void Open()
	{
		Open(this, base.Parent);
	}

	private static void Open(ComboBox @this, Base? parent)
	{
		if (parent == null)
		{
			Open(@this);
		}
		else
		{
			parent.RunOnMainThread(Open, @this);
		}
	}

	private static void Open(ComboBox @this)
	{
		if (!@this.IsDisabledByTree)
		{
			@this._menu.Parent = @this.Canvas;
			@this._menu.IsHidden = false;
			@this._menu.BringToFront();
			Padding padding = @this._menu.Padding;
			int num = padding.Left + padding.Right;
			int num2 = padding.Top + padding.Bottom;
			Margin margin = @this._menu.Margin;
			int num3 = margin.Top + margin.Bottom;
			int width = @this.Width;
			int num4 = 0;
			MenuItem[] array = @this._menu.Children.OfType<MenuItem>().ToArray();
			MenuItem[] array2 = array;
			foreach (MenuItem menuItem in array2)
			{
				menuItem.AutoSizeToContents = false;
				num4 += menuItem.OuterHeight;
			}
			Point point = @this.ToCanvas(default(Point));
			Menu menu = @this._menu;
			Point maximumSize = @this._menu.MaximumSize;
			maximumSize.X = width;
			menu.MaximumSize = maximumSize;
			Rectangle rectangle = @this.Canvas?.Bounds ?? new Rectangle(0, 0, int.MaxValue, int.MinValue);
			int num5 = num4 + num2;
			Point maximumSize2 = @this._menu.MaximumSize;
			if (maximumSize2.Y > 0)
			{
				num5 = Math.Min(num5, maximumSize2.Y);
			}
			Rectangle bounds = new Rectangle(point.X, point.Y, width, num5);
			bounds.X = Math.Clamp(bounds.X, rectangle.Left, rectangle.Right - width);
			bool flag = rectangle.Bottom < bounds.Bottom;
			if (!flag)
			{
				flag = @this._positionMenuAbove && rectangle.Top + bounds.Height < bounds.Top;
			}
			if (flag)
			{
				bounds.Y -= bounds.Height;
			}
			else
			{
				bounds.Y += @this.Height;
			}
			@this._menu.RestrictToParent = false;
			@this._menu.SetBounds(bounds);
			@this._menu.RestrictToParent = true;
			@this.BasePlaySound(@this.mOpenMenuSound);
		}
	}

	private bool BasePlaySound(string? sound)
	{
		return PlaySound(sound);
	}

	public virtual void Close()
	{
		_menu.Hide();
		PlaySound(mCloseMenuSound);
	}

	protected override bool OnKeyDown(bool down)
	{
		if (!down)
		{
			return true;
		}
		int num = _menu.IndexOf((Base? x) => x == mSelectedItem);
		if (num + 1 >= _menu.Children.Count)
		{
			return true;
		}
		Base obj = _menu.Children[num + 1];
		OnItemSelected(this, new ItemSelectedEventArgs(obj, automated: false, obj.UserData));
		return true;
	}

	protected override bool OnKeyUp(bool down)
	{
		if (!down)
		{
			return true;
		}
		int num = _menu.LastIndexOf((Base? x) => x == mSelectedItem);
		if (num - 1 < 0)
		{
			return true;
		}
		Base obj = _menu.Children[num - 1];
		OnItemSelected(this, new ItemSelectedEventArgs(obj, automated: false, obj.UserData));
		return true;
	}

	protected override void RenderFocus(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
	}

	public bool SelectByText(string text)
	{
		foreach (MenuItem child in _menu.Children)
		{
			if (child.Text == text)
			{
				SelectedItem = child;
				return true;
			}
		}
		return false;
	}

	public bool SelectByName(string name)
	{
		foreach (MenuItem child in _menu.Children)
		{
			if (child.Name == name)
			{
				SelectedItem = child;
				return true;
			}
		}
		return false;
	}

	public virtual bool SelectByUserData(object? userdata)
	{
		foreach (MenuItem child in _menu.Children)
		{
			if (userdata == null)
			{
				if (child.UserData == null)
				{
					SelectedItem = child;
					return true;
				}
			}
			else if (userdata.Equals(child.UserData))
			{
				SelectedItem = child;
				return true;
			}
		}
		return false;
	}

	public void SetMenuBackgroundColor(Color clr)
	{
		_menu.RenderColor = clr;
	}

	public void SetMenuMaxSize(int w, int h)
	{
		_menu.MaximumSize = new Point(w, h);
	}

	protected override Point GetContentSize()
	{
		if (!(_itemMaximumSize == default(Point)))
		{
			return _itemMaximumSize + new Point(4, 0);
		}
		return base.GetContentSize();
	}

	protected override Padding GetContentPadding()
	{
		Padding contentPadding = base.GetContentPadding();
		contentPadding.Right += _arrowIcon.OuterWidth;
		return contentPadding;
	}

	public override void SetTextColor(Color clr, ComponentState state)
	{
		base.SetTextColor(clr, state);
		foreach (MenuItem child in _menu.Children)
		{
			child.SetTextColor(clr, state);
		}
	}

	protected override void OnMouseEntered()
	{
		base.OnMouseEntered();
		if (!IsOpen && base.ShouldDrawHover)
		{
			PlaySound(mHoverMenuSound);
		}
	}

	public void ClearItems()
	{
		mSelectedItem = null;
		MenuItem[] array = base.Children.OfType<MenuItem>().ToArray();
		MenuItem[] array2 = array;
		foreach (MenuItem child in array2)
		{
			RemoveChild(child, dispose: true);
		}
	}
}
