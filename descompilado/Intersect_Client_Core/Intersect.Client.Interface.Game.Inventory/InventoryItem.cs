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
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.DragDrop;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Input;
using Intersect.Client.Framework.Items;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Bag;
using Intersect.Client.Interface.Game.Bank;
using Intersect.Client.Interface.Game.Hotbar;
using Intersect.Client.Interface.Game.Shop;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Configuration;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.GameObjects;
using Intersect.Localization;
using Intersect.Models;
using Intersect.Utilities;

namespace Intersect.Client.Interface.Game.Inventory;

public class InventoryItem : SlotItem
{
	private static readonly Color BadgeText = new Color(255, 176, 186, 206);

	private readonly Label _quantityLabel;

	private readonly Label _equipLabel;

	private readonly Label _cooldownLabel;

	private readonly ImagePanel _equipImageBackground;

	private readonly ImagePanel _marqueDeVente;

	private readonly InventoryWindow _inventoryWindow;

	private readonly MenuItem _useItemMenuItem;

	private readonly MenuItem _actionItemMenuItem;

	private readonly MenuItem _dropItemMenuItem;

	public InventoryItem(InventoryWindow inventoryWindow, Base parent, int index, ContextMenu contextMenu)
		: base(parent, "InventoryItem", index, contextMenu)
	{
		_inventoryWindow = inventoryWindow;
		base.TextureFilename = "inventoryitem.png";
		Icon.HoverEnter += Icon_HoverEnter;
		Icon.HoverLeave += Icon_HoverLeave;
		Icon.Clicked += Icon_Clicked;
		Icon.DoubleClicked += Icon_DoubleClicked;
		_equipImageBackground = new ImagePanel(this, "EquippedIcon")
		{
			Texture = Graphics.Renderer.WhitePixel
		};
		_marqueDeVente = new ImagePanel(this, "SellMark")
		{
			Texture = Graphics.Renderer.WhitePixel,
			RenderColor = new Color(52, 255, 208, 106),
			MouseInputEnabled = false,
			IsVisibleInParent = false
		};
		_equipLabel = new Label(this, "EquippedLabel")
		{
			IsVisibleInParent = false,
			Text = Strings.Inventory.EquippedSymbol,
			FontName = "chakrapetch",
			FontSize = 10,
			TextColor = BadgeText,
			Alignment = new Alignments[2]
			{
				Alignments.Right,
				Alignments.Top
			},
			BackgroundTemplateName = "equipped.png",
			Padding = new Padding(2)
		};
		_cooldownLabel = new Label(this, "CooldownLabel")
		{
			IsVisibleInParent = false,
			FontName = "chakrapetch",
			FontSize = 10,
			TextColor = BadgeText,
			Alignment = new Alignments[1] { Alignments.Center },
			BackgroundTemplateName = "quantity.png",
			Padding = new Padding(2)
		};
		_quantityLabel = new Label(this, "Quantity")
		{
			Alignment = new Alignments[2]
			{
				Alignments.Bottom,
				Alignments.Right
			},
			BackgroundTemplateName = "quantity.png",
			FontName = "chakrapetch",
			FontSize = 10,
			TextColor = BadgeText,
			Padding = new Padding(2)
		};
		LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
		contextMenu.ClearChildren();
		_useItemMenuItem = contextMenu.AddItem(Strings.ItemContextMenu.Use);
		_useItemMenuItem.Clicked += _useItemContextItem_Clicked;
		_dropItemMenuItem = contextMenu.AddItem(Strings.ItemContextMenu.Drop);
		_dropItemMenuItem.Clicked += _dropItemContextItem_Clicked;
		_actionItemMenuItem = contextMenu.AddItem(Strings.ItemContextMenu.Bank);
		_actionItemMenuItem.Clicked += _actionItemContextItem_Clicked;
		contextMenu.LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
		Player me = Globals.Me;
		if (me != null)
		{
			me.InventoryUpdated += PlayerOnInventoryUpdated;
		}
	}

	protected override void OnContextMenuOpening(ContextMenu contextMenu)
	{
		contextMenu.ClearChildren();
		Player? me = Globals.Me;
		IItem item = ((me != null) ? me.Inventory[SlotIndex] : null);
		if (item != null && DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var databaseObject))
		{
			switch (databaseObject.ItemType)
			{
			case ItemType.Spell:
			{
				contextMenu.AddChild(_useItemMenuItem);
				LocalizedString localizedString2 = (databaseObject.QuickCast ? Strings.ItemContextMenu.Cast : Strings.ItemContextMenu.Learn);
				_useItemMenuItem.Text = localizedString2.ToString(databaseObject.Name);
				break;
			}
			case ItemType.Consumable:
			case ItemType.Event:
				contextMenu.AddChild(_useItemMenuItem);
				_useItemMenuItem.Text = Strings.ItemContextMenu.Use.ToString(databaseObject.Name);
				break;
			case ItemType.Bag:
				contextMenu.AddChild(_useItemMenuItem);
				_useItemMenuItem.Text = Strings.ItemContextMenu.Open.ToString(databaseObject.Name);
				break;
			case ItemType.Equipment:
			{
				contextMenu.AddChild(_useItemMenuItem);
				LocalizedString localizedString = (Globals.Me.MyEquipment.Contains(SlotIndex) ? Strings.ItemContextMenu.Unequip : Strings.ItemContextMenu.Equip);
				_useItemMenuItem.Text = localizedString.ToString(databaseObject.Name);
				break;
			}
			}
			if (Globals.InBag && databaseObject.CanBag)
			{
				contextMenu.AddChild(_actionItemMenuItem);
				_actionItemMenuItem.SetText(Strings.ItemContextMenu.Bag.ToString(databaseObject.Name));
			}
			else if (Globals.InBank && (databaseObject.CanBank || databaseObject.CanGuildBank))
			{
				contextMenu.AddChild(_actionItemMenuItem);
				_actionItemMenuItem.SetText(Strings.ItemContextMenu.Bank.ToString(databaseObject.Name));
			}
			else if (Globals.InTrade && databaseObject.CanTrade)
			{
				contextMenu.AddChild(_actionItemMenuItem);
				_actionItemMenuItem.SetText(Strings.ItemContextMenu.Trade.ToString(databaseObject.Name));
			}
			else if (Globals.GameShop != null && databaseObject.CanSell)
			{
				contextMenu.AddChild(_actionItemMenuItem);
				_actionItemMenuItem.SetText(Strings.ItemContextMenu.Sell.ToString(databaseObject.Name));
			}
			if (databaseObject.CanDrop)
			{
				contextMenu.AddChild(_dropItemMenuItem);
				_dropItemMenuItem.SetText(Strings.ItemContextMenu.Drop.ToString(databaseObject.Name));
			}
			base.OnContextMenuOpening(contextMenu);
		}
	}

	private void _useItemContextItem_Clicked(Base sender, MouseButtonState arguments)
	{
		Globals.Me?.TryUseItem(SlotIndex);
	}

	private void _actionItemContextItem_Clicked(Base sender, MouseButtonState arguments)
	{
		if (Globals.GameShop != null)
		{
			Globals.Me?.TrySellItem(SlotIndex);
		}
		else if (Globals.InBank)
		{
			Globals.Me?.TryStoreItemInBank(SlotIndex);
		}
		else if (Globals.InBag)
		{
			Globals.Me?.TryStoreItemInBag(SlotIndex, -1);
		}
		else if (Globals.InTrade)
		{
			Globals.Me?.TryOfferItemToTrade(SlotIndex);
		}
	}

	private void _dropItemContextItem_Clicked(Base sender, MouseButtonState arguments)
	{
		Globals.Me?.TryDropItem(SlotIndex);
	}

	private void Icon_DoubleClicked(Base sender, MouseButtonState arguments)
	{
		if (Globals.Me == null)
		{
			return;
		}
		if (Globals.GameShop != null)
		{
			Globals.Me.TrySellItem(SlotIndex);
		}
		else if (Globals.InBank)
		{
			if (Globals.InputManager.IsKeyDown(Keys.Shift))
			{
				Globals.Me.TryStoreItemInBank(SlotIndex, null, -1, -1, skipPrompt: true);
				return;
			}
			IItem item = Globals.Me.Inventory[SlotIndex];
			Globals.Me.TryStoreItemInBank(SlotIndex, item, -1, item.Quantity);
		}
		else if (Globals.InBag)
		{
			Globals.Me.TryStoreItemInBag(SlotIndex, -1);
		}
		else if (Globals.InTrade)
		{
			Globals.Me.TryOfferItemToTrade(SlotIndex);
		}
		else
		{
			Globals.Me.TryUseItem(SlotIndex);
		}
	}

	private void Icon_Clicked(Base sender, MouseButtonState arguments)
	{
		if (arguments.MouseButton != MouseButton.Right)
		{
			return;
		}
		if (ClientConfiguration.Instance.EnableContextMenus)
		{
			OpenContextMenu();
			return;
		}
		Player me = Globals.Me;
		if (me != null)
		{
			if (Globals.GameShop != null)
			{
				me.TrySellItem(SlotIndex);
			}
			else if (Globals.InBank)
			{
				me.TryStoreItemInBank(SlotIndex);
			}
			else if (Globals.InBag)
			{
				me.TryStoreItemInBag(SlotIndex, -1);
			}
			else if (Globals.InTrade)
			{
				me.TryOfferItemToTrade(SlotIndex);
			}
			else
			{
				me.TryDropItem(SlotIndex);
			}
		}
	}

	private void Icon_HoverLeave(Base sender, EventArgs arguments)
	{
		Interface.GameUi.ItemDescriptionWindow?.Hide();
	}

	private void Icon_HoverEnter(Base? sender, EventArgs? arguments)
	{
		if (InputHandler.MouseFocus != null || Globals.InputManager.IsMouseButtonDown(MouseButton.Left))
		{
			return;
		}
		Player? me = Globals.Me;
		IItem item = ((me != null) ? me.Inventory[SlotIndex] : null);
		if (item == null)
		{
			return;
		}
		ItemDescriptor descriptor = item.Descriptor;
		if (descriptor == null)
		{
			return;
		}
		if (Globals.GameShop == null)
		{
			Interface.GameUi.ItemDescriptionWindow?.Show(descriptor, item.Quantity, item.ItemProperties);
			return;
		}
		ShopItemDescriptor shopItemDescriptor = null;
		for (int i = 0; i < Globals.GameShop.BuyingItems.Count; i++)
		{
			ShopItemDescriptor shopItemDescriptor2 = Globals.GameShop.BuyingItems[i];
			if (item.ItemId == shopItemDescriptor2.ItemId)
			{
				shopItemDescriptor = shopItemDescriptor2;
				break;
			}
		}
		if (Globals.GameShop.BuyingWhitelist && shopItemDescriptor != null)
		{
			if (DatabaseObject<ItemDescriptor>.TryGet(shopItemDescriptor.CostItemId, out var databaseObject))
			{
				Interface.GameUi.ItemDescriptionWindow?.Show(descriptor, item.Quantity, item.ItemProperties, Strings.Shop.SellsFor.ToString(shopItemDescriptor.CostItemQuantity, databaseObject.Name));
			}
		}
		else if (shopItemDescriptor == null)
		{
			ItemDescriptor defaultCurrency = Globals.GameShop.DefaultCurrency;
			if (descriptor != null && defaultCurrency != null)
			{
				Interface.GameUi.ItemDescriptionWindow?.Show(descriptor, item.Quantity, item.ItemProperties, Strings.Shop.SellsFor.ToString(descriptor.Price.ToString(), defaultCurrency.Name));
			}
		}
		else
		{
			Interface.GameUi.ItemDescriptionWindow?.Show(descriptor, item.Quantity, item.ItemProperties, Strings.Shop.WontBuy);
		}
	}

	public override bool DragAndDrop_HandleDrop(Package package, int x, int y)
	{
		Player me = Globals.Me;
		if (me == null)
		{
			return false;
		}
		IItem[] inventory = me.Inventory;
		if (inventory == null)
		{
			return false;
		}
		IItem item = inventory[SlotIndex];
		if (item == null)
		{
			return false;
		}
		if (!Interface.DoesMouseHitInterface() && !me.IsBusy)
		{
			PacketSender.SendDropItem(SlotIndex, item.Quantity);
			return true;
		}
		Base obj = Interface.FindComponentUnderCursor();
		while (obj != null)
		{
			if (!(obj is InventoryItem inventoryItem))
			{
				if (!(obj is BagItem bagItem))
				{
					if (!(obj is BankItem bankItem))
					{
						if (!(obj is EquipmentSlotItem equipmentSlotItem))
						{
							if (!(obj is HotbarItem hotbarItem))
							{
								if (!(obj is ShopWindow))
								{
									if (obj is BankWindow)
									{
										me.TryStoreItemInBank(SlotIndex, null, -1, item.Quantity, skipPrompt: true);
										return true;
									}
									obj = obj.Parent;
									continue;
								}
								me.TrySellItem(SlotIndex);
								return true;
							}
							me.AddToHotbar(hotbarItem.SlotIndex, 0, SlotIndex);
							return true;
						}
						ItemDescriptor descriptor = item.Descriptor;
						if (descriptor != null && descriptor.ItemType == ItemType.Equipment && (descriptor.EquipmentSlot == equipmentSlotItem.SlotIndex || AreInterchangeable(descriptor.EquipmentSlot, equipmentSlotItem.SlotIndex)))
						{
							me.TryUseItem(SlotIndex);
							return true;
						}
						return false;
					}
					me.TryStoreItemInBank(SlotIndex, null, bankItem.SlotIndex, item.Quantity, skipPrompt: true);
					return true;
				}
				me.TryStoreItemInBag(SlotIndex, bagItem.SlotIndex);
				return true;
			}
			if (inventoryItem.SlotIndex == SlotIndex)
			{
				return false;
			}
			me.SwapItems(SlotIndex, inventoryItem.SlotIndex);
			return true;
		}
		return false;
	}

	private void PlayerOnInventoryUpdated(Player player, int slotIndex)
	{
		if (player == Globals.Me && slotIndex == SlotIndex && Globals.Me.Inventory[SlotIndex] != null)
		{
			Icon.Texture = null;
		}
	}

	public override void Update()
	{
		if (Globals.Me == null)
		{
			return;
		}
		IItem item = Globals.Me.Inventory[SlotIndex];
		if (item == null)
		{
			return;
		}
		if (!DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var databaseObject))
		{
			_reset();
			return;
		}
		bool flag = Globals.Me.MyEquipment.Any((int s) => s == SlotIndex);
		bool isDragging = Icon.IsDragging;
		bool flag2 = !isDragging && !flag && ShopWindow.Rachete(databaseObject.Id);
		if (flag2 != _marqueDeVente.IsVisibleInParent)
		{
			_marqueDeVente.IsVisibleInParent = flag2;
		}
		if (flag2 && (_marqueDeVente.Width != base.Width || _marqueDeVente.Height != base.Height))
		{
			_marqueDeVente.SetBounds(0, 0, base.Width, base.Height);
		}
		_equipImageBackground.IsVisibleInParent = !isDragging && flag;
		_equipLabel.IsVisibleInParent = !isDragging && flag;
		_quantityLabel.IsVisibleInParent = !isDragging && databaseObject.IsStackable && item.Quantity > 1;
		if (_quantityLabel.IsVisibleInParent)
		{
			_quantityLabel.Text = Strings.FormatQuantityAbbreviated(item.Quantity);
		}
		_cooldownLabel.IsVisibleInParent = !isDragging && Globals.Me.IsItemOnCooldown(SlotIndex);
		if (_cooldownLabel.IsVisibleInParent)
		{
			long itemRemainingCooldown = Globals.Me.GetItemRemainingCooldown(SlotIndex);
			_cooldownLabel.Text = TimeSpan.FromMilliseconds(itemRemainingCooldown).WithSuffix("0.0");
			Icon.RenderColor.A = 100;
		}
		else
		{
			Icon.RenderColor.A = databaseObject.Color.A;
		}
		if (!(Icon.TextureFilename == databaseObject.Icon))
		{
			IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Item, databaseObject.Icon);
			if (texture != null)
			{
				Icon.Texture = texture;
				Icon.RenderColor = (Globals.Me.IsItemOnCooldown(SlotIndex) ? new Color(100, databaseObject.Color.R, databaseObject.Color.G, databaseObject.Color.B) : databaseObject.Color);
				Icon.IsVisibleInParent = true;
			}
			else if (Icon.Texture != null)
			{
				Icon.Texture = null;
				Icon.IsVisibleInParent = false;
			}
		}
	}

	private void _reset()
	{
		Icon.IsVisibleInParent = false;
		Icon.Texture = null;
		_equipImageBackground.IsVisibleInParent = false;
		_marqueDeVente.IsVisibleInParent = false;
		_quantityLabel.IsVisibleInParent = false;
		_equipLabel.IsVisibleInParent = false;
		_cooldownLabel.IsVisibleInParent = false;
	}

	private static bool AreInterchangeable(int first, int second)
	{
		List<string> slots = Options.Instance.Equipment.Slots;
		if (first < 0 || second < 0 || first >= slots.Count || second >= slots.Count)
		{
			return false;
		}
		if (IsSpecial(slots[first]))
		{
			return IsSpecial(slots[second]);
		}
		return false;
	}

	private static bool IsSpecial(string? name)
	{
		if (name != null)
		{
			if (!name.StartsWith("Spé", StringComparison.OrdinalIgnoreCase))
			{
				return name.StartsWith("Spe", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
		return false;
	}
}
