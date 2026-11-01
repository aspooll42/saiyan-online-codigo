namespace Intersect.Config;

public class ClashOptions
{
	public bool Actif { get; set; } = true;

	public int ParfaitMs { get; set; } = 50;

	public int BienMs { get; set; } = 120;

	public int CercleMinMs { get; set; } = 520;

	public int CercleMaxMs { get; set; } = 820;

	public int PremierCercleMs { get; set; } = 1000;

	public int BossEcartTypeMs { get; set; } = 65;

	public int BossRatePourcent { get; set; } = 15;

	public int MargeAnnonceeMs { get; set; } = 160;

	public bool CorpsACorps { get; set; } = true;

	public int CorpsACorpsEcartMs { get; set; } = 120;

	public int OuvertureMs { get; set; } = 450;

	public int PauseMs { get; set; } = 420;

	public int CoupsGagnants { get; set; } = 2;

	public int CerclesMax { get; set; } = 7;

	public int CoupFinalPourcent { get; set; } = 250;

	public int ProjectionPx { get; set; } = 64;

	public int SonneMs { get; set; } = 1300;

	public int DelaiMemePaireMs { get; set; } = 15000;

	public bool Contact { get; set; } = true;

	public int ContactMaxMs { get; set; } = 5000;

	public int PousseePourMille { get; set; } = 150;

	public int EgalitePourcent { get; set; } = 10;

	public bool GardeSelonLaForce { get; set; } = true;

	public int DevieDesPourcent { get; set; } = 150;

	public int BriseSousPourcent { get; set; } = 80;

	public int ChargePleinePourcent { get; set; } = 150;
}
