using System;
using System.Globalization;
using System.Text;
using Intersect.Config;

namespace Intersect.Framework.Core.Entities;

public static class PassifsDeClasse
{
	private static readonly PassifsOptions ParDefaut = new PassifsOptions();

	public static PassifsOptions Reglages => Options.Instance?.Combat?.Passifs ?? ParDefaut;

	public static PassifDeClasse De(string? nomDeClasse)
	{
		switch (Cle(nomDeClasse))
		{
		case "goku":
			return PassifDeClasse.CoeurPur;
		case "vegeta":
			return PassifDeClasse.Orgueil;
		case "gohan":
		case "gohan petit":
		case "petit gohan":
		case "gohan enfant":
			return PassifDeClasse.Colere;
		case "gohan ado":
		case "gohan adolescent":
			return PassifDeClasse.Heritier;
		case "trunks petit":
		case "petit trunks":
		case "trunks enfant":
			return PassifDeClasse.Insolent;
		case "trunks futur":
		case "trunks du futur":
			return PassifDeClasse.Epee;
		case "goten":
			return PassifDeClasse.Cadet;
		default:
			return PassifDeClasse.Aucun;
		}
	}

	public static bool Agit(PassifDeClasse passifDuJoueur, PassifDeClasse passif)
	{
		if (passif != PassifDeClasse.Aucun && passifDuJoueur == passif)
		{
			return Reglages.Actif;
		}
		return false;
	}

	public static float AllongePx(PassifDeClasse passif)
	{
		if (!Agit(passif, PassifDeClasse.Epee))
		{
			return 0f;
		}
		return Math.Clamp(Reglages.EpeeAllongePx, 0, 32);
	}

	public static int RechargeDuDash(PassifDeClasse passif, int millisecondes)
	{
		if (!Agit(passif, PassifDeClasse.Insolent) || millisecondes <= 0)
		{
			return millisecondes;
		}
		int num = Math.Clamp(Reglages.InsolentRechargePourcent, 0, 90);
		return (int)Math.Round((double)(millisecondes * (100 - num)) / 100.0);
	}

	private static string Cle(string? nom)
	{
		if (string.IsNullOrWhiteSpace(nom))
		{
			return string.Empty;
		}
		string text = nom.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		bool flag = false;
		string text2 = text;
		foreach (char c in text2)
		{
			if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
			{
				continue;
			}
			bool flag2 = char.IsWhiteSpace(c);
			bool flag3 = flag2;
			if (!flag3)
			{
				bool flag4 = ((c == '-' || c == '_') ? true : false);
				flag3 = flag4;
			}
			if (flag3)
			{
				flag = stringBuilder.Length > 0;
				continue;
			}
			if (flag)
			{
				stringBuilder.Append(' ');
				flag = false;
			}
			stringBuilder.Append(c);
		}
		return stringBuilder.ToString();
	}
}
