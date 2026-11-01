using System;
using System.Globalization;
using System.Linq;
using Intersect.Client.Framework.Database;
using Intersect.Client.General;
using Intersect.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Localization;

public static class Langue
{
	public const string Source = "fr";

	private const string Preference = "Langue";

	public static readonly (string Code, string Nom)[] Disponibles = new(string, string)[5]
	{
		("fr", "Français"),
		("en", "English"),
		("es", "Español"),
		("de", "Deutsch"),
		("it", "Italiano")
	};

	private static bool _lue;

	public static string Actuelle { get; private set; } = "fr";

	public static string Choisie { get; private set; } = "fr";

	public static bool EstSource => Actuelle == "fr";

	public static void Lire()
	{
		if (!_lue)
		{
			_lue = true;
			string code = null;
			try
			{
				code = new JsonDatabase().LoadPreference("Langue");
			}
			catch (Exception exception)
			{
				ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Langue : preferences illisibles");
			}
			Actuelle = Connue(code) ?? Detecter();
			Choisie = Actuelle;
			ApplicationContext.Context.Value?.Logger.LogInformation("Langue : {Langue} ({Origine})", Actuelle, (Connue(code) != null) ? "choisie par le joueur" : "langue de Windows");
		}
	}

	public static void Choisir(string code)
	{
		string text = Connue(code);
		if (text != null)
		{
			Choisie = text;
			Globals.Database?.SavePreference("Langue", text);
		}
	}

	public static int Indice(string code)
	{
		return Array.FindIndex(Disponibles, ((string Code, string Nom) langue) => langue.Code == code);
	}

	private static string? Connue(string? code)
	{
		if (!string.IsNullOrWhiteSpace(code))
		{
			if (!Disponibles.Any(((string Code, string Nom) langue) => langue.Code == code))
			{
				return null;
			}
			return code;
		}
		return null;
	}

	private static string Detecter()
	{
		try
		{
			return Connue(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName) ?? "en";
		}
		catch
		{
			return "en";
		}
	}
}
