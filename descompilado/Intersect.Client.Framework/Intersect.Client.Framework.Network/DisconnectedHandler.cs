using Intersect.Network;
using Intersect.Network.Events;

namespace Intersect.Client.Framework.Network;

public delegate void DisconnectedHandler(INetworkLayerInterface sender, ConnectionEventArgs connectionEventArgs);
