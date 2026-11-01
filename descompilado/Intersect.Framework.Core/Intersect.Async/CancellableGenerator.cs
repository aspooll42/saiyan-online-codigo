using System;
using System.Threading;

namespace Intersect.Async;

public class CancellableGenerator<TValue> : ICancellableGenerator<TValue>, ICancellableGenerator, IDisposable
{
	private readonly CancellationTokenSource _cancellationTokenSource;

	public AsyncValueGenerator<TValue> Generator { get; }

	public CancellableGenerator(Func<CancellationToken, AsyncValueGenerator<TValue>> generatorFactory)
	{
		if (generatorFactory == null)
		{
			throw new ArgumentNullException("generatorFactory");
		}
		_cancellationTokenSource = new CancellationTokenSource();
		Generator = generatorFactory(_cancellationTokenSource.Token);
	}

	public ICancellableGenerator<TValue> Start()
	{
		Generator.Start();
		return this;
	}

	public void Dispose()
	{
		_cancellationTokenSource.Cancel();
		_cancellationTokenSource?.Dispose();
	}
}
