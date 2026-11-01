using System;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.DragDrop;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Inventory;
using Intersect.Client.Items;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Configuration;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Models;

namespace Intersect.Client.Interface.Game.Bag;

public class BagItem : SlotItem
{
	private readonly Label _quantityLabel;

	private readonly BagWindow _bagWindow;

	private readonly MenuItem _withdrawContextItem;

	public BagItem(BagWindow bagWindow, Base parent, int index, ContextMenu contextMenu)
		: base(parent, "BagItem", index, contextMenu)
	{
		_bagWindow = bagWindow;
		base.TextureFilename = "bagitem.png";
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
			FontName = "sourcesansproblack",
			FontSize = 9,
			Padding = new Padding(2)
		};
		LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
		contextMenu.ClearChildren();
		_withdrawContextItem = contextMenu.AddItem(Strings.BagContextMenu.Withdraw);
		_withdrawContextItem.Clicked += _withdrawMenuItem_Clicked;
		contextMenu.LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
	}

	protected override void OnContextMenuOpening(ContextMenu contextMenu)
	{
		contextMenu.ClearChildren();
		Item[] bagSlots = Globals.BagSlots;
		if (bagSlots == null || bagSlots.Length <= 0)
		{
			return;
		}
		int slotIndex = SlotIndex;
		if (slotIndex < bagSlots.Length)
		{
			Item item = bagSlots[slotIndex];
			if (item != null && DatabaseObject<ItemDescriptor>.TryGet(item.ItemId, out var databaseObject))
			{
				_withdrawContextItem.SetText(Strings.BagContextMenu.Withdraw.ToString(databaseObject.Name));
				contextMenu.AddChild(_withdrawContextItem);
				base.OnContextMenuOpening(contextMenu);
			}
		}
	}

	private void _withdrawMenuItem_Clicked(Base sender, MouseButtonState arguments)
	{
		if (Globals.InBag)
		{
			Globals.Me?.TryRetrieveItemFromBag(SlotIndex, -1);
		}
	}

	private void Icon_HoverEnter(Base? sender, EventArgs? arguments)
	{
		if (InputHandler.MouseFocus != null || Globals.InputManager.IsMouseButtonDown(MouseButton.Left))
		{
			return;
		}
		Item[] bagSlots = Globals.BagSlots;
		if (bagSlots == null || bagSlots.Length <= 0)
		{
			return;
		}
		Item item = bagSlots[SlotIndex];
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
			Item item2 = bagSlots[SlotIndex];
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
		if (arguments.MouseButton == MouseButton.Left)
		{
			Globals.Me?.TryRetrieveItemFromBag(SlotIndex, -1);
		}
	}

	public override bool DragAndDrop_HandleDrop(Package package, int x, int y)
	{
		Base obj = Interface.FindComponentUnderCursor();
		while (obj != null)
		{
			if (!(obj is BagItem bagItem))
			{
				if (obj is InventoryItem inventoryItem)
				{
					Globals.Me?.TryRetrieveItemFromBag(SlotIndex, inventoryItem.SlotIndex);
					return true;
				}
				obj = obj.Parent;
				continue;
			}
			PacketSender.SendMoveBagItems(SlotIndex, bagItem.SlotIndex);
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
		Item[] bagSlots = Globals.BagSlots;
		if (bagSlots == null || bagSlots.Length <= 0)
		{
			return;
		}
		Item item = bagSlots[SlotIndex];
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
		Item item2 = bagSlots[SlotIndex];
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
