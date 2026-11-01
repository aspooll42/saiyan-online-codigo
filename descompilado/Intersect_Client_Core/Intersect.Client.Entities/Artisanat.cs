using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.MonoGame.Graphics;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Entities;

public static class Artisanat
{
	private sealed class Travail
	{
		public int Metier;

		public long Duree;

		public int Index;

		public int Nombre;

		public long Debut;

		public int DernierCoup = -1;

		public float Chaleur;

		public long Emission;
	}

	private enum Genre
	{
		Etincelle,
		Braise,
		Fumee,
		Goutte,
		Eclat,
		Eclair,
		Arc
	}

	private sealed class Particule
	{
		public Genre Genre;

		public float X;

		public float Y;

		public float Vx;

		public float Vy;

		public float Gravite;

		public float Frein = 1f;

		public long Ne;

		public long Vie;

		public int R;

		public int G;

		public int B;

		public float Taille0 = 1f;

		public float Taille1 = 1f;

		public float Alpha = 1f;

		public float Onde;

		public float Phase;

		public float[]? Points;
	}

	private const int ImageMs = 70;

	private const int ImagesDeGeste = 4;

	private static readonly Dictionary<Guid, Travail> Travaux = new Dictionary<Guid, Travail>();

	private static readonly List<Particule> Particules = new List<Particule>();

	private static readonly Random Hasard = new Random();

	private static readonly FloatRect UnPixel = new FloatRect(0f, 0f, 1f, 1f);

	private static IGameTexture? _halo;

	private static long _derniereImage;

	private static readonly (int, int, int)[] Feu = new(int, int, int)[3]
	{
		(255, 244, 194),
		(255, 211, 90),
		(255, 165, 58)
	};

	private static readonly (int, int, int)[] Soudure = new(int, int, int)[3]
	{
		(255, 255, 255),
		(191, 244, 255),
		(111, 216, 255)
	};

	private static readonly (int R, int G, int B) Vapeur = (R: 246, G: 246, B: 250);

	private static readonly (int R, int G, int B) Potion = (R: 120, G: 222, B: 170);

	private static readonly Color BarreContour = new Color(255, 11, 14, 23);

	private static readonly Color BarreFond = new Color(255, 42, 51, 73);

	private static readonly Color BarrePleine = new Color(255, 255, 122, 26);

	private static readonly Color BarreReflet = new Color(255, 255, 172, 82);

	private static readonly Color BarreBord = new Color(255, 255, 240, 208);

	private static int Rythme(int metier)
	{
		return metier switch
		{
			5 => 460, 
			6 => 640, 
			7 => 700, 
			8 => 380, 
			_ => 600, 
		};
	}

	public static void SurActivite(CraftingActivityPacket paquet)
	{
		long milliseconds = Timing.Global.Milliseconds;
		Globals.Entities.TryGetValue(paquet.EntityId, out Entity value);
		Travaux.TryGetValue(paquet.EntityId, out Travail value2);
		if (paquet.RecipeId == Guid.Empty)
		{
			if (value2 != null)
			{
				Travaux.Remove(paquet.EntityId);
				if (paquet.Finished && value != null)
				{
					Fin(value2.Metier, value, milliseconds);
				}
			}
			return;
		}
		if (value2 != null && paquet.Index > value2.Index && value != null)
		{
			Fin(value2.Metier, value, milliseconds);
		}
		if (value2 == null)
		{
			value2 = new Travail();
		}
		value2.Metier = paquet.Profession;
		value2.Duree = Math.Max(200L, paquet.Duration);
		value2.Index = paquet.Index;
		value2.Nombre = Math.Max(1, paquet.Count);
		value2.Debut = milliseconds;
		value2.DernierCoup = -1;
		Travaux[paquet.EntityId] = value2;
	}

	public static bool EnTravail(Entity entite)
	{
		if (Travaux.TryGetValue(entite.Id, out Travail value))
		{
			return Timing.Global.Milliseconds - value.Debut < value.Duree;
		}
		return false;
	}

	public static int? ImageDeTravail(Entity entite, int images)
	{
		if (!Travaux.TryGetValue(entite.Id, out Travail value))
		{
			return null;
		}
		long num = Timing.Global.Milliseconds - value.Debut;
		if (num < 0 || num >= value.Duree)
		{
			return null;
		}
		long num2 = num % Rythme(value.Metier);
		if (num2 >= 280)
		{
			return null;
		}
		return Math.Clamp((int)(num2 / 70), 0, Math.Max(0, images - 1));
	}

	public static FloatRect PoseElan(Entity entite, FloatRect dest)
	{
		int num;
		switch (ImageDeTravail(entite, 4))
		{
		case null:
			return dest;
		case 1:
			num = 2;
			break;
		case 2:
			num = 3;
			break;
		case 3:
			num = 1;
			break;
		default:
			num = 0;
			break;
		}
		int num2 = num;
		var (num3, num4) = Sens(entite.DirectionFacing);
		return new FloatRect(dest.X + (float)(num3 * num2), dest.Y + (float)(num4 * num2), dest.Width, dest.Height);
	}

	public static bool Progression(Guid entite, out float avancee, out int index, out int nombre)
	{
		avancee = 0f;
		index = 0;
		nombre = 0;
		if (!Travaux.TryGetValue(entite, out Travail value))
		{
			return false;
		}
		avancee = Math.Clamp((float)(Timing.Global.Milliseconds - value.Debut) / (float)value.Duree, 0f, 1f);
		index = value.Index;
		nombre = value.Nombre;
		return true;
	}

	public static void Dessiner()
	{
		if (Graphics.Renderer?.WhitePixel == null || (Travaux.Count == 0 && Particules.Count == 0))
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		float num = ((_derniereImage == 0L) ? 16.7f : ((float)Math.Clamp(milliseconds - _derniereImage, 0L, 50L)));
		_derniereImage = milliseconds;
		Halo();
		KeyValuePair<Guid, Travail>[] array = Travaux.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			KeyValuePair<Guid, Travail> keyValuePair = array[i];
			var (key, travail2) = keyValuePair;
			if (!Globals.Entities.TryGetValue(key, out Entity value))
			{
				Travaux.Remove(key);
				continue;
			}
			long num2 = milliseconds - travail2.Debut;
			if (num2 > travail2.Duree + 8000)
			{
				Travaux.Remove(key);
				continue;
			}
			bool flag = num2 >= 0 && num2 < travail2.Duree;
			float num3 = Math.Clamp((float)num2 / (float)travail2.Duree, 0f, 1f);
			var (x, y) = PointDeTravail(value);
			var (dx, dy) = Sens(value.DirectionFacing);
			if (flag)
			{
				int num4 = Rythme(travail2.Metier);
				int num5 = (int)(num2 / num4);
				if (num2 % num4 >= 140 && num5 != travail2.DernierCoup && num2 < travail2.Duree - 120)
				{
					travail2.DernierCoup = num5;
					Coup(travail2.Metier, x, y, dx, dy, milliseconds);
				}
				Ambiance(travail2, x, y, num, milliseconds);
			}
			float num6 = (flag ? (0.3f + 0.65f * num3) : 0f);
			travail2.Chaleur += (num6 - travail2.Chaleur) * MathF.Min(1f, num / (flag ? 200f : 900f));
			Lueurs(travail2, x, y, milliseconds, flag);
			if (flag)
			{
				DessinerBarre(value, num3);
			}
		}
		MettreAJour(num);
		DessinerParticules(milliseconds);
	}

	private static (float X, float Y) PointDeTravail(Entity entite)
	{
		FloatRect bodyWorldPos = entite.BodyWorldPos;
		float item = bodyWorldPos.X + bodyWorldPos.Width / 2f;
		var (num, num2) = Sens(entite.DirectionFacing);
		if (num2 < 0)
		{
			return (X: item, Y: bodyWorldPos.Y + bodyWorldPos.Height * 0.3f);
		}
		if (num2 > 0)
		{
			return (X: item, Y: bodyWorldPos.Bottom + 2f);
		}
		return (X: (num < 0) ? (bodyWorldPos.X - 5f) : (bodyWorldPos.Right + 5f), Y: bodyWorldPos.Y + bodyWorldPos.Height * 0.62f);
	}

	private static (int Dx, int Dy) Sens(Direction direction)
	{
		return direction switch
		{
			Direction.Up => (Dx: 0, Dy: -1), 
			Direction.Down => (Dx: 0, Dy: 1), 
			Direction.Left => (Dx: -1, Dy: 0), 
			Direction.Right => (Dx: 1, Dy: 0), 
			Direction.UpLeft => (Dx: -1, Dy: 0), 
			Direction.UpRight => (Dx: 1, Dy: 0), 
			Direction.DownLeft => (Dx: -1, Dy: 0), 
			Direction.DownRight => (Dx: 1, Dy: 0), 
			_ => (Dx: 0, Dy: 1), 
		};
	}

	private static float Entre(float a, float b)
	{
		return a + (float)Hasard.NextDouble() * (b - a);
	}

	private static void Ajouter(Particule particule, long maintenant)
	{
		particule.Ne = maintenant;
		Particules.Add(particule);
		if (Particules.Count > 600)
		{
			Particules.RemoveAt(0);
		}
	}

	private static void Etincelles(float x, float y, int dx, int n, float vitesse, (int R, int G, int B)[] couleurs, long maintenant, int vieMin = 220, int vieMax = 520)
	{
		for (int i = 0; i < n; i++)
		{
			var (r, g, b) = couleurs[Hasard.Next(couleurs.Length)];
			Ajouter(new Particule
			{
				Genre = Genre.Etincelle,
				X = x,
				Y = y,
				Vx = ((dx == 0) ? Entre(-1.6f, 1.6f) : ((float)dx * Entre(0.2f, 2.2f) + Entre(-0.6f, 0.6f))) * vitesse,
				Vy = Entre(-2.6f, -0.4f) * vitesse,
				Gravite = 0.15f,
				Frein = 0.985f,
				Vie = Hasard.Next(vieMin, vieMax),
				R = r,
				G = g,
				B = b
			}, maintenant);
		}
	}

	private static void Fumee(float x, float y, float vx, float vy, float taille0, float taille1, float alpha, int vie, (int R, int G, int B) couleur, long maintenant, float frein = 0.99f)
	{
		Ajouter(new Particule
		{
			Genre = Genre.Fumee,
			X = x,
			Y = y,
			Vx = vx,
			Vy = vy,
			Frein = frein,
			Taille0 = taille0,
			Taille1 = taille1,
			Alpha = alpha,
			Vie = vie,
			R = couleur.R,
			G = couleur.G,
			B = couleur.B,
			Onde = 0.08f,
			Phase = Entre(0f, 1000f)
		}, maintenant);
	}

	private static void Eclair(float x, float y, float rayon, int vie, (int R, int G, int B) couleur, long maintenant)
	{
		Particule obj = new Particule
		{
			Genre = Genre.Eclair,
			X = x,
			Y = y,
			Taille0 = rayon,
			Vie = vie
		};
		(obj.R, obj.G, obj.B) = couleur;
		Ajouter(obj, maintenant);
	}

	private static void Coup(int metier, float x, float y, int dx, int dy, long maintenant)
	{
		switch (metier)
		{
		case 5:
			Etincelles(x, y, dx, 10, 1f, Feu, maintenant);
			Eclair(x, y, 8f, 90, (R: 255, G: 230, B: 160), maintenant);
			break;
		case 6:
		{
			for (int k = 0; k < 4; k++)
			{
				Ajouter(new Particule
				{
					Genre = Genre.Goutte,
					X = x + Entre(-4f, 4f),
					Y = y - 2f,
					Vx = Entre(-0.8f, 0.8f),
					Vy = Entre(-1.5f, -0.9f),
					Gravite = 0.14f,
					Vie = Hasard.Next(280, 420),
					R = 255,
					G = 243,
					B = 200
				}, maintenant);
			}
			break;
		}
		case 7:
		{
			for (int l = 0; l < 4; l++)
			{
				Ajouter(new Particule
				{
					Genre = Genre.Goutte,
					X = x + Entre(-1f, 1f),
					Y = y - 6f,
					Vx = Entre(-0.55f, 0.55f),
					Vy = Entre(-1.1f, -0.6f),
					Gravite = 0.06f,
					Vie = Hasard.Next(320, 480),
					R = Potion.R,
					G = Potion.G,
					B = Potion.B
				}, maintenant);
			}
			break;
		}
		case 8:
		{
			for (int i = 0; i < 8; i++)
			{
				var (r, g, b) = Soudure[Hasard.Next(Soudure.Length)];
				Ajouter(new Particule
				{
					Genre = Genre.Etincelle,
					X = x,
					Y = y,
					Vx = (float)(-dx) * Entre(0.2f, 2.2f) + Entre(-0.8f, 0.8f),
					Vy = Entre(-2f, 0.4f),
					Gravite = 0.1f,
					Frein = 0.97f,
					Vie = Hasard.Next(160, 360),
					R = r,
					G = g,
					B = b
				}, maintenant);
			}
			Eclair(x, y, 9f, 80, (R: 170, G: 235, B: 255), maintenant);
			float[] array = new float[8];
			float num = x;
			float num2 = y;
			for (int j = 0; j < 4; j++)
			{
				array[j * 2] = MathF.Round(num);
				array[j * 2 + 1] = MathF.Round(num2);
				num += Entre(-5f, 5f);
				num2 += Entre(-4f, 4f);
			}
			Ajouter(new Particule
			{
				Genre = Genre.Arc,
				X = x,
				Y = y,
				Vie = 90L,
				R = 191,
				G = 244,
				B = 255,
				Points = array
			}, maintenant);
			break;
		}
		default:
			Fumee(x, y, Entre(-0.2f, 0.2f), Entre(-0.4f, -0.2f), 2f, 6f, 0.3f, 700, (R: 220, G: 210, B: 190), maintenant);
			break;
		}
	}

	private static void Fin(int metier, Entity entite, long maintenant)
	{
		(float X, float Y) tuple = PointDeTravail(entite);
		float item = tuple.X;
		float item2 = tuple.Y;
		int item3 = Sens(entite.DirectionFacing).Dx;
		switch (metier)
		{
		case 5:
		{
			Etincelles(item, item2, item3, 26, 1.3f, Feu, maintenant, 300, 700);
			Eclair(item, item2, 20f, 200, (R: 255, G: 220, B: 150), maintenant);
			for (int m = 0; m < 7; m++)
			{
				Fumee(item + Entre(-10f, 10f), item2 + Entre(-2f, 3f), Entre(-0.1f, 0.15f), Entre(-0.45f, -0.25f), 2f, Entre(7f, 11f), 0.45f, Hasard.Next(1100, 1700), (R: 235, G: 238, B: 245), maintenant);
			}
			break;
		}
		case 6:
		{
			for (int j = 0; j < 9; j++)
			{
				Fumee(item + Entre(-10f, 10f), item2 + Entre(-6f, 0f), Entre(-0.2f, 0.25f), Entre(-0.6f, -0.3f), 2f, Entre(7f, 11f), 0.5f, Hasard.Next(1100, 1700), Vapeur, maintenant);
			}
			Eclats(item, item2 - 8f, 5, (R: 255, G: 231, B: 168), maintenant);
			break;
		}
		case 7:
		{
			Eclair(item, item2 - 4f, 22f, 260, Potion, maintenant);
			for (int k = 0; k < 10; k++)
			{
				float x2 = (float)k / 10f * ((float)Math.PI * 2f);
				Ajouter(new Particule
				{
					Genre = Genre.Eclat,
					X = item + MathF.Cos(x2) * 8f,
					Y = item2 - 6f + MathF.Sin(x2) * 4f,
					Vx = MathF.Cos(x2) * 0.35f,
					Vy = -0.45f + MathF.Sin(x2) * 0.2f,
					Frein = 0.97f,
					Vie = Hasard.Next(500, 800),
					R = ((k % 2 == 0) ? 255 : Potion.R),
					G = ((k % 2 == 0) ? 255 : Potion.G),
					B = ((k % 2 == 0) ? 255 : Potion.B)
				}, maintenant);
			}
			for (int l = 0; l < 5; l++)
			{
				Fumee(item + Entre(-6f, 6f), item2 - Entre(6f, 10f), Entre(-0.2f, 0.2f), Entre(-0.5f, -0.3f), 2f, Entre(6f, 10f), 0.45f, Hasard.Next(1100, 1600), Potion, maintenant);
			}
			break;
		}
		case 8:
		{
			Eclair(item, item2, 18f, 200, (R: 255, G: 255, B: 255), maintenant);
			for (int i = 0; i < 12; i++)
			{
				float x = (float)i / 12f * ((float)Math.PI * 2f);
				Fumee(item + MathF.Cos(x) * 3f, item2 + MathF.Sin(x) * 2f, MathF.Cos(x) * Entre(0.5f, 0.9f), MathF.Sin(x) * Entre(0.3f, 0.5f) - 0.25f, 3f, Entre(8f, 12f), 0.85f, Hasard.Next(650, 900), (R: 250, G: 252, B: 255), maintenant, 0.9f);
			}
			break;
		}
		default:
			Eclats(item, item2 - 6f, 6, (R: 255, G: 231, B: 168), maintenant);
			break;
		}
	}

	private static void Eclats(float x, float y, int n, (int R, int G, int B) couleur, long maintenant)
	{
		for (int i = 0; i < n; i++)
		{
			Particule obj = new Particule
			{
				Genre = Genre.Eclat,
				X = x + Entre(-12f, 12f),
				Y = y + Entre(-8f, 6f),
				Vy = -0.2f,
				Vie = Hasard.Next(400, 700)
			};
			(obj.R, obj.G, obj.B) = couleur;
			Ajouter(obj, maintenant);
		}
	}

	private static void Ambiance(Travail travail, float x, float y, float dt, long maintenant)
	{
		int num = travail.Metier switch
		{
			5 => 45, 
			6 => 150, 
			7 => 110, 
			8 => 400, 
			_ => 0, 
		};
		if (num != 0 && maintenant - travail.Emission >= num)
		{
			travail.Emission = maintenant;
			switch (travail.Metier)
			{
			case 5:
				var (r, g, b) = Feu[Hasard.Next(Feu.Length)];
				Ajouter(new Particule
				{
					Genre = Genre.Braise,
					X = x + Entre(-8f, 8f),
					Y = y + Entre(-2f, 4f),
					Vx = Entre(-0.15f, 0.15f),
					Vy = Entre(-0.8f, -0.35f),
					Frein = 0.99f,
					Onde = 0.1f,
					Phase = Entre(0f, 1000f),
					Vie = Hasard.Next(900, 1700),
					R = r,
					G = g,
					B = b
				}, maintenant);
				break;
			case 6:
				Fumee(x + Entre(-5f, 5f), y - 3f, Entre(0.02f, 0.18f), Entre(-0.4f, -0.25f), 1.5f, Entre(5f, 8f), 0.4f, Hasard.Next(1200, 1800), Vapeur, maintenant, 0.995f);
				break;
			case 7:
				Ajouter(new Particule
				{
					Genre = Genre.Goutte,
					X = x + Entre(-3f, 3f),
					Y = y,
					Vy = Entre(-0.28f, -0.16f),
					Vie = Hasard.Next(240, 380),
					R = 255,
					G = 255,
					B = 255
				}, maintenant);
				Fumee(x + Entre(-1f, 1f), y - 8f, Entre(-0.1f, 0.1f), Entre(-0.35f, -0.2f), 1f, Entre(4f, 7f), 0.4f, Hasard.Next(1200, 1800), Potion, maintenant);
				break;
			case 8:
				Ajouter(new Particule
				{
					Genre = Genre.Eclat,
					X = x + Entre(-6f, 6f),
					Y = y + Entre(-6f, 4f),
					Vie = 200L,
					R = 150,
					G = 230,
					B = 255
				}, maintenant);
				break;
			}
		}
	}

	private static void Lueurs(Travail travail, float x, float y, long maintenant, bool enCours)
	{
		if (!(travail.Chaleur < 0.01f) || enCours)
		{
			float num = 0.5f + 0.5f * MathF.Sin((float)maintenant / 170f) * MathF.Sin((float)maintenant / 97f + 1f);
			float num2 = MathF.Max(travail.Chaleur, enCours ? 0.3f : 0f);
			switch (travail.Metier)
			{
			case 5:
				HaloEn(x, y, 26f, 255, 120, 30, (0.25f + 0.1f * num) * num2);
				HaloEn(x, y, 10f, 255, 220, 150, 0.35f * travail.Chaleur);
				break;
			case 6:
				HaloEn(x, y + 6f, 14f, 255, 130, 40, (0.2f + 0.12f * num) * num2);
				break;
			case 7:
				HaloEn(x, y - 4f, 16f, Potion.R, Potion.G, Potion.B, (0.15f + 0.08f * num) * num2);
				break;
			case 8:
				HaloEn(x, y, 20f, 90, 200, 255, (0.18f + 0.08f * num) * num2);
				break;
			}
		}
	}

	private static void MettreAJour(float dt)
	{
		float num = dt / 16.7f;
		long milliseconds = Timing.Global.Milliseconds;
		for (int num2 = Particules.Count - 1; num2 >= 0; num2--)
		{
			Particule particule = Particules[num2];
			if (milliseconds - particule.Ne >= particule.Vie)
			{
				Particules.RemoveAt(num2);
			}
			else
			{
				particule.Vy += particule.Gravite * num;
				float num3 = MathF.Pow(particule.Frein, num);
				particule.Vx *= num3;
				particule.Vy *= num3;
				particule.X += particule.Vx * num + ((particule.Onde > 0f) ? (MathF.Sin(((float)(milliseconds - particule.Ne) + particule.Phase) / 240f) * particule.Onde * num) : 0f);
				particule.Y += particule.Vy * num;
			}
		}
	}

	private static void DessinerParticules(long maintenant)
	{
		foreach (Particule particule in Particules)
		{
			float num = Math.Clamp((float)(maintenant - particule.Ne) / (float)particule.Vie, 0f, 1f);
			if (particule.Genre == Genre.Fumee && _halo != null)
			{
				float num2 = particule.Taille0 + (particule.Taille1 - particule.Taille0) * (1f - (1f - num) * (1f - num));
				float a = particule.Alpha * MathF.Min(1f, num * 8f) * MathF.Pow(1f - num, 1.4f);
				Texture(_halo, particule.X - num2, particule.Y - num2, num2 * 2f, num2 * 2f, particule.R, particule.G, particule.B, a, GameBlendModes.Alpha);
			}
			else if (particule.Genre == Genre.Goutte)
			{
				Carre(MathF.Round(particule.X), MathF.Round(particule.Y), 1f, 1f, particule.R, particule.G, particule.B, MathF.Min(1f, (1f - num) * 2f));
			}
		}
		foreach (Particule particule2 in Particules)
		{
			float num3 = Math.Clamp((float)(maintenant - particule2.Ne) / (float)particule2.Vie, 0f, 1f);
			float num4 = 1f - num3;
			float num5 = MathF.Round(particule2.X);
			float num6 = MathF.Round(particule2.Y);
			switch (particule2.Genre)
			{
			case Genre.Etincelle:
			{
				float num7 = particule2.X - particule2.Vx * 1.6f;
				float num8 = particule2.Y - particule2.Vy * 1.6f;
				Carre(MathF.Round(num7), MathF.Round(num8), 1f, 1f, particule2.R, particule2.G, particule2.B, num4, GameBlendModes.Add);
				Carre(MathF.Round((num7 + particule2.X) / 2f), MathF.Round((num8 + particule2.Y) / 2f), 1f, 1f, particule2.R, particule2.G, particule2.B, num4, GameBlendModes.Add);
				Carre(num5, num6, 1f, 1f, 255, 255, 255, num4, GameBlendModes.Add);
				break;
			}
			case Genre.Braise:
				Carre(num5, num6, 1f, 1f, particule2.R, particule2.G, particule2.B, num4 * (0.55f + 0.45f * MathF.Sin((float)(maintenant - particule2.Ne) / 55f + particule2.Phase)), GameBlendModes.Add);
				break;
			case Genre.Eclat:
			{
				int num9 = ((!(num3 < 0.45f)) ? 1 : 2);
				Carre(num5, num6, 1f, 1f, particule2.R, particule2.G, particule2.B, num4, GameBlendModes.Add);
				Carre(num5 - (float)num9, num6, 1f, 1f, particule2.R, particule2.G, particule2.B, num4, GameBlendModes.Add);
				Carre(num5 + (float)num9, num6, 1f, 1f, particule2.R, particule2.G, particule2.B, num4, GameBlendModes.Add);
				Carre(num5, num6 - (float)num9, 1f, 1f, particule2.R, particule2.G, particule2.B, num4, GameBlendModes.Add);
				Carre(num5, num6 + (float)num9, 1f, 1f, particule2.R, particule2.G, particule2.B, num4, GameBlendModes.Add);
				break;
			}
			case Genre.Eclair:
				HaloEn(particule2.X, particule2.Y, particule2.Taille0 * (0.7f + 0.3f * num3), particule2.R, particule2.G, particule2.B, num4);
				break;
			case Genre.Arc:
				if (particule2.Points != null)
				{
					for (int i = 0; i + 3 < particule2.Points.Length; i += 2)
					{
						Trait(particule2.Points[i], particule2.Points[i + 1], particule2.Points[i + 2], particule2.Points[i + 3], particule2.R, particule2.G, particule2.B, num4);
					}
				}
				break;
			}
		}
	}

	private static void Trait(float x0, float y0, float x1, float y1, int r, int g, int b, float a)
	{
		int num = (int)MathF.Max(MathF.Abs(x1 - x0), MathF.Abs(y1 - y0));
		for (int i = 0; i <= num; i++)
		{
			float num2 = ((num == 0) ? 0f : ((float)i / (float)num));
			Carre(MathF.Round(x0 + (x1 - x0) * num2), MathF.Round(y0 + (y1 - y0) * num2), 1f, 1f, r, g, b, a, GameBlendModes.Add);
		}
	}

	private static void DessinerBarre(Entity entite, float avancee)
	{
		FloatRect bodyWorldPos = entite.BodyWorldPos;
		float num = MathF.Round(bodyWorldPos.X + bodyWorldPos.Width / 2f - 20f);
		float num2 = MathF.Round(bodyWorldPos.Y - 12f);
		Rect(num - 1f, num2 - 1f, 42f, 8f, BarreContour);
		Rect(num, num2, 40f, 6f, BarreFond);
		float num3 = MathF.Round(40f * avancee);
		Rect(num, num2, num3, 6f, BarrePleine);
		Rect(num, num2, num3, 2f, BarreReflet);
		if (num3 > 0f && num3 < 40f)
		{
			Rect(num + num3 - 1f, num2, 1f, 6f, BarreBord);
		}
	}

	private static void Halo()
	{
		if (_halo != null || !(Graphics.Renderer is MonoRenderer monoRenderer))
		{
			return;
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
		_halo = monoRenderer.CreerTexturePixels("artisanat/halo", 64, 64, array);
	}

	private static void HaloEn(float cx, float cy, float rayon, int r, int g, int b, float alpha)
	{
		if (_halo != null && alpha > 0.005f)
		{
			Texture(_halo, cx - rayon, cy - rayon, rayon * 2f, rayon * 2f, r, g, b, alpha, GameBlendModes.Add);
		}
	}

	private static Color Couleur(int r, int g, int b, float a)
	{
		return new Color((int)Math.Clamp(a * 255f, 0f, 255f), Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(b, 0, 255));
	}

	private static void Carre(float x, float y, float l, float h, int r, int g, int b, float a, GameBlendModes mode = GameBlendModes.Alpha)
	{
		if (!(a <= 0.004f))
		{
			IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
			if (gameTexture != null)
			{
				Graphics.DrawGameTexture(gameTexture, UnPixel, new FloatRect(x, y, l, h), Couleur(r, g, b, a), null, mode);
			}
		}
	}

	private static void Rect(float x, float y, float l, float h, Color couleur)
	{
		if (!(l <= 0f) && !(h <= 0f))
		{
			IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
			if (gameTexture != null)
			{
				Graphics.DrawGameTexture(gameTexture, UnPixel, new FloatRect(x, y, l, h), couleur);
			}
		}
	}

	private static void Texture(IGameTexture texture, float x, float y, float l, float h, int r, int g, int b, float a, GameBlendModes mode)
	{
		if (!(a <= 0.004f))
		{
			Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), new FloatRect(x, y, l, h), Couleur(r, g, b, a), null, mode);
		}
	}
}
