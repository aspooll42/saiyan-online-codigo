using System;
using Intersect.Enums;

namespace Intersect.Client.Entities;

public sealed class Chien : Figurant
{
	internal readonly Figurant Maitre;

	internal bool SansMaitre
	{
		get
		{
			if (!Maitre.IsDisposed)
			{
				return !Foyer.Gens.Contains(Maitre);
			}
			return true;
		}
	}

	internal Chien(Figurants.Ville foyer, Figurant maitre)
		: base(foyer, "chien_ville.png", maitre.X, Math.Min(maitre.Y + 1, Options.Instance.Map.MapHeight - 1), 330)
	{
		Maitre = maitre;
	}

	protected override void Conduite(long now)
	{
		if (SEcarterDuJoueur(now))
		{
			return;
		}
		if (SansMaitre)
		{
			Attendre(now, 1000);
			return;
		}
		int x = Maitre.X;
		int y = Maitre.Y;
		(int, int) tuple = Maitre.DirectionFacing switch
		{
			Direction.Up => (x, y + 1), 
			Direction.Down => (x, y - 1), 
			Direction.Left => (x + 1, y), 
			_ => (x - 1, y), 
		};
		int item = tuple.Item1;
		int item2 = tuple.Item2;
		int num = Math.Abs(item - base.X) + Math.Abs(item2 - base.Y);
		if (num == 0 || (num == 1 && !Maitre.IsMoving))
		{
			base.DirectionFacing = (Maitre.IsMoving ? Maitre.DirectionFacing : Maitre.DirectionFacing);
			Attendre(now, 200);
			return;
		}
		if (Etat != Etats.Marche || base.ArriveAuBout)
		{
			Aller(item, item2, 12);
			if (Etat != Etats.Marche)
			{
				Attendre(now, 400);
				return;
			}
		}
		SuivreLeChemin(now);
	}
}
