using Intersect.Client.Framework.Graphics;

namespace Intersect.Client.Interface.Shared;

public sealed class ConfirmCardRequest
{
	public required string Title { get; init; }

	public string Icon { get; init; } = "saiyan_carte_icone_alerte.png";

	public required string Heading { get; init; }

	public required string Prompt { get; init; }

	public string? Detail { get; init; }

	public string? Chip { get; init; }

	public bool ChipDanger { get; init; }

	public required string ConfirmLabel { get; init; }

	public bool Danger { get; init; }

	public string? CancelLabel { get; init; }

	public string? PlayerName { get; init; }

	public bool ShowMe { get; init; }

	public IGameTexture? Emblem { get; init; }

	public bool Centered { get; init; }

	public bool Notice { get; init; }
}
