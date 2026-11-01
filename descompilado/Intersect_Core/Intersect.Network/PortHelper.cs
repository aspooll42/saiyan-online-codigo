namespace Intersect.Network;

public static class PortHelper
{
	public static bool IsValidPort(ulong port)
	{
		if (0 < port)
		{
			return port <= 65535;
		}
		return false;
	}
}
