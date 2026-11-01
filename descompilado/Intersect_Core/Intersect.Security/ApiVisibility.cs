using System;

namespace Intersect.Security;

[Flags]
public enum ApiVisibility : uint
{
	Public = uint.MaxValue,
	Restricted = 2u,
	Private = 1u,
	Hidden = 0u
}
