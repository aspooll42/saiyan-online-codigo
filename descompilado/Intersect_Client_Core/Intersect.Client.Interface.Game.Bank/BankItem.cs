using System;
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
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Chat;
using Intersect.Client.Interface.Game.Inventory;
using Intersect.Client.Items;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Configuration;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Models;

namespace Intersect.Client.Interface.Game.Bank;

public class BankItem : SlotItem
{
	private readonly Label _quantityLabel;

	private BankWindow _bankWindow;

	private MenuItem _withdrawContextItem;

	public BankItem(BankWindow bankWindow, Base parent, int index, ContextMenu contextMenu)
		: base(parent, "BankItem", index, contextMenu)
	{
		_bankWindow = bankWindow;
		base.TextureFilename = "saiyan_slot.png";
		Icon.HoverEnter += Icon_HoverEnter;
		Icon.HoverLeave += Icon_HoverLeave;
		Icon.Clicked += Icon_Clicked;
		Icon.DoubleClicked += Icon_DoubleClicked;
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
			Padding = new Padding(2)
		};
		LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
		_quantityLabel.FontName = "chakrapetch";
		_quantityLabel.FontSize = 10;
		_quantityLabel.TextColor = new Color(255, 176, 186, 206);
		contextMenu.ClearChildren();
		_withdrawContextItem = contextMenu.AddItem(Strings.BankContextMenu.Withdraw);
		_withdrawContextItem.Clicked += _withdrawMenuItem_Clicked;
		contextMenu.LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
	}

	protected override void OnContextMenuOpening(ContextMenu contextMenu)
	{
		contextMenu.ClearChildren();
		Item[] bankSlots = Globals.BankSlots;
		if (bankSlots == null || bankSlots.Length <= 0)
		{
			return;
		}
		int slotIndex = SlotIndex;
		if (slotIndex < bankSlots.Length)
		{
			Item item = bankSlots[slotIndex];
			if (item != null && DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var databaseObject))
			{
				_withdrawContextItem.SetText(Strings.BankContextMenu.Withdraw.ToString(databaseObject.Name));
				contextMenu.AddChild(_withdrawContextItem);
				base.OnContextMenuOpening(contextMenu);
			}
		}
	}

	private void _withdrawMenuItem_Clicked(Base sender, MouseButtonState arguments)
	{
		Globals.Me?.TryRetrieveItemFromBank(SlotIndex);
	}

	private void Icon_HoverEnter(Base? sender, EventArgs? arguments)
	{
		if (InputHandler.MouseFocus != null || Globals.InputManager.IsMouseButtonDown(MouseButton.Left))
		{
			return;
		}
		Item[] bankSlots = Globals.BankSlots;
		if (bankSlots == null || bankSlots.Length <= 0)
		{
			return;
		}
		Item item = bankSlots[SlotIndex];
		bool flag;
		if (item != null)
		{
			ItemDescriptor descriptor = item.Descriptor;
			if (descriptor != null)
			{
				int quantity = item.Quantity;
				if (quantity > 0)
				{
					flag = false;
					goto IL_0051;
				}
			}
		}
		flag = true;
		goto IL_0051;
		IL_0051:
		if (!flag)
		{
			Item item2 = bankSlots[SlotIndex];
			Interface.GameUi.ItemDescriptionWindow?.Show(item2.Descriptor, item2.Quantity, item2.ItemProperties);
		}
	}

	private void Icon_HoverLeave(Base sender, EventArgs arguments)
	{
		Interface.GameUi.ItemDescriptionWindow?.Hide();
	}

	private void Icon_Clicked(Base sender, MouseButtonState arguments)
	{
		if (arguments.MouseButton == MouseButton.Right)
		{
			if (ClientConfiguration.Instance.EnableContextMenus)
			{
				OpenContextMenu();
			}
			else
			{
				Icon_DoubleClicked(sender, arguments);
			}
		}
	}

	private void Icon_DoubleClicked(Base sender, MouseButtonState arguments)
	{
		if (arguments.MouseButton != MouseButton.Left)
		{
			return;
		}
		Item[] bankSlots = Globals.BankSlots;
		if (bankSlots == null || bankSlots.Length <= 0)
		{
			return;
		}
		Item item = bankSlots[SlotIndex];
		if (item != null && item.Quantity > 0)
		{
			if (Globals.InputManager.IsKeyDown(Keys.Shift))
			{
				Globals.Me?.TryRetrieveItemFromBank(SlotIndex, null, -1, -1, skipPrompt: true);
			}
			else
			{
				Globals.Me?.TryRetrieveItemFromBank(SlotIndex, item, -1, item.Quantity);
			}
		}
	}

	public override bool DragAndDrop_HandleDrop(Package package, int x, int y)
	{
		Player me = Globals.Me;
		if (me == null)
		{
			return false;
		}
		Base obj = Interface.FindComponentUnderCursor();
		while (obj != null)
		{
			if (!(obj is BankItem bankItem))
			{
				if (!(obj is InventoryItem inventoryItem))
				{
					if (obj is InventoryWindow)
					{
						Item[] bankSlots = Globals.BankSlots;
						if (bankSlots != null && bankSlots.Length > 0)
						{
							Item item = bankSlots[SlotIndex];
							if (item != null && item.Quantity > 0)
							{
								me.TryRetrieveItemFromBank(SlotIndex, null, -1, item.Quantity, skipPrompt: true);
								return true;
							}
						}
						return false;
					}
					obj = obj.Parent;
					continue;
				}
				Item[] bankSlots2 = Globals.BankSlots;
				if (bankSlots2 == null || bankSlots2.Length <= 0)
				{
					return false;
				}
				Item item2 = bankSlots2[SlotIndex];
				if (item2 == null || item2.Quantity <= 0)
				{
					return false;
				}
				me.TryRetrieveItemFromBank(SlotIndex, null, inventoryItem.SlotIndex, item2.Quantity, skipPrompt: true);
				return true;
			}
			if (bankItem.SlotIndex == SlotIndex)
			{
				return false;
			}
			if (Globals.IsGuildBank && !BankWindow.CanMove(me))
			{
				BankWindow.Refuse(BankWindow.Refusal.Move);
				ChatboxMsg.AddMessage(new ChatboxMsg(Strings.Guilds.NotAllowedSwap.ToString(me.Guild), CustomColors.Alerts.Error, ChatMessageType.Bank));
				return false;
			}
			PacketSender.SendMoveBankItems(SlotIndex, bankItem.SlotIndex);
			return true;
		}
		return false;
	}

	public override void Update()
	{
		if (Globals.Me == null)
		{
			return;
		}
		Item[] bankSlots = Globals.BankSlots;
		if (bankSlots == null || bankSlots.Length <= 0)
		{
			return;
		}
		Item item = bankSlots[SlotIndex];
		bool flag;
		if (item != null)
		{
			ItemDescriptor descriptor = item.Descriptor;
			if (descriptor != null)
			{
				int quantity = item.Quantity;
				if (quantity > 0)
				{
					flag = false;
					goto IL_0049;
				}
			}
		}
		flag = true;
		goto IL_0049;
		IL_0049:
		if (flag)
		{
			_quantityLabel.IsVisibleInParent = false;
			Icon.Texture = null;
			return;
		}
		Item item2 = bankSlots[SlotIndex];
		ItemDescriptor descriptor2 = item2.Descriptor;
		_quantityLabel.IsVisibleInParent = !Icon.IsDragging && descriptor2.IsStackable && item2.Quantity > 1;
		if (_quantityLabel.IsVisibleInParent)
		{
			_quantityLabel.Text = Strings.FormatQuantityAbbreviated(item2.Quantity);
		}
		if (!(Icon.TextureFilename == descriptor2.Icon))
		{
			IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Item, descriptor2.Icon);
			if (texture != null)
			{
				Icon.Texture = texture;
				Icon.RenderColor = descriptor2.Color;
				Icon.IsVisibleInParent = true;
			}
			else if (Icon.Texture != null)
			{
				Icon.Texture = null;
				Icon.IsVisibleInParent = false;
			}
		}
	}
}
