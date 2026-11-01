using System;

namespace Intersect.Config;

public class FlightOptions
{
	public Guid CloudSpellId { get; set; } = new Guid("7C1D5A3E-4F21-4B6D-9A83-5E1C0D2E3F01");

	public Guid KiFlightSpellId { get; set; } = new Guid("7C1D5A3E-4F21-4B6D-9A83-5E1C0D2E3F02");

	public float CloudSpeedBoost { get; set; } = 0.45f;

	public float CloudSprintSpeedBoost { get; set; } = 0.58f;

	public float KiSpeedBoost { get; set; } = 0.55f;

	public float KiTakeoffPercent { get; set; } = 5f;

	public float KiDrainPercentPerSecond { get; set; } = 1.5f;

	public float KiSprintSpeedBoost { get; set; } = 0.68f;

	public float KiSprintDrainPercentPerSecond { get; set; } = 4f;

	public int FallLockMilliseconds { get; set; } = 3000;

	public int FallDownMilliseconds { get; set; } = 900;

	public int DiveRange { get; set; } = 6;

	public bool CloudRefusesCriminals { get; set; } = true;
}
