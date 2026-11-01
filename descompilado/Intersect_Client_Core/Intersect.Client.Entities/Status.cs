using System;
using Intersect.Client.Framework.Entities;
using Intersect.Enums;
using Intersect.Framework.Core;

namespace Intersect.Client.Entities;

public class Status : IStatus
{
	public string Data { get; set; } = string.Empty;

	public long[] Shield { get; set; } = new long[Enum.GetValues<Vital>().Length];

	public Guid SpellId { get; set; }

	public long TimeRecevied { get; set; }

	public long TimeRemaining { get; set; }

	public long TotalDuration { get; set; } = 1L;

	public SpellEffect Type { get; set; }

	public int Sequence { get; set; }

	public bool IsActive => RemainingMs > 0;

	public long RemainingMs => TimeRemaining - (Timing.Global.Milliseconds - TimeRecevied);

	public Status(Guid spellId, SpellEffect type, string data, long timeRemaining, long totalDuration)
	{
		SpellId = spellId;
		Type = type;
		Data = data;
		TimeRemaining = timeRemaining;
		TotalDuration = totalDuration;
		TimeRecevied = Timing.Global.Milliseconds;
	}
}
