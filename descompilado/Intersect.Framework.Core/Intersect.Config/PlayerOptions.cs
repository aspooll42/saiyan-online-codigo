namespace Intersect.Config;

public class PlayerOptions
{
	private const int DefaultInitialBankSlots = 100;

	public const int DefaultHotbarSlotCount = 10;

	public bool AllowCombatMovement { get; set; } = true;

	public ushort AutoTurnToTargetDelay { get; set; } = 500;

	public bool AutoTurnToTargetIgnoresEntitiesBehind { get; set; }

	public bool EnableAutoTurnToTarget { get; set; } = true;

	public bool EnableFriendLoginNotifications { get; set; } = true;

	public bool ExpLossFromCurrentExp { get; set; } = true;

	public int ExpLossOnDeathPercent { get; set; }

	public int HotbarSlotCount { get; set; } = 10;

	public int InitialBankslots { get; set; } = 100;

	public int ItemDropChance { get; set; }

	public int MaxCharacters { get; set; } = 1;

	public int MaxInventory { get; set; } = 35;

	public int MaxLevel { get; set; } = 100;

	public int MaxSpells { get; set; } = 35;

	public int MaxStat { get; set; } = 255;

	public int ZenisParNiveauPourReprendreLesPoints { get; set; } = 100;

	public int RequestTimeout { get; set; } = 300000;

	public bool ShowLevelByName { get; set; }

	public bool SkipCharacterSelect { get; set; }

	public bool EnableSprinting { get; set; } = true;

	public float SprintSpeedBoost { get; set; } = 0.4f;

	public float SprintKiCost { get; set; } = 0.6f;

	public bool EnableKiCharge { get; set; } = true;

	public float KiChargeRate { get; set; } = 4f;

	public int KiChargeInterval { get; set; } = 250;

	public float KiRegenPercent { get; set; } = 2f;

	public string DragonBallVariablePrefix { get; set; } = "DragonBall";

	public int DragonBallCount { get; set; } = 7;

	public int TradeRange { get; set; } = 6;
}
