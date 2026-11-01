using System;
using System.IO;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Items;
using Intersect.Client.General;
using Intersect.Client.Maps;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Models;

namespace Intersect.Client.Entities;

public static class BoulesDeCristal
{
	private const string PaperdollDesBoules = "boules_cristal";

	private const string Atlas = "boules_cristal_orbite.png";

	private const int Cellule = 24;

	private const int Tailles = 4;

	private const float Boucle = 6400f;

	private static readonly float[,] Orbites = new float[7, 6]
	{
		{ -22f, 7f, 2.5f, 1f, 2f, 0f },
		{ 10f, 11f, 3.5f, -2f, 1f, 0.55f },
		{ -4f, 12f, 4f, 3f, 1f, 0.3f },
		{ 15f, 10f, 3f, 1f, 2f, 0.8f },
		{ -8f, 11f, 3.5f, -3f, 1f, 0.12f },
		{ 4f, 9f, 3f, -2f, 2f, 0.66f },
		{ -18f, 10f, 3f, 2f, 1f, 0.42f }
	};

	private static Action<IGameTexture, FloatRect, FloatRect, Color>? _apercu;

	private static float _echelleApercu = 1f;

	internal static float EchelleDuDessin
	{
		get
		{
			if (_apercu == null)
			{
				if (!(Graphics.WorldScale > 0f))
				{
					return 1f;
				}
				return Graphics.WorldScale;
			}
			return _echelleApercu;
		}
	}

	public static void DrawBehind(Entity entity, FloatRect corps, Color couleur)
	{
		Dessiner(entity, corps, couleur, devant: false);
	}

	public static void DrawFront(Entity entity, FloatRect corps, Color couleur)
	{
		Dessiner(entity, corps, couleur, devant: true);
	}

	public static void DessinerApercu(Entity entity, FloatRect corps, bool devant, float echelle, Action<IGameTexture, FloatRect, FloatRect, Color> poser)
	{
		_apercu = poser;
		_echelleApercu = ((echelle > 0f) ? echelle : 1f);
		try
		{
			Dessiner(entity, corps, Color.White, devant);
			if (devant)
			{
				AurasEquipees.DrawFront(entity, corps, Color.White);
			}
			else
			{
				AurasEquipees.DrawBehind(entity, corps, Color.White);
			}
		}
		finally
		{
			_apercu = null;
		}
	}

	internal static void Tracer(IGameTexture atlas, FloatRect source, FloatRect destination, Color couleur)
	{
		if (_apercu != null)
		{
			_apercu(atlas, source, destination, couleur);
		}
		else
		{
			Graphics.DrawGameTexture(atlas, source, destination, couleur);
		}
	}

	private static void Dessiner(Entity entity, FloatRect corps, Color couleur, bool devant)
	{
		if (corps.Width <= 0f || couleur.A == 0 || !Porte(entity, "boules_cristal"))
		{
			return;
		}
		IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Misc, "boules_cristal_orbite.png");
		if (gameTexture == null)
		{
			return;
		}
		float num = corps.X + corps.Width / 2f;
		float num2 = corps.Y + corps.Height * 0.63f;
		float num3 = (float)(Timing.Global.Milliseconds + (entity.Id.GetHashCode() & 0xFFFF)) / 6400f;
		float echelleDuDessin = EchelleDuDessin;
		Span<float> span = stackalloc float[7];
		Span<int> span2 = stackalloc int[7];
		for (int i = 0; i < 7; i++)
		{
			span[i] = MathF.Sin(Angle(i, num3));
			span2[i] = i;
		}
		for (int j = 1; j < 7; j++)
		{
			int num4 = j;
			while (num4 > 0 && span[span2[num4]] < span[span2[num4 - 1]])
			{
				ref int reference = ref span2[num4];
				ref int reference2 = ref span2[num4 - 1];
				int num5 = span2[num4 - 1];
				int num6 = span2[num4];
				reference = num5;
				reference2 = num6;
				num4--;
			}
		}
		Span<int> span3 = span2;
		for (int k = 0; k < span3.Length; k++)
		{
			int num7 = span3[k];
			float num8 = span[num7];
			if (num8 > 0f == devant)
			{
				float x = Angle(num7, num3);
				float num9 = MathF.Cos(x);
				float num10 = num + Orbites[num7, 1] * num9;
				float num11 = num2 + Orbites[num7, 0] + Orbites[num7, 2] * num8 + Orbites[num7, 3] * num9 + 0.8f * MathF.Sin((float)Math.PI * 2f * num3 * 2f + (float)num7 * 1.3f);
				float num12 = (num8 + 1f) / 2f;
				int num13 = Math.Clamp((int)(num12 * 4f), 0, 3);
				float num14 = 1f - 0.24f * (1f - num12);
				Color couleur2 = new Color(couleur.A, (int)((float)(int)couleur.R * num14), (int)((float)(int)couleur.G * num14), (int)((float)(int)couleur.B * num14));
				float x2 = MathF.Round(num10 * echelleDuDessin) / echelleDuDessin - 6f;
				float y = MathF.Round(num11 * echelleDuDessin) / echelleDuDessin - 6f;
				Tracer(gameTexture, new FloatRect(num13 * 24, num7 * 24, 24f, 24f), new FloatRect(x2, y, 12f, 12f), couleur2);
			}
		}
	}

	private static float Angle(int boule, float temps)
	{
		return (float)Math.PI * 2f * (temps * Orbites[boule, 4] + Orbites[boule, 5]);
	}

	internal static bool Porte(Entity entity, string paperdollObjet)
	{
		if (!string.IsNullOrEmpty(entity.TransformedSprite) && !Transformation.KeepsPaperdolls(entity))
		{
			return false;
		}
		MapInstance mapInstance = MapInstance.Get(entity.MapId);
		if (mapInstance == null || mapInstance.HideEquipment)
		{
			return false;
		}
		int count = Options.Instance.Equipment.Slots.Count;
		if (entity.Equipment.Length != count)
		{
			return false;
		}
		for (int i = 0; i < count; i++)
		{
			Guid guid = Guid.Empty;
			if (entity == Globals.Me)
			{
				int num = ((i < entity.MyEquipment.Length) ? entity.MyEquipment[i] : (-1));
				if (num > -1 && num < entity.Inventory.Length)
				{
					IItem item = entity.Inventory[num];
					if (item != null)
					{
						guid = item.ItemId;
					}
				}
			}
			else
			{
				guid = entity.Equipment[i];
			}
			if (!(guid == Guid.Empty) && DatabaseObject<ItemDescriptor>.TryGet(guid, out var databaseObject))
			{
				string path = ((entity.Gender == Gender.Male) ? databaseObject.MalePaperdoll : databaseObject.FemalePaperdoll);
				if (string.Equals(Path.GetFileNameWithoutExtension(path), paperdollObjet, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
		}
		return false;
	}
}
