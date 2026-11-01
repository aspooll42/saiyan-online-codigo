using System;

namespace Intersect.Client.Interface.Data;

public abstract class DelayedDataProvider<TValue> : DataProvider<TValue>
{
	public static readonly TimeSpan MinimumDelay = TimeSpan.FromMilliseconds(10.0);

	private TimeSpan _delay;

	private TimeSpan _nextUpdate;

	public TimeSpan Delay
	{
		get
		{
			return _delay;
		}
		set
		{
			if (!(value == _delay))
			{
				if (value < MinimumDelay)
				{
					throw new ArgumentOutOfRangeException("value", value, $"Delay must be at least {MinimumDelay}");
				}
				_delay = value;
			}
		}
	}

	protected DelayedDataProvider()
		: this(MinimumDelay)
	{
	}

	protected DelayedDataProvider(TimeSpan delay)
	{
		if (delay < MinimumDelay)
		{
			throw new ArgumentOutOfRangeException("delay", delay, $"Delay must be at least {MinimumDelay}");
		}
		_delay = delay;
	}

	public override bool TryUpdate(TimeSpan elapsed, TimeSpan total)
	{
		if (_nextUpdate > total)
		{
			return false;
		}
		bool result = TryDelayedUpdate(elapsed, total);
		_nextUpdate = total + _delay;
		return result;
	}

	protected abstract bool TryDelayedUpdate(TimeSpan elapsed, TimeSpan total);
}
