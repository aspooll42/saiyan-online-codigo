using System;

namespace Intersect.Client.Interface.Game;

public sealed class DropdownRow
{
	public string Label { get; init; } = string.Empty;

	public string? Icon { get; init; }

	public string? Info { get; init; }

	public DropdownInfoKind InfoKind { get; init; }

	public string? Key { get; init; }

	public bool HasNews { get; init; }

	public bool IsUnavailable { get; init; }

	public bool RuleAbove { get; init; }

	public bool SeparatorAbove { get; init; }

	public bool IsDanger { get; init; }

	public bool TintIcon { get; init; }

	public Action? Action { get; init; }
}
