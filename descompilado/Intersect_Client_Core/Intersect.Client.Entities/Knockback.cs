using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Maps;
using Intersect.Client.General;
using Intersect.Client.Maps;
using Intersect.Enums;
using Intersect.Framework.Core;

namespace Intersect.Client.Entities;

public static class Knockback
{
	private sealed class Slide
	{
		public required Guid EntityId;

		public required bool Npc;

		public float StartX;

		public float StartY;

		public float EndX;

		public float EndY;

		public float DirX;

		public float DirY;

		public long StartedAt;

		public long PlannedEndAt;

		public long EndedAt;

		public long LastDustAt;
	}

	public const long StunMs = 400L;

	private const float LeanPixels = 5f;

	private const long LeanTailMs = 150L;

	private const long DustEveryMs = 45L;

	private const long ShiverMs = 220L;

	private const long MarksHoldMs = 800L;

	private const long MarksFadeMs = 1400L;

	private static readonly List<Slide> Slides = new List<Slide>();

	private static long EndOf(Slide slide, long now)
	{
		if (slide.EndedAt <= 0)
		{
			if (now <= slide.PlannedEndAt + 500)
			{
				return 0L;
			}
			return slide.PlannedEndAt;
		}
		return slide.EndedAt;
	}

	public static void OnStart(Entity entity, Dash dash, IMapInstance map)
	{
		(float X, float Y) startOffset = dash.StartOffset;
		float item = startOffset.X;
		float item2 = startOffset.Y;
		(float X, float Y) endOffset = dash.EndOffset;
		float item3 = endOffset.X;
		float item4 = endOffset.Y;
		float num = item3 - item;
		float num2 = item4 - item2;
		float num3 = MathF.Sqrt(num * num + num2 * num2);
		if (!(num3 < 1f))
		{
			Slides.RemoveAll((Slide slide) => slide.EntityId == entity.Id && slide.EndedAt == 0);
			float num4 = (float)(map.X + entity.X * Options.Instance.Map.TileWidth) + (float)Options.Instance.Map.TileWidth / 2f + item;
			float num5 = (float)(map.Y + entity.Y * Options.Instance.Map.TileHeight + Options.Instance.Map.TileHeight) + item2;
			Slides.Add(new Slide
			{
				EntityId = entity.Id,
				Npc = !(entity is Player),
				StartX = num4,
				StartY = num5,
				EndX = num4,
				EndY = num5,
				DirX = num / num3,
				DirY = num2 / num3,
				StartedAt = dash.StartedAt,
				PlannedEndAt = dash.EndsAt
			});
			if (Slides.Count > 64)
			{
				Slides.RemoveAt(0);
			}
		}
	}

	public static void Observe(Entity entity, IMapInstance? map)
	{
		Slide slide = Slides.FindLast((Slide s) => s.EntityId == entity.Id && s.EndedAt == 0);
		if (slide != null && map != null)
		{
			long milliseconds = Timing.Global.Milliseconds;
			slide.EndX = (float)(map.X + entity.X * Options.Instance.Map.TileWidth) + (float)Options.Instance.Map.TileWidth / 2f + entity.OffsetX;
			slide.EndY = (float)(map.Y + entity.Y * Options.Instance.Map.TileHeight + Options.Instance.Map.TileHeight) + entity.OffsetY;
			if (entity.Dashing == null)
			{
				slide.EndedAt = milliseconds;
			}
			else if (milliseconds - slide.LastDustAt >= 45 && !(map is MapInstance { IsIndoors: not false }))
			{
				slide.LastDustAt = milliseconds;
				Direction direction = ((!(MathF.Abs(slide.DirX) >= MathF.Abs(slide.DirY))) ? ((slide.DirY > 0f) ? Direction.Down : Direction.Up) : ((slide.DirX > 0f) ? Direction.Right : Direction.Left));
				SprintDust.AddPuff(slide.EndX, slide.EndY - 2f, direction);
			}
		}
	}

	public static (float Lean, float Shiver) Pose(Entity entity)
	{
		if (entity is Player || Slides.Count == 0)
		{
			return (Lean: 0f, Shiver: 0f);
		}
		Slide slide = Slides.FindLast((Slide s) => s.EntityId == entity.Id);
		if (slide == null || !slide.Npc)
		{
			return (Lean: 0f, Shiver: 0f);
		}
		long milliseconds = Timing.Global.Milliseconds;
		long num = EndOf(slide, milliseconds);
		long num2 = ((num > 0) ? num : Math.Max(slide.PlannedEndAt, milliseconds));
		long num3 = Math.Max(1L, num2 - slide.StartedAt + 400 + 150);
		float num4 = Math.Clamp((float)(milliseconds - slide.StartedAt) / (float)num3, 0f, 1f);
		float item = ((MathF.Abs(slide.DirX) >= 0.5f) ? ((float)MathF.Sign(slide.DirX) * 5f * (1f - num4) * (1f - num4)) : 0f);
		float item2 = 0f;
		if (num > 0 && milliseconds - num < 220 && !entity.IsDying)
		{
			item2 = (((milliseconds - num) / 40 % 2 == 0L) ? 1f : (-1f));
		}
		return (Lean: item, Shiver: item2);
	}

	public static void DrawGround()
	{
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture == null || Slides.Count == 0)
		{
			return;
		}
		long now = Timing.Global.Milliseconds;
		Slides.RemoveAll(delegate(Slide slide)
		{
			long num7 = EndOf(slide, now);
			return num7 > 0 && now - num7 > 2600;
		});
		foreach (Slide slide in Slides)
		{
			float num = 1f;
			long num2 = EndOf(slide, now);
			if (num2 > 0 && now - num2 > 1200)
			{
				num = 1f - (float)(now - num2 - 400 - 800) / 1400f;
			}
			if (num <= 0f)
			{
				continue;
			}
			Color renderColor = new Color((int)(110f * num), 52, 38, 20);
			if (MathF.Abs(slide.DirX) >= MathF.Abs(slide.DirY))
			{
				float num3 = MathF.Round(MathF.Min(slide.StartX, slide.EndX));
				float num4 = MathF.Round(MathF.Abs(slide.EndX - slide.StartX));
				if (!(num4 < 3f))
				{
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num3, MathF.Round(slide.StartY - 3f), num4, 1f), renderColor);
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num3 + 2f, MathF.Round(slide.StartY), MathF.Max(1f, num4 - 2f), 1f), renderColor);
				}
			}
			else
			{
				float num5 = MathF.Round(MathF.Min(slide.StartY, slide.EndY));
				float num6 = MathF.Round(MathF.Abs(slide.EndY - slide.StartY));
				if (!(num6 < 3f))
				{
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(slide.StartX - 4f), num5 - 2f, 1f, num6), renderColor);
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(slide.StartX + 3f), num5, 1f, MathF.Max(1f, num6 - 2f)), renderColor);
				}
			}
		}
	}

	public static void DrawFront()
	{
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture == null || Slides.Count == 0)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		foreach (Slide slide in Slides)
		{
			if (!slide.Npc || slide.EndedAt == 0L || milliseconds - slide.EndedAt >= 400 || !Globals.Entities.TryGetValue(slide.EntityId, out Entity value) || value == null || value.IsDying || value.IsDisposed)
			{
				continue;
			}
			FloatRect worldPos = value.WorldPos;
			if (!(worldPos.Width <= 0f) && !(worldPos.Height <= 0f))
			{
				Random random = new Random(((int)(milliseconds / 70) * 397) ^ slide.EntityId.GetHashCode());
				float num = 1f - (float)(milliseconds - slide.EndedAt) / 400f;
				for (int i = 0; i < 4; i++)
				{
					int x = (int)MathF.Round(worldPos.X + 8f + (float)random.NextDouble() * MathF.Max(1f, worldPos.Width - 16f));
					int y = (int)MathF.Round(worldPos.Y + worldPos.Height * 0.22f + (float)random.NextDouble() * worldPos.Height * 0.45f);
					int side = ((random.Next(2) == 0) ? 1 : (-1));
					Zigzag(gameTexture, x, y, side, (int)(245f * MathF.Min(1f, num * 1.6f)));
				}
			}
		}
	}

	private static void Zigzag(IGameTexture pixel, int x, int y, int side, int alpha)
	{
		Color renderColor = new Color(alpha, 255, 196, 50);
		Color renderColor2 = new Color(alpha, 255, 251, 224);
		_003C_003Ey__InlineArray4<(int, int)> buffer = default(_003C_003Ey__InlineArray4<(int, int)>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray4<(int, int)>, (int, int)>(ref buffer, 0) = (0, 0);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray4<(int, int)>, (int, int)>(ref buffer, 1) = (2 * side, 3);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray4<(int, int)>, (int, int)>(ref buffer, 2) = (0, 4);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray4<(int, int)>, (int, int)>(ref buffer, 3) = (3 * side, 8);
		ReadOnlySpan<(int, int)> readOnlySpan = global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<_003C_003Ey__InlineArray4<(int, int)>, (int, int)>(in buffer, 4);
		for (int i = 0; i < readOnlySpan.Length - 1; i++)
		{
			(int, int) tuple = readOnlySpan[i];
			int item = tuple.Item1;
			int item2 = tuple.Item2;
			(int, int) tuple2 = readOnlySpan[i + 1];
			int item3 = tuple2.Item1;
			int item4 = tuple2.Item2;
			int num = Math.Max(Math.Abs(item3 - item), Math.Abs(item4 - item2));
			for (int j = 0; j <= num; j++)
			{
				int num2 = x + item + (item3 - item) * j / Math.Max(1, num);
				int num3 = y + item2 + (item4 - item2) * j / Math.Max(1, num);
				Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num2, num3, 1f, 1f), renderColor);
			}
		}
		Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(x + readOnlySpan[1].Item1, y + readOnlySpan[1].Item2, 1f, 1f), renderColor2);
		Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(x + readOnlySpan[2].Item1, y + readOnlySpan[2].Item2, 1f, 1f), renderColor2);
	}
}
