using System;
using System.Runtime.Serialization;

namespace Intersect.Config;

public class HeritageTechniquesOptions
{
	public bool Lien { get; set; }

	public int LienMemoireMs { get; set; } = 500;

	public int LienToleranceMs { get; set; } = 300;

	public int LienDepartMs { get; set; } = 90;

	public bool Moments { get; set; } = true;

	public int MomentAcquisMs { get; set; } = 2000;

	public int PleinDegatsPourcent { get; set; } = 150;

	public bool Refaites { get; set; } = true;

	public string BouleSortId { get; set; } = "5c156935-f40c-1d33-a756-defd697f1f88";

	public int BoulePousseePx { get; set; } = 32;

	public int BoulePousseeMs { get; set; } = 200;

	public int BouleSonneMs { get; set; } = 400;

	public int BoulePleinePx { get; set; } = 96;

	public int BoulePleineMs { get; set; } = 240;

	public int BouleClouePlusMs { get; set; } = 300;

	public int BoulePousseeLourdPx { get; set; }

	public int KiaiPousseePx { get; set; } = 70;

	public int KiaiPousseeMs { get; set; } = 300;

	public int KiaiPleinPx { get; set; } = 140;

	public int OndeSonnePleinMs { get; set; } = 900;

	public int RayonPalier2Ms { get; set; } = 600;

	public int RayonPalier3Ms { get; set; } = 1200;

	public int RayonDepartForceMs { get; set; } = 2000;

	public int RayonRechargeMs { get; set; } = 16000;

	public bool RayonXAnnule { get; set; } = true;

	public bool RayonPartSurCoupRecu { get; set; } = true;

	public bool RayonPorte { get; set; } = true;

	public int RayonPorteParTouchePx { get; set; } = 32;

	public int RayonReculPx { get; set; } = 24;

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		Borner();
	}

	internal void Borner()
	{
		LienMemoireMs = Math.Max(0, LienMemoireMs);
		LienToleranceMs = Math.Max(0, LienToleranceMs);
		LienDepartMs = Math.Max(0, LienDepartMs);
		MomentAcquisMs = Math.Max(0, MomentAcquisMs);
		PleinDegatsPourcent = Math.Clamp(PleinDegatsPourcent, 0, 1000);
		if (BouleSortId == null)
		{
			string text = (BouleSortId = string.Empty);
		}
		BoulePousseePx = Math.Clamp(BoulePousseePx, 0, 512);
		BoulePousseeMs = Math.Max(0, BoulePousseeMs);
		BouleSonneMs = Math.Max(0, BouleSonneMs);
		BoulePleinePx = Math.Clamp(BoulePleinePx, 0, 512);
		BoulePleineMs = Math.Max(0, BoulePleineMs);
		BouleClouePlusMs = Math.Max(0, BouleClouePlusMs);
		BoulePousseeLourdPx = Math.Clamp(BoulePousseeLourdPx, 0, 512);
		KiaiPousseePx = Math.Clamp(KiaiPousseePx, 0, 512);
		KiaiPousseeMs = Math.Max(0, KiaiPousseeMs);
		KiaiPleinPx = Math.Clamp(KiaiPleinPx, 0, 512);
		OndeSonnePleinMs = Math.Max(0, OndeSonnePleinMs);
		RayonPalier2Ms = Math.Max(0, RayonPalier2Ms);
		RayonPalier3Ms = Math.Max(RayonPalier2Ms, RayonPalier3Ms);
		RayonDepartForceMs = Math.Max(0, RayonDepartForceMs);
		RayonRechargeMs = Math.Max(0, RayonRechargeMs);
		RayonPorteParTouchePx = Math.Clamp(RayonPorteParTouchePx, 0, 512);
		RayonReculPx = Math.Clamp(RayonReculPx, 0, 512);
	}
}
