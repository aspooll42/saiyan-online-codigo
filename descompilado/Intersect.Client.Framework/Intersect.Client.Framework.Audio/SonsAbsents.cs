using System;
using System.Collections.Generic;
using Intersect.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Framework.Audio;

public static class SonsAbsents
{
	private static readonly HashSet<string> DejaSignales = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	public static void Signaler(string? nom)
	{
		if (string.IsNullOrWhiteSpace(nom))
		{
			return;
		}
		lock (DejaSignales)
		{
			if (!DejaSignales.Add(nom))
			{
				return;
			}
		}
		ApplicationContext.Context.Value?.Logger.LogWarning("Son introuvable : « {Son} » est demandé mais absent du dossier sounds", nom);
	}
}
