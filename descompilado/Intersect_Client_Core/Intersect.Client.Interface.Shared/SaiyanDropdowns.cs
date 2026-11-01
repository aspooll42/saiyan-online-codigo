using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;

namespace Intersect.Client.Interface.Shared;

public static class SaiyanDropdowns
{
	private const int FontSize = 11;

	private static readonly Color Entry = new Color(255, 196, 204, 220);

	private static readonly Color OrangeLight = new Color(255, 255, 172, 82);

	private static readonly Color Dim = new Color(255, 150, 160, 182);

	public static void Apply(Base root)
	{
		IFont font = GameContentManager.Current?.GetFont("chakrapetch");
		if (font == null)
		{
			return;
		}
		foreach (ComboBox item in Descendants(root).OfType<ComboBox>())
		{
			item.Font = font;
			item.FontSize = 11;
			item.SetTextColor(Entry, ComponentState.Normal);
			item.SetTextColor(OrangeLight, ComponentState.Hovered);
			item.SetTextColor(Entry, ComponentState.Active);
			item.SetTextColor(Dim, ComponentState.Disabled);
		}
	}

	private static IEnumerable<Base> Descendants(Base root)
	{
		Stack<Base> pending = new Stack<Base>();
		pending.Push(root);
		while (pending.Count > 0)
		{
			Base control = pending.Pop();
			yield return control;
			foreach (Base child in control.Children)
			{
				pending.Push(child);
			}
		}
	}
}
