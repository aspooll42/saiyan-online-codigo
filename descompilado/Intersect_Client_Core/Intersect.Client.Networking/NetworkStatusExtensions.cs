using System;
using Intersect.Client.Localization;
using Intersect.Network;

namespace Intersect.Client.Networking;

public static class NetworkStatusExtensions
{
	public static string ToLocalizedString(this NetworkStatus networkStatus)
	{
		return networkStatus switch
		{
			NetworkStatus.Unknown => Strings.Server.Unknown, 
			NetworkStatus.Connecting => Strings.Server.Connecting, 
			NetworkStatus.Online => Strings.Server.Online, 
			NetworkStatus.Offline => Strings.Server.Offline, 
			NetworkStatus.Failed => Strings.Server.Failed, 
			NetworkStatus.VersionMismatch => Strings.Server.VersionMismatch, 
			NetworkStatus.ServerFull => Strings.Server.ServerFull, 
			NetworkStatus.HandshakeFailure => Strings.Server.HandshakeFailure, 
			NetworkStatus.Quitting => "", 
			_ => throw new ArgumentOutOfRangeException("networkStatus", networkStatus, null), 
		};
	}
}
