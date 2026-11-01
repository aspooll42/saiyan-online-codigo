using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Core.Sounds;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Entities;

public static class Choc
{
	private sealed class Gerbe
	{
		public float X;

		public float Y;

		public float Angle;

		public float BiaisX;

		public float BiaisY;

		public long Debut;

		public long Duree;

		public float Force;

		public long ProchaineEtincelle;
	}

	private const long EclatMs = 260L;

	private const float EcartDesArcs = (float)Math.PI / 4f;

	private const float OuvertureDArc = 1.8f;

	private static readonly List<Gerbe> Gerbes = new List<Gerbe>();

	private static readonly List<(float X, float Y, float VitesseX, float VitesseY, long Debut, Color Couleur)> Eclats = new List<(float, float, float, float, long, Color)>();

	private static readonly Color Or = new Color(255, 255, 211, 92);

	private static readonly Color Blanc = new Color(255, 238, 240, 244);

	public static void OnChoc(EchangePacket packet)
	{
		Globals.Entities.TryGetValue(packet.EntityId, out Entity value);
		Globals.Entities.TryGetValue(packet.AutreId, out Entity value2);
		Entity entity = value ?? value2;
		if (entity != null)
		{
			long milliseconds = Timing.Global.Milliseconds;
			float num = Math.Clamp((float)packet.Valeur / 100f, 0f, 1f);
			bool flag = packet.Index == 1;
			if (value != null && value2 != null)
			{
				value.DirectionFacing = Vers(value, value2);
				value2.DirectionFacing = Vers(value2, value);
			}
			(float X, float Y) tuple = Axe(value, value2);
			float item = tuple.X;
			float item2 = tuple.Y;
			(float X, float Y) tuple2 = Contact(value, value2, flag ? 0.62f : 0.5f);
			float item3 = tuple2.X;
			float item4 = tuple2.Y;
			float biaisX = (flag ? (item * 0.02f) : 0f);
			float biaisY = (flag ? (item2 * 0.02f) : 0f);
			Gerbes.Add(new Gerbe
			{
				X = item3,
				Y = item4,
				Angle = MathF.Atan2(item2, item),
				BiaisX = biaisX,
				BiaisY = biaisY,
				Debut = milliseconds,
				Duree = Math.Clamp(packet.DureeMs, 140, 900),
				Force = num
			});
			if (Gerbes.Count > 8)
			{
				Gerbes.RemoveAt(0);
			}
			int nombre = 6 + (int)(num * 12f);
			Jaillir(item3, item4, nombre, Blanc, num, biaisX, biaisY);
			Jaillir(item3, item4, nombre, Or, num, biaisX, biaisY);
			float num2 = 1f + num * 1.6f;
			if (value != null)
			{
				Echanges.Poussiere(value, 2, flag ? (num2 * 0.7f) : num2);
			}
			if (value2 != null)
			{
				Echanges.Poussiere(value2, flag ? 4 : 2, flag ? (num2 * 1.4f) : num2);
			}
			if (value2 != null)
			{
				Reculer(value2, item, item2, milliseconds);
			}
			if (!flag && value != null)
			{
				Reculer(value, 0f - item, 0f - item2, milliseconds);
			}
			if (!Heritage.Actif || !Heritage.LeCoupFortADejaSonne)
			{
				CombatAudio.Play("strongcoup.wav", entity, 0.05f);
			}
			CombatFeedback.Callout(entity.Id, Strings.Echanges.Choc, Or, 13);
			Player me = Globals.Me;
			if (me != null && (me.Id == packet.EntityId || me.Id == packet.AutreId))
			{
				CombatFeedback.HitStop(60 + (long)(60f * num));
				CombatFeedback.StartShake(2f + 4f * num, 120 + (long)(100f * num), flag ? item : 0f, flag ? item2 : 0f);
			}
		}
	}

	public static void Draw(IGameTexture pixel)
	{
		if (Gerbes.Count == 0 && Eclats.Count == 0)
		{
			return;
		}
		long maintenant = Timing.Global.Milliseconds;
		Gerbes.RemoveAll((Gerbe gerbe) => maintenant - gerbe.Debut > gerbe.Duree);
		foreach (Gerbe gerbe in Gerbes)
		{
			float num = Math.Clamp((float)(maintenant - gerbe.Debut) / (float)Math.Max(1L, gerbe.Duree), 0f, 1f);
			float num2 = 1f - num * num;
			float rayon = 4f + gerbe.Force * 5f + num * (9f + gerbe.Force * 15f);
			float num3 = 2f + gerbe.Force * 2f;
			Color couleur = new Color((int)(235f * num2), Or.R, Or.G, Or.B);
			Color couleur2 = new Color((int)(255f * num2), Blanc.R, Blanc.G, Blanc.B);
			Arc(pixel, gerbe.X, gerbe.Y, gerbe.Angle + (float)Math.PI / 4f, rayon, num3 + 1f, couleur);
			Arc(pixel, gerbe.X, gerbe.Y, gerbe.Angle - (float)Math.PI / 4f, rayon, num3 + 1f, couleur);
			Arc(pixel, gerbe.X, gerbe.Y, gerbe.Angle + (float)Math.PI / 4f, rayon, num3, couleur2);
			Arc(pixel, gerbe.X, gerbe.Y, gerbe.Angle - (float)Math.PI / 4f, rayon, num3, couleur2);
			if (num < 0.45f)
			{
				float num4 = 1f - num / 0.45f;
				Disque(pixel, gerbe.X, gerbe.Y, 1.5f + gerbe.Force * 3f * num4, new Color((int)(255f * num4), 255, 255, 255));
			}
			if (num < 0.6f && maintenant >= gerbe.ProchaineEtincelle)
			{
				gerbe.ProchaineEtincelle = maintenant + 45;
				Jaillir(gerbe.X + (float)(Random.Shared.NextDouble() - 0.5) * 6f, gerbe.Y + (float)(Random.Shared.NextDouble() - 0.5) * 6f, 2 + (int)(gerbe.Force * 3f), (Random.Shared.Next(2) == 0) ? Or : Blanc, gerbe.Force, gerbe.BiaisX, gerbe.BiaisY);
			}
		}
		Eclats.RemoveAll(((float X, float Y, float VitesseX, float VitesseY, long Debut, Color Couleur) eclat) => maintenant - eclat.Debut > 260);
		foreach (var eclat in Eclats)
		{
			float item = eclat.X;
			float item2 = eclat.Y;
			float item3 = eclat.VitesseX;
			float item4 = eclat.VitesseY;
			long item5 = eclat.Debut;
			Color item6 = eclat.Couleur;
			long num5 = maintenant - item5;
			Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(item + item3 * (float)num5), MathF.Round(item2 + item4 * (float)num5), 1f, 1f), new Color((int)(255f * (1f - (float)num5 / 260f)), item6.R, item6.G, item6.B));
		}
	}

	private static (float X, float Y) Contact(Entity? a, Entity? b, float part)
	{
		FloatRect floatRect = a?.WorldPos ?? default(FloatRect);
		FloatRect floatRect2 = b?.WorldPos ?? default(FloatRect);
		if (a == null)
		{
			floatRect = floatRect2;
		}
		if (b == null)
		{
			floatRect2 = floatRect;
		}
		float num = floatRect.X + floatRect.Width / 2f;
		float num2 = floatRect.Y + floatRect.Height * 0.55f - ((a != null) ? Echanges.Hauteur(a) : 0f);
		float num3 = floatRect2.X + floatRect2.Width / 2f;
		float num4 = floatRect2.Y + floatRect2.Height * 0.55f - ((b != null) ? Echanges.Hauteur(b) : 0f);
		return (X: num + (num3 - num) * part, Y: num2 + (num4 - num2) * part);
	}

	private static (float X, float Y) Axe(Entity? a, Entity? b)
	{
		if (a == null || b == null)
		{
			return (X: 1f, Y: 0f);
		}
		float num = b.WorldPos.X + b.WorldPos.Width / 2f - (a.WorldPos.X + a.WorldPos.Width / 2f);
		float num2 = b.WorldPos.Y + b.WorldPos.Height - (a.WorldPos.Y + a.WorldPos.Height);
		float num3 = MathF.Sqrt(num * num + num2 * num2);
		if (!(num3 < 0.001f))
		{
			return (X: num / num3, Y: num2 / num3);
		}
		return (X: 1f, Y: 0f);
	}

	private static Direction Vers(Entity de, Entity vers)
	{
		float num = vers.WorldPos.X - de.WorldPos.X;
		float num2 = vers.WorldPos.Y + vers.WorldPos.Height - (de.WorldPos.Y + de.WorldPos.Height);
		if (!(MathF.Abs(num) >= MathF.Abs(num2)))
		{
			if (!(num2 < 0f))
			{
				return Direction.Down;
			}
			return Direction.Up;
		}
		if (!(num < 0f))
		{
			return Direction.Right;
		}
		return Direction.Left;
	}

	private static void Reculer(Entity entity, float versX, float versY, long maintenant)
	{
		entity.HitShakeAt = maintenant;
		entity.HitRecoilX = versX;
		entity.HitRecoilY = versY;
	}

	private static void Jaillir(float x, float y, int nombre, Color couleur, float force, float biaisX, float biaisY)
	{
		long milliseconds = Timing.Global.Milliseconds;
		for (int i = 0; i < nombre; i++)
		{
			double num = Random.Shared.NextDouble() * Math.PI * 2.0;
			double num2 = (0.03 + Random.Shared.NextDouble() * 0.05) * (double)(1f + force);
			Eclats.Add((x, y, (float)(Math.Cos(num) * num2) + biaisX, (float)(Math.Sin(num) * num2 * 0.7) + biaisY, milliseconds, couleur));
		}
		if (Eclats.Count > 160)
		{
			Eclats.RemoveRange(0, Eclats.Count - 160);
		}
	}

	private static void Arc(IGameTexture pixel, float x, float y, float angle, float rayon, float epaisseur, Color couleur)
	{
		if (couleur.A > 0 && !(rayon <= 0f))
		{
			float num = x - MathF.Cos(angle) * rayon;
			float num2 = y - MathF.Sin(angle) * rayon;
			int num3 = Math.Max(8, (int)(rayon * 1.8f));
			for (int i = 0; i <= num3; i++)
			{
				float num4 = (float)i / (float)num3;
				float x2 = angle + (num4 - 0.5f) * 1.8f;
				float num5 = MathF.Max(1f, MathF.Round(epaisseur * (1f - MathF.Abs(num4 - 0.5f) * 1.6f)));
				Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(num + MathF.Cos(x2) * rayon - num5 / 2f), MathF.Round(num2 + MathF.Sin(x2) * rayon - num5 / 2f), num5, num5), couleur);
			}
		}
	}

	private static void Disque(IGameTexture pixel, float x, float y, float rayon, Color couleur)
	{
		int num = (int)MathF.Ceiling(rayon);
		for (int i = -num; i <= num; i++)
		{
			float num2 = rayon * rayon - (float)(i * i);
			if (!(num2 < 0f))
			{
				float num3 = MathF.Floor(MathF.Sqrt(num2));
				Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(x - num3), MathF.Round(y + (float)i), num3 * 2f + 1f, 1f), couleur);
			}
		}
	}
}
