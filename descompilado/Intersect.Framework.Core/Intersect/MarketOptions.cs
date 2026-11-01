namespace Intersect;

public class MarketOptions
{
	public bool Enabled { get; set; } = true;

	public string CurrencyItemName { get; set; } = "Zeni";

	public int SaleTaxPercent { get; set; } = 5;

	public int ListingDurationHours { get; set; } = 72;

	public int MaxListingsPerPlayer { get; set; } = 20;

	public int PageSize { get; set; } = 8;

	public int AccessTiles { get; set; } = 3;

	public int AnnounceMinRarity { get; set; } = 4;

	public long AnnounceMinPrice { get; set; } = 25000L;

	public int AnnounceCooldownSeconds { get; set; } = 60;
}
