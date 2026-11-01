using System;
using Intersect.Client.Dungeons;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Framework.Core.GameObjects.Dungeons;

namespace Intersect.Client.Interface.Game.Dungeons;

internal static class DungeonMedal
{
	private static readonly Color Back = new Color(255, 10, 18, 32);

	private static readonly Color BackHot = new Color(255, 17, 26, 44);

	public static void Draw(Base renderer, CharteScreen.Design d, Rectangle rect, DungeonAchievementInfo info, bool dimmed, bool hot, float alpha)
	{
		bool obtained = info.Obtained;
		if (obtained)
		{
			TechniqueArt.FillVertical(renderer, rect, TechniqueArt.Alpha(TechniqueArt.Gold, 0.22f * alpha), TechniqueArt.Alpha(TechniqueArt.Gold, 0.06f * alpha));
			TechniqueArt.Fill(renderer, rect, TechniqueArt.Alpha(Back, 0.55f * alpha));
			TechniqueArt.FillVertical(renderer, rect, TechniqueArt.Alpha(TechniqueArt.Gold, 0.18f * alpha), TechniqueArt.Alpha(TechniqueArt.Gold, 0.04f * alpha));
		}
		else
		{
			TechniqueArt.Fill(renderer, rect, TechniqueArt.Alpha(hot ? BackHot : Back, alpha));
		}
		Color color = (obtained ? TechniqueArt.Gold : (hot ? TechniqueArt.Soft : (info.Custom ? TechniqueArt.Alpha(TechniqueArt.Red, 0.6f) : TechniqueArt.Line2)));
		TechniqueArt.Outline(renderer, rect, TechniqueArt.Alpha(color, alpha));
		float alpha2 = alpha * (obtained ? 1f : (dimmed ? 0.28f : 0.85f));
		int num = Math.Min(rect.Width, rect.Height) * 26 / 40;
		DrawIcon(box: new Rectangle(rect.X + (rect.Width - num) / 2, rect.Y + (rect.Height - num) / 2, num, num), renderer: renderer, d: d, info: info, alpha: alpha2);
		if (obtained)
		{
			int num2 = Math.Max(4, rect.Width * 8 / 40);
			DungeonHud.Diamond(renderer, rect.Right - num2 - d.L(3f), rect.Bottom - num2 / 2 - d.L(3f), num2, TechniqueArt.Alpha(TechniqueArt.Gold, alpha));
		}
	}

	private static void DrawIcon(Base renderer, CharteScreen.Design d, Rectangle box, DungeonAchievementInfo info, float alpha)
	{
		int num = box.X + box.Width / 2;
		int num2 = box.Y + box.Height / 2;
		float num3 = (float)box.Width / 26f;
		switch (info.Feat)
		{
		case DungeonFeat.CompleteNormal:
		case DungeonFeat.CompleteHard:
		case DungeonFeat.CompleteHeroic:
		{
			int num9 = (int)(info.Feat + 1);
			Color color2 = TechniqueArt.Alpha(Dungeon.DifficultyColor((DungeonDifficulty)info.Feat), alpha);
			int num10 = (int)MathF.Round(9f * num3);
			int num11 = (int)MathF.Round(1f * num3);
			int num12 = num9 * num10 + (num9 - 1) * num11;
			for (int j = 0; j < num9; j++)
			{
				DungeonHud.Diamond(renderer, num - num12 / 2 + j * (num10 + num11), num2, num10, color2);
			}
			return;
		}
		case DungeonFeat.NoBeans:
		{
			IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Item, "haricot_magique.png");
			int num13 = (int)MathF.Round(24f * num3);
			SaiyanHud.Draw(renderer, texture, num - num13 / 2, num2 - num13 / 2, num13, num13, TechniqueArt.Alpha(TechniqueArt.White, alpha));
			int num14 = Math.Max(2, (int)MathF.Round(2.5f * num3));
			int num15 = (int)MathF.Round(22f * num3);
			for (int k = 0; k < num15; k++)
			{
				int x = num - num15 / 2 + k;
				int num16 = num2 + num15 / 2 - k * 5 / 6 - num15 / 12;
				TechniqueArt.Fill(renderer, new Rectangle(x, num16 - num14 / 2, 2, num14), TechniqueArt.Alpha(TechniqueArt.Red, alpha));
			}
			return;
		}
		case DungeonFeat.RoomUnderSeconds:
			if (HasSprite(info))
			{
				break;
			}
			goto case DungeonFeat.UnderTarget;
		case DungeonFeat.UnderTarget:
		{
			Color color3 = TechniqueArt.Alpha(TechniqueArt.Cyan, alpha);
			Ring(renderer, num, num2, 10f * num3, Math.Max(2, (int)MathF.Round(2f * num3)), color3);
			TechniqueArt.Fill(renderer, new Rectangle(num - (int)num3, num2 - (int)MathF.Round(7f * num3), Math.Max(2, (int)MathF.Round(2f * num3)), (int)MathF.Round(8f * num3)), color3);
			TechniqueArt.Fill(renderer, new Rectangle(num, num2 - (int)num3, (int)MathF.Round(6f * num3), Math.Max(2, (int)MathF.Round(2f * num3))), color3);
			return;
		}
		case DungeonFeat.Solo:
		{
			Color color = TechniqueArt.Alpha(TechniqueArt.Text, alpha);
			Disc(renderer, num, box.Y + (int)MathF.Round(6.5f * num3), 4.5f * num3, color);
			int num4 = box.Y + (int)MathF.Round(13f * num3);
			float num5 = 18f * num3;
			int num6 = (int)MathF.Round(11f * num3);
			for (int i = 0; i < num6; i++)
			{
				float num7 = MathF.Max(0f, 4 - i) * num3;
				int num8 = (int)MathF.Round(num5 - num7 * 1.6f);
				TechniqueArt.Fill(renderer, new Rectangle(num - num8 / 2, num4 + i, num8, 1), color);
			}
			return;
		}
		case DungeonFeat.Regular:
			TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, d.Pt((float)(15 * box.Width) / (26f * d.S)), $"×{10}", num, num2, TechniqueArt.Alpha(TechniqueArt.Text, alpha));
			return;
		}
		if (HasSprite(info))
		{
			Face(renderer, info.Sprite, box, alpha);
		}
		else
		{
			DungeonHud.Diamond(renderer, num - box.Width / 3, num2, box.Width * 2 / 3, TechniqueArt.Alpha(TechniqueArt.Red, alpha));
		}
	}

	private static bool HasSprite(DungeonAchievementInfo info)
	{
		if (info.Custom)
		{
			return !string.IsNullOrWhiteSpace(info.Sprite);
		}
		return false;
	}

	private static void Face(Base renderer, string sprite, Rectangle box, float alpha)
	{
		(IGameTexture, float, float, float, float)? tuple = DungeonEntryWindow.StandingFrame(sprite);
		if (tuple.HasValue)
		{
			(IGameTexture, float, float, float, float) valueOrDefault = tuple.GetValueOrDefault();
			(IGameTexture, float, float, float, float) tuple2 = valueOrDefault;
			IGameTexture item = tuple2.Item1;
			float item2 = tuple2.Item2;
			float item3 = tuple2.Item3;
			float item4 = tuple2.Item4;
			float item5 = tuple2.Item5;
			float num = (float)box.Width * 1.7f / item4;
			int num2 = (int)(item4 * num);
			int num3 = (int)(item5 * num);
			Rectangle previous = TechniqueArt.BeginClip(renderer, box);
			SaiyanHud.Draw(renderer, item, box.X + (box.Width - num2) / 2, box.Y - num3 / 5, num2, num3, TechniqueArt.Alpha(TechniqueArt.White, alpha), item2 / (float)item.Width, item3 / (float)item.Height, (item2 + item4) / (float)item.Width, (item3 + item5) / (float)item.Height);
			TechniqueArt.EndClip(renderer, previous);
		}
	}

	public static void Disc(Base renderer, int centerX, int centerY, float radius, Color color)
	{
		int num = (int)MathF.Floor(0f - radius);
		int num2 = (int)MathF.Ceiling(radius);
		for (int i = num; i < num2; i++)
		{
			float num3 = (float)i + 0.5f;
			float num4 = MathF.Sqrt(MathF.Max(0f, radius * radius - num3 * num3));
			int num5 = (int)MathF.Round(num4 * 2f);
			if (num5 > 0)
			{
				TechniqueArt.Fill(renderer, new Rectangle(centerX - num5 / 2, centerY + i, num5, 1), color);
			}
		}
	}

	public static void Ring(Base renderer, int centerX, int centerY, float radius, int thickness, Color color)
	{
		float num = radius - (float)thickness;
		int num2 = (int)MathF.Floor(0f - radius);
		int num3 = (int)MathF.Ceiling(radius);
		for (int i = num2; i < num3; i++)
		{
			float num4 = (float)i + 0.5f;
			float x = MathF.Sqrt(MathF.Max(0f, radius * radius - num4 * num4));
			float x2 = ((MathF.Abs(num4) < num) ? MathF.Sqrt(MathF.Max(0f, num * num - num4 * num4)) : 0f);
			int num5 = (int)MathF.Round(x);
			int num6 = (int)MathF.Round(x2);
			if (num5 > 0)
			{
				if (num6 <= 0)
				{
					TechniqueArt.Fill(renderer, new Rectangle(centerX - num5, centerY + i, num5 * 2, 1), color);
					continue;
				}
				TechniqueArt.Fill(renderer, new Rectangle(centerX - num5, centerY + i, num5 - num6, 1), color);
				TechniqueArt.Fill(renderer, new Rectangle(centerX + num6, centerY + i, num5 - num6, 1), color);
			}
		}
	}
}
