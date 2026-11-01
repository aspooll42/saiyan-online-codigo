using System.Collections.Generic;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Interface.Game;

namespace Intersect.Client.Utilities;

public static class PopulateSlotContainer
{
	public static void Populate(ScrollControl slotContainer, List<SlotItem> items)
	{
		float num = slotContainer.InnerPanel.InnerWidth;
		for (int i = 0; i < items.Count; i++)
		{
			SlotItem slotItem = items[i];
			Point size = slotItem.OuterBounds.Size;
			int num2 = (int)(num / (float)size.X);
			int num3 = i % num2;
			int num4 = i / num2;
			int x = num3 * size.X + slotItem.Margin.Left;
			int y = num4 * size.Y + slotItem.Margin.Top;
			slotItem.SetPosition(x, y);
		}
	}
}
