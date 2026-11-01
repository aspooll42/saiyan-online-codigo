using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Items;
using Intersect.Client.General;
using Intersect.Client.Items;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Items;

namespace Intersect.Client.Interface.Game.Inventory;

public class MapItemWindow
{
	public List<MapItemIcon> Items = new List<MapItemIcon>();

	private List<Label> mValues = new List<Label>();

	private List<Label> mNames = new List<Label>();

	private PlaqueObjets mMapItemWindow;

	private ScrollControl mItemContainer;

	private Button mBtnLootAll;

	private bool mFoundItems;

	private int mScanTimer = 250;

	private long mLastItemScan;

	public int X => mMapItemWindow.X;

	public int Y => mMapItemWindow.Y;

	public ImagePanel Panel => mMapItemWindow;

	public MapItemWindow(Canvas gameCanvas)
	{
		mMapItemWindow = new PlaqueObjets(gameCanvas);
		mItemContainer = new ScrollControl(mMapItemWindow, "ItemsContainer");
		mItemContainer.EnableScroll(horizontal: false, vertical: true);
		mBtnLootAll = new Button(mMapItemWindow, "LootAllButton", disableText: true);
		mBtnLootAll.Clicked += MBtnLootAll_Clicked;
		mMapItemWindow.Bouton = mBtnLootAll;
		mMapItemWindow.LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
		mBtnLootAll.ShouldDrawBackground = false;
		mBtnLootAll.AutoSizeToContents = false;
		StyleScroll(mItemContainer);
		CreateItemContainer();
	}

	private static void StyleScroll(ScrollControl scroll)
	{
		scroll.VerticalScrollBar.SetBackgroundTemplate(null, "saiyan_scroll_bg.png");
		IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_scroll_bar.png");
		IGameTexture texture2 = GameContentManager.Current.GetTexture(TextureType.Gui, "saiyan_scroll_bar_hover.png");
		scroll.VerticalScrollBar.SetScrollBarImage(texture, "saiyan_scroll_bar.png", ComponentState.Normal);
		scroll.VerticalScrollBar.SetScrollBarImage(texture2, "saiyan_scroll_bar_hover.png", ComponentState.Hovered);
		scroll.VerticalScrollBar.SetScrollBarImage(texture2, "saiyan_scroll_bar_hover.png", ComponentState.Active);
	}

	public void Update()
	{
		if (!Options.Instance.Loot.EnableLootWindow)
		{
			mMapItemWindow.Hide();
			return;
		}
		if (mLastItemScan < Timing.Global.Milliseconds)
		{
			mFoundItems = false;
			int num = 0;
			foreach (KeyValuePair<MapInstance, List<int>> item in FindSurroundingTiles(Globals.Me.X, Globals.Me.Y, Options.Instance.Loot.MaximumLootWindowDistance))
			{
				Dictionary<int, List<IMapItemInstance>> mapItems = item.Key.MapItems;
				List<int> value = item.Value;
				foreach (int item2 in value)
				{
					if (!mapItems.ContainsKey(item2))
					{
						continue;
					}
					foreach (IMapItemInstance item3 in mapItems[item2])
					{
						if (num > Options.Instance.Loot.MaximumLootWindowItems - 1)
						{
							continue;
						}
						ItemDescriptor descriptor = item3.Descriptor;
						if (descriptor != null)
						{
							Items[num].TileIndex = item2;
							Items[num].MapId = item.Key.Id;
							Items[num].MyItem = item3 as MapItemInstance;
							Items[num].Container.IsHidden = false;
							Items[num].Pnl.IsHidden = false;
							mNames[num].Text = Traduction.Contenu(descriptor.Name);
							mNames[num].IsHidden = false;
							if (descriptor.IsStackable)
							{
								mValues[num].IsHidden = item3.Quantity <= 1;
								mValues[num].Text = Strings.FormatQuantityAbbreviated(item3.Quantity);
							}
							else
							{
								mValues[num].IsHidden = true;
							}
							mFoundItems = true;
							num++;
						}
						else
						{
							Items[num].TileIndex = -1;
							Items[num].MyItem = null;
							Items[num].Pnl.IsHidden = true;
							mNames[num].IsHidden = true;
							mValues[num].IsHidden = true;
						}
					}
				}
			}
			int num2 = Math.Min(Items.Count, mValues.Count);
			for (int i = 0; i < num2; i++)
			{
				MapItemIcon mapItemIcon = Items[i];
				if (i > num - 1)
				{
					mapItemIcon.MyItem = null;
					ImagePanel container = mapItemIcon.Container;
					if (container != null && !container.IsHidden)
					{
						container.IsHidden = true;
					}
					ImagePanel pnl = mapItemIcon.Pnl;
					if (pnl != null && !pnl.IsHidden)
					{
						pnl.IsHidden = true;
					}
					Label label = mValues[i];
					if (label != null && !label.IsHidden)
					{
						label.IsHidden = true;
					}
					Label label2 = mNames[i];
					if (label2 != null && !label2.IsHidden)
					{
						label2.IsHidden = true;
					}
				}
				mapItemIcon.Update();
			}
			mMapItemWindow.Nombre = num;
			mLastItemScan = Timing.Global.Milliseconds + mScanTimer;
		}
		if (mFoundItems != mMapItemWindow.IsVisibleInTree)
		{
			mMapItemWindow.IsVisibleInTree = mFoundItems;
		}
	}

	private void CreateItemContainer()
	{
		for (int i = 0; i < Options.Instance.Loot.MaximumLootWindowItems; i++)
		{
			Items.Add(new MapItemIcon(this));
			Items[i].Container = new ImagePanel(mItemContainer, "MapItemIcon");
			Items[i].Setup();
			mNames.Add(new Label(Items[i].Container, "MapItemName"));
			mNames[i].Text = string.Empty;
			mValues.Add(new Label(Items[i].Container, "MapItemValue"));
			mValues[i].Text = string.Empty;
			Items[i].Container.LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
			int num = Items[i].Container.Margin.Left + Items[i].Container.Margin.Right;
			int num2 = Items[i].Container.Margin.Top + Items[i].Container.Margin.Bottom;
			Items[i].Container.SetPosition(i % (mItemContainer.Width / (Items[i].Container.Width + num)) * (Items[i].Container.Width + num) + num, i / (mItemContainer.Width / (Items[i].Container.Width + num)) * (Items[i].Container.Height + num2) + num2);
		}
	}

	private void MBtnLootAll_Clicked(Base sender, MouseButtonState arguments)
	{
		if (Globals.Me.MapInstance != null && Globals.Me.MapInstance is MapInstance)
		{
			var (mapId, tileIndex) = Globals.Me.CaseDeRamassage();
			Player.TryPickupItem(mapId, tileIndex);
		}
	}

	private Dictionary<MapInstance, List<int>> FindSurroundingTiles(int myX, int myY, int distance)
	{
		Dictionary<MapInstance, List<int>> dictionary = new Dictionary<MapInstance, List<int>>();
		for (int i = -distance; i <= distance; i++)
		{
			for (int j = -distance; j <= distance; j++)
			{
				MapInstance mapInstance = Globals.Me.MapInstance as MapInstance;
				int num = myX + i;
				int num2 = myY + j;
				if (mapInstance == null)
				{
					break;
				}
				if (num < 0)
				{
					MapInstance mapInstance2 = mapInstance;
					if (mapInstance.Left != Guid.Empty)
					{
						mapInstance = MapInstance.Get(mapInstance.Left);
						if (mapInstance == null)
						{
							mapInstance = mapInstance2;
							continue;
						}
						num = Options.Instance.Map.MapWidth + 1 + i;
					}
				}
				if (num >= Options.Instance.Map.MapWidth)
				{
					MapInstance mapInstance3 = mapInstance;
					if (mapInstance.Right != Guid.Empty)
					{
						mapInstance = MapInstance.Get(mapInstance.Right);
						if (mapInstance == null)
						{
							mapInstance = mapInstance3;
							continue;
						}
						num = -1 + i;
					}
				}
				if (num2 < 0)
				{
					MapInstance mapInstance4 = mapInstance;
					if (mapInstance.Up != Guid.Empty)
					{
						mapInstance = MapInstance.Get(mapInstance.Up);
						if (mapInstance == null)
						{
							mapInstance = mapInstance4;
							continue;
						}
						num2 = Options.Instance.Map.MapHeight + 1 + j;
					}
				}
				if (num2 >= Options.Instance.Map.MapHeight)
				{
					MapInstance mapInstance5 = mapInstance;
					if (mapInstance.Down != Guid.Empty)
					{
						mapInstance = MapInstance.Get(mapInstance.Down);
						if (mapInstance == null)
						{
							mapInstance = mapInstance5;
							continue;
						}
						num2 = -1 + j;
					}
				}
				if (!dictionary.ContainsKey(mapInstance))
				{
					dictionary.Add(mapInstance, new List<int>());
				}
				dictionary[mapInstance].Add(num2 * Options.Instance.Map.MapWidth + num);
			}
		}
		return dictionary;
	}
}
