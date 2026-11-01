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

internal sealed class FenetreRecompenses : FenetreCharte
{
	private const float Largeur = 1240f;

	private const float Hauteur = 748f;

	private const float BodyTop = 58f;

	private const float BodyHeight = 676f;

	private const float HeroLeft = 14f;

	private const float HeroWidth = 330f;

	private const float PathLeft = 354f;

	private const float PathWidth = 872f;

	private const float FooterHeight = 64f;

	private const int PerRow = 10;

	private const long ClaimShowMs = 2200L;

	private const long CountMs = 700L;

	private static readonly Color Exp = new Color(255, 255, 146, 64);

	private static readonly Color Off = new Color(255, 58, 72, 104);

	private static readonly Color OffText = new Color(255, 91, 104, 132);

	private static readonly Color TakenText = new Color(255, 184, 154, 90);

	private DailyRewardEntry? _claimed;

	private long _claimedAt;

	protected override Rectangle Place => new Rectangle(180, 76, 1240, 748);

	public bool IsOpen => !base.IsHidden;

	public FenetreRecompenses(Canvas gameCanvas)
		: base(gameCanvas, "FenetreRecompenses")
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
			Placer();
		}
	}

	public bool Reclamer()
	{
		if (base.IsHidden || !Globals.DailyRewardCanClaim)
		{
			return false;
		}
		Claim();
		return true;
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

	protected override void Dessiner(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		DessinerCadre(renderer);
		int num = DessinerBarre(renderer, Strings.DailyRewards.Title);
		DailyRewardEntry[] dailyRewards = Globals.DailyRewards;
		if (dailyRewards.Length != 0)
		{
			int num2 = _d.Y(23f);
			string text = Strings.DailyRewards.CycleProgress.ToString(Math.Clamp(Globals.DailyRewardDay, 1, dailyRewards.Length), dailyRewards.Length);
			int num3 = num - _d.L(18f);
			int num4 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(16f), text, num3, num2, A(TechniqueArt.Gold));
			TechniqueArt.SpacedRight(renderer, TechniqueArt.Label, Pt(10f), Strings.DailyRewards.Day.ToString(string.Empty).Trim().ToUpperInvariant(), num3 - num4 - _d.L(8f), num2 + _d.L(1f), _d.Em(0.16f, 10f), A(TechniqueArt.Dim));
		}
		Panel(renderer, _d.R(14f, 58f, 330f, 676f));
		Panel(renderer, _d.R(354f, 58f, 872f, 676f));
		if (dailyRewards.Length == 0)
		{
			TechniqueArt.TextCenter(renderer, TechniqueArt.Body, Pt(15f), Strings.DailyRewards.Empty, _d.X(790f), _d.Y(396f), A(TechniqueArt.Dim));
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

	private void Panel(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect)
	{
		TechniqueArt.Patch(renderer, "tech_panneau.png", rect, A(TechniqueArt.White), 12);
	}

	private void Embleme(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle outer, IGameTexture? icon, Color color, float iconRatio)
	{
		TechniqueArt.Image(renderer, "tech_hexa_bord.png", outer, A(TechniqueArt.Mix(color, TechniqueArt.White, 0.95f)));
		int num = Math.Max(1, (int)MathF.Round((float)outer.Width * 2f / 62f));
		Rectangle rect = new Rectangle(outer.X + num, outer.Y + num, outer.Width - num * 2, outer.Height - num * 2);
		TechniqueArt.Image(renderer, "tech_hexa.png", rect, A(new Color(255, 8, 11, 19)));
		TechniqueArt.Image(renderer, "tech_hexa_radial.png", rect, A(TechniqueArt.Mix(color, new Color(255, 22, 28, 44), 0.3f)));
		int num2 = (int)MathF.Round((float)rect.Width * iconRatio);
		Rectangle rectangle = new Rectangle(rect.X + (rect.Width - num2) / 2, rect.Y + (rect.Height - num2) / 2, num2, num2);
		TechniqueArt.Glow(renderer, (float)rectangle.X + (float)num2 / 2f, (float)rectangle.Y + (float)num2 / 2f, (float)num2 * 0.75f, A(color, 0.45f));
		if (icon != null)
		{
			SaiyanHud.Draw(renderer, icon, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, A(TechniqueArt.White));
		}
	}

	private void DrawHero(Intersect.Client.Framework.Gwen.Renderer.Base renderer, DailyRewardEntry entry, bool waiting, bool justClaimed, long now)
	{
		Rectangle rect = _d.R(28f, 72f, 302f, 648f);
		bool flag = waiting || justClaimed;
		Color color = ((!flag) ? TechniqueArt.Dim : (entry.Milestone ? TechniqueArt.Gold : TechniqueArt.Ki));
		TechniqueArt.FillVertical(renderer, rect, A(new Color(255, 23, 34, 56)), A(new Color(255, 15, 22, 38)));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line2));
		TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y + _d.L(10f), Math.Max(2, _d.L(3f)), rect.Height - _d.L(20f)), A(color));
		int num = rect.X + rect.Width / 2;
		string text = (flag ? Strings.DailyRewards.Today : Strings.DailyRewards.Tomorrow).ToString(entry.Day).ToUpperInvariant();
		TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(12f), text, num, rect.Y + _d.L(30f), _d.L(2f), A(color));
		int num2 = _d.L(150f);
		Rectangle outer = new Rectangle(num - num2 / 2, rect.Y + _d.L(62f), num2, num2);
		if (justClaimed)
		{
			float num3 = Math.Clamp((float)(now - _claimedAt) / 500f, 0f, 1f);
			int num4 = (int)((float)num2 * 0.12f * MathF.Sin(num3 * (float)Math.PI));
			outer = new Rectangle(outer.X - num4 / 2, outer.Y - num4 / 2, num2 + num4, num2 + num4);
		}
		IGameTexture gameTexture = IconOf(entry);
		Color color2 = ((entry.BonusPercent > 0) ? TechniqueArt.Cyan : ((gameTexture == null) ? Exp : TechniqueArt.Gold));
		TechniqueArt.Glow(renderer, (float)outer.X + (float)outer.Width / 2f, (float)outer.Y + (float)outer.Height / 2f, (float)outer.Width * 0.62f, A(color2, 0.38f));
		Embleme(renderer, outer, gameTexture, color2, 0.56f);
		if (gameTexture == null)
		{
			string text2 = ((entry.BonusPercent > 0) ? Strings.DailyRewards.BonusValue.ToString(entry.BonusPercent) : Strings.DailyRewards.ExperienceUnit.ToString().ToUpperInvariant());
			TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(32f), text2, outer.X + outer.Width / 2, outer.Y + outer.Height / 2, A(color2));
		}
		float num5 = (justClaimed ? Math.Clamp((float)(now - _claimedAt - 60) / 700f, 0f, 1f) : 1f);
		num5 = 1f - MathF.Pow(1f - num5, 3f);
		int num6 = rect.Y + _d.L(250f);
		if (!string.IsNullOrWhiteSpace(entry.ItemName))
		{
			string text3 = Traduction.Contenu(entry.ItemName);
			string text4 = ((entry.ItemQuantity > 1) ? Strings.DailyRewards.ItemQuantity.ToString(text3, entry.ItemQuantity) : text3);
			foreach (string item in TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(21f), text4, rect.Width - _d.L(24f), 2))
			{
				TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(21f), item, num, num6, A(TechniqueArt.Text));
				num6 += _d.L(26f);
			}
			num6 += _d.L(6f);
		}
		if (entry.Zeni > 0)
		{
			Line(renderer, num, num6, "+" + MarketWindow.FormatAmount((long)((float)entry.Zeni * num5)), Strings.DailyRewards.ZeniUnit, TechniqueArt.Gold);
			num6 += _d.L(36f);
		}
		if (entry.Experience > 0)
		{
			Line(renderer, num, num6, "+" + MarketWindow.FormatAmount((long)((float)entry.Experience * num5)), Strings.DailyRewards.ExperienceUnit, Exp);
			num6 += _d.L(36f);
		}
		if (entry.BonusPercent > 0)
		{
			Line(renderer, num, num6, Strings.DailyRewards.BonusValue.ToString(entry.BonusPercent), Strings.DailyRewards.BonusUnit.ToString(entry.BonusMinutes), TechniqueArt.Cyan);
		}
		Rectangle rect2 = new Rectangle(rect.X + _d.L(30f), rect.Bottom - _d.L(110f), rect.Width - _d.L(60f), _d.L(54f));
		if (waiting)
		{
			bool flag2 = IsHot("reclamer");
			float num7 = 0.75f + 0.25f * MathF.Sin((float)now / 260f);
			TechniqueArt.GlowRect(renderer, new Rectangle(rect2.X - _d.L(16f), rect2.Y - _d.L(12f), rect2.Width + _d.L(32f), rect2.Height + _d.L(24f)), A(TechniqueArt.Ki, 0.45f * num7));
			TechniqueArt.Patch(renderer, flag2 ? "tech_bouton_survol.png" : "tech_bouton.png", rect2, A(TechniqueArt.White), 8);
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Heavy, Pt(24f), Strings.DailyRewards.Claim.ToString().ToUpperInvariant(), rect2.X + rect2.Width / 2, rect2.Y + rect2.Height / 2, _d.L(2f), A(new Color(255, 22, 10, 2)));
			AddHit(rect2, "reclamer", Claim);
			int num8 = LargeurTouche(renderer, "Entrée");
			Touche(renderer, "Entrée", num - num8 / 2, rect2.Bottom + _d.L(22f));
		}
		else
		{
			TechniqueArt.Patch(renderer, "tech_bouton_eteint.png", rect2, A(TechniqueArt.White), 8);
			LocalizedString localizedString = (justClaimed ? Strings.DailyRewards.Claimed : Strings.DailyRewards.ComeBackShort);
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Heavy, Pt(17f), localizedString.ToString().ToUpperInvariant(), rect2.X + rect2.Width / 2, rect2.Y + rect2.Height / 2, _d.L(2f), A(TechniqueArt.Dim));
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
		float num2 = 82f;
		float num3 = 588f;
		float num4 = num3 / (float)Math.Max(1, num);
		float left = 376f;
		float column = 82.8f;
		for (int i = 0; i < num; i++)
		{
			float num5 = num2 + num4 * ((float)i + 0.5f);
			int num6 = Math.Min(10, calendar.Length - i * 10);
			TechniqueArt.Fill(renderer, _d.R(X(0), num5 - 1f, X(num6 - 1) - X(0), 2f), A(TechniqueArt.Line2));
			int num7 = Math.Clamp(taken - i * 10, 0, num6);
			if (num7 > 1)
			{
				TechniqueArt.Fill(renderer, _d.R(X(0), num5 - 1f, X(num7 - 1) - X(0), 2f), A(TechniqueArt.Gold));
			}
			for (int j = 0; j < num6; j++)
			{
				DailyRewardEntry dailyRewardEntry = calendar[i * 10 + j];
				DrawNode(renderer, dailyRewardEntry, _d.X(X(j)), _d.Y(num5), dailyRewardEntry.Day <= taken, dailyRewardEntry.Day == today, dailyRewardEntry.Day == flash, now);
			}
		}
		float X(int k)
		{
			return left + column * ((float)k + 0.5f);
		}
	}

	private void DrawNode(Intersect.Client.Framework.Gwen.Renderer.Base renderer, DailyRewardEntry entry, int x, int y, bool taken, bool today, bool flash, long now)
	{
		Color color = (taken ? TakenText : (today ? TechniqueArt.Text : OffText));
		TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(10.5f), entry.Day.ToString(), x, y - _d.L(entry.Milestone ? 48 : 34), A(color));
		if (today)
		{
			float num = (float)(now % 1500) / 1500f;
			int num2 = _d.L((float)(entry.Milestone ? 80 : 52) * (0.85f + 0.55f * num));
			TechniqueArt.Image(renderer, "lieu_anneau.png", new Rectangle(x - num2 / 2, y - num2 / 2, num2, num2), A(TechniqueArt.Ki, 0.9f * (1f - num)));
		}
		float num3 = 0f;
		if (flash)
		{
			float num4 = Math.Clamp((float)(now - _claimedAt) / 700f, 0f, 1f);
			num3 = 1f - MathF.Abs(num4 * 2f - 1f);
			TechniqueArt.Glow(renderer, x, y, _d.L(32f + 32f * num3), A(TechniqueArt.Gold, 0.8f * num3));
		}
		IGameTexture gameTexture = IconOf(entry);
		if (entry.Milestone)
		{
			int num5 = _d.L(66f + 12f * num3);
			Color color2 = (taken ? TechniqueArt.Gold : (today ? TechniqueArt.Ki : new Color(255, 138, 106, 42)));
			Embleme(renderer, new Rectangle(x - num5 / 2, y - num5 / 2, num5, num5), gameTexture, color2, 0.6f);
		}
		else
		{
			int num6 = _d.L(80f + 30f * num3);
			Color color3 = (taken ? TechniqueArt.Mix(TechniqueArt.Gold, TechniqueArt.White, num3) : (today ? TechniqueArt.Ki : Off));
			TechniqueArt.Image(renderer, "lieu_point.png", new Rectangle(x - num6 / 2, y - num6 / 2, num6, num6), A(color3));
			if (!taken && gameTexture != null)
			{
				int num7 = _d.L(18f);
				SaiyanHud.Draw(renderer, gameTexture, x - num7 / 2, y - num7 / 2, num7, num7, A(TechniqueArt.White, today ? 1f : 0.55f));
			}
		}
		string text = ((!string.IsNullOrWhiteSpace(entry.ItemName)) ? ShortName(Traduction.Contenu(entry.ItemName)) : ((entry.BonusPercent > 0) ? Strings.DailyRewards.BonusValue.ToString(entry.BonusPercent) : ((entry.Experience > 0 && entry.Zeni <= 0) ? $"{Short(entry.Experience)} {Strings.DailyRewards.ExperienceUnit}" : Short(entry.Zeni))));
		Color color4 = (taken ? TakenText : (today ? TechniqueArt.Text : (entry.Milestone ? TakenText : OffText)));
		TechniqueArt.TextCenter(renderer, TechniqueArt.Heavy, Pt(13.5f), text, x, y + _d.L(entry.Milestone ? 50 : 36), A(color4));
	}

	private void DrawFooter(Intersect.Client.Framework.Gwen.Renderer.Base renderer, DailyRewardEntry[] calendar, DailyRewardEntry next, bool waiting)
	{
		TechniqueArt.Fill(renderer, _d.R(376f, 670f, 828f, 1f), A(TechniqueArt.Line));
		int num = _d.Y(702f);
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
				int num2 = dailyRewardEntry.Day - next.Day + ((!waiting) ? 1 : 0);
				text = ((num2 <= 1) ? Strings.DailyRewards.NextMilestoneTomorrow.ToString(text2) : Strings.DailyRewards.NextMilestone.ToString(num2, text2));
			}
		}
		string text3 = Strings.DailyRewards.NoPenalty.ToString();
		int x = TechniqueArt.Measure(renderer, TechniqueArt.Body, Pt(12.5f), text3).X;
		int num3 = _d.X(382f);
		int num4 = _d.X(1198f);
		int num5 = num4 - x - _d.L(24f) - num3;
		if (num5 < _d.L(200f))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(14f), text, num4 - num3, 1).FirstOrDefault(), num3, num - _d.L(11f), A(TechniqueArt.Soft));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), text3, num4 - num3, 1).FirstOrDefault(), num3, num + _d.L(11f), A(TechniqueArt.Dim));
		}
		else
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(14f), TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(14f), text, num5, 1).FirstOrDefault(), num3, num, A(TechniqueArt.Soft));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), text3, num4 - x, num, A(TechniqueArt.Dim));
		}
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
