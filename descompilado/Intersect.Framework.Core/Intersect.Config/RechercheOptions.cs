namespace Intersect.Config;

public class RechercheOptions
{
	public bool Active { get; set; } = true;

	public int[] Crans { get; set; } = new int[3] { 25, 50, 80 };

	public int PointsCoup { get; set; } = 3;

	public int PointsMur { get; set; } = 6;

	public int PointsRayonMur { get; set; } = 5;

	public int PointsObjet { get; set; } = 8;

	public int PointsArbre { get; set; } = 8;

	public int PointsFeu { get; set; } = 2;

	public int PointsBatiment { get; set; } = 40;

	public int PointsPrisSurLeFait { get; set; } = 25;

	public float FacteurVille { get; set; } = 1f;

	public float FacteurVillage { get; set; } = 0.4f;

	public int TemoinsPorteeCases { get; set; } = 8;

	public int PoliceVuePixels { get; set; } = 480;

	public int PoliceOuiePixels { get; set; } = 800;

	public int PrisSurLeFaitPixels { get; set; } = 320;

	public int PrisSurLeFaitSecondes { get; set; } = 20;

	public int BaisseApresSecondes { get; set; } = 6;

	public float BaisseParSeconde { get; set; } = 3f;

	public int PoliceVilleMs { get; set; } = 1500;

	public int PoliceVillageMs { get; set; } = 12000;

	public long Amende { get; set; } = 500L;

	public int PoliciersPrison { get; set; } = 3;

	public int AvertissementMs { get; set; } = 3500;

	public int PoursuiteAbandonSecondes { get; set; } = 4;
}
