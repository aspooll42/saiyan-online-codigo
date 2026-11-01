namespace Intersect.Config;

public class PassifsOptions
{
	public bool Actif { get; set; } = true;

	public int CoeurPurPalierMs { get; set; } = 4000;

	public int CoeurPurPourcentParPalier { get; set; } = 1;

	public int CoeurPurPourcentMax { get; set; } = 6;

	public int OrgueilDegatsPourcent { get; set; } = 8;

	public int OrgueilPvPourcent { get; set; } = 8;

	public int ColereCoups { get; set; } = 30;

	public int ColereDegatsPourcent { get; set; } = 15;

	public int ColereMs { get; set; } = 6000;

	public int HeritierKiPourcent { get; set; } = 20;

	public int InsolentRechargePourcent { get; set; } = 25;

	public int InsolentDegatsPourcent { get; set; } = 10;

	public int EpeeAllongePx { get; set; } = 8;

	public int CadetDegatsPourcent { get; set; } = 15;

	public int CadetOubliMs { get; set; } = 30000;
}
