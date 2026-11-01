using System.IO;
using System.Reflection;

namespace Intersect.Plugins.Interfaces;

public interface IEmbeddedResourceHelper
{
	bool Exists(string resourceName);

	ManifestResourceInfo GetInfo(string resourceName);

	Stream Read(string resourceName);

	string Resolve(string resourceName);

	bool TryGetInfo(string resourceName, out ManifestResourceInfo resourceInfo);

	bool TryRead(string resourceName, out Stream stream);

	bool TryResolve(string resourceName, out string manifestResourceName);
}
