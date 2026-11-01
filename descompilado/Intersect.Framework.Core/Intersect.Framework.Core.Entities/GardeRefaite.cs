using System;
using Intersect.Config;
using Intersect.Network.Packets.Server;

namespace Intersect.Framework.Core.Entities;

public static class GardeRefaite
{
	public const int GlissadeLegereMs = 120;

	public const int GlissadeLourdeMs = 180;

	public const int GlissadeBouleMs = 150;

	public static GenreDeCoupGarde Genre(GenreDeCoupGarde enCours, bool coupFort)
	{
		if (enCours - 3 > GenreDeCoupGarde.Leger)
		{
			if (!coupFort)
			{
				return GenreDeCoupGarde.Leger;
			}
			return GenreDeCoupGarde.Lourd;
		}
		return enCours;
	}

	public static bool CoupFort(bool coupDePied, bool finalDeCombo, bool attaquantLourd, bool attaquantBoss)
	{
		return coupDePied || finalDeCombo || attaquantLourd || attaquantBoss;
	}

	public static bool Immobile(GardeOptions? garde, bool gardeActive, bool enGarde)
	{
		return (garde?.Immobile ?? false) && gardeActive && enGarde;
	}

	public static (float Px, int Ms) Glissade(GardeOptions garde, GuardEvent evenement, GenreDeCoupGarde genre)
	{
		if (genre == GenreDeCoupGarde.Aucun)
		{
			return (Px: 0f, Ms: 0);
		}
		var (num, item) = evenement switch
		{
			GuardEvent.Break => (garde.GlissadeLourdePx, 180), 
			GuardEvent.Blocked => genre switch
			{
				GenreDeCoupGarde.Leger => (garde.GlissadeLegerePx, 120), 
				GenreDeCoupGarde.Lourd => (garde.GlissadeLourdePx, 180), 
				GenreDeCoupGarde.Boule => (garde.GlissadeBoulePx, 150), 
				_ => (0, 0), 
			}, 
			_ => (0, 0), 
		};
		if (num <= 0)
		{
			return (Px: 0f, Ms: 0);
		}
		return (Px: num, Ms: item);
	}

	public static bool RayonPousseLaGarde(GardeOptions garde)
	{
		if (garde.GlissadeRayonPxParSeconde > 0)
		{
			return garde.GlissadeRayonMaxPx > 0;
		}
		return false;
	}

	public static float GlissadeDuRayon(GardeOptions garde, float dejaPx, int intervalleMs)
	{
		if (!RayonPousseLaGarde(garde) || intervalleMs <= 0 || !float.IsFinite(dejaPx))
		{
			return 0f;
		}
		float val = (float)(garde.GlissadeRayonPxParSeconde * intervalleMs) / 1000f;
		return Math.Clamp(Math.Min(val, (float)garde.GlissadeRayonMaxPx - dejaPx), 0f, garde.GlissadeRayonMaxPx);
	}

	public static (int X, int Y) Axe(float versX, float versY)
	{
		if (!float.IsFinite(versX) || !float.IsFinite(versY) || (versX == 0f && versY == 0f))
		{
			return (X: 0, Y: 0);
		}
		if (!(MathF.Abs(versX) >= MathF.Abs(versY)))
		{
			return (X: 0, Y: MathF.Sign(versY));
		}
		return (X: MathF.Sign(versX), Y: 0);
	}

	public static (string Fichier, float Volume, bool Boucle) Son(GardeOptions garde, GuardEvent evenement, GenreDeCoupGarde genre)
	{
		if (genre == GenreDeCoupGarde.Aucun)
		{
			return (Fichier: string.Empty, Volume: 0f, Boucle: false);
		}
		var (text, num, item) = evenement switch
		{
			GuardEvent.Perfect => (garde.SonParade, garde.VolumeParade, false), 
			GuardEvent.Break => (garde.SonBrisee, garde.VolumeBrisee, false), 
			GuardEvent.Blocked => genre switch
			{
				GenreDeCoupGarde.Boule => (garde.SonBoule, garde.VolumeBoule, false), 
				GenreDeCoupGarde.Rayon => (garde.SonRayon, garde.VolumeRayon, true), 
				_ => (garde.SonCoup, garde.VolumeCoup, false), 
			}, 
			_ => (string.Empty, 0, false), 
		};
		if (!string.IsNullOrWhiteSpace(text) && num > 0)
		{
			return (Fichier: text, Volume: (float)Math.Clamp(num, 0, 100) / 100f, Boucle: item);
		}
		return (Fichier: string.Empty, Volume: 0f, Boucle: false);
	}

	public static float VolumePourCrete(double creteDb, double viseeDb = -15.0)
	{
		return (float)Math.Min(1.0, Math.Pow(10.0, (viseeDb - creteDb) / 20.0));
	}
}
