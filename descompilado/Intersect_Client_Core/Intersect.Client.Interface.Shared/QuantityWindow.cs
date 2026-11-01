using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.General;
using Intersect.Client.Interface.Game;
using Intersect.Client.Localization;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Localization;

namespace Intersect.Client.Interface.Shared;

public sealed class QuantityWindow : WindowControl
{
	private sealed class QuantityField : TextBox
	{
		public QuantityField(Base parent, string name)
			: base(parent, name)
		{
		}

		public override void SetText(string? text, bool doEvents = true)
		{
			if (text == null || text.All(char.IsAsciiDigit))
			{
				base.SetText(text, doEvents);
			}
		}
	}

	private const int WindowWidth = 440;

	private const int PlainHeight = 326;

	private const int PricedHeight = 408;

	private new const int Margin = 24;

	private const int ContentWidth = 392;

	private const int StepY = 144;

	private const int StepHeight = 40;

	private const int SliderY = 198;

	private const int KnobSize = 20;

	private const int RailHeight = 10;

	private const int CoinFrames = 4;

	private const int CoinRestMs = 1600;

	private const int CoinFrameMs = 90;

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color Entry = new Color(255, 196, 204, 220);

	private static readonly Color Muted = new Color(255, 176, 186, 206);

	private static readonly Color Dim = new Color(255, 150, 160, 182);

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private static readonly Color OrangeLight = new Color(255, 255, 172, 82);

	private static readonly Color Gold = new Color(255, 245, 190, 90);

	private static readonly Color Alert = new Color(255, 255, 120, 110);

	private static QuantityWindow? _open;

	private readonly QuantityPricing? _pricing;

	private readonly Action<int> _onConfirm;

	private readonly Func<bool>? _stillValid;

	private readonly int _maximum;

	private readonly int _windowHeight;

	private readonly IFont? _body;

	private readonly Label _title;

	private readonly Button _close;

	private readonly ImagePanel _slot;

	private readonly Label _name;

	private readonly Label _kind;

	private readonly ImagePanel _topRule;

	private readonly Button _minus;

	private readonly QuantityField _field;

	private readonly Button _plus;

	private readonly Button _max;

	private readonly ImagePanel _rail;

	private readonly ImagePanel _fill;

	private readonly Slider _slider;

	private readonly Label _limit;

	private readonly Label _maxLegend;

	private readonly Button _cancel;

	private readonly Button _confirm;

	private readonly IGameTexture? _coinStrip;

	private readonly List<ImagePanel> _coins = new List<ImagePanel>();

	private readonly Label? _unitCaption;

	private readonly Label? _unitAmount;

	private readonly ImagePanel? _unitMark;

	private readonly ImagePanel? _priceRule;

	private readonly Label? _totalCaption;

	private readonly Label? _totalAmount;

	private readonly ImagePanel? _totalMark;

	private readonly Label? _balanceCaption;

	private readonly Label? _balanceAmount;

	private readonly ImagePanel? _balanceMark;

	private int _quantity;

	private bool _finished;

	private bool _syncing;

	private double _fillRatio;

	private int _laidOutTitleBarHeight = -1;

	private int _coinFrame = -1;

	private string _shownPrices = "?";

	public static QuantityWindow? Current
	{
		get
		{
			QuantityWindow open = _open;
			if (open == null || open.IsHidden)
			{
				return null;
			}
			return open;
		}
	}

	public QuantityWindow(string title, ItemDescriptor item, string confirmLabel, int quantity, int maximum, Action<int> onConfirm, QuantityPricing? pricing = null, string? limitHint = null, Func<bool>? stillValid = null)
		: base(Interface.CurrentInterface.Root, string.Empty, modal: true, "QuantityWindow")
	{
		_open?.Cancel();
		_open = this;
		_pricing = pricing;
		_onConfirm = onConfirm;
		_stillValid = stillValid;
		_maximum = Math.Max(1, maximum);
		_windowHeight = ((pricing == null) ? 326 : 408);
		GameContentManager current = GameContentManager.Current;
		_body = current.GetFont("chakrapetch");
		IFont font = current.GetFont("rajdhanibold");
		IFont font2 = current.GetFont("refuel");
		Interface.InputBlockingComponents.Add(this);
		base.DeleteOnClose = true;
		DisableResizing();
		base.IsClosable = false;
		base.Titlebar.ShouldDrawBackground = false;
		base.TitleLabel.IsHidden = true;
		SetSize(440, _windowHeight);
		string text = ((pricing == null) ? "saiyan_quantite.png" : "saiyan_quantite_prix.png");
		IGameTexture texture = current.GetTexture(TextureType.Gui, text);
		SetImage(texture, text, ControlState.Active);
		SetImage(texture, text, ControlState.Inactive);
		_coinStrip = current.GetTexture(TextureType.Item, "zeni_anime.png");
		_title = MakeLabel(this, "Title", 18, White, font2);
		_title.Text = title.ToUpperInvariant();
		WindowTitle.Apply(_title);
		_close = new Button(this, "Close", disableText: true);
		_close.SetStateTexture(ComponentState.Normal, "saiyan_close.png");
		_close.SetStateTexture(ComponentState.Hovered, "saiyan_close_hover.png");
		_close.SetStateTexture(ComponentState.Active, "saiyan_close_clicked.png");
		_close.Clicked += delegate
		{
			Cancel();
		};
		_slot = MakeImage(this, "Slot", "saiyan_slot.png");
		_slot.TextureNinePatchMargin = new Margin(6);
		ImagePanel imagePanel = MakeImage(_slot, "Icon", null);
		imagePanel.Texture = current.GetTexture(TextureType.Item, item.Icon);
		imagePanel.SetSize(32, 32);
		imagePanel.SetPosition(5, 5);
		_name = MakeLabel(this, "Name", 12, RarityColor(item), _body);
		_name.Text = item.Name;
		_kind = MakeLabel(this, "Kind", 10, Muted, _body);
		_kind.Text = Classification(item);
		if (pricing != null)
		{
			_unitCaption = MakeLabel(this, "UnitCaption", 9, Muted, _body);
			_unitCaption.Text = (pricing.Earning ? Strings.Quantity.BuybackPrice : Strings.Quantity.UnitPrice).ToString().ToUpperInvariant();
			_unitCaption.TextAlign = Pos.Right | Pos.CenterV;
			_unitAmount = MakeLabel(this, "UnitAmount", 14, White, font);
			_unitAmount.Text = MarketWindow.FormatAmount(pricing.UnitPrice);
			_unitAmount.TextAlign = Pos.Right | Pos.CenterV;
			_unitMark = MakeCurrencyMark("UnitMark", pricing.Currency, 18);
		}
		_topRule = MakeImage(this, "TopRule", "hdv_filet.png");
		_minus = MakeStepButton("Minus", "saiyan_pas_moins");
		_minus.Clicked += delegate
		{
			SetQuantity(Math.Max(1, _quantity - 1));
		};
		_field = new QuantityField(this, "Field")
		{
			Font = font2,
			FontSize = 20,
			TextAlign = Pos.Center,
			MaximumLength = 7
		};
		_field.SetTextColor(White, ComponentState.Normal);
		IGameTexture texture2 = current.GetTexture(TextureType.Gui, "saiyan_field_216x40.png");
		if (texture2 != null)
		{
			_field.SetBackgroundTemplate(texture2, "saiyan_field_216x40.png");
		}
		_field.TextChanged += delegate
		{
			OnFieldEdited();
		};
		_field.SubmitPressed += delegate
		{
			Confirm();
		};
		_plus = MakeStepButton("Plus", "saiyan_pas_plus");
		_plus.Clicked += delegate
		{
			SetQuantity(Math.Min(_maximum, _quantity + 1));
		};
		_max = MakeButton("Max", "saiyan_btn_72x40", 11, Entry);
		_max.Text = Strings.Quantity.Max;
		_max.Clicked += delegate
		{
			SetQuantity(_maximum);
		};
		_rail = MakeImage(this, "Rail", "saiyan_bar_track.png");
		_rail.TextureNinePatchMargin = new Margin(5);
		_fill = MakeImage(this, "Fill", "saiyan_bar_fill.png");
		_fill.TextureNinePatchMargin = new Margin(5);
		_slider = new Slider(this, "Slider")
		{
			BackgroundImageName = "saiyan_vide.png",
			NotchCount = 0,
			SnapToNotches = false
		};
		_slider.DraggerSize = new Point(20, 20);
		_slider.SetDraggerImage(current.GetTexture(TextureType.Gui, "saiyan_curseur.png"), ComponentState.Normal);
		_slider.SetDraggerImage(current.GetTexture(TextureType.Gui, "saiyan_curseur_hover.png"), ComponentState.Hovered);
		_slider.SetDraggerImage(current.GetTexture(TextureType.Gui, "saiyan_curseur_clicked.png"), ComponentState.Active);
		_slider.SetDraggerImage(current.GetTexture(TextureType.Gui, "saiyan_curseur.png"), ComponentState.Disabled);
		_slider.SetRange((_maximum > 1) ? 1 : 0, _maximum);
		_slider.IsDisabled = _maximum <= 1;
		_slider.ValueChanged += delegate(Base _, ValueChangedEventArgs<double> args)
		{
			if (!_syncing)
			{
				SetQuantity((int)Math.Round(args.Value), fromField: false, fromSlider: true);
			}
		};
		_limit = MakeLabel(this, "Limit", 10, Dim, _body);
		_limit.Text = limitHint ?? string.Empty;
		_maxLegend = MakeLabel(this, "MaxLegend", 10, Muted, _body);
		_maxLegend.Text = Strings.Quantity.MaxLegend.ToString(MarketWindow.FormatAmount(_maximum));
		_maxLegend.TextAlign = Pos.Right | Pos.CenterV;
		if (pricing != null)
		{
			_priceRule = MakeImage(this, "PriceRule", "hdv_filet.png");
			_totalCaption = MakeLabel(this, "TotalCaption", 12, Entry, _body);
			_totalCaption.Text = (pricing.Earning ? Strings.Quantity.Earnings : Strings.Quantity.Total);
			_totalAmount = MakeLabel(this, "TotalAmount", 20, Gold, font);
			_totalAmount.TextAlign = Pos.Right | Pos.CenterV;
			_totalMark = MakeCurrencyMark("TotalMark", pricing.Currency, 22);
			if (IsWallet(pricing.Currency))
			{
				_balanceCaption = MakeLabel(this, "BalanceCaption", 10, Muted, _body);
				_balanceCaption.Text = (pricing.Earning ? Strings.Quantity.BalanceAfterSale : Strings.Quantity.BalanceAfterPurchase);
				_balanceAmount = MakeLabel(this, "BalanceAmount", 13, Muted, font);
				_balanceAmount.TextAlign = Pos.Right | Pos.CenterV;
				_balanceMark = MakeCurrencyMark("BalanceMark", pricing.Currency, 16);
			}
		}
		_cancel = MakeButton("Cancel", "saiyan_btn_128x34", 11, Entry);
		_cancel.Text = Strings.InputBox.Cancel;
		_cancel.Clicked += delegate
		{
			Cancel();
		};
		_confirm = MakeButton("Confirm", "saiyan_cta_244x34", 11, Night);
		_confirm.SetTextColor(Night, ComponentState.Hovered);
		_confirm.Text = confirmLabel;
		_confirm.Clicked += delegate
		{
			Confirm();
		};
		Layout();
		SetPosition(Math.Max(0, (Interface.CurrentInterface.Root.Width - 440) / 2), Math.Max(0, (Interface.CurrentInterface.Root.Height - _windowHeight) / 2));
		SetQuantity(quantity);
		Show();
		BringToFront();
		_field.Focus();
		_field.SelectAll();
	}

	public void Confirm()
	{
		if (!_finished)
		{
			int num = Math.Clamp(_quantity, 1, _maximum);
			if (CanAfford(num))
			{
				Finish();
				_onConfirm(num);
			}
		}
	}

	public void Cancel()
	{
		if (!_finished)
		{
			Finish();
		}
	}

	private void Finish()
	{
		_finished = true;
		if (_open == this)
		{
			_open = null;
		}
		Interface.InputBlockingComponents.Remove(this);
		Close();
	}

	protected override void Dispose(bool disposing)
	{
		Interface.InputBlockingComponents.Remove(this);
		if (_open == this)
		{
			_open = null;
		}
		base.Dispose(disposing);
	}

	protected override bool OnMouseWheeled(int delta)
	{
		if (_finished || delta == 0)
		{
			return true;
		}
		SetQuantity(Math.Clamp(_quantity + Math.Sign(delta), 1, _maximum));
		return true;
	}

	public override void Think()
	{
		base.Think();
		if (_finished || base.IsHidden)
		{
			return;
		}
		if (_stillValid != null && !_stillValid())
		{
			Cancel();
			return;
		}
		if (base.TitleBarBounds.Height != _laidOutTitleBarHeight)
		{
			Layout();
		}
		RefreshPrices();
		AnimateCoins();
	}

	private void OnFieldEdited()
	{
		if (!_syncing)
		{
			string text = _field.Text ?? string.Empty;
			if (text.Length == 0)
			{
				_quantity = 0;
				RefreshState();
			}
			else
			{
				long result;
				long num = (long.TryParse(text, out result) ? result : 0);
				SetQuantity((int)Math.Min(2147483647L, num), text.Length > 0 && num >= 1 && num <= _maximum);
			}
		}
	}

	private void SetQuantity(int quantity, bool fromField = false, bool fromSlider = false)
	{
		_quantity = Math.Clamp(quantity, 1, _maximum);
		_syncing = true;
		try
		{
			if (!fromField)
			{
				_field.Text = _quantity.ToString();
				_field.CursorPos = _field.TextLength;
				_field.CursorEnd = _field.TextLength;
			}
			if (!fromSlider)
			{
				_slider.SetValue(_quantity, skipEvents: true);
			}
		}
		finally
		{
			_syncing = false;
		}
		_fillRatio = ((_maximum <= 1) ? 1.0 : (fromSlider ? Math.Clamp((_slider.Value - 1.0) / (double)(_maximum - 1), 0.0, 1.0) : (((double)_quantity - 1.0) / (double)(_maximum - 1))));
		RefreshState();
	}

	private void RefreshState()
	{
		_minus.IsDisabled = _quantity <= 1;
		_plus.IsDisabled = _quantity >= _maximum;
		_max.IsDisabled = _quantity >= _maximum;
		_confirm.IsDisabled = _quantity < 1 || !CanAfford(_quantity);
		int num = 10 + (int)Math.Round(372.0 * _fillRatio);
		_fill.SetSize(Math.Max(10, num + 4), 10);
		_shownPrices = "?";
		RefreshPrices();
	}

	private bool CanAfford(int quantity)
	{
		QuantityPricing pricing = _pricing;
		if ((object)pricing != null && !pricing.Earning)
		{
			return pricing.UnitPrice * quantity <= Owned(pricing.Currency);
		}
		return true;
	}

	private void RefreshPrices()
	{
		QuantityPricing pricing = _pricing;
		if ((object)pricing == null || _totalAmount == null)
		{
			return;
		}
		long num = Owned(pricing.Currency);
		string text = $"{_quantity}:{num}";
		if (!(text == _shownPrices))
		{
			_shownPrices = text;
			long num2 = pricing.UnitPrice * Math.Max(0, _quantity);
			_totalAmount.Text = MarketWindow.FormatAmount(num2);
			if (_balanceAmount != null)
			{
				long num3 = (pricing.Earning ? (num + num2) : (num - num2));
				_balanceAmount.Text = MarketWindow.FormatAmount(Math.Max(0L, num3));
				_balanceAmount.SetTextColor((num3 < 0) ? Alert : Muted, ComponentState.Normal);
				_totalAmount.SetTextColor((num3 < 0) ? Alert : Gold, ComponentState.Normal);
			}
		}
	}

	private static bool IsWallet(ItemDescriptor? currency)
	{
		if (currency != null)
		{
			return currency.ItemType == ItemType.Currency;
		}
		return false;
	}

	private static long Owned(ItemDescriptor? currency)
	{
		if (currency != null)
		{
			Player me = Globals.Me;
			if (me != null)
			{
				if (!IsWallet(currency))
				{
					return me.GetQuantityOfItemInInventory(currency.Id);
				}
				return me.Zeni;
			}
		}
		return 0L;
	}

	private ImagePanel MakeCurrencyMark(string name, ItemDescriptor? currency, int size)
	{
		ImagePanel imagePanel = MakeImage(this, name, null);
		if (IsWallet(currency))
		{
			IGameTexture coinStrip = _coinStrip;
			if (coinStrip != null && coinStrip.Width > 0)
			{
				imagePanel.Texture = coinStrip;
				imagePanel.SetTextureRect(0, 0, coinStrip.Width / 4, coinStrip.Height);
				_coins.Add(imagePanel);
				goto IL_008b;
			}
		}
		if (IsWallet(currency))
		{
			imagePanel.Texture = _coinStrip;
			_coins.Add(imagePanel);
		}
		else if (currency != null)
		{
			imagePanel.Texture = GameContentManager.Current.GetTexture(TextureType.Item, currency.Icon);
		}
		goto IL_008b;
		IL_008b:
		imagePanel.SetSize(size, size);
		return imagePanel;
	}

	private void AnimateCoins()
	{
		if (_coins.Count == 0)
		{
			return;
		}
		IGameTexture coinStrip = _coinStrip;
		if (coinStrip == null || coinStrip.Width <= 0)
		{
			return;
		}
		int num = 1870;
		long num2 = Timing.Global.Milliseconds % num;
		int num3 = ((num2 >= 1600) ? Math.Min(3, 1 + (int)((num2 - 1600) / 90)) : 0);
		if (num3 == _coinFrame)
		{
			return;
		}
		_coinFrame = num3;
		int num4 = coinStrip.Width / 4;
		foreach (ImagePanel coin in _coins)
		{
			int width = coin.Width;
			coin.SetTextureRect(num3 * num4, 0, num4, coinStrip.Height);
			coin.SetSize(width, width);
		}
	}

	private new int Y(int windowY)
	{
		return windowY - _laidOutTitleBarHeight;
	}

	private void Layout()
	{
		_laidOutTitleBarHeight = base.TitleBarBounds.Height;
		_title.SetSize(340, 34);
		_title.SetPosition(24, Y(9));
		_close.SetSize(24, 24);
		_close.SetPosition(396, Y(14));
		_slot.SetSize(42, 42);
		_slot.SetPosition(24, Y(70));
		int width = ((_pricing == null) ? 336 : 206);
		_name.SetSize(width, 24);
		_name.SetPosition(80, Y(68));
		_kind.SetSize(width, 20);
		_kind.SetPosition(80, Y(92));
		_unitCaption?.SetSize(130, 20);
		_unitCaption?.SetPosition(286, Y(68));
		_unitAmount?.SetSize(100, 26);
		_unitAmount?.SetPosition(292, Y(90));
		_unitMark?.SetPosition(398, Y(94));
		_topRule.SetSize(392, 1);
		_topRule.SetPosition(24, Y(128));
		_minus.SetPosition(24, Y(144));
		_field.SetSize(216, 40);
		_field.SetPosition(72, Y(144));
		_plus.SetPosition(296, Y(144));
		_max.SetPosition(344, Y(144));
		_rail.SetSize(392, 10);
		_rail.SetPosition(24, Y(203));
		_fill.SetPosition(24, Y(203));
		_slider.SetSize(392, 42);
		_slider.SetPosition(24, Y(198));
		_limit.SetSize(260, 18);
		_limit.SetPosition(24, Y(222));
		_maxLegend.SetSize(160, 18);
		_maxLegend.SetPosition(256, Y(222));
		_priceRule?.SetSize(392, 1);
		_priceRule?.SetPosition(24, Y(254));
		_totalCaption?.SetSize(160, 36);
		_totalCaption?.SetPosition(24, Y(262));
		_totalAmount?.SetSize(170, 36);
		_totalAmount?.SetPosition(220, Y(262));
		_totalMark?.SetPosition(394, Y(269));
		_balanceCaption?.SetSize(200, 24);
		_balanceCaption?.SetPosition(24, Y(298));
		_balanceAmount?.SetSize(140, 24);
		_balanceAmount?.SetPosition(256, Y(298));
		_balanceMark?.SetPosition(400, Y(302));
		int windowY = _windowHeight - 52;
		_cancel.SetPosition(24, Y(windowY));
		_confirm.SetPosition(172, Y(windowY));
	}

	private static string Classification(ItemDescriptor descriptor)
	{
		List<string> list = new List<string>();
		if (Strings.ItemDescription.ItemTypes.TryGetValue((int)descriptor.ItemType, out LocalizedString value) && value.ToString().Length > 0)
		{
			list.Add(value);
		}
		if (descriptor.Rarity > 0 && Options.Instance.Items.TryGetRarityName(descriptor.Rarity, out string rarityName) && Strings.ItemDescription.Rarity.TryGetValue(rarityName, out LocalizedString value2))
		{
			list.Add(value2);
		}
		return string.Join(Strings.ItemDescription.ClassificationSeparator, list);
	}

	private static Color RarityColor(ItemDescriptor descriptor)
	{
		if (!CustomColors.Items.Rarities.TryGetValue(descriptor.Rarity, out var value) || !(value != null))
		{
			return Entry;
		}
		return value;
	}

	private static Label MakeLabel(Base parent, string name, int size, Color color, IFont? font)
	{
		Label label = new Label(parent, name)
		{
			Font = font,
			FontSize = size,
			AutoSizeToContents = false,
			MouseInputEnabled = false,
			TextAlign = (Pos.Left | Pos.CenterV)
		};
		label.SetTextColor(color, ComponentState.Normal);
		return label;
	}

	private static ImagePanel MakeImage(Base parent, string name, string? texture)
	{
		ImagePanel imagePanel = new ImagePanel(parent, name)
		{
			MouseInputEnabled = false
		};
		if (texture != null)
		{
			imagePanel.Texture = GameContentManager.Current.GetTexture(TextureType.Gui, texture);
		}
		return imagePanel;
	}

	private Button MakeStepButton(string name, string key)
	{
		Button button = new Button(this, name, disableText: true);
		button.SetStateTexture(ComponentState.Normal, key + ".png");
		button.SetStateTexture(ComponentState.Hovered, key + "_hover.png");
		button.SetStateTexture(ComponentState.Active, key + "_clicked.png");
		button.SetStateTexture(ComponentState.Disabled, key + "_disabled.png");
		button.SetSize(40, 40);
		return button;
	}

	private Button MakeButton(string name, string key, int fontSize, Color ink)
	{
		string[] array = key.Split('_')[^1].Split('x');
		int width = int.Parse(array[0]);
		int height = int.Parse(array[1]);
		Button button = new Button(this, name)
		{
			Font = _body,
			FontSize = fontSize,
			TextAlign = Pos.Center,
			UsesHeadingFace = false
		};
		button.SetStateTexture(ComponentState.Normal, key + ".png");
		button.SetStateTexture(ComponentState.Hovered, key + "_hover.png");
		button.SetStateTexture(ComponentState.Active, key + "_clicked.png");
		button.SetStateTexture(ComponentState.Disabled, key + "_disabled.png");
		button.SetTextColor(ink, ComponentState.Normal);
		button.SetTextColor(OrangeLight, ComponentState.Hovered);
		button.SetTextColor(Night, ComponentState.Active);
		button.SetTextColor(Dim, ComponentState.Disabled);
		button.SetSize(width, height);
		return button;
	}
}
