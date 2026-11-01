using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Intersect.Client.Core;
using Intersect.Client.Core.Sounds;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Maps;
using Intersect.Framework.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Intersect.Client.Entities;

public static class KiCharge
{
	private sealed record Palette(Color Edge, Color Outer, Color Mid, Color Core, Color Spark, Color Bolt, Color Ground);

	private sealed class Debris
	{
		public float X;

		public float GroundY;

		public float Height;

		public float Target;

		public float Rise;

		public float Phase;

		public float Vy;

		public float Spin;

		public int Shape;

		public long At;

		public int State;

		public long LandedAt;
	}

	private readonly record struct Speck(float X, float Y, float Vx, float Vy, long At, long Life, int Size);

	private sealed class Charge
	{
		public long StartedAt;

		public long StoppedAt = -1L;

		public float ReleaseFrom;

		public int Tier = 1;

		public long NextSurgeAt;

		public long SurgeAt = -100000L;

		public float SurgeForce;

		public long NextStreakAt;

		public long NextBoltAt;

		public int Seed;

		public MapSound? IntroSound;

		public MapSound? LoopSound;

		public readonly List<Debris> Debris = new List<Debris>();

		public readonly List<Speck> DustSpecks = new List<Speck>();

		public bool Stopping => StoppedAt >= 0;
	}

	private readonly record struct Streak(float X, float Y, float Vx, float Vy, long At, long Life, int Length, Color Head, Color Tail);

	private sealed class Bolt
	{
		public float X;

		public float Y;

		public float Angle;

		public float Length;

		public long Until;

		public long JitteredAt;

		public (float X, float Y)[] Points = Array.Empty<(float, float)>();

		public Color Glow;
	}

	private readonly record struct Flash(float X, float Y, long At, long Life, float Radius);

	private const long RampMs = 900L;

	private const long FrameMs = 55L;

	private const long Tier2Ms = 1000L;

	private const long MaxMinimumMs = 1700L;

	private const long MaxFallbackMs = 2900L;

	private const long SurgeCalmMs = 720L;

	private const long SurgeMaxMs = 540L;

	private const long SurgeShowMs = 150L;

	private const long SurgeShowMaxMs = 190L;

	private const long ReleaseMs = 320L;

	private const long IntroDurationMs = 1550L;

	private const int FrameWidth = 72;

	private const int FrameHeight = 160;

	private const int AnchorX = 36;

	private const int AnchorY = 146;

	private const int Frames = 12;

	private const int RowEdge = 0;

	private const int RowOuter = 1;

	private const int RowMid = 2;

	private const int RowCore = 3;

	private const int RowFrontOuter = 4;

	private const int RowFrontCore = 5;

	private const string SheetBirth = "ki_charge_naissance.png";

	private const string SheetCalm = "ki_charge_calme.png";

	private const string SheetMax = "ki_charge_max.png";

	private const string SheetSurge = "ki_charge_pulsation.png";

	private const float Drag = 4f;

	private static readonly Palette Base = new Palette(C(96, 128, 186), C(168, 196, 236), C(222, 236, 255), C(255, 255, 255), C(200, 228, 255), C(150, 200, 255), C(170, 205, 255));

	private static readonly Palette SuperSaiyan = new Palette(C(196, 116, 20), C(255, 196, 60), C(255, 232, 130), C(255, 252, 225), C(255, 236, 150), C(160, 215, 255), C(255, 205, 90));

	private static readonly Palette Kaioken = new Palette(C(150, 20, 24), C(236, 58, 50), C(255, 140, 110), C(255, 236, 226), C(255, 180, 160), C(255, 200, 180), C(255, 110, 90));

	private static readonly Palette Blue = new Palette(C(20, 96, 170), C(70, 180, 255), C(170, 232, 255), C(240, 252, 255), C(150, 225, 255), C(190, 240, 255), C(90, 190, 255));

	private static readonly Palette God = new Palette(C(160, 30, 70), C(250, 90, 140), C(255, 170, 200), C(255, 236, 244), C(255, 190, 215), C(255, 210, 230), C(255, 120, 170));

	private static readonly Color Dust = new Color(255, 232, 218, 184);

	private static readonly Color DebrisShadow = new Color(255, 20, 18, 14);

	private static readonly Dictionary<Guid, Charge> Charges = new Dictionary<Guid, Charge>();

	private static readonly List<Streak> Streaks = new List<Streak>();

	private static readonly List<Bolt> Bolts = new List<Bolt>();

	private static readonly List<Speck> Sparks = new List<Speck>();

	private static readonly List<Flash> Flashes = new List<Flash>();

	private static readonly Dictionary<(IGameTexture, int, int, int, int, int), IGameTexture> Silhouettes = new Dictionary<(IGameTexture, int, int, int, int, int), IGameTexture>();

	private static readonly Random Noise = new Random();

	private static long _lastUpdate;

	private static bool _enVol;

	private static uint[]? _crcTable;

	private static Color C(int r, int g, int b)
	{
		return new Color(255, r, g, b);
	}

	private static Color Tint(Color color, float alpha)
	{
		return new Color((int)Math.Clamp(alpha * 255f, 0f, 255f), color.R, color.G, color.B);
	}

	public static void Start(Entity entity)
	{
		if (!Charges.TryGetValue(entity.Id, out Charge value) || value.Stopping)
		{
			value?.IntroSound?.Stop();
			value?.LoopSound?.Stop();
			long milliseconds = Timing.Global.Milliseconds;
			(float X, float Y) tuple = Feet(entity);
			float item = tuple.X;
			float item2 = tuple.Y;
			Charge charge = new Charge
			{
				StartedAt = milliseconds,
				NextStreakAt = milliseconds,
				NextSurgeAt = milliseconds + 1000,
				Seed = (entity.Id.GetHashCode() & 0x7FFF),
				IntroSound = Audio.AddMapSound("Aura_intro.wav", entity.X, entity.Y, entity.MapId, loop: false, 0, 14, entity)
			};
			Charges[entity.Id] = charge;
			Meteo.ChargeDeKi(entity, commence: true);
			Flashes.Add(new Flash(item, item2 - 30f, milliseconds, 200L, 20f));
			BlowDust(charge, item, item2, milliseconds, 34, 40f, 110f, 0.3f);
			if (entity == Globals.Me)
			{
				CombatFeedback.StartShake(2f, 160L);
			}
		}
	}

	public static void Stop(Entity entity)
	{
		if (!Charges.TryGetValue(entity.Id, out Charge value) || value.Stopping)
		{
			return;
		}
		value.IntroSound?.Stop();
		value.LoopSound?.Stop();
		long milliseconds = Timing.Global.Milliseconds;
		value.ReleaseFrom = Intensity(value, milliseconds);
		value.StoppedAt = milliseconds;
		Meteo.ChargeDeKi(entity, commence: false);
		foreach (Debris item3 in value.Debris.Where((Debris debris) => debris.State == 0))
		{
			item3.State = 1;
			item3.Vy = 0f;
		}
		(float X, float Y) tuple = Feet(entity);
		float item = tuple.X;
		float item2 = tuple.Y;
		for (int num = 0; num < 14; num++)
		{
			float x = -(float)Math.PI / 2f + ((float)Noise.NextDouble() - 0.5f) * 2.2f;
			float num2 = 40f + (float)Noise.NextDouble() * 60f;
			Sparks.Add(new Speck(item + ((float)Noise.NextDouble() - 0.5f) * 16f, item2 - 20f - (float)Noise.NextDouble() * 20f, MathF.Cos(x) * num2, MathF.Sin(x) * num2, milliseconds, 300 + Noise.Next(250), 1));
		}
		BlowDust(value, item, item2, milliseconds, 16, 18f, 44f, 0.3f);
	}

	public static void Clear()
	{
		foreach (Charge value in Charges.Values)
		{
			value.IntroSound?.Stop();
			value.LoopSound?.Stop();
		}
		Charges.Clear();
		Meteo.ChargesEffacees();
		Streaks.Clear();
		Bolts.Clear();
		Sparks.Clear();
		Flashes.Clear();
	}

	private static float Intensity(Charge charge, long now)
	{
		if (!charge.Stopping)
		{
			return Math.Clamp((float)(now - charge.StartedAt) / 900f, 0f, 1f);
		}
		return charge.ReleaseFrom * Math.Clamp(1f - (float)(now - charge.StoppedAt) / 320f, 0f, 1f);
	}

	private static float SurgeNow(Charge charge, long now)
	{
		long num = now - charge.SurgeAt;
		if (num < 0 || num >= 260)
		{
			return 0f;
		}
		return charge.SurgeForce * MathF.Exp((float)(-num) / 90f);
	}

	private static (float X, float Y) Feet(Entity entity)
	{
		FloatRect worldPos = entity.WorldPos;
		return (X: worldPos.X + worldPos.Width / 2f, Y: worldPos.Y + worldPos.Height - 2f);
	}

	private static Palette PaletteOf(Entity entity)
	{
		string transformedSprite = entity.TransformedSprite;
		if (string.IsNullOrEmpty(transformedSprite))
		{
			return Base;
		}
		string text = transformedSprite.ToLowerInvariant();
		if (text.Contains("blue") || text.Contains("ssb") || text.Contains("ssgss"))
		{
			return Blue;
		}
		if (text.Contains("god") || text.Contains("ssg") || text.Contains("rose"))
		{
			return God;
		}
		if (text.Contains("kaioken"))
		{
			return Kaioken;
		}
		return SuperSaiyan;
	}

	private static bool KiFull(Entity entity)
	{
		long num = entity.MaxVital[1];
		if (num > 0)
		{
			return entity.Vital[1] >= num;
		}
		return false;
	}

	private static bool KiKnown(Entity entity)
	{
		return entity.MaxVital[1] > 0;
	}

	private static (string Sheet, int Frame, float Height, float HalfWidth) Look(Charge charge, long now, float intensity)
	{
		if (charge.Stopping || now - charge.StartedAt < 900)
		{
			int num = (charge.Stopping ? Math.Clamp((int)MathF.Ceiling(intensity * 12f) - 1, 0, 11) : Math.Clamp((int)((now - charge.StartedAt) * 12 / 900), 0, 11));
			float num2 = (float)(num + 1) / 12f;
			return (Sheet: "ki_charge_naissance.png", Frame: num, Height: 76f * (0.35f + 0.65f * num2), HalfWidth: 13.5f * (0.6f + 0.4f * num2));
		}
		int item = (int)((now / 55 + charge.Seed) % 12);
		long num3 = ((charge.Tier >= 3) ? 190 : 150);
		if (now - charge.SurgeAt < num3)
		{
			return (Sheet: "ki_charge_pulsation.png", Frame: item, Height: 110.2f, HalfWidth: 16.632f);
		}
		if (charge.Tier < 3)
		{
			return (Sheet: "ki_charge_calme.png", Frame: item, Height: 76f, HalfWidth: 13.5f);
		}
		return (Sheet: "ki_charge_max.png", Frame: item, Height: 88.159996f, HalfWidth: 14.85f);
	}

	public static void DrawAura(Entity entity, FloatRect bounds, IGameTexture? texture = null, FloatRect source = default(FloatRect))
	{
		if (!Charges.TryGetValue(entity.Id, out Charge value))
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		float num = Intensity(value, milliseconds);
		float num2 = MathF.Round(bounds.X + bounds.Width / 2f);
		float num3 = MathF.Round(bounds.Y + bounds.Height - 2f);
		Palette palette = PaletteOf(entity);
		float num4 = SurgeNow(value, milliseconds);
		if (num > 0f)
		{
			(string Sheet, int Frame, float Height, float HalfWidth) tuple = Look(value, milliseconds, num);
			string item = tuple.Sheet;
			int item2 = tuple.Frame;
			float item3 = tuple.Height;
			float item4 = tuple.HalfWidth;
			IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Misc, "ki_halo.png");
			if (texture2 != null)
			{
				DrawCentered(texture2, num2, num3 - item3 * 0.4f, item4 * 4.2f, item3 * 1.24f, Tint(palette.Outer, 0.2f * num * (1f + 0.8f * num4)), GameBlendModes.Add);
				DrawCentered(texture2, num2, num3 - item3 * 0.3f, item4 * 2.2f, item3 * 0.84f, Tint(palette.Core, 0.16f * num * (1f + 0.8f * num4)), GameBlendModes.Add);
			}
			IGameTexture texture3 = Globals.ContentManager.GetTexture(TextureType.Misc, "ki_charge_sol.png");
			if (texture3 != null)
			{
				float num5 = 1f + 0.2f * num4;
				DrawCentered(texture3, num2, num3 + 1f, (float)texture3.Width * num5, (float)texture3.Height * num5, Tint(palette.Ground, 0.32f * num * (1f + 0.5f * num4)), GameBlendModes.Add);
			}
		}
		DrawDust(value, milliseconds);
		DrawDebris(value, num3, front: false);
		if (num <= 0f)
		{
			return;
		}
		IGameTexture texture4 = Globals.ContentManager.GetTexture(TextureType.Misc, Look(value, milliseconds, num).Sheet);
		if (texture4 == null)
		{
			return;
		}
		(string, int, float, float) tuple2 = Look(value, milliseconds, num);
		float num6 = Math.Min(1f, num * 1.4f);
		DrawLayer(texture4, tuple2.Item2, 0, num2, num3, Tint(palette.Edge, 0.45f * num6), GameBlendModes.None);
		DrawLayer(texture4, tuple2.Item2, 1, num2, num3, Tint(palette.Outer, 0.55f * num6), GameBlendModes.None);
		DrawLayer(texture4, tuple2.Item2, 2, num2, num3, Tint(palette.Mid, 0.62f * num6), GameBlendModes.None);
		DrawLayer(texture4, tuple2.Item2, 3, num2, num3, Tint(palette.Core, 0.5f * num6), GameBlendModes.Add);
		if (texture != null && source.Width > 0f)
		{
			IGameTexture gameTexture = MaskOf(texture, source, 1);
			if (gameTexture != null)
			{
				float alpha = (0.5f + 0.2f * MathF.Sin((float)milliseconds / 70f) + 0.3f * num4) * num;
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, gameTexture.Width, gameTexture.Height), new FloatRect(bounds.X - 1f, bounds.Y - 1f, bounds.Width + 2f, bounds.Height + 2f), Tint(palette.Core, alpha), null, GameBlendModes.Add);
			}
		}
	}

	public static void DrawGlow(Entity entity, IGameTexture texture, FloatRect source, FloatRect destination)
	{
		if (!Charges.TryGetValue(entity.Id, out Charge value))
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		float num = Intensity(value, milliseconds);
		Palette palette = PaletteOf(entity);
		float num2 = SurgeNow(value, milliseconds);
		float fx = MathF.Round(destination.X + destination.Width / 2f);
		float num3 = MathF.Round(destination.Y + destination.Height - 2f);
		if (num > 0f)
		{
			float alpha = (0.08f + 0.05f * MathF.Sin((float)milliseconds / 95f) + 0.2f * num2) * num;
			Graphics.DrawGameTexture(texture, source, destination, Tint(palette.Mid, alpha), null, GameBlendModes.Add);
			(string, int, float, float) tuple = Look(value, milliseconds, num);
			IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Misc, tuple.Item1);
			if (texture2 != null)
			{
				DrawLayer(texture2, tuple.Item2, 4, fx, num3, Tint(palette.Outer, 0.45f * num), GameBlendModes.None);
				DrawLayer(texture2, tuple.Item2, 5, fx, num3, Tint(palette.Core, 0.45f * num), GameBlendModes.Add);
			}
		}
		DrawDebris(value, num3, front: true);
	}

	private static void DrawLayer(IGameTexture sheet, int frame, int row, float fx, float fy, Color color, GameBlendModes blend)
	{
		if (color.A > 0)
		{
			Graphics.DrawGameTexture(sheet, new FloatRect(frame * 72, row * 160, 72f, 160f), new FloatRect(fx - 36f, fy - 146f, 72f, 160f), color, null, blend);
		}
	}

	private static void DrawCentered(IGameTexture texture, float x, float y, float width, float height, Color color, GameBlendModes blend)
	{
		if (color.A > 0)
		{
			Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), new FloatRect(x - width / 2f, y - height / 2f, width, height), color, null, blend);
		}
	}

	private static void DrawDust(Charge charge, long now)
	{
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture == null)
		{
			return;
		}
		foreach (Speck dustSpeck in charge.DustSpecks)
		{
			long num = now - dustSpeck.At;
			if (num >= 0 && num < dustSpeck.Life)
			{
				float num2 = (float)num / (float)dustSpeck.Life;
				float num3 = (1f - MathF.Exp(-4f * (float)num / 1000f)) / 4f;
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(dustSpeck.X + dustSpeck.Vx * num3), MathF.Round(dustSpeck.Y + dustSpeck.Vy * num3 - 3f * num2), dustSpeck.Size + 1, dustSpeck.Size), Tint(Dust, 0.85f * (1f - num2 * num2)));
			}
		}
	}

	private static void DrawDebris(Charge charge, float feetY, bool front)
	{
		if (charge.Debris.Count == 0)
		{
			return;
		}
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "ki_charge_debris.png");
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		long milliseconds = Timing.Global.Milliseconds;
		foreach (Debris debri in charge.Debris)
		{
			if (milliseconds >= debri.At && debri.GroundY >= feetY == front)
			{
				float num = ((debri.State == 2) ? Math.Clamp(1f - (float)(milliseconds - debri.LandedAt - 600) / 300f, 0f, 1f) : 1f);
				if (gameTexture != null && debri.Height > 0f)
				{
					float alpha = Math.Max(0f, 1f - debri.Height / 28f) * 0.45f * num;
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(debri.X - 1f), MathF.Round(debri.GroundY + 1f), 3f, 1f), Tint(DebrisShadow, alpha));
				}
				if (texture != null)
				{
					int num2 = debri.Shape * 2 + ((int)debri.Spin & 1);
					Graphics.DrawGameTexture(texture, new FloatRect(num2 * 6, 0f, 6f, 6f), new FloatRect(MathF.Round(debri.X - 1f), MathF.Round(debri.GroundY - debri.Height - 1f), 6f, 6f), new Color((int)(255f * num), 255, 255, 255));
				}
			}
		}
	}

	public static void Draw()
	{
		long now = Timing.Global.Milliseconds;
		float dt = ((_lastUpdate == 0L) ? 0f : Math.Clamp((float)(now - _lastUpdate) / 1000f, 0f, 0.1f));
		_lastUpdate = now;
		_enVol = false;
		List<Guid> list = null;
		foreach (KeyValuePair<Guid, Charge> charge2 in Charges)
		{
			charge2.Deconstruct(out var key, out var value);
			Guid guid = key;
			Charge charge = value;
			Entity entity = FindEntity(guid);
			if (entity == null)
			{
				charge.IntroSound?.Stop();
				charge.LoopSound?.Stop();
				(list ?? (list = new List<Guid>())).Add(guid);
				continue;
			}
			_enVol |= Flight.IsAirborne(entity);
			Update(entity, charge, now, dt);
			if (charge.Stopping && now - charge.StoppedAt >= 320 && charge.Debris.All((Debris debris) => debris.State == 2 && now - debris.LandedAt >= 900) && charge.DustSpecks.All((Speck speck) => now - speck.At >= speck.Life))
			{
				(list ?? (list = new List<Guid>())).Add(guid);
			}
		}
		if (list != null)
		{
			foreach (Guid item in list)
			{
				Charges.Remove(item);
			}
		}
		if (!_enVol)
		{
			DrawStreaks(now);
			DrawBolts(now);
			DrawSparks(now);
		}
	}

	public static void DrawSky()
	{
		long milliseconds = Timing.Global.Milliseconds;
		if (_enVol)
		{
			DrawStreaks(milliseconds);
			DrawBolts(milliseconds);
			DrawSparks(milliseconds);
		}
		DrawFlashes(milliseconds);
	}

	private static void Update(Entity entity, Charge charge, long now, float dt)
	{
		(float X, float Y) tuple = Feet(entity);
		float item = tuple.X;
		float item2 = tuple.Y;
		Palette palette = PaletteOf(entity);
		if (!charge.Stopping)
		{
			charge.IntroSound?.UpdatePosition(entity.X, entity.Y, entity.MapId);
			charge.LoopSound?.UpdatePosition(entity.X, entity.Y, entity.MapId);
			if (charge.LoopSound == null && now - charge.StartedAt >= 1550)
			{
				charge.LoopSound = Audio.AddMapSound("Aura_boucle.wav", entity.X, entity.Y, entity.MapId, loop: true, 0, 14, entity);
			}
			long num = now - charge.StartedAt;
			if (charge.Tier == 1 && num >= 1000)
			{
				charge.Tier = 2;
				LiftDebris(charge, item, item2, now);
				Surge(charge, item, item2, now, 1f);
				charge.NextSurgeAt = now + 720;
			}
			if (charge.Tier == 2 && num >= 1700 && (KiFull(entity) || (!KiKnown(entity) && num >= 2900)))
			{
				charge.Tier = 3;
				Flashes.Add(new Flash(item, item2 - 30f, now, 220L, 30f));
				Surge(charge, item, item2, now, 1.6f);
				for (int i = 0; i < 3; i++)
				{
					Bolts.Add(NewBolt(item, item2, now, toGround: true, palette.Bolt));
				}
				if (entity == Globals.Me)
				{
					CombatFeedback.StartShake(2f, 160L);
				}
			}
			if (charge.Tier >= 2 && now >= charge.NextSurgeAt)
			{
				bool flag = charge.Tier >= 3;
				charge.NextSurgeAt = now + (flag ? 540 : 720);
				Surge(charge, item, item2, now, flag ? 1.4f : 1f);
				if (flag && entity == Globals.Me)
				{
					CombatFeedback.StartShake(1f, 90L);
				}
			}
			float num2 = Intensity(charge, now);
			if (now >= charge.NextStreakAt)
			{
				bool flag2 = charge.Tier >= 3;
				charge.NextStreakAt = now + (flag2 ? 26 : 42);
				(Color Main, Color Light) tuple2 = KarmaAura.SparkColors(entity, palette.Spark, palette.Core);
				Color item3 = tuple2.Main;
				Color item4 = tuple2.Light;
				float num3 = 13.5f * (0.6f + 0.4f * num2);
				for (int j = 0; j < 1 + (int)(num2 * 1.5f); j++)
				{
					float num4 = item + ((float)Noise.NextDouble() + (float)Noise.NextDouble() - 1f) * num3 * 1.3f;
					Streaks.Add(new Streak(num4, item2 - (float)Noise.NextDouble() * 16f, (0f - (num4 - item)) * 0.9f, (0f - (70f + (float)Noise.NextDouble() * 90f)) * (flag2 ? 1.3f : 1f), now, 380 + Noise.Next(260), 3 + Noise.Next(5), item4, item3));
				}
				if (Streaks.Count > 500)
				{
					Streaks.RemoveRange(0, Streaks.Count - 500);
				}
			}
			if (charge.Tier >= 3 && now >= charge.NextBoltAt)
			{
				charge.NextBoltAt = now + 110 + Noise.Next(230);
				Bolts.Add(NewBolt(item, item2, now, Noise.NextDouble() < 0.22, palette.Bolt));
			}
		}
		foreach (Debris debri in charge.Debris)
		{
			if (now < debri.At)
			{
				continue;
			}
			switch (debri.State)
			{
			case 0:
				debri.Height += (debri.Target - debri.Height) * Math.Min(1f, dt * debri.Rise * 2.2f);
				debri.Height += MathF.Sin((float)now / 380f + debri.Phase) * 0.05f;
				debri.Spin += dt * (1.5f + debri.Rise);
				break;
			case 1:
				debri.Vy += 260f * dt;
				debri.Height -= debri.Vy * dt;
				debri.Spin += dt * 4f;
				if (debri.Height <= 0f)
				{
					if (debri.Vy > 45f)
					{
						debri.Vy = (0f - debri.Vy) * 0.35f;
						debri.Height = 0.01f;
					}
					else
					{
						debri.Height = 0f;
						debri.State = 2;
						debri.LandedAt = now;
					}
				}
				break;
			}
		}
		charge.DustSpecks.RemoveAll((Speck speck) => now - speck.At >= speck.Life);
	}

	private static void Surge(Charge charge, float fx, float fy, long now, float force)
	{
		charge.SurgeForce = force;
		charge.SurgeAt = now;
		BlowDust(charge, fx, fy, now, 10 + (int)(8f * force), 22f, 22f + 40f * force, 0.35f);
	}

	private static void BlowDust(Charge charge, float fx, float fy, long now, int count, float minSpeed, float maxSpeed, float flatten)
	{
		for (int i = 0; i < count; i++)
		{
			float x = (float)(Noise.NextDouble() * Math.PI * 2.0);
			float num = minSpeed + (float)Noise.NextDouble() * (maxSpeed - minSpeed);
			charge.DustSpecks.Add(new Speck(fx + MathF.Cos(x) * 5f, fy + 1f + MathF.Sin(x) * 2f, MathF.Cos(x) * num, MathF.Sin(x) * num * flatten, now, 380 + Noise.Next(400), (!(Noise.NextDouble() < 0.45)) ? 1 : 2));
		}
		if (charge.DustSpecks.Count > 200)
		{
			charge.DustSpecks.RemoveRange(0, charge.DustSpecks.Count - 200);
		}
	}

	private static void LiftDebris(Charge charge, float fx, float fy, long now)
	{
		charge.Debris.Clear();
		for (int i = 0; i < 12; i++)
		{
			float x = (float)i / 12f * (float)Math.PI * 2f + (float)Noise.NextDouble() * 0.5f;
			float num = 12f + (float)Noise.NextDouble() * 20f;
			charge.Debris.Add(new Debris
			{
				X = MathF.Round(fx + MathF.Cos(x) * num),
				GroundY = MathF.Round(fy + MathF.Sin(x) * num * 0.32f),
				Target = 5f + (float)Noise.NextDouble() * 18f,
				Rise = 0.6f + (float)Noise.NextDouble() * 0.8f,
				Phase = (float)Noise.NextDouble() * 6f,
				Shape = Noise.Next(5),
				At = now + i * 70
			});
		}
	}

	private static Entity? FindEntity(Guid id)
	{
		if (Globals.Me != null && Globals.Me.Id == id)
		{
			return Globals.Me;
		}
		if (Globals.Entities == null || !Globals.Entities.TryGetValue(id, out Entity value))
		{
			return null;
		}
		return value;
	}

	private static Bolt NewBolt(float fx, float fy, long now, bool toGround, Color glow)
	{
		float x = (float)(Noise.NextDouble() * Math.PI * 2.0);
		Bolt bolt = new Bolt
		{
			X = fx + MathF.Cos(x) * (8f + (float)Noise.NextDouble() * 8f),
			Y = fy - 12f - (float)Noise.NextDouble() * 38f,
			Angle = (toGround ? ((float)Math.PI / 2f + ((float)Noise.NextDouble() - 0.5f) * 0.6f) : ((float)(Noise.NextDouble() * Math.PI * 2.0))),
			Length = (toGround ? (22f + (float)Noise.NextDouble() * 10f) : (12f + (float)Noise.NextDouble() * 14f)),
			Until = now + 70 + Noise.Next(70),
			JitteredAt = -1L,
			Glow = glow
		};
		if (toGround)
		{
			bolt.X = fx + ((float)Noise.NextDouble() - 0.5f) * 18f;
			bolt.Y = fy - 26f;
		}
		return bolt;
	}

	private static void Jitter(Bolt bolt)
	{
		int num = 5 + Noise.Next(3);
		(float, float)[] array = new(float, float)[num + 1];
		float num2 = MathF.Cos(bolt.Angle);
		float num3 = MathF.Sin(bolt.Angle);
		for (int i = 0; i <= num; i++)
		{
			float num4 = bolt.Length * (float)i / (float)num;
			float num5 = ((i == 0 || i == num) ? 0f : (((float)Noise.NextDouble() - 0.5f) * 5f));
			array[i] = (bolt.X + num2 * num4 - num3 * num5, bolt.Y + num3 * num4 + num2 * num5);
		}
		bolt.Points = array;
	}

	private static void DrawBolts(long now)
	{
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		Bolts.RemoveAll((Bolt bolt) => now >= bolt.Until);
		if (gameTexture == null)
		{
			return;
		}
		foreach (Bolt bolt in Bolts)
		{
			if (bolt.JitteredAt < 0 || now - bolt.JitteredAt > 34)
			{
				bolt.JitteredAt = now;
				Jitter(bolt);
			}
			for (int num = 0; num < bolt.Points.Length - 1; num++)
			{
				var (num2, fromY) = bolt.Points[num];
				var (num3, toY) = bolt.Points[num + 1];
				Line(gameTexture, num2 + 1f, fromY, num3 + 1f, toY, Tint(bolt.Glow, 0.45f));
				Line(gameTexture, num2 - 1f, fromY, num3 - 1f, toY, Tint(bolt.Glow, 0.45f));
				Line(gameTexture, num2, fromY, num3, toY, new Color(255, 255, 255, 255));
			}
		}
	}

	private static void Line(IGameTexture pixel, float fromX, float fromY, float toX, float toY, Color color)
	{
		int num = (int)MathF.Round(fromX);
		int num2 = (int)MathF.Round(fromY);
		int num3 = (int)MathF.Round(toX);
		int num4 = (int)MathF.Round(toY);
		int num5 = Math.Abs(num3 - num);
		int num6 = -Math.Abs(num4 - num2);
		int num7 = ((num < num3) ? 1 : (-1));
		int num8 = ((num2 < num4) ? 1 : (-1));
		int num9 = num5 + num6;
		for (int i = 0; i < 64; i++)
		{
			Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num, num2, 1f, 1f), color, null, GameBlendModes.Add);
			if (num != num3 || num2 != num4)
			{
				int num10 = 2 * num9;
				if (num10 >= num6)
				{
					num9 += num6;
					num += num7;
				}
				if (num10 <= num5)
				{
					num9 += num5;
					num2 += num8;
				}
				continue;
			}
			break;
		}
	}

	private static void DrawStreaks(long now)
	{
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		Streaks.RemoveAll((Streak streak) => now - streak.At >= streak.Life);
		if (gameTexture == null)
		{
			return;
		}
		foreach (Streak streak in Streaks)
		{
			float num = (float)(now - streak.At) / 1000f;
			float num2 = (float)(now - streak.At) / (float)streak.Life;
			float x = MathF.Round(streak.X + streak.Vx * num);
			float num3 = MathF.Round(streak.Y + streak.Vy * num);
			int num4 = streak.Length + 2;
			for (int num5 = 0; num5 < num4; num5++)
			{
				float alpha = (1f - num2) * (1f - (float)num5 / (float)num4);
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(x, num3 + (float)num5, 1f, 1f), Tint((num5 < 2) ? streak.Head : streak.Tail, alpha), null, GameBlendModes.Add);
			}
		}
	}

	private static void DrawSparks(long now)
	{
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		Sparks.RemoveAll((Speck spark) => now - spark.At >= spark.Life);
		if (gameTexture == null)
		{
			return;
		}
		foreach (Speck spark in Sparks)
		{
			float num = (float)(now - spark.At) / 1000f;
			float num2 = (float)(now - spark.At) / (float)spark.Life;
			Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(spark.X + spark.Vx * num), MathF.Round(spark.Y + spark.Vy * num + 60f * num * num), 1f, 1f), Tint(Base.Spark, 1f - num2), null, GameBlendModes.Add);
		}
	}

	private static void DrawFlashes(long now)
	{
		Flashes.RemoveAll((Flash flash) => now - flash.At >= flash.Life);
		if (Flashes.Count == 0)
		{
			return;
		}
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "ki_halo.png");
		if (texture == null)
		{
			return;
		}
		foreach (Flash flash in Flashes)
		{
			float num = (float)(now - flash.At) / (float)flash.Life;
			DrawCentered(texture, flash.X, flash.Y, flash.Radius * 1.8f, flash.Radius * 6.4f * (0.7f + 0.5f * num), Tint(Base.Core, 0.8f * (1f - num)), GameBlendModes.Add);
			DrawCentered(texture, flash.X, flash.Y + 10f, flash.Radius * 2.4f * (0.6f + 0.6f * num), flash.Radius * 2.6f * (0.6f + 0.6f * num), Tint(Base.Core, 0.55f * (1f - num)), GameBlendModes.Add);
		}
	}

	internal static IGameTexture? MaskOf(IGameTexture texture, FloatRect source, int grow)
	{
		(IGameTexture, int, int, int, int, int) key = (texture, (int)source.X, (int)source.Y, (int)source.Width, (int)source.Height, grow);
		if (Silhouettes.TryGetValue(key, out IGameTexture value))
		{
			return value;
		}
		try
		{
			Texture2D texture2 = texture.GetTexture<Texture2D>();
			if (texture2 != null)
			{
				AtlasReference atlasReference = texture.AtlasReference;
				if ((object)atlasReference == null || !atlasReference.IsRotated)
				{
					int item = key.Item4;
					int item2 = key.Item5;
					int num = key.Item2;
					int num2 = key.Item3;
					AtlasReference atlasReference2 = texture.AtlasReference;
					if ((object)atlasReference2 != null)
					{
						num += atlasReference2.Bounds.X;
						num2 += atlasReference2.Bounds.Y;
					}
					Microsoft.Xna.Framework.Color[] array = new Microsoft.Xna.Framework.Color[item * item2];
					texture2.GetData(0, new Microsoft.Xna.Framework.Rectangle(num, num2, item, item2), array, 0, array.Length);
					int num3 = item + 2 * grow;
					int num4 = item2 + 2 * grow;
					byte[] array2 = new byte[num3 * num4 * 4];
					for (int i = 0; i < item2; i++)
					{
						for (int j = 0; j < item; j++)
						{
							if (array[i * item + j].A < 128)
							{
								continue;
							}
							for (int k = 0; k <= 2 * grow; k++)
							{
								for (int l = 0; l <= 2 * grow; l++)
								{
									int num5 = ((i + k) * num3 + j + l) * 4;
									int num6 = num5 + 1;
									int num7 = num5 + 2;
									byte b;
									array2[num5 + 3] = (b = byte.MaxValue);
									array2[num5] = (array2[num6] = (array2[num7] = b));
								}
							}
						}
					}
					byte[] png = Png(array2, num3, num4);
					if (Silhouettes.Count > 192)
					{
						Silhouettes.Clear();
					}
					IGameTexture gameTexture = Graphics.Renderer.CreateTextureFromStreamFactory($"ki_silhouette/{texture.Name}_{key.Item2}_{key.Item3}_{item}x{item2}_{grow}", () => new MemoryStream(png));
					Silhouettes[key] = gameTexture;
					return gameTexture;
				}
			}
			return null;
		}
		catch
		{
			return null;
		}
	}

	private static byte[] Png(byte[] rgba, int width, int height)
	{
		using MemoryStream memoryStream = new MemoryStream();
		memoryStream.Write(new byte[8] { 137, 80, 78, 71, 13, 10, 26, 10 });
		byte[] array = new byte[13];
		WriteInt(array, 0, width);
		WriteInt(array, 4, height);
		array[8] = 8;
		array[9] = 6;
		Chunk(memoryStream, "IHDR", array);
		byte[] array2 = new byte[(width * 4 + 1) * height];
		for (int i = 0; i < height; i++)
		{
			Buffer.BlockCopy(rgba, i * width * 4, array2, i * (width * 4 + 1) + 1, width * 4);
		}
		using (MemoryStream memoryStream2 = new MemoryStream())
		{
			using (ZLibStream zLibStream = new ZLibStream(memoryStream2, CompressionLevel.Fastest, leaveOpen: true))
			{
				zLibStream.Write(array2, 0, array2.Length);
			}
			Chunk(memoryStream, "IDAT", memoryStream2.ToArray());
		}
		Chunk(memoryStream, "IEND", Array.Empty<byte>());
		return memoryStream.ToArray();
	}

	private static void WriteInt(byte[] target, int position, int value)
	{
		target[position] = (byte)(value >> 24);
		target[position + 1] = (byte)(value >> 16);
		target[position + 2] = (byte)(value >> 8);
		target[position + 3] = (byte)value;
	}

	private static void Chunk(Stream output, string type, byte[] data)
	{
		byte[] array = new byte[4];
		WriteInt(array, 0, data.Length);
		output.Write(array);
		byte[] bytes = Encoding.ASCII.GetBytes(type);
		output.Write(bytes);
		output.Write(data);
		uint value = Crc(bytes, data);
		byte[] array2 = new byte[4];
		WriteInt(array2, 0, (int)value);
		output.Write(array2);
	}

	private static uint Crc(byte[] type, byte[] data)
	{
		if (_crcTable == null)
		{
			_crcTable = new uint[256];
			for (uint num = 0u; num < 256; num++)
			{
				uint num2 = num;
				for (int i = 0; i < 8; i++)
				{
					num2 = (((num2 & 1) != 0) ? (0xEDB88320u ^ (num2 >> 1)) : (num2 >> 1));
				}
				_crcTable[num] = num2;
			}
		}
		uint num3 = uint.MaxValue;
		foreach (byte b in type)
		{
			num3 = _crcTable[(num3 ^ b) & 0xFF] ^ (num3 >> 8);
		}
		foreach (byte b2 in data)
		{
			num3 = _crcTable[(num3 ^ b2) & 0xFF] ^ (num3 >> 8);
		}
		return num3 ^ 0xFFFFFFFFu;
	}
}
