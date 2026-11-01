using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.DragDrop;
using Intersect.Client.Framework.Items;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.Ornaments;
using Intersect.Framework.Core.GameObjects.Titles;
using Intersect.Models;

namespace Intersect.Client.Interface.Game.Inventory;

public class InventoryWindow : WindowControl
{
	private enum BagCategory
	{
		All,
		Equipment,
		Consumables,
		Techniques,
		Resources,
		Quest,
		Bags
	}

	private enum BagSort
	{
		None,
		Name,
		Type,
		Quantity,
		Value
	}

	private readonly record struct PickerEntry(Guid Id, string Name, bool Unlocked, bool Worn, string? Note, string? Tooltip);

	private const int CellSize = 38;

	private const int CellPitch = 42;

	private const int GridColumns = 7;

	private const int GridRows = 8;

	private const int GridCardY = 144;

	private const int GridCardHeight = 382;

	private const int FooterY = 534;

	private const int SearchWidth = 128;

	private const int FilterWidth = 176;

	private const int SortWidth = 128;

	private const int ZeniIconSize = 32;

	private const int ZeniFrames = 4;

	private const long ZeniRestMs = 1600L;

	private const long ZeniFrameMs = 90L;

	private const int CapacityX = 182;

	private const int CapacityBarWidth = 120;

	private readonly List<(Button Button, ImagePanel Glyph, BagCategory Category)> _categoryButtons = new List<(Button Button, ImagePanel Glyph, BagCategory Category)>();

	private ImagePanel _searchBack;

	private TextBox _search;

	private Label _searchHint;

	private Button _filterButton;

	private Button _sortButton;

	private Label _visibleCount;

	private ImagePanel _gridCard;

	private ScrollControl _grid;

	private ContextMenu _menu;

	private ImagePanel _footer;

	private ImagePanel _zeniIcon;

	private Label _zeniAmount;

	private ImagePanel _capacityIcon;

	private Label _capacityLabel;

	private ImagePanel _capacityTrack;

	private ImagePanel _capacityFill;

	private BagCategory _category;

	private int _slotFilter = -1;

	private BagSort _sort;

	private string _query = string.Empty;

	private bool _bagDirty = true;

	private string? _bagSignature;

	private int[] _cellLook = Array.Empty<int>();

	private readonly HashSet<int> _highlightedSlots = new HashSet<int>();

	private long _shownZeni = -1L;

	private IGameTexture? _zeniAnimation;

	private int _zeniFrame = -1;

	private const int WindowWidth = 392;

	private const int WindowHeight = 600;

	private const int HeaderHeight = 56;

	private new const int Margin = 16;

	private const int ContentTop = 72;

	private const int LeftX = 16;

	private const int LeftWidth = 360;

	private const int SidebarX = 16;

	private const int RightX = 64;

	private const int RightWidth = 312;

	private static readonly Color OrangeLight = new Color(255, 255, 172, 82);

	private static readonly Color Gold = new Color(255, 245, 190, 90);

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color Muted = new Color(255, 176, 186, 206);

	private static readonly Color Dim = new Color(255, 150, 160, 182);

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private readonly Canvas _gameCanvas;

	private readonly IFont? _font;

	private readonly IFont? _strongFont;

	private readonly IFont? _numberFont;

	private readonly IFont? _titleFont;

	private readonly Label _title;

	private readonly Button _close;

	private readonly ContextMenu _contextMenu;

	private int _laidOutTitleBarHeight = -1;

	private const int TabWidth = 180;

	private const int TabHeight = 34;

	private const int PreviewZoneHeight = 236;

	private const int PreviewFeetY = 198;

	private const int PreviewScale = 2;

	private const int PickerX = 12;

	private const int PickerWidth = 336;

	private const int PickerTabsY = 244;

	private const int PickerTabHeight = 26;

	private const int PickerListY = 280;

	private const int PickerListHeight = 184;

	private const int PickerRowHeight = 26;

	private const int PickerScrollWidth = 14;

	private static readonly Color Orange = new Color(255, 255, 122, 26);

	private static readonly Color RuleColor = new Color(255, 42, 51, 73);

	private static readonly Color RowText = new Color(255, 196, 204, 220);

	private static readonly Color Locked = new Color(255, 110, 120, 145);

	private Button _equipmentTabButton;

	private Button _customizationTabButton;

	private ImagePanel _customizationCard;

	private ImagePanel _customizationScene;

	private ImagePanel _customizationShadow;

	private ImagePanel[] _customizationLayers = Array.Empty<ImagePanel>();

	private NamePlatePreview _platePreview;

	private Label _customizationHint;

	private Button _titlesTab;

	private Button _ornamentsTab;

	private Label _titlesCount;

	private Label _ornamentsCount;

	private ImagePanel _pickerUnderline;

	private ImagePanel _pickerRule;

	private ScrollControl _pickerList;

	private readonly List<Base> _pickerRows = new List<Base>();

	private bool _customizing;

	private bool _pickingOrnaments;

	private string? _pickerSignature;

	private string? _customizationCharacterKey;

	private Guid? _tryTitle;

	private Guid? _tryOrnament;

	private const int EquipmentCardY = 112;

	private const int EquipmentCardHeight = 472;

	private const int SlotPitch = 108;

	private const int SlotTop = 20;

	private const int FeetY = 270;

	private const int CoreY = 318;

	private const int BallsY = 406;

	private const int StageCenterX = 180;

	private const int PreviewHeight = 240;

	private const int BallSize = 30;

	private const int BallGap = 8;

	private static readonly Dictionary<string, (int Column, int Row)> SlotPlaces = new Dictionary<string, (int Column, int Row)>
	{
		["tete"] = (0, 0),
		["tenue"] = (0, 1),
		["haut"] = (0, 1),
		["pantalon"] = (0, 2),
		["bottes"] = (0, 3),
		["dos"] = (1, 0),
		["gants"] = (1, 1),
		["aura"] = (1, 2),
		["noyau"] = (2, 0)
	};

	private ImagePanel _equipmentCard;

	private ImagePanel _scene;

	private ImagePanel _pedestal;

	private Button _turnLeft;

	private Button _turnRight;

	private readonly List<EquipmentSlotItem> _equipmentSlots = new List<EquipmentSlotItem>();

	private readonly Dictionary<EquipmentSlotItem, (int Column, int Row)> _slotPlacement = new Dictionary<EquipmentSlotItem, (int Column, int Row)>();

	private ImagePanel[] _previewLayers = Array.Empty<ImagePanel>();

	private Direction _facing = Direction.Down;

	private string? _previewKey;

	private ImagePanel[] _balls = Array.Empty<ImagePanel>();

	private bool[] _ballsOwned = Array.Empty<bool>();

	private bool _bagHighlighted;

	private static (BagCategory Category, string Name, string Glyph)[] Categories => new(BagCategory, string, string)[7]
	{
		(BagCategory.All, Strings.Inventory.CategoryAll.ToString(), "tout"),
		(BagCategory.Equipment, Strings.Personnage.CategoryEquipment.ToString(), "equipement"),
		(BagCategory.Consumables, Strings.Personnage.CategoryConsumables.ToString(), "consommable"),
		(BagCategory.Techniques, Strings.Personnage.CategoryTechniques.ToString(), "technique"),
		(BagCategory.Resources, Strings.Personnage.CategoryResources.ToString(), "ressource"),
		(BagCategory.Quest, Strings.Inventory.CategoryQuest.ToString(), "quete"),
		(BagCategory.Bags, Strings.Personnage.CategoryBags.ToString(), "sac")
	};

	private static (BagSort Sort, string Name)[] Sorts => new(BagSort, string)[5]
	{
		(BagSort.None, Strings.Personnage.SortNone.ToString()),
		(BagSort.Name, Strings.Personnage.SortName.ToString()),
		(BagSort.Type, Strings.Personnage.SortType.ToString()),
		(BagSort.Quantity, Strings.Personnage.SortQuantity.ToString()),
		(BagSort.Value, Strings.Personnage.SortValue.ToString())
	};

	public List<SlotItem> Items { get; set; } = new List<SlotItem>();

	private void BuildBag()
	{
		(BagCategory Category, string Name, string Glyph)[] categories = Categories;
		for (int i = 0; i < categories.Length; i++)
		{
			(BagCategory Category, string Name, string Glyph) tuple = categories[i];
			BagCategory category = tuple.Item1;
			string item = tuple.Item2;
			string item2 = tuple.Item3;
			Button button = new Button(this, $"Category_{category}", disableText: true);
			button.SetSize(36, 36);
			button.SetToolTipText(item);
			ImagePanel imagePanel = MakeImage(button, "Glyph", "inv_glyphe_" + item2 + ".png", 22, 22);
			imagePanel.SetPosition(7, 7);
			button.Clicked += delegate
			{
				SelectCategory(category);
			};
			_categoryButtons.Add((button, imagePanel, category));
		}
		_searchBack = MakeImage(this, "SearchBack", $"saiyan_field_{128}x28.png", 128, 28, mouse: true);
		ImagePanel imagePanel2 = MakeImage(_searchBack, "Loupe", "inv_loupe.png", 14, 14);
		imagePanel2.RenderColor = Muted;
		imagePanel2.SetPosition(9, 7);
		_search = new TextBox(_searchBack, "Search")
		{
			Font = _font,
			FontSize = 11,
			ShouldDrawBackground = false,
			MaximumLength = 32
		};
		_search.SetTextColor(White, ComponentState.Normal);
		_search.SetSize(94, 22);
		_search.SetPosition(28, 3);
		_search.TextChanged += delegate
		{
			_query = Normalize(_search.Text);
			_searchHint.IsHidden = !string.IsNullOrEmpty(_search.Text);
			_bagDirty = true;
		};
		Interface.FocusComponents.Add(_search);
		_searchHint = MakeLabel(_searchBack, "SearchHint", 11, Dim, _font);
		_searchHint.Text = Strings.Inventory.SearchHint.ToString();
		_searchHint.Padding = _search.Padding;
		_searchHint.TextAlign = Pos.Left | Pos.CenterV;
		_searchHint.SetBounds(_search.Bounds);
		_filterButton = MakeDropdown("Filter", 176);
		_filterButton.Clicked += delegate
		{
			OpenFilterMenu();
		};
		_sortButton = MakeDropdown("Sort", 128);
		_sortButton.Clicked += delegate
		{
			OpenSortMenu();
		};
		_visibleCount = MakeLabel(this, "VisibleCount", 10, Muted, _font);
		_visibleCount.TextAlign = Pos.Right | Pos.CenterV;
		_gridCard = MakeCard(this, "GridCard");
		_grid = new ScrollControl(this, "ItemsContainer")
		{
			OverflowX = OverflowBehavior.Hidden,
			OverflowY = OverflowBehavior.Auto
		};
		_grid.VerticalScrollBar.SetBackgroundTemplate(null, "saiyan_scroll_bg.png");
		_grid.VerticalScrollBar.SetScrollBarImage(null, "saiyan_scroll_bar.png", ComponentState.Normal);
		_grid.VerticalScrollBar.SetScrollBarImage(null, "saiyan_scroll_bar_hover.png", ComponentState.Hovered);
		_grid.VerticalScrollBar.SetScrollBarImage(null, "saiyan_scroll_bar_hover.png", ComponentState.Active);
		_menu = new ContextMenu(_gameCanvas, "InventoryFilterMenu")
		{
			IsVisibleInParent = false,
			IconMarginDisabled = true,
			ItemFont = _font,
			ItemFontSize = 11
		};
		for (int num = 0; num < Options.Instance.Player.MaxInventory; num++)
		{
			InventoryItem inventoryItem = new InventoryItem(this, _grid, num, _contextMenu)
			{
				MinimumSize = new Point(38, 38),
			};
			inventoryItem.SetSize(38, 38);
			inventoryItem.Margin = new Margin(0);
			inventoryItem.TextureFilename = "saiyan_slot.png";
			inventoryItem.Icon.SetPosition(3, 3);
			Items.Add(inventoryItem);
		}
		_cellLook = Enumerable.Repeat(-1, Items.Count).ToArray();
		_footer = MakeCard(this, "Footer");
		_zeniIcon = new ImagePanel(this, "ZeniIcon")
		{
			MouseInputEnabled = false
		};
		_zeniIcon.SetSize(32, 32);
		_zeniAnimation = Globals.ContentManager.GetTexture(TextureType.Item, "zeni_anime.png");
		_zeniIcon.Texture = _zeniAnimation ?? Globals.ContentManager.GetTexture(TextureType.Gui, "hud_zeni.png");
		_zeniAmount = MakeLabel(this, "ZeniAmount", 13, Gold, _numberFont);
		_zeniAmount.TextAlign = Pos.Left | Pos.CenterV;
		_capacityIcon = MakeImage(this, "CapacityIcon", "inv_capacite.png", 18, 18);
		_capacityIcon.RenderColor = Muted;
		_capacityLabel = MakeLabel(this, "CapacityLabel", 10, Muted, _font);
		_capacityLabel.TextAlign = Pos.Left | Pos.CenterV;
		_capacityTrack = MakeImage(this, "CapacityTrack", "saiyan_bar_track.png", 120, 8);
		_capacityTrack.TextureNinePatchMargin = new Margin(4);
		_capacityFill = MakeImage(this, "CapacityFill", "saiyan_bar_fill.png", 0, 8);
		_capacityFill.TextureNinePatchMargin = new Margin(4);
		RefreshCategoryButtons();
		RefreshDropdownLabels();
	}

	private Button MakeDropdown(string name, int width)
	{
		Button button = MakeButton(this, name, width, 28, Pos.Left | Pos.CenterV);
		button.Padding = new Padding(8, 0, 20, 0);
		ImagePanel imagePanel = MakeImage(button, "Chevron", "inv_chevron.png", 12, 8);
		imagePanel.RenderColor = Muted;
		imagePanel.SetPosition(width - 18, 10);
		return button;
	}

	private void LayoutBag()
	{
		for (int i = 0; i < _categoryButtons.Count; i++)
		{
			_categoryButtons[i].Button.SetPosition(18, Y(72 + i * 42));
		}
		_searchBack.SetPosition(64, Y(72));
		_filterButton.SetPosition(200, Y(72));
		_sortButton.SetPosition(64, Y(108));
		_visibleCount.SetSize(176, 28);
		_visibleCount.SetPosition(200, Y(108));
		_gridCard.SetSize(312, 382);
		_gridCard.SetPosition(64, Y(144));
		_grid.SetSize(300, 370);
		_grid.SetPosition(70, Y(150));
		_footer.SetSize(312, 50);
		_footer.SetPosition(64, Y(534));
		_zeniIcon.SetPosition(74, Y(543));
		_zeniAmount.SetSize(130, 50);
		_zeniAmount.SetPosition(110, Y(534));
		_capacityIcon.SetPosition(246, Y(542));
		_capacityLabel.SetSize(98, 20);
		_capacityLabel.SetPosition(268, Y(541));
		_capacityTrack.SetPosition(246, Y(567));
		_capacityFill.SetPosition(246, Y(567));
		_bagDirty = true;
	}

	internal bool IsBagArea(Base control)
	{
		if (control != _grid && control != _grid.InnerPanel)
		{
			return control == _gridCard;
		}
		return true;
	}

	private new void CloseMenus()
	{
		_menu?.Close();
	}

	private void UpdateBag()
	{
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		string text = BagSignature(me);
		if (_bagDirty || text != _bagSignature)
		{
			_bagDirty = false;
			_bagSignature = text;
			RelayoutBag(me);
		}
		for (int i = 0; i < Items.Count; i++)
		{
			SlotItem slotItem = Items[i];
			if (!slotItem.IsHidden)
			{
				slotItem.Update();
				int num = (_highlightedSlots.Contains(i) ? 2 : ((slotItem.IsHovered || slotItem.Icon.IsHovered) ? 1 : 0));
				if (num != _cellLook[i])
				{
					_cellLook[i] = num;
					SlotItem slotItem2 = slotItem;
					slotItem2.TextureFilename = num switch
					{
						2 => "saiyan_slot_selected.png", 
						1 => "saiyan_slot_hover.png", 
						_ => "saiyan_slot.png", 
					};
				}
			}
		}
		UpdateFooter(me);
	}

	public void SetHighlightedSlots(IReadOnlyCollection<int>? slots)
	{
		if (slots == null || slots.Count == 0)
		{
			_highlightedSlots.Clear();
		}
		else if (!_highlightedSlots.SetEquals(slots))
		{
			_highlightedSlots.Clear();
			_highlightedSlots.UnionWith(slots);
		}
	}

	private static string BagSignature(Player me)
	{
		string[] array = new string[me.Inventory.Length];
		for (int i = 0; i < me.Inventory.Length; i++)
		{
			IItem item = me.Inventory[i];
			array[i] = $"{item?.ItemId}:{item?.Quantity}:{me.MyEquipment.Contains(i)}";
		}
		return string.Join('|', array);
	}

	private void RelayoutBag(Player me)
	{
		List<(int Slot, ItemDescriptor Item, int Quantity)> list = new List<(int Slot, ItemDescriptor Item, int Quantity)>();
		List<int> list2 = new List<int>();
		bool flag = _category == BagCategory.All && _slotFilter < 0 && _query.Length == 0;
		for (int i = 0; i < Items.Count; i++)
		{
			IItem item = ((i < me.Inventory.Length) ? me.Inventory[i] : null);
			if (item == null || !DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var databaseObject))
			{
				list2.Add(i);
			}
			else if (!me.MyEquipment.Contains(i) && Matches(databaseObject))
			{
				list.Add((i, databaseObject, item.Quantity));
			}
		}
		List<int> list3 = (_sort switch
		{
			BagSort.Name => list.OrderBy<(int, ItemDescriptor, int), string>(((int Slot, ItemDescriptor Item, int Quantity) entry) => Normalize(entry.Item.Name)), 
			BagSort.Type => from entry in list
				orderby entry.Item.ItemType, Normalize(entry.Item.Name)
				select entry, 
			BagSort.Quantity => list.OrderByDescending<(int, ItemDescriptor, int), int>(((int Slot, ItemDescriptor Item, int Quantity) entry) => entry.Quantity), 
			BagSort.Value => list.OrderByDescending<(int, ItemDescriptor, int), int>(((int Slot, ItemDescriptor Item, int Quantity) entry) => entry.Item.Price), 
			_ => list.OrderBy(entry => 0), 
		}).Select<(int, ItemDescriptor, int), int>(((int Slot, ItemDescriptor Item, int Quantity) entry) => entry.Slot).ToList();
		int count = list3.Count;
		if (flag)
		{
			list3.AddRange(list2.Take(Math.Max(0, Player.CapaciteDuSac - count)));
		}
		foreach (SlotItem item2 in Items)
		{
			item2.IsHidden = true;
		}
		int num = 4;
		int num2 = Math.Max(0, (_grid.Width - (294 - num)) / 2);
		int num3 = Math.Max(0, (_grid.Height - (336 - num)) / 2);
		for (int num4 = 0; num4 < list3.Count; num4++)
		{
			SlotItem slotItem = Items[list3[num4]];
			slotItem.IsHidden = false;
			slotItem.SetPosition(num2 + num4 % 7 * 42, num3 + num4 / 7 * 42);
		}
		Label visibleCount = _visibleCount;
		visibleCount.Text = count switch
		{
			0 => Strings.Inventory.CountNone.ToString(), 
			1 => Strings.Inventory.CountOne.ToString(), 
			_ => Strings.Personnage.BagCount.ToString(count), 
		};
	}

	private bool Matches(ItemDescriptor item)
	{
		bool flag;
		switch (_category)
		{
		case BagCategory.Equipment:
			flag = item.ItemType == ItemType.Equipment;
			break;
		case BagCategory.Consumables:
			flag = item.ItemType == ItemType.Consumable;
			break;
		case BagCategory.Techniques:
			flag = item.ItemType == ItemType.Spell;
			break;
		case BagCategory.Resources:
		{
			ItemType itemType = item.ItemType;
			bool flag2 = ((itemType == ItemType.None || itemType == ItemType.Currency) ? true : false);
			flag = flag2;
			break;
		}
		case BagCategory.Quest:
			flag = item.ItemType == ItemType.Event;
			break;
		case BagCategory.Bags:
			flag = item.ItemType == ItemType.Bag;
			break;
		default:
			flag = true;
			break;
		}
		if (!flag)
		{
			return false;
		}
		if (_slotFilter >= 0 && (item.ItemType != ItemType.Equipment || item.EquipmentSlot != _slotFilter))
		{
			return false;
		}
		if (_query.Length != 0)
		{
			return Normalize(item.Name).Contains(_query);
		}
		return true;
	}

	private void UpdateFooter(Player me)
	{
		IGameTexture zeniAnimation = _zeniAnimation;
		if (zeniAnimation != null && zeniAnimation.Width > 0)
		{
			long num = 1870L;
			long num2 = Timing.Global.Milliseconds % num;
			int num3 = ((num2 >= 1600) ? Math.Min(3, 1 + (int)((num2 - 1600) / 90)) : 0);
			if (num3 != _zeniFrame)
			{
				_zeniFrame = num3;
				int num4 = zeniAnimation.Width / 4;
				_zeniIcon.SetTextureRect(num3 * num4, 0, num4, zeniAnimation.Height);
				_zeniIcon.SetSize(32, 32);
			}
		}
		if (me.Zeni != _shownZeni)
		{
			_shownZeni = me.Zeni;
			string text = DatabaseObject<ItemDescriptor>.Get(me.CurrencyItemId)?.Name ?? Strings.AdminWindow.Zeni.ToString();
			_zeniAmount.Text = MarketWindow.FormatAmount(me.Zeni) + " " + text;
		}
		Player.Charge charge = me.LireLaCharge();
		_capacityLabel.Text = Strings.Inventaire.ChargeCount.ToString(SaiyanHud.Amount(charge.Porte), SaiyanHud.Amount(charge.Capacite));
		int num5 = ((charge.Capacite <= 0) ? 120 : ((int)Math.Round(120.0 * (double)Math.Min(charge.Porte, charge.Capacite) / (double)charge.Capacite)));
		_capacityFill.IsHidden = num5 < 8;
		_capacityFill.SetSize(num5, 8);
		_capacityFill.TextureFilename = ((charge.Surcharge || charge.PresquePleine) ? "saiyan_bar_fill_done.png" : "saiyan_bar_fill.png");
	}

	private void SelectCategory(BagCategory category)
	{
		_category = category;
		_slotFilter = -1;
		_bagDirty = true;
		RefreshCategoryButtons();
		RefreshDropdownLabels();
	}

	private void RefreshCategoryButtons()
	{
		foreach (var categoryButton in _categoryButtons)
		{
			Button item = categoryButton.Button;
			ImagePanel item2 = categoryButton.Glyph;
			BagCategory item3 = categoryButton.Category;
			bool flag = item3 == _category;
			item.SetStateTexture(ComponentState.Normal, flag ? "inv_categorie_active.png" : "inv_categorie.png");
			item.SetStateTexture(ComponentState.Hovered, flag ? "inv_categorie_active.png" : "inv_categorie_survol.png");
			item.SetStateTexture(ComponentState.Active, "inv_categorie_active.png");
			item2.RenderColor = (flag ? Night : Muted);
		}
	}

	private void RefreshDropdownLabels()
	{
		List<string> slots = Options.Instance.Equipment.Slots;
		_filterButton.Text = ((_category != BagCategory.Equipment) ? ((_category == BagCategory.All) ? Strings.Inventory.AllCategories.ToString() : Categories.First(((BagCategory Category, string Name, string Glyph) c) => c.Category == _category).Name) : ((_slotFilter >= 0 && _slotFilter < slots.Count) ? slots[_slotFilter] : Strings.Inventory.AllSlots.ToString()));
		_sortButton.Text = Sorts.First(((BagSort Sort, string Name) s) => s.Sort == _sort).Name;
	}

	private void OpenFilterMenu()
	{
		_menu.ClearChildren();
		if (_category == BagCategory.Equipment)
		{
			AddMenuItem(Strings.Inventory.AllSlots.ToString(), delegate
			{
				_slotFilter = -1;
				_bagDirty = true;
				RefreshDropdownLabels();
			});
			List<string> slots = Options.Instance.Equipment.Slots;
			for (int num = 0; num < slots.Count; num++)
			{
				int chosen = num;
				AddMenuItem(slots[num], delegate
				{
					_slotFilter = chosen;
					_bagDirty = true;
					RefreshDropdownLabels();
				});
			}
		}
		else
		{
			AddMenuItem(Strings.Inventory.AllCategories.ToString(), delegate
			{
				SelectCategory(BagCategory.All);
			});
			foreach (var item in Categories.Skip(1))
			{
				var (category, text, _) = item;
				AddMenuItem(text, delegate
				{
					SelectCategory(category);
				});
			}
		}
		OpenMenuUnder(_filterButton);
	}

	private void OpenSortMenu()
	{
		_menu.ClearChildren();
		(BagSort Sort, string Name)[] sorts = Sorts;
		for (int i = 0; i < sorts.Length; i++)
		{
			var (sort, text) = sorts[i];
			AddMenuItem(text, delegate
			{
				_sort = sort;
				_bagDirty = true;
				RefreshDropdownLabels();
			});
		}
		OpenMenuUnder(_sortButton);
	}

	private void AddMenuItem(string text, Action action)
	{
		MenuItem menuItem = _menu.AddItem(text);
		menuItem.Clicked += delegate
		{
			action();
		};
	}

	private void OpenMenuUnder(Base anchor)
	{
		_menu.Open(Pos.None);
		Point point = anchor.ToCanvas(new Point(0, anchor.Height + 2));
		_menu.SetPosition(point.X, point.Y);
	}

	public InventoryWindow(Canvas gameCanvas)
		: base(gameCanvas, string.Empty, modal: false, "InventoryWindow")
	{
		_gameCanvas = gameCanvas;
		_font = Globals.ContentManager.GetFont("chakrapetch");
		_strongFont = Globals.ContentManager.GetFont("rajdhanibold");
		_numberFont = Globals.ContentManager.GetFont("rajdhanibold");
		_titleFont = Globals.ContentManager.GetFont("refuel");
		DisableResizing();
		SetSize(392, 600);
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_window_sac.png");
		SetImage(texture, "saiyan_window_sac.png", ControlState.Active);
		SetImage(texture, "saiyan_window_sac.png", ControlState.Inactive);
		base.IsClosable = false;
		if (base.Titlebar is Titlebar titlebar)
		{
			titlebar.Label.IsHidden = true;
		}
		_title = MakeLabel(this, "Title", 20, White, _titleFont);
		_title.Text = Strings.Inventory.Title.ToString().ToUpperInvariant();
		_title.TextAlign = Pos.Left | Pos.CenterV;
		WindowTitle.Apply(_title);
		_close = new Button(this, "Close", disableText: true);
		_close.SetStateTexture(ComponentState.Normal, "saiyan_close.png");
		_close.SetStateTexture(ComponentState.Hovered, "saiyan_close_hover.png");
		_close.SetStateTexture(ComponentState.Active, "saiyan_close_clicked.png");
		_close.Clicked += delegate
		{
			Hide();
		};
		_contextMenu = new ContextMenu(gameCanvas, "InventoryContextMenu")
		{
			IsVisibleInParent = false,
			IconMarginDisabled = true,
			ItemFont = _font,
			ItemFontSize = 11
		};
		BuildEquipment();
		BuildBag();
		BuildCustomization();
		_equipmentCard.IsHidden = true;
		_customizationCard.IsHidden = true;
		_equipmentTabButton.IsHidden = true;
		_customizationTabButton.IsHidden = true;
		base.IsHidden = true;
	}

	public void Open()
	{
		OpenAt((_gameCanvas.Width - 392) / 2);
	}

	public void OpenBeside()
	{
		OpenAt(_gameCanvas.Width - 392 - 16);
	}

	private void OpenAt(int x)
	{
		SetPosition(Math.Max(0, x), Math.Max(0, (_gameCanvas.Height - 600) / 2));
		Layout();
		_bagDirty = true;
		Show();
		BringToFront();
	}

	public override void Hide()
	{
		if (Globals.CanCloseInventory)
		{
			_contextMenu?.Close();
			CloseMenus();
			Interface.GameUi?.ItemDescriptionWindow?.Hide();
			base.Hide();
		}
	}

	public void OpenContextMenu(int slot)
	{
		if (Items.Count > slot)
		{
			Items[slot].OpenContextMenu();
		}
	}

	public void Update()
	{
		if (!base.IsHidden && Globals.Me?.Inventory != null)
		{
			if (base.TitleBarBounds.Height != _laidOutTitleBarHeight)
			{
				Layout();
			}
			_close.IsHidden = !Globals.CanCloseInventory;
			UpdateBag();
		}
	}

	private new int Y(int windowY)
	{
		return windowY - _laidOutTitleBarHeight;
	}

	private void Layout()
	{
		_laidOutTitleBarHeight = base.TitleBarBounds.Height;
		_title.SetSize(318, 38);
		_title.SetPosition(26, Y(10));
		_close.SetSize(24, 24);
		_close.SetPosition(352, Y(16));
		LayoutEquipment();
		LayoutBag();
		LayoutCustomization();
	}

	private static Label MakeLabel(Base parent, string name, int size, Color color, IFont? font)
	{
		Label label = new Label(parent, name)
		{
			Font = font,
			FontSize = size,
			AutoSizeToContents = false,
			MouseInputEnabled = false
		};
		label.SetTextColor(color, ComponentState.Normal);
		return label;
	}

	private static ImagePanel MakeCard(Base parent, string name)
	{
		return new ImagePanel(parent, name)
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_card.png"),
			TextureNinePatchMargin = new Margin(8),
			MouseInputEnabled = false
		};
	}

	private static ImagePanel MakeImage(Base parent, string name, string texture, int width, int height, bool mouse = false)
	{
		ImagePanel imagePanel = new ImagePanel(parent, name)
		{
			TextureFilename = texture,
			MouseInputEnabled = mouse
		};
		imagePanel.SetSize(width, height);
		return imagePanel;
	}

	private Button MakeButton(Base parent, string name, int width, int height, Pos align = Pos.Center)
	{
		Button button = new Button(parent, name)
		{
			Font = _font,
			FontSize = 11,
			TextAlign = align
		};
		string text = $"saiyan_btn_{width}x{height}";
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

	private void BuildCustomization()
	{
		_equipmentTabButton = MakeTab("EquipmentTabButton", Strings.Personnage.TabEquipment.ToString().ToUpperInvariant());
		_equipmentTabButton.Clicked += delegate
		{
			SelectTab(customizing: false);
		};
		_customizationTabButton = MakeTab("CustomizationTabButton", Strings.Personnage.TabCustomization.ToString().ToUpperInvariant());
		_customizationTabButton.Clicked += delegate
		{
			SelectTab(customizing: true);
		};
		_customizationCard = MakeCard(this, "CustomizationCard");
		_customizationCard.MouseInputEnabled = true;
		_customizationScene = MakeImage(_customizationCard, "CustomizationScene", "inv_scene.png", 220, 250);
		_customizationShadow = MakeImage(_customizationCard, "CustomizationShadow", "inv_ombre.png", 48, 12);
		_customizationLayers = new ImagePanel[Options.Instance.Equipment.Slots.Count + 1];
		for (int num = 0; num < _customizationLayers.Length; num++)
		{
			_customizationLayers[num] = new ImagePanel(_customizationCard, $"CustomizationLayer{num}")
			{
				MouseInputEnabled = false,
				IsHidden = true
			};
		}
		_platePreview = new NamePlatePreview(_customizationCard, "PlatePreview");
		_customizationHint = MakeLabel(_customizationCard, "CustomizationHint", 10, Muted, _font);
		_customizationHint.Text = Strings.Inventory.CustomizationHint.ToString();
		_customizationHint.TextAlign = Pos.Center;
		_titlesTab = MakePickerTab("TitlesTab", Strings.Personnage.TitlesHeading.ToString().ToUpperInvariant());
		_titlesTab.Clicked += delegate
		{
			SelectPicker(ornaments: false);
		};
		_ornamentsTab = MakePickerTab("OrnamentsTab", Strings.Personnage.OrnamentsHeading.ToString().ToUpperInvariant());
		_ornamentsTab.Clicked += delegate
		{
			SelectPicker(ornaments: true);
		};
		_titlesCount = MakeLabel(_customizationCard, "TitlesCount", 9, Locked, _font);
		_titlesCount.TextAlign = Pos.Left | Pos.CenterV;
		_ornamentsCount = MakeLabel(_customizationCard, "OrnamentsCount", 9, Locked, _font);
		_ornamentsCount.TextAlign = Pos.Left | Pos.CenterV;
		_pickerUnderline = MakeImage(_customizationCard, "PickerUnderline", "inv_plein.png", 60, 2);
		_pickerUnderline.RenderColor = Orange;
		_pickerRule = MakeImage(_customizationCard, "PickerRule", "inv_plein.png", 336, 1);
		_pickerRule.RenderColor = RuleColor;
		_pickerList = new ScrollControl(_customizationCard, "PickerList")
		{
			OverflowX = OverflowBehavior.Hidden,
			OverflowY = OverflowBehavior.Auto
		};
		_pickerList.VerticalScrollBar.SetBackgroundTemplate(null, "saiyan_scroll_bg.png");
		_pickerList.VerticalScrollBar.SetScrollBarImage(null, "saiyan_scroll_bar.png", ComponentState.Normal);
		_pickerList.VerticalScrollBar.SetScrollBarImage(null, "saiyan_scroll_bar_hover.png", ComponentState.Hovered);
		_pickerList.VerticalScrollBar.SetScrollBarImage(null, "saiyan_scroll_bar_hover.png", ComponentState.Active);
		SelectTab(customizing: false);
		SelectPicker(ornaments: false);
	}

	private Button MakeTab(string name, string text)
	{
		Button button = new Button(this, name)
		{
			Font = _strongFont,
			FontSize = 14,
			Text = text,
			TextAlign = Pos.Center,
			UsesHeadingFace = false
		};
		button.SetSize(180, 34);
		return button;
	}

	private Button MakePickerTab(string name, string text)
	{
		Button button = new Button(_customizationCard, name)
		{
			Font = _strongFont,
			FontSize = 13,
			Text = text,
			TextAlign = Pos.Center,
			UsesHeadingFace = false
		};
		ComponentState[] array = new ComponentState[4]
		{
			ComponentState.Normal,
			ComponentState.Hovered,
			ComponentState.Active,
			ComponentState.Disabled
		};
		foreach (ComponentState componentState in array)
		{
			button.SetStateTexture(componentState, "saiyan_vide.png");
		}
		button.SetSize(168, 26);
		return button;
	}

	private void LayoutCustomization()
	{
		_equipmentTabButton.SetPosition(16, Y(72));
		_customizationTabButton.SetPosition(196, Y(72));
		_customizationCard.SetSize(360, 472);
		_customizationCard.SetPosition(16, Y(112));
		_customizationScene.SetPosition(70, -8);
		_customizationShadow.SetPosition(156, 192);
		_platePreview.SetSize(356, 236);
		_platePreview.SetPosition(2, 2);
		_customizationHint.SetSize(360, 20);
		_customizationHint.SetPosition(0, 210);
		_titlesTab.SetPosition(12, 244);
		_ornamentsTab.SetPosition(180, 244);
		_pickerRule.SetPosition(12, 273);
		_pickerList.SetSize(336, 184);
		_pickerList.SetPosition(12, 280);
		_customizationCharacterKey = null;
		_pickerSignature = null;
	}

	private void SelectTab(bool customizing)
	{
		_customizing = customizing;
		_equipmentCard.IsHidden = customizing;
		_customizationCard.IsHidden = !customizing;
		StyleTab(_equipmentTabButton, !customizing);
		StyleTab(_customizationTabButton, customizing);
		_tryTitle = null;
		_tryOrnament = null;
		_pickerSignature = null;
		_customizationCharacterKey = null;
	}

	private static void StyleTab(Button tab, bool active)
	{
		tab.SetStateTexture(ComponentState.Normal, active ? "inv_onglet_actif.png" : "inv_onglet.png");
		tab.SetStateTexture(ComponentState.Hovered, active ? "inv_onglet_actif.png" : "inv_onglet_survol.png");
		tab.SetStateTexture(ComponentState.Active, "inv_onglet_actif.png");
		tab.SetStateTexture(ComponentState.Disabled, "inv_onglet.png");
		tab.SetTextColor(active ? White : Muted, ComponentState.Normal);
		tab.SetTextColor(active ? White : OrangeLight, ComponentState.Hovered);
		tab.SetTextColor(White, ComponentState.Active);
	}

	private void SelectPicker(bool ornaments)
	{
		_pickingOrnaments = ornaments;
		_tryTitle = null;
		_tryOrnament = null;
		_pickerSignature = null;
	}

	private void UpdateCustomization()
	{
		if (!_customizing)
		{
			return;
		}
		Player me = Globals.Me;
		if (me != null)
		{
			UpdateCustomizationCharacter(me);
			UpdatePicker(me);
			Guid? tryTitle = _tryTitle;
			object obj;
			if (tryTitle.HasValue)
			{
				Guid valueOrDefault = tryTitle.GetValueOrDefault();
				obj = ((valueOrDefault != Guid.Empty && DatabaseObject<TitleDescriptor>.TryGet(valueOrDefault, out var databaseObject)) ? databaseObject : null);
			}
			else
			{
				obj = me.Title;
			}
			TitleDescriptor title = (TitleDescriptor)obj;
			tryTitle = _tryOrnament;
			object obj2;
			if (tryTitle.HasValue)
			{
				Guid valueOrDefault2 = tryTitle.GetValueOrDefault();
				obj2 = ((valueOrDefault2 != Guid.Empty && DatabaseObject<OrnamentDescriptor>.TryGet(valueOrDefault2, out var databaseObject2)) ? databaseObject2 : null);
			}
			else
			{
				obj2 = me.Ornament;
			}
			OrnamentDescriptor ornament = (OrnamentDescriptor)obj2;
			_platePreview.Content = NamePlate.ContentOf(me, me.NamePlateColor())with
			{
				Title = title,
				Ornament = ornament
			};
			_platePreview.PlateBottom = 196 - (me.NamePlateBodyHeight + 6) * 2;
		}
	}

	private void UpdateCustomizationCharacter(Player me)
	{
		string sprite = (string.IsNullOrWhiteSpace(me.TransformedSprite) ? me.Sprite : me.TransformedSprite);
		string text = PreviewKey(me, sprite, Direction.Down);
		if (!(text == _customizationCharacterKey))
		{
			_customizationCharacterKey = text;
			LayCharacter(me, sprite, _customizationLayers, Direction.Down, 180, 198, 2);
			_platePreview.BringToFront();
		}
	}

	private void UpdatePicker(Player me)
	{
		string text = string.Join('|', _pickingOrnaments, me.TitleId, string.Join(',', me.TitleIds), DatabaseObject<TitleDescriptor>.Lookup.Count, me.OrnamentId, string.Join(',', me.OrnamentIds), DatabaseObject<OrnamentDescriptor>.Lookup.Count);
		if (!(text == _pickerSignature))
		{
			_pickerSignature = text;
			_tryTitle = null;
			_tryOrnament = null;
			List<PickerEntry> list = TitleEntries(me);
			List<PickerEntry> list2 = OrnamentEntries(me);
			LayoutPickerTabs($"{list.Count((PickerEntry entry) => entry.Unlocked && entry.Id != Guid.Empty)} / {list.Count - 1}", $"{list2.Count((PickerEntry entry) => entry.Unlocked && entry.Id != Guid.Empty)} / {list2.Count - 1}");
			RebuildPickerRows(_pickingOrnaments ? list2 : list);
		}
	}

	private static List<PickerEntry> TitleEntries(Player me)
	{
		List<PickerEntry> list = new List<PickerEntry>
		{
			new PickerEntry(Guid.Empty, Strings.Titles.None, Unlocked: true, me.TitleId == Guid.Empty, null, null)
		};
		list.AddRange((from title in DatabaseObject<TitleDescriptor>.Lookup.Values.OfType<TitleDescriptor>()
			select new PickerEntry(title.Id, title.Name, me.TitleIds.Contains(title.Id), title.Id == me.TitleId, null, string.IsNullOrWhiteSpace(title.Description) ? null : title.Description) into entry
			orderby entry.Unlocked descending
			select entry).ThenBy<PickerEntry, string>((PickerEntry entry) => entry.Name, StringComparer.CurrentCultureIgnoreCase));
		return list;
	}

	private static List<PickerEntry> OrnamentEntries(Player me)
	{
		List<PickerEntry> list = new List<PickerEntry>
		{
			new PickerEntry(Guid.Empty, Strings.Ornaments.None, Unlocked: true, me.OrnamentId == Guid.Empty, null, null)
		};
		list.AddRange((from entry in DatabaseObject<OrnamentDescriptor>.Lookup.Values.OfType<OrnamentDescriptor>().Select(delegate(OrnamentDescriptor ornament)
			{
				bool flag = me.OrnamentIds.Contains(ornament.Id);
				return new PickerEntry(ornament.Id, ornament.Name, flag, ornament.Id == me.OrnamentId, (!flag && ornament.UnlockLevel > 0) ? Strings.Personnage.LevelDetail.ToString(ornament.UnlockLevel) : null, string.IsNullOrWhiteSpace(ornament.Description) ? null : ornament.Description);
			})
			orderby entry.Unlocked descending
			select entry).ThenBy<PickerEntry, string>((PickerEntry entry) => entry.Name, StringComparer.CurrentCultureIgnoreCase));
		return list;
	}

	private void LayoutPickerTabs(string titlesCount, string ornamentsCount)
	{
		_titlesCount.Text = titlesCount;
		_ornamentsCount.Text = ornamentsCount;
		(Button, Label, bool)[] array = new(Button, Label, bool)[2]
		{
			(_titlesTab, _titlesCount, !_pickingOrnaments),
			(_ornamentsTab, _ornamentsCount, _pickingOrnaments)
		};
		for (int i = 0; i < array.Length; i++)
		{
			var (button, label, flag) = array[i];
			button.SetTextColor(flag ? White : Muted, ComponentState.Normal);
			button.SetTextColor(flag ? White : OrangeLight, ComponentState.Hovered);
			button.SetTextColor(White, ComponentState.Active);
			int num = TextWidth(button.Text, button.Font, button.FontSize);
			int num2 = button.X + (button.Width + num) / 2;
			label.SetSize(60, 26);
			label.SetPosition(num2 + 5, button.Y);
			if (flag)
			{
				_pickerUnderline.SetSize(num, 2);
				_pickerUnderline.SetPosition(num2 - num, 268);
			}
		}
	}

	private void RebuildPickerRows(List<PickerEntry> entries)
	{
		foreach (Base pickerRow in _pickerRows)
		{
			pickerRow.Parent?.RemoveChild(pickerRow, dispose: true);
		}
		_pickerRows.Clear();
		int width = 322;
		for (int i = 0; i < entries.Count; i++)
		{
			_pickerRows.Add(BuildPickerRow(entries[i], i * 26, width));
		}
	}

	private Base BuildPickerRow(PickerEntry entry, int y, int width)
	{
		bool ornaments = _pickingOrnaments;
		Color restColor = (entry.Worn ? White : (entry.Unlocked ? RowText : Locked));
		string restTexture = (entry.Worn ? "inv_ligne_portee.png" : null);
		ImagePanel row = new ImagePanel(_pickerList, $"Pick_{entry.Id}")
		{
			TextureNinePatchMargin = new Margin(4),
			MouseInputEnabled = true
		};
		row.Texture = ((restTexture == null) ? null : Globals.ContentManager.GetTexture(TextureType.Gui, restTexture));
		row.SetSize(width, 26);
		row.SetPosition(0, y);
		if (entry.Worn)
		{
			ImagePanel imagePanel = MakeImage(row, "Accent", "inv_plein.png", 3, 16);
			imagePanel.RenderColor = Orange;
			imagePanel.SetPosition(0, 5);
		}
		Label name = MakeLabel(row, "Name", 11, restColor, _font);
		name.Text = entry.Name;
		name.TextAlign = Pos.Left | Pos.CenterV;
		name.SetSize(width - 110, 26);
		name.SetPosition(12, 0);
		if (entry.Worn)
		{
			Label label = MakeLabel(row, "Worn", 10, OrangeLight, _font);
			label.Text = Strings.Personnage.SheetWorn.ToString();
			label.TextAlign = Pos.Right | Pos.CenterV;
			label.SetSize(90, 26);
			label.SetPosition(width - 100, 0);
		}
		else
		{
			if (!entry.Unlocked)
			{
				string note = entry.Note;
				if (note != null)
				{
					Label label2 = MakeLabel(row, "Level", 9, Locked, _font);
					label2.Text = note;
					label2.TextAlign = Pos.Right | Pos.CenterV;
					label2.SetSize(90, 26);
					label2.SetPosition(width - 100, 0);
					goto IL_02bc;
				}
			}
			if (!entry.Unlocked)
			{
				ImagePanel imagePanel2 = MakeImage(row, "Padlock", "inv_cadenas.png", 10, 12);
				imagePanel2.RenderColor = Locked;
				imagePanel2.SetPosition(width - 22, 7);
			}
		}
		goto IL_02bc;
		IL_02bc:
		string tooltip = entry.Tooltip;
		if (tooltip != null)
		{
			row.SetToolTipText(tooltip);
		}
		row.HoverEnter += delegate
		{
			if (!entry.Worn)
			{
				row.Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "hdv_ligne_survol.png");
			}
			name.SetTextColor(entry.Unlocked ? OrangeLight : RowText, ComponentState.Normal);
			if (ornaments)
			{
				_tryOrnament = entry.Id;
			}
			else
			{
				_tryTitle = entry.Id;
			}
		};
		row.HoverLeave += delegate
		{
			row.Texture = ((restTexture == null) ? null : Globals.ContentManager.GetTexture(TextureType.Gui, restTexture));
			name.SetTextColor(restColor, ComponentState.Normal);
			if (ornaments && _tryOrnament == entry.Id)
			{
				_tryOrnament = null;
			}
			else if (!ornaments && _tryTitle == entry.Id)
			{
				_tryTitle = null;
			}
		};
		row.Clicked += delegate
		{
			if (entry.Unlocked && !entry.Worn)
			{
				if (ornaments)
				{
					PacketSender.SendSetActiveOrnament(entry.Id);
				}
				else
				{
					PacketSender.SendSetActiveTitle(entry.Id);
				}
			}
		};
		return row;
	}

	private static int TextWidth(string? text, IFont? font, int size)
	{
		if (font != null && Graphics.Renderer != null && !string.IsNullOrEmpty(text))
		{
			return (int)Graphics.Renderer.MeasureText(text, font, size, 1f).X;
		}
		return (text?.Length ?? 0) * 8;
	}

	private void BuildEquipment()
	{
		_equipmentCard = MakeCard(this, "EquipmentCard");
		_equipmentCard.MouseInputEnabled = true;
		_scene = MakeImage(_equipmentCard, "Scene", "inv_scene.png", 220, 250);
		_pedestal = MakeImage(_equipmentCard, "Pedestal", "inv_socle.png", 200, 74);
		List<string> slots = Options.Instance.Equipment.Slots;
		_previewLayers = new ImagePanel[slots.Count + 1];
		for (int i = 0; i < _previewLayers.Length; i++)
		{
			_previewLayers[i] = new ImagePanel(_equipmentCard, $"PreviewLayer{i}")
			{
				MouseInputEnabled = false,
				IsHidden = true
			};
		}
		_turnLeft = MakeTurnButton("TurnLeft", "inv_tourner_gauche");
		_turnLeft.Clicked += delegate
		{
			Turn(clockwise: false);
		};
		_turnRight = MakeTurnButton("TurnRight", "inv_tourner_droite");
		_turnRight.Clicked += delegate
		{
			Turn(clockwise: true);
		};
		HashSet<(int Column, int Row)> hashSet = new HashSet<(int Column, int Row)>();
		int num = 0;
		for (int num2 = 0; num2 < slots.Count; num2++)
		{
			string text = Normalize(slots[num2]);
			(int Column, int Row) tuple = ((!text.StartsWith("special")) ? SlotPlaces.GetValueOrDefault(text, (-1, -1)) : ((num++ == 0) ? (0, 3) : (1, 3)));
			string text2 = (text.StartsWith("special") ? "special" : text);
			string glyph = ((tuple.Item1 >= 0) ? ("inv_emplacement_" + text2 + ".png") : null);
			EquipmentSlotItem equipmentSlotItem = new EquipmentSlotItem(this, _equipmentCard, num2, slots[num2], glyph);
			_equipmentSlots.Add(equipmentSlotItem);
			if (tuple.Item1 >= 0 && hashSet.Add(tuple))
			{
				_slotPlacement[equipmentSlotItem] = tuple;
			}
		}
		foreach (EquipmentSlotItem item2 in _equipmentSlots.Where((EquipmentSlotItem s) => !_slotPlacement.ContainsKey(s)))
		{
			for (int num3 = 0; num3 < 3; num3++)
			{
				if (_slotPlacement.ContainsKey(item2))
				{
					break;
				}
				int[] array = new int[2] { 0, 1 };
				foreach (int item in array)
				{
					if (hashSet.Add((item, num3)))
					{
						_slotPlacement[item2] = (item, num3);
						break;
					}
				}
			}
			item2.IsHidden = !_slotPlacement.ContainsKey(item2);
		}
		BuildDragonBalls();
	}

	private Button MakeTurnButton(string name, string texture)
	{
		Button button = new Button(_equipmentCard, name, disableText: true);
		button.SetStateTexture(ComponentState.Normal, texture + ".png");
		button.SetStateTexture(ComponentState.Hovered, texture + "_survol.png");
		button.SetStateTexture(ComponentState.Active, texture + "_survol.png");
		button.SetSize(30, 30);
		button.SetToolTipText(Strings.Personnage.TurnHint.ToString());
		return button;
	}

	private void LayoutEquipment()
	{
		_equipmentCard.SetSize(360, 472);
		_equipmentCard.SetPosition(16, Y(112));
		_scene.SetPosition(70, 40);
		_pedestal.SetPosition(80, 240);
		foreach (KeyValuePair<EquipmentSlotItem, (int Column, int Row)> item3 in _slotPlacement)
		{
			item3.Deconstruct(out var key, out var value);
			(int Column, int Row) tuple = value;
			EquipmentSlotItem equipmentSlotItem = key;
			int item = tuple.Item1;
			int item2 = tuple.Item2;
			int x = item switch
			{
				0 => 14, 
				1 => 294, 
				_ => 154, 
			};
			int y = ((item == 2) ? 318 : (20 + item2 * 108));
			equipmentSlotItem.SetPosition(x, y);
		}
		int y2 = 329;
		_turnLeft.SetPosition(98, y2);
		_turnRight.SetPosition(232, y2);
		LayoutDragonBalls();
		_previewKey = null;
	}

	private void UpdateEquipment()
	{
		foreach (EquipmentSlotItem equipmentSlot in _equipmentSlots)
		{
			equipmentSlot.Update();
		}
		UpdatePreview();
		UpdateDragonBalls();
		UpdateDragHighlights();
	}

	private void UpdateDragHighlights()
	{
		int num = -1;
		bool flag = false;
		if (DragAndDrop.CurrentPackage != null)
		{
			Player me = Globals.Me;
			if (me != null)
			{
				Base obj = DragAndDrop.SourceControl?.Parent;
				if (!(obj is InventoryItem inventoryItem))
				{
					if (obj is EquipmentSlotItem)
					{
						flag = true;
					}
				}
				else if (inventoryItem.SlotIndex < me.Inventory.Length)
				{
					ItemDescriptor itemDescriptor = me.Inventory[inventoryItem.SlotIndex]?.Descriptor;
					if (itemDescriptor != null && itemDescriptor.ItemType == ItemType.Equipment)
					{
						num = itemDescriptor.EquipmentSlot;
					}
				}
			}
		}
		int alpha = (int)(170f + 85f * MathF.Sin((float)Timing.Global.Milliseconds / 170f));
		foreach (EquipmentSlotItem equipmentSlot in _equipmentSlots)
		{
			equipmentSlot.SetHighlight(equipmentSlot.SlotIndex == num, alpha);
		}
		if (flag != _bagHighlighted)
		{
			_bagHighlighted = flag;
			_gridCard.Texture = Globals.ContentManager.GetTexture(TextureType.Gui, flag ? "saiyan_card_hover.png" : "saiyan_card.png");
		}
	}

	private void Turn(bool clockwise)
	{
		Direction[] array = new Direction[4]
		{
			Direction.Down,
			Direction.Right,
			Direction.Up,
			Direction.Left
		};
		int num = Array.IndexOf(array, _facing);
		_facing = array[(num + (clockwise ? 1 : 3)) % 4];
		_previewKey = null;
	}

	private void UpdatePreview()
	{
		Player me = Globals.Me;
		if (me != null)
		{
			string sprite = (string.IsNullOrWhiteSpace(me.TransformedSprite) ? me.Sprite : me.TransformedSprite);
			string text = PreviewKey(me, sprite, _facing);
			if (!(text == _previewKey))
			{
				_previewKey = text;
				RebuildPreview(me, sprite);
			}
		}
	}

	private static string PreviewKey(Player me, string? sprite, Direction facing)
	{
		StringBuilder stringBuilder = new StringBuilder().Append(sprite).Append('|').Append(facing)
			.Append('|')
			.Append(me.Gender);
		int[] myEquipment = me.MyEquipment;
		foreach (int num in myEquipment)
		{
			Guid? guid = ((num >= 0 && num < me.Inventory.Length) ? me.Inventory[num]?.ItemId : ((Guid?)null));
			stringBuilder.Append('|').Append(guid);
		}
		return stringBuilder.ToString();
	}

	private void RebuildPreview(Player me, string? sprite)
	{
		LayCharacter(me, sprite, _previewLayers, _facing, 180, 270, 0);
		foreach (EquipmentSlotItem equipmentSlot in _equipmentSlots)
		{
			equipmentSlot.BringToFront();
		}
		_turnLeft.BringToFront();
		_turnRight.BringToFront();
	}

	private static void LayCharacter(Player me, string? sprite, ImagePanel[] layers, Direction facing, int centerX, int feetY, int scale)
	{
		foreach (ImagePanel imagePanel in layers)
		{
			imagePanel.IsHidden = true;
		}
		int row = facing switch
		{
			Direction.Left => 1, 
			Direction.Right => 2, 
			Direction.Up => 3, 
			_ => 0, 
		};
		if (string.IsNullOrWhiteSpace(sprite))
		{
			return;
		}
		SpriteSheets.Pose? pose = SpriteSheets.StandingBody(me.Sprite, sprite, row);
		if (!pose.HasValue)
		{
			return;
		}
		SpriteSheets.Pose valueOrDefault = pose.GetValueOrDefault();
		if (valueOrDefault.IsEmpty)
		{
			return;
		}
		IGameTexture texture = valueOrDefault.Texture;
		(int, int, int, int) frame = valueOrDefault.Frame;
		if (scale <= 0)
		{
			scale = Math.Clamp(240 / Math.Max(1, frame.Item4), 1, 6);
		}
		bool flag = string.IsNullOrWhiteSpace(me.TransformedSprite) || Transformation.KeepsPaperdolls(me);
		List<string> list = Options.Instance.Equipment.Paperdoll.Directions[(int)facing];
		List<string> slots = Options.Instance.Equipment.Slots;
		for (int j = 0; j < list.Count && j < layers.Length; j++)
		{
			IGameTexture gameTexture = null;
			(int, int, int, int) tuple = default((int, int, int, int));
			Color color = me.Color;
			if (list[j] == "Player")
			{
				gameTexture = texture;
				tuple = frame;
			}
			else if (flag)
			{
				int num = slots.IndexOf(list[j]);
				int num2 = ((num >= 0 && num < me.MyEquipment.Length) ? me.MyEquipment[num] : (-1));
				Guid? guid = ((num2 < 0 || num2 >= me.Inventory.Length) ? ((Guid?)null) : me.Inventory[num2]?.ItemId);
				if (guid.HasValue)
				{
					Guid valueOrDefault2 = guid.GetValueOrDefault();
					if (DatabaseObject<ItemDescriptor>.TryGet(valueOrDefault2, out var databaseObject))
					{
						string outfitName = Paperdolls.ForForm(me, Paperdolls.ForClass((me.Gender == Gender.Male) ? databaseObject.MalePaperdoll : databaseObject.FemalePaperdoll, me.Class));
						pose = SpriteSheets.StandingOutfit(me.Sprite, outfitName, valueOrDefault, row);
						if (pose.HasValue)
						{
							SpriteSheets.Pose valueOrDefault3 = pose.GetValueOrDefault();
							if (!valueOrDefault3.IsEmpty)
							{
								gameTexture = valueOrDefault3.Texture;
								tuple = valueOrDefault3.Frame;
								color = databaseObject.Color;
							}
						}
					}
				}
			}
			if (gameTexture != null && tuple.Item3 > 0 && tuple.Item4 > 0)
			{
				ImagePanel imagePanel2 = layers[j];
				imagePanel2.Texture = gameTexture;
				imagePanel2.SetTextureRect(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
				imagePanel2.SetSize(tuple.Item3 * scale, tuple.Item4 * scale);
				imagePanel2.SetPosition(centerX - tuple.Item3 * scale / 2, feetY - tuple.Item4 * scale);
				imagePanel2.RenderColor = color;
				imagePanel2.IsHidden = false;
				imagePanel2.BringToFront();
			}
		}
	}

	private void BuildDragonBalls()
	{
		int num = Math.Max(0, Options.Instance.Player.DragonBallCount);
		_balls = new ImagePanel[num];
		_ballsOwned = new bool[num];
		for (int i = 0; i < num; i++)
		{
			_balls[i] = new ImagePanel(_equipmentCard, $"DragonBall{i + 1}");
			_balls[i].SetSize(30, 30);
			_ballsOwned[i] = true;
		}
		RefreshDragonBalls(force: true);
	}

	private void LayoutDragonBalls()
	{
		int num = _balls.Length * 30 + Math.Max(0, _balls.Length - 1) * 8;
		int num2 = (360 - num) / 2;
		for (int i = 0; i < _balls.Length; i++)
		{
			_balls[i].SetPosition(num2 + i * 38, 406);
		}
	}

	private void UpdateDragonBalls()
	{
		RefreshDragonBalls(force: false);
		long milliseconds = Timing.Global.Milliseconds;
		for (int i = 0; i < _balls.Length; i++)
		{
			float x = (_ballsOwned[i] ? (MathF.Sin((float)(milliseconds % 2400) / 2400f * ((float)Math.PI * 2f) + (float)i * 0.9f) * 1.5f) : 0f);
			_balls[i].SetPosition(_balls[i].X, 406 + (int)MathF.Round(x));
		}
	}

	private void RefreshDragonBalls(bool force)
	{
		bool[] dragonBalls = Globals.DragonBalls;
		for (int i = 0; i < _balls.Length; i++)
		{
			bool flag = i < dragonBalls.Length && dragonBalls[i];
			if (force || _ballsOwned[i] != flag)
			{
				_ballsOwned[i] = flag;
				string text = (flag ? "_owned" : string.Empty);
				_balls[i].Texture = Globals.ContentManager.GetTexture(TextureType.Image, $"dragonball{i + 1}{text}.png") ?? Globals.ContentManager.GetTexture(TextureType.Image, "dragonball1" + text + ".png");
				_balls[i].SetToolTipText(flag ? Strings.Character.DragonBallOwned.ToString(i + 1) : Strings.Character.DragonBallMissing.ToString(i + 1));
			}
		}
	}

	private static string Normalize(string? text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		string text2 = text.Normalize(NormalizationForm.FormD);
		StringBuilder stringBuilder = new StringBuilder(text2.Length);
		string text3 = text2;
		foreach (char c in text3)
		{
			if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
			{
				stringBuilder.Append(char.ToLowerInvariant(c));
			}
		}
		return stringBuilder.ToString();
	}
}
