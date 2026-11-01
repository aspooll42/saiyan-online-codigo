using System;

namespace Intersect.Framework.Core.Entities;

[Flags]
public enum FormeDeLancer : byte
{
	Simple = 0,
	Long = 1,
	Critique = 2,
	Cueilli = 4,
	Technique = 8,
	Lourd = 0x10,
	DeLHeritage = 0x80
}
