using System;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using Intersect.Framework.Reflection;
using Intersect.Framework.Services.Bootstrapping;
using Microsoft.Extensions.Logging;

namespace Intersect.Framework.Services.EmbeddedResources;

public sealed class EmbeddedResourceUnpackingService : IBootstrapTask
{
	private readonly ILogger<EmbeddedResourceUnpackingService> _logger;

	private readonly EmbeddedResourceUnpackingServiceOptions _options;

	public EmbeddedResourceUnpackingService(ILoggerFactory loggerFactory, EmbeddedResourceUnpackingServiceOptions options)
	{
		_logger = loggerFactory.CreateLogger<EmbeddedResourceUnpackingService>();
		_options = options;
	}

	public async Task ExecuteAsync(CancellationToken cancellationToken)
	{
		foreach (EmbeddedResourceUnpackingRequest resourceUnpackingRequest in _options.Requests)
		{
			string sanitizedResourceName = resourceUnpackingRequest.ResourceName;
			sanitizedResourceName = sanitizedResourceName.Replace('-', '_').Replace('/', '.').Replace('\\', '.');
			try
			{
				await resourceUnpackingRequest.Assembly.UnpackEmbeddedFileAsync(cancellationToken, sanitizedResourceName, resourceUnpackingRequest.Overwrite, resourceUnpackingRequest.UnpackedName);
			}
			catch (MissingManifestResourceException exception)
			{
				_logger.LogError(exception, "'{ResourceName}' was not found in '{AssemblyName}' ({SanitizedResourceName})", resourceUnpackingRequest.ResourceName, resourceUnpackingRequest.Assembly.FullName ?? resourceUnpackingRequest.Assembly.GetName().FullName, sanitizedResourceName);
				throw;
			}
			catch (Exception exception2)
			{
				_logger.LogError(exception2, "Error unpacking {ResourceName}", resourceUnpackingRequest.ResourceName);
				throw;
			}
		}
	}
}
