using System;
using System.Linq;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Techniques;

namespace Intersect.Client.Interface.Shared;

public static class WindowTitle
{
	private sealed class Decoration : Intersect.Client.Framework.Gwen.Control.Base
	{
		private static readonly Color Shadow = new Color(255, 0, 0, 0);

		private readonly Label _title;

		private readonly Color _color;

		private string? _measuredText;

		private int _measuredWidth;

		private int _size;

		protected override bool ShouldClip => false;

		private Color Accent
		{
			get
			{
				if (_color.R <= 200 || _color.G <= 200 || _color.B <= 200)
				{
					return _color;
				}
				return TechniqueArt.Ki;
			}
		}

		public Decoration(Label title, Color color)
			: base(title, "TitleDecoration")
		{
			_title = title;
			_color = color;
			base.MouseInputEnabled = false;
			SetBounds(0, 0, 1, 1);
		}

		protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
		{
			string text = _title.Text;
			if (string.IsNullOrEmpty(text) || _title.IsHidden)
			{
				return;
			}
			IFont display = TechniqueArt.Display;
			if (display == null)
			{
				return;
			}
			IFont font = _title.Font;
			if (font == null)
			{
				return;
			}
			Color textColor = _title.TextColor;
			if ((object)textColor != null && textColor.A == 0)
			{
				textColor = _title.GetTextColor(ComponentState.Normal);
				if ((object)textColor != null && textColor.A == 0)
				{
					textColor = _title.TextColorOverride;
					if ((object)textColor == null || textColor.A <= 0)
					{
						goto IL_0097;
					}
				}
			}
			HideText(_title);
			goto IL_0097;
			IL_0097:
			Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
			string text2 = text.ToUpperInvariant();
			Point point = renderer.MeasureText(font, _title.FontSize, text);
			if (!string.Equals(_measuredText, text2, StringComparison.Ordinal))
			{
				_measuredText = text2;
				int fontSize = _title.FontSize;
				_size = DisplaySizes[0];
				int[] displaySizes = DisplaySizes;
				foreach (int num in displaySizes)
				{
					if (num > fontSize)
					{
						break;
					}
					Point point2 = TechniqueArt.Measure(renderer, display, num, text2);
					if (point2.X + point2.Y / 5 + 4 > Math.Max(_title.Width, point.X) || (float)point2.Y * 0.6f > (float)Math.Max(_title.Height, point.Y))
					{
						break;
					}
					_size = num;
				}
				_measuredWidth = TechniqueArt.Measure(renderer, display, _size, text2).X;
			}
			Point point3 = TechniqueArt.Measure(renderer, display, _size, text2);
			Point characterPosition = _title.GetCharacterPosition(0);
			int num2 = characterPosition.X;
			Pos textAlign = _title.TextAlign;
			if ((textAlign & Pos.CenterH) != Pos.None)
			{
				num2 += (point.X - _measuredWidth) / 2;
			}
			else if ((textAlign & Pos.Right) != Pos.None)
			{
				num2 += point.X - _measuredWidth;
			}
			int num3 = characterPosition.Y + point.Y / 2 - point3.Y / 2;
			Color accent = Accent;
			TechniqueArt.Styled(renderer, display, _size, text2, num2 + 1, num3 + 2, TechniqueArt.Stops.Solid(Shadow), 0.55f);
			TechniqueArt.Styled(renderer, display, _size, text2, num2, num3, new TechniqueArt.Stops(TechniqueArt.White, 0.3f, TechniqueArt.Mix(accent, TechniqueArt.White, 0.45f), 0.75f, accent));
			int y = Math.Min(_title.Height - 2, num3 + (int)((float)point3.Y * 0.74f) + 2);
			int w = Math.Max(18, Math.Min(_measuredWidth / 3, 46));
			TechniqueArt.FillHorizontal(renderer, new Rectangle(num2, y, w, 2), accent, TechniqueArt.Alpha(accent, 0.6f), TechniqueArt.Alpha(accent, 0f));
		}
	}

	public const string FontName = "sairatitre";

	private static readonly int[] DisplaySizes = new int[12]
	{
		12, 14, 15, 16, 18, 20, 22, 23, 24, 26,
		27, 30
	};

	public static void Apply(Label title)
	{
		if (!title.Children.OfType<Decoration>().Any())
		{
			IFont font = Globals.ContentManager.GetFont("sairatitre");
			if (font != null)
			{
				Color textColorOverride = title.TextColorOverride;
				Color color = (((object)textColorOverride != null && textColorOverride.A > 0) ? textColorOverride : (title.GetTextColor(ComponentState.Normal) ?? new Color(255, 242, 242, 242)));
				title.UsesHeadingFace = false;
				title.Font = font;
				title.FontSize = Math.Clamp(title.FontSize + 2, 8, 32);
				HideText(title);
				new Decoration(title, color);
			}
		}
	}

	private static void HideText(Label title)
	{
		Color color = new Color(0, 0, 0, 0);
		ComponentState[] values = Enum.GetValues<ComponentState>();
		foreach (ComponentState state in values)
		{
			title.SetTextColor(color, state);
		}
		title.TextColor = color;
		title.TextColorOverride = null;
	}
}
