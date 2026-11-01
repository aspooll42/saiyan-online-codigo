using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Intersect.Framework.Threading;

public abstract class ActionQueue<TActionQueue, TEnqueueState> where TActionQueue : ActionQueue<TActionQueue, TEnqueueState>
{
	private sealed record Return<TState, TReturn>(Func<TState, TReturn> Func)
	{
		public TReturn Value { get; private set; }

		public static readonly Action<State<TState, Return<TState, TReturn>>> Wrapper;

		public static readonly Action<TState, Return<TState, TReturn>> FuncWrapper;

		[CompilerGenerated]
		private Return(Return<TState, TReturn> original)
		{
			Func = original.Func;
			Value = original.Value;
		}

		static Return()
		{
			Wrapper = delegate(State<TState, Return<TState, TReturn>> state)
			{
				state.Action(state.State0, state.State1);
			};
			FuncWrapper = delegate(TState state, Return<TState, TReturn> returnValue)
			{
				returnValue.Value = returnValue.Func(state);
			};
		}
	}

	private sealed record Return<TState0, TState1, TReturn>(Func<TState0, TState1, TReturn> Func)
	{
		public TReturn Value { get; private set; }

		public static readonly Action<State<TState0, TState1, Return<TState0, TState1, TReturn>>> Wrapper;

		public static readonly Action<TState0, TState1, Return<TState0, TState1, TReturn>> FuncWrapper;

		[CompilerGenerated]
		private Return(Return<TState0, TState1, TReturn> original)
		{
			Func = original.Func;
			Value = original.Value;
		}

		static Return()
		{
			Wrapper = delegate(State<TState0, TState1, Return<TState0, TState1, TReturn>> state)
			{
				state.Action(state.State0, state.State1, state.State2);
			};
			FuncWrapper = delegate(TState0 state0, TState1 state1, Return<TState0, TState1, TReturn> returnValue)
			{
				returnValue.Value = returnValue.Func(state0, state1);
			};
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private record struct State<TState>
	{
		public record struct DeferredAction(Action<TState> Action, TState ActionState, Action<TEnqueueState> PostInvocationAction, TEnqueueState EnqueueState);

		public static readonly Queue<DeferredAction> ActionQueue;

		public static readonly Action Next;

		private static void InternalNext()
		{
			if (!ActionQueue.TryDequeue(out var result))
			{
				throw new InvalidOperationException("Action queue is not being properly synchronized");
			}
			result.Action(result.ActionState);
			result.PostInvocationAction(result.EnqueueState);
		}

		static State()
		{
			ActionQueue = new Queue<DeferredAction>();
			Next = InternalNext;
		}
	}

	private record struct State<TState0, TState1>(Action<TState0, TState1> Action, TState0 State0, TState1 State1)
	{
		public static readonly Action<State<TState0, TState1>> Wrapper;

		static State()
		{
			Wrapper = delegate(State<TState0, TState1> state)
			{
				state.Action(state.State0, state.State1);
			};
		}
	}

	private record struct State<TState0, TState1, TState2>(Action<TState0, TState1, TState2> Action, TState0 State0, TState1 State1, TState2 State2)
	{
		public static readonly Action<State<TState0, TState1, TState2>> Wrapper;

		static State()
		{
			Wrapper = delegate(State<TState0, TState1, TState2> state)
			{
				state.Action(state.State0, state.State1, state.State2);
			};
		}
	}

	private readonly TActionQueue @this;

	private readonly Queue<Action> _actionQueue = new Queue<Action>();

	private readonly Action<Action> _statelessAction = delegate(Action action)
	{
		action();
	};

	private readonly Action<TActionQueue>? _beginInvokePending;

	private readonly Action<TActionQueue>? _endInvokePending;

	private bool _empty = true;

	protected abstract bool IsActive { get; }

	protected abstract Action<TEnqueueState> PostInvocationAction { get; }

	public event Action? QueueNotEmpty;

	protected ActionQueue(Action<TActionQueue>? beginInvokePending, Action<TActionQueue>? endInvokePending)
	{
		this.@this = (this as TActionQueue) ?? throw new InvalidCastException();
		_beginInvokePending = beginInvokePending;
		_endInvokePending = endInvokePending;
	}

	public virtual void ClearPending()
	{
	}

	public bool InvokePending()
	{
		if (_empty)
		{
			return false;
		}
		_beginInvokePending?.Invoke(this.@this);
		_empty = true;
		lock (_actionQueue)
		{
			Action result;
			while (_actionQueue.TryDequeue(out result))
			{
				result();
			}
		}
		_endInvokePending?.Invoke(this.@this);
		return _actionQueue.Count < 1;
	}

	public void Enqueue(Action action)
	{
		if (IsActive)
		{
			action();
		}
		else
		{
			Enqueue(_statelessAction, action);
		}
	}

	public void Enqueue<TState>(Action<TState> action, TState state)
	{
		ArgumentNullException.ThrowIfNull(action, "action");
		if (IsActive)
		{
			action(state);
			return;
		}
		TEnqueueState enqueueState = EnqueueCreateState();
		try
		{
			State<TState>.DeferredAction item = new State<TState>.DeferredAction(action, state, PostInvocationAction, enqueueState);
			lock (_actionQueue)
			{
				State<TState>.ActionQueue.Enqueue(item);
				_actionQueue.Enqueue(State<TState>.Next);
				try
				{
					if (_empty)
					{
						this.QueueNotEmpty?.Invoke();
					}
				}
				catch
				{
				}
				_empty = false;
			}
			EnqueueSuccessful(enqueueState);
		}
		finally
		{
			EnqueueFinally(enqueueState);
		}
	}

	protected abstract TEnqueueState EnqueueCreateState();

	protected abstract void EnqueueSuccessful(TEnqueueState enqueueState);

	protected abstract void EnqueueFinally(TEnqueueState enqueueState);

	public TReturn EnqueueReturn<TState, TReturn>(Func<TState, TReturn> func, TState state)
	{
		if (IsActive)
		{
			return func(state);
		}
		Return<TState, TReturn> obj = new Return<TState, TReturn>(func);
		Enqueue(Return<TState, TReturn>.Wrapper, new State<TState, Return<TState, TReturn>>(Return<TState, TReturn>.FuncWrapper, state, obj));
		return obj.Value;
	}

	public TReturn EnqueueReturn<TState0, TState1, TReturn>(Func<TState0, TState1, TReturn> func, TState0 state0, TState1 state1)
	{
		if (IsActive)
		{
			return func(state0, state1);
		}
		Return<TState0, TState1, TReturn> obj = new Return<TState0, TState1, TReturn>(func);
		Enqueue(Return<TState0, TState1, TReturn>.Wrapper, new State<TState0, TState1, Return<TState0, TState1, TReturn>>(Return<TState0, TState1, TReturn>.FuncWrapper, state0, state1, obj));
		return obj.Value;
	}

	public void Enqueue<TState0, TState1>(Action<TState0, TState1> action, TState0 state0, TState1 state1)
	{
		if (IsActive)
		{
			action(state0, state1);
		}
		else
		{
			Enqueue(State<TState0, TState1>.Wrapper, new State<TState0, TState1>(action, state0, state1));
		}
	}

	public void Enqueue<TState0, TState1, TState2>(Action<TState0, TState1, TState2> action, TState0 state0, TState1 state1, TState2 state2)
	{
		if (IsActive)
		{
			action(state0, state1, state2);
		}
		else
		{
			Enqueue(State<TState0, TState1, TState2>.Wrapper, new State<TState0, TState1, TState2>(action, state0, state1, state2));
		}
	}
}
