using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Intersect.Config;

public class HeritageOptions
{
	[JsonProperty(Order = 4)]
	public HeritageAllureOptions Allure { get; set; } = new HeritageAllureOptions();

	public bool Actif { get; set; }

	public int DirectionTapeeMs { get; set; } = 300;

	public int DirectionEnMemeTempsMs { get; set; } = 50;

	public int MemoireAppuiMs { get; set; } = 300;

	public int MargeClientMs { get; set; }

	public int ArmementPendantLAppuiMs { get; set; } = 60;

	public int CompensationImpactMaxMs { get; set; } = 150;

	public int ElanSuiteMs { get; set; }

	public int EnCombatMs { get; set; } = 3000;

	public bool RegardTenu { get; set; } = true;

	public int PousseeMs { get; set; } = 100;

	public int PousseeAttenteMs { get; set; } = 250;

	public int SouleveMs { get; set; } = 280;

	public int SoulevePx { get; set; } = 8;

	public int SouleveRepitMs { get; set; } = 1500;

	public bool SouleveMalgreLaProtection { get; set; }

	public int MontantBondPx { get; set; } = 10;

	public bool Remanences { get; set; }

	public bool DevantSaCible { get; set; } = true;

	public bool CacherCoupFinal { get; set; } = true;

	public bool CacherVolDeVie { get; set; } = true;

	public int SonHasardCentiemes { get; set; } = 4;

	public int RefusSonCentiemes { get; set; } = -50;

	public bool JournalDEssai { get; set; } = true;

	public bool JournalDesAppuis { get; set; } = true;

	public CoupsHeritageOptions Coups { get; set; } = new CoupsHeritageOptions();

	[JsonProperty(Order = 2)]
	public HeritageDecorOptions Decor { get; set; } = new HeritageDecorOptions();

	[JsonProperty(Order = 3)]
	public HeritageDecorVuOptions DecorVu { get; set; } = new HeritageDecorVuOptions();

	[JsonProperty(Order = 1)]
	public HeritageEtatsOptions Etats { get; set; } = new HeritageEtatsOptions();

	[JsonProperty(Order = 7)]
	public HeritageRiposteOptions Riposte { get; set; } = new HeritageRiposteOptions();

	[JsonProperty(Order = 5)]
	public HeritageTechniquesOptions Techniques { get; set; } = new HeritageTechniquesOptions();

	[JsonProperty(Order = 6)]
	public HeritageTechniquesVuOptions TechniquesVu { get; set; } = new HeritageTechniquesVuOptions();

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		Borner();
	}

	internal void Borner()
	{
		DirectionTapeeMs = Math.Clamp(DirectionTapeeMs, 0, 600);
		DirectionEnMemeTempsMs = Math.Max(0, DirectionEnMemeTempsMs);
		MemoireAppuiMs = Math.Clamp(MemoireAppuiMs, 0, 500);
		ArmementPendantLAppuiMs = Math.Clamp(ArmementPendantLAppuiMs, 0, 60);
		CompensationImpactMaxMs = Math.Max(0, CompensationImpactMaxMs);
		ElanSuiteMs = Math.Max(0, ElanSuiteMs);
		EnCombatMs = Math.Max(0, EnCombatMs);
		PousseeMs = Math.Max(0, PousseeMs);
		PousseeAttenteMs = Math.Clamp(PousseeAttenteMs, 100, 1000);
		SouleveMs = Math.Clamp(SouleveMs, 0, 1500);
		SoulevePx = Math.Max(0, SoulevePx);
		SouleveRepitMs = Math.Max(0, SouleveRepitMs);
		MontantBondPx = Math.Clamp(MontantBondPx, 0, 16);
		SonHasardCentiemes = Math.Max(0, SonHasardCentiemes);
		if (Coups == null)
		{
			CoupsHeritageOptions coupsHeritageOptions = (Coups = new CoupsHeritageOptions());
		}
		Coups.Borner();
		if (Etats == null)
		{
			HeritageEtatsOptions heritageEtatsOptions = (Etats = new HeritageEtatsOptions());
		}
		Etats.Borner();
		if (Decor == null)
		{
			HeritageDecorOptions heritageDecorOptions = (Decor = new HeritageDecorOptions());
		}
		Decor.Borner();
		if (DecorVu == null)
		{
			HeritageDecorVuOptions heritageDecorVuOptions = (DecorVu = new HeritageDecorVuOptions());
		}
		DecorVu.Borner();
		if (Allure == null)
		{
			HeritageAllureOptions heritageAllureOptions = (Allure = new HeritageAllureOptions());
		}
		Allure.Borner();
		if (Techniques == null)
		{
			HeritageTechniquesOptions heritageTechniquesOptions = (Techniques = new HeritageTechniquesOptions());
		}
		Techniques.Borner();
		if (TechniquesVu == null)
		{
			HeritageTechniquesVuOptions heritageTechniquesVuOptions = (TechniquesVu = new HeritageTechniquesVuOptions());
		}
		TechniquesVu.Borner();
		if (Riposte == null)
		{
			HeritageRiposteOptions heritageRiposteOptions = (Riposte = new HeritageRiposteOptions());
		}
		Riposte.Borner();
	}
}
