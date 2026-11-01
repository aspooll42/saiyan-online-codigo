using System;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Input;

namespace Intersect.Client.Interface.Shared;

internal sealed class OptionRow
{
	public OptionRowKind Kind { get; init; }

	public string Label { get; init; } = string.Empty;

	public string? Help { get; init; }

	public bool AppliesOnConfirm { get; init; }

	public Func<string?>? Unavailable { get; init; }

	public Func<bool>? Get { get; init; }

	public Action<bool>? Set { get; init; }

	public Func<bool>? GetSecond { get; init; }

	public Action<bool>? SetSecond { get; init; }

	public string[] Choices { get; init; } = Array.Empty<string>();

	public Func<int>? GetChoice { get; init; }

	public Action<int>? SetChoice { get; init; }

	public Control KeyControl { get; init; }

	public Color? Dot { get; init; }

	public Base? Widget { get; init; }

	public int WidgetWidth { get; init; }

	public Func<string>? Value { get; init; }

	public Func<double[]?>? Notches { get; init; }

	public int Height { get; init; } = 36;

	internal int Top { get; set; }

	internal bool RuleBelow { get; set; }
}
