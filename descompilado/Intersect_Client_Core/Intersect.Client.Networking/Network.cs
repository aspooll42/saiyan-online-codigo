using System;
using System.Threading;
using System.Threading.Tasks;
using Intersect.Client.Core;
using Intersect.Client.Framework.Network;
using Intersect.Client.General;
using Intersect.Client.Interface;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Configuration;
using Intersect.Core;
using Intersect.Network;
using Intersect.Network.Events;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Networking;

internal static class Network
{
	private static readonly TimeSpan CloseTaskLockTimeout = TimeSpan.FromSeconds(15.0);

	private static readonly object CloseTaskLock = new object();

	private static Task? _closeTask;

	private static GameSocket? _socket;

	internal static PacketHandler? PacketHandler { get; set; }

	public static bool IsConnected => Socket.IsConnected;

	public static int Ping => Socket.Ping;

	public static GameSocket Socket
	{
		get
		{
			return _socket ?? throw new InvalidOperationException("Socket not created.");
		}
		set
		{
			_socket = value;
		}
	}

	public static void Close(string reason)
	{
		try
		{
			Socket.Disconnect(reason);
			Socket.Dispose();
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogTrace(exception, "Error closing socket");
		}
	}

	public static void DebounceClose(string reason)
	{
		if (!IsConnected)
		{
			return;
		}
		if (!Monitor.TryEnter(CloseTaskLock, CloseTaskLockTimeout))
		{
			throw new Exception("Failed to acquire lock on CloseTaskLock from debounce");
		}
		try
		{
			if (_closeTask != null)
			{
				ApplicationContext.Context.Value?.Logger.LogDebug("Disconnect already queued, skipping");
				return;
			}
			_closeTask = Task.Delay(2500);
			_closeTask.ContinueWith(delegate(Task task)
			{
				if (!Monitor.TryEnter(CloseTaskLock, CloseTaskLockTimeout))
				{
					throw new Exception("Failed to acquire lock on CloseTaskLock from debounce continue");
				}
				try
				{
					if (_closeTask != task)
					{
						ApplicationContext.Context.Value?.Logger.LogDebug("Disconnect interrupted, skipping");
					}
					else
					{
						Globals.SoftLogout = true;
						_closeTask = null;
						Close(reason);
					}
				}
				finally
				{
					Monitor.Exit(CloseTaskLock);
				}
			});
		}
		finally
		{
			Monitor.Exit(CloseTaskLock);
		}
	}

	public static void InitNetwork(IClientContext context)
	{
		Socket.Connected += OnConnected;
		Socket.Disconnected += OnDisconnected;
		Socket.DataReceived += OnDataReceived;
		Socket.ConnectionFailed += OnConnectionFailed;
	}

	public static bool InterruptDisconnectsIfConnected()
	{
		if (!Monitor.TryEnter(CloseTaskLock, CloseTaskLockTimeout))
		{
			throw new Exception("Failed to acquire lock on CloseTaskLock from InterruptDisconnectsIfConnected");
		}
		try
		{
			_closeTask = null;
			return IsConnected;
		}
		finally
		{
			Monitor.Exit(CloseTaskLock);
		}
	}

	private static void OnConnected(INetworkLayerInterface sender, ConnectionEventArgs connectionEventArgs)
	{
		Globals.SoftLogout = false;
	}

	private static void OnConnectionFailed(INetworkLayerInterface sender, ConnectionEventArgs connectionEventArgs, bool denied)
	{
		Globals.WaitingOnServer = false;
		if (!denied)
		{
			TryConnect();
		}
	}

	private static void OnDataReceived(IPacket packet)
	{
		PacketHandler?.HandlePacket(packet);
	}

	private static void OnDisconnected(INetworkLayerInterface sender, ConnectionEventArgs connectionEventArgs)
	{
		string message = ((connectionEventArgs.EventId == default(Guid)) ? Strings.Errors.LostConnection.ToString() : Strings.Errors.DisconnectionEvent.ToString(connectionEventArgs.EventId));
		if (Globals.SoftLogout)
		{
			Globals.SoftLogout = false;
		}
		else
		{
			Intersect.Client.Interface.Interface.ShowAlert(message, null, AlertType.Information);
			Fade.Cancel();
		}
		Globals.WaitingOnServer = false;
		if (Globals.GameState == GameStates.InGame || Globals.GameState == GameStates.Loading)
		{
			Globals.ConnectionLost = true;
		}
	}

	public static void SendPacket(IntersectPacket packet)
	{
		Socket.SendPacket(packet);
	}

	public static void SendPacket(IntersectPacket packet, TransmissionMode mode)
	{
		Socket.SendPacket(packet, mode);
	}

	public static void TryConnect()
	{
		if (!IsConnected)
		{
			Globals.WaitingOnServer = true;
			Socket.Connect(ClientConfiguration.Instance.Host, ClientConfiguration.Instance.Port);
		}
	}

	public static void Update()
	{
		Socket.Update();
	}
}
