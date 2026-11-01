using System;
using Intersect.Framework.Core.GameObjects.Maps;

namespace Intersect.Framework.Core.Entities;

public static class CorpsLibre
{
	public delegate bool EstBloquee(int caseX, int caseY);

	public delegate float ProfondeurEau(float x, float y);

	public delegate LigneDeBlocage[]? LignesDeLaCase(int caseX, int caseY, out float origineX, out float origineY);

	public const float Rayon = 10f;

	public const float DecalageVersLesPieds = 6f;

	public const float AideAuxCoins = 32f;

	public const float ProfondeurPermise = 1.5f;

	private static readonly (float X, float Y)[] Semelle = new(float, float)[9]
	{
		(0f, 0f),
		(-0.7f, 0f),
		(0.7f, 0f),
		(0f, -0.4f),
		(0f, 0.4f),
		(-0.5f, -0.28f),
		(0.5f, -0.28f),
		(-0.5f, 0.28f),
		(0.5f, 0.28f)
	};

	public static float ProfondeurSousLesPieds(float x, float y, float rayon, ProfondeurEau profondeur, out float pointX, out float pointY)
	{
		float num = y + rayon * 0.6f;
		float num2 = 0f;
		pointX = x;
		pointY = num;
		(float, float)[] semelle = Semelle;
		for (int i = 0; i < semelle.Length; i++)
		{
			(float, float) tuple = semelle[i];
			float item = tuple.Item1;
			float item2 = tuple.Item2;
			float num3 = x + item * rayon;
			float num4 = num + item2 * rayon;
			float num5 = profondeur(num3, num4);
			if (num5 > num2)
			{
				num2 = num5;
				pointX = num3;
				pointY = num4;
			}
		}
		return num2;
	}

	public static float Vitesse(float millisecondesParCase, int largeurCase)
	{
		if (!(millisecondesParCase <= 0f))
		{
			return (float)largeurCase * 1000f / millisecondesParCase;
		}
		return 0f;
	}

	public static bool Chevauche(float x, float y, float rayon, int largeurCase, int hauteurCase, EstBloquee bloquee, LignesDeLaCase? lignes = null)
	{
		float normaleX;
		float normaleY;
		return PlusProfondContact(x, y, rayon, largeurCase, hauteurCase, bloquee, lignes, out normaleX, out normaleY) > 0.001f;
	}

	public static (float X, float Y) Deplacer(float x, float y, float dx, float dy, int largeurCase, int hauteurCase, EstBloquee bloquee, float rayon = 10f, float aideAuxCoins = 32f, ProfondeurEau? profondeur = null, LignesDeLaCase? lignes = null)
	{
		float num = MathF.Sqrt(dx * dx + dy * dy);
		if (num < 0.0001f)
		{
			return (X: x, Y: y);
		}
		int num2 = Math.Max(1, (int)MathF.Ceiling(num / (rayon * 0.5f)));
		float num3 = dx / (float)num2;
		float num4 = dy / (float)num2;
		float num5 = num / (float)num2;
		for (int i = 0; i < num2; i++)
		{
			float num6 = x;
			float num7 = y;
			x += num3;
			y += num4;
			Repousser(ref x, ref y, rayon, largeurCase, hauteurCase, bloquee, lignes);
			float num8 = ((x - num6) * num3 + (y - num7) * num4) / num5;
			if (aideAuxCoins > 0f && num8 < num5 * 0.3f)
			{
				GuiderVersLOuverture(ref x, ref y, num3, num4, num5, rayon, aideAuxCoins, largeurCase, hauteurCase, bloquee, profondeur, lignes);
			}
			if (profondeur != null && RamenerVersLaRive(ref x, ref y, rayon, profondeur))
			{
				Repousser(ref x, ref y, rayon, largeurCase, hauteurCase, bloquee, lignes);
			}
		}
		return (X: x, Y: y);
	}

	public static bool TrajetLibre(float x0, float y0, float x1, float y1, float sonde, int largeurCase, int hauteurCase, EstBloquee bloquee, LignesDeLaCase? lignes = null)
	{
		float num = x1 - x0;
		float num2 = y1 - y0;
		float num3 = MathF.Sqrt(num * num + num2 * num2);
		int num4 = Math.Max(1, (int)MathF.Ceiling(num3 / 4f));
		for (int i = 1; i <= num4; i++)
		{
			float num5 = (float)i / (float)num4;
			if (Chevauche(x0 + num * num5, y0 + num2 * num5, sonde, largeurCase, hauteurCase, bloquee))
			{
				return false;
			}
		}
		if (lignes != null)
		{
			return !CoupeUneLigne(x0, y0, x1, y1, largeurCase, hauteurCase, lignes);
		}
		return true;
	}

	public static bool CoupeUneLigne(float x0, float y0, float x1, float y1, int largeurCase, int hauteurCase, LignesDeLaCase lignes)
	{
		int num = (int)MathF.Floor(MathF.Min(x0, x1) / (float)largeurCase);
		int num2 = (int)MathF.Floor(MathF.Max(x0, x1) / (float)largeurCase);
		int num3 = (int)MathF.Floor(MathF.Min(y0, y1) / (float)hauteurCase);
		int num4 = (int)MathF.Floor(MathF.Max(y0, y1) / (float)hauteurCase);
		for (int i = num; i <= num2; i++)
		{
			for (int j = num3; j <= num4; j++)
			{
				float origineX;
				float origineY;
				LigneDeBlocage[] array = lignes(i, j, out origineX, out origineY);
				if (array == null)
				{
					continue;
				}
				LigneDeBlocage[] array2 = array;
				foreach (LigneDeBlocage ligneDeBlocage in array2)
				{
					if (ligneDeBlocage.Coupe(x0 - origineX, y0 - origineY, x1 - origineX, y1 - origineY))
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private static float PlusProfondContact(float x, float y, float rayon, int largeurCase, int hauteurCase, EstBloquee bloquee, LignesDeLaCase? lignes, out float normaleX, out float normaleY)
	{
		normaleX = 0f;
		normaleY = 0f;
		float num = 0f;
		int num2 = (int)MathF.Floor((x - rayon) / (float)largeurCase);
		int num3 = (int)MathF.Floor((x + rayon) / (float)largeurCase);
		int num4 = (int)MathF.Floor((y - rayon) / (float)hauteurCase);
		int num5 = (int)MathF.Floor((y + rayon) / (float)hauteurCase);
		for (int i = num2; i <= num3; i++)
		{
			for (int j = num4; j <= num5; j++)
			{
				if (lignes != null)
				{
					float origineX;
					float origineY;
					LigneDeBlocage[] array = lignes(i, j, out origineX, out origineY);
					if (array != null)
					{
						LigneDeBlocage[] array2 = array;
						foreach (LigneDeBlocage ligneDeBlocage in array2)
						{
							float num6 = ligneDeBlocage.X1 - ligneDeBlocage.X0;
							float num7 = ligneDeBlocage.Y1 - ligneDeBlocage.Y0;
							float num8 = num6 * num6 + num7 * num7;
							if (num8 < 0.01f)
							{
								continue;
							}
							float num9 = x - origineX;
							float num10 = y - origineY;
							float num11 = Math.Clamp(((num9 - ligneDeBlocage.X0) * num6 + (num10 - ligneDeBlocage.Y0) * num7) / num8, 0f, 1f);
							float num12 = num9 - (ligneDeBlocage.X0 + num6 * num11);
							float num13 = num10 - (ligneDeBlocage.Y0 + num7 * num11);
							float num14 = num12 * num12 + num13 * num13;
							if (!(num14 >= rayon * rayon))
							{
								float num16;
								float num17;
								float num18;
								if (num14 > 1E-06f)
								{
									float num15 = MathF.Sqrt(num14);
									num16 = rayon - num15;
									num17 = num12 / num15;
									num18 = num13 / num15;
								}
								else
								{
									float num19 = MathF.Sqrt(num8);
									num16 = rayon;
									num17 = (0f - num7) / num19;
									num18 = num6 / num19;
								}
								if (num16 > num)
								{
									num = num16;
									normaleX = num17;
									normaleY = num18;
								}
							}
						}
					}
				}
				if (!bloquee(i, j))
				{
					continue;
				}
				int num20 = i * largeurCase;
				int num21 = num20 + largeurCase;
				int num22 = j * hauteurCase;
				int num23 = num22 + hauteurCase;
				float num24 = Math.Clamp(x, num20, num21);
				float num25 = Math.Clamp(y, num22, num23);
				float num26 = x - num24;
				float num27 = y - num25;
				float num28 = num26 * num26 + num27 * num27;
				float num30;
				float num31;
				float num32;
				if (num28 > 1E-06f)
				{
					if (num28 >= rayon * rayon)
					{
						continue;
					}
					float num29 = MathF.Sqrt(num28);
					num30 = rayon - num29;
					num31 = num26 / num29;
					num32 = num27 / num29;
				}
				else
				{
					float num33 = x - (float)num20;
					float num34 = (float)num21 - x;
					float num35 = y - (float)num22;
					float y2 = (float)num23 - y;
					float num36 = MathF.Min(MathF.Min(num33, num34), MathF.Min(num35, y2));
					num30 = rayon + num36;
					if (num36 != num33)
					{
						if (num36 != num34)
						{
							if (num36 != num35)
							{
								float num37 = 1f;
								num32 = num37;
								num31 = 0f;
							}
							else
							{
								float num37 = -1f;
								num32 = num37;
								num31 = 0f;
							}
						}
						else
						{
							num32 = 0f;
							num31 = 1f;
						}
					}
					else
					{
						num32 = 0f;
						num31 = -1f;
					}
				}
				if (num30 > num)
				{
					num = num30;
					normaleX = num31;
					normaleY = num32;
				}
			}
		}
		return num;
	}

	private static void Repousser(ref float x, ref float y, float rayon, int largeurCase, int hauteurCase, EstBloquee bloquee, LignesDeLaCase? lignes)
	{
		for (int i = 0; i < 4; i++)
		{
			float normaleX;
			float normaleY;
			float num = PlusProfondContact(x, y, rayon, largeurCase, hauteurCase, bloquee, lignes, out normaleX, out normaleY);
			if (num <= 0.001f)
			{
				break;
			}
			x += normaleX * (num + 0.001f);
			y += normaleY * (num + 0.001f);
		}
	}

	private static void GuiderVersLOuverture(ref float x, ref float y, float pasX, float pasY, float pasLongueur, float rayon, float aide, int largeurCase, int hauteurCase, EstBloquee bloquee, ProfondeurEau? profondeur, LignesDeLaCase? lignes)
	{
		float versX = pasX / pasLongueur;
		float versY = pasY / pasLongueur;
		float num = 0f - versY;
		float num2 = versX;
		float sonde = MathF.Max(2f, pasLongueur);
		float? num3 = null;
		for (float num4 = 2f; num4 <= aide; num4 += 2f)
		{
			if (num3.HasValue)
			{
				break;
			}
			bool flag = Passe(x + num * num4, y + num2 * num4);
			bool flag2 = Passe(x - num * num4, y - num2 * num4);
			if (flag != flag2)
			{
				num3 = (flag ? num4 : (0f - num4));
			}
			else if (flag)
			{
				return;
			}
		}
		if (num3.HasValue)
		{
			float valueOrDefault = num3.GetValueOrDefault();
			float num5 = (float)MathF.Sign(valueOrDefault) * MathF.Min(MathF.Abs(valueOrDefault), pasLongueur);
			float num6 = x + num * num5;
			float num7 = y + num2 * num5;
			if (PlusProfondContact(num6, num7, rayon, largeurCase, hauteurCase, bloquee, lignes, out var _, out var _) <= 0.001f)
			{
				x = num6;
				y = num7;
			}
		}
		bool Passe(float px, float py)
		{
			if (PlusProfondContact(px, py, rayon, largeurCase, hauteurCase, bloquee, lignes, out var normaleX2, out var normaleY2) <= 0.001f && PlusProfondContact(px + versX * sonde, py + versY * sonde, rayon, largeurCase, hauteurCase, bloquee, lignes, out normaleY2, out normaleX2) <= 0.001f)
			{
				if (profondeur != null)
				{
					return ProfondeurSousLesPieds(px + versX * sonde, py + versY * sonde, rayon, profondeur, out normaleX2, out normaleY2) <= 1.5f;
				}
				return true;
			}
			return false;
		}
	}

	private static bool RamenerVersLaRive(ref float x, ref float y, float rayon, ProfondeurEau profondeur)
	{
		bool result = false;
		for (int i = 0; i < 3; i++)
		{
			float pointX;
			float pointY;
			float num = ProfondeurSousLesPieds(x, y, rayon, profondeur, out pointX, out pointY);
			if (num <= 1.5f)
			{
				break;
			}
			float num2 = profondeur(pointX + 3f, pointY) - profondeur(pointX - 3f, pointY);
			float num3 = profondeur(pointX, pointY + 3f) - profondeur(pointX, pointY - 3f);
			float num4 = MathF.Sqrt(num2 * num2 + num3 * num3);
			if (num4 < 0.0001f)
			{
				break;
			}
			float num5 = num - 1.5f;
			x -= num2 / num4 * num5;
			y -= num3 / num4 * num5;
			result = true;
		}
		return result;
	}
}
