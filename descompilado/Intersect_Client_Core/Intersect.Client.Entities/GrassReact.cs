using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Maps;
using Intersect.Client.MonoGame.Graphics;
using Intersect.Config;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Entities;

public static class GrassReact
{
	private enum Kind : byte
	{
		Plate,
		Ring,
		Wake,
		Burn
	}

	private sealed class Stamp
	{
		public Kind Kind;

		public Guid MapId;

		public float LocalX;

		public float LocalY;

		public long Born;

		public long LifeMs;

		public float Ux;

		public float Uy;

		public float Rx;

		public float Ry;

		public float Force;
	}

	private sealed class Tracker
	{
		public float LastX = float.NaN;

		public float LastY = float.NaN;

		public int Steps;

		public long SeenAt;

		public long ChargeSince = -1L;

		public long NextRingAt;

		public float WakeX = float.NaN;

		public float WakeY = float.NaN;
	}

	private sealed class Cell
	{
		public MapInstance Map;

		public Meteo.Analyse Analyse;

		public int LocalX;

		public int LocalY;

		public readonly float[] Ox = new float[1024];

		public readonly float[] Oy = new float[1024];

		public readonly float[] Light = new float[1024];

		public readonly float[] Burn = new float[1024];

		public readonly float[] Torn = new float[1024];

		public readonly float[] Sprout = new float[1024];

		public void Clear()
		{
			Array.Clear(Ox);
			Array.Clear(Oy);
			Array.Clear(Light);
			Array.Clear(Burn);
			Array.Clear(Torn);
			Array.Clear(Sprout);
		}
	}

	private sealed class Blade
	{
		public float X;

		public float Y;

		public float Vx;

		public float Vy;

		public long Born;

		public int LifeMs;

		public Color Color = new Color(255, 150, 190, 40);
	}

	private delegate void PixelEffect(float nx, float ny, float d, ref float ox, ref float oy, ref float light);

	private const int Chunk = 32;

	private const int AtlasSize = 512;

	private const int AtlasColumns = 16;

	private const int Slots = 256;

	private const long BurnLifeMs = 60000L;

	private const long BurnRegrowFromMs = 3000L;

	private static readonly List<Stamp> Stamps = new List<Stamp>();

	private static readonly Dictionary<Guid, Tracker> Trackers = new Dictionary<Guid, Tracker>();

	private static readonly Dictionary<(Guid, int, int), Cell> Cells = new Dictionary<(Guid, int, int), Cell>();

	private static readonly List<Cell> CellOrder = new List<Cell>();

	private static readonly Stack<Cell> Pool = new Stack<Cell>();

	private static readonly List<Blade> Blades = new List<Blade>();

	private static readonly Random Noise = new Random();

	private static readonly byte[] Pixels = new byte[1048576];

	private static IGameTexture? _atlas;

	private static bool _broken;

	private static long _lastUpdate;

#if ANDROID
	private static long _ultimaComposicion;
#endif

	private static MapInstance? _map;

	private static Meteo.Analyse? _analyse;

	private static Cell? _cell;

	private static readonly Color[] BladeColors = new Color[3]
	{
		new Color(255, 150, 190, 40),
		new Color(255, 200, 230, 80),
		new Color(255, 110, 140, 20)
	};

	public static void Burn(float worldX, float worldY, bool torn)
	{
		if (_broken || !Meteo.EstHerbe(worldX, worldY))
		{
			return;
		}
		int num = 0;
		for (int num2 = Stamps.Count - 1; num2 >= 0; num2--)
		{
			if (Stamps[num2].Kind == Kind.Burn && ++num >= 40)
			{
				Stamps.RemoveAt(num2);
			}
		}
		AddStamp(Kind.Burn, worldX, worldY, Timing.Global.Milliseconds, 60000L, 0f, 0f, torn ? 30 : 17, torn ? 15 : 8, (!torn) ? 1 : 2);
	}

	public static void DrawGround()
	{
		if (_broken)
		{
			return;
		}
		try
		{
			long milliseconds = Timing.Global.Milliseconds;
			long dt = ((_lastUpdate == 0L) ? 16 : Math.Clamp(milliseconds - _lastUpdate, 0L, 50L));
			_lastUpdate = milliseconds;
#if !ANDROID
			ResetCells();
			TrackEntities(milliseconds, dt);
			ApplyStamps(milliseconds);
			if (CellOrder.Count > 0)
			{
				Compose();
				Draw();
			}
#else
			if (milliseconds - _ultimaComposicion >= 33)
			{
				long dtComposicion = (_ultimaComposicion == 0L) ? 33 : Math.Clamp(milliseconds - _ultimaComposicion, 0L, 50L);
				_ultimaComposicion = milliseconds;
				ResetCells();
				TrackEntities(milliseconds, dtComposicion);
				ApplyStamps(milliseconds);
				if (CellOrder.Count > 0)
				{
					Compose();
					Draw();
				}
			}
			else if (CellOrder.Count > 0)
			{
				Draw();
			}
#endif
			ChronoImage.Tour("sol : herbe, cases foulees");
			DrawBlades(milliseconds);
			ChronoImage.Tour("sol : herbe, brins arraches");
			Meteo.DessinerRafales();
			ChronoImage.Tour("sol : rafales de vent");
		}
		catch (Exception exception)
		{
			_broken = true;
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Herbe qui reagit coupee");
		}
	}

	private static void TrackEntities(long now, long dt)
	{
		if (Globals.Entities == null)
		{
			return;
		}
		FloatRect worldViewport = Graphics.WorldViewport;
		foreach (Entity value2 in Globals.Entities.Values)
		{
			if (value2 == null || value2.IsHidden || (value2.Type != EntityType.Player && value2.Type != EntityType.GlobalEntity) || value2 is Critter)
			{
				continue;
			}
			Vector2 origin = value2.Origin;
			if (origin.X < worldViewport.Left - 48f || origin.X > worldViewport.Right + 48f || origin.Y < worldViewport.Top - 48f || origin.Y > worldViewport.Bottom + 64f)
			{
				continue;
			}
			if (!Trackers.TryGetValue(value2.Id, out Tracker value))
			{
				value = new Tracker();
				Trackers[value2.Id] = value;
			}
			value.SeenAt = now;
			if (Flight.IsAirborne(value2))
			{
				value.LastX = float.NaN;
				TrackFlight(value2, value, now);
				continue;
			}
			value.WakeX = float.NaN;
			float x = origin.X;
			float num = origin.Y - 1f;
			bool flag = value2.IsCharging || value2.IsTransforming;
			if (!Meteo.EstHerbe(x, num))
			{
				value.LastX = float.NaN;
				value.ChargeSince = -1L;
				continue;
			}
			if (flag)
			{
				if (value.ChargeSince < 0)
				{
					value.ChargeSince = now;
					value.NextRingAt = now;
				}
				Charge(x, num, now - value.ChargeSince, now);
				if (now >= value.NextRingAt)
				{
					value.NextRingAt = now + 800;
					AddStamp(Kind.Ring, x, num, now, 700L, 0f, 0f, 90f, 0f, 1f);
				}
				if (Noise.NextDouble() < (double)((float)dt / 70f))
				{
					double num2 = Noise.NextDouble() * Math.PI * 2.0;
					AddBlade(x + (float)Math.Cos(num2) * 10f, num + (float)Math.Sin(num2) * 5f, (float)Math.Cos(num2) * Between(20f, 40f), (float)Math.Sin(num2) * Between(8f, 16f) - Between(20f, 35f), now);
				}
				value.LastX = float.NaN;
				continue;
			}
			value.ChargeSince = -1L;
			bool flag2 = value2.IsSprinting && value2.IsMoving;
			Part(x, num, (!value2.IsMoving) ? 7f : (flag2 ? 12f : 10f));
			if (float.IsNaN(value.LastX) || MathF.Abs(x - value.LastX) + MathF.Abs(num - value.LastY) > 40f)
			{
				value.LastX = x;
				value.LastY = num;
				continue;
			}
			float num3 = x - value.LastX;
			float num4 = num - value.LastY;
			float num5 = MathF.Sqrt(num3 * num3 + num4 * num4);
			if (!(num5 < 4f))
			{
				float num6 = num3 / num5;
				float num7 = num4 / num5;
				value.LastX = x;
				value.LastY = num;
				value.Steps++;
				bool flag3 = value.Steps % (flag2 ? 4 : 2) == 0;
				float num8 = ((value.Steps / (flag2 ? 4 : 2) % 2 == 1) ? 1f : (-1f)) * (flag2 ? 3f : 2f);
				float worldX = x - num6 * 3f + (flag3 ? ((0f - num7) * num8) : 0f);
				float worldY = num - num7 * 3f + (flag3 ? (num6 * num8) : 0f);
				AddStamp(Kind.Plate, worldX, worldY, now, flag2 ? 2800 : 2200, num6, num7, (!flag3) ? 5 : (flag2 ? 9 : 7), (!flag3) ? 3 : (flag2 ? 5 : 4), flag3 ? 1f : 0.55f);
				if (flag2 && Noise.NextDouble() < 0.35)
				{
					AddBlade(x + Between(-3f, 3f), num, (0f - num6) * Between(15f, 35f), (0f - num7) * Between(15f, 35f) - Between(28f, 48f), now);
				}
			}
		}
		if (Trackers.Count <= 64)
		{
			return;
		}
		foreach (Guid item in (from pair in Trackers
			where now - pair.Value.SeenAt > 5000
			select pair.Key).ToList())
		{
			Trackers.Remove(item);
		}
	}

	private static void TrackFlight(Entity entity, Tracker tracker, long now)
	{
		if (entity.FlightMode != FlightMode.Ki)
		{
			tracker.WakeX = float.NaN;
			return;
		}
		float x = entity.Origin.X;
		float num = entity.Origin.Y + Flight.Altitude(entity) - 1f;
		if (float.IsNaN(tracker.WakeX) || MathF.Abs(x - tracker.WakeX) + MathF.Abs(num - tracker.WakeY) > 60f)
		{
			tracker.WakeX = x;
			tracker.WakeY = num;
			return;
		}
		float num2 = x - tracker.WakeX;
		float num3 = num - tracker.WakeY;
		float num4 = MathF.Sqrt(num2 * num2 + num3 * num3);
		if (!(num4 < 3f))
		{
			tracker.WakeX = x;
			tracker.WakeY = num;
			if (Meteo.EstHerbe(x, num))
			{
				AddStamp(Kind.Wake, x, num, now, 1300L, num2 / num4, num3 / num4, 7f, 9f, 1f);
			}
		}
	}

	private static void AddStamp(Kind kind, float worldX, float worldY, long now, long life, float ux, float uy, float rx, float ry, float force)
	{
		if (Meteo.HerbeEn(worldX, worldY, out MapInstance carte) != null && carte != null)
		{
			if (Stamps.Count >= 900)
			{
				Stamps.RemoveAt(0);
			}
			Stamps.Add(new Stamp
			{
				Kind = kind,
				MapId = carte.Id,
				LocalX = worldX - (float)carte.X,
				LocalY = worldY - (float)carte.Y,
				Born = now,
				LifeMs = life,
				Ux = ux,
				Uy = uy,
				Rx = rx,
				Ry = ry,
				Force = force
			});
		}
	}

	private static void Part(float cx, float cy, float radius)
	{
		float ry = radius * 0.55f;
		ForEachPixel(cx, cy, radius, ry, delegate(float nx, float ny, float d, ref float ox, ref float oy, ref float light)
		{
			if (d < 0.42f)
			{
				light -= 0.26f * (1f - d / 0.42f);
			}
			else
			{
				float num = MathF.Sin((float)Math.PI * (d - 0.42f) / 0.58f);
				float num2 = 1f / MathF.Max(d, 0.001f);
				ox += nx * num2 * 2.6f * num;
				oy += ny * num2 * 1.6f * num;
				light += 0.18f * num;
			}
		});
	}

	private static void Charge(float cx, float cy, long since, long now)
	{
		float rise = Math.Min(1f, (float)since / 450f);
		float breath = 0.85f + 0.15f * MathF.Sin((float)now / 85f);
		ForEachPixel(cx, cy, 36f, 20f, delegate(float nx, float ny, float d, ref float ox, ref float oy, ref float light)
		{
			float num = rise * breath * ((d < 0.3f) ? (d / 0.3f) : (1f - (d - 0.3f) / 0.7f));
			float num2 = 1f / MathF.Max(d, 0.001f);
			ox += nx * num2 * 3.2f * num;
			oy += ny * num2 * 2f * num;
			light += ((d < 0.28f) ? (-0.26f) : 0.3f) * num;
		});
	}

	private static void ApplyStamps(long now)
	{
		Guid guid = default(Guid);
		MapInstance mapInstance = null;
		for (int num = Stamps.Count - 1; num >= 0; num--)
		{
			Stamp stamp = Stamps[num];
			float num2 = (float)(now - stamp.Born) / (float)stamp.LifeMs;
			if (num2 >= 1f)
			{
				Stamps.RemoveAt(num);
			}
			else if (!(num2 < 0f))
			{
				if (stamp.MapId != guid)
				{
					guid = stamp.MapId;
					mapInstance = (MapInstance.TryGet(stamp.MapId, out MapInstance instance) ? instance : null);
				}
				if (mapInstance != null)
				{
					float cx = (float)mapInstance.X + stamp.LocalX;
					float cy = (float)mapInstance.Y + stamp.LocalY;
					switch (stamp.Kind)
					{
					case Kind.Plate:
						Plate(stamp, cx, cy, num2);
						break;
					case Kind.Ring:
						Ring(stamp, cx, cy, num2);
						break;
					case Kind.Wake:
						Wake(stamp, cx, cy, num2);
						break;
					case Kind.Burn:
						BurnMark(stamp, cx, cy, now);
						break;
					}
				}
			}
		}
	}

	private static void Plate(Stamp s, float cx, float cy, float k)
	{
		float f = s.Force * ((k < 0.06f) ? (k / 0.06f) : MathF.Pow(1f - (k - 0.06f) / 0.94f, 1.5f));
		float extent = MathF.Max(s.Rx, s.Ry);
		ForEachPixel(cx, cy, extent, extent, delegate(float nx, float ny, float d, ref float ox, ref float oy, ref float light)
		{
			float num = nx * extent;
			float num2 = ny * extent;
			float num3 = num * s.Ux + num2 * s.Uy;
			float num4 = (0f - num) * s.Uy + num2 * s.Ux;
			float num5 = MathF.Sqrt(num3 / s.Rx * (num3 / s.Rx) + num4 / s.Ry * (num4 / s.Ry));
			if (!(num5 >= 1f))
			{
				float num6 = f * (1f - num5 * num5);
				ox += s.Ux * 2.6f * num6;
				oy += s.Uy * 2.6f * num6;
				light += ((num3 / s.Rx > 0.55f) ? (-0.12f) : 0.24f) * num6;
			}
		});
	}

	private static void Ring(Stamp s, float cx, float cy, float k)
	{
		float r = 8f + (s.Rx - 8f) * (1f - (1f - k) * (1f - k));
		float force = 1f - k;
		float outer = r + 9f;
		ForEachPixel(cx, cy, outer, outer * 0.58f, delegate(float nx, float ny, float d, ref float ox, ref float oy, ref float light)
		{
			float num = d * outer;
			float num2 = 1f - MathF.Abs(num - r) / 9f;
			if (!(num2 <= 0f))
			{
				float num3 = 1f / MathF.Max(d, 0.001f);
				ox += nx * num3 * 3f * num2 * force;
				oy += ny * num3 * 1.8f * num2 * force;
				light += ((num > r) ? 0.34f : (-0.08f)) * num2 * force;
			}
		});
	}

	private static void Wake(Stamp s, float cx, float cy, float k)
	{
		float f = ((k < 0.06f) ? (k / 0.06f) : MathF.Pow(1f - (k - 0.06f) / 0.94f, 1.4f));
		float extent = MathF.Max(s.Rx, s.Ry);
		ForEachPixel(cx, cy, extent, extent, delegate(float nx, float ny, float d, ref float ox, ref float oy, ref float light)
		{
			float num = nx * extent;
			float num2 = ny * extent;
			float num3 = num * s.Ux + num2 * s.Uy;
			float num4 = (0f - num) * s.Uy + num2 * s.Ux;
			float num5 = MathF.Sqrt(num3 / s.Rx * (num3 / s.Rx) + num4 / s.Ry * (num4 / s.Ry));
			if (!(num5 >= 1f))
			{
				float num6 = f * MathF.Sin((float)Math.PI * MathF.Min(1f, num5 * 1.15f));
				float num7 = ((num4 >= 0f) ? 1f : (-1f)) * 1.6f * num6;
				ox += (0f - s.Uy) * num7 + s.Ux * 1.2f * num6 * f;
				oy += s.Ux * num7 + s.Uy * 1.2f * num6 * f;
				light += ((num5 < 0.25f) ? (-0.1f) : 0.17f) * num6;
			}
		});
	}

	private static void BurnMark(Stamp s, float cx, float cy, long now)
	{
		long num = now - s.Born;
		float num2 = Math.Clamp((float)(num - 3000) / (float)(s.LifeMs - 3000), 0f, 1f);
		num2 = num2 * num2 * (3f - 2f * num2);
		float num3 = 1f - num2 * 0.92f;
		bool flag = s.Force >= 2f;
		float num4 = s.Rx * num3;
		float num5 = s.Ry * num3;
		if (num4 < 1f || num5 < 1f)
		{
			return;
		}
		int num6 = (int)MathF.Floor(cx - s.Rx);
		int num7 = (int)MathF.Ceiling(cx + s.Rx);
		int num8 = (int)MathF.Floor(cy - s.Ry);
		int num9 = (int)MathF.Ceiling(cy + s.Ry);
		for (int i = num8; i <= num9; i++)
		{
			for (int j = num6; j <= num7; j++)
			{
				float num10 = ((float)j - cx) / num4;
				float num11 = ((float)i - cy) / num5;
				float num12 = MathF.Sqrt(num10 * num10 + num11 * num11) + (float)(((j * 7 + i * 13) % 5 + 5) % 5) * 0.04f;
				if (!(num12 >= 1f) && CellAt(j, i, out Cell cell, out int index))
				{
					if (flag && num12 < 0.75f)
					{
						cell.Torn[index] = 1f;
					}
					else
					{
						cell.Burn[index] = MathF.Max(cell.Burn[index], MathF.Min(1f, (1f - num12) * 2.2f) * (flag ? 0.8f : 1f));
					}
					if (num2 > 0.3f && (float)(((j * 31 + i * 17 + (int)s.LocalX) % 11 + 11) % 11) < num2 * 5f)
					{
						cell.Sprout[index] = 1f;
					}
				}
			}
		}
	}

	private static void ForEachPixel(float cx, float cy, float rx, float ry, PixelEffect effect)
	{
		int num = (int)MathF.Floor(cx - rx);
		int num2 = (int)MathF.Ceiling(cx + rx);
		int num3 = (int)MathF.Floor(cy - ry);
		int num4 = (int)MathF.Ceiling(cy + ry);
		for (int i = num3; i <= num4; i++)
		{
			float num5 = ((float)i - cy) / ry;
			if (num5 <= -1f || num5 >= 1f)
			{
				continue;
			}
			for (int j = num; j <= num2; j++)
			{
				float num6 = ((float)j - cx) / rx;
				float num7 = MathF.Sqrt(num6 * num6 + num5 * num5);
				if (!(num7 >= 1f) && CellAt(j, i, out Cell cell, out int index))
				{
					effect(num6, num5, num7, ref cell.Ox[index], ref cell.Oy[index], ref cell.Light[index]);
				}
			}
		}
	}

	private static bool CellAt(int x, int y, out Cell cell, out int index)
	{
		cell = null;
		index = 0;
		if (_map == null || _analyse == null || x < _map.X || y < _map.Y || x >= _map.X + _analyse.L || y >= _map.Y + _analyse.H)
		{
			_analyse = Meteo.HerbeEn(x, y, out _map);
			_cell = null;
			if (_analyse == null || _map == null)
			{
				return false;
			}
		}
		int num = x - _map.X;
		int num2 = y - _map.Y;
		if (num < 0 || num2 < 0 || num >= _analyse.L || num2 >= _analyse.H || _analyse.Herbe[num2 * _analyse.L + num] == 0)
		{
			return false;
		}
		int num3 = num / 32 * 32;
		int num4 = num2 / 32 * 32;
		if (_cell == null || _cell.Map != _map || _cell.LocalX != num3 || _cell.LocalY != num4)
		{
			(Guid, int, int) key = (_map.Id, num3, num4);
			if (!Cells.TryGetValue(key, out Cell value))
			{
				if (CellOrder.Count >= 256)
				{
					return false;
				}
				value = ((Pool.Count > 0) ? Pool.Pop() : new Cell());
				value.Clear();
				value.Map = _map;
				value.Analyse = _analyse;
				value.LocalX = num3;
				value.LocalY = num4;
				Cells[key] = value;
				CellOrder.Add(value);
			}
			_cell = value;
		}
		cell = _cell;
		index = (num2 - num4) * 32 + (num - num3);
		return true;
	}

	private static void ResetCells()
	{
		foreach (Cell item in CellOrder)
		{
			Pool.Push(item);
		}
		Cells.Clear();
		CellOrder.Clear();
		_map = null;
		_analyse = null;
		_cell = null;
	}

	private static void Compose()
	{
		Dictionary<Guid, List<(int, int, int, int, byte[])>> dictionary = null;
		for (int i = 0; i < CellOrder.Count; i++)
		{
			Cell cell = CellOrder[i];
			if (dictionary == null)
			{
				dictionary = new Dictionary<Guid, List<(int, int, int, int, byte[])>>();
			}
			if (!dictionary.TryGetValue(cell.Map.Id, out var value))
			{
				value = (dictionary[cell.Map.Id] = Casse.MasquesDuSol(cell.Map.Id));
			}
			Meteo.Analyse analyse = cell.Analyse;
			ushort[] herbe = analyse.Herbe;
			int l = analyse.L;
			int h = analyse.H;
			int num = i % 16 * 32;
			int num2 = i / 16 * 32;
			for (int j = 0; j < 32; j++)
			{
				for (int k = 0; k < 32; k++)
				{
					int num3 = j * 32 + k;
					int num4 = ((num2 + j) * 512 + num + k) * 4;
					int num5 = cell.LocalX + k;
					int num6 = cell.LocalY + j;
					ushort num7 = (ushort)((num5 < l && num6 < h) ? herbe[num6 * l + num5] : 0);
					float x = Math.Clamp(cell.Ox[num3], -3f, 3f);
					float x2 = Math.Clamp(cell.Oy[num3], -3f, 3f);
					float num8 = Math.Clamp(cell.Light[num3], -0.4f, 0.5f);
					float num9 = cell.Burn[num3];
					float num10 = cell.Torn[num3];
					bool flag = cell.Sprout[num3] > 0f;
					if (num7 == 0 || (MathF.Abs(x) < 0.5f && MathF.Abs(x2) < 0.5f && MathF.Abs(num8) < 0.01f && num9 < 0.01f && num10 < 0.01f && !flag) || (value != null && Casse.SousUnSillon(value, num5, num6)))
					{
						Pixels[num4] = (Pixels[num4 + 1] = (Pixels[num4 + 2] = (Pixels[num4 + 3] = 0)));
						continue;
					}
					if (flag)
					{
						Pixels[num4] = 150;
						Pixels[num4 + 1] = 200;
						Pixels[num4 + 2] = 50;
						Pixels[num4 + 3] = byte.MaxValue;
						continue;
					}
					if (num10 > 0.5f)
					{
						int num11 = ((num5 * 7 + num6 * 13) % 5 + (num5 * 3 + num6) % 3) * 6;
						Pixels[num4] = (byte)(92 + num11);
						Pixels[num4 + 1] = (byte)(66 + num11 * 3 / 4);
						Pixels[num4 + 2] = (byte)(40 + num11 / 2);
						Pixels[num4 + 3] = byte.MaxValue;
						continue;
					}
					int num12 = Math.Clamp(num5 - (int)MathF.Round(x), 0, l - 1);
					int num13 = Math.Clamp(num6 - (int)MathF.Round(x2), 0, h - 1);
					ushort num14 = herbe[num13 * l + num12];
					if (num14 == 0)
					{
						num14 = num7;
					}
					int num15 = ((num14 >> 11) & 0x1F) * 255 / 31;
					int num16 = ((num14 >> 5) & 0x3F) * 255 / 63;
					int num17 = (num14 & 0x1F) * 255 / 31;
					float num18 = 1f + num8;
					float num19 = ((num8 > 0f) ? num8 : 0f);
					float num20 = (float)num15 * num18 + 18f * num19;
					float num21 = (float)num16 * num18 + 22f * num19;
					float num22 = (float)num17 * num18;
					if (num9 > 0.01f)
					{
						float num23 = (num20 + num21 + num22) / 3f;
						num20 = num20 * (1f - num9) + (num23 * 0.35f + 18f) * num9;
						num21 = num21 * (1f - num9) + (num23 * 0.3f + 10f) * num9;
						num22 = num22 * (1f - num9) + num23 * 0.25f * num9;
					}
					Pixels[num4] = (byte)Math.Clamp(num20, 0f, 255f);
					Pixels[num4 + 1] = (byte)Math.Clamp(num21, 0f, 255f);
					Pixels[num4 + 2] = (byte)Math.Clamp(num22, 0f, 255f);
					Pixels[num4 + 3] = byte.MaxValue;
				}
			}
		}
		int lignes = (CellOrder.Count + 16 - 1) / 16 * 32;
		if (_atlas == null)
		{
			if (Graphics.Renderer is MonoRenderer monoRenderer)
			{
				_atlas = monoRenderer.CreerTexturePixels("herbe-reagit", 512, 512, Pixels);
			}
		}
		else
		{
			MonoRenderer.RemplacerLignes(_atlas, Pixels, lignes);
		}
	}

	private static void Draw()
	{
		if (_atlas == null)
		{
			return;
		}
		FloatRect worldViewport = Graphics.WorldViewport;
		for (int i = 0; i < CellOrder.Count; i++)
		{
			Cell cell = CellOrder[i];
			int num = cell.Map.X + cell.LocalX;
			int num2 = cell.Map.Y + cell.LocalY;
			if (!((float)num > worldViewport.Right) && !((float)num2 > worldViewport.Bottom) && !((float)(num + 32) < worldViewport.Left) && !((float)(num2 + 32) < worldViewport.Top))
			{
				Graphics.DrawGameTexture(_atlas, new FloatRect(i % 16 * 32, i / 16 * 32, 32f, 32f), new FloatRect(num, num2, 32f, 32f), Color.White);
			}
		}
	}

	private static float Between(float min, float max)
	{
		return min + (float)Noise.NextDouble() * (max - min);
	}

	private static void AddBlade(float x, float y, float vx, float vy, long now)
	{
		if (Blades.Count < 160)
		{
			Blades.Add(new Blade
			{
				X = x,
				Y = y,
				Vx = vx,
				Vy = vy,
				Born = now,
				LifeMs = 360 + Noise.Next(220),
				Color = BladeColors[Noise.Next(BladeColors.Length)]
			});
		}
	}

	private static void DrawBlades(long now)
	{
		if (Blades.Count == 0)
		{
			return;
		}
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture == null)
		{
			return;
		}
		for (int num = Blades.Count - 1; num >= 0; num--)
		{
			Blade blade = Blades[num];
			long num2 = now - blade.Born;
			if (num2 >= blade.LifeMs)
			{
				Blades.RemoveAt(num);
			}
			else
			{
				float num3 = (float)num2 / (float)blade.LifeMs;
				float num4 = (float)num2 / 1000f;
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(blade.X + blade.Vx * num4), MathF.Round(blade.Y + blade.Vy * num4 + 75f * num4 * num4), 1f, 2f), new Color((int)(242f * (1f - num3 * num3)), blade.Color.R, blade.Color.G, blade.Color.B));
			}
		}
	}
}
