using System;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.EntityPanel;

namespace Intersect.Client.Interface.Shared;

internal sealed class OverheadPreview : Intersect.Client.Framework.Gwen.Control.Base
{
	private const string FallbackSprite = "krillin.png";

	public string Heading { get; set; } = string.Empty;

	public Func<(string Label, Color Tint, bool Name, bool Bar)>? State { get; set; }

	public Func<string, string>? HoveredCaption { get; set; }

	public Func<bool, bool, string>? VisibilityCaption { get; set; }

	public OverheadPreview(Intersect.Client.Framework.Gwen.Control.Base parent, string name)
		: base(parent, name)
	{
		base.MouseInputEnabled = false;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		OptionsStyle.Caps(renderer, Heading, 16, 12, 18, OptionsStyle.Soft);
		OptionsStyle.Draw(renderer, "saiyan_option_apercu.png", 14, 40, SaiyanHud.White);
		(string, Color, bool, bool)? tuple = State?.Invoke();
		if (!tuple.HasValue)
		{
			return;
		}
		(string, Color, bool, bool) valueOrDefault = tuple.GetValueOrDefault();
		Player me = Globals.Me;
		string spriteName = me?.Sprite;
		IGameTexture gameTexture = me?.Texture;
		if (gameTexture == null)
		{
			spriteName = "krillin.png";
			gameTexture = Globals.ContentManager.GetTexture(TextureType.Entity, "krillin.png");
		}
		int num = 136;
		if (gameTexture != null && gameTexture.Width > 0 && gameTexture.Height > 0)
		{
			(int, int, int, int) tuple2 = DefaultFrame(gameTexture);
			if (Options.IsLoaded)
			{
				SpriteSheets.Pose? pose = SpriteSheets.StandingBody(spriteName, gameTexture.Name);
				if (pose.HasValue)
				{
					SpriteSheets.Pose valueOrDefault2 = pose.GetValueOrDefault();
					if (!valueOrDefault2.IsEmpty)
					{
						gameTexture = valueOrDefault2.Texture;
						tuple2 = valueOrDefault2.Frame;
					}
				}
			}
			int num2 = Math.Clamp(150 / Math.Max(1, tuple2.Item4), 1, 3);
			int num3 = tuple2.Item3 * num2;
			int num4 = tuple2.Item4 * num2;
			int num5 = 14 + (170 - num3) / 2;
			int num6 = 266 - num4;
			SaiyanHud.Draw(renderer, OptionsStyle.Texture("saiyan_option_point.png"), num5 + num3 / 2 - 26, num6 + num4 - 10, 52, 14, new Color(90, 0, 0, 0));
			renderer.DrawColor = SaiyanHud.White;
			renderer.DrawTexturedRect(gameTexture, new Rectangle(num5, num6, num3, num4), SaiyanHud.White, ((float)tuple2.Item1 + 0.25f) / (float)gameTexture.Width, ((float)tuple2.Item2 + 0.25f) / (float)gameTexture.Height, ((float)(tuple2.Item1 + tuple2.Item3) + 0.75f) / (float)gameTexture.Width, ((float)(tuple2.Item2 + tuple2.Item4) + 0.75f) / (float)gameTexture.Height);
			int num7 = SaiyanHud.TopMargin(gameTexture, new Rectangle(tuple2.Item1, tuple2.Item2, tuple2.Item3, tuple2.Item4));
			num = num6 + num7 * num2;
		}
		int num8 = 99;
		int num9 = num - 14;
		if (valueOrDefault.Item4)
		{
			OptionsStyle.NinePatch(renderer, "saiyan_bar_track.png", num8 - 30, num9, 60, 10, 5, SaiyanHud.White);
			OptionsStyle.NinePatch(renderer, "saiyan_bar_fill_pv.png", num8 - 30, num9, 44, 10, 5, SaiyanHud.White);
		}
		if (valueOrDefault.Item3)
		{
			int num10 = (valueOrDefault.Item4 ? (num9 - 2) : (num - 4));
			string text = me?.Name ?? "Krillin";
			SaiyanHud.TextCenter(renderer, SaiyanHud.NameFont, 12, text, 14, num10 - 22, 170, 22, valueOrDefault.Item2);
		}
		string text2 = HoveredCaption?.Invoke(valueOrDefault.Item1);
		if (text2 != null)
		{
			SaiyanHud.TextCenter(renderer, OptionsStyle.Body, 10, text2, 8, 298, base.Width - 16, 20, OptionsStyle.Soft, shadow: false);
		}
		string text3 = VisibilityCaption?.Invoke(valueOrDefault.Item3, valueOrDefault.Item4);
		if (text3 != null)
		{
			SaiyanHud.TextCenter(renderer, OptionsStyle.Body, 10, text3, 8, 316, base.Width - 16, 20, OptionsStyle.Dim, shadow: false);
		}
	}

	private static (int X, int Y, int Width, int Height) DefaultFrame(IGameTexture texture)
	{
		int num = (Options.IsLoaded ? Math.Max(1, Options.Instance.Sprites.NormalFrames) : 4);
		int num2 = (Options.IsLoaded ? Math.Max(1, Options.Instance.Sprites.Directions) : 4);
		return (X: 0, Y: 0, Width: texture.Width / num, Height: texture.Height / num2);
	}
}
