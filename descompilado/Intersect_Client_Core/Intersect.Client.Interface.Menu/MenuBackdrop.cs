using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Framework.Graphics;

namespace Intersect.Client.Interface.Menu;

internal sealed class MenuBackdrop
{
	private struct Glint
	{
		public float X;

		public float Y;

		public bool Warm;

		public float Phase;

		public float Period;

		public float Strength;
	}

	private readonly record struct Frame(float Scale, float OriginX, float OriginY, float OffsetX, float OffsetY, float Cover, float Left, float Top, float StageToTexel)
	{
		public float Unit => Cover * Scale * StageToTexel;

		public float X(float texelX)
		{
			return OriginX + (Left + texelX * Cover - OriginX) * Scale + OffsetX;
		}

		public float Y(float texelY)
		{
			return OriginY + (Top + texelY * Cover - OriginY) * Scale + OffsetY;
		}
	}

	private static readonly Color Veil = new Color(255, 8, 12, 26);

	private static bool _analyzed;

	private static (float X, float Y) _sun = (X: 283f, Y: 625f);

	private static (float X, float Y)? _eye;

	private static readonly List<Glint> Glints = new List<Glint>();

	private readonly (float Angle, float Length, float Width, float Alpha, float Speed, float Phase)[] _rays;

	private readonly (float Y, float Speed, float Alpha, float Offset)[] _mists = new(float, float, float, float)[3]
	{
		(615f, 9f, 0.09f, 0f),
		(668f, 14f, 0.07f, 1300f),
		(722f, 6f, 0.05f, 2500f)
	};

	private readonly (float Y, float Duration, float Offset, float Unit, float Beat)[] _birds = new(float, float, float, float, float)[3]
	{
		(330f, 46f, 4f, 3f, 0f),
		(372f, 54f, 12f, 2f, 0.35f),
		(268f, 63f, 38f, 2f, 0.8f)
	};

	private static readonly (int X, int Y)[][] Wings = new(int, int)[3][]
	{
		new(int, int)[9]
		{
			(-4, -3),
			(-3, -2),
			(-2, -1),
			(-1, 0),
			(0, 0),
			(1, 0),
			(2, -1),
			(3, -2),
			(4, -3)
		},
		new(int, int)[9]
		{
			(-4, -1),
			(-3, -1),
			(-2, 0),
			(-1, 0),
			(0, 0),
			(1, 0),
			(2, 0),
			(3, -1),
			(4, -1)
		},
		new(int, int)[9]
		{
			(-4, 1),
			(-3, 1),
			(-2, 0),
			(-1, 0),
			(0, 0),
			(1, 0),
			(2, 0),
			(3, 1),
			(4, 1)
		}
	};

	private float _cameraX;

	private float _cameraY;

	private float _cameraTargetX;

	private float _cameraTargetY;

	private long _shakeAt = -2305843009213693952L;

	private long _grainAt;

	private int _grainX;

	private int _grainY;

	private readonly Random _random = new Random();

	public long IntroStart { get; set; } = -2305843009213693952L;

	private static IGameTexture? VeilLeft => MenuTextures.Ramp("veil-left", (0f, Rgba(0.74f)), (0.25f, Rgba(0.54f)), (0.47f, Rgba(0.14f)), (0.6f, Rgba(0f)), (1f, Rgba(0f)));

	private static IGameTexture? VeilBottom => MenuTextures.RampVertical("veil-bottom", (0f, Rgba(0f)), (0.78f, Rgba(0f)), (1f, Rgba(0.7f)));

	private static IGameTexture? VeilTop => MenuTextures.RampVertical("veil-top", (0f, Rgba(0.36f)), (0.16f, Rgba(0f)), (1f, Rgba(0f)));

	private static IGameTexture? StreakRamp => MenuTextures.Ramp("streak", (0f, new Color(0, 255, 255, 255)), (0.5f, new Color(255, 255, 255, 255)), (1f, new Color(0, 255, 255, 255)));

	public MenuBackdrop()
	{
		_rays = new float[9] { -168f, -150f, -129f, -108f, -87f, -66f, -45f, -27f, -12f }.Select((float degrees, int i) => (degrees * (float)Math.PI / 180f, 0.7f + (float)(i * 37 % 5) / 10f, 0.5f + (float)(i * 53 % 7) / 10f, 0.035f + (float)(i * 29 % 4) / 100f, 0.11f + (float)i * 0.013f, (float)i * 1.7f)).ToArray();
	}

	public void Shake(long now)
	{
		_shakeAt = now;
	}

	public void Update(float dt, float mouseX, float mouseY, bool mouseInside)
	{
		_cameraTargetX = (mouseInside ? ((0f - (mouseX - MenuDraw.Width / 2f)) / (MenuDraw.Width / 2f) * 16f) : 0f);
		_cameraTargetY = (mouseInside ? ((0f - (mouseY - MenuDraw.Height / 2f)) / (MenuDraw.Height / 2f) * 9f) : 0f);
		_cameraX = MenuDraw.Approach(_cameraX, _cameraTargetX, dt, 3.5f);
		_cameraY = MenuDraw.Approach(_cameraY, _cameraTargetY, dt, 3.5f);
	}

	private Frame CurrentFrame(long now, IGameTexture painting)
	{
		GameRenderer renderer = MenuDraw.Renderer;
		float num = renderer.ScreenWidth;
		float num2 = renderer.ScreenHeight;
		float num3 = painting.Width;
		float num4 = painting.Height;
		float num5 = MathF.Max(num / num3, num2 / num4);
		float num6 = (float)now / 1000f;
		float num7 = MenuDraw.Lerp(1.09f, 1f, MenuDraw.EaseOut((float)(now - IntroStart) / 2600f));
		float scale = (1.075f + MathF.Sin(num6 / 23f) * 0.008f) * num7;
		float num8 = 0f;
		float num9 = 0f;
		float num10 = (float)(now - _shakeAt) / 260f;
		if (num10 >= 0f && num10 < 1f)
		{
			num8 = ((float)_random.NextDouble() - 0.5f) * 10f * (1f - num10);
			num9 = ((float)_random.NextDouble() - 0.5f) * 8f * (1f - num10);
		}
		float num11 = MathF.Max(1920f / num3, 1080f / num4);
		return new Frame(scale, num * 0.5f, num2 * 0.55f, (MathF.Sin(num6 / 31f) * 10f + _cameraX + num8) * MenuDraw.Scale, (MathF.Cos(num6 / 27f) * 6f + _cameraY + num9) * MenuDraw.Scale, num5, (num - num3 * num5) / 2f, (num2 - num4 * num5) / 2f, 1f / num11);
	}

	private static void DrawPainting(Frame frame, IGameTexture texture, float paintingWidth, float paintingHeight, Color color, IGameRenderTexture? target = null)
	{
		float tx = frame.X(0f);
		float ty = frame.Y(0f);
		float tw = paintingWidth * frame.Cover * frame.Scale;
		float th = paintingHeight * frame.Cover * frame.Scale;
		MenuDraw.Renderer.DrawTexture(texture, 0f, 0f, texture.Width, texture.Height, tx, ty, tw, th, color, target);
	}

	private static void Analyze()
	{
		if (_analyzed)
		{
			return;
		}
		byte[] pixels = MenuTextures.PaintingPixels;
		if (pixels == null)
		{
			return;
		}
		_analyzed = true;
		int width = MenuTextures.PaintingPixelWidth;
		int height = MenuTextures.PaintingPixelHeight;
		float stageCover = MathF.Max(1920f / (float)width, 1080f / (float)height);
		float num = 0f;
		float num2 = 0f;
		int num3 = 0;
		for (int i = T(600f); i < Math.Min(height, T(820f)); i += 2)
		{
			for (int j = T(60f); j < Math.Min(width, T(700f)); j += 2)
			{
				if (Luma(j, i) > 246f)
				{
					num += (float)j;
					num2 += (float)i;
					num3++;
				}
			}
		}
		if (num3 > 20)
		{
			_sun = (X: num / (float)num3, Y: num2 / (float)num3);
		}
		List<Glint> list = new List<Glint>();
		int num4 = Math.Max(3, T(5f));
		for (int k = (int)(_sun.Y + (float)T(34f)); k < height - num4; k += 3)
		{
			for (int l = num4; l < width - num4; l += 3)
			{
				if ((l > T(1240f) && k < T(880f)) || (l > T(760f) && k < T(792f)))
				{
					continue;
				}
				float num5 = Luma(l, k);
				if (num5 < 196f)
				{
					continue;
				}
				int num6 = (k * width + l) * 4;
				bool flag = pixels[num6] - pixels[num6 + 2] > 50;
				if (!flag || !(MathF.Abs((float)l - _sun.X) > (float)T(170f)))
				{
					float num7 = (Luma(l - num4, k) + Luma(l + num4, k) + Luma(l, k - num4 + 1) + Luma(l, k + num4 - 1)) / 4f;
					if (!(num5 - num7 < 16f))
					{
						list.Add(new Glint
						{
							X = l,
							Y = k,
							Warm = flag
						});
					}
				}
			}
		}
		Random random = new Random(761);
		for (int num8 = list.Count - 1; num8 > 0; num8--)
		{
			int num9 = random.Next(num8 + 1);
			List<Glint> list2 = list;
			int index = num8;
			int index2 = num9;
			Glint value = list[num9];
			Glint value2 = list[num8];
			list2[index] = value;
			list[index2] = value2;
		}
		Glints.Clear();
		foreach (Glint item2 in list.Take(340))
		{
			Glint item = item2;
			item.Phase = (float)random.NextDouble() * 10f;
			item.Period = 1.3f + (float)random.NextDouble() * 2.7f;
			item.Strength = 0.5f + (float)random.NextDouble() * 0.5f;
			Glints.Add(item);
		}
		float num10 = 0f;
		float num11 = 0f;
		int num12 = 0;
		for (int m = T(150f); m < Math.Min(height, T(220f)); m++)
		{
			for (int n = T(1350f); n < Math.Min(width, T(1440f)); n++)
			{
				int num13 = (m * width + n) * 4;
				if (pixels[num13] > 185 && pixels[num13 + 1] < 75 && pixels[num13 + 2] < 75)
				{
					num10 += (float)n;
					num11 += (float)m;
					num12++;
				}
			}
		}
		_eye = ((num12 > 3) ? new(float, float)?((num10 / (float)num12, num11 / (float)num12)) : (((float, float)?)null));
		float Luma(int x, int y)
		{
			x = Math.Clamp(x, 0, width - 1);
			y = Math.Clamp(y, 0, height - 1);
			int num14 = (y * width + x) * 4;
			return (float)(int)pixels[num14] * 0.299f + (float)(int)pixels[num14 + 1] * 0.587f + (float)(int)pixels[num14 + 2] * 0.114f;
		}
		int T(float stage)
		{
			return (int)(stage / stageCover);
		}
	}

	private static Color Rgba(float alpha)
	{
		return MenuDraw.Alpha(Veil, alpha);
	}

	public void PaintGlass(long now, float veil, float blur)
	{
		MenuDraw.GlassReady = false;
		IGameTexture painting = MenuTextures.Painting;
		if (painting == null)
		{
			return;
		}
		IGameTexture paintingBlurredStrong = MenuTextures.PaintingBlurredStrong;
		if (paintingBlurredStrong == null)
		{
			return;
		}
		IGameRenderTexture glassTarget = MenuDraw.GlassTarget;
		if (glassTarget != null)
		{
			GameRenderer renderer = MenuDraw.Renderer;
			glassTarget.Clear(MenuDraw.Night);
			Frame frame = CurrentFrame(now, painting);
			DrawPainting(frame, paintingBlurredStrong, painting.Width, painting.Height, Color.White, glassTarget);
			(float, float) tuple = (renderer.ScreenWidth, renderer.ScreenHeight);
			renderer.DrawTexture(MenuDraw.Pixel, 0f, 0f, 1f, 1f, 0f, 0f, tuple.Item1, tuple.Item2, MenuDraw.Rgba(0, 0, 0, 0.5f * blur), glassTarget);
			IGameTexture veilLeft = VeilLeft;
			if (veilLeft != null)
			{
				renderer.DrawTexture(veilLeft, 0f, 0f, veilLeft.Width, 1f, 0f, 0f, tuple.Item1, tuple.Item2, MenuDraw.Alpha(Color.White, veil), glassTarget);
			}
			IGameTexture veilBottom = VeilBottom;
			if (veilBottom != null)
			{
				renderer.DrawTexture(veilBottom, 0f, 0f, 1f, veilBottom.Height, 0f, 0f, tuple.Item1, tuple.Item2, MenuDraw.Alpha(Color.White, veil), glassTarget);
			}
			glassTarget.End();
			MenuDraw.GlassReady = true;
		}
	}

	public void Draw(long now, float veil, float blur, float spotlight, float focusX)
	{
		GameRenderer renderer = MenuDraw.Renderer;
		float num = renderer.ScreenWidth;
		float num2 = renderer.ScreenHeight;
		IGameTexture painting = MenuTextures.Painting;
		if (painting == null)
		{
			return;
		}
		Analyze();
		Frame frame = CurrentFrame(now, painting);
		float t = (float)now / 1000f;
		DrawPainting(frame, painting, painting.Width, painting.Height, Color.White);
		DrawEffects(frame, t);
		if (blur > 0.001f)
		{
			IGameTexture paintingBlurredSoft = MenuTextures.PaintingBlurredSoft;
			if (paintingBlurredSoft != null)
			{
				DrawPainting(frame, paintingBlurredSoft, painting.Width, painting.Height, MenuDraw.Alpha(Color.White, blur));
				MenuDraw.ScreenRect(0f, 0f, num, num2, MenuDraw.Rgba(0, 0, 0, 0.5f * blur));
			}
		}
		IGameTexture veilLeft = VeilLeft;
		if (veilLeft != null)
		{
			renderer.DrawTexture(veilLeft, 0f, 0f, veilLeft.Width, 1f, 0f, 0f, num, num2, MenuDraw.Alpha(Color.White, veil));
		}
		IGameTexture veilBottom = VeilBottom;
		if (veilBottom != null)
		{
			renderer.DrawTexture(veilBottom, 0f, 0f, 1f, veilBottom.Height, 0f, 0f, num, num2, MenuDraw.Alpha(Color.White, veil));
		}
		IGameTexture veilTop = VeilTop;
		if (veilTop != null)
		{
			renderer.DrawTexture(veilTop, 0f, 0f, 1f, veilTop.Height, 0f, 0f, num, num2, MenuDraw.Alpha(Color.White, veil));
		}
		if (spotlight > 0.001f)
		{
			float scale = MenuDraw.Scale;
			float num3 = focusX * scale;
			MenuDraw.Renderer.DrawTexture(MenuTextures.Radial, 0f, 0f, 128f, 128f, num3 - 450f * scale, (MenuDraw.Height * 0.68f - 375f) * scale, 900f * scale, 750f * scale, MenuDraw.Rgba(120, 160, 255, 0.14f * spotlight));
			MenuDraw.Renderer.DrawTexture(MenuTextures.Ring, 0f, 0f, 128f, 128f, num3 - 1500f * scale, (MenuDraw.Height * 0.6f - 1000f) * scale, 3000f * scale, 2000f * scale, MenuDraw.Rgba(3, 5, 10, 0.6f * spotlight));
		}
		IGameTexture ring = MenuTextures.Ring;
		if (ring != null)
		{
			renderer.DrawTexture(ring, 0f, 0f, 128f, 128f, num * -0.58000004f, num2 * -0.42f, num * 2.4f, num2 * 1.8f, MenuDraw.Rgba(0, 0, 0, 0.55f));
		}
		DrawGrain(now, num, num2);
	}

	private void DrawGrain(long now, float width, float height)
	{
		IGameTexture noise = MenuTextures.Noise;
		if (noise == null)
		{
			return;
		}
		if (now - _grainAt > 70)
		{
			_grainAt = now;
			_grainX = _random.Next(256);
			_grainY = _random.Next(256);
		}
		Color renderColor = MenuDraw.Rgba(255, 255, 255, 0.035f);
		for (int i = -_grainY; (float)i < height; i += 256)
		{
			for (int j = -_grainX; (float)j < width; j += 256)
			{
				MenuDraw.Renderer.DrawTexture(noise, 0f, 0f, 256f, 256f, j, i, 256f, 256f, renderColor);
			}
		}
	}

	private void DrawEffects(Frame frame, float t)
	{
		float unit = frame.Unit;
		GameRenderer renderer = MenuDraw.Renderer;
		float num = frame.X(_sun.X);
		float num2 = frame.Y(_sun.Y);
		IGameTexture ray = MenuTextures.Ray;
		if (ray != null)
		{
			(float, float, float, float, float, float)[] rays = _rays;
			for (int i = 0; i < rays.Length; i++)
			{
				(float, float, float, float, float, float) tuple = rays[i];
				float item = tuple.Item1;
				float item2 = tuple.Item2;
				float item3 = tuple.Item3;
				float item4 = tuple.Item4;
				float item5 = tuple.Item5;
				float item6 = tuple.Item6;
				float num3 = item + MathF.Sin(t * item5 + item6) * 0.02f;
				float num4 = 1200f * item2 * unit;
				float num5 = 100f * item3 * unit;
				float num6 = num + MathF.Cos(num3) * num4 / 2f;
				float num7 = num2 + MathF.Sin(num3) * num4 / 2f;
				renderer.DrawTexture(ray, 0f, 0f, ray.Width, ray.Height, num6 - num4 / 2f, num7 - num5 / 2f, num4, num5, MenuDraw.Rgba(255, 190, 110, item4 * (0.7f + 0.3f * MathF.Sin(t * item5 * 2f + item6))), null, GameBlendModes.Add, null, num3 * 180f / (float)Math.PI);
			}
		}
		IGameTexture radial = MenuTextures.Radial;
		if (radial != null)
		{
			float num8 = 260f * unit;
			renderer.DrawTexture(radial, 0f, 0f, 128f, 128f, num - num8, num2 - num8, num8 * 2f, num8 * 2f, MenuDraw.Rgba(255, 214, 150, 0.2f + 0.05f * MathF.Sin(t * 0.8f)), null, GameBlendModes.Add);
		}
		IGameTexture streakRamp = StreakRamp;
		if (streakRamp != null)
		{
			float num9 = 0.34f + 0.08f * MathF.Sin(t * 1.1f);
			renderer.DrawTexture(streakRamp, 0f, 0f, 256f, 1f, num - 760f * unit, num2 - unit, 1520f * unit, 2f * unit, MenuDraw.Rgba(255, 228, 184, num9), null, GameBlendModes.Add);
			renderer.DrawTexture(streakRamp, 0f, 0f, 256f, 1f, num - 760f * unit, num2 - 6f * unit, 1520f * unit, 12f * unit, MenuDraw.Rgba(255, 228, 184, num9 * 0.35f), null, GameBlendModes.Add);
		}
		float stageToTexel = frame.StageToTexel;
		IGameTexture mist = MenuTextures.Mist;
		if (mist != null)
		{
			(float, float, float, float)[] mists = _mists;
			for (int j = 0; j < mists.Length; j++)
			{
				(float, float, float, float) tuple2 = mists[j];
				float item7 = tuple2.Item1;
				float item8 = tuple2.Item2;
				float item9 = tuple2.Item3;
				float item10 = tuple2.Item4;
				float num10 = (t * item8 + item10) % 3600f - 1700f;
				float tx = frame.X(num10 * stageToTexel);
				float ty = frame.Y((item7 - 55f) * stageToTexel);
				renderer.DrawTexture(mist, 0f, 0f, mist.Width, mist.Height, tx, ty, 1600f * unit, 110f * unit, MenuDraw.Rgba(236, 242, 255, item9));
			}
		}
		(float, float, float, float, float)[] birds = _birds;
		for (int k = 0; k < birds.Length; k++)
		{
			(float, float, float, float, float) tuple3 = birds[k];
			float item11 = tuple3.Item1;
			float item12 = tuple3.Item2;
			float item13 = tuple3.Item3;
			float item14 = tuple3.Item4;
			float item15 = tuple3.Item5;
			float num11 = (t + item13) % item12 / item12;
			float num12 = MathF.Round(-80f + num11 * 1260f);
			float num13 = MathF.Round(item11 - num11 * 60f + MathF.Sin(t * 0.7f + item13) * 12f);
			float num14 = (t + item15) % 2.4f;
			int num15 = ((!(num14 < 1f)) ? 1 : (new int[4] { 0, 1, 2, 1 })[(int)(num14 / 0.125f) % 4]);
			Color color = MenuDraw.Rgba(24, 20, 36, 0.75f * MenuDraw.Clamp01((1180f - num12) / 160f));
			float num16 = item14 * unit;
			(int, int)[] array = Wings[num15];
			for (int l = 0; l < array.Length; l++)
			{
				var (num17, num18) = array[l];
				MenuDraw.ScreenRect(frame.X((num12 + (float)num17 * item14) * stageToTexel), frame.Y((num13 + (float)num18 * item14) * stageToTexel), num16, num16, color);
			}
		}
		foreach (Glint glint in Glints)
		{
			float num19 = (t + glint.Phase) % glint.Period / glint.Period;
			int num20 = (int)(num19 * 12f);
			if (num20 <= 4)
			{
				float alpha = (new float[5] { 0.35f, 0.75f, 1f, 0.75f, 0.35f })[num20] * glint.Strength;
				Color color2 = (glint.Warm ? MenuDraw.Rgba(255, 232, 178, alpha) : MenuDraw.Rgba(222, 242, 255, alpha));
				int num21 = (new int[5] { 0, 3, 6, 3, 0 })[num20];
				float num22 = frame.X(glint.X);
				float num23 = frame.Y(glint.Y);
				MenuDraw.ScreenRect(num22 - (float)num21 * unit, num23, (float)(2 + num21 * 2) * unit, 2f * unit, color2, GameBlendModes.Add);
				if (num20 == 2)
				{
					MenuDraw.ScreenRect(num22, num23 - 2f * unit, 2f * unit, 6f * unit, color2, GameBlendModes.Add);
				}
			}
		}
		(float, float)? eye = _eye;
		if (!eye.HasValue)
		{
			return;
		}
		(float, float) valueOrDefault = eye.GetValueOrDefault();
		IGameTexture radial2 = MenuTextures.Radial;
		if (radial2 != null)
		{
			int num24 = (int)(t % 6.5f / 0.07f);
			if (num24 <= 6)
			{
				int num25 = (new int[7] { 1, 2, 4, 6, 4, 2, 1 })[num24];
				float num26 = frame.X(valueOrDefault.Item1);
				float num27 = frame.Y(valueOrDefault.Item2);
				float num28 = 26f * unit;
				renderer.DrawTexture(radial2, 0f, 0f, 128f, 128f, num26 - num28, num27 - num28, num28 * 2f, num28 * 2f, MenuDraw.Rgba(255, 80, 60, 0.12f * (float)num25 / 6f + 0.05f), null, GameBlendModes.Add);
				Color color3 = MenuDraw.Rgba(255, 96, 72, 0.9f);
				MenuDraw.ScreenRect(num26 - (float)(num25 * 2) * unit, num27 - unit, (float)(num25 * 4 + 2) * unit, 2f * unit, color3, GameBlendModes.Add);
				MenuDraw.ScreenRect(num26 - unit, num27 - (float)num25 * unit, 2f * unit, (float)(num25 * 2 + 2) * unit, color3, GameBlendModes.Add);
				MenuDraw.ScreenRect(num26 - unit, num27 - unit, 3f * unit, 3f * unit, MenuDraw.Rgba(255, 244, 226, 0.95f), GameBlendModes.Add);
			}
		}
	}
}
