using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Enums;

namespace Intersect.Framework.Core.GameObjects.Maps;

public sealed class IndexDesLignes
{
	public static readonly IndexDesLignes Vide = new IndexDesLignes(null, null, null, Array.Empty<LigneDeBlocage>());

	private readonly LigneDeBlocage[]?[,]? _parCase;

	private LigneDeBlocage[]?[,]? _sansVide;

	private LigneDeBlocage[]?[,]? _vide;

	private readonly bool[,]? _coupees;

	private readonly byte[,]? _passages;

	private static readonly (int X, int Y)[] Pas = new(int, int)[8]
	{
		(0, -1),
		(0, 1),
		(-1, 0),
		(1, 0),
		(-1, -1),
		(1, -1),
		(1, 1),
		(-1, 1)
	};

	public object? Source { get; private init; }

	public bool Aucune => _parCase == null;

	public LigneDeBlocage[] PourLesTirs { get; }

	public bool AVide => _vide != null;

	private IndexDesLignes(LigneDeBlocage[]?[,]? parCase, bool[,]? coupees, byte[,]? passages, LigneDeBlocage[] pourLesTirs)
	{
		_parCase = parCase;
		_coupees = coupees;
		_passages = passages;
		PourLesTirs = pourLesTirs;
	}

	public LigneDeBlocage[]? DeLaCase(int x, int y)
	{
		if (_parCase != null && x >= 0 && y >= 0 && x < _parCase.GetLength(0) && y < _parCase.GetLength(1))
		{
			return _parCase[x, y];
		}
		return null;
	}

	public LigneDeBlocage[]? DeLaCaseSansVide(int x, int y)
	{
		if (_vide != null)
		{
			if (x >= 0 && y >= 0 && x < _sansVide.GetLength(0) && y < _sansVide.GetLength(1))
			{
				return _sansVide[x, y];
			}
			return null;
		}
		return DeLaCase(x, y);
	}

	public LigneDeBlocage[]? VideDeLaCase(int x, int y)
	{
		if (_vide != null && x >= 0 && y >= 0 && x < _vide.GetLength(0) && y < _vide.GetLength(1))
		{
			return _vide[x, y];
		}
		return null;
	}

	public bool Coupee(int x, int y)
	{
		if (_coupees != null && x >= 0 && y >= 0 && x < _coupees.GetLength(0) && y < _coupees.GetLength(1))
		{
			return _coupees[x, y];
		}
		return false;
	}

	public bool Ferme(int x, int y, Direction direction)
	{
		if (_passages != null && direction >= Direction.Up && direction <= Direction.DownLeft && x >= 0 && y >= 0 && x < _passages.GetLength(0) && y < _passages.GetLength(1))
		{
			return (_passages[x, y] & (1 << (int)direction)) != 0;
		}
		return false;
	}

	public byte Passages(int x, int y)
	{
		if (_passages != null && x >= 0 && y >= 0 && x < _passages.GetLength(0) && y < _passages.GetLength(1))
		{
			return _passages[x, y];
		}
		return 0;
	}

	public static Direction EnFace(Direction direction)
	{
		return direction switch
		{
			Direction.Up => Direction.Down, 
			Direction.Down => Direction.Up, 
			Direction.Left => Direction.Right, 
			Direction.Right => Direction.Left, 
			Direction.UpLeft => Direction.DownRight, 
			Direction.UpRight => Direction.DownLeft, 
			Direction.DownRight => Direction.UpLeft, 
			Direction.DownLeft => Direction.UpRight, 
			_ => Direction.None, 
		};
	}

	public static Direction DuPas(int dx, int dy)
	{
		switch (dx)
		{
		case 0:
			switch (dy)
			{
			case -1:
				return Direction.Up;
			case 1:
				return Direction.Down;
			}
			break;
		case -1:
			switch (dy)
			{
			case 0:
				return Direction.Left;
			case -1:
				return Direction.UpLeft;
			case 1:
				return Direction.DownLeft;
			}
			break;
		case 1:
			switch (dy)
			{
			case 0:
				return Direction.Right;
			case -1:
				return Direction.UpRight;
			case 1:
				return Direction.DownRight;
			}
			break;
		}
		return Direction.None;
	}

	public static bool BarreLePas(IndexDesLignes depart, int x, int y, IndexDesLignes arrivee, int versX, int versY, Direction direction)
	{
		if (direction < Direction.Up || direction > Direction.DownLeft || (depart.Aucune && arrivee.Aucune))
		{
			return false;
		}
		if (!depart.Ferme(x, y, direction) && !arrivee.Coupee(versX, versY))
		{
			return arrivee.Ferme(versX, versY, EnFace(direction));
		}
		return true;
	}

	public static IndexDesLignes Construire(IReadOnlyList<LigneDeBlocage>? lignes, int colonnes, int rangees, int largeurCase, int hauteurCase)
	{
		LigneDeBlocage[] array = lignes?.Where((LigneDeBlocage l) => l != null && l.Longueur >= 1f).ToArray() ?? Array.Empty<LigneDeBlocage>();
		if (array.Length == 0 || colonnes <= 0 || rangees <= 0)
		{
			return new IndexDesLignes(null, null, null, Array.Empty<LigneDeBlocage>())
			{
				Source = lignes
			};
		}
		List<LigneDeBlocage>[,] array2 = new List<LigneDeBlocage>[colonnes, rangees];
		LigneDeBlocage[] array3 = array;
		foreach (LigneDeBlocage ligneDeBlocage in array3)
		{
			int num2 = Math.Clamp((int)MathF.Floor((MathF.Min(ligneDeBlocage.X0, ligneDeBlocage.X1) - 0.5f) / (float)largeurCase), 0, colonnes - 1);
			int num3 = Math.Clamp((int)MathF.Floor((MathF.Max(ligneDeBlocage.X0, ligneDeBlocage.X1) + 0.5f) / (float)largeurCase), 0, colonnes - 1);
			int num4 = Math.Clamp((int)MathF.Floor((MathF.Min(ligneDeBlocage.Y0, ligneDeBlocage.Y1) - 0.5f) / (float)hauteurCase), 0, rangees - 1);
			int num5 = Math.Clamp((int)MathF.Floor((MathF.Max(ligneDeBlocage.Y0, ligneDeBlocage.Y1) + 0.5f) / (float)hauteurCase), 0, rangees - 1);
			for (int num6 = num2; num6 <= num3; num6++)
			{
				for (int num7 = num4; num7 <= num5; num7++)
				{
					float num8 = (float)(num6 * largeurCase) - 0.5f;
					float num9 = (float)(num7 * hauteurCase) - 0.5f;
					float num10 = num8 + (float)largeurCase + 1f;
					float num11 = num9 + (float)hauteurCase + 1f;
					if ((ligneDeBlocage.X0 >= num8 && ligneDeBlocage.X0 <= num10 && ligneDeBlocage.Y0 >= num9 && ligneDeBlocage.Y0 <= num11) || (ligneDeBlocage.X1 >= num8 && ligneDeBlocage.X1 <= num10 && ligneDeBlocage.Y1 >= num9 && ligneDeBlocage.Y1 <= num11) || ligneDeBlocage.Coupe(num8, num9, num10, num9) || ligneDeBlocage.Coupe(num10, num9, num10, num11) || ligneDeBlocage.Coupe(num10, num11, num8, num11) || ligneDeBlocage.Coupe(num8, num11, num8, num9))
					{
						List<LigneDeBlocage>[,] array4 = array2;
						int num12 = num6;
						int num13 = num7;
						(array4[num12, num13] ?? (array4[num12, num13] = new List<LigneDeBlocage>())).Add(ligneDeBlocage);
					}
				}
			}
		}
		LigneDeBlocage[,][] array5 = new LigneDeBlocage[colonnes, rangees][];
		bool[,] array6 = new bool[colonnes, rangees];
		byte[,] array7 = new byte[colonnes, rangees];
		float num14 = 6f;
		for (int num15 = 0; num15 < colonnes; num15++)
		{
			for (int num16 = 0; num16 < rangees; num16++)
			{
				array5[num15, num16] = array2[num15, num16]?.ToArray();
				float num17 = (float)(num15 * largeurCase) + (float)largeurCase / 2f;
				float num18 = (float)(num16 * hauteurCase) + (float)hauteurCase / 2f + 6f;
				LigneDeBlocage[] array8 = array;
				foreach (LigneDeBlocage ligneDeBlocage2 in array8)
				{
					if (!array6[num15, num16] && ligneDeBlocage2.DistanceCarree(num17, num18) < num14 * num14)
					{
						array6[num15, num16] = true;
					}
					for (int num20 = 0; num20 < Pas.Length; num20++)
					{
						if ((array7[num15, num16] & (1 << num20)) == 0 && ligneDeBlocage2.Coupe(num17, num18, num17 + (float)(Pas[num20].X * largeurCase), num18 + (float)(Pas[num20].Y * hauteurCase)))
						{
							array7[num15, num16] |= (byte)(1 << num20);
						}
					}
				}
			}
		}
		IndexDesLignes indexDesLignes = new IndexDesLignes(array5, array6, array7, array.Where((LigneDeBlocage l) => l.ArreteLesTirs).ToArray())
		{
			Source = lignes
		};
		if (array.Any((LigneDeBlocage l) => l.Chute))
		{
			indexDesLignes._sansVide = new LigneDeBlocage[colonnes, rangees][];
			indexDesLignes._vide = new LigneDeBlocage[colonnes, rangees][];
			for (int num21 = 0; num21 < colonnes; num21++)
			{
				for (int num22 = 0; num22 < rangees; num22++)
				{
					LigneDeBlocage[] array9 = array5[num21, num22];
					if (array9 != null)
					{
						LigneDeBlocage[] array10 = array9.Where((LigneDeBlocage l) => l.Chute).ToArray();
						indexDesLignes._vide[num21, num22] = ((array10.Length == 0) ? null : array10);
						indexDesLignes._sansVide[num21, num22] = ((array10.Length == 0) ? array9 : ((array10.Length == array9.Length) ? null : array9.Where((LigneDeBlocage l) => !l.Chute).ToArray()));
					}
				}
			}
		}
		return indexDesLignes;
	}
}
