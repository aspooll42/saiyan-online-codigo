using System;
using Intersect.Client.Core;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Techniques;

namespace Intersect.Client.Interface.Game.Clan;

internal static class ClanEmbleme
{
	public const int Pictos = 12;

	public static readonly Color[] Couleurs = new Color[8]
	{
		new Color(255, 255, 148, 42),
		new Color(255, 232, 72, 72),
		new Color(255, 74, 136, 255),
		new Color(255, 86, 204, 110),
		new Color(255, 176, 96, 240),
		new Color(255, 255, 206, 72),
		new Color(255, 72, 214, 228),
		new Color(255, 214, 220, 232)
	};

	public static Color Couleur(int index)
	{
		return Couleurs[Math.Clamp(index, 0, Couleurs.Length - 1)];
	}

	public static void Dessiner(Base renderer, Rectangle rect, int embleme, int couleur, Color? teinteEcusson = null, float alpha = 1f)
	{
		bool flag = rect.Width > 48;
		IGameTexture gameTexture = TechniqueArt.Tex(flag ? "clan_ecusson_hd.png" : "clan_ecusson.png") ?? TechniqueArt.Tex("clan_ecusson.png");
		IGameTexture gameTexture2 = TechniqueArt.Tex(flag ? "clan_emblemes_hd.png" : "clan_emblemes.png") ?? TechniqueArt.Tex("clan_emblemes.png");
		Color color = TechniqueArt.Alpha(teinteEcusson ?? Couleur(couleur), alpha);
		if (gameTexture != null)
		{
			SaiyanHud.Draw(renderer, gameTexture, rect.X, rect.Y, rect.Width, rect.Height, color);
		}
		if (gameTexture2 != null)
		{
			int num = Math.Clamp(embleme, 0, 11);
			int num2 = (int)Math.Round((float)rect.Width * 0.56f);
			int x = rect.X + (rect.Width - num2) / 2;
			int y = rect.Y + (int)Math.Round((float)rect.Height * 0.44f) - num2 / 2;
			SaiyanHud.Draw(renderer, gameTexture2, x, y, num2, num2, TechniqueArt.Alpha(TechniqueArt.White, alpha), (float)num / 12f, 0f, (float)(num + 1) / 12f);
		}
	}

	public static void DessinerPicto(Base renderer, Rectangle rect, int embleme, Color teinte)
	{
		IGameTexture gameTexture = TechniqueArt.Tex("clan_emblemes_hd.png") ?? TechniqueArt.Tex("clan_emblemes.png");
		if (gameTexture != null)
		{
			int num = Math.Clamp(embleme, 0, 11);
			SaiyanHud.Draw(renderer, gameTexture, rect.X, rect.Y, rect.Width, rect.Height, teinte, (float)num / 12f, 0f, (float)(num + 1) / 12f);
		}
	}

	public static void DessinerMonde(float x, float y, float cote, int embleme, int couleur)
	{
		IGameTexture gameTexture = TechniqueArt.Tex("clan_ecusson.png");
		IGameTexture gameTexture2 = TechniqueArt.Tex("clan_emblemes.png");
		if (gameTexture != null && gameTexture2 != null && Graphics.Renderer != null)
		{
			Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, gameTexture.Width, gameTexture.Height), new FloatRect(x, y, cote, cote), Couleur(couleur));
			int num = Math.Clamp(embleme, 0, 11);
			float num2 = cote * 0.56f;
			Graphics.DrawGameTexture(gameTexture2, new FloatRect(num * gameTexture2.Height, 0f, gameTexture2.Height, gameTexture2.Height), new FloatRect(x + (cote - num2) / 2f, y + cote * 0.44f - num2 / 2f, num2, num2), Color.White);
		}
	}
}
