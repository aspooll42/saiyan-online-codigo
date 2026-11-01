using System.Reflection;
using System.Runtime.CompilerServices;

namespace Intersect.Framework.Services.EmbeddedResources;

public sealed record EmbeddedResourceUnpackingRequest
{
	public bool Overwrite { get; init; }

	public string? UnpackedName { get; init; }

	public Assembly Assembly { get; }

	public string ResourceName { get; }

	public EmbeddedResourceUnpackingRequest(Assembly Assembly, string ResourceName, bool Overwrite = false, string? UnpackedName = null)
	{
		this.Overwrite = Overwrite;
		this.UnpackedName = UnpackedName;
		this.Assembly = Assembly;
		this.ResourceName = ResourceName;
	}

	[CompilerGenerated]
	public void Deconstruct(out Assembly Assembly, out string ResourceName, out bool Overwrite, out string? UnpackedName)
	{
		Assembly = this.Assembly;
		ResourceName = this.ResourceName;
		Overwrite = this.Overwrite;
		UnpackedName = this.UnpackedName;
	}
}
