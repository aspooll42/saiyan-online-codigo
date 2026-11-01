using System;
using System.Runtime.Serialization;

namespace Intersect.Config;

public class HeritageTechniquesVuOptions
{
	public bool Actif { get; set; } = true;

	public bool BouleTrainee { get; set; } = true;

	public int BouleAttenteMs { get; set; } = 150;

	public int BouleEclatEchelle { get; set; } = 1;

	public int BouleArretMs { get; set; } = 80;

	public int BouleSecousseDixiemes { get; set; } = 40;

	public int BouleSecousseMs { get; set; } = 220;

	public int ChiffreBoulePt { get; set; } = 16;

	public int KiaiFrontPx { get; set; } = 130;

	public int KiaiFrontMs { get; set; } = 300;

	public int KiaiFinMs { get; set; } = 150;

	public int KiaiArretMs { get; set; } = 60;

	public int KiaiSecousseDixiemes { get; set; } = 40;

	public int KiaiSecousseMs { get; set; } = 240;

	public int ChiffreKiaiPt { get; set; } = 13;

	public int RayonEpaisseur1Px { get; set; } = 10;

	public int RayonEpaisseur2Px { get; set; } = 15;

	public int RayonEpaisseur3Px { get; set; } = 18;

	public int RayonCerneDixiemes { get; set; } = 15;

	public int RayonArretTirMs { get; set; } = 60;

	public int RayonArretChocMs { get; set; } = 130;

	public int RayonSecousseDixiemes { get; set; } = 60;

	public int RayonSecousseMs { get; set; } = 320;

	public bool ChargeCouronne { get; set; } = true;

	public int ClouePeriodeMs { get; set; } = 90;

	public int LezardesPx { get; set; } = 43;

	public int LezardesNombre { get; set; } = 8;

	public bool RayonUnSeulChiffre { get; set; } = true;

	public int ChiffreRayonPt { get; set; } = 29;

	public int ChiffreRayonVieMs { get; set; } = 1400;

	public bool OndeRefaite { get; set; } = true;

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		Borner();
	}

	internal void Borner()
	{
		BouleAttenteMs = Math.Max(0, BouleAttenteMs);
		BouleEclatEchelle = Math.Clamp(BouleEclatEchelle, 1, 4);
		BouleArretMs = Math.Max(0, BouleArretMs);
		BouleSecousseDixiemes = Math.Max(0, BouleSecousseDixiemes);
		BouleSecousseMs = Math.Max(0, BouleSecousseMs);
		ChiffreBoulePt = Math.Max(0, ChiffreBoulePt);
		KiaiFrontPx = Math.Clamp(KiaiFrontPx, 0, 512);
		KiaiFrontMs = Math.Max(0, KiaiFrontMs);
		KiaiFinMs = Math.Max(0, KiaiFinMs);
		KiaiArretMs = Math.Max(0, KiaiArretMs);
		KiaiSecousseDixiemes = Math.Max(0, KiaiSecousseDixiemes);
		KiaiSecousseMs = Math.Max(0, KiaiSecousseMs);
		ChiffreKiaiPt = Math.Max(0, ChiffreKiaiPt);
		RayonEpaisseur1Px = Math.Clamp(RayonEpaisseur1Px, 0, 512);
		RayonEpaisseur2Px = Math.Clamp(RayonEpaisseur2Px, 0, 512);
		RayonEpaisseur3Px = Math.Clamp(RayonEpaisseur3Px, 0, 512);
		RayonCerneDixiemes = Math.Max(0, RayonCerneDixiemes);
		RayonArretTirMs = Math.Max(0, RayonArretTirMs);
		RayonArretChocMs = Math.Max(0, RayonArretChocMs);
		RayonSecousseDixiemes = Math.Max(0, RayonSecousseDixiemes);
		RayonSecousseMs = Math.Max(0, RayonSecousseMs);
		ClouePeriodeMs = Math.Max(0, ClouePeriodeMs);
		LezardesPx = Math.Clamp(LezardesPx, 0, 512);
		LezardesNombre = Math.Max(0, LezardesNombre);
		ChiffreRayonPt = Math.Max(0, ChiffreRayonPt);
		ChiffreRayonVieMs = Math.Max(0, ChiffreRayonVieMs);
	}
}
