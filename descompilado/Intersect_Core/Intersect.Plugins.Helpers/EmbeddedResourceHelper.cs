using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Intersect.Plugins.Interfaces;

namespace Intersect.Plugins.Helpers;

internal sealed class EmbeddedResourceHelper : IEmbeddedResourceHelper
{
	private Assembly Assembly { get; }

	internal EmbeddedResourceHelper(Assembly assembly)
	{
		Assembly = assembly;
	}

	public bool Exists(string resourceName)
	{
		return Assembly.GetManifestResourceInfo(Resolve(resourceName)) != null;
	}

	public ManifestResourceInfo GetInfo(string resourceName)
	{
		return Assembly.GetManifestResourceInfo(Resolve(resourceName)) ?? throw new InvalidOperationException("Resource exists but info is null.");
	}

	public Stream Read(string resourceName)
	{
		return Assembly.GetManifestResourceStream(Resolve(resourceName)) ?? throw new InvalidOperationException("Resource exists but stream is null.");
	}

	public string Resolve(string resourceName)
	{
		if (string.IsNullOrWhiteSpace(resourceName))
		{
			throw new ArgumentNullException("resourceName", "resourceName cannot be null, empty, or whitespace.");
		}
		string partialManifestResourceName = resourceName.Replace("/", ".");
		string text = (from potentialManifestResourceName in Assembly.GetManifestResourceNames()
			where potentialManifestResourceName.EndsWith(partialManifestResourceName)
			orderby potentialManifestResourceName.Length
			select potentialManifestResourceName).FirstOrDefault();
		if (text == null)
		{
			throw new FileNotFoundException("Unable to find resource: " + resourceName);
		}
		return text;
	}

	public bool TryGetInfo(string resourceName, out ManifestResourceInfo resourceInfo)
	{
		try
		{
			resourceInfo = GetInfo(resourceName);
			return true;
		}
		catch
		{
			resourceInfo = null;
			return false;
		}
	}

	public bool TryRead(string resourceName, out Stream stream)
	{
		try
		{
			stream = Read(resourceName);
			return true;
		}
		catch
		{
			stream = null;
			return false;
		}
	}

	public bool TryResolve(string resourceName, out string manifestResourceName)
	{
		try
		{
			manifestResourceName = Resolve(resourceName);
			return true;
		}
		catch
		{
			manifestResourceName = null;
			return false;
		}
	}
}
