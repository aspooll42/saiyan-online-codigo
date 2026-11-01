using System;
using System.Runtime.Serialization;

namespace Intersect.Config;

public class MaitresOptions
{
	public bool Actif { get; set; } = true;

	public Guid EblouissementSortId { get; set; } = new Guid("8A6C1E40-5D2B-4F79-9E13-7C4A2B6D0F01");

	public Guid KienzanSortId { get; set; } = new Guid("8A6C1E40-5D2B-4F79-9E13-7C4A2B6D0F02");

	public Guid MakankosappoSortId { get; set; } = new Guid("8A6C1E40-5D2B-4F79-9E13-7C4A2B6D0F03");

	public int EblouissementRayonPx { get; set; } = 96;

	public float EblouissementRegard { get; set; } = 0.3f;

	public int EblouissementAveugleMs { get; set; } = 1500;

	public int EblouissementAveuglePleinMs { get; set; } = 2500;

	public int EblouissementPleinMinimum { get; set; } = 2;

	public int EblouissementJoueurMs { get; set; } = 300;

	public int KienzanFormationMs { get; set; } = 500;

	public int KienzanDepartForceMs { get; set; } = 2000;

	public int KienzanVitessePx { get; set; } = 256;

	public int KienzanPorteePx { get; set; } = 288;

	public int KienzanLargeurPx { get; set; } = 14;

	public int KienzanArbresPx { get; set; } = 16;

	public int KienzanLancerMs { get; set; } = 280;

	public int MakankoChargeMs { get; set; } = 1500;

	public int MakankoDepartForceMs { get; set; } = 3000;

	public int MakankoPorteePx { get; set; } = 320;

	public int MakankoLargeurPx { get; set; } = 12;

	public int MakankoArbresPx { get; set; } = 12;

	public int MakankoSonnePleinMs { get; set; } = 1000;

	public int MakankoReculPx { get; set; } = 8;

	public int MakankoTenueMs { get; set; } = 650;

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		EblouissementRayonPx = Math.Clamp(EblouissementRayonPx, 16, 400);
		EblouissementRegard = (float.IsFinite(EblouissementRegard) ? Math.Clamp(EblouissementRegard, -1f, 1f) : 0.3f);
		EblouissementAveugleMs = Math.Clamp(EblouissementAveugleMs, 0, 10000);
		EblouissementAveuglePleinMs = Math.Clamp(EblouissementAveuglePleinMs, 0, 10000);
		EblouissementPleinMinimum = Math.Max(1, EblouissementPleinMinimum);
		EblouissementJoueurMs = Math.Clamp(EblouissementJoueurMs, 0, 5000);
		KienzanFormationMs = Math.Clamp(KienzanFormationMs, 0, 5000);
		KienzanDepartForceMs = Math.Max(KienzanFormationMs, KienzanDepartForceMs);
		KienzanVitessePx = Math.Clamp(KienzanVitessePx, 32, 2000);
		KienzanPorteePx = Math.Clamp(KienzanPorteePx, 32, 1000);
		KienzanLargeurPx = Math.Clamp(KienzanLargeurPx, 2, 64);
		KienzanArbresPx = Math.Clamp(KienzanArbresPx, 0, 64);
		KienzanLancerMs = Math.Clamp(KienzanLancerMs, 0, 2000);
		MakankoChargeMs = Math.Clamp(MakankoChargeMs, 0, 10000);
		MakankoDepartForceMs = Math.Max(MakankoChargeMs, MakankoDepartForceMs);
		MakankoPorteePx = Math.Clamp(MakankoPorteePx, 32, 1000);
		MakankoLargeurPx = Math.Clamp(MakankoLargeurPx, 2, 64);
		MakankoArbresPx = Math.Clamp(MakankoArbresPx, 0, 64);
		MakankoSonnePleinMs = Math.Clamp(MakankoSonnePleinMs, 0, 10000);
		MakankoReculPx = Math.Clamp(MakankoReculPx, 0, 64);
		MakankoTenueMs = Math.Clamp(MakankoTenueMs, 100, 5000);
	}

	public TechniqueDeMaitre TechniqueDe(Guid spellId)
	{
		if (!Actif || spellId == Guid.Empty)
		{
			return TechniqueDeMaitre.Aucune;
		}
		if (spellId == EblouissementSortId)
		{
			return TechniqueDeMaitre.Eblouissement;
		}
		if (spellId == KienzanSortId)
		{
			return TechniqueDeMaitre.Kienzan;
		}
		if (!(spellId == MakankosappoSortId))
		{
			return TechniqueDeMaitre.Aucune;
		}
		return TechniqueDeMaitre.Makankosappo;
	}

	public Guid SortDe(TechniqueDeMaitre technique)
	{
		return technique switch
		{
			TechniqueDeMaitre.Eblouissement => EblouissementSortId, 
			TechniqueDeMaitre.Kienzan => KienzanSortId, 
			TechniqueDeMaitre.Makankosappo => MakankosappoSortId, 
			_ => Guid.Empty, 
		};
	}

	public static bool SeTient(TechniqueDeMaitre technique)
	{
		if (technique - 2 <= TechniqueDeMaitre.Eblouissement)
		{
			return true;
		}
		return false;
	}

	public static int AngleEnCentiemes(float degres)
	{
		if (!float.IsFinite(degres))
		{
			return 0;
		}
		return (int)MathF.Round(degres * 100f);
	}

	public static float AngleDepuisCentiemes(int centiemes)
	{
		return (float)centiemes / 100f;
	}
}
