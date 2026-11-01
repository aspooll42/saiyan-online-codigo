using System;
using System.Runtime.Serialization;

namespace Intersect.Config;

public class HeritageEtatsOptions
{
	public bool Actifs { get; set; } = true;

	public int DureeMs { get; set; } = 1500;

	public int DureeLourdMs { get; set; } = 1000;

	public int ToleranceMs { get; set; } = 150;

	public int FilMs { get; set; } = 1200;

	public int RepitMs { get; set; } = 500;

	public bool FilRemplaceResiste { get; set; } = true;

	public int LancerLongPx { get; set; } = 144;

	public int LancerLongMs { get; set; } = 400;

	public int LancerSonneAuSolMs { get; set; }

	public int CueilliSonneMs { get; set; } = 900;

	public bool CritiqueSurFlanc { get; set; } = true;

	public bool FlancSansGarde { get; set; } = true;

	public int TitubeLourdMs { get; set; } = 300;

	public bool BossCommeLourd { get; set; } = true;

	public bool DegatsApresLaFormule { get; set; } = true;

	public int FaucheRayonPx { get; set; } = 50;

	public int FaucheVictimesMax { get; set; } = 4;

	public int FaucheKiMaxPourcent { get; set; } = 16;

	public int FaucheQuartDeTourMs { get; set; } = 90;

	public int FauchePorteeMaxPx { get; set; } = 54;

	public int RangGardeMs { get; set; } = 1200;

	public bool SerieSurPlusieursCibles { get; set; } = true;

	public int GardeParfaiteFenetreMs { get; set; } = 300;

	public int GardeParfaiteRepriseMs { get; set; } = 700;

	public int RecollePx { get; set; } = 40;

	public int EcartReculMaxPx { get; set; } = 10;

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		Borner();
	}

	internal void Borner()
	{
		DureeMs = Math.Max(0, DureeMs);
		DureeLourdMs = Math.Max(0, DureeLourdMs);
		ToleranceMs = Math.Max(0, ToleranceMs);
		FilMs = Math.Max(0, FilMs);
		RepitMs = Math.Max(0, RepitMs);
		LancerLongPx = Math.Clamp(LancerLongPx, 0, 512);
		LancerLongMs = Math.Max(0, LancerLongMs);
		LancerSonneAuSolMs = Math.Max(0, LancerSonneAuSolMs);
		CueilliSonneMs = Math.Max(0, CueilliSonneMs);
		TitubeLourdMs = Math.Max(0, TitubeLourdMs);
		FaucheRayonPx = Math.Clamp(FaucheRayonPx, 0, 512);
		FaucheVictimesMax = Math.Max(0, FaucheVictimesMax);
		FaucheKiMaxPourcent = Math.Clamp(FaucheKiMaxPourcent, 0, 1000);
		FaucheQuartDeTourMs = Math.Max(0, FaucheQuartDeTourMs);
		FauchePorteeMaxPx = Math.Clamp(FauchePorteeMaxPx, 0, 512);
		RangGardeMs = Math.Max(0, RangGardeMs);
		GardeParfaiteFenetreMs = Math.Max(0, GardeParfaiteFenetreMs);
		GardeParfaiteRepriseMs = Math.Max(0, GardeParfaiteRepriseMs);
		RecollePx = Math.Clamp(RecollePx, 0, 512);
		EcartReculMaxPx = Math.Clamp(EcartReculMaxPx, 0, 512);
	}
}
