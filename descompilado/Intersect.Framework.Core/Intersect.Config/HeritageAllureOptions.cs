using System;
using System.Runtime.Serialization;

namespace Intersect.Config;

public class HeritageAllureOptions
{
	public bool Actif { get; set; } = true;

	public int BlancMs { get; set; } = 90;

	public int EcraseMs { get; set; } = 110;

	public int EcrasePourcent { get; set; } = 10;

	public int Grains1 { get; set; } = 2;

	public int Grains2 { get; set; } = 3;

	public int Grains3 { get; set; } = 5;

	public bool SansZoom { get; set; } = true;

	public int TalonsVieMs { get; set; } = 4000;

	public int SillonVieMs { get; set; } = 6000;

	public int ArcVieMs { get; set; } = 5000;

	public bool Fantomes { get; set; } = true;

	public int LiserePx { get; set; } = 2;

	public int LiserePeriodeMs { get; set; } = 360;

	public int PencheRepoussePx { get; set; } = 6;

	public bool Chiffres { get; set; } = true;

	public int ChiffreCoupPt { get; set; } = 13;

	public int ChiffrePiedPt { get; set; } = 13;

	public int ChiffreFauchePt { get; set; } = 10;

	public int ChiffreCritiquePt { get; set; } = 17;

	public int ChiffreArbrePt { get; set; } = 16;

	public int ChiffreRocherPt { get; set; } = 19;

	public int ChiffreFalaisePt { get; set; } = 23;

	public int ChiffreVieMs { get; set; } = 700;

	public int ChiffreDecorVieMs { get; set; } = 1200;

	public bool MotsCaches { get; set; } = true;

	public int SouleveMonteeMs { get; set; } = 100;

	public int SouleveRetombeeMs { get; set; } = 140;

	public int SonCritiqueCentiemes { get; set; } = -38;

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		Borner();
	}

	internal void Borner()
	{
		BlancMs = Math.Max(0, BlancMs);
		EcraseMs = Math.Max(0, EcraseMs);
		EcrasePourcent = Math.Clamp(EcrasePourcent, 0, 1000);
		Grains1 = Math.Max(0, Grains1);
		Grains2 = Math.Max(0, Grains2);
		Grains3 = Math.Max(0, Grains3);
		TalonsVieMs = Math.Max(0, TalonsVieMs);
		SillonVieMs = Math.Max(0, SillonVieMs);
		ArcVieMs = Math.Max(0, ArcVieMs);
		LiserePx = Math.Clamp(LiserePx, 0, 512);
		LiserePeriodeMs = Math.Max(0, LiserePeriodeMs);
		PencheRepoussePx = Math.Clamp(PencheRepoussePx, 0, 512);
		ChiffreCoupPt = Math.Max(0, ChiffreCoupPt);
		ChiffrePiedPt = Math.Max(0, ChiffrePiedPt);
		ChiffreFauchePt = Math.Max(0, ChiffreFauchePt);
		ChiffreCritiquePt = Math.Max(0, ChiffreCritiquePt);
		ChiffreArbrePt = Math.Max(0, ChiffreArbrePt);
		ChiffreRocherPt = Math.Max(0, ChiffreRocherPt);
		ChiffreFalaisePt = Math.Max(0, ChiffreFalaisePt);
		ChiffreVieMs = Math.Max(0, ChiffreVieMs);
		ChiffreDecorVieMs = Math.Max(0, ChiffreDecorVieMs);
		SouleveMonteeMs = Math.Max(0, SouleveMonteeMs);
		SouleveRetombeeMs = Math.Max(0, SouleveRetombeeMs);
	}
}
