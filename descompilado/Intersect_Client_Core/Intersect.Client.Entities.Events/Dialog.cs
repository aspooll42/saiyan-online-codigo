using System;

namespace Intersect.Client.Entities.Events;

public class Dialog
{
	public Guid EventId;

	public string? Face;

	public string[] Options = Array.Empty<string>();

	public string? Prompt;

	public bool ResponseSent;
}
