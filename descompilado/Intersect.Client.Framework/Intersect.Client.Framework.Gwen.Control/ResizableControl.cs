using System;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Framework.Gwen.Control;

public class ResizableControl : Base
{
	private readonly Resizer[] mResizer;

	private bool mClampMovement;

	private bool _resizable;

	public bool ClampMovement
	{
		get
		{
			return mClampMovement;
		}
		set
		{
			mClampMovement = value;
		}
	}

	public bool IsResizable
	{
		get
		{
			return _resizable;
		}
		set
		{
			if (value != _resizable)
			{
				_resizable = value;
				if (value)
				{
					EnableResizing();
				}
				else
				{
					DisableResizing();
				}
			}
		}
	}

	public event GwenEventHandler<EventArgs> Resized;

	public ResizableControl(Base? parent, string? name)
		: base(parent, name)
	{
		_resizable = true;
		mResizer = new Resizer[10];
		base.MinimumSize = new Point(5, 5);
		mClampMovement = false;
		mResizer[2] = new Resizer(this);
		mResizer[2].Dock = Pos.Bottom;
		mResizer[2].ResizeDir = Pos.Bottom;
		mResizer[2].Resized += OnResized;
		mResizer[2].Target = this;
		mResizer[1] = new Resizer(mResizer[2]);
		mResizer[1].Dock = Pos.Left;
		mResizer[1].ResizeDir = Pos.Left | Pos.Bottom;
		mResizer[1].Resized += OnResized;
		mResizer[1].Target = this;
		mResizer[3] = new Resizer(mResizer[2]);
		mResizer[3].Dock = Pos.Right;
		mResizer[3].ResizeDir = Pos.Right | Pos.Bottom;
		mResizer[3].Resized += OnResized;
		mResizer[3].Target = this;
		mResizer[8] = new Resizer(this);
		mResizer[8].Dock = Pos.Top;
		mResizer[8].ResizeDir = Pos.Top;
		mResizer[8].Resized += OnResized;
		mResizer[8].Target = this;
		mResizer[7] = new Resizer(mResizer[8]);
		mResizer[7].Dock = Pos.Left;
		mResizer[7].ResizeDir = Pos.Left | Pos.Top;
		mResizer[7].Resized += OnResized;
		mResizer[7].Target = this;
		mResizer[9] = new Resizer(mResizer[8]);
		mResizer[9].Dock = Pos.Right;
		mResizer[9].ResizeDir = Pos.Right | Pos.Top;
		mResizer[9].Resized += OnResized;
		mResizer[9].Target = this;
		mResizer[4] = new Resizer(this);
		mResizer[4].Dock = Pos.Left;
		mResizer[4].ResizeDir = Pos.Left;
		mResizer[4].Resized += OnResized;
		mResizer[4].Target = this;
		mResizer[6] = new Resizer(this);
		mResizer[6].Dock = Pos.Right;
		mResizer[6].ResizeDir = Pos.Right;
		mResizer[6].Resized += OnResized;
		mResizer[6].Target = this;
	}

	public override JObject? GetJson(bool isRoot = false, bool onlySerializeIfNotEmpty = false)
	{
		JObject json = base.GetJson(isRoot, onlySerializeIfNotEmpty);
		if (json == null)
		{
			return null;
		}
		json.Add("ClampMovement", ClampMovement);
		return base.FixJson(json);
	}

	public override void LoadJson(JToken obj, bool isRoot = false)
	{
		base.LoadJson(obj);
		if (obj["ClampMovement"] != null)
		{
			ClampMovement = (bool)obj["ClampMovement"];
		}
	}

	protected virtual void OnResized(Base control, EventArgs args)
	{
		if (this.Resized != null)
		{
			this.Resized(this, EventArgs.Empty);
		}
	}

	protected Resizer GetResizer(int i)
	{
		return mResizer[i];
	}

	public virtual void DisableResizing()
	{
		for (int i = 0; i < 10; i++)
		{
			if (mResizer[i] != null)
			{
				mResizer[i].MouseInputEnabled = false;
				mResizer[i].IsHidden = true;
			}
		}
	}

	public void EnableResizing()
	{
		for (int i = 0; i < 10; i++)
		{
			if (mResizer[i] != null)
			{
				mResizer[i].MouseInputEnabled = true;
				mResizer[i].IsHidden = false;
				base.Padding = new Padding(0, 0, 0, 0);
			}
		}
	}

	public override bool SetBounds(int x, int y, int width, int height)
	{
		Point minimumSize = base.MinimumSize;
		if (width < minimumSize.X)
		{
			width = minimumSize.X;
		}
		if (height < minimumSize.Y)
		{
			height = minimumSize.Y;
		}
		Base parent = base.Parent;
		if (parent != null && mClampMovement)
		{
			if (x + width > parent.Width)
			{
				x = parent.Width - width;
			}
			if (x < 0)
			{
				x = 0;
			}
			if (y + height > parent.Height)
			{
				y = parent.Height - height;
			}
			if (y < 0)
			{
				y = 0;
			}
		}
		return base.SetBounds(x, y, width, height);
	}

	public override bool SetSize(int width, int height)
	{
		bool flag = base.SetSize(width, height);
		if (flag)
		{
			OnResized(this, EventArgs.Empty);
		}
		return flag;
	}
}
