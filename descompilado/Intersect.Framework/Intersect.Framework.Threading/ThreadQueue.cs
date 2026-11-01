using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Intersect.Framework.Threading;

public sealed class ThreadQueue : ActionQueue<ThreadQueue, ManualResetEventSlim>
{
	public static readonly ThreadQueue Default = new ThreadQueue();

	private readonly object _lock = new object();

	private readonly Stack<ManualResetEventSlim> _resetEventPool = new Stack<ManualResetEventSlim>();

	private readonly HashSet<CancellationTokenSource> _pendingCancellationTokenSources = new HashSet<CancellationTokenSource>();

	private readonly int? _spinCount;

	private int _mainThreadId;

	protected override bool IsActive => IsOnMainThread;

	public bool IsOnMainThread
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return _mainThreadId == Environment.CurrentManagedThreadId;
		}
	}

	protected override Action<ManualResetEventSlim> PostInvocationAction { get; } = delegate(ManualResetEventSlim resetEvent)
	{
		resetEvent.Set();
	};

	public ThreadQueue(int? spinCount = null)
		: base((Action<ThreadQueue>?)BeginInvokePending, (Action<ThreadQueue>?)null)
	{
		_spinCount = spinCount;
		SetMainThreadId();
	}

	public ThreadQueue(ThreadQueue parent)
		: base((Action<ThreadQueue>?)BeginInvokePending, (Action<ThreadQueue>?)null)
	{
		_spinCount = parent._spinCount;
		_mainThreadId = parent._mainThreadId;
	}

	public override void ClearPending()
	{
		base.ClearPending();
		CancellationTokenSource[] array;
		lock (_pendingCancellationTokenSources)
		{
			array = _pendingCancellationTokenSources.ToArray();
			_pendingCancellationTokenSources.Clear();
		}
		CancellationTokenSource[] array2 = array;
		foreach (CancellationTokenSource cancellationTokenSource in array2)
		{
			cancellationTokenSource.Cancel();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void BeginInvokePending(ThreadQueue @this)
	{
		@this.ThrowIfNotOnMainThread();
	}

	protected override ManualResetEventSlim EnqueueCreateState()
	{
		return ResetEventPoolPop();
	}

	protected override void EnqueueSuccessful(ManualResetEventSlim resetEvent)
	{
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		lock (_pendingCancellationTokenSources)
		{
			_pendingCancellationTokenSources.Add(cancellationTokenSource);
		}
		try
		{
			resetEvent.Wait(cancellationTokenSource.Token);
		}
		catch (OperationCanceledException ex)
		{
			if (!cancellationTokenSource.IsCancellationRequested || !ex.CancellationToken.Equals(cancellationTokenSource.Token))
			{
				throw;
			}
		}
		lock (_pendingCancellationTokenSources)
		{
			_pendingCancellationTokenSources.Remove(cancellationTokenSource);
		}
	}

	protected override void EnqueueFinally(ManualResetEventSlim resetEvent)
	{
		ResetEventPoolPush(resetEvent);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RunOnMainThread(Action action)
	{
		Enqueue(action);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RunOnMainThread<TState>(Action<TState> action, TState state)
	{
		Enqueue(action, state);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TReturn RunOnMainThread<TState, TReturn>(Func<TState, TReturn> func, TState state)
	{
		return EnqueueReturn(func, state);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RunOnMainThread<TState0, TState1>(Action<TState0, TState1> action, TState0 state0, TState1 state1)
	{
		Enqueue(action, state0, state1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TReturn RunOnMainThread<TState0, TState1, TReturn>(Func<TState0, TState1, TReturn> func, TState0 state0, TState1 state1)
	{
		return EnqueueReturn(func, state0, state1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RunOnMainThread<TState0, TState1, TState2>(Action<TState0, TState1, TState2> action, TState0 state0, TState1 state1, TState2 state2)
	{
		Enqueue(action, state0, state1, state2);
	}

	private ManualResetEventSlim ResetEventPoolPop()
	{
		lock (_resetEventPool)
		{
			if (_resetEventPool.TryPop(out ManualResetEventSlim result))
			{
				return result;
			}
		}
		if (!_spinCount.HasValue)
		{
			return new ManualResetEventSlim();
		}
		return new ManualResetEventSlim(initialState: false, _spinCount.Value);
	}

	private void ResetEventPoolPush(ManualResetEventSlim resetEvent)
	{
		resetEvent.Reset();
		lock (_resetEventPool)
		{
			_resetEventPool.Push(resetEvent);
		}
	}

	public void SetMainThreadId(int? mainThreadId = null)
	{
		lock (_lock)
		{
			_mainThreadId = mainThreadId ?? Environment.CurrentManagedThreadId;
		}
	}

	public void SetMainThreadId(ThreadQueue other)
	{
		lock (_lock)
		{
			_mainThreadId = other._mainThreadId;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ThrowIfNotOnMainThread()
	{
		if (IsOnMainThread)
		{
			return;
		}
		throw new InvalidOperationException("Operation was not called on the main thread.");
	}
}
