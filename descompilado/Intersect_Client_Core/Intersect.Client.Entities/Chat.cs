using System;
using System.Linq;
using Intersect.Client.General;
using Intersect.Enums;

namespace Intersect.Client.Entities;

public sealed class Chat : Figurant
{
	internal readonly (int X, int Y) Repos;

	private int _phase;

	internal Chat(Figurants.Ville foyer, int x, int y)
		: base(foyer, "chat_ville.png", x, y, 300)
	{
		Repos = (X: x, Y: y);
		base.DirectionFacing = Direction.Down;
	}

	protected override void Conduite(long now)
	{
		bool flag = Globals.Entities.Values.Any((Entity e) => e is Player && e.MapId == MapId && Math.Abs(e.X - base.X) <= 1 && Math.Abs(e.Y - base.Y) <= 1) || Foyer.Gens.Any((Figurant g) => Math.Abs(g.X - base.X) <= 1 && Math.Abs(g.Y - base.Y) <= 1);
		switch (_phase)
		{
		case 0:
			if (flag)
			{
				for (int num = 0; num < 10; num++)
				{
					int num2 = Repos.X + Globals.Random.Next(-4, 5);
					int num3 = Repos.Y + Globals.Random.Next(-3, 4);
					if (Math.Abs(num2 - base.X) + Math.Abs(num3 - base.Y) >= 2 && Figurants.Trottoir(Foyer, num2, num3) && Figurants.PeutSArreter(Foyer, num2, num3, this))
					{
						Aller(num2, num3, 10);
						if (Etat == Etats.Marche)
						{
							_phase = 1;
							return;
						}
					}
				}
			}
			Attendre(now, 300);
			break;
		case 1:
			if (base.ArriveAuBout)
			{
				_phase = 2;
				Attendre(now, Globals.Random.Next(4000, 9000));
			}
			else
			{
				SuivreLeChemin(now);
			}
			break;
		case 2:
			if (Etat == Etats.Pause && !flag)
			{
				Aller(Repos.X, Repos.Y, 12);
				_phase = ((Etat == Etats.Marche) ? 3 : 0);
			}
			else
			{
				Attendre(now, 300);
			}
			break;
		case 3:
			if (base.ArriveAuBout)
			{
				_phase = 0;
				base.DirectionFacing = Direction.Down;
				Attendre(now, 300);
			}
			else
			{
				SuivreLeChemin(now);
			}
			break;
		}
	}
}
