using System;

namespace Intersect.Framework.Core.Entities;

[Flags]
public enum MomentHeritage : byte
{
	Aucun = 0,
	Boule = 1,
	Kiai = 2,
	Onde = 4,
	Signature = 8
}
