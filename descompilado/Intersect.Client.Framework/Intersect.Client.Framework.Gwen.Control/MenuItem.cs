using System;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;

namespace Intersect.Client.Framework.Gwen.Control;

public class MenuItem : Button
{
	private bool _checkable;

	private bool _checked;

	private bool _onStrip;

	private Label? _accelerator;

	private Menu? _submenu;

	private Base? _submenuArrow;

	public bool IsOnStrip
	{
		get
		{
			return _onStrip;
		}
		set
		{
			_onStrip = value;
		}
	}

	public bool IsCheckable
	{
		get
		{
			return _checkable;
		}
		set
		{
			_checkable = value;
		}
	}

	public bool IsMenuOpen
	{
		get
		{
			if (_submenu == null)
			{
				return false;
			}
			return !_submenu.IsHidden;
		}
	}

	public bool IsChecked
	{
		get
		{
			return _checked;
		}
		set
		{
			if (value == _checked)
			{
				return;
			}
			_checked = value;
			if (this.CheckChanged != null)
			{
				this.CheckChanged(this, EventArgs.Empty);
			}
			if (value)
			{
				if (this.Checked != null)
				{
					this.Checked(this, EventArgs.Empty);
				}
			}
			else if (this.UnChecked != null)
			{
				this.UnChecked(this, EventArgs.Empty);
			}
		}
	}

	public Menu Menu
	{
		get
		{
			if (_submenu != null)
			{
				return _submenu;
			}
			_submenu = new Menu(base.Canvas ?? throw new InvalidOperationException("No canvas"));
			_submenu.IsHidden = true;
			if (!_onStrip)
			{
				_submenuArrow?.Dispose();
				_submenuArrow = new RightArrow(this);
				_submenuArrow.SetSize(15, 15);
			}
			Invalidate();
			return _submenu;
		}
	}

	public event GwenEventHandler<ItemSelectedEventArgs>? Selected;

	public event GwenEventHandler<EventArgs>? Checked;

	public event GwenEventHandler<EventArgs>? UnChecked;

	public event GwenEventHandler<EventArgs>? CheckChanged;

	public MenuItem(Base parent)
		: base(parent)
	{
		base.AutoSizeToContents = true;
		_onStrip = false;
		base.IsTabable = false;
		IsCheckable = false;
		IsChecked = false;
		_accelerator = new Label(this);
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawMenuItem(this, IsMenuOpen, _checkable && _checked);
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		_submenuArrow?.Position(Pos.Right | Pos.CenterV, 4);
		base.Layout(skin);
	}

	protected override void OnMouseClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		if (_submenu != null)
		{
			ToggleMenu();
		}
		else if (!_onStrip)
		{
			IsChecked = !IsChecked;
			this.Selected?.Invoke(this, new ItemSelectedEventArgs(this, automated: false, base.UserData));
			base.Canvas?.CloseMenus();
		}
		base.OnMouseClicked(mouseButton, mousePosition, userAction);
	}

	public void ToggleMenu()
	{
		if (IsMenuOpen)
		{
			CloseMenu();
		}
		else
		{
			OpenMenu();
		}
	}

	public void OpenMenu()
	{
		if (_submenu != null)
		{
			_submenu.IsHidden = false;
			_submenu.BringToFront();
			Point point = ToCanvas(Point.Empty);
			if (_onStrip)
			{
				_submenu.SetPosition(point.X, point.Y + base.Height + 1);
			}
			else
			{
				_submenu.SetPosition(point.X + base.Width, point.Y);
			}
		}
	}

	public void CloseMenu()
	{
		if (_submenu != null)
		{
			_submenu.Close();
			_submenu.CloseAll();
		}
	}

	protected override Point GetContentSize()
	{
		Point contentSize = base.GetContentSize();
		Label accelerator = _accelerator;
		if (accelerator != null)
		{
			contentSize.X += accelerator.Width;
		}
		Base parent = base.Parent;
		if (parent == null)
		{
			return contentSize;
		}
		int width = parent.Width;
		contentSize.X = Math.Max(contentSize.X, width);
		return contentSize;
	}

	public override bool SizeToContents(out Point contentSize)
	{
		_accelerator?.SizeToContents();
		bool result = base.SizeToContents(out contentSize);
		if (_accelerator != null)
		{
			_accelerator.TextAlign = Pos.Left;
		}
		return result;
	}

	public MenuItem SetAction(GwenEventHandler<EventArgs> handler, GwenEventHandler<ItemSelectedEventArgs> selHandler)
	{
		if (_accelerator != null)
		{
			AddAccelerator(_accelerator.Text, handler);
		}
		Selected += selHandler;
		return this;
	}

	public void SetAccelerator(string? accelerator)
	{
		if (string.IsNullOrWhiteSpace(accelerator))
		{
			_accelerator = null;
			return;
		}
		_accelerator = new Label(this)
		{
			TextAlign = (Pos.Right | Pos.CenterV),
			Dock = Pos.Right,
			Margin = new Margin(0, 0, 16, 0),
			Text = accelerator
		};
	}
}
