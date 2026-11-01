using System;
using System.Collections.Generic;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Framework.Core;

namespace Intersect.Client.Entities;

public static class PartyMarkers
{
	private const int BadgeSize = 32;

	private const int Directions = 16;

	private const int EdgeMargin = 30;

	private const int LabelGap = 16;

	private const int LabelPaddingX = 5;

	private const int LabelPaddingY = 2;

	private const int NameFontSize = 11;

	private const int DistanceFontSize = 9;

	private const float BobPixels = 2f;

	private const float BobPeriodMs = 1200f;

	public static readonly Color NameColor = new Color(255, 140, 235, 170);

	private static readonly Color DistanceColor = new Color(255, 176, 184, 200);

	private static readonly Color InitialColor = new Color(255, 242, 242, 242);

	private static readonly Color ShadowColor = new Color(180, 0, 0, 0);

	private static readonly Color BandColor = new Color(170, 11, 14, 23);

	private static IFont? _nameFont;

	private static IFont? _smallFont;

	public static void Draw()
	{
		GameRenderer renderer = Graphics.Renderer;
		if (renderer == null)
		{
			return;
		}
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		List<IPartyMember> party = me.Party;
		if (party == null || party.Count <= 1)
		{
			return;
		}
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Gui, "hud_groupe_fleches.png");
		if (_nameFont == null)
		{
			_nameFont = Globals.ContentManager.GetFont("sairanom") ?? Globals.ContentManager.GetFont("rajdhanibold");
		}
		if (_smallFont == null)
		{
			_smallFont = Globals.ContentManager.GetFont("chakrapetch");
		}
		if (texture == null || _nameFont == null || _smallFont == null)
		{
			return;
		}
		float num = Math.Max(0.25f, Graphics.WorldScale);
		float pixel = 1f / num;
		FloatRect currentView = Graphics.CurrentView;
		FloatRect viewport = Graphics.WorldViewport;
		Vector2 vector = new Vector2(viewport.X + viewport.Width / 2f, viewport.Y + viewport.Height / 2f);
		float num2 = viewport.Width / 2f - 30f * pixel;
		float num3 = viewport.Height / 2f - 30f * pixel;
		if (num2 <= 0f || num3 <= 0f)
		{
			return;
		}
		float num4 = MathF.Sin((float)Timing.Global.Milliseconds * 0.0052359877f) * 2f * pixel;
		int num5 = Math.Max(1, Options.Instance.Map.TileWidth);
		int num6 = Math.Max(1, Options.Instance.Map.TileHeight);
		foreach (IPartyMember item in party)
		{
			Vector2 direction;
			float labelWidth;
			float labelHeight;
			bool onSide;
			if (!(item.Id == me.Id) && Globals.Entities.TryGetValue(item.Id, out Entity value) && value is Player { IsDisposed: false, LatestMap: not null, Center: var center, BodyWorldPos: var bodyWorldPos } player && (!(bodyWorldPos.Right > viewport.X) || !(bodyWorldPos.X < viewport.X + viewport.Width) || !(bodyWorldPos.Bottom > viewport.Y) || !(bodyWorldPos.Y < viewport.Y + viewport.Height)))
			{
				direction = center - vector;
				if (!(direction.LengthSquared() < 1f))
				{
					float num7 = MathF.Min((direction.X == 0f) ? float.MaxValue : (num2 / MathF.Abs(direction.X)), (direction.Y == 0f) ? float.MaxValue : (num3 / MathF.Abs(direction.Y)));
					Vector2 vector2 = Vector2.Normalize(direction);
					float num8 = MathF.Atan2(direction.Y, direction.X);
					int num9 = ((int)MathF.Round(num8 / ((float)Math.PI / 8f)) % 16 + 16) % 16;
					Vector2 center2 = me.Center;
					int num10 = (int)MathF.Max(MathF.Abs(center.X - center2.X) / (float)num5, MathF.Abs(center.Y - center2.Y) / (float)num6);
					string text = player.Name ?? string.Empty;
					string text2 = Strings.Parties.Tiles.ToString(num10);
					Vector2 vector3 = renderer.MeasureText(text, _nameFont, 11, pixel);
					Vector2 vector4 = renderer.MeasureText(text2, _smallFont, 9, pixel);
					float num11 = 6f * pixel;
					labelWidth = vector3.X + num11 + vector4.X + 10f * pixel;
					labelHeight = MathF.Max(vector3.Y, vector4.Y) + 4f * pixel;
					onSide = MathF.Abs(direction.X) * num3 >= MathF.Abs(direction.Y) * num2;
					Vector2 insigne = QuestMarkers.HorsDeLInterface(vector + direction * num7, viewport, pixel, Couvert) + vector2 * num4;
					float num12 = Snap(insigne.X - 16f * pixel, currentView.X, num);
					float num13 = Snap(insigne.Y - 16f * pixel, currentView.Y, num);
					Graphics.DrawGameTexture(texture, new FloatRect(num9 * 32, 0f, 32f, 32f), new FloatRect(num12, num13, 32f * pixel, 32f * pixel), Color.White);
					string text3 = (string.IsNullOrEmpty(player.Name) ? "?" : player.Name.Substring(0, 1).ToUpperInvariant());
					Vector2 vector5 = renderer.MeasureText(text3, _nameFont, 11, pixel);
					DrawText(renderer, text3, _nameFont, 11, Snap(num12 + 16f * pixel - vector5.X / 2f, currentView.X, num), Snap(num13 + 16f * pixel - vector5.Y / 2f, currentView.Y, num), pixel, InitialColor);
					Vector2 vector6 = Etiquette(insigne);
					float num14 = Snap(vector6.X, currentView.X, num);
					float num15 = Snap(vector6.Y, currentView.Y, num);
					Graphics.DrawGameTexture(renderer.WhitePixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num14, num15, labelWidth, labelHeight), BandColor);
					Graphics.DrawGameTexture(renderer.WhitePixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num14, num15, 2f * pixel, labelHeight), NameColor);
					float num16 = num14 + 5f * pixel;
					DrawText(renderer, text, _nameFont, 11, num16, Snap(num15 + (labelHeight - vector3.Y) / 2f, currentView.Y, num), pixel, NameColor);
					DrawText(renderer, text2, _smallFont, 9, Snap(num16 + vector3.X + num11, currentView.X, num), Snap(num15 + (labelHeight - vector4.Y) / 2f, currentView.Y, num), pixel, DistanceColor);
				}
			}
			FloatRect Couvert(Vector2 insigne2)
			{
				Vector2 vector7 = Etiquette(insigne2);
				float num17 = 16f * pixel;
				float num18 = MathF.Min(insigne2.X - num17, vector7.X);
				float num19 = MathF.Min(insigne2.Y - num17, vector7.Y);
				float num20 = MathF.Max(insigne2.X + num17, vector7.X + labelWidth);
				float num21 = MathF.Max(insigne2.Y + num17, vector7.Y + labelHeight);
				return new FloatRect(num18, num19, num20 - num18, num21 - num19);
			}
			Vector2 Etiquette(Vector2 vector7)
			{
				float value2;
				float value3;
				if (onSide)
				{
					value2 = ((direction.X > 0f) ? (vector7.X - 16f * pixel - labelWidth) : (vector7.X + 16f * pixel));
					value3 = vector7.Y - labelHeight / 2f;
				}
				else
				{
					value2 = vector7.X - labelWidth / 2f;
					value3 = ((direction.Y > 0f) ? (vector7.Y - 16f * pixel - labelHeight) : (vector7.Y + 16f * pixel));
				}
				return new Vector2(Math.Clamp(value2, viewport.X + 2f * pixel, viewport.X + viewport.Width - labelWidth - 2f * pixel), Math.Clamp(value3, viewport.Y + 2f * pixel, viewport.Y + viewport.Height - labelHeight - 2f * pixel));
			}
		}
	}

	private static void DrawText(GameRenderer renderer, string text, IFont font, int size, float x, float y, float pixel, Color color)
	{
		FloatRect currentView = Graphics.CurrentView;
		FloatRect clipRect = new FloatRect(0f, 0f, renderer.ScreenWidth, renderer.ScreenHeight);
		renderer.DrawString(text, font, size, x + pixel - currentView.X, y + pixel - currentView.Y, pixel, ShadowColor, worldPos: true, null, clipRect);
		renderer.DrawString(text, font, size, x - currentView.X, y - currentView.Y, pixel, color, worldPos: true, null, clipRect);
	}

	private static float Snap(float world, float viewOrigin, float zoom)
	{
		return viewOrigin + MathF.Round((world - viewOrigin) * zoom) / zoom;
	}
}
