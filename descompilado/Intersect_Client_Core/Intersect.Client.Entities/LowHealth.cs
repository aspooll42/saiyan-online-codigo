using System;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Framework.Core;

namespace Intersect.Client.Entities;

public static class LowHealth
{
	private const float Start = 0.3f;

	private const float Full = 0.1f;

	private const float SlowBpm = 70f;

	private const float FastBpm = 130f;

	private static readonly Color Dull = new Color(255, 112, 112, 118);

	private const float DullAlpha = 0.3f;

	private static readonly Color Veil = new Color(255, 150, 6, 18);

	private static readonly Color HitEdge = new Color(255, 230, 20, 30);

	private const long HitEdgeMs = 320L;

	private static float _shown = 1f;

	private static double _clock;

	private static long _lastBeat = -1L;

	private static long _lastUpdate;

	private static long _dubAt = -1L;

	private static float _lastLowness;

	private static long _hitAt = -2305843009213693952L;

	private static int _hitSide;

	public static void OnLocalHit(float recoilX)
	{
		_hitAt = Timing.Global.Milliseconds;
		_hitSide = ((recoilX > 0.25f) ? (-1) : ((recoilX < -0.25f) ? 1 : 0));
	}

	public static void Draw()
	{
		Player me = Globals.Me;
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (me == null || gameTexture == null)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		long num = Math.Clamp(milliseconds - _lastUpdate, 0L, 100L);
		_lastUpdate = milliseconds;
		long num2 = me.MaxVital[0];
		long num3 = me.Vital[0];
		float num4 = ((num2 > 0 && num3 > 0) ? Math.Clamp((float)num3 / (float)num2, 0f, 1f) : 1f);
		_shown += (num4 - _shown) * Math.Min(1f, (float)num / 180f);
		float num5 = Math.Clamp((0.3f - _shown) / 0.20000002f, 0f, 1f);
		float num6 = 60000f / (70f + 60f * num5);
		if (num5 > 0f && _lastLowness <= 0f)
		{
			_clock = Math.Ceiling(_clock) - 0.001;
		}
		_lastLowness = num5;
		_clock += (float)num / num6;
		long num7 = (long)Math.Floor(_clock);
		float num8 = (float)(_clock - (double)num7);
		float num9 = Math.Clamp(0.3f * num6 / 1000f, 0.15f, 0.3f);
		float num10 = MathF.Exp((0f - num8) * num6 / 90f);
		float num11 = num8 - num9 * 1000f / num6;
		float num12 = ((num11 > 0f) ? (0.7f * MathF.Exp((0f - num11) * num6 / 90f)) : 0f);
		float num13 = Math.Min(1f, num10 + num12);
		if (num5 > 0f && num7 != _lastBeat)
		{
			_lastBeat = num7;
			Audio.AddGameSound("coeur_pv_bas_1", loop: false);
			_dubAt = milliseconds + (long)(num9 * 1000f);
		}
		else if (num5 <= 0f)
		{
			_lastBeat = num7;
		}
		if (_dubAt > 0 && milliseconds >= _dubAt)
		{
			_dubAt = -1L;
			if (num5 > 0f)
			{
				Audio.AddGameSound("coeur_pv_bas_2", loop: false);
			}
		}
		FloatRect worldViewport = Graphics.WorldViewport;
		if (num5 > 0.01f)
		{
			Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), worldViewport, Tint(Dull, 0.3f * num5));
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "pv_bord_cote.png");
			if (texture != null)
			{
				DrawSoftEdges(texture, worldViewport, MathF.Min(worldViewport.Width, worldViewport.Height) * 0.3f, Tint(Veil, num5 * (0.28f + 0.4f * num13)));
			}
		}
		float num14 = 1f - (float)(milliseconds - _hitAt) / 320f;
		if (!(num14 > 0f))
		{
			return;
		}
		IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Misc, "pv_bord_cote.png");
		if (texture2 != null)
		{
			float num15 = worldViewport.Width * 0.23f;
			float alpha = 0.6f * num14 * ((_hitSide == 0) ? 0.6f : 1f);
			FloatRect srcRectangle = new FloatRect(0f, 0f, texture2.Width, texture2.Height);
			if (_hitSide <= 0)
			{
				Graphics.DrawGameTexture(texture2, srcRectangle, new FloatRect(worldViewport.X, worldViewport.Y, num15, worldViewport.Height), Tint(HitEdge, alpha));
			}
			if (_hitSide >= 0)
			{
				Graphics.DrawGameTexture(texture2, srcRectangle, new FloatRect(worldViewport.X + worldViewport.Width - num15, worldViewport.Y, num15, worldViewport.Height), Tint(HitEdge, alpha), null, GameBlendModes.None, null, 180f);
			}
		}
	}

	public static void DrawSoftEdges(IGameTexture edge, FloatRect view, float thickness, Color color)
	{
		FloatRect srcRectangle = new FloatRect(0f, 0f, edge.Width, edge.Height);
		Graphics.DrawGameTexture(edge, srcRectangle, new FloatRect(view.X, view.Y, thickness, view.Height), color);
		Graphics.DrawGameTexture(edge, srcRectangle, new FloatRect(view.X + view.Width - thickness, view.Y, thickness, view.Height), color, null, GameBlendModes.None, null, 180f);
		float num = view.X + view.Width / 2f;
		float num2 = view.Y + thickness / 2f;
		float num3 = view.Y + view.Height - thickness / 2f;
		Graphics.DrawGameTexture(edge, srcRectangle, new FloatRect(num - thickness / 2f, num2 - view.Width / 2f, thickness, view.Width), color, null, GameBlendModes.None, null, 90f);
		Graphics.DrawGameTexture(edge, srcRectangle, new FloatRect(num - thickness / 2f, num3 - view.Width / 2f, thickness, view.Width), color, null, GameBlendModes.None, null, -90f);
	}

	private static Color Tint(Color color, float alpha)
	{
		return new Color((int)Math.Clamp(alpha * 255f, 0f, 255f), color.R, color.G, color.B);
	}
}
