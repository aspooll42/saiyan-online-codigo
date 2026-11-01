using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Core.Sounds;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Maps;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.GameObjects;
using Intersect.Models;

namespace Intersect.Client.Entities;

public static class Transformation
{
	private enum Kind
	{
		SuperSaiyan,
		Kaioken,
		UltraInstinct
	}

	private sealed record Palette(Color Edge, Color Outer, Color Mid, Color Core, Color Spark, Color Bolt, Color Ground);

	private sealed class Rock
	{
		public float X;

		public float Ground;

		public float Height;

		public float Target;

		public float Vx;

		public float Vy;

		public float Spin;

		public int Shape;

		public int State;

		public long At;

		public long LandedAt;

		public float Angle;

		public float Radius;

		public float Orbit;

		public float BaseOrbit;

		public float Alpha = 1f;
	}

	private readonly record struct Speck(float X, float Y, float Vx, float Vy, long At, long Life, int Size);

	private sealed class Show
	{
		public Kind Kind;

		public Palette Palette = Gold;

		public bool Ultra;

		public string Name = string.Empty;

		public long StartedAt;

		public long BurstAt;

		public long EndedAt;

		public bool Burst;

		public bool Opened;

		public bool EndShown;

		public bool RocksLifted;

		public bool MoreCracks;

		public bool Established;

		public bool Anchored;

		public Guid MapId;

		public float AnchorX;

		public float AnchorY;

		public float EndHeight;

		public float EndAlpha;

		public long FrozenUntil;

		public long PulseAt = -4611686018427387904L;

		public long NextPulseAt;

		public long NextSkyBoltAt;

		public long NextSparkAt;

		public long NextStreakAt;

		public long NextDustAt;

		public long NextShakeAt;

		public long NextBurstAt;

		public long NextHeatAt;

		public int Beats;

		public readonly List<Rock> Rocks = new List<Rock>();

		public readonly List<Speck> Dust = new List<Speck>();

		public readonly List<MapSound> Sounds = new List<MapSound>();

		public bool Short;

		public bool Mine;

		public bool Emptied;

		public bool Blown;

		public int UltraBeats;

		public long BeatAt = -4611686018427387904L;

		public long NextMoteAt;

		public long NextGlintAt;

		public float Fx;

		public float Fy;

		public IGameTexture? LastTexture;

		public FloatRect LastSource;

		public FloatRect LastDestination;
	}

	private readonly record struct Streak(float X, float Y, float Vx, float Vy, long At, long Life, int Length, Color Head, Color Tail);

	private readonly record struct Spark(float X, float Y, float Vx, float Vy, long At, long Life, Color Color, float Gravity);

	private sealed class Bolt
	{
		public float X;

		public float Y;

		public float Angle;

		public float Length;

		public long Until;

		public long JitteredAt = -1L;

		public (float X, float Y)[] Points = Array.Empty<(float, float)>();

		public Color Glow;
	}

	private sealed record SkyBolt((float X, float Y)[] Points, (float X, float Y)[] Branch, long At);

	private readonly record struct Glow(float X, float Y, long At, long Life);

	private readonly record struct Gust(float X, float Y, float Angle, float Radius, float Speed, float Length, long At, long Life, Color Color);

	private readonly record struct Heat(float X, float Y, long At, long Life, float Phase, float Height);

	private readonly record struct Smoke(float X, float Y, float Speed, long At, long Life, float Phase);

	private sealed record Mark(Guid MapId, float X, float Y, (float X, float Y)[]? Crack, int Stain, long At);

	private readonly record struct ScreenFlash(long At, long Hold, long Fade, Color Color, float Alpha);

	private sealed record UltraRing(Guid MapId, float X, float Y, long At, long Life, float Speed, Color Color, bool Shock);

	private sealed class Mote
	{
		public Show Owner;

		public Guid MapId;

		public float X;

		public float Y;

		public float Rise;

		public float Phase;

		public long At;

		public long Life;

		public long SuckAt = -1L;

		public Color Color;
	}

	private readonly record struct Glint(float X, float Y, long At, long Life);

	private const long AuraHoldMs = 5000L;

	private const long AuraSettleMs = 700L;

	private const long SuperSaiyanCollapseMs = 320L;

	private const long KaiokenCollapseMs = 260L;

	private const long MarksLifeMs = 6000L;

	private const long MarksFadeMs = 1500L;

	private const long CracksGrowMs = 700L;

	private const float NearbyTiles = 12f;

	private const float NearbyShake = 0.4f;

	private static readonly (int Height, int Width, int FrameHeight, int FeetY)[] Bands = new(int, int, int, int)[13]
	{
		(40, 52, 61, 53),
		(45, 54, 67, 59),
		(50, 58, 73, 65),
		(56, 60, 81, 73),
		(63, 64, 89, 81),
		(71, 68, 99, 91),
		(80, 72, 110, 102),
		(90, 78, 122, 114),
		(101, 84, 136, 128),
		(113, 90, 150, 142),
		(127, 98, 167, 159),
		(142, 108, 186, 178),
		(159, 118, 206, 198)
	};

	private const int Frames = 12;

	private const int RowEdge = 0;

	private const int RowOuter = 1;

	private const int RowMid = 2;

	private const int RowCore = 3;

	private const int RowFrontOuter = 4;

	private const int RowFrontCore = 5;

	private static readonly Palette Base = new Palette(C(96, 128, 186), C(168, 196, 236), C(222, 236, 255), C(255, 255, 255), C(200, 228, 255), C(150, 200, 255), C(170, 205, 255));

	private static readonly Palette Gold = new Palette(C(196, 116, 20), C(255, 196, 60), C(255, 232, 130), C(255, 252, 225), C(255, 236, 150), C(200, 230, 255), C(255, 205, 90));

	private static readonly Palette Red = new Palette(C(110, 0, 12), C(255, 36, 24), C(255, 112, 72), C(255, 232, 212), C(255, 160, 90), C(255, 210, 190), C(255, 60, 40));

	private static readonly Palette Blue = new Palette(C(20, 96, 170), C(70, 180, 255), C(170, 232, 255), C(240, 252, 255), C(150, 225, 255), C(190, 240, 255), C(90, 190, 255));

	private static readonly Palette God = new Palette(C(160, 30, 70), C(250, 90, 140), C(255, 170, 200), C(255, 236, 244), C(255, 190, 215), C(255, 210, 230), C(255, 120, 170));

	private static readonly Palette Ultra = new Palette(C(64, 40, 150), C(120, 200, 255), C(214, 222, 255), C(255, 255, 255), C(200, 240, 255), C(210, 180, 255), C(150, 160, 255));

	private static readonly Color White = C(255, 255, 255);

	private static readonly Color Dust = C(232, 218, 184);

	private static readonly Color SkyGlow = C(150, 200, 255);

	private static readonly Color VeilColor = C(10, 12, 30);

	private static readonly Color BorderColor = C(110, 8, 8);

	private static readonly Color KaiokenBody = C(255, 60, 40);

	private static readonly Dictionary<Guid, Show> Shows = new Dictionary<Guid, Show>();

	private static readonly List<Streak> Streaks = new List<Streak>();

	private static readonly List<Spark> Sparks = new List<Spark>();

	private static readonly List<Bolt> Bolts = new List<Bolt>();

	private static readonly List<SkyBolt> SkyBolts = new List<SkyBolt>();

	private static readonly List<Glow> Glows = new List<Glow>();

	private static readonly List<Gust> Gusts = new List<Gust>();

	private static readonly List<Heat> Heats = new List<Heat>();

	private static readonly List<Smoke> Smokes = new List<Smoke>();

	private static readonly List<Mark> Marks = new List<Mark>();

	private static readonly List<ScreenFlash> Flashes = new List<ScreenFlash>();

	private static readonly Random Noise = new Random();

	private static readonly Queue<string> Preload = new Queue<string>();

	private static long _lastUpdate;

	private static float _veilAlpha;

	private static float _veilX;

	private static float _veilY;

	private static float _borderAlpha;

	private const int AirFade = 22;

	private static bool _enVol;

	private const long UltraImpactMs = 230L;

	private const long UltraVoidMs = 350L;

	private static readonly long[] UltraBeatsFull = new long[6] { 300L, 1300L, 1900L, 2300L, 2550L, 2730L };

	private static readonly long[] UltraBeatsShort = new long[2] { 0L, 260L };

	private static readonly Color UltraSilver = C(226, 232, 255);

	private static readonly Color UltraCyan = C(120, 200, 255);

	private static readonly Color UltraViolet = C(150, 110, 255);

	private static readonly Color UltraDull = C(112, 112, 118);

	private static readonly List<UltraRing> UltraRings = new List<UltraRing>();

	private static readonly List<Mote> Motes = new List<Mote>();

	private static readonly List<Glint> Glints = new List<Glint>();

	private static readonly List<Mark> GlowingCracks = new List<Mark>();

	private static readonly (float U, float V, float Phase, float Speed, bool Big, int Tint)[] UltraStars = MakeStars();

	private static float _ultraDull;

	private static float _ultraStars;

	private static float _pushTarget = 1f;

	private static float _silenceTarget = 1f;

	private static float _cameraPush = 1f;

	private static Show? _impactShow;

	public static float CameraPush => _cameraPush;

	private static Color C(int r, int g, int b)
	{
		return new Color(255, r, g, b);
	}

	private static Color Tint(Color color, float alpha)
	{
		return new Color((int)Math.Clamp(alpha * 255f, 0f, 255f), color.R, color.G, color.B);
	}

	private static float R()
	{
		return (float)Noise.NextDouble();
	}

	public static bool KeepsPaperdolls(Entity entity)
	{
		return IsOwnForm(entity.Sprite, entity.TransformedSprite);
	}

	private static bool IsOwnForm(string? sprite, string? transformed)
	{
		if (string.IsNullOrEmpty(sprite) || string.IsNullOrEmpty(transformed))
		{
			return false;
		}
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sprite);
		string fileNameWithoutExtension2 = Path.GetFileNameWithoutExtension(transformed);
		return fileNameWithoutExtension2.StartsWith(fileNameWithoutExtension + "_", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsKaioken(string? form)
	{
		return form?.Contains("kaioken", StringComparison.OrdinalIgnoreCase) ?? false;
	}

	internal static bool IsUltraForm(string? form)
	{
		return IsUltra(form);
	}

	internal static bool IsKaiokenForm(string? form)
	{
		if (!string.IsNullOrEmpty(form))
		{
			return IsKaioken(form);
		}
		return false;
	}

	internal static float KaiokenBeat(Entity entity, long now)
	{
		if (Shows.TryGetValue(entity.Id, out Show value) && value.Kind == Kind.Kaioken && value.EndedAt == 0L)
		{
			return Heart(value, now);
		}
		long num = now % 900;
		if (num >= 180)
		{
			if (num < 290)
			{
				return 0.7f * (1f - (float)(num - 180) / 110f);
			}
		}
		else if (num < 110)
		{
			return 1f - (float)num / 110f;
		}
		return 0f;
	}

	private static bool IsUltra(string? form)
	{
		if (form != null)
		{
			if (!Path.GetFileNameWithoutExtension(form).EndsWith("_ui", StringComparison.OrdinalIgnoreCase))
			{
				return form.Contains("ultra", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
		return false;
	}

	public static string TextureNameFor(string sprite, string transformed)
	{
		if (string.IsNullOrEmpty(transformed))
		{
			return sprite;
		}
		if (!IsKaioken(transformed) || !IsOwnForm(sprite, transformed))
		{
			return transformed;
		}
		return sprite;
	}

	public static Color KiColorOf(string? transformed)
	{
		if (!string.IsNullOrEmpty(transformed))
		{
			return PaletteOf(transformed).Mid;
		}
		return Base.Mid;
	}

	public static Color KiEdgeColorOf(string? transformed)
	{
		if (!string.IsNullOrEmpty(transformed))
		{
			return PaletteOf(transformed).Edge;
		}
		return Base.Edge;
	}

	private static Palette PaletteOf(string form)
	{
		string text = form.ToLowerInvariant();
		if (IsUltra(form))
		{
			return Ultra;
		}
		if (text.Contains("blue") || text.Contains("ssb") || text.Contains("ssgss"))
		{
			return Blue;
		}
		if (text.Contains("god") || text.Contains("ssg") || text.Contains("rose"))
		{
			return God;
		}
		if (!text.Contains("kaioken"))
		{
			return Gold;
		}
		return Red;
	}

	public static void OnSpriteChanged(Entity entity, string? previous, string? next)
	{
		Show value;
		bool flag = Shows.TryGetValue(entity.Id, out value) && value.EndedAt == 0;
		if (!string.IsNullOrEmpty(next) && (IsOwnForm(entity.Sprite, next) || IsUltra(next)))
		{
			if (!flag)
			{
				Begin(entity, next);
			}
		}
		else if (flag)
		{
			End(entity, value);
		}
	}

	private static void Begin(Entity entity, string form)
	{
		if (Shows.TryGetValue(entity.Id, out Show value))
		{
			StopSounds(value);
		}
		long milliseconds = Timing.Global.Milliseconds;
		int num = Math.Max(0, SequenceOf(entity, form));
		bool flag = IsKaioken(form);
		bool flag2 = IsUltra(form) && !flag;
		Show show = new Show
		{
			Kind = (flag ? Kind.Kaioken : (flag2 ? Kind.UltraInstinct : Kind.SuperSaiyan)),
			Palette = PaletteOf(form),
			Ultra = IsUltra(form),
			Name = FormName(entity),
			StartedAt = milliseconds,
			BurstAt = milliseconds + num,
			MapId = entity.MapId
		};
		Shows[entity.Id] = show;
		Status status = entity.Status.OfType<Status>().FirstOrDefault((Status status2) => status2.Type == SpellEffect.Transform);
		if (status != null && status.TotalDuration > 1 && status.TotalDuration - status.TimeRemaining + (milliseconds - status.TimeRecevied) > num + 1500)
		{
			show.Established = (show.Burst = (show.Opened = (show.RocksLifted = (show.MoreCracks = true))));
			show.BurstAt = milliseconds - 5000 - 700;
			return;
		}
		entity.TransformingUntil = milliseconds + num;
		entity.ResetSpriteFrame();
		foreach (string item in new string[3] { "transfo_rochers.png", "transfo_voile.png", "pv_bord_cote.png" }.Concat(from band in Bands.Reverse()
			select $"transfo_aura_{band.Height}.png"))
		{
			if (!Preload.Contains(item))
			{
				Preload.Enqueue(item);
			}
		}
		if (flag2)
		{
			BeginUltra(entity, show);
			return;
		}
		Play(entity, show, flag ? "kaioken.wav" : "ssj_aura.wav", 16);
		if (!flag)
		{
			Play(entity, show, "ssj_montee.wav", 16);
		}
	}

	private static void End(Entity entity, Show show)
	{
		long milliseconds = Timing.Global.Milliseconds;
		(float, float, int, float) tuple = Look(show, milliseconds);
		float endHeight;
		if (tuple.Item2 > 0.05f)
		{
			(endHeight, _, _, _) = tuple;
		}
		else
		{
			endHeight = 70f;
		}
		show.EndHeight = endHeight;
		show.EndAlpha = ((tuple.Item2 > 0.05f) ? tuple.Item2 : 0.8f);
		show.EndedAt = milliseconds;
		StopSounds(show);
		entity.TransformingUntil = 0L;
		Sound(entity, (show.Kind == Kind.Kaioken) ? "kaioken_fin.wav" : "fin_transfo.wav", "fin_transfo.wav", loop: false, 16);
		foreach (Rock item in show.Rocks.Where((Rock rock) => rock.State == 0))
		{
			item.State = 2;
			item.Vy = 0f;
		}
	}

	private static void StopSounds(Show show)
	{
		foreach (MapSound sound in show.Sounds)
		{
			sound.Stop();
		}
		show.Sounds.Clear();
	}

	private static void Play(Entity entity, Show show, string name, int distance)
	{
		MapSound mapSound = Sound(entity, name, null, loop: false, distance);
		if (mapSound != null)
		{
			show.Sounds.Add(mapSound);
		}
	}

	public static void Clear()
	{
		foreach (Show value in Shows.Values)
		{
			StopSounds(value);
		}
		Shows.Clear();
		Streaks.Clear();
		Sparks.Clear();
		Bolts.Clear();
		SkyBolts.Clear();
		Glows.Clear();
		Gusts.Clear();
		Heats.Clear();
		Smokes.Clear();
		Marks.Clear();
		Flashes.Clear();
		_veilAlpha = (_borderAlpha = 0f);
		ClearUltra();
	}

	private static MapSound? Sound(Entity entity, string name, string? fallback, bool loop, int distance)
	{
		if (Globals.ContentManager.GetSound(Path.GetFileNameWithoutExtension(name)) == null)
		{
			if (fallback == null || Globals.ContentManager.GetSound(Path.GetFileNameWithoutExtension(fallback)) == null)
			{
				return null;
			}
			name = fallback;
		}
		return Audio.AddMapSound(name, entity.X, entity.Y, entity.MapId, loop, 0, distance, entity);
	}

	private static string FormName(Entity entity)
	{
		foreach (IStatus item in entity.Status)
		{
			if (item.Type == SpellEffect.Transform && DatabaseObject<SpellDescriptor>.TryGet(item.SpellId, out var databaseObject))
			{
				return databaseObject.Name ?? string.Empty;
			}
		}
		return string.Empty;
	}

	private static float EaseOut(float x)
	{
		float num = 1f - Math.Clamp(x, 0f, 1f);
		return 1f - num * num * num;
	}

	private static float Build(Show show, long now)
	{
		return Math.Clamp((float)(now - show.StartedAt) / (float)Math.Max(1L, show.BurstAt - show.StartedAt), 0f, 1f);
	}

	private static float Pulse(Show show, long now)
	{
		long num = now - show.PulseAt;
		if (num < 0 || num >= 260)
		{
			return 0f;
		}
		return MathF.Exp((float)(-num) / 90f);
	}

	private static float Heart(Show show, long now)
	{
		long num = now - show.PulseAt;
		if (num >= 180)
		{
			if (num < 290)
			{
				return 0.7f * (1f - (float)(num - 180) / 110f);
			}
		}
		else if (num >= 0 && num < 110)
		{
			return 1f - (float)num / 110f;
		}
		return 0f;
	}

	private static bool FlickersGold(Show show, long now)
	{
		if (show.Burst)
		{
			return true;
		}
		float num = Build(show, now);
		if (num > 0.4f)
		{
			return now / (long)(220f - 170f * num) % 2 == 1;
		}
		return false;
	}

	private static float Dose(Entity entity)
	{
		Player me = Globals.Me;
		if (me == null)
		{
			return 0f;
		}
		if (entity == me)
		{
			return 1f;
		}
		FloatRect worldPos = entity.WorldPos;
		FloatRect worldPos2 = me.WorldPos;
		float num = worldPos.X + worldPos.Width / 2f - (worldPos2.X + worldPos2.Width / 2f);
		float num2 = worldPos.Y + worldPos.Height / 2f - (worldPos2.Y + worldPos2.Height / 2f);
		if (!(MathF.Sqrt(num * num + num2 * num2) / (float)Options.Instance.Map.TileWidth > 12f))
		{
			return 0.5f;
		}
		return 0f;
	}

	private static void Shake(Entity entity, float dose, float strength, long durationMs)
	{
		if (dose > 0f)
		{
			CombatFeedback.StartShake((entity == Globals.Me) ? strength : (strength * 0.4f), durationMs);
		}
	}

	private static void AddFlash(float dose, long at, long hold, long fade, Color color, float alpha)
	{
		if (dose > 0f)
		{
			Flashes.Add(new ScreenFlash(at, hold, fade, color, alpha * dose));
		}
	}

	private static (float X, float Y)? AnchorOf(Show show)
	{
		if (!show.Anchored)
		{
			return null;
		}
		if (show.MapId == Guid.Empty)
		{
			return (show.AnchorX, show.AnchorY);
		}
		if (!MapInstance.TryGet(show.MapId, out MapInstance instance))
		{
			return null;
		}
		return ((float)instance.X + show.AnchorX, (float)instance.Y + show.AnchorY);
	}

	private static void Anchor(Show show, Guid mapId, float fx, float fy)
	{
		show.Anchored = true;
		if (MapInstance.TryGet(mapId, out MapInstance instance))
		{
			show.MapId = mapId;
			show.AnchorX = fx - (float)instance.X;
			show.AnchorY = fy - (float)instance.Y;
		}
		else
		{
			show.MapId = Guid.Empty;
			show.AnchorX = fx;
			show.AnchorY = fy;
		}
	}

	private static (float Height, float Alpha, int Frame, float Force) Look(Show show, long now)
	{
		if (show.Kind == Kind.UltraInstinct)
		{
			return (Height: 0f, Alpha: 0f, Frame: 0, Force: 1f);
		}
		bool flag = show.Kind == Kind.Kaioken;
		float item = (flag ? 1.25f : 1f);
		int num = (flag ? 38 : 50);
		if (show.EndedAt > 0)
		{
			float num2 = 1f - (float)(now - show.EndedAt) / (float)(flag ? 260 : 320);
			if (!(num2 <= 0f))
			{
				return (Height: show.EndHeight * (0.4f + 0.6f * num2), Alpha: show.EndAlpha * num2, Frame: FrameAt(now, 45L), Force: item);
			}
			return (Height: 0f, Alpha: 0f, Frame: 0, Force: item);
		}
		if (!show.Burst)
		{
			if (flag)
			{
				return (Height: 0f, Alpha: 0f, Frame: 0, Force: item);
			}
			float num3 = Build(show, now);
			return (Height: 50f + 55f * num3, Alpha: Math.Min(1f, 0.3f + num3 * 2f), Frame: FrameAt(now, 45L), Force: item);
		}
		long num4 = now - show.BurstAt;
		int item2 = ((now < show.FrozenUntil) ? FrameAt(show.BurstAt, num) : FrameAt(now, num));
		if (num4 < 5000)
		{
			if (flag)
			{
				float num5 = ((now / 76 % 2 == 1) ? 1.07f : 0.95f);
				return (Height: (140f - 34f * EaseOut((float)num4 / 600f)) * num5 * (1f + 0.25f * Heart(show, now)), Alpha: 1f, Frame: item2, Force: item);
			}
			return (Height: (160f - 64f * EaseOut((float)num4 / 1500f)) * (1f + 0.28f * Pulse(show, now)), Alpha: 1f, Frame: item2, Force: item);
		}
		float num6 = (float)(num4 - 5000) / 700f;
		if (!(num6 >= 1f))
		{
			return (Height: (float)(flag ? 106 : 96) * (0.4f + 0.6f * (1f - num6)), Alpha: 1f - num6, Frame: item2, Force: item);
		}
		return (Height: 0f, Alpha: 0f, Frame: 0, Force: item);
	}

	private static int FrameAt(long now, long frameMs)
	{
		return (int)(now / frameMs % 12);
	}

	private static float HalfWidth(float height)
	{
		if (!(height < 96f))
		{
			return 16f + (height - 96f) * 0.155f;
		}
		return 10f + (height - 50f) * 0.13f;
	}

	private static int BandOf(float height)
	{
		return Math.Clamp((int)MathF.Round(MathF.Log(Math.Max(1f, height) / 40f) / MathF.Log(1.12f)), 0, Bands.Length - 1);
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

	private static void Update(Entity entity, Show show, long now, float dt)
	{
		FloatRect worldPos = entity.WorldPos;
		float fx = worldPos.X + worldPos.Width / 2f;
		float fy = worldPos.Y + worldPos.Height - 2f;
		foreach (MapSound sound in show.Sounds)
		{
			sound.UpdatePosition(entity.X, entity.Y, entity.MapId);
		}
		if (!show.Anchored && worldPos.Width > 0f && !Flight.IsAirborne(entity))
		{
			Anchor(show, entity.MapId, fx, fy);
		}
		if (now >= show.FrozenUntil)
		{
			UpdateRocks(show, now, dt);
		}
		show.Dust.RemoveAll((Speck speck) => now - speck.At >= speck.Life);
		if (!show.Anchored)
		{
			return;
		}
		float dose = Dose(entity);
		if (show.EndedAt > 0)
		{
			if (!show.EndShown)
			{
				show.EndShown = true;
				ShowEnd(show, fx, fy, now);
			}
		}
		else if (show.Kind == Kind.Kaioken)
		{
			UpdateKaioken(entity, show, now, fx, fy, dose);
		}
		else if (show.Kind == Kind.UltraInstinct)
		{
			UpdateUltra(entity, show, now, fx, fy, dose);
		}
		else
		{
			UpdateSuperSaiyan(entity, show, now, fx, fy, dose);
		}
	}

	private static void UpdateSuperSaiyan(Entity entity, Show show, long now, float fx, float fy, float dose)
	{
		Palette palette = show.Palette;
		if (!show.Burst)
		{
			long num = now - show.StartedAt;
			float num2 = Build(show, now);
			if (!show.Opened)
			{
				show.Opened = true;
				BlowDust(show, 0f, 0f, now, 20, 20f, 60f, 0.3f, 2, 600L);
				AddCracks(show, now, 4, 18f);
			}
			if (!show.RocksLifted && (float)num >= (float)(show.BurstAt - show.StartedAt) * 0.15f)
			{
				show.RocksLifted = true;
				LiftRocks(show, now);
			}
			if (!show.MoreCracks && num2 > 0.5f)
			{
				show.MoreCracks = true;
				AddCracks(show, now, 4, 30f);
			}
			if (num2 > 0.22f && now >= show.NextSkyBoltAt)
			{
				show.NextSkyBoltAt = now + (long)(520f - 380f * num2 + R() * 120f);
				StrikeSky(entity, show, now, (float)((!(R() < 0.5f)) ? 1 : (-1)) * (18f + R() * 42f), dose);
			}
			if (num2 > 0.55f && now >= show.NextSparkAt)
			{
				show.NextSparkAt = now + 90 + (long)(R() * 120f);
				float x = R() * (float)Math.PI * 2f;
				Bolts.Add(NewBolt(fx + MathF.Cos(x) * 12f, fy - 22f + MathF.Sin(x) * 16f, R() * (float)Math.PI * 2f, 10f + R() * 12f, now, Base.Bolt));
			}
			if (now - show.NextStreakAt > 250)
			{
				show.NextStreakAt = now;
			}
			while (show.NextStreakAt <= now)
			{
				show.NextStreakAt += 17L;
				for (int i = 0; i < 1 + (int)(num2 * 2f); i++)
				{
					Jet(fx, fy, now, (R() < 0.5f) ? Base : palette, 80f + 80f * num2, 160f + 160f * num2);
				}
			}
			if (now >= show.NextDustAt)
			{
				show.NextDustAt = now + 55;
				BlowDust(show, 0f, 0f, now, 1, 10f, 30f, 0.3f, 1, 500L);
			}
			if (now >= show.NextShakeAt)
			{
				show.NextShakeAt = now + 80;
				Shake(entity, dose, 0.4f + 2.2f * num2, 130L);
			}
			Veil(0.58f * num2 * dose, fx, fy);
			if (now >= show.BurstAt)
			{
				BurstSuperSaiyan(entity, show, now, fx, fy, dose);
			}
			return;
		}
		long num3 = now - show.BurstAt;
		if (num3 < 1400)
		{
			Veil(0.58f * (1f - EaseOut((float)num3 / 1400f)) * dose, fx, fy);
		}
		if (num3 < 5000)
		{
			if (now >= show.NextPulseAt)
			{
				show.NextPulseAt = now + 650;
				show.PulseAt = now;
				(float, float)? tuple = AnchorOf(show);
				if (tuple.HasValue)
				{
					var (num4, num5) = tuple.GetValueOrDefault();
					BlowDust(show, fx - num4, fy - num5, now, 12, 20f, 60f, 0.35f, 2, 600L);
				}
			}
			if (now >= show.NextStreakAt)
			{
				show.NextStreakAt = now + 33;
				if (R() < 0.5f)
				{
					Jet(fx, fy, now, palette, 90f, 170f);
				}
			}
			if (num3 > 900 && now >= show.NextSparkAt)
			{
				show.NextSparkAt = now + 380 + (long)(R() * 700f);
				float x2 = R() * (float)Math.PI * 2f;
				Bolts.Add(NewBolt(fx + MathF.Cos(x2) * 10f, fy - 34f + MathF.Sin(x2) * 8f, R() * (float)Math.PI * 2f, 8f + R() * 8f, now, palette.Bolt));
			}
		}
		else
		{
			if (now >= show.NextStreakAt)
			{
				show.NextStreakAt = now + 160 + (long)(R() * 160f);
				Jet(fx, fy, now, palette, 50f, 110f);
			}
			if (now >= show.NextSparkAt)
			{
				show.NextSparkAt = now + 900 + (long)(R() * 1300f);
				float x3 = R() * (float)Math.PI * 2f;
				Bolts.Add(NewBolt(fx + MathF.Cos(x3) * 10f, fy - 30f + MathF.Sin(x3) * 10f, R() * (float)Math.PI * 2f, 7f + R() * 7f, now, palette.Bolt));
			}
		}
	}

	private static void BurstSuperSaiyan(Entity entity, Show show, long now, float fx, float fy, float dose)
	{
		show.Burst = true;
		bool ultra = show.Ultra;
		show.FrozenUntil = now + (ultra ? 170 : 90);
		Palette palette = show.Palette;
		Play(entity, show, "ssj_cri.wav", ultra ? 24 : 18);
		if (entity == Globals.Me)
		{
			CombatFeedback.HitStop(ultra ? 170 : 90);
			CombatFeedback.PunchZoom(ultra ? 0.2f : 0.12f, ultra ? 800 : 520);
		}
		Shake(entity, dose, ultra ? 8f : 5f, ultra ? 1000 : 650);
		AddFlash(dose, now, ultra ? 150 : 80, ultra ? 800 : 480, ultra ? C(236, 244, 255) : C(255, 246, 214), ultra ? 1f : 0.95f);
		if (ultra)
		{
			AddFlash(dose, now + 260, 60L, 520L, C(150, 110, 255), 0.45f);
		}
		BlowDust(show, 0f, 0f, now, ultra ? 170 : 110, 110f, ultra ? 320 : 240, 0.33f, 2, 900L);
		(float, float)? tuple = AnchorOf(show);
		if (tuple.HasValue)
		{
			var (ax, ay) = tuple.GetValueOrDefault();
			Blow(ax, ay, now, ultra ? 34 : 22, ultra ? C(236, 244, 255) : C(255, 250, 230), ultra ? 360 : 280);
			Blow(ax, ay, now, ultra ? 20 : 12, ultra ? C(150, 200, 255) : C(255, 214, 120), ultra ? 260 : 200);
		}
		for (int i = 0; i < (ultra ? 100 : 50); i++)
		{
			Jet(fx, fy, now, palette, 180f, ultra ? 420 : 320);
		}
		Scatter(fx, fy, now, ultra ? 80 : 40, palette, 90f, ultra ? 260 : 200);
		AddCracks(show, now, ultra ? 12 : 7, ultra ? 64 : 44);
		AddStain(show, now, 1);
		foreach (Rock rock in show.Rocks)
		{
			int num = ((MathF.Sign(rock.X) == 0) ? 1 : MathF.Sign(rock.X));
			rock.State = 1;
			rock.At = Math.Min(rock.At, now);
			rock.Vx = rock.X * 4f + (float)num * (60f + R() * 90f);
			rock.Vy = 0f - (40f + R() * 60f);
		}
		for (int j = 0; j < (show.Anchored ? 10 : 0); j++)
		{
			float x = R() * (float)Math.PI * 2f;
			float num2 = 8f + R() * 12f;
			float num3 = MathF.Cos(x) * num2;
			show.Rocks.Add(new Rock
			{
				X = num3,
				Ground = MathF.Sin(x) * num2 * 0.32f,
				Height = 1f,
				State = 1,
				Vx = num3 * 6f + (R() - 0.5f) * 60f,
				Vy = 0f - (80f + R() * 70f),
				Shape = ((R() < 0.5f) ? Noise.Next(4) : (4 + Noise.Next(3))),
				At = now
			});
		}
		show.NextPulseAt = now + 900;
		show.NextSparkAt = now + 900;
		if (show.Name.Length > 0)
		{
			CombatFeedback.Callout(entity.Id, show.Name.ToUpperInvariant(), show.Ultra ? new Color(255, 200, 240, 255) : new Color(255, 255, 214, 80), show.Ultra ? 18 : 14);
		}
	}

	private static void UpdateKaioken(Entity entity, Show show, long now, float fx, float fy, float dose)
	{
		if (!show.Burst)
		{
			long num = now - show.StartedAt;
			long num2 = Math.Max(1L, show.BurstAt - show.StartedAt);
			int beats = show.Beats;
			while (beats < 3 && (float)num >= (float)(num2 * beats) * 0.307f)
			{
				show.Beats++;
				show.PulseAt = now;
				Shake(entity, dose, 1f + (float)beats * 0.5f, 120L);
				for (int i = 0; i < 6 + beats * 4; i++)
				{
					Sparks.Add(new Spark(fx + (R() - 0.5f) * 14f, fy - R() * 30f, (R() - 0.5f) * 30f, -20f - R() * 40f, now, 250 + (long)(R() * 200f), Red.Outer, 0f));
				}
				beats = show.Beats;
			}
			_borderAlpha = Math.Max(_borderAlpha, 0.34f * Build(show, now) * dose);
			if (now >= show.BurstAt)
			{
				BurstKaioken(entity, show, now, fx, fy, dose);
			}
			return;
		}
		long num3 = now - show.BurstAt;
		bool flag = num3 < 5000;
		if (now >= show.NextPulseAt)
		{
			show.NextPulseAt = now + 900;
			show.PulseAt = now;
			if (flag)
			{
				Shake(entity, dose, 1f, 70L);
				Sound(entity, "kaioken_coeur.wav", null, loop: false, 12);
				(float, float)? tuple = AnchorOf(show);
				if (tuple.HasValue)
				{
					var (num4, num5) = tuple.GetValueOrDefault();
					BlowDust(show, fx - num4, fy - num5, now, 6, 14f, 40f, 0.3f, 1, 400L);
				}
			}
		}
		if (now >= show.NextStreakAt)
		{
			show.NextStreakAt = now + (flag ? 22 : (120 + (long)(R() * 120f)));
			if (!flag || R() < 0.75f)
			{
				Jet(fx, fy, now, Red, flag ? 140 : 90, flag ? 240 : 160);
			}
		}
		if (now >= show.NextBurstAt)
		{
			show.NextBurstAt = now + (flag ? (600 + (long)(R() * 500f)) : (1200 + (long)(R() * 1000f)));
			Scatter(fx, fy, now, flag ? 10 : 6, Red, 40f, 110f);
		}
		if (now >= show.NextHeatAt)
		{
			show.NextHeatAt = now + (flag ? 70 : 160);
			Heats.Add(new Heat(fx + (R() - 0.5f) * 36f, fy - 30f - R() * 30f, now, 700L, R() * 6f, 10f + R() * 12f));
		}
	}

	private static void BurstKaioken(Entity entity, Show show, long now, float fx, float fy, float dose)
	{
		show.Burst = true;
		show.FrozenUntil = now + 60;
		Play(entity, show, "kaioken_cri.wav", 18);
		if (entity == Globals.Me)
		{
			CombatFeedback.HitStop(60L);
			CombatFeedback.PunchZoom(0.09f, 420L);
		}
		Shake(entity, dose, 4.5f, 550L);
		AddFlash(dose, now, 60L, 0L, C(255, 235, 220), 0.9f);
		AddFlash(dose, now + 60, 0L, 400L, C(255, 30, 20), 0.6f);
		BlowDust(show, 0f, 0f, now, 50, 90f, 200f, 0.33f, 2, 700L);
		(float, float)? tuple = AnchorOf(show);
		if (tuple.HasValue)
		{
			var (ax, ay) = tuple.GetValueOrDefault();
			Blow(ax, ay, now, 20, C(255, 120, 90), 300f);
		}
		Scatter(fx, fy, now, 70, Red, 110f, 260f);
		for (int i = 0; i < 36; i++)
		{
			Jet(fx, fy, now, Red, 200f, 340f);
		}
		AddCracks(show, now, 5, 26f);
		AddStain(show, now, 2);
		show.NextPulseAt = now + 700;
		if (show.Name.Length > 0)
		{
			CombatFeedback.Callout(entity.Id, show.Name.ToUpperInvariant(), new Color(255, 255, 90, 70), 14);
		}
	}

	private static void ShowEnd(Show show, float fx, float fy, long now)
	{
		(float, float)? tuple = AnchorOf(show);
		if (tuple.HasValue)
		{
			var (num, num2) = tuple.GetValueOrDefault();
			BlowDust(show, fx - num, fy - num2, now, 16, 18f, 44f, 0.3f, 2, 600L);
		}
		if (show.Kind == Kind.Kaioken)
		{
			for (int i = 0; i < 26; i++)
			{
				Smokes.Add(new Smoke(fx + (R() - 0.5f) * 18f, fy - 10f - R() * 30f, 12f + R() * 22f, now + i * 30, 900 + (long)(R() * 600f), R() * 6f));
			}
		}
		else
		{
			for (int j = 0; j < 18; j++)
			{
				Sparks.Add(new Spark(fx + (R() - 0.5f) * 20f, fy - 20f - R() * 20f, (R() - 0.5f) * 40f, -20f - R() * 30f, now, 500 + (long)(R() * 400f), show.Palette.Mid, 80f));
			}
		}
	}

	private static void Veil(float alpha, float fx, float fy)
	{
		if (alpha > _veilAlpha)
		{
			_veilAlpha = alpha;
			_veilX = fx;
			_veilY = fy - 40f;
		}
	}

	private static void LiftRocks(Show show, long now)
	{
		if (show.Anchored)
		{
			for (int i = 0; i < 16; i++)
			{
				float x = (float)i / 16f * (float)Math.PI * 2f + R() * 0.5f;
				float num = 12f + R() * 22f;
				show.Rocks.Add(new Rock
				{
					X = MathF.Cos(x) * num,
					Ground = MathF.Sin(x) * num * 0.32f,
					Target = 10f + R() * 24f,
					Shape = Noise.Next(4),
					At = now + i * 45
				});
			}
		}
	}

	private static void UpdateRocks(Show show, long now, float dt)
	{
		foreach (Rock rock in show.Rocks)
		{
			if (now >= rock.At)
			{
				switch (rock.State)
				{
				case 0:
					rock.Height += (rock.Target - rock.Height) * Math.Min(1f, dt * 2.4f);
					rock.Spin += dt * 3f;
					break;
				case 1:
					rock.X += rock.Vx * dt;
					rock.Vx *= MathF.Exp((0f - dt) * 1.2f);
					Fall(rock, now, dt, 300f, 60f, 9f);
					break;
				case 2:
					Fall(rock, now, dt, 260f, 45f, 4f);
					break;
				default:
					UpdateUltraRock(rock, dt);
					break;
				}
			}
		}
		show.Rocks.RemoveAll((Rock rock) => rock.State == 3 && now - rock.LandedAt >= 1400);
	}

	private static void Fall(Rock rock, long now, float dt, float gravity, float bounce, float spin)
	{
		rock.Vy += gravity * dt;
		rock.Height -= rock.Vy * dt;
		rock.Spin += dt * spin;
		if (!(rock.Height > 0f))
		{
			if (rock.Vy > bounce)
			{
				rock.Vy = (0f - rock.Vy) * 0.35f;
				rock.Height = 0.01f;
			}
			else
			{
				rock.Height = 0f;
				rock.State = 3;
				rock.LandedAt = now;
			}
		}
	}

	private static void BlowDust(Show show, float x, float y, long now, int count, float minSpeed, float maxSpeed, float flatten, int size, long life)
	{
		for (int i = 0; i < count; i++)
		{
			float x2 = R() * (float)Math.PI * 2f;
			float num = minSpeed + R() * (maxSpeed - minSpeed);
			show.Dust.Add(new Speck(x + MathF.Cos(x2) * 4f, y + MathF.Sin(x2) * 1.5f, MathF.Cos(x2) * num, MathF.Sin(x2) * num * flatten, now, (long)((float)life * (0.6f + 0.8f * R())), (!(R() < 0.5f)) ? 1 : size));
		}
		if (show.Dust.Count > 400)
		{
			show.Dust.RemoveRange(0, show.Dust.Count - 400);
		}
	}

	private static void AddCracks(Show show, long now, int count, float length)
	{
		if (!show.Anchored)
		{
			return;
		}
		for (int i = 0; i < count; i++)
		{
			float num = (float)i / (float)count * (float)Math.PI * 2f + (R() - 0.5f) * 0.6f;
			int num2 = 6 + Noise.Next(3);
			(float, float)[] array = new(float, float)[num2 + 1];
			float num3 = 0f;
			float num4 = 1f;
			array[0] = (num3, num4);
			for (int j = 1; j <= num2; j++)
			{
				float num5 = length / (float)num2 * (0.7f + 0.6f * R());
				float x = num + (R() - 0.5f) * 0.9f;
				num3 += MathF.Cos(x) * num5;
				num4 += MathF.Sin(x) * num5 * 0.35f;
				array[j] = (num3, num4);
			}
			Marks.Add(new Mark(show.MapId, show.AnchorX, show.AnchorY, array, 0, now));
		}
	}

	private static void AddStain(Show show, long now, int stain)
	{
		if (show.Anchored)
		{
			Marks.Add(new Mark(show.MapId, show.AnchorX, show.AnchorY, null, stain, now));
		}
	}

	private static void StrikeSky(Entity entity, Show show, long now, float offsetX, float dose)
	{
		(float, float)? tuple = AnchorOf(show);
		if (tuple.HasValue)
		{
			(float, float) valueOrDefault = tuple.GetValueOrDefault();
			float item = valueOrDefault.Item1;
			float item2 = valueOrDefault.Item2;
			float num = item + offsetX;
			float num2 = item2 - 2f + R() * 8f;
			float num3 = Math.Clamp(Graphics.CurrentView.Y - 4f, num2 - 320f, num2 - 90f);
			float num4 = num2 - num3;
			float num5 = Math.Clamp(9f * num4 / 116f, 9f, 18f);
			int num6 = 12 + (int)(num4 / 60f);
			(float, float)[] array = new(float, float)[num6 + 1];
			float num7 = num + (R() - 0.5f) * 20f;
			array[0] = (num7, num3);
			for (int i = 1; i <= num6; i++)
			{
				num7 += (R() - 0.5f) * num5;
				array[i] = ((i == num6) ? num : num7, num3 + num4 * (float)i / (float)num6);
			}
			int num8 = 4 + Noise.Next(5);
			(float, float)[] array2 = new(float, float)[5]
			{
				array[num8],
				default((float, float)),
				default((float, float)),
				default((float, float)),
				default((float, float))
			};
			int num9 = ((!(R() < 0.5f)) ? 1 : (-1));
			float num10 = Math.Max(1f, num4 / 116f);
			for (int j = 1; j < array2.Length; j++)
			{
				array2[j] = (array2[j - 1].Item1 + (float)num9 * (3f + R() * 4f) * num10, array2[j - 1].Item2 + (3f + R() * 4f) * num10);
			}
			SkyBolts.Add(new SkyBolt(array, array2, now));
			Glows.Add(new Glow(num, num2 - 2f, now, 260L));
			BlowDust(show, offsetX, num2 - item2, now, 12, 20f, 60f, 0.35f, 2, 500L);
			AddFlash(dose, now, 0L, 80L, C(200, 220, 255), 0.18f);
			Sound(entity, "eclair.wav", null, loop: false, 20);
		}
	}

	private static Bolt NewBolt(float x, float y, float angle, float length, long now, Color glow)
	{
		return new Bolt
		{
			X = x,
			Y = y,
			Angle = angle,
			Length = length,
			Until = now + 70 + (long)(R() * 70f),
			Glow = glow
		};
	}

	private static void Jet(float fx, float fy, long now, Palette palette, float minSpeed, float maxSpeed)
	{
		float num = fx + (R() + R() - 1f) * 18f;
		Streaks.Add(new Streak(num, fy - R() * 20f, (0f - (num - fx)) * 0.6f + (R() - 0.5f) * 40f, 0f - (minSpeed + R() * (maxSpeed - minSpeed)), now, 300 + (long)(R() * 300f), 4 + Noise.Next(6), palette.Core, palette.Spark));
		if (Streaks.Count > 700)
		{
			Streaks.RemoveRange(0, Streaks.Count - 700);
		}
	}

	private static void Scatter(float fx, float fy, long now, int count, Palette palette, float minSpeed, float maxSpeed)
	{
		for (int i = 0; i < count; i++)
		{
			float x = R() * (float)Math.PI * 2f;
			float num = minSpeed + R() * (maxSpeed - minSpeed);
			float num2 = R();
			Color color = ((num2 < 0.3f) ? palette.Core : ((num2 < 0.6f) ? palette.Mid : palette.Outer));
			Sparks.Add(new Spark(fx + MathF.Cos(x) * 6f, fy - 16f + MathF.Sin(x) * 10f, MathF.Cos(x) * num, MathF.Sin(x) * num * 0.55f - 20f, now, 300 + (long)(R() * 350f), color, 90f));
		}
	}

	private static void Blow(float ax, float ay, long now, int count, Color color, float speed)
	{
		for (int i = 0; i < count; i++)
		{
			Gusts.Add(new Gust(ax, ay, (float)i / (float)count * (float)Math.PI * 2f + R() * 0.2f, 10f + R() * 6f, speed * (0.8f + 0.4f * R()), 8f + R() * 8f, now, 260 + (long)(R() * 120f), color));
		}
	}

	public static IGameTexture? BaseKiPose(Entity entity)
	{
		if (!Shows.TryGetValue(entity.Id, out Show value) || value.Burst || value.EndedAt > 0 || string.IsNullOrEmpty(entity.Sprite))
		{
			return null;
		}
		return Globals.ContentManager.GetTexture(TextureType.Entity, Path.GetFileNameWithoutExtension(entity.Sprite) + "_ki.png");
	}

	public static IGameTexture? SpriteTexture(Entity entity, IGameTexture? texture, bool oneImageSheet, bool ownKiPose)
	{
		if (texture != null && !oneImageSheet && (!ownKiPose || IsUltraBuilding(entity)) && entity.SpriteAnimation == SpriteAnimations.Ki)
		{
			IGameTexture gameTexture = BaseKiPose(entity);
			if (gameTexture != null)
			{
				return gameTexture;
			}
		}
		if (texture == null || oneImageSheet || !Shows.TryGetValue(entity.Id, out Show value) || value.Kind != Kind.SuperSaiyan || value.EndedAt > 0 || entity.SpriteAnimation != SpriteAnimations.Normal || FlickersGold(value, Timing.Global.Milliseconds) || string.IsNullOrEmpty(entity.Sprite))
		{
			return texture;
		}
		IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Entity, entity.Sprite);
		if (texture2 == null || texture2.Width != texture.Width || texture2.Height != texture.Height)
		{
			return texture;
		}
		return texture2;
	}

	public static void DrawGround()
	{
		DrawUltraGround();
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (Marks.Count == 0 || gameTexture == null)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		for (int num = Marks.Count - 1; num >= 0; num--)
		{
			Mark mark = Marks[num];
			long num2 = milliseconds - mark.At;
			if (num2 >= 6000)
			{
				Marks.RemoveAt(num);
				continue;
			}
			float num3 = mark.X;
			float num4 = mark.Y;
			if (mark.MapId != Guid.Empty)
			{
				if (!MapInstance.TryGet(mark.MapId, out MapInstance instance))
				{
					continue;
				}
				num3 += (float)instance.X;
				num4 += (float)instance.Y;
			}
			long num5 = 6000 - num2;
			float num6 = ((num5 >= 1500) ? 1f : ((float)num5 / 1500f));
			(float, float)[] crack = mark.Crack;
			if (crack != null)
			{
				float num7 = Math.Clamp((float)num2 / 700f, 0f, 1f);
				int num8 = Math.Max(1, (int)((float)(crack.Length - 1) * num7));
				for (int i = 0; i < num8; i++)
				{
					var (num9, num10) = crack[i];
					var (num11, num12) = crack[i + 1];
					Line(gameTexture, num3 + num9, num4 + num10 - 1f, num3 + num11, num4 + num12 - 1f, Tint(C(242, 194, 153), 0.55f * num6), GameBlendModes.None);
					Line(gameTexture, num3 + num9, num4 + num10, num3 + num11, num4 + num12, Tint(C(48, 28, 16), 0.9f * num6), GameBlendModes.None);
				}
			}
			else if (mark.Stain == 1)
			{
				Ellipse(gameTexture, num3, num4 + 1f, 24f, 7f, Tint(C(40, 24, 14), 0.35f * num6));
			}
			else
			{
				Ellipse(gameTexture, num3, num4 + 1f, 18f, 5.5f, Tint(C(60, 14, 10), 0.45f * num6));
				Ellipse(gameTexture, num3, num4 + 1f, 10f, 3f, Tint(C(30, 8, 6), 0.35f * num6));
			}
		}
		DrawUltraCrackLight(gameTexture, milliseconds);
	}

	public static void DrawAura(Entity entity, FloatRect bounds)
	{
		if (!Shows.TryGetValue(entity.Id, out Show value))
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		float num = MathF.Round(bounds.X + bounds.Width / 2f);
		float num2 = MathF.Round(bounds.Y + bounds.Height - 2f);
		bool flag = Flight.IsAirborne(entity);
		if (!value.Anchored && !flag)
		{
			Anchor(value, entity.MapId, num, num2);
		}
		if (value.Kind == Kind.UltraInstinct)
		{
			DrawUltraBehind(entity, value, num, num2, milliseconds, flag);
		}
		(float, float, int, float) tuple = Look(value, milliseconds);
		bool flag2 = value.Kind == Kind.Kaioken;
		Palette palette = ((flag2 || FlickersGold(value, milliseconds) || value.EndedAt > 0) ? value.Palette : Base);
		float num3 = (flag2 ? Heart(value, milliseconds) : Pulse(value, milliseconds));
		if (tuple.Item2 > 0f)
		{
			float num4 = HalfWidth(tuple.Item1);
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "ki_halo.png");
			if (texture != null)
			{
				DrawCentered(texture, num, num2 - tuple.Item1 * 0.4f, num4 * (flag2 ? 4.4f : 4.2f), tuple.Item1 * (flag2 ? 1.28f : 1.24f), Tint(palette.Outer, (flag2 ? 0.28f : 0.22f) * tuple.Item2 * (1f + num3)), GameBlendModes.Add);
				DrawCentered(texture, num, num2 - tuple.Item1 * 0.3f, num4 * 2.2f, tuple.Item1 * 0.84f, Tint(palette.Core, (flag2 ? 0.16f : 0.18f) * tuple.Item2 * (1f + num3)), GameBlendModes.Add);
			}
			if (!flag)
			{
				IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Misc, "ki_charge_sol.png");
				if (texture2 != null)
				{
					float num5 = (float)(flag2 ? 36 : 34) + num4;
					float num6 = (flag2 ? 12f : (11f + num4 * 0.2f));
					DrawCentered(texture2, num, num2 + 1f, num5 * 2f * 64f / 60f, num6 * 2f * 24f / 20f, Tint(palette.Ground, (flag2 ? 0.42f : 0.4f) * tuple.Item2 * (1f + 0.6f * num3)), GameBlendModes.Add);
				}
			}
		}
		(float, float)? tuple2 = AnchorOf(value);
		if (tuple2.HasValue)
		{
			var (ax, ay) = tuple2.GetValueOrDefault();
			DrawDust(value, ax, ay, milliseconds);
			DrawRocks(value, ax, ay, milliseconds, front: false);
		}
		if (tuple.Item2 <= 0f)
		{
			return;
		}
		(IGameTexture, int)? tuple4 = Sheet(tuple.Item1);
		if (tuple4.HasValue)
		{
			(IGameTexture, int) valueOrDefault = tuple4.GetValueOrDefault();
			var (gameTexture, _) = valueOrDefault;
			if (gameTexture != null)
			{
				int item = valueOrDefault.Item2;
				float num7 = tuple.Item2 * tuple.Item4;
				DrawLayer(gameTexture, item, tuple.Item3, 0, num, num2, Tint(palette.Edge, 0.45f * num7), GameBlendModes.None, flag);
				DrawLayer(gameTexture, item, tuple.Item3, 1, num, num2, Tint(palette.Outer, 0.55f * num7), GameBlendModes.None, flag);
				DrawLayer(gameTexture, item, tuple.Item3, 2, num, num2, Tint(palette.Mid, 0.62f * num7), GameBlendModes.None, flag);
				DrawLayer(gameTexture, item, tuple.Item3, 3, num, num2, Tint(palette.Core, 0.5f * num7), GameBlendModes.Add, flag);
			}
		}
	}

	public static void DrawGlow(Entity entity, IGameTexture texture, FloatRect source, FloatRect destination)
	{
		if (!Shows.TryGetValue(entity.Id, out Show value))
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		float fx = MathF.Round(destination.X + destination.Width / 2f);
		float fy = MathF.Round(destination.Y + destination.Height - 2f);
		var (num, color, num2) = BodyLight(value, milliseconds);
		if (num > 0.01f)
		{
			IGameTexture gameTexture = KiCharge.MaskOf(texture, source, 0);
			if (gameTexture != null)
			{
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, gameTexture.Width, gameTexture.Height), destination, Tint(color, Math.Min(1f, num)), null, GameBlendModes.Add);
			}
		}
		if (num2 > 0.01f)
		{
			IGameTexture gameTexture2 = KiCharge.MaskOf(texture, source, 1);
			if (gameTexture2 != null)
			{
				Graphics.DrawGameTexture(gameTexture2, new FloatRect(0f, 0f, gameTexture2.Width, gameTexture2.Height), new FloatRect(destination.X - 1f, destination.Y - 1f, destination.Width + 2f, destination.Height + 2f), Tint(White, num2), null, GameBlendModes.Add);
			}
		}
		(float, float, int, float) tuple2 = Look(value, milliseconds);
		Palette palette = ((value.Kind == Kind.Kaioken || FlickersGold(value, milliseconds) || value.EndedAt > 0) ? value.Palette : Base);
		if (tuple2.Item2 > 0f)
		{
			(IGameTexture, int)? tuple3 = Sheet(tuple2.Item1);
			if (tuple3.HasValue)
			{
				(IGameTexture, int) valueOrDefault = tuple3.GetValueOrDefault();
				var (gameTexture3, _) = valueOrDefault;
				if (gameTexture3 != null)
				{
					int item = valueOrDefault.Item2;
					bool inAir = Flight.IsAirborne(entity);
					DrawLayer(gameTexture3, item, tuple2.Item3, 4, fx, fy, Tint(palette.Outer, 0.45f * tuple2.Item2), GameBlendModes.None, inAir);
					DrawLayer(gameTexture3, item, tuple2.Item3, 5, fx, fy, Tint(palette.Core, 0.45f * tuple2.Item2), GameBlendModes.Add, inAir);
				}
			}
		}
		if (value.Kind == Kind.UltraInstinct)
		{
			DrawUltraFront(entity, value, texture, source, destination, milliseconds);
		}
		(float, float)? tuple5 = AnchorOf(value);
		if (tuple5.HasValue)
		{
			var (ax, ay) = tuple5.GetValueOrDefault();
			DrawRocks(value, ax, ay, milliseconds, front: true);
		}
	}

	private static (float Tint, Color Color, float White) BodyLight(Show show, long now)
	{
		if (show.EndedAt > 0)
		{
			return (Tint: 0f, Color: White, White: 0f);
		}
		if (show.Kind == Kind.UltraInstinct)
		{
			return UltraBodyLight(show, now);
		}
		if (show.Kind == Kind.SuperSaiyan)
		{
			if (!show.Burst)
			{
				(float, Color, int) tuple = (FlickersGold(show, now) ? (0.3f + 0.35f * Build(show, now), show.Palette.Mid, 0) : (0.1f + 0.2f * Build(show, now), Base.Mid, 0));
				return (Tint: tuple.Item1, Color: tuple.Item2, White: tuple.Item3);
			}
			long num = now - show.BurstAt;
			float item = ((num < 260) ? (0.9f * (1f - (float)num / 260f)) : 0f);
			if (num >= 5000)
			{
				return (Tint: 0.07f + 0.04f * MathF.Sin((float)now / 110f), Color: show.Palette.Mid, White: 0f);
			}
			return (Tint: 0.12f + 0.2f * Pulse(show, now), Color: show.Palette.Mid, White: item);
		}
		if (!show.Burst)
		{
			float num2 = 0f;
			long num3 = Math.Max(1L, show.BurstAt - show.StartedAt);
			for (int i = 0; i < 3; i++)
			{
				long num4 = now - (show.StartedAt + (long)((float)(num3 * i) * 0.307f));
				if (num4 >= 0 && num4 < 220)
				{
					num2 = Math.Max(num2, 1f - (float)num4 / 220f);
				}
			}
			return (Tint: num2, Color: KaiokenBody, White: 0f);
		}
		long num5 = now - show.BurstAt;
		float item2 = ((num5 < 160) ? (1f - (float)num5 / 160f) : 0f);
		float num6 = Heart(show, now);
		if (num5 >= 5000)
		{
			return (Tint: 0.14f + 0.22f * num6, Color: KaiokenBody, White: 0f);
		}
		return (Tint: 0.32f + 0.4f * num6, Color: KaiokenBody, White: item2);
	}

	private static (IGameTexture Sheet, int Band)? Sheet(float height)
	{
		int num = BandOf(height);
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, $"transfo_aura_{Bands[num].Height}.png");
		if (texture == null)
		{
			return null;
		}
		return (texture, num);
	}

	private static void DrawLayer(IGameTexture sheet, int band, int frame, int row, float fx, float fy, Color color, GameBlendModes blend, bool inAir = false)
	{
		if (color.A <= 0)
		{
			return;
		}
		(int Height, int Width, int FrameHeight, int FeetY) tuple = Bands[band];
		int item = tuple.Width;
		int item2 = tuple.FrameHeight;
		int item3 = tuple.FeetY;
		if (!inAir)
		{
			Graphics.DrawGameTexture(sheet, new FloatRect(frame * item, row * item2, item, item2), new FloatRect(fx - (float)(item / 2), fy - (float)item3, item, item2), color, null, blend);
			return;
		}
		int num = Math.Max(0, item3 - 22);
		Graphics.DrawGameTexture(sheet, new FloatRect(frame * item, row * item2, item, num), new FloatRect(fx - (float)(item / 2), fy - (float)item3, item, num), color, null, blend);
		for (int i = num; i < item3; i += 2)
		{
			int num2 = Math.Min(2, item3 - i);
			float num3 = 1f - ((float)i + (float)num2 / 2f - (float)num) / (float)(item3 - num);
			Graphics.DrawGameTexture(sheet, new FloatRect(frame * item, row * item2 + i, item, num2), new FloatRect(fx - (float)(item / 2), fy - (float)item3 + (float)i, item, num2), Tint(color, (float)(int)color.A / 255f * num3 * num3), null, blend);
		}
	}

	private static void DrawCentered(IGameTexture texture, float x, float y, float width, float height, Color color, GameBlendModes blend)
	{
		if (color.A > 0)
		{
			Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), new FloatRect(x - width / 2f, y - height / 2f, width, height), color, null, blend);
		}
	}

	private static void DrawDust(Show show, float ax, float ay, long now)
	{
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture == null)
		{
			return;
		}
		foreach (Speck item in show.Dust)
		{
			long num = now - item.At;
			if (num >= 0 && num < item.Life)
			{
				float num2 = (float)num / (float)item.Life;
				float num3 = (1f - MathF.Exp(-4f * (float)num / 1000f)) / 4f;
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(ax + item.X + item.Vx * num3), MathF.Round(ay + item.Y + item.Vy * num3 - 3f * num2), item.Size + 1, item.Size), Tint(Dust, 0.85f * (1f - num2 * num2)));
			}
		}
	}

	private static void DrawRocks(Show show, float ax, float ay, long now, bool front)
	{
		if (show.Rocks.Count == 0)
		{
			return;
		}
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "transfo_rochers.png");
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		bool flag = !show.Burst && show.EndedAt == 0;
		foreach (Rock rock in show.Rocks)
		{
			if (now >= rock.At && rock.Ground >= 0f == front)
			{
				float num = ((rock.State == 3) ? Math.Clamp(1f - (float)(now - rock.LandedAt - 1000) / 400f, 0f, 1f) : 1f) * rock.Alpha;
				float num2 = MathF.Round(ax + rock.X) + (float)(((flag && rock.State == 0) || rock.State == 5) ? (Noise.Next(3) - 1) : 0);
				float num3 = MathF.Round(ay + rock.Ground);
				if (gameTexture != null && rock.Height > 0f)
				{
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num2 - 1f, num3 + 1f, 4f, 1f), Tint(C(20, 18, 14), 0.45f * Math.Max(0f, 1f - rock.Height / 40f) * num));
				}
				if (texture != null)
				{
					int num4 = rock.Shape * 2 + ((int)rock.Spin & 1);
					Graphics.DrawGameTexture(texture, new FloatRect(num4 * 6, 0f, 6f, 6f), new FloatRect(num2 - 1f, MathF.Round(num3 - rock.Height) - 1f, 6f, 6f), new Color((int)(255f * num), 255, 255, 255));
				}
			}
		}
	}

	public static void Draw()
	{
		long milliseconds = Timing.Global.Milliseconds;
		float dt = ((_lastUpdate == 0L) ? 0f : Math.Clamp((float)(milliseconds - _lastUpdate) / 1000f, 0f, 0.1f));
		_lastUpdate = milliseconds;
		if (Globals.Me == null)
		{
			if (Shows.Count > 0 || Streaks.Count > 0 || Marks.Count > 0)
			{
				Clear();
			}
			return;
		}
		if (Preload.TryDequeue(out string result))
		{
			Globals.ContentManager.GetTexture(TextureType.Misc, result)?.GetTexture();
		}
		_veilAlpha = 0f;
		_borderAlpha = 0f;
		_enVol = false;
		BeginUltraFrame();
		KeyValuePair<Guid, Show>[] array = Shows.ToArray();
		foreach (KeyValuePair<Guid, Show> keyValuePair in array)
		{
			keyValuePair.Deconstruct(out var key, out var value);
			Guid guid = key;
			Show show = value;
			Entity entity = FindEntity(guid);
			if (entity == null)
			{
				StopSounds(show);
				Shows.Remove(guid);
				continue;
			}
			_enVol |= Flight.IsAirborne(entity);
			Update(entity, show, milliseconds, dt);
			if (show.EndedAt > 0 && milliseconds - show.EndedAt > 600 && show.Rocks.Count == 0 && show.Dust.Count == 0)
			{
				Shows.Remove(guid);
			}
		}
		EndUltraFrame(dt);
		if (!_enVol)
		{
			IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
			if (gameTexture != null)
			{
				DrawAroundBodies(gameTexture, milliseconds);
			}
		}
	}

	private static void DrawAroundBodies(IGameTexture pixel, long now)
	{
		DrawGlows(now);
		DrawGusts(pixel, now);
		DrawStreaks(pixel, now);
		DrawSparks(pixel, now);
		DrawBolts(pixel, now);
		DrawHeat(pixel, now);
		DrawSmoke(pixel, now);
		DrawUltraMotes(pixel, now);
	}

	public static void DrawSky()
	{
		if (Globals.Me == null)
		{
			return;
		}
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture != null)
		{
			long milliseconds = Timing.Global.Milliseconds;
			DrawSkyBolts(gameTexture, milliseconds);
			if (_enVol)
			{
				DrawAroundBodies(gameTexture, milliseconds);
			}
		}
	}

	private static void DrawSkyBolts(IGameTexture pixel, long now)
	{
		SkyBolts.RemoveAll((SkyBolt bolt) => now - bolt.At >= 190);
		foreach (SkyBolt skyBolt in SkyBolts)
		{
			long num = now - skyBolt.At;
			if (num < 60 || num > 95)
			{
				float num2 = ((num < 60) ? 1f : 0.7f);
				for (int num3 = 0; num3 < skyBolt.Points.Length - 1; num3++)
				{
					var (num4, fromY) = skyBolt.Points[num3];
					var (num5, toY) = skyBolt.Points[num3 + 1];
					Line(pixel, num4 - 1f, fromY, num5 - 1f, toY, Tint(SkyGlow, 0.55f * num2), GameBlendModes.Add);
					Line(pixel, num4 + 1f, fromY, num5 + 1f, toY, Tint(SkyGlow, 0.55f * num2), GameBlendModes.Add);
					Line(pixel, num4, fromY, num5, toY, Tint(White, num2), GameBlendModes.Add);
				}
				for (int num6 = 0; num6 < skyBolt.Branch.Length - 1; num6++)
				{
					var (fromX, fromY2) = skyBolt.Branch[num6];
					var (toX, toY2) = skyBolt.Branch[num6 + 1];
					Line(pixel, fromX, fromY2, toX, toY2, Tint(C(220, 235, 255), 0.8f * num2), GameBlendModes.Add);
				}
			}
		}
	}

	private static void DrawGlows(long now)
	{
		Glows.RemoveAll((Glow glow) => now - glow.At >= glow.Life);
		if (Glows.Count == 0)
		{
			return;
		}
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "ki_halo.png");
		if (texture == null)
		{
			return;
		}
		foreach (Glow glow in Glows)
		{
			float num = (float)(now - glow.At) / (float)glow.Life;
			DrawCentered(texture, glow.X, glow.Y, 28f * (1f + num), 12f * (1f + num), Tint(C(210, 230, 255), 0.9f * (1f - num)), GameBlendModes.Add);
		}
	}

	private static void DrawGusts(IGameTexture pixel, long now)
	{
		Gusts.RemoveAll((Gust gust) => now - gust.At >= gust.Life);
		foreach (Gust gust in Gusts)
		{
			float num = (float)(now - gust.At) / (float)gust.Life;
			float num2 = gust.Radius + gust.Speed * (float)(now - gust.At) / 1000f;
			float num3 = MathF.Cos(gust.Angle);
			float num4 = MathF.Sin(gust.Angle) * 0.42f;
			Line(pixel, gust.X + num3 * num2, gust.Y - 6f + num4 * num2, gust.X + num3 * (num2 - gust.Length), gust.Y - 6f + num4 * (num2 - gust.Length), Tint(gust.Color, 0.75f * (1f - num)), GameBlendModes.Add);
		}
	}

	private static void DrawStreaks(IGameTexture pixel, long now)
	{
		Streaks.RemoveAll((Streak streak) => now - streak.At >= streak.Life);
		foreach (Streak streak in Streaks)
		{
			float num = (float)(now - streak.At) / 1000f;
			float num2 = (float)(now - streak.At) / (float)streak.Life;
			float x = MathF.Round(streak.X + streak.Vx * num);
			float num3 = MathF.Round(streak.Y + streak.Vy * num);
			int num4 = streak.Length + 2;
			for (int num5 = 0; num5 < num4; num5++)
			{
				Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(x, num3 + (float)num5, 1f, 1f), Tint((num5 < 2) ? streak.Head : streak.Tail, (1f - num2) * (1f - (float)num5 / (float)num4)), null, GameBlendModes.Add);
			}
		}
	}

	private static void DrawSparks(IGameTexture pixel, long now)
	{
		Sparks.RemoveAll((Spark spark) => now - spark.At >= spark.Life);
		if (Sparks.Count > 900)
		{
			Sparks.RemoveRange(0, Sparks.Count - 900);
		}
		foreach (Spark spark in Sparks)
		{
			float num = (float)(now - spark.At) / 1000f;
			float num2 = (float)(now - spark.At) / (float)spark.Life;
			float num3 = (1f - MathF.Exp(-3f * num)) / 3f;
			int num4 = ((!(num2 < 0.5f)) ? 1 : 2);
			Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(spark.X + spark.Vx * num3), MathF.Round(spark.Y + spark.Vy * num3 + spark.Gravity * num * num), num4, num4), Tint(spark.Color, 1f - num2), null, GameBlendModes.Add);
		}
	}

	private static void DrawBolts(IGameTexture pixel, long now)
	{
		Bolts.RemoveAll((Bolt bolt) => now >= bolt.Until);
		foreach (Bolt bolt in Bolts)
		{
			if (bolt.JitteredAt < 0 || now - bolt.JitteredAt > 34)
			{
				bolt.JitteredAt = now;
				int num = 5 + Noise.Next(3);
				float num2 = MathF.Cos(bolt.Angle);
				float num3 = MathF.Sin(bolt.Angle);
				bolt.Points = new(float, float)[num + 1];
				for (int num4 = 0; num4 <= num; num4++)
				{
					float num5 = bolt.Length * (float)num4 / (float)num;
					float num6 = ((num4 == 0 || num4 == num) ? 0f : ((R() - 0.5f) * 5f));
					bolt.Points[num4] = (X: bolt.X + num2 * num5 - num3 * num6, Y: bolt.Y + num3 * num5 + num2 * num6);
				}
			}
			for (int num7 = 0; num7 < bolt.Points.Length - 1; num7++)
			{
				var (num8, fromY) = bolt.Points[num7];
				var (num9, toY) = bolt.Points[num7 + 1];
				Line(pixel, num8 + 1f, fromY, num9 + 1f, toY, Tint(bolt.Glow, 0.45f), GameBlendModes.Add);
				Line(pixel, num8 - 1f, fromY, num9 - 1f, toY, Tint(bolt.Glow, 0.45f), GameBlendModes.Add);
				Line(pixel, num8, fromY, num9, toY, White, GameBlendModes.Add);
			}
		}
	}

	private static void DrawHeat(IGameTexture pixel, long now)
	{
		Heats.RemoveAll((Heat heat) => now - heat.At >= heat.Life);
		foreach (Heat heat in Heats)
		{
			long num = now - heat.At;
			float num2 = (float)num / (float)heat.Life;
			for (int num3 = 0; (float)num3 < heat.Height; num3 += 2)
			{
				float x = MathF.Round(heat.X + MathF.Sin((float)num3 / 3f + (float)num / 90f + heat.Phase) * 1.6f);
				float y = MathF.Round(heat.Y - (float)num3 - 30f * num2);
				Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(x, y, 1f, 1f), Tint(C(255, 120, 80), 0.22f * (1f - num2) * (1f - (float)num3 / heat.Height)), null, GameBlendModes.Add);
			}
		}
	}

	private static void DrawSmoke(IGameTexture pixel, long now)
	{
		Smokes.RemoveAll((Smoke smoke) => now - smoke.At >= smoke.Life);
		foreach (Smoke smoke in Smokes)
		{
			long num = now - smoke.At;
			if (num >= 0)
			{
				float num2 = (float)num / (float)smoke.Life;
				float x = MathF.Round(smoke.X + MathF.Sin((float)num / 180f + smoke.Phase) * 3f);
				float y = MathF.Round(smoke.Y - smoke.Speed * (float)num / 1000f);
				Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(x, y, 2f, 2f), Tint(C(210, 210, 215), 0.5f * (1f - num2)));
			}
		}
	}

	public static void DrawVeil()
	{
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture == null || Globals.Me == null)
		{
			return;
		}
		FloatRect worldViewport = Graphics.WorldViewport;
		if (_veilAlpha > 0.005f)
		{
			Color color = Tint(VeilColor, _veilAlpha);
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "transfo_voile.png");
			if (texture != null)
			{
				float num = MathF.Round(_veilX - 110f);
				float num2 = MathF.Round(_veilY - 130f);
				Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), new FloatRect(num, num2, 220f, 260f), color);
				Fill(gameTexture, worldViewport.X, worldViewport.Y, worldViewport.Width, num2 - worldViewport.Y, color);
				Fill(gameTexture, worldViewport.X, num2 + 260f, worldViewport.Width, worldViewport.Y + worldViewport.Height - num2 - 260f, color);
				Fill(gameTexture, worldViewport.X, num2, num - worldViewport.X, 260f, color);
				Fill(gameTexture, num + 220f, num2, worldViewport.X + worldViewport.Width - num - 220f, 260f, color);
			}
			else
			{
				Fill(gameTexture, worldViewport.X, worldViewport.Y, worldViewport.Width, worldViewport.Height, color);
			}
		}
		if (_borderAlpha > 0.005f)
		{
			IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Misc, "pv_bord_cote.png");
			if (texture2 != null)
			{
				LowHealth.DrawSoftEdges(texture2, worldViewport, MathF.Min(worldViewport.Width, worldViewport.Height) * 0.3f, Tint(BorderColor, _borderAlpha));
			}
		}
		DrawUltraVeil(gameTexture, worldViewport);
	}

	public static void DrawFlash()
	{
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		long now = Timing.Global.Milliseconds;
		Flashes.RemoveAll((ScreenFlash flash) => now - flash.At >= flash.Hold + flash.Fade);
		if (gameTexture == null)
		{
			return;
		}
		foreach (ScreenFlash flash in Flashes)
		{
			long num = now - flash.At;
			if (num >= 0)
			{
				float alpha = ((num < flash.Hold) ? flash.Alpha : (flash.Alpha * (1f - (float)(num - flash.Hold) / (float)Math.Max(1L, flash.Fade))));
				Fill(gameTexture, Graphics.CurrentView.X, Graphics.CurrentView.Y, Graphics.CurrentView.Width, Graphics.CurrentView.Height, Tint(flash.Color, alpha));
			}
		}
	}

	private static void Fill(IGameTexture pixel, float x, float y, float width, float height, Color color)
	{
		if (!(width <= 0f) && !(height <= 0f) && color.A > 0)
		{
			Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(x, y, width, height), color);
		}
	}

	private static void Ellipse(IGameTexture pixel, float cx, float cy, float rx, float ry, Color color)
	{
		if (color.A <= 0)
		{
			return;
		}
		for (int i = -(int)ry; i <= (int)ry; i++)
		{
			float num = MathF.Round(rx * MathF.Sqrt(Math.Max(0f, 1f - (float)(i * i) / (ry * ry))));
			if (num > 0f)
			{
				Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(cx) - num, MathF.Round(cy) + (float)i, num * 2f, 1f), color);
			}
		}
	}

	private static void Line(IGameTexture pixel, float fromX, float fromY, float toX, float toY, Color color, GameBlendModes blend)
	{
		if (color.A <= 0)
		{
			return;
		}
		int num = (int)MathF.Round(fromX);
		int num2 = (int)MathF.Round(fromY);
		int num3 = (int)MathF.Round(toX);
		int num4 = (int)MathF.Round(toY);
		int num5 = Math.Abs(num3 - num);
		int num6 = -Math.Abs(num4 - num2);
		int num7 = ((num < num3) ? 1 : (-1));
		int num8 = ((num2 < num4) ? 1 : (-1));
		int num9 = num5 + num6;
		for (int i = 0; i < 512; i++)
		{
			Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num, num2, 1f, 1f), color, null, blend);
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

	private static (float U, float V, float Phase, float Speed, bool Big, int Tint)[] MakeStars()
	{
		Random random = new Random(7);
		(float, float, float, float, bool, int)[] array = new(float, float, float, float, bool, int)[70];
		for (int i = 0; i < array.Length; i++)
		{
			double num = random.NextDouble();
			array[i] = ((float)random.NextDouble(), (float)random.NextDouble() * 0.88f, (float)random.NextDouble() * 6f, 1.5f + (float)random.NextDouble() * 3f, random.NextDouble() < 0.14, (!(num < 0.6)) ? ((num < 0.8) ? 1 : 2) : 0);
		}
		return array;
	}

	public static int SequenceOf(Entity entity, string? form)
	{
		foreach (Status item in entity.Status.OfType<Status>())
		{
			if (item.Type == SpellEffect.Transform && item.Sequence > 0)
			{
				return item.Sequence;
			}
		}
		return Options.Instance.Transformation.SequenceDurationFor(form);
	}

	public static float FormAuraStrength(Entity entity, long now)
	{
		if (!Shows.TryGetValue(entity.Id, out Show value) || value.Kind != Kind.UltraInstinct || value.Burst || value.EndedAt > 0 || value.Established)
		{
			return 1f;
		}
		var (num, num2, _) = UltraProgress(value, now);
		if (!value.Short)
		{
			return Math.Clamp(0.35f * num + 0.6f * num2, 0.05f, 1f);
		}
		return 0.2f + 0.6f * num2;
	}

	private static bool IsUltraBuilding(Entity entity)
	{
		if (Shows.TryGetValue(entity.Id, out Show value) && value.Kind == Kind.UltraInstinct && !value.Burst && value.EndedAt == 0L)
		{
			return !value.Established;
		}
		return false;
	}

	private static float EaseInOut(float x)
	{
		x = Math.Clamp(x, 0f, 1f);
		if (!(x < 0.5f))
		{
			return 1f - MathF.Pow(-2f * x + 2f, 3f) / 2f;
		}
		return 4f * x * x * x;
	}

	private static (float K, float Kv, long VoidAt) UltraProgress(Show show, long now)
	{
		long val = Math.Max(1L, show.BurstAt - show.StartedAt);
		long num = (show.Short ? show.StartedAt : (show.BurstAt - Math.Min(350L, val)));
		float item = (show.Short ? 1f : Math.Clamp((float)(now - show.StartedAt) / (float)Math.Max(1L, num - show.StartedAt), 0f, 1f));
		float item2 = Math.Clamp((float)(now - num) / (float)Math.Max(1L, show.BurstAt - num), 0f, 1f);
		return (K: item, Kv: item2, VoidAt: num);
	}

	private static void BeginUltra(Entity entity, Show show)
	{
		show.Short = show.BurstAt - show.StartedAt < 1500;
		show.Mine = entity == Globals.Me;
		show.NextMoteAt = show.StartedAt;
		PlayUltra(entity, show, show.Short ? "ui_eveil_court.wav" : "ui_eveil.wav", 16);
	}

	private static void PlayUltra(Entity entity, Show show, string name, int distance)
	{
		MapSound mapSound = Sound(entity, name, null, loop: false, distance);
		if (mapSound != null)
		{
			mapSound.IgnoresSilence = true;
			show.Sounds.Add(mapSound);
		}
	}

	private static void ClearUltra()
	{
		UltraRings.Clear();
		Motes.Clear();
		Glints.Clear();
		GlowingCracks.Clear();
		_ultraDull = (_ultraStars = 0f);
		_pushTarget = (_silenceTarget = (_cameraPush = 1f));
		_impactShow = null;
		Audio.Silence = 1f;
	}

	private static void UpdateUltra(Entity entity, Show show, long now, float fx, float fy, float dose)
	{
		show.Fx = fx;
		show.Fy = fy;
		if (!show.Burst)
		{
			(float K, float Kv, long VoidAt) tuple = UltraProgress(show, now);
			float item = tuple.K;
			float item2 = tuple.Kv;
			long item3 = tuple.VoidAt;
			bool flag = now >= item3;
			if (!show.Short && !show.RocksLifted && now >= show.StartedAt + 350)
			{
				show.RocksLifted = true;
				LiftUltraRocks(show, now, 4);
			}
			foreach (Rock rock in show.Rocks)
			{
				if (rock.State == 4)
				{
					rock.Orbit = rock.BaseOrbit + 0.9f * item;
				}
			}
			long[] array = (show.Short ? UltraBeatsShort : UltraBeatsFull);
			float num = (float)(show.BurstAt - show.StartedAt) / (show.Short ? 500f : 3200f);
			while (show.UltraBeats < array.Length && now >= show.StartedAt + (long)((float)array[show.UltraBeats] * num))
			{
				UltraHeartbeat(entity, show, now, fx, fy, dose, show.UltraBeats);
				show.UltraBeats++;
			}
			if (!show.Short && !flag)
			{
				if (now - show.NextMoteAt > 250)
				{
					show.NextMoteAt = now;
				}
				while (show.NextMoteAt <= now)
				{
					show.NextMoteAt += (long)(1000f / (14f + 50f * item));
					float x = R() * (float)Math.PI * 2f;
					float num2 = 10f + R() * 70f;
					AddMote(show, MathF.Cos(x) * num2, MathF.Sin(x) * num2 * 0.35f, 5f + R() * 12f, now, 1500 + (long)(R() * 1200f), -1L);
				}
			}
			if (flag && !show.Emptied)
			{
				show.Emptied = true;
				if (show.Short)
				{
					LiftUltraRocks(show, now, 5);
					for (int i = 0; i < 110; i++)
					{
						float x2 = R() * (float)Math.PI * 2f;
						float num3 = 16f + R() * 64f;
						AddMote(show, MathF.Cos(x2) * num3, MathF.Sin(x2) * num3 * 0.35f - R() * 30f, 0f, now, 99999L, now + 60 + (long)(R() * 140f));
					}
					BlowDust(show, 0f, 0f, now, 40, 60f, 160f, 0.33f, 2, 500L);
				}
				else
				{
					foreach (Mote mote in Motes)
					{
						if (mote.Owner == show && mote.SuckAt < 0)
						{
							long num4 = now - mote.At;
							mote.Y -= mote.Rise * (float)num4 / 1000f;
							mote.X += MathF.Sin((float)num4 / 400f + mote.Phase) * 2f;
							mote.Rise = 0f;
							mote.SuckAt = now;
						}
					}
					foreach (Rock rock2 in show.Rocks)
					{
						if (rock2.State == 4)
						{
							rock2.State = 5;
						}
					}
				}
			}
			if (show.Mine)
			{
				_ultraDull = Math.Max(_ultraDull, show.Short ? (0.5f * EaseOut(item2 * 3f)) : (flag ? 0.5f : (0.45f * EaseOut(item * 1.6f))));
				Veil(show.Short ? (0.85f * EaseOut(item2 * 2f)) : (flag ? (0.42f + 0.45f * item2) : (0.12f + 0.3f * item)), fx, fy);
				_ultraStars = Math.Max(_ultraStars, show.Short ? (2f * item2) : (flag ? (1f + item2) : EaseOut((item - 0.1f) / 0.5f)));
				_pushTarget = Math.Max(_pushTarget, 1f + (show.Short ? (0.06f * item2) : (flag ? (0.1f + 0.04f * item2) : (0.1f * EaseInOut(item)))));
				_silenceTarget = Math.Min(_silenceTarget, show.Short ? 0.08f : Math.Max(0.08f, 1f - (float)(now - show.StartedAt) / 600f));
			}
			if (now >= show.BurstAt)
			{
				BurstUltra(entity, show, now, fx, fy);
			}
			return;
		}
		long num5 = now - show.BurstAt;
		if (!show.Blown && num5 >= 230)
		{
			BlowUltra(entity, show, now, fx, fy, dose);
		}
		foreach (Rock rock3 in show.Rocks)
		{
			if (rock3.Alpha < 1f || (rock3.State == 1 && show.Blown))
			{
				rock3.Alpha = Math.Clamp(1f - (float)(num5 - 230) / 625f, 0f, 1f);
			}
		}
		if (show.Blown && num5 < 5000)
		{
			if (now >= show.NextGlintAt)
			{
				show.NextGlintAt = now + 700 + (long)(R() * 900f);
				Glints.Add(new Glint(fx + (R() - 0.5f) * 22f, fy - 30f - R() * 20f, now, 260L));
			}
			if (now >= show.NextMoteAt)
			{
				show.NextMoteAt = now + 50;
				if (R() < 1.05f)
				{
					float x3 = R() * (float)Math.PI * 2f;
					float num6 = 6f + R() * 22f;
					AddMote(show, MathF.Cos(x3) * num6, MathF.Sin(x3) * num6 * 0.35f - 4f, 10f + R() * 16f, now, 900 + (long)(R() * 700f), -1L);
				}
			}
		}
		if (show.Mine)
		{
			long num7 = num5 - 230;
			_ultraDull = Math.Max(_ultraDull, 0.5f * (1f - EaseOut((float)num7 / 1800f)));
			if (num7 < 1400)
			{
				Veil(0.5f * (1f - EaseOut((float)num7 / 1400f)), fx, fy);
			}
			_ultraStars = Math.Max(_ultraStars, 1f - Math.Clamp((float)num7 / 1500f, 0f, 1f));
			if (num5 < 1600)
			{
				_pushTarget = Math.Max(_pushTarget, 1f + 0.14f * (1f - EaseOut((float)num5 / 1600f)));
			}
			_silenceTarget = Math.Min(_silenceTarget, Math.Clamp((float)num7 / 1500f, 0.08f, 1f));
			if (num5 < 230)
			{
				_impactShow = show;
			}
		}
	}

	private static void UltraHeartbeat(Entity entity, Show show, long now, float fx, float fy, float dose, int n)
	{
		show.BeatAt = now;
		int num = (show.Short ? 4 : n);
		AddUltraRing(show, 0f, 1f, now, 900L, 70 + num * 12, (n < 3 && !show.Short) ? UltraSilver : UltraCyan, shock: false);
		Shake(entity, dose, 0.8f + (float)num * 0.45f, 140L);
		if (n >= 2 || show.Short)
		{
			float x = R() * (float)Math.PI * 2f;
			Bolts.Add(NewBolt(fx + MathF.Cos(x) * 10f, fy - 26f + MathF.Sin(x) * 12f, R() * (float)Math.PI * 2f, 8f + R() * 8f, now, UltraViolet));
		}
	}

	private static void LiftUltraRocks(Show show, long now, int state)
	{
		if (show.Anchored)
		{
			int num = (show.Short ? 14 : 16);
			for (int i = 0; i < num; i++)
			{
				float num2 = (float)i / (float)num * (float)Math.PI * 2f + R() * 0.4f;
				float num3 = 14f + R() * 34f;
				float num4 = 0.15f + R() * 0.2f;
				show.Rocks.Add(new Rock
				{
					Angle = num2,
					Radius = num3,
					X = MathF.Cos(num2) * num3,
					Ground = MathF.Sin(num2) * num3 * 0.32f,
					Height = ((state == 5) ? (2f + R() * 4f) : 0f),
					Target = 12f + R() * 34f,
					BaseOrbit = num4,
					Orbit = num4,
					Spin = R() * 2f,
					Shape = ((R() < 0.45f) ? Noise.Next(4) : (4 + Noise.Next(3))),
					State = state,
					At = now + i * (show.Short ? 12 : 90)
				});
			}
		}
	}

	private static void UpdateUltraRock(Rock rock, float dt)
	{
		switch (rock.State)
		{
		default:
			return;
		case 4:
			rock.Height += (rock.Target - rock.Height) * Math.Min(1f, dt * 0.9f);
			rock.Angle += dt * rock.Orbit;
			rock.Spin += dt * 0.8f;
			break;
		case 5:
			rock.Radius += (4f - rock.Radius) * Math.Min(1f, dt * 6f);
			rock.Height += (26f - rock.Height) * Math.Min(1f, dt * 6f);
			rock.Angle += dt * 4f;
			rock.Spin += dt * 8f;
			break;
		}
		rock.X = MathF.Cos(rock.Angle) * rock.Radius;
		rock.Ground = MathF.Sin(rock.Angle) * rock.Radius * 0.32f;
	}

	private static void BurstUltra(Entity entity, Show show, long now, float fx, float fy)
	{
		show.Burst = true;
		show.FrozenUntil = now + 230;
		Motes.RemoveAll((Mote mote) => mote.Owner == show);
		PlayUltra(entity, show, "ui_explosion.wav", 24);
		if (show.Mine)
		{
			CombatFeedback.HitStop(230L);
			_impactShow = show;
		}
		show.NextGlintAt = now + 900;
		show.NextMoteAt = now + 230;
		if (show.Name.Length > 0)
		{
			CombatFeedback.Callout(entity.Id, show.Name.ToUpperInvariant(), new Color(255, 200, 240, 255), 18);
		}
	}

	private static void BlowUltra(Entity entity, Show show, long now, float fx, float fy, float dose)
	{
		show.Blown = true;
		if (show.Mine)
		{
			CombatFeedback.PunchZoom(0.24f, 1000L);
			AddFlash(1f, now, 120L, 700L, C(236, 244, 255), 1f);
			AddFlash(1f, now + 260, 60L, 520L, UltraViolet, 0.5f);
		}
		Shake(entity, dose, 8f, 1100L);
		AddUltraRing(show, 0f, 1f, now, 950L, 300f, White, shock: true);
		AddUltraRing(show, 0f, 1f, now + 130, 950L, 220f, UltraViolet, shock: true);
		(float, float)? tuple = AnchorOf(show);
		if (tuple.HasValue)
		{
			var (ax, ay) = tuple.GetValueOrDefault();
			Blow(ax, ay, now, 36, C(236, 244, 255), 380f);
			Blow(ax, ay, now, 22, UltraCyan, 280f);
		}
		BlowDust(show, 0f, 0f, now, 150, 110f, 320f, 0.33f, 2, 900L);
		Color[] array = new Color[4] { White, UltraSilver, UltraCyan, UltraViolet };
		for (int i = 0; i < 140; i++)
		{
			float x = R() * (float)Math.PI * 2f;
			float num = 80f + R() * 220f;
			Sparks.Add(new Spark(fx + MathF.Cos(x) * 6f, fy - 20f + MathF.Sin(x) * 10f, MathF.Cos(x) * num, MathF.Sin(x) * num * 0.55f - 20f, now, 300 + (long)(R() * 450f), array[Noise.Next(array.Length)], 40f));
		}
		int count = Marks.Count;
		AddCracks(show, now, 14, 70f);
		for (int j = count; j < Marks.Count; j++)
		{
			GlowingCracks.Add(Marks[j]);
		}
		foreach (Rock rock in show.Rocks)
		{
			float x2 = R() * (float)Math.PI * 2f;
			rock.X = MathF.Cos(x2) * 6f;
			rock.Ground = MathF.Sin(x2) * 2f;
			rock.Height = Math.Max(rock.Height, 12f);
			rock.Vx = MathF.Cos(x2) * (140f + R() * 120f);
			rock.Vy = 0f - (60f + R() * 90f);
			rock.State = 1;
			rock.At = Math.Min(rock.At, now);
		}
	}

	private static void AddUltraRing(Show show, float dx, float dy, long at, long life, float speed, Color color, bool shock)
	{
		if (show.Anchored)
		{
			UltraRings.Add(new UltraRing(show.MapId, show.AnchorX + dx, show.AnchorY + dy, at, life, speed, color, shock));
		}
	}

	private static void AddMote(Show show, float dx, float dy, float rise, long now, long life, long suckAt)
	{
		if (show.Anchored && Motes.Count <= 900)
		{
			float num = R();
			Motes.Add(new Mote
			{
				Owner = show,
				MapId = show.MapId,
				X = show.AnchorX + dx,
				Y = show.AnchorY + dy,
				Rise = rise,
				Phase = R() * 6f,
				At = now,
				Life = life,
				SuckAt = suckAt,
				Color = ((num < 0.55f) ? UltraSilver : ((num < 0.8f) ? UltraCyan : UltraViolet))
			});
		}
	}

	private static (float X, float Y)? ToWorld(Guid mapId, float x, float y)
	{
		if (mapId == Guid.Empty)
		{
			return (x, y);
		}
		if (!MapInstance.TryGet(mapId, out MapInstance instance))
		{
			return null;
		}
		return ((float)instance.X + x, (float)instance.Y + y);
	}

	private static (float Tint, Color Color, float White) UltraBodyLight(Show show, long now)
	{
		if (!show.Burst)
		{
			float item = UltraProgress(show, now).K;
			float num = Math.Max(0f, 1f - (float)(now - show.BeatAt) / 260f);
			return (Tint: 0.15f * item + 0.35f * num, Color: UltraCyan, White: 0f);
		}
		long num2 = now - show.BurstAt;
		float item2 = ((!show.Mine) ? ((num2 < 260) ? (1f - (float)num2 / 260f) : 0f) : ((num2 < 430) ? Math.Clamp(1f - (float)(num2 - 230) / 200f, 0f, 1f) : 0f));
		return (Tint: (num2 < 5000) ? 0.12f : 0.06f, Color: UltraCyan, White: item2);
	}

	private static float BeamWidth(Show show, long now)
	{
		if (show.EndedAt > 0 || show.Established)
		{
			return 0f;
		}
		if (!show.Burst)
		{
			var (num, num2, num3) = UltraProgress(show, now);
			if (now < num3)
			{
				if (show.Short || !(num > 0.35f))
				{
					return 0f;
				}
				return 1f + 5f * EaseInOut((num - 0.35f) / 0.65f);
			}
			if (!show.Short)
			{
				return 6f - 5f * EaseOut(num2 * 1.5f);
			}
			if (!(num2 < 0.2f))
			{
				return 6f - 5f * EaseOut((num2 - 0.2f) / 0.8f);
			}
			return 6f * num2 / 0.2f;
		}
		long num4 = now - show.BurstAt;
		if (num4 >= 230)
		{
			if (num4 >= 900)
			{
				return 0f;
			}
			return 42f * (1f - EaseOut((float)(num4 - 230) / 670f));
		}
		return 42f;
	}

	private static void FillAdd(IGameTexture pixel, float x, float y, float width, float height, Color color)
	{
		if (!(width <= 0f) && !(height <= 0f) && color.A > 0)
		{
			Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(x), MathF.Round(y), MathF.Ceiling(width), MathF.Ceiling(height)), color, null, GameBlendModes.Add);
		}
	}

	private static void DrawUltraBehind(Entity entity, Show show, float fx, float fy, long now, bool inAir)
	{
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture == null)
		{
			return;
		}
		float num = Math.Max(0f, 1f - (float)(now - show.BeatAt) / 260f);
		float num2 = 0f;
		if (!show.Established && show.EndedAt == 0L)
		{
			if (!show.Burst)
			{
				(float K, float Kv, long VoidAt) tuple = UltraProgress(show, now);
				float item = tuple.K;
				float item2 = tuple.Kv;
				long item3 = tuple.VoidAt;
				num2 = ((now < item3) ? (0.12f + 0.25f * item) : (show.Short ? (0.4f * (1f - item2) + 0.2f) : (0.4f * (1f - item2))));
			}
			else
			{
				long num3 = now - show.BurstAt;
				num2 = ((num3 >= 5000) ? 0f : ((num3 < 1400) ? (0.8f * (1f - EaseOut((float)(num3 - 230) / 1200f)) + 0.3f) : 0.3f));
			}
		}
		if (num2 > 0f && !inAir)
		{
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "ki_charge_sol.png");
			if (texture != null)
			{
				float num4 = 30f + 20f * num2;
				float num5 = 10f + 3f * num2;
				DrawCentered(texture, fx, fy + 1f, num4 * 2f * 64f / 60f, num5 * 2f * 24f / 20f, Tint(C(150, 170, 255), Math.Min(1f, num2 * (1f + 0.8f * num))), GameBlendModes.Add);
			}
		}
		float num6 = BeamWidth(show, now);
		if (!(num6 <= 0.2f))
		{
			float num7 = Graphics.WorldViewport.Y - 8f;
			float num8 = (inAir ? (fy - 24f) : (fy + 2f));
			float num9 = MathF.Sin((float)now / 37f) * 0.4f;
			FillAdd(gameTexture, fx - num6 - 4f + num9, num7, num6 * 2f + 8f, num8 - num7, Tint(UltraViolet, 0.16f));
			FillAdd(gameTexture, fx - num6 - 1f + num9, num7, num6 * 2f + 2f, num8 - num7, Tint(UltraCyan, 0.45f));
			FillAdd(gameTexture, fx - num6 * 0.5f + num9, num7, Math.Max(1f, num6), num8 - num7, Tint(White, 0.9f));
			IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Misc, "ki_halo.png");
			if (texture2 != null)
			{
				DrawCentered(texture2, fx, num8, (num6 * 2f + 12f) * 2f, (5f + num6 * 0.3f) * 2f, Tint(UltraCyan, 0.8f), GameBlendModes.Add);
			}
		}
	}

	private static void DrawUltraFront(Entity entity, Show show, IGameTexture texture, FloatRect source, FloatRect destination, long now)
	{
		show.LastTexture = texture;
		show.LastSource = source;
		show.LastDestination = destination;
		if (!show.Burst && show.EndedAt == 0L && !show.Established && entity.SpriteAnimation == SpriteAnimations.Ki)
		{
			float item = UltraProgress(show, now).K;
			float num = Math.Max(0f, 1f - (float)(now - show.BeatAt) / 260f);
			if (num > 0.15f && (item > 0.2f || show.Short) && !string.IsNullOrEmpty(entity.TransformedSprite))
			{
				string name = Path.GetFileNameWithoutExtension(TextureNameFor(entity.Sprite, entity.TransformedSprite)) + "_ki.png";
				IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Entity, name);
				if (texture2 != null && texture2.Width == texture.Width && texture2.Height == texture.Height)
				{
					Graphics.DrawGameTexture(texture2, source, destination, Tint(White, num));
				}
			}
		}
		float num2 = BeamWidth(show, now);
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (!(num2 <= 0.2f) && gameTexture != null)
		{
			float num3 = MathF.Round(destination.X + destination.Width / 2f);
			float num4 = MathF.Sin((float)now / 37f) * 0.4f;
			float num5 = Math.Min(1f, 8f / Math.Max(num2, 1f));
			float y = destination.Y;
			float num6 = destination.Y + destination.Height;
			FillAdd(gameTexture, num3 - num2 - 1f + num4, y, num2 * 2f + 2f, num6 - y, Tint(UltraCyan, 0.3f * num5));
			FillAdd(gameTexture, num3 - num2 * 0.5f + num4, y, Math.Max(1f, num2), num6 - y, Tint(White, 0.55f * num5));
		}
	}

	private static void DrawUltraGround()
	{
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (UltraRings.Count == 0 || gameTexture == null)
		{
			return;
		}
		long now = Timing.Global.Milliseconds;
		UltraRings.RemoveAll((UltraRing ring) => now - ring.At >= ring.Life);
		foreach (UltraRing ultraRing in UltraRings)
		{
			if (now < ultraRing.At)
			{
				continue;
			}
			(float, float)? tuple = ToWorld(ultraRing.MapId, ultraRing.X, ultraRing.Y);
			if (tuple.HasValue)
			{
				(float, float) valueOrDefault = tuple.GetValueOrDefault();
				float item = valueOrDefault.Item1;
				float item2 = valueOrDefault.Item2;
				float num = (float)(now - ultraRing.At) / (float)ultraRing.Life;
				float num2 = 6f + ultraRing.Speed * (float)(now - ultraRing.At) / 1000f;
				DrawRing(gameTexture, item, item2, num2, num2 * 0.36f, Tint(ultraRing.Color, (ultraRing.Shock ? 1f : 0.7f) * (1f - num)), (!ultraRing.Shock) ? 1 : 2);
				if (ultraRing.Shock)
				{
					DrawRing(gameTexture, item, item2, num2 - 3f, (num2 - 3f) * 0.36f, Tint(UltraCyan, 0.5f * (1f - num)), 1);
				}
			}
		}
	}

	private static void DrawRing(IGameTexture pixel, float x, float y, float rx, float ry, Color color, int size)
	{
		if (color.A > 0 && !(rx < 1f))
		{
			int num = Math.Max(24, (int)(rx * 3f));
			for (int i = 0; i < num; i++)
			{
				float x2 = (float)i / (float)num * (float)Math.PI * 2f;
				Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(x + MathF.Cos(x2) * rx), MathF.Round(y + MathF.Sin(x2) * ry), size, size), color, null, GameBlendModes.Add);
			}
		}
	}

	private static void DrawUltraCrackLight(IGameTexture pixel, long now)
	{
		GlowingCracks.RemoveAll((Mark mark) => now - mark.At >= 6000 || !Marks.Contains(mark));
		foreach (Mark glowingCrack in GlowingCracks)
		{
			(float, float)[] crack = glowingCrack.Crack;
			if (crack == null)
			{
				continue;
			}
			(float, float)? tuple = ToWorld(glowingCrack.MapId, glowingCrack.X, glowingCrack.Y);
			if (!tuple.HasValue)
			{
				continue;
			}
			(float, float) valueOrDefault = tuple.GetValueOrDefault();
			float item = valueOrDefault.Item1;
			float item2 = valueOrDefault.Item2;
			long num = now - glowingCrack.At;
			float num2 = Math.Clamp(1f - (float)(num - 1500) / 2500f, 0f, 1f);
			if (!(num2 <= 0f))
			{
				float num3 = Math.Clamp((float)num / 700f, 0f, 1f);
				int num4 = Math.Max(1, (int)((float)(crack.Length - 1) * num3));
				for (int num5 = 0; num5 < num4; num5++)
				{
					var (num6, num7) = crack[num5];
					var (num8, num9) = crack[num5 + 1];
					Line(pixel, item + num6, item2 + num7, item + num8, item2 + num9, Tint(UltraCyan, 0.9f * num2 * (0.8f + 0.2f * MathF.Sin((float)now / 90f + (float)num5))), GameBlendModes.Add);
					Line(pixel, item + num6, item2 + num7 + 1f, item + num8, item2 + num9 + 1f, Tint(UltraViolet, 0.35f * num2), GameBlendModes.Add);
				}
			}
		}
	}

	private static void BeginUltraFrame()
	{
		_ultraDull = 0f;
		_ultraStars = 0f;
		_pushTarget = 1f;
		_silenceTarget = 1f;
		_impactShow = null;
	}

	private static void EndUltraFrame(float dt)
	{
		_cameraPush += (_pushTarget - _cameraPush) * Math.Min(1f, dt * 6f);
		if (Math.Abs(_cameraPush - _pushTarget) < 0.0005f)
		{
			_cameraPush = _pushTarget;
		}
		Audio.Silence += (_silenceTarget - Audio.Silence) * Math.Min(1f, dt * 8f);
		if (Math.Abs(Audio.Silence - _silenceTarget) < 0.005f)
		{
			Audio.Silence = _silenceTarget;
		}
	}

	private static void DrawUltraMotes(IGameTexture pixel, long now)
	{
		Motes.RemoveAll((Mote mote) => (mote.SuckAt < 0) ? (now - mote.At >= mote.Life) : (now - mote.SuckAt > 360));
		foreach (Mote mote in Motes)
		{
			(float, float)? tuple = ToWorld(mote.MapId, mote.X, mote.Y);
			if (tuple.HasValue)
			{
				(float, float) valueOrDefault = tuple.GetValueOrDefault();
				float item = valueOrDefault.Item1;
				float item2 = valueOrDefault.Item2;
				long num = now - mote.At;
				float x;
				float x2;
				float alpha;
				if (mote.SuckAt >= 0)
				{
					float num2 = EaseOut((float)Math.Max(0L, now - mote.SuckAt) / 340f);
					x = item + (mote.Owner.Fx - item) * num2;
					x2 = item2 + (mote.Owner.Fy - 24f - item2) * num2;
					alpha = 1f;
				}
				else
				{
					float num3 = (float)num / (float)mote.Life;
					x = item + MathF.Sin((float)num / 400f + mote.Phase) * 2f;
					x2 = item2 - mote.Rise * (float)num / 1000f;
					alpha = Math.Min(1f, num3 * 5f) * (1f - num3);
				}
				Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(x), MathF.Round(x2), 1f, 1f), Tint(mote.Color, alpha), null, GameBlendModes.Add);
			}
		}
		Glints.RemoveAll((Glint glint) => now - glint.At >= glint.Life);
		foreach (Glint glint in Glints)
		{
			float num4 = (float)(now - glint.At) / (float)glint.Life;
			int num5 = 1 + (int)MathF.Round(3f * MathF.Sin(num4 * (float)Math.PI));
			for (int num6 = -num5; num6 <= num5; num6++)
			{
				float alpha2 = (1f - num4) * (1f - (float)Math.Abs(num6) / (float)(num5 + 1));
				Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(glint.X + (float)num6), MathF.Round(glint.Y), 1f, 1f), Tint(White, alpha2), null, GameBlendModes.Add);
				Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(glint.X), MathF.Round(glint.Y + (float)num6), 1f, 1f), Tint(White, alpha2), null, GameBlendModes.Add);
			}
		}
	}

	private static void DrawUltraVeil(IGameTexture pixel, FloatRect view)
	{
		long milliseconds = Timing.Global.Milliseconds;
		if (_ultraDull > 0.005f)
		{
			Fill(pixel, view.X, view.Y, view.Width, view.Height, Tint(UltraDull, _ultraDull));
		}
		if (_ultraStars > 0.005f)
		{
			Color[] array = new Color[3] { UltraSilver, UltraCyan, UltraViolet };
			float num = Math.Clamp(_ultraStars, 0f, 1.4f);
			(float, float, float, float, bool, int)[] ultraStars = UltraStars;
			for (int i = 0; i < ultraStars.Length; i++)
			{
				(float, float, float, float, bool, int) tuple = ultraStars[i];
				float num2 = num * (0.35f + 0.65f * MathF.Abs(MathF.Sin((float)milliseconds / 1000f * tuple.Item4 + tuple.Item3)));
				float num3 = MathF.Round(view.X + tuple.Item1 * view.Width);
				float num4 = MathF.Round(view.Y + tuple.Item2 * view.Height);
				Color color = array[tuple.Item6];
				FillAdd(pixel, num3, num4, 1f, 1f, Tint(color, Math.Min(1f, num2)));
				if (tuple.Item5 && num2 > 0.6f)
				{
					Color color2 = Tint(color, Math.Min(1f, num2 * 0.5f));
					FillAdd(pixel, num3 - 1f, num4, 1f, 1f, color2);
					FillAdd(pixel, num3 + 1f, num4, 1f, 1f, color2);
					FillAdd(pixel, num3, num4 - 1f, 1f, 1f, color2);
					FillAdd(pixel, num3, num4 + 1f, 1f, 1f, color2);
				}
			}
		}
		Show impactShow = _impactShow;
		if (impactShow == null || impactShow.EndedAt != 0L)
		{
			return;
		}
		long num5 = milliseconds - impactShow.BurstAt;
		if (num5 < 0 || num5 >= 230)
		{
			return;
		}
		bool flag = num5 < 115;
		Fill(pixel, view.X, view.Y, view.Width, view.Height, flag ? C(244, 246, 255) : C(5, 6, 12));
		Color color3 = (flag ? C(8, 8, 16) : White);
		if (flag)
		{
			Fill(pixel, impactShow.Fx - 3f, view.Y, 6f, impactShow.Fy + 2f - view.Y, color3);
		}
		IGameTexture lastTexture = impactShow.LastTexture;
		if (lastTexture != null)
		{
			IGameTexture gameTexture = KiCharge.MaskOf(lastTexture, impactShow.LastSource, 0);
			if (gameTexture != null)
			{
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, gameTexture.Width, gameTexture.Height), impactShow.LastDestination, color3);
			}
		}
		if (!flag)
		{
			DrawRing(pixel, impactShow.Fx, impactShow.Fy, 22f, 8f, UltraCyan, 1);
			DrawRing(pixel, impactShow.Fx, impactShow.Fy, 34f, 12f, Tint(UltraViolet, 0.8f), 1);
		}
	}
}
