using System;
using Intersect.Config;

namespace Intersect.Framework.Core.GameObjects.Casse;

public sealed class CasseObjet
{
	public const int Maille = 8;

	public CasseGenre Genre { get; init; }

	public int X { get; init; }

	public int Y { get; init; }

	public int Largeur { get; init; }

	public int Hauteur { get; init; }

	public (string Couche, int X, int Y)[] Cases { get; init; } = Array.Empty<(string, int, int)>();

	public (int X, int Y)[] Parties { get; init; } = Array.Empty<(int, int)>();

	public bool Rocher { get; init; }

	public (int Partie, Guid Carte, int Objet, int AutrePartie)[] Liens { get; init; } = Array.Empty<(int, Guid, int, int)>();

	public byte[] Silhouette { get; init; } = Array.Empty<byte>();

	public bool Dehors(int partie, int largeurCarte, int hauteurCarte)
	{
		if (partie >= 0 && partie < Parties.Length)
		{
			if (Parties[partie].X >= 0 && Parties[partie].Y >= 0 && Parties[partie].X < largeurCarte)
			{
				return Parties[partie].Y >= hauteurCarte;
			}
			return true;
		}
		return false;
	}

	public bool Opaque(float px, float py, int tuile)
	{
		if (Silhouette.Length == 0)
		{
			return false;
		}
		int num = Largeur * tuile / 8;
		int num2 = Hauteur * tuile / 8;
		int num3 = (int)MathF.Floor((px - (float)(X * tuile)) / 8f);
		int num4 = (int)MathF.Floor((py - (float)(Y * tuile)) / 8f);
		if (num3 < 0 || num4 < 0 || num3 >= num || num4 >= num2)
		{
			return false;
		}
		int num5 = num4 * num + num3;
		if (num5 >> 3 < Silhouette.Length)
		{
			return (Silhouette[num5 >> 3] & (1 << (num5 & 7))) != 0;
		}
		return false;
	}
}
