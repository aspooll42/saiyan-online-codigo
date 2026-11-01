using Intersect.Plugins.Manifests.Types;
using Semver;

namespace Intersect.Plugins.Interfaces;

public interface IManifestHelper
{
	string Name { get; }

	string Key { get; }

	SemVersion Version { get; }

	Authors Authors { get; }

	string Homepage { get; }
}
