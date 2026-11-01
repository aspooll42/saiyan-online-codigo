using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.General;
using Intersect.Framework.Core;

namespace Intersect.Client.Entities;

internal static class CoupsDifferes
{
	private readonly record struct Differe(long A, Guid Auteur, Action Faire);

	private static readonly List<Differe> File = new List<Differe>();

	private static readonly Dictionary<Guid, long> VisentMoi = new Dictionary<Guid, long>();

	private const long VisePendantMs = 2500L;

	private static long Maintenant => Timing.Global.Milliseconds;

	public static bool Differer(Entity? auteur, Action faire)
	{
		if (auteur == null || auteur == Globals.Me || !auteur.EstInterpoleeLibrement)
		{
			return false;
		}
		long maintenant = Maintenant;
		if (VisentMoi.TryGetValue(auteur.Id, out var value) && maintenant - value < 2500)
		{
			return false;
		}
		File.Add(new Differe(maintenant + DeplacementLibre.RetardDe(auteur), auteur.Id, faire));
		if (File.Count > 256)
		{
			ToutFaire();
		}
		return true;
	}

	public static void ViseMoi(Guid auteur)
	{
		if (auteur == Guid.Empty)
		{
			return;
		}
		VisentMoi[auteur] = Maintenant;
		if (VisentMoi.Count > 64)
		{
			long maintenant = Maintenant;
			Guid[] array = (from paire in VisentMoi
				where maintenant - paire.Value > 2500
				select paire.Key).ToArray();
			foreach (Guid key in array)
			{
				VisentMoi.Remove(key);
			}
		}
		for (int num2 = 0; num2 < File.Count; num2++)
		{
			if (!(File[num2].Auteur != auteur))
			{
				Action faire = File[num2].Faire;
				File.RemoveAt(num2);
				num2--;
				Faire(faire);
			}
		}
	}

	public static void Maj()
	{
		if (File.Count != 0)
		{
			long maintenant = Maintenant;
			while (File.Count > 0 && File[0].A <= maintenant)
			{
				Action faire = File[0].Faire;
				File.RemoveAt(0);
				Faire(faire);
			}
		}
	}

	private static void Faire(Action faire)
	{
		try
		{
			faire();
		}
		catch (Exception)
		{
		}
	}

	public static void ToutFaire()
	{
		while (File.Count > 0)
		{
			Action faire = File[0].Faire;
			File.RemoveAt(0);
			Faire(faire);
		}
	}
}
