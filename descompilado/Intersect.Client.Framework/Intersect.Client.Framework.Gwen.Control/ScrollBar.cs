using System;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Skin;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Framework.Gwen.Control;

public class ScrollBar : Base
{
	protected readonly ScrollBarBar mBar;

	protected readonly ScrollBarButton[] mScrollButton;

	private string mBackgroundTemplateFilename;

	private IGameTexture mBackgroundTemplateTex;

	protected float mContentSize;

	protected bool mDepressed;

	protected float _nudgeAmount;

	protected float _scrollAmount;

	protected float mViewableContentSize;

	public virtual int BarSize { get; set; }

	public virtual int BarPos => 0;

	public virtual int ButtonSize => 0;

	public float BaseNudgeAmount
	{
		get
		{
			return _nudgeAmount;
		}
		set
		{
			_nudgeAmount = value;
		}
	}

	public virtual float NudgeAmount
	{
		get
		{
			return _nudgeAmount / mContentSize;
		}
		set
		{
			_nudgeAmount = value;
		}
	}

	public float ScrollAmount
	{
		get
		{
			return _scrollAmount;
		}
		set
		{
			SetScrollAmount(value);
		}
	}

	public float ContentSize
	{
		get
		{
			return mContentSize;
		}
		set
		{
			if (mContentSize != value)
			{
				Invalidate();
			}
			mContentSize = value;
		}
	}

	public float ViewableContentSize
	{
		get
		{
			return mViewableContentSize;
		}
		set
		{
			if (mViewableContentSize != value)
			{
				Invalidate();
			}
			mViewableContentSize = value;
		}
	}

	public virtual bool IsHorizontal => false;

	public event GwenEventHandler<EventArgs> BarMoved;

	protected ScrollBar(Base parent)
		: base(parent)
	{
		mScrollButton = new ScrollBarButton[2];
		mScrollButton[0] = new ScrollBarButton(this);
		mScrollButton[1] = new ScrollBarButton(this);
		mBar = new ScrollBarBar(this);
		base.MinimumSize = new Point(15, 15);
		base.Size = new Point(15, 15);
		mDepressed = false;
		_scrollAmount = 0f;
		mContentSize = 0f;
		mViewableContentSize = 0f;
		NudgeAmount = 20f;
	}

	public override JObject? GetJson(bool isRoot = false, bool onlySerializeIfNotEmpty = false)
	{
		JObject json = base.GetJson(isRoot, onlySerializeIfNotEmpty);
		if (json == null)
		{
			return null;
		}
		json.Add("BackgroundTemplate", mBackgroundTemplateFilename);
		json.Add("UpOrLeftButton", mScrollButton[0].GetJson());
		json.Add("Bar", mBar.GetJson());
		json.Add("DownOrRightButton", mScrollButton[1].GetJson());
		return base.FixJson(json);
	}

	public override void LoadJson(JToken obj, bool isRoot = false)
	{
		base.LoadJson(obj);
		if (obj["BackgroundTemplate"] != null)
		{
			SetBackgroundTemplate(GameContentManager.Current.GetTexture(TextureType.Gui, (string?)obj["BackgroundTemplate"]), (string?)obj["BackgroundTemplate"]);
		}
		if (obj["UpOrLeftButton"] != null)
		{
			mScrollButton[0].LoadJson(obj["UpOrLeftButton"]);
		}
		if (obj["Bar"] != null)
		{
			mBar.LoadJson(obj["Bar"]);
		}
		if (obj["DownOrRightButton"] != null)
		{
			mScrollButton[1].LoadJson(obj["DownOrRightButton"]);
		}
	}

	public IGameTexture GetTemplate()
	{
		return mBackgroundTemplateTex;
	}

	public void SetBackgroundTemplate(IGameTexture texture, string fileName)
	{
		if (texture == null && !string.IsNullOrWhiteSpace(fileName))
		{
			texture = GameContentManager.Current?.GetTexture(TextureType.Gui, fileName);
		}
		mBackgroundTemplateFilename = fileName;
		mBackgroundTemplateTex = texture;
	}

	public virtual bool SetScrollAmount(float value, bool forceUpdate = false)
	{
		if (_scrollAmount == value && !forceUpdate)
		{
			return false;
		}
		float scrollAmount = _scrollAmount;
		_scrollAmount = value;
		Invalidate();
		OnBarMoved(this, EventArgs.Empty);
		return true;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawScrollBar(this, IsHorizontal, mDepressed);
	}

	protected virtual void OnBarMoved(Base control, EventArgs args)
	{
		this.BarMoved?.Invoke(this, args);
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Layout(skin);
		float obj = CalculateScrolledAmount();
		float scrollAmount = _scrollAmount;
		if (!scrollAmount.Equals(obj))
		{
			SetScrollAmount(scrollAmount, forceUpdate: true);
		}
	}

	protected virtual float CalculateScrolledAmount()
	{
		return 0f;
	}

	protected virtual int CalculateBarSize()
	{
		return 0;
	}

	public virtual void ScrollToLeft()
	{
	}

	public virtual void ScrollToRight()
	{
	}

	public virtual void ScrollToTop()
	{
	}

	public virtual void ScrollToBottom()
	{
	}

	public ScrollBarButton GetScrollBarButton(Pos direction)
	{
		for (int i = 0; i < mScrollButton.Length; i++)
		{
			if (mScrollButton[i].Direction == direction)
			{
				return mScrollButton[i];
			}
		}
		return null;
	}

	public void SetScrollBarImage(IGameTexture texture, string fileName, ComponentState state)
	{
		mBar.SetImage(texture, fileName, state);
	}

	public IGameTexture GetScrollBarImage(ComponentState state)
	{
		return mBar.GetImage(state);
	}
}
