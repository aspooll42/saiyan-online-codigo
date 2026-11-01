using System;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;

namespace Intersect.Client.Entities;

internal static class TexteMonde
{
	public static float Echelle => 1f / MathF.Max(0.25f, Graphics.WorldScale);

	public static int Taille(int points)
	{
		return Math.Max(1, (int)MathF.Round((float)points * MathF.Max(0.25f, Graphics.WorldScale)));
	}

	public static void Lisse(Action dessin)
	{
		GameRenderer renderer = Graphics.Renderer;
		if (renderer == null)
		{
			return;
		}
		bool linearFiltering = renderer.LinearFiltering;
		renderer.LinearFiltering = true;
		try
		{
			dessin();
		}
		finally
		{
			renderer.LinearFiltering = linearFiltering;
		}
	}

	public static Vector2 Mesurer(string? texte, IFont? police, int taille)
	{
		GameRenderer renderer = Graphics.Renderer;
		if (renderer == null || string.IsNullOrEmpty(texte))
		{
			return default(Vector2);
		}
		return renderer.MeasureText(texte, police, taille, Echelle);
	}

	public static float Caler(float monde, float origine)
	{
		return origine + MathF.Round((monde - origine) * MathF.Max(0.25f, Graphics.WorldScale)) / MathF.Max(0.25f, Graphics.WorldScale);
	}

	public static float CalerX(float monde)
	{
		return Caler(monde, Graphics.CurrentView.X);
	}

	public static float CalerY(float monde)
	{
		return Caler(monde, Graphics.CurrentView.Y);
	}

	public static void Ecrire(string? texte, IFont? police, int taille, float x, float y, Color couleur, Color? contour = null)
	{
		GameRenderer renderer = Graphics.Renderer;
		if (renderer != null && !string.IsNullOrEmpty(texte) && couleur.A > 0)
		{
			FloatRect currentView = Graphics.CurrentView;
			renderer.DrawString(clipRect: new FloatRect(0f, 0f, renderer.ScreenWidth, renderer.ScreenHeight), text: texte, gameFont: police, size: taille, x: CalerX(x) - currentView.X, y: CalerY(y) - currentView.Y, fontScale: Echelle, fontColor: couleur, worldPos: true, renderTexture: null, borderColor: ((object)contour != null && contour.A > 0) ? contour : null);
		}
	}
}
