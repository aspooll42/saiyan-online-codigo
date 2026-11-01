using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Localization;
using Intersect.Framework.Core;

namespace Intersect.Client.Interface.Game.Techniques;

public abstract class CharteScreen : Intersect.Client.Framework.Gwen.Control.Base
{
	private readonly record struct Hit(Rectangle Rect, string Key, Action Action);

	protected internal readonly record struct Design(float S, float OriginX, float OriginY)
	{
		public const float Width = 1600f;

		public const float Height = 900f;

		public static Design For(int width, int height)
		{
			float num = Math.Max(0.4f, Math.Min((float)width / 1600f, (float)height / 900f));
			return new Design(num, ((float)width - 1600f * num) / 2f, ((float)height - 900f * num) / 2f);
		}

		public int X(float x)
		{
			return (int)MathF.Round(OriginX + x * S);
		}

		public int Y(float y)
		{
			return (int)MathF.Round(OriginY + y * S);
		}

		public int L(float length)
		{
			return (int)MathF.Round(length * S);
		}

		public Rectangle R(float x, float y, float width, float height)
		{
			int num = X(x);
			int num2 = Y(y);
			return new Rectangle(num, num2, X(x + width) - num, Y(y + height) - num2);
		}

		public int Pt(float cssPixels)
		{
			return Math.Max(7, (int)MathF.Round(cssPixels * S * 0.75f));
		}

		public float Em(float em, float cssPixels)
		{
			return em * cssPixels * S;
		}
	}

	protected readonly record struct Gauge(string Label, string Value, Color Color);

	private List<Hit> _hits = new List<Hit>();

	private List<Hit> _drawingHits = new List<Hit>();

	protected Design _d = Design.For(1600, 900);

	protected long _openedAt;

	protected float _fade = 1f;

	private bool? _details;

	public static CharteScreen? Ouvert { get; private set; }

	public static bool EstOuvert => Ouvert?.IsVisibleInTree ?? false;

	protected virtual string? DetailsKey => null;

	protected bool Details
	{
		get
		{
			bool valueOrDefault = _details == true;
			if (!_details.HasValue)
			{
				valueOrDefault = DetailsKey != null && Globals.Database?.LoadPreference(DetailsKey) == "1";
				_details = valueOrDefault;
			}
			return _details.Value;
		}
	}

	private string DetailsToggleText => (Details ? Strings.Techniques.Simple : Strings.Techniques.Details).ToString().ToUpperInvariant();

	protected virtual bool BlocksHover => false;

	protected CharteScreen(Canvas gameCanvas, string name)
		: base(gameCanvas, name)
	{
		base.MouseInputEnabled = true;
		SetBounds(0, 0, gameCanvas.Width, gameCanvas.Height);
		base.IsHidden = true;
	}

	public bool IsVisible()
	{
		return !base.IsHidden;
	}

	public override void Show()
	{
		Ouvert = this;
		FitCanvas();
		_openedAt = Timing.Global.MillisecondsUtc;
		base.Show();
		BringToFront();
		Audio.AddGameSound("menu_open.wav", loop: false);
	}

	public override void Hide()
	{
		if (Ouvert == this)
		{
			Ouvert = null;
		}
		base.Hide();
	}

	protected virtual void ToggleDetails()
	{
		if (DetailsKey != null)
		{
			_details = !Details;
			Globals.Database?.SavePreference(DetailsKey, Details ? "1" : "0");
			Audio.AddGameSound("menu_click.wav", loop: false);
		}
	}

	protected int DetailsToggleWidth(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		return _d.L(12f) + TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(13f), DetailsToggleText, _d.Em(0.08f, 13f)) + _d.L(10f) + KeyChipWidth(renderer, "D") + _d.L(12f);
	}

	protected int DrawDetailsToggleLeft(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int left, int centerY)
	{
		return DrawDetailsToggle(renderer, left + DetailsToggleWidth(renderer), centerY);
	}

	protected int DrawDetailsToggle(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int right, int centerY)
	{
		string detailsToggleText = DetailsToggleText;
		bool flag = IsHot("details");
		int num = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(13f), detailsToggleText, _d.Em(0.08f, 13f));
		int num2 = DetailsToggleWidth(renderer);
		Rectangle rect = new Rectangle(right - num2, centerY - _d.L(14f), num2, _d.L(28f));
		TechniqueArt.Patch(renderer, "tech_chanfrein8.png", rect, A(new Color(204, 12, 17, 28)), 10);
		TechniqueArt.Patch(renderer, "tech_chanfrein8_bord.png", rect, A(flag ? TechniqueArt.Gold : TechniqueArt.Line2), 10);
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(13f), detailsToggleText, rect.X + _d.L(12f), centerY, _d.Em(0.08f, 13f), A(flag ? TechniqueArt.Text : TechniqueArt.Soft));
		KeyChip(renderer, "D", rect.X + _d.L(12f) + num + _d.L(10f), centerY, flag);
		AddHit(rect, "details", ToggleDetails);
		return num2;
	}

	internal virtual bool HandleKey(Keys modifier, Keys key)
	{
		if (modifier == Keys.None && key == Keys.D && DetailsKey != null)
		{
			ToggleDetails();
			return true;
		}
		return false;
	}

	protected void FitCanvas()
	{
		Intersect.Client.Framework.Gwen.Control.Base parent = base.Parent;
		if (parent != null && (base.Width != parent.Width || base.Height != parent.Height || base.X != 0 || base.Y != 0))
		{
			SetBounds(0, 0, parent.Width, parent.Height);
		}
	}

	protected Color A(Color color)
	{
		return TechniqueArt.Alpha(color, _fade);
	}

	protected Color A(Color color, float alpha)
	{
		return TechniqueArt.Alpha(color, alpha * _fade);
	}

	protected int Pt(float cssPixels)
	{
		return _d.Pt(cssPixels);
	}

	protected void AddHit(Rectangle rect, string key, Action action)
	{
		_drawingHits.Add(new Hit(rect, key, action));
	}

	protected Point LocalMouse()
	{
		return CanvasPosToLocal(InputHandler.MousePosition);
	}

	protected bool IsHot(string key)
	{
		if (!IsHovered || BlocksHover)
		{
			return false;
		}
		Point point = LocalMouse();
		for (int num = _hits.Count - 1; num >= 0; num--)
		{
			if (_hits[num].Rect.Contains(point.X, point.Y))
			{
				return _hits[num].Key == key;
			}
		}
		return false;
	}

	protected string? HitKeyAt(Point local)
	{
		for (int num = _hits.Count - 1; num >= 0; num--)
		{
			if (_hits[num].Rect.Contains(local.X, local.Y))
			{
				return _hits[num].Key;
			}
		}
		return null;
	}

	protected virtual bool InterceptClick()
	{
		return false;
	}

	protected virtual void OnEmptyClick(MouseButton mouseButton, Point local)
	{
	}

	protected override void OnMouseClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseClicked(mouseButton, mousePosition, userAction);
		Point local = CanvasPosToLocal(mousePosition);
		if (mouseButton != MouseButton.Left)
		{
			OnEmptyClick(mouseButton, local);
		}
		else
		{
			if (InterceptClick())
			{
				return;
			}
			for (int num = _hits.Count - 1; num >= 0; num--)
			{
				if (_hits[num].Rect.Contains(local.X, local.Y))
				{
					Audio.AddGameSound("menu_click.wav", loop: false);
					_hits[num].Action();
					return;
				}
			}
			OnEmptyClick(mouseButton, local);
		}
	}

	protected bool IsOverHit(Point local)
	{
		foreach (Hit hit in _hits)
		{
			if (hit.Rect.Contains(local.X, local.Y))
			{
				return true;
			}
		}
		return false;
	}

	protected sealed override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		_drawingHits = new List<Hit>();
		_d = Design.For(base.Width, base.Height);
		float num = Math.Clamp((float)(millisecondsUtc - _openedAt) / 240f, 0f, 1f);
		_fade = 1f - (1f - num) * (1f - num);
		DrawScreen(renderer, millisecondsUtc);
		_hits = _drawingHits;
	}

	protected abstract void DrawScreen(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now);

	protected void Panel(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect)
	{
		TechniqueArt.Patch(renderer, "tech_panneau.png", rect, A(TechniqueArt.White), 12);
	}

	protected void Heading(Intersect.Client.Framework.Gwen.Renderer.Base renderer, float x, float y, float width, string title, string? detail, Color? detailColor = null)
	{
		int centerY = _d.Y(y + 9.4f);
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(13f), title.ToUpperInvariant(), _d.X(x), centerY, _d.Em(0.16f, 13f), A(TechniqueArt.Soft));
		if (!string.IsNullOrEmpty(detail))
		{
			TechniqueArt.SpacedRight(renderer, TechniqueArt.Label, Pt(13f), detail.ToUpperInvariant(), _d.X(x + width), centerY, _d.Em(0.1f, 13f), A(detailColor ?? TechniqueArt.Dim));
		}
	}

	protected int KeyChip(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string text, int x, int centerY, bool hot = false)
	{
		int num = KeyChipWidth(renderer, text);
		int num2 = _d.L(24f);
		Rectangle rect = new Rectangle(x, centerY - num2 / 2, num, num2);
		TechniqueArt.Fill(renderer, rect, A(new Color(255, 17, 24, 39)));
		TechniqueArt.Outline(renderer, rect, A(hot ? TechniqueArt.Ki : TechniqueArt.Line2));
		TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Bottom - _d.L(3f), rect.Width, _d.L(3f)), A(hot ? TechniqueArt.Ki : TechniqueArt.Line2));
		TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(12f), text, rect.X + rect.Width / 2, rect.Y + (rect.Height - _d.L(2f)) / 2, A(TechniqueArt.Text));
		return num;
	}

	protected int KeyChipWidth(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string text)
	{
		return Math.Max(_d.L(30f), TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(12f), text).X + _d.L(14f));
	}

	protected void DrawBackdrop(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Color accent)
	{
		Rectangle rect = new Rectangle(0, 0, base.Width, base.Height);
		TechniqueArt.Fill(renderer, rect, A(TechniqueArt.Black, 0.55f));
		TechniqueArt.Image(renderer, "tech_voile.png", rect, A(TechniqueArt.White));
		TechniqueArt.Image(renderer, "tech_voile_haut.png", rect, A(TechniqueArt.White));
		TechniqueArt.GlowRect(renderer, _d.R(560f, 120f, 700f, 520f), A(accent, 0.3f), additive: false);
	}

	protected void DrawTopBar(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string title, string thread, IReadOnlyList<Gauge> gauges)
	{
		Rectangle rect = new Rectangle(0, _d.Y(0f), base.Width, _d.L(74f));
		TechniqueArt.FillVertical(renderer, rect, A(new Color(230, 6, 8, 14)), A(new Color(102, 6, 8, 14)));
		TechniqueArt.Fill(renderer, new Rectangle(0, rect.Bottom - 1, base.Width, 1), A(TechniqueArt.White, 0.06f));
		int num = _d.Y(37f);
		string text = title.ToUpperInvariant();
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, Pt(40f), text);
		int num2 = _d.X(36f);
		TechniqueArt.GlowRect(renderer, new Rectangle(num2 - _d.L(18f), num - _d.L(30f), point.X + _d.L(36f), _d.L(60f)), A(TechniqueArt.Ki, 0.28f), additive: false);
		TechniqueArt.Styled(renderer, TechniqueArt.Display, Pt(40f), text, num2, num - point.Y / 2, new TechniqueArt.Stops(TechniqueArt.White, 0.3f, new Color(255, 255, 194, 122), 0.75f, TechniqueArt.Ki), _fade);
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), thread.ToUpperInvariant(), num2 + point.X + _d.L(26f), num, _d.Em(0.22f, 12f), A(TechniqueArt.Dim));
		int num3 = _d.X(1564f);
		int x = TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(13f), Strings.Techniques.Close).X;
		string text2 = Strings.Techniques.Escape.ToString();
		int num4 = KeyChipWidth(renderer, text2);
		int num5 = _d.L(24f);
		int num6 = num3 - x - _d.L(8f) - num4;
		bool flag = IsHot("fermer");
		KeyChip(renderer, text2, num6, num, flag);
		TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(13f), Strings.Techniques.Close, num3, num, A(flag ? TechniqueArt.Text : TechniqueArt.Soft));
		AddHit(new Rectangle(num6, num - num5 / 2 - 2, num3 - num6, num5 + 4), "fermer", Hide);
		int num7 = num6 - _d.L(26f);
		for (int num8 = gauges.Count - 1; num8 >= 0; num8--)
		{
			Gauge gauge = gauges[num8];
			string text3 = gauge.Label.ToUpperInvariant();
			int val = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(12f), text3, _d.Em(0.2f, 12f));
			int x2 = TechniqueArt.Measure(renderer, TechniqueArt.Heavy, Pt(26f), gauge.Value).X;
			TechniqueArt.SpacedRight(renderer, TechniqueArt.Label, Pt(12f), text3, num7, _d.Y(24f), _d.Em(0.2f, 12f), A(TechniqueArt.Dim));
			TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(26f), gauge.Value, num7, _d.Y(46f), A(gauge.Color));
			num7 -= Math.Max(val, x2) + _d.L(22f);
		}
	}

	protected void DrawHexEmblem(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle outer, IGameTexture? icon, int glyph, Color color, float iconRatio)
	{
		TechniqueArt.Image(renderer, "tech_hexa_bord.png", outer, A(TechniqueArt.Mix(color, TechniqueArt.White, 0.95f)));
		int num = Math.Max(1, (int)MathF.Round((float)outer.Width * 2f / 62f));
		Rectangle rect = new Rectangle(outer.X + num, outer.Y + num, outer.Width - num * 2, outer.Height - num * 2);
		TechniqueArt.Image(renderer, "tech_hexa.png", rect, A(new Color(255, 8, 11, 19)));
		TechniqueArt.Image(renderer, "tech_hexa_radial.png", rect, A(TechniqueArt.Mix(color, new Color(255, 22, 28, 44), 0.3f)));
		int num2 = (int)MathF.Round((float)rect.Width * iconRatio);
		Rectangle rect2 = new Rectangle(rect.X + (rect.Width - num2) / 2, rect.Y + (rect.Height - num2) / 2, num2, num2);
		TechniqueArt.Glow(renderer, (float)rect2.X + (float)num2 / 2f, (float)rect2.Y + (float)num2 / 2f, (float)num2 * 0.75f, A(color, 0.45f));
		if (icon != null)
		{
			SaiyanHud.Draw(renderer, icon, rect2.X, rect2.Y, rect2.Width, rect2.Height, A(TechniqueArt.White));
		}
		else if (glyph >= 0)
		{
			TechniqueArt.Cell(renderer, "tech_familles.png", glyph, 40, rect2, A(color));
		}
	}
}
