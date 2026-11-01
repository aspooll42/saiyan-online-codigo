namespace Intersect.Config;

public class LootOptions
{
	public int ItemDespawnTime { get; set; } = 15000;

	public int ItemOwnershipTime { get; set; } = 5000;

	public bool ShowUnownedItems { get; set; }

	public bool ConsolidateMapDrops { get; set; } = true;

	public bool EnableLootWindow { get; set; } = true;

	public byte MaximumLootWindowItems { get; set; } = 10;

	public byte MaximumLootWindowDistance { get; set; } = 3;

	public bool IndividualizedLootAutoIncludePartyMembers { get; set; }

	public int IndividualizedLootPartyRange { get; set; } = 10;
}
