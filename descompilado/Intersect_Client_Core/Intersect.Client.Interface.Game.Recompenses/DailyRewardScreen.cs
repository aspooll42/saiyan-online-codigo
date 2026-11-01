using System;
using System.Globalization;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.DailyRewards;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Localization;
using Intersect.Models;

namespace Intersect.Client.Interface.Game.Recompenses;

internal sealed class DailyRewardScreen : CharteScreen
{
	private const float Left = 310f;

	private const float Top = 150f;

	private const float WindowWidth = 980f;

	private const float WindowHeight = 600f;

	private const float HeroLeft = 332f;

	private const float HeroTop = 222f;

	private const float HeroWidth = 290f;

	private const float HeroHeight = 452f;

	private const float PathLeft = 640f;

	private const float PathRight = 1268f;

	private const int PerRow = 10;

	private const long ClaimShowMs = 2200L;

	private const long CountMs = 700L;

	private static readonly Color Exp = new Color(255, 255, 146, 64);

	private static readonly Color Off = new Color(255, 58, 72, 104);

	private static readonly Color OffText = new Color(255, 91, 104, 132);

	private static readonly Color TakenText = new Color(255, 184, 154, 90);

	private DailyRewardEntry? _claimed;

	private long _claimedAt;

	public bool IsOpen => !base.IsHidden;

	public override bool IsBlockingInput => !base.IsHidden;

	public DailyRewardScreen(Canvas gameCanvas)
		: base(gameCanvas, "DailyRewardScreen")
	{
	}

	public void Toggle()
	{
		if (IsOpen)
		{
			Hide();
		}
		else
		{
			Open();
		}
	}

	public void Open()
	{
		_claimed = null;
		Show();
	}

	public void Refresh()
	{
	}

	public void Update()
	{
		if (!base.IsHidden)
		{
			FitCanvas();
		}
	}

	internal override bool HandleKey(Keys modifier, Keys key)
	{
		if (modifier == Keys.None && key == Keys.Return)
		{
			Claim();
			return true;
		}
		return base.HandleKey(modifier, key);
	}

	private void Claim()
	{
		if (Globals.DailyRewardCanClaim)
		{
			_claimed = Globals.DailyRewards.FirstOrDefault((DailyRewardEntry entry) => entry.Day == Globals.DailyRewardDay);
			_claimedAt = Timing.Global.MillisecondsUtc;
			PacketSender.SendDailyRewardClaim();
			Globals.DailyRewardCanClaim = false;
			Audio.AddGameSound("recolte_butin_cristal.wav", loop: false);
		}
	}

	protected override void DrawScreen(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		DrawBackdrop(renderer, TechniqueArt.Ki);
		Rectangle rect = _d.R(310f, 150f, 980f, 600f);
		TechniqueArt.GlowRect(renderer, _d.R(270f, 110f, 1060f, 680f), A(TechniqueArt.Ki, 0.14f), additive: false);
		Panel(renderer, rect);
		DailyRewardEntry[] dailyRewards = Globals.DailyRewards;
		DrawHeader(renderer, dailyRewards);
		if (dailyRewards.Length == 0)
		{
			TechniqueArt.TextCenter(renderer, TechniqueArt.Body, Pt(16f), Strings.DailyRewards.Empty, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, A(TechniqueArt.Dim));
			return;
		}
		bool flag = _claimed != null && now - _claimedAt < 2200;
		if (!flag)
		{
			_claimed = null;
		}
		DailyRewardEntry dailyRewardEntry = dailyRewards.FirstOrDefault((DailyRewardEntry dailyRewardEntry2) => dailyRewardEntry2.Day == Globals.DailyRewardDay) ?? dailyRewards[0];
		DailyRewardEntry entry = (flag ? _claimed : dailyRewardEntry);
		bool flag2 = Globals.DailyRewardCanClaim && !flag;
		int taken = Globals.DailyRewardDay - 1;
		if (flag && _claimed.Day >= Globals.DailyRewardDay)
		{
			taken = _claimed.Day;
		}
		DrawHero(renderer, entry, flag2, flag, now);
		DrawPath(renderer, dailyRewards, taken, flag2 ? dailyRewardEntry.Day : (-1), flag ? _claimed.Day : (-1), now);
		DrawFooter(renderer, dailyRewards, dailyRewardEntry, flag2);
	}

	private void DrawHeader(Intersect.Client.Framework.Gwen.Renderer.Base renderer, DailyRewardEntry[] calendar)
	{
		string text = Strings.DailyRewards.Title.ToString().ToUpperInvariant();
		TechniqueArt.Styled(stops: new TechniqueArt.Stops(TechniqueArt.White, 0.3f, new Color(255, 255, 194, 122), 0.75f, TechniqueArt.Ki), renderer: renderer, font: TechniqueArt.Display, size: Pt(30f), text: text, x: _d.X(338f), y: _d.Y(168f), alpha: _fade);
		TechniqueArt.Fill(renderer, _d.R(332f, 206f, 936f, 1f), A(TechniqueArt.Line));
		Rectangle rect = _d.R(1242f, 166f, 32f, 32f);
		bool flag = IsHot("fermer");
		TechniqueArt.Fill(renderer, rect, A(new Color(255, 10, 14, 24)));
		TechniqueArt.Outline(renderer, rect, A(flag ? TechniqueArt.Gold : TechniqueArt.Line2));
		TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(16f), "X", rect.X + rect.Width / 2, rect.Y + rect.Height / 2, A(flag ? TechniqueArt.Text : TechniqueArt.Dim));
		AddHit(rect, "fermer", Hide);
		if (calendar.Length != 0)
		{
			string text2 = Strings.DailyRewards.CycleProgress.ToString(Math.Clamp(Globals.DailyRewardDay, 1, calendar.Length), calendar.Length);
			int num = rect.X - _d.L(14f);
			int x = TechniqueArt.Measure(renderer, TechniqueArt.Heavy, Pt(22f), text2).X;
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(22f), text2, num - x, _d.Y(182f), A(TechniqueArt.Gold));
			TechniqueArt.SpacedRight(renderer, TechniqueArt.Label, Pt(12f), Strings.DailyRewards.Day.ToString(string.Empty).Trim().ToUpperInvariant(), num - x - _d.L(8f), _d.Y(183f), _d.L(2f), A(TechniqueArt.Dim));
		}
	}

	private void DrawHero(Intersect.Client.Framework.Gwen.Renderer.Base renderer, DailyRewardEntry entry, bool waiting, bool justClaimed, long now)
	{
		Rectangle rect = _d.R(332f, 222f, 290f, 452f);
		bool flag = waiting || justClaimed;
		Color color = ((!flag) ? TechniqueArt.Dim : (entry.Milestone ? TechniqueArt.Gold : TechniqueArt.Ki));
		TechniqueArt.FillVertical(renderer, rect, A(new Color(255, 23, 34, 56)), A(new Color(255, 15, 22, 38)));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line2));
		TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y + _d.L(10f), Math.Max(2, _d.L(3f)), rect.Height - _d.L(20f)), A(color));
		int num = rect.X + rect.Width / 2;
		string text = (flag ? Strings.DailyRewards.Today : Strings.DailyRewards.Tomorrow).ToString(entry.Day).ToUpperInvariant();
		TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(12f), text, num, _d.Y(248f), _d.L(2f), A(color));
		int num2 = _d.L(124f);
		Rectangle outer = new Rectangle(num - num2 / 2, _d.Y(272f), num2, num2);
		if (justClaimed)
		{
			float num3 = Math.Clamp((float)(now - _claimedAt) / 500f, 0f, 1f);
			int num4 = (int)((float)num2 * 0.12f * MathF.Sin(num3 * (float)Math.PI));
			outer = new Rectangle(outer.X - num4 / 2, outer.Y - num4 / 2, num2 + num4, num2 + num4);
		}
		IGameTexture gameTexture = IconOf(entry);
		Color color2 = ((entry.BonusPercent > 0) ? TechniqueArt.Cyan : ((gameTexture == null) ? Exp : TechniqueArt.Gold));
		TechniqueArt.Glow(renderer, (float)outer.X + (float)outer.Width / 2f, (float)outer.Y + (float)outer.Height / 2f, (float)outer.Width * 0.62f, A(color2, 0.38f));
		DrawHexEmblem(renderer, outer, gameTexture, -1, color2, 0.56f);
		if (gameTexture == null)
		{
			string text2 = ((entry.BonusPercent > 0) ? Strings.DailyRewards.BonusValue.ToString(entry.BonusPercent) : Strings.DailyRewards.ExperienceUnit.ToString().ToUpperInvariant());
			TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(30f), text2, outer.X + outer.Width / 2, outer.Y + outer.Height / 2, A(color2));
		}
		float num5 = (justClaimed ? Math.Clamp((float)(now - _claimedAt - 60) / 700f, 0f, 1f) : 1f);
		num5 = 1f - MathF.Pow(1f - num5, 3f);
		int num6 = _d.Y(422f);
		if (!string.IsNullOrWhiteSpace(entry.ItemName))
		{
			string text3 = Traduction.Contenu(entry.ItemName);
			TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(22f), (entry.ItemQuantity > 1) ? Strings.DailyRewards.ItemQuantity.ToString(text3, entry.ItemQuantity) : text3, num, num6, A(TechniqueArt.Text));
			num6 += _d.L(30f);
		}
		if (entry.Zeni > 0)
		{
			Line(renderer, num, num6, "+" + MarketWindow.FormatAmount((long)((float)entry.Zeni * num5)), Strings.DailyRewards.ZeniUnit, TechniqueArt.Gold);
			num6 += _d.L(34f);
		}
		if (entry.Experience > 0)
		{
			Line(renderer, num, num6, "+" + MarketWindow.FormatAmount((long)((float)entry.Experience * num5)), Strings.DailyRewards.ExperienceUnit, Exp);
			num6 += _d.L(34f);
		}
		if (entry.BonusPercent > 0)
		{
			Line(renderer, num, num6, Strings.DailyRewards.BonusValue.ToString(entry.BonusPercent), Strings.DailyRewards.BonusUnit.ToString(entry.BonusMinutes), TechniqueArt.Cyan);
		}
		Rectangle rect2 = _d.R(362f, 578f, 230f, 54f);
		if (waiting)
		{
			bool flag2 = IsHot("reclamer");
			float num7 = 0.75f + 0.25f * MathF.Sin((float)now / 260f);
			TechniqueArt.GlowRect(renderer, new Rectangle(rect2.X - _d.L(16f), rect2.Y - _d.L(12f), rect2.Width + _d.L(32f), rect2.Height + _d.L(24f)), A(TechniqueArt.Ki, 0.45f * num7));
			TechniqueArt.Patch(renderer, flag2 ? "tech_bouton_survol.png" : "tech_bouton.png", rect2, A(TechniqueArt.White), 8);
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Heavy, Pt(24f), Strings.DailyRewards.Claim.ToString().ToUpperInvariant(), rect2.X + rect2.Width / 2, rect2.Y + rect2.Height / 2, _d.L(2f), A(new Color(255, 22, 10, 2)));
			AddHit(rect2, "reclamer", Claim);
			int num8 = KeyChipWidth(renderer, "Entrée");
			KeyChip(renderer, "Entrée", num - num8 / 2, rect2.Bottom + _d.L(20f), flag2);
		}
		else
		{
			TechniqueArt.Patch(renderer, "tech_bouton_eteint.png", rect2, A(TechniqueArt.White), 8);
			LocalizedString localizedString = (justClaimed ? Strings.DailyRewards.Claimed : Strings.DailyRewards.ComeBackShort);
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Heavy, Pt(18f), localizedString.ToString().ToUpperInvariant(), rect2.X + rect2.Width / 2, rect2.Y + rect2.Height / 2, _d.L(2f), A(TechniqueArt.Dim));
		}
	}

	private void Line(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int centreX, int centreY, string valeur, string unite, Color couleur)
	{
		unite = unite.ToUpperInvariant();
		int x = TechniqueArt.Measure(renderer, TechniqueArt.Heavy, Pt(30f), valeur).X;
		int num = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(11f), unite, _d.L(2f));
		int num2 = centreX - (x + _d.L(8f) + num) / 2;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(30f), valeur, num2, centreY, A(couleur));
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(11f), unite, num2 + x + _d.L(8f), centreY + _d.L(6f), _d.L(2f), A(TechniqueArt.Dim));
	}

	private void DrawPath(Intersect.Client.Framework.Gwen.Renderer.Base renderer, DailyRewardEntry[] calendar, int taken, int today, int flash, long now)
	{
		int num = (calendar.Length + 10 - 1) / 10;
		float num2 = 222f;
		float num3 = 452f / (float)Math.Max(1, num);
		float column = 62.8f;
		for (int i = 0; i < num; i++)
		{
			float num4 = num2 + num3 * ((float)i + 0.5f) - 4f;
			int num5 = Math.Min(10, calendar.Length - i * 10);
			TechniqueArt.Fill(renderer, _d.R(X(0), num4 - 1f, X(num5 - 1) - X(0), 2f), A(TechniqueArt.Line2));
			int num6 = Math.Clamp(taken - i * 10, 0, num5);
			if (num6 > 1)
			{
				TechniqueArt.Fill(renderer, _d.R(X(0), num4 - 1f, X(num6 - 1) - X(0), 2f), A(TechniqueArt.Gold));
			}
			for (int j = 0; j < num5; j++)
			{
				DailyRewardEntry dailyRewardEntry = calendar[i * 10 + j];
				DrawNode(renderer, dailyRewardEntry, _d.X(X(j)), _d.Y(num4), dailyRewardEntry.Day <= taken, dailyRewardEntry.Day == today, dailyRewardEntry.Day == flash, now);
			}
		}
		float X(int k)
		{
			return 640f + column * ((float)k + 0.5f);
		}
	}

	private void DrawNode(Intersect.Client.Framework.Gwen.Renderer.Base renderer, DailyRewardEntry entry, int x, int y, bool taken, bool today, bool flash, long now)
	{
		Color color = (taken ? TakenText : (today ? TechniqueArt.Text : OffText));
		TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(10f), entry.Day.ToString(), x, y - _d.L(entry.Milestone ? 42 : 30), A(color));
		if (today)
		{
			float num = (float)(now % 1500) / 1500f;
			int num2 = _d.L((float)(entry.Milestone ? 70 : 46) * (0.85f + 0.55f * num));
			TechniqueArt.Image(renderer, "lieu_anneau.png", new Rectangle(x - num2 / 2, y - num2 / 2, num2, num2), A(TechniqueArt.Ki, 0.9f * (1f - num)));
		}
		float num3 = 0f;
		if (flash)
		{
			float num4 = Math.Clamp((float)(now - _claimedAt) / 700f, 0f, 1f);
			num3 = 1f - MathF.Abs(num4 * 2f - 1f);
			TechniqueArt.Glow(renderer, x, y, _d.L(30f + 30f * num3), A(TechniqueArt.Gold, 0.8f * num3));
		}
		IGameTexture gameTexture = IconOf(entry);
		if (entry.Milestone)
		{
			int num5 = _d.L(58f + 12f * num3);
			Color color2 = (taken ? TechniqueArt.Gold : (today ? TechniqueArt.Ki : new Color(255, 138, 106, 42)));
			DrawHexEmblem(renderer, new Rectangle(x - num5 / 2, y - num5 / 2, num5, num5), gameTexture, -1, color2, 0.6f);
		}
		else
		{
			int num6 = _d.L(72f + 30f * num3);
			Color color3 = (taken ? TechniqueArt.Mix(TechniqueArt.Gold, TechniqueArt.White, num3) : (today ? TechniqueArt.Ki : Off));
			TechniqueArt.Image(renderer, "lieu_point.png", new Rectangle(x - num6 / 2, y - num6 / 2, num6, num6), A(color3));
			if (!taken && gameTexture != null)
			{
				int num7 = _d.L(16f);
				SaiyanHud.Draw(renderer, gameTexture, x - num7 / 2, y - num7 / 2, num7, num7, A(TechniqueArt.White, today ? 1f : 0.55f));
			}
		}
		string text = ((!string.IsNullOrWhiteSpace(entry.ItemName)) ? ShortName(Traduction.Contenu(entry.ItemName)) : ((entry.BonusPercent > 0) ? Strings.DailyRewards.BonusValue.ToString(entry.BonusPercent) : ((entry.Experience > 0 && entry.Zeni <= 0) ? $"{Short(entry.Experience)} {Strings.DailyRewards.ExperienceUnit}" : Short(entry.Zeni))));
		Color color4 = (taken ? TakenText : (today ? TechniqueArt.Text : (entry.Milestone ? TakenText : OffText)));
		TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(13f), text, x, y + _d.L(entry.Milestone ? 44 : 32), A(color4));
	}

	private void DrawFooter(Intersect.Client.Framework.Gwen.Renderer.Base renderer, DailyRewardEntry[] calendar, DailyRewardEntry next, bool waiting)
	{
		TechniqueArt.Fill(renderer, _d.R(332f, 688f, 936f, 1f), A(TechniqueArt.Line));
		int centerY = _d.Y(716f);
		string text;
		if (waiting && next.Milestone)
		{
			text = Strings.DailyRewards.MilestoneToday.ToString(next.Label);
		}
		else
		{
			DailyRewardEntry dailyRewardEntry = (from entry in calendar
				where entry.Milestone && entry.Day > next.Day - ((!waiting) ? 1 : 0)
				orderby entry.Day
				select entry).FirstOrDefault();
			if (dailyRewardEntry == null)
			{
				text = string.Empty;
			}
			else
			{
				string text2 = (string.IsNullOrWhiteSpace(dailyRewardEntry.ItemName) ? dailyRewardEntry.Label : Traduction.Contenu(dailyRewardEntry.ItemName));
				int num = dailyRewardEntry.Day - next.Day + ((!waiting) ? 1 : 0);
				text = ((num <= 1) ? Strings.DailyRewards.NextMilestoneTomorrow.ToString(text2) : Strings.DailyRewards.NextMilestone.ToString(num, text2));
			}
		}
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(15f), text, _d.X(338f), centerY, A(TechniqueArt.Soft));
		string text3 = Strings.DailyRewards.NoPenalty.ToString();
		int x = TechniqueArt.Measure(renderer, TechniqueArt.Body, Pt(13f), text3).X;
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), text3, _d.X(1262f) - x, centerY, A(TechniqueArt.Dim));
	}

	private static IGameTexture? IconOf(DailyRewardEntry entry)
	{
		if (!string.IsNullOrWhiteSpace(entry.ItemName))
		{
			string nom = entry.ItemName.Trim();
			string traduit = Traduction.Contenu(nom);
			ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Lookup.Values.OfType<ItemDescriptor>().FirstOrDefault((ItemDescriptor item) => string.Equals(item.Name?.Trim(), nom, StringComparison.OrdinalIgnoreCase) || string.Equals(item.Name?.Trim(), traduit, StringComparison.OrdinalIgnoreCase));
			if (itemDescriptor != null && !string.IsNullOrWhiteSpace(itemDescriptor.Icon))
			{
				return Globals.ContentManager?.GetTexture(TextureType.Item, itemDescriptor.Icon);
			}
			return null;
		}
		if (entry.Zeni <= 0)
		{
			return null;
		}
		Guid guid = Globals.Me?.CurrencyItemId ?? Guid.Empty;
		if (guid != Guid.Empty && DatabaseObject<ItemDescriptor>.TryGet(guid, out var databaseObject) && !string.IsNullOrWhiteSpace(databaseObject.Icon))
		{
			IGameTexture gameTexture = Globals.ContentManager?.GetTexture(TextureType.Item, databaseObject.Icon);
			if (gameTexture != null)
			{
				return gameTexture;
			}
		}
		return Globals.ContentManager?.GetTexture(TextureType.Item, "zeni_anime.png");
	}

	private static string Short(long montant)
	{
		if (montant < 1000)
		{
			return montant.ToString();
		}
		return ((float)montant / 1000f).ToString("0.#", CultureInfo.CurrentCulture) + "k";
	}

	private static string ShortName(string nom)
	{
		string[] array = nom.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (array.Length != 0)
		{
			return array[^1];
		}
		return nom;
	}
}
