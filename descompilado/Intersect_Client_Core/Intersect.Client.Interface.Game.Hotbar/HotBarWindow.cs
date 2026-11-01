using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Core.Controls;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Techniques;

namespace Intersect.Client.Interface.Game.Hotbar;

public class HotBarWindow : ImagePanel
{
	public readonly List<HotbarItem> Items = new List<HotbarItem>();

	private const int CharteMargin = 5;

	private const int CharteGap = 5;

	private const int CharteRuleBlock = 12;

	private const int CharteRow = 59;

	private readonly List<Rectangle> _charteRules = new List<Rectangle>();

	protected override bool ShouldClip => false;

	public HotBarWindow(Canvas gameCanvas)
		: base(gameCanvas, "HotBarWindow")
	{
		base.AlignmentPadding = new Padding
		{
			Top = 4,
			Right = 4
		};
		base.Alignment = new Alignments[2]
		{
			Alignments.Top,
			Alignments.Right
		};
		base.RestrictToParent = true;
		base.TextureFilename = "hotbar.png";
		base.TextureNinePatchMargin = Margin.Three;
		base.ShouldCacheToTexture = true;
		if (Graphics.Renderer == null)
		{
			return;
		}
		int hotbarSlotCount = Options.Instance.Player.HotbarSlotCount;
		for (int i = 0; i < hotbarSlotCount; i++)
		{
			HotbarItem item = new HotbarItem(i, this);
			Items.Add(item);
		}
		SizeToChildren();
		LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
		foreach (HotbarItem item2 in Items)
		{
			item2.RestrictToParent = true;
		}
		SizeToChildren();
		ApplyCharte();
	}

	private void ApplyCharte()
	{
		base.Texture = null;
		base.TextureFilename = null;
		base.ShouldCacheToTexture = false;
		int num = Math.Max(1, Math.Min(10, Items.Count));
		int num2 = Math.Max(1, (Items.Count + 9) / 10);
		bool flag = num > 5;
		int num3 = (flag ? 7 : 0);
		SetSize(10 + num * 50 + (num - 1) * 5 + num3, 10 + num2 * 59 + (num2 - 1) * 5);
		_charteRules.Clear();
		for (int i = 0; i < Items.Count; i++)
		{
			int num4 = i % 10;
			int num5 = i / 10;
			Items[i].Margin = Margin.Zero;
			Items[i].SetBounds(5 + num4 * 55 + ((num4 >= 5) ? num3 : 0), 5 + num5 * 64 + 3, 50, 50);
			Items[i].ApplyCharte();
		}
		if (flag)
		{
			for (int j = 0; j < num2; j++)
			{
				_charteRules.Add(new Rectangle(281, 5 + j * 64 + 6, 1, 49));
			}
		}
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		MenuContainer.DrawPlate(renderer, base.Width, base.Height, 1f);
		foreach (Rectangle charteRule in _charteRules)
		{
			MenuContainer.DrawRule(renderer, charteRule, 1f);
		}
	}

	protected override void RenderOver(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.RenderOver(skin);
		if (Manette.EnUsage)
		{
			Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
			bool techniquesArmees = Manette.TechniquesArmees;
			Rectangle rect = new Rectangle(-46, (base.Height - 24) / 2 - 3, 40, 24);
			if (techniquesArmees)
			{
				TechniqueArt.Glow(renderer, (float)rect.X + (float)rect.Width / 2f, (float)rect.Y + (float)rect.Height / 2f, 34f, TechniqueArt.Alpha(TechniqueArt.Ki, 0.5f));
			}
			TechniqueArt.Image(renderer, Manette.Glyphe(Manette.Bouton.LT), rect, TechniqueArt.Alpha(TechniqueArt.White, techniquesArmees ? 1f : 0.7f));
		}
	}

	public void Update()
	{
		if (Globals.Me == null)
		{
			return;
		}
		foreach (HotbarItem item in Items)
		{
			item.Update();
		}
	}
}
