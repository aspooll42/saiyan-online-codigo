using System;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Localization;
using Intersect.Client.Spells;
using Intersect.Framework.Core;
using Intersect.GameObjects;
using Intersect.Models;

namespace Intersect.Client.Interface.Game.Techniques;

public sealed class MasteryToast : Intersect.Client.Framework.Gwen.Control.Base
{
	private const long InMs = 220L;

	private const long HoldMs = 2300L;

	private const long OutMs = 450L;

	private static long _announcedAt;

	private TechniqueProgress.RankUp _current;

	private long _startedAt;

	internal static bool EstAffiche { get; private set; }

	private float Scale
	{
		get
		{
			Intersect.Client.Framework.Gwen.Control.Base parent = base.Parent;
			if (parent == null)
			{
				return 1f;
			}
			return Math.Max(0.6f, Math.Min((float)parent.Width / 1600f, (float)parent.Height / 900f));
		}
	}

	public MasteryToast(Canvas canvas)
		: base(canvas, "MasteryToast")
	{
		base.MouseInputEnabled = false;
		SetSize(460, 120);
		base.IsHidden = true;
		_announcedAt = Math.Max(_announcedAt, TechniqueProgress.LastRankUp?.At ?? 0);
	}

	public void Update(bool windowHidden)
	{
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		TechniqueProgress.RankUp? lastRankUp = TechniqueProgress.LastRankUp;
		if (lastRankUp.HasValue)
		{
			TechniqueProgress.RankUp valueOrDefault = lastRankUp.GetValueOrDefault();
			if (valueOrDefault.At > _announcedAt)
			{
				if (!windowHidden)
				{
					_announcedAt = valueOrDefault.At;
				}
				else if (!Annonces.EstAffichee)
				{
					_announcedAt = valueOrDefault.At;
					_current = valueOrDefault;
					_startedAt = millisecondsUtc;
					base.IsHidden = false;
					BringToFront();
					Audio.AddGameSound("menu_open.wav", loop: false);
				}
			}
		}
		if (base.IsHidden)
		{
			EstAffiche = false;
			return;
		}
		if (millisecondsUtc - _startedAt > 2970 || !windowHidden)
		{
			base.IsHidden = true;
			EstAffiche = false;
			return;
		}
		EstAffiche = true;
		float scale = Scale;
		int num = (int)(460f * scale);
		int num2 = (int)(120f * scale);
		if (base.Width != num || base.Height != num2)
		{
			SetSize(num, num2);
		}
		Intersect.Client.Framework.Gwen.Control.Base parent = base.Parent;
		if (parent != null)
		{
			SetPosition((parent.Width - num) / 2, (int)(118f * scale));
		}
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		if (!DatabaseObject<SpellDescriptor>.TryGet(_current.SpellId, out var databaseObject))
		{
			return;
		}
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		float s = Scale;
		long num = Timing.Global.MillisecondsUtc - _startedAt;
		float alpha = ((num < 220) ? ((float)num / 220f) : ((num > 2520) ? (1f - (float)(num - 220 - 2300) / 450f) : 1f));
		alpha = Math.Clamp(alpha, 0f, 1f);
		int num2 = ((num < 220) ? ((int)((1f - alpha) * (float)(-L(16f)))) : 0);
		Color color = TechniqueArt.FamilyColor(SpellMastery.FamilyOf(databaseObject));
		Rectangle rect = new Rectangle(L(40f), L(20f) + num2, L(380f), L(80f));
		TechniqueArt.GlowRect(renderer, new Rectangle(rect.X - L(30f), rect.Y - L(20f), rect.Width + L(60f), rect.Height + L(40f)), A(color, 0.25f), additive: false);
		TechniqueArt.Patch(renderer, "tech_panneau.png", rect, A(TechniqueArt.White), 12);
		TechniqueArt.Fill(renderer, new Rectangle(rect.X + L(12f), rect.Y, rect.Width - L(24f), Math.Max(1, L(2f))), A(color));
		Rectangle rect2 = new Rectangle(rect.X + L(14f), rect.Y + L(11f), L(58f), L(58f));
		TechniqueArt.Image(renderer, "tech_hexa_bord.png", rect2, A(TechniqueArt.Mix(color, TechniqueArt.White, 0.95f)));
		int num3 = Math.Max(1, L(2f));
		Rectangle rect3 = new Rectangle(rect2.X + num3, rect2.Y + num3, rect2.Width - num3 * 2, rect2.Height - num3 * 2);
		TechniqueArt.Image(renderer, "tech_hexa.png", rect3, A(new Color(255, 8, 11, 19)));
		TechniqueArt.Image(renderer, "tech_hexa_radial.png", rect3, A(TechniqueArt.Mix(color, new Color(255, 22, 28, 44), 0.3f)));
		IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Spell, databaseObject.Icon);
		if (texture != null)
		{
			int num4 = L(34f);
			SaiyanHud.Draw(renderer, texture, rect3.X + (rect3.Width - num4) / 2, rect3.Y + (rect3.Height - num4) / 2, num4, num4, A(TechniqueArt.White));
		}
		int num5 = rect2.Right + L(14f);
		string text = Strings.Techniques.Toast.ToString(_current.Rank).ToUpperInvariant();
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(30f), text);
		TechniqueArt.Styled(renderer, TechniqueArt.Display, Pt(30f), text, num5, rect.Y + L(27f) - point.Y / 2, new TechniqueArt.Stops(TechniqueArt.White, 0.3f, TechniqueArt.Gold, 0.75f, TechniqueArt.Ki), alpha);
		int spacing = 0;
		string text2 = SaiyanHud.Fit(renderer, TechniqueArt.Heavy, Pt(18f), databaseObject.Name, rect.Right - L(16f) - num5, ref spacing);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(18f), text2, num5, rect.Y + L(58f), A(TechniqueArt.Text));
		for (int i = 1; i <= 5; i++)
		{
			Rectangle rect4 = new Rectangle(num5 + point.X + L(14f) + (i - 1) * L(19f), rect.Y + L(23f), L(15f), L(7f));
			bool flag = i <= _current.Rank;
			bool flag2 = i == _current.Rank && num < 900;
			if (flag)
			{
				TechniqueArt.Glow(renderer, (float)rect4.X + (float)rect4.Width / 2f, (float)rect4.Y + (float)rect4.Height / 2f, L(flag2 ? (16f + 6f * MathF.Sin((float)num / 60f)) : 9f), A(flag2 ? TechniqueArt.Gold : color, 0.5f));
				TechniqueArt.Image(renderer, "tech_pip_mini.png", rect4, A(flag2 ? TechniqueArt.GoldPale : color));
			}
			else
			{
				TechniqueArt.Image(renderer, "tech_pip_mini.png", rect4, A(new Color(255, 10, 14, 24)));
				TechniqueArt.Image(renderer, "tech_pip_mini_bord.png", rect4, A(TechniqueArt.Line2));
			}
		}
		Color A(Color value, float factor = 1f)
		{
			return TechniqueArt.Alpha(value, factor * alpha);
		}
		int L(float value)
		{
			return (int)MathF.Round(value * s);
		}
		int Pt(float css)
		{
			return Math.Max(7, (int)MathF.Round(css * s * 0.75f));
		}
	}
}
