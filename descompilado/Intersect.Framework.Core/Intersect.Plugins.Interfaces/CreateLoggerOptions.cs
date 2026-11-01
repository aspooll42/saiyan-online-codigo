using System;
using Microsoft.Extensions.Logging;

namespace Intersect.Plugins.Interfaces;

public record struct CreateLoggerOptions
{
	public LogLevel Console { get; init; }

	public LogLevel File { get; init; }

	public string Name { get; init; }

	public string? ContextName { get; init; }

	public Type? ContextType { get; init; }
}
