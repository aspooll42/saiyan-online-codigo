using System;

namespace Intersect.Network;

public interface IClient : INetwork, IDisposable
{
	IConnection Connection { get; }

	new bool IsConnected { get; }

	bool IsServerOnline { get; }

	new int Ping { get; }

	new bool Connect();
}
