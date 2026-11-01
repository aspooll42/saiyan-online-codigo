using System;

namespace Intersect.Client.Framework.Gwen.Control;

[Flags]
public enum NodeFilter
{
	None = 0,
	IncludeHidden = 1,
	IncludeMouseInputDisabled = 2,
	IncludeText = 4
}
