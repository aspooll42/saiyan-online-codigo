using System;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Localization;

namespace Intersect.Client.Interface.Menu;

internal static class MenuLoadingScreen
{
	private static bool _active;

	private static long _startedAt = -2305843009213693952L;

	private static long _readyAt = -1L;

	private static float _progressAtReady;

	private static string _destination = string.Empty;

	private static long _destinationAt = -2305843009213693952L;

	private static string _tip = string.Empty;

	private static long Now => MenuDraw.Maintenant;

	public static bool IsActive => _active;

	public static void Start(string? destination, string tip)
	{
		_active = true;
		_startedAt = Now;
		_readyAt = -1L;
		_destination = destination?.Trim() ?? string.Empty;
		_destinationAt = -2305843009213693952L;
		_tip = tip;
	}

	public static void Stop()
	{
		_active = false;
	}

	private static float Waiting(long now)
	{
		return 0.85f * MenuDraw.EaseOut((float)(now - _startedAt - 450) / 2600f);
	}

	private static float Progress(long now)
	{
		if (_readyAt >= 0)
		{
			return MenuDraw.Lerp(_progressAtReady, 1f, MenuDraw.EaseOut((float)(now - _readyAt) / 400f));
		}
		return Waiting(now);
	}

	public static bool ReadyToEnter(string? place)
	{
		if (!_active)
		{
			return true;
		}
		long now = Now;
		if (!string.IsNullOrWhiteSpace(place) && !string.Equals(_destination, place.Trim(), StringComparison.Ordinal))
		{
			_destination = place.Trim();
			_destinationAt = now;
		}
		if (_readyAt < 0)
		{
			if (now - _startedAt < 900)
			{
				return false;
			}
			_progressAtReady = Waiting(now);
			_readyAt = now;
		}
		if (now - _readyAt < 450)
		{
			return false;
		}
		_active = false;
		return true;
	}

	public static void Draw()
	{
		if (!_active || MenuDraw.Renderer == null)
		{
			return;
		}
		long now = Now;
		MenuDraw.Begin();
		float num = MenuDraw.Clamp01((float)(now - _startedAt) / 500f);
		float width = MenuDraw.Width;
		float height = MenuDraw.Height;
		MenuDraw.Rect(0f, 0f, width, height, MenuDraw.Rgba(5, 7, 12, num));
		DrawPainting(num);
		float num2 = height - 150f;
		float num3 = width - 192f;
		float num4 = MenuDraw.Clamp01(Progress(now));
		MenuDraw.Rect(96f, num2 - 4f, num3, 4f, MenuDraw.Rgba(255, 255, 255, 0.1f * num));
		if (num4 > 0.001f)
		{
			IGameTexture radial = MenuTextures.Radial;
			if (radial != null)
			{
				MenuDraw.Texture(radial, 82f, num2 - 18f, num3 * num4 + 28f, 32f, MenuDraw.Rgba(255, 122, 26, 0.45f * num));
			}
			MenuDraw.HorizontalRamp(MenuTextures.Ramp("ki", (0f, MenuDraw.Ki), (1f, MenuDraw.KiLight)), 96f, num2 - 4f, num3 * num4, 4f, MenuDraw.Alpha(Color.White, num));
		}
		float num5 = num2 - 4f - 42f - 30.8f;
		MenuDraw.Text(_tip, MenuFont.ChakraMedium, 22f, 96f, num5, MenuDraw.Alpha(MenuDraw.Mist, num), MenuAlign.Left, 0f, 30.8f);
		float num6 = num5 - 18f - 132f;
		if (_destination.Length > 0)
		{
			float alpha = num * MenuDraw.Clamp01((float)(now - _destinationAt) / 400f);
			string text = MenuDraw.Upper(_destination);
			float item = MenuDraw.Measure(text, MenuFont.Saira, 150f, 0.005f).Width;
			float px = ((item > num3) ? (150f * num3 / item) : 150f);
			MenuDraw.Text(text, MenuFont.Saira, px, 96f, num6, MenuDraw.Alpha(MenuDraw.White, alpha), MenuAlign.Left, 0.005f, 132f);
		}
		MenuDraw.Text(MenuDraw.Upper(Strings.MenuScene.Destination), MenuFont.Chakra, 14f, 96f, num6 - 24f, MenuDraw.Alpha(MenuDraw.KiLight, num), MenuAlign.Left, 0.24f, 14f);
		MenuDraw.End();
	}

	private static void DrawPainting(float alpha)
	{
		IGameTexture paintingBlurredStrong = MenuTextures.PaintingBlurredStrong;
		if (paintingBlurredStrong != null)
		{
			GameRenderer renderer = MenuDraw.Renderer;
			float num = renderer.ScreenWidth;
			float num2 = renderer.ScreenHeight;
			float num3 = 40f * MenuDraw.Scale;
			float num4 = MathF.Max((num + num3 * 2f) / (float)paintingBlurredStrong.Width, (num2 + num3 * 2f) / (float)paintingBlurredStrong.Height);
			float num5 = (float)paintingBlurredStrong.Width * num4;
			float num6 = (float)paintingBlurredStrong.Height * num4;
			renderer.DrawTexture(paintingBlurredStrong, 0f, 0f, paintingBlurredStrong.Width, paintingBlurredStrong.Height, (num - num5) / 2f, (num2 - num6) / 2f, num5, num6, new Color((int)(255f * alpha), 89, 89, 89));
		}
	}
}
