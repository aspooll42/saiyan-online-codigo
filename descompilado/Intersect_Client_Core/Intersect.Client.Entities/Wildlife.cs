using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Maps;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Entities;

public static class Wildlife
{
	private sealed class Butterfly
	{
		public float X;

		public float Y;

		public float Z = 9f;

		public float HomeX;

		public float HomeY;

		public float TargetX;

		public float TargetY;

		public float Phase;

		public bool HasTarget;

		public int Mode;

		public float Timer;

		public int Palette;

		public float Fade;
	}

	private sealed class Hopper
	{
		public float X0;

		public float Y0;

		public float X1;

		public float Y1;

		public float Height;

		public float Duration;

		public float Time;

		public float Visible;

		public bool Jumping = true;
	}

	private sealed class Bird
	{
		public float X0;

		public float Y0;

		public float X;

		public float Y;

		public float Z;

		public float Time;

		public int Mode;

		public int Dir = 1;
	}

	private sealed class Flock
	{
		public readonly List<Bird> Birds = new List<Bird>();

		public float CenterX;

		public float CenterY;

		public float Away;
	}

	private static readonly List<Butterfly> Butterflies = new List<Butterfly>();

	private static readonly List<Hopper> Hoppers = new List<Hopper>();

	private static readonly List<Flock> Flocks = new List<Flock>();

	private static readonly List<(float X, float Y)> Flowers = new List<(float, float)>();

	private static readonly List<(float X, float Y)> Raw = new List<(float, float)>();

	private static readonly HashSet<(int, int)> Cells = new HashSet<(int, int)>();

	private static long _reportAt;

	private static readonly List<(float X, float Y, bool Moving, bool Charging)> Walkers = new List<(float, float, bool, bool)>();

	private static readonly Dictionary<Guid, (float X, float Y, float Walked)> Steps = new Dictionary<Guid, (float, float, float)>();

	private static readonly Random Noise = new Random();

	private static readonly SuiviGrille Grille = new SuiviGrille();

	private static long _lastUpdate;

	private static long _flowersAt;

	private static long _nextFlockAt;

	private static bool _broken;

	private static readonly Color[][] Palettes = new Color[4][]
	{
		new Color[2]
		{
			new Color(255, 244, 244, 232),
			new Color(255, 60, 60, 70)
		},
		new Color[2]
		{
			new Color(255, 250, 214, 70),
			new Color(255, 120, 80, 20)
		},
		new Color[2]
		{
			new Color(255, 120, 176, 255),
			new Color(255, 30, 50, 110)
		},
		new Color[2]
		{
			new Color(255, 255, 150, 60),
			new Color(255, 90, 40, 10)
		}
	};

	private static readonly Dictionary<char, Color> BirdColors = new Dictionary<char, Color>
	{
		['H'] = new Color(255, 110, 74, 44),
		['E'] = new Color(255, 20, 16, 14),
		['>'] = new Color(255, 244, 196, 60),
		['B'] = new Color(255, 132, 92, 56),
		['T'] = new Color(255, 70, 48, 30),
		['W'] = new Color(255, 226, 206, 168),
		['A'] = new Color(255, 92, 62, 38),
		['l'] = new Color(255, 60, 40, 26)
	};

	private static readonly string[] BirdStand = new string[6] { "....HH..", "...HHHE>", "TBBBBBB.", ".TBWWWB.", "...WWW..", "...l.l.." };

	private static readonly string[] BirdPeck = new string[6] { "........", "........", "TBBBBHH.", ".TBWWHHE", "...WWW.>", "...l.l.." };

	private static readonly string[] BirdWingsUp = new string[6] { "..AA....", "..AAA...", "TBBBBHE>", "..BWWB..", "........", "........" };

	private static readonly string[] BirdWingsDown = new string[6] { "........", "........", "TBBBBHE>", "..BWWB..", "..AAA...", "...AA..." };

	private static readonly string[] WingsOpen = new string[5] { "UU.UU", "UUbUU", "UUbUU", ".LbL.", ".L.L." };

	private static readonly string[] WingsClosed = new string[5] { "..U..", ".UbU.", ".UbU.", "..b..", "....." };

	private static float Between(float min, float max)
	{
		return min + (float)Noise.NextDouble() * (max - min);
	}

	public static void Scare(float x, float y, float radius)
	{
		foreach (Flock flock in Flocks)
		{
			foreach (Bird bird in flock.Birds)
			{
				if (bird.Mode == 0 && Distance(bird.X0 - x, bird.Y0 - y) < radius)
				{
					TakeOff(bird, x);
				}
			}
		}
		foreach (Butterfly butterfly in Butterflies)
		{
			if (Distance(butterfly.X - x, butterfly.Y - y) < radius)
			{
				Flee(butterfly, x, y);
			}
		}
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
			float dt = ((_lastUpdate == 0L) ? 0.016f : Math.Clamp((float)(milliseconds - _lastUpdate) / 1000f, 0f, 0.05f));
			_lastUpdate = milliseconds;
			Update(milliseconds, dt);
			if (Graphics.Renderer?.WhitePixel == null)
			{
				return;
			}
			foreach (Hopper hopper in Hoppers)
			{
				DrawHopper(hopper);
			}
			foreach (Butterfly butterfly in Butterflies)
			{
				Dot(butterfly.X - 1f, butterfly.Y, 2f, 1f, new Color((int)(56f * butterfly.Fade), 20, 30, 10));
			}
			foreach (Flock flock in Flocks)
			{
				foreach (Bird bird in flock.Birds)
				{
					if (bird.Mode != 2)
					{
						float num = MathF.Max(0.3f, 1f - bird.Z / 80f);
						Dot(bird.X - 2f * num, bird.Y, MathF.Max(1f, MathF.Round(5f * num)), 1f, new Color((int)(72f * num), 20, 30, 10));
						if (bird.Mode == 0)
						{
							DrawBird(bird.X, bird.Y, bird.Dir, ((int)(bird.Time * 1000f / 260f) % 5 < 2) ? BirdPeck : BirdStand);
						}
					}
				}
			}
		}
		catch (Exception exception)
		{
			_broken = true;
			Clear();
			ApplicationContext.Context.Value?.Logger.LogError(exception, "Petite faune coupee apres un incident");
		}
	}

	public static void DrawAir()
	{
		if (_broken || Graphics.Renderer?.WhitePixel == null)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		foreach (Butterfly butterfly in Butterflies)
		{
			DrawButterfly(butterfly, milliseconds);
		}
		foreach (Flock flock in Flocks)
		{
			foreach (Bird bird in flock.Birds)
			{
				int mode = bird.Mode;
				if ((mode == 1 || mode == 3) ? true : false)
				{
					DrawBird(bird.X, bird.Y - bird.Z, bird.Dir, ((int)(bird.Time * 1000f / 70f) % 2 == 0) ? BirdWingsUp : BirdWingsDown);
				}
			}
		}
	}

	private static void Update(long now, float dt)
	{
		Player me = Globals.Me;
		if (me == null || !Meteo.Dehors)
		{
			Clear();
			if (me != null && now >= _reportAt)
			{
				_reportAt = now + 20000;
				ApplicationContext.Context.Value?.Logger.LogInformation("Faune : pas dehors pour la meteo (interieur ou meteo coupee), aucune bete");
			}
			return;
		}
		if (!Grille.Continu())
		{
			Clear();
		}
		CollectWalkers(dt);
		FloatRect worldViewport = Graphics.WorldViewport;
		float partDuJour = Meteo.PartDuJour;
		bool flag = !Meteo.MauvaisTemps;
		UpdateHoppers(dt);
		UpdateButterflies(now, dt, worldViewport, flag && partDuJour > 0.3f);
		UpdateBirds(now, dt, worldViewport, flag && partDuJour > 0.2f);
		if (now >= _reportAt)
		{
			_reportAt = now + 20000;
			ApplicationContext.Context.Value?.Logger.LogInformation("Faune : jour {Jour:0.00}, beau temps {Beau}, fleurs {Fleurs}, papillons {Papillons}, groupes d'oiseaux {Groupes}, sauterelles {Sauterelles}, herbe sous les pieds {Herbe}", partDuJour, flag, Flowers.Count, Butterflies.Count, Flocks.Count, Hoppers.Count, Meteo.EstHerbe(me.Origin.X, me.Origin.Y - 1f));
		}
	}

	private static void Clear()
	{
		Butterflies.Clear();
		Hoppers.Clear();
		Flocks.Clear();
		Steps.Clear();
		_flowersAt = 0L;
	}

	private static void CollectWalkers(float dt)
	{
		Walkers.Clear();
		if (Globals.Entities == null)
		{
			return;
		}
		FloatRect worldViewport = Graphics.WorldViewport;
		foreach (Entity value2 in Globals.Entities.Values)
		{
			if (value2 == null || value2.IsHidden || (value2.Type != EntityType.Player && value2.Type != EntityType.GlobalEntity) || value2 is Critter || Flight.IsAirborne(value2))
			{
				continue;
			}
			Vector2 origin = value2.Origin;
			if (origin.X < worldViewport.Left - 64f || origin.X > worldViewport.Right + 64f || origin.Y < worldViewport.Top - 64f || origin.Y > worldViewport.Bottom + 64f)
			{
				continue;
			}
			Walkers.Add((origin.X, origin.Y, value2.IsMoving, value2.IsCharging || value2.IsTransforming));
			if (!Steps.TryGetValue(value2.Id, out (float, float, float) value) || MathF.Abs(origin.X - value.Item1) + MathF.Abs(origin.Y - value.Item2) > 48f)
			{
				Steps[value2.Id] = (origin.X, origin.Y, 0f);
				continue;
			}
			float num = value.Item3 + MathF.Abs(origin.X - value.Item1) + MathF.Abs(origin.Y - value.Item2);
			if (num >= 20f)
			{
				num = 0f;
				if (Noise.NextDouble() < 0.22 && Hoppers.Count < 24 && Meteo.EstHerbe(origin.X, origin.Y))
				{
					float x = Between(0f, (float)Math.PI * 2f);
					float num2 = origin.X + MathF.Cos(x) * Between(5f, 11f);
					float num3 = origin.Y + MathF.Sin(x) * Between(3f, 7f);
					if (Meteo.EstHerbe(num2, num3))
					{
						Jump(new Hopper
						{
							X1 = num2,
							Y1 = num3
						}, origin.X, origin.Y);
					}
				}
			}
			Steps[value2.Id] = (origin.X, origin.Y, num);
		}
		if (Steps.Count > 128)
		{
			Steps.Clear();
		}
	}

	private static void Jump(Hopper hopper, float fromX, float fromY)
	{
		float x = MathF.Atan2(hopper.Y1 - fromY, hopper.X1 - fromX) + Between(-0.6f, 0.6f);
		float num = Between(14f, 24f);
		hopper.X0 = hopper.X1;
		hopper.Y0 = hopper.Y1;
		hopper.X1 = hopper.X0 + MathF.Cos(x) * num;
		hopper.Y1 = hopper.Y0 + MathF.Sin(x) * num * 0.6f;
		hopper.Height = Between(9f, 15f);
		hopper.Duration = Between(0.34f, 0.44f);
		hopper.Time = 0f;
		hopper.Jumping = true;
		if (!Hoppers.Contains(hopper))
		{
			Hoppers.Add(hopper);
		}
	}

	private static void UpdateHoppers(float dt)
	{
		for (int num = Hoppers.Count - 1; num >= 0; num--)
		{
			Hopper hopper = Hoppers[num];
			if (hopper.Jumping)
			{
				hopper.Time += dt;
				if (hopper.Time >= hopper.Duration)
				{
					hopper.Jumping = false;
					hopper.Visible = 1.6f;
					foreach (var walker in Walkers)
					{
						if (walker.Moving && Distance(walker.X - hopper.X1, (walker.Y - hopper.Y1) * 1.4f) < 18f && Noise.NextDouble() < 0.5)
						{
							Jump(hopper, walker.X, walker.Y);
							break;
						}
					}
				}
			}
			else
			{
				hopper.Visible -= dt;
				if (hopper.Visible <= 0f)
				{
					Hoppers.RemoveAt(num);
				}
			}
		}
	}

	private static void DrawHopper(Hopper hopper)
	{
		float num2;
		float num3;
		float x;
		float x2;
		if (hopper.Jumping)
		{
			float num = Math.Clamp(hopper.Time / hopper.Duration, 0f, 1f);
			x = hopper.X0 + (hopper.X1 - hopper.X0) * num;
			x2 = hopper.Y0 + (hopper.Y1 - hopper.Y0) * num;
			num2 = MathF.Sin((float)Math.PI * num) * hopper.Height;
			num3 = 1f;
		}
		else
		{
			x = hopper.X1;
			x2 = hopper.Y1;
			num2 = 0f;
			num3 = Math.Min(1f, hopper.Visible / 0.5f);
		}
		x = MathF.Round(x);
		x2 = MathF.Round(x2);
		Dot(x - 1f, x2, 3f, 1f, new Color((int)(76f * num3), 30, 40, 10));
		float num4 = MathF.Round(x2 - num2);
		Dot(x - 1f, num4 - 1f, 3f, 1f, new Color((int)(255f * num3), 118, 162, 40));
		Dot(x + 1f, num4 - 2f, 1f, 1f, new Color((int)(255f * num3), 80, 110, 24));
		Dot(x - 2f, (num2 > 2f) ? num4 : (num4 - 1f), 1f, 1f, new Color((int)(255f * num3), 60, 80, 20));
	}

	private static void UpdateButterflies(long now, float dt, FloatRect view, bool allowed)
	{
		if (now - _flowersAt > 2000)
		{
			_flowersAt = now;
			Raw.Clear();
			foreach (MapInstance item in Meteo.CartesVisibles())
			{
				item.AjouterFleurs(Raw);
			}
			Meteo.AjouterFleursDuSol(Raw);
			Flowers.Clear();
			Cells.Clear();
			foreach (var (num, num2) in Raw)
			{
				if (!(num < view.Left - 32f) && !(num > view.Right + 32f) && !(num2 < view.Top - 32f) && !(num2 > view.Bottom + 32f) && Cells.Add(((int)MathF.Floor(num / 24f), (int)MathF.Floor(num2 / 24f))))
				{
					Flowers.Add((num, num2));
				}
			}
		}
		int num3 = ((allowed && Flowers.Count > 0) ? Math.Min(8, 2 + Flowers.Count / 6) : 0);
		if (Butterflies.Count < num3 && Noise.NextDouble() < (double)dt * 1.5)
		{
			var (num4, num5) = Flowers[Noise.Next(Flowers.Count)];
			Butterflies.Add(new Butterfly
			{
				X = num4 + Between(-10f, 10f),
				Y = num5 + Between(-6f, 6f),
				HomeX = num4,
				HomeY = num5,
				Phase = Between(0f, 6f),
				Palette = Noise.Next(Palettes.Length),
				Timer = Between(0.5f, 2f)
			});
		}
		for (int num6 = Butterflies.Count - 1; num6 >= 0; num6--)
		{
			Butterfly butterfly = Butterflies[num6];
			bool flag = !allowed || Butterflies.Count > num3 + 1 || butterfly.X < view.Left - 160f || butterfly.X > view.Right + 160f || butterfly.Y < view.Top - 160f || butterfly.Y > view.Bottom + 160f;
			butterfly.Fade = Math.Clamp(butterfly.Fade + (flag ? ((0f - dt) * 1.5f) : (dt * 2.5f)), 0f, 1f);
			if (flag && butterfly.Fade <= 0f)
			{
				Butterflies.RemoveAt(num6);
				continue;
			}
			foreach (var walker in Walkers)
			{
				if (butterfly.Mode != 2 && Distance(walker.X - butterfly.X, (walker.Y - butterfly.Y) * 1.4f) < 26f)
				{
					Flee(butterfly, walker.X, walker.Y);
				}
			}
			butterfly.Timer -= dt;
			if (butterfly.Mode == 1)
			{
				butterfly.Z += (0f - butterfly.Z) * MathF.Min(1f, dt * 8f);
				if (butterfly.Timer <= 0f)
				{
					butterfly.Mode = 0;
					butterfly.Timer = Between(1.5f, 3f);
					butterfly.HasTarget = false;
				}
				continue;
			}
			if (!butterfly.HasTarget || Distance(butterfly.TargetX - butterfly.X, butterfly.TargetY - butterfly.Y) < 3f)
			{
				if (butterfly.Mode == 0 && butterfly.HasTarget && Noise.NextDouble() < 0.35)
				{
					butterfly.Mode = 1;
					butterfly.Timer = Between(1.2f, 2.6f);
					butterfly.X = butterfly.HomeX + Between(-5f, 5f);
					butterfly.Y = butterfly.HomeY + Between(-3f, 3f);
					continue;
				}
				butterfly.TargetX = butterfly.HomeX + Between(-22f, 22f);
				butterfly.TargetY = butterfly.HomeY + Between(-14f, 14f);
				butterfly.HasTarget = true;
			}
			if (butterfly.Mode == 2 && butterfly.Timer <= 0f)
			{
				butterfly.Mode = 0;
				butterfly.HasTarget = false;
			}
			float num7 = ((butterfly.Mode == 2) ? 70f : 24f);
			float x = MathF.Atan2(butterfly.TargetY - butterfly.Y, butterfly.TargetX - butterfly.X) + MathF.Sin((float)now / 170f + butterfly.Phase) * 0.9f;
			butterfly.X += MathF.Cos(x) * num7 * dt;
			butterfly.Y += MathF.Sin(x) * num7 * dt;
			float num8 = ((butterfly.Mode == 2) ? 22f : (9f + MathF.Sin((float)now / 260f + butterfly.Phase) * 3f));
			butterfly.Z += (num8 - butterfly.Z) * MathF.Min(1f, dt * 3f);
		}
	}

	private static void Flee(Butterfly b, float fromX, float fromY)
	{
		b.Mode = 2;
		b.Timer = 2.2f;
		float x = MathF.Atan2(b.Y - fromY, b.X - fromX);
		b.TargetX = b.X + MathF.Cos(x) * 60f;
		b.TargetY = b.Y + MathF.Sin(x) * 40f;
		b.HasTarget = true;
	}

	private static void DrawButterfly(Butterfly b, long now)
	{
		float num = MathF.Round(b.X);
		float num2 = MathF.Round(b.Y - b.Z);
		bool flag = ((b.Mode == 1) ? (MathF.Sin((float)now / 600f + b.Phase) > 0.6f) : (MathF.Sin((float)now / 45f + b.Phase) > 0f));
		string[] array = (flag ? WingsOpen : WingsClosed);
		Color color = Palettes[b.Palette][0];
		Color color2 = Palettes[b.Palette][1];
		int a = (int)(255f * b.Fade);
		for (int i = 0; i < 5; i++)
		{
			for (int j = 0; j < 5; j++)
			{
				char c = array[i][j];
				if (c != '.')
				{
					Color color3 = c switch
					{
						'L' => new Color(255, (int)((float)(int)color.R * 0.8f), (int)((float)(int)color.G * 0.8f), (int)((float)(int)color.B * 0.8f)), 
						'b' => color2, 
						_ => color, 
					};
					Dot(num - 2f + (float)j, num2 - 5f + (float)i, 1f, 1f, new Color(a, color3.R, color3.G, color3.B));
				}
			}
		}
		if (flag)
		{
			Dot(num - 2f, num2 - 5f, 1f, 1f, new Color(a, color2.R, color2.G, color2.B));
			Dot(num + 2f, num2 - 5f, 1f, 1f, new Color(a, color2.R, color2.G, color2.B));
		}
	}

	private static void UpdateBirds(long now, float dt, FloatRect view, bool allowed)
	{
		if (allowed && Flocks.Count < 2 && now >= _nextFlockAt)
		{
			_nextFlockAt = now + (long)Between(4000f, 8000f);
			for (int i = 0; i < 12; i++)
			{
				float cx = Between(view.Left + 40f, view.Right - 40f);
				float cy = Between(view.Top + 40f, view.Bottom - 30f);
				if (!Meteo.EstHerbe(cx, cy) || Walkers.Any(((float X, float Y, bool Moving, bool Charging) w) => Distance(w.X - cx, w.Y - cy) < 110f) || Flocks.Any((Flock f) => Distance(f.CenterX - cx, f.CenterY - cy) < 80f))
				{
					continue;
				}
				Flock flock = new Flock
				{
					CenterX = cx,
					CenterY = cy
				};
				int num = Noise.Next(2, 5);
				for (int num2 = 0; num2 < num; num2++)
				{
					float num3 = cx + Between(-16f, 16f);
					float num4 = cy + Between(-10f, 10f);
					if (Meteo.EstHerbe(num3, num4))
					{
						flock.Birds.Add(new Bird
						{
							X0 = num3,
							Y0 = num4,
							X = num3,
							Y = num4,
							Time = Between(0f, 1f),
							Dir = ((Noise.Next(2) == 0) ? 1 : (-1))
						});
					}
				}
				if (flock.Birds.Count > 0)
				{
					Flocks.Add(flock);
				}
				break;
			}
		}
		for (int num5 = Flocks.Count - 1; num5 >= 0; num5--)
		{
			Flock flock2 = Flocks[num5];
			bool flag = true;
			foreach (Bird bird in flock2.Birds)
			{
				bird.Time += dt;
				switch (bird.Mode)
				{
				case 0:
					foreach (var walker in Walkers)
					{
						float num8 = Distance(walker.X - bird.X0, (walker.Y - bird.Y0) * 1.4f);
						if ((walker.Moving && num8 < 50f) || num8 < 26f || (walker.Charging && num8 < 90f))
						{
							TakeOff(bird, walker.X);
							break;
						}
					}
					break;
				case 1:
				{
					float num9 = MathF.Min(1f, bird.Time / 1.4f);
					bird.X = bird.X0 + (float)bird.Dir * 110f * num9 * num9;
					bird.Y = bird.Y0 - 30f * num9;
					bird.Z = 70f * num9 * num9 + 6f * MathF.Min(1f, bird.Time / 0.15f);
					if (num9 >= 1f)
					{
						bird.Mode = 2;
						bird.Time = 0f;
					}
					break;
				}
				case 2:
					if (bird.Time > 1.8f && allowed && Walkers.All(((float X, float Y, bool Moving, bool Charging) w) => Distance(w.X - bird.X0, (w.Y - bird.Y0) * 1.4f) > 110f))
					{
						bird.Mode = 3;
						bird.Time = 0f;
					}
					break;
				case 3:
				{
					float num6 = MathF.Min(1f, bird.Time / 1.5f);
					float num7 = 1f - num6;
					bird.X = bird.X0 + (float)bird.Dir * 110f * num7 * num7;
					bird.Y = bird.Y0 - 30f * num7;
					bird.Z = 70f * num7 * num7 + 6f * MathF.Min(1f, (1.5f - bird.Time) / 0.15f);
					if (num6 >= 1f)
					{
						bird.Mode = 0;
						bird.X = bird.X0;
						bird.Y = bird.Y0;
						bird.Z = 0f;
						bird.Time = 0f;
					}
					break;
				}
				}
				flag &= bird.Mode == 2;
			}
			flock2.Away = (flag ? (flock2.Away + dt) : 0f);
			bool flag2 = flock2.CenterX < view.Left - 150f || flock2.CenterX > view.Right + 150f || flock2.CenterY < view.Top - 150f || flock2.CenterY > view.Bottom + 150f;
			if (flock2.Away > 20f || (flag2 && flock2.Birds.All(delegate(Bird b)
			{
				int mode = b.Mode;
				return (mode == 0 || mode == 2) ? true : false;
			})) || (!allowed && flag))
			{
				Flocks.RemoveAt(num5);
			}
		}
	}

	private static void TakeOff(Bird bird, float fromX)
	{
		bird.Mode = 1;
		bird.Time = 0f;
		bird.Dir = ((bird.X0 >= fromX) ? 1 : (-1));
	}

	private static void DrawBird(float x, float y, int dir, string[] rows)
	{
		x = MathF.Round(x);
		y = MathF.Round(y);
		for (int i = 0; i < rows.Length; i++)
		{
			for (int j = 0; j < 8; j++)
			{
				char c = rows[i][j];
				if (c != '.' && BirdColors.TryGetValue(c, out Color value))
				{
					Dot(x + (float)((dir > 0) ? (j - 4) : (3 - j)), y - 5f + (float)i, 1f, 1f, value);
				}
			}
		}
	}

	private static float Distance(float dx, float dy)
	{
		return MathF.Sqrt(dx * dx + dy * dy);
	}

	private static void Dot(float x, float y, float width, float height, Color color)
	{
		if (color.A > 0)
		{
			IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
			if (gameTexture != null)
			{
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(x, y, width, height), color);
			}
		}
	}
}
