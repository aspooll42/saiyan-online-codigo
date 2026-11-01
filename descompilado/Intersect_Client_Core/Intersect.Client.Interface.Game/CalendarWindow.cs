using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Maps;

namespace Intersect.Client.Interface.Game;

public sealed class CalendarWindow : WindowControl
{
	private const int WindowWidth = 760;

	private const int WindowHeight = 600;

	private const int MonthX = 16;

	private const int MonthWidth = 452;

	private const int SideX = 484;

	private const int SideWidth = 260;

	private const int CardsTop = 72;

	private const int TodayHeight = 222;

	private const int CardGap = 14;

	private const int CardsBottom = 580;

	private const int Inset = 14;

	private const int CellX = 12;

	private const int CellWidth = 56;

	private const int CellHeight = 62;

	private const int CellGap = 6;

	private const int GridTop = 118;

	private const int BarWidth = 232;

	private const int BarTop = 130;

	private const int UpcomingRows = 3;

	private const int UpcomingStep = 76;

	private static readonly Color White = new Color(255, 238, 240, 244);

	private static readonly Color Muted = new Color(255, 154, 163, 184);

	private static readonly Color Dim = new Color(255, 108, 116, 137);

	private static readonly Color Gold = new Color(255, 240, 184, 74);

	private static readonly Color PastTint = new Color(255, 150, 150, 162);

	private readonly IFont? _strong;

	private readonly IFont? _semi;

	private readonly IFont? _body;

	private readonly IFont? _bodyStrong;

	private readonly Label _title;

	private readonly Button _close;

	private readonly ImagePanel _monthCard;

	private readonly ImagePanel _todayCard;

	private readonly ImagePanel _upcomingCard;

	private readonly (ImagePanel Chip, Label Text) _yearChip;

	private readonly (ImagePanel Chip, Label Text) _seasonChip;

	private readonly (ImagePanel Chip, Label Text) _keyChip;

	private readonly Label _monthName;

	private readonly Label _monthRank;

	private readonly Label _rhythm1;

	private readonly Label _rhythm2;

	private readonly Label _dayNumber;

	private readonly Label _weekday;

	private readonly Label _monthYear;

	private readonly Label _time;

	private readonly ImagePanel _weatherIcon;

	private readonly Label _weatherName;

	private readonly ImagePanel _marker;

	private readonly Label _nextStep;

	private readonly Label _nextWeather;

	private readonly ImagePanel[] _upcomingIcon = new ImagePanel[3];

	private readonly Label[] _upcomingTitle = new Label[3];

	private readonly Label[] _upcomingDate = new Label[3];

	private readonly Label[] _upcomingWhen = new Label[3];

	private readonly Label _seasonAbbreviation;

	private readonly List<Base> _days = new List<Base>();

	private long _month;

	private string _shownMonth = string.Empty;

	private string _shownTexts = string.Empty;

	private int _laidOutTitleBarHeight = -1;

	public bool IsOpen => !base.IsHidden;

	public CalendarWindow(Canvas gameCanvas)
		: base(gameCanvas, string.Empty, modal: false, "CalendarWindow")
	{
		_strong = Globals.ContentManager.GetFont("rajdhanibold");
		_semi = Globals.ContentManager.GetFont("rajdhanisemibold");
		_body = Globals.ContentManager.GetFont("poppinsmedium");
		_bodyStrong = Globals.ContentManager.GetFont("poppinssemibold");
		DisableResizing();
		SetSize(760, 600);
		SetPosition((gameCanvas.Width - 760) / 2, (gameCanvas.Height - 600) / 2);
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_window_clan.png");
		SetImage(texture, "saiyan_window_clan.png", ControlState.Active);
		SetImage(texture, "saiyan_window_clan.png", ControlState.Inactive);
		base.IsClosable = false;
		if (base.Titlebar is Titlebar titlebar)
		{
			titlebar.Label.IsHidden = true;
		}
		_title = MakeLabel(this, "Title", 20, new Color(255, 242, 242, 242), Globals.ContentManager.GetFont("refuel"));
		_title.Text = Strings.Calendar.Title.ToString().ToUpperInvariant();
		WindowTitle.Apply(_title);
		_close = new Button(this, "Close", disableText: true);
		_close.SetStateTexture(ComponentState.Normal, "saiyan_close.png");
		_close.SetStateTexture(ComponentState.Hovered, "saiyan_close_hover.png");
		_close.SetStateTexture(ComponentState.Active, "saiyan_close_clicked.png");
		_close.Clicked += delegate
		{
			Close();
		};
		_yearChip = MakeChip("Year", 10, Gold, _bodyStrong);
		_seasonChip = MakeChip("Season", 10, White, _bodyStrong);
		_keyChip = MakeChip("Key", 11, Muted, _semi);
		_monthCard = MakeCard(this, "MonthCard");
		ArrowButton("Previous", "gauche", 12, delegate
		{
			_month--;
		});
		ArrowButton("Next", "droite", 412, delegate
		{
			_month++;
		});
		_monthName = MakeLabel(_monthCard, "MonthName", 16, White, _strong);
		_monthName.TextAlign = Pos.Center;
		_monthName.SetBounds(48, 12, 356, 28);
		_monthRank = MakeLabel(_monthCard, "MonthRank", 10, Muted, _body);
		_monthRank.TextAlign = Pos.Center;
		_monthRank.SetBounds(48, 40, 356, 18);
		for (int num = 1; num <= 7; num++)
		{
			int num2 = 12 + (num - 1) * 62;
			Picture(_monthCard, $"Ball{num}", $"calendrier_boule_{num}.png", num2 + 18, 70, 20, 20);
			Label label = MakeLabel(_monthCard, $"Weekday{num}", 9, Muted, _semi);
			label.Text = num.ToString();
			label.TextAlign = Pos.Right | Pos.CenterV;
			label.SetBounds(num2, 92, 28, 14);
			Picture(_monthCard, $"Star{num}", "calendrier_etoile.png", num2 + 28 + 3, 95, 7, 7);
		}
		int num3 = 400;
		int num4 = 12;
		(string, string, int, string)[] array = new(string, string, int, string)[3]
		{
			("Today", "calendrier_legende_jour.png", 12, Strings.Calendar.Today),
			("FullMoon", "calendrier_lune_4.png", 14, Strings.Calendar.FullMoon),
			("Budokai", "calendrier_budokai.png", 14, Strings.Calendar.Budokai)
		};
		for (int num5 = 0; num5 < array.Length; num5++)
		{
			var (text, texture2, num6, text2) = array[num5];
			Picture(_monthCard, "Legend" + text, texture2, num4, num3 + (18 - num6) / 2, num6, num6);
			Label label2 = MakeLabel(_monthCard, "Legend" + text + "Text", 10, Muted, _body);
			label2.Text = text2;
			int num7 = Measure(text2, _body, 9);
			label2.SetBounds(num4 + num6 + 6, num3, num7 + 4, 18);
			num4 += num6 + 6 + num7 + 24;
		}
		int num8 = num3 + 34;
		Rule(_monthCard, "RhythmRule", 12, num8, 428);
		_rhythm1 = MakeLabel(_monthCard, "Rhythm1", 10, Muted, _body);
		_rhythm1.SetBounds(12, num8 + 12, 428, 18);
		_rhythm2 = MakeLabel(_monthCard, "Rhythm2", 10, Muted, _body);
		_rhythm2.SetBounds(12, num8 + 32, 428, 18);
		_todayCard = MakeCard(this, "TodayCard");
		Heading(_todayCard, "TodayHeading", Strings.Calendar.Today);
		_dayNumber = MakeLabel(_todayCard, "DayNumber", 32, White, _strong);
		_dayNumber.SetBounds(14, 30, 64, 44);
		_weekday = MakeLabel(_todayCard, "Weekday", 10, Gold, _bodyStrong);
		_monthYear = MakeLabel(_todayCard, "MonthYear", 10, White, _body);
		_time = MakeLabel(_todayCard, "Time", 22, White, _strong);
		_time.SetBounds(14, 84, 100, 36);
		_weatherName = MakeLabel(_todayCard, "WeatherName", 10, Muted, _body);
		_weatherName.TextAlign = Pos.Right | Pos.CenterV;
		_weatherName.SetBounds(96, 92, 150, 20);
		_weatherIcon = Picture(_todayCard, "WeatherIcon", "meteo_soleil.png", 0, 94, 16, 16);
		Picture(_todayCard, "DayBar", "calendrier_barre_jour.png", 14, 130, 232, 10);
		_marker = Picture(_todayCard, "Marker", "calendrier_repere.png", 14, 127, 3, 16);
		for (int num9 = 0; num9 <= 4; num9++)
		{
			Label label3 = MakeLabel(_todayCard, $"Graduation{num9}", 9, Dim, _semi);
			label3.Text = $"{num9 * 6} h";
			label3.TextAlign = num9 switch
			{
				4 => Pos.Right | Pos.CenterV, 
				0 => Pos.Left | Pos.CenterV, 
				_ => Pos.Center, 
			};
			int num10 = 14 + num9 * 232 / 4;
			label3.SetBounds(num9 switch
			{
				4 => 206, 
				0 => 14, 
				_ => num10 - 20, 
			}, 144, 40, 14);
		}
		_nextStep = MakeLabel(_todayCard, "NextStep", 10, Muted, _body);
		_nextStep.SetBounds(14, 166, 232, 18);
		_nextWeather = MakeLabel(_todayCard, "NextWeather", 10, Muted, _body);
		_nextWeather.SetBounds(14, 186, 232, 18);
		_upcomingCard = MakeCard(this, "UpcomingCard");
		Heading(_upcomingCard, "UpcomingHeading", Strings.Calendar.Upcoming);
		for (int num11 = 0; num11 < 3; num11++)
		{
			int num12 = 40 + num11 * 76;
			ImagePanel[] upcomingIcon = _upcomingIcon;
			int num13 = num11;
			upcomingIcon[num13] = num11 switch
			{
				0 => Picture(_upcomingCard, "UpcomingMoon", "calendrier_lune_4.png", 14, num12 + 4, 28, 28), 
				1 => Picture(_upcomingCard, "UpcomingBudokai", "calendrier_budokai.png", 14, num12 + 4, 28, 28), 
				_ => MakeImage(_upcomingCard, "UpcomingSeason", "saiyan_chip.png", 12, num12 + 7, 34, 22, new Margin(6)), 
			};
			_upcomingTitle[num11] = MakeLabel(_upcomingCard, $"UpcomingTitle{num11}", 10, White, _body);
			_upcomingTitle[num11].SetBounds(56, num12, 190, 20);
			_upcomingDate[num11] = MakeLabel(_upcomingCard, $"UpcomingDate{num11}", 10, Muted, _body);
			_upcomingDate[num11].SetBounds(56, num12 + 20, 190, 16);
			_upcomingWhen[num11] = MakeLabel(_upcomingCard, $"UpcomingWhen{num11}", 10, Muted, _body);
			_upcomingWhen[num11].SetBounds(56, num12 + 37, 190, 16);
			if (num11 < 2)
			{
				Rule(_upcomingCard, $"UpcomingRule{num11}", 14, num12 + 64, 232);
			}
		}
		_upcomingTitle[0].Text = Strings.Calendar.FullMoon;
		_upcomingTitle[1].Text = Strings.Calendar.Budokai;
		_seasonAbbreviation = MakeLabel(_upcomingIcon[2], "Abbreviation", 9, White, _bodyStrong);
		_seasonAbbreviation.TextAlign = Pos.Center;
		_seasonAbbreviation.SetBounds(0, 0, 34, 22);
		Layout();
		Hide();
	}

	public void Toggle()
	{
		if (IsOpen)
		{
			Close();
			return;
		}
		_month = Calendrier.Maintenant.MoisAbsolu;
		_shownMonth = string.Empty;
		_shownTexts = string.Empty;
		Layout();
		Show();
		BringToFront();
		Update();
	}

	public void Update()
	{
		if (!base.IsHidden)
		{
			if (base.TitleBarBounds.Height != _laidOutTitleBarHeight)
			{
				Layout();
			}
			Calendrier.Date maintenant = Calendrier.Maintenant;
			string text = $"{maintenant.Numero}:{_month}";
			if (text != _shownMonth)
			{
				_shownMonth = text;
				BuildMonth(maintenant);
			}
			float num = (Meteo.HeureAffichee % 24f + 24f) % 24f;
			string text2 = $"{maintenant.Numero}:{(int)(num * 60f)}:{Meteo.NomTempsAffiche}:{Meteo.NomSuivantAffiche}:{(int)(Meteo.SecondesAvantSuivant / 60f)}:{Time.Vitesse}";
			if (text2 != _shownTexts)
			{
				_shownTexts = text2;
				RefreshTexts(maintenant, num);
			}
			_marker.SetPosition(14 + (int)MathF.Round(num / 24f * 232f) - 1, 127);
		}
	}

	private void BuildMonth(Calendrier.Date today)
	{
		foreach (Base day in _days)
		{
			day.Parent?.RemoveChild(day, dispose: true);
		}
		_days.Clear();
		var (num, num2) = Calendrier.MoisDe(_month);
		_monthName.Text = Calendrier.NomDuMois(num2);
		string text = ((num2 == 0) ? Strings.Calendar.FirstMonthRank : Strings.Calendar.MonthRank).ToString(num2 + 1, Calendrier.NomDeSaison(Calendrier.SaisonDu(num2)));
		_monthRank.Text = ((num == today.Annee) ? text : (text + " · " + Strings.Calendar.Age.ToString(num)));
		long num3 = Calendrier.PremierJourDu(_month);
		for (int i = 1; i <= 28; i++)
		{
			long num4 = num3 + i - 1;
			bool flag = num4 == today.Numero;
			bool flag2 = num4 < today.Numero;
			bool flag3 = i == 14;
			bool flag4 = num2 == 4 && i == 21;
			ImagePanel imagePanel = MakeImage(_monthCard, $"Day{i}", flag ? "saiyan_slot_selected.png" : "saiyan_slot.png", 12 + (i - 1) % 7 * 62, 118 + (i - 1) / 7 * 68, 56, 62, new Margin(6));
			_days.Add(imagePanel);
			if (flag2)
			{
				imagePanel.RenderColor = PastTint;
			}
			if (flag3)
			{
				MakeImage(imagePanel, "Gold", "calendrier_cadre_or.png", 0, 0, 56, 62, new Margin(6));
			}
			Label label = MakeLabel(imagePanel, "Number", 11, flag2 ? Dim : White, _semi);
			label.Text = i.ToString();
			label.SetBounds(7, 5, 30, 20);
			ImagePanel imagePanel2 = Picture(imagePanel, "Moon", $"calendrier_lune_{Calendrier.PhaseDeLune(i)}.png", 35, 41, 14, 14);
			if (flag4)
			{
				ImagePanel imagePanel3 = Picture(imagePanel, "Budokai", "calendrier_budokai.png", 7, 41, 14, 14);
				imagePanel3.RenderColor = (flag2 ? PastTint : imagePanel3.RenderColor);
			}
			imagePanel2.RenderColor = (flag2 ? PastTint : imagePanel2.RenderColor);
			if (flag3 || flag4)
			{
				imagePanel.MouseInputEnabled = true;
				imagePanel.TooltipFont = _body;
				imagePanel.TooltipFontSize = 9;
				imagePanel.SetToolTipText((flag3 && flag4) ? $"{Strings.Calendar.FullMoon} · {Strings.Calendar.Budokai}" : (flag3 ? ((string)Strings.Calendar.FullMoon) : ((string)Strings.Calendar.Budokai)));
			}
		}
	}

	private void RefreshTexts(Calendrier.Date today, float hour)
	{
		Calendrier.Saison saison = today.Saison;
		_yearChip.Text.Text = Strings.Calendar.Age.ToString(today.Annee);
		_seasonChip.Text.Text = Calendrier.NomDeSaison(saison);
		_seasonChip.Text.SetTextColor(SeasonColor(saison), ComponentState.Normal);
		_keyChip.Text.Text = DropdownPanel.KeyOf(Control.OpenCalendar) ?? string.Empty;
		LayoutChips();
		_dayNumber.Text = today.Jour.ToString();
		int num = 14 + Measure(_dayNumber.Text, _strong, 32) + 10;
		_weekday.Text = Calendrier.NomDuJour(today.JourDeSemaine);
		_weekday.SetBounds(num, 36, 246 - num, 20);
		_monthYear.Text = Strings.Calendar.MonthYear.ToString(Calendrier.NomDuMois(today.Mois), today.Annee);
		_monthYear.SetBounds(num, 56, 246 - num, 18);
		_time.Text = Calendrier.TexteHeure(hour);
		string nomTempsAffiche = Meteo.NomTempsAffiche;
		_weatherName.Text = nomTempsAffiche;
		_weatherIcon.IsHidden = nomTempsAffiche.Length == 0;
		if (nomTempsAffiche.Length > 0)
		{
			_weatherIcon.Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "meteo_" + Meteo.IconeTemps + ".png");
			_weatherIcon.SetPosition(246 - Measure(nomTempsAffiche, _body, 10) - 22, 94);
		}
		var (text, num2) = Calendrier.ProchaineEtape(hour);
		_nextStep.Text = Strings.Calendar.NextStep.ToString(text, Calendrier.DureeCourte(num2 * 3600.0 / (double)Time.Vitesse));
		string nomSuivantAffiche = Meteo.NomSuivantAffiche;
		_nextWeather.Text = ((nomSuivantAffiche.Length == 0 || nomTempsAffiche.Length == 0) ? string.Empty : Strings.Calendar.ThenWeather.ToString(nomSuivantAffiche, Calendrier.DureeCourte(Meteo.SecondesAvantSuivant)));
		double instant = Calendrier.ProchainePleineLune(today);
		Upcoming(0, today, instant, laNuit: true);
		double instant2 = Calendrier.ProchainBudokai(today);
		Upcoming(1, today, instant2, laNuit: false);
		(Calendrier.Saison Saison, double Debut) tuple2 = Calendrier.SaisonSuivante(today);
		Calendrier.Saison item = tuple2.Saison;
		double item2 = tuple2.Debut;
		string text2 = Calendrier.NomDeSaison(item);
		_upcomingTitle[2].Text = text2;
		_seasonAbbreviation.Text = ((text2.Length > 3) ? text2.Substring(0, 3) : text2);
		_seasonAbbreviation.SetTextColor(SeasonColor(item), ComponentState.Normal);
		Upcoming(2, today, item2, laNuit: false);
		float vitesse = Time.Vitesse;
		_rhythm1.Text = Strings.Calendar.Rhythm1.ToString(Calendrier.Duree(1f / vitesse), Calendrier.Duree(24f / vitesse));
		_rhythm2.Text = Strings.Calendar.Rhythm2.ToString(Calendrier.Duree(672f / vitesse), Calendrier.Duree(8064f / vitesse));
	}

	private void Upcoming(int row, Calendrier.Date today, double instant, bool laNuit)
	{
		Calendrier.Date date = Calendrier.DateA(instant);
		string text = Calendrier.JourEtMois(date.Jour, date.Mois);
		_upcomingDate[row].Text = ((date.Annee == today.Annee) ? text : (text + " · " + Strings.Calendar.Age.ToString(date.Annee)));
		_upcomingWhen[row].Text = Calendrier.Echeance(today, instant, laNuit) + " · " + Calendrier.EnTempsReel(instant - today.Instant);
	}

	private static Color SeasonColor(Calendrier.Saison season)
	{
		return season switch
		{
			Calendrier.Saison.Printemps => new Color(255, 158, 224, 122), 
			Calendrier.Saison.Ete => new Color(255, 255, 201, 92), 
			Calendrier.Saison.Automne => new Color(255, 255, 154, 85), 
			_ => new Color(255, 169, 212, 255), 
		};
	}

	private new int Y(int windowY)
	{
		return windowY - _laidOutTitleBarHeight;
	}

	private void Layout()
	{
		_laidOutTitleBarHeight = base.TitleBarBounds.Height;
		_title.SetSize(400, 38);
		_title.SetPosition(26, Y(10));
		_close.SetSize(24, 24);
		_close.SetPosition(720, Y(16));
		_monthCard.SetBounds(16, Y(72), 452, 508);
		_todayCard.SetBounds(484, Y(72), 260, 222);
		int num = 308;
		_upcomingCard.SetBounds(484, Y(num), 260, 580 - num);
		LayoutChips();
	}

	private void LayoutChips()
	{
		int num = 708;
		(ImagePanel, Label)[] array = new(ImagePanel, Label)[3] { _keyChip, _seasonChip, _yearChip };
		for (int i = 0; i < array.Length; i++)
		{
			(ImagePanel, Label) tuple = array[i];
			ImagePanel item = tuple.Item1;
			Label item2 = tuple.Item2;
			int num2 = ((!string.IsNullOrEmpty(item2.Text)) ? (Measure(item2.Text, item2.Font, item2.FontSize) + 20) : 0);
			item.IsHidden = num2 == 0;
			item.SetBounds(num - num2, Y(18), num2, 24);
			item2.SetBounds(0, 0, num2, 24);
			num -= num2 + ((num2 != 0) ? 8 : 0);
		}
	}

	private (ImagePanel Chip, Label Text) MakeChip(string name, int size, Color color, IFont? font)
	{
		ImagePanel imagePanel = MakeImage(this, name + "Chip", "saiyan_chip.png", 0, 0, 32, 24, new Margin(6));
		Label label = MakeLabel(imagePanel, "Text", size, color, font);
		label.TextAlign = Pos.Center;
		return (Chip: imagePanel, Text: label);
	}

	private void ArrowButton(string name, string direction, int x, Action turn)
	{
		Button button = new Button(_monthCard, name, disableText: true);
		button.SetStateTexture(ComponentState.Normal, "calendrier_fleche_" + direction + ".png");
		button.SetStateTexture(ComponentState.Hovered, "calendrier_fleche_" + direction + "_survol.png");
		button.SetStateTexture(ComponentState.Active, "calendrier_fleche_" + direction + "_survol.png");
		button.SetBounds(x, 18, 28, 28);
		button.Clicked += delegate
		{
			turn();
		};
	}

	private void Heading(Base parent, string name, string text)
	{
		Label label = MakeLabel(parent, name, 10, Muted, _strong);
		label.Text = text.ToUpperInvariant();
		label.SetBounds(14, 10, 232, 20);
	}

	private static int Measure(string? text, IFont? font, int size)
	{
		if (!string.IsNullOrEmpty(text) && font != null && Graphics.Renderer != null)
		{
			return (int)MathF.Ceiling(Graphics.Renderer.MeasureText(text, font, size, 1f).X);
		}
		return (text?.Length ?? 0) * size / 2;
	}

	private static ImagePanel Picture(Base parent, string name, string texture, int x, int y, int width, int height)
	{
		return MakeImage(parent, name, texture, x, y, width, height, null);
	}

	private static ImagePanel MakeImage(Base parent, string name, string texture, int x, int y, int width, int height, Margin? margin)
	{
		ImagePanel imagePanel = new ImagePanel(parent, name)
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, texture),
			TextureNinePatchMargin = margin,
			MouseInputEnabled = false
		};
		imagePanel.SetBounds(x, y, width, height);
		return imagePanel;
	}

	private static void Rule(Base parent, string name, int x, int y, int width)
	{
		MakeImage(parent, name, "hdv_filet.png", x, y, width, 1, null);
	}

	private static Label MakeLabel(Base parent, string name, int size, Color color, IFont? font)
	{
		Label label = new Label(parent, name)
		{
			Font = font,
			FontSize = size,
			AutoSizeToContents = false,
			MouseInputEnabled = false,
			TextAlign = (Pos.Left | Pos.CenterV)
		};
		label.SetTextColor(color, ComponentState.Normal);
		return label;
	}

	private static ImagePanel MakeCard(Base parent, string name)
	{
		return new ImagePanel(parent, name)
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_card.png"),
			TextureNinePatchMargin = new Margin(8),
			MouseInputEnabled = false
		};
	}
}
