using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Maps;
using Intersect.Enums;
using Intersect.Framework.Core;

namespace Intersect.Client.Entities;

public static class DashAnime
{
	private sealed class Etat
	{
		public long AppuiA = -1L;

		public int AppuiSensX;

		public int AppuiSensY;

		public Dash? Glissade;

		public int SensX;

		public int SensY;

		public long ArriveeA = -1L;

		public long DernierTraitA;
	}

	private const long AppuiMs = 70L;

	private const long ArriveeMs = 90L;

	private const long TraitTousLesMs = 22L;

	private const string Son = "dash.wav";

	private static readonly Dictionary<Guid, Etat> Etats = new Dictionary<Guid, Etat>();

	private static readonly List<(float X, float Y, float Vx, float Vy, int Longueur, long NeA, long Vie)> Traits = new List<(float, float, float, float, int, long, long)>();

	private static readonly Random Hasard = new Random();

	private static Etat EtatDe(Entity entity)
	{
		if (Etats.Count > 128 && !Etats.ContainsKey(entity.Id))
		{
			Etats.Clear();
		}
		if (!Etats.TryGetValue(entity.Id, out Etat value))
		{
			value = new Etat();
			Etats[entity.Id] = value;
		}
		return value;
	}

	public static void Appui(Player joueur, float versX, float versY)
	{
		Etat etat = EtatDe(joueur);
		long milliseconds = Timing.Global.Milliseconds;
		etat.AppuiA = milliseconds;
		(etat.AppuiSensX, etat.AppuiSensY) = Sens(versX, versY);
		Poussiere(joueur, etat.AppuiSensX, etat.AppuiSensY, derriere: true);
	}

	public static void Depart(Entity entity, Dash dash)
	{
		Etat etat = EtatDe(entity);
		(float X, float Y) startOffset = dash.StartOffset;
		float item = startOffset.X;
		float item2 = startOffset.Y;
		(float X, float Y) endOffset = dash.EndOffset;
		float item3 = endOffset.X;
		float item4 = endOffset.Y;
		Etat etat2 = etat;
		(int, int) tuple = Sens(item3 - item, item4 - item2);
		etat.SensX = tuple.Item1;
		etat2.SensY = tuple.Item2;
		etat.Glissade = dash;
		etat.AppuiA = -1L;
		etat.ArriveeA = -1L;
		etat.DernierTraitA = 0L;
		Audio.AddMapSound("dash.wav", entity.X, entity.Y, entity.MapId, loop: false, 0, 12, entity);
	}

	public static void Observer(Entity entity)
	{
		if (Etats.Count == 0 || !Etats.TryGetValue(entity.Id, out Etat value))
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		if (value.Glissade == null)
		{
			return;
		}
		if (entity.Dashing != value.Glissade)
		{
			value.Glissade = null;
			value.ArriveeA = milliseconds;
			Poussiere(entity, value.SensX, value.SensY, derriere: false);
		}
		else
		{
			if (milliseconds - value.DernierTraitA < 22 || (value.SensX == 0 && value.SensY == 0))
			{
				return;
			}
			value.DernierTraitA = milliseconds;
			FloatRect worldPos = entity.WorldPos;
			if (!(worldPos.Width <= 0f) && !(worldPos.Height <= 0f))
			{
				for (int i = 0; i < 2; i++)
				{
					float num = worldPos.Y + worldPos.Height * (0.3f + 0.3f * (float)Hasard.NextDouble());
					float num2 = (float)(Hasard.NextDouble() - 0.5) * worldPos.Width * 0.6f;
					float item = worldPos.X + worldPos.Width / 2f + ((value.SensX != 0) ? ((float)(-value.SensX) * (worldPos.Width / 2f + 1f)) : num2);
					float item2 = ((value.SensY != 0) ? ((value.SensY > 0) ? (worldPos.Y - 1f) : (worldPos.Y + worldPos.Height - 2f)) : num);
					float num3 = 150f + (float)Hasard.NextDouble() * 90f;
					Traits.Add((item, item2, (float)(-value.SensX) * num3, (float)(-value.SensY) * num3, 5 + Hasard.Next(5), milliseconds, 100 + Hasard.Next(60)));
				}
				if (Traits.Count > 160)
				{
					Traits.RemoveRange(0, Traits.Count - 160);
				}
			}
		}
	}

	public static FloatRect Pose(Entity entity, FloatRect destination)
	{
		if (Etats.Count == 0 || !Etats.TryGetValue(entity.Id, out Etat value))
		{
			return destination;
		}
		long milliseconds = Timing.Global.Milliseconds;
		if (value.AppuiA >= 0)
		{
			if (milliseconds - value.AppuiA < 70)
			{
				return Tassee(destination, -value.AppuiSensX, -value.AppuiSensY);
			}
			value.AppuiA = -1L;
		}
		if (value.ArriveeA >= 0)
		{
			long num = milliseconds - value.ArriveeA;
			if (num >= 90)
			{
				value.ArriveeA = -1L;
			}
			else if (num < 45)
			{
				return Tassee(destination, 0, 0);
			}
		}
		return destination;
	}

	public static float Penche(Entity entity)
	{
		if (Etats.Count == 0 || !Etats.TryGetValue(entity.Id, out Etat value) || value.Glissade == null || entity.Dashing != value.Glissade)
		{
			return 0f;
		}
		return 2f * (float)value.SensX;
	}

	public static void Draw()
	{
		if (Traits.Count == 0)
		{
			return;
		}
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture == null)
		{
			return;
		}
		long maintenant = Timing.Global.Milliseconds;
		Traits.RemoveAll(((float X, float Y, float Vx, float Vy, int Longueur, long NeA, long Vie) t) => maintenant - t.NeA >= t.Vie);
		foreach (var trait in Traits)
		{
			long num = maintenant - trait.NeA;
			float num2 = (float)num / (float)trait.Vie;
			float num3 = (float)num / 1000f;
			float num4 = MathF.Round(trait.X + trait.Vx * num3);
			float num5 = MathF.Round(trait.Y + trait.Vy * num3);
			int num6 = (int)(210f * (1f - num2));
			if (num6 > 0)
			{
				Color renderColor = new Color(num6, 255, 255, 255);
				bool flag = MathF.Abs(trait.Vx) >= MathF.Abs(trait.Vy);
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), flag ? new FloatRect((trait.Vx > 0f) ? (num4 - (float)trait.Longueur) : num4, num5, trait.Longueur, 1f) : new FloatRect(num4, (trait.Vy > 0f) ? (num5 - (float)trait.Longueur) : num5, 1f, trait.Longueur), renderColor);
			}
		}
	}

	private static FloatRect Tassee(FloatRect destination, int reculX, int reculY)
	{
		if (destination.Height <= 2f)
		{
			return destination;
		}
		return new FloatRect(destination.X + (float)reculX, destination.Y + 1f + (float)reculY, destination.Width, destination.Height - 1f);
	}

	private static void Poussiere(Entity entity, int sensX, int sensY, bool derriere)
	{
		FloatRect worldPos = entity.WorldPos;
		if (!(worldPos.Width <= 0f) && !(worldPos.Height <= 0f) && entity.MapInstance is MapInstance { IsIndoors: false } && !Flight.IsAirborne(entity))
		{
			int num3;
			int num4;
			if (!derriere)
			{
				int num = -sensX;
				int num2 = -sensY;
				num3 = num2;
				num4 = num;
			}
			else
			{
				int num2 = sensY;
				num3 = num2;
				num4 = sensX;
			}
			Direction direction = ((num4 > 0) ? Direction.Right : ((num4 < 0) ? Direction.Left : ((num3 >= 0) ? Direction.Down : Direction.Up)));
			float feetX = worldPos.X + worldPos.Width / 2f + (float)(derriere ? (-sensX) : sensX) * 2f;
			SprintDust.AddPuff(feetX, worldPos.Y + worldPos.Height, direction);
		}
	}

	private static (int X, int Y) Sens(float x, float y)
	{
		if (x == 0f && y == 0f)
		{
			return (X: 0, Y: 0);
		}
		if (!(MathF.Abs(x) >= MathF.Abs(y)))
		{
			return (X: 0, Y: Math.Sign(y));
		}
		return (X: Math.Sign(x), Y: 0);
	}
}
