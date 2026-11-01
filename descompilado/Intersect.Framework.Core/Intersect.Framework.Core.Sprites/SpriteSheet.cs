using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Intersect.Framework.Core.IO;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.Sprites;

public sealed class SpriteSheet
{
	private static readonly string[][] DirectionNames = new string[4][]
	{
		new string[2] { "down", "bas" },
		new string[2] { "left", "gauche" },
		new string[2] { "right", "droite" },
		new string[2] { "up", "haut" }
	};

	private static readonly Dictionary<string, (DateTime Stamp, SpriteSheet? Sheet)> Cache = new Dictionary<string, (DateTime, SpriteSheet)>(StringComparer.OrdinalIgnoreCase);

	public int FrameWidth { get; set; } = 32;

	public int FrameHeight { get; set; } = 32;

	public List<string> Rows { get; set; }

	public List<SpriteSheetAnimation> Animations { get; set; }

	public static SpriteSheet? ForTexture(string directory, string? textureName)
	{
		if (string.IsNullOrWhiteSpace(textureName))
		{
			return null;
		}
		string text = Path.Combine(directory, Path.ChangeExtension(Path.GetFileName(textureName), ".json"));
		lock (Cache)
		{
			DateTime dateTime = (LectureDesRessources.Existe(text) ? LectureDesRessources.DateUtc(text) : DateTime.MinValue);
			if (Cache.TryGetValue(text, out (DateTime, SpriteSheet) value) && value.Item1 == dateTime)
			{
				return value.Item2;
			}
			SpriteSheet spriteSheet = null;
			if (dateTime != DateTime.MinValue)
			{
				try
				{
					spriteSheet = JsonConvert.DeserializeObject<SpriteSheet>(LectureDesRessources.LireTexte(text), new JsonSerializerSettings
					{
						ObjectCreationHandling = ObjectCreationHandling.Replace
					});
					spriteSheet?.Normalize();
				}
				catch (IOException)
				{
					return null;
				}
				catch (Exception)
				{
					spriteSheet = null;
				}
			}
			Cache[text] = (dateTime, spriteSheet);
			return spriteSheet;
		}
	}

	public List<SpriteSheetAnimation> For(string animation)
	{
		return Animations.Where((SpriteSheetAnimation band) => string.Equals(band.Animation, animation, StringComparison.OrdinalIgnoreCase)).ToList();
	}

	public int WidthOf(SpriteSheetAnimation band)
	{
		return band.FrameWidth ?? FrameWidth;
	}

	public int HeightOf(SpriteSheetAnimation band)
	{
		return band.FrameHeight ?? FrameHeight;
	}

	public int RowFor(int engineRow)
	{
		if (engineRow < 0 || engineRow >= DirectionNames.Length)
		{
			return 0;
		}
		for (int i = 0; i < Rows.Count; i++)
		{
			if (DirectionNames[engineRow].Contains(Rows[i].Trim().ToLowerInvariant()))
			{
				return i;
			}
		}
		return Math.Min(engineRow, Math.Max(0, Rows.Count - 1));
	}

	public (int X, int Y, int Width, int Height) FrameRect(SpriteSheetAnimation band, int frame, int engineRow)
	{
		int num = WidthOf(band);
		int num2 = HeightOf(band);
		frame = Math.Clamp(frame, 0, band.Frames - 1);
		return (X: band.X + frame * num, Y: band.Y + RowFor(engineRow) * num2, Width: num, Height: num2);
	}

	public (int X, int Y, int Width, int Height) PreviewRect()
	{
		SpriteSheetAnimation spriteSheetAnimation = For("Idle").FirstOrDefault() ?? For("Normal").FirstOrDefault() ?? Animations.FirstOrDefault();
		if (spriteSheetAnimation != null)
		{
			return FrameRect(spriteSheetAnimation, 0, 0);
		}
		return (X: 0, Y: 0, Width: FrameWidth, Height: FrameHeight);
	}

	private void Normalize()
	{
		FrameWidth = Math.Max(1, FrameWidth);
		FrameHeight = Math.Max(1, FrameHeight);
		List<string> rows = Rows;
		List<string> list;
		if (rows == null || rows.Count <= 0)
		{
			int num = 4;
			list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "down";
			num2++;
			span[num2] = "left";
			num2++;
			span[num2] = "right";
			num2++;
			span[num2] = "up";
			num2++;
		}
		else
		{
			list = Rows;
		}
		Rows = list;
		if (Animations == null)
		{
			List<SpriteSheetAnimation> list2 = (Animations = new List<SpriteSheetAnimation>());
		}
		foreach (SpriteSheetAnimation animation in Animations)
		{
			animation.Frames = Math.Max(1, animation.Frames);
			int? frameWidth = animation.FrameWidth;
			animation.FrameWidth = ((frameWidth.HasValue && frameWidth.GetValueOrDefault() > 0) ? animation.FrameWidth : ((int?)null));
			frameWidth = animation.FrameHeight;
			animation.FrameHeight = ((frameWidth.HasValue && frameWidth.GetValueOrDefault() > 0) ? animation.FrameHeight : ((int?)null));
		}
	}

	public SpriteSheet()
	{
		int num = 4;
		List<string> list = new List<string>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<string> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = "down";
		num2++;
		span[num2] = "left";
		num2++;
		span[num2] = "right";
		num2++;
		span[num2] = "up";
		num2++;
		Rows = list;
		Animations = new List<SpriteSheetAnimation>();
	}
}
