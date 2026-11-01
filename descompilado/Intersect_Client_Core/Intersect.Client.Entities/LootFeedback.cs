using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Interface.Game;
using Intersect.Client.Localization;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Models;

namespace Intersect.Client.Entities;

public static class LootFeedback
{
	private sealed class Entry
	{
		public ItemDescriptor? Item { get; init; }

		public int Profession { get; init; } = -1;

		public long Quantity { get; set; }

		public long ShownAt { get; set; }

		public string? Tag { get; init; }

		public Color TagColor { get; init; } = Color.White;

		public int Rarity { get; init; }

		public string? Title { get; init; }

		public string? IconName { get; init; }

		public long Duration { get; init; } = 2200L;
	}

	private const long PanoplieShownMs = 4000L;

	private const int TagGap = 10;

	private const int TagFontSize = 11;

	private static readonly Color Gold = new Color(255, 245, 190, 90);

	private static IFont? _tagFont;

	private const int IconSize = 32;

	private const int Padding = 3;

	private const int AccentWidth = 2;

	private const int IconGap = 6;

	private const int RowGap = 4;

	private const int HeadGap = 6;

	private const int FontSize = 12;

	private const long ShownMs = 2200L;

	private const long FadeInMs = 160L;

	private const long FadeOutMs = 450L;

	private const long MergeMs = 1200L;

	private const int MaxEntries = 4;

	private static readonly Color QuantityColor = new Color(255, 242, 242, 242);

	private static readonly Color DefaultNameColor = new Color(255, 196, 204, 220);

	private static readonly List<Entry> Entries = new List<Entry>();

	private static IFont? _font;

	private static long _dernierDessin;

	public static void OnPanoplie(string name, string icon, string bonus)
	{
		Entries.Add(new Entry
		{
			Title = name,
			IconName = icon,
			Tag = bonus,
			TagColor = Gold,
			ShownAt = Timing.Global.Milliseconds,
			Duration = 4000L
		});
		if (Entries.Count > 4)
		{
			Entries.RemoveAt(0);
		}
	}

	public static void OnCrafted(Guid itemId, int quantity, ItemProperties? properties, bool precise)
	{
		if (quantity <= 0 || !DatabaseObject<ItemDescriptor>.TryGet(itemId, out var databaseObject))
		{
			return;
		}
		double? num = JetDObjet.Qualite(databaseObject, properties);
		string text = null;
		Color tagColor = Gold;
		if (num.HasValue)
		{
			double valueOrDefault = num.GetValueOrDefault();
			int num2 = (int)Math.Round(valueOrDefault * 100.0);
			text = (precise ? Strings.CraftedLoot.PreciseRoll.ToString(num2) : Strings.CraftedLoot.Roll.ToString(num2));
			tagColor = ((precise || JetDObjet.EstParfait(valueOrDefault)) ? Gold : RollColor(valueOrDefault));
		}
		else if (precise)
		{
			text = Strings.CraftedLoot.Precise;
		}
		if (text == null)
		{
			OnPickup(itemId, quantity);
			return;
		}
		Entries.Add(new Entry
		{
			Item = databaseObject,
			Quantity = quantity,
			ShownAt = Timing.Global.Milliseconds,
			Tag = text,
			TagColor = tagColor,
			Rarity = JetDObjet.Rarete(databaseObject, properties)
		});
		if (Entries.Count > 4)
		{
			Entries.RemoveAt(0);
		}
	}

	public static Color RollColor(double quality)
	{
		if (!(quality >= 0.9999))
		{
			if (!(quality >= 0.7))
			{
				if (quality >= 0.35)
				{
					return new Color(255, 196, 204, 220);
				}
				return new Color(255, 150, 160, 182);
			}
			return new Color(255, 142, 214, 124);
		}
		return Gold;
	}

	public static void OnPickup(Guid itemId, int quantity)
	{
		if (quantity <= 0 || !DatabaseObject<ItemDescriptor>.TryGet(itemId, out var databaseObject))
		{
			return;
		}
		Recolte.SurButin(itemId);
		long now = Timing.Global.Milliseconds;
		Entry entry = Entries.FirstOrDefault(delegate(Entry entry2)
		{
			ItemDescriptor? item = entry2.Item;
			return item != null && item.Id == itemId && entry2.Tag == null && now - entry2.ShownAt < 1200;
		});
		if (entry != null)
		{
			entry.Quantity += quantity;
			entry.ShownAt = now - 160;
			return;
		}
		Entries.Add(new Entry
		{
			Item = databaseObject,
			Quantity = quantity,
			ShownAt = now,
			Rarity = databaseObject.Rarity
		});
		if (Entries.Count > 4)
		{
			Entries.RemoveAt(0);
		}
	}

	public static void OnProfessionExperience(int profession, long experience)
	{
		if (experience <= 0 || !Professions.IsValid(profession))
		{
			return;
		}
		long now = Timing.Global.Milliseconds;
		Entry entry = Entries.FirstOrDefault((Entry entry2) => entry2.Profession == profession && now - entry2.ShownAt < 1200);
		if (entry != null)
		{
			entry.Quantity += experience;
			entry.ShownAt = now - 160;
			return;
		}
		Entries.Add(new Entry
		{
			Profession = profession,
			Quantity = experience,
			ShownAt = now
		});
		if (Entries.Count > 4)
		{
			Entries.RemoveAt(0);
		}
	}

	public static int StackHeight()
	{
		long now = Timing.Global.Milliseconds;
		int num = Entries.Count((Entry entry) => now - entry.ShownAt <= entry.Duration);
		int num2 = ((num == 0) ? 6 : (6 + num * 38 + (num - 1) * 4));
		return (int)MathF.Round((float)num2 * Graphics.ReductionDesReperes);
	}

	public static void Draw()
	{
		long milliseconds = Timing.Global.Milliseconds;
		long num = ((_dernierDessin == 0L) ? 0 : Math.Max(0L, milliseconds - _dernierDessin));
		_dernierDessin = milliseconds;
		if (BandesDeCinema.InterfaceEscamotee)
		{
			foreach (Entry entry2 in Entries)
			{
				entry2.ShownAt += num;
			}
			return;
		}
		if (Entries.Count == 0)
		{
			return;
		}
		GameRenderer renderer = Graphics.Renderer;
		if (renderer == null)
		{
			return;
		}
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		if (_font == null)
		{
			_font = Globals.ContentManager.GetFont("chakrapetch");
		}
		if (_font == null)
		{
			return;
		}
		long now = Timing.Global.Milliseconds;
		Entries.RemoveAll((Entry entry2) => now - entry2.ShownAt > entry2.Duration);
		float num2 = Math.Max(0.25f, Graphics.WorldScale);
		float num3 = 1f / num2;
		float num4 = Graphics.ReductionDesReperes / num2;
		int size = Graphics.TailleDeRepere(12);
		int size2 = Graphics.TailleDeRepere(11);
		FloatRect currentView = Graphics.CurrentView;
		FloatRect bodyWorldPos = me.BodyWorldPos;
		float num5 = bodyWorldPos.X + bodyWorldPos.Width / 2f;
		float num6 = 38f * num4;
		float num7 = bodyWorldPos.Y - 6f * num4;
		int num8 = 0;
		int num9 = Entries.Count - 1;
		while (num9 >= 0)
		{
			Entry entry = Entries[num9];
			long num10 = now - entry.ShownAt;
			float num11 = ((num10 < 160) ? ((float)num10 / 160f) : ((num10 > entry.Duration - 450) ? Math.Max(0f, (float)(entry.Duration - num10) / 450f) : 1f));
			float num12 = ((num10 < 160) ? ((1f - (float)num10 / 160f) * 8f * num4) : 0f);
			ItemDescriptor item = entry.Item;
			string text = ((entry.Title != null) ? string.Empty : ((item != null) ? $"+{entry.Quantity} " : $"+{entry.Quantity} {Strings.Professions.Exp} "));
			string text2 = entry.Title ?? ((item != null) ? item.Name : Professions.Name(entry.Profession));
			Vector2 vector = renderer.MeasureText(text, _font, size, num3);
			Vector2 vector2 = renderer.MeasureText(text2, _font, size, num3);
			if (_tagFont == null)
			{
				_tagFont = Globals.ContentManager.GetFont("rajdhanibold");
			}
			float num13 = ((entry.Tag != null && _tagFont != null) ? (10f * num4 + renderer.MeasureText(entry.Tag, _tagFont, size2, num3).X) : 0f);
			float num14 = 46f * num4 + vector.X + vector2.X + num13;
			float num15 = Snap(num5 - num14 / 2f, currentView.X, num2);
			float num16 = Snap(num7 - (float)(num8 + 1) * num6 - (float)(num8 * 4) * num4 + num12, currentView.Y, num2);
			Color value;
			Color color = ((entry.Title != null) ? Gold : ((item == null) ? Professions.Tint(entry.Profession) : ((CustomColors.Items.Rarities.TryGetValue(entry.Rarity, out value) && value != null) ? value : DefaultNameColor)));
			Graphics.DrawGameTexture(renderer.WhitePixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num15, num16, num14, num6), new Color((int)(200f * num11), 11, 14, 23));
			Graphics.DrawGameTexture(renderer.WhitePixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num15, num16, 2f * num4, num6), new Color((int)(255f * num11), color.R, color.G, color.B));
			float num17 = num15 + 5f * num4;
			float y = num16 + 3f * num4;
			string name = entry.IconName ?? ((item != null) ? item.Icon : Professions.Icon(entry.Profession));
			Color color2 = item?.Color ?? Color.White;
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Item, name);
			if (texture != null && texture.Width > 0 && texture.Height > 0)
			{
				int num18 = Math.Min(texture.Width, texture.Height);
				Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, num18, texture.Height), new FloatRect(num17, y, 32f * num4, 32f * num4), new Color((int)((float)(int)color2.A * num11), color2.R, color2.G, color2.B));
			}
			float num19 = num17 + 38f * num4;
			float y2 = Snap(num16 + (num6 - vector2.Y) / 2f, currentView.Y, num2);
			DrawText(renderer, text, num19, y2, num3, QuantityColor, num11, null, size);
			DrawText(renderer, text2, Snap(num19 + vector.X, currentView.X, num2), y2, num3, color, num11, null, size);
			if (num13 > 0f && _tagFont != null)
			{
				float y3 = Snap(num16 + (num6 - renderer.MeasureText(entry.Tag, _tagFont, size2, num3).Y) / 2f, currentView.Y, num2);
				float x = Snap(num19 + vector.X + vector2.X + 10f * num4, currentView.X, num2);
				DrawText(renderer, entry.Tag, x, y3, num3, entry.TagColor, num11, _tagFont, size2);
			}
			num9--;
			num8++;
		}
	}

	private static void DrawText(GameRenderer renderer, string text, float x, float y, float pixel, Color color, float alpha, IFont? font = null, int size = 12)
	{
		FloatRect currentView = Graphics.CurrentView;
		FloatRect clipRect = new FloatRect(0f, 0f, renderer.ScreenWidth, renderer.ScreenHeight);
		if (font == null)
		{
			font = _font;
		}
		renderer.DrawString(text, font, size, x + pixel - currentView.X, y + pixel - currentView.Y, pixel, new Color((int)(180f * alpha), 0, 0, 0), worldPos: true, null, clipRect);
		renderer.DrawString(text, font, size, x - currentView.X, y - currentView.Y, pixel, new Color((int)(255f * alpha), color.R, color.G, color.B), worldPos: true, null, clipRect);
	}

	private static float Snap(float world, float viewOrigin, float zoom)
	{
		return viewOrigin + MathF.Round((world - viewOrigin) * zoom) / zoom;
	}
}
