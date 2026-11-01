using System;
using System.IO;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Ornaments;

public class OrnamentDescriptor : DatabaseObject<OrnamentDescriptor>, IFolderable
{
	public const string FramePart = "cadre";

	public const string EmblemPart = "embleme";

	public const string GlowPart = "lueur";

	public const string LeftPart = "gauche";

	public const string RightPart = "droite";

	public const string TopPart = "sommet";

	public const string BottomPart = "socle";

	public static readonly string[] Parts = new string[7] { "cadre", "embleme", "lueur", "gauche", "droite", "sommet", "socle" };

	public const string TexturePrefix = "ornement_";

	public string Style { get; set; } = string.Empty;

	public int UnlockLevel { get; set; }

	public string Description { get; set; } = string.Empty;

	public string Folder { get; set; } = string.Empty;

	[JsonConstructor]
	public OrnamentDescriptor(Guid id)
		: base(id)
	{
		base.Name = "Nouvel ornement";
	}

	public OrnamentDescriptor()
	{
		base.Name = "Nouvel ornement";
	}

	public static string TextureName(string style, string part)
	{
		return $"{"ornement_"}{style}_{part}.png";
	}

	public static string? StyleFromFileName(string fileName)
	{
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
		if (!fileNameWithoutExtension.StartsWith("ornement_", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		string[] parts = Parts;
		foreach (string text in parts)
		{
			string text2 = "_" + text;
			if (fileNameWithoutExtension.EndsWith(text2, StringComparison.OrdinalIgnoreCase) && fileNameWithoutExtension.Length > "ornement_".Length + text2.Length)
			{
				return fileNameWithoutExtension.Substring("ornement_".Length, fileNameWithoutExtension.Length - "ornement_".Length - text2.Length);
			}
		}
		return null;
	}
}
