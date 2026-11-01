using System;
using System.IO;

namespace Intersect.Framework.Core.IO;

public static class LectureDesRessources
{
	public static Func<string, bool> Existe { get; set; } = File.Exists;

	public static Func<string, string> LireTexte { get; set; } = File.ReadAllText;

	public static Func<string, DateTime> DateUtc { get; set; } = File.GetLastWriteTimeUtc;
}
