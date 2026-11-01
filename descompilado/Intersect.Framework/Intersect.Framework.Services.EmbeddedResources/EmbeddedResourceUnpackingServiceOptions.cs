using System.Collections.Generic;

namespace Intersect.Framework.Services.EmbeddedResources;

public sealed record EmbeddedResourceUnpackingServiceOptions(IEnumerable<EmbeddedResourceUnpackingRequest> Requests);
