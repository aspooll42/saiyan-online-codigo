using System;
using Intersect.Client.Entities;
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
using Intersect.Client.Interface.Game.Hotbar;
using Intersect.Client.Networking;
using Intersect.Configuration;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Models;

namespace Intersect.Client.Interface.Game.Inventory;

public class EquipmentSlotItem : SlotItem
{
	public new const int Size = 52;

	private const int GlyphSize = 30;

	private static readonly Color EmptyGlyph = new Color(255, 70, 78, 96);

	private readonly InventoryWindow _window;

	private readonly ImagePanel _glyph;

	private readonly ImagePanel _highlight;

	private readonly string _displayName;

	private Guid _shownItemId;

	private bool _hovered;

	public EquipmentSlotItem(InventoryWindow window, Base parent, int slotIndex, string displayName, string? glyph)
		: base(parent, $"EquipmentSlot{slotIndex}", slotIndex, null)
	{
		_window = window;
		_displayName = displayName;
		base.MinimumSize = new Point(52, 52);
		base.Margin = new Margin(0);
		SetSize(52, 52);
		base.TextureFilename = "inv_case_equipement.png";
		_glyph = new ImagePanel(this, "Glyph")
		{
			MouseInputEnabled = false,
			RenderColor = EmptyGlyph
		};
		if (glyph != null)
		{
			_glyph.TextureFilename = glyph;
		}
		_glyph.SetSize(30, 30);
		_glyph.SetPosition(11, 11);
		_highlight = new ImagePanel(this, "Highlight")
		{
			MouseInputEnabled = false,
			IsHidden = true,
			TextureFilename = "inv_case_equipement_cible.png"
		};
		_highlight.SetSize(52, 52);
		Icon.SetSize(32, 32);
		Icon.SetPosition(10, 10);
		Icon.IsVisibleInParent = false;
		Icon.HoverEnter += Icon_HoverEnter;
		Icon.HoverLeave += delegate
		{
			Interface.GameUi.ItemDescriptionWindow?.Hide();
		};
		Icon.DoubleClicked += delegate
		{
			PacketSender.SendUnequipItem(SlotIndex);
		};
		Icon.Clicked += Icon_Clicked;
		SetToolTipText(displayName);
	}

	public void SetHighlight(bool on, int alpha)
	{
		_highlight.IsHidden = !on;
		if (on)
		{
			_highlight.RenderColor = new Color(alpha, 255, 255, 255);
		}
	}

	private ItemDescriptor? Equipped(out int inventorySlot)
	{
		inventorySlot = -1;
		Player me = Globals.Me;
		if (me == null || SlotIndex >= me.MyEquipment.Length)
		{
			return null;
		}
		inventorySlot = me.MyEquipment[SlotIndex];
		if (inventorySlot < 0 || inventorySlot >= me.Inventory.Length)
		{
			return null;
		}
		Guid id = me.Inventory[inventorySlot]?.ItemId ?? Guid.Empty;
		if (!DatabaseObject<ItemDescriptor>.TryGet(id, out var databaseObject))
		{
			return null;
		}
		return databaseObject;
	}

	public override void Update()
	{
		bool flag = IsHovered || Icon.IsHovered;
		if (flag != _hovered)
		{
			_hovered = flag;
			base.TextureFilename = (flag ? "inv_case_equipement_survol.png" : "inv_case_equipement.png");
		}
		int inventorySlot;
		ItemDescriptor itemDescriptor = Equipped(out inventorySlot);
		if (itemDescriptor == null)
		{
			if (_shownItemId != Guid.Empty || Icon.IsVisibleInParent)
			{
				_shownItemId = Guid.Empty;
				Icon.Texture = null;
				Icon.IsVisibleInParent = false;
				_glyph.IsVisibleInParent = true;
				SetToolTipText(_displayName);
			}
		}
		else if (!(itemDescriptor.Id == _shownItemId) || Icon.Texture == null)
		{
			_shownItemId = itemDescriptor.Id;
			IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Item, itemDescriptor.Icon);
			Icon.Texture = texture;
			Icon.RenderColor = itemDescriptor.Color;
			Icon.IsVisibleInParent = texture != null;
			_glyph.IsVisibleInParent = texture == null;
			SetToolTipText(null);
		}
	}

	private void Icon_HoverEnter(Base sender, EventArgs arguments)
	{
		if (InputHandler.MouseFocus != null || Globals.InputManager.IsMouseButtonDown(MouseButton.Left))
		{
			return;
		}
		int inventorySlot;
		ItemDescriptor itemDescriptor = Equipped(out inventorySlot);
		if (itemDescriptor != null)
		{
			Player me = Globals.Me;
			if (me != null)
			{
				Interface.GameUi.ItemDescriptionWindow?.Show(itemDescriptor, 1, me.Inventory[inventorySlot]?.ItemProperties);
			}
		}
	}

	private void Icon_Clicked(Base sender, MouseButtonState arguments)
	{
		if (arguments.MouseButton == MouseButton.Right && Equipped(out var inventorySlot) != null)
		{
			if (ClientConfiguration.Instance.EnableContextMenus)
			{
				_window.OpenContextMenu(inventorySlot);
			}
			else
			{
				PacketSender.SendUnequipItem(SlotIndex);
			}
		}
	}

	public override bool DragAndDrop_HandleDrop(Package package, int x, int y)
	{
		Player me = Globals.Me;
		if (me == null || Equipped(out var inventorySlot) == null)
		{
			return false;
		}
		if (!Interface.DoesMouseHitInterface())
		{
			return false;
		}
		Base obj = Interface.FindComponentUnderCursor();
		while (obj != null)
		{
			if (!(obj is EquipmentSlotItem))
			{
				if (!(obj is InventoryItem))
				{
					if (obj is HotbarItem hotbarItem)
					{
						me.AddToHotbar(hotbarItem.SlotIndex, 0, inventorySlot);
						return true;
					}
					if (_window.IsBagArea(obj))
					{
						PacketSender.SendUnequipItem(SlotIndex);
						return true;
					}
					obj = obj.Parent;
					continue;
				}
				PacketSender.SendUnequipItem(SlotIndex);
				return true;
			}
			return false;
		}
		return false;
	}
}
