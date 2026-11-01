using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Items;
using Intersect.Client.General;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Items;
using Intersect.Client.Localization;
using Intersect.Config.Guilds;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Localization;
using Intersect.Models;

namespace Intersect.Client.Interface.Game.Bank;

public class BankWindow : WindowControl
{
	public enum Refusal
	{
		Deposit,
		Withdraw,
		Move,
		BankFull,
		BagFull
	}

	private enum BankCategory
	{
		All,
		Equipment,
		Consumables,
		Techniques,
		Resources,
		Quest,
		Bags
	}

	private const int WindowWidth = 480;

	private const int WindowHeight = 600;

	private new const int Margin = 16;

	private const int ContentTop = 72;

	private const int CardWidth = 448;

	private const int CellSize = 38;

	private const int CellPitch = 40;

	private const int Columns = 10;

	private const int PersonalRows = 10;

	private const int GuildRows = 5;

	private const int GridCardY = 116;

	private const int CardPadding = 8;

	private const int FooterY = 538;

	private const int SearchWidth = 150;

	private const int CtaWidth = 180;

	private const int CtaHeight = 34;

	private const int CapacityBarWidth = 150;

	private const long RefusalMs = 2800L;

	private const long FlashMs = 650L;

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color Entry = new Color(255, 196, 204, 220);

	private static readonly Color Muted = new Color(255, 176, 186, 206);

	private static readonly Color Dim = new Color(255, 150, 160, 182);

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private static readonly Color Green = new Color(255, 142, 214, 124);

	private static readonly Color Red = new Color(255, 255, 123, 107);

	public List<SlotItem> Items = new List<SlotItem>();

	private readonly Canvas _gameCanvas;

	private readonly IFont? _font;

	private readonly IFont? _mediumFont;

	private readonly IFont? _strongFont;

	private readonly IFont? _ctaFont;

	private readonly Label _title;

	private readonly Button _close;

	private readonly ContextMenu _contextMenu;

	private readonly List<(Button Button, ImagePanel Glyph, BankCategory Category)> _categoryButtons = new List<(Button, ImagePanel, BankCategory)>();

	private readonly ImagePanel _searchBack;

	private readonly TextBox _search;

	private readonly Label _searchHint;

	private readonly ImagePanel _gridCard;

	private readonly ScrollControl _grid;

	private readonly Label _noMatch;

	private readonly ImagePanel? _rightsCard;

	private readonly Label? _rankTitle;

	private readonly Label? _guildName;

	private readonly List<(ImagePanel Dot, Label Name, Label Detail)> _rights = new List<(ImagePanel, Label, Label)>();

	private readonly ImagePanel _capacityIcon;

	private readonly Label _capacityLabel;

	private readonly ImagePanel _capacityTrack;

	private readonly ImagePanel _capacityFill;

	private readonly Button _store;

	private readonly bool _guild;

	private readonly int _rows;

	private int[] _cellLook = Array.Empty<int>();

	private Guid[] _slotItems = Array.Empty<Guid>();

	private int[] _slotQuantities = Array.Empty<int>();

	private bool _seen;

	private long[] _flashUntil = Array.Empty<long>();

	private BankCategory _category;

	private string _query = string.Empty;

	private bool _dirty = true;

	private string? _signature;

	private string? _storeText;

	private string _refusal = string.Empty;

	private long _refusalUntil;

	private bool _showingRefusal;

	private int _laidOutTitleBarHeight = -1;

	private static (BankCategory Category, string Name, string Glyph)[] Categories => new(BankCategory, string, string)[7]
	{
		(BankCategory.All, Strings.Inventory.CategoryAll.ToString(), "tout"),
		(BankCategory.Equipment, Strings.Personnage.CategoryEquipment.ToString(), "equipement"),
		(BankCategory.Consumables, Strings.Personnage.CategoryConsumables.ToString(), "consommable"),
		(BankCategory.Techniques, Strings.Personnage.CategoryTechniques.ToString(), "technique"),
		(BankCategory.Resources, Strings.Personnage.CategoryResources.ToString(), "ressource"),
		(BankCategory.Quest, Strings.Inventory.CategoryQuest.ToString(), "quete"),
		(BankCategory.Bags, Strings.Personnage.CategoryBags.ToString(), "sac")
	};

	public BankWindow(Canvas gameCanvas)
		: base(gameCanvas, string.Empty, modal: false, "BankWindow")
	{
		_gameCanvas = gameCanvas;
		_guild = Globals.IsGuildBank;
		_rows = (_guild ? 5 : 10);
		_font = Globals.ContentManager.GetFont("chakrapetch");
		_mediumFont = Globals.ContentManager.GetFont("chakrapetchmedium") ?? _font;
		_strongFont = Globals.ContentManager.GetFont("rajdhanibold");
		_ctaFont = Globals.ContentManager.GetFont("sairanom") ?? _strongFont;
		Interface.InputBlockingComponents.Add(this);
		DisableResizing();
		SetSize(480, 600);
		SetPosition(16, Math.Max(0, (_gameCanvas.Height - 600) / 2));
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_window_boutique.png");
		SetImage(texture, "saiyan_window_boutique.png", ControlState.Active);
		SetImage(texture, "saiyan_window_boutique.png", ControlState.Inactive);
		base.IsClosable = false;
		if (base.Titlebar is Titlebar titlebar)
		{
			titlebar.Label.IsHidden = true;
		}
		_title = MakeLabel(this, "Title", 20, White, Globals.ContentManager.GetFont("refuel"));
		_title.Text = (_guild ? Strings.Guilds.Bank.ToString(Globals.Me?.Guild) : Strings.Bank.Title.ToString()).ToUpperInvariant();
		WindowTitle.Apply(_title);
		_close = new Button(this, "Close", disableText: true);
		_close.SetStateTexture(ComponentState.Normal, "saiyan_close.png");
		_close.SetStateTexture(ComponentState.Hovered, "saiyan_close_hover.png");
		_close.SetStateTexture(ComponentState.Active, "saiyan_close_clicked.png");
		_close.Clicked += delegate
		{
			Interface.GameUi?.NotifyCloseBank();
		};
		_contextMenu = new ContextMenu(gameCanvas, "BankContextMenu")
		{
			IsVisibleInParent = false,
			IconMarginDisabled = true,
			ItemFont = _font,
			ItemFontSize = 11
		};
		(BankCategory, string, string)[] categories = Categories;
		for (int num = 0; num < categories.Length; num++)
		{
			(BankCategory, string, string) tuple = categories[num];
			BankCategory category = tuple.Item1;
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
		_searchBack = MakeImage(this, "SearchBack", $"saiyan_field_{150}x28.png", 150, 28, mouse: true);
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
		_search.SetSize(116, 22);
		_search.SetPosition(28, 3);
		_search.TextChanged += delegate
		{
			_query = Normalize(_search.Text);
			_searchHint.IsHidden = !string.IsNullOrEmpty(_search.Text);
			_dirty = true;
		};
		Interface.FocusComponents.Add(_search);
		_searchHint = MakeLabel(_searchBack, "SearchHint", 11, Dim, _mediumFont);
		_searchHint.Text = Strings.Bank.SearchHint.ToString();
		_searchHint.Padding = _search.Padding;
		_searchHint.SetBounds(_search.Bounds);
		_gridCard = MakeCard(this, "GridCard");
		_grid = new ScrollControl(this, "ItemContainer")
		{
			OverflowX = OverflowBehavior.Hidden,
			OverflowY = OverflowBehavior.Auto
		};
		StyleScroll(_grid);
		_noMatch = MakeLabel(this, "NoMatch", 11, Dim, _mediumFont);
		_noMatch.Text = Strings.Bank.NoMatch.ToString();
		_noMatch.TextAlign = Pos.Center;
		_noMatch.IsHidden = true;
		int num2 = Math.Max(0, Globals.BankSlotCount);
		for (int num3 = 0; num3 < num2; num3++)
		{
			BankItem bankItem = new BankItem(this, _grid, num3, _contextMenu)
			{
				MinimumSize = new Point(38, 38),
			};
			bankItem.SetSize(38, 38);
			bankItem.Margin = new Margin(0);
			bankItem.TextureFilename = "saiyan_slot.png";
			bankItem.Icon.SetPosition(3, 3);
			Items.Add(bankItem);
		}
		_cellLook = Enumerable.Repeat(-1, Items.Count).ToArray();
		_slotItems = new Guid[Items.Count];
		_slotQuantities = new int[Items.Count];
		_flashUntil = new long[Items.Count];
		if (_guild)
		{
			_rightsCard = MakeCard(this, "RightsCard");
			Label label = MakeLabel(_rightsCard, "RankCaption", 10, Muted, _font);
			label.Text = Strings.Bank.YourRank.ToString();
			label.SetBounds(18, 10, 160, 18);
			_rankTitle = MakeLabel(_rightsCard, "RankTitle", 16, White, _strongFont);
			_rankTitle.SetBounds(18, 26, 220, 32);
			_guildName = MakeLabel(_rightsCard, "GuildName", 10, Muted, _mediumFont);
			_guildName.TextAlign = Pos.Right | Pos.CenterV;
			_guildName.SetBounds(230, 30, 200, 24);
			ImagePanel imagePanel3 = MakeImage(_rightsCard, "RightsRule", "hdv_filet.png", 412, 1);
			imagePanel3.SetPosition(18, 66);
			string[] array = new string[3]
			{
				Strings.Quantity.Deposit.ToString(),
				Strings.Quantity.Withdraw.ToString(),
				Strings.Bank.RightMove.ToString()
			};
			for (int num4 = 0; num4 < array.Length; num4++)
			{
				int num5 = 78 + num4 * 34;
				ImagePanel imagePanel4 = MakeImage(_rightsCard, $"RightDot{num4}", "saiyan_puce.png", 8, 8);
				imagePanel4.SetPosition(24, num5 + 13);
				Label label2 = MakeLabel(_rightsCard, $"RightName{num4}", 11, White, _font);
				label2.Text = array[num4];
				label2.SetBounds(42, num5, 150, 34);
				Label label3 = MakeLabel(_rightsCard, $"RightDetail{num4}", 10, Muted, _mediumFont);
				label3.TextAlign = Pos.Right | Pos.CenterV;
				label3.SetBounds(190, num5, 240, 34);
				_rights.Add((imagePanel4, label2, label3));
			}
		}
		_capacityIcon = MakeImage(this, "CapacityIcon", "inv_capacite.png", 18, 18);
		_capacityIcon.RenderColor = Muted;
		_capacityLabel = MakeLabel(this, "CapacityLabel", 10, Muted, _font);
		_capacityTrack = MakeImage(this, "CapacityTrack", "saiyan_bar_track.png", 150, 8);
		_capacityTrack.TextureNinePatchMargin = new Margin(4);
		_capacityFill = MakeImage(this, "CapacityFill", "saiyan_bar_fill.png", 0, 8);
		_capacityFill.TextureNinePatchMargin = new Margin(4);
		_store = new Button(this, "StoreResources")
		{
			Font = _ctaFont,
			FontSize = 11,
			TextAlign = Pos.Center
		};
		string text = $"saiyan_cta_{180}x{34}";
		_store.SetStateTexture(ComponentState.Normal, text + ".png");
		_store.SetStateTexture(ComponentState.Hovered, text + "_hover.png");
		_store.SetStateTexture(ComponentState.Active, text + "_clicked.png");
		_store.SetStateTexture(ComponentState.Disabled, text + "_disabled.png");
		_store.SetTextColor(Night, ComponentState.Normal);
		_store.SetTextColor(Night, ComponentState.Hovered);
		_store.SetTextColor(Night, ComponentState.Active);
		_store.SetTextColor(Dim, ComponentState.Disabled);
		_store.SetSize(180, 34);
		_store.SetToolTipText(Strings.Bank.StoreTooltip.ToString());
		_store.Clicked += delegate
		{
			StoreResources();
		};
		RefreshCategoryButtons();
		Layout();
	}

	public void Detach()
	{
		_contextMenu.Close();
		_contextMenu.DelayedDelete();
		Interface.GameUi?.ItemDescriptionWindow?.Hide();
		Interface.GameUi?.GameMenu?.GetInventoryWindow()?.SetHighlightedSlots(null);
		Interface.InputBlockingComponents.Remove(this);
		Interface.FocusComponents.Remove(_search);
		Hide();
		DelayedDelete();
	}

	public void DockWith(Base inventory)
	{
		int num = Math.Max(0, (_gameCanvas.Width - 480 - inventory.Width) / 2);
		SetPosition(num, Math.Max(0, (_gameCanvas.Height - 600) / 2));
		inventory.SetPosition(Math.Max(0, Math.Min(num + 480, _gameCanvas.Width - inventory.Width)), Math.Max(0, (_gameCanvas.Height - inventory.Height) / 2));
	}

	public static void Refuse(Refusal refusal)
	{
		BankWindow bankWindow = Interface.GameUi?.GetBankWindow();
		if (bankWindow != null)
		{
			BankWindow bankWindow2 = bankWindow;
			bankWindow2._refusal = refusal switch
			{
				Refusal.Deposit => Strings.Bank.RefusalDeposit.ToString(), 
				Refusal.Withdraw => Strings.Bank.RefusalWithdraw.ToString(), 
				Refusal.Move => Strings.Bank.RefusalMove.ToString(), 
				Refusal.BankFull => Strings.Bank.RefusalBankFull.ToString(), 
				_ => Strings.Bank.RefusalBagFull.ToString(), 
			};
			bankWindow._refusalUntil = Timing.Global.Milliseconds + 2800;
		}
	}

	public void Update()
	{
		if (base.IsHidden)
		{
			return;
		}
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		Item[] bankSlots = Globals.BankSlots;
		if (bankSlots == null)
		{
			return;
		}
		if (base.TitleBarBounds.Height != _laidOutTitleBarHeight)
		{
			Layout();
		}
		long milliseconds = Timing.Global.Milliseconds;
		string text = Signature(bankSlots, milliseconds);
		if (_dirty || text != _signature)
		{
			_dirty = false;
			_signature = text;
			Relayout(bankSlots);
		}
		for (int i = 0; i < Items.Count; i++)
		{
			SlotItem slotItem = Items[i];
			if (!slotItem.IsHidden)
			{
				slotItem.Update();
				int num = ((_flashUntil[i] > milliseconds) ? 2 : ((slotItem.IsHovered || slotItem.Icon.IsHovered) ? 1 : 0));
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
		UpdateRights(me);
		UpdateFooter(me, bankSlots, milliseconds);
	}

	private string Signature(Item?[] slots, long now)
	{
		string[] array = new string[Items.Count];
		for (int i = 0; i < Items.Count; i++)
		{
			Item item = ((i < slots.Length) ? slots[i] : null);
			bool flag = item != null && item.ItemId != Guid.Empty;
			Guid guid = (flag ? item.ItemId : Guid.Empty);
			int num = (flag ? item.Quantity : 0);
			if (_seen && flag && (guid != _slotItems[i] || num > _slotQuantities[i]))
			{
				_flashUntil[i] = now + 650;
			}
			_slotItems[i] = guid;
			_slotQuantities[i] = num;
			array[i] = (flag ? $"{guid}:{num}" : string.Empty);
		}
		_seen = true;
		return string.Join('|', array);
	}

	private void Relayout(Item?[] slots)
	{
		bool flag = _category == BankCategory.All && _query.Length == 0;
		List<int> list = new List<int>();
		for (int i = 0; i < Items.Count; i++)
		{
			Item item = ((i < slots.Length) ? slots[i] : null);
			if (item == null || !DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var databaseObject))
			{
				if (flag)
				{
					list.Add(i);
				}
			}
			else if (flag || Matches(databaseObject))
			{
				list.Add(i);
			}
		}
		foreach (SlotItem item2 in Items)
		{
			item2.IsHidden = true;
		}
		int num = 2;
		int num2 = Math.Max(0, (_grid.Width - (400 - num)) / 2);
		for (int j = 0; j < list.Count; j++)
		{
			SlotItem slotItem = Items[list[j]];
			slotItem.IsHidden = false;
			slotItem.SetPosition(num2 + j % 10 * 40, j / 10 * 40);
		}
		_noMatch.IsHidden = flag || list.Count > 0;
	}

	private bool Matches(ItemDescriptor item)
	{
		bool flag;
		switch (_category)
		{
		case BankCategory.Equipment:
			flag = item.ItemType == ItemType.Equipment;
			break;
		case BankCategory.Consumables:
			flag = item.ItemType == ItemType.Consumable;
			break;
		case BankCategory.Techniques:
			flag = item.ItemType == ItemType.Spell;
			break;
		case BankCategory.Resources:
		{
			ItemType itemType = item.ItemType;
			bool flag2 = ((itemType == ItemType.None || itemType == ItemType.Currency) ? true : false);
			flag = flag2;
			break;
		}
		case BankCategory.Quest:
			flag = item.ItemType == ItemType.Event;
			break;
		case BankCategory.Bags:
			flag = item.ItemType == ItemType.Bag;
			break;
		default:
			flag = true;
			break;
		}
		if (flag)
		{
			if (_query.Length != 0)
			{
				return Normalize(item.Name).Contains(_query);
			}
			return true;
		}
		return false;
	}

	private void SelectCategory(BankCategory category)
	{
		_category = category;
		_dirty = true;
		RefreshCategoryButtons();
	}

	private void RefreshCategoryButtons()
	{
		foreach (var categoryButton in _categoryButtons)
		{
			Button item = categoryButton.Button;
			ImagePanel item2 = categoryButton.Glyph;
			BankCategory item3 = categoryButton.Category;
			bool flag = item3 == _category;
			item.SetStateTexture(ComponentState.Normal, flag ? "inv_categorie_active.png" : "inv_categorie.png");
			item.SetStateTexture(ComponentState.Hovered, flag ? "inv_categorie_active.png" : "inv_categorie_survol.png");
			item.SetStateTexture(ComponentState.Active, "inv_categorie_active.png");
			item2.RenderColor = (flag ? Night : Muted);
		}
	}

	private static string Normalize(string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}
		string source = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
		return new string(source.Where((char c) => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
	}

	private static List<int> StorableResources(Player me)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < me.Inventory.Length; i++)
		{
			IItem item = me.Inventory[i];
			if (item != null && !me.MyEquipment.Contains(i) && DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var databaseObject) && databaseObject.ItemType == ItemType.None && (Globals.IsGuildBank ? databaseObject.CanGuildBank : databaseObject.CanBank))
			{
				list.Add(i);
			}
		}
		return list;
	}

	private static bool Allowed(Player me, Func<GuildPermissions, bool> permission)
	{
		if (Globals.IsGuildBank)
		{
			if (!string.IsNullOrWhiteSpace(me.Guild))
			{
				if (me.Rank != 0)
				{
					GuildRank guildRank = me.GuildRank;
					if (guildRank != null)
					{
						return permission(guildRank.Permissions);
					}
					return false;
				}
				return true;
			}
			return false;
		}
		return true;
	}

	public static bool CanDeposit(Player me)
	{
		return Allowed(me, (GuildPermissions p) => p.BankDeposit);
	}

	public static bool CanWithdraw(Player me)
	{
		return Allowed(me, (GuildPermissions p) => p.BankRetrieve);
	}

	public static bool CanMove(Player me)
	{
		return Allowed(me, (GuildPermissions p) => p.BankMove);
	}

	private void StoreResources()
	{
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		if (!CanDeposit(me))
		{
			Refuse(Refusal.Deposit);
			return;
		}
		foreach (int item2 in StorableResources(me))
		{
			IItem item = me.Inventory[item2];
			if (item != null)
			{
				me.TryStoreItemInBank(item2, null, -1, item.Quantity, skipPrompt: true);
			}
		}
	}

	private void UpdateRights(Player me)
	{
		if (_rightsCard != null && _rankTitle != null && _guildName != null)
		{
			_rankTitle.Text = me.GuildRank?.Title ?? string.Empty;
			_guildName.Text = me.Guild ?? string.Empty;
			bool[] array = new bool[3]
			{
				CanDeposit(me),
				CanWithdraw(me),
				CanMove(me)
			};
			Func<GuildPermissions, bool>[] array2 = new Func<GuildPermissions, bool>[3]
			{
				(GuildPermissions p) => p.BankDeposit,
				(GuildPermissions p) => p.BankRetrieve,
				(GuildPermissions p) => p.BankMove
			};
			for (int num = 0; num < _rights.Count; num++)
			{
				(ImagePanel Dot, Label Name, Label Detail) tuple = _rights[num];
				ImagePanel item = tuple.Dot;
				Label item2 = tuple.Name;
				Label item3 = tuple.Detail;
				item.RenderColor = (array[num] ? Green : Red);
				item2.SetTextColor(array[num] ? White : Entry, ComponentState.Normal);
				item3.SetTextColor(array[num] ? Green : Dim, ComponentState.Normal);
				item3.Text = (array[num] ? Strings.Bank.RightAllowed.ToString() : AllowedRanks(array2[num]));
			}
		}
	}

	private static string AllowedRanks(Func<GuildPermissions, bool> permission)
	{
		GuildRank[] ranks = Options.Instance.Guild.Ranks;
		List<string> list = (from rank in ranks.Where((GuildRank rank, int index) => index == 0 || permission(rank.Permissions))
			select rank.Title).ToList();
		switch (list.Count)
		{
		case 0:
			return Strings.Bank.RanksNobody.ToString();
		case 1:
			return Strings.Bank.RanksReserved.ToString(list[0]);
		default:
		{
			LocalizedString ranksReserved = Strings.Bank.RanksReserved;
			object[] array = new object[1];
			LocalizedString ranksLast = Strings.Bank.RanksLast;
			object[] obj = new object[2]
			{
				string.Join(", ", list.Take(list.Count - 1)),
				null
			};
			obj[1] = list[list.Count - 1];
			array[0] = ranksLast.ToString(obj);
			return ranksReserved.ToString(array);
		}
		}
	}

	private void UpdateFooter(Player me, Item?[] slots, long now)
	{
		bool flag = now < _refusalUntil;
		if (flag != _showingRefusal)
		{
			_showingRefusal = flag;
			_capacityIcon.RenderColor = (flag ? Red : Muted);
			_capacityLabel.SetTextColor(flag ? Red : Muted, ComponentState.Normal);
		}
		int num = slots.Count((Item content) => content != null && content.ItemId != Guid.Empty);
		int num2 = Math.Max(1, Items.Count);
		_capacityLabel.Text = (flag ? _refusal : Strings.Personnage.PlacesUsed.ToString(num, num2));
		int num3 = (int)Math.Round(150.0 * (double)Math.Min(num, num2) / (double)num2);
		_capacityFill.IsHidden = num3 < 8;
		_capacityFill.SetSize(num3, 8);
		_capacityFill.TextureFilename = ((num >= num2) ? "saiyan_bar_fill_done.png" : "saiyan_bar_fill.png");
		List<int> list = StorableResources(me);
		string text = list.Count switch
		{
			0 => Strings.Bank.StoreNone.ToString(), 
			1 => Strings.Bank.StoreOne.ToString(), 
			_ => Strings.Bank.StoreMany.ToString(list.Count), 
		};
		if (text != _storeText)
		{
			_storeText = text;
			_store.Text = text;
		}
		_store.IsDisabled = list.Count == 0 || !CanDeposit(me);
		(Interface.GameUi?.GameMenu?.GetInventoryWindow())?.SetHighlightedSlots((_store.IsHovered && !_store.IsDisabled) ? list : null);
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
		_close.SetPosition(440, Y(16));
		for (int i = 0; i < _categoryButtons.Count; i++)
		{
			_categoryButtons[i].Button.SetPosition(16 + i * 40, Y(72));
		}
		_searchBack.SetPosition(314, Y(76));
		int num = _rows * 40 - 2;
		int num2 = num + 16;
		_gridCard.SetSize(448, num2);
		_gridCard.SetPosition(16, Y(116));
		_grid.SetSize(432, num);
		_grid.SetPosition(24, Y(124));
		_noMatch.SetSize(448, num2);
		_noMatch.SetPosition(16, Y(116));
		if (_rightsCard != null)
		{
			int num3 = 116 + num2 + 12;
			_rightsCard.SetSize(448, 530 - num3);
			_rightsCard.SetPosition(16, Y(num3));
		}
		_capacityIcon.SetPosition(22, Y(543));
		_capacityLabel.SetSize(228, 20);
		_capacityLabel.SetPosition(46, Y(542));
		_capacityTrack.SetPosition(22, Y(568));
		_capacityFill.SetPosition(22, Y(568));
		_store.SetPosition(284, Y(546));
		_dirty = true;
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

	private static void StyleScroll(ScrollControl scroll)
	{
		scroll.VerticalScrollBar.SetBackgroundTemplate(null, "saiyan_scroll_bg.png");
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_scroll_bar.png");
		IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_scroll_bar_hover.png");
		scroll.VerticalScrollBar.SetScrollBarImage(texture, "saiyan_scroll_bar.png", ComponentState.Normal);
		scroll.VerticalScrollBar.SetScrollBarImage(texture2, "saiyan_scroll_bar_hover.png", ComponentState.Hovered);
		scroll.VerticalScrollBar.SetScrollBarImage(texture2, "saiyan_scroll_bar_hover.png", ComponentState.Active);
	}
}
