using System;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Items;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Models;

namespace Intersect.Client.Interface.Game.Inventory;

public class MapItemIcon
{
	public ImagePanel Container;

	public MapItemInstance? MyItem;

	public Guid MapId;

	public int TileIndex;

	public ImagePanel Pnl;

	private MapItemWindow mMapItemWindow;

	public MapItemIcon(MapItemWindow window)
	{
		mMapItemWindow = window;
	}

	public void Setup()
	{
		new ImagePanel(Container, "MapItemCase").MouseInputEnabled = false;
		Pnl = new ImagePanel(Container, "MapItemIcon")
		{
			MouseInputEnabled = false
		};
		Container.HoverEnter += pnl_HoverEnter;
		Container.HoverLeave += pnl_HoverLeave;
		Container.Clicked += pnl_Clicked;
	}

	private void pnl_Clicked(Base sender, MouseButtonState arguments)
	{
		if (MyItem != null && TileIndex >= 0 && TileIndex < Options.Instance.Map.MapWidth * Options.Instance.Map.MapHeight)
		{
			Player.TryPickupItem(MapId, TileIndex, MyItem.Id);
		}
	}

	private void pnl_HoverLeave(Base sender, EventArgs arguments)
	{
		Interface.GameUi.ItemDescriptionWindow?.Hide();
	}

	private void pnl_HoverEnter(Base? sender, EventArgs? arguments)
	{
		if (MyItem != null && InputHandler.MouseFocus == null && !Globals.InputManager.IsMouseButtonDown(MouseButton.Left))
		{
			Interface.GameUi.ItemDescriptionWindow?.Show(DatabaseObject<ItemDescriptor>.Get(MyItem.ItemId), MyItem.Quantity, MyItem.ItemProperties);
		}
	}

	public FloatRect RenderBounds()
	{
		return new FloatRect
		{
			X = Pnl.ToCanvas(new Point(0, 0)).X,
			Y = Pnl.ToCanvas(new Point(0, 0)).Y,
			Width = Pnl.Width,
			Height = Pnl.Height
		};
	}

	public void Update()
	{
		if (MyItem == null)
		{
			return;
		}
		ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(MyItem.ItemId);
		if (itemDescriptor != null)
		{
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Item, itemDescriptor.Icon);
			if (texture != null)
			{
				Pnl.RenderColor = itemDescriptor.Color;
				Pnl.Texture = texture;
			}
			else if (Pnl.Texture != null)
			{
				Pnl.Texture = null;
			}
		}
		else if (Pnl.Texture != null)
		{
			Pnl.Texture = null;
		}
	}
}
