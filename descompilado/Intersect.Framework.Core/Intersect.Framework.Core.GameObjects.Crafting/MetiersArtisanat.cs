using System;
using Intersect.Models;

namespace Intersect.Framework.Core.GameObjects.Crafting;

public static class MetiersArtisanat
{
	public const int Forgeron = 5;

	public const int Cuisinier = 6;

	public const int Apothicaire = 7;

	public const int Ingenieur = 8;

	public const int Premier = 5;

	public const int Nombre = 4;

	public const int NombreDeMetiers = 9;

	public static bool EstArtisanat(int metier)
	{
		if (metier >= 5)
		{
			return metier < 9;
		}
		return false;
	}

	public static int MetierDeLAtelier(CraftingTableDescriptor? atelier)
	{
		if (atelier == null)
		{
			return -1;
		}
		int[] array = new int[4];
		foreach (Guid craft in atelier.Crafts)
		{
			if (DatabaseObject<CraftingRecipeDescriptor>.TryGet(craft, out var databaseObject) && EstArtisanat(databaseObject.Profession))
			{
				array[databaseObject.Profession - 5]++;
			}
		}
		int num = -1;
		for (int i = 0; i < 4; i++)
		{
			if (array[i] > 0 && (num < 0 || array[i] > array[num]))
			{
				num = i;
			}
		}
		if (num >= 0)
		{
			return 5 + num;
		}
		return -1;
	}
}
