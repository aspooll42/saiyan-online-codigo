using System;
using System.Collections.Generic;
using System.Diagnostics;
using Intersect.Client.General;

namespace Intersect.Client.Entities;

internal static class DeplacementLibre
{
	public const int RetardAffichageMs = 100;

	public const int IntervalleEnvoiMs = 50;

	private const float PredictionSautPx = 96f;

	private const double PredictionEcartMaxMs = 200.0;

	private static HashSet<Guid> _cartes = new HashSet<Guid>();

	private static readonly Stopwatch Montre = Stopwatch.StartNew();

	private static double _ecartServeur;

	private static bool _ecartConnu;

	private static HashSet<Guid> _cartesMonstres = new HashSet<Guid>();

	public static long MaintenantLocal => Montre.ElapsedMilliseconds;

	public static double MaintenantPrecis => Montre.Elapsed.TotalMilliseconds;

	public static double TempsServeur => MaintenantPrecis + _ecartServeur;

	public static int LargeurCarte => Options.Instance.Map.MapWidth * Options.Instance.Map.TileWidth;

	public static int HauteurCarte => Options.Instance.Map.MapHeight * Options.Instance.Map.TileHeight;

	public static int RetardDe(Entity entite)
	{
		if (!(entite is Player))
		{
			return 100;
		}
		return Math.Clamp(Options.Instance.Combat.AutresJoueursRetardMs, 0, 100);
	}

	public static int PredictionDe(Entity entite)
	{
		if (!(entite is Player))
		{
			return 0;
		}
		return Math.Clamp(Options.Instance.Combat.AutresJoueursPredictionMs, 0, 150);
	}

	internal static (float X, float Y) Predire(float avantX, float avantY, double avantT, float apresX, float apresY, double apresT, double rendu, int predictionMs)
	{
		double num = apresT - avantT;
		float num2 = apresX - avantX;
		float num3 = apresY - avantY;
		if (num <= 0.0 || num > 200.0 || predictionMs <= 0 || rendu <= apresT || num2 * num2 + num3 * num3 > 9216f)
		{
			return (X: apresX, Y: apresY);
		}
		float num4 = (float)(Math.Min(rendu - apresT, predictionMs) / num);
		return (X: apresX + num2 * num4, Y: apresY + num3 * num4);
	}

	public static bool EstLibre(Guid mapId)
	{
		if (mapId != Guid.Empty)
		{
			return _cartes.Contains(mapId);
		}
		return false;
	}

	public static bool MonstresLibres(Guid mapId)
	{
		if (mapId != Guid.Empty)
		{
			return _cartesMonstres.Contains(mapId);
		}
		return false;
	}

	public static void RecevoirCartes(Guid[]? cartes, Guid[]? cartesMonstres)
	{
		HashSet<Guid> hashSet = new HashSet<Guid>();
		Guid[] array = cartes ?? Array.Empty<Guid>();
		foreach (Guid item in array)
		{
			hashSet.Add(item);
		}
		_cartes = hashSet;
		HashSet<Guid> hashSet2 = new HashSet<Guid>();
		Guid[] array2 = cartesMonstres ?? Array.Empty<Guid>();
		foreach (Guid item in array2)
		{
			hashSet2.Add(item);
		}
		_cartesMonstres = hashSet2;
	}

	public static void NoterTempsServeur(long tempsServeur)
	{
		double num = (double)tempsServeur - (double)MaintenantLocal;
		if (!_ecartConnu || Math.Abs(num - _ecartServeur) > 400.0)
		{
			_ecartServeur = num;
			_ecartConnu = true;
		}
		else
		{
			double num2 = ((num > _ecartServeur) ? 0.2 : 0.02);
			_ecartServeur += (num - _ecartServeur) * num2;
		}
	}

	public static Guid CarteAuPoint(float mondeX, float mondeY, out int grilleX, out int grilleY)
	{
		grilleX = (int)MathF.Floor(mondeX / (float)LargeurCarte);
		grilleY = (int)MathF.Floor(mondeY / (float)HauteurCarte);
		Guid[,] mapGrid = Globals.MapGrid;
		if (mapGrid == null || grilleX < 0 || grilleY < 0 || grilleX >= Globals.MapGridWidth || grilleY >= Globals.MapGridHeight || grilleX >= mapGrid.GetLength(0) || grilleY >= mapGrid.GetLength(1))
		{
			return Guid.Empty;
		}
		return mapGrid[grilleX, grilleY];
	}
}
