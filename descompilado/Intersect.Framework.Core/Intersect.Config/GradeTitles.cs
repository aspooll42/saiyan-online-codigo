using System;

namespace Intersect.Config;

public static class GradeTitles
{
	public static readonly Guid[] Police = new Guid[6]
	{
		new Guid("5E1C7A20-8B3D-4F61-9C2E-7D4A1B6E3F01"),
		new Guid("5E1C7A20-8B3D-4F61-9C2E-7D4A1B6E3F02"),
		new Guid("5E1C7A20-8B3D-4F61-9C2E-7D4A1B6E3F03"),
		new Guid("5E1C7A20-8B3D-4F61-9C2E-7D4A1B6E3F04"),
		new Guid("5E1C7A20-8B3D-4F61-9C2E-7D4A1B6E3F05"),
		new Guid("5E1C7A20-8B3D-4F61-9C2E-7D4A1B6E3F06")
	};

	public static readonly Guid[] RubanRouge = new Guid[6]
	{
		new Guid("5E1C7A20-8B3D-4F61-9C2E-7D4A1B6E3F11"),
		new Guid("5E1C7A20-8B3D-4F61-9C2E-7D4A1B6E3F12"),
		new Guid("5E1C7A20-8B3D-4F61-9C2E-7D4A1B6E3F13"),
		new Guid("5E1C7A20-8B3D-4F61-9C2E-7D4A1B6E3F14"),
		new Guid("5E1C7A20-8B3D-4F61-9C2E-7D4A1B6E3F15"),
		new Guid("5E1C7A20-8B3D-4F61-9C2E-7D4A1B6E3F16")
	};

	public static Guid PoliceTitle(int grade)
	{
		if (grade < 0 || grade >= Police.Length)
		{
			return Guid.Empty;
		}
		return Police[grade];
	}

	public static Guid RibbonTitle(int grade)
	{
		if (grade < 0 || grade >= RubanRouge.Length)
		{
			return Guid.Empty;
		}
		return RubanRouge[grade];
	}
}
