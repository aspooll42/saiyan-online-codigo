using System;
using System.Collections.Generic;

namespace Intersect.Client.Maps;

public static class Cascades
{
	internal readonly record struct Chute(int X0, int X1, int Rideau, int Pied)
	{
		public int Largeur => X1 - X0;

		public int Ecume => Pied + 12;

		public int Levre => Rideau - 32;

		public int Riviere => Levre - 64;

		public int Nombre(int pour160)
		{
			return Math.Max(1, (int)Math.Round((double)(pour160 * Largeur) / 160.0));
		}

		public double Mesure(double pour160)
		{
			return pour160 * (double)Largeur / 160.0;
		}
	}

	internal sealed class Tampon
	{
		public readonly byte[] Rgba;

		public readonly int L;

		public readonly int H;

		public readonly int X;

		public readonly int Y;

		public Tampon(int x, int y, int l, int h)
		{
			X = x;
			Y = y;
			L = l;
			H = h;
			Rgba = new byte[l * h * 4];
		}

		public void Vider()
		{
			Array.Clear(Rgba);
		}

		public void Poser(double carteX, double carteY, (byte R, byte G, byte B) ton, double alpha)
		{
			int num = (int)Math.Floor(carteX + 0.5) - X;
			int num2 = (int)Math.Floor(carteY + 0.5) - Y;
			if (num >= 0 && num2 >= 0 && num < L && num2 < H && !(alpha <= 0.0))
			{
				int num3 = (num2 * L + num) * 4;
				if (alpha >= 1.0 || Rgba[num3 + 3] == 0)
				{
					Rgba[num3] = ton.R;
					Rgba[num3 + 1] = ton.G;
					Rgba[num3 + 2] = ton.B;
					Rgba[num3 + 3] = (byte)Math.Round(Math.Min(1.0, alpha) * 255.0);
				}
				else
				{
					double num4 = (double)(int)Rgba[num3 + 3] / 255.0;
					double num5 = num4 * (1.0 - alpha);
					double num6 = alpha + num5;
					Rgba[num3] = (byte)Math.Round(((double)(int)ton.R * alpha + (double)(int)Rgba[num3] * num5) / num6);
					Rgba[num3 + 1] = (byte)Math.Round(((double)(int)ton.G * alpha + (double)(int)Rgba[num3 + 1] * num5) / num6);
					Rgba[num3 + 2] = (byte)Math.Round(((double)(int)ton.B * alpha + (double)(int)Rgba[num3 + 2] * num5) / num6);
					Rgba[num3 + 3] = (byte)Math.Round(num6 * 255.0);
				}
			}
		}
	}

	internal const int CreteDeLaLevre = 14;

	internal const int SousLaCrete = 18;

	internal const int HautDeLEcume = 8;

	internal const int BasDeLEcume = 18;

	private static readonly (byte, byte, byte) Blanc = (byte.MaxValue, byte.MaxValue, byte.MaxValue);

	private static readonly (byte, byte, byte) Pale = (205, 243, 243);

	private static readonly (byte, byte, byte) Turquoise = (154, 219, 219);

	private static readonly (byte, byte, byte)[] Bandes = new(byte, byte, byte)[6]
	{
		(150, 120, 235),
		(96, 156, 245),
		(112, 222, 150),
		(250, 238, 128),
		(250, 176, 96),
		(242, 108, 108)
	};

	internal static List<(int X0, int X1, int Haut, int Pied)> Trouver(Func<int, int, int> genre, int largeur, int hauteur)
	{
		List<(int, int, int, int)> list = new List<(int, int, int, int)>();
		for (int i = 0; i < hauteur; i++)
		{
			for (int j = 0; j < largeur; j++)
			{
				if (genre(j, i) != 2 || (j > 0 && genre(j - 1, i) == 2))
				{
					continue;
				}
				int k;
				for (k = j; k + 1 < largeur && genre(k + 1, i) == 2; k++)
				{
				}
				int num;
				for (num = i; num > 0; num--)
				{
					bool flag = true;
					for (int l = j; l <= k && flag; l++)
					{
						flag = genre(l, num - 1) == 1;
					}
					if (!flag)
					{
						break;
					}
				}
				if (num < i)
				{
					list.Add((j, k, num, i));
				}
			}
		}
		return list;
	}

	internal static (int De, int A) LignesDeChute(int piece, int hauteurDeTuile = 32)
	{
		return piece switch
		{
			0 => (De: 14, A: hauteurDeTuile), 
			2 => (De: 0, A: 18), 
			_ => (De: 0, A: hauteurDeTuile), 
		};
	}

	internal static (int De, int A) LignesQuiDefilent(int piece, int hauteurDeTuile = 32)
	{
		return piece switch
		{
			0 => (De: 18, A: hauteurDeTuile), 
			2 => (De: 0, A: 8), 
			_ => (De: 0, A: hauteurDeTuile), 
		};
	}

	internal static bool[]? Masque(IReadOnlyList<(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy)> poses, IReadOnlyList<(int Pose, int De, int A)> chutes, int largeur, int hauteur)
	{
		if (chutes.Count == 0)
		{
			return null;
		}
		int num = largeur;
		int num2 = hauteur;
		int num3 = 0;
		int num4 = 0;
		foreach (var chute in chutes)
		{
			int item = chute.Pose;
			int item2 = chute.De;
			int item3 = chute.A;
			(byte[], int, int, int, int, int, int, int, int) tuple = poses[item];
			num = Math.Min(num, tuple.Rest.Item1);
			num3 = Math.Max(num3, tuple.Rest.Item1 + tuple.Item6);
			num2 = Math.Min(num2, tuple.Rest.Item2 + item2);
			num4 = Math.Max(num4, tuple.Rest.Item2 + item3);
		}
		num = Math.Max(0, num);
		num2 = Math.Max(0, num2);
		num3 = Math.Min(largeur, num3);
		num4 = Math.Min(hauteur, num4);
		bool[] array = new bool[largeur * hauteur];
		int num5 = 0;
		for (int i = chutes[0].Pose; i < poses.Count; i++)
		{
			(byte[] Rgba, int PL, int PH, int Sx, int Sy, int W, int Hh, int Dx, int Dy) tuple2 = poses[i];
			byte[] item4 = tuple2.Rgba;
			int item5 = tuple2.PL;
			int item6 = tuple2.PH;
			int item7 = tuple2.Sx;
			int item8 = tuple2.Sy;
			int item9 = tuple2.W;
			int item10 = tuple2.Hh;
			int item11 = tuple2.Rest.Item1;
			int item12 = tuple2.Rest.Item2;
			int num6 = 0;
			int num7 = 0;
			if (num5 < chutes.Count && chutes[num5].Pose == i)
			{
				(int Pose, int De, int A) tuple3 = chutes[num5++];
				num6 = tuple3.De;
				num7 = tuple3.A;
			}
			int j = Math.Max(item12, num2);
			for (int num8 = Math.Min(item12 + item10, num4); j < num8; j++)
			{
				int num9 = j - item12;
				int num10 = item8 + num9;
				if (num10 < 0 || num10 >= item6)
				{
					continue;
				}
				bool flag = num9 >= num6 && num9 < num7;
				int k = Math.Max(item11, num);
				for (int num11 = Math.Min(item11 + item9, num3); k < num11; k++)
				{
					int num12 = item7 + k - item11;
					if (num12 < 0 || num12 >= item5)
					{
						continue;
					}
					byte b = item4[(num10 * item5 + num12) * 4 + 3];
					if (flag)
					{
						if (b >= 128)
						{
							array[j * largeur + k] = true;
						}
					}
					else if (b == byte.MaxValue)
					{
						array[j * largeur + k] = false;
					}
				}
			}
		}
		return array;
	}

	internal static double Tirage(long a, long b)
	{
		int num = (int)a * 374761393 + (int)b * 668265263;
		num = (num ^ (num >>> 13)) * 1274126177;
		return (double)(uint)(num ^ (num >>> 16)) / 4294967296.0;
	}

	internal static double Bruit(double x, double y)
	{
		double num = Math.Floor(x);
		double num2 = Math.Floor(y);
		long num3 = (long)num;
		long num4 = (long)num2;
		double num5 = x - num;
		double num6 = y - num2;
		double num7 = num5 * num5 * (3.0 - 2.0 * num5);
		double num8 = num6 * num6 * (3.0 - 2.0 * num6);
		double num9 = Tirage(num3, num4);
		double num10 = Tirage(num3 + 1, num4);
		double num11 = Tirage(num3, num4 + 1);
		double num12 = Tirage(num3 + 1, num4 + 1);
		return num9 + (num10 - num9) * num7 + (num11 - num9) * num8 + (num9 - num10 - num11 + num12) * num7 * num8;
	}

	private static (long Tour, double U) Cycle(long t, double decal, double duree)
	{
		double num = (double)t + decal;
		double num2 = Math.Floor(num / duree);
		return (Tour: (long)num2, U: (num - num2 * duree) / duree);
	}

	internal static void Filets(Tampon p, Chute c, long t, double force, Func<int, int, bool>? visible = null)
	{
		int i = 0;
		for (int num = c.Nombre(34); i < num; i++)
		{
			(long Tour, double U) tuple = Cycle(t, Tirage(i, 5L) * 4000.0, 460.0 + Tirage(i, 3L) * 380.0);
			long item = tuple.Tour;
			double item2 = tuple.U;
			int num2 = c.X0 + 3 + (int)Math.Floor(Tirage(i, item) * (double)(c.Largeur - 6));
			double num3 = (double)c.Rideau + item2 * (double)(c.Pied - c.Rideau - 14);
			int num4 = 7 + (int)Math.Floor(Tirage(i, item + 9) * 12.0);
			for (int j = 0; j < num4; j++)
			{
				if (visible == null || visible(num2, (int)Math.Floor(num3 + (double)j + 0.5)))
				{
					p.Poser(num2, num3 + (double)j, (j > num4 - 4) ? Blanc : Pale, force * (0.25 + 0.75 * (double)j / (double)num4));
				}
			}
		}
	}

	internal static void Courant(Tampon p, Chute c, long t, Func<int, int, bool> surLEau)
	{
		int i = 0;
		for (int num = c.Nombre(16); i < num; i++)
		{
			(long Tour, double U) tuple = Cycle(t, Tirage(i, 103L) * 5000.0, 1500.0 + Tirage(i, 101L) * 900.0);
			long item = tuple.Tour;
			double item2 = tuple.U;
			int num2 = c.X0 + 6 + (int)Math.Floor(Tirage(i, item + 110) * (double)(c.Largeur - 12));
			double num3 = (double)(c.Riviere + 6) + item2 * item2 * (double)(c.Levre + 10 - c.Riviere);
			int num4 = 3 + (int)Math.Floor(item2 * 7.0);
			for (int j = 0; j < num4; j++)
			{
				if (surLEau(num2, (int)Math.Floor(num3 + (double)j + 0.5)))
				{
					p.Poser(num2, num3 + (double)j, Blanc, 0.5 * Math.Sin(Math.PI * item2));
				}
			}
		}
	}

	internal static void Ondes(Tampon p, Chute c, long t, Func<int, int, bool> surLEau)
	{
		double num = (double)(c.X0 + c.X1) / 2.0;
		double num2 = c.Ecume - 34;
		for (int i = 0; i < 6; i++)
		{
			double num3 = ((double)t + (double)i * 5200.0 / 6.0) % 5200.0 / 5200.0;
			double num4 = c.Mesure(52.0) + num3 * c.Mesure(150.0);
			double alpha = 0.5 * (1.0 - num3) * Math.Min(1.0, num3 * 6.0);
			for (double num5 = 0.12; num5 < 3.021592653589793; num5 += 0.7 / num4)
			{
				double num6 = num + Math.Cos(num5) * num4 * 1.25;
				double num7 = num2 + Math.Sin(num5) * num4 * 0.86;
				if (!(Bruit(num5 * 9.0 + (double)(i * 5), num3 * 3.0) < 0.36) && !(num7 < (double)(c.Ecume + 7)) && surLEau((int)Math.Floor(num6 + 0.5), (int)Math.Floor(num7 + 0.5)))
				{
					p.Poser(num6, num7, Pale, alpha);
				}
			}
		}
		int j = 0;
		for (int num8 = c.Nombre(20); j < num8; j++)
		{
			(long Tour, double U) tuple = Cycle(t, Tirage(j, 33L) * 5000.0, 1900.0 + Tirage(j, 31L) * 1500.0);
			long item = tuple.Tour;
			double item2 = tuple.U;
			double num9 = (double)(c.X0 + 8) + Tirage(j, item + 40) * (double)(c.Largeur - 16);
			double num10 = (double)(c.Ecume + 9) + item2 * (26.0 + Tirage(j, item + 41) * 44.0);
			double num11 = 2.0 + item2 * (5.0 + Tirage(j, item + 42) * 8.0);
			double num12 = (Tirage(j, item + 43) - 0.5) * 34.0 * item2;
			for (double num13 = 0.0 - num11; num13 <= num11; num13 += 1.0)
			{
				double num14 = num9 + num12 + num13;
				double num15 = num10 + (double)((Math.Abs(num13) > num11 * 0.6) ? 1 : 0);
				if (surLEau((int)Math.Floor(num14 + 0.5), (int)Math.Floor(num15 + 0.5)))
				{
					p.Poser(num14, num15, Blanc, 0.6 * (1.0 - item2));
				}
			}
		}
	}

	internal static void Panache(Tampon p, Chute c, long t)
	{
		double[] hauteurs = new double[c.Largeur];
		Rang(7, c.Nombre(9), 9.0, 30.0, 7.0, 12.0, 0.0021, Pale, Pale, 0.5);
		Rang(3, c.Nombre(13), 4.0, 15.0, 4.0, 8.0, 0.0034, Pale, Blanc, 0.95);
		long b = (long)Math.Floor((double)t / 150.0);
		for (int i = 0; i < c.Largeur; i++)
		{
			int num = (int)Math.Floor(Tirage((c.X0 + i >> 1) + 900, b) * 3.2);
			for (int j = 1; j <= num; j++)
			{
				p.Poser(c.X0 + i, c.Ecume + 5 + j, (j == num) ? Pale : Blanc, 0.9);
			}
		}
		void Rang(int graine, int nombre, double hauteurMin, double hauteurMax, double demiMin, double demiMax, double vitesse, (byte, byte, byte) ton, (byte, byte, byte) coeur, double force)
		{
			Array.Clear(hauteurs);
			for (int k = 0; k < nombre; k++)
			{
				double num2 = ((double)k + 0.5) * (double)c.Largeur / (double)nombre + (Tirage(k, graine) - 0.5) * 9.0 + 3.0 * Math.Sin((double)t * 0.0011 + (double)k * 1.9 + (double)graine);
				double num3 = Bruit((double)k * 2.7 + (double)(graine * 13), (double)t * vitesse + (double)graine);
				double num4 = hauteurMin + (hauteurMax - hauteurMin) * num3 * num3;
				double num5 = demiMin + (demiMax - demiMin) * Tirage(k, graine + 1);
				double num6 = (Tirage(k, graine + 2) - 0.5) * 0.5;
				for (double num7 = 0.0 - Math.Ceiling(num5); num7 <= num5; num7 += 1.0)
				{
					int num8 = (int)Math.Floor(num2 + num7 + 0.5);
					if (num8 >= 0 && num8 < hauteurs.Length)
					{
						double num9 = num4 * Math.Pow(Math.Max(0.0, 1.0 - Math.Abs(num7) / num5), 1.6) * (1.0 + num6 * num7 / num5);
						if (num9 > hauteurs[num8])
						{
							hauteurs[num8] = num9;
						}
					}
				}
			}
			long b2 = (long)Math.Floor((double)t / 95.0) + graine;
			for (int l = 0; l < hauteurs.Length; l++)
			{
				int num10 = (int)Math.Floor(hauteurs[l] + Tirage(l >> 1, b2) * 1.6 + 0.5);
				for (int m = 0; m <= num10; m++)
				{
					bool flag = m >= num10 - 1;
					p.Poser(c.X0 + l, c.Ecume - 1 - m, flag ? Turquoise : (((double)m >= (double)num10 * 0.55) ? ton : coeur), force * (flag ? 0.75 : 1.0));
				}
			}
		}
	}

	internal static void Embruns(Tampon p, Chute c, long t, Func<int, int, bool> surLEau)
	{
		int i = 0;
		for (int num = c.Nombre(80); i < num; i++)
		{
			double num2 = 950.0 + Tirage(i, 51L) * 800.0;
			(long Tour, double U) tuple = Cycle(t, Tirage(i, 53L) * 6000.0, num2);
			long item = tuple.Tour;
			double item2 = tuple.U;
			double num3 = item2 * num2 / 1000.0;
			double num4 = (double)(c.X0 + 4) + Tirage(i, item + 60) * (double)(c.Largeur - 8);
			double num5 = (Tirage(i, item + 61) - 0.5) * 64.0;
			double num6 = 0.0 - (52.0 + Tirage(i, item + 62) * 78.0);
			double num7 = 6.0 + Tirage(i, item + 63) * 44.0;
			double num8 = (0.0 - num6 + Math.Sqrt(num6 * num6 + 460.0 * num7)) / 230.0;
			if (num3 < num8)
			{
				double num9 = num4 + num5 * num3;
				double num10 = (double)(c.Ecume - 6) + num6 * num3 + 115.0 * num3 * num3;
				p.Poser(num9, num10, Blanc, 1.0);
				p.Poser(num9, num10 - 1.0, Pale, 0.7);
				if (Tirage(i, item + 64) > 0.7)
				{
					p.Poser(num9 + 1.0, num10, Blanc, 1.0);
					p.Poser(num9 + 1.0, num10 - 1.0, Pale, 0.7);
				}
				continue;
			}
			double num11 = (num3 - num8) / 0.42;
			if (num11 >= 1.0)
			{
				continue;
			}
			double num12 = num4 + num5 * num8;
			double num13 = (double)(c.Ecume - 6) + num7;
			double num14 = 1.5 + num11 * 6.5;
			double num15 = num14 * 0.5;
			for (int j = 0; j < 24; j++)
			{
				double num16 = num12 + Math.Cos((double)j * Math.PI / 12.0) * num14;
				double num17 = num13 + Math.Sin((double)j * Math.PI / 12.0) * num15;
				if (surLEau((int)Math.Floor(num16 + 0.5), (int)Math.Floor(num17 + 0.5)))
				{
					p.Poser(num16, num17, Blanc, 0.7 * (1.0 - num11));
				}
			}
		}
	}

	internal static void Brume(Tampon p, Chute c, long t, double nuit)
	{
		(byte, byte, byte) ton = (Entre(240.0, 170.0), Entre(249.0, 204.0), Entre(252.0, 255.0));
		double poids = 1.0 - 0.2 * nuit;
		int i = 0;
		for (int num = c.Nombre(14); i < num; i++)
		{
			Voile(i, 5200.0 + Tirage(i, 71L) * 3200.0, (double)(c.X0 + 20) + Tirage(i, 72L) * (double)(c.Largeur - 40), (double)(c.Ecume - 10) - Tirage(i, 74L) * 12.0, 54.0 + Tirage(i, 75L) * 52.0, (Tirage(i, 76L) - 0.5) * 22.0, 30.0, 66.0, 1.0, c.X0 + 2, c.X1 - 2);
		}
		int j = 20;
		for (int num2 = 20 + c.Nombre(7); j < num2; j++)
		{
			Voile(j, 7000.0 + Tirage(j, 71L) * 3000.0, (double)(c.X0 + 14) + Tirage(j, 72L) * (double)(c.Largeur - 28), (double)(c.Ecume + 14) + Tirage(j, 74L) * 12.0, 0.0 - (8.0 + Tirage(j, 75L) * 20.0), (Tirage(j, 76L) - 0.5) * 60.0, 40.0, 84.0, 0.62, c.X0 - 6, c.X1 + 6);
		}
		byte Entre(double jour, double deNuit)
		{
			return (byte)Math.Round(jour + (deNuit - jour) * nuit);
		}
		void Voile(int k, double duree, double x0, double y0, double monte, double derive, double large0, double large1, double force, int gauche, int droite)
		{
			(long Tour, double U) tuple = Cycle(t, Tirage(k, 73L) * duree, duree);
			long item = tuple.Tour;
			double item2 = tuple.U;
			double num3 = Math.Pow(Math.Sin(Math.PI * item2), 0.7) * force * poids;
			double num4 = x0 + (Tirage(k, item + 80) - 0.5) * 24.0 + derive * item2 + Math.Sin((double)t * 0.0006 + (double)k) * 4.0;
			double num5 = y0 - monte * item2;
			double num6 = (large0 + (large1 - large0) * item2) / 2.0;
			double num7 = num6 * 0.34;
			double num8 = (double)k * 37.7 + (double)item * 11.3;
			for (double num9 = 0.0 - Math.Ceiling(num7) - 2.0; num9 <= num7 + 2.0; num9 += 1.0)
			{
				for (double num10 = 0.0 - Math.Ceiling(num6) - 3.0; num10 <= num6 + 3.0; num10 += 1.0)
				{
					double num11 = num4 + num10;
					if (!(num11 < (double)gauche) && !(num11 >= (double)droite))
					{
						double num12 = num10 / num6;
						double num13 = num9 / num7;
						double num14 = num12 * num12 + num13 * num13 + (Bruit(num10 * 0.19 + num8, num9 * 0.45 + num8 * 1.7 + (double)t * 0.0004) - 0.5) * 1.2;
						if (num14 < 0.42)
						{
							p.Poser(num11, num5 + num9, ton, 0.52 * num3);
						}
						else if (num14 < 0.98)
						{
							p.Poser(num11, num5 + num9, ton, 0.27 * num3);
						}
					}
				}
			}
		}
	}

	internal static void ArcEnCiel(Tampon p, Chute c, long t, double force)
	{
		if (force <= 0.01)
		{
			return;
		}
		double num = (double)c.X0 + c.Mesure(94.0);
		double num2 = c.Ecume + 30;
		double num3 = c.Mesure(78.0);
		double num4 = 0.8 + 0.2 * Math.Sin((double)t * 0.0009);
		for (int i = (int)(num2 - num3 - 8.0); i < c.Ecume - 2; i++)
		{
			for (int j = Math.Max(c.X0, (int)(num - num3 - 8.0)); j <= Math.Min(c.X1 - 1, (int)(num + num3 + 8.0)); j++)
			{
				double num5 = (double)j - num;
				double num6 = (double)i - num2;
				int num7 = (int)Math.Floor(Math.Sqrt(num5 * num5 + num6 * num6) - num3);
				if (num7 >= 0 && num7 < 6)
				{
					double num8 = Math.Atan2(0.0 - num6, num5);
					double num9 = Math.Clamp((num8 - 0.3) / 0.45, 0.0, 1.0) * Math.Clamp((2.85 - num8) / 0.8, 0.0, 1.0);
					p.Poser(j, i, Bandes[num7], 0.4 * num9 * num4 * force);
				}
			}
		}
	}

	internal static (double X, double Y, double Eclat) Luciole(int k, double attacheX, double attacheY, long t)
	{
		double num = 0.7 + Tirage(k, 91L) * 0.6;
		double item = attacheX + 20.0 * Math.Sin((double)t * 0.0006 * num + (double)k) + 9.0 * Math.Sin((double)t * 0.0013 * num + (double)k * 2.1);
		double item2 = attacheY + 12.0 * Math.Sin((double)t * 0.0008 * num + (double)k * 1.3) + 5.0 * Math.Cos((double)t * 0.0015 * num + (double)k);
		return (X: item, Y: item2, Eclat: 0.25 + 0.75 * Math.Max(0.0, Math.Sin((double)t * 0.0026 * num + (double)k * 2.4)));
	}
}
