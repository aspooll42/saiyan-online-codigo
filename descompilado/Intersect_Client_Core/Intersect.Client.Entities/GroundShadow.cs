using System;
using System.Collections.Generic;
using System.IO;
using Intersect.Client.Core;
using Intersect.Client.Entities.Events;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Configuration;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Events;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Entities;

public static class GroundShadow
{
	private static HashSet<string>? _withoutShadow;

	public static void Draw(Entity entity, FloatRect bounds, byte alpha)
	{
		if (!(entity is Player) && !(entity is Critter) && entity.Type != EntityType.GlobalEntity)
		{
			if (!(entity is Event obj))
			{
				return;
			}
			EventGraphic graphic = obj.Graphic;
			if (graphic == null || graphic.Type != EventGraphicType.Sprite)
			{
				return;
			}
		}
		if (entity is Player)
		{
			Mort.DessinerFlaque(entity, bounds);
		}
		if (entity is Player)
		{
			Mort.DessinerFlaque(entity, bounds);
		}
		string text = (string.IsNullOrEmpty(entity.TransformedSprite) ? entity.Sprite : entity.TransformedSprite);
		if (!string.IsNullOrEmpty(text) && WithoutShadow().Contains(Path.GetFileNameWithoutExtension(text)))
		{
			return;
		}
		bool flag = entity.Dashing != null;
		float num = Flight.Altitude(entity);
		if (Flight.HidesBodyShadow(entity))
		{
			return;
		}
		string name = ((bounds.Width < 24f || flag || num > 14f) ? "ombre_petite.png" : ((bounds.Width < 44f) ? "ombre.png" : "ombre_grande.png"));
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, name);
		if (texture != null)
		{
			float num2 = (flag ? ((float)(int)alpha * 0.55f) : ((float)(int)alpha));
			if (num > 0f)
			{
				num2 *= Math.Clamp(1f - num / 80f, 0.5f, 1f) * Flight.ShadowStrength(entity);
			}
			if (entity.IsDying)
			{
				num2 = ((!(entity is Player)) ? CombatFeedback.DeathAlpha(entity, (int)num2) : ((!Mort.Couche(entity)) ? Mort.Alpha(entity, (int)num2) : 0));
			}
			if (!(num2 < 1f))
			{
				float num3 = bounds.X + bounds.Width / 2f;
				float num4 = bounds.Y + bounds.Height - 2f + num;
				Graphics.DrawGameTexture(targetRect: new FloatRect(MathF.Round(num3 - (float)texture.Width / 2f) + 1f, MathF.Round(num4 - (float)texture.Height / 2f), texture.Width, texture.Height), tex: texture, srcRectangle: new FloatRect(0f, 0f, texture.Width, texture.Height), renderColor: new Color((int)Math.Clamp(num2, 0f, 255f), 255, 255, 255));
			}
		}
	}

	private static HashSet<string> WithoutShadow()
	{
		if (_withoutShadow != null)
		{
			return _withoutShadow;
		}
		_withoutShadow = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			string chemin = Path.Combine(ClientConfiguration.ResourcesDirectory, "ombres.json");
			if (Coffre.Existe(chemin) && JObject.Parse(Coffre.LireTexte(chemin))["SansOmbre"] is JArray jArray)
			{
				foreach (string item in jArray.Values<string>())
				{
					if (!string.IsNullOrWhiteSpace(item))
					{
						_withoutShadow.Add(Path.GetFileNameWithoutExtension(item.Trim()));
					}
				}
			}
		}
		catch
		{
		}
		return _withoutShadow;
	}
}
