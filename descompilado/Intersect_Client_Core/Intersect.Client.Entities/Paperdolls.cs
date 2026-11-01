using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Framework.Core.GameObjects.PlayerClass;
using Intersect.Models;

namespace Intersect.Client.Entities;

public static class Paperdolls
{
	private static readonly Dictionary<(string Paperdoll, Guid ClassId), string> Resolved = new Dictionary<(string, Guid), string>();

	public static string ForClass(string paperdoll, Guid classId)
	{
		if (string.IsNullOrWhiteSpace(paperdoll) || classId == Guid.Empty || !DatabaseObject<ClassDescriptor>.TryGet(classId, out var databaseObject))
		{
			return paperdoll;
		}
		if (Resolved.TryGetValue((paperdoll, classId), out string value))
		{
			return value;
		}
		string extension = Path.GetExtension(paperdoll);
		string text = Path.GetFileNameWithoutExtension(paperdoll);
		int num = text.LastIndexOf('_');
		if (num > 0)
		{
			string text2 = text;
			int num2 = num + 1;
			string suffix = Key(text2.Substring(num2, text2.Length - num2));
			if (DatabaseObject<ClassDescriptor>.Lookup.Values.OfType<ClassDescriptor>().Any((ClassDescriptor descriptor) => Key(descriptor.Name) == suffix))
			{
				text = text.Substring(0, num);
			}
		}
		string text3 = text + "_" + Key(databaseObject.Name) + extension;
		string text4 = (Exists(text3) ? text3 : paperdoll);
		Resolved[(paperdoll, classId)] = text4;
		return text4;
	}

	public static string? ForForm(Entity entity, string paperdoll)
	{
		if (string.IsNullOrWhiteSpace(paperdoll) || string.IsNullOrEmpty(entity.TransformedSprite))
		{
			return paperdoll;
		}
		if (!Transformation.KeepsPaperdolls(entity))
		{
			return null;
		}
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(entity.Sprite);
		string fileNameWithoutExtension2 = Path.GetFileNameWithoutExtension(Transformation.TextureNameFor(entity.Sprite, entity.TransformedSprite));
		if (fileNameWithoutExtension2.Length <= fileNameWithoutExtension.Length + 1)
		{
			return paperdoll;
		}
		string? fileNameWithoutExtension3 = Path.GetFileNameWithoutExtension(paperdoll);
		string text = fileNameWithoutExtension2;
		int num = fileNameWithoutExtension.Length + 1;
		string text2 = fileNameWithoutExtension3 + "_" + text.Substring(num, text.Length - num) + Path.GetExtension(paperdoll);
		if (Globals.ContentManager?.GetTexture(TextureType.Paperdoll, text2) == null)
		{
			return paperdoll;
		}
		return text2;
	}

	public static string ForClassNamed(string paperdoll, string? className)
	{
		string text = Key(className);
		if (string.IsNullOrWhiteSpace(paperdoll) || text.Length == 0)
		{
			return paperdoll;
		}
		string extension = Path.GetExtension(paperdoll);
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(paperdoll);
		if (Exists(fileNameWithoutExtension + "_" + text + extension))
		{
			return fileNameWithoutExtension + "_" + text + extension;
		}
		int num = fileNameWithoutExtension.LastIndexOf('_');
		if (num <= 0 || !Exists(fileNameWithoutExtension.Substring(0, num) + "_" + text + extension))
		{
			return paperdoll;
		}
		return fileNameWithoutExtension.Substring(0, num) + "_" + text + extension;
	}

	public static string SansCouleur(string? name)
	{
		if (!string.IsNullOrEmpty(name) && name.Contains("-c", StringComparison.OrdinalIgnoreCase))
		{
			return Regex.Replace(name, "-c\\d+(?=_|\\.|$)", string.Empty);
		}
		return name ?? string.Empty;
	}

	public static string PlancheDesEnveloppes(string? name)
	{
		return CorpsDuCreateur.Commun(SansCouleur(name));
	}

	private static bool Exists(string paperdoll)
	{
		IGameTexture gameTexture = GameContentManager.Current?.GetTexture(TextureType.Paperdoll, paperdoll);
		if (gameTexture != null && gameTexture.Width > 0)
		{
			return gameTexture.Height > 0;
		}
		return false;
	}

	private static string Key(string? name)
	{
		return new string((name ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
	}
}
