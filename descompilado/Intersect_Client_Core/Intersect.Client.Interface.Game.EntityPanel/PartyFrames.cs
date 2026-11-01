using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.GameObjects;
using Intersect.Models;

namespace Intersect.Client.Interface.Game.EntityPanel;

public sealed class PartyFrames
{
	private sealed class Card
	{
		public Guid Id;

		public readonly GaugeMotion Hp = new GaugeMotion();

		public readonly GaugeMotion Mp = new GaugeMotion();

		public string Name = string.Empty;

		public string Level = string.Empty;

		public string HpText = string.Empty;

		public string Tooltip = string.Empty;

		public bool IsLeader;

		public bool IsFar;

		public int Steps;

		public bool IsDead;

		public readonly List<IGameTexture> Statuses = new List<IGameTexture>();

		public SaiyanHud.Accent Accent = SaiyanHud.BaseAccent;

		public IGameTexture? Texture;

		public Rectangle Frame;

		public string? PortraitKey;
	}

	private sealed class HudPanel : Intersect.Client.Framework.Gwen.Control.Base
	{
		private readonly PartyFrames _owner;

		protected override bool ShouldClip => false;

		public HudPanel(Intersect.Client.Framework.Gwen.Control.Base parent, PartyFrames owner)
			: base(parent, "HudPanel")
		{
			_owner = owner;
			base.MouseInputEnabled = false;
		}

		protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
		{
			base.Render(skin);
			_owner.Draw(skin.Renderer);
		}
	}

	private const int Left = 4;

	private const int CardWidth = 190;

	private const int CardHeight = 36;

	private const int CardGap = 2;

	private const int MaxCards = 5;

	private const int HexCenterX = 18;

	private const int HexCenterY = 18;

	private const int HexRadius = 17;

	private const int HeadSkip = 5;

	private const int HeadRows = 15;

	private const int NameX = 40;

	private const int NameY = 3;

	private const int NameHeight = 12;

	private const int StarSize = 11;

	private const int HpX = 40;

	private const int HpY = 16;

	private const int HpWidth = 134;

	private const int HpHeight = 10;

	private const int MpX = 44;

	private const int MpY = 29;

	private const int MpWidth = 112;

	private const int MpHeight = 4;

	private const int RightEdge = 176;

	private const int StatusSize = 10;

	private const int MaxStatuses = 3;

	private const int StripePeriod = 14;

	private const float StripeSpeed = 18f;

	private const float LowHealthRatio = 0.25f;

	private const float FarAlpha = 0.5f;

	private readonly ImagePanel _window;

	private readonly HudPanel _panel;

	private readonly Intersect.Client.Framework.Gwen.Control.Base[] _hovers = new Intersect.Client.Framework.Gwen.Control.Base[5];

	private readonly Guid[] _hoverIds = new Guid[5];

	private readonly string?[] _hoverTips = new string[5];

	private readonly List<Card> _cards = new List<Card>();

	private long _lastUpdateTime;

	internal Intersect.Client.Framework.Gwen.Control.Base Fenetre => _window;

	public int Bottom => _window.Y + _window.Height;

	public bool IsShown => !_window.IsHidden;

	public PartyFrames(Canvas gameCanvas)
	{
		_window = new ImagePanel(gameCanvas, "PartyFrames")
		{
			ShouldDrawBackground = false,
			ShouldCacheToTexture = false,
			MouseInputEnabled = false
		};
		_window.SetSize(190, 36);
		_panel = new HudPanel(_window, this);
		_panel.SetBounds(0, 0, 190, 36);
		for (int i = 0; i < 5; i++)
		{
			int slot = i;
			Intersect.Client.Framework.Gwen.Control.Base obj = new Intersect.Client.Framework.Gwen.Control.Base(_window, $"Member{i}")
			{
				MouseInputEnabled = true
			};
			obj.SetBounds(40, i * 38, 150, 36);
			obj.Clicked += delegate
			{
				TargetMember(_hoverIds[slot]);
			};
			obj.Hide();
			_hovers[i] = obj;
		}
		_window.Hide();
		_lastUpdateTime = Timing.Global.MillisecondsUtc;
	}

	public void Update()
	{
		Player me = Globals.Me;
		List<IPartyMember> list = me?.Party;
		List<IPartyMember> members = ((list != null && list.Count > 1) ? list.Where((IPartyMember partyMember) => partyMember.Id != me.Id).Take(5).ToList() : new List<IPartyMember>());
		if (me == null || members.Count == 0 || Disposition.Masquee(Disposition.Piece.Groupe))
		{
			if (!_window.IsHidden)
			{
				_window.Hide();
				_cards.Clear();
			}
			return;
		}
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		float elapsed = Math.Clamp((float)(millisecondsUtc - _lastUpdateTime) / 1000f, 0f, 0.1f);
		_lastUpdateTime = millisecondsUtc;
		Guid id = me.Party[0].Id;
		_cards.RemoveAll((Card card2) => members.All((IPartyMember partyMember) => partyMember.Id != card2.Id));
		for (int num = 0; num < members.Count; num++)
		{
			IPartyMember member = members[num];
			Card card = _cards.FirstOrDefault((Card existing) => existing.Id == member.Id);
			bool fresh = card == null;
			if (card == null)
			{
				card = new Card
				{
					Id = member.Id
				};
				_cards.Add(card);
			}
			int num2 = _cards.IndexOf(card);
			if (num2 != num)
			{
				_cards.RemoveAt(num2);
				_cards.Insert(num, card);
			}
			Refresh(card, member, id, me, elapsed, millisecondsUtc, fresh);
			Intersect.Client.Framework.Gwen.Control.Base obj = _hovers[num];
			_hoverIds[num] = member.Id;
			if (_hoverTips[num] != card.Tooltip)
			{
				_hoverTips[num] = card.Tooltip;
				obj.SetToolTipText(card.Tooltip);
			}
			if (obj.IsHidden)
			{
				obj.Show();
			}
		}
		for (int num3 = members.Count; num3 < 5; num3++)
		{
			_hoverIds[num3] = Guid.Empty;
			if (!_hovers[num3].IsHidden)
			{
				_hovers[num3].Hide();
			}
		}
		int num4 = members.Count * 38 - 2;
		_window.SetSize(190, num4);
		_panel.SetBounds(0, 0, 190, num4);
		Point point = Disposition.Poser(Disposition.Piece.Groupe, 190, num4, 4, Top(), _window.Parent);
		_window.SetPosition(point.X, point.Y);
		if (_window.IsHidden)
		{
			_window.Show();
		}
	}

	private static int Top()
	{
		return 98;
	}

	private static void Refresh(Card card, IPartyMember member, Guid leaderId, Player me, float elapsed, long now, bool fresh)
	{
		Entity value;
		Entity entity = ((Globals.Entities.TryGetValue(member.Id, out value) && value is Player { IsDisposed: false } player) ? player : null);
		card.Name = member.Name;
		card.Level = member.Level.ToString();
		card.IsLeader = member.Id == leaderId;
		card.IsFar = IsOutOfRange(entity, me, out card.Steps);
		card.Accent = ((entity != null) ? SaiyanHud.AccentOf(entity) : SaiyanHud.BaseAccent);
		long value2 = ((entity != null) ? entity.Vital[0] : member.Vital[0]);
		long val = ((entity != null) ? entity.MaxVital[0] : member.MaxVital[0]);
		long value3 = ((entity != null) ? entity.Vital[1] : member.Vital[1]);
		long val2 = ((entity != null) ? entity.MaxVital[1] : member.MaxVital[1]);
		val = Math.Max(1L, val);
		val2 = Math.Max(1L, val2);
		value2 = Math.Clamp(value2, 0L, val);
		value3 = Math.Clamp(value3, 0L, val2);
		card.IsDead = value2 <= 0;
		card.HpText = (card.IsDead ? Strings.Parties.KnockedOut.ToString() : $"{(float)value2 * 100f / (float)val:0}%");
		string text = $"{Strings.EntityBox.ShortVital0} {Strings.EntityBox.Vital0Value.ToString(SaiyanHud.Amount(value2), SaiyanHud.Amount(val))} · {Strings.EntityBox.ShortVital1} {Strings.EntityBox.Vital1Value.ToString(SaiyanHud.Amount(value3), SaiyanHud.Amount(val2))}";
		card.Tooltip = (card.IsLeader ? $"{Strings.Parties.Leader} · {text}" : text);
		RefreshStatuses(card, card.IsFar ? null : entity);
		float ratio = (float)value2 / (float)val;
		float ratio2 = (float)value3 / (float)val2;
		if (fresh)
		{
			card.Hp.Snap(ratio);
			card.Mp.Snap(ratio2);
		}
		else
		{
			card.Hp.Update(ratio, elapsed, now);
			card.Mp.Update(ratio2, elapsed, now);
		}
		RefreshPortrait(card, member, entity);
	}

	private static void RefreshPortrait(Card card, IPartyMember member, Entity? entity)
	{
		IGameTexture gameTexture = entity?.Texture;
		string text = entity?.Sprite;
		if (gameTexture == null && !string.IsNullOrWhiteSpace(member.Sprite))
		{
			gameTexture = Globals.ContentManager.GetTexture(TextureType.Entity, member.Sprite);
			text = member.Sprite;
		}
		string text2 = ((gameTexture == null) ? null : (gameTexture.Name + "|" + text));
		if (text2 == card.PortraitKey)
		{
			return;
		}
		card.PortraitKey = text2;
		card.Texture = gameTexture;
		if (gameTexture == null)
		{
			return;
		}
		SpriteSheets.Pose? pose = SpriteSheets.StandingBody(text, gameTexture.Name);
		if (pose.HasValue)
		{
			SpriteSheets.Pose valueOrDefault = pose.GetValueOrDefault();
			if (!valueOrDefault.IsEmpty)
			{
				card.Texture = valueOrDefault.Texture;
				card.Frame = new Rectangle(valueOrDefault.Frame.X, valueOrDefault.Frame.Y, valueOrDefault.Frame.Width, valueOrDefault.Frame.Height);
				return;
			}
		}
		card.Frame = new Rectangle(0, 0, gameTexture.Width / Math.Max(1, Options.Instance.Sprites.NormalFrames), gameTexture.Height / Math.Max(1, Options.Instance.Sprites.Directions));
	}

	private static bool IsOutOfRange(Entity? entity, Player me, out int steps)
	{
		steps = 0;
		if (entity == null || entity.MapId != me.MapId)
		{
			return false;
		}
		steps = Math.Max(Math.Abs(entity.X - me.X), Math.Abs(entity.Y - me.Y));
		return steps > Options.Instance.Party.SharedXpRange;
	}

	private static void RefreshStatuses(Card card, Entity? entity)
	{
		card.Statuses.Clear();
		if (entity == null)
		{
			return;
		}
		int num = entity.Status.Count - 1;
		while (num >= 0 && card.Statuses.Count < 3)
		{
			IStatus status = entity.Status[num];
			if (status.Type != SpellEffect.Transform && DatabaseObject<SpellDescriptor>.TryGet(status.SpellId, out var databaseObject) && !string.IsNullOrWhiteSpace(databaseObject.Icon))
			{
				IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Spell, databaseObject.Icon);
				if (texture != null && !card.Statuses.Contains(texture))
				{
					card.Statuses.Add(texture);
				}
			}
			num--;
		}
	}

	private static void TargetMember(Guid id)
	{
		if (!(id == Guid.Empty) && Globals.Me != null && Globals.Entities.TryGetValue(id, out Entity value) && value is Player { IsDisposed: false } player)
		{
			Globals.Me.TryTarget(player);
		}
	}

	private void Draw(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		for (int i = 0; i < _cards.Count; i++)
		{
			DrawCard(renderer, _cards[i], i * 38, millisecondsUtc);
		}
	}

	private static void DrawCard(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Card card, int top, long now)
	{
		float dim = (card.IsFar ? 0.5f : 1f);
		bool flag = !card.IsDead && card.Hp.Target > 0f && card.Hp.Target < 0.25f;
		float num = (flag ? (0.5f + 0.5f * MathF.Sin((float)now * 0.0069813174f)) : 0f);
		SaiyanHud.Draw(renderer, SaiyanHud.Texture("hud_equipier_plaque.png"), 0, top, Dim(SaiyanHud.White));
		SaiyanHud.Draw(renderer, SaiyanHud.Texture("hud_equipier_accent.png"), 0, top, Dim(card.IsDead ? SaiyanHud.Muted : card.Accent.Main));
		int num2 = 40;
		if (card.IsLeader)
		{
			SaiyanHud.Draw(renderer, SaiyanHud.Texture("hud_equipier_etoile.png"), num2, top + 3, Dim(SaiyanHud.Gold));
			num2 += 14;
		}
		Color color = (card.IsDead ? SaiyanHud.Muted : SaiyanHud.White);
		SaiyanHud.TextLeft(renderer, SaiyanHud.NameFont, 11, card.Name, num2, top + 3, 12, Dim(color));
		num2 += SaiyanHud.Measure(renderer, SaiyanHud.NameFont, 11, card.Name).X + 5;
		SaiyanHud.TextLeft(renderer, SaiyanHud.StrongFont, 9, card.Level, num2, top + 3, 12, Dim(card.IsDead ? SaiyanHud.Muted : SaiyanHud.Gold));
		num2 += SaiyanHud.Measure(renderer, SaiyanHud.StrongFont, 9, card.Level).X + 6;
		if (card.IsFar)
		{
			int num3 = 176 - num2;
			if (num3 >= 24)
			{
				int spacing = 1;
				string text = Strings.Parties.FarSteps.ToString(card.Steps).ToUpperInvariant();
				string text2 = SaiyanHud.Fit(renderer, SaiyanHud.SmallFont, 8, text, num3, ref spacing);
				SaiyanHud.TextSpaced(renderer, SaiyanHud.SmallFont, 8, text2, 176, top + 3 + 1, 12, spacing, Dim(SaiyanHud.Red));
			}
		}
		else
		{
			DrawStatuses(renderer, card, num2, top, dim);
		}
		DrawGauge(renderer, "hud_equipier_pv", 40, top + 16, 134, 10, card.Hp, card.HpText, SaiyanHud.Red, num * 0.9f, stripes: false, now, dim);
		float num4 = ((card.Mp.Target >= 0.999f) ? (0.55f + 0.45f * MathF.Sin((float)now * 0.0028559933f)) : 0f);
		DrawGauge(renderer, "hud_equipier_ki", 44, top + 29, 112, 4, card.Mp, null, SaiyanHud.KiLight, num4 * 0.7f, stripes: true, now, dim);
		DrawPortrait(renderer, card, top, flag, num, dim);
		Color Dim(Color color2)
		{
			if (!(dim < 1f))
			{
				return color2;
			}
			return SaiyanHud.WithAlpha(color2, dim);
		}
	}

	private static void DrawStatuses(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Card card, int left, int top, float dim)
	{
		int num = 176;
		foreach (IGameTexture status in card.Statuses)
		{
			int num2 = num - 10;
			if (num2 < left)
			{
				break;
			}
			SaiyanHud.Draw(renderer, status, num2, top + 3 + 1, 10, 10, SaiyanHud.WithAlpha(SaiyanHud.White, dim));
			num = num2 - 2;
		}
	}

	private static void DrawGauge(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string prefix, int x, int y, int width, int height, GaugeMotion motion, string? value, Color haloColor, float haloAlpha, bool stripes, long now, float dim)
	{
		int num = height / 2;
		Color color = SaiyanHud.WithAlpha(SaiyanHud.White, dim);
		if (haloAlpha > 0.01f)
		{
			SaiyanHud.Draw(renderer, SaiyanHud.Texture(prefix + "_halo.png"), x - 10, y - 10, SaiyanHud.WithAlpha(haloColor, haloAlpha * dim));
		}
		SaiyanHud.Draw(renderer, SaiyanHud.Texture(prefix + "_piste.png"), x, y, color);
		float num2 = Math.Clamp(motion.Shown, 0f, 1f);
		int num3 = ((!(num2 <= 0f)) ? Math.Max((int)MathF.Round((float)width * num2), num + 2) : 0);
		int num4 = (int)MathF.Round((float)width * Math.Clamp(motion.Ghost, 0f, 1f));
		if (num4 > num3)
		{
			SaiyanHud.DrawSlanted(renderer, SaiyanHud.Texture(prefix + "_ombre.png"), x, y, Math.Max(num4, num + 2), num, color);
		}
		if (num3 > 0)
		{
			SaiyanHud.DrawSlanted(renderer, SaiyanHud.Texture(prefix + "_plein.png"), x, y, num3, num, color);
			if (stripes)
			{
				DrawStripes(renderer, x + num, y, num3 - 2 * num, height, now, dim);
			}
			if (motion.Head > 0.01f)
			{
				IGameTexture gameTexture = SaiyanHud.Texture("hud_joueur_tete.png");
				int num5 = x + num3 - num;
				int num6 = Math.Max(x + num, num5 - 14);
				int num7 = num5 - num6;
				if (gameTexture != null && num7 > 0)
				{
					SaiyanHud.Draw(renderer, gameTexture, num6, y, num7, height, SaiyanHud.WithAlpha(SaiyanHud.White, motion.Head * dim), ((float)(14 - num7) + 0.25f) / 16f, 0f, 59f / 64f);
				}
			}
		}
		SaiyanHud.Draw(renderer, SaiyanHud.Texture(prefix + "_dessus.png"), x, y, color);
		if (!string.IsNullOrEmpty(value))
		{
			SaiyanHud.TextRight(renderer, SaiyanHud.StrongFont, 8, value, x + width - 9, y, height, color);
		}
	}

	private static void DrawStripes(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int x, int y, int width, int height, long now, float dim)
	{
		IGameTexture gameTexture = SaiyanHud.Texture("hud_joueur_stries.png");
		if (gameTexture == null || width <= 0)
		{
			return;
		}
		int num = (int)((double)now / 1000.0 * 18.0 % 14.0);
		for (int i = x - num; i < x + width; i += 14)
		{
			int num2 = Math.Max(i, x);
			int num3 = Math.Min(i + 14, x + width);
			if (num3 > num2)
			{
				SaiyanHud.Draw(renderer, gameTexture, num2, y, num3 - num2, height, SaiyanHud.WithAlpha(SaiyanHud.White, dim), (float)(num2 - i) / 14f, 0f, (float)(num3 - i) / 14f);
			}
		}
	}

	private static void DrawPortrait(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Card card, int top, bool lowHealth, float lowPulse, float dim)
	{
		int x = -4;
		int y = top + 18 - 20;
		Color color = SaiyanHud.WithAlpha(SaiyanHud.White, dim);
		SaiyanHud.Draw(renderer, SaiyanHud.Texture("hud_groupe_portrait_fond.png"), x, y, color);
		SaiyanHud.Draw(renderer, SaiyanHud.Texture("hud_groupe_portrait_lueur.png"), x, y, SaiyanHud.WithAlpha(card.Accent.Main, dim));
		if (card.Texture != null)
		{
			DrawSprite(renderer, card, top, card.IsDead ? SaiyanHud.WithAlpha(new Color(255, 92, 96, 108), dim) : color);
		}
		SaiyanHud.Draw(renderer, SaiyanHud.Texture("hud_groupe_portrait_verre.png"), x, y, color);
		Color color2 = card.Accent.Main;
		if (lowHealth)
		{
			SaiyanHud.Draw(renderer, SaiyanHud.Texture("hud_groupe_portrait_halo.png"), -10, top + 18 - 26, SaiyanHud.WithAlpha(SaiyanHud.Red, lowPulse * dim));
			color2 = SaiyanHud.Lerp(card.Accent.Main, SaiyanHud.Red, 0.4f + 0.6f * lowPulse);
		}
		SaiyanHud.Draw(renderer, SaiyanHud.Texture("hud_groupe_portrait_cadre.png"), x, y, SaiyanHud.WithAlpha(color2, dim));
		if (card.IsDead)
		{
			SaiyanHud.TextCenter(renderer, SaiyanHud.StrongFont, 10, Strings.Parties.KnockedOut.ToString(), 2, top + 18 - 7, 32, 14, SaiyanHud.WithAlpha(SaiyanHud.Red, dim));
		}
	}

	private static void DrawSprite(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Card card, int top, Color tint)
	{
		IGameTexture texture = card.Texture;
		if (texture == null)
		{
			return;
		}
		IntersectRenderer intersectRenderer = renderer as IntersectRenderer;
		Rectangle frame = card.Frame;
		if (frame.Width < 1 || frame.Height < 1)
		{
			return;
		}
		int num = top + 18;
		int num2 = SaiyanHud.TopMargin(texture, frame);
		int num3 = frame.Y + Math.Min(Math.Max(0, num2 + 5), Math.Max(0, frame.Height - 15));
		int num4 = Math.Clamp(frame.Y + frame.Height - num3, 0, 15);
		int num5 = 18 - frame.Width;
		int num6 = top + 18 - 15;
		renderer.DrawColor = tint;
		for (int i = 0; i < num4; i++)
		{
			int num7 = num6 + i * 2;
			float num8 = MathF.Abs((float)num7 + 1f - (float)num);
			if (num8 > 13.222f)
			{
				continue;
			}
			float num9 = 15.5f - num8 / 1.732f;
			if (num9 <= 0f)
			{
				continue;
			}
			int num10 = Math.Max(0, (int)MathF.Ceiling((18f - num9 - (float)num5) / 2f));
			int num11 = Math.Min(frame.Width, (int)MathF.Floor((18f + num9 - (float)num5) / 2f));
			if (num11 > num10)
			{
				int num12 = num11 - num10;
				if (intersectRenderer == null)
				{
					renderer.DrawTexturedRect(texture, new Rectangle(num5 + num10 * 2, num7, num12 * 2, 2), tint, (float)(frame.X + num10) / (float)texture.Width, (float)(num3 + i) / (float)texture.Height, (float)(frame.X + num11) / (float)texture.Width, (float)(num3 + i + 1) / (float)texture.Height);
				}
				else
				{
					intersectRenderer.DrawTextureBand(texture, (float)(frame.X + num10) + 0.5f, (float)(num3 + i) + 0.5f, (float)num12 + 0.49f, 1.49f, num5 + num10 * 2, num7, ((float)num12 + 0.49f) * 2f, 2.98f);
				}
			}
		}
	}
}
