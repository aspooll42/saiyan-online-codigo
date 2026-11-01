using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Config;
using Intersect.Framework.Core.Sprites;

namespace Intersect.Client.Interface.Menu;

internal sealed class MenuCharacterStage
{
	private sealed class AuraPass(bool back)
	{
		private readonly float[] _r = new float[48000];

		private readonly float[] _g = new float[48000];

		private readonly float[] _b = new float[48000];

		private readonly float[] _a = new float[48000];

		private readonly List<(int X, int Y, int Width, Color Color)> _runs = new List<(int, int, int, Color)>();

		private long _step = -1L;

		private bool _green;

		public void Draw(long now, float centerX, float ground, float force, bool green, float alpha)
		{
			long num = now / 70;
			if (num != _step || green != _green)
			{
				_step = num;
				_green = green;
				Rasterize(num, force, green);
			}
			float num2 = centerX - 360f;
			float num3 = ground - 519f;
			foreach (var (num4, num5, num6, color) in _runs)
			{
				MenuDraw.Rect(num2 + (float)num4 * 3f, num3 + (float)num5 * 3f, (float)num6 * 3f, 3f, MenuDraw.Alpha(color, alpha));
			}
		}

		private void Rasterize(long step, float force, bool green)
		{
			Array.Clear(_r);
			Array.Clear(_g);
			Array.Clear(_b);
			Array.Clear(_a);
			(float, float, float, float)[] array = ((!green) ? new(float, float, float, float)[3]
			{
				(150f, 196f, 255f, 0.22f),
				(206f, 228f, 255f, 0.34f),
				(250f, 252f, 255f, 0.55f)
			} : new(float, float, float, float)[3]
			{
				(110f, 240f, 140f, 0.22f),
				(180f, 255f, 190f, 0.34f),
				(240f, 255f, 242f, 0.55f)
			});
			for (int i = 0; i < 14; i++)
			{
				float x = (float)i / 14f * (float)Math.PI * 2f + 0.22f;
				bool flag = MathF.Sin(x) > 0.2f;
				if (flag != back)
				{
					Random random = new Random((int)(i * 977 + step * 131 + 7));
					float num = MathF.Abs(MathF.Cos(x));
					float baseX = 120f + MathF.Cos(x) * (float)(flag ? 20 : 27);
					float baseY = 173f + MathF.Sin(x) * 5f;
					float num2 = (flag ? (10f + (float)random.NextDouble() * 12f) : ((24f + (float)random.NextDouble() * 42f) * (0.62f + 0.62f * num))) * force;
					float num3 = (float)(flag ? 5 : 6) + (float)random.NextDouble() * 4f;
					float curve = ((float)random.NextDouble() - 0.5f) * 7f + MathF.Cos(x) * (6f + num2 * 0.08f);
					for (int j = 0; j < 3; j++)
					{
						float num4 = (new float[3] { 1f, 0.62f, 0.3f })[j];
						float height = num2 * (new float[3] { 1f, 0.78f, 0.46f })[j];
						FillTongue(baseX, baseY, num3 * num4, height, curve, array[j]);
					}
				}
			}
			_runs.Clear();
			for (int k = 0; k < 200; k++)
			{
				int num5 = 0;
				while (num5 < 240)
				{
					int num6 = k * 240 + num5;
					if (_a[num6] < 0.01f)
					{
						num5++;
						continue;
					}
					int num7 = Quantize(num6);
					int l;
					for (l = 1; num5 + l < 240 && Quantize(num6 + l) == num7; l++)
					{
					}
					float num8 = _a[num6];
					_runs.Add((num5, k, l, new Color((int)(num8 * 255f), (int)(_r[num6] / num8), (int)(_g[num6] / num8), (int)(_b[num6] / num8))));
					num5 += l;
				}
			}
		}

		private int Quantize(int index)
		{
			float num = _a[index];
			if (num < 0.01f)
			{
				return -1;
			}
			return ((int)(num * 31f) << 15) | ((int)(_r[index] / num / 16f) << 10) | ((int)(_g[index] / num / 16f) << 5) | (int)(_b[index] / num / 16f);
		}

		private void FillTongue(float baseX, float baseY, float width, float height, float curve, (float R, float G, float B, float A) color)
		{
			List<(float, float)> list = new List<(float, float)>(26);
			(float, float) a = (baseX - width / 2f, baseY);
			(float, float) tuple = (baseX + curve, baseY - height);
			(float, float) b = (baseX + width / 2f, baseY);
			(float, float) c = (baseX - width * 0.3f + curve * 0.4f, baseY - height * 0.55f);
			(float, float) c2 = (baseX + width * 0.3f + curve * 0.4f, baseY - height * 0.55f);
			for (int i = 0; i <= 12; i++)
			{
				list.Add(Quad(a, c, tuple, (float)i / 12f));
			}
			for (int j = 1; j <= 12; j++)
			{
				list.Add(Quad(tuple, c2, b, (float)j / 12f));
			}
			int num = (int)MathF.Max(0f, MathF.Floor(list.Min<(float, float)>(((float X, float Y) p) => p.Y)));
			int num2 = (int)MathF.Min(199f, MathF.Ceiling(list.Max<(float, float)>(((float X, float Y) p) => p.Y)));
			List<float> list2 = new List<float>(8);
			for (int num3 = num; num3 <= num2; num3++)
			{
				float num4 = (float)num3 + 0.5f;
				list2.Clear();
				for (int num5 = 0; num5 < list.Count; num5++)
				{
					(float, float) tuple2 = list[num5];
					(float, float) tuple3 = list[(num5 + 1) % list.Count];
					if ((tuple2.Item2 <= num4 && tuple3.Item2 > num4) || (tuple3.Item2 <= num4 && tuple2.Item2 > num4))
					{
						list2.Add(tuple2.Item1 + (num4 - tuple2.Item2) / (tuple3.Item2 - tuple2.Item2) * (tuple3.Item1 - tuple2.Item1));
					}
				}
				list2.Sort();
				for (int num6 = 0; num6 + 1 < list2.Count; num6 += 2)
				{
					int num7 = (int)MathF.Max(0f, MathF.Ceiling(list2[num6] - 0.5f));
					int num8 = (int)MathF.Min(239f, MathF.Floor(list2[num6 + 1] - 0.5f));
					for (int num9 = num7; num9 <= num8; num9++)
					{
						int num10 = num3 * 240 + num9;
						float num11 = 1f - color.A;
						_r[num10] = color.R * color.A + _r[num10] * num11;
						_g[num10] = color.G * color.A + _g[num10] * num11;
						_b[num10] = color.B * color.A + _b[num10] * num11;
						_a[num10] = color.A + _a[num10] * num11;
					}
				}
			}
			static (float X, float Y) Quad((float X, float Y) tuple4, (float X, float Y) tuple5, (float X, float Y) tuple6, float t)
			{
				return (X: (1f - t) * (1f - t) * tuple4.X + 2f * (1f - t) * t * tuple5.X + t * t * tuple6.X, Y: (1f - t) * (1f - t) * tuple4.Y + 2f * (1f - t) * t * tuple5.Y + t * t * tuple6.Y);
			}
		}
	}

	public const float Zoom = 9f;

	private const int GridWidth = 240;

	private const int GridHeight = 200;

	private const float Cell = 3f;

	private const float GridCenterX = 120f;

	private const float GridGround = 173f;

	private static readonly Dictionary<string, (int Width, int Height, byte[] Rgba)?> PixelCache = new Dictionary<string, (int, int, byte[])?>();

	private readonly AuraPass _back = new AuraPass(back: true);

	private readonly AuraPass _front = new AuraPass(back: false);

	private static readonly Dictionary<string, (IGameTexture Texture, int X, int Y, int Size)> HeadCrops = new Dictionary<string, (IGameTexture, int, int, int)>();

	public int Direction { get; set; }

	public void Turn(int sense)
	{
		int[] array = new int[4] { 0, 1, 3, 2 };
		int num = Array.IndexOf(array, Direction);
		Direction = array[(num + sense + 4) % 4];
	}

	public void Draw(long now, float centerX, float ground, MenuCharacterLook? look, float alpha, long materializedAt, bool greenAura)
	{
		if (alpha <= 0.001f)
		{
			return;
		}
		long num = now - materializedAt;
		float num2 = MenuDraw.Clamp01(1f - (float)num / 650f);
		float num3 = 1f + num2 * 1.3f;
		IGameTexture gameTexture = ((look != null) ? GameContentManager.Current.GetTexture(TextureType.Entity, look.Sprite) : null);
		float force = num3 * ((gameTexture != null) ? 1f : 0.6f);
		IGameTexture radial = MenuTextures.Radial;
		if (radial != null)
		{
			MenuDraw.Texture(radial, centerX - 170f, ground + 4f - 34f, 340f, 68f, MenuDraw.Rgba(150, 196, 255, (0.16f + num2 * 0.2f) * alpha));
			MenuDraw.Texture(radial, centerX - 96f, ground + 4f - 19f, 192f, 38f, MenuDraw.Rgba(0, 0, 0, 0.6f * alpha));
		}
		_back.Draw(now, centerX, ground, force, greenAura, alpha);
		if (look != null && gameTexture != null)
		{
			(IGameTexture Texture, int Columns) tuple = StandingSheet(look.Sprite, gameTexture);
			IGameTexture item = tuple.Texture;
			int item2 = tuple.Columns;
			(int, int, int, int) frame = FrameFor(look.Sprite, item, Direction, item2);
			int num4 = FeetRow(item, frame);
			float left = MathF.Round(centerX - (float)frame.Item3 * 9f / 2f);
			float top = ground - (float)(num4 + 1) * 9f;
			float alpha2 = MenuDraw.Clamp01((float)(num - 60) / 220f) * alpha;
			float num5 = MenuDraw.Clamp01(1f - (float)(num - 140) / 260f) * alpha;
			foreach (var (gameTexture2, path, tint) in Layers(look, gameTexture))
			{
				if (gameTexture2 == gameTexture)
				{
					DrawPixels(item, frame, left, top, tint, alpha2, silhouette: false);
					continue;
				}
				if (item != gameTexture)
				{
					IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Paperdoll, Path.GetFileNameWithoutExtension(path) + "_Idle.png");
					if (texture != null && texture.Width > 0 && texture.Height > 0)
					{
						DrawPixels(texture, FrameFor(look.Sprite, texture, Direction, Math.Max(1, Entity.SheetColumns(texture, gameTexture, item2))), left, top, tint, alpha2, silhouette: false);
						continue;
					}
				}
				DrawPixels(gameTexture2, FrameFor(look.Sprite, gameTexture2, Direction), left, top, tint, alpha2, silhouette: false);
			}
			if (num5 > 0.001f)
			{
				DrawPixels(item, frame, left, top, Color.White, num5, silhouette: true);
			}
		}
		_front.Draw(now, centerX, ground, force, greenAura, alpha * 0.55f);
	}

	private IEnumerable<(IGameTexture Texture, string Paperdoll, Color Tint)> Layers(MenuCharacterLook look, IGameTexture body)
	{
		PaperdollOptions paperdoll = Options.Instance.Equipment.Paperdoll;
		List<string> list = Direction switch
		{
			1 => paperdoll.Left, 
			2 => paperdoll.Right, 
			3 => paperdoll.Up, 
			_ => paperdoll.Down, 
		};
		List<string> down = paperdoll.Down;
		foreach (string item in list)
		{
			if (string.Equals(item, "Player", StringComparison.Ordinal))
			{
				yield return (Texture: body, Paperdoll: string.Empty, Tint: Color.White);
				continue;
			}
			int num = down.IndexOf(item);
			if (num >= 0 && num < look.Paperdolls.Count && !string.IsNullOrEmpty(look.Paperdolls[num].Name))
			{
				IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Paperdoll, look.Paperdolls[num].Name);
				if (texture != null && texture.Width == body.Width && texture.Height == body.Height)
				{
					yield return (Texture: texture, Paperdoll: look.Paperdolls[num].Name, Tint: look.Paperdolls[num].Tint);
				}
			}
		}
	}

	public static (IGameTexture Texture, int X, int Y, int Size)? HeadCrop(string sprite)
	{
		if (!string.IsNullOrEmpty(sprite))
		{
			IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Entity, sprite);
			if (texture != null)
			{
				var (gameTexture, columns) = StandingSheet(sprite, texture);
				if (HeadCrops.TryGetValue(sprite, out (IGameTexture, int, int, int) value) && value.Item1 == gameTexture)
				{
					return (gameTexture, value.Item2, value.Item3, value.Item4);
				}
				(int, int, int)? tuple2 = FindHeadCrop(sprite, gameTexture, columns);
				if (tuple2.HasValue)
				{
					(int, int, int) valueOrDefault = tuple2.GetValueOrDefault();
					HeadCrops[sprite] = (gameTexture, valueOrDefault.Item1, valueOrDefault.Item2, valueOrDefault.Item3);
					return (gameTexture, valueOrDefault.Item1, valueOrDefault.Item2, valueOrDefault.Item3);
				}
				return null;
			}
		}
		return null;
	}

	private static (int X, int Y, int Size)? FindHeadCrop(string sprite, IGameTexture texture, int columns)
	{
		if (!TryPixels(texture, out var pixels))
		{
			return null;
		}
		(int X, int Y, int Width, int Height) frame = FrameFor(sprite, texture, 0, columns);
		int num = -1;
		for (int i = 0; i < frame.Height; i++)
		{
			if (num >= 0)
			{
				break;
			}
			for (int j = 0; j < frame.Width; j++)
			{
				if (Opaque(j, i))
				{
					num = i;
					break;
				}
			}
		}
		if (num < 0)
		{
			return null;
		}
		long num2 = 0L;
		int num3 = 0;
		for (int k = num; k < Math.Min(frame.Height, num + 12); k++)
		{
			for (int l = 0; l < frame.Width; l++)
			{
				if (Opaque(l, k))
				{
					num2 += l;
					num3++;
				}
			}
		}
		int num4 = Math.Min(18, Math.Min(frame.Width, frame.Height));
		int num5 = ((num3 > 0) ? ((int)MathF.Round((float)num2 / (float)num3)) : (frame.Width / 2));
		int num6 = Math.Clamp(num5 - num4 / 2, 0, frame.Width - num4);
		int num7 = Math.Clamp(num - 2, 0, frame.Height - num4);
		return (frame.X + num6, frame.Y + num7, num4);
		bool Opaque(int x, int y)
		{
			int num8 = frame.X + x;
			int num9 = frame.Y + y;
			if (num8 < pixels.Width && num9 < pixels.Height)
			{
				return pixels.Rgba[(num9 * pixels.Width + num8) * 4 + 3] > 0;
			}
			return false;
		}
	}

	private static (IGameTexture Texture, int Columns) StandingSheet(string sprite, IGameTexture body)
	{
		if (SpriteSheets.ForEntity(sprite) == null)
		{
			IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Entity, Path.GetFileNameWithoutExtension(sprite) + "_idle" + Path.GetExtension(sprite));
			if (texture != null && texture.Width > 0 && texture.Height > 0)
			{
				return (Texture: texture, Columns: Math.Max(1, Entity.SheetColumns(texture, body, Options.Instance.Sprites.IdleFrames)));
			}
		}
		return (Texture: body, Columns: Math.Max(1, Options.Instance.Sprites.NormalFrames));
	}

	private static (int X, int Y, int Width, int Height) FrameFor(string sprite, IGameTexture texture, int direction, int columns = 0)
	{
		SpriteSheet spriteSheet = SpriteSheets.ForEntity(sprite);
		if (spriteSheet != null)
		{
			SpriteSheetAnimation spriteSheetAnimation = spriteSheet.For("Idle").FirstOrDefault() ?? spriteSheet.For("Normal").FirstOrDefault();
			if (spriteSheetAnimation != null)
			{
				return spriteSheet.FrameRect(spriteSheetAnimation, 0, direction);
			}
		}
		int item = texture.Width / Math.Max(1, (columns > 0) ? columns : Options.Instance.Sprites.NormalFrames);
		int num = texture.Height / Math.Max(1, Options.Instance.Sprites.Directions);
		return (X: 0, Y: direction * num, Width: item, Height: num);
	}

	private static bool TryPixels(IGameTexture texture, out (int Width, int Height, byte[] Rgba) pixels)
	{
		if (!PixelCache.TryGetValue(texture.Name, out (int, int, byte[])? value))
		{
			value = (MenuDraw.Renderer.TryReadPixels(texture, out byte[] rgba, out int width, out int height) ? new(int, int, byte[])?((width, height, rgba)) : (((int, int, byte[])?)null));
			if (value.HasValue)
			{
				PixelCache[texture.Name] = value;
			}
		}
		pixels = value.GetValueOrDefault();
		return value.HasValue;
	}

	private static int FeetRow(IGameTexture texture, (int X, int Y, int Width, int Height) frame)
	{
		if (!TryPixels(texture, out (int, int, byte[]) pixels))
		{
			return frame.Height - 2;
		}
		for (int num = frame.Height - 1; num >= 0; num--)
		{
			for (int i = 0; i < frame.Width; i++)
			{
				int num2 = frame.X + i;
				int num3 = frame.Y + num;
				if (num2 < pixels.Item1 && num3 < pixels.Item2 && pixels.Item3[(num3 * pixels.Item1 + num2) * 4 + 3] > 0)
				{
					return num;
				}
			}
		}
		return frame.Height - 2;
	}

	private static void DrawPixels(IGameTexture texture, (int X, int Y, int Width, int Height) frame, float left, float top, Color tint, float alpha, bool silhouette)
	{
		if (alpha <= 0.001f || !TryPixels(texture, out (int, int, byte[]) pixels))
		{
			return;
		}
		for (int i = 0; i < frame.Height; i++)
		{
			int num = frame.Y + i;
			if (num >= pixels.Item2)
			{
				break;
			}
			int num2 = 0;
			while (num2 < frame.Width)
			{
				int num3 = frame.X + num2;
				if (num3 >= pixels.Item1)
				{
					break;
				}
				int num4 = (num * pixels.Item1 + num3) * 4;
				byte b = pixels.Item3[num4 + 3];
				if (b == 0)
				{
					num2++;
					continue;
				}
				byte b2 = pixels.Item3[num4];
				byte b3 = pixels.Item3[num4 + 1];
				byte b4 = pixels.Item3[num4 + 2];
				int j;
				for (j = 1; num2 + j < frame.Width && frame.X + num2 + j < pixels.Item1; j++)
				{
					int num5 = (num * pixels.Item1 + frame.X + num2 + j) * 4;
					bool num6;
					if (!silhouette)
					{
						if (pixels.Item3[num5] != b2 || pixels.Item3[num5 + 1] != b3 || pixels.Item3[num5 + 2] != b4)
						{
							break;
						}
						num6 = pixels.Item3[num5 + 3] == b;
					}
					else
					{
						num6 = pixels.Item3[num5 + 3] > 0;
					}
					if (!num6)
					{
						break;
					}
				}
				Color color = (silhouette ? MenuDraw.Alpha(Color.White, alpha) : new Color((int)((float)(int)b * alpha * (float)(int)tint.A / 255f), b2 * tint.R / 255, b3 * tint.G / 255, b4 * tint.B / 255));
				MenuDraw.Rect(left + (float)num2 * 9f, top + (float)i * 9f, (float)j * 9f, 9f, color);
				num2 += j;
			}
		}
	}
}
