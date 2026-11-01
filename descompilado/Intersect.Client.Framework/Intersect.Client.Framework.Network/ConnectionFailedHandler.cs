using Intersect.Network;
using Intersect.Network.Events;

namespace Intersect.Client.Framework.Network;

public delegate void ConnectionFailedHandler(INetworkLayerInterface sender, ConnectionEventArgs connectionEventArgs, bool denied);
