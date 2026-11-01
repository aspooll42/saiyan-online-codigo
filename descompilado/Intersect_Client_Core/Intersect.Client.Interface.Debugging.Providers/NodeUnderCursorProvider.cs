using System;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Interface.Data;

namespace Intersect.Client.Interface.Debugging.Providers;

public sealed class NodeUnderCursorProvider : DelayedDataProvider<Base?>
{
	public NodeFilter Filter { get; set; } = NodeFilter.IncludeMouseInputDisabled;

	public NodeUnderCursorProvider(TimeSpan delay)
		: base(delay)
	{
	}

	public NodeUnderCursorProvider()
		: this(DelayedDataProvider<Base>.MinimumDelay)
	{
	}

	protected override bool TryDelayedUpdate(TimeSpan elapsed, TimeSpan total)
	{
		Base value = Interface.FindComponentUnderCursor(Filter);
		return TrySetValue(value);
	}
}
