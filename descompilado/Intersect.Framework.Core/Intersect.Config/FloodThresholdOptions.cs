namespace Intersect.Config;

public class FloodThresholdOptions
{
	public int MaxPacketSize { get; set; } = 10240;

	public int MaxPacketPerSec { get; set; } = 50;

	public int KickAvgPacketPerSec { get; set; } = 30;

	public static FloodThresholdOptions Editor()
	{
		return new FloodThresholdOptions
		{
			MaxPacketSize = int.MaxValue,
			MaxPacketPerSec = int.MaxValue,
			KickAvgPacketPerSec = int.MaxValue
		};
	}

	public static FloodThresholdOptions NotLoggedIn()
	{
		return new FloodThresholdOptions
		{
			MaxPacketSize = 10240,
			MaxPacketPerSec = 5,
			KickAvgPacketPerSec = 3
		};
	}
}
