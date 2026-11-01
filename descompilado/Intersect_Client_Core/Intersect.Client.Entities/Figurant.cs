using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Framework.Entities;
using Intersect.Client.General;
using Intersect.Client.Maps;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.Entities;
using Intersect.Framework.Core.GameObjects.Maps.Attributes;

namespace Intersect.Client.Entities;

public class Figurant : Critter
{
	internal enum Etats
	{
		Pause,
		Marche,
		Vitrine,
		Fuite
	}

	private long _fuiteA;

	private long _reessaiA;

	internal readonly Figurants.Ville Foyer;

	internal Etats Etat;

	private long _jusqua;

	private readonly List<(int X, int Y)> _chemin = new List<(int, int)>();

	private int _echecs;

	private bool _versVitrine;

	protected bool ArriveAuBout => _chemin.Count == 0;

	private static MapCritterAttribute Attribut(string sprite, int vitesse)
	{
		return new MapCritterAttribute
		{
			Sprite = sprite,
			Movement = 2,
			Speed = vitesse,
			Frequency = 1000,
			Layer = 1,
			Direction = 0,
			BlockPlayers = true,
			IgnoreNpcAvoids = false
		};
	}

	internal Figurant(Figurants.Ville foyer, string sprite, int x, int y, int vitesse = 420)
		: base(foyer.Carte, (byte)x, (byte)y, Attribut(sprite, vitesse))
	{
		Foyer = foyer;
		_jusqua = Timing.Global.MillisecondsUtc + Globals.Random.Next(500, 3500);
		base.DirectionFacing = Direction.Down;
	}

	public override bool Update()
	{
		if (!base.Update())
		{
			return false;
		}
		if (base.IsMoving)
		{
			return true;
		}
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		if (millisecondsUtc < _reessaiA)
		{
			return true;
		}
		if (Etat == Etats.Fuite)
		{
			Fuite(millisecondsUtc);
			return true;
		}
		Conduite(millisecondsUtc);
		return true;
	}

	public override float GetMovementTime()
	{
		if (Etat != Etats.Fuite)
		{
			return base.GetMovementTime();
		}
		return base.GetMovementTime() * 0.42f;
	}

	internal void Fuir(long now, List<(int X, int Y)> chemin)
	{
		Etat = Etats.Fuite;
		_fuiteA = now + 350;
		_chemin.Clear();
		_chemin.AddRange(chemin);
		_echecs = 0;
		_reessaiA = 0L;
	}

	private void Fuite(long now)
	{
		if (now < _fuiteA)
		{
			return;
		}
		if (_chemin.Count == 0)
		{
			Figurants.Cacher(Foyer, this);
			return;
		}
		var (num, num2) = _chemin[0];
		if (Pas(num - base.X, num2 - base.Y, now))
		{
			_chemin.RemoveAt(0);
			_echecs = 0;
			return;
		}
		_reessaiA = now + 120;
		if (++_echecs > 8)
		{
			_chemin.Clear();
		}
	}

	protected bool SEcarterDuJoueur(long now)
	{
		Player me = Globals.Me;
		if (me == null || me.MapId != MapId)
		{
			return false;
		}
		int num = base.X - me.X;
		int num2 = base.Y - me.Y;
		if (Math.Abs(num) + Math.Abs(num2) != 1)
		{
			return false;
		}
		var (value, value2) = CorpsACorps.Vecteur(me.DirectionFacing);
		if ((num == 0 || Math.Sign(value) != num) && (num2 == 0 || Math.Sign(value2) != num2))
		{
			return false;
		}
		int num3 = ((Globals.Random.Next(2) == 0) ? 1 : (-1));
		(int, int)[] array = ((num == 0) ? new(int, int)[3]
		{
			(num3, 0),
			(-num3, 0),
			(0, num2)
		} : new(int, int)[3]
		{
			(0, num3),
			(0, -num3),
			(num, 0)
		});
		(int, int)[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			var (dx, dy) = array2[i];
			if (Pas(dx, dy, now))
			{
				_chemin.Clear();
				_versVitrine = false;
				Etat = Etats.Pause;
				_jusqua = now + Globals.Random.Next(1500, 3000);
				return true;
			}
		}
		return false;
	}

	protected virtual void Conduite(long now)
	{
		if (SEcarterDuJoueur(now))
		{
			return;
		}
		switch (Etat)
		{
		case Etats.Pause:
		case Etats.Vitrine:
			if (now >= _jusqua)
			{
				Choisir(now);
			}
			break;
		case Etats.Marche:
			if (_chemin.Count == 0)
			{
				Etat = (_versVitrine ? Etats.Vitrine : Etats.Pause);
				_jusqua = now + (_versVitrine ? Globals.Random.Next(3000, 8000) : Globals.Random.Next(1000, 4000));
				if (_versVitrine)
				{
					base.DirectionFacing = Direction.Up;
				}
				break;
			}
			var (num, num2) = _chemin[0];
			if (Pas(num - base.X, num2 - base.Y, now))
			{
				_chemin.RemoveAt(0);
				_echecs = 0;
			}
			else if (++_echecs > 12)
			{
				_chemin.Clear();
				_echecs = 0;
			}
			else
			{
				_reessaiA = now + 250;
			}
			break;
		}
	}

	private void Choisir(long now)
	{
		Meteo.DonneesVille donnees = Foyer.Donnees;
		_versVitrine = false;
		if (Globals.Random.Next(5) == 0 && donnees.Vitres.Length != 0)
		{
			Meteo.Vitre2 vitre = donnees.Vitres[Globals.Random.Next(donnees.Vitres.Length)];
			int num = (vitre.X0 + vitre.L / 2) / Options.Instance.Map.TileWidth;
			int num2 = (vitre.Y0 + vitre.H) / Options.Instance.Map.TileHeight + 1;
			if (Figurants.Trottoir(Foyer, num, num2) && Figurants.PeutSArreter(Foyer, num, num2, this))
			{
				List<(int, int)> list = Figurants.Chemin(Foyer, base.X, base.Y, num, num2, 24);
				if (list.Count > 0)
				{
					_chemin.Clear();
					_chemin.AddRange(list);
					_versVitrine = true;
					Etat = Etats.Marche;
					return;
				}
			}
		}
		(int, int)? tuple = null;
		int num3 = int.MaxValue;
		for (int i = 0; i < 8; i++)
		{
			int x = Globals.Random.Next(donnees.TrottoirL);
			int y = Globals.Random.Next(donnees.TrottoirH);
			int num4 = Math.Abs(x - base.X) + Math.Abs(y - base.Y);
			if (num4 >= 4 && num4 <= 12 && Figurants.Trottoir(Foyer, x, y) && Figurants.PeutSArreter(Foyer, x, y, this))
			{
				int num5 = Foyer.Gens.Count((Figurant g) => g != this && Math.Abs(g.X - x) <= 3 && Math.Abs(g.Y - y) <= 3) * 3 + Globals.Random.Next(3);
				if (num5 < num3)
				{
					num3 = num5;
					tuple = (x, y);
				}
			}
		}
		if (tuple.HasValue)
		{
			(int, int) valueOrDefault = tuple.GetValueOrDefault();
			List<(int, int)> list2 = Figurants.Chemin(Foyer, base.X, base.Y, valueOrDefault.Item1, valueOrDefault.Item2);
			if (list2.Count > 0)
			{
				_chemin.Clear();
				_chemin.AddRange(list2);
				Etat = Etats.Marche;
				return;
			}
		}
		_jusqua = now + Globals.Random.Next(800, 2500);
	}

	protected bool Pas(int dx, int dy, long now)
	{
		if (Math.Abs(dx) + Math.Abs(dy) != 1)
		{
			return false;
		}
		int num = base.X + dx;
		int num2 = base.Y + dy;
		if (num < 0 || num2 < 0 || num >= Options.Instance.Map.MapWidth || num2 >= Options.Instance.Map.MapHeight)
		{
			return false;
		}
		Direction directionMoving = (base.DirectionFacing = ((dx > 0) ? Direction.Right : ((dx < 0) ? Direction.Left : ((dy > 0) ? Direction.Down : Direction.Up))));
		IEntity blockedBy = null;
		if (IsTileBlocked(new Point(num, num2), base.Z, MapId, ref blockedBy, ignoreAliveResources: true, ignoreDeadResources: true, ignoreNpcAvoids: false) != -1 || PlayerOnTile(MapId, num, num2) || Figurants.Occupee(Foyer, num, num2, this) || Figurants.Evenement(MapId, num, num2) || LigneBarreMonPas(num, num2) || !Figurants.Accessible(Foyer, num, num2))
		{
			return false;
		}
		DirectionMoving = directionMoving;
		base.IsMoving = true;
		base.OffsetX = ((dx != 0) ? ((dx > 0) ? (-Options.Instance.Map.TileWidth) : Options.Instance.Map.TileWidth) : 0);
		base.OffsetY = ((dy != 0) ? ((dy > 0) ? (-Options.Instance.Map.TileHeight) : Options.Instance.Map.TileHeight) : 0);
		base.X = (byte)num;
		base.Y = (byte)num2;
		base.MoveTimer = now + (long)GetMovementTime();
		return true;
	}

	protected void Aller(int x, int y, int maxPas = 20)
	{
		_chemin.Clear();
		_chemin.AddRange(Figurants.Chemin(Foyer, base.X, base.Y, x, y, maxPas));
		Etat = ((_chemin.Count > 0) ? Etats.Marche : Etats.Pause);
	}

	protected void Attendre(long now, int ms)
	{
		Etat = Etats.Pause;
		_jusqua = now + ms;
		_reessaiA = now + ms;
	}

	protected void SuivreLeChemin(long now)
	{
		if (_chemin.Count == 0)
		{
			return;
		}
		var (num, num2) = _chemin[0];
		if (Pas(num - base.X, num2 - base.Y, now))
		{
			_chemin.RemoveAt(0);
			return;
		}
		_reessaiA = now + 250;
		if (++_echecs > 8)
		{
			_chemin.Clear();
			_echecs = 0;
		}
	}
}
