using System;
using Newtonsoft.Json;

namespace Intersect.Config;

public class NpcOptions
{
	public bool AllowResetRadius { get; set; }

	public int ResetRadius { get; set; } = 8;

	public bool AllowNewResetLocationBeforeFinish { get; set; }

	[JsonProperty]
	[Obsolete("Use ResetVitalsAndStatuses instead, the ResetVitalsAndStatusses property will be removed in 0.9-beta", true)]
	private bool ResetVitalsAndStatusses
	{
		set
		{
			ResetVitalsAndStatuses = value;
		}
	}

	public bool ResetVitalsAndStatuses { get; set; }

	public bool ContinuouslyResetVitalsAndStatuses { get; set; }

	public bool AllowEngagingWhileResetting { get; set; }

	public bool ShowLevelByName { get; set; }

	public bool IntangibleDuringReset { get; set; } = true;

	public bool ResetIfCombatTimerExceeded { get; set; } = true;
}
