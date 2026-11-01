using System;
using Intersect.Framework.Core.GameObjects.DailyRewards;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class DailyRewardsPacket : IntersectPacket
{
	[Key(0)]
	public DailyRewardEntry[] Rewards { get; set; } = Array.Empty<DailyRewardEntry>();

	[Key(1)]
	public int Day { get; set; }

	[Key(2)]
	public bool CanClaim { get; set; }

	[Key(3)]
	public long TotalClaimed { get; set; }

	public DailyRewardsPacket()
	{
	}

	public DailyRewardsPacket(DailyRewardEntry[] rewards, int day, bool canClaim, long totalClaimed)
	{
		Rewards = rewards;
		Day = day;
		CanClaim = canClaim;
		TotalClaimed = totalClaimed;
	}
}
