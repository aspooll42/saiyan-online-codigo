using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Resources;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Intersect.Client.Core;
using Intersect.Client.Framework.Network;
using Intersect.Client.General;
using Intersect.Client.Interface;
using Intersect.Client.Interface.Menu;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Configuration;
using Intersect.Core;
using Intersect.Framework.Core;
using Intersect.Network;
using Intersect.Network.Events;
using Intersect.Network.Packets;
using Intersect.Network.Packets.Unconnected.Client;
using Intersect.Network.Packets.Unconnected.Server;
using Intersect.Rsa;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.MonoGame.Network;

internal class MonoSocket : GameSocket
{
	private record struct ServerStatusRequestMetadata
	{
		public DateTime RequestTime { get; init; }

		public Guid StateToken { get; init; }
	}

	private static readonly NetworkFactory DefaultNetworkFactory = delegate(IApplicationContext context, RSAParameters parameters, HandlePacket handlePacket, ShouldProcessPacket? _)
	{
		NetworkConfiguration configuration = new NetworkConfiguration(ClientConfiguration.Instance.Host, ClientConfiguration.Instance.Port);
		return new ClientNetwork(context, configuration, parameters)
		{
			Handler = handlePacket
		};
	};

	private const long ServerStatusPingInterval = 15000L;

	private const string AsymmetricKeyManifestResourceName = "Intersect.Client.network.handshake.bkey.pub";

	private IClient? _network;

	private static readonly ConcurrentQueue<KeyValuePair<IConnection, IPacket>> PacketQueue = new ConcurrentQueue<KeyValuePair<IConnection, IPacket>>();

	private long _nextServerStatusPing;

	private string? _lastHost;

	private int? _lastPort;

	private IPEndPoint? _lastEndpoint;

	private volatile bool _resolvingHost;

	private int _ping;

	private ServerStatusRequestMetadata? _serverStatusRequestMetadata;

	internal static NetworkFactory? NetworkFactory { get; set; }

	public override IClient Network
	{
		get
		{
			if (_network != null)
			{
				return _network;
			}
			using Stream stream = typeof(MonoSocket).Assembly.GetManifestResourceStream("Intersect.Client.network.handshake.bkey.pub");
			if (stream == null)
			{
				throw new MissingManifestResourceException("Unable to get manifest resource stream for 'Intersect.Client.network.handshake.bkey.pub'");
			}
			RsaKey rsaKey = new RsaKey(stream);
			INetwork network = (NetworkFactory ?? DefaultNetworkFactory)(Context, rsaKey.Parameters, AddPacketToQueue, null);
			network.OnConnected += base.OnConnected;
			network.OnDisconnected += base.OnDisconnected;
			network.OnConnectionDenied += delegate(INetworkLayerInterface sender, ConnectionEventArgs connectionEventArgs)
			{
				OnConnectionFailed(sender, connectionEventArgs, denied: true);
			};
			_network = network as IClient;
			return _network;
		}
	}

	private IClientContext Context { get; }

	public static MonoSocket Instance { get; private set; } = null;

	public override bool IsConnected => Network.IsConnected;

	public override int Ping
	{
		get
		{
			if (_ping >= 0)
			{
				return _ping;
			}
			return Network.Ping;
		}
	}

	internal MonoSocket(IClientContext context)
	{
		Context = context;
		Instance = this;
	}

	private bool TryResolveEndPoint([NotNullWhen(true)] out IPEndPoint? endPoint)
	{
		string host = ClientConfiguration.Instance.Host;
		if (ClientNetwork.UnresolvableHostNames.Contains(host))
		{
			endPoint = null;
			return false;
		}
		if (!string.Equals(_lastHost, host))
		{
			_lastHost = host;
			_lastEndpoint = null;
		}
		ushort port = ClientConfiguration.Instance.Port;
		if (_lastPort != port)
		{
			_lastPort = port;
			_lastEndpoint = null;
		}
		if (string.IsNullOrWhiteSpace(_lastHost))
		{
			endPoint = null;
			return false;
		}
		if (_lastEndpoint != null)
		{
			endPoint = _lastEndpoint;
			return true;
		}
		try
		{
			IPAddress iPAddress = Dns.GetHostAddresses(_lastHost).FirstOrDefault();
			int? lastPort = _lastPort;
			if (iPAddress == null || !lastPort.HasValue)
			{
				endPoint = null;
				return false;
			}
			endPoint = new IPEndPoint(iPAddress, lastPort.Value);
			_lastEndpoint = endPoint;
			return true;
		}
		catch (SocketException ex)
		{
			if (ex.SocketErrorCode != SocketError.HostNotFound)
			{
				throw;
			}
			ClientNetwork.UnresolvableHostNames.Add(_lastHost);
			Intersect.Client.Interface.Interface.ShowAlert(Strings.Errors.HostNotFound);
			ApplicationContext.Context.Value?.Logger.LogError(ex, "Failed to resolve host: '" + _lastHost + "'");
			endPoint = null;
			return false;
		}
	}

	public override void Connect(string host, int port)
	{
		if (!Network.Connect())
		{
			ApplicationContext.Context.Value?.Logger.LogError("An error occurred while attempting to connect.");
		}
	}

	public override void SendPacket(object packet)
	{
		if (packet is IntersectPacket packet2)
		{
			_network?.Send(packet2);
		}
	}

	public override void SendPacket(object packet, TransmissionMode mode)
	{
		if (packet is IntersectPacket packet2)
		{
			_network?.Send(packet2, mode);
		}
	}

	public static bool AddPacketToQueue(IConnection connection, IPacket packet)
	{
		if (packet is AbstractTimedPacket abstractTimedPacket)
		{
			Timing.Global.Synchronize(abstractTimedPacket.UTC, abstractTimedPacket.Offset);
		}
		PacketQueue.Enqueue(new KeyValuePair<IConnection, IPacket>(connection, packet));
		return true;
	}

	public override void Update()
	{
		ChargementDesCartes.Installer();
		KeyValuePair<IConnection, IPacket> result;
		KeyValuePair<IConnection, IPacket> result2;
		while ((!ChargementDesCartes.Bloque || (PacketQueue.TryPeek(out result) && ChargementDesCartes.SeLitAPart(result.Value))) && PacketQueue.TryDequeue(out result2))
		{
			double maintenant = ChronoImage.Maintenant;
			OnDataReceived(result2.Value);
			double num = ChronoImage.Maintenant - maintenant;
			if (num > 0.3)
			{
				ChronoImage.Ajouter("paquet " + result2.Value.GetType().Name, num);
			}
		}
		switch (Globals.GameState)
		{
		case GameStates.Intro:
		case GameStates.Loading:
		case GameStates.InGame:
		case GameStates.Error:
			_ping = -1;
			break;
		case GameStates.Menu:
		{
			long millisecondsUtc = Timing.Global.MillisecondsUtc;
			if (_nextServerStatusPing > millisecondsUtc && MainMenu.LastNetworkStatusChangeTime >= 0)
			{
				break;
			}
			if (!_resolvingHost)
			{
				_resolvingHost = true;
				Task.Run(delegate
				{
					try
					{
						if (TryResolveEndPoint(out IPEndPoint endPoint))
						{
							IClient network = Network;
							if (network == null)
							{
								ApplicationContext.Context.Value?.Logger.LogInformation("No network created to poll for server status.");
							}
							else
							{
								ServerStatusRequestMetadata value = new ServerStatusRequestMetadata
								{
									RequestTime = DateTime.UtcNow,
									StateToken = Guid.NewGuid()
								};
								network.SendUnconnected(endPoint, new ServerStatusRequestPacket
								{
									StateToken = value.StateToken.ToByteArray(),
									VersionData = SharedConstants.VersionData
								}, null, 10000L);
								_serverStatusRequestMetadata = value;
							}
						}
						else if (!ClientNetwork.UnresolvableHostNames.Contains(_lastHost))
						{
							ApplicationContext.Context.Value?.Logger.LogInformation($"Unable to resolve '{_lastHost}:{_lastPort}'");
						}
					}
					catch (Exception exception)
					{
						ApplicationContext.Context.Value?.Logger.LogError(exception, "Error resolving host");
					}
					_resolvingHost = false;
				});
			}
			if (MainMenu.LastNetworkStatusChangeTime + 22500 < millisecondsUtc)
			{
				MainMenu.SetNetworkStatus(NetworkStatus.Offline);
			}
			_nextServerStatusPing = millisecondsUtc + 15000;
			break;
		}
		}
	}

	public override void Disconnect(string reason)
	{
		Network.Disconnect(reason);
	}

	public override void Dispose()
	{
		_network?.Close();
		_network?.Dispose();
		_network = null;
	}

	public void NotifyServerStatusResponse(ServerStatusResponsePacket responsePacket)
	{
		DateTime utcNow = DateTime.UtcNow;
		ServerStatusRequestMetadata? serverStatusRequestMetadata = _serverStatusRequestMetadata;
		if (serverStatusRequestMetadata.HasValue)
		{
			ServerStatusRequestMetadata valueOrDefault = serverStatusRequestMetadata.GetValueOrDefault();
			Guid guid = new Guid(responsePacket.StateToken);
			if (!(valueOrDefault.StateToken != guid))
			{
				_ping = (int)(utcNow - valueOrDefault.RequestTime).TotalMilliseconds;
				_serverStatusRequestMetadata = null;
			}
		}
	}
}
