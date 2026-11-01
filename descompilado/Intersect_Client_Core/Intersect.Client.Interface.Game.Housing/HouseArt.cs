using System;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Housing;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Framework.Core.GameObjects.Housing;
using Intersect.Localization;

namespace Intersect.Client.Interface.Game.Housing;

internal static class HouseArt
{
	public readonly record struct PlateContent(string Title, Color TitleColor, string Detail, Color DetailColor, bool ForSale, bool Locked);

	public static readonly Color Page = new Color(255, 4, 6, 11);

	public static readonly Color Dark = new Color(255, 20, 13, 2);

	public static readonly Color KeyBack = new Color(255, 17, 24, 39);

	public static readonly Color KiLight = new Color(255, 255, 179, 92);

	public static readonly Color ButtonBack = new Color(242, 10, 14, 24);

	public static Color AccessColor(HouseAccess access)
	{
		return access switch
		{
			HouseAccess.FriendsAndGuild => TechniqueArt.Cyan, 
			HouseAccess.Locked => TechniqueArt.Red, 
			_ => TechniqueArt.Green, 
		};
	}

	public static void Window(Base renderer, Rectangle rect, float alpha = 1f)
	{
		TechniqueArt.Patch(renderer, "tech_chanfrein10.png", rect, TechniqueArt.Alpha(new Color(247, 14, 20, 33), alpha), 12);
		TechniqueArt.Patch(renderer, "tech_chanfrein10_degrade.png", rect, TechniqueArt.Alpha(new Color(255, 18, 26, 42), alpha), 12);
		TechniqueArt.Patch(renderer, "tech_chanfrein10_bord.png", rect, TechniqueArt.Alpha(TechniqueArt.Line2, alpha), 12);
	}

	public static int WindowHeader(Base renderer, CharteScreen.Design d, Rectangle window, string title, string? detail, float alpha = 1f)
	{
		int num = d.L(56f);
		string text = title.ToUpperInvariant();
		int size = d.Pt(28f);
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, size, text);
		int num2 = window.Y + d.L(16f) + d.L(28f) / 2;
		TechniqueArt.Styled(renderer, TechniqueArt.Display, size, text, window.X + d.L(18f), num2 - point.Y / 2, new TechniqueArt.Stops(TechniqueArt.White, 0.35f, new Color(255, 255, 194, 122), 0.8f, TechniqueArt.Ki), alpha);
		if (!string.IsNullOrEmpty(detail))
		{
			TechniqueArt.SpacedRight(renderer, TechniqueArt.Label, d.Pt(12f), detail.ToUpperInvariant(), window.Right - d.L(18f), num2 + d.L(4f), d.Em(0.14f, 12f), TechniqueArt.Alpha(TechniqueArt.Dim, alpha));
		}
		TechniqueArt.Fill(renderer, new Rectangle(window.X + 1, window.Y + num, window.Width - 2, 1), TechniqueArt.Alpha(TechniqueArt.White, 0.06f * alpha));
		return window.Y + num;
	}

	public static void Cta(Base renderer, CharteScreen.Design d, Rectangle rect, string label, bool enabled, bool hot, float alpha = 1f)
	{
		string texture = ((!enabled) ? "tech_bouton_eteint.png" : (hot ? "tech_bouton_survol.png" : "tech_bouton.png"));
		TechniqueArt.Patch(renderer, texture, rect, TechniqueArt.Alpha(TechniqueArt.White, alpha), 14);
		TechniqueArt.SpacedCenter(renderer, TechniqueArt.Heavy, d.Pt(20f), label.ToUpperInvariant(), rect.X + rect.Width / 2, rect.Y + rect.Height / 2, d.Em(0.06f, 20f), TechniqueArt.Alpha(enabled ? Dark : TechniqueArt.Dim, alpha));
	}

	public static void Hollow(Base renderer, CharteScreen.Design d, Rectangle rect, string label, bool hot, float alpha = 1f, Color? color = null)
	{
		TechniqueArt.Fill(renderer, rect, TechniqueArt.Alpha(ButtonBack, alpha));
		TechniqueArt.Fill(renderer, rect, TechniqueArt.Alpha(color ?? TechniqueArt.White, (hot ? 0.16f : 0.06f) * alpha));
		TechniqueArt.Outline(renderer, rect, TechniqueArt.Alpha(color ?? (hot ? TechniqueArt.Soft : TechniqueArt.Line2), alpha));
		TechniqueArt.TextCenter(renderer, TechniqueArt.Label, d.Pt(14f), label, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, TechniqueArt.Alpha(color ?? (hot ? TechniqueArt.Text : TechniqueArt.Soft), alpha));
	}

	public static int KeyCapWidth(Base renderer, CharteScreen.Design d, string text)
	{
		return Math.Max(d.L(30f), TechniqueArt.Measure(renderer, TechniqueArt.Label, d.Pt(13f), text).X + d.L(14f));
	}

	public static int KeyCap(Base renderer, CharteScreen.Design d, int x, int centerY, string text, float alpha = 1f)
	{
		int num = KeyCapWidth(renderer, d, text);
		int num2 = d.L(26f);
		Rectangle rect = new Rectangle(x, centerY - num2 / 2, num, num2);
		TechniqueArt.Fill(renderer, rect, TechniqueArt.Alpha(KeyBack, alpha));
		TechniqueArt.Outline(renderer, rect, TechniqueArt.Alpha(TechniqueArt.Line2, alpha));
		TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Bottom - d.L(3f), rect.Width, d.L(3f)), TechniqueArt.Alpha(TechniqueArt.Line2, alpha));
		TechniqueArt.TextCenter(renderer, TechniqueArt.Label, d.Pt(13f), text, rect.X + rect.Width / 2, rect.Y + (rect.Height - d.L(2f)) / 2, TechniqueArt.Alpha(TechniqueArt.Text, alpha));
		return num;
	}

	public static void Zeni(Base renderer, Rectangle rect, int frame = 0, float alpha = 1f)
	{
		IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Item, "zeni_anime.png");
		if (texture != null)
		{
			float num = (float)((frame % 4 + 4) % 4) / 4f;
			SaiyanHud.Draw(renderer, texture, rect.X, rect.Y, rect.Width, rect.Height, TechniqueArt.Alpha(TechniqueArt.White, alpha), num, 0f, num + 0.25f);
		}
	}

	public static void Padlock(Base renderer, Rectangle rect, Color color)
	{
		float unit = Math.Max(1f, (float)rect.Width / 12f);
		TechniqueArt.Fill(renderer, new Rectangle(rect.X + U(2f), rect.Y + U(5f), U(8f), U(6f)), color);
		TechniqueArt.Fill(renderer, new Rectangle(rect.X + U(3.2f), rect.Y + U(1.5f), Math.Max(1, U(1.6f)), U(3.5f)), color);
		TechniqueArt.Fill(renderer, new Rectangle(rect.X + U(7.2f), rect.Y + U(1.5f), Math.Max(1, U(1.6f)), U(3.5f)), color);
		TechniqueArt.Fill(renderer, new Rectangle(rect.X + U(3.2f), rect.Y + U(1f), U(5.6f), Math.Max(1, U(1.6f))), color);
		int U(float value)
		{
			return (int)MathF.Round(value * unit);
		}
	}

	public static PlateContent PlateOf(Houses.Plot plot)
	{
		HouseStateInfo state = plot.State;
		if (state != null)
		{
			return new PlateContent(Strings.Housing.PlateOwner.ToString(state.OwnerName), TechniqueArt.Text, Houses.AccessText(state.Access), AccessColor(state.Access), ForSale: false, state.Access == HouseAccess.Locked);
		}
		LocalizedString localizedString = (plot.HasHouse ? Strings.Housing.PlateHouseForSale : Strings.Housing.PlateForSale);
		string detail = Strings.Housing.PlateSizePrice.ToString(plot.Terrain.Width, plot.Terrain.Height, MarketWindow.FormatAmount(plot.Terrain.Price));
		return new PlateContent(localizedString, TechniqueArt.Gold, detail, TechniqueArt.GoldPale, ForSale: true, Locked: false);
	}

	public static Rectangle Plate(Base renderer, CharteScreen.Design d, int centerX, int bottom, PlateContent content, float alpha = 1f, IGameTexture? icon = null)
	{
		string text = content.Title.ToUpperInvariant();
		string text2 = content.Detail.ToUpperInvariant();
		float spacing = d.Em(0.08f, 16f);
		float spacing2 = d.Em(0.12f, 12f);
		int num = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Heavy, d.Pt(16f), text, spacing);
		int num2 = (content.Locked ? d.L(16f) : 0);
		int num3 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, d.Pt(12f), text2, spacing2) + num2;
		int num4 = Math.Max(num, num3);
		int num5 = ((icon != null && icon.Width > 0) ? d.L(26f) : 0);
		int num6 = ((num5 > 0) ? d.L(10f) : 0);
		int num7 = num5 + num6 + num4 + d.L(24f);
		int num8 = d.L(44f);
		Rectangle rectangle = new Rectangle(centerX - num7 / 2, bottom - num8, num7, num8);
		TechniqueArt.Patch(renderer, "tech_chanfrein8.png", rectangle, TechniqueArt.Alpha(new Color(219, 6, 8, 14), alpha), 10);
		TechniqueArt.Patch(renderer, "tech_chanfrein8_bord.png", rectangle, TechniqueArt.Alpha(content.ForSale ? new Color(153, 255, 208, 106) : TechniqueArt.Line2, alpha), 10);
		if (num5 > 0)
		{
			SaiyanHud.Draw(renderer, icon, rectangle.X + d.L(12f), rectangle.Y + (num8 - num5) / 2, num5, num5, TechniqueArt.Alpha(Color.White, alpha));
		}
		int num9 = rectangle.X + d.L(12f) + num5 + num6 + num4 / 2;
		TechniqueArt.Spaced(renderer, TechniqueArt.Heavy, d.Pt(16f), text, num9 - num / 2, rectangle.Y + d.L(14.5f), spacing, TechniqueArt.Alpha(content.TitleColor, alpha));
		int num10 = num9 - num3 / 2;
		int num11 = rectangle.Y + d.L(31f);
		if (content.Locked)
		{
			Padlock(renderer, new Rectangle(num10, num11 - d.L(6f), d.L(11f), d.L(11f)), TechniqueArt.Alpha(content.DetailColor, alpha));
		}
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, d.Pt(12f), text2, num10 + num2, num11, spacing2, TechniqueArt.Alpha(content.DetailColor, alpha));
		return rectangle;
	}

	public static void Pill(Base renderer, CharteScreen.Design d, int centerX, int bottom, string key, string text, string? detail, bool refused, float alpha = 1f)
	{
		int x = TechniqueArt.Measure(renderer, TechniqueArt.Label, d.Pt(16f), text).X;
		int val = ((!string.IsNullOrEmpty(detail)) ? TechniqueArt.Measure(renderer, TechniqueArt.Label, d.Pt(12f), detail).X : 0);
		int num = Math.Max(d.L(30f), TechniqueArt.Measure(renderer, TechniqueArt.Label, d.Pt(13f), key).X + d.L(14f));
		int num2 = (string.IsNullOrEmpty(detail) ? d.L(40f) : d.L(52f));
		int num3 = d.L(8f) + num + d.L(10f) + Math.Max(x, val) + d.L(14f);
		Rectangle rect = new Rectangle(centerX - num3 / 2, bottom - num2, num3, num2);
		TechniqueArt.Fill(renderer, rect, TechniqueArt.Alpha(new Color(230, 6, 8, 14), alpha));
		TechniqueArt.Outline(renderer, rect, TechniqueArt.Alpha(refused ? new Color(153, 255, 85, 102) : new Color(140, 255, 208, 106), alpha));
		int num4 = rect.Y + rect.Height / 2;
		KeyCap(renderer, d, rect.X + d.L(8f), num4, key, alpha);
		int x2 = rect.X + d.L(8f) + num + d.L(10f);
		if (string.IsNullOrEmpty(detail))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, d.Pt(16f), text, x2, num4, TechniqueArt.Alpha(TechniqueArt.Text, alpha));
			return;
		}
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, d.Pt(16f), text, x2, num4 - d.L(8f), TechniqueArt.Alpha(refused ? TechniqueArt.Red : TechniqueArt.Text, alpha));
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, d.Pt(12f), detail, x2, num4 + d.L(11f), TechniqueArt.Alpha(TechniqueArt.Soft, alpha));
	}

	public static void FurnitureThumbnail(Base renderer, FurnitureDescriptor furniture, int variant, Rectangle box, float alpha = 1f)
	{
		FurnitureVariant furnitureVariant = furniture.VariantAt(variant);
		IGameTexture gameTexture = HouseWorld.TilesetTexture(furnitureVariant.TilesetId);
		if (gameTexture != null && gameTexture.Width > 0 && gameTexture.Height > 0)
		{
			int tileWidth = Options.Instance.Map.TileWidth;
			int tileHeight = Options.Instance.Map.TileHeight;
			int num = Math.Max(1, furnitureVariant.Width) * tileWidth;
			int num2 = Math.Max(1, furnitureVariant.Height) * tileHeight;
			float num3 = Math.Min((float)box.Width / (float)num, (float)box.Height / (float)num2);
			int num4 = (int)MathF.Round((float)num * num3);
			int num5 = (int)MathF.Round((float)num2 * num3);
			SaiyanHud.Draw(renderer, gameTexture, box.X + (box.Width - num4) / 2, box.Y + (box.Height - num5) / 2, num4, num5, TechniqueArt.Alpha(TechniqueArt.White, alpha), (float)(furnitureVariant.X * tileWidth) / (float)gameTexture.Width, (float)(furnitureVariant.Y * tileHeight) / (float)gameTexture.Height, (float)(furnitureVariant.X * tileWidth + num) / (float)gameTexture.Width, (float)(furnitureVariant.Y * tileHeight + num2) / (float)gameTexture.Height);
		}
	}

	public static void CapsuleImage(Base renderer, CapsuleDescriptor capsule, Rectangle box, float alpha = 1f, bool bottom = false)
	{
		IGameTexture gameTexture = HouseWorld.CapsuleTexture(capsule);
		if (gameTexture != null && gameTexture.Width > 0 && gameTexture.Height > 0)
		{
			float num = Math.Min((float)box.Width / (float)gameTexture.Width, (float)box.Height / (float)gameTexture.Height);
			int num2 = (int)MathF.Round((float)gameTexture.Width * num);
			int num3 = (int)MathF.Round((float)gameTexture.Height * num);
			int y = (bottom ? (box.Bottom - num3) : (box.Y + (box.Height - num3) / 2));
			SaiyanHud.Draw(renderer, gameTexture, box.X + (box.Width - num2) / 2, y, num2, num3, TechniqueArt.Alpha(TechniqueArt.White, alpha));
		}
	}
}
