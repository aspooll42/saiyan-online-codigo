namespace Intersect.Config;

public class PacketOptions
{
	public bool BatchPlayerMovementPackets { get; set; } = true;

	public bool BatchActionMessagePackets { get; set; } = true;

	public bool BatchAnimationPackets { get; set; } = true;
}
