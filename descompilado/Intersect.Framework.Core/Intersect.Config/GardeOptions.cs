namespace Intersect.Config;

public class GardeOptions
{
	public bool Immobile { get; set; } = true;

	public int GlissadeLegerePx { get; set; } = 6;

	public int GlissadeLourdePx { get; set; } = 14;

	public int GlissadeBoulePx { get; set; } = 10;

	public int GlissadeRayonPxParSeconde { get; set; } = 40;

	public int GlissadeRayonMaxPx { get; set; } = 48;

	public string SonCoup { get; set; } = "miss.wav";

	public int VolumeCoup { get; set; } = 18;

	public string SonBoule { get; set; } = "kiblast.wav";

	public int VolumeBoule { get; set; } = 22;

	public string SonRayon { get; set; } = "Aura_boucle.wav";

	public int VolumeRayon { get; set; } = 36;

	public string SonParade { get; set; } = "eclair.wav";

	public int VolumeParade { get; set; } = 22;

	public string SonBrisee { get; set; } = "heritage_rocher_1.wav";

	public int VolumeBrisee { get; set; } = 18;
}
