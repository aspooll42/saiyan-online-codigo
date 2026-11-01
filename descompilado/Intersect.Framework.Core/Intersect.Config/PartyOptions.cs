namespace Intersect.Config;

public class PartyOptions
{
	public int MaximumMembers { get; set; } = 4;

	public int SharedXpRange { get; set; } = 40;

	public int NpcDeathCommonEventStartRange { get; set; }

	public float BonusExperiencePercentPerMember { get; set; } = 35f;
}
