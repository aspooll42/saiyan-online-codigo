using System;
using System.IO;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Configuration;
using Intersect.Framework.Core.Sprites;

namespace Intersect.Client.Entities;

public static class SpriteSheets
{
	public readonly record struct Pose(IGameTexture Texture, (int X, int Y, int Width, int Height) Frame)
	{
		public bool IsEmpty
		{
			get
			{
				if (Frame.Width >= 1)
				{
					return Frame.Height < 1;
				}
				return true;
			}
		}
	}

	public static SpriteSheet? ForEntity(string? spriteName)
	{
		return SpriteSheet.ForTexture(Path.Combine(ClientConfiguration.ResourcesDirectory, "entities"), spriteName);
	}

	public static bool TryPortraitFrame(string? spriteName, IGameTexture texture, out (int X, int Y, int Width, int Height) frame)
	{
		SpriteSheet spriteSheet = ForEntity(spriteName);
		if (spriteSheet != null)
		{
			IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Entity, spriteName);
			if (texture2 == null || (texture2.Width == texture.Width && texture2.Height == texture.Height))
			{
				frame = spriteSheet.PreviewRect();
				return true;
			}
			frame = default((int, int, int, int));
			return false;
		}
		frame = (X: 0, Y: 0, Width: texture.Width / Options.Instance.Sprites.NormalFrames, Height: texture.Height / Options.Instance.Sprites.Directions);
		return true;
	}

	public static Pose? StandingBody(string? spriteName, string? bodyName = null, int row = 0)
	{
		bodyName = (string.IsNullOrWhiteSpace(bodyName) ? spriteName : bodyName);
		bodyName = (string.IsNullOrWhiteSpace(bodyName) ? bodyName : Path.GetFileName(bodyName));
		if (!string.IsNullOrWhiteSpace(bodyName))
		{
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Entity, bodyName);
			if (texture != null && texture.Width > 0 && texture.Height > 0)
			{
				int num = Math.Max(1, Options.Instance.Sprites.Directions);
				row = Math.Clamp(row, 0, num - 1);
				SpriteSheet spriteSheet = ForEntity(bodyName);
				if (spriteSheet != null)
				{
					(int, int, int, int) tuple = spriteSheet.PreviewRect();
					int num2 = tuple.Item2 + row * tuple.Item4;
					return new Pose(texture, (X: tuple.Item1, Y: (num2 + tuple.Item4 > texture.Height) ? tuple.Item2 : num2, Width: tuple.Item3, Height: tuple.Item4));
				}
				string name = Path.GetFileNameWithoutExtension(bodyName) + "_idle" + Path.GetExtension(bodyName);
				IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Entity, name);
				if (texture2 != null && texture2.Width > 0 && texture2.Height > 0)
				{
					int num3 = Math.Max(1, Entity.SheetColumns(texture2, texture, Options.Instance.Sprites.IdleFrames));
					int num4 = texture2.Height / num;
					return new Pose(texture2, (X: 0, Y: row * num4, Width: texture2.Width / num3, Height: num4));
				}
				return new Pose(texture, WalkFrame(texture, row));
			}
		}
		return null;
	}

	public static Pose? StandingOutfit(string? spriteName, string? outfitName, Pose body, int row = 0)
	{
		if (!string.IsNullOrWhiteSpace(outfitName))
		{
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Paperdoll, outfitName);
			if (texture != null && texture.Width > 0 && texture.Height > 0)
			{
				int num = Math.Max(1, Options.Instance.Sprites.Directions);
				row = Math.Clamp(row, 0, num - 1);
				if (ForEntity(spriteName) != null && texture.Width == body.Texture.Width && texture.Height == body.Texture.Height)
				{
					return new Pose(texture, body.Frame);
				}
				if (Path.GetFileNameWithoutExtension(body.Texture.Name ?? string.Empty).EndsWith("_idle", StringComparison.OrdinalIgnoreCase))
				{
					IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Paperdoll, Path.GetFileNameWithoutExtension(outfitName) + "_idle" + Path.GetExtension(outfitName));
					if (texture2 != null && texture2.Width > 0 && texture2.Height > 0)
					{
						int num2 = Math.Max(1, Entity.SheetColumns(texture2, texture, Options.Instance.Sprites.IdleFrames));
						int num3 = texture2.Height / num;
						return new Pose(texture2, (X: 0, Y: row * num3, Width: texture2.Width / num2, Height: num3));
					}
				}
				return new Pose(texture, WalkFrame(texture, row));
			}
		}
		return null;
	}

	private static (int X, int Y, int Width, int Height) WalkFrame(IGameTexture texture, int row)
	{
		int num = texture.Height / Math.Max(1, Options.Instance.Sprites.Directions);
		return (X: 0, Y: row * num, Width: texture.Width / Math.Max(1, Options.Instance.Sprites.NormalFrames), Height: num);
	}
}
