using Microsoft.Extensions.Logging;

namespace Intersect.Plugins.Interfaces;

public interface ILoggingHelper
{
	ILogger Application { get; }

	ILogger Plugin { get; }

	ILogger CreateLogger(CreateLoggerOptions createLoggerOptions);

	ILogger<TContext> CreateLogger<TContext>(CreateLoggerOptions createLoggerOptions);
}
