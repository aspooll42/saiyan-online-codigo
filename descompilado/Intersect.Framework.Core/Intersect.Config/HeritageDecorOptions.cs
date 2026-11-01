using System;
using System.Runtime.Serialization;

namespace Intersect.Config;

public class HeritageDecorOptions
{
	public bool Actif { get; set; } = true;

	public bool PremierContact { get; set; } = true;

	public bool CorpsArretent { get; set; } = true;

	public int RedressementDegres { get; set; } = 20;

	public int PartCorps { get; set; } = 25;

	public int PartSol { get; set; } = 25;

	public int PartPetit { get; set; } = 25;

	public int PartArbre { get; set; } = 50;

	public int PartRocher { get; set; } = 100;

	public int PartFalaise { get; set; } = 150;

	public int PartGlobalePourcent { get; set; } = 100;

	public int ResteMinPx { get; set; } = 4;

	public int EmpreintePx { get; set; } = 48;

	public int PlanteMs { get; set; } = 1600;

	public int CorpsGlissadePx { get; set; } = 48;

	public int CorpsSonneMs { get; set; } = 500;

	public bool CasseAuLancer { get; set; } = true;

	public bool MarquesAuLancer { get; set; } = true;

	public int VolPlancherMs { get; set; } = 90;

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		Borner();
	}

	internal void Borner()
	{
		RedressementDegres = Math.Max(0, RedressementDegres);
		PartCorps = Math.Clamp(PartCorps, 0, 1000);
		PartSol = Math.Clamp(PartSol, 0, 1000);
		PartPetit = Math.Clamp(PartPetit, 0, 1000);
		PartArbre = Math.Clamp(PartArbre, 0, 1000);
		PartRocher = Math.Clamp(PartRocher, 0, 1000);
		PartFalaise = Math.Clamp(PartFalaise, 0, 1000);
		PartGlobalePourcent = Math.Clamp(PartGlobalePourcent, 0, 1000);
		ResteMinPx = Math.Clamp(ResteMinPx, 0, 512);
		EmpreintePx = Math.Clamp(EmpreintePx, 0, 512);
		PlanteMs = Math.Max(0, PlanteMs);
		CorpsGlissadePx = Math.Clamp(CorpsGlissadePx, 0, 512);
		CorpsSonneMs = Math.Max(0, CorpsSonneMs);
		VolPlancherMs = Math.Max(0, VolPlancherMs);
	}
}
