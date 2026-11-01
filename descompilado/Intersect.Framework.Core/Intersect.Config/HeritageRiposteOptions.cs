using System;
using Newtonsoft.Json;

namespace Intersect.Config;

public class HeritageRiposteOptions
{
	[JsonProperty(Order = 1)]
	public int AvertissementPoints { get; set; } = 24;

	[JsonProperty(Order = 2)]
	public int ContourPx { get; set; } = 2;

	[JsonProperty(Order = 3)]
	public bool ContourTenu { get; set; } = true;

	[JsonProperty(Order = 4)]
	public string Son { get; set; } = "dialogue_tic.wav";

	[JsonProperty(Order = 5)]
	public int VolumeDuSon { get; set; } = 100;

	public bool Actif { get; set; } = true;

	public int CoupsAvant { get; set; } = 3;

	public int TenaceMs { get; set; } = 1200;

	public int ArmeMs { get; set; } = 450;

	internal void BornerLAlerte()
	{
		AvertissementPoints = Math.Clamp(AvertissementPoints, 0, 32);
		ContourPx = Math.Clamp(ContourPx, 0, 4);
		if (Son == null)
		{
			string text = (Son = string.Empty);
		}
		VolumeDuSon = Math.Clamp(VolumeDuSon, 0, 100);
	}

	internal void Borner()
	{
		CoupsAvant = Math.Clamp(CoupsAvant, 0, 10);
		TenaceMs = Math.Clamp(TenaceMs, 0, 10000);
		ArmeMs = Math.Clamp(ArmeMs, 0, 3000);
		BornerLAlerte();
	}
}
