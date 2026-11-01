using System;

namespace Intersect.Async;

public interface ICancellableGenerator : IDisposable
{
	ICancellableGenerator Start();
}
public interface ICancellableGenerator<TValue> : ICancellableGenerator, IDisposable
{
	new ICancellableGenerator<TValue> Start();

	ICancellableGenerator ICancellableGenerator.Start()
	{
		return Start();
	}
}
