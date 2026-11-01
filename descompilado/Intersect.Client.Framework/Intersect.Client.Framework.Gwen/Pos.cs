using System;

namespace Intersect.Client.Framework.Gwen;

[Flags]
public enum Pos
{
	None = 0,
	Left = 2,
	Right = 4,
	Top = 8,
	Bottom = 0x10,
	CenterV = 0x20,
	CenterH = 0x40,
	Fill = 0x80,
	Center = 0x60
}
