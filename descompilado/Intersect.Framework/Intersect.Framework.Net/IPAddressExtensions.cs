using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Intersect.Framework.Net;

public static class IPAddressExtensions
{
	private static readonly Dictionary<NetworkTypes, IPRange[]> IPRangesForNetworkType = new Dictionary<NetworkTypes, IPRange[]>
	{
		{
			NetworkTypes.Loopback,
			new IPRange[3]
			{
				new IPRange(IPAddress.Parse("::1")),
				new IPRange(IPAddress.Parse("0.0.0.0"), IPAddress.Parse("0.255.255.255")),
				new IPRange(IPAddress.Parse("127.0.0.0"), IPAddress.Parse("127.255.255.255"))
			}
		},
		{
			NetworkTypes.Subnet,
			new IPRange[3]
			{
				new IPRange(IPAddress.Parse("fe80::"), IPAddress.Parse("fe80::ffff:ffff:ffff:ffff")),
				new IPRange(IPAddress.Parse("169.254.0.0"), IPAddress.Parse("169.254.255.255")),
				new IPRange(IPAddress.Parse("255.255.255.255"))
			}
		},
		{
			NetworkTypes.PrivateNetwork,
			new IPRange[8]
			{
				new IPRange(IPAddress.Parse("fc00::"), IPAddress.Parse("fdff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")),
				new IPRange(IPAddress.Parse("::ffff:10.0.0.0"), IPAddress.Parse("::ffff:10.255.255.255")),
				new IPRange(IPAddress.Parse("10.0.0.0"), IPAddress.Parse("10.255.255.255")),
				new IPRange(IPAddress.Parse("100.64.0.0"), IPAddress.Parse("100.127.255.255")),
				new IPRange(IPAddress.Parse("172.16.0.0"), IPAddress.Parse("172.31.255.255")),
				new IPRange(IPAddress.Parse("192.0.0.0"), IPAddress.Parse("192.0.0.255")),
				new IPRange(IPAddress.Parse("192.168.0.0"), IPAddress.Parse("192.168.255.255")),
				new IPRange(IPAddress.Parse("192.18.0.0"), IPAddress.Parse("192.19.255.255"))
			}
		}
	};

	public static NetworkTypes GetNetworkType(this IPAddress address)
	{
		if (address.IsLoopback())
		{
			return NetworkTypes.Loopback;
		}
		if (address.IsPrivate())
		{
			return NetworkTypes.PrivateNetwork;
		}
		if (address.IsSubnet())
		{
			return NetworkTypes.Subnet;
		}
		return NetworkTypes.Public;
	}

	public static bool IsLoopback(this IPAddress address)
	{
		return address.MatchesNetworkTypes(NetworkTypes.Loopback);
	}

	public static bool IsPrivate(this IPAddress address)
	{
		return address.MatchesNetworkTypes(NetworkTypes.PrivateNetwork);
	}

	public static bool IsPublic(this IPAddress address)
	{
		return IPRangesForNetworkType.Values.All((IPRange[] ipRanges) => !ipRanges.Any((IPRange range) => range.Contains(address)));
	}

	public static bool IsSubnet(this IPAddress address)
	{
		return address.MatchesNetworkTypes(NetworkTypes.Subnet);
	}

	public static bool MatchesNetworkTypes(this IPAddress address, NetworkTypes networkTypes)
	{
		if (address.IsIPv4MappedToIPv6)
		{
			return address.MapToIPv4().MatchesNetworkTypes(networkTypes);
		}
		if (networkTypes == NetworkTypes.Public)
		{
			return address.IsPublic();
		}
		NetworkTypes[] flags = networkTypes.GetFlags();
		IPRange[] value;
		return flags.Any((NetworkTypes flag) => IPRangesForNetworkType.TryGetValue(flag, out value) && value.Any((IPRange range) => range.Contains(address)));
	}
}
