using System;
using System.Collections.Generic;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Interface.Data;

public abstract class DataProvider<TValue> : IDataProvider<TValue>, IDataProvider, IUpdatableDataProvider
{
	private readonly Dictionary<DataProviderEventHandler<ValueChangedEventArgs<object?>>, DataProviderEventHandler<ValueChangedEventArgs<TValue>>> _wrappedDelegates = new Dictionary<DataProviderEventHandler<ValueChangedEventArgs<object>>, DataProviderEventHandler<ValueChangedEventArgs<TValue>>>();

	public object? UserData { get; set; }

	public TValue Value { get; private set; }

	public event DataProviderEventHandler<ValueChangedEventArgs<TValue>>? ValueChanged;

	event DataProviderEventHandler<ValueChangedEventArgs<object?>>? IDataProvider.ValueChanged
	{
		add
		{
			if (value != null && !_wrappedDelegates.TryGetValue(value, out DataProviderEventHandler<ValueChangedEventArgs<TValue>> value2))
			{
				value2 = delegate(IDataProvider sender, ValueChangedEventArgs<TValue> args)
				{
					value?.Invoke(sender, new ValueChangedEventArgs<object>
					{
						Value = args.Value,
						OldValue = args.OldValue
					});
				};
				ValueChanged += value2;
				_wrappedDelegates[value] = value2;
			}
		}
		remove
		{
			if (value != null && _wrappedDelegates.Remove(value, out DataProviderEventHandler<ValueChangedEventArgs<TValue>> value2))
			{
				ValueChanged -= value2;
			}
		}
	}

	protected bool TrySetValue(TValue value)
	{
		TValue value2 = Value;
		bool num;
		if (value != null)
		{
			num = value.Equals(value2);
		}
		else if (value2 != null)
		{
			num = value2.Equals(value);
		}
		else
		{
			return false;
		}
		if (num)
		{
			return false;
		}
		Value = value;
		EmitValueChanged(value, value2);
		return true;
	}

	protected void EmitValueChanged(TValue value, TValue oldValue)
	{
		try
		{
			this.ValueChanged?.Invoke(this, new ValueChangedEventArgs<TValue>
			{
				Value = value,
				OldValue = oldValue
			});
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Exception thrown in ValueChanged event handler");
		}
	}

	public abstract bool TryUpdate(TimeSpan elapsed, TimeSpan total);
}
