using System;

namespace Intersect.Client.Framework;

public static class Chrono
{
	public static Action? Depart;

	public static Action<string>? Etape;

	public static Action<string>? Note;

	public static void Commencer()
	{
		Depart?.Invoke();
	}

	public static void Tour(string nom)
	{
		Etape?.Invoke(nom);
	}

	public static void Noter(string note)
	{
		Note?.Invoke(note);
	}
}
