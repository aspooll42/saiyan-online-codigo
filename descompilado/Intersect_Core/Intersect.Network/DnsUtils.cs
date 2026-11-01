using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Intersect.Network;

public static class DnsUtils
{
	public static IPAddress Resolve(string hostname)
	{
		if (!string.IsNullOrEmpty(hostname?.Trim()))
		{
			return Dns.GetHostEntry(hostname.Trim()).AddressList?.First((IPAddress ip) => ip != null && ip.AddressFamily == AddressFamily.InterNetwork);
		}
		return new IPAddress(0L);
	}
}
