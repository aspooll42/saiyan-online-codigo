using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Framework.Graphics;
using Intersect.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Maps;

internal sealed class ModeleArbre
{
	public const int EtageHaut = 0;

	public const int EtageMilieu = 1;

	public const int EtageBas = 2;

	public const int Ombre = 3;

	public const int Interieur = 4;

	public const int Tronc = 5;

	public const int RefletsHaut = 6;

	public const int RefletsMilieu = 12;

	private const int Calques = 18;

	private const int PosesReflets = 6;

	public IGameTexture Atlas;

	public int Largeur;

	public int Hauteur;

	public int LargeurTuile;

	public int HauteurTuile;

	public int Pied;

	public float[] Poids = Array.Empty<float>();

	public (int X, int Y)[] Points = Array.Empty<(int, int)>();

	public Color[] Tons = new Color[3]
	{
		Color.White,
		Color.White,
		Color.White
	};

	private bool[,,] _plein = new bool[18, 1, 1];

	private byte[] _atlas = Array.Empty<byte>();

	private static readonly uint[] Crc = Enumerable.Range(0, 256).Select(delegate(int n)
	{
		uint num = (uint)n;
		for (int i = 0; i < 8; i++)
		{
			num = (((num & 1) != 0) ? (0xEDB88320u ^ (num >> 1)) : (num >> 1));
		}
		return num;
	}).ToArray();

	public bool Plein(int calque, int caseX, int caseY)
	{
		return _plein[calque, caseX, caseY];
	}

	public static ModeleArbre? Construire(string planche, IGameTexture texture, int bx, int by, int blocLargeur, int blocHauteur)
	{
		int tileWidth = Options.Instance.Map.TileWidth;
		int tileHeight = Options.Instance.Map.TileHeight;
		return ConstruireZone(planche, texture, bx * blocLargeur * tileWidth, by * blocHauteur * tileHeight, blocLargeur * tileWidth, blocHauteur * tileHeight);
	}

	public static ModeleArbre? ConstruireZone(string nom, IGameTexture texture, int ox, int oy, int largeur, int hauteur)
	{
		try
		{
			int tileWidth = Options.Instance.Map.TileWidth;
			int tileHeight = Options.Instance.Map.TileHeight;
			if (largeur < tileWidth || hauteur < tileHeight || ox < 0 || oy < 0)
			{
				return null;
			}
			if (texture.GetTexture() == null || !PixelsPlanche.Lire(texture, out Color[] pixels) || pixels.Length != texture.Width * texture.Height)
			{
				return null;
			}
			if (ox + largeur > texture.Width || oy + hauteur > texture.Height)
			{
				return null;
			}
			ModeleArbre modeleArbre = new ModeleArbre
			{
				Largeur = largeur,
				Hauteur = hauteur,
				LargeurTuile = tileWidth,
				HauteurTuile = tileHeight
			};
			modeleArbre.Analyser(pixels, texture.Width, ox, oy, Math.Max(1, largeur / tileWidth), Math.Max(1, hauteur / tileHeight));
			if (modeleArbre.Poids.Length == 0)
			{
				return null;
			}
			byte[] png = Png(modeleArbre._atlas, 18 * largeur, hauteur);
			modeleArbre._atlas = Array.Empty<byte>();
			modeleArbre.Atlas = Graphics.Renderer.CreateTextureFromStreamFactory($"vent/{nom}_{ox}_{oy}_{largeur}x{hauteur}", () => new MemoryStream(png));
			return modeleArbre;
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Arbre {Nom} ({X},{Y}) laisse immobile", nom, ox, oy);
			return null;
		}
	}

	private void Analyser(Color[] source, int largeurPlanche, int ox, int oy, int cases, int rangs)
	{
		int w = Largeur;
		int h = Hauteur;
		byte[] genre = new byte[w * h];
		float[] lum = new float[w * h];
		int[] array = new int[h];
		int num = 0;
		for (int i = 0; i < h; i++)
		{
			for (int j = 0; j < w; j++)
			{
				Color color = Px(j, i);
				if (color.A == 0)
				{
					continue;
				}
				double num2 = (double)(int)color.R / 255.0;
				double num3 = (double)(int)color.G / 255.0;
				double num4 = (double)(int)color.B / 255.0;
				double num5 = Math.Max(num2, Math.Max(num3, num4));
				double num6 = Math.Min(num2, Math.Min(num3, num4));
				double num7 = (num5 + num6) / 2.0;
				double num8 = num5 - num6;
				double num9 = ((num8 == 0.0) ? 0.0 : ((num7 > 0.5) ? (num8 / (2.0 - num5 - num6)) : (num8 / (num5 + num6))));
				double num10 = 0.0;
				if (num8 > 0.0)
				{
					num10 = ((num5 == num2) ? (60.0 * ((num3 - num4) / num8 % 6.0)) : ((num5 == num3) ? (60.0 * ((num4 - num2) / num8 + 2.0)) : (60.0 * ((num2 - num3) / num8 + 4.0))));
				}
				if (num10 < 0.0)
				{
					num10 += 360.0;
				}
				int num11 = i * w + j;
				lum[num11] = (float)(0.299 * num2 + 0.587 * num3 + 0.114 * num4);
				if (num9 < 0.14 && color.A < 235)
				{
					genre[num11] = 1;
					continue;
				}
				bool flag = num9 >= 0.2 && num10 >= 55.0 && num10 <= 175.0;
				genre[num11] = (byte)(flag ? 2u : 3u);
				if (color.A > 200)
				{
					num = Math.Max(num, i);
				}
				if (flag)
				{
					array[i]++;
				}
			}
		}
		int num12 = num;
		while (num12 > 0 && array[num12] < 10)
		{
			num12--;
		}
		int k;
		for (k = 0; k < num12 && array[k] < 3; k++)
		{
		}
		if (num12 - k < 12)
		{
			return;
		}
		int num13 = Math.Min(num12 - 14, num - 30);
		Pied = num;
		Poids = new float[h];
		for (int l = 0; l < h; l++)
		{
			Poids[l] = Vent.Lisse((float)(num - l) / (float)Math.Max(1, num - num13));
		}
		int num14 = (int)Math.Round((double)k + (double)(num12 - k) * 0.36);
		int num15 = (int)Math.Round((double)k + (double)(num12 - k) * 0.68);
		int rayon = Math.Clamp((num15 - num14) / 2 - 1, 3, 10);
		int[] array2 = Couture(num14, rayon);
		int[] array3 = Couture(num15, rayon);
		List<float> list = new List<float>();
		for (int m = 0; m < w * h; m++)
		{
			if (genre[m] == 2)
			{
				list.Add(lum[m]);
			}
		}
		if (list.Count == 0)
		{
			Poids = Array.Empty<float>();
			return;
		}
		list.Sort();
		float num16 = list[(int)((double)list.Count * 0.12)];
		float num17 = list[(int)((double)list.Count * 0.5)];
		float num18 = list[(int)((double)list.Count * 0.84)];
		long num19 = 0L;
		long num20 = 0L;
		long num21 = 0L;
		long num22 = 0L;
		Color color2 = null;
		Color color3 = null;
		for (int n = 0; n < h; n++)
		{
			for (int num23 = 0; num23 < w; num23++)
			{
				int num24 = n * w + num23;
				if (genre[num24] == 2)
				{
					Color color4 = Px(num23, n);
					if (lum[num24] <= num16)
					{
						num19 += color4.R;
						num20 += color4.G;
						num21 += color4.B;
						num22++;
					}
					if (color2 == null && Math.Abs(lum[num24] - num17) < 0.004f)
					{
						color2 = color4;
					}
					if (color3 == null && Math.Abs(lum[num24] - num18) < 0.004f)
					{
						color3 = color4;
					}
				}
			}
		}
		long num25 = Math.Max(1L, num22);
		Color color5 = new Color(255, (int)(num19 / num25), (int)(num20 / num25), (int)(num21 / num25));
		Tons = new Color[3]
		{
			Opaque(color3 ?? color2 ?? color5),
			Opaque(color2 ?? color5),
			color5
		};
		bool[] array4 = new bool[w * h];
		Queue<(int, int)> queue = new Queue<(int, int)>();
		for (int num26 = Math.Max(0, num - 6); num26 <= num; num26++)
		{
			for (int num27 = 0; num27 < w; num27++)
			{
				if (genre[num26 * w + num27] == 3 && !array4[num26 * w + num27])
				{
					array4[num26 * w + num27] = true;
					queue.Enqueue((num27, num26));
				}
			}
		}
		while (queue.Count > 0)
		{
			(int, int) tuple = queue.Dequeue();
			int item = tuple.Item1;
			int item2 = tuple.Item2;
			for (int num28 = -1; num28 <= 1; num28++)
			{
				for (int num29 = -1; num29 <= 1; num29++)
				{
					int num30 = item + num29;
					int num31 = item2 + num28;
					if (num30 >= 0 && num31 >= 0 && num30 < w && num31 < h && genre[num31 * w + num30] == 3 && !array4[num31 * w + num30])
					{
						array4[num31 * w + num30] = true;
						queue.Enqueue((num30, num31));
					}
				}
			}
		}
		_atlas = new byte[18 * w * h * 4];
		_plein = new bool[18, cases, rangs];
		List<(int, int)> list2 = new List<(int, int)>();
		for (int num32 = 0; num32 < h; num32++)
		{
			for (int num33 = 0; num33 < w; num33++)
			{
				int num34 = num32 * w + num33;
				byte b = genre[num34];
				if (b == 0)
				{
					continue;
				}
				Color color6 = Px(num33, num32);
				int num35 = ((num32 >= array2[num33]) ? ((num32 < array3[num33]) ? 1 : 2) : 0);
				if (b == 1)
				{
					Poser((num32 >= num12 - 6) ? 3 : num35, num33, num32, color6);
					continue;
				}
				if (array4[num34])
				{
					Poser(5, num33, num32, color6);
					continue;
				}
				Poser(num35, num33, num32, color6);
				if (Plein(num33 - 2, num32) && Plein(num33 - 1, num32) && Plein(num33 + 1, num32) && Plein(num33 + 2, num32) && Plein(num33, num32 - 1) && Plein(num33, num32 + 1))
				{
					Color c = color5;
					for (int num36 = -2; num36 <= 2; num36++)
					{
						if (num33 + num36 >= 0 && num33 + num36 < w && array4[num32 * w + num33 + num36])
						{
							Color color7 = Px(num33 + num36, num32);
							c = new Color(255, color7.R * 4 / 5, color7.G * 4 / 5, color7.B * 4 / 5);
							break;
						}
					}
					Poser(4, num33, num32, c);
				}
				if (b != 2 || num35 == 2)
				{
					continue;
				}
				if (num33 % 3 == 0 && num32 % 3 == 0)
				{
					list2.Add((num33, num32));
				}
				if (lum[num34] < num18)
				{
					continue;
				}
				Color c2 = new Color(255, (int)Math.Round((double)(int)color6.R + (double)(255 - color6.R) * 0.38), (int)Math.Round((double)(int)color6.G + (double)(252 - color6.G) * 0.38), (int)Math.Round((double)(int)color6.B + (double)(210 - color6.B) * 0.38));
				for (int num37 = 0; num37 < 6; num37++)
				{
					if (Vent.Bruit2((double)num33 / 4.5 + (double)num37 * 13.7, (double)num32 / 4.5 + (double)num37 * 7.3) > 0.6)
					{
						Poser(((num35 == 0) ? 6 : 12) + num37, num33, num32, c2);
					}
				}
			}
		}
		Points = list2.ToArray();
		int[] Couture(int hauteur, int num39)
		{
			int num38 = 2 * num39 + 1;
			float[,] array5 = new float[w, num38];
			sbyte[,] array6 = new sbyte[w, num38];
			for (int num40 = 0; num40 < w; num40++)
			{
				for (int num41 = 0; num41 < num38; num41++)
				{
					int num42 = hauteur - num39 + num41;
					float num43 = ((num42 < 0 || num42 >= h) ? 99f : ((Plein(num40, num42) ? lum[num42 * w + num40] : 0f) + 0.004f * (float)Math.Abs(num42 - hauteur)));
					if (num40 == 0)
					{
						array5[num40, num41] = num43;
					}
					else
					{
						float num44 = array5[num40 - 1, num41];
						sbyte b2 = 0;
						if (num41 > 0 && array5[num40 - 1, num41 - 1] < num44)
						{
							num44 = array5[num40 - 1, num41 - 1];
							b2 = -1;
						}
						if (num41 < num38 - 1 && array5[num40 - 1, num41 + 1] < num44)
						{
							num44 = array5[num40 - 1, num41 + 1];
							b2 = 1;
						}
						array5[num40, num41] = num43 + num44;
						array6[num40, num41] = b2;
					}
				}
			}
			int num45 = 0;
			for (int num46 = 1; num46 < num38; num46++)
			{
				if (array5[w - 1, num46] < array5[w - 1, num45])
				{
					num45 = num46;
				}
			}
			int[] array7 = new int[w];
			for (int num47 = w - 1; num47 >= 0; num47--)
			{
				array7[num47] = hauteur - num39 + num45;
				num45 += array6[num47, num45];
			}
			return array7;
		}
		bool Plein(int x, int y)
		{
			if (x >= 0 && y >= 0 && x < w && y < h)
			{
				return genre[y * w + x] >= 2;
			}
			return false;
		}
		void Poser(int calque, int x, int y, Color color8)
		{
			int num38 = (y * 18 * w + calque * w + x) * 4;
			_atlas[num38] = color8.R;
			_atlas[num38 + 1] = color8.G;
			_atlas[num38 + 2] = color8.B;
			_atlas[num38 + 3] = color8.A;
			_plein[calque, Math.Min(cases - 1, x / LargeurTuile), Math.Min(rangs - 1, y / HauteurTuile)] = true;
		}
		Color Px(int x, int y)
		{
			return source[(oy + y) * largeurPlanche + ox + x];
		}
	}

	private static Color Opaque(Color c)
	{
		return new Color(255, c.R, c.G, c.B);
	}

	private static byte[] Png(byte[] rgba, int largeur, int hauteur)
	{
		using MemoryStream memoryStream = new MemoryStream();
		memoryStream.Write(new byte[8] { 137, 80, 78, 71, 13, 10, 26, 10 });
		byte[] array = new byte[13];
		EcrireEntier(array, 0, largeur);
		EcrireEntier(array, 4, hauteur);
		array[8] = 8;
		array[9] = 6;
		Morceau(memoryStream, "IHDR", array);
		byte[] array2 = new byte[(largeur * 4 + 1) * hauteur];
		for (int i = 0; i < hauteur; i++)
		{
			Buffer.BlockCopy(rgba, i * largeur * 4, array2, i * (largeur * 4 + 1) + 1, largeur * 4);
		}
		using (MemoryStream memoryStream2 = new MemoryStream())
		{
			using (ZLibStream zLibStream = new ZLibStream(memoryStream2, CompressionLevel.Fastest, leaveOpen: true))
			{
				zLibStream.Write(array2, 0, array2.Length);
			}
			Morceau(memoryStream, "IDAT", memoryStream2.ToArray());
		}
		Morceau(memoryStream, "IEND", Array.Empty<byte>());
		return memoryStream.ToArray();
	}

	private static void EcrireEntier(byte[] cible, int position, int valeur)
	{
		cible[position] = (byte)(valeur >> 24);
		cible[position + 1] = (byte)(valeur >> 16);
		cible[position + 2] = (byte)(valeur >> 8);
		cible[position + 3] = (byte)valeur;
	}

	private static void Morceau(Stream sortie, string type, byte[] donnees)
	{
		byte[] array = new byte[4];
		EcrireEntier(array, 0, donnees.Length);
		sortie.Write(array);
		byte[] array2 = new byte[4 + donnees.Length];
		for (int i = 0; i < 4; i++)
		{
			array2[i] = (byte)type[i];
		}
		Buffer.BlockCopy(donnees, 0, array2, 4, donnees.Length);
		sortie.Write(array2);
		uint num = uint.MaxValue;
		byte[] array3 = array2;
		foreach (byte b in array3)
		{
			num = Crc[(num ^ b) & 0xFF] ^ (num >> 8);
		}
		byte[] array4 = new byte[4];
		EcrireEntier(array4, 0, (int)num ^ -1);
		sortie.Write(array4);
	}
}
