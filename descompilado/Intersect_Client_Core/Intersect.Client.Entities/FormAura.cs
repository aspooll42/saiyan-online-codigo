using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Framework.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Intersect.Client.Entities;

public static class FormAura
{
	private sealed class Flame
	{
		public int Layer;

		public float X;

		public float Y;

		public float Vx;

		public float Vy;

		public float W;

		public float H;

		public float Seed;

		public long Born;

		public long Life;

		public bool Front;
	}

	private sealed class Extra
	{
		public bool Wisp;

		public float X;

		public float Y;

		public float Vx;

		public float Vy;

		public float W;

		public float H;

		public float Seed;

		public float Size;

		public long Born;

		public long Life;

		public Color? Tint;
	}

	private enum Origin
	{
		Edge,
		Top,
		Ground
	}

	private sealed class Ghost
	{
		public IGameTexture Mask;

		public FloatRect Destination;

		public long Born;
	}

	private sealed class Layer
	{
		public required float Rate;

		public required (int Min, int Max) Life;

		public required (float Min, float Max) Rise;

		public required (float Min, float Max) Width;

		public required (float Min, float Max) Height;

		public required float Push;

		public required Color Tint;

		public required float Alpha;

		public required GameBlendModes Blend;

		public float Jitter = 1.2f;

		public float JitterMs = 340f;

		public bool Front;

		public float Body;

		public Origin From;

		public Color? Core;

		public float CoreAlpha;
	}

	private sealed class Style
	{
		public required Layer[] Layers;

		public required (Color Color, float Alpha, int Grow)[] Rings;

		public required GameBlendModes RingBlend;

		public required Color Pool;

		public required float PoolAlpha;

		public required GameBlendModes PoolBlend;

		public Color? Torso;

		public bool Ghosts;

		public float Wisps;

		public required float Motes;

		public required Color MoteTint;

		public bool Beats;

		public float Side = 0.35f;

		public float Swell = 2f;

		public bool Surges;

		public float PulseRate = 1f;

		public float Tall;

		public float PoolSize = 1f;

		public float PoolSwell;

		public float TorsoAlpha = 0.16f;

		public Color? BodyTint;

		public (float Alpha, float Pulse) BodyTintAlpha;

		public Color? MoteAlt;

		public (float Min, float Max) MoteRise = (Min: 8f, Max: 24f);

		public (int Min, int Max) MoteLife = (Min: 600, Max: 1200);

		public int Burst;
	}

	private sealed class State
	{
		public Style? Style;

		public readonly List<Flame> Flames = new List<Flame>();

		public readonly float[] Debt = new float[8];

		public long SurgeCycle = long.MinValue;

		public IGameTexture? BodyMask;

		public readonly List<Extra> Extras = new List<Extra>();

		public readonly List<Ghost> Ghosts = new List<Ghost>();

		public long LastUpdate;

		public long LastGhost;

		public float LastCenterX = float.NaN;

		public float Velocity;

		public bool Active;
	}

	private static readonly Style UltraInstinct = new Style
	{
		Layers = new Layer[3]
		{
			new Layer
			{
				Rate = 95f,
				Life = (Min: 620, Max: 900),
				Rise = (Min: 12f, Max: 22f),
				Width = (Min: 8f, Max: 12f),
				Height = (Min: 9f, Max: 14f),
				Push = 5f,
				Tint = new Color(255, 52, 36, 150),
				Alpha = 0.3f,
				Blend = GameBlendModes.None
			},
			new Layer
			{
				Rate = 115f,
				Life = (Min: 480, Max: 720),
				Rise = (Min: 16f, Max: 28f),
				Width = (Min: 6f, Max: 9f),
				Height = (Min: 8f, Max: 12f),
				Push = 3f,
				Tint = new Color(255, 80, 130, 255),
				Alpha = 0.4f,
				Blend = GameBlendModes.Add,
				Front = true
			},
			new Layer
			{
				Rate = 135f,
				Life = (Min: 340, Max: 540),
				Rise = (Min: 20f, Max: 32f),
				Width = (Min: 4.5f, Max: 6.5f),
				Height = (Min: 6f, Max: 9f),
				Push = 1.4f,
				Tint = new Color(255, 220, 248, 255),
				Alpha = 0.5f,
				Blend = GameBlendModes.Add,
				Front = true,
				Body = 0.35f
			}
		},
		Rings = new(Color, float, int)[2]
		{
			(new Color(255, 110, 205, 255), 0.24f, 1),
			(new Color(255, 110, 205, 255), 0.1f, 2)
		},
		RingBlend = GameBlendModes.Add,
		Pool = new Color(255, 170, 230, 255),
		PoolAlpha = 0.34f,
		PoolBlend = GameBlendModes.Add,
		Torso = new Color(255, 160, 220, 255),
		Ghosts = true,
		Wisps = 5f,
		Motes = 14f,
		MoteTint = new Color(255, 225, 245, 255)
	};

	private static readonly Style Kaioken = new Style
	{
		Layers = new Layer[3]
		{
			new Layer
			{
				Rate = 80f,
				Life = (Min: 420, Max: 640),
				Rise = (Min: 14f, Max: 24f),
				Width = (Min: 6f, Max: 9f),
				Height = (Min: 8f, Max: 12f),
				Push = 3f,
				Tint = new Color(255, 190, 20, 40),
				Alpha = 0.4f,
				Blend = GameBlendModes.None,
				Jitter = 1.4f,
				JitterMs = 160f
			},
			new Layer
			{
				Rate = 95f,
				Life = (Min: 360, Max: 560),
				Rise = (Min: 16f, Max: 26f),
				Width = (Min: 5f, Max: 7f),
				Height = (Min: 7f, Max: 10f),
				Push = 1.8f,
				Tint = new Color(255, 255, 70, 70),
				Alpha = 0.38f,
				Blend = GameBlendModes.None,
				Jitter = 1.4f,
				JitterMs = 160f,
				Front = true
			},
			new Layer
			{
				Rate = 90f,
				Life = (Min: 280, Max: 440),
				Rise = (Min: 18f, Max: 28f),
				Width = (Min: 3.5f, Max: 5f),
				Height = (Min: 5f, Max: 8f),
				Push = 0.8f,
				Tint = new Color(255, 255, 195, 200),
				Alpha = 0.45f,
				Blend = GameBlendModes.None,
				JitterMs = 160f,
				Front = true,
				Body = 0.3f
			}
		},
		Rings = new(Color, float, int)[2]
		{
			(new Color(255, 255, 90, 100), 0.36f, 1),
			(new Color(255, 200, 30, 50), 0.16f, 2)
		},
		RingBlend = GameBlendModes.None,
		Pool = new Color(255, 225, 50, 70),
		PoolAlpha = 0.18f,
		PoolBlend = GameBlendModes.None,
		Motes = 5f,
		MoteTint = new Color(255, 255, 150, 120),
		Beats = true,
		Swell = 1f
	};

	private static readonly Style God = new Style
	{
		Layers = new Layer[6]
		{
			new Layer
			{
				Rate = 120f,
				Life = (Min: 640, Max: 960),
				Rise = (Min: 22f, Max: 36f),
				Width = (Min: 9f, Max: 13f),
				Height = (Min: 12f, Max: 18f),
				Push = 4f,
				Tint = new Color(255, 140, 14, 44),
				Alpha = 0.46f,
				Blend = GameBlendModes.None,
				Jitter = 1.8f,
				JitterMs = 230f
			},
			new Layer
			{
				Rate = 120f,
				Life = (Min: 520, Max: 800),
				Rise = (Min: 26f, Max: 42f),
				Width = (Min: 7f, Max: 10f),
				Height = (Min: 10f, Max: 15f),
				Push = 2.4f,
				Tint = new Color(255, 232, 40, 68),
				Alpha = 0.42f,
				Blend = GameBlendModes.None,
				Jitter = 1.6f,
				JitterMs = 200f,
				Front = true
			},
			new Layer
			{
				Rate = 100f,
				Life = (Min: 400, Max: 640),
				Rise = (Min: 30f, Max: 48f),
				Width = (Min: 5f, Max: 7f),
				Height = (Min: 8f, Max: 12f),
				Push = 1.2f,
				Tint = new Color(255, 255, 140, 50),
				Alpha = 0.36f,
				Blend = GameBlendModes.Add,
				Jitter = 1.4f,
				JitterMs = 180f,
				Front = true
			},
			new Layer
			{
				Rate = 90f,
				Life = (Min: 300, Max: 480),
				Rise = (Min: 34f, Max: 52f),
				Width = (Min: 3.5f, Max: 5f),
				Height = (Min: 6f, Max: 9f),
				Push = 0.4f,
				Tint = new Color(255, 255, 222, 150),
				Alpha = 0.42f,
				Blend = GameBlendModes.Add,
				JitterMs = 160f,
				Front = true,
				Body = 0.3f
			},
			new Layer
			{
				Rate = 26f,
				Life = (Min: 520, Max: 780),
				Rise = (Min: 30f, Max: 46f),
				Width = (Min: 8f, Max: 11f),
				Height = (Min: 18f, Max: 26f),
				Push = 1.5f,
				Tint = new Color(255, 200, 26, 56),
				Alpha = 0.5f,
				Blend = GameBlendModes.None,
				Jitter = 2.2f,
				JitterMs = 260f,
				From = Origin.Top,
				Core = new Color(255, 255, 190, 90),
				CoreAlpha = 0.5f
			},
			new Layer
			{
				Rate = 34f,
				Life = (Min: 380, Max: 620),
				Rise = (Min: 10f, Max: 20f),
				Width = (Min: 6f, Max: 9f),
				Height = (Min: 12f, Max: 20f),
				Push = 0f,
				Tint = new Color(255, 190, 20, 50),
				Alpha = 0.45f,
				Blend = GameBlendModes.None,
				Jitter = 1.6f,
				JitterMs = 200f,
				Front = true,
				From = Origin.Ground,
				Core = new Color(255, 255, 170, 80),
				CoreAlpha = 0.45f
			}
		},
		Rings = new(Color, float, int)[2]
		{
			(new Color(255, 255, 198, 96), 0.42f, 1),
			(new Color(255, 170, 18, 50), 0.26f, 2)
		},
		RingBlend = GameBlendModes.None,
		Pool = new Color(255, 255, 110, 50),
		PoolAlpha = 0.32f,
		PoolBlend = GameBlendModes.Add,
		PoolSize = 1.4f,
		PoolSwell = 0.15f,
		Torso = new Color(255, 255, 170, 90),
		TorsoAlpha = 0.12f,
		BodyTint = new Color(255, 255, 150, 80),
		BodyTintAlpha = (Alpha: 0.05f, Pulse: 0.16f),
		Motes = 16f,
		MoteTint = new Color(255, 255, 200, 96),
		MoteAlt = new Color(255, 255, 110, 50),
		MoteRise = (Min: 24f, Max: 58f),
		MoteLife = (Min: 800, Max: 1600),
		Surges = true,
		PulseRate = 0.8f,
		Swell = 1.4f,
		Tall = 0.3f,
		Burst = 14
	};

	private const long SurgeMs = 2600L;

	private const long SurgePeakMs = 450L;

	private static readonly Color GhostTint = new Color(255, 150, 190, 255);

	private static readonly Color WispTint = new Color(255, 180, 225, 255);

	private const long StaleMs = 250L;

	private static readonly Dictionary<Guid, State> States = new Dictionary<Guid, State>();

	private static readonly Dictionary<(IGameTexture, int, int, int, int), ((int X, int Y)[] Edge, (int X, int Y)[] Body)> Contours = new Dictionary<(IGameTexture, int, int, int, int), ((int, int)[], (int, int)[])>();

	private static readonly Random Random = new Random();

	private static long _lastSweep;

	private static float Between((float Min, float Max) range)
	{
		return range.Min + (float)Random.NextDouble() * (range.Max - range.Min);
	}

	private static long Between((int Min, int Max) range)
	{
		return Random.Next(range.Min, range.Max + 1);
	}

	private static Color Tint(Color color, float alpha)
	{
		return new Color((int)Math.Clamp(255f * alpha * (float)(int)color.A / 255f, 0f, 255f), color.R, color.G, color.B);
	}

	private static Style? StyleOf(Entity entity)
	{
		if (!(entity is Player))
		{
			return null;
		}
		if (Transformation.IsUltraForm(entity.TransformedSprite))
		{
			return UltraInstinct;
		}
		if (IsGodForm(entity.TransformedSprite))
		{
			return God;
		}
		if (!Transformation.IsKaiokenForm(entity.TransformedSprite))
		{
			return null;
		}
		return Kaioken;
	}

	private static bool IsGodForm(string? form)
	{
		if (string.IsNullOrEmpty(form))
		{
			return false;
		}
		string text = Path.GetFileNameWithoutExtension(form).ToLowerInvariant();
		if ((text.Contains("ssg") || text.Contains("god")) && !text.Contains("ssgss"))
		{
			return !text.Contains("blue");
		}
		return false;
	}

	private static long SurgeClock(Entity entity, long now)
	{
		return now + (entity.Id.GetHashCode() & 0x7FF);
	}

	private static float PulseOf(Entity entity, Style style, long now)
	{
		if (style.Beats)
		{
			return Transformation.KaiokenBeat(entity, now);
		}
		if (!style.Surges)
		{
			return 0f;
		}
		long num = SurgeClock(entity, now) % 2600;
		if (num < 700)
		{
			if (num < 450)
			{
				return Ease((float)num / 450f);
			}
			return 1f;
		}
		if (num < 1700)
		{
			return 1f - Ease((float)(num - 700) / 1000f);
		}
		return 0f;
		static float Ease(float k)
		{
			return k * k * (3f - 2f * k);
		}
	}

	public static void DrawBehind(Entity entity, FloatRect destination, IGameTexture? texture, FloatRect source)
	{
		long milliseconds = Timing.Global.Milliseconds;
		Sweep(milliseconds);
		Style style = ((texture != null && source.Width > 0f) ? StyleOf(entity) : null);
		if (!States.TryGetValue(entity.Id, out State value))
		{
			if (style == null)
			{
				return;
			}
			value = new State
			{
				LastUpdate = milliseconds
			};
			States[entity.Id] = value;
		}
		if (style != null && style != value.Style)
		{
			Clear(value);
			value.Style = style;
		}
		Style style2 = value.Style;
		if (style2 == null)
		{
			States.Remove(entity.Id);
			return;
		}
		long num = Math.Clamp(milliseconds - value.LastUpdate, 0L, 50L);
		if (milliseconds - value.LastUpdate > 250)
		{
			Clear(value);
			value.LastCenterX = float.NaN;
		}
		value.LastUpdate = milliseconds;
		bool flag = (value.Active = style != null);
		float num2 = destination.X + destination.Width / 2f;
		float num3 = destination.Y + destination.Height - 2f;
		if (!float.IsNaN(value.LastCenterX) && num > 0)
		{
			float num4 = num2 - value.LastCenterX;
			float num5 = ((Math.Abs(num4) > 48f) ? 0f : (num4 * 1000f / (float)num));
			value.Velocity += (num5 - value.Velocity) * Math.Min(1f, (float)num / 90f);
		}
		value.LastCenterX = num2;
		bool flag2 = Math.Abs(value.Velocity) > 12f;
		float num6 = 0.5f + 0.5f * MathF.Sin((float)milliseconds / 900f);
		float num7 = PulseOf(entity, style2, milliseconds);
		float num8 = Transformation.FormAuraStrength(entity, milliseconds);
		if (flag)
		{
			((int, int)[], (int, int)[])? tuple = ContourOf(texture, source);
			if (tuple.HasValue)
			{
				((int, int)[], (int, int)[]) valueOrDefault = tuple.GetValueOrDefault();
				Emit(value, style2, milliseconds, (long)((float)num * num8), SurgeClock(entity, milliseconds), destination, valueOrDefault, num7);
				if (style2.Ghosts && flag2 && milliseconds - value.LastGhost > ((Math.Abs(value.Velocity) > 80f) ? 60 : 100))
				{
					IGameTexture gameTexture = KiCharge.MaskOf(texture, source, 0);
					if (gameTexture != null)
					{
						value.LastGhost = milliseconds;
						value.Ghosts.Add(new Ghost
						{
							Mask = gameTexture,
							Destination = destination,
							Born = milliseconds
						});
					}
				}
			}
		}
		if (flag)
		{
			IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Misc, "ki_charge_sol.png");
			if (texture2 != null)
			{
				float num9 = style2.PoolSize * (1f + style2.PoolSwell * num7);
				float num10 = 64f * num9;
				float num11 = 24f * num9;
				Graphics.DrawGameTexture(texture2, new FloatRect(0f, 0f, texture2.Width, texture2.Height), new FloatRect(num2 - num10 / 2f, num3 + 1f - num11 / 2f, num10, num11), Tint(style2.Pool, (style2.PoolAlpha + 0.1f * num6 + 0.15f * num7) * num8), null, style2.PoolBlend);
			}
		}
		Update(value, milliseconds, num);
		DrawFlames(value, style2, milliseconds, num7, front: false);
		float num12 = ((Math.Abs(value.Velocity) > 80f) ? 300f : 380f);
		for (int num13 = value.Ghosts.Count - 1; num13 >= 0; num13--)
		{
			Ghost ghost = value.Ghosts[num13];
			float num14 = (float)(milliseconds - ghost.Born) / num12;
			if (num14 >= 1f)
			{
				value.Ghosts.RemoveAt(num13);
			}
			else
			{
				float num15 = (1f - num14) * (1f - num14);
				Graphics.DrawGameTexture(ghost.Mask, new FloatRect(0f, 0f, ghost.Mask.Width, ghost.Mask.Height), ghost.Destination, Tint(GhostTint, 0.42f * num15), null, GameBlendModes.Add);
			}
		}
		if (flag)
		{
			value.BodyMask = ((style2.BodyTint != null) ? KiCharge.MaskOf(texture, source, 0) : null);
			(Color, float, int)[] rings = style2.Rings;
			for (int i = 0; i < rings.Length; i++)
			{
				(Color, float, int) tuple2 = rings[i];
				Color item = tuple2.Item1;
				float item2 = tuple2.Item2;
				int item3 = tuple2.Item3;
				float num16 = ((item3 == 1) ? (0.08f * num6 + 0.12f * num7) : 0f);
				DrawRing(texture, source, destination, item3, Tint(item, (item2 + num16) * num8), style2.RingBlend);
			}
		}
		if (!flag && Empty(value))
		{
			States.Remove(entity.Id);
		}
	}

	private static void DrawRing(IGameTexture texture, FloatRect source, FloatRect destination, int grow, Color color, GameBlendModes blend)
	{
		IGameTexture gameTexture = KiCharge.MaskOf(texture, source, grow);
		if (gameTexture != null)
		{
			Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, gameTexture.Width, gameTexture.Height), new FloatRect(destination.X - (float)grow, destination.Y - (float)grow, destination.Width + (float)(2 * grow), destination.Height + (float)(2 * grow)), color, null, blend);
		}
	}

	public static void DrawFront(Entity entity, FloatRect destination)
	{
		if (!States.TryGetValue(entity.Id, out State value))
		{
			return;
		}
		Style style = value.Style;
		if (style == null)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		float num = 0.5f + 0.5f * MathF.Sin((float)milliseconds / 900f);
		float num2 = PulseOf(entity, style, milliseconds);
		if (value.Active)
		{
			Color bodyTint = style.BodyTint;
			if ((object)bodyTint != null)
			{
				IGameTexture bodyMask = value.BodyMask;
				if (bodyMask != null)
				{
					Graphics.DrawGameTexture(bodyMask, new FloatRect(0f, 0f, bodyMask.Width, bodyMask.Height), destination, Tint(bodyTint, style.BodyTintAlpha.Alpha + style.BodyTintAlpha.Pulse * num2), null, GameBlendModes.Add);
				}
			}
		}
		DrawFlames(value, style, milliseconds, num2, front: true);
		if (value.Active)
		{
			Color torso = style.Torso;
			if ((object)torso != null)
			{
				IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "ki_halo.png");
				if (texture != null)
				{
					float num3 = destination.X + destination.Width / 2f;
					float num4 = destination.Y + destination.Height - 28f;
					Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), new FloatRect(num3 - 18f, num4 - 18f, 36f, 36f), Tint(torso, style.TorsoAlpha + 0.05f * num + 0.1f * num2), null, GameBlendModes.Add);
				}
			}
		}
		IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Misc, "ui_langue.png");
		IGameTexture texture3 = Globals.ContentManager.GetTexture(TextureType.Misc, "ui_point.png");
		foreach (Extra extra in value.Extras)
		{
			float num5 = (float)(milliseconds - extra.Born) / (float)extra.Life;
			if (num5 >= 1f)
			{
				continue;
			}
			float num6 = ((num5 < 0.2f) ? (num5 / 0.2f) : (1f - (num5 - 0.2f) / 0.8f));
			if (extra.Wisp)
			{
				if (texture2 != null)
				{
					DrawTongue(texture2, extra.X, extra.Y, extra.W * (1f - num5 * 0.5f), extra.H * (0.8f + num5 * 0.5f), MathF.Sin(num5 * 5f + extra.Seed) * 2f, Tint(WispTint, 0.4f * num6), GameBlendModes.Add);
				}
			}
			else if (texture3 != null)
			{
				float num7 = ((style.MoteAlt != null) ? (0.6f + 0.4f * MathF.Sin((float)milliseconds / 55f + extra.Seed)) : 1f);
				Graphics.DrawGameTexture(texture3, new FloatRect(0f, 0f, texture3.Width, texture3.Height), new FloatRect(MathF.Round(extra.X), MathF.Round(extra.Y), extra.Size, extra.Size), Tint(extra.Tint ?? style.MoteTint, 0.85f * num6 * num7), null, GameBlendModes.Add);
			}
		}
	}

	private static void Emit(State state, Style style, long now, long dt, long clock, FloatRect destination, ((int X, int Y)[] Edge, (int X, int Y)[] Body) shape, float beat)
	{
		if (shape.Edge.Length == 0)
		{
			return;
		}
		float num = destination.X + destination.Width / 2f;
		float num2 = destination.Y + destination.Height * 0.55f;
		float num3 = destination.Y + destination.Height - 2f;
		(int, int)[] array = null;
		for (int i = 0; i < style.Layers.Length && i < state.Debt.Length; i++)
		{
			Layer layer = style.Layers[i];
			state.Debt[i] += layer.Rate * (1f + style.PulseRate * beat) * (float)dt / 1000f;
			while (state.Debt[i] >= 1f)
			{
				state.Debt[i] -= 1f;
				int num4;
				float x;
				float y;
				if (layer.From == Origin.Ground)
				{
					x = num + Between((Min: -13f, Max: 13f));
					y = num3 + Between((Min: -1.5f, Max: 0.5f));
					num4 = (int)destination.Height;
				}
				else
				{
					(int, int)[] array2;
					if (layer.From == Origin.Top)
					{
						if (array == null)
						{
							int highest = shape.Edge.Min(((int X, int Y) point) => point.Y);
							array = shape.Edge.Where(((int X, int Y) point) => point.Y <= highest + 6).ToArray();
						}
						array2 = array;
					}
					else
					{
						(int X, int Y)[] array3;
						if (!(layer.Body > 0f) || !(Random.NextDouble() < (double)layer.Body) || shape.Body.Length == 0)
						{
							(array3, _) = shape;
						}
						else
						{
							array3 = shape.Body;
						}
						array2 = array3;
					}
					(int, int) tuple2 = array2[Random.Next(array2.Length)];
					int item = tuple2.Item1;
					int item2 = tuple2.Item2;
					num4 = item2;
					x = destination.X + (float)item + 0.5f;
					y = destination.Y + (float)item2 + 0.5f;
					float num5 = x - num;
					float num6 = y - num2;
					float num7 = MathF.Max(1f, MathF.Sqrt(num5 * num5 + num6 * num6));
					float num8 = (layer.Push + style.Swell * beat) * (0.55f + 0.45f * (float)Random.NextDouble());
					int num9 = ((num5 != 0f) ? MathF.Sign(num5) : ((Random.Next(2) != 0) ? 1 : (-1)));
					x += (float)num9 * (MathF.Abs(num5) / num7 * num8 + num8 * style.Side);
					y += num6 / num7 * num8 * 0.4f;
				}
				state.Flames.Add(new Flame
				{
					Layer = i,
					X = x,
					Y = y,
					Born = now,
					Life = Between(layer.Life),
					Vy = 0f - Between(layer.Rise),
					Vx = Between((Min: -3f, Max: 3f)) - state.Velocity * 0.18f,
					W = Between(layer.Width),
					H = Between(layer.Height),
					Seed = (float)(Random.NextDouble() * Math.PI * 2.0),
					Front = (layer.Front && (float)num4 > destination.Height - 18f && Random.NextDouble() < 0.3)
				});
			}
		}
		if (style.Wisps > 0f && Random.NextDouble() < (double)dt / 1000.0 * (double)style.Wisps)
		{
			int num10 = shape.Edge.Min(((int X, int Y) point) => point.Y);
			state.Extras.Add(new Extra
			{
				Wisp = true,
				X = num + Between((Min: -7f, Max: 7f)),
				Y = destination.Y + (float)num10 - Between((Min: 4f, Max: 12f)),
				Vx = Between((Min: -4f, Max: 4f)) - state.Velocity * 0.2f,
				Vy = 0f - Between((Min: 20f, Max: 30f)),
				Born = now,
				Life = Between((Min: 500, Max: 800)),
				W = Between((Min: 3f, Max: 4.5f)),
				H = Between((Min: 8f, Max: 13f)),
				Seed = (float)(Random.NextDouble() * Math.PI * 2.0)
			});
		}
		if (Random.NextDouble() < (double)dt / 1000.0 * (double)style.Motes * (double)(1f + beat))
		{
			AddMote(state, style, now, destination, num);
		}
		if (style.Burst <= 0)
		{
			return;
		}
		long num11 = (clock - 450) / 2600;
		if (num11 == state.SurgeCycle)
		{
			return;
		}
		if (state.SurgeCycle != long.MinValue)
		{
			for (int num12 = 0; num12 < style.Burst; num12++)
			{
				AddMote(state, style, now, destination, num);
			}
		}
		state.SurgeCycle = num11;
	}

	private static void AddMote(State state, Style style, long now, FloatRect destination, float centerX)
	{
		List<Extra> extras = state.Extras;
		Extra obj = new Extra
		{
			X = centerX + Between((Min: -18f, Max: 18f)),
			Y = destination.Y + Between((Min: 8f, Max: destination.Height)),
			Vx = Between((Min: -3f, Max: 3f)) - state.Velocity * 0.25f,
			Vy = 0f - Between(style.MoteRise),
			Born = now,
			Life = Between(style.MoteLife),
			Size = ((!(Random.NextDouble() < 0.2)) ? 1 : 2),
			Seed = (float)(Random.NextDouble() * Math.PI * 2.0)
		};
		Color moteAlt = style.MoteAlt;
		obj.Tint = (((object)moteAlt != null && Random.NextDouble() < 0.45) ? moteAlt : null);
		extras.Add(obj);
	}

	private static void Update(State state, long now, long dt)
	{
		float num = (float)dt / 1000f;
		for (int num2 = state.Flames.Count - 1; num2 >= 0; num2--)
		{
			Flame flame = state.Flames[num2];
			float num3 = (float)(now - flame.Born) / (float)flame.Life;
			if (num3 >= 1f)
			{
				state.Flames.RemoveAt(num2);
			}
			else
			{
				flame.X += (flame.Vx + MathF.Sin((float)now / 260f + flame.Seed) * 5f) * num;
				flame.Y += flame.Vy * (1f - num3 * 0.4f) * num;
			}
		}
		for (int num4 = state.Extras.Count - 1; num4 >= 0; num4--)
		{
			Extra extra = state.Extras[num4];
			if (now - extra.Born >= extra.Life)
			{
				state.Extras.RemoveAt(num4);
			}
			else
			{
				extra.X += (extra.Vx + (extra.Wisp ? (MathF.Sin((float)now / 240f + extra.Seed) * 6f) : 0f)) * num;
				extra.Y += extra.Vy * num;
			}
		}
	}

	private static void DrawFlames(State state, Style style, long now, float beat, bool front)
	{
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "ui_langue.png");
		if (texture == null)
		{
			return;
		}
		foreach (Flame flame in state.Flames)
		{
			if ((front && !flame.Front) || flame.Layer >= style.Layers.Length)
			{
				continue;
			}
			float num = (float)(now - flame.Born) / (float)flame.Life;
			if (!(num >= 1f))
			{
				Layer layer = style.Layers[flame.Layer];
				float num2 = ((num < 0.25f) ? (num / 0.25f) : (1f - (num - 0.25f) / 0.75f));
				float num3 = (0.7f + num * 0.9f) * (1f + style.Tall * beat);
				float num4 = flame.Vx * 0.12f + MathF.Sin((float)now / layer.JitterMs + flame.Seed) * layer.Jitter;
				float num5 = (front ? 0.55f : 1f);
				float num6 = flame.W * (1.1f - num * 0.55f);
				float num7 = flame.H * num3;
				DrawTongue(texture, flame.X, flame.Y, num6, num7, num4, Tint(layer.Tint, layer.Alpha * num2 * num5 * (1f + 0.5f * beat)), layer.Blend);
				Color core = layer.Core;
				if ((object)core != null)
				{
					DrawTongue(texture, flame.X, flame.Y, num6 * 0.45f, num7 * 0.62f, num4 * 0.8f, Tint(core, layer.CoreAlpha * num2 * num5 * (1f + 0.4f * beat)), GameBlendModes.Add);
				}
			}
		}
	}

	private static void DrawTongue(IGameTexture langue, float x, float y, float w, float h, float lean, Color color, GameBlendModes blend)
	{
		if (color.A > 0 && !(w <= 0.2f) && !(h <= 0.2f))
		{
			float num = h * 1.16f;
			float rotationDegrees = MathF.Atan2(lean, h) * 180f / (float)Math.PI;
			Graphics.DrawGameTexture(langue, new FloatRect(0f, 0f, langue.Width, langue.Height), new FloatRect(x + lean / 2f - w / 2f, y - num * 0.86f, w, num), color, null, blend, null, rotationDegrees);
		}
	}

	private static bool Empty(State state)
	{
		if (state.Flames.Count == 0 && state.Extras.Count == 0)
		{
			return state.Ghosts.Count == 0;
		}
		return false;
	}

	private static void Clear(State state)
	{
		state.Flames.Clear();
		state.Extras.Clear();
		state.Ghosts.Clear();
		Array.Clear(state.Debt);
		state.SurgeCycle = long.MinValue;
	}

	private static void Sweep(long now)
	{
		if (now - _lastSweep < 5000)
		{
			return;
		}
		_lastSweep = now;
		foreach (Guid item in (from pair in States
			where now - pair.Value.LastUpdate > 5000
			select pair.Key).ToList())
		{
			States.Remove(item);
		}
	}

	internal static ((int X, int Y)[] Edge, (int X, int Y)[] Body)? ContourOf(IGameTexture texture, FloatRect source)
	{
		(IGameTexture, int, int, int, int) key = (texture, (int)source.X, (int)source.Y, (int)source.Width, (int)source.Height);
		if (Contours.TryGetValue(key, out ((int, int)[], (int, int)[]) value))
		{
			return value;
		}
		try
		{
			Texture2D texture2 = texture.GetTexture<Texture2D>();
			int w;
			int h;
			Microsoft.Xna.Framework.Color[] colors;
			if (texture2 != null)
			{
				AtlasReference atlasReference = texture.AtlasReference;
				if ((object)atlasReference == null || !atlasReference.IsRotated)
				{
					w = key.Item4;
					h = key.Item5;
					int num = key.Item2;
					int num2 = key.Item3;
					AtlasReference atlasReference2 = texture.AtlasReference;
					if ((object)atlasReference2 != null)
					{
						num += atlasReference2.Bounds.X;
						num2 += atlasReference2.Bounds.Y;
					}
					colors = new Microsoft.Xna.Framework.Color[w * h];
					texture2.GetData(0, new Microsoft.Xna.Framework.Rectangle(num, num2, w, h), colors, 0, colors.Length);
					List<(int, int)> list = new List<(int, int)>();
					List<(int, int)> list2 = new List<(int, int)>();
					for (int i = 0; i < h; i++)
					{
						for (int j = 0; j < w; j++)
						{
							if (Opaque(j, i))
							{
								list2.Add((j, i));
								if (!Opaque(j - 1, i) || !Opaque(j + 1, i) || !Opaque(j, i - 1))
								{
									list.Add((j, i));
								}
							}
						}
					}
					if (Contours.Count > 256)
					{
						Contours.Clear();
					}
					((int, int)[], (int, int)[]) value2 = (list.ToArray(), list2.ToArray());
					Contours[key] = value2;
					return value2;
				}
			}
			return null;
			bool Opaque(int x, int y)
			{
				if (x >= 0 && y >= 0 && x < w && y < h)
				{
					return colors[y * w + x].A > 40;
				}
				return false;
			}
		}
		catch
		{
			return null;
		}
	}
}
