using System;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Framework.Core;

namespace Intersect.Client.Entities;

public static class AurasEquipees
{
	private const string PaperdollRoches = "aura_roches";

	private const string AtlasRoches = "aura_roches.png";

	private const float CycleRoches = 3200f;

	private const int NombreDeRoches = 12;

	private const string PaperdollBoulesDeKi = "aura_boules_ki";

	private const string AtlasBoulesDeKi = "aura_boules_ki.png";

	private const float TourBoulesDeKi = 1600f;

	private static readonly (float Hauteur, float Inclinaison)[] OrbitesKi = new(float, float)[3]
	{
		(-5f, 3f),
		(3f, -2.5f),
		(-1f, 0.5f)
	};

	private static float Echelle => BoulesDeCristal.EchelleDuDessin;

	public static void DrawBehind(Entity entity, FloatRect corps, Color couleur)
	{
		Roches(entity, corps, couleur, devant: false);
		BoulesDeKi(entity, corps, couleur, devant: false);
	}

	public static void DrawFront(Entity entity, FloatRect corps, Color couleur)
	{
		Roches(entity, corps, couleur, devant: true);
		BoulesDeKi(entity, corps, couleur, devant: true);
	}

	private static void Poser(IGameTexture atlas, FloatRect source, float x, float y, Color couleur)
	{
		float echelle = Echelle;
		float num = source.Width / 2f;
		float num2 = source.Height / 2f;
		float x2 = MathF.Round(x * echelle) / echelle - num / 2f;
		float y2 = MathF.Round(y * echelle) / echelle - num2 / 2f;
		BoulesDeCristal.Tracer(atlas, source, new FloatRect(x2, y2, num, num2), couleur);
	}

	private static Color Teinte(Color couleur, float alpha, float lumiere = 1f)
	{
		return new Color((int)((float)(int)couleur.A * Math.Clamp(alpha, 0f, 1f)), (int)((float)(int)couleur.R * lumiere), (int)((float)(int)couleur.G * lumiere), (int)((float)(int)couleur.B * lumiere));
	}

	private static float Hasard(int n)
	{
		float num = MathF.Sin((float)n * 12.9898f + 78.233f) * 43758.547f;
		return num - MathF.Floor(num);
	}

	private static void Roches(Entity entity, FloatRect corps, Color couleur, bool devant)
	{
		if (corps.Width <= 0f || couleur.A == 0 || Flight.IsAirborne(entity) || !BoulesDeCristal.Porte(entity, "aura_roches"))
		{
			return;
		}
		IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Misc, "aura_roches.png");
		if (gameTexture == null)
		{
			return;
		}
		float num = corps.X + corps.Width / 2f;
		float num2 = corps.Y + corps.Height * 46f / 48f;
		float num3 = (float)(entity.Id.GetHashCode() & 0xFFFF) / 3200f;
		float num4 = (float)Timing.Global.Milliseconds / 3200f + num3;
		for (int i = 0; i < 12; i++)
		{
			float num5 = num4 + (float)i / 12f;
			int num6 = (int)MathF.Floor(num5);
			float num7 = num5 - (float)num6;
			float num8 = (float)i * 2.39996f + (float)num6 * 1.7f + Hasard(i + num6 * 31) * 0.8f;
			float num9 = MathF.Sin(num8);
			if (!devant)
			{
				SolDeRoche(gameTexture, i, num6, num7, num, num2, num8, couleur);
			}
			if (num9 > 0f == devant)
			{
				float num10 = num + (10f + 5f * Hasard(i * 7 + num6)) * MathF.Cos(num8);
				float num11 = num2 + 3.8f * num9;
				float num12 = 1f - MathF.Pow(1f - num7, 2.2f);
				float x = num10 + 1.8f * MathF.Sin(num7 * 4f + (float)i);
				float y = num11 - 34f * num12;
				float alpha = MathF.Min(1f, num7 / 0.08f) * MathF.Min(1f, (1f - num7) / 0.28f);
				int num13 = i % 2;
				int num14 = i % 3;
				float num15 = num7 * 2.2f * ((i % 2 == 0) ? 1f : (-1f)) + (float)i;
				int num16 = ((int)MathF.Floor(num15 / ((float)Math.PI / 4f)) % 8 + 8) % 8;
				Poser(gameTexture, new FloatRect(num16 * 16, (num13 * 3 + num14) * 16, 16f, 16f), x, y, Teinte(couleur, alpha));
			}
		}
	}

	private static void SolDeRoche(IGameTexture atlas, int i, int tour, float vie, float piedsX, float piedsY, float angle, Color couleur)
	{
		float num = piedsX + (10f + 5f * Hasard(i * 7 + tour)) * MathF.Cos(angle);
		float num2 = piedsY + 3.8f * MathF.Sin(angle);
		float alpha = MathF.Min(1f, vie / 0.08f) * MathF.Min(1f, (1f - vie) / 0.28f) * (1f - vie) * 0.9f;
		Poser(atlas, new FloatRect(i % 3 * 16, 96f, 16f, 16f), num, num2, Teinte(couleur, alpha));
		if (!(vie >= 0.16f))
		{
			float num3 = vie / 0.16f;
			for (int j = 0; j < 4; j++)
			{
				float x = (float)j * 1.57f + (float)i;
				Poser(atlas, new FloatRect(48f, 96f, 16f, 16f), num + MathF.Cos(x) * (1f + 3.5f * num3), num2 + MathF.Sin(x) * 0.9f * num3 - 1.2f * num3, Teinte(couleur, 0.67f * (1f - num3)));
			}
		}
	}

	private static void BoulesDeKi(Entity entity, FloatRect corps, Color couleur, bool devant)
	{
		if (corps.Width <= 0f || couleur.A == 0 || !BoulesDeCristal.Porte(entity, "aura_boules_ki"))
		{
			return;
		}
		IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Misc, "aura_boules_ki.png");
		if (gameTexture == null)
		{
			return;
		}
		float num = corps.X + corps.Width / 2f;
		float num2 = corps.Y + corps.Height * 0.6f;
		float num3 = (float)(Timing.Global.Milliseconds + (entity.Id.GetHashCode() & 0xFFFF)) / 1600f;
		for (int i = 0; i < OrbitesKi.Length; i++)
		{
			for (int num4 = 5; num4 >= 0; num4--)
			{
				float x = (float)Math.PI * 2f * (num3 + (float)i / 3f) - (float)num4 * 0.11f;
				float num5 = MathF.Sin(x);
				if (num5 > 0f == devant)
				{
					float num6 = MathF.Cos(x);
					float x2 = num + 13f * num6;
					float y = num2 + OrbitesKi[i].Hauteur + 4f * num5 + OrbitesKi[i].Inclinaison * num6;
					float num7 = (num5 + 1f) / 2f;
					float num8 = 0.7f + 0.3f * num7;
					if (num4 > 0)
					{
						Poser(gameTexture, new FloatRect((num4 - 1) * 12, 36f, 12f, 12f), x2, y, Teinte(couleur, 0.43f * (1f - (float)num4 / 6f) * num8, num8));
					}
					else
					{
						int num9 = Math.Clamp((int)(num7 * 4f), 0, 3);
						Poser(gameTexture, new FloatRect(num9 * 36, 0f, 36f, 36f), x2, y, Teinte(couleur, 1f, num8));
					}
				}
			}
		}
	}
}
