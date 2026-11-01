using System;

namespace Intersect.Config;

public class CasseOptions
{
	public bool Active { get; set; } = true;

	public string Fichier { get; set; } = "casse.json";

	public int[] Points { get; set; } = new int[10] { 0, 12, 12, 24, 24, 36, 100, 260, 260, 24 };

	public int[] RetourSecondes { get; set; } = new int[10] { 0, 60, 120, 180, 180, 120, 300, 600, 0, 180 };

	public int[] Karma { get; set; } = new int[10] { 0, -2, -1, -2, -2, -5, -20, -40, 0, -2 };

	public int DegatsBoule { get; set; } = 12;

	public int[] DegatsRayon { get; set; } = new int[3] { 7, 9, 12 };

	public int ReparationDelaiMs { get; set; } = 20000;

	public int ReparationParSeconde { get; set; } = 1;

	public int CombatMonstreMs { get; set; } = 8000;

	public int FeuDureeMs { get; set; } = 6000;

	public int FeuPorteePixels { get; set; } = 92;

	public int FeuDelaiMs { get; set; } = 2000;

	public int FeuChancePourCent { get; set; } = 75;

	public int FeuSauts { get; set; } = 3;

	public int FeuSautsRayon { get; set; } = 5;

	public Guid[] ProjectilesPercants { get; set; } = new Guid[1]
	{
		new Guid("2872EA16-0955-7330-382C-A29E9F860969")
	};

	public int FeuBordsChancePourCent { get; set; } = 30;

	public int FeuBordsSauts { get; set; } = 1;

	public int DegatsCorps { get; set; } = 8;

	public int DegatsTirMonstre { get; set; } = 6;

	public int TracesVieSecondes { get; set; } = 240;

	public int TracesParCarte { get; set; } = 40;

	public bool Percant(Guid projectileId)
	{
		if (projectileId != Guid.Empty)
		{
			return Array.IndexOf(ProjectilesPercants ?? Array.Empty<Guid>(), projectileId) >= 0;
		}
		return false;
	}
}
