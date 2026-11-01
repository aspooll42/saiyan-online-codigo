using System;

namespace Intersect.Client.Interface.Data;

public sealed class DelegateDataProvider<TValue> : DataProvider<TValue>
{
	private readonly TryProviderValueDelegate<TValue> _providerDelegate;

	public DelayProvider? DelayProvider { get; set; }

	public DelegateDataProvider(Func<TValue> simpleProvider)
		: this(CreateTryProviderValueDelegateFrom(simpleProvider))
	{
	}

	public DelegateDataProvider(Func<TValue> simpleProvider, int delayMilliseconds)
		: this(CreateTryProviderValueDelegateFrom(simpleProvider), TimeSpan.FromMilliseconds(delayMilliseconds))
	{
	}

	public DelegateDataProvider(Func<TValue> simpleProvider, TimeSpan delay)
		: this(CreateTryProviderValueDelegateFrom(simpleProvider), delay)
	{
	}

	public DelegateDataProvider(TryProviderValueDelegate<TValue> providerDelegate)
	{
		_providerDelegate = providerDelegate;
	}

	public DelegateDataProvider(TryProviderValueDelegate<TValue> providerDelegate, TimeSpan delay)
	{
		_providerDelegate = providerDelegate;
		DelayProvider = new DelayProvider(delay);
	}

	public override bool TryUpdate(TimeSpan elapsed, TimeSpan total)
	{
		DelayProvider delayProvider = DelayProvider;
		if (delayProvider != null && !delayProvider.TryUpdate(elapsed, total))
		{
			return false;
		}
		if (_providerDelegate(elapsed, total, out var newValue))
		{
			return TrySetValue(newValue);
		}
		return false;
	}

	public static TryProviderValueDelegate<TValue> CreateTryProviderValueDelegateFrom(Func<TValue> simpleProvider)
	{
		return delegate(TimeSpan _, TimeSpan _, out TValue value)
		{
			value = simpleProvider();
			return true;
		};
	}
}
