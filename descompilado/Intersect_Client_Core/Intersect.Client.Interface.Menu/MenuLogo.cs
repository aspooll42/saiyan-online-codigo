using System;
using System.Collections.Generic;
using Intersect.Client.Framework.Graphics;

namespace Intersect.Client.Interface.Menu;

internal sealed class MenuLogo
{
	private sealed class Bolt
	{
		public float X;

		public float Y;

		public float Angle;

		public float Length;

		public long Born;

		public long Life;

		public int Seed;
	}

	private readonly List<Bolt> _bolts = new List<Bolt>();

	private readonly Random _random = new Random();

	private long _nextBolt;

	public long AppearedAt { get; private set; } = -1L;

	public bool IsVisible => AppearedAt >= 0;

	public void Reset()
	{
		AppearedAt = -1L;
		_bolts.Clear();
	}

	public void Appear(long now)
	{
		AppearedAt = now;
		_nextBolt = now + 1400;
		for (int i = 0; i < 6; i++)
		{
			SpawnBolt(now + i * 40, strong: true);
		}
	}

	public void ShowSettled(long now)
	{
		if (AppearedAt < 0)
		{
			AppearedAt = now - 3000;
			_nextBolt = now + 900;
		}
	}

	private void SpawnBolt(long born, bool strong)
	{
		IReadOnlyList<(int, int)> logoEdges = MenuTextures.LogoEdges;
		if (logoEdges.Count != 0)
		{
			var (num, num2) = logoEdges[_random.Next(logoEdges.Count)];
			_bolts.Add(new Bolt
			{
				X = num,
				Y = num2,
				Angle = MathF.Atan2(num2 - 235, num - 450) + ((float)_random.NextDouble() - 0.5f) * 1.1f,
				Length = (float)(strong ? 90 : 46) + (float)_random.NextDouble() * (float)(strong ? 90 : 56),
				Born = born,
				Life = (strong ? 230 : 150),
				Seed = _random.Next(1000000)
			});
		}
	}

	public void Draw(long now, float originX, float originY, float scale, float alpha, bool idleBolts)
	{
		if (AppearedAt < 0 || alpha <= 0.001f)
		{
			return;
		}
		IGameTexture logo = MenuTextures.Logo;
		if (logo == null)
		{
			return;
		}
		long num = now - AppearedAt;
		float num2 = (1f - MenuDraw.EaseOut((float)num / 340f)) * -18f;
		float num3 = num2 + MathF.Sin((float)now / 1000f * 1.9f) * 4f;
		float num4 = MenuDraw.Clamp01((float)num / 110f) * alpha;
		IGameTexture radial = MenuTextures.Radial;
		if (radial != null)
		{
			float alpha2 = (0.1f + 0.04f * MathF.Sin((float)now / 1000f * 1.25f)) * num4;
			MenuDraw.Texture(radial, originX + 10f * scale, originY - 200f * scale, 1080f * scale, 1080f * scale, MenuDraw.Rgba(255, 150, 60, alpha2));
		}
		float num5 = originX + 100f * scale;
		float num6 = originY + (100f + num3) * scale;
		MenuDraw.Texture(logo, num5, num6, 900f * scale, 450f * scale, MenuDraw.Alpha(Color.White, num4));
		long num7 = num - 520;
		if (num7 >= 0 && num7 % 7200 < 950)
		{
			IGameTexture logoSweep = MenuTextures.LogoSweep;
			if (logoSweep != null)
			{
				int num8 = Math.Min(15, (int)((float)(num7 % 7200) / 950f * 16f));
				MenuDraw.TexturePart(logoSweep, num8 % 8 * 450, num8 / 8 * 225, 450f, 225f, num5, num6, 900f * scale, 450f * scale, MenuDraw.Alpha(Color.White, num4), GameBlendModes.Add);
			}
		}
		for (int num9 = _bolts.Count - 1; num9 >= 0; num9--)
		{
			Bolt bolt = _bolts[num9];
			long num10 = now - bolt.Born;
			if (num10 >= 0)
			{
				if (num10 > bolt.Life)
				{
					_bolts.RemoveAt(num9);
				}
				else
				{
					int num11 = (int)(num10 / 55);
					float num12 = ((num11 >= 2) ? 0.5f : 1f) * alpha;
					(List<(float X, float Y)> Trunk, List<(float X, float Y)> Branch) tuple = Trace(bolt, bolt.Seed + num11 * 31);
					List<(float, float)> item = tuple.Trunk;
					List<(float, float)> item2 = tuple.Branch;
					(float, Color)[] array = new(float, Color)[4]
					{
						(14f, MenuDraw.Rgba(90, 150, 255, 0.16f * num12)),
						(8f, MenuDraw.Rgba(90, 150, 255, 0.3f * num12)),
						(3f, MenuDraw.Rgba(170, 210, 255, 0.8f * num12)),
						(1.4f, MenuDraw.Rgba(255, 255, 255, num12))
					};
					for (int i = 0; i < array.Length; i++)
					{
						(float, Color) tuple2 = array[i];
						float item3 = tuple2.Item1;
						Color item4 = tuple2.Item2;
						List<(float, float)>[] array2 = new List<(float, float)>[2] { item, item2 };
						foreach (List<(float, float)> list in array2)
						{
							for (int k = 1; k < list.Count; k++)
							{
								MenuDraw.Line(num5 + list[k - 1].Item1 * scale, num6 + list[k - 1].Item2 * scale, num5 + list[k].Item1 * scale, num6 + list[k].Item2 * scale, item3 * scale, item4, GameBlendModes.Add);
							}
						}
					}
				}
			}
		}
		if (idleBolts && now > _nextBolt)
		{
			SpawnBolt(now, strong: false);
			if (_random.NextDouble() < 0.35)
			{
				SpawnBolt(now + 70, strong: false);
			}
			_nextBolt = now + 1300 + (long)(_random.NextDouble() * 2300.0);
		}
	}

	private static (List<(float X, float Y)> Trunk, List<(float X, float Y)> Branch) Trace(Bolt bolt, int seed)
	{
		Random random = new Random(seed);
		int num = Math.Max(3, (int)MathF.Round(bolt.Length / 12f));
		float num2 = bolt.Length / (float)num;
		List<(float, float)> list = new List<(float, float)> { (bolt.X, bolt.Y) };
		float num3 = bolt.X;
		float num4 = bolt.Y;
		for (int i = 1; i <= num; i++)
		{
			float num5 = ((float)random.NextDouble() - 0.5f) * 18f;
			num3 += MathF.Cos(bolt.Angle) * num2 - MathF.Sin(bolt.Angle) * num5 * 0.6f;
			num4 += MathF.Sin(bolt.Angle) * num2 + MathF.Cos(bolt.Angle) * num5 * 0.6f;
			list.Add((num3, num4));
		}
		(float, float) item = list[num / 2];
		float x = bolt.Angle + ((random.NextDouble() < 0.5) ? 0.7f : (-0.7f));
		List<(float, float)> list2 = new List<(float, float)> { item };
		float num6 = item.Item1;
		float num7 = item.Item2;
		for (int j = 0; j < 3; j++)
		{
			float num8 = ((float)random.NextDouble() - 0.5f) * 14f;
			num6 += MathF.Cos(x) * num2 * 0.8f - MathF.Sin(x) * num8 * 0.5f;
			num7 += MathF.Sin(x) * num2 * 0.8f + MathF.Cos(x) * num8 * 0.5f;
			list2.Add((num6, num7));
		}
		return (Trunk: list, Branch: list2);
	}
}
