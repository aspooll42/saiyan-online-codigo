using System;
using System.Threading;

namespace Intersect.Framework.Threading;

public sealed class ManualActionQueue : ActionQueue<ManualActionQueue, bool>
{
	private readonly object _lock = new object();

	private bool _active;

	private readonly IManualActionQueueParent? _parent;

	protected override bool IsActive => _active;

	protected override Action<bool> PostInvocationAction => delegate
	{
	};

	public ManualActionQueue(IManualActionQueueParent? parent = null)
		: base((Action<ManualActionQueue>?)BeginInvokePending, (Action<ManualActionQueue>?)EndInvokePending)
	{
		_parent = parent;
	}

	private static void BeginInvokePending(ManualActionQueue @this)
	{
		IManualActionQueueParent parent = @this._parent;
		if ((parent != null && !parent.IsExecuting) || !Monitor.TryEnter(@this._lock))
		{
			throw new InvalidOperationException("Tried to invoke pending actions from an invalid source");
		}
		@this._active = true;
	}

	private static void EndInvokePending(ManualActionQueue @this)
	{
		@this._active = false;
		Monitor.Exit(@this._lock);
	}

	protected override bool EnqueueCreateState()
	{
		bool lockTaken = false;
		Monitor.Enter(_lock, ref lockTaken);
		return lockTaken;
	}

	protected override void EnqueueSuccessful(bool lockTaken)
	{
	}

	protected override void EnqueueFinally(bool lockTaken)
	{
		if (lockTaken)
		{
			Monitor.Exit(_lock);
		}
	}
}
