using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Intersect.Framework.Core.GameObjects.PlayerClass;
using Intersect.Models;

namespace Intersect.Client.Entities;

public static class CorpsDuCreateur
{
	private static readonly Regex Nom = new Regex("^saiyan-(?<coiffure>[^-_]+)-(?<couleur>[^-_]+)-(?<peau>[^-_]+)(?<suite>_.*)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

	public static bool TryLire([NotNullWhen(true)] string? sprite, out string coiffure, out string couleur, out string peau)
	{
		Match match = Nom.Match(Path.GetFileNameWithoutExtension(sprite ?? string.Empty));
		bool flag = match.Success && !match.Groups["suite"].Success;
		coiffure = (flag ? match.Groups["coiffure"].Value : string.Empty);
		couleur = (flag ? match.Groups["couleur"].Value : string.Empty);
		peau = (flag ? match.Groups["peau"].Value : string.Empty);
		return flag;
	}

	public static string Commun(string planche)
	{
		Match match = Nom.Match(planche);
		if (!match.Success)
		{
			return planche;
		}
		return "saiyan" + match.Groups["suite"].Value;
	}

	public static string? Forme(string sprite, string forme)
	{
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sprite);
		if (!TryLire(fileNameWithoutExtension, out string _, out string _, out string peau))
		{
			return null;
		}
		string nom = Path.GetFileNameWithoutExtension(forme);
		string text = (from nomDeCorps in (from entree in DatabaseObject<ClassDescriptor>.Lookup.Values.OfType<ClassDescriptor>().SelectMany((ClassDescriptor classe) => classe.Sprites)
				select Path.GetFileNameWithoutExtension(entree?.Sprite ?? string.Empty)).Append<string>(fileNameWithoutExtension)
			where nomDeCorps.Length > 0 && nom.StartsWith(nomDeCorps + "_", StringComparison.OrdinalIgnoreCase)
			orderby nomDeCorps.Length descending
			select nomDeCorps).FirstOrDefault();
		if (text != null && !string.Equals(text, fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
		{
			peau = nom;
			int length = text.Length;
			return fileNameWithoutExtension + peau.Substring(length, peau.Length - length) + Path.GetExtension(forme);
		}
		return null;
	}
}
