using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.DragDrop;
using Intersect.Client.Framework.Items;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Items;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Models;

namespace Intersect.Client.Interface.Game.Trades;

public class TradingWindow
{
	private sealed class SlotView
	{
		public required ImagePanel Box;

		public required ImagePanel Icon;

		public required Label Quantity;

		public ImagePanel? Glow;

		public int Slot = -1;

		public int GridX;

		public int GridY;

		public Guid ItemId;

		public int Amount;
	}

	private sealed class OfferView
	{
		public required int Side;

		public required int X;

		public required ImagePanel Ready;

		public required Label Title;

		public required HexPortrait Portrait;

		public required Label Name;

		public required ImagePanel Chip;

		public required ImagePanel ChipCheck;

		public required Label ChipText;

		public required ScrollControl Grid;

		public readonly List<SlotView> Slots = new List<SlotView>();

		public required Label ZeniCaption;

		public required ImagePanel ZeniCoin;

		public required Label ValueCaption;

		public required Label Value;

		public required Label ItemsValue;
	}

	private sealed class TradeIcon : Draggable
	{
		public Func<bool>? CanDrag;

		public Action? DragStarted;

		public Action<int, int>? Dropped;

		public TradeIcon(Base parent, string name)
			: base(parent, name)
		{
		}

		public override bool DragAndDrop_Draggable()
		{
			return CanDrag?.Invoke() ?? false;
		}

		public override void DragAndDrop_StartDragging(Package package, int x, int y)
		{
			base.DragAndDrop_StartDragging(package, x, y);
			DragStarted?.Invoke();
		}

		public override void DragAndDrop_EndDragging(bool success, int x, int y)
		{
			base.DragAndDrop_EndDragging(success, x, y);
			Dropped?.Invoke(x, y);
		}
	}

	private sealed class ZeniField : TextBox
	{
		public ZeniField(Base parent, string name)
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

	private const int WindowWidth = 1000;

	private const int WindowHeight = 620;

	private const int Pitch = 44;

	private const int Cell = 42;

	private const int InventoryColumns = 7;

	private const int InventoryVisibleRows = 6;

	private const int OfferColumns = 6;

	private const int OfferVisibleRows = 3;

	private const int GaugeX = 225;

	private const int GaugeWidth = 250;

	private const long ValidateLockMs = 800L;

	private const long GlowMs = 2500L;

	private const long ZeniTypingDelayMs = 600L;

	private static readonly Color White = new Color(255, 242, 242, 242);

	private static readonly Color Entry = new Color(255, 196, 204, 220);

	private static readonly Color Muted = new Color(255, 176, 186, 206);

	private static readonly Color Dim = new Color(255, 130, 140, 162);

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private static readonly Color Orange = new Color(255, 255, 122, 26);

	private static readonly Color OrangeLight = new Color(255, 255, 172, 82);

	private static readonly Color Gold = new Color(255, 245, 190, 90);

	private static readonly Color GoldLight = new Color(255, 255, 226, 150);

	private static readonly Color GreenLight = new Color(255, 140, 235, 170);

	private readonly WindowControl _window;

	private readonly string _traderName;

	private readonly IFont? _body;

	private readonly IFont? _strong;

	private readonly IFont? _poster;

	private readonly Label _title;

	private readonly Label _hint;

	private readonly Button _close;

	private readonly Label _inventoryTitle;

	private readonly Label _inventoryCount;

	private readonly ScrollControl _inventoryGrid;

	private readonly List<SlotView> _inventorySlots = new List<SlotView>();

	private readonly ImagePanel _walletCoin;

	private readonly Label _wallet;

	private readonly Label _walletOffered;

	private readonly OfferView _mine;

	private readonly OfferView _theirs;

	private readonly ZeniField _zeniField;

	private readonly Button _plusThousand;

	private readonly Button _max;

	private readonly ImagePanel _theirZeniField;

	private readonly Label _theirZeni;

	private readonly ImagePanel _swap;

	private readonly Label _balanceTitle;

	private readonly Label _giveCaption;

	private readonly Label _give;

	private readonly Label _receiveCaption;

	private readonly Label _receive;

	private readonly Label _gap;

	private readonly ImagePanel _gaugeTrack;

	private readonly ImagePanel _gaugeMine;

	private readonly ImagePanel _gaugeTheirs;

	private readonly ImagePanel _gaugeMarker;

	private readonly Button _cancel;

	private readonly Button _validate;

	private readonly int[] _theirSeen = new int[Options.Instance.Player.MaxInventory];

	private readonly long[] _glowUntil = new long[Options.Instance.Player.MaxInventory];

	private int _laidOutTitleBarHeight = -1;

	private bool _zeniDirty;

	private long _zeniEditedAt;

	private int _gaugeSplit = -1;

	private string _chipsShown = string.Empty;

	public int X => _window.X;

	public int Y => _window.Y;

	public TradingWindow(Canvas gameCanvas, string traderName)
	{
		_traderName = traderName;
		GameContentManager current = GameContentManager.Current;
		_body = current.GetFont("chakrapetch");
		_strong = current.GetFont("rajdhanibold");
		_poster = current.GetFont("refuel");
		_window = new WindowControl(gameCanvas, string.Empty, modal: false, "TradeWindowSaiyan");
		_window.DisableResizing();
		_window.IsClosable = false;
		_window.SetSize(1000, 620);
		IGameTexture texture = current.GetTexture(TextureType.Gui, "saiyan_echange_fenetre.png");
		_window.SetImage(texture, "saiyan_echange_fenetre.png", WindowControl.ControlState.Active);
		_window.SetImage(texture, "saiyan_echange_fenetre.png", WindowControl.ControlState.Inactive);
		if (_window.Titlebar is Titlebar titlebar)
		{
			titlebar.Label.IsHidden = true;
		}
		Interface.InputBlockingComponents.Add(_window);
		_title = MakeLabel(_window, "Title", 22, White, _poster);
		_title.Text = Strings.TradeWindow.Title.ToString(traderName).ToUpperInvariant();
		WindowTitle.Apply(_title);
		_hint = MakeLabel(_window, "Hint", 10, Dim, _body);
		_hint.Text = Strings.TradeWindow.Hint;
		_close = new Button(_window, "Close", disableText: true);
		_close.SetStateTexture(ComponentState.Normal, "saiyan_close.png");
		_close.SetStateTexture(ComponentState.Hovered, "saiyan_close_hover.png");
		_close.SetStateTexture(ComponentState.Active, "saiyan_close_clicked.png");
		_close.Clicked += delegate
		{
			Hide();
		};
		_inventoryTitle = MakeLabel(_window, "InventoryTitle", 10, Muted, _body);
		_inventoryTitle.Text = Strings.TradeWindow.YourInventory;
		_inventoryCount = MakeLabel(_window, "InventoryCount", 9, Dim, _body);
		_inventoryCount.TextAlign = Pos.Right | Pos.CenterV;
		_inventoryGrid = MakeScroll(_window, "InventoryGrid");
		for (int num = 0; num < Options.Instance.Player.MaxInventory; num++)
		{
			SlotView item = MakeSlot(_inventoryGrid, $"Inventory{num}", num % 7 * 44, num / 7 * 44, withGlow: false, delegate(int slot)
			{
				Globals.Me?.TryOfferItemToTrade(slot);
			}, (int x, int y) => Contains(_mine.Ready, 0, 0, 300, 440, x, y));
			_inventorySlots.Add(item);
		}
		_walletCoin = MakeImage(_window, "WalletCoin", null, 26, 26);
		_wallet = MakeLabel(_window, "Wallet", 16, GoldLight, _strong);
		_walletOffered = MakeLabel(_window, "WalletOffered", 10, Dim, _body);
		_walletOffered.TextAlign = Pos.Right | Pos.CenterV;
		_mine = MakeOffer(0, 378, Strings.TradeWindow.YourOffer, Globals.Me?.Name ?? string.Empty);
		_theirs = MakeOffer(1, 690, Strings.TradeWindow.TheirOffer, traderName);
		_zeniField = new ZeniField(_window, "ZeniField")
		{
			Font = _strong,
			FontSize = 14,
			TextAlign = (Pos.Left | Pos.CenterV),
			MaximumLength = 12,
			Padding = new Padding(40, 0, 100, 0)
		};
		_zeniField.SetTextColor(GoldLight, ComponentState.Normal);
		IGameTexture texture2 = current.GetTexture(TextureType.Gui, "saiyan_echange_champ.png");
		if (texture2 != null)
		{
			_zeniField.SetBackgroundTemplate(texture2, "saiyan_echange_champ.png");
		}
		_zeniField.Text = "0";
		_zeniField.TextChanged += delegate
		{
			_zeniDirty = true;
			_zeniEditedAt = Timing.Global.Milliseconds;
		};
		_zeniField.SubmitPressed += delegate
		{
			FlushZeni();
		};
		_mine.ZeniCoin.BringToFront();
		_plusThousand = MakeMiniButton("PlusThousand", Strings.TradeWindow.PlusThousand);
		_plusThousand.Clicked += delegate
		{
			OfferZeni(ReadZeniField() + 1000);
		};
		_max = MakeMiniButton("Max", Strings.TradeWindow.Max);
		_max.Clicked += delegate
		{
			OfferZeni(AvailableZeni());
		};
		_theirZeniField = MakeImage(_window, "TheirZeniField", "saiyan_echange_champ_lecture.png", 276, 38);
		_theirs.ZeniCoin.BringToFront();
		_theirZeni = MakeLabel(_window, "TheirZeni", 14, GoldLight, _strong);
		_swap = MakeImage(_window, "Swap", "saiyan_echange_rond.png", 34, 34);
		_balanceTitle = MakeLabel(_window, "BalanceTitle", 10, Muted, _body);
		_balanceTitle.Text = Strings.TradeWindow.Balance;
		_giveCaption = MakeLabel(_window, "GiveCaption", 10, Muted, _body);
		_giveCaption.Text = Strings.TradeWindow.YouGive;
		_give = MakeLabel(_window, "Give", 13, OrangeLight, _strong);
		_receiveCaption = MakeLabel(_window, "ReceiveCaption", 10, Muted, _body);
		_receiveCaption.Text = Strings.TradeWindow.YouReceive;
		_receive = MakeLabel(_window, "Receive", 13, GreenLight, _strong);
		_gap = MakeLabel(_window, "Gap", 10, Muted, _body);
		_gap.TextAlign = Pos.Center;
		_gaugeTrack = MakeImage(_window, "GaugeTrack", "saiyan_echange_balance_piste.png", 250, 8);
		_gaugeMine = MakeImage(_window, "GaugeMine", "saiyan_echange_balance_moi.png", 250, 8);
		_gaugeTheirs = MakeImage(_window, "GaugeTheirs", "saiyan_echange_balance_lui.png", 250, 8);
		_gaugeMarker = MakeImage(_window, "GaugeMarker", "saiyan_echange_balance_repere.png", 2, 14);
		_cancel = MakeButton("Cancel", "saiyan_echange_annuler", 130, 44, Entry, White);
		_cancel.Text = Strings.TradeWindow.Cancel;
		_cancel.Clicked += delegate
		{
			Hide();
		};
		_validate = MakeButton("Validate", "saiyan_echange_valider", 192, 44, Night, Night);
		_validate.SetStateTexture(ComponentState.Disabled, "saiyan_echange_valider_disabled.png");
		_validate.SetTextColor(GreenLight, ComponentState.Disabled);
		_validate.Text = Strings.TradeWindow.Validate;
		_validate.Clicked += delegate
		{
			Validate();
		};
		Layout();
		_window.SetPosition((gameCanvas.Width - 1000) / 2, Math.Max(0, (gameCanvas.Height - 620) / 2));
		_window.Show();
		_window.BringToFront();
		Update();
	}

	public void Close()
	{
		Interface.GameUi?.ItemDescriptionWindow?.Hide();
		Interface.InputBlockingComponents.Remove(_window);
		_window.Close();
	}

	public bool IsVisible()
	{
		return !_window.IsHidden;
	}

	public void Hide()
	{
		Interface.GameUi?.ItemDescriptionWindow?.Hide();
		_window.IsHidden = true;
	}

	public FloatRect RenderBounds()
	{
		Point point = _window.ToCanvas(new Point(0, 0));
		return new FloatRect(point.X, point.Y, _window.Width, _window.Height);
	}

	public void Update()
	{
		Player me = Globals.Me;
		if (_window.IsHidden || me == null || Globals.Trade == null)
		{
			return;
		}
		if (_window.TitleBarBounds.Height != _laidOutTitleBarHeight)
		{
			Layout();
		}
		long milliseconds = Timing.Global.Milliseconds;
		int num = 0;
		for (int i = 0; i < Options.Instance.Player.MaxInventory; i++)
		{
			IItem item = me.Inventory[i];
			if (item != null && !(item.ItemId == Guid.Empty) && item.Quantity > 0 && DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var _))
			{
				Fill(_inventorySlots[num], i, item.ItemId, item.Quantity);
				num++;
			}
		}
		int num2 = num;
		int num3 = Math.Max(42, (num + 7 - 1) / 7 * 7);
		for (int j = num; j < _inventorySlots.Count; j++)
		{
			Fill(_inventorySlots[j], -1, Guid.Empty, 0);
			Place(_inventorySlots[j], j < num3);
		}
		for (int k = 0; k < num; k++)
		{
			Place(_inventorySlots[k], visible: true);
		}
		Player.Charge charge = me.LireLaCharge();
		_inventoryCount.Text = Strings.Inventaire.ChargeCount.ToString(SaiyanHud.Amount(charge.Porte), SaiyanHud.Amount(charge.Capacite));
		long num4 = UpdateOffer(_mine, milliseconds);
		long num5 = UpdateOffer(_theirs, milliseconds);
		long value = AvailableZeni();
		if (_zeniDirty && milliseconds - _zeniEditedAt >= 600)
		{
			FlushZeni();
		}
		if (!_zeniDirty && !_zeniField.HasFocus && _zeniField.Text != TradeState.MyZeni.ToString())
		{
			_zeniField.Text = TradeState.MyZeni.ToString();
			_zeniDirty = false;
		}
		_theirZeni.Text = SaiyanHud.Amount(TradeState.TheirZeni);
		_wallet.Text = Strings.TradeWindow.Wallet.ToString(SaiyanHud.Amount(value));
		_walletOffered.Text = ((TradeState.MyZeni > 0) ? Strings.TradeWindow.WalletOffered.ToString(SaiyanHud.Amount(TradeState.MyZeni)) : string.Empty);
		IGameTexture texture = CurrencyIcon();
		_walletCoin.Texture = texture;
		_mine.ZeniCoin.Texture = texture;
		_theirs.ZeniCoin.Texture = texture;
		long num6 = num4 + TradeState.MyZeni;
		long num7 = num5 + TradeState.TheirZeni;
		SetValue(_mine, num6, num4);
		SetValue(_theirs, num7, num5);
		_give.Text = Strings.TradeWindow.Amount.ToString(SaiyanHud.Amount(num6));
		_receive.Text = Strings.TradeWindow.Amount.ToString(SaiyanHud.Amount(num7));
		long num8 = num7 - num6;
		if (num8 > 0)
		{
			_gap.Text = Strings.TradeWindow.InYourFavor.ToString(SaiyanHud.Amount(num8));
			_gap.SetTextColor(GreenLight, ComponentState.Normal);
		}
		else if (num8 < 0)
		{
			_gap.Text = Strings.TradeWindow.InTheirFavor.ToString(SaiyanHud.Amount(-num8));
			_gap.SetTextColor(OrangeLight, ComponentState.Normal);
		}
		else
		{
			_gap.Text = Strings.TradeWindow.Even;
			_gap.SetTextColor(Muted, ComponentState.Normal);
		}
		long num9 = num6 + num7;
		int num10 = ((num9 <= 0) ? 125 : ((int)Math.Round(250.0 * (double)num6 / (double)num9)));
		SetGauge((num9 <= 0) ? (-1) : num10);
		string text = $"{TradeState.MyReady}:{TradeState.TheirReady}";
		if (text != _chipsShown)
		{
			_chipsShown = text;
			SetReady(_mine, TradeState.MyReady);
			SetReady(_theirs, TradeState.TheirReady);
		}
		bool flag = TradeState.RevisionChangedAt > 0 && milliseconds - TradeState.RevisionChangedAt < 800;
		_validate.IsDisabled = TradeState.MyReady || flag;
		_validate.Text = (TradeState.MyReady ? Strings.TradeWindow.Validated : Strings.TradeWindow.Validate);
		_max.IsHidden = false;
		_plusThousand.IsHidden = false;
	}

	private long UpdateOffer(OfferView offer, long now)
	{
		long num = 0L;
		int num2 = 0;
		for (int i = 0; i < Options.Instance.Player.MaxInventory; i++)
		{
			Item item = Globals.Trade[offer.Side, i];
			int num3 = ((item != null && item.ItemId != Guid.Empty) ? item.Quantity : 0);
			if (offer.Side == 1)
			{
				if (num3 > _theirSeen[i])
				{
					_glowUntil[i] = now + 2500;
				}
				_theirSeen[i] = num3;
			}
			if (num3 <= 0 || item == null || !DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var databaseObject))
			{
				continue;
			}
			num += (long)databaseObject.Price * (long)num3;
			SlotView slotView = offer.Slots[num2];
			Fill(slotView, i, item.ItemId, num3);
			if (slotView.Glow != null)
			{
				bool flag = _glowUntil[i] > now;
				PlaceGlow(slotView, flag);
				if (flag)
				{
					float num4 = 0.6f + 0.4f * MathF.Sin((float)now / 110f);
					slotView.Glow.RenderColor = new Color((int)(255f * num4), 255, 255, 255);
				}
			}
			Place(slotView, visible: true);
			num2++;
		}
		int num5 = Math.Max(18, (num2 + 6 - 1) / 6 * 6);
		for (int j = num2; j < offer.Slots.Count; j++)
		{
			SlotView view = offer.Slots[j];
			Fill(view, -1, Guid.Empty, 0);
			Place(view, j < num5);
			PlaceGlow(view, visible: false);
		}
		return num;
	}

	private void Fill(SlotView view, int slot, Guid itemId, int amount)
	{
		view.Slot = slot;
		view.Amount = amount;
		if (view.ItemId != itemId)
		{
			view.ItemId = itemId;
			view.Icon.Texture = ((itemId != Guid.Empty && DatabaseObject<ItemDescriptor>.TryGet(itemId, out var databaseObject)) ? GameContentManager.Current.GetTexture(TextureType.Item, databaseObject.Icon) : null);
		}
		if (!(view.Icon is TradeIcon { IsDragging: not false }))
		{
			view.Icon.IsHidden = itemId == Guid.Empty;
		}
		string text = ((amount > 1) ? Strings.FormatQuantityAbbreviated(amount) : string.Empty);
		if (view.Quantity.Text != text)
		{
			view.Quantity.Text = text;
		}
	}

	private static void Place(SlotView view, bool visible)
	{
		if (view.Box.IsHidden == visible)
		{
			view.Box.IsHidden = !visible;
			view.Box.SetPosition(visible ? view.GridX : 0, visible ? view.GridY : 0);
		}
	}

	private static void PlaceGlow(SlotView view, bool visible)
	{
		if (view.Glow != null && view.Glow.IsHidden == visible)
		{
			view.Glow.IsHidden = !visible;
			view.Glow.SetPosition(visible ? (view.GridX - 4) : 0, visible ? (view.GridY - 4) : 0);
		}
	}

	private void SetValue(OfferView offer, long total, long items)
	{
		offer.Value.Text = Strings.TradeWindow.Amount.ToString(SaiyanHud.Amount(total));
		offer.ItemsValue.Text = Strings.TradeWindow.ItemsValue.ToString(SaiyanHud.Amount(items));
	}

	private void SetReady(OfferView offer, bool ready)
	{
		offer.Ready.IsHidden = !ready;
		offer.Chip.Texture = GameContentManager.Current.GetTexture(TextureType.Gui, ready ? "saiyan_echange_puce_pret.png" : "saiyan_echange_puce_attente.png");
		offer.ChipCheck.IsHidden = !ready;
		offer.ChipText.Text = (ready ? Strings.TradeWindow.Ready : Strings.TradeWindow.Waiting);
		offer.ChipText.SetTextColor(ready ? GreenLight : Dim, ComponentState.Normal);
		LayoutChip(offer);
	}

	private void SetGauge(int split)
	{
		if (split != _gaugeSplit)
		{
			_gaugeSplit = split;
			bool flag = split < 0;
			int num = ((!flag) ? Math.Clamp(split, 0, 250) : 0);
			int num2 = 250 - num;
			_gaugeMine.IsHidden = flag || num <= 0;
			if (!_gaugeMine.IsHidden)
			{
				_gaugeMine.SetTextureRect(0, 0, num, 8);
				_gaugeMine.SetSize(num, 8);
			}
			_gaugeTheirs.IsHidden = flag || num2 <= 0;
			if (!_gaugeTheirs.IsHidden)
			{
				_gaugeTheirs.SetTextureRect(num, 0, num2, 8);
				_gaugeTheirs.SetSize(num2, 8);
				_gaugeTheirs.SetPosition(225 + num, FrameY(578));
			}
			_gaugeMarker.SetPosition(225 + (flag ? 125 : num) - 1, FrameY(575));
		}
	}

	private long AvailableZeni()
	{
		return (Globals.Me?.Zeni ?? 0) + TradeState.MyZeni;
	}

	private long ReadZeniField()
	{
		if (!long.TryParse(_zeniField.Text, out var result))
		{
			return 0L;
		}
		return result;
	}

	private void FlushZeni()
	{
		if (_zeniDirty)
		{
			OfferZeni(ReadZeniField());
		}
	}

	private void OfferZeni(long amount)
	{
		_zeniDirty = false;
		amount = Math.Clamp(amount, 0L, AvailableZeni());
		_zeniField.Text = amount.ToString();
		_zeniDirty = false;
		if (amount != TradeState.MyZeni)
		{
			PacketSender.SendOfferTradeZeni(amount);
		}
	}

	private void Validate()
	{
		if (_zeniDirty)
		{
			FlushZeni();
		}
		else if (!TradeState.MyReady)
		{
			PacketSender.SendAcceptTrade(TradeState.Revision);
		}
	}

	private IGameTexture? CurrencyIcon()
	{
		Guid id = Globals.Me?.CurrencyItemId ?? Guid.Empty;
		if (!DatabaseObject<ItemDescriptor>.TryGet(id, out var databaseObject))
		{
			return null;
		}
		return GameContentManager.Current.GetTexture(TextureType.Item, databaseObject.Icon);
	}

	private int FrameY(int windowY)
	{
		return windowY - _laidOutTitleBarHeight;
	}

	private void Layout()
	{
		_laidOutTitleBarHeight = _window.TitleBarBounds.Height;
		_title.SetSize(700, 40);
		_title.SetPosition(24, FrameY(10));
		_hint.SetSize(760, 20);
		_hint.SetPosition(26, FrameY(50));
		_close.SetSize(24, 24);
		_close.SetPosition(956, FrameY(18));
		_inventoryTitle.SetSize(200, 24);
		_inventoryTitle.SetPosition(34, FrameY(88));
		_inventoryCount.SetSize(128, 24);
		_inventoryCount.SetPosition(220, FrameY(88));
		_inventoryGrid.SetSize(326, 300);
		_inventoryGrid.SetPosition(30, FrameY(118));
		_walletCoin.SetPosition(34, FrameY(447));
		_wallet.SetSize(170, 36);
		_wallet.SetPosition(66, FrameY(442));
		_walletOffered.SetSize(150, 36);
		_walletOffered.SetPosition(200, FrameY(442));
		LayoutOffer(_mine);
		LayoutOffer(_theirs);
		_zeniField.SetSize(276, 38);
		_zeniField.SetPosition(_mine.X + 12, FrameY(340));
		_mine.ZeniCoin.SetPosition(_mine.X + 20, FrameY(347));
		_max.SetPosition(_mine.X + 12 + 276 - 8 - _max.Width, FrameY(348));
		_plusThousand.SetPosition(_mine.X + 12 + 276 - 8 - _max.Width - 6 - _plusThousand.Width, FrameY(348));
		_theirZeniField.SetPosition(_theirs.X + 12, FrameY(340));
		_theirs.ZeniCoin.SetPosition(_theirs.X + 20, FrameY(347));
		_theirZeni.SetSize(220, 38);
		_theirZeni.SetPosition(_theirs.X + 52, FrameY(340));
		_swap.SetPosition(667, FrameY(262));
		_balanceTitle.SetSize(100, 20);
		_balanceTitle.SetPosition(34, FrameY(542));
		_giveCaption.SetSize(76, 30);
		_giveCaption.SetPosition(34, FrameY(562));
		_give.SetSize(110, 30);
		_give.SetPosition(108, FrameY(562));
		_gap.SetSize(250, 22);
		_gap.SetPosition(225, FrameY(550));
		_gaugeTrack.SetPosition(225, FrameY(578));
		_gaugeMine.SetPosition(225, FrameY(578));
		_receiveCaption.SetSize(66, 30);
		_receiveCaption.SetPosition(486, FrameY(562));
		_receive.SetSize(80, 30);
		_receive.SetPosition(548, FrameY(562));
		_gaugeSplit = -2;
		_cancel.SetPosition(646, FrameY(548));
		_validate.SetPosition(788, FrameY(548));
	}

	private void LayoutOffer(OfferView offer)
	{
		int x = offer.X;
		offer.Ready.SetPosition(x, FrameY(80));
		offer.Title.SetSize(200, 24);
		offer.Title.SetPosition(x + 14, FrameY(88));
		offer.Portrait.SetPosition(x + 12, FrameY(118));
		offer.Name.SetSize(140, 30);
		offer.Name.SetPosition(x + 58, FrameY(121));
		LayoutChip(offer);
		offer.Grid.SetSize(282, 136);
		offer.Grid.SetPosition(x + 10, FrameY(176));
		offer.ZeniCaption.SetSize(120, 20);
		offer.ZeniCaption.SetPosition(x + 14, FrameY(318));
		offer.ValueCaption.SetSize(140, 24);
		offer.ValueCaption.SetPosition(x + 14, FrameY(450));
		offer.Value.SetSize(166, 32);
		offer.Value.SetPosition(x + 120, FrameY(444));
		offer.ItemsValue.SetSize(272, 18);
		offer.ItemsValue.SetPosition(x + 14, FrameY(478));
	}

	private void LayoutChip(OfferView offer)
	{
		int num = MeasureWidth(_body, offer.ChipText.FontSize, offer.ChipText.Text);
		int num2 = ((!offer.ChipCheck.IsHidden) ? 16 : 0);
		int num3 = num + 18 + num2;
		int num4 = offer.X + 288;
		offer.Chip.SetSize(num3, 22);
		offer.Chip.SetPosition(num4 - num3, FrameY(125));
		offer.ChipCheck.SetPosition(num4 - num3 + 7, FrameY(131));
		offer.ChipText.SetSize(num + 6, 22);
		offer.ChipText.SetPosition(num4 - num3 + 7 + num2, FrameY(125));
	}

	private OfferView MakeOffer(int side, int x, string title, string name)
	{
		ImagePanel imagePanel = MakeImage(_window, $"Ready{side}", "saiyan_echange_pret.png", 300, 440);
		imagePanel.IsHidden = true;
		Label label = MakeLabel(_window, $"OfferTitle{side}", 10, Muted, _body);
		label.Text = title;
		HexPortrait hexPortrait = new HexPortrait(_window, $"Portrait{side}", 18)
		{
			Accent = ((side == 0) ? Orange : Gold)
		};
		hexPortrait.Entity = ((side == 0) ? Globals.Me : FindTrader());
		Label label2 = MakeLabel(_window, $"Name{side}", 15, White, _strong);
		label2.Text = name;
		ImagePanel imagePanel2 = MakeImage(_window, $"Chip{side}", "saiyan_echange_puce_attente.png", 24, 22);
		imagePanel2.TextureNinePatchMargin = new Margin(5);
		ImagePanel imagePanel3 = MakeImage(_window, $"ChipCheck{side}", "saiyan_suivi_coche.png", 12, 10);
		imagePanel3.RenderColor = GreenLight;
		imagePanel3.IsHidden = true;
		Label label3 = MakeLabel(_window, $"ChipText{side}", 9, Dim, _body);
		label3.Text = Strings.TradeWindow.Waiting;
		ScrollControl scrollControl = MakeScroll(_window, $"OfferGrid{side}");
		Label label4 = MakeLabel(_window, $"ZeniCaption{side}", 10, Muted, _body);
		label4.Text = Strings.TradeWindow.Zeni;
		ImagePanel zeniCoin = MakeImage(_window, $"ZeniCoin{side}", null, 24, 24);
		Label label5 = MakeLabel(_window, $"ValueCaption{side}", 10, Muted, _body);
		label5.Text = Strings.TradeWindow.TotalValue;
		Label label6 = MakeLabel(_window, $"Value{side}", 16, Gold, _strong);
		label6.TextAlign = Pos.Right | Pos.CenterV;
		Label label7 = MakeLabel(_window, $"ItemsValue{side}", 9, Dim, _body);
		label7.TextAlign = Pos.Right | Pos.CenterV;
		OfferView offerView = new OfferView
		{
			Side = side,
			X = x,
			Ready = imagePanel,
			Title = label,
			Portrait = hexPortrait,
			Name = label2,
			Chip = imagePanel2,
			ChipCheck = imagePanel3,
			ChipText = label3,
			Grid = scrollControl,
			ZeniCaption = label4,
			ZeniCoin = zeniCoin,
			ValueCaption = label5,
			Value = label6,
			ItemsValue = label7
		};
		for (int i = 0; i < Options.Instance.Player.MaxInventory; i++)
		{
			SlotView item = MakeSlot(scrollControl, $"Offer{side}_{i}", i % 6 * 44 + 4, i / 6 * 44 + 4, side == 1, (side == 0) ? ((Action<int>)delegate(int slot)
			{
				Globals.Me?.TryCancelOfferToTradeItem(slot);
			}) : null, (side == 0) ? ((Func<int, int, bool>)((int x2, int y) => Contains(_inventoryGrid, -10, -38, 342, 440, x2, y))) : null);
			offerView.Slots.Add(item);
		}
		return offerView;
	}

	private Player? FindTrader()
	{
		return Globals.Entities.Values.OfType<Player>().FirstOrDefault((Player player) => player != Globals.Me && player.Name == _traderName);
	}

	private SlotView MakeSlot(Base parent, string name, int x, int y, bool withGlow, Action<int>? action, Func<int, int, bool>? dropTarget)
	{
		ImagePanel imagePanel = null;
		if (withGlow)
		{
			imagePanel = MakeImage(parent, name + "Glow", "saiyan_echange_case_neuve.png", 50, 50);
			imagePanel.SetPosition(0, 0);
			imagePanel.IsHidden = true;
		}
		ImagePanel box = new ImagePanel(parent, name)
		{
			Texture = GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_echange_case.png"),
			TextureNinePatchMargin = new Margin(5)
		};
		box.SetSize(42, 42);
		box.SetPosition(x, y);
		TradeIcon icon = new TradeIcon(box, "Icon")
		{
			MouseInputEnabled = (action != null)
		};
		icon.SetSize(32, 32);
		icon.SetPosition(5, 5);
		Label label = MakeLabel(box, "Quantity", 10, White, _strong);
		label.TextAlign = Pos.Right | Pos.Bottom;
		label.SetSize(38, 40);
		label.SetPosition(0, 0);
		SlotView view = new SlotView
		{
			Box = box,
			Icon = icon,
			Quantity = label,
			Glow = imagePanel,
			GridX = x,
			GridY = y
		};
		IGameTexture normal = box.Texture;
		IGameTexture hover = GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_echange_case_survol.png");
		box.HoverEnter += delegate
		{
			Enter();
		};
		box.HoverLeave += delegate
		{
			Leave();
		};
		icon.HoverEnter += delegate
		{
			Enter();
		};
		icon.HoverLeave += delegate
		{
			Leave();
		};
		if (action != null)
		{
			box.Clicked += delegate
			{
				Act();
			};
			icon.Clicked += delegate
			{
				Act();
			};
			icon.CanDrag = () => view.Slot >= 0 && view.ItemId != Guid.Empty;
			icon.DragStarted = delegate
			{
				Interface.GameUi?.ItemDescriptionWindow?.Hide();
			};
			icon.Dropped = delegate(int dropX, int dropY)
			{
				if (dropTarget != null && dropTarget(dropX, dropY))
				{
					Act();
				}
			};
		}
		return view;
		void Act()
		{
			if (view.Slot >= 0)
			{
				action(view.Slot);
			}
		}
		void Enter()
		{
			box.Texture = hover;
			if (view.ItemId != Guid.Empty && !icon.IsDragging && DatabaseObject<ItemDescriptor>.TryGet(view.ItemId, out var databaseObject))
			{
				Interface.GameUi?.ItemDescriptionWindow?.Show(databaseObject, view.Amount, PropertiesOf(view, parent));
			}
		}
		void Leave()
		{
			box.Texture = normal;
			Interface.GameUi?.ItemDescriptionWindow?.Hide();
		}
	}

	private static bool Contains(Base reference, int left, int top, int width, int height, int x, int y)
	{
		Point point = reference.ToCanvas(left, top);
		if (x >= point.X && x < point.X + width && y >= point.Y)
		{
			return y < point.Y + height;
		}
		return false;
	}

	private ItemProperties? PropertiesOf(SlotView view, Base parent)
	{
		if (view.Slot < 0 || Globals.Me == null)
		{
			return null;
		}
		if (parent == _inventoryGrid)
		{
			return (Globals.Me.Inventory[view.Slot] as Item)?.ItemProperties;
		}
		int num = ((parent == _theirs?.Grid) ? 1 : 0);
		Item[,] trade = Globals.Trade;
		if (trade == null)
		{
			return null;
		}
		return trade[num, view.Slot]?.ItemProperties;
	}

	private static ScrollControl MakeScroll(Base parent, string name)
	{
		ScrollControl scrollControl = new ScrollControl(parent, name);
		scrollControl.EnableScroll(horizontal: false, vertical: true);
		scrollControl.VerticalScrollBar.SetBackgroundTemplate(null, "saiyan_scroll_bg.png");
		IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_scroll_bar.png");
		IGameTexture texture2 = GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_scroll_bar_hover.png");
		scrollControl.VerticalScrollBar.SetScrollBarImage(texture, "saiyan_scroll_bar.png", ComponentState.Normal);
		scrollControl.VerticalScrollBar.SetScrollBarImage(texture2, "saiyan_scroll_bar_hover.png", ComponentState.Hovered);
		scrollControl.VerticalScrollBar.SetScrollBarImage(texture2, "saiyan_scroll_bar_hover.png", ComponentState.Active);
		return scrollControl;
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

	private static ImagePanel MakeImage(Base parent, string name, string? texture, int width, int height)
	{
		ImagePanel imagePanel = new ImagePanel(parent, name)
		{
			MouseInputEnabled = false
		};
		if (texture != null)
		{
			imagePanel.Texture = GameContentManager.Current.GetTexture(TextureType.Gui, texture);
		}
		imagePanel.SetSize(width, height);
		return imagePanel;
	}

	private Button MakeButton(string name, string key, int width, int height, Color ink, Color hoverInk)
	{
		Button button = new Button(_window, name)
		{
			Font = _strong,
			FontSize = 13,
			TextAlign = Pos.Center,
			UsesHeadingFace = false
		};
		button.SetStateTexture(ComponentState.Normal, key + ".png");
		button.SetStateTexture(ComponentState.Hovered, key + "_hover.png");
		button.SetStateTexture(ComponentState.Active, key + "_clicked.png");
		button.SetTextColor(ink, ComponentState.Normal);
		button.SetTextColor(hoverInk, ComponentState.Hovered);
		button.SetTextColor(ink, ComponentState.Active);
		button.SetSize(width, height);
		return button;
	}

	private Button MakeMiniButton(string name, string text)
	{
		int val = MeasureWidth(_body, 8, text) + 16;
		Button button = new Button(_window, name)
		{
			Font = _body,
			FontSize = 9,
			TextAlign = Pos.Center,
			UsesHeadingFace = false,
			Text = text
		};
		button.SetStateTexture(ComponentState.Normal, "saiyan_echange_mini.png");
		button.SetStateTexture(ComponentState.Hovered, "saiyan_echange_mini_survol.png");
		button.SetStateTexture(ComponentState.Active, "saiyan_echange_mini_survol.png");
		button.SetTextColor(OrangeLight, ComponentState.Normal);
		button.SetTextColor(GoldLight, ComponentState.Hovered);
		button.SetTextColor(GoldLight, ComponentState.Active);
		button.SetSize(Math.Max(34, val), 22);
		return button;
	}

	private static int MeasureWidth(IFont? font, int size, string? text)
	{
		if (font != null && !string.IsNullOrEmpty(text))
		{
			GameRenderer renderer = Graphics.Renderer;
			if (renderer != null)
			{
				return (int)MathF.Ceiling(renderer.MeasureText(text, font, size, 1f).X);
			}
		}
		return 0;
	}
}
