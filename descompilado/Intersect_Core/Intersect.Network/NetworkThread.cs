using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Intersect.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Network;

public sealed class NetworkThread
{
	private readonly object mLifecycleLock;

	private readonly PacketDispatcher mDispatcher;

	private bool mStarted;

	public string Name { get; }

	public Thread CurrentThread { get; }

	public PacketQueue Queue { get; }

	public IList<IConnection> Connections { get; }

	public bool IsRunning { get; private set; }

	public NetworkThread(PacketDispatcher dispatcher, string name = null)
	{
		mLifecycleLock = new object();
		mDispatcher = dispatcher;
		Name = name ?? "Network Worker Thread";
		CurrentThread = new Thread(Loop);
		Queue = new PacketQueue();
		Connections = new List<IConnection>();
	}

	public void Start()
	{
		lock (mLifecycleLock)
		{
			if (mStarted)
			{
				return;
			}
			mStarted = true;
			IsRunning = true;
		}
		CurrentThread.Start();
	}

	public void Stop()
	{
		lock (mLifecycleLock)
		{
			IsRunning = false;
		}
		Queue.Interrupt();
	}

	private void Loop()
	{
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		while (IsRunning)
		{
			if (Queue.TryNext(out var packet))
			{
				if (!mDispatcher.Dispatch(packet))
				{
					ApplicationContext.Context.Value?.Logger.LogWarning($"Failed to dispatch packet '{packet}'.");
				}
				packet?.Dispose();
				Thread.Yield();
			}
		}
		stopwatch.Stop();
		ApplicationContext.Context.Value?.Logger.LogDebug("Exiting network thread (" + Name + ").");
	}
}
