using System;
using System.Collections.Generic;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Boss;
using Intersect.Client.Localization;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Entities;

public static class BossWorld
{
	private static bool _broken;

	private static IEnumerable<Entity> Pnjs()
	{
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value != null && !value.IsHidden && value.Type == EntityType.GlobalEntity && !(value is Critter) && !(value is Resource))
			{
				yield return value;
			}
		}
	}

	private static Entity? Colosse(string nom)
	{
		foreach (Entity item in Pnjs())
		{
			if (string.Equals(NomFr(item), nom, StringComparison.OrdinalIgnoreCase))
			{
				return item;
			}
		}
		return null;
	}

	private static bool EstCristal(Entity entity)
	{
		return NomFr(entity).StartsWith("Cristal", StringComparison.OrdinalIgnoreCase);
	}

	private static string NomFr(Entity entity)
	{
		return Traduction.NomSource(entity.Id, entity.Name);
	}

	public static void DrawGround()
	{
		if (_broken || BossMondial.Etat != 2)
		{
			return;
		}
		try
		{
			long milliseconds = Timing.Global.Milliseconds;
			Entity entity = Colosse(BossMondial.Nom);
			foreach (Entity item in Pnjs())
			{
				if (!EstCristal(item))
				{
					continue;
				}
				Vector2 origin = item.Origin;
				float num = 0.5f + 0.5f * MathF.Sin((float)milliseconds / 320f + origin.X);
				for (int i = 0; i < 10; i++)
				{
					float x = (float)i / 10f * ((float)Math.PI * 2f);
					float num2 = 13f + num * 3f;
					Dot(origin.X + MathF.Cos(x) * num2 - 1f, origin.Y + MathF.Sin(x) * num2 * 0.42f - 1f, 2f, 2f, new Color((int)(120f + 60f * num), 110, 210, 255), GameBlendModes.Add);
				}
				if (entity != null)
				{
					float x2 = origin.X;
					float num3 = origin.Y - 14f;
					float x3 = entity.Origin.X;
					float num4 = entity.Origin.Y - 26f;
					for (int j = 1; j <= 14; j++)
					{
						float num5 = (float)j / 15f;
						float num6 = MathF.Sin((float)milliseconds / 90f + (float)j * 1.5f + origin.X) * 3.5f * MathF.Sin(num5 * (float)Math.PI);
						Dot(x2 + (x3 - x2) * num5, num3 + (num4 - num3) * num5 + num6, 2f, 2f, new Color(150, 150, 225, 255), GameBlendModes.Add);
					}
				}
			}
		}
		catch (Exception exception)
		{
			Fail(exception);
		}
	}

	public static void DrawAir()
	{
		if (_broken || BossMondial.Etat != 2)
		{
			return;
		}
		try
		{
			long milliseconds = Timing.Global.Milliseconds;
			foreach (Entity item in Pnjs())
			{
				if (EstCristal(item))
				{
					Cristal(item, milliseconds);
				}
				else if (string.Equals(NomFr(item), BossMondial.Nom, StringComparison.OrdinalIgnoreCase))
				{
					Colosse(item, milliseconds);
				}
			}
		}
		catch (Exception exception)
		{
			Fail(exception);
		}
	}

	private static void Cristal(Entity entity, long now)
	{
		Vector2 origin = entity.Origin;
		float num = 0.5f + 0.5f * MathF.Sin((float)now / 320f + origin.X);
		for (int i = 0; i < 26; i++)
		{
			float num2 = (float)i / 26f;
			float num3 = MathF.Max(1f, (1f - num2) * 6f + 1f);
			float num4 = 0.55f + 0.45f * num2;
			Color color = new Color(235, (int)(60f + 180f * num4), (int)(140f + 105f * num4), 255);
			Dot(origin.X - num3, origin.Y - 26f + (float)i, num3 * 2f, 1f, color);
		}
		Dot(origin.X - 1f, origin.Y - 26f, 2f, 26f, new Color((int)(70f + 60f * num), 225, 245, 255), GameBlendModes.Add);
		for (int j = 0; j < 12; j++)
		{
			float x = (float)j / 12f * ((float)Math.PI * 2f) + (float)now / 900f;
			float num5 = 10f + num * 4f;
			Dot(origin.X + MathF.Cos(x) * num5, origin.Y - 14.3f + MathF.Sin(x) * num5 * 0.6f, 2f, 2f, new Color((int)(90f * num + 40f), 130, 220, 255), GameBlendModes.Add);
		}
	}

	private static void Colosse(Entity entity, long now)
	{
		FloatRect bodyWorldPos = entity.BodyWorldPos;
		float num = bodyWorldPos.X + bodyWorldPos.Width / 2f;
		float num2 = bodyWorldPos.Y + bodyWorldPos.Height * 0.55f;
		float num3 = MathF.Max(40f, bodyWorldPos.Width * 0.85f);
		float num4 = MathF.Max(28f, bodyWorldPos.Height * 0.62f);
		if (BossMondial.Cristaux > 0)
		{
			float num5 = Math.Clamp((float)BossMondial.Cristaux / (float)Math.Max(1, BossMondial.CristauxTotal), 0.25f, 1f);
			for (int i = 0; i < 44; i++)
			{
				float x = (float)i / 44f * ((float)Math.PI * 2f);
				float num6 = MathF.Sin((float)now / 260f + (float)i * 0.5f) * 2f;
				Dot(num + MathF.Cos(x) * (num3 + num6), num2 + MathF.Sin(x) * (num4 + num6), 2f, 2f, new Color((int)(120f * num5), 130, 215, 255), GameBlendModes.Add);
			}
		}
		int num7 = Math.Clamp(BossMondial.Phase, 0, 3);
		for (int j = 0; j < num7; j++)
		{
			for (int k = 0; k < 16; k++)
			{
				float x2 = (float)k / 16f * ((float)Math.PI * 2f) + (float)j * 0.4f;
				float num8 = 6f + 5f * MathF.Sin((float)now / 110f + (float)k * 2.1f + (float)j);
				for (int l = 0; l < 4; l++)
				{
					float num9 = 1f - (float)l / 4f;
					Dot(num + MathF.Cos(x2) * (num3 * 0.82f + num8 * (float)l * 0.5f), num2 + MathF.Sin(x2) * (num4 * 0.82f + num8 * (float)l * 0.35f), 2f, 2f, new Color((int)(120f * num9), 255, (int)(120f + 90f * num9), 60), GameBlendModes.Add);
				}
			}
		}
	}

	private static void Dot(float x, float y, float width, float height, Color color, GameBlendModes mode = GameBlendModes.None)
	{
		if (color.A != 0)
		{
			IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
			if (gameTexture != null)
			{
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(x), MathF.Round(y), width, height), color, null, mode);
			}
		}
	}

	private static void Fail(Exception exception)
	{
		_broken = true;
		ApplicationContext.Context.Value?.Logger.LogError(exception, "Boss Mondial : dessin coupé");
	}
}
