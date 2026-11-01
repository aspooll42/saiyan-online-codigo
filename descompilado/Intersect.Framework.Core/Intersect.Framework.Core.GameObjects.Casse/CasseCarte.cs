using System;
using System.Collections.Generic;
using Intersect.Config;

namespace Intersect.Framework.Core.GameObjects.Casse;

public sealed class CasseCarte
{
	public Guid Id { get; init; }

	public string Nom { get; init; } = string.Empty;

	public string Signature { get; init; } = string.Empty;

	public CasseCarteGenre Genre { get; init; }

	public CasseObjet[] Objets { get; init; } = Array.Empty<CasseObjet>();

	public Dictionary<(int X, int Y), int[]> ParCase { get; init; } = new Dictionary<(int, int), int[]>();

	public int ObjetA(int x, int y)
	{
		if (!ParCase.TryGetValue((x, y), out var value) || value.Length == 0)
		{
			return -1;
		}
		return value[0];
	}

	public int ObjetAuPixel(float px, float py, int tuile)
	{
		if (px < 0f || py < 0f || !ParCase.TryGetValue(((int)(px / (float)tuile), (int)(py / (float)tuile)), out var value))
		{
			return -1;
		}
		int result = -1;
		int num = int.MinValue;
		int[] array = value;
		foreach (int num2 in array)
		{
			CasseObjet casseObjet = Objets[num2];
			if (casseObjet.Y + casseObjet.Hauteur > num && casseObjet.Opaque(px, py, tuile))
			{
				num = casseObjet.Y + casseObjet.Hauteur;
				result = num2;
			}
		}
		return result;
	}

	public int PartiePres(int objet, float x, float y)
	{
		if (objet < 0 || objet >= Objets.Length)
		{
			return 0;
		}
		(int, int)[] parties = Objets[objet].Parties;
		int result = 0;
		float num = float.MaxValue;
		for (int i = 0; i < parties.Length; i++)
		{
			float num2 = (float)parties[i].Item1 - x;
			float num3 = (float)parties[i].Item2 - y;
			float num4 = num2 * num2 + num3 * num3;
			if (num4 < num)
			{
				num = num4;
				result = i;
			}
		}
		return result;
	}
}
