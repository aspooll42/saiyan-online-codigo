using System;
using Intersect.Client.Entities;
using Intersect.Client.General;
using Intersect.Client.Interface.Data;

namespace Intersect.Client.Interface.Debugging.Providers;

public sealed class ActivePlayerProvider : DelayedDataProvider<Player?>
{
	public ActivePlayerProvider(TimeSpan delay)
		: base(delay)
	{
	}

	public ActivePlayerProvider()
		: this(DelayedDataProvider<Player>.MinimumDelay)
	{
	}

	protected override bool TryDelayedUpdate(TimeSpan elapsed, TimeSpan total)
	{
		Player me = Globals.Me;
		return TrySetValue(me);
	}
}
