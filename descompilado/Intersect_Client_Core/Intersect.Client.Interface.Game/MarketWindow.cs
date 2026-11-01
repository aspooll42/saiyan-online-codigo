using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Items;
using Intersect.Client.General;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.Market;
using Intersect.Models;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game;

public class MarketWindow : WindowControl
{
	private enum Mode
	{
		Buy,
		Sell,
		Mine
	}

	private const int WindowWidth = 760;

	private const int WindowHeight = 600;

	private const int SidebarX = 16;

	private const int SidebarWidth = 184;

	private const int ContentX = 216;

	private const int ContentWidth = 528;

	private const int TabsY = 72;

	private const int ToolbarY = 118;

	private const int TableY = 156;

	private const int FooterY = 556;

	private const int BottomY = 584;

	private const int TabWidth = 172;

	private const int TabHeight = 34;

	private const int CategoryHeight = 38;

	private const int CategoryGap = 6;

	private const int RowHeight = 46;

	private const int SlotSize = 42;

	private const int SlotGap = 6;

	private const int JetColumnX = 222;

	private const int JetColumnWidth = 58;

	private const int GridColumns = 6;

	private const int FieldHeight = 28;

	private const long SearchDelayMs = 350L;

	private const int CoinSize = 32;

	private const int CoinFrames = 4;

	private const long CoinRestMs = 1600L;

	private const long CoinFrameMs = 90L;

	private static readonly Color OrangeLight = new Color(255, 255, 172, 82);

	private static readonly Color Gold = new Color(255, 245, 190, 90);

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color Entry = new Color(255, 196, 204, 220);

	private static readonly Color Muted = new Color(255, 176, 186, 206);

	private static readonly Color Dim = new Color(255, 150, 160, 182);

	private static readonly Color Green = new Color(255, 120, 200, 140);

	private static readonly Color RollGreen = new Color(255, 142, 214, 124);

	private static readonly Color Red = new Color(255, 225, 95, 95);

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private static Guid _knownCurrencyId;

	private readonly Canvas _gameCanvas;

	private readonly IFont? _font;

	private readonly IFont? _strongFont;

	private readonly IFont? _posterFont;

	private readonly Label _title;

	private readonly ImagePanel _wallet;

	private readonly ImagePanel _coin;

	private readonly IGameTexture? _coinAnimation;

	private int _coinFrame = -1;

	private readonly Label _balance;

	private readonly Label _currencyName;

	private readonly Button _close;

	private readonly List<(Button Tab, Mode Mode)> _modeTabs = new List<(Button, Mode)>();

	private readonly Label _categoriesLabel;

	private readonly List<(Button Tab, ImagePanel Glyph, Label Count, MarketCategory Category)> _categoryTabs = new List<(Button, ImagePanel, Label, MarketCategory)>();

	private readonly Button _minePanel;

	private readonly Label _mineHeading;

	private readonly Label _mineCount;

	private readonly Label _minePending;

	private readonly ImagePanel _searchBack;

	private readonly ImagePanel _searchGlyph;

	private readonly TextBox _search;

	private readonly Label _searchHint;

	private readonly Button _sortButton;

	private readonly ImagePanel _sortChevron;

	private readonly Label _countLabel;

	private readonly Label _mineSummary;

	private readonly Button _collectButton;

	private readonly ImagePanel _tableCard;

	private readonly Label _columnItem;

	private readonly Label _columnRoll;

	private readonly Label _columnPrice;

	private readonly ImagePanel _tableRule;

	private readonly ScrollControl _list;

	private readonly Label _status;

	private readonly Button _previous;

	private readonly Label _pageLabel;

	private readonly Button _next;

	private readonly ImagePanel _gridCard;

	private readonly Label _gridHeading;

	private readonly Label _gridCount;

	private readonly ImagePanel _gridRule;

	private readonly ScrollControl _grid;

	private readonly ImagePanel _formCard;

	private readonly Label _formHeading;

	private readonly ImagePanel _formRule;

	private readonly Label _sellHint;

	private readonly ImagePanel _sellSlot;

	private readonly ImagePanel _sellIcon;

	private readonly Label _sellName;

	private readonly Label _sellOwned;

	private readonly Label _quantityLabel;

	private readonly ImagePanel _quantityBack;

	private readonly TextBoxNumeric _quantity;

	private readonly Label _quantityMax;

	private readonly Label _priceLabel;

	private readonly ImagePanel _priceBack;

	private readonly TextBoxNumeric _price;

	private readonly ImagePanel _priceCoin;

	private readonly Button _cheapestHint;

	private readonly Label _cheapestLabel;

	private readonly Label _cheapestValue;

	private readonly Button _sameRollHint;

	private readonly Label _sameRollLabel;

	private readonly Label _sameRollValue;

	private readonly ImagePanel _summaryRule;

	private readonly Label _totalLabel;

	private readonly Label _totalValue;

	private readonly Label _taxLineLabel;

	private readonly Label _taxLineValue;

	private readonly ImagePanel _netRule;

	private readonly Label _netLabel;

	private readonly Label _netValue;

	private readonly ImagePanel _netCoin;

	private readonly Label _sellStatus;

	private readonly Button _sellButton;

	private readonly List<Base> _rows = new List<Base>();

	private Mode _mode;

	private MarketCategory _category;

	private MarketSort _sort;

	private int _page;

	private int _pageCount = 1;

	private int _totalCount;

	private string _query = string.Empty;

	private long _searchDueAt;

	private MarketListingInfo[] _listings = Array.Empty<MarketListingInfo>();

	private MarketListingInfo[] _mine = Array.Empty<MarketListingInfo>();

	private int _maxListings;

	private int _taxPercent;

	private int _selectedSlot = -1;

	private Guid _selectedItemId;

	private MarketPriceInfo? _priceHints;

	private Button? _hoveredHint;

	private int _inventorySignature;

	private string _balanceText = string.Empty;

	private string _pageStatusText = string.Empty;

	private bool _needsRebuild = true;

	private int _laidOutTitleBarHeight = -1;

	public bool IsOpen => !base.IsHidden;

	internal static Guid KnownCurrencyId => _knownCurrencyId;

	public MarketWindow(Canvas gameCanvas)
		: base(gameCanvas, string.Empty, modal: false, "MarketWindow")
	{
		_gameCanvas = gameCanvas;
		_font = Globals.ContentManager.GetFont("chakrapetch");
		_strongFont = Globals.ContentManager.GetFont("rajdhanibold");
		_posterFont = Globals.ContentManager.GetFont("refuel");
		DisableResizing();
		SetSize(760, 600);
		SetPosition((_gameCanvas.Width - 760) / 2, (_gameCanvas.Height - 600) / 2);
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_window_marche.png");
		SetImage(texture, "saiyan_window_marche.png", ControlState.Active);
		SetImage(texture, "saiyan_window_marche.png", ControlState.Inactive);
		base.IsClosable = false;
		if (base.Titlebar is Titlebar titlebar)
		{
			titlebar.Label.IsHidden = true;
		}
		_title = MakeLabel(this, "Title", 20, White, _posterFont);
		_title.Text = Strings.Market.Title.ToString().ToUpperInvariant();
		WindowTitle.Apply(_title);
		_wallet = MakeCard(this, "Wallet", "saiyan_slot.png", 6);
		_coin = MakeImage(_wallet, "Coin", null, 32, 32);
		_coinAnimation = Globals.ContentManager.GetTexture(TextureType.Item, "zeni_anime.png");
		_balance = MakeLabel(_wallet, "Balance", 14, Gold, _strongFont);
		_currencyName = MakeLabel(_wallet, "CurrencyName", 10, Muted, _font);
		_close = new Button(this, "Close", disableText: true);
		_close.SetStateTexture(ComponentState.Normal, "saiyan_close.png");
		_close.SetStateTexture(ComponentState.Hovered, "saiyan_close_hover.png");
		_close.SetStateTexture(ComponentState.Active, "saiyan_close_clicked.png");
		_close.Clicked += delegate
		{
			CloseWindow();
		};
		Mode[] array = new Mode[3]
		{
			Mode.Buy,
			Mode.Sell,
			Mode.Mine
		};
		foreach (Mode mode in array)
		{
			Button button = new Button(this, $"Mode_{mode}")
			{
				Font = _font,
				FontSize = 11,
				TextAlign = Pos.Center
			};
			button.Clicked += delegate
			{
				SetMode(mode);
			};
			_modeTabs.Add((button, mode));
		}
		_categoriesLabel = MakeLabel(this, "CategoriesLabel", 10, Muted, _font);
		_categoriesLabel.Text = Strings.Market.Categories.ToString().ToUpperInvariant();
		MarketCategory[] array2 = new MarketCategory[4]
		{
			MarketCategory.All,
			MarketCategory.Equipment,
			MarketCategory.Consumables,
			MarketCategory.Resources
		};
		foreach (MarketCategory category in array2)
		{
			Button button2 = new Button(this, $"Category_{category}")
			{
				Font = _font,
				FontSize = 11,
				TextAlign = (Pos.Left | Pos.CenterV),
				Padding = new Padding(42, 0, 40, 0),
				Text = CategoryName(category)
			};
			button2.Clicked += delegate
			{
				_category = category;
				_needsRebuild = true;
				RequestSearch(0);
			};
			ImagePanel item = MakeImage(button2, "Glyph", CategoryGlyph(category), 22, 22);
			Label label = MakeLabel(button2, "Count", 9, Muted, _font);
			label.TextAlign = Pos.Right | Pos.CenterV;
			_categoryTabs.Add((button2, item, label, category));
		}
		_minePanel = new Button(this, "MinePanel", disableText: true);
		_minePanel.SetStateTexture(ComponentState.Normal, "saiyan_card.png");
		_minePanel.SetStateTexture(ComponentState.Hovered, "saiyan_card_hover.png");
		_minePanel.SetStateTexture(ComponentState.Active, "saiyan_card_hover.png");
		_minePanel.Clicked += delegate
		{
			SetMode(Mode.Mine);
		};
		_mineHeading = MakeLabel(_minePanel, "MineHeading", 10, Muted, _font);
		_mineHeading.Text = Strings.Market.MineHeading.ToString().ToUpperInvariant();
		_mineCount = MakeLabel(_minePanel, "MineCount", 13, White, _strongFont);
		_minePending = MakeLabel(_minePanel, "MinePending", 10, Muted, _font);
		(ImagePanel, TextBox) tuple = MakeField(this, "Search", 260, numeric: false, 30);
		_searchBack = tuple.Item1;
		_search = tuple.Item2;
		_searchGlyph = MakeImage(_searchBack, "SearchGlyph", "inv_loupe.png", 14, 14);
		_search.TextChanged += delegate
		{
			_query = _search.Text ?? string.Empty;
			_searchHint.IsHidden = _query.Length > 0;
			_searchDueAt = Timing.Global.Milliseconds + 350;
		};
		_search.SubmitPressed += delegate
		{
			RequestSearch(0);
		};
		_searchHint = MakeLabel(_searchBack, "SearchHint", 10, Dim, _font);
		_searchHint.Text = Strings.Market.SearchHint;
		_sortButton = MakeButton(this, "Sort", 176, 28);
		_sortButton.TextAlign = Pos.Left | Pos.CenterV;
		_sortButton.Padding = new Padding(12, 0, 28, 0);
		_sortButton.Clicked += delegate
		{
			_sort = (MarketSort)((int)(_sort + 1) % Enum.GetValues<MarketSort>().Length);
			_needsRebuild = true;
			RequestSearch(0);
		};
		_sortChevron = MakeImage(_sortButton, "Chevron", "inv_chevron.png", 12, 8);
		_countLabel = MakeLabel(this, "Count", 10, Muted, _font);
		_countLabel.TextAlign = Pos.Right | Pos.CenterV;
		_mineSummary = MakeLabel(this, "MineSummary", 13, White, _strongFont);
		_collectButton = MakeButton(this, "Collect", 150, 28);
		_collectButton.Text = Strings.Market.CollectAll;
		_collectButton.Clicked += delegate
		{
			PacketSender.SendMarketCollect();
		};
		_tableCard = MakeCard(this, "TableCard", "saiyan_card.png", 8);
		_columnItem = MakeLabel(_tableCard, "ColumnItem", 10, Muted, _font);
		_columnItem.Text = Strings.Market.ColumnItem.ToString().ToUpperInvariant();
		_columnRoll = MakeLabel(_tableCard, "ColumnRoll", 10, Muted, _font);
		_columnRoll.Text = Strings.Market.ColumnRoll.ToString().ToUpperInvariant();
		_columnPrice = MakeLabel(_tableCard, "ColumnPrice", 10, Muted, _font);
		_columnPrice.Text = Strings.Market.UnitPriceLabel.ToString().ToUpperInvariant();
		_columnPrice.TextAlign = Pos.Right | Pos.CenterV;
		_tableRule = MakeImage(_tableCard, "Rule", "hdv_filet.png", 8, 1);
		_list = new ScrollControl(_tableCard, "MarketList");
		StyleScroll(_list);
		_status = MakeLabel(this, "Status", 10, Muted, _font);
		_previous = MakeButton(this, "Previous", 28, 28);
		_previous.Text = "<";
		_previous.Clicked += delegate
		{
			RequestSearch(_page - 1);
		};
		_pageLabel = MakeLabel(this, "Page", 13, White, _strongFont);
		_pageLabel.TextAlign = Pos.Center;
		_next = MakeButton(this, "Next", 28, 28);
		_next.Text = ">";
		_next.Clicked += delegate
		{
			RequestSearch(_page + 1);
		};
		_gridCard = MakeCard(this, "GridCard", "saiyan_card.png", 8);
		_gridHeading = MakeLabel(_gridCard, "GridHeading", 10, Muted, _font);
		_gridHeading.Text = Strings.Market.InventoryHeading.ToString().ToUpperInvariant();
		_gridCount = MakeLabel(_gridCard, "GridCount", 9, Muted, _font);
		_gridCount.TextAlign = Pos.Right | Pos.CenterV;
		_gridRule = MakeImage(_gridCard, "Rule", "hdv_filet.png", 8, 1);
		_grid = new ScrollControl(_gridCard, "InventoryGrid");
		StyleScroll(_grid);
		_formCard = MakeCard(this, "FormCard", "saiyan_card.png", 8);
		_formHeading = MakeLabel(_formCard, "FormHeading", 10, Muted, _font);
		_formHeading.Text = Strings.Market.SellButton.ToString().ToUpperInvariant();
		_formRule = MakeImage(_formCard, "Rule", "hdv_filet.png", 8, 1);
		_sellHint = MakeLabel(_formCard, "SellHint", 11, Dim, _font);
		_sellHint.Text = Strings.Market.SellHint;
		_sellHint.WrappingBehavior = WrappingBehavior.Wrapped;
		_sellHint.TextAlign = Pos.Left | Pos.Top;
		_sellSlot = MakeCard(_formCard, "SellSlot", "saiyan_slot_selected.png", 6);
		_sellSlot.MouseInputEnabled = true;
		_sellSlot.HoverEnter += delegate
		{
			ShowSelectedTooltip();
		};
		_sellSlot.HoverLeave += delegate
		{
			HideTooltip();
		};
		_sellIcon = MakeImage(_sellSlot, "SellIcon", null, 32, 32);
		_sellName = MakeLabel(_formCard, "SellName", 11, Entry, _font);
		_sellOwned = MakeLabel(_formCard, "SellOwned", 10, Muted, _font);
		_quantityLabel = MakeLabel(_formCard, "QuantityLabel", 10, Muted, _font);
		_quantityLabel.Text = Strings.Market.Quantity.ToString().ToUpperInvariant();
		tuple = MakeField(_formCard, "Quantity", 128, numeric: true, 8);
		_quantityBack = tuple.Item1;
		TextBox item2 = tuple.Item2;
		_quantity = (TextBoxNumeric)item2;
		_quantity.TextChanged += delegate
		{
			UpdateSellSummary();
		};
		_quantityMax = MakeLabel(_formCard, "QuantityMax", 10, Dim, _font);
		_priceLabel = MakeLabel(_formCard, "PriceLabel", 10, Muted, _font);
		_priceLabel.Text = Strings.Market.UnitPriceLabel.ToString().ToUpperInvariant();
		tuple = MakeField(_formCard, "Price", 128, numeric: true, 8);
		_priceBack = tuple.Item1;
		TextBox item3 = tuple.Item2;
		_price = (TextBoxNumeric)item3;
		_price.TextChanged += delegate
		{
			UpdateSellSummary();
		};
		_price.SubmitPressed += delegate
		{
			SubmitSell();
		};
		_priceCoin = MakeImage(_formCard, "PriceCoin", null, 16, 16);
		(_cheapestHint, _cheapestLabel, _cheapestValue) = MakePriceHint("CheapestHint", Strings.Market.HintCheapest, () => _priceHints?.Cheapest ?? 0);
		(_sameRollHint, _sameRollLabel, _sameRollValue) = MakePriceHint("SameRollHint", Strings.Market.HintSameRoll, () => _priceHints?.CheapestSameRollOrBetter ?? 0);
		MarketPrices.Received += OnPricesReceived;
		_summaryRule = MakeImage(_formCard, "SummaryRule", "hdv_filet.png", 8, 1);
		_totalLabel = MakeLabel(_formCard, "TotalLabel", 10, Muted, _font);
		_totalLabel.Text = Strings.Market.Total;
		_totalValue = MakeLabel(_formCard, "TotalValue", 10, Entry, _font);
		_totalValue.TextAlign = Pos.Right | Pos.CenterV;
		_taxLineLabel = MakeLabel(_formCard, "TaxLineLabel", 10, Muted, _font);
		_taxLineValue = MakeLabel(_formCard, "TaxLineValue", 10, Entry, _font);
		_taxLineValue.TextAlign = Pos.Right | Pos.CenterV;
		_netRule = MakeImage(_formCard, "NetRule", "hdv_filet.png", 8, 1);
		_netLabel = MakeLabel(_formCard, "NetLabel", 11, White, _font);
		_netLabel.Text = Strings.Market.SellNetLabel;
		_netValue = MakeLabel(_formCard, "NetValue", 13, Gold, _strongFont);
		_netValue.TextAlign = Pos.Right | Pos.CenterV;
		_netCoin = MakeImage(_formCard, "NetCoin", null, 16, 16);
		_sellStatus = MakeLabel(_formCard, "SellStatus", 10, Muted, _font);
		_sellStatus.WrappingBehavior = WrappingBehavior.Wrapped;
		_sellStatus.TextAlign = Pos.Left | Pos.Top;
		_sellButton = MakeCtaButton(_formCard, "SellButton");
		_sellButton.Text = Strings.Market.SellButton;
		_sellButton.Clicked += delegate
		{
			SubmitSell();
		};
		Layout();
		Hide();
	}

	public void Toggle()
	{
		if (IsOpen)
		{
			CloseWindow();
			return;
		}
		Layout();
		_needsRebuild = true;
		SetStatus(string.Empty, Muted);
		Show();
		BringToFront();
		RequestSearch(_page);
		PacketSender.SendMarketMyListings();
		RequestPriceHints();
	}

	private void CloseWindow()
	{
		HideTooltip();
		Close();
	}

	protected override void Dispose(bool disposing)
	{
		MarketPrices.Received -= OnPricesReceived;
		base.Dispose(disposing);
	}

	public void OnListings(MarketListingsPacket packet)
	{
		_listings = packet.Listings ?? Array.Empty<MarketListingInfo>();
		_page = packet.Page;
		_pageCount = Math.Max(1, packet.PageCount);
		_totalCount = packet.TotalCount;
		_taxPercent = packet.SaleTaxPercent;
		RememberCurrency(packet.CurrencyItemId);
		_needsRebuild = true;
	}

	public void OnMyListings(MarketMyListingsResultPacket packet)
	{
		_mine = packet.Listings ?? Array.Empty<MarketListingInfo>();
		_maxListings = packet.MaxListings;
		_taxPercent = packet.SaleTaxPercent;
		RememberCurrency(packet.CurrencyItemId);
		_needsRebuild = true;
	}

	public void OnResult(MarketResultPacket packet, string text, bool success)
	{
		if (packet.Result == MarketResult.Bought)
		{
			Audio.AddGameSound("buy.wav", loop: false);
		}
		if (!base.IsHidden)
		{
			SetStatus((packet.Result == MarketResult.NoCurrency) ? string.Empty : text, success ? Green : Red);
			switch (packet.Result)
			{
			default:
				return;
			case MarketResult.Listed:
				_selectedSlot = -1;
				_selectedItemId = Guid.Empty;
				break;
			case MarketResult.Bought:
			case MarketResult.Cancelled:
			case MarketResult.Collected:
			case MarketResult.ItemsReturned:
			case MarketResult.SoldNotice:
			case MarketResult.ListingUnavailable:
				break;
			}
			if (_mode == Mode.Buy)
			{
				RequestSearch(_page);
			}
			PacketSender.SendMarketMyListings();
			_needsRebuild = true;
		}
	}

	internal static void RememberCurrency(Guid currencyId)
	{
		if (currencyId != Guid.Empty)
		{
			_knownCurrencyId = currencyId;
		}
	}

	private void OnPricesReceived(MarketPricesResultPacket packet)
	{
		if (!base.IsHidden && packet.Slot >= 0 && packet.Slot == _selectedSlot && !(_selectedItemId == Guid.Empty))
		{
			_priceHints = packet.Prices?.FirstOrDefault((MarketPriceInfo info) => info?.ItemId == _selectedItemId) ?? new MarketPriceInfo
			{
				ItemId = _selectedItemId
			};
			UpdatePriceHints();
		}
	}

	private void RequestPriceHints()
	{
		_priceHints = null;
		if (_selectedSlot >= 0 && _selectedItemId != Guid.Empty)
		{
			MarketPrices.Request(new Guid[1] { _selectedItemId }, _selectedSlot);
		}
	}

	public void Update()
	{
		if (base.IsHidden)
		{
			return;
		}
		if (base.TitleBarBounds.Height != _laidOutTitleBarHeight)
		{
			Layout();
			_needsRebuild = true;
		}
		if (_searchDueAt > 0 && Timing.Global.Milliseconds >= _searchDueAt)
		{
			RequestSearch(0);
		}
		int num = InventorySignature();
		if (num != _inventorySignature)
		{
			_inventorySignature = num;
			UpdateBalance();
			if (_mode == Mode.Sell)
			{
				_needsRebuild = true;
			}
		}
		AnimateCoin();
		if (_needsRebuild)
		{
			_needsRebuild = false;
			Rebuild();
		}
	}

	private void AnimateCoin()
	{
		IGameTexture coinAnimation = _coinAnimation;
		if (coinAnimation != null && coinAnimation.Width > 0 && !_coin.IsHidden)
		{
			long num = 1870L;
			long num2 = Timing.Global.Milliseconds % num;
			int num3 = ((num2 >= 1600) ? Math.Min(3, 1 + (int)((num2 - 1600) / 90)) : 0);
			if (num3 != _coinFrame)
			{
				_coinFrame = num3;
				int num4 = coinAnimation.Width / 4;
				_coin.SetTextureRect(num3 * num4, 0, num4, coinAnimation.Height);
				_coin.SetSize(32, 32);
			}
		}
	}

	private void RequestSearch(int page)
	{
		_searchDueAt = 0L;
		PacketSender.SendMarketSearch(_query.Trim(), _category, _sort, Math.Max(0, page));
	}

	private void SetMode(Mode mode)
	{
		_mode = mode;
		HideTooltip();
		_needsRebuild = true;
		switch (mode)
		{
		case Mode.Buy:
			RequestSearch(_page);
			break;
		case Mode.Mine:
			PacketSender.SendMarketMyListings();
			break;
		default:
			RequestPriceHints();
			break;
		}
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

	private static ImagePanel MakeCard(Base parent, string name, string texture, int ninePatch)
	{
		return new ImagePanel(parent, name)
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, texture),
			TextureNinePatchMargin = new Margin(ninePatch),
			MouseInputEnabled = false
		};
	}

	private static ImagePanel MakeImage(Base parent, string name, string? texture, int width, int height)
	{
		ImagePanel imagePanel = new ImagePanel(parent, name)
		{
			MouseInputEnabled = false
		};
		if (texture != null)
		{
			imagePanel.Texture = Globals.ContentManager.GetTexture(TextureType.Gui, texture);
		}
		imagePanel.SetSize(width, height);
		return imagePanel;
	}

	private Button MakeCtaButton(Base parent, string name)
	{
		Button button = MakeButton(parent, name, 180, 34, "saiyan_cta");
		button.FontSize = 12;
		button.SetTextColor(Night, ComponentState.Normal);
		button.SetTextColor(Night, ComponentState.Hovered);
		button.SetTextColor(Night, ComponentState.Active);
		button.SetTextColor(Dim, ComponentState.Disabled);
		return button;
	}

	private Button MakeButton(Base parent, string name, int width, int height, string famille = "saiyan_btn")
	{
		Button button = new Button(parent, name)
		{
			Font = _font,
			FontSize = 11,
			TextAlign = Pos.Center
		};
		string text = $"{famille}_{width}x{height}";
		button.SetStateTexture(ComponentState.Normal, text + ".png");
		button.SetStateTexture(ComponentState.Hovered, text + "_hover.png");
		button.SetStateTexture(ComponentState.Active, text + "_clicked.png");
		button.SetStateTexture(ComponentState.Disabled, text + "_disabled.png");
		button.SetTextColor(White, ComponentState.Normal);
		button.SetTextColor(OrangeLight, ComponentState.Hovered);
		button.SetTextColor(Night, ComponentState.Active);
		button.SetTextColor(Dim, ComponentState.Disabled);
		button.SetSize(width, height);
		return button;
	}

	private (ImagePanel Back, TextBox Box) MakeField(Base parent, string name, int width, bool numeric, int leading)
	{
		ImagePanel imagePanel = new ImagePanel(parent, name + "Back")
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, $"saiyan_field_{width}x{28}.png")
		};
		imagePanel.SetSize(width, 28);
		TextBox textBox = (numeric ? new TextBoxNumeric(imagePanel, name) : new TextBox(imagePanel, name));
		textBox.Font = _font;
		textBox.FontSize = 11;
		textBox.ShouldDrawBackground = false;
		textBox.SetTextColor(White, ComponentState.Normal);
		textBox.SetSize(width - leading - 8, 22);
		textBox.SetPosition(leading, 3);
		textBox.MaximumLength = (numeric ? 10 : 32);
		return (Back: imagePanel, Box: textBox);
	}

	private (Button Hit, Label Label, Label Value) MakePriceHint(string name, string text, Func<long> amount)
	{
		Button hit = new Button(_formCard, name, disableText: true)
		{
			ShouldDrawBackground = false
		};
		Label label = MakeLabel(hit, "Label", 10, Muted, _font);
		label.Text = text;
		Label label2 = MakeLabel(hit, "Value", 12, White, _strongFont);
		label2.TextAlign = Pos.Right | Pos.CenterV;
		hit.HoverEnter += delegate
		{
			_hoveredHint = hit;
			UpdatePriceHints();
		};
		hit.HoverLeave += delegate
		{
			if (_hoveredHint == hit)
			{
				_hoveredHint = null;
			}
			UpdatePriceHints();
		};
		hit.Clicked += delegate
		{
			UsePriceHint(amount());
		};
		return (Hit: hit, Label: label, Value: label2);
	}

	private static void StyleScroll(ScrollControl scroll)
	{
		scroll.EnableScroll(horizontal: false, vertical: true);
		scroll.VerticalScrollBar.SetBackgroundTemplate(null, "saiyan_scroll_bg.png");
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_scroll_bar.png");
		IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_scroll_bar_hover.png");
		scroll.VerticalScrollBar.SetScrollBarImage(texture, "saiyan_scroll_bar.png", ComponentState.Normal);
		scroll.VerticalScrollBar.SetScrollBarImage(texture2, "saiyan_scroll_bar_hover.png", ComponentState.Hovered);
		scroll.VerticalScrollBar.SetScrollBarImage(texture2, "saiyan_scroll_bar_hover.png", ComponentState.Active);
	}

	private static void StyleModeTab(Button tab, bool selected)
	{
		tab.SetStateTexture(ComponentState.Normal, selected ? "inv_onglet_actif.png" : "inv_onglet.png");
		tab.SetStateTexture(ComponentState.Hovered, selected ? "inv_onglet_actif.png" : "inv_onglet_survol.png");
		tab.SetStateTexture(ComponentState.Active, "inv_onglet_actif.png");
		tab.SetTextColor(selected ? OrangeLight : Muted, ComponentState.Normal);
		tab.SetTextColor(selected ? OrangeLight : White, ComponentState.Hovered);
		tab.SetTextColor(OrangeLight, ComponentState.Active);
	}

	private static void StyleCategoryTab(Button tab, bool selected)
	{
		tab.SetStateTexture(ComponentState.Normal, selected ? "saiyan_tab_selected.png" : "saiyan_tab.png");
		tab.SetStateTexture(ComponentState.Hovered, selected ? "saiyan_tab_selected.png" : "saiyan_tab_hover.png");
		tab.SetStateTexture(ComponentState.Active, "saiyan_tab_selected.png");
		tab.SetTextColor(selected ? White : Entry, ComponentState.Normal);
		tab.SetTextColor(OrangeLight, ComponentState.Hovered);
		tab.SetTextColor(OrangeLight, ComponentState.Active);
	}

	private static string CategoryGlyph(MarketCategory category)
	{
		return category switch
		{
			MarketCategory.Equipment => "inv_glyphe_equipement.png", 
			MarketCategory.Consumables => "inv_glyphe_consommable.png", 
			MarketCategory.Resources => "inv_glyphe_ressource.png", 
			_ => "inv_glyphe_tout.png", 
		};
	}

	private new int Y(int windowY)
	{
		return windowY - _laidOutTitleBarHeight;
	}

	private void Layout()
	{
		_laidOutTitleBarHeight = base.TitleBarBounds.Height;
		_title.SetSize(400, 38);
		_title.SetPosition(26, Y(10));
		_close.SetSize(24, 24);
		_close.SetPosition(720, Y(16));
		_wallet.SetSize(176, 36);
		_wallet.SetPosition(528, Y(11));
		_coin.SetPosition(6, 2);
		_balance.SetSize(90, 36);
		_balance.SetPosition(42, 0);
		_currencyName.SetSize(38, 36);
		_currencyName.SetPosition(128, 0);
		for (int i = 0; i < _modeTabs.Count; i++)
		{
			_modeTabs[i].Tab.SetSize(172, 34);
			_modeTabs[i].Tab.SetPosition(216 + i * 178, Y(72));
		}
		_categoriesLabel.SetSize(156, 20);
		_categoriesLabel.SetPosition(30, Y(80));
		int num = Y(106);
		foreach (var (button, imagePanel, label, _) in _categoryTabs)
		{
			button.SetSize(168, 38);
			button.SetPosition(24, num);
			imagePanel.SetPosition(12, 8);
			label.SetSize(34, 38);
			label.SetPosition(124, 0);
			num += 44;
		}
		_minePanel.SetSize(168, 76);
		_minePanel.SetPosition(24, Y(508));
		_mineHeading.SetSize(140, 18);
		_mineHeading.SetPosition(14, 6);
		_mineCount.SetSize(140, 26);
		_mineCount.SetPosition(14, 24);
		_minePending.SetSize(140, 20);
		_minePending.SetPosition(14, 50);
		_searchBack.SetPosition(216, Y(118));
		_searchGlyph.SetPosition(9, 7);
		_searchHint.SetSize(222, 22);
		_searchHint.SetPosition(30, 3);
		_sortButton.SetPosition(484, Y(118));
		_sortChevron.SetPosition(152, 10);
		_countLabel.SetSize(100, 28);
		_countLabel.SetPosition(644, Y(118));
		_mineSummary.SetSize(366, 28);
		_mineSummary.SetPosition(216, Y(118));
		_collectButton.SetPosition(594, Y(118));
		int num2 = 394;
		_tableCard.SetSize(528, num2);
		_tableCard.SetPosition(216, Y(156));
		_columnItem.SetSize(150, 20);
		_columnItem.SetPosition(62, 8);
		_columnRoll.SetSize(58, 20);
		_columnRoll.SetPosition(228, 8);
		_columnPrice.SetSize(120, 20);
		_columnPrice.SetPosition(300, 8);
		_tableRule.SetSize(508, 1);
		_tableRule.SetPosition(10, 32);
		_list.SetSize(516, num2 - 40 - 6);
		_list.SetPosition(6, 40);
		_status.SetSize(396, 28);
		_status.SetPosition(216, Y(556));
		_next.SetPosition(716, Y(556));
		_pageLabel.SetSize(60, 28);
		_pageLabel.SetPosition(656, Y(556));
		_previous.SetPosition(628, Y(556));
		int num3 = 466;
		_gridCard.SetSize(316, num3);
		_gridCard.SetPosition(216, Y(118));
		_gridHeading.SetSize(200, 20);
		_gridHeading.SetPosition(14, 8);
		_gridCount.SetSize(70, 20);
		_gridCount.SetPosition(232, 8);
		_gridRule.SetSize(296, 1);
		_gridRule.SetPosition(10, 32);
		_grid.SetSize(300, num3 - 40 - 10);
		_grid.SetPosition(12, 40);
		int x = 544;
		int num4 = 200;
		_formCard.SetSize(num4, num3);
		_formCard.SetPosition(x, Y(118));
		_formHeading.SetSize(num4 - 28, 20);
		_formHeading.SetPosition(14, 8);
		_formRule.SetSize(num4 - 20, 1);
		_formRule.SetPosition(10, 32);
		_sellHint.SetSize(num4 - 28, 80);
		_sellHint.SetPosition(14, 44);
		_sellSlot.SetSize(42, 42);
		_sellSlot.SetPosition(14, 44);
		_sellIcon.SetPosition(5, 5);
		_sellName.SetSize(num4 - 66 - 12, 22);
		_sellName.SetPosition(66, 44);
		_sellOwned.SetSize(num4 - 66 - 12, 20);
		_sellOwned.SetPosition(66, 66);
		_quantityLabel.SetSize(150, 18);
		_quantityLabel.SetPosition(14, 104);
		_quantityBack.SetPosition(14, 124);
		_quantityMax.SetSize(num4 - 148 - 10, 28);
		_quantityMax.SetPosition(148, 124);
		_priceLabel.SetSize(150, 18);
		_priceLabel.SetPosition(14, 164);
		_priceBack.SetPosition(14, 184);
		_priceCoin.SetPosition(150, 190);
		(Button, Label, Label, int)[] array = new(Button, Label, Label, int)[2]
		{
			(_cheapestHint, _cheapestLabel, _cheapestValue, 218),
			(_sameRollHint, _sameRollLabel, _sameRollValue, 240)
		};
		for (int j = 0; j < array.Length; j++)
		{
			var (button2, label2, label3, y) = array[j];
			button2.SetSize(num4 - 20, 22);
			button2.SetPosition(10, y);
			label2.SetSize(num4 - 28, 22);
			label2.SetPosition(4, 0);
			label3.SetSize(num4 - 28, 22);
			label3.SetPosition(4, 0);
		}
		_summaryRule.SetSize(num4 - 20, 1);
		_summaryRule.SetPosition(10, 276);
		_totalLabel.SetSize(100, 22);
		_totalLabel.SetPosition(14, 284);
		_totalValue.SetSize(num4 - 100 - 14, 22);
		_totalValue.SetPosition(100, 284);
		_taxLineLabel.SetSize(100, 22);
		_taxLineLabel.SetPosition(14, 306);
		_taxLineValue.SetSize(num4 - 100 - 14, 22);
		_taxLineValue.SetPosition(100, 306);
		_netRule.SetSize(num4 - 20, 1);
		_netRule.SetPosition(10, 334);
		_netLabel.SetSize(90, 28);
		_netLabel.SetPosition(14, 340);
		_netValue.SetSize(100, 28);
		_netValue.SetPosition(num4 - 14 - 16 - 4 - 100, 340);
		_netCoin.SetPosition(num4 - 14 - 16, 346);
		_sellStatus.SetSize(num4 - 28, 34);
		_sellStatus.SetPosition(14, 378);
		_sellButton.SetPosition((num4 - 180) / 2, num3 - 34 - 14);
	}

	private void Rebuild()
	{
		foreach (Base row in _rows)
		{
			row.Parent?.RemoveChild(row, dispose: true);
		}
		_rows.Clear();
		HideTooltip();
		UpdateBalance();
		foreach (var (button, mode) in _modeTabs)
		{
			StyleModeTab(button, mode == _mode);
			Button button2 = button;
			button2.Text = mode switch
			{
				Mode.Sell => Strings.Market.TabSell, 
				Mode.Mine => Strings.Market.TabMine.ToString(_mine.Count((MarketListingInfo listing) => listing.Quantity > 0)), 
				_ => Strings.Market.TabBuy, 
			};
		}
		foreach (var categoryTab in _categoryTabs)
		{
			Button item = categoryTab.Tab;
			Label item2 = categoryTab.Count;
			MarketCategory item3 = categoryTab.Category;
			bool flag = item3 == _category;
			StyleCategoryTab(item, flag);
			item2.Text = ((flag && _mode == Mode.Buy) ? FormatAmount(_totalCount) : string.Empty);
		}
		int num = _mine.Count((MarketListingInfo listing) => listing.Quantity > 0);
		long num2 = _mine.Sum((MarketListingInfo listing) => listing.PendingProceeds);
		_mineCount.Text = Strings.Market.ListingsCount.ToString(num, _maxListings);
		_minePending.Text = ((num2 > 0) ? Strings.Market.PendingTotal.ToString(Money(num2)) : ((string)Strings.Market.NothingPending));
		_minePending.SetTextColor((num2 > 0) ? Gold : Dim, ComponentState.Normal);
		bool flag2 = _mode == Mode.Buy;
		bool flag3 = _mode == Mode.Sell;
		bool flag4 = _mode == Mode.Mine;
		_searchBack.IsHidden = !flag2;
		_sortButton.IsHidden = !flag2;
		_countLabel.IsHidden = !flag2;
		_mineSummary.IsHidden = !flag4;
		_collectButton.IsHidden = !flag4;
		_tableCard.IsHidden = flag3;
		_status.IsHidden = flag3;
		_previous.IsHidden = !flag2;
		_pageLabel.IsHidden = !flag2;
		_next.IsHidden = !flag2;
		_gridCard.IsHidden = !flag3;
		_formCard.IsHidden = !flag3;
		switch (_mode)
		{
		case Mode.Buy:
			BuildBuyRows();
			break;
		case Mode.Mine:
			BuildMineRows();
			break;
		default:
			BuildSellGrid();
			UpdateSellPanel();
			break;
		}
	}

	private void BuildBuyRows()
	{
		_sortButton.Text = Capitalize(SortName(_sort));
		_countLabel.Text = Strings.Market.Count.ToString(FormatAmount(_totalCount));
		_pageLabel.Text = Strings.Market.Page.ToString(_page + 1, _pageCount);
		_previous.IsDisabled = _page <= 0;
		_next.IsDisabled = _page >= _pageCount - 1;
		if (_listings.Length == 0)
		{
			AddEmpty((_knownCurrencyId == Guid.Empty) ? Strings.Market.NoCurrencyHint : Strings.Market.Empty);
			return;
		}
		int width = _list.Width - _list.VerticalScrollBar.Width;
		for (int i = 0; i < _listings.Length; i++)
		{
			_rows.Add(BuildRow(_listings[i], i, width, mine: false, i == _listings.Length - 1));
		}
		string text = Strings.Market.PageOf.ToString(_page + 1, _pageCount);
		string text2 = _status.Text;
		if (text2 == null || text2.Length <= 0 || _status.Text == _pageStatusText)
		{
			_status.Text = text;
			_status.SetTextColor(Muted, ComponentState.Normal);
		}
		_pageStatusText = text;
	}

	private void BuildMineRows()
	{
		int num = _mine.Count((MarketListingInfo listing) => listing.Quantity > 0);
		long num2 = _mine.Sum((MarketListingInfo listing) => listing.PendingProceeds);
		bool flag = num2 > 0 || _mine.Any((MarketListingInfo listing) => listing.State == MarketListingState.Expired);
		_mineSummary.Text = ((num2 > 0) ? (Strings.Market.ListingsCount.ToString(num, _maxListings) + "   ·   " + Strings.Market.PendingTotal.ToString(Money(num2))) : Strings.Market.ListingsCount.ToString(num, _maxListings));
		_collectButton.IsDisabled = !flag;
		if (_status.Text == _pageStatusText)
		{
			_status.Text = string.Empty;
		}
		if (_mine.Length == 0)
		{
			AddEmpty(Strings.Market.MineEmpty);
			return;
		}
		int width = _list.Width - _list.VerticalScrollBar.Width;
		for (int num3 = 0; num3 < _mine.Length; num3++)
		{
			_rows.Add(BuildRow(_mine[num3], num3, width, mine: true, num3 == _mine.Length - 1));
		}
	}

	private void AddEmpty(string text)
	{
		Label label = MakeLabel(_list, "MarketEmpty", 11, Dim, _font);
		label.WrappingBehavior = WrappingBehavior.Wrapped;
		label.TextAlign = Pos.Left | Pos.Top;
		label.Text = text;
		label.SetSize(_list.Width - 32, 60);
		label.SetPosition(16, 12);
		_rows.Add(label);
	}

	private Base BuildRow(MarketListingInfo listing, int index, int width, bool mine, bool last)
	{
		ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(listing.ItemId);
		ImagePanel row = new ImagePanel(_list, $"Listing_{listing.Id}")
		{
			TextureNinePatchMargin = new Margin(4)
		};
		row.SetSize(width, 46);
		row.SetPosition(0, index * 46);
		IGameTexture lit = Globals.ContentManager.GetTexture(TextureType.Gui, "hdv_ligne_survol.png");
		row.HoverEnter += delegate
		{
			row.Texture = lit;
		};
		row.HoverLeave += delegate
		{
			row.Texture = null;
		};
		ImagePanel imagePanel = ItemSlot(row, itemDescriptor, mine ? Math.Max(listing.Quantity, 0) : listing.Quantity, listing.Properties);
		imagePanel.SetPosition(6, 2);
		imagePanel.HoverEnter += delegate
		{
			row.Texture = lit;
		};
		imagePanel.HoverLeave += delegate
		{
			row.Texture = null;
		};
		int num = width - 84 - 12 - 10;
		int num2 = 284;
		int width2 = num - 20 - num2;
		Label label = MakeLabel(row, "Name", 11, RarityColor(itemDescriptor, listing.Properties), _font);
		label.Text = itemDescriptor?.Name ?? "?";
		label.SetSize(160, 22);
		label.SetPosition(56, 3);
		Label label2 = MakeLabel(row, "Detail", 10, Muted, _font);
		label2.Text = (mine ? StateText(listing) : Strings.Market.SoldBy.ToString(listing.SellerName));
		label2.SetSize(160, 18);
		label2.SetPosition(56, 24);
		bool flag = !mine && !listing.IsMine && listing.UnitPrice > OwnedCurrency(_knownCurrencyId);
		bool flag2 = (mine && listing.PendingProceeds > 0) || listing.Quantity > 1;
		double? num3 = JetDObjet.Qualite(itemDescriptor, listing.Properties);
		Label label3 = MakeLabel(row, "Jet", 12, num3.HasValue ? RollColor(num3.Value) : Dim, _strongFont);
		label3.Text = (num3.HasValue ? Strings.Market.RollPercent.ToString(RollPercent(num3.Value)) : "—");
		if (flag2)
		{
			label3.SetSize(58, 24);
			label3.SetPosition(222, 3);
		}
		else if (num3.HasValue)
		{
			label3.SetSize(58, 22);
			label3.SetPosition(222, 3);
			Label label4 = MakeLabel(row, "JetNote", 10, RollColor(num3.Value), _font);
			Label label5 = label4;
			label5.Text = RollTier(num3.Value) switch
			{
				3 => Strings.Market.RollPerfect, 
				2 => Strings.Market.RollGood, 
				1 => Strings.Market.RollAverage, 
				_ => Strings.Market.RollWeak, 
			};
			label4.SetSize(58, 18);
			label4.SetPosition(222, 24);
		}
		else
		{
			label3.SetSize(58, 46);
			label3.SetPosition(222, 0);
		}
		Label label6 = MakeLabel(row, "Price", 13, flag ? Dim : Gold, _strongFont);
		label6.Text = FormatAmount(listing.UnitPrice);
		label6.TextAlign = Pos.Right | Pos.CenterV;
		label6.SetSize(width2, flag2 ? 24 : 46);
		label6.SetPosition(num2, flag2 ? 3 : 0);
		ImagePanel imagePanel2 = MakeImage(row, "Coin", null, 16, 16);
		imagePanel2.Texture = CurrencyIcon();
		imagePanel2.IsHidden = imagePanel2.Texture == null;
		imagePanel2.SetPosition(num - 16, flag2 ? 7 : 15);
		if (flag2)
		{
			Label label7 = MakeLabel(row, "PriceDetail", 10, Muted, _font);
			label7.TextAlign = Pos.Right | Pos.CenterV;
			label7.SetSize(num - 222, 18);
			label7.SetPosition(222, 25);
			if (mine && listing.PendingProceeds > 0)
			{
				label7.Text = Strings.Market.Pending.ToString(Money(listing.PendingProceeds));
				label7.SetTextColor(Green, ComponentState.Normal);
			}
			else
			{
				label7.Text = Strings.Market.UnitPrice.ToString(FormatAmount(listing.UnitPrice * listing.Quantity), listing.Quantity);
			}
		}
		Button button = MakeButton(row, "Action", 84, 28);
		button.SetPosition(width - 84 - 12, 9);
		button.HoverEnter += delegate
		{
			row.Texture = lit;
		};
		button.HoverLeave += delegate
		{
			row.Texture = null;
		};
		if (mine)
		{
			button.Text = Strings.Market.Withdraw;
			button.IsHidden = listing.Quantity <= 0;
			button.Clicked += delegate
			{
				PromptWithdraw(listing);
			};
		}
		else if (listing.IsMine)
		{
			button.Text = Strings.Market.YourListing;
			button.IsDisabled = true;
			button.MouseInputEnabled = false;
		}
		else
		{
			button.Text = Strings.Market.Buy;
			button.Clicked += delegate
			{
				PromptBuy(listing);
			};
		}
		if (!last)
		{
			ImagePanel imagePanel3 = MakeImage(row, "Rule", "hdv_filet.png", width - 56 - 8, 1);
			imagePanel3.SetPosition(56, 45);
		}
		return row;
	}

	private ImagePanel ItemSlot(Base parent, ItemDescriptor? descriptor, int quantity, ItemProperties? properties)
	{
		ImagePanel imagePanel = new ImagePanel(parent, "Slot")
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_slot.png")
		};
		imagePanel.SetSize(42, 42);
		if (descriptor == null)
		{
			return imagePanel;
		}
		ImagePanel imagePanel2 = new ImagePanel(imagePanel, "Icon")
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Item, descriptor.Icon),
			MouseInputEnabled = false
		};
		imagePanel2.SetSize(32, 32);
		imagePanel2.SetPosition(5, 5);
		if (quantity > 1)
		{
			Label label = MakeLabel(imagePanel, "Count", 9, White, _strongFont);
			label.Text = FormatAmount(quantity);
			label.TextAlign = Pos.Right | Pos.CenterV;
			label.SetSize(38, 18);
			label.SetPosition(0, 24);
		}
		imagePanel.HoverEnter += delegate
		{
			Interface.GameUi.ItemDescriptionWindow?.Show(descriptor, Math.Max(1, quantity), properties);
		};
		imagePanel.HoverLeave += delegate
		{
			HideTooltip();
		};
		return imagePanel;
	}

	private void BuildSellGrid()
	{
		IItem[] array = Globals.Me?.Inventory;
		if (array == null)
		{
			return;
		}
		if (_selectedSlot >= array.Length || (_selectedSlot >= 0 && array[_selectedSlot]?.ItemId != _selectedItemId))
		{
			_selectedSlot = -1;
			_selectedItemId = Guid.Empty;
		}
		int num = array.Count((IItem item2) => item2 != null && item2.ItemId != Guid.Empty);
		_gridCount.Text = Strings.Market.Page.ToString(num, array.Length);
		for (int num2 = 0; num2 < array.Length; num2++)
		{
			IItem item = array[num2];
			ItemDescriptor descriptor = ((item == null || item.ItemId == Guid.Empty) ? null : DatabaseObject<ItemDescriptor>.Get(item.ItemId));
			int slotIndex = num2;
			bool flag = num2 == _selectedSlot;
			ImagePanel slot = ((descriptor == null) ? new ImagePanel(_grid, "Slot")
			{
				Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_slot.png")
			} : ItemSlot(_grid, descriptor, item.Quantity, item.ItemProperties));
			slot.SetSize(42, 42);
			slot.SetPosition(num2 % 6 * 48, num2 / 6 * 48);
			_rows.Add(slot);
			if (descriptor == null)
			{
				continue;
			}
			string block = BlockOf(num2, item, descriptor);
			if (flag)
			{
				slot.Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_slot_selected.png");
			}
			else if (block == null)
			{
				slot.HoverEnter += delegate
				{
					slot.Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_slot_hover.png");
				};
				slot.HoverLeave += delegate
				{
					slot.Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_slot.png");
				};
			}
			if (block != null)
			{
				ImagePanel imagePanel = new ImagePanel(slot, "Locked")
				{
					Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_slot_locked.png"),
					MouseInputEnabled = false
				};
				imagePanel.SetSize(42, 42);
				imagePanel.SetPosition(0, 0);
			}
			slot.Clicked += delegate
			{
				if (block != null)
				{
					SetStatus(block, Red);
				}
				else
				{
					SelectSlot(slotIndex, item, descriptor);
				}
			};
		}
	}

	private string? BlockOf(int index, IItem item, ItemDescriptor descriptor)
	{
		Player? me = Globals.Me;
		if (me != null && me.MyEquipment.Contains(index))
		{
			return Strings.Market.LockedEquipped;
		}
		bool flag = !descriptor.CanTrade;
		bool flag2 = flag;
		if (!flag2)
		{
			ItemType itemType = descriptor.ItemType;
			bool flag3 = ((itemType == ItemType.Currency || itemType == ItemType.Bag) ? true : false);
			flag2 = flag3;
		}
		if (!flag2 && !(descriptor.Id == _knownCurrencyId))
		{
			Guid? bagId = item.BagId;
			if (bagId.HasValue)
			{
				Guid valueOrDefault = bagId.GetValueOrDefault();
				if (valueOrDefault != Guid.Empty)
				{
					goto IL_0088;
				}
			}
			return null;
		}
		goto IL_0088;
		IL_0088:
		return Strings.Market.LockedNotTradable;
	}

	private void SelectSlot(int index, IItem item, ItemDescriptor descriptor)
	{
		_selectedSlot = index;
		_selectedItemId = item.ItemId;
		_quantity.SetValue((!descriptor.Stackable) ? 1 : item.Quantity, skipEvents: true);
		_price.SetValue(Math.Max(1, descriptor.Price), skipEvents: true);
		SetStatus(string.Empty, Muted);
		RequestPriceHints();
		_needsRebuild = true;
	}

	private bool TryGetSelected(out IItem item, out ItemDescriptor descriptor)
	{
		item = null;
		descriptor = null;
		IItem[] array = Globals.Me?.Inventory;
		if (array == null || _selectedSlot < 0 || _selectedSlot >= array.Length)
		{
			return false;
		}
		item = array[_selectedSlot];
		if (item != null && !(item.ItemId != _selectedItemId))
		{
			ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(item.ItemId);
			if (itemDescriptor != null)
			{
				descriptor = itemDescriptor;
				return true;
			}
		}
		return false;
	}

	private void UpdateSellPanel()
	{
		IItem item;
		ItemDescriptor descriptor;
		bool flag = TryGetSelected(out item, out descriptor);
		_sellHint.IsHidden = flag;
		Base[] array = new Base[20]
		{
			_sellSlot, _sellName, _sellOwned, _quantityLabel, _quantityBack, _quantityMax, _priceLabel, _priceBack, _priceCoin, _cheapestHint,
			_sameRollHint, _summaryRule, _totalLabel, _totalValue, _taxLineLabel, _taxLineValue, _netRule, _netLabel, _netValue, _netCoin
		};
		foreach (Base obj in array)
		{
			obj.IsHidden = !flag;
		}
		_sellButton.IsDisabled = !flag;
		if (flag)
		{
			_sellIcon.Texture = Globals.ContentManager.GetTexture(TextureType.Item, descriptor.Icon);
			_sellName.Text = descriptor.Name;
			_sellName.SetTextColor(RarityColor(descriptor, item.ItemProperties), ComponentState.Normal);
			_sellOwned.Text = Strings.Market.Owned.ToString(FormatAmount(item.Quantity));
			_quantity.IsDisabled = !descriptor.Stackable;
			_quantityMax.Text = (descriptor.Stackable ? ("/ " + FormatAmount(item.Quantity)) : string.Empty);
			_priceCoin.Texture = CurrencyIcon();
			_priceCoin.IsHidden = _priceCoin.Texture == null;
			_netCoin.Texture = _priceCoin.Texture;
			_netCoin.IsHidden = _priceCoin.IsHidden;
			_taxLineLabel.Text = Strings.Market.TaxLine.ToString(_taxPercent);
			_sameRollHint.IsHidden = !JetDObjet.ADesJets(descriptor);
			UpdatePriceHints();
			UpdateSellSummary();
		}
	}

	private void UpdatePriceHints()
	{
		ShowPriceHint(_cheapestHint, _cheapestLabel, _cheapestValue, _priceHints?.Cheapest);
		ShowPriceHint(_sameRollHint, _sameRollLabel, _sameRollValue, _priceHints?.CheapestSameRollOrBetter);
	}

	private void ShowPriceHint(Button hit, Label label, Label value, long? amount)
	{
		bool flag = _hoveredHint == hit && amount > 0;
		label.SetTextColor(flag ? OrangeLight : Muted, ComponentState.Normal);
		if (amount > 0)
		{
			value.Font = _strongFont;
			value.FontSize = 12;
			value.Text = FormatAmount(amount.Value);
			value.SetTextColor(flag ? OrangeLight : White, ComponentState.Normal);
		}
		else
		{
			value.Font = _font;
			value.FontSize = 10;
			value.Text = (amount.HasValue ? Strings.Market.HintNone.ToString() : "...");
			value.SetTextColor(Dim, ComponentState.Normal);
		}
	}

	private void UsePriceHint(long amount)
	{
		if (amount > 0 && TryGetSelected(out IItem _, out ItemDescriptor _))
		{
			_price.SetValue(Math.Max(1L, amount - 1), skipEvents: true);
			UpdateSellSummary();
		}
	}

	private void UpdateSellSummary()
	{
		if (TryGetSelected(out IItem item, out ItemDescriptor descriptor))
		{
			long num = (descriptor.Stackable ? ((long)Math.Clamp(Math.Floor(_quantity.Value), 0.0, item.Quantity)) : 1);
			long num2 = (long)Math.Max(0.0, Math.Floor(_price.Value));
			long num3 = num * num2;
			long num4 = num3 * _taxPercent / 100;
			_totalValue.Text = FormatAmount(num3);
			_taxLineValue.Text = ((num4 > 0) ? ("- " + FormatAmount(num4)) : FormatAmount(0L));
			_netValue.Text = FormatAmount(num3 - num4);
			_sellButton.IsDisabled = num < 1 || num2 < 1;
		}
	}

	private void SubmitSell()
	{
		if (TryGetSelected(out IItem item, out ItemDescriptor descriptor))
		{
			int num = ((!descriptor.Stackable) ? 1 : ((int)Math.Clamp(Math.Floor(_quantity.Value), 0.0, item.Quantity)));
			long num2 = (long)Math.Floor(_price.Value);
			if (num < 1)
			{
				SetStatus(Strings.Market.ResultInvalidQuantity, Red);
				return;
			}
			if (num2 < 1)
			{
				SetStatus(Strings.Market.ResultInvalidPrice, Red);
				return;
			}
			PacketSender.SendMarketSell(_selectedSlot, num, num2);
			SetStatus(Strings.Market.Selling, Muted);
		}
	}

	private void ShowSelectedTooltip()
	{
		if (TryGetSelected(out IItem item, out ItemDescriptor descriptor))
		{
			Interface.GameUi.ItemDescriptionWindow?.Show(descriptor, item.Quantity, item.ItemProperties);
		}
	}

	private static void HideTooltip()
	{
		Interface.GameUi?.ItemDescriptionWindow?.Hide();
	}

	private void PromptBuy(MarketListingInfo listing)
	{
		string heading = DatabaseObject<ItemDescriptor>.Get(listing.ItemId)?.Name ?? "?";
		if (listing.Quantity > 1 && DatabaseObject<ItemDescriptor>.TryGet(listing.ItemId, out var databaseObject))
		{
			ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(_knownCurrencyId);
			int num = (int)((listing.UnitPrice > 0) ? Math.Min(2147483647L, OwnedCurrency(_knownCurrencyId) / listing.UnitPrice) : listing.Quantity);
			int num2 = RoomInBag(databaseObject);
			int num3 = Math.Min(listing.Quantity, Math.Min(num, num2));
			string limitHint = null;
			if (num < listing.Quantity && num <= num2)
			{
				limitHint = Strings.Quantity.LimitedBy.ToString(itemDescriptor?.Name ?? string.Empty);
			}
			else if (num2 < listing.Quantity && num2 < num)
			{
				limitHint = Strings.Quantity.LimitedByBag;
			}
			if (num3 < 1)
			{
				PacketSender.SendMarketBuy(listing.Id, 1, listing.UnitPrice);
				return;
			}
			new QuantityWindow(Strings.Market.BuyTitle, databaseObject, Strings.Market.Buy, 1, num3, delegate(int quantity)
			{
				PacketSender.SendMarketBuy(listing.Id, quantity, listing.UnitPrice);
			}, (itemDescriptor != null) ? new QuantityPricing(itemDescriptor, listing.UnitPrice, Earning: false) : null, limitHint, () => !base.IsHidden);
		}
		else
		{
			ConfirmCardWindow.Open(new ConfirmCardRequest
			{
				Title = Strings.ConfirmCard.BuyTitle,
				Icon = "saiyan_carte_icone_echange.png",
				Heading = heading,
				Prompt = Strings.ConfirmCard.BuyPrompt,
				Chip = Money(listing.UnitPrice),
				ConfirmLabel = Strings.ConfirmCard.Buy,
				Emblem = ListingIcon(listing)
			}, delegate
			{
				PacketSender.SendMarketBuy(listing.Id, 1, listing.UnitPrice);
			});
		}
	}

	private static int RoomInBag(ItemDescriptor descriptor)
	{
		Player me = Globals.Me;
		if (me == null)
		{
			return 0;
		}
		int num = ((!descriptor.IsStackable) ? 1 : Math.Max(1, descriptor.MaxInventoryStack));
		int num2 = me.Inventory.Count((IItem slot) => slot.ItemId == default(Guid));
		int num3 = me.Inventory.Count((IItem slot) => slot.ItemId == descriptor.Id);
		long value = (long)num2 * (long)num + (long)num3 * (long)num - me.GetQuantityOfItemInInventory(descriptor.Id);
		return (int)Math.Clamp(value, 0L, 2147483647L);
	}

	private static IGameTexture? ListingIcon(MarketListingInfo listing)
	{
		ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(listing.ItemId);
		if (itemDescriptor == null)
		{
			return null;
		}
		return Globals.ContentManager.GetTexture(TextureType.Item, itemDescriptor.Icon);
	}

	private void PromptWithdraw(MarketListingInfo listing)
	{
		string heading = DatabaseObject<ItemDescriptor>.Get(listing.ItemId)?.Name ?? "?";
		ConfirmCardRequest confirmCardRequest = new ConfirmCardRequest()
		{
			Title = Strings.ConfirmCard.WithdrawTitle,
			Icon = "saiyan_carte_icone_echange.png",
			Heading = heading,
			Prompt = Strings.ConfirmCard.WithdrawPrompt,
			Chip = Strings.ConfirmCard.Quantity.ToString(listing.Quantity),
			ConfirmLabel = Strings.ConfirmCard.Withdraw,
			Emblem = ListingIcon(listing),
		};
		ConfirmCardWindow.Open(confirmCardRequest, delegate
		{
			PacketSender.SendMarketCancel(listing.Id);
		});
	}

	private void SetStatus(string text, Color color)
	{
		_status.Text = text;
		_status.SetTextColor(color, ComponentState.Normal);
		_sellStatus.Text = text;
		_sellStatus.SetTextColor(color, ComponentState.Normal);
	}

	private void UpdateBalance()
	{
		Guid knownCurrencyId = _knownCurrencyId;
		ItemDescriptor itemDescriptor = ((knownCurrencyId == Guid.Empty) ? null : DatabaseObject<ItemDescriptor>.Get(knownCurrencyId));
		_coin.Texture = _coinAnimation ?? CurrencyIcon();
		_coin.IsHidden = _coin.Texture == null;
		_coinFrame = -1;
		_currencyName.Text = itemDescriptor?.Name ?? string.Empty;
		string text = ((itemDescriptor == null) ? string.Empty : FormatAmount(OwnedCurrency(knownCurrencyId)));
		if (text != _balanceText)
		{
			_balanceText = text;
			_balance.Text = text;
		}
	}

	private static IGameTexture? CurrencyIcon()
	{
		if (_knownCurrencyId != Guid.Empty)
		{
			ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(_knownCurrencyId);
			if (itemDescriptor != null && !string.IsNullOrEmpty(itemDescriptor.Icon))
			{
				return Globals.ContentManager.GetTexture(TextureType.Item, itemDescriptor.Icon);
			}
		}
		return Globals.ContentManager.GetTexture(TextureType.Gui, "hud_zeni.png");
	}

	private static long OwnedCurrency(Guid currencyId)
	{
		return Globals.Me?.Zeni ?? 0;
	}

	private static int InventorySignature()
	{
		HashCode hashCode = default(HashCode);
		Player me = Globals.Me;
		if (me == null)
		{
			return 0;
		}
		IItem[] inventory = me.Inventory;
		foreach (IItem item in inventory)
		{
			hashCode.Add(item?.ItemId ?? Guid.Empty);
			hashCode.Add(item?.Quantity ?? 0);
		}
		int[] myEquipment = me.MyEquipment;
		foreach (int value in myEquipment)
		{
			hashCode.Add(value);
		}
		hashCode.Add(me.Zeni);
		return hashCode.ToHashCode();
	}

	private static Color RarityColor(ItemDescriptor? descriptor, ItemProperties? properties)
	{
		if (descriptor != null && CustomColors.Items.Rarities.TryGetValue(JetDObjet.Rarete(descriptor, properties), out var value) && value != null)
		{
			return value;
		}
		return Entry;
	}

	private static string StateText(MarketListingInfo listing)
	{
		return listing.State switch
		{
			MarketListingState.Active => Strings.Market.StateActive.ToString(FormatAmount(listing.Quantity), TimeLeft(listing.ExpiresAt)), 
			MarketListingState.Expired => Strings.Market.StateExpired.ToString(FormatAmount(listing.Quantity)), 
			_ => Strings.Market.StateSold, 
		};
	}

	private static string TimeLeft(long expiresAt)
	{
		TimeSpan timeSpan = DateTimeOffset.FromUnixTimeMilliseconds(expiresAt) - DateTimeOffset.UtcNow;
		if (timeSpan <= TimeSpan.Zero)
		{
			return Strings.Market.TimeMinutes.ToString(0);
		}
		if (timeSpan.TotalDays >= 1.0)
		{
			return Strings.Market.TimeDays.ToString((int)timeSpan.TotalDays, timeSpan.Hours);
		}
		if (!(timeSpan.TotalHours >= 1.0))
		{
			return Strings.Market.TimeMinutes.ToString(Math.Max(1, timeSpan.Minutes));
		}
		return Strings.Market.TimeHours.ToString((int)timeSpan.TotalHours, timeSpan.Minutes);
	}

	private static string CategoryName(MarketCategory category)
	{
		return category switch
		{
			MarketCategory.Equipment => Strings.Market.CategoryEquipment, 
			MarketCategory.Consumables => Strings.Market.CategoryConsumables, 
			MarketCategory.Resources => Strings.Market.CategoryResources, 
			_ => Strings.Market.CategoryAll, 
		};
	}

	private static string SortName(MarketSort sort)
	{
		return sort switch
		{
			MarketSort.PriceDescending => Strings.Market.SortPriceDescending, 
			MarketSort.Newest => Strings.Market.SortNewest, 
			MarketSort.Name => Strings.Market.SortName, 
			MarketSort.BestRoll => Strings.Market.SortBestRoll, 
			_ => Strings.Market.SortPriceAscending, 
		};
	}

	public static int RollTier(double quality)
	{
		if (!JetDObjet.EstParfait(quality))
		{
			if (!(quality >= 0.7))
			{
				return (quality >= 0.35) ? 1 : 0;
			}
			return 2;
		}
		return 3;
	}

	public static Color RollColor(double quality)
	{
		return RollTier(quality) switch
		{
			3 => Gold, 
			2 => RollGreen, 
			1 => Entry, 
			_ => Dim, 
		};
	}

	public static int RollPercent(double quality)
	{
		if (!JetDObjet.EstParfait(quality))
		{
			return Math.Clamp((int)Math.Round(quality * 100.0), 0, 99);
		}
		return 100;
	}

	private static string Capitalize(string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			return char.ToUpperInvariant(text[0]) + text.Substring(1, text.Length - 1);
		}
		return text;
	}

	public static string FormatAmount(long value)
	{
		return value.ToString("#,0", CultureInfo.InvariantCulture).Replace(',', ' ');
	}

	private static string CurrencyName(Guid currencyId)
	{
		if (currencyId != Guid.Empty)
		{
			ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(currencyId);
			if (itemDescriptor != null)
			{
				return itemDescriptor.Name;
			}
		}
		return DatabaseObject<ItemDescriptor>.Lookup.Values.OfType<ItemDescriptor>().FirstOrDefault((ItemDescriptor item) => item.ItemType == ItemType.Currency)?.Name ?? "Zeni";
	}

	public static string Money(long amount)
	{
		return FormatAmount(amount) + " " + CurrencyName(_knownCurrencyId);
	}

	public static (string Text, bool Success, bool ToChat) Describe(MarketResultPacket packet, bool windowOpen)
	{
		if (packet.Result == MarketResult.Collected)
		{
			RememberCurrency(packet.ItemId);
		}
		string text = ((packet.ItemId == Guid.Empty) ? string.Empty : (DatabaseObject<ItemDescriptor>.Get(packet.ItemId)?.Name ?? "?"));
		string text2 = FormatAmount(packet.Quantity);
		string item = packet.Result switch
		{
			MarketResult.Listed => Strings.Market.ResultListed.ToString(text2, text, Money(packet.Amount)), 
			MarketResult.Bought => Strings.Market.ResultBought.ToString(text2, text, Money(packet.Amount)), 
			MarketResult.Cancelled => Strings.Market.ResultCancelled.ToString(text2, text), 
			MarketResult.Collected => Strings.Market.ResultCollected.ToString(Money(packet.Amount)), 
			MarketResult.ItemsReturned => Strings.Market.ResultReturned.ToString(text2), 
			MarketResult.SoldNotice => Strings.Market.ResultSold.ToString(text2, text, Money(packet.Amount)), 
			MarketResult.ProceedsWaiting => Strings.Market.ResultProceedsWaiting, 
			MarketResult.NothingToCollect => Strings.Market.ResultNothingToCollect, 
			MarketResult.Disabled => Strings.Market.ResultDisabled, 
			MarketResult.NoCurrency => Strings.Market.NoCurrencyHint, 
			MarketResult.NotEnoughCurrency => Strings.Market.ResultNotEnoughCurrency.ToString(CurrencyName(_knownCurrencyId)), 
			MarketResult.InventoryFull => Strings.Market.ResultInventoryFull, 
			MarketResult.InvalidItem => Strings.Market.ResultInvalidItem, 
			MarketResult.NotTradable => Strings.Market.LockedNotTradable, 
			MarketResult.Equipped => Strings.Market.LockedEquipped, 
			MarketResult.TooManyListings => Strings.Market.ResultTooManyListings, 
			MarketResult.InvalidPrice => Strings.Market.ResultInvalidPrice, 
			MarketResult.InvalidQuantity => Strings.Market.ResultInvalidQuantity, 
			MarketResult.ListingUnavailable => Strings.Market.ResultListingUnavailable, 
			MarketResult.OwnListing => Strings.Market.ResultOwnListing, 
			MarketResult.LinkedAccount => Strings.Market.ResultLinkedAccount, 
			MarketResult.AccountTooYoung => Strings.Market.ResultAccountTooYoung, 
			_ => Strings.Market.ResultError, 
		};
		MarketResult result = packet.Result;
		bool flag = result <= MarketResult.SoldNotice;
		bool flag2 = flag;
		bool flag3 = flag2 || packet.Result == MarketResult.ProceedsWaiting;
		bool flag4 = flag3;
		bool flag5 = flag4;
		if (!flag5)
		{
			bool flag6 = !windowOpen;
			bool flag7 = flag6;
			if (flag7)
			{
				result = packet.Result;
				flag = result - 8 <= MarketResult.Bought;
				flag7 = !flag;
			}
			flag5 = flag7;
		}
		bool item2 = flag5;
		return (Text: item, Success: flag2, ToChat: item2);
	}
}
