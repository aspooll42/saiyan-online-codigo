namespace Intersect.Network;

public enum NetworkStatus
{
	Unknown,
	Connecting,
	Online,
	Offline,
	Failed,
	VersionMismatch,
	ServerFull,
	HandshakeFailure,
	Quitting
}
