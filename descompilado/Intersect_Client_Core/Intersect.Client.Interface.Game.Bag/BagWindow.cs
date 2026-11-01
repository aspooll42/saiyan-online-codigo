using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.General;
using Intersect.Client.Items;
using Intersect.Client.Localization;
using Intersect.Client.Utilities;

namespace Intersect.Client.Interface.Game.Bag;

public class BagWindow : Window
{
	public List<SlotItem> Items = new List<SlotItem>();

	private readonly ScrollControl _slotContainer;

	private readonly ContextMenu _contextMenu;

	public BagWindow(Canvas gameCanvas)
		: base(gameCanvas, Strings.Bags.Title, modal: false, "BagWindow")
	{
		DisableResizing();
		Interface.InputBlockingComponents.Add(this);
		base.Alignment = new Alignments[1] { Alignments.Center };
		base.MinimumSize = new Point(192, 254);
		base.IsResizable = false;
		base.IsClosable = true;
		base.TitleLabel.FontSize = 14;
		base.TitleLabel.TextColorOverride = Color.White;
		_slotContainer = new ScrollControl(this, "ItemContainer")
		{
			Dock = Pos.Fill,
			OverflowX = OverflowBehavior.Auto,
			OverflowY = OverflowBehavior.Scroll
		};
		_contextMenu = new ContextMenu(gameCanvas, "BagContextMenu")
		{
			IsVisibleInParent = false,
			IconMarginDisabled = true,
			ItemFont = GameContentManager.Current.GetFont("sourcesansproblack"),
			ItemFontSize = 10
		};
	}

	protected override void EnsureInitialized()
	{
		LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
		InitItemContainer();
	}

	private void InitItemContainer()
	{
		Item[] bagSlots = Globals.BagSlots;
		if (bagSlots != null && bagSlots.Length > 0)
		{
			for (int i = 0; i < bagSlots.Length; i++)
			{
				Items.Add(new BagItem(this, _slotContainer, i, _contextMenu));
			}
			PopulateSlotContainer.Populate(_slotContainer, Items);
		}
	}

	public void Update()
	{
		if (!base.IsVisibleInTree)
		{
			return;
		}
		for (int i = 0; i < Items.Count; i++)
		{
			if (Items[i] is BagItem bagItem)
			{
				bagItem.Update();
			}
		}
	}

	public override void Hide()
	{
		_contextMenu?.Close();
		base.Hide();
	}
}
