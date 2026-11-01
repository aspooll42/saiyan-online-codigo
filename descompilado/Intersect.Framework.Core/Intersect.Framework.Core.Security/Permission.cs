using System.Diagnostics;

namespace Intersect.Framework.Core.Security;

[DebuggerDisplay("{Name,nq}")]
public readonly record struct Permission(string Name)
{
	public static readonly Permission EngineVersion = "EngineVersion";

	public static readonly Permission WindowAdmin = "WindowAdmin";

	public static implicit operator Permission(string name)
	{
		return new Permission(name);
	}
}
