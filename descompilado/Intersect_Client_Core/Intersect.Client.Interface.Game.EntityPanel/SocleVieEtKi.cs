using System;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Interface.Game.Techniques;

namespace Intersect.Client.Interface.Game.EntityPanel;

public static class SocleVieEtKi
{
	public struct Jauge
	{
		public float Montre;

		public float Ombre;

		public float Cible;

		public float Tete;

		public bool Tient;
	}

	public struct Charges
	{
		public bool Visibles;

		public int Pleines;

		public int Maximum;

		public float Recharge;

		public bool Espace;

		public string? Touche;

		public float Prise;

		public float Revenue;

		public float Nouvelle;

		public bool Enfoncee;

		public bool Refusee;
	}

	public struct Etat
	{
		public float Echelle;

		public Color? Accent;

		public Color? AccentClair;

		public Jauge Vie;

		public Jauge Ki;

		public Jauge Experience;

		public float Bouclier;

		public string? TexteVie;

		public string? TexteKi;

		public string? TexteExperience;

		public string? LibelleVie;

		public string? LibelleKi;

		public string? LibelleExperience;

		public Charges Dash;

		public Charges Esquive;

		public bool VieBasse;

		public float Battement;

		public float Coup;

		public float NiveauGagne;

		public float EclatExperience;

		public long Maintenant;
	}

	public const int Largeur = 562;

	public const int Hauteur = 86;

	public const float BlasonX = 281f;

	public const float BlasonY = 39.2f;

	public const float BlasonEchelle = 2f / 3f;

	public static bool HorsJeu;

	private static readonly Color Noir = new Color(255, 0, 0, 0);

	private static readonly Color Trait = new Color(255, 59, 69, 99);

	private static readonly HudShapes.Ramp Plaque = new HudShapes.Ramp(new Color(255, 29, 36, 57), new Color(255, 16, 20, 34), new Color(255, 9, 11, 20), 0.45f);

	private static readonly HudShapes.Ramp Epaule = HudShapes.Ramp.Two(new Color(255, 42, 51, 80), new Color(255, 23, 29, 48));

	private static readonly Color PisteVie = new Color(255, 36, 10, 17);

	private static readonly Color BordVie = new Color(255, 90, 34, 44);

	private static readonly HudShapes.Ramp PleinVie = new HudShapes.Ramp(new Color(255, 255, 132, 108), new Color(255, 230, 58, 58), new Color(255, 120, 14, 30), 0.45f);

	private static readonly HudShapes.Ramp PleinKi = new HudShapes.Ramp(new Color(255, 170, 218, 255), new Color(255, 74, 146, 255), new Color(255, 30, 62, 184), 0.45f);

	private static readonly Color CaseVide = new Color(255, 26, 35, 64);

	private static readonly HudShapes.Ramp JetonDash = HudShapes.Ramp.Two(new Color(255, 255, 184, 102), new Color(255, 255, 122, 26));

	private static readonly Color BordDash = new Color(178, 70, 24, 0);

	private static readonly HudShapes.Ramp JetonEsquive = HudShapes.Ramp.Two(new Color(255, 200, 242, 255), new Color(255, 90, 210, 255));

	private static readonly Color BordEsquive = new Color(178, 0, 40, 70);

	private static readonly Color JetonVide = new Color(255, 14, 18, 32);

	private static readonly Color Touche = new Color(255, 42, 51, 82);

	private static readonly Color BordTouche = new Color(255, 82, 96, 140);

	private static readonly Color ToucheRefus = new Color(255, 201, 55, 44);

	private static readonly Color BordToucheRefus = new Color(255, 255, 123, 107);

	private static readonly Color PisteExperience = new Color(255, 26, 33, 53);

	private static readonly Color LibelleKiCouleur = new Color(255, 170, 212, 255);

	private static readonly Color LibelleExperienceCouleur = new Color(255, 232, 182, 90);

	private static readonly int[] Corps = new int[15]
	{
		8, 9, 10, 11, 12, 13, 14, 16, 18, 20,
		22, 24, 26, 28, 32
	};

	private const float EpauleHaut = 21f;

	private const float SocleHaut = 47f;

	private const float SocleBas = 86f;

	private const float VieHaut = 53f;

	private const float VieBas = 75f;

	private const float KiHaut = 57f;

	private const float KiBas = 71f;

	private const float JetonHaut = 27.5f;

	private const float JetonBas = 42.5f;

	private static IFont? PoliceDesValeurs
	{
		get
		{
			if (!HorsJeu)
			{
				return SaiyanHud.StrongFont;
			}
			return null;
		}
	}

	private static IFont? PoliceDesLibelles
	{
		get
		{
			object obj;
			if (!HorsJeu)
			{
				obj = TechniqueArt.Body;
				if (obj == null)
				{
					return SaiyanHud.SmallFont;
				}
			}
			else
			{
				obj = null;
			}
			return (IFont?)obj;
		}
	}

	public static void Dessiner(Base renderer, in Etat etat)
	{
		float z = ((etat.Echelle <= 0f) ? 1f : etat.Echelle);
		Color color = etat.Accent ?? SaiyanHud.BaseAccent.Main;
		Color color2 = etat.AccentClair ?? SaiyanHud.BaseAccent.Light;
		Color color3 = SaiyanHud.Lerp(color, color2, 0.3f);
		HudShapes.Ramp levre = new HudShapes.Ramp(SaiyanHud.Lerp(color2, SaiyanHud.White, 0.12f), SaiyanHud.Lerp(color2, color, 0.5f), color);
		if (etat.VieBasse)
		{
			float t = 0.35f + 0.65f * etat.Battement;
			color3 = SaiyanHud.Lerp(color3, SaiyanHud.Red, t);
			levre = new HudShapes.Ramp(SaiyanHud.Lerp(levre.Top, SaiyanHud.Red, t), SaiyanHud.Lerp(levre.Middle, SaiyanHud.Red, t), SaiyanHud.Lerp(levre.Bottom, SaiyanHud.Red, t));
		}
		Charges dash = etat.Dash;
		bool gauche = dash.Visibles && dash.Maximum > 0;
		dash = etat.Esquive;
		bool droite = dash.Visibles && dash.Maximum > 0;
		DessinerOmbre(renderer, z, gauche, droite);
		DessinerEpaules(renderer, z, color3, gauche, droite);
		DessinerCharges(renderer, z, in etat.Dash, 181.75f, toucheDAbord: true, JetonDash, BordDash, new Color(255, 255, 214, 160));
		DessinerCharges(renderer, z, in etat.Esquive, 380.75f, toucheDAbord: false, JetonEsquive, BordEsquive, new Color(255, 214, 246, 255));
		DessinerSocle(renderer, z, levre, color3, in etat);
		DessinerVie(renderer, z, in etat);
		DessinerKi(renderer, z, in etat);
		DessinerExperience(renderer, z, in etat);
	}

	private static void DessinerOmbre(Base renderer, float z, bool gauche, bool droite)
	{
		(float, float)[] array = new(float, float)[2]
		{
			(2.5f, 0.13f),
			(1f, 0.22f)
		};
		for (int i = 0; i < array.Length; i++)
		{
			(float, float) tuple = array[i];
			float item = tuple.Item1;
			float item2 = tuple.Item2;
			HudShapes.Ramp rampe = HudShapes.Ramp.Solid(Noir);
			if (gauche)
			{
				Quad(renderer, z, 23f - item, 49f, 125.7f - item, 120f - item, 237f + item, 237f + item, rampe, item2);
			}
			if (droite)
			{
				Quad(renderer, z, 23f - item, 49f, 325f - item, 325f - item, 436.3f + item, 442f + item, rampe, item2);
			}
			Quad(renderer, z, 49f - item, 86f, 8.6f - item, 0f - item, 553.4f + item, 562f + item, rampe, item2);
		}
	}

	private static void DessinerEpaules(Base renderer, float z, Color filet, bool gauche, bool droite)
	{
		if (gauche)
		{
			Quad(renderer, z, 21f, 47f, 125.7f, 120f, 237f, 237f, Epaule);
			Flanc(renderer, z, 126.1f, 21.5f, 120.4f, 47f, Trait);
			Filet(renderer, z, 125.6f, 237f, 21.5f, 1f, Trait);
			Flanc(renderer, z, 236.5f, 21.5f, 236.5f, 47f, Trait);
			Filet(renderer, z, 126.6f, 236f, 22.4f, 1.8f, filet);
		}
		if (droite)
		{
			Quad(renderer, z, 21f, 47f, 325f, 325f, 436.3f, 442f, Epaule);
			Flanc(renderer, z, 325.5f, 21.5f, 325.5f, 47f, Trait);
			Filet(renderer, z, 325f, 436.4f, 21.5f, 1f, Trait);
			Flanc(renderer, z, 435.9f, 21.5f, 441.6f, 47f, Trait);
			Filet(renderer, z, 326f, 435.4f, 22.4f, 1.8f, filet);
		}
	}

	private static void DessinerSocle(Base renderer, float z, HudShapes.Ramp levre, Color filet, in Etat etat)
	{
		Quad(renderer, z, 47f, 86f, 8.6f, 0f, 553.4f, 562f, Plaque);
		if (etat.Coup > 0f)
		{
			Quad(renderer, z, 47f, 86f, 18.1f, 9.5f, 543.9f, 552.5f, HudShapes.Ramp.Solid(SaiyanHud.Red), 0.22f * etat.Coup);
		}
		Charges dash = etat.Dash;
		Filet(renderer, z, 18.5f, (dash.Visibles && dash.Maximum > 0) ? 120f : 240f, 47.5f, 1f, Trait);
		dash = etat.Esquive;
		Filet(renderer, z, (dash.Visibles && dash.Maximum > 0) ? 442f : 322f, 543.5f, 47.5f, 1f, Trait);
		Quad(renderer, z, 47f, 86f, 8.6f, 0f, 13.6f, 5f, levre);
		Quad(renderer, z, 47f, 86f, 548.4f, 557f, 553.4f, 562f, levre);
		Quad(renderer, z, 47f, 86f, 16.1f, 7.5f, 18.1f, 9.5f, HudShapes.Ramp.Solid(filet), 0.8f);
		Quad(renderer, z, 47f, 86f, 543.9f, 552.5f, 545.9f, 554.5f, HudShapes.Ramp.Solid(filet), 0.8f);
		if (etat.VieBasse && !HorsJeu)
		{
			HudShapes.Glow(renderer, 281f * z, 66f * z, 300f * z, 40f * z, SaiyanHud.WithAlpha(SaiyanHud.Red, 0.3f * etat.Battement));
		}
		if (etat.NiveauGagne > 0f && !HorsJeu)
		{
			Rectangle previous = TechniqueArt.BeginClip(renderer, new Rectangle((int)(19f * z), (int)(47f * z), (int)(524f * z), (int)(39f * z)));
			HudShapes.Glow(renderer, (-60f + (1f - etat.NiveauGagne) * 680f) * z, 66f * z, 70f * z, 60f * z, SaiyanHud.WithAlpha(new Color(255, 255, 220, 140), 0.6f * etat.NiveauGagne));
			TechniqueArt.EndClip(renderer, previous);
		}
	}

	private static void DessinerCharges(Base renderer, float z, in Charges charges, float centre, bool toucheDAbord, HudShapes.Ramp plein, Color bord, Color lueur)
	{
		if (!charges.Visibles || charges.Maximum <= 0)
		{
			return;
		}
		bool flag = charges.Maximum > 2;
		float num = (flag ? 12.8f : 18.8f);
		float num2 = (flag ? 17f : 23f);
		float num3 = (float)(charges.Maximum - 1) * num2 + num + 3.2f;
		bool flag2 = charges.Espace || !string.IsNullOrEmpty(charges.Touche);
		float num4 = ((!flag2) ? 0f : (charges.Espace ? 25f : MathF.Max(21f, LargeurDuTexte(renderer, PoliceDesValeurs, 9, z, charges.Touche) + 8f)));
		float num5 = ((!flag2) ? 0f : (toucheDAbord ? 4.5f : 6.5f));
		float num6 = num3 + num5 + num4;
		float num7;
		float num8;
		if (toucheDAbord)
		{
			num7 = MathF.Round(centre - num6 / 2f - 0.5f) + 0.5f;
			num8 = num7 + num4 + num5;
		}
		else
		{
			num8 = MathF.Round(centre - num6 / 2f);
			num7 = num8 + num3 + num5;
		}
		for (int i = 0; i < charges.Maximum; i++)
		{
			float num9 = num8 + (float)i * num2;
			float num10 = ((i < charges.Pleines) ? 1f : ((i == charges.Pleines) ? Math.Clamp(charges.Recharge, 0f, 1f) : 0f));
			Quad(renderer, z, 27.5f, 42.5f, num9 + 3.2f, num9, num9 + 3.2f + num, num9 + num, HudShapes.Ramp.Solid(JetonVide));
			if (i == charges.Pleines && charges.Prise > 0f)
			{
				Quad(renderer, z, 27.5f, 42.5f, num9 + 3.2f, num9, num9 + 3.2f + num, num9 + num, HudShapes.Ramp.Solid(SaiyanHud.White), 0.75f * charges.Prise);
			}
			if (num10 > 0f)
			{
				Quad(renderer, z, 27.5f, 42.5f, num9 + 3.2f, num9, num9 + 3.2f + num10 * num, num9 + num10 * num, plein, (num10 >= 1f) ? 1f : 0.55f);
			}
			Color couleur = ((num10 >= 1f) ? bord : SaiyanHud.WithAlpha(Trait, 0.9f));
			Filet(renderer, z, num9 + 2.7f, num9 + 3.7f + num, 27.5f, 1f, couleur);
			Filet(renderer, z, num9 - 0.5f, num9 + 0.5f + num, 42.5f, 1f, couleur);
			Flanc(renderer, z, num9 + 3.2f, 27.5f, num9, 42.5f, couleur);
			Flanc(renderer, z, num9 + 3.2f + num, 27.5f, num9 + num, 42.5f, couleur);
			if (!HorsJeu)
			{
				float centerX = (num9 + (num + 3.2f) / 2f) * z;
				float centerY = 35f * z;
				if (num10 >= 1f && i == charges.Pleines - 1 && charges.Revenue > 0f)
				{
					HudShapes.Glow(renderer, centerX, centerY, num * z, 15f * z, SaiyanHud.WithAlpha(SaiyanHud.White, 0.8f * charges.Revenue));
				}
				if (i == charges.Maximum - 1 && charges.Nouvelle > 0f)
				{
					HudShapes.Glow(renderer, centerX, centerY, num * 2f * z, 24f * z, SaiyanHud.WithAlpha(lueur, 0.9f * charges.Nouvelle));
				}
			}
		}
		if (flag2)
		{
			DessinerTouche(renderer, z, num7, num4, in charges);
		}
	}

	private static void DessinerTouche(Base renderer, float z, float x, float largeur, in Charges charges)
	{
		float num = (charges.Enfoncee ? 1f : 0f);
		float num2 = 27.5f + num;
		float num3 = 42.5f + num;
		Color color = (charges.Refusee ? ToucheRefus : Touche);
		Color couleur = (charges.Refusee ? BordToucheRefus : BordTouche);
		Quad(renderer, z, num2, num3, x, x, x + largeur, x + largeur, HudShapes.Ramp.Solid(color));
		Filet(renderer, z, x + 1f, x + largeur - 1f, num2, 1f, couleur);
		Filet(renderer, z, x + 1f, x + largeur - 1f, num3, 1f, couleur);
		Flanc(renderer, z, x, num2 + 1f, x, num3 - 1f, couleur);
		Flanc(renderer, z, x + largeur, num2 + 1f, x + largeur, num3 - 1f, couleur);
		if (charges.Espace)
		{
			float num4 = x + largeur / 2f - 6f;
			float num5 = x + largeur / 2f + 6f;
			Flanc(renderer, z, num4, num2 + 5f, num4, num2 + 10.8f, SaiyanHud.White, 1.6f);
			Flanc(renderer, z, num5, num2 + 5f, num5, num2 + 10.8f, SaiyanHud.White, 1.6f);
			Filet(renderer, z, num4 - 0.8f, num5 + 0.8f, num2 + 10f, 1.6f, SaiyanHud.White);
		}
		else
		{
			Texte(renderer, PoliceDesValeurs, 9, z, charges.Touche, (x + largeur / 2f) * z, (num2 + num3) / 2f * z, 0.5f, SaiyanHud.White, ombre: false);
		}
	}

	private static void DessinerVie(Base renderer, float z, in Etat etat)
	{
		if (etat.VieBasse && !HorsJeu)
		{
			HudShapes.Glow(renderer, 138f * z, 64f * z, 140f * z, 28f * z, SaiyanHud.WithAlpha(SaiyanHud.Red, 0.8f * etat.Battement));
		}
		Quad(renderer, z, 53f, 75f, 23.5f, 18.7f, 245f, 257.6f, HudShapes.Ramp.Solid(PisteVie));
		Filet(renderer, z, 23f, 245.5f, 53f, 1f, BordVie);
		Filet(renderer, z, 18.2f, 258.1f, 75f, 1f, BordVie);
		Flanc(renderer, z, 23.5f, 53f, 18.7f, 75f, BordVie);
		Flanc(renderer, z, 245f, 53f, 257.6f, 75f, BordVie);
		float num = Math.Clamp(etat.Vie.Montre, 0f, 1f);
		float num2 = Math.Clamp(etat.Vie.Ombre, 0f, 1f);
		float num3 = Math.Clamp(etat.Vie.Cible, 0f, 1f);
		float num4 = ((etat.Bouclier > 0f && num > 0f) ? Math.Min(1f, num + etat.Bouclier) : 0f);
		if (num4 > num)
		{
			Part(num4, HudShapes.Ramp.Solid(SaiyanHud.KiLight), 0.85f);
		}
		if (num2 > num + 0.002f)
		{
			Part(num2, HudShapes.Ramp.Solid(etat.Vie.Tient ? new Color(247, 255, 246, 226) : new Color(224, 255, 150, 80)));
		}
		if (num3 > num + 0.002f)
		{
			Part(num3, HudShapes.Ramp.Solid(new Color(199, 140, 245, 170)));
		}
		Part(num, PleinVie);
		if (num > 0f && (num < 0.999f || etat.Vie.Tete > 0.01f))
		{
			float num5 = 24.5f + 219.9f * num;
			float num6 = 19.9f + 235.90001f * num;
			Quad(renderer, z, 54f, 74f, num5 - 2f, num6 - 2f, num5, num6, HudShapes.Ramp.Solid(new Color(255, 255, 236, 226)), 0.55f + 0.45f * etat.Vie.Tete);
			if (!HorsJeu)
			{
				HudShapes.Glow(renderer, (num5 + num6) / 2f * z, 64f * z, 20f * z, 13f * z, SaiyanHud.WithAlpha(SaiyanHud.White, 0.2f + 0.6f * etat.Vie.Tete));
			}
		}
		Quad(renderer, z, 54f, 55.3f, 24.5f, 24.2f, 244.4f, 245.09999f, HudShapes.Ramp.Solid(new Color(128, 255, 235, 225)));
		Quad(renderer, z, 71.5f, 74f, 20.5f, 19.9f, 254.40001f, 255.8f, HudShapes.Ramp.Solid(new Color(89, 60, 0, 10)));
		Color couleur = new Color(128, 70, 0, 14);
		for (int i = 1; i <= 9; i++)
		{
			float num7 = 23.5f + (float)i * 22.15f;
			float num8 = 18.7f + (float)i * 23.89f;
			for (float num9 = 0f; num9 < 20f; num9 += 4f)
			{
				float num10 = 54f + num9;
				float num11 = MathF.Min(74f, num10 + 2f);
				Flanc(renderer, z, num7 + (num8 - num7) * (num10 - 54f) / 20f, num10, num7 + (num8 - num7) * (num11 - 54f) / 20f, num11, couleur);
			}
		}
		float milieu = 64f * z;
		Texte(renderer, PoliceDesLibelles, 8, z, etat.LibelleVie, 33.5f * z, milieu, 0f, new Color(235, 255, 240, 236));
		Texte(renderer, PoliceDesValeurs, 12, z, etat.TexteVie, 239f * z, milieu, 1f, SaiyanHud.White);
		void Part(float part, HudShapes.Ramp rampe, float alpha = 1f)
		{
			if (part > 0f)
			{
				Quad(renderer, z, 54f, 74f, 24.5f, 19.9f, 24.5f + 219.9f * part, 19.9f + 235.90001f * part, rampe, alpha, 53f, 75f);
			}
		}
	}

	private static void DessinerKi(Base renderer, float z, in Etat etat)
	{
		bool flag = etat.Ki.Cible >= 0.999f;
		float num = 154.5f;
		float num2 = 64f * z;
		if (flag && !HorsJeu)
		{
			float num3 = 0.55f + 0.35f * MathF.Sin((float)(etat.Maintenant % 2200) / 2200f * ((float)Math.PI * 2f));
			HudShapes.Glow(renderer, (351f + num / 2f) * z, num2, (num / 2f + 16f) * z, 20f * z, SaiyanHud.WithAlpha(SaiyanHud.KiLight, 0.6f * num3));
		}
		Color color = (etat.Ki.Tient ? new Color(242, 235, 245, 255) : new Color(255, 79, 96, 136));
		IGameTexture gameTexture = (HorsJeu ? null : SaiyanHud.Texture("hud_joueur_stries.png"));
		for (int i = 0; i < 10; i++)
		{
			float num4 = 351f + (float)i * 15.4f;
			float num5 = Math.Clamp(etat.Ki.Montre * 10f - (float)i, 0f, 1f);
			float num6 = Math.Clamp(etat.Ki.Ombre * 10f - (float)i, 0f, 1f);
			Quad(renderer, z, 57f, 71f, num4 + 3.1f, num4, num4 + 3.1f + 12.8f, num4 + 12.8f, HudShapes.Ramp.Solid(CaseVide));
			if (num6 > num5 + 0.01f)
			{
				Quad(renderer, z, 57f, 71f, num4 + 3.1f, num4, num4 + 3.1f + num6 * 12.8f, num4 + num6 * 12.8f, HudShapes.Ramp.Solid(color));
			}
			if (num5 <= 0f)
			{
				continue;
			}
			Quad(renderer, z, 57f, 71f, num4 + 3.1f, num4, num4 + 3.1f + num5 * 12.8f, num4 + num5 * 12.8f, PleinKi);
			if (gameTexture != null && num5 * 12.8f > 3.1f)
			{
				int num7 = (int)MathF.Round(14f * z);
				int num8 = Math.Max(4, (int)MathF.Round((float)(gameTexture.Width * num7) / (float)gameTexture.Height));
				Rectangle previous = TechniqueArt.BeginClip(renderer, new Rectangle((int)MathF.Ceiling((num4 + 3.1f) * z), (int)MathF.Round(57f * z), (int)((num5 * 12.8f - 3.1f) * z), num7));
				int num9 = (int)((float)(etat.Maintenant % 60000) / 1000f * 20f * z % (float)num8);
				for (int j = (int)(num4 * z) - num9; (float)j < (num4 + 3.1f + 12.8f) * z; j += num8)
				{
					SaiyanHud.Draw(renderer, gameTexture, j, (int)MathF.Round(57f * z), num8, num7, SaiyanHud.WithAlpha(SaiyanHud.White, 0.7f));
				}
				TechniqueArt.EndClip(renderer, previous);
			}
		}
		if (flag && !HorsJeu)
		{
			float num10 = (float)(etat.Maintenant % 2200) / 2200f;
			float centerX = (321f + num10 * (num + 60f)) * z;
			int num11 = (int)MathF.Round(57f * z);
			int num12 = (int)MathF.Round(14f * z);
			for (int k = 0; k < num12; k++)
			{
				float num13 = ((float)k + 0.5f) / (float)num12;
				int num14 = (int)MathF.Ceiling((351f + 3.1f * (1f - num13)) * z);
				int num15 = (int)MathF.Floor((351f + num - 3.1f * num13) * z);
				if (num15 > num14)
				{
					Rectangle previous2 = TechniqueArt.BeginClip(renderer, new Rectangle(num14, num11 + k, num15 - num14, 1));
					HudShapes.Glow(renderer, centerX, num2, 30f * z, num12, SaiyanHud.WithAlpha(SaiyanHud.White, 0.6f));
					TechniqueArt.EndClip(renderer, previous2);
				}
			}
		}
		Texte(renderer, PoliceDesLibelles, 8, z, etat.LibelleKi, 334f * z, num2, 0f, LibelleKiCouleur);
		Texte(renderer, PoliceDesValeurs, 10, z, etat.TexteKi, 542f * z, num2, 1f, SaiyanHud.White);
	}

	private static void DessinerExperience(Base renderer, float z, in Etat etat)
	{
		float num = Math.Clamp(etat.Experience.Montre, 0f, 1f);
		float num2 = 468f * num;
		Quad(renderer, z, 80f, 83f, 20f, 20f, 488f, 488f, HudShapes.Ramp.Solid(PisteExperience));
		if (num2 > 0f)
		{
			Quad(renderer, z, 80f, 83f, 20f, 20f, 20f + num2, 20f + num2, HudShapes.Ramp.Two(new Color(255, 255, 238, 176), new Color(255, 232, 150, 40)));
			if (etat.EclatExperience > 0f && !HorsJeu)
			{
				Rectangle previous = TechniqueArt.BeginClip(renderer, new Rectangle((int)(20f * z), (int)(80f * z), (int)(num2 * z), (int)MathF.Ceiling(3f * z)));
				HudShapes.Glow(renderer, (20f + (1f - etat.EclatExperience) * num2) * z, 81.5f * z, 30f * z, 4f * z, SaiyanHud.WithAlpha(SaiyanHud.White, 0.95f * etat.EclatExperience));
				TechniqueArt.EndClip(renderer, previous);
			}
		}
		if (etat.NiveauGagne > 0f)
		{
			Quad(renderer, z, 80f, 83f, 20f, 20f, 488f, 488f, HudShapes.Ramp.Solid(SaiyanHud.White), 0.85f * etat.NiveauGagne);
		}
		float num3 = 81.5f * z;
		if (!HorsJeu)
		{
			float num4 = (20f + num2) * z;
			if (etat.Experience.Tete > 0.01f)
			{
				HudShapes.Glow(renderer, num4, num3, 16f * z, 16f * z, SaiyanHud.WithAlpha(new Color(255, 255, 190, 80), 0.9f * etat.Experience.Tete));
			}
			int num5 = Math.Max(10, (int)MathF.Round(12f * z));
			SaiyanHud.Draw(renderer, SaiyanHud.Texture("hud_joueur_dragonball.png"), (int)MathF.Round(num4 - (float)num5 / 2f), (int)MathF.Round(num3 - (float)num5 / 2f), num5, num5, SaiyanHud.White);
		}
		int num6 = Texte(renderer, PoliceDesValeurs, 9, z, etat.TexteExperience, 540f * z, num3, 1f, SaiyanHud.White);
		if (!string.IsNullOrEmpty(etat.LibelleExperience))
		{
			Texte(renderer, PoliceDesLibelles, 8, z, etat.LibelleExperience, 540f * z - (float)num6 - 5f * z, num3, 1f, LibelleExperienceCouleur, ombre: false);
		}
	}

	private static void Quad(Base renderer, float z, float haut, float bas, float gaucheHaut, float gaucheBas, float droiteHaut, float droiteBas, HudShapes.Ramp rampe, float alpha = 1f, float rampeHaut = float.NaN, float rampeBas = float.NaN)
	{
		if (alpha <= 0f || bas <= haut)
		{
			return;
		}
		if (float.IsNaN(rampeHaut))
		{
			rampeHaut = haut;
			rampeBas = bas;
		}
		float num = haut * z;
		float num2 = bas * z;
		int num3 = (int)MathF.Ceiling(num2);
		for (int i = (int)MathF.Floor(num); i < num3; i++)
		{
			float num4 = MathF.Max(i, num);
			float num5 = MathF.Min(i + 1, num2);
			float num6 = num5 - num4;
			if (!(num6 <= 0.02f))
			{
				float num7 = (num4 + num5) / 2f / z;
				float num8 = (num7 - haut) / (bas - haut);
				float t = Math.Clamp((num7 - rampeHaut) / Math.Max(0.0001f, rampeBas - rampeHaut), 0f, 1f);
				HudShapes.Span(renderer, (gaucheHaut + (gaucheBas - gaucheHaut) * num8) * z, (droiteHaut + (droiteBas - droiteHaut) * num8) * z, i, 1, SaiyanHud.WithAlpha(rampe.At(t), alpha * num6));
			}
		}
	}

	private static void Flanc(Base renderer, float z, float x0, float y0, float x1, float y1, Color couleur, float epaisseur = 1f)
	{
		float num = y0 * z;
		float num2 = y1 * z;
		if (num2 <= num || couleur.A <= 0)
		{
			return;
		}
		float num3 = epaisseur * MathF.Max(1f, z) / 2f;
		int num4 = (int)MathF.Ceiling(num2);
		for (int i = (int)MathF.Floor(num); i < num4; i++)
		{
			float num5 = MathF.Max(i, num);
			float num6 = MathF.Min(i + 1, num2);
			float num7 = num6 - num5;
			if (!(num7 <= 0.02f))
			{
				float num8 = ((num5 + num6) / 2f - num) / (num2 - num);
				float num9 = (x0 + (x1 - x0) * num8) * z;
				HudShapes.Span(renderer, num9 - num3, num9 + num3, i, 1, SaiyanHud.WithAlpha(couleur, num7));
			}
		}
	}

	private static void Filet(Base renderer, float z, float x0, float x1, float y, float epaisseur, Color couleur)
	{
		float num = epaisseur * MathF.Max(1f, z) / 2f;
		float num2 = y * z - num;
		float num3 = y * z + num;
		int num4 = (int)MathF.Ceiling(num3);
		for (int i = (int)MathF.Floor(num2); i < num4; i++)
		{
			float num5 = MathF.Min(i + 1, num3) - MathF.Max(i, num2);
			if (num5 > 0.02f)
			{
				HudShapes.Span(renderer, x0 * z, x1 * z, i, 1, SaiyanHud.WithAlpha(couleur, num5));
			}
		}
	}

	private static int Corp(int points, float z)
	{
		float num = (float)points * z;
		int num2 = Corps[0];
		int[] corps = Corps;
		foreach (int num3 in corps)
		{
			if (MathF.Abs((float)num3 - num) < MathF.Abs((float)num2 - num))
			{
				num2 = num3;
			}
		}
		return num2;
	}

	private static float LargeurDuTexte(Base renderer, IFont? police, int points, float z, string? texte)
	{
		if (!HorsJeu && !string.IsNullOrEmpty(texte))
		{
			return (float)SaiyanHud.Measure(renderer, police, Corp(points, z), texte).X / z;
		}
		return 0f;
	}

	private static int Texte(Base renderer, IFont? police, int points, float z, string? texte, float x, float milieu, float ancre, Color couleur, bool ombre = true)
	{
		if (HorsJeu || string.IsNullOrEmpty(texte))
		{
			return 0;
		}
		int size = Corp(points, z);
		Point point = SaiyanHud.Measure(renderer, police, size, texte);
		SaiyanHud.Text(renderer, police, size, texte, (int)MathF.Round(x - (float)point.X * ancre), (int)MathF.Round(milieu - (float)point.Y / 2f), couleur, ombre);
		return point.X;
	}
}
