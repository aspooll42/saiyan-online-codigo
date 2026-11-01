using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Core.Sounds;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Maps;
using Intersect.Config;
using Intersect.Enums;
using Intersect.Framework.Core;

namespace Intersect.Client.Entities;

public static class KiBeam
{
	private sealed class Beam
	{
		public BeamPhase Phase;

		public float Angle;

		public float AngleGoal;

		public Direction Direction;

		public int Level;

		public long ChargeStartedAt;

		public long FiredAt;

		public long StoppedAt;

		public Guid TargetId;

		public float ReachTiles;

		public float Head;

		public float HeadMax;

		public float Tail;

		public long UpdatedAt;

		public int ReachedLevel = -1;

		public int Syllable = -1;

		public bool Exploded;

		public long DustAt;

		public long SparksAt;

		public int Seed;

		public bool Confirmed = true;

		public MapSound? ChargeSound;

		public MapSound? ShoutSound;

		public float Courbe;

		public float CourbeGoal;

		public void SetAngle(float degrees)
		{
			Angle = Normalize(degrees);
			Direction = Cardinal(Angle);
		}
	}

	private sealed class FadingSound
	{
		public required MapSound Sound;

		public long At;

		public long Ms;
	}

	private enum ScarKind
	{
		Trail,
		Crater,
		Cracks
	}

	private sealed class Scar
	{
		public ScarKind Kind;

		public float X;

		public float Y;

		public float Angle;

		public float Length;

		public float K;

		public int Level;

		public long At;

		public int Seed;
	}

	private sealed class Wave
	{
		public float X;

		public float Y;

		public int Level;

		public long At;

		public bool Explosion;

		public float BurstX;

		public float BurstY;
	}

	private sealed class Mote
	{
		public float X;

		public float Y;

		public float VX;

		public float VY;

		public long At;

		public long Life;

		public Color Color;

		public bool Falls;
	}

	private static readonly Color Outer = new Color(255, 47, 123, 255);

	private static readonly Color Mid = new Color(255, 108, 203, 255);

	private static readonly Color Light = new Color(255, 207, 244, 255);

	private static readonly Color Core = new Color(255, 255, 255, 255);

	private const long FrameMs = 50L;

	private const long PushFrameMs = 70L;

	private const long RecoverMs = 300L;

	private const string ChargeSoundFile = "kamehameha_charge.wav";

	private const string ShoutSoundFile = "kamehameha_ha.wav";

	private const int HearingTiles = 14;

	private const long ChargeCutMs = 60L;

	private const long ChargeCancelMs = 180L;

	private const long ShoutFadeMs = 450L;

	private const long ScarMs = 5000L;

	private const long ScarFadeMs = 1500L;

	private const float RemoteTurnDegreesPerSecond = 720f;

	private static readonly string[] Syllables = new string[4] { "KA…", "ME…", "HA…", "ME…" };

	private static readonly Dictionary<Guid, Beam> Beams = new Dictionary<Guid, Beam>();

	private static readonly List<Scar> Scars = new List<Scar>();

	private static readonly List<Wave> Waves = new List<Wave>();

	private static readonly List<Mote> Motes = new List<Mote>();

	private static readonly List<FadingSound> FadingSounds = new List<FadingSound>();

	private static BeamOptions Settings => Options.Instance.Beam;

	public static bool IsActive(Entity entity)
	{
		return Beams.ContainsKey(entity.Id);
	}

	public static BeamPhase PhaseOf(Entity entity)
	{
		if (!Beams.TryGetValue(entity.Id, out Beam value))
		{
			return BeamPhase.None;
		}
		return value.Phase;
	}

	public static Direction DirectionOf(Entity entity)
	{
		if (!Beams.TryGetValue(entity.Id, out Beam value))
		{
			return entity.DirectionFacing;
		}
		return value.Direction;
	}

	public static float AngleOf(Entity entity)
	{
		if (!Beams.TryGetValue(entity.Id, out Beam value))
		{
			return AngleOf(entity.DirectionFacing);
		}
		return value.Angle;
	}

	public static long ChargedFor(Entity entity)
	{
		if (!Beams.TryGetValue(entity.Id, out Beam value) || value.Phase != BeamPhase.Charging)
		{
			return 0L;
		}
		return Timing.Global.Milliseconds - value.ChargeStartedAt;
	}

	public static float AngleOf(Direction direction)
	{
		return direction switch
		{
			Direction.Up => -90f, 
			Direction.Down => 90f, 
			Direction.Left => 180f, 
			Direction.UpLeft => -135f, 
			Direction.UpRight => -45f, 
			Direction.DownLeft => 135f, 
			Direction.DownRight => 45f, 
			_ => 0f, 
		};
	}

	public static Direction Cardinal(float degrees)
	{
		var (num, num2) = AxisOf(degrees);
		if (!(MathF.Abs(num) >= MathF.Abs(num2)))
		{
			if (!(num2 >= 0f))
			{
				return Direction.Up;
			}
			return Direction.Down;
		}
		if (!(num >= 0f))
		{
			return Direction.Left;
		}
		return Direction.Right;
	}

	public static float Normalize(float degrees)
	{
		if (!float.IsFinite(degrees))
		{
			return 0f;
		}
		degrees %= 360f;
		if (degrees > 180f)
		{
			degrees -= 360f;
		}
		else if (degrees <= -180f)
		{
			degrees += 360f;
		}
		return degrees;
	}

	public static float DeltaAngle(float from, float to)
	{
		return Normalize(to - from);
	}

	public static void StartCharge(Entity entity, float angle)
	{
		long milliseconds = Timing.Global.Milliseconds;
		if (Beams.TryGetValue(entity.Id, out Beam value))
		{
			Silence(value, milliseconds, 180L);
		}
		Beam beam = new Beam
		{
			Phase = BeamPhase.Charging,
			ChargeStartedAt = milliseconds,
			UpdatedAt = milliseconds,
			Seed = (int)(milliseconds % 100000),
			ChargeSound = Audio.AddMapSound("kamehameha_charge.wav", entity.X, entity.Y, entity.MapId, loop: false, 0, 14, entity)
		};
		beam.SetAngle(angle);
		beam.AngleGoal = beam.Angle;
		Beams[entity.Id] = beam;
	}

	public static void Cancel(Entity entity)
	{
		if (Beams.Remove(entity.Id, out Beam value))
		{
			Silence(value, Timing.Global.Milliseconds, 180L);
		}
	}

	public static void Aim(Entity entity, float angle)
	{
		if (Beams.TryGetValue(entity.Id, out Beam value) && value.Phase == BeamPhase.Charging)
		{
			value.SetAngle(angle);
			value.AngleGoal = value.Angle;
		}
	}

	public static void PredictFire(Entity entity)
	{
		if (Beams.TryGetValue(entity.Id, out Beam value) && value.Phase == BeamPhase.Charging)
		{
			long milliseconds = Timing.Global.Milliseconds;
			int num = Settings.LevelFor(milliseconds - value.ChargeStartedAt);
			Fire(entity, value, num, Guid.Empty, Settings.RangeTiles[num], milliseconds);
			value.Confirmed = false;
		}
	}

	public static void OnServerState(Entity entity, BeamPhase phase, float angle, int level, Guid targetId, float reachTiles, float courbe = 0f)
	{
		courbe = (float.IsFinite(courbe) ? courbe : 0f);
		long milliseconds = Timing.Global.Milliseconds;
		Beams.TryGetValue(entity.Id, out Beam value);
		level = Math.Clamp(level, 0, Settings.LevelCount - 1);
		angle = Normalize(angle);
		switch (phase)
		{
		case BeamPhase.None:
			if (value != null)
			{
				if (value.Phase == BeamPhase.Firing)
				{
					Stop(entity, value, milliseconds);
					break;
				}
				Beams.Remove(entity.Id);
				Silence(value, milliseconds, 180L);
			}
			break;
		case BeamPhase.Charging:
			if (value != null && value.Phase == BeamPhase.Charging)
			{
				if (entity != Globals.Me)
				{
					value.AngleGoal = angle;
				}
			}
			else
			{
				StartCharge(entity, angle);
			}
			break;
		case BeamPhase.Firing:
			if (value == null || value.Phase != BeamPhase.Firing || value.StoppedAt > 0)
			{
				Silence(value, milliseconds, 60L);
				value = new Beam
				{
					Phase = BeamPhase.Charging,
					ChargeStartedAt = milliseconds,
					UpdatedAt = milliseconds
				};
				value.SetAngle(angle);
				value.AngleGoal = value.Angle;
				Beams[entity.Id] = value;
				Fire(entity, value, level, targetId, reachTiles, milliseconds);
				value.Courbe = (value.CourbeGoal = courbe);
			}
			else
			{
				value.Level = level;
				value.TargetId = targetId;
				value.ReachTiles = MathF.Max(0f, reachTiles);
				value.Confirmed = true;
				if (entity != Globals.Me)
				{
					value.CourbeGoal = courbe;
				}
			}
			break;
		}
	}

	private static void Fire(Entity entity, Beam beam, int level, Guid targetId, float reachTiles, long now)
	{
		beam.Phase = BeamPhase.Firing;
		beam.Level = level;
		beam.FiredAt = now;
		beam.UpdatedAt = now;
		beam.TargetId = targetId;
		beam.ReachTiles = MathF.Max(0f, reachTiles);
		beam.Head = 0f;
		beam.HeadMax = 0f;
		beam.Tail = 0f;
		beam.Courbe = 0f;
		beam.CourbeGoal = 0f;
		beam.Seed = (int)(now % 100000);
		Silence(beam, now, 60L);
		beam.ShoutSound = Audio.AddMapSound("kamehameha_ha.wav", entity.X, entity.Y, entity.MapId, loop: false, 0, 14, entity);
		CombatFeedback.Callout(entity.Id, "HA !", new Color(255, 234, 249, 255), 20);
		FloatRect worldPos = entity.WorldPos;
		Scars.Add(new Scar
		{
			Kind = ScarKind.Cracks,
			X = worldPos.X + worldPos.Width / 2f,
			Y = worldPos.Bottom - 2f,
			Level = level,
			At = now,
			Seed = beam.Seed
		});
		Trim(Scars, 40);
		if (entity == Globals.Me)
		{
			var (dirX, dirY) = AxisOf(beam.Angle);
			CombatFeedback.StartShake(1f + (float)level, 240L, dirX, dirY);
			if (level >= 2)
			{
				CombatFeedback.PunchZoom(0.06f, 360L);
			}
		}
	}

	private static void Stop(Entity entity, Beam beam, long now)
	{
		if (beam.StoppedAt <= 0)
		{
			beam.StoppedAt = now;
			Silence(beam, now, 450L);
			float item = Hand(entity, beam, firing: true, now).X;
			Scars.Add(new Scar
			{
				Kind = ScarKind.Trail,
				X = item,
				Y = entity.WorldPos.Bottom - 2f,
				Angle = beam.Angle,
				Length = beam.HeadMax,
				K = Courbure(beam),
				Level = beam.Level,
				At = now,
				Seed = beam.Seed
			});
			Trim(Scars, 40);
		}
	}

	private static void Silence(Beam? beam, long now, long ms)
	{
		if (beam != null)
		{
			FadeOut(beam.ChargeSound);
			FadeOut(beam.ShoutSound);
			beam.ChargeSound = null;
			beam.ShoutSound = null;
		}
		void FadeOut(MapSound? sound)
		{
			if (sound != null && sound.Loaded)
			{
				FadingSounds.Add(new FadingSound
				{
					Sound = sound,
					At = now,
					Ms = Math.Max(1L, ms)
				});
			}
		}
	}

	private static void AdvanceSounds(long now)
	{
		for (int num = FadingSounds.Count - 1; num >= 0; num--)
		{
			FadingSound fadingSound = FadingSounds[num];
			float num2 = (float)(now - fadingSound.At) / (float)fadingSound.Ms;
			if (num2 >= 1f || !fadingSound.Sound.Loaded)
			{
				fadingSound.Sound.Stop();
				FadingSounds.RemoveAt(num);
			}
			else
			{
				fadingSound.Sound.Fade = 1f - num2;
			}
		}
	}

	public static bool TryPose(Entity entity, int row, ref IGameTexture texture, ref FloatRect source)
	{
		if (!Beams.TryGetValue(entity.Id, out Beam value))
		{
			return false;
		}
		row = value.Direction switch
		{
			Direction.Down => 0, 
			Direction.Left => 1, 
			Direction.Right => 2, 
			_ => 3, 
		};
		long milliseconds = Timing.Global.Milliseconds;
		int val = ((value.Phase != BeamPhase.Charging) ? ((value.StoppedAt != 0L && milliseconds - value.StoppedAt >= Settings.FadeMs) ? 3 : (1 + (int)((milliseconds - value.FiredAt) / 70 % 2))) : 0);
		int columns;
		IGameTexture gameTexture = PoseSheet(entity, out columns);
		if (gameTexture == null)
		{
			return false;
		}
		int num = gameTexture.Height / Math.Max(1, Options.Instance.Sprites.Directions);
		int num2 = gameTexture.Width / columns;
		val = Math.Min(val, columns - 1);
		texture = gameTexture;
		source = new FloatRect(val * num2, row * num, num2, num);
		return true;
	}

	public static FloatRect Pose(Entity entity, FloatRect rectangle)
	{
		if (!Beams.TryGetValue(entity.Id, out Beam value) || value.Phase != BeamPhase.Firing)
		{
			return rectangle;
		}
		float num = MathF.Round(Recoil(value, Timing.Global.Milliseconds));
		if (num <= 0f)
		{
			return rectangle;
		}
		var (num2, num3) = AxisOf(value.Angle);
		return new FloatRect(rectangle.X - MathF.Round(num2 * num), rectangle.Y - MathF.Round(num3 * num), rectangle.Width, rectangle.Height);
	}

	private static float Recoil(Beam beam, long now)
	{
		if (Heritage.Actif && Heritage.Rayon.SansReculDessine)
		{
			return 0f;
		}
		float x = 2f + (float)beam.Level * 2f;
		if (beam.StoppedAt == 0L)
		{
			return MathF.Min(x, (float)(now - beam.FiredAt) / 350f);
		}
		float num = MathF.Min(x, (float)(beam.StoppedAt - beam.FiredAt) / 350f);
		return num * (1f - Math.Clamp((float)(now - beam.StoppedAt) / 300f, 0f, 1f));
	}

	private static IGameTexture? PoseSheet(Entity entity, out int columns)
	{
		columns = 1;
		string text = (string.IsNullOrWhiteSpace(entity.TransformedSprite) ? entity.Sprite : Transformation.TextureNameFor(entity.Sprite, entity.TransformedSprite));
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text);
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Entity, fileNameWithoutExtension + "_kamehameha.png");
		if (texture != null)
		{
			columns = 4;
			return texture;
		}
		IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Entity, fileNameWithoutExtension + "_spell.png");
		if (texture2 == null)
		{
			return null;
		}
		int num = texture2.Height / Math.Max(1, Options.Instance.Sprites.Directions);
		columns = Math.Max(1, texture2.Width / Math.Max(1, num * 2 / 3));
		return texture2;
	}

	private static (float Cos, float Sin) AxisOf(float degrees)
	{
		degrees = Normalize(degrees);
		if (MathF.Abs(degrees) < 0.05f)
		{
			return (Cos: 1f, Sin: 0f);
		}
		if (MathF.Abs(degrees - 90f) < 0.05f)
		{
			return (Cos: 0f, Sin: 1f);
		}
		if (MathF.Abs(degrees + 90f) < 0.05f)
		{
			return (Cos: 0f, Sin: -1f);
		}
		if (MathF.Abs(MathF.Abs(degrees) - 180f) < 0.05f)
		{
			return (Cos: -1f, Sin: 0f);
		}
		float x = degrees * (float)Math.PI / 180f;
		return (Cos: MathF.Cos(x), Sin: MathF.Sin(x));
	}

	private static (float X, float Y) Hand(Entity entity, Beam beam, bool firing, long now)
	{
		FloatRect worldPos = entity.WorldPos;
		float num = worldPos.Width / 32f;
		float num2 = worldPos.Height / 48f;
		var (num3, num4) = AxisOf(beam.Angle);
		float num5;
		float num6;
		if (!firing)
		{
			(num5, num6) = beam.Direction switch
			{
				Direction.Right => (5f, 35f), 
				Direction.Left => (26f, 35f), 
				Direction.Down => (12f, 35f), 
				_ => (16f, 33f), 
			};
		}
		else
		{
			num5 = 15.5f + num3 * 13.5f;
			num6 = 31f + num4 * ((num4 > 0f) ? 7f : 17f);
		}
		float num7 = ((beam.Phase == BeamPhase.Firing) ? MathF.Round(Recoil(beam, now)) : 0f);
		return (X: MathF.Round(worldPos.X + num5 * num - num3 * num7), Y: MathF.Round(worldPos.Y + num6 * num2 - num4 * num7));
	}

	private static float MaxLength(Entity entity, Beam beam, float ox, float oy)
	{
		int tileWidth = Options.Instance.Map.TileWidth;
		int tileHeight = Options.Instance.Map.TileHeight;
		FloatRect worldPos = entity.WorldPos;
		float num = worldPos.X + worldPos.Width / 2f;
		float num2 = worldPos.Bottom - (float)tileHeight / 2f;
		(float Cos, float Sin) tuple = AxisOf(beam.Angle);
		float item = tuple.Cos;
		float item2 = tuple.Sin;
		float num3 = MathF.Sqrt(item * (float)tileWidth * item * (float)tileWidth + item2 * (float)tileHeight * item2 * (float)tileHeight);
		float y = (num - ox) * item + (num2 - oy) * item2 + (beam.ReachTiles + 0.5f) * num3 - 4f;
		return MathF.Max(0f, y);
	}

	private static float DistanceTo(Beam beam, Entity target, float ox, float oy)
	{
		FloatRect worldPos = target.WorldPos;
		(float Cos, float Sin) tuple = AxisOf(beam.Angle);
		float item = tuple.Cos;
		float item2 = tuple.Sin;
		float num = worldPos.X + worldPos.Width * 0.5f;
		float num2 = worldPos.Y + worldPos.Height * 0.65f;
		float num3 = Courbure(beam);
		if (num3 != 0f)
		{
			float item3 = ArcDuRayon.Projeter(ox, oy, item, item2, num3, num, num2).Le;
			(float X, float Y, float Cos, float Sin) tuple2 = ArcDuRayon.Point(ox, oy, item, item2, num3, item3);
			float item4 = tuple2.Cos;
			float item5 = tuple2.Sin;
			return item3 - (MathF.Abs(item4) * worldPos.Width * 0.25f + MathF.Abs(item5) * worldPos.Height * 0.2f);
		}
		return (num - ox) * item + (num2 - oy) * item2 - (MathF.Abs(item) * worldPos.Width * 0.25f + MathF.Abs(item2) * worldPos.Height * 0.2f);
	}

	private static float Courbure(Beam beam)
	{
		if (beam.Phase != BeamPhase.Firing)
		{
			return 0f;
		}
		return Settings.Courbure(beam.Courbe, beam.Level, Options.Instance.Map.TileWidth);
	}

	public static float CourbeOf(Entity entity)
	{
		if (!Beams.TryGetValue(entity.Id, out Beam value))
		{
			return 0f;
		}
		return value.Courbe;
	}

	public static void Courber(Entity entity, float degres)
	{
		if (Beams.TryGetValue(entity.Id, out Beam value) && value.Phase == BeamPhase.Firing && value.StoppedAt == 0L && float.IsFinite(degres))
		{
			value.Courbe = (value.CourbeGoal = degres);
		}
	}

	public static (float X, float Y, float Portee)? OrigineDuTir(Entity entity)
	{
		if (!Beams.TryGetValue(entity.Id, out Beam value) || value.Phase != BeamPhase.Firing || value.StoppedAt > 0)
		{
			return null;
		}
		var (item, item2) = Hand(entity, value, firing: true, Timing.Global.Milliseconds);
		return (item, item2, Settings.RangeTiles[Math.Clamp(value.Level, 0, Settings.RangeTiles.Length - 1)] * Options.Instance.Map.TileWidth);
	}

	private static void Advance(long now)
	{
		AdvanceSounds(now);
		KeyValuePair<Guid, Beam>[] array = Beams.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			KeyValuePair<Guid, Beam> keyValuePair = array[i];
			var (guid2, beam2) = keyValuePair;
			if (!Globals.Entities.TryGetValue(guid2, out Entity value) || value == null || value.IsDisposed)
			{
				Beams.Remove(guid2);
				Silence(beam2, now, 180L);
				continue;
			}
			long num = Math.Clamp(now - beam2.UpdatedAt, 0L, 100L);
			beam2.UpdatedAt = now;
			beam2.ChargeSound?.UpdatePosition(value.X, value.Y, value.MapId);
			beam2.ShoutSound?.UpdatePosition(value.X, value.Y, value.MapId);
			if (beam2.Phase == BeamPhase.Charging)
			{
				float num2 = DeltaAngle(beam2.Angle, beam2.AngleGoal);
				if (num2 != 0f)
				{
					float num3 = 720f * (float)num / 1000f;
					beam2.SetAngle(beam2.Angle + Math.Clamp(num2, 0f - num3, num3));
				}
				AdvanceCharge(value, beam2, now);
				continue;
			}
			if (beam2.StoppedAt > 0 && now - beam2.StoppedAt >= Math.Max(Settings.FadeMs, 300L))
			{
				Beams.Remove(guid2);
				continue;
			}
			if (beam2.Courbe != beam2.CourbeGoal)
			{
				float num4 = Settings.CourbeDegresParSeconde * 2f * (float)num / 1000f;
				beam2.Courbe += Math.Clamp(beam2.CourbeGoal - beam2.Courbe, 0f - num4, num4);
			}
			int num5 = Math.Clamp(beam2.Level, 0, Settings.LevelCount - 1);
			if (beam2.StoppedAt == 0L && now - beam2.FiredAt > Settings.TravelMs(Settings.RangeTiles[num5]) + Settings.HoldMs[num5] + 1500)
			{
				Stop(value, beam2, now);
			}
			(float, float)? tuple = ((beam2.StoppedAt == 0L) ? Clash.BouleDeChoc(guid2) : (((float, float)?)null));
			if (tuple.HasValue)
			{
				(float, float) valueOrDefault = tuple.GetValueOrDefault();
				beam2.Courbe = (beam2.CourbeGoal = 0f);
				var (num6, num7) = Hand(value, beam2, firing: true, now);
				if (MathF.Abs(valueOrDefault.Item1 - num6) + MathF.Abs(valueOrDefault.Item2 - num7) > 0.5f)
				{
					beam2.SetAngle(MathF.Atan2(valueOrDefault.Item2 - num7, valueOrDefault.Item1 - num6) * 180f / (float)Math.PI);
				}
			}
			var (num8, num9) = Hand(value, beam2, firing: true, now);
			float num10;
			if (tuple.HasValue)
			{
				(float, float) valueOrDefault2 = tuple.GetValueOrDefault();
				num10 = MathF.Sqrt((valueOrDefault2.Item1 - num8) * (valueOrDefault2.Item1 - num8) + (valueOrDefault2.Item2 - num9) * (valueOrDefault2.Item2 - num9));
			}
			else
			{
				num10 = MaxLength(value, beam2, num8, num9);
			}
			float num11 = num10;
			float num12 = num11;
			Entity entity = null;
			if (beam2.TargetId != Guid.Empty && Globals.Entities.TryGetValue(beam2.TargetId, out Entity value2) && value2 != null && !value2.IsDisposed)
			{
				entity = value2;
				num12 = Math.Clamp(DistanceTo(beam2, value2, num8, num9), 0f, num11);
			}
			bool flag = beam2.Confirmed || now - beam2.FiredAt >= 600;
			if (!flag)
			{
				num12 = MathF.Min(num12, (float)Options.Instance.Map.TileWidth * 1.5f);
			}
			if (beam2.StoppedAt == 0L)
			{
				float num13 = Settings.HeadTilesPerSecond * (float)Options.Instance.Map.TileWidth / 1000f;
				beam2.Head = MathF.Min(num12, beam2.Head + num13 * (float)num);
			}
			beam2.Head = MathF.Min(beam2.Head, num12);
			if (flag)
			{
				beam2.HeadMax = MathF.Max(beam2.HeadMax, beam2.Head);
			}
			if (beam2.StoppedAt > 0)
			{
				float num14 = Math.Clamp((float)(now - beam2.StoppedAt) / (float)Math.Max(1, Settings.FadeMs), 0f, 1f);
				beam2.Tail = beam2.HeadMax * num14 * num14;
				continue;
			}
			(float Cos, float Sin) tuple4 = AxisOf(beam2.Angle);
			float item = tuple4.Cos;
			float item2 = tuple4.Sin;
			(float X, float Y, float Cos, float Sin) tuple5 = ArcDuRayon.Point(num8, num9, item, item2, Courbure(beam2), beam2.Head);
			float item3 = tuple5.X;
			float item4 = tuple5.Y;
			float item5 = tuple5.Cos;
			float item6 = tuple5.Sin;
			bool flag2 = beam2.ReachTiles < (float)Settings.RangeTiles[num5] - 0.05f;
			if (flag && !beam2.Exploded && entity == null && !flag2 && beam2.Level >= 1 && num11 > 8f && beam2.Head >= num11 - 0.5f)
			{
				beam2.Exploded = true;
				float num15 = value.WorldPos.Bottom - 2f + (item4 - num9);
				Waves.Add(new Wave
				{
					X = item3,
					Y = num15,
					Level = beam2.Level,
					At = now,
					Explosion = true,
					BurstX = item3,
					BurstY = item4
				});
				if (!Meteo.GeyserDeRayon(item3, num15, beam2.Level))
				{
					Scars.Add(new Scar
					{
						Kind = ScarKind.Crater,
						X = item3,
						Y = num15,
						Level = beam2.Level,
						At = now,
						Seed = beam2.Seed
					});
					Trim(Scars, 40);
				}
				for (int j = 0; j < 16; j++)
				{
					float x = Rand(j, beam2.Seed) * (float)Math.PI * 2f;
					float num16 = 0.03f + Rand(beam2.Seed, j) * 0.07f;
					float x2 = item3;
					float y = num15 - 2f;
					float vx = MathF.Cos(x) * num16;
					float vy = MathF.Sin(x) * num16 * 0.5f - 0.05f;
					long life = 420 + (long)(Rand(j, 3) * 200f);
					AddMote(x2, y, vx, vy, life, (j % 3) switch
					{
						0 => new Color(255, 143, 122, 82), 
						1 => new Color(255, 107, 143, 42), 
						_ => new Color(255, 201, 177, 132), 
					}, falls: true, now);
				}
			}
			if ((entity != null || (flag && flag2)) && beam2.Head >= num12 - 0.5f && now - beam2.SparksAt >= Settings.TickMs)
			{
				beam2.SparksAt = now;
				for (int k = 0; k < 5; k++)
				{
					float num17 = (Rand(k, (int)(now % 9973)) - 0.5f) * 0.08f;
					float num18 = 0.02f + Rand((int)(now % 7919), k) * 0.05f;
					AddMote(item3, item4, (0f - item5) * num18 - item6 * num17, (0f - item6) * num18 + item5 * num17, 260L, (k % 2 == 0) ? Core : Light, falls: false, now);
				}
			}
			if (now - beam2.DustAt >= 90)
			{
				beam2.DustAt = now;
				FloatRect worldPos = value.WorldPos;
				AddMote(worldPos.X + worldPos.Width / 2f - item * 6f + (Rand((int)(now % 7907), 1) - 0.5f) * 12f * MathF.Abs(item2), worldPos.Bottom - 2f, (0f - item) * 0.03f, -0.02f - item2 * 0.02f, 300L, new Color(255, 201, 177, 132), falls: false, now);
			}
		}
	}

	private static void AdvanceCharge(Entity entity, Beam beam, long now)
	{
		long num = now - beam.ChargeStartedAt;
		int num2 = Settings.LevelFor(num);
		if (num2 > beam.ReachedLevel)
		{
			beam.ReachedLevel = num2;
			if (num2 > 0)
			{
				FloatRect worldPos = entity.WorldPos;
				Waves.Add(new Wave
				{
					X = worldPos.X + worldPos.Width / 2f,
					Y = worldPos.Bottom - 1f,
					Level = num2,
					At = now
				});
			}
		}
		int num3 = (int)Math.Min(Syllables.Length - 1, num / Math.Max(1, Settings.DepartForce / Syllables.Length));
		if (num3 != beam.Syllable)
		{
			beam.Syllable = num3;
			CombatFeedback.Callout(entity.Id, Syllables[num3], new Color(255, 234, 249, 255), 14);
		}
		if (Heritage.Actif)
		{
			Heritage.Rayon.SuivreLaCharge(entity, num);
		}
		if (num > Settings.DepartForce + 2000)
		{
			Beams.Remove(entity.Id);
			Silence(beam, now, 180L);
		}
	}

	private static void AddMote(float x, float y, float vx, float vy, long life, Color color, bool falls, long now)
	{
		Motes.Add(new Mote
		{
			X = x,
			Y = y,
			VX = vx,
			VY = vy,
			Life = life,
			Color = color,
			Falls = falls,
			At = now
		});
		Trim(Motes, 220);
	}

	private static void Trim<T>(List<T> list, int most)
	{
		if (list.Count > most)
		{
			list.RemoveRange(0, list.Count - most);
		}
	}

	public static void DrawGround()
	{
		long now = Timing.Global.Milliseconds;
		Advance(now);
		if (Graphics.Renderer?.WhitePixel == null)
		{
			return;
		}
		Scars.RemoveAll((Scar scar) => now - scar.At >= 5000);
		foreach (Scar scar in Scars)
		{
			DrawScar(scar, now);
		}
		Guid key;
		Beam value;
		foreach (KeyValuePair<Guid, Beam> beam3 in Beams)
		{
			beam3.Deconstruct(out key, out value);
			Guid key2 = key;
			Beam beam = value;
			if (beam.Phase == BeamPhase.Charging && Globals.Entities.TryGetValue(key2, out Entity value2) && value2 != null)
			{
				DrawChargeRing(value2, beam, now);
			}
		}
		Waves.RemoveAll((Wave wave) => now - wave.At >= 520);
		foreach (Wave wave in Waves)
		{
			DrawWaveRing(wave, now);
		}
		foreach (KeyValuePair<Guid, Beam> beam4 in Beams)
		{
			beam4.Deconstruct(out key, out value);
			Guid key3 = key;
			Beam beam2 = value;
			if (beam2.Phase == BeamPhase.Firing && !(beam2.Head <= beam2.Tail + 0.5f) && Globals.Entities.TryGetValue(key3, out Entity value3) && value3 != null)
			{
				float item = Hand(value3, beam2, firing: true, now).X;
				(float Cos, float Sin) tuple = AxisOf(beam2.Angle);
				float item2 = tuple.Cos;
				float item3 = tuple.Sin;
				float force = ((beam2.StoppedAt > 0) ? (1f - Math.Clamp((float)(now - beam2.StoppedAt) / (float)Math.Max(1, Settings.FadeMs), 0f, 1f)) : 1f);
				Meteo.RayonSurLEau(beam2, item, value3.WorldPos.Bottom - 2f, item2, item3, MathF.Max(6f, beam2.Tail), beam2.Head, 7f + 4f * (float)Math.Clamp(beam2.Level, 0, 2), Light, force, Courbure(beam2));
			}
		}
		Meteo.DessinerTracesDesRayons();
	}

	public static void Draw()
	{
		if (Graphics.Renderer?.WhitePixel == null)
		{
			return;
		}
		long now = Timing.Global.Milliseconds;
		foreach (var (key, beam2) in Beams)
		{
			if (Globals.Entities.TryGetValue(key, out Entity value) && value != null)
			{
				if (beam2.Phase == BeamPhase.Charging)
				{
					DrawChargeBall(value, beam2, now);
				}
				else
				{
					DrawBeam(value, beam2, now);
				}
			}
		}
		Motes.RemoveAll((Mote mote) => now - mote.At >= mote.Life);
		foreach (Mote mote in Motes)
		{
			long num = now - mote.At;
			int a = (int)((float)(int)mote.Color.A * 0.9f * (1f - (float)num / (float)mote.Life));
			float x = mote.Y + mote.VY * (float)num + (mote.Falls ? (0.00025f * (float)num * (float)num) : 0f);
			Rect(MathF.Round(mote.X + mote.VX * (float)num), MathF.Round(x), 1f, 1f, new Color(a, mote.Color.R, mote.Color.G, mote.Color.B));
		}
	}

	public static void DrawAir()
	{
		if (Graphics.Renderer?.WhitePixel == null || Waves.Count == 0)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		foreach (Wave wave in Waves)
		{
			if (wave.Explosion)
			{
				DrawExplosionTongues(wave, milliseconds);
			}
		}
	}

	private static void DrawChargeBall(Entity entity, Beam beam, long now)
	{
		float num = Math.Clamp((float)(now - beam.ChargeStartedAt) / (float)Math.Max(1, Settings.DernierPalier), 0f, 1f);
		(float X, float Y) tuple = Hand(entity, beam, firing: false, now);
		float item = tuple.X;
		float item2 = tuple.Y;
		int num2 = (int)(now / 50);
		float num3 = 1.5f + 4.5f * num;
		for (int i = 0; i < 7; i++)
		{
			long num4 = now + i * 97;
			float num5 = (float)(num4 % 320) / 320f;
			float x = Rand(i, (int)(num4 / 320)) * (float)Math.PI * 2f;
			float num6 = (1f - num5) * (14f + 8f * num) + num3;
			Rect(MathF.Round(item + MathF.Cos(x) * num6), MathF.Round(item2 + MathF.Sin(x) * num6), (!(num5 > 0.5f)) ? 1 : 2, 1f, (i % 2 == 0) ? Mid : Light);
		}
		for (int j = 0; j < 5; j++)
		{
			Tongue(item, item2, Rand(j, num2) * (float)Math.PI * 2f, num3 + 2f + Rand(num2, j) * (3f + 4f * num), 1.2f + num, Outer);
		}
		Disc(item, item2, num3 + 1f, Outer);
		Disc(item, item2, num3 * 0.72f + (float)(num2 % 2) * 0.4f, Mid);
		Disc(item, item2, num3 * 0.45f, Light);
		Disc(item, item2, num3 * 0.2f, Core);
	}

	private static void DrawChargeRing(Entity entity, Beam beam, long now)
	{
		if (Heritage.Actif && Heritage.Rayon.SansAnneau)
		{
			return;
		}
		long num = now - beam.ChargeStartedAt;
		float num2 = Math.Clamp((float)num / (float)Math.Max(1, Settings.DernierPalier), 0f, 1f);
		FloatRect worldPos = entity.WorldPos;
		float num3 = worldPos.X + worldPos.Width / 2f;
		float num4 = worldPos.Bottom - 1f;
		bool flag = num2 >= 1f && now / 120 % 2 == 0;
		for (int i = 0; i < 54; i++)
		{
			if (i % 18 != 17)
			{
				float x = (float)Math.PI / 2f + (float)i / 54f * (float)Math.PI * 2f;
				bool flag2 = (float)(i + 1) / 54f <= num2 + 0.001f;
				Rect(MathF.Round(num3 + MathF.Cos(x) * 14f), MathF.Round(num4 + MathF.Sin(x) * 5f), 1f, 1f, (!flag2) ? new Color(128, 8, 16, 34) : (flag ? Core : Light));
			}
		}
		int num5 = Settings.LevelFor(num);
		(float Cos, float Sin) tuple = AxisOf(beam.Angle);
		float item = tuple.Cos;
		float item2 = tuple.Sin;
		Beam beam2 = new Beam
		{
			Phase = BeamPhase.Charging,
			ReachTiles = Settings.RangeTiles[num5]
		};
		beam2.SetAngle(beam.Angle);
		(float X, float Y) tuple2 = Hand(entity, beam2, firing: true, now);
		float item3 = tuple2.X;
		float item4 = tuple2.Y;
		float num6 = MaxLength(entity, beam2, item3, item4);
		float num7 = worldPos.Bottom - 2f;
		float num8 = (0f - item2) * 5f;
		float num9 = item * 3f;
		int num10 = (int)(now / 40 % 6);
		Color color = new Color(102, 160, 230, 255);
		for (int j = 8; (float)j < num6; j++)
		{
			if ((j + num10) % 6 <= 2)
			{
				float num11 = item3 + item * (float)j;
				float num12 = num7 + item2 * (float)j;
				Rect(MathF.Round(num11 + num8), MathF.Round(num12 + num9), 1f, 1f, color);
				Rect(MathF.Round(num11 - num8), MathF.Round(num12 - num9), 1f, 1f, color);
			}
		}
		float num13 = item3 + item * num6;
		float num14 = num7 + item2 * num6;
		Color color2 = new Color(178, 200, 240, 255);
		for (int k = -2; k <= 2; k++)
		{
			Rect(MathF.Round(num13 - item * (float)Math.Abs(k) - item2 * (float)k), MathF.Round(num14 - item2 * (float)Math.Abs(k) * 0.6f + item * (float)k * 0.6f), 1f, 1f, color2);
		}
	}

	private static void DrawWaveRing(Wave wave, long now)
	{
		float num = (float)(now - wave.At) / 520f;
		int a = (int)(230f * (1f - num));
		if (wave.Explosion)
		{
			float num2 = (10f + (float)wave.Level * 6f) * (0.4f + num * 0.9f);
			DrawEllipseDots(wave.X, wave.Y, num2, num2 * 0.4f, 40, new Color(a, Light.R, Light.G, Light.B));
		}
		else
		{
			float num3 = 8f + num * 22f;
			DrawEllipseDots(wave.X, wave.Y, num3, num3 * 0.36f, 44, (wave.Level >= 2) ? new Color(a, 255, 255, 255) : new Color(a, Light.R, Light.G, Light.B));
		}
	}

	private static void DrawExplosionTongues(Wave wave, long now)
	{
		float num = (float)(now - wave.At) / 520f;
		if (!(num >= 0.55f))
		{
			float num2 = 1f - num / 0.55f;
			int num3 = (int)(now / 50);
			for (int i = 0; i < 7; i++)
			{
				float angle = -(float)Math.PI / 2f + (Rand(i, num3) - 0.5f) * 2.2f;
				Tongue(wave.BurstX + (Rand(num3, i) - 0.5f) * 8f, wave.BurstY + 4f, angle, (8f + (float)wave.Level * 6f) * num2 + Rand(i + 3, num3) * 6f, 2f + (float)wave.Level * 0.6f, (i % 2 == 0) ? Mid : Outer);
			}
			Disc(wave.BurstX, wave.BurstY, (5f + (float)wave.Level * 3f) * num2, Light);
			Disc(wave.BurstX, wave.BurstY, (2.5f + (float)wave.Level * 1.5f) * num2, Core);
		}
	}

	private static void DrawEllipseDots(float cx, float cy, float rx, float ry, int points, Color color)
	{
		for (int i = 0; i < points; i++)
		{
			float x = (float)i / (float)points * (float)Math.PI * 2f;
			Rect(MathF.Round(cx + MathF.Cos(x) * rx), MathF.Round(cy + MathF.Sin(x) * ry), 1f, 1f, color);
		}
	}

	private static void DrawBeam(Entity entity, Beam beam, long now)
	{
		if (beam.Head <= beam.Tail + 0.5f && beam.StoppedAt > 0)
		{
			return;
		}
		(float X, float Y) tuple = Hand(entity, beam, firing: true, now);
		float item = tuple.X;
		float item2 = tuple.Y;
		(float Cos, float Sin) tuple2 = AxisOf(beam.Angle);
		float item3 = tuple2.Cos;
		float item4 = tuple2.Sin;
		float angle = beam.Angle;
		int num = Math.Clamp(beam.Level, 0, 2);
		float k = Courbure(beam);
		float num2 = ((beam.StoppedAt > 0) ? (1f - 0.7f * Math.Clamp((float)(now - beam.StoppedAt) / (float)Math.Max(1, Settings.FadeMs), 0f, 1f)) : 1f);
		int num3 = (int)(now / 50);
		float num4 = 1f + MathF.Sin((float)now / 40f) * 0.08f;
		float num5 = (7f + 4f * (float)num) * num2 * num4;
		if (Heritage.Actif)
		{
			num5 = Heritage.Rayon.Epaisseur(num, 7f + 4f * (float)num) * num2 * num4;
		}
		int num6 = (int)MathF.Floor(beam.Tail);
		int num7 = (int)beam.Head;
		(Color, float, int)[] array = new(Color, float, int)[3]
		{
			(Outer, 0.5f, 0),
			(Mid, 0.34f, 1),
			(Light, 0.2f, 2)
		};
		if (Heritage.Actif)
		{
			Color cerne = Heritage.Rayon.Cerne;
			if ((object)cerne != null)
			{
				float num8 = (float)Options.Instance.Combat.Heritage.TechniquesVu.RayonCerneDixiemes / 10f / MathF.Max(1f, num5);
				array = array.Prepend((cerne, 0.5f + num8, 0)).ToArray();
			}
		}
		(Color, float, int)[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			(Color, float, int) tuple3 = array2[i];
			Color item5 = tuple3.Item1;
			float item6 = tuple3.Item2;
			int item7 = tuple3.Item3;
			for (int j = num6; j < num7; j++)
			{
				float num9 = MathF.Min(1f, 0.55f + (float)j / 30f);
				float num10 = (float)(now - beam.FiredAt) * 0.3f % 90f - 10f;
				float num11 = ((float)(j % 90) - num10) / 8f;
				float num12 = 1f + 0.32f * MathF.Exp((0f - num11) * num11);
				float num13 = MathF.Max(0.5f, num5 * item6 * num9 * num12 + Edge((float)j + ((item7 > 0) ? ((float)now * 0.15f) : 0f), item7, num3) * num2);
				ColumnSurLArc(item, item2, item3, item4, angle, k, j, 0f - num13, num13, item5);
			}
		}
		float num14 = MathF.Max(0f, num5 * 0.08f);
		for (int l = num6; l < num7; l++)
		{
			ColumnSurLArc(item, item2, item3, item4, angle, k, l, 0f - num14, num14, Core);
		}
		float num15 = MathF.Max(1f, beam.HeadMax);
		for (int m = 0; m < 6; m++)
		{
			float num16 = ((float)now * 0.5f + (float)(m * 53)) % num15;
			if (!(num16 < beam.Tail) && !(num16 > beam.Head - 6f))
			{
				float num17 = ((m % 2 == 0) ? (-1f) : 1f) * MathF.Max(1f, num5 * 0.2f);
				for (int n = 0; n < 6; n++)
				{
					ColumnSurLArc(item, item2, item3, item4, angle, k, (int)num16 + n, num17, num17, Core);
				}
			}
		}
		var (num18, num19, num20, num21) = ArcDuRayon.Point(item, item2, item3, item4, k, beam.Head);
		if (Heritage.Actif)
		{
			Heritage.Rayon.Tete(entity, num18, num19, num20, num21, num5, beam.TargetId);
		}
		float num22 = num5 * 0.62f + 2f + (float)(num3 % 2);
		float num23 = MathF.Atan2(num21, num20);
		for (int num24 = 0; num24 < 6; num24++)
		{
			Tongue(num18, num19, num23 + (Rand(num24, num3) - 0.5f) * 2.4f, num22 + 2f + Rand(num3, num24) * (3f + (float)num * 2f), 1.5f + (float)num * 0.5f, Outer);
		}
		if (beam.TargetId != Guid.Empty && beam.StoppedAt == 0L)
		{
			for (int num25 = 0; num25 < 8; num25++)
			{
				float num26 = ((num25 % 2 == 0) ? 1f : (-1f));
				Tongue(num18, num19, num23 + num26 * (1.2f + Rand(num25, num3) * 1.3f), num22 + 3f + Rand(num3, num25 + 40) * (5f + (float)num * 3f), 1.4f + (float)num * 0.4f, (num25 % 3 == 0) ? Mid : Outer);
			}
		}
		Disc(num18, num19, num22, Outer);
		Disc(num18, num19, num22 * 0.74f, Mid);
		Disc(num18, num19, num22 * 0.48f, Light);
		Disc(num18, num19, num22 * 0.24f, Core);
		if (beam.Tail < 4f)
		{
			float num27 = num5 * 0.5f + 1.5f;
			for (int num28 = 0; num28 < 6; num28++)
			{
				Tongue(item, item2, Rand(num28 + 20, num3) * (float)Math.PI * 2f, num27 + 2f + Rand(num3, num28 + 20) * 5f, 1.3f, Mid);
			}
			Disc(item, item2, num27, Mid);
			Disc(item, item2, num27 * 0.6f, Light);
			Disc(item, item2, num27 * 0.3f, Core);
		}
	}

	private static void DrawScar(Scar scar, long now)
	{
		long num = now - scar.At;
		float num2 = Math.Clamp((float)(5000 - num) / 1500f, 0f, 1f);
		switch (scar.Kind)
		{
		case ScarKind.Trail:
		{
			(float Cos, float Sin) tuple = AxisOf(scar.Angle);
			float item = tuple.Cos;
			float item2 = tuple.Sin;
			bool flag = scar.K == 0f && (item == 0f || item2 == 0f);
			int num8 = 2 + scar.Level;
			Color color3 = new Color((int)(87f * num2), 42, 26, 12);
			for (int k = 6; (float)k < scar.Length; k += (flag ? 1 : 2))
			{
				var (num9, num10, num11, num12) = ArcDuRayon.Point(scar.X, scar.Y, item, item2, scar.K, k);
				if (!Meteo.EstDeLEau(num9, num10))
				{
					int num13 = num8 + (int)(Hash(k / 3, scar.Seed) % 3) - 1;
					Strip(num9, num10, (0f - num12) * (float)num13, num11 * (float)num13 * 0.5f, color3);
				}
			}
			break;
		}
		case ScarKind.Crater:
		{
			int num14 = ((scar.Level >= 2) ? 14 : 9);
			int num15 = (int)MathF.Ceiling((float)num14 * 0.4f);
			Color color4 = new Color((int)(115f * num2), 36, 22, 10);
			for (int l = -num15; l <= num15; l++)
			{
				int num16 = (int)((float)num14 * MathF.Sqrt(1f - (float)(l * l) / (float)(num15 * num15)));
				Rect(MathF.Round(scar.X) - (float)num16, MathF.Round(scar.Y) + (float)l, num16 * 2 + 1, 1f, color4);
			}
			Color color5 = new Color((int)(128f * num2), 201, 177, 132);
			DrawEllipseDots(scar.X, scar.Y, num14 + 1, num15 + 1, 10, color5);
			break;
		}
		case ScarKind.Cracks:
		{
			float num3 = Math.Clamp((float)num / 200f, 0f, 1f);
			Color color = new Color((int)(153f * num2), 42, 26, 12);
			Color color2 = new Color((int)(153f * num2), 179, 155, 108);
			for (int i = 0; i < 3 + scar.Level; i++)
			{
				float num4 = scar.X + (Rand(i, scar.Seed) - 0.5f) * 6f;
				float num5 = scar.Y + (Rand(scar.Seed, i) - 0.5f) * 2f;
				float num6 = Rand(i + 7, scar.Seed) * (float)Math.PI * 2f;
				int num7 = (int)((float)(4 + scar.Level * 2) * num3);
				for (int j = 0; j < num7; j++)
				{
					float x = num6 + (Rand(j, i + scar.Seed) - 0.5f) * 1.2f;
					num4 += MathF.Cos(x) * 1.6f;
					num5 += MathF.Sin(x) * 0.7f;
					Rect(MathF.Round(num4), MathF.Round(num5), 1f, 1f, color);
					Rect(MathF.Round(num4), MathF.Round(num5) + 1f, 1f, 1f, color2);
				}
			}
			break;
		}
		}
	}

	private static float Edge(float u, int layer, int image)
	{
		float num = layer switch
		{
			0 => 1.5f, 
			1 => 0.8f, 
			_ => 0.5f, 
		};
		float num2 = MathF.Sin(u * 0.23f + (float)image * 1.9f + (float)layer * 2.1f) * num;
		float num3 = MathF.Sin(u * 0.071f - (float)image * 0.83f + (float)layer) * num * 0.8f;
		float num4 = (Rand((int)MathF.Floor(u / 3f) + layer * 101, image) - 0.5f) * num * 1.4f;
		float num5 = 0f;
		if (layer == 0)
		{
			int a = (int)MathF.Floor(u / 6f);
			if (Hash(a, image * 7) % 5 == 0)
			{
				num5 = 4f * (1f - u % 6f / 6f);
			}
		}
		return num2 + num3 + num4 + num5;
	}

	private static void Column(float ox, float oy, float cos, float sin, float degrees, int u, float from, float to, Color color)
	{
		if (sin == 0f)
		{
			Rect(MathF.Round(ox + cos * (float)u), MathF.Round(oy + from), 1f, MathF.Round(to - from) + 1f, color);
			return;
		}
		if (cos == 0f)
		{
			Rect(MathF.Round(ox + from), MathF.Round(oy + sin * (float)u), MathF.Round(to - from) + 1f, 1f, color);
			return;
		}
		float num = (from + to) / 2f;
		RotatedRect(ox + cos * (float)u - sin * num, oy + sin * (float)u + cos * num, 2f, MathF.Round(to - from) + 1f, degrees, color);
	}

	private static void ColumnSurLArc(float ox, float oy, float cos, float sin, float degrees, float k, int u, float from, float to, Color color)
	{
		if (k == 0f)
		{
			Column(ox, oy, cos, sin, degrees, u, from, to, color);
			return;
		}
		(float X, float Y, float Cos, float Sin) tuple = ArcDuRayon.Point(ox, oy, cos, sin, k, u);
		float item = tuple.X;
		float item2 = tuple.Y;
		float item3 = tuple.Cos;
		float item4 = tuple.Sin;
		float num = (from + to) / 2f;
		RotatedRect(item - item4 * num, item2 + item3 * num, 2f, MathF.Round(to - from) + 1f, MathF.Atan2(item4, item3) * 180f / (float)Math.PI, color);
	}

	private static void Strip(float cx, float cy, float px, float py, Color color)
	{
		if (MathF.Abs(px) < 0.01f)
		{
			float num = MathF.Max(1f, MathF.Round(2f * MathF.Abs(py)));
			Rect(MathF.Round(cx), MathF.Round(cy - num / 2f), 1f, num, color);
		}
		else if (MathF.Abs(py) < 0.01f)
		{
			float num2 = MathF.Round(MathF.Abs(px));
			Rect(MathF.Round(cx) - num2, MathF.Round(cy), num2 * 2f + 1f, 1f, color);
		}
		else
		{
			float width = MathF.Round(2f * MathF.Sqrt(px * px + py * py)) + 1f;
			RotatedRect(cx, cy, width, 2f, MathF.Atan2(py, px) * 180f / (float)Math.PI, color);
		}
	}

	private static void Disc(float cx, float cy, float radius, Color color)
	{
		if (radius < 0.5f)
		{
			return;
		}
		float num = MathF.Round(cx);
		float num2 = MathF.Round(cy);
		int num3 = (int)MathF.Ceiling(radius);
		for (int i = -num3; i <= num3; i++)
		{
			float num4 = radius * radius - (float)(i * i);
			if (!(num4 < 0f))
			{
				int num5 = (int)MathF.Floor(MathF.Sqrt(num4) + 0.35f);
				Rect(num - (float)num5, num2 + (float)i, num5 * 2 + 1, 1f, color);
			}
		}
	}

	private static void Tongue(float x, float y, float angle, float length, float foot, Color color)
	{
		float num = MathF.Cos(angle);
		float num2 = MathF.Sin(angle);
		FillTriangle(x - num2 * foot, y + num * foot, x + num * length, y + num2 * length, x + num2 * foot, y - num * foot, color);
	}

	private static void FillTriangle(float ax, float ay, float bx, float by, float cx, float cy, Color color)
	{
		int num = (int)MathF.Floor(MathF.Min(ay, MathF.Min(by, cy)));
		int num2 = (int)MathF.Ceiling(MathF.Max(ay, MathF.Max(by, cy)));
		for (int i = num; i <= num2; i++)
		{
			float y = (float)i + 0.5f;
			float left = float.MaxValue;
			float right = float.MinValue;
			Cross(ax, ay, bx, by, y, ref left, ref right);
			Cross(bx, by, cx, cy, y, ref left, ref right);
			Cross(cx, cy, ax, ay, y, ref left, ref right);
			if (right >= left)
			{
				float num3 = MathF.Round(left);
				Rect(num3, i, MathF.Max(1f, MathF.Round(right) - num3), 1f, color);
			}
		}
	}

	private static void Cross(float ax, float ay, float bx, float by, float y, ref float left, ref float right)
	{
		if ((ay <= y && by > y) || (by <= y && ay > y))
		{
			float y2 = ax + (y - ay) * (bx - ax) / (by - ay);
			left = MathF.Min(left, y2);
			right = MathF.Max(right, y2);
		}
	}

	private static void Rect(float x, float y, float width, float height, Color color)
	{
		if (!(width <= 0f) && !(height <= 0f) && color.A > 0)
		{
			IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
			if (gameTexture != null)
			{
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(x, y, width, height), color);
			}
		}
	}

	private static void RotatedRect(float cx, float cy, float width, float height, float degrees, Color color)
	{
		if (!(width <= 0f) && !(height <= 0f) && color.A > 0)
		{
			IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
			if (gameTexture != null)
			{
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(cx - width / 2f, cy - height / 2f, width, height), color, null, GameBlendModes.None, null, degrees);
			}
		}
	}

	private static uint Hash(int a, int b)
	{
		uint num = (uint)(a * 374761393 + b * 668265263);
		num = (num ^ (num >> 13)) * 1274126177;
		return num ^ (num >> 16);
	}

	private static float Rand(int a, int b)
	{
		return (float)(Hash(a, b) % 10000) / 10000f;
	}
}
