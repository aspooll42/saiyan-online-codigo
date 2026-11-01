using Intersect.Plugins.Interfaces;
using Intersect.Plugins.Manifests.Types;
using Newtonsoft.Json;
using Semver;

namespace Intersect.Plugins.Manifests;

public class JsonManifest : IManifestHelper
{
	[JsonProperty]
	public string Name { get; private set; } = string.Empty;

	[JsonProperty]
	public string Key { get; private set; } = string.Empty;

	[JsonProperty]
	public SemVersion Version { get; private set; } = new SemVersion(1);

	[JsonProperty]
	public Authors Authors { get; private set; }

	[JsonProperty]
	public string Homepage { get; private set; } = string.Empty;
}
