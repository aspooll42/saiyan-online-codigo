using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Maps;
using Intersect.Client.MonoGame.Graphics;
using Intersect.Config;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Entities;

public static class Flight
{
	public readonly record struct ContrailPoint(float LeftX, float LeftY, float RightX, float RightY, long At, long Life);

	private sealed class LigneDeVitesse
	{
		public float X;

		public float Y;

		public float Longueur;

		public float Alpha;
	}

	private readonly record struct Nappe(float X, float Y, int Forme);

	private enum CloudPart
	{
		Full,
		Back,
		Front
	}

	private sealed class State
	{
		public FlightMode Mode;

		public FlightMode PreviousMode;

		public FlightEvent Event;

		public long EventAt = -2305843009213693952L;

		public float FromAltitude;

		public float ShiftX;

		public float ShiftY;

		public long ShiftMs;

		public float CloudX;

		public float CloudY;

		public float CloudAltitude;

		public int CloudRow = 1;

		public bool CloudParked;

		public readonly List<ContrailPoint> Contrail = new List<ContrailPoint>();

		public long SparkleAt;

		public float SparkleDebt;

		public bool CloudSprint;

		public long CourseSonAt;

		public long DownUntil;

		public float LastX;

		public float LastY;

		public long LastAt;

		public float VelocityX;

		public float VelocityY;

		public long MovingUntil;

		public float KiFloat = 0.8f;

		public float HairFrameMs = 110f;

		public float HairPhase;

		public long BreathCycle = long.MinValue;

		public readonly SprintState Sprint = new SprintState();

		public float FloatAmplitude = 1.6f;

		public float Lean;

		public bool CrashDustDone;

		public long PuffAt;

		public float LastSpeed01;

		public float LastNx;

		public float CloudLean;
	}

	private sealed class Particle
	{
		public float X;

		public float Y;

		public float Vx;

		public float Vy;

		public long BornAt;

		public int LifeMs;

		public int Size;

		public Color Color = new Color(255, 255, 255, 255);

		public bool Additive;

		public bool Shrinks;

		public int Length;

		public float LineX;

		public float LineY;
	}

	private sealed class Breath
	{
		public float X;

		public float Y;

		public long BornAt;

		public float R0 = 5f;

		public float R1 = 22f;

		public long Duration = 900L;

		public int Alpha = 115;
	}

	private sealed class SprintState
	{
		public bool Active;

		public long StartedAt = -2305843009213693952L;

		public float DirX = 1f;

		public float DirY;

		public float TongueDebt;

		public long LastDrawAt;

		public long LastGhostAt;

		public float BodyLeft = 2f;

		public float BodyRight = 30f;

		public float BodyTop = 18f;

		public float BodyBottom = 37f;

		public readonly List<Tongue> Tongues = new List<Tongue>();

		public readonly List<TrailPoint> Trail = new List<TrailPoint>();

		public readonly List<Ghost> Ghosts = new List<Ghost>();
	}

	private sealed class Tongue
	{
		public float OffsetX;

		public float OffsetY;

		public float Push;

		public float Drift;

		public float Length;

		public float Width;

		public float Seed;

		public long Born;

		public long Life;

		public int Layer;
	}

	private readonly record struct TrailPoint(float X, float Y, long At);

	private sealed class Ghost
	{
		public IGameTexture Mask;

		public long Born;
	}

	private sealed class AirRing
	{
		public float X;

		public float Y;

		public float R0;

		public float R1;

		public float Alpha;

		public Color Color = new Color(255, 255, 255, 255);

		public long Born;

		public long Duration;
	}

	private const long ContrailMs = 900L;

	private const long ContrailSprintMs = 1600L;

	private const int VoileMax = 56;

	private static IGameTexture? _texDouce;

	private static long _altitudeAt;

	private static readonly Color[] Blancs = CreerBlancs();

	private static readonly Color PailletteOr = new Color(230, 255, 236, 150);

	private static readonly Color PailletteBlanche = new Color(210, 255, 255, 255);

	private static readonly List<LigneDeVitesse> LignesDeVitesse = new List<LigneDeVitesse>();

	private static float _course;

	private static float _dirX;

	private static float _dirY;

	private const float NappesLargeur = 2600f;

	private const float NappesHauteur = 2000f;

	private const float NappesParallaxe = 1.5f;

	private static readonly (int Largeur, int Hauteur, int Graine)[] FormesDeNappes = new(int, int, int)[3]
	{
		(136, 56, 11),
		(104, 44, 23),
		(168, 64, 37)
	};

	private static readonly IGameTexture?[] TexturesDeNappes = new IGameTexture[FormesDeNappes.Length];

	private static readonly Nappe[] Nappes = CreerNappes();

	private const float CloudAltitude = 56f;

	private const long CloudRiseMs = 900L;

	private const long CloudLandMs = 700L;

	private const float KiAltitude = 26f;

	private const float LyingLift = 9f;

	private const int Columns = 16;

	private const long KiCrouchMs = 140L;

	private const long KiRiseMs = 340L;

	private const long CloudArriveMs = 520L;

	private const long CloudHopMs = 300L;

	private const long LandMs = 320L;

	private const long DiveMs = 240L;

	private const long FallMs = 520L;

	private const float GlideMsPerPixel = 1.2f;

	private const long GlideMsMax = 900L;

	private const long CrashMs = 380L;

	private const int CrashColumn = 4;

	private const long CloudWaitMs = 1400L;

	private const long CloudLeaveMs = 650L;

	private const int CloudWidth = 48;

	private const int CloudHeight = 36;

	private static readonly float[] CloudFeet = new float[4] { 17f, 14f, 14f, 17f };

	private const int CloudSink = 2;

	private static readonly Dictionary<Guid, State> States = new Dictionary<Guid, State>();

	private static readonly List<Particle> Particles = new List<Particle>();

	private static readonly List<Particle> GroundParticles = new List<Particle>();

	private static readonly List<Breath> Breaths = new List<Breath>();

	private static readonly Random Noise = new Random();

	private const int CloudPoseColumns = 16;

	private static readonly int[] LyingLoop = new int[6] { 4, 5, 6, 7, 6, 5 };

	private const float FloatPeriod = 430f;

	private const float PreviewSpeed = 127f;

	private const float TrailLength = 36f;

	private const float GhostSpacing = 5.7f;

	private const long GhostEveryMs = 45L;

	private static readonly List<AirRing> AirRings = new List<AirRing>();

	private const long TrailMs = 280L;

	private const long GhostMs = 240L;

	private const long FlashMs = 200L;

	private static long _lastWakeAt;

	private static Color[] CreerBlancs()
	{
		Color[] array = new Color[256];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new Color(i, 255, 255, 255);
		}
		return array;
	}

	private static Color Blanc(float alpha)
	{
		return Blancs[Math.Clamp((int)alpha, 0, 255)];
	}

	private static long LandDuration(State state)
	{
		if (state.PreviousMode != FlightMode.Cloud)
		{
			return 320L;
		}
		return 700L;
	}

	private static float HauteurLocale()
	{
		Player me = Globals.Me;
		if (me == null || !States.ContainsKey(me.Id))
		{
			return 0f;
		}
		return Math.Clamp(Altitude(me) / 56f, 0f, 1f);
	}

	public static void DessinerAltitude()
	{
		DrawAirShadows();
		long milliseconds = Timing.Global.Milliseconds;
		long dt = ((_altitudeAt == 0L) ? 0 : Math.Clamp(milliseconds - _altitudeAt, 0L, 100L));
		_altitudeAt = milliseconds;
		float num = HauteurLocale();
		if (!(num <= 0.01f))
		{
			IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
			if (gameTexture != null)
			{
				FloatRect worldViewport = Graphics.WorldViewport;
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), worldViewport, new Color((int)(56f * num), 206, 226, 255));
				DessinerNappes(worldViewport, num, milliseconds);
				DessinerLignesDeVitesse(gameTexture, worldViewport, num, dt);
				return;
			}
		}
		LignesDeVitesse.Clear();
		_course = 0f;
	}

	private static bool RidesCloud(State state, long now)
	{
		long num = now - state.EventAt;
		switch (state.Event)
		{
		case FlightEvent.TakeOff:
			if (state.Mode == FlightMode.Cloud)
			{
				return num >= 520;
			}
			break;
		case FlightEvent.Land:
			if (state.PreviousMode == FlightMode.Cloud && state.Mode == FlightMode.None)
			{
				return (float)num < (float)Math.Max(700L, state.ShiftMs) * 0.8f;
			}
			break;
		case FlightEvent.CloudRefused:
			return false;
		}
		return state.Mode == FlightMode.Cloud;
	}

	private static void DrawAirShadows()
	{
		if (States.Count == 0)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		foreach (var (key, state2) in States)
		{
			if (RidesCloud(state2, milliseconds) && Globals.Entities.TryGetValue(key, out Entity value) && value != null && !value.IsHidden)
			{
				float num = Altitude(value);
				Vector2 origin = value.Origin;
				CloudShadow(origin.X + RiderSway(value), origin.Y + num, Math.Max(4f, num), 1f);
			}
		}
	}

	private static void CloudGlow(float x, float feetY, float altitude, State state, long now)
	{
		IGameTexture gameTexture = TextureDouce();
		if (gameTexture != null)
		{
			float num = Math.Clamp((altitude - 8f) / 48f, 0f, 1f);
			float num2 = 0.85f + 0.15f * MathF.Sin((float)now / 137f) * MathF.Sin((float)now / 322f);
			Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, gameTexture.Width, gameTexture.Height), new FloatRect(MathF.Round(x - 30f), MathF.Round(feetY + 4f - 15f), 60f, 30f), new Color((int)(78f * num * num2), 255, 196, 70), null, GameBlendModes.Add);
		}
	}

	private static void CloudSparkles(float x, float feetY, State state, long now)
	{
		long num = ((state.SparkleAt == 0L) ? 0 : Math.Clamp(now - state.SparkleAt, 0L, 100L));
		state.SparkleAt = now;
		if (Particles.Count <= 300)
		{
			float num2 = Speed(state);
			state.SparkleDebt += (float)num * (14f + 40f * Math.Clamp(num2 / 0.2f, 0f, 1f)) / 1000f;
			float num3 = ((num2 > 0.01f) ? (state.VelocityX / num2) : 0f);
			float num4 = ((num2 > 0.01f) ? (state.VelocityY / num2) : 0f);
			while (state.SparkleDebt >= 1f)
			{
				state.SparkleDebt -= 1f;
				float x2 = (float)(Noise.NextDouble() * Math.PI * 2.0);
				Particles.Add(new Particle
				{
					X = x + MathF.Cos(x2) * (6f + (float)Noise.NextDouble() * 10f),
					Y = feetY + 4f + MathF.Sin(x2) * (3f + (float)Noise.NextDouble() * 5f),
					Vx = (0f - num3) * 26f + (float)(Noise.NextDouble() * 12.0 - 6.0),
					Vy = (0f - num4) * 26f + (float)(Noise.NextDouble() * 12.0 - 10.0),
					BornAt = now,
					LifeMs = 400 + Noise.Next(400),
					Size = 1,
					Color = ((Noise.NextDouble() < 0.55) ? PailletteOr : PailletteBlanche),
					Additive = true
				});
			}
		}
	}

	private static void RecordContrail(float x, float feetY, State state, long now)
	{
		float num = Speed(state);
		if (num < 0.03f)
		{
			return;
		}
		if (state.Contrail.Count > 0)
		{
			List<ContrailPoint> contrail = state.Contrail;
			if (now - contrail[contrail.Count - 1].At < 16)
			{
				return;
			}
		}
		float num2 = state.VelocityX / num;
		float num3 = state.VelocityY / num;
		float num4 = x - num2 * 14f;
		float num5 = feetY + 3f - num3 * 5f;
		float num6 = (0f - num3) * 6f;
		float num7 = num2 * 6f * 0.6f;
		long life = (state.CloudSprint ? 1600 : 900);
		state.Contrail.Add(new ContrailPoint(num4 + num6, num5 + num7, num4 - num6, num5 - num7, now, life));
		if (state.Contrail.Count > 160)
		{
			state.Contrail.RemoveAt(0);
		}
	}

	private static void DrawContrails(IGameTexture pixel, long now)
	{
		foreach (State value in States.Values)
		{
			List<ContrailPoint> contrail = value.Contrail;
			while (contrail.Count > 0 && now - contrail[0].At >= contrail[0].Life)
			{
				contrail.RemoveAt(0);
			}
			for (int i = 1; i < contrail.Count; i++)
			{
				ContrailPoint contrailPoint = contrail[i - 1];
				ContrailPoint contrailPoint2 = contrail[i];
				float num = (float)(now - contrailPoint2.At) / (float)contrailPoint2.Life;
				float num2 = 150f * (1f - num) * (1f - num * 0.3f);
				if (!(num2 < 4f))
				{
					Color color = Blanc(num2);
					Ligne(pixel, contrailPoint.LeftX, contrailPoint.LeftY, contrailPoint2.LeftX, contrailPoint2.LeftY, color);
					Ligne(pixel, contrailPoint.RightX, contrailPoint.RightY, contrailPoint2.RightX, contrailPoint2.RightY, color);
				}
			}
		}
	}

	private static void Ligne(IGameTexture pixel, float x0, float y0, float x1, float y1, Color color)
	{
		float num = x1 - x0;
		float num2 = y1 - y0;
		if (MathF.Abs(num) > 40f || MathF.Abs(num2) > 40f)
		{
			return;
		}
		int num3 = (int)MathF.Ceiling(MathF.Max(MathF.Abs(num), MathF.Abs(num2)));
		if (num3 == 0)
		{
			return;
		}
		bool flag = MathF.Abs(num) >= MathF.Abs(num2);
		float num4 = MathF.Round(x0);
		float num5 = MathF.Round(y0);
		for (int i = 1; i <= num3; i++)
		{
			float num6 = MathF.Round(x0 + num * (float)i / (float)num3);
			float num7 = MathF.Round(y0 + num2 * (float)i / (float)num3);
			bool flag2 = (flag ? (num7 == num5) : (num6 == num4));
			if (!flag2 || i >= num3)
			{
				float num8 = (flag2 ? num6 : MathF.Round(x0 + num * (float)(i - 1) / (float)num3));
				float num9 = (flag2 ? num7 : MathF.Round(y0 + num2 * (float)(i - 1) / (float)num3));
				FloatRect targetRect = (flag ? new FloatRect(MathF.Min(num4, num8), num5, MathF.Abs(num8 - num4) + 1f, 1f) : new FloatRect(num4, MathF.Min(num5, num9), 1f, MathF.Abs(num9 - num5) + 1f));
				Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), targetRect, color);
				num4 = num6;
				num5 = num7;
			}
		}
	}

	private static Nappe[] CreerNappes()
	{
		Random random = new Random(761);
		Nappe[] array = new Nappe[12];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new Nappe((float)random.NextDouble() * 2600f, (float)random.NextDouble() * 2000f, i % FormesDeNappes.Length);
		}
		return array;
	}

	private static void DessinerNappes(FloatRect vue, float h, long now)
	{
		float num = vue.X + vue.Width / 2f;
		float num2 = vue.Y + vue.Height / 2f;
		float num3 = (float)now / 100f;
		Color renderColor = Blanc(255f * h);
		Nappe[] nappes = Nappes;
		for (int i = 0; i < nappes.Length; i++)
		{
			Nappe nappe = nappes[i];
			IGameTexture gameTexture = TextureNappe(nappe.Forme);
			if (gameTexture != null)
			{
				float num4 = num + (Enroule(nappe.X - num * 1.5f + num3, 2600f) - 1300f) * 0.8f;
				float num5 = num2 + (Enroule(nappe.Y - num2 * 1.5f, 2000f) - 1000f) * 0.8f;
				if (!(num4 + (float)gameTexture.Width < vue.Left) && !(num4 - (float)gameTexture.Width > vue.Right) && !(num5 + (float)gameTexture.Height < vue.Top) && !(num5 - (float)gameTexture.Height > vue.Bottom))
				{
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, gameTexture.Width, gameTexture.Height), new FloatRect(MathF.Round(num4 - (float)gameTexture.Width / 2f), MathF.Round(num5 - (float)gameTexture.Height / 2f), gameTexture.Width, gameTexture.Height), renderColor);
				}
			}
		}
	}

	private static float Enroule(float valeur, float taille)
	{
		return (valeur % taille + taille) % taille;
	}

	private static void DessinerLignesDeVitesse(IGameTexture pixel, FloatRect vue, float h, long dt)
	{
		Player me = Globals.Me;
		bool flag = false;
		if (me != null && States.TryGetValue(me.Id, out State value) && value.Mode == FlightMode.Cloud && me.IsSprinting && Speed(value) > 0.05f)
		{
			flag = true;
			float num = Speed(value);
			_dirX = value.VelocityX / num;
			_dirY = value.VelocityY / num;
		}
		_course += ((flag ? 1f : 0f) - _course) * Math.Min(1f, (float)dt / 250f);
		if (_course < 0.02f)
		{
			LignesDeVitesse.Clear();
			return;
		}
		bool flag2 = MathF.Abs(_dirX) >= MathF.Abs(_dirY);
		while (flag && LignesDeVitesse.Count < 22)
		{
			LignesDeVitesse.Add(new LigneDeVitesse
			{
				X = vue.X + (float)Noise.NextDouble() * vue.Width,
				Y = vue.Y + (float)Noise.NextDouble() * vue.Height,
				Longueur = 50f + (float)Noise.NextDouble() * 70f,
				Alpha = 0.1f + (float)Noise.NextDouble() * 0.14f
			});
		}
		float num2 = 900f * (float)dt / 1000f;
		for (int num3 = LignesDeVitesse.Count - 1; num3 >= 0; num3--)
		{
			LigneDeVitesse ligneDeVitesse = LignesDeVitesse[num3];
			ligneDeVitesse.X -= _dirX * num2;
			ligneDeVitesse.Y -= _dirY * num2;
			if (ligneDeVitesse.X < vue.Left - 150f || ligneDeVitesse.X > vue.Right + 150f || ligneDeVitesse.Y < vue.Top - 150f || ligneDeVitesse.Y > vue.Bottom + 150f)
			{
				LignesDeVitesse.RemoveAt(num3);
			}
			else
			{
				for (int i = 0; i < 3; i++)
				{
					float num4 = ligneDeVitesse.Longueur * (float)i / 3f;
					float num5 = ligneDeVitesse.Longueur / 3f;
					FloatRect targetRect = (flag2 ? new FloatRect(MathF.Round(ligneDeVitesse.X - (float)MathF.Sign(_dirX) * (num4 + ((_dirX > 0f) ? num5 : 0f))), MathF.Round(ligneDeVitesse.Y), MathF.Round(num5), 1f) : new FloatRect(MathF.Round(ligneDeVitesse.X), MathF.Round(ligneDeVitesse.Y - (float)MathF.Sign(_dirY) * (num4 + ((_dirY > 0f) ? num5 : 0f))), 1f, MathF.Round(num5)));
					Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), targetRect, Blanc(255f * ligneDeVitesse.Alpha * _course * h * (1f - (float)i * 0.33f)));
				}
			}
		}
	}

	private static IGameTexture? TextureDouce()
	{
		if (_texDouce != null || !(Graphics.Renderer is MonoRenderer monoRenderer))
		{
			return _texDouce;
		}
		byte[] array = new byte[16384];
		for (int i = 0; i < 64; i++)
		{
			for (int j = 0; j < 64; j++)
			{
				float num = ((float)j + 0.5f) / 64f * 2f - 1f;
				float num2 = ((float)i + 0.5f) / 64f * 2f - 1f;
				float num3 = MathF.Min(1f, MathF.Sqrt(num * num + num2 * num2));
				float num4 = (1f - num3) * (1f - num3);
				int num5 = (i * 64 + j) * 4;
				array[num5] = byte.MaxValue;
				array[num5 + 1] = byte.MaxValue;
				array[num5 + 2] = byte.MaxValue;
				array[num5 + 3] = (byte)(num4 * 255f);
			}
		}
		_texDouce = monoRenderer.CreerTexturePixels("vol/douce", 64, 64, array);
		return _texDouce;
	}

	private static IGameTexture? TextureNappe(int forme)
	{
		if (TexturesDeNappes[forme] != null || !(Graphics.Renderer is MonoRenderer monoRenderer))
		{
			return TexturesDeNappes[forme];
		}
		(int Largeur, int Hauteur, int Graine) tuple = FormesDeNappes[forme];
		int item = tuple.Largeur;
		int item2 = tuple.Hauteur;
		int item3 = tuple.Graine;
		Random random = new Random(item3);
		List<(float, float, float)> list = new List<(float, float, float)>();
		int num = 5 + random.Next(3);
		for (int i = 0; i < num; i++)
		{
			float num2 = (float)item2 * (0.24f + (float)random.NextDouble() * 0.14f);
			float item4 = num2 + 2f + (float)random.NextDouble() * ((float)item - 2f * num2 - 4f);
			float item5 = MathF.Max(num2 + 1f, (float)item2 - num2 * 1.05f - (float)random.NextDouble() * (float)item2 * 0.15f);
			list.Add((item4, item5, num2));
		}
		float num3 = (float)item2 * 0.42f;
		list.Add(((float)item * (0.4f + (float)random.NextDouble() * 0.2f), num3 + 1f, num3));
		int num4 = item2 - 3;
		byte[] array = new byte[item * item2 * 4];
		for (int j = 0; j < item2; j++)
		{
			for (int k = 0; k < item; k++)
			{
				if (j > num4)
				{
					continue;
				}
				float num5 = 0f;
				foreach (var item9 in list)
				{
					float item6 = item9.Item1;
					float item7 = item9.Item2;
					float item8 = item9.Item3;
					float num6 = (float)k + 0.5f - item6;
					float num7 = (float)j + 0.5f - item7;
					num5 = MathF.Max(num5, 1f - MathF.Sqrt(num6 * num6 + num7 * num7) / item8);
				}
				int num8;
				int num9;
				int num10;
				int num11;
				if (num5 > 0.45f)
				{
					num8 = 175;
					num9 = 255;
					num10 = 255;
					num11 = 255;
				}
				else if (num5 > 0.22f)
				{
					num8 = 125;
					num9 = 246;
					num10 = 250;
					num11 = 255;
				}
				else
				{
					if (!(num5 > 0.06f) || ((k + j) & 1) != 0)
					{
						continue;
					}
					num8 = 80;
					num9 = 236;
					num10 = 243;
					num11 = 255;
				}
				if ((float)j > (float)item2 * 0.62f && num5 > 0.22f)
				{
					num9 = 226;
					num10 = 235;
					num11 = 250;
				}
				int num12 = (j * item + k) * 4;
				array[num12] = (byte)num9;
				array[num12 + 1] = (byte)num10;
				array[num12 + 2] = (byte)num11;
				array[num12 + 3] = (byte)num8;
			}
		}
		TexturesDeNappes[forme] = monoRenderer.CreerTexturePixels($"vol/nappe/{forme}", item, item2, array);
		return TexturesDeNappes[forme];
	}

	public static void SetState(Entity entity, FlightMode mode)
	{
		entity.FlightMode = mode;
		if (mode != FlightMode.None || States.ContainsKey(entity.Id))
		{
			State state = StateOf(entity);
			state.Mode = mode;
			state.PreviousMode = mode;
			state.Event = FlightEvent.State;
			state.EventAt = Timing.Global.Milliseconds - 60000;
			state.ShiftX = (state.ShiftY = 0f);
		}
	}

	public static void OnPacket(Entity entity, EntityFlightPacket packet)
	{
		long milliseconds = Timing.Global.Milliseconds;
		State state = StateOf(entity);
		float num = Altitude(entity);
		float x = entity.Origin.X;
		float num2 = entity.Origin.Y + num;
		state.PreviousMode = state.Mode;
		state.FromAltitude = num;
		state.Mode = packet.Mode;
		state.Event = packet.Event;
		state.EventAt = milliseconds;
		state.ShiftX = (state.ShiftY = 0f);
		state.ShiftMs = 0L;
		state.CrashDustDone = false;
		state.CloudParked = false;
		entity.FlightMode = packet.Mode;
		if (state.PreviousMode == FlightMode.Cloud && packet.Mode != FlightMode.Cloud)
		{
			state.CloudX = x;
			state.CloudY = num2;
			state.CloudAltitude = num;
			state.CloudRow = CloudRowOf(entity);
		}
		if ((packet.MapId != entity.MapId || entity.X != packet.X || entity.Y != packet.Y) && MapInstance.TryGet(entity.MapId, out MapInstance instance) && MapInstance.TryGet(packet.MapId, out MapInstance instance2))
		{
			int tileWidth = Options.Instance.Map.TileWidth;
			int tileHeight = Options.Instance.Map.TileHeight;
			float num3 = (float)(instance.X + entity.X * tileWidth) + entity.OffsetX;
			float num4 = (float)(instance.Y + entity.Y * tileHeight) + entity.OffsetY;
			if (packet.MapId != entity.MapId)
			{
				entity.MapId = packet.MapId;
				if (entity == Globals.Me)
				{
					Player.FetchNewMaps();
				}
			}
			entity.X = (byte)Math.Clamp(packet.X, 0, 255);
			entity.Y = (byte)Math.Clamp(packet.Y, 0, 255);
			entity.OffsetX = 0f;
			entity.OffsetY = 0f;
			entity.IsMoving = false;
			state.ShiftX = num3 - (float)(instance2.X + packet.X * tileWidth);
			state.ShiftY = num4 - (float)(instance2.Y + packet.Y * tileHeight);
			long min = packet.Event switch
			{
				FlightEvent.Dive => 240L, 
				FlightEvent.Fall => 520L, 
				_ => 320L, 
			};
			float num5 = MathF.Sqrt(state.ShiftX * state.ShiftX + state.ShiftY * state.ShiftY);
			state.ShiftMs = ((packet.Event == FlightEvent.Dive) ? 240 : Math.Clamp((long)(num5 * 1.2f), min, 900L));
		}
		switch (packet.Event)
		{
		case FlightEvent.Fall:
			state.DownUntil = milliseconds + Math.Max(520L, state.ShiftMs) + Math.Max(380L, Options.Instance.Flight.FallDownMilliseconds);
			break;
		case FlightEvent.CloudRefused:
			state.DownUntil = milliseconds + 520 + 300 + 260 + 380;
			break;
		case FlightEvent.TakeOff:
			if (packet.Mode == FlightMode.Ki)
			{
				Dust(x, num2, 14, 1f);
			}
			break;
		}
		switch (packet.Event)
		{
		case FlightEvent.TakeOff:
			if (packet.Mode == FlightMode.Cloud)
			{
				SonDuNuage(entity, "nuage_appel.wav");
			}
			break;
		case FlightEvent.CloudRefused:
			SonDuNuage(entity, "nuage_refus.wav");
			break;
		case FlightEvent.Land:
			if (state.PreviousMode == FlightMode.Cloud && packet.Mode == FlightMode.None)
			{
				SonDuNuage(entity, "nuage_atterrissage.wav");
			}
			break;
		case FlightEvent.Dive:
		case FlightEvent.Fall:
			break;
		}
	}

	private static void SonDuNuage(Entity entity, string fichier)
	{
		Audio.AddMapSound(fichier, entity.X, entity.Y, entity.MapId, loop: false, 0, 14, entity);
	}

	public static bool IsGliding(Entity entity)
	{
		if (States.TryGetValue(entity.Id, out State value))
		{
			return ShiftLeft(value, Timing.Global.Milliseconds) > 0f;
		}
		return false;
	}

	public static bool IsDown(Entity entity)
	{
		if (States.TryGetValue(entity.Id, out State value))
		{
			return Timing.Global.Milliseconds < value.DownUntil;
		}
		return false;
	}

	public static bool IsAirborne(Entity entity)
	{
		return Altitude(entity) > 0.5f;
	}

	public static bool HidesBodyShadow(Entity entity)
	{
		if (States.TryGetValue(entity.Id, out State value) && Altitude(entity) > 0.5f)
		{
			return RidesCloud(value, Timing.Global.Milliseconds);
		}
		return false;
	}

	public static float Altitude(Entity entity)
	{
		if (!States.TryGetValue(entity.Id, out State value))
		{
			return 0f;
		}
		long num = Timing.Global.Milliseconds - value.EventAt;
		switch (value.Event)
		{
		case FlightEvent.TakeOff:
			if (value.Mode == FlightMode.Ki)
			{
				if (num >= 140)
				{
					return 26f * Ease((float)(num - 140) / 340f);
				}
				return 0f;
			}
			if (value.Mode == FlightMode.Cloud)
			{
				if (num < 520)
				{
					return 0f;
				}
				long num7 = num - 520;
				if (num7 < 300)
				{
					float num8 = (float)num7 / 300f;
					return 4f * num8 + MathF.Sin(num8 * (float)Math.PI) * 12f;
				}
				return 4f + 52f * Ease(Math.Clamp((float)(num7 - 300) / 900f, 0f, 1f));
			}
			break;
		case FlightEvent.CloudRefused:
		{
			if (num < 520)
			{
				return 0f;
			}
			long num4 = num - 520;
			if (num4 < 300)
			{
				float num5 = (float)num4 / 300f;
				return 4f * num5 + MathF.Sin(num5 * (float)Math.PI) * 12f;
			}
			float num6 = (float)(num4 - 300) / 260f;
			if (!(num6 >= 1f))
			{
				return 4f * (1f - num6 * num6);
			}
			return 0f;
		}
		case FlightEvent.Land:
		case FlightEvent.Dive:
		{
			long num9 = ((value.Event == FlightEvent.Dive) ? 240 : Math.Max(LandDuration(value), value.ShiftMs));
			if (value.Mode != FlightMode.None)
			{
				return AltitudeOf(value.Mode);
			}
			if (num < num9)
			{
				return value.FromAltitude * (1f - Ease((float)num / (float)num9));
			}
			return 0f;
		}
		case FlightEvent.Fall:
		{
			long num2 = Math.Max(520L, value.ShiftMs);
			if (num >= num2)
			{
				return 0f;
			}
			float num3 = (float)num / (float)num2;
			return Math.Max(0f, value.FromAltitude * (1f - num3 * num3) + 10f * num3 * (1f - num3));
		}
		}
		return AltitudeOf(value.Mode);
	}

	public static float ShiftX(Entity entity)
	{
		if (!States.TryGetValue(entity.Id, out State value))
		{
			return 0f;
		}
		return ShiftXOf(value, Timing.Global.Milliseconds);
	}

	public static float ShiftY(Entity entity)
	{
		if (!States.TryGetValue(entity.Id, out State value))
		{
			return 0f;
		}
		return ShiftYOf(value, Timing.Global.Milliseconds);
	}

	private static float ShiftXOf(State state, long now)
	{
		return state.ShiftX * ShiftLeft(state, now);
	}

	private static float ShiftYOf(State state, long now)
	{
		return state.ShiftY * ShiftLeft(state, now);
	}

	private static float ShiftLeft(State state, long now)
	{
		if (state.ShiftMs <= 0 || (state.ShiftX == 0f && state.ShiftY == 0f))
		{
			return 0f;
		}
		float num = (float)(now - state.EventAt) / (float)state.ShiftMs;
		if (!(num >= 1f))
		{
			return 1f - Ease(num);
		}
		return 0f;
	}

	public static bool BlocksFlight(Entity entity, Point tile)
	{
		if (!MapInstance.TryGet(entity.MapId, out MapInstance instance))
		{
			return true;
		}
		int mapWidth = Options.Instance.Map.MapWidth;
		int mapHeight = Options.Instance.Map.MapHeight;
		Guid guid = entity.MapId;
		if (tile.X < 0 || tile.Y < 0 || tile.X >= mapWidth || tile.Y >= mapHeight)
		{
			int num = instance.GridX + ((tile.X < 0) ? (-1) : ((tile.X >= mapWidth) ? 1 : 0));
			int num2 = instance.GridY + ((tile.Y < 0) ? (-1) : ((tile.Y >= mapHeight) ? 1 : 0));
			if (Globals.MapGrid == null || num < 0 || num2 < 0 || num >= Globals.MapGrid.GetLength(0) || num2 >= Globals.MapGrid.GetLength(1))
			{
				return true;
			}
			guid = Globals.MapGrid[num, num2];
			if (guid == Guid.Empty)
			{
				return true;
			}
		}
		if (MapInstance.TryGet(guid, out MapInstance instance2))
		{
			return instance2.VolInterdit;
		}
		return true;
	}

	public static bool TryPose(Entity entity, int row, ref IGameTexture texture, ref FloatRect source, out float lift)
	{
		lift = 0f;
		if (!States.TryGetValue(entity.Id, out State value))
		{
			return false;
		}
		long milliseconds = Timing.Global.Milliseconds;
		Observe(entity, value, milliseconds);
		lift = RiderFloat(value, milliseconds);
		long num = milliseconds - value.EventAt;
		int num2;
		switch (value.Event)
		{
		case FlightEvent.TakeOff:
			if (value.Mode == FlightMode.Ki)
			{
				num2 = ((num < 140) ? 1 : ((num < 480) ? 2 : KiColumn(value, ref lift)));
				break;
			}
			if (value.Mode == FlightMode.Cloud)
			{
				if (num < 520)
				{
					return false;
				}
				if (num >= 820)
				{
					return CloudPose(entity, row, ref texture, ref source);
				}
				num2 = ((num < 630) ? 1 : 2);
				break;
			}
			goto default;
		case FlightEvent.CloudRefused:
		{
			if (num < 520)
			{
				return false;
			}
			long num3 = num - 520;
			num2 = ((num3 < 110) ? 1 : ((num3 < 300) ? 2 : ((num3 < 560) ? (8 + (int)(num3 / 80) % 2) : ((num3 < 940) ? 4 : (-1)))));
			if (num2 < 0)
			{
				return false;
			}
			break;
		}
		case FlightEvent.Land:
			if (value.Mode == FlightMode.None)
			{
				long num4 = ((value.PreviousMode == FlightMode.Cloud) ? Math.Max(700L, value.ShiftMs) : 320);
				if (value.PreviousMode == FlightMode.Cloud && (float)num < (float)num4 * 0.8f)
				{
					return CloudPose(entity, row, ref texture, ref source);
				}
				if (num >= num4)
				{
					return false;
				}
				num2 = 2;
				break;
			}
			goto default;
		case FlightEvent.Dive:
			if (value.Mode == FlightMode.None)
			{
				if (num >= 240)
				{
					return false;
				}
				num2 = ((value.PreviousMode == FlightMode.Ki) ? 4 : 2);
				if (value.PreviousMode == FlightMode.Ki)
				{
					lift += 9f;
				}
				break;
			}
			goto default;
		case FlightEvent.Fall:
			if (num < 520)
			{
				num2 = 8 + (int)(num / 80) % 4;
			}
			else if (num < 900)
			{
				if (!value.CrashDustDone)
				{
					value.CrashDustDone = true;
					Vector2 origin = entity.Origin;
					Dust(origin.X, origin.Y, 18, 1.3f);
				}
				num2 = 4;
			}
			else if (milliseconds < value.DownUntil - 200)
			{
				num2 = 4 + (int)((num - 520 - 380) / 160) % 4;
			}
			else
			{
				if (milliseconds >= value.DownUntil)
				{
					return false;
				}
				num2 = 1;
			}
			break;
		default:
			if (value.Mode == FlightMode.Cloud)
			{
				return CloudPose(entity, row, ref texture, ref source);
			}
			if (value.Mode != FlightMode.Ki || entity.IsTransforming)
			{
				return false;
			}
			num2 = KiColumn(value, ref lift);
			break;
		}
		IGameTexture gameTexture = FlightSheet(entity);
		if (gameTexture == null)
		{
			return false;
		}
		int num5 = gameTexture.Width / 16;
		int num6 = gameTexture.Height / Math.Max(1, Options.Instance.Sprites.Directions);
		texture = gameTexture;
		source = new FloatRect(num2 * num5, row * num6, num5, num6);
		return true;
	}

	private static bool CloudPose(Entity entity, int row, ref IGameTexture texture, ref FloatRect source)
	{
		bool flag = entity.IsTransforming || entity.IsAttacking || entity.IsBlocking || entity.IsCasting || entity.IsCharging || KiBeam.IsActive(entity);
		bool flag2 = flag;
		if (!flag2)
		{
			SpriteAnimations spriteAnimation = entity.SpriteAnimation;
			bool flag3 = (((uint)spriteAnimation <= 1u || spriteAnimation == SpriteAnimations.Afk) ? true : false);
			flag2 = !flag3;
		}
		if (!flag2)
		{
			IGameTexture gameTexture = CloudSheet(entity);
			if (gameTexture != null)
			{
				int num = gameTexture.Width / 16;
				int num2 = gameTexture.Height / Math.Max(1, Options.Instance.Sprites.Directions);
				texture = gameTexture;
				int num3 = Math.Max(0, entity.SpriteFrame) % 16;
				if (States.TryGetValue(entity.Id, out State value))
				{
					num3 = CloudColumn(entity, value, row, Timing.Global.Milliseconds);
				}
				source = new FloatRect(num3 * num, row * num2, num, num2);
				return true;
			}
		}
		return false;
	}

	private static int CloudColumn(Entity entity, State state, int row, long now)
	{
		bool flag = MathF.Cos((float)(now - 120) / 430f) > 0f;
		bool flag2 = entity.IsSprinting && !IsStill(state, now) && Speed(state) > 0.02f;
		bool flag3 = (uint)(row - 1) <= 1u;
		int num = ((!(flag3 && flag2)) ? (flag ? 1 : 0) : (flag ? 2 : 3));
		return num + 4 * (Math.Max(0, entity.SpriteFrame) % 4);
	}

	private static bool Tasse(State state, long now)
	{
		return MathF.Sin((float)(now - 120) / 430f) > 0.55f;
	}

	public static float CloudLean(Entity entity)
	{
		if (!States.TryGetValue(entity.Id, out State value) || value.Mode != FlightMode.Cloud)
		{
			return 0f;
		}
		return MathF.Round(value.CloudLean * 2f) / 2f;
	}

	private static int KiColumn(State state, ref float lift)
	{
		lift += 9f;
		return LyingLoop[(int)state.HairPhase % LyingLoop.Length];
	}

	private static bool IsStill(State state, long now)
	{
		return now >= state.MovingUntil;
	}

	public static float RiderSway(Entity entity)
	{
		if (!States.TryGetValue(entity.Id, out State value) || value.Mode != FlightMode.Cloud)
		{
			return 0f;
		}
		return MathF.Round(value.Lean) + CloudDrift(value, Timing.Global.Milliseconds);
	}

	private static float RiderFloat(State state, long now)
	{
		if (state.Mode == FlightMode.None || Altitude0(state, now))
		{
			return 0f;
		}
		if (state.Mode != FlightMode.Ki)
		{
			return (0f - state.FloatAmplitude) * MathF.Sin((float)(now - 120) / 430f) - (Tasse(state, now) ? 1f : 0f);
		}
		return KiBob(state, now);
	}

	private static float KiBob(State state, long now)
	{
		return MathF.Round((0f - state.KiFloat) * MathF.Sin((float)now / 430f));
	}

	public static float ShadowStrength(Entity entity)
	{
		if (!States.TryGetValue(entity.Id, out State value) || value.Mode != FlightMode.Ki)
		{
			return 1f;
		}
		return 1f - 0.06f * KiBob(value, Timing.Global.Milliseconds);
	}

	private static bool Altitude0(State state, long now)
	{
		if (state.Event == FlightEvent.TakeOff)
		{
			return now - state.EventAt < ((state.Mode == FlightMode.Cloud) ? 820 : 480);
		}
		return false;
	}

	private static float CloudDrift(State state, long now)
	{
		float num = 1f - Math.Clamp(Speed(state) / 0.2f, 0f, 1f);
		return num * MathF.Sin((float)now / 950f) * 1.2f;
	}

	public static void DrawCloud(Entity entity, FloatRect body)
	{
		DrawCloudPart(entity, front: true);
	}

	public static void DrawCloudBehind(Entity entity)
	{
		DrawCloudPart(entity, front: false);
	}

	private static void DrawCloudPart(Entity entity, bool front)
	{
		if (!States.TryGetValue(entity.Id, out State value))
		{
			return;
		}
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "nuage_magique.png");
		if (texture == null)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		long num = milliseconds - value.EventAt;
		Vector2 origin = entity.Origin;
		float num2 = Altitude(entity);
		float x = origin.X;
		float num3 = origin.Y + num2;
		int num4 = CloudRowOf(entity);
		float bob = CloudFloat(value, milliseconds);
		if (!front && entity.IsSprinting && !value.CloudSprint && RidesCloud(value, milliseconds) && num2 > 8f && milliseconds - value.CourseSonAt > 1000)
		{
			value.CourseSonAt = milliseconds;
			SonDuNuage(entity, "nuage_course.wav");
		}
		value.CloudSprint = entity.IsSprinting;
		CloudPart part = ((!front) ? CloudPart.Back : CloudPart.Front);
		switch (value.Event)
		{
		case FlightEvent.TakeOff:
			if (value.Mode == FlightMode.Cloud)
			{
				goto case FlightEvent.CloudRefused;
			}
			goto default;
		case FlightEvent.CloudRefused:
		{
			bool flag = value.Event == FlightEvent.CloudRefused;
			if (num < 520)
			{
				if (front)
				{
					float num6 = Ease((float)num / 520f);
					DrawCloudAt(texture, x + 90f * (1f - num6), num3, 4f + 70f * (1f - num6), 1, bob, 1f, trail: true, value, milliseconds, CloudPart.Full);
				}
			}
			else if (!flag)
			{
				DrawCloudAt(texture, x + RiderSway(entity), num3, Math.Max(4f, num2), num4, bob, 1f, trail: false, value, milliseconds, part);
			}
			else if (front)
			{
				long num7 = 1460L;
				if (num < num7)
				{
					float alpha = ((num > 820 && num / 60 % 2 == 0L) ? 0.55f : 1f);
					DrawCloudAt(texture, x, num3, 4f, 1, bob, alpha, trail: false, value, milliseconds, CloudPart.Full);
				}
				else
				{
					DrawLeaving(texture, x, num3, 4f, 1, num - num7, bob, value, milliseconds);
				}
			}
			break;
		}
		case FlightEvent.Land:
			if (value.PreviousMode == FlightMode.Cloud && value.Mode == FlightMode.None)
			{
				float num5 = (float)Math.Max(700L, value.ShiftMs) * 0.8f;
				if ((float)num < num5)
				{
					DrawCloudAt(texture, x + RiderSway(entity), num3, Math.Max(4f, num2), num4, bob, 1f, trail: false, value, milliseconds, part);
				}
				else if (front)
				{
					if (!value.CloudParked)
					{
						value.CloudParked = true;
						value.CloudX = x;
						value.CloudY = num3;
						value.CloudAltitude = Math.Max(4f, num2);
						value.CloudRow = num4;
					}
					DrawLeaving(texture, value.CloudX, value.CloudY, value.CloudAltitude, value.CloudRow, num - (long)num5, bob, value, milliseconds);
				}
				break;
			}
			goto case FlightEvent.Dive;
		case FlightEvent.Dive:
		case FlightEvent.Fall:
			if (value.PreviousMode == FlightMode.Cloud && value.Mode != FlightMode.Cloud)
			{
				if (front)
				{
					long num8 = ((value.Event == FlightEvent.Dive) ? 1400 : 0);
					if (num < num8)
					{
						DrawCloudAt(texture, value.CloudX, value.CloudY, value.CloudAltitude, value.CloudRow, bob, 1f, trail: false, value, milliseconds, CloudPart.Full);
					}
					else
					{
						DrawLeaving(texture, value.CloudX, value.CloudY, value.CloudAltitude, value.CloudRow, num - num8, bob, value, milliseconds);
					}
				}
				break;
			}
			goto default;
		default:
			if (value.Mode == FlightMode.Cloud)
			{
				DrawCloudAt(texture, x + RiderSway(entity) - MathF.Round(value.Lean), num3, num2, num4, bob, 1f, trail: true, value, milliseconds, part);
			}
			break;
		}
	}

	private static void DrawLeaving(IGameTexture texture, float x, float y, float altitude, int row, long t, float bob, State state, long now)
	{
		if (t < 650)
		{
			float num = Ease((float)t / 650f);
			float num2 = ((row == 2) ? 1f : (-1f));
			DrawCloudAt(texture, x + num2 * 110f * num, y, altitude + 90f * num, row, bob, 1f - num * 0.3f, trail: true, state, now, CloudPart.Full);
		}
	}

	private static void DrawCloudAt(IGameTexture texture, float groundX, float groundY, float altitude, int row, float bob, float alpha, bool trail, State state, long now, CloudPart part)
	{
		row = Math.Clamp(row, 0, 3);
		if (part == CloudPart.Full)
		{
			CloudShadow(groundX, groundY, altitude, alpha);
		}
		int num = texture.Width / 4;
		int num2 = texture.Height / 4;
		int num3 = (int)CloudFeet[row];
		float num4 = MathF.Round(groundY - altitude - (float)num3 + bob);
		float num5 = MathF.Round(groundX - (float)num / 2f);
		Color color = new Color((int)(255f * Math.Clamp(alpha, 0f, 1f)), 255, 255, 255);
		if (part == CloudPart.Back && altitude > 8f)
		{
			CloudGlow(groundX, num4 + (float)num3, altitude, state, now);
		}
		(int, int) tuple = part switch
		{
			CloudPart.Back => (0, num3), 
			CloudPart.Front => (num3 - 2, num2 - num3 + 2), 
			_ => (0, num2), 
		};
		int item = tuple.Item1;
		int item2 = tuple.Item2;
		float num6 = Math.Clamp(Speed(state) / 0.2f, 0f, 1f);
		bool flag = num6 > 0.06f;
		float num7 = 1f + 0.55f * num6 * (state.CloudSprint ? 1.3f : 1f) - (flag ? 0f : 0.18f);
		int num8 = ((state.VelocityX > 0.01f) ? 1 : ((state.VelocityX < -0.01f) ? (-1) : 0));
		bool flag2 = part == CloudPart.Back && flag && num8 != 0;
		bool flag3 = flag2;
		if (flag3)
		{
			bool flag4 = (uint)(row - 1) <= 1u;
			flag3 = flag4;
		}
		if (flag3)
		{
			DrawCloudColumns(texture, row, 0, num2, num5 - (float)(num8 * 3), num4, num7 + 0.3f, tasse: false, now, new Color((int)(56f * Math.Clamp(alpha, 0f, 1f)), 255, 255, 255));
		}
		DrawCloudColumns(texture, row, item, item2, num5, num4, num7, part == CloudPart.Back && Tasse(state, now), now, color);
		if (part != CloudPart.Front)
		{
			if (trail)
			{
				CloudTrail(groundX, num4 + (float)num2 * 0.45f, state, now);
			}
			if (part == CloudPart.Back && altitude > 8f)
			{
				CloudSparkles(groundX, num4 + (float)num3, state, now);
				RecordContrail(groundX, num4 + (float)num3, state, now);
				CloudPuffs(groundX, num4 + (float)num3, row, state, now);
			}
		}
	}

	private static void DrawCloudColumns(IGameTexture texture, int row, int from, int height, float left, float top, float etire, bool tasse, long now, Color color)
	{
		int num = texture.Width / 4;
		int num2 = texture.Height / 4;
		bool flag = row == 1;
		bool flag2 = row == 2;
		int num3 = (tasse ? (height - 1) : height);
		if (num3 <= 0)
		{
			return;
		}
		int num4 = (tasse ? 1 : 0);
		for (int i = 0; i < num; i++)
		{
			int num5 = 0;
			if (MathF.Abs(etire - 1f) > 0.001f)
			{
				int num6 = (flag ? (i - 30) : (flag2 ? (17 - i) : (-1)));
				if (num6 > 0)
				{
					num5 = (int)MathF.Round((float)num6 * (etire - 1f)) * (flag ? 1 : (-1));
				}
			}
			int num7 = (int)MathF.Round(MathF.Sin((float)now / 260f + (float)i / 4.5f));
			bool flag3 = num5 != 0 && etire > 1f;
			Graphics.DrawGameTexture(texture, new FloatRect(i, row * num2 + from, 1f, num3), new FloatRect(left + (float)i + (float)num5 - (float)((flag3 && flag) ? 1 : 0), top + (float)from + (float)num4 + (float)num7, (!flag3) ? 1 : 2, num3), color);
		}
	}

	private static void CloudPuffs(float x, float feetY, int row, State state, long now)
	{
		float num = Speed(state);
		float num2 = Math.Clamp(num / 0.2f, 0f, 1f);
		bool flag = num2 > 0.06f;
		float num3 = ((num > 0.01f) ? (state.VelocityX / num) : 0f);
		float num4 = ((num > 0.01f) ? (state.VelocityY / num) : 0f);
		float num5 = 1f + 0.55f * num2 * (state.CloudSprint ? 1.3f : 1f) - (flag ? 0f : 0.18f);
		float num6 = 25f + 17f * Math.Max(0f, num5 - 1f);
		float num7 = x + row switch
		{
			2 => 0f - num6, 
			1 => num6, 
			_ => (float)(Noise.NextDouble() * 10.0 - 5.0), 
		};
		float y = row switch
		{
			3 => feetY + 20f, 
			0 => feetY - 16f, 
			_ => feetY + 4f + (float)(Noise.NextDouble() * 6.0 - 3.0), 
		};
		float num8 = (flag ? (90f / Math.Max(0.5f, num2)) : 700f);
		if ((float)(now - state.PuffAt) > num8 && Particles.Count < 350)
		{
			state.PuffAt = now;
			Puff(num7, y, flag ? ((0f - num3) * (40f + 50f * num2)) : ((float)(Noise.NextDouble() * 6.0 - 3.0)), flag ? ((0f - num4) * (40f + 50f * num2) - 4f) : (-11f), flag ? (2 + Noise.Next(2)) : (1 + Noise.Next(2)), flag ? 420 : 650, now);
		}
		if (state.LastSpeed01 > 0.3f && !flag)
		{
			for (int i = 0; i < 3; i++)
			{
				Puff(num7 + (float)i * 3f - 3f, y, (0f - state.LastNx) * 45f + (float)(i - 1) * 8f, -10f, 2, 500, now);
			}
		}
		state.LastSpeed01 = num2;
		state.LastNx = num3;
	}

	private static void Puff(float x, float y, float vx, float vy, int size, int lifeMs, long now)
	{
		Particles.Add(new Particle
		{
			X = x,
			Y = y,
			Vx = vx,
			Vy = vy,
			BornAt = now,
			LifeMs = lifeMs,
			Size = size,
			Color = ((Noise.NextDouble() < 0.4) ? new Color(217, 255, 240, 168) : new Color(217, 255, 211, 77)),
			Shrinks = true
		});
	}

	private static void CloudShadow(float groundX, float groundY, float altitude, float alpha)
	{
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "ombre_grande.png");
		if (texture != null)
		{
			float num = Math.Clamp(1f - altitude / 110f, 0.3f, 1f);
			float num2 = (float)texture.Width * num;
			float num3 = (float)texture.Height * num;
			Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), new FloatRect(MathF.Round(groundX - num2 / 2f), MathF.Round(groundY - 2f - num3 / 2f), num2, num3), new Color((int)(200f * num * alpha), 255, 255, 255));
		}
	}

	private static float CloudFloat(State state, long now)
	{
		return state.FloatAmplitude * MathF.Sin((float)now / 430f);
	}

	private static int CloudRowOf(Entity entity)
	{
		switch (entity.DirectionFacing)
		{
		case Direction.Left:
		case Direction.UpLeft:
		case Direction.DownLeft:
			return 1;
		case Direction.Right:
		case Direction.UpRight:
		case Direction.DownRight:
			return 2;
		case Direction.Up:
			return 3;
		default:
			return 0;
		}
	}

	private static void Observe(Entity entity, State state, long now)
	{
		Vector2 origin = entity.Origin;
		long num = now - state.LastAt;
		if (num <= 0)
		{
			return;
		}
		if (num < 200)
		{
			float num2 = (origin.X - state.LastX) / (float)num;
			float num3 = (origin.Y - state.LastY) / (float)num;
			if (Math.Abs(num2) > 1f || Math.Abs(num3) > 1f)
			{
				num2 = (num3 = 0f);
			}
			float num4 = Math.Min(1f, (float)num / 90f);
			state.VelocityX += (num2 - state.VelocityX) * num4;
			state.VelocityY += (num3 - state.VelocityY) * num4;
		}
		state.LastX = origin.X;
		state.LastY = origin.Y;
		state.LastAt = now;
		float num5 = Math.Clamp(Speed(state) / 0.2f, 0f, 1f);
		float num6 = Math.Min(1f, (float)num / 250f);
		state.FloatAmplitude += (1.8f + -1.3499999f * num5 - state.FloatAmplitude) * num6;
		float num7 = ((Speed(state) > 0.01f) ? ((0f - state.VelocityX) / Speed(state) * num5) : 0f);
		state.Lean += (num7 - state.Lean) * Math.Min(1f, (float)num / 180f);
		int num8 = CloudRowOf(entity);
		bool flag = state.Mode == FlightMode.Cloud && entity.IsSprinting && num5 > 0.3f;
		bool flag2 = flag;
		if (flag2)
		{
			bool flag3 = (uint)(num8 - 1) <= 1u;
			flag2 = flag3;
		}
		float num9 = ((!flag2) ? 0f : ((num8 == 1) ? (-3f) : 3f));
		state.CloudLean += (num9 - state.CloudLean) * Math.Min(1f, (float)num / 150f);
		if (entity.IsMoving)
		{
			state.MovingUntil = now + 220;
		}
		bool flag4 = IsStill(state, now);
		bool flag5 = IsSprintingInFlight(entity, state, now);
		state.KiFloat += ((flag4 ? 2f : (flag5 ? 0f : 0.8f)) - state.KiFloat) * Math.Min(1f, (float)num / (flag5 ? 200f : 600f));
		state.HairFrameMs += ((flag4 ? 210f : (flag5 ? 70f : 110f)) - state.HairFrameMs) * Math.Min(1f, (float)num / (flag5 ? 300f : 500f));
		state.HairPhase = (state.HairPhase + (float)num / state.HairFrameMs) % (float)LyingLoop.Length;
		UpdateSprint(entity, state, origin.X, origin.Y, flag5, num, now);
		bool flag6 = !flag5 && state.Mode == FlightMode.Ki;
		bool flag7 = flag6;
		if (flag7)
		{
			FlightEvent flightEvent = state.Event;
			bool flag3 = (uint)flightEvent <= 1u;
			flag7 = flag3;
		}
		if (flag7 && Altitude(entity) > 4f && Particles.Count < 400)
		{
			KiSparks(entity, state, origin.X, origin.Y, flag4, num, now);
		}
		else
		{
			state.BreathCycle = long.MinValue;
		}
	}

	private static void KiSparks(Entity entity, State state, float originX, float originY, bool still, long dt, long now)
	{
		Color color = Transformation.KiColorOf(entity.TransformedSprite);
		Color color2 = new Color(255, color.R, color.G, color.B);
		float num = originY - 48f - 9f - KiBob(state, now) + 42f;
		if (!still)
		{
			state.BreathCycle = long.MinValue;
			if (Noise.NextDouble() < (double)((float)dt / 25f))
			{
				float num2 = Speed(state);
				float num3 = ((num2 > 0.01f) ? (state.VelocityX / num2) : 0f);
				float num4 = ((num2 > 0.01f) ? (state.VelocityY / num2) : 0f);
				Particles.Add(new Particle
				{
					X = originX - num3 * 13f + (float)(Noise.NextDouble() * 4.0 - 2.0),
					Y = num - 4f - num4 * 6f + (float)(Noise.NextDouble() * 4.0 - 2.0),
					Vx = (0f - num3) * 30f + (float)(Noise.NextDouble() * 8.0 - 4.0),
					Vy = (0f - num4) * 30f + (float)(Noise.NextDouble() * 4.0 - 2.0),
					BornAt = now,
					LifeMs = 220 + Noise.Next(160),
					Size = 1,
					Color = color2,
					Additive = true
				});
			}
			return;
		}
		if (Noise.NextDouble() < (double)((float)dt / 75f))
		{
			int num5 = CloudRowOf(entity);
			bool flag = (uint)(num5 - 1) <= 1u;
			float num6 = (flag ? 11f : 5f);
			Particles.Add(new Particle
			{
				X = originX + (float)(Noise.NextDouble() * 2.0 - 1.0) * num6,
				Y = num + (float)Noise.NextDouble() * 2f,
				Vx = (float)(Noise.NextDouble() - 0.5) * 4f,
				Vy = 10f + (float)Noise.NextDouble() * 12f,
				BornAt = now,
				LifeMs = 380 + Noise.Next(260),
				Size = 1,
				Color = color2,
				Additive = true
			});
		}
		long num7 = (long)MathF.Floor(((float)now / 430f - (float)Math.PI / 2f) / ((float)Math.PI * 2f));
		if (state.BreathCycle != long.MinValue && num7 != state.BreathCycle && now - (state.MovingUntil - 220) > 500)
		{
			float num8 = originY + Altitude(entity);
			if (!Meteo.SouffleDeVolSurLEau(originX, num8))
			{
				Breaths.Add(new Breath
				{
					X = originX,
					Y = num8,
					BornAt = now
				});
				for (int i = 0; i < 7; i++)
				{
					double num9 = Noise.NextDouble() * Math.PI * 2.0;
					GroundParticles.Add(new Particle
					{
						X = originX + (float)Math.Cos(num9) * 5f,
						Y = num8 + (float)Math.Sin(num9) * 2f,
						Vx = (float)Math.Cos(num9) * (16f + (float)Noise.NextDouble() * 14f),
						Vy = (float)Math.Sin(num9) * 5f - 2f,
						BornAt = now,
						LifeMs = 520 + Noise.Next(240),
						Size = ((!(Noise.NextDouble() < 0.4)) ? 1 : 2),
						Color = new Color(128, 205, 196, 170),
						Shrinks = true
					});
				}
			}
		}
		state.BreathCycle = num7;
	}

	private static float Speed(State state)
	{
		return MathF.Sqrt(state.VelocityX * state.VelocityX + state.VelocityY * state.VelocityY);
	}

	private static void CloudTrail(float x, float y, State state, long now)
	{
		float num = Speed(state);
		float num2 = Math.Clamp(num / 0.2f, 0f, 1f);
		if (num2 < 0.06f || Particles.Count > 300)
		{
			return;
		}
		float num3 = state.VelocityX / num;
		float num4 = state.VelocityY / num;
		if (Noise.NextDouble() < (double)(num2 * 0.8f))
		{
			bool flag = Noise.NextDouble() < 0.35;
			Particles.Add(new Particle
			{
				X = x - num3 * 20f + (float)(Noise.NextDouble() * 4.0 - 2.0),
				Y = y - num4 * 6f + (float)(Noise.NextDouble() * 5.0 - 2.0),
				Vx = (0f - num3) * (14f + (float)Noise.NextDouble() * 20f) * num2 * (state.CloudSprint ? 1.5f : 1f),
				Vy = (0f - num4) * (14f + (float)Noise.NextDouble() * 20f) * num2 * (state.CloudSprint ? 1.5f : 1f),
				BornAt = now,
				LifeMs = (int)((float)(320 + Noise.Next(300)) * (0.6f + 0.4f * num2)),
				Size = ((Noise.NextDouble() < 0.4) ? 3 : 2),
				Color = (flag ? new Color(230, 254, 240, 164) : new Color(215, 248, 196, 40)),
				Shrinks = true
			});
		}
		if (num2 > 0.5f && Noise.NextDouble() < (double)((num2 - 0.5f) * 0.5f))
		{
			for (int i = 0; i < 3; i++)
			{
				Particles.Add(new Particle
				{
					X = x - num3 * (8f + (float)i * 3f) - num4 * (float)(Noise.NextDouble() * 20.0 - 10.0),
					Y = y - 10f - (float)Noise.NextDouble() * 14f + num3 * (float)(Noise.NextDouble() * 4.0 - 2.0),
					Vx = (0f - num3) * 40f,
					Vy = (0f - num4) * 40f,
					BornAt = now,
					LifeMs = 200,
					Size = 1,
					Color = new Color(110, 255, 244, 200),
					Additive = true
				});
			}
		}
	}

	private static void Dust(float x, float y, int count, float force)
	{
		long milliseconds = Timing.Global.Milliseconds;
		for (int i = 0; i < count; i++)
		{
			double num = Noise.NextDouble() * Math.PI * 2.0;
			Particles.Add(new Particle
			{
				X = x + (float)Math.Cos(num) * 4f,
				Y = y + (float)Math.Sin(num) * 1.5f,
				Vx = (float)Math.Cos(num) * (20f + (float)Noise.NextDouble() * 26f) * force,
				Vy = (float)Math.Sin(num) * (6f + (float)Noise.NextDouble() * 8f) * force - 4f - (float)Noise.NextDouble() * 10f,
				BornAt = milliseconds,
				LifeMs = 380 + Noise.Next(240),
				Size = ((!(Noise.NextDouble() < 0.4)) ? 1 : 2),
				Color = new Color(205, 196, 170, 128)
			});
		}
	}

	public static void DrawTrails()
	{
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture != null)
		{
			if (AirRings.Count > 0)
			{
				DrawAirRings(gameTexture, Timing.Global.Milliseconds);
			}
			DrawContrails(gameTexture, Timing.Global.Milliseconds);
			if (Particles.Count > 0)
			{
				DrawParticles(gameTexture, Particles);
			}
		}
	}

	public static void DrawGround()
	{
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture == null)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		DrawSprintWakes(milliseconds);
		for (int num = Breaths.Count - 1; num >= 0; num--)
		{
			Breath breath = Breaths[num];
			float num2 = (float)(milliseconds - breath.BornAt) / (float)breath.Duration;
			if (num2 >= 1f)
			{
				Breaths.RemoveAt(num);
			}
			else
			{
				float num3 = breath.R0 + (breath.R1 - breath.R0) * Ease(num2);
				float num4 = num3 * 0.42f;
				Color renderColor = new Color((int)((float)breath.Alpha * (1f - num2)), 240, 228, 200);
				int num5 = Math.Max(8, (int)MathF.Round(num3 * 3f));
				for (int i = 0; i < num5; i++)
				{
					if (i % 3 != 2)
					{
						float x = (float)i / (float)num5 * ((float)Math.PI * 2f);
						Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(breath.X + MathF.Cos(x) * num3), MathF.Round(breath.Y + MathF.Sin(x) * num4), 1f, 1f), renderColor);
					}
				}
			}
		}
		if (GroundParticles.Count > 0)
		{
			DrawParticles(gameTexture, GroundParticles);
		}
	}

	private static void DrawParticles(IGameTexture pixel, List<Particle> particles)
	{
		long milliseconds = Timing.Global.Milliseconds;
		for (int num = particles.Count - 1; num >= 0; num--)
		{
			Particle particle = particles[num];
			long num2 = milliseconds - particle.BornAt;
			if (num2 >= particle.LifeMs)
			{
				particles.RemoveAt(num);
			}
			else
			{
				float num3 = (float)num2 / (float)particle.LifeMs;
				float num4 = (float)num2 / 1000f;
				float num5 = particle.X + particle.Vx * num4 * (particle.Shrinks ? (1f - num3 * 0.5f) : 1f);
				float num6 = particle.Y + particle.Vy * num4 + ((particle.Additive || particle.Shrinks || particle.Length > 0) ? 0f : (40f * num4 * num4));
				int num7 = (particle.Shrinks ? Math.Max(1, (int)MathF.Round((float)particle.Size * (1f - 0.6f * num3))) : particle.Size);
				Color renderColor = new Color((int)((float)(int)particle.Color.A * (1f - num3)), particle.Color.R, particle.Color.G, particle.Color.B);
				if (particle.Length > 0)
				{
					for (int i = 0; i < particle.Length; i++)
					{
						Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(num5 + particle.LineX * (float)i), MathF.Round(num6 + particle.LineY * (float)i), 1f, 1f), renderColor, null, (!particle.Additive) ? GameBlendModes.None : GameBlendModes.Add);
					}
				}
				else
				{
					Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(num5), MathF.Round(num6), num7, num7), renderColor, null, (!particle.Additive) ? GameBlendModes.None : GameBlendModes.Add);
				}
			}
		}
	}

	public static int DrawAirborne(TimeSpan deltaTime)
	{
		if (States.Count == 0)
		{
			return 0;
		}
		List<Entity> list = new List<Entity>();
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value != null && !value.IsHidden && IsAirborne(value))
			{
				list.Add(value);
			}
		}
		list.Sort((Entity a, Entity b) => (a.Origin.Y + Altitude(a)).CompareTo(b.Origin.Y + Altitude(b)));
		foreach (Entity item in list)
		{
			Globals.OnGameDraw(DrawStates.BeforeEntity, item, deltaTime);
			item.Draw();
			Globals.OnGameDraw(DrawStates.AfterEntity, item, deltaTime);
		}
		if (States.Count > 64)
		{
			foreach (Guid item2 in States.Keys.Where((Guid id) => !Globals.Entities.ContainsKey(id)).ToList())
			{
				States.Remove(item2);
			}
		}
		return list.Count;
	}

	private static State StateOf(Entity entity)
	{
		if (!States.TryGetValue(entity.Id, out State value))
		{
			value = new State
			{
				LastAt = Timing.Global.Milliseconds,
				LastX = entity.Origin.X,
				LastY = entity.Origin.Y
			};
			States[entity.Id] = value;
		}
		return value;
	}

	private static float AltitudeOf(FlightMode mode)
	{
		return mode switch
		{
			FlightMode.Cloud => 56f, 
			FlightMode.Ki => 26f, 
			_ => 0f, 
		};
	}

	internal static IGameTexture? FlightSheet(Entity entity)
	{
		return SheetOfForm(entity, "_vol.png");
	}

	private static IGameTexture? CloudSheet(Entity entity)
	{
		return SheetOfForm(entity, "_nuage.png");
	}

	private static IGameTexture? SheetOfForm(Entity entity, string suffix)
	{
		string text = (string.IsNullOrWhiteSpace(entity.TransformedSprite) ? entity.Sprite : Transformation.TextureNameFor(entity.Sprite, entity.TransformedSprite));
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		return Globals.ContentManager.GetTexture(TextureType.Entity, Path.GetFileNameWithoutExtension(text) + suffix);
	}

	private static float Ease(float t)
	{
		t = Math.Clamp(t, 0f, 1f);
		return 1f - (1f - t) * (1f - t) * (1f - t);
	}

	private static bool IsSprintingInFlight(Entity entity, State state, long now)
	{
		bool flag = state.Mode == FlightMode.Ki && entity.IsSprinting && !IsStill(state, now);
		bool flag2 = flag;
		if (flag2)
		{
			FlightEvent flightEvent = state.Event;
			bool flag3 = (uint)flightEvent <= 1u;
			flag2 = flag3;
		}
		if (flag2)
		{
			return !Altitude0(state, now);
		}
		return false;
	}

	private static (float X, float Y) DirectionVector(Direction direction)
	{
		return direction switch
		{
			Direction.Up => (X: 0f, Y: -1f), 
			Direction.Left => (X: -1f, Y: 0f), 
			Direction.Right => (X: 1f, Y: 0f), 
			Direction.UpLeft => (X: -0.7071f, Y: -0.7071f), 
			Direction.UpRight => (X: 0.7071f, Y: -0.7071f), 
			Direction.DownLeft => (X: -0.7071f, Y: 0.7071f), 
			Direction.DownRight => (X: 0.7071f, Y: 0.7071f), 
			_ => (X: 0f, Y: 1f), 
		};
	}

	private static void UpdateSprint(Entity entity, State state, float originX, float originY, bool sprinting, long dt, long now)
	{
		SprintState sprint = state.Sprint;
		if (sprinting)
		{
			(sprint.DirX, sprint.DirY) = DirectionVector(entity.DirectionFacing);
		}
		float dirX = sprint.DirX;
		float dirY = sprint.DirY;
		Color color = Transformation.KiColorOf(entity.TransformedSprite);
		Color color2 = new Color(255, color.R, color.G, color.B);
		float num = originX - 16f + (sprint.BodyLeft + sprint.BodyRight) / 2f;
		float num2 = originY - 48f - 9f - KiBob(state, now) + (sprint.BodyTop + sprint.BodyBottom) / 2f;
		if (sprinting && !sprint.Active)
		{
			sprint.Active = true;
			sprint.StartedAt = now;
			Burst(num, num2, originX, originY + Altitude(entity), dirX, dirY, color2, now);
		}
		else if (!sprinting && sprint.Active)
		{
			sprint.Active = false;
			for (int i = 0; i < 7; i++)
			{
				float num3 = (float)(Noise.NextDouble() * 20.0 - 10.0);
				float num4 = 30f + (float)Noise.NextDouble() * 30f;
				Particles.Add(new Particle
				{
					X = num + dirX * 10f,
					Y = num2 + (float)(Noise.NextDouble() * 6.0 - 3.0),
					Vx = dirX * num4 - dirY * num3,
					Vy = dirY * num4 + dirX * num3,
					BornAt = now,
					LifeMs = 200 + Noise.Next(120),
					Size = 1,
					Color = color2,
					Additive = true,
					Shrinks = true
				});
			}
		}
		if (sprint.Active)
		{
			sprint.Trail.Insert(0, new TrailPoint(num, num2, now));
		}
		while (sprint.Trail.Count > 0)
		{
			List<TrailPoint> trail = sprint.Trail;
			if (now - trail[trail.Count - 1].At <= 280)
			{
				break;
			}
			sprint.Trail.RemoveAt(sprint.Trail.Count - 1);
		}
		if (sprint.Active && Particles.Count <= 400)
		{
			float num5 = Math.Min(Speed(state) * 1000f, 600f);
			if (Noise.NextDouble() < (double)((float)dt / 30f))
			{
				float num6 = (float)(Noise.NextDouble() * 40.0 - 10.0);
				float num7 = ((Noise.Next(2) == 0) ? (-1f) : 1f) * (8f + (float)Noise.NextDouble() * 14f);
				float num8 = 220f + (float)Noise.NextDouble() * 80f + 127f - num5;
				Particles.Add(new Particle
				{
					X = num + dirX * num6 - dirY * num7,
					Y = num2 + dirY * num6 + dirX * num7,
					Vx = (0f - dirX) * num8,
					Vy = (0f - dirY) * num8,
					BornAt = now,
					LifeMs = 150,
					Size = 1,
					Length = 6 + Noise.Next(9),
					LineX = 0f - dirX,
					LineY = 0f - dirY,
					Color = new Color(140, 255, 255, 255)
				});
			}
			if (Noise.NextDouble() < (double)((float)dt / 10f))
			{
				float num9 = (float)(Noise.NextDouble() * 6.0 - 3.0);
				float num10 = 60f + (float)Noise.NextDouble() * 40f + 127f - num5;
				float num11 = (float)(Noise.NextDouble() * 12.0 - 6.0);
				Particles.Add(new Particle
				{
					X = num - dirX * 14f - dirY * num9,
					Y = num2 - dirY * 14f + dirX * num9,
					Vx = (0f - dirX) * num10 - dirY * num11,
					Vy = (0f - dirY) * num10 + dirX * num11,
					BornAt = now,
					LifeMs = 160 + Noise.Next(100),
					Size = 1,
					Color = color2,
					Additive = true
				});
			}
		}
	}

	private static void Burst(float centerX, float centerY, float groundX, float groundY, float nx, float ny, Color kiColor, long now)
	{
		AirRings.Add(new AirRing
		{
			X = centerX,
			Y = centerY,
			R0 = 6f,
			R1 = 30f,
			Alpha = 0.9f,
			Born = now,
			Duration = 300L
		});
		AirRings.Add(new AirRing
		{
			X = centerX,
			Y = centerY,
			R0 = 4f,
			R1 = 20f,
			Alpha = 0.7f,
			Color = kiColor,
			Born = now + 70,
			Duration = 300L
		});
		if (!Meteo.SouffleDeVolSurLEau(groundX, groundY, 1.8f))
		{
			Breaths.Add(new Breath
			{
				X = groundX,
				Y = groundY,
				BornAt = now,
				R0 = 6f,
				R1 = 34f,
				Duration = 700L,
				Alpha = 140
			});
			float num = MathF.Atan2(0f - ny, 0f - nx);
			for (int i = 0; i < 14; i++)
			{
				float x = num + (float)(Noise.NextDouble() * 1.4 - 0.7);
				float num2 = (16f + (float)Noise.NextDouble() * 18f) * 1.6f;
				GroundParticles.Add(new Particle
				{
					X = groundX + MathF.Cos(x) * 4f,
					Y = groundY + MathF.Sin(x) * 1.5f,
					Vx = MathF.Cos(x) * num2,
					Vy = MathF.Sin(x) * num2 * 0.3f - 2f - (float)Noise.NextDouble() * 6f,
					BornAt = now,
					LifeMs = 420 + Noise.Next(280),
					Size = ((!(Noise.NextDouble() < 0.4)) ? 1 : 2),
					Color = new Color(140, 205, 196, 170),
					Shrinks = true
				});
			}
		}
		for (int j = 0; j < 12; j++)
		{
			double num3 = Noise.NextDouble() * Math.PI * 2.0;
			Particles.Add(new Particle
			{
				X = centerX,
				Y = centerY,
				Vx = (float)Math.Cos(num3) * (40f + (float)Noise.NextDouble() * 40f),
				Vy = (float)Math.Sin(num3) * (20f + (float)Noise.NextDouble() * 30f),
				BornAt = now,
				LifeMs = 180 + Noise.Next(140),
				Size = 1,
				Color = kiColor,
				Additive = true,
				Shrinks = true
			});
		}
	}

	public static void DrawSprintBehind(Entity entity, FloatRect body, IGameTexture texture, FloatRect source)
	{
		if (!States.TryGetValue(entity.Id, out State value))
		{
			return;
		}
		SprintState sprint = value.Sprint;
		long now = Timing.Global.Milliseconds;
		long num = ((sprint.LastDrawAt == 0L) ? 16 : Math.Clamp(now - sprint.LastDrawAt, 0L, 50L));
		sprint.LastDrawAt = now;
		if (!sprint.Active && sprint.Tongues.Count == 0 && sprint.Ghosts.Count == 0 && sprint.Trail.Count < 2)
		{
			return;
		}
		if (sprint.Active && source.Width > 0f)
		{
			((int, int)[], (int, int)[])? tuple = FormAura.ContourOf(texture, source);
			if (tuple.HasValue)
			{
				((int, int)[], (int, int)[]) valueOrDefault = tuple.GetValueOrDefault();
				var (array, _) = valueOrDefault;
				if (array != null && array.Length > 0)
				{
					(int, int)[] pixels;
					if (valueOrDefault.Item2.Length == 0)
					{
						(pixels, _) = valueOrDefault;
					}
					else
					{
						pixels = valueOrDefault.Item2;
					}
					MeasureBody(sprint, pixels);
					sprint.TongueDebt += (float)num * 0.16f;
					while (sprint.TongueDebt >= 1f)
					{
						sprint.TongueDebt -= 1f;
						bool flag = valueOrDefault.Item2.Length != 0 && Noise.NextDouble() < 0.3;
						(int, int)[] array2;
						if (!flag)
						{
							(array2, _) = valueOrDefault;
						}
						else
						{
							array2 = valueOrDefault.Item2;
						}
						(int, int)[] array3 = array2;
						(int, int) tuple5 = array3[Noise.Next(array3.Length)];
						int item = tuple5.Item1;
						int item2 = tuple5.Item2;
						int num2 = (flag ? 2 : Noise.Next(2));
						float num3 = ((num2 == 2) ? 0.7f : 1f);
						sprint.Tongues.Add(new Tongue
						{
							OffsetX = (float)item + 0.5f,
							OffsetY = (float)item2 + 0.5f,
							Push = 40f + (float)Noise.NextDouble() * 40f + 127f,
							Drift = (float)(Noise.NextDouble() * 10.0 - 5.0),
							Born = now,
							Life = (long)((float)(150 + Noise.Next(140)) * num3),
							Length = (7f + (float)Noise.NextDouble() * 6f) * num3,
							Width = (3f + (float)Noise.NextDouble() * 2f) * ((num2 == 2) ? 0.8f : 1f),
							Seed = (float)(Noise.NextDouble() * Math.PI * 2.0),
							Layer = num2
						});
					}
				}
			}
			if (now - sprint.LastGhostAt > 45)
			{
				IGameTexture gameTexture = KiCharge.MaskOf(texture, source, 0);
				if (gameTexture != null)
				{
					sprint.LastGhostAt = now;
					sprint.Ghosts.Insert(0, new Ghost
					{
						Mask = gameTexture,
						Born = now
					});
				}
			}
		}
		sprint.Tongues.RemoveAll((Tongue tongue) => now - tongue.Born >= tongue.Life);
		while (sprint.Ghosts.Count > 0)
		{
			if (sprint.Ghosts.Count <= 6)
			{
				long num4 = now;
				List<Ghost> ghosts = sprint.Ghosts;
				if (num4 - ghosts[ghosts.Count - 1].Born <= 240)
				{
					break;
				}
			}
			sprint.Ghosts.RemoveAt(sprint.Ghosts.Count - 1);
		}
		Color color = Transformation.KiColorOf(entity.TransformedSprite);
		DrawTrail(sprint, color, now);
		for (int num5 = 1; num5 < sprint.Ghosts.Count; num5++)
		{
			Ghost ghost = sprint.Ghosts[num5];
			float num6 = (float)Math.Max((long)num5 * 45L, now - ghost.Born) / 240f;
			if (!(num6 >= 1f))
			{
				Graphics.DrawGameTexture(ghost.Mask, new FloatRect(0f, 0f, ghost.Mask.Width, ghost.Mask.Height), new FloatRect(MathF.Round(body.X - sprint.DirX * 5.7f * (float)num5), MathF.Round(body.Y - sprint.DirY * 5.7f * (float)num5), body.Width, body.Height), new Color((int)(97f * (1f - num6)), color.R, color.G, color.B), null, GameBlendModes.Add);
			}
		}
		DrawTongues(entity, sprint, body, now, front: false);
	}

	public static void DrawSprintFront(Entity entity, FloatRect body, IGameTexture texture, FloatRect source)
	{
		if (!States.TryGetValue(entity.Id, out State value))
		{
			return;
		}
		SprintState sprint = value.Sprint;
		long milliseconds = Timing.Global.Milliseconds;
		DrawTongues(entity, sprint, body, milliseconds, front: true);
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture == null)
		{
			return;
		}
		if (sprint.Active)
		{
			float dirX = sprint.DirX;
			float dirY = sprint.DirY;
			bool flag = MathF.Abs(dirX) >= MathF.Abs(dirY);
			float num = body.X + (sprint.BodyLeft + sprint.BodyRight) / 2f;
			float num2 = body.Y + (sprint.BodyTop + sprint.BodyBottom) / 2f;
			float num3 = (flag ? ((sprint.BodyBottom - sprint.BodyTop) / 2f) : ((sprint.BodyRight - sprint.BodyLeft) / 2f));
			float x = (flag ? (((dirX > 0f) ? (body.X + sprint.BodyRight + 1f) : (body.X + sprint.BodyLeft)) - num) : (((dirY > 0f) ? (body.Y + sprint.BodyBottom + 1f) : (body.Y + sprint.BodyTop)) - num2));
			long num4 = milliseconds / 50 % 2;
			for (int i = 0; i < 2; i++)
			{
				float num5 = 4f + (float)(i * 2) + (float)num4;
				float num6 = Math.Max(4f, num3 + 1f) + (float)(i * 2);
				float num7 = MathF.Abs(x) + 2f + (float)(i * 3);
				float num8 = (flag ? (num + (float)MathF.Sign(dirX) * num7) : (num + dirX * num7 * 0.5f));
				float num9 = (flag ? (num2 + dirY * num7 * 0.5f) : (num2 + (float)MathF.Sign(dirY) * num7));
				Color renderColor = new Color((i == 0) ? 128 : 56, 255, 255, 255);
				for (float num10 = -1.1f; num10 <= 1.1f; num10 += 0.12f)
				{
					float num11 = MathF.Cos(num10) * num5 - num5;
					float num12 = MathF.Sin(num10) * num6;
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(num8 + dirX * num11 - dirY * num12), MathF.Round(num9 + dirY * num11 + dirX * num12), 1f, 1f), renderColor, null, GameBlendModes.Add);
				}
			}
		}
		long num13 = milliseconds - sprint.StartedAt;
		if (num13 < 200 && source.Width > 0f)
		{
			IGameTexture gameTexture2 = KiCharge.MaskOf(texture, source, 0);
			if (gameTexture2 != null)
			{
				Graphics.DrawGameTexture(gameTexture2, new FloatRect(0f, 0f, gameTexture2.Width, gameTexture2.Height), body, new Color((int)(217f * (1f - (float)num13 / 200f)), 255, 255, 255), null, GameBlendModes.Add);
			}
		}
	}

	private static void MeasureBody(SprintState sprint, (int X, int Y)[] pixels)
	{
		if (pixels.Length != 0)
		{
			int num = int.MaxValue;
			int num2 = int.MinValue;
			int num3 = int.MaxValue;
			int num4 = int.MinValue;
			for (int i = 0; i < pixels.Length; i++)
			{
				(int X, int Y) tuple = pixels[i];
				int item = tuple.X;
				int item2 = tuple.Y;
				num = Math.Min(num, item);
				num2 = Math.Max(num2, item);
				num3 = Math.Min(num3, item2);
				num4 = Math.Max(num4, item2);
			}
			sprint.BodyLeft = num;
			sprint.BodyRight = num2;
			sprint.BodyTop = num3;
			sprint.BodyBottom = num4;
		}
	}

	private static void DrawTrail(SprintState sprint, Color ki, long now)
	{
		if (sprint.Trail.Count < 2)
		{
			return;
		}
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture == null)
		{
			return;
		}
		float dirX = sprint.DirX;
		float dirY = sprint.DirY;
		float num = 0f;
		for (int i = 0; i < sprint.Trail.Count - 1; i++)
		{
			if (!(num < 36f))
			{
				break;
			}
			TrailPoint trailPoint = sprint.Trail[i];
			TrailPoint trailPoint2 = sprint.Trail[i + 1];
			float num2 = trailPoint2.X - trailPoint.X;
			float num3 = trailPoint2.Y - trailPoint.Y;
			int num4 = (int)MathF.Round(MathF.Max(MathF.Abs(num2), MathF.Abs(num3)));
			if (num4 == 0)
			{
				continue;
			}
			int num5 = num4;
			num4 = Math.Min(num4, (int)MathF.Ceiling(36f - num));
			float num6 = Math.Clamp(Math.Max((float)(now - trailPoint.At) / 280f, (num + (float)num4 / 2f) / 36f), 0f, 1f);
			num += (float)num4;
			int num7 = Math.Max(1, (int)MathF.Round(5f * (1f - num6)));
			int num8 = Math.Max(1, (int)MathF.Round((float)num7 / 2f));
			Color renderColor = new Color((int)(82f * (1f - num6)), ki.R, ki.G, ki.B);
			Color renderColor2 = new Color((int)(128f * MathF.Pow(1f - num6, 1.5f)), 255, 255, 255);
			bool flag = MathF.Abs(num2) >= MathF.Abs(num3);
			for (int j = 0; j < num4; j++)
			{
				float num9 = trailPoint.X + num2 * (float)j / (float)num5 - dirX * 8f;
				float num10 = trailPoint.Y + num3 * (float)j / (float)num5 - dirY * 8f;
				float num11 = MathF.Sin((num9 + num10 + (float)now / 20f) / 5f) * 0.6f;
				if (flag)
				{
					num10 += num11;
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(num9), MathF.Round(num10 - (float)num7 / 2f - 1f), 1f, num7 + 2), renderColor, null, GameBlendModes.Add);
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(num9), MathF.Round(num10 - (float)num8 / 2f), 1f, num8), renderColor2, null, GameBlendModes.Add);
				}
				else
				{
					num9 += num11;
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(num9 - (float)num7 / 2f - 1f), MathF.Round(num10), num7 + 2, 1f), renderColor, null, GameBlendModes.Add);
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(num9 - (float)num8 / 2f), MathF.Round(num10), num8, 1f), renderColor2, null, GameBlendModes.Add);
				}
			}
		}
	}

	private static void DrawTongues(Entity entity, SprintState sprint, FloatRect body, long now, bool front)
	{
		if (sprint.Tongues.Count == 0)
		{
			return;
		}
		IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Misc, "ui_langue.png");
		if (gameTexture == null)
		{
			return;
		}
		Color item = Transformation.KiColorOf(entity.TransformedSprite);
		Color item2 = Transformation.KiEdgeColorOf(entity.TransformedSprite);
		foreach (Tongue tongue in sprint.Tongues)
		{
			if (tongue.Layer == 2 == front)
			{
				float num = (float)(now - tongue.Born) / (float)tongue.Life;
				if (!(num >= 1f))
				{
					float num2 = ((num < 0.2f) ? (num / 0.2f) : (1f - (num - 0.2f) / 0.8f));
					(Color, float, GameBlendModes) tuple = tongue.Layer switch
					{
						0 => (item2, 0.5f, GameBlendModes.None), 
						1 => (item, 0.5f, GameBlendModes.Add), 
						_ => (new Color(255, 255, 255, 255), 0.55f, GameBlendModes.Add), 
					};
					Color item3 = tuple.Item1;
					float item4 = tuple.Item2;
					GameBlendModes item5 = tuple.Item3;
					float num3 = (float)(now - tongue.Born) / 1000f;
					float num4 = tongue.Push * num3;
					float num5 = tongue.Drift * num3 + MathF.Sin((float)now / 60f + tongue.Seed) * 0.8f;
					DrawSprintTongue(gameTexture, body.X + tongue.OffsetX - sprint.DirX * num4 - sprint.DirY * num5, body.Y + tongue.OffsetY - sprint.DirY * num4 + sprint.DirX * num5, tongue.Length * (0.6f + num * 0.9f), tongue.Width * (1.1f - num * 0.5f), 0f - sprint.DirX, 0f - sprint.DirY, new Color((int)Math.Clamp(255f * item4 * num2, 0f, 255f), item3.R, item3.G, item3.B), item5);
				}
			}
		}
	}

	private static void DrawSprintTongue(IGameTexture langue, float x, float y, float length, float width, float tipX, float tipY, Color color, GameBlendModes blend)
	{
		if (color.A > 0 && !(length <= 0.2f) && !(width <= 0.2f))
		{
			float num = length * 1.16f;
			float num2 = x + tipX * num * 0.36f;
			float num3 = y + tipY * num * 0.36f;
			float rotationDegrees = MathF.Atan2(tipX, 0f - tipY) * 180f / (float)Math.PI;
			Graphics.DrawGameTexture(langue, new FloatRect(0f, 0f, langue.Width, langue.Height), new FloatRect(num2 - width / 2f, num3 - num / 2f, width, num), color, null, blend, null, rotationDegrees);
		}
	}

	private static void DrawAirRings(IGameTexture pixel, long now)
	{
		for (int num = AirRings.Count - 1; num >= 0; num--)
		{
			AirRing airRing = AirRings[num];
			long num2 = now - airRing.Born;
			if (num2 >= airRing.Duration)
			{
				AirRings.RemoveAt(num);
			}
			else if (num2 >= 0)
			{
				float num3 = (float)num2 / (float)airRing.Duration;
				float num4 = airRing.R0 + (airRing.R1 - airRing.R0) * Ease(num3);
				float num5 = num4 * 0.62f;
				Color renderColor = new Color((int)(255f * airRing.Alpha * (1f - num3)), airRing.Color.R, airRing.Color.G, airRing.Color.B);
				int num6 = Math.Max(10, (int)MathF.Round(num4 * 3.2f));
				for (int i = 0; i < num6; i++)
				{
					float x = (float)i / (float)num6 * ((float)Math.PI * 2f);
					Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(airRing.X + MathF.Cos(x) * num4), MathF.Round(airRing.Y + MathF.Sin(x) * num5), 1f, 1f), renderColor, null, GameBlendModes.Add);
				}
			}
		}
	}

	private static void DrawSprintWakes(long now)
	{
		long num = ((_lastWakeAt == 0L) ? 16 : Math.Clamp(now - _lastWakeAt, 0L, 50L));
		_lastWakeAt = now;
		if (Globals.Entities == null)
		{
			return;
		}
		foreach (var (key, state2) in States)
		{
			if (state2.Sprint.Active && Globals.Entities.TryGetValue(key, out Entity value) && value != null && !value.IsHidden)
			{
				float x = value.Origin.X;
				float num2 = value.Origin.Y + Altitude(value);
				float dirX = state2.Sprint.DirX;
				float dirY = state2.Sprint.DirY;
				Color lumiere = Transformation.KiColorOf(value.TransformedSprite);
				if (!Meteo.KiSurLEau(value, x, num2, dirX, dirY, 0.6f, lumiere) && GroundParticles.Count <= 300 && !(Noise.NextDouble() >= (double)((float)num / 35f)))
				{
					float x2 = MathF.Atan2(0f - dirY, 0f - dirX) + (float)(Noise.NextDouble() * 1.4 - 0.7);
					float num3 = 16f + (float)Noise.NextDouble() * 18f;
					GroundParticles.Add(new Particle
					{
						X = x - dirX * 4f + MathF.Cos(x2) * 4f,
						Y = num2 - dirY * 4f + MathF.Sin(x2) * 1.5f,
						Vx = MathF.Cos(x2) * num3,
						Vy = MathF.Sin(x2) * num3 * 0.3f - 2f - (float)Noise.NextDouble() * 6f,
						BornAt = now,
						LifeMs = 420 + Noise.Next(280),
						Size = ((!(Noise.NextDouble() < 0.4)) ? 1 : 2),
						Color = new Color(140, 205, 196, 170),
						Shrinks = true
					});
				}
			}
		}
	}
}
