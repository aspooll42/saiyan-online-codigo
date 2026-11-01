using System;
using System.Linq;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Skin;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Framework.Gwen.Control;

public class Menu : ScrollControl
{
	private string mBackgroundTemplateFilename;

	private IGameTexture mBackgroundTemplateTex;

	private bool mDeleteOnClose;

	private bool mDisableIconMargin;

	protected Color mItemHoverTextColor;

	protected Color mItemNormalTextColor;

	private IFont? _itemFont;

	private string? _itemFontName;

	private int _itemFontSize;

	private IFontProvider? _fontProvider;

	public IFontProvider? FontProvider
	{
		get
		{
			return _fontProvider;
		}
		set
		{
			if (value != _fontProvider)
			{
				_fontProvider = value;
				UpdateItemStyles();
			}
		}
	}

	public IFont? ItemFont
	{
		get
		{
			return _itemFont;
		}
		set
		{
			SetItemFont(value, value?.Name);
		}
	}

	public string? ItemFontName
	{
		get
		{
			return _itemFontName;
		}
		set
		{
			SetItemFont(GameContentManager.Current.GetFont(value), value);
		}
	}

	public int ItemFontSize
	{
		get
		{
			return _itemFontSize;
		}
		set
		{
			if (value != _itemFontSize)
			{
				int itemFontSize = _itemFontSize;
				_itemFontSize = value;
				OnFontSizeChanged(this, value, itemFontSize);
			}
		}
	}

	internal override bool IsMenuComponent => true;

	public bool IconMarginDisabled
	{
		get
		{
			return mDisableIconMargin;
		}
		set
		{
			mDisableIconMargin = value;
		}
	}

	public bool DeleteOnClose
	{
		get
		{
			return mDeleteOnClose;
		}
		set
		{
			mDeleteOnClose = value;
		}
	}

	protected virtual bool ShouldHoverOpenMenu => true;

	public MenuItem[] MenuItems => base.Children.OfType<MenuItem>().ToArray();

	public Menu(Base parent, string? name = null)
		: base(parent, name)
	{
		_scrollPanel.Padding = default(Padding);
		base.Size = new Point(10, 10);
		base.Padding = Padding.Two;
		IconMarginDisabled = false;
		DeleteOnClose = false;
	}

	private void SetItemFont(IFont? itemFont, string? itemFontName)
	{
		IFont itemFont2 = _itemFont;
		_itemFont = itemFont;
		_itemFontName = itemFontName;
		if (itemFont != itemFont2)
		{
			OnFontChanged(this, itemFont, itemFont2);
		}
	}

	protected virtual void OnFontChanged(Base sender, IFont? newFont, IFont? oldFont)
	{
		UpdateItemStyles();
	}

	protected virtual void OnFontSizeChanged(Base sender, int newSize, int oldSize)
	{
		UpdateItemStyles();
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawMenu(this, IconMarginDisabled);
	}

	protected override void RenderUnder(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.RenderUnder(skin);
		skin.DrawShadow(this);
	}

	public void Open(Pos pos)
	{
		RunOnMainThread(Open, this, pos);
	}

	private static void Open(Menu @this, Pos position)
	{
		@this.IsVisibleInParent = true;
		@this.BringToFront();
		Point mousePosition = InputHandler.MousePosition;
		int num = mousePosition.X - 4;
		int num2 = mousePosition.Y - 4;
		@this.OnPositioningBeforeOpen();
		Canvas canvas = @this.Canvas;
		if (canvas != null)
		{
			Point size = canvas.Size;
			Point size2 = @this.Size;
			num = Math.Min(num, Math.Max(0, size.X - size2.X));
			num2 = Math.Min(num2, Math.Max(0, size.Y - size2.Y));
		}
		@this.SetPosition(num, num2);
		@this.OnOpen();
	}

	protected virtual void OnPositioningBeforeOpen()
	{
		MenuItem[] array = base.Children.OfType<MenuItem>().ToArray();
		MenuItem[] array2 = array;
		foreach (MenuItem menuItem in array2)
		{
			menuItem.AutoSizeToContents = false;
		}
		SizeToChildren(resizeX: true, resizeY: true, recursive: true);
	}

	protected virtual void OnOpen()
	{
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		int num = base.Children.Sum((Base child) => child?.Height ?? 0);
		if (base.InnerPanel.MaximumSize.Y > 0 && num > base.InnerPanel.MaximumSize.Y)
		{
			base.InnerPanel.MaximumSize = new Point(base.InnerPanel.MaximumSize.X, num);
		}
		int num2 = base.Canvas?.Height ?? (base.Y + num);
		if (base.Y + num > num2)
		{
			num = num2 - base.Y;
		}
		SetSize(base.Width, num);
		base.Layout(skin);
	}

	public override void Invalidate()
	{
		base.Invalidate();
	}

	public virtual MenuItem AddItem(string text)
	{
		return AddItem(text, null);
	}

	public virtual MenuItem AddItem(string text, IGameTexture? iconTexture, string? textureFilename = null, string? accelerator = null, IFont? font = null)
	{
		MenuItem menuItem = new MenuItem(this)
		{
			Font = font,
			Text = text,
			Padding = new Padding(8)
		};
		menuItem.SetStateTexture(iconTexture, textureFilename, ComponentState.Normal);
		menuItem.SetAccelerator(accelerator);
		OnAddItem(menuItem);
		return menuItem;
	}

	protected virtual void OnAddItem(MenuItem menuItem)
	{
		menuItem.Padding = new Padding(IconMarginDisabled ? 8 : 32, 4, 8, 4);
		menuItem.Dock = Pos.Top;
		menuItem.SizeToContents();
		menuItem.TextAlign = Pos.Left | Pos.CenterV;
		menuItem.HoverEnter += OnHoverItem;
		base.Width = Math.Max(base.Width, menuItem.Width + 10 + 32);
		UpdateItemStyles();
	}

	public virtual void CloseAll()
	{
		foreach (MenuItem item in base.Children.OfType<MenuItem>())
		{
			item.CloseMenu();
		}
	}

	public virtual bool IsMenuOpen()
	{
		return base.Children.Any((Base child) => child is MenuItem && (child as MenuItem).IsMenuOpen);
	}

	protected virtual void OnHoverItem(Base control, EventArgs args)
	{
		if (ShouldHoverOpenMenu && control is MenuItem { IsMenuOpen: false } menuItem)
		{
			CloseAll();
			menuItem.OpenMenu();
		}
	}

	public virtual void Close()
	{
		if (!base.IsHidden)
		{
			base.IsHidden = true;
			if (DeleteOnClose)
			{
				DelayedDelete();
			}
		}
	}

	public override void CloseMenus()
	{
		base.CloseMenus();
		CloseAll();
		Close();
	}

	public virtual void AddDivider()
	{
		MenuDivider menuDivider = new MenuDivider(this)
		{
			Dock = Pos.Top,
			Margin = new Margin((!IconMarginDisabled) ? 24 : 0, 0, 4, 0),
		};
	}

	public override bool SizeToChildren(SizeToChildrenArgs args)
	{
		bool result = base.SizeToChildren(args);
		if (!args.X)
		{
			return result;
		}
		int num = 0;
		foreach (Base child in base.Children)
		{
			if (child.Width > num)
			{
				num = child.Width;
			}
		}
		return SetSize(num, base.Height);
	}

	public override JObject? GetJson(bool isRoot = false, bool onlySerializeIfNotEmpty = false)
	{
		JObject json = base.GetJson(isRoot, onlySerializeIfNotEmpty);
		if (json == null)
		{
			return null;
		}
		json.Add("BackgroundTemplate", mBackgroundTemplateFilename);
		json.Add("ItemTextColor", Color.ToString(mItemNormalTextColor));
		json.Add("ItemHoveredTextColor", Color.ToString(mItemHoverTextColor));
		json.Add("ItemFontName", ItemFontName);
		json.Add("ItemFontSize", ItemFontSize);
		return base.FixJson(json);
	}

	public override void LoadJson(JToken token, bool isRoot = false)
	{
		base.LoadJson(token, isRoot);
		if (!(token is JObject jObject))
		{
			return;
		}
		if (jObject["BackgroundTemplate"] != null)
		{
			SetBackgroundTemplate(GameContentManager.Current.GetTexture(TextureType.Gui, (string?)jObject["BackgroundTemplate"]), (string?)jObject["BackgroundTemplate"]);
		}
		if (jObject["ItemTextColor"] != null)
		{
			mItemNormalTextColor = Color.FromString((string?)jObject["ItemTextColor"]);
		}
		if (jObject["ItemHoveredTextColor"] != null)
		{
			mItemHoverTextColor = Color.FromString((string?)jObject["ItemHoveredTextColor"]);
		}
		string text = null;
		int? num = null;
		if (jObject.TryGetValue("ItemFont", out JToken value) && value is JValue { Type: JTokenType.String } jValue)
		{
			string text2 = jValue.Value<string>()?.Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				string[] array = text2.Split(',');
				text = array.FirstOrDefault();
				if (array.Length > 1 && int.TryParse(array[1], out var result))
				{
					num = result;
				}
			}
		}
		if (jObject.TryGetValue("ItemFontName", out JToken value2) && value2 is JValue { Type: JTokenType.String } jValue2)
		{
			text = jValue2.Value<string>();
		}
		if (jObject.TryGetValue("ItemFontSize", out JToken value3) && value3 is JValue { Type: JTokenType.Integer } jValue3)
		{
			num = jValue3.Value<int>();
		}
		if (num.HasValue)
		{
			ItemFontSize = num.Value;
		}
		text = text?.Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			ItemFontName = text;
		}
		UpdateItemStyles();
	}

	private void UpdateItemStyles()
	{
		IFont font = _fontProvider?.Font;
		int num = ((_itemFontSize < 1) ? 10 : _itemFontSize);
		int fontSize = _fontProvider?.FontSize ?? num;
		IFont itemFont = _itemFont;
		if (itemFont != null)
		{
			font = itemFont;
			fontSize = num;
		}
		MenuItem[] array = base.Children.OfType<MenuItem>().ToArray();
		MenuItem[] array2 = array;
		foreach (MenuItem menuItem in array2)
		{
			if (font != null)
			{
				menuItem.Font = font;
			}
			menuItem.FontSize = fontSize;
			menuItem.SetTextColor(mItemNormalTextColor, ComponentState.Normal);
			menuItem.SetTextColor(mItemHoverTextColor, ComponentState.Hovered);
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
}
