using System;
using System.Threading;
using System.Threading.Tasks;
using Intersect.Async;

namespace Intersect.Client.Framework.Gwen.Control.Data;

public class ValueTableCellDataProvider<TValue> : ITableCellDataProvider
{
	private readonly int _delay;

	private readonly Func<CancellationToken, TValue> _providerValue;

	private readonly Func<Task<bool>>? _waitPredicate;

	public CancellableGenerator<TValue> Generator { get; }

	public event TableCellDataChangedEventHandler? DataChanged;

	public ValueTableCellDataProvider(Func<TValue> provideValue, int delay = 100, Func<Task<bool>>? waitPredicate = null)
		: this((Func<CancellationToken, TValue>)((CancellationToken _) => provideValue()), delay, waitPredicate)
	{
	}

	public ValueTableCellDataProvider(Func<CancellationToken, TValue> provideValue, int delay = 100, Func<Task<bool>>? waitPredicate = null)
	{
		_delay = delay;
		_providerValue = provideValue;
		_waitPredicate = waitPredicate;
		Generator = new CancellableGenerator<TValue>(CreateValueGenerator);
	}

	private AsyncValueGenerator<TValue> CreateValueGenerator(CancellationToken cancellationToken)
	{
		return new AsyncValueGenerator<TValue>(() => WaitAndProvideValue(cancellationToken), delegate(TValue value)
		{
			this.DataChanged?.Invoke(this, new CellDataChangedEventArgs(null, value));
		}, cancellationToken);
	}

	private async Task<TValue> WaitAndProvideValue(CancellationToken cancellationToken)
	{
		if (_waitPredicate == null)
		{
			await Task.Delay(_delay, cancellationToken);
		}
		else
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				await Task.Delay(_delay, cancellationToken);
				if (await _waitPredicate())
				{
					break;
				}
			}
		}
		cancellationToken.ThrowIfCancellationRequested();
		return _providerValue(cancellationToken);
	}
}
