using System;

namespace Intersect.Client.Interface.Data;

public sealed class DelayProvider : DelayedDataProvider<bool>
{
	public DelayProvider(TimeSpan delay)
		: base(delay)
	{
	}

	protected override bool TryDelayedUpdate(TimeSpan elapsed, TimeSpan total)
	{
		EmitValueChanged(value: true, oldValue: true);
		return true;
	}
}
