using System;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.General;
using Intersect.Client.Housing;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Housing;

namespace Intersect.Client.Interface.Game.Housing;

internal sealed class HouseOverlay : Intersect.Client.Framework.Gwen.Control.Base
{
	private const long ToastMs = 3600L;

	protected override bool ShouldClip => false;

	public HouseOverlay(Canvas canvas)
		: base(canvas, "HouseOverlay")
	{
		base.MouseInputEnabled = false;
		base.KeyboardInputEnabled = false;
		SetBounds(0, 0, canvas.Width, canvas.Height);
	}

	public void Update()
	{
		Intersect.Client.Framework.Gwen.Control.Base parent = base.Parent;
		if (parent != null && (base.Width != parent.Width || base.Height != parent.Height))
		{
			SetBounds(0, 0, parent.Width, parent.Height);
		}
		base.IsHidden = Globals.Me == null;
	}

	private Point ToLocal(float worldX, float worldY)
	{
		float num = ((base.Parent as Canvas)?.Scale ?? 1f) * Graphics.EchelleDeLInterface;
		FloatRect currentView = Graphics.CurrentView;
		float num2 = Graphics.WorldScale * Graphics.EchelleDuMonde;
		return new Point((int)MathF.Round((worldX - currentView.X) * num2 / num), (int)MathF.Round((worldY - currentView.Y) * num2 / num));
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		Guid[,] mapGrid = Globals.MapGrid;
		if (mapGrid == null)
		{
			return;
		}
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		CharteScreen.Design d = CharteScreen.Design.For(base.Width, base.Height);
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		int tileWidth = Options.Instance.Map.TileWidth;
		int tileHeight = Options.Instance.Map.TileHeight;
		if (!Decoration.Active)
		{
			Guid[,] array = mapGrid;
			foreach (Guid guid in array)
			{
				if (guid == Guid.Empty || !MapInstance.TryGet(guid, out MapInstance instance))
				{
					continue;
				}
				foreach (Houses.Plot item in Houses.PlotsOf(instance))
				{
					DrawPlate(renderer, d, instance, item, tileWidth, tileHeight);
				}
			}
			Houses.Prompt? prompt = Houses.PromptFor(me);
			if (prompt.HasValue)
			{
				Houses.Prompt valueOrDefault = prompt.GetValueOrDefault();
				FloatRect bodyWorldPos = me.BodyWorldPos;
				Point point = ToLocal(bodyWorldPos.X + bodyWorldPos.Width / 2f, bodyWorldPos.Y);
				float num = (float)(millisecondsUtc % 1600) / 1600f;
				int num2 = (int)MathF.Round(-5f * d.S * (1f - MathF.Cos(num * ((float)Math.PI * 2f))) / 2f);
				HouseArt.Pill(renderer, d, point.X, point.Y - d.L(14f) + num2, Strings.Housing.PromptKey, valueOrDefault.Text, valueOrDefault.Detail, valueOrDefault.Refused);
			}
		}
		DrawToast(renderer, d, millisecondsUtc);
	}

	private void DrawPlate(Intersect.Client.Framework.Gwen.Renderer.Base renderer, CharteScreen.Design d, MapInstance map, Houses.Plot plot, int tileWidth, int tileHeight)
	{
		HouseArt.PlateContent content = HouseArt.PlateOf(plot);
		CapsuleDescriptor capsule = plot.Capsule;
		float worldX;
		float worldY;
		if (capsule != null)
		{
			HouseFootprint footprint = plot.Footprint;
			worldX = (float)map.X + ((float)footprint.X + (float)footprint.Width / 2f) * (float)tileWidth;
			int num = HouseWorld.CapsuleTexture(capsule)?.Height ?? (footprint.Height * tileHeight);
			worldY = map.Y + (footprint.Y + footprint.Height) * tileHeight - num - 2;
		}
		else
		{
			worldX = (float)map.X + ((float)plot.Terrain.X + (float)plot.Terrain.Width / 2f) * (float)tileWidth;
			worldY = (float)(map.Y + plot.Terrain.Y * tileHeight) + (float)tileHeight / 2f;
		}
		Point point = ToLocal(worldX, worldY);
		if (point.X >= -d.L(200f) && point.X <= base.Width + d.L(200f) && point.Y >= 0 && point.Y <= base.Height + d.L(60f))
		{
			HouseArt.Plate(renderer, d, point.X, point.Y, content);
		}
	}

	private void DrawToast(Intersect.Client.Framework.Gwen.Renderer.Base renderer, CharteScreen.Design d, long now)
	{
		long num = now - Houses.BoughtAt;
		if (Houses.BoughtAt != 0L && num <= 3900)
		{
			float num2 = ((num < 300) ? ((float)num / 300f) : ((num > 3600) ? (1f - (float)(num - 3600) / 300f) : 1f));
			int num3 = (int)MathF.Round((1f - Math.Min(1f, (float)num / 400f)) * (float)d.L(-10f));
			string text = Houses.BoughtName.ToUpperInvariant();
			int val = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, d.Pt(12f), Strings.Housing.ToastBought.ToString().ToUpperInvariant(), d.Em(0.22f, 12f));
			int val2 = TechniqueArt.Measure(renderer, TechniqueArt.Display, d.Pt(26f), text).X + d.L(8f);
			int x = TechniqueArt.Measure(renderer, TechniqueArt.Body, d.Pt(13.5f), Strings.Housing.ToastBoughtText).X;
			int num4 = d.L(112f) + Math.Max(val, Math.Max(val2, x));
			int h = d.L(80f);
			Rectangle rect = new Rectangle(base.Width / 2 - num4 / 2, d.Y(96f) + num3, num4, h);
			TechniqueArt.Patch(renderer, "tech_chanfrein10.png", rect, TechniqueArt.Alpha(new Color(240, 10, 12, 20), num2), 12);
			TechniqueArt.FillHorizontal(renderer, new Rectangle(rect.X + d.L(6f), rect.Y + 1, rect.Width / 2, rect.Height - 2), TechniqueArt.Alpha(new Color(140, 40, 30, 8), num2), TechniqueArt.Alpha(new Color(60, 40, 30, 8), num2), TechniqueArt.Alpha(new Color(0, 40, 30, 8), num2));
			TechniqueArt.Patch(renderer, "tech_chanfrein10_bord.png", rect, TechniqueArt.Alpha(new Color(153, 255, 208, 106), num2), 12);
			Rectangle rect2 = new Rectangle(rect.X + d.L(14f), rect.Y + d.L(12f), d.L(56f), d.L(56f));
			TechniqueArt.GlowRect(renderer, rect2, TechniqueArt.Alpha(TechniqueArt.Gold, 0.28f * num2), additive: false);
			TechniqueArt.Outline(renderer, rect2, TechniqueArt.Alpha(new Color(128, 255, 208, 106), num2));
			IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Item, "capsule_bleue.png");
			SaiyanHud.Draw(renderer, texture, rect2.X + d.L(8f), rect2.Y + d.L(8f), d.L(40f), d.L(40f), TechniqueArt.Alpha(TechniqueArt.White, num2));
			int x2 = rect2.Right + d.L(16f);
			TechniqueArt.Spaced(renderer, TechniqueArt.Label, d.Pt(12f), Strings.Housing.ToastBought.ToString().ToUpperInvariant(), x2, rect.Y + d.L(18f), d.Em(0.22f, 12f), TechniqueArt.Alpha(TechniqueArt.Gold, num2));
			Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, d.Pt(26f), text);
			TechniqueArt.Styled(renderer, TechniqueArt.Display, d.Pt(26f), text, x2, rect.Y + d.L(40f) - point.Y / 2, TechniqueArt.Stops.Solid(TechniqueArt.Text), num2);
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, d.Pt(13.5f), Strings.Housing.ToastBoughtText, x2, rect.Y + d.L(62f), TechniqueArt.Alpha(TechniqueArt.Soft, num2));
		}
	}
}
