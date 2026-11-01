using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Configuration;

namespace Intersect.Client.Interface.Menu;

internal static class MenuTextures
{
	public const int LogoWidth = 900;

	public const int LogoHeight = 450;

	public const int SweepFrames = 16;

	public const int SweepColumns = 8;

	public const int SweepFrameWidth = 450;

	public const int SweepFrameHeight = 225;

	private static IGameTexture? _radial;

	private static IGameTexture? _ring;

	private static IGameTexture? _ray;

	private static IGameTexture? _mist;

	private static IGameTexture? _noise;

	private static IGameTexture? _weather;

	private static readonly Dictionary<string, IGameTexture?> Ramps = new Dictionary<string, IGameTexture>();

	public static IGameTexture? Painting => GameContentManager.Current.GetTexture(TextureType.Gui, PaintingName);

	public static string PaintingName
	{
		get
		{
			List<string> menuBackground = ClientConfiguration.Instance.MenuBackground;
			if (menuBackground == null || menuBackground.Count <= 0)
			{
				return "background.png";
			}
			return menuBackground[0];
		}
	}

	public static IGameTexture? PaintingBlurredStrong { get; private set; }

	public static IGameTexture? PaintingBlurredSoft { get; private set; }

	public static IGameTexture? Logo { get; private set; }

	public static IGameTexture? LogoSweep { get; private set; }

	public static IReadOnlyList<(int X, int Y)> LogoEdges { get; private set; } = Array.Empty<(int, int)>();

	public static byte[]? PaintingPixels { get; private set; }

	public static int PaintingPixelWidth { get; private set; }

	public static int PaintingPixelHeight { get; private set; }

	public static IGameTexture? Radial => _radial ?? (_radial = Build("menu:radial", 128, 128, delegate(int x, int y)
	{
		float num = MathF.Sqrt(Sq(((float)x + 0.5f) / 64f - 1f) + Sq(((float)y + 0.5f) / 64f - 1f));
		float num2 = MathF.Max(0f, 1f - num);
		return (R: 255, G: 255, B: 255, A: num2 * num2 * (3f - 2f * num2));
	}));

	public static IGameTexture? Ring => _ring ?? (_ring = Build("menu:ring", 128, 128, delegate(int x, int y)
	{
		float num = MathF.Sqrt(Sq(((float)x + 0.5f) / 64f - 1f) + Sq(((float)y + 0.5f) / 64f - 1f));
		float num2 = Math.Clamp((num - 0.42f) / 0.58f, 0f, 1f);
		return (R: 255, G: 255, B: 255, A: num2 * num2);
	}));

	public static IGameTexture? Ray => _ray ?? (_ray = Build("menu:ray", 512, 64, delegate(int x, int y)
	{
		float num = ((float)x + 0.5f) / 512f;
		float num2 = 0.04f + 0.96f * num;
		float num3 = MathF.Abs(((float)y + 0.5f) / 32f - 1f);
		float num4 = Math.Clamp((num2 - num3) / MathF.Max(0.02f, num2 * 0.35f), 0f, 1f);
		float num5 = ((num < 0.3f) ? (1f - 0.55f * (num / 0.3f)) : (0.45f * (1f - (num - 0.3f) / 0.7f)));
		return (R: 255, G: 255, B: 255, A: num4 * num5);
	}));

	public static IGameTexture? Mist => _mist ?? (_mist = Build("menu:mist", 512, 48, delegate(int x, int y)
	{
		float num = ((float)x + 0.5f) / 256f - 1f;
		float num2 = ((float)y + 0.5f) / 24f - 1f;
		float num3 = MathF.Sqrt(num * num + num2 * num2);
		float num4 = MathF.Max(0f, 1f - num3);
		return (R: 255, G: 255, B: 255, A: (num3 < 0.55f) ? (0.9f - 0.55f * (num3 / 0.55f)) : (0.35f * (num4 / 0.45f)));
	}));

	public static IGameTexture? Noise => _noise ?? (_noise = BuildNoise());

	public static IGameTexture? WeatherIcon => _weather ?? (_weather = BuildWeatherIcon());

	public static IGameTexture? Ramp(string key, params (float Position, Color Color)[] stops)
	{
		if (Ramps.TryGetValue(key, out IGameTexture value))
		{
			return value;
		}
		value = Build("menu:ramp:" + key, 256, 1, delegate(int x, int _)
		{
			float num = ((float)x + 0.5f) / 256f;
			int i;
			for (i = 0; i < stops.Length - 2 && num > stops[i + 1].Position; i++)
			{
			}
			(float Position, Color Color) tuple = stops[i];
			float item = tuple.Position;
			Color item2 = tuple.Color;
			(float Position, Color Color) tuple2 = stops[Math.Min(i + 1, stops.Length - 1)];
			float item3 = tuple2.Position;
			Color item4 = tuple2.Color;
			float num2 = ((item3 > item) ? Math.Clamp((num - item) / (item3 - item), 0f, 1f) : 0f);
			return (R: (int)((float)(int)item2.R + (float)(item4.R - item2.R) * num2), G: (int)((float)(int)item2.G + (float)(item4.G - item2.G) * num2), B: (int)((float)(int)item2.B + (float)(item4.B - item2.B) * num2), A: ((float)(int)item2.A + (float)(item4.A - item2.A) * num2) / 255f);
		});
		Ramps[key] = value;
		return value;
	}

	public static IGameTexture? RampVertical(string key, params (float Position, Color Color)[] stops)
	{
		string text = "v:" + key;
		if (Ramps.TryGetValue(text, out IGameTexture value))
		{
			return value;
		}
		value = Build("menu:ramp:" + text, 1, 256, delegate(int _, int y)
		{
			float num = ((float)y + 0.5f) / 256f;
			int i;
			for (i = 0; i < stops.Length - 2 && num > stops[i + 1].Position; i++)
			{
			}
			(float Position, Color Color) tuple = stops[i];
			float item = tuple.Position;
			Color item2 = tuple.Color;
			(float Position, Color Color) tuple2 = stops[Math.Min(i + 1, stops.Length - 1)];
			float item3 = tuple2.Position;
			Color item4 = tuple2.Color;
			float num2 = ((item3 > item) ? Math.Clamp((num - item) / (item3 - item), 0f, 1f) : 0f);
			return (R: (int)((float)(int)item2.R + (float)(item4.R - item2.R) * num2), G: (int)((float)(int)item2.G + (float)(item4.G - item2.G) * num2), B: (int)((float)(int)item2.B + (float)(item4.B - item2.B) * num2), A: ((float)(int)item2.A + (float)(item4.A - item2.A) * num2) / 255f);
		});
		Ramps[text] = value;
		return value;
	}

	public static bool EnsureSourceTextures()
	{
		GameRenderer renderer = MenuDraw.Renderer;
		if (PaintingPixels == null)
		{
			IGameTexture painting = Painting;
			if (painting != null && renderer.TryReadPixels(painting, out byte[] rgba, out int width, out int height))
			{
				PaintingPixels = rgba;
				PaintingPixelWidth = width;
				PaintingPixelHeight = height;
				PaintingBlurredStrong = BuildBlur("menu:painting:strong", rgba, width, height, 4, 3, 3);
				PaintingBlurredSoft = BuildBlur("menu:painting:soft", rgba, width, height, 3, 2, 2);
			}
		}
		if (Logo == null)
		{
			IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Gui, "logo.png");
			if (texture != null && renderer.TryReadPixels(texture, out byte[] rgba2, out int width2, out int height2))
			{
				BuildLogo(rgba2, width2, height2);
			}
		}
		if (PaintingPixels != null)
		{
			return Logo != null;
		}
		return false;
	}

	private static float Sq(float value)
	{
		return value * value;
	}

	private static IGameTexture? Build(string name, int width, int height, Func<int, int, (int R, int G, int B, float A)> pixel)
	{
		byte[] array = new byte[width * height * 4];
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				(int, int, int, float) tuple = pixel(j, i);
				int item = tuple.Item1;
				int item2 = tuple.Item2;
				int item3 = tuple.Item3;
				float item4 = tuple.Item4;
				int num = (i * width + j) * 4;
				array[num] = (byte)Math.Clamp(item, 0, 255);
				array[num + 1] = (byte)Math.Clamp(item2, 0, 255);
				array[num + 2] = (byte)Math.Clamp(item3, 0, 255);
				array[num + 3] = (byte)Math.Clamp(item4 * 255f, 0f, 255f);
			}
		}
		return MenuDraw.Renderer.CreateTextureFromPixels(name, width, height, array);
	}

	private static IGameTexture? BuildNoise()
	{
		Random random = new Random(761);
		return Build("menu:noise", 256, 256, delegate
		{
			int num = 128 + (int)((random.NextDouble() - 0.5) * 96.0);
			return (R: num, G: num, B: num, A: 1f);
		});
	}

	private static IGameTexture? BuildBlur(string name, byte[] source, int width, int height, int factor, int radius, int passes)
	{
		int num = Math.Max(1, width / factor);
		int num2 = Math.Max(1, height / factor);
		float[][] array = new float[4][];
		for (int i = 0; i < 4; i++)
		{
			array[i] = new float[num * num2];
		}
		for (int j = 0; j < num2; j++)
		{
			for (int k = 0; k < num; k++)
			{
				float num3 = 0f;
				float num4 = 0f;
				float num5 = 0f;
				float num6 = 0f;
				int num7 = 0;
				for (int l = 0; l < factor; l++)
				{
					for (int m = 0; m < factor; m++)
					{
						int num8 = Math.Min(width - 1, k * factor + m);
						int num9 = Math.Min(height - 1, j * factor + l);
						int num10 = (num9 * width + num8) * 4;
						num3 += (float)(int)source[num10];
						num4 += (float)(int)source[num10 + 1];
						num5 += (float)(int)source[num10 + 2];
						num6 += (float)(int)source[num10 + 3];
						num7++;
					}
				}
				int num11 = j * num + k;
				array[0][num11] = num3 / (float)num7;
				array[1][num11] = num4 / (float)num7;
				array[2][num11] = num5 / (float)num7;
				array[3][num11] = num6 / (float)num7;
			}
		}
		float[] array2 = new float[num * num2];
		for (int n = 0; n < passes; n++)
		{
			float[][] array3 = array;
			foreach (float[] array4 in array3)
			{
				BoxBlur(array4, array2, num, num2, radius, horizontal: true);
				BoxBlur(array2, array4, num, num2, radius, horizontal: false);
			}
		}
		byte[] array5 = new byte[num * num2 * 4];
		for (int num13 = 0; num13 < num * num2; num13++)
		{
			for (int num14 = 0; num14 < 4; num14++)
			{
				array5[num13 * 4 + num14] = (byte)Math.Clamp(array[num14][num13], 0f, 255f);
			}
		}
		return MenuDraw.Renderer.CreateTextureFromPixels(name, num, num2, array5);
	}

	private static void BoxBlur(float[] source, float[] target, int width, int height, int radius, bool horizontal)
	{
		int num = (horizontal ? width : height);
		int num2 = (horizontal ? height : width);
		int num3 = radius * 2 + 1;
		for (int i = 0; i < num2; i++)
		{
			float num4 = 0f;
			for (int j = -radius; j <= radius; j++)
			{
				num4 += source[Index(Math.Clamp(j, 0, num - 1), i)];
			}
			for (int k = 0; k < num; k++)
			{
				target[Index(k, i)] = num4 / (float)num3;
				num4 += source[Index(Math.Min(num - 1, k + radius + 1), i)] - source[Index(Math.Max(0, k - radius), i)];
			}
		}
		int Index(int along, int line)
		{
			if (!horizontal)
			{
				return along * width + line;
			}
			return line * width + along;
		}
	}

	private static void BuildLogo(byte[] source, int width, int height)
	{
		byte[] logo = new byte[1620000];
		float num = (float)width / 900f;
		float num2 = (float)height / 450f;
		for (int i = 0; i < 450; i++)
		{
			for (int j = 0; j < 900; j++)
			{
				int num3 = (int)((float)j * num);
				int num4 = (int)((float)i * num2);
				int num5 = Math.Min(width, Math.Max(num3 + 1, (int)((float)(j + 1) * num)));
				int num6 = Math.Min(height, Math.Max(num4 + 1, (int)((float)(i + 1) * num2)));
				float num7 = 0f;
				float num8 = 0f;
				float num9 = 0f;
				float num10 = 0f;
				int num11 = 0;
				for (int k = num4; k < num6; k++)
				{
					for (int l = num3; l < num5; l++)
					{
						int num12 = (k * width + l) * 4;
						byte b = source[num12 + 3];
						num7 += (float)(source[num12] * b);
						num8 += (float)(source[num12 + 1] * b);
						num9 += (float)(source[num12 + 2] * b);
						num10 += (float)(int)b;
						num11++;
					}
				}
				int num13 = (i * 900 + j) * 4;
				if (num10 > 0f)
				{
					logo[num13] = (byte)(num7 / num10);
					logo[num13 + 1] = (byte)(num8 / num10);
					logo[num13 + 2] = (byte)(num9 / num10);
				}
				logo[num13 + 3] = (byte)(num10 / (float)num11);
			}
		}
		BleedColors(logo, 900, 450);
		Logo = MenuDraw.Renderer.CreateTextureFromPixels("menu:logo", 900, 450, logo);
		int num14 = 2;
		int num15 = 3600;
		int num16 = num14 * 225;
		byte[] array = new byte[num15 * num16 * 4];
		for (int m = 0; m < 16; m++)
		{
			float num17 = -300f + (float)m / 15f * 1500f;
			int num18 = m % 8 * 450;
			int num19 = m / 8 * 225;
			for (int n = 0; n < 225; n++)
			{
				for (int num20 = 0; num20 < 450; num20++)
				{
					int num21 = num20 * 2;
					int num22 = n * 2;
					float num23 = (float)(int)logo[(num22 * 900 + num21) * 4 + 3] / 255f;
					float num24 = (((float)num21 - (num17 - 90f)) * 180f + (float)(num22 * 120)) / 46800f;
					float num25 = ((num24 > 0f && num24 < 1f) ? (0.55f * (1f - MathF.Abs(num24 * 2f - 1f))) : 0f);
					int num26 = ((num19 + n) * num15 + num18 + num20) * 4;
					array[num26] = byte.MaxValue;
					array[num26 + 1] = byte.MaxValue;
					array[num26 + 2] = byte.MaxValue;
					array[num26 + 3] = (byte)(255f * num25 * num23);
				}
			}
		}
		LogoSweep = MenuDraw.Renderer.CreateTextureFromPixels("menu:logo:sweep", num15, num16, array);
		List<(int, int)> list = new List<(int, int)>();
		for (int num27 = 2; num27 < 448; num27 += 3)
		{
			for (int num28 = 2; num28 < 898; num28 += 3)
			{
				if (AlphaAt(num28, num27) > 170 && (AlphaAt(num28 + 6, num27) < 20 || AlphaAt(num28 - 6, num27) < 20 || AlphaAt(num28, num27 + 6) < 20 || AlphaAt(num28, num27 - 6) < 20))
				{
					list.Add((num28, num27));
				}
			}
		}
		LogoEdges = list;
		byte AlphaAt(int x, int y)
		{
			if (x >= 0 && y >= 0 && x < 900 && y < 450)
			{
				return logo[(y * 900 + x) * 4 + 3];
			}
			return 0;
		}
	}

	private static void BleedColors(byte[] rgba, int width, int height)
	{
		for (int i = 0; i < 4; i++)
		{
			byte[] array = (byte[])rgba.Clone();
			for (int j = 0; j < height; j++)
			{
				for (int k = 0; k < width; k++)
				{
					int num = (j * width + k) * 4;
					if (array[num + 3] > 0 || (array[num] | array[num + 1] | array[num + 2]) != 0)
					{
						continue;
					}
					int num2 = 0;
					int num3 = 0;
					int num4 = 0;
					int num5 = 0;
					for (int l = -1; l <= 1; l++)
					{
						for (int m = -1; m <= 1; m++)
						{
							int num6 = k + m;
							int num7 = j + l;
							if (num6 >= 0 && num7 >= 0 && num6 < width && num7 < height)
							{
								int num8 = (num7 * width + num6) * 4;
								if (array[num8 + 3] != 0 || (array[num8] | array[num8 + 1] | array[num8 + 2]) != 0)
								{
									num2 += array[num8];
									num3 += array[num8 + 1];
									num4 += array[num8 + 2];
									num5++;
								}
							}
						}
					}
					if (num5 > 0)
					{
						rgba[num] = (byte)(num2 / num5);
						rgba[num + 1] = (byte)(num3 / num5);
						rgba[num + 2] = (byte)(num4 / num5);
					}
				}
			}
		}
	}

	private static IGameTexture? BuildWeatherIcon()
	{
		Color?[] pixels = new Color[1024];
		Color[] array = new Color[5]
		{
			new Color(255, 255, 242, 184),
			new Color(255, 255, 211, 106),
			new Color(255, 255, 172, 82),
			new Color(255, 255, 122, 26),
			new Color(255, 200, 82, 8)
		};
		for (int i = 0; i < 32; i++)
		{
			for (int j = 0; j < 32; j++)
			{
				float num = MathF.Sqrt(Sq((float)j + 0.5f - 12f) + Sq((float)i + 0.5f - 12f));
				if (num <= 6.4f)
				{
					Put(j, i, array[(int)MathF.Round(Math.Clamp((float)(j - 12 + i - 12) / 7f + num / 5f + 1f, 0f, 4f))]);
				}
			}
		}
		(float, float)[] array2 = new(float, float)[5]
		{
			(0f, -1f),
			(-1f, 0f),
			(-0.71f, -0.71f),
			(0.71f, -0.71f),
			(-0.71f, 0.71f)
		};
		for (int k = 0; k < array2.Length; k++)
		{
			(float, float) tuple = array2[k];
			float item = tuple.Item1;
			float item2 = tuple.Item2;
			for (float num2 = 8.6f; num2 <= 10.6f; num2 += 1f)
			{
				Put((int)MathF.Floor(12f + item * num2), (int)MathF.Floor(12f + item2 * num2), array[1]);
			}
		}
		for (int l = 0; l < 32; l++)
		{
			for (int m = 0; m < 32; m++)
			{
				if (Cloud(m, l))
				{
					Put(m, l, (l < 16) ? new Color(255, 255, 255, 255) : ((l < 20) ? new Color(255, 238, 243, 251) : ((l < 24) ? new Color(255, 211, 221, 236) : new Color(255, 167, 182, 205))));
				}
			}
		}
		(int, int)[] array3 = new(int, int)[3]
		{
			(15, 28),
			(20, 29),
			(25, 28)
		};
		for (int n = 0; n < array3.Length; n++)
		{
			var (x, num3) = array3[n];
			Put(x, num3, new Color(255, 156, 196, 255));
			Put(x, num3 + 1, new Color(255, 64, 132, 255));
		}
		Color color = new Color(255, 3, 5, 42);
		bool[] array4 = pixels.Select((Color p) => p != null).ToArray();
		for (int num4 = 0; num4 < 32; num4++)
		{
			for (int num5 = 0; num5 < 32; num5++)
			{
				int num6 = num4 * 32 + num5;
				if (!array4[num6] && ((num5 > 0 && array4[num6 - 1]) || (num5 < 31 && array4[num6 + 1]) || (num4 > 0 && array4[num6 - 32]) || (num4 < 31 && array4[num6 + 32])))
				{
					pixels[num6] = color;
				}
			}
		}
		return Build("menu:weather", 32, 32, delegate(int num7, int y)
		{
			Color color2 = pixels[y * 32 + num7];
			return ((object)color2 == null) ? (R: 0, G: 0, B: 0, A: 0f) : (R: (int)color2.R, G: (int)color2.G, B: (int)color2.B, A: 1f);
		});
		static bool Cloud(int num7, int y)
		{
			if (!new(float, float, float)[3]
			{
				(16f, 21f, 5.2f),
				(22.5f, 17.5f, 6.2f),
				(27.5f, 21.5f, 4.3f)
			}.Any(((float, float, float) c) => MathF.Sqrt(Sq((float)num7 + 0.5f - c.Item1) + Sq((float)y + 0.5f - c.Item2)) <= c.Item3))
			{
				if (num7 >= 12 && num7 <= 30 && y >= 21)
				{
					return y <= 25;
				}
				return false;
			}
			return true;
		}
		void Put(int num7, int y, Color color2)
		{
			if (num7 >= 0 && y >= 0 && num7 < 32 && y < 32)
			{
				pixels[y * 32 + num7] = color2;
			}
		}
	}
}
