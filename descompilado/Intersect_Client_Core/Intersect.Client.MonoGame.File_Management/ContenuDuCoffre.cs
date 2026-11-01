using System;
using System.IO;
using Intersect.Client.Framework.File_Management;
using Microsoft.Xna.Framework.Content;

namespace Intersect.Client.MonoGame.File_Management;

public sealed class ContenuDuCoffre : ContentManager
{
	public ContenuDuCoffre(IServiceProvider services)
		: base(services)
	{
	}

	protected override Stream OpenStream(string assetName)
	{
		if (Coffre.Actif)
		{
			string chemin = Path.Combine(base.RootDirectory ?? string.Empty, assetName) + ".xnb";
			if (Coffre.Existe(chemin))
			{
				return Coffre.Ouvrir(chemin);
			}
		}
		return base.OpenStream(assetName);
	}
}
