using System;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Config;

namespace Intersect.Client.Interface.Game;

public sealed class HellPanel
{
	private const int MinWidth = 320;

	private const int PaddingX = 24;

	private const int Height = 54;

	private const int Top = 112;

	private static readonly Color Red = new Color(255, 232, 64, 64);

	private static readonly Color LightGrey = new Color(255, 196, 204, 220);

	private readonly Canvas _canvas;

	private readonly ImagePanel _panel;

	private readonly Label _sentence;

	private readonly Label _bail;

	internal Base Fenetre => _panel;

	public HellPanel(Canvas canvas)
	{
		_canvas = canvas;
		_panel = new ImagePanel(canvas, "HellPanel")
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_card.png"),
			TextureNinePatchMargin = new Margin(8),
			MouseInputEnabled = false,
			IsHidden = true
		};
		_sentence = new Label(_panel, "HellSentence")
		{
			Font = Globals.ContentManager.GetFont("rajdhanibold"),
			FontSize = 14,
			AutoSizeToContents = true,
			MouseInputEnabled = false
		};
		_sentence.SetTextColor(Red, ComponentState.Normal);
		_bail = new Label(_panel, "HellBail")
		{
			Font = Globals.ContentManager.GetFont("chakrapetch"),
			FontSize = 10,
			AutoSizeToContents = true,
			MouseInputEnabled = false
		};
		_bail.SetTextColor(LightGrey, ComponentState.Normal);
	}

	public void Update()
	{
		Player me = Globals.Me;
		if (me == null || !me.IsDetained)
		{
			_panel.IsHidden = true;
			return;
		}
		bool flag = me.DetentionPlace == DetentionPlace.Prison;
		long hellSentenceRemainingMs = me.HellSentenceRemainingMs;
		string text = $"{hellSentenceRemainingMs / 60000:00}:{hellSentenceRemainingMs / 1000 % 60:00}";
		SetText(_sentence, (flag ? Strings.Hell.PrisonSentence : Strings.Hell.Sentence).ToString(text));
		SetText(_bail, (flag ? Strings.Hell.PrisonBail : Strings.Hell.Bail).ToString(MarketWindow.FormatAmount(me.HellBail)));
		int num = Math.Max(320, Math.Max(_sentence.Width, _bail.Width) + 48);
		_panel.SetBounds((_canvas.Width - num) / 2, 112, num, 54);
		_sentence.SetPosition((num - _sentence.Width) / 2, 6);
		_bail.SetPosition((num - _bail.Width) / 2, 30);
		_panel.IsHidden = false;
	}

	private static void SetText(Label label, string text)
	{
		if (!(label.Text == text))
		{
			label.Text = text;
			label.SizeToContents();
		}
	}
}
