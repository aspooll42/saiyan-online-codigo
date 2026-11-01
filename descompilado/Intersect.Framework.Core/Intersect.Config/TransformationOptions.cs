using System;
using System.IO;

namespace Intersect.Config;

public class TransformationOptions
{
	public float KiCostPercent { get; set; } = 30f;

	public float KiDrainPercentPerSecond { get; set; } = 1.2f;

	public bool RegenKiWhileTransformed { get; set; }

	public int SequenceDuration { get; set; } = 2000;

	public int KaiokenSequenceDuration { get; set; } = 750;

	public int UltraSequenceDuration { get; set; } = 3200;

	public int UltraCombatSequenceDuration { get; set; } = 500;

	public static bool IsUltraSprite(string? transformSprite)
	{
		if (!string.IsNullOrEmpty(transformSprite))
		{
			if (!Path.GetFileNameWithoutExtension(transformSprite).EndsWith("_ui", StringComparison.OrdinalIgnoreCase))
			{
				return transformSprite.Contains("ultra", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
		return false;
	}

	public int SequenceDurationFor(string? transformSprite, bool inCombat = false)
	{
		if (transformSprite == null || !transformSprite.Contains("kaioken", StringComparison.OrdinalIgnoreCase))
		{
			if (!IsUltraSprite(transformSprite))
			{
				return SequenceDuration;
			}
			if (!inCombat)
			{
				return UltraSequenceDuration;
			}
			return UltraCombatSequenceDuration;
		}
		return KaiokenSequenceDuration;
	}
}
