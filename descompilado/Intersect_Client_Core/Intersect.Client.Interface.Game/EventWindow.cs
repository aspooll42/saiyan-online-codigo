using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core.Controls;
using Intersect.Client.Entities;
using Intersect.Client.Entities.Events;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Database;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Typewriting;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Client.Networking;
using Intersect.Client.Utilities;
using Intersect.Configuration;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Framework.Threading;
using Intersect.Utilities;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Interface.Game;

public class EventWindow : Panel
{
	private sealed record OptionRow(ImagePanel Row, ImagePanel Badge, Label Number, ImagePanel? Arrow, Label Text, EventResponseType Response);

	private const int BoxMaxWidth = 780;

	private const int Overhang = 34;

	private const int PortraitWidth = 128;

	private const int PortraitHeight = 150;

	private const int PortraitLeft = 18;

	private const int PortraitInset = 5;

	private const int GlowWidth = 400;

	private const int GlowHeight = 260;

	private const int PortraitHeadY = 26;

	private const int SidePadding = 22;

	private const int NameTop = 8;

	private const int TextTop = 50;

	private const int TextTopWithoutName = 18;

	private const int PromptFontSize = 11;

	private const int MinTextHeight = 40;

	private const int MaxTextHeight = 124;

	private const int TextSlack = 6;

	private const int ScrollBarReserve = 16;

	private const int OptionHeight = 28;

	private const int OptionGap = 4;

	private const int BadgeSize = 22;

	private const int ContinueWidth = 150;

	private const int HintHeight = 20;

	private const int BottomMargin = 76;

	private static readonly Color Orange = new Color(255, 255, 172, 82);

	private static readonly Color PromptColor = new Color(255, 232, 234, 242);

	private static readonly Color OptionColor = new Color(255, 214, 220, 232);

	private static readonly Color HintColor = new Color(255, 120, 130, 156);

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private static readonly Color RuleColor = new Color(160, 255, 122, 26);

	private static readonly Dictionary<string, (int Top, int Gauche, int Droite)> PortraitTops = new Dictionary<string, (int, int, int)>();

	private readonly IFont? _font;

	private readonly IFont? _strongFont;

	private readonly ImagePanel _box;

	private readonly Label _name;

	private readonly ScrollControl _promptScroller;

	private readonly RichLabel _promptLabel;

	private readonly Label _hint;

	private readonly List<OptionRow> _options = new List<OptionRow>();

	private readonly int _boxHeight;

	private Point _canvasSize;

	private string? _hintState;

	private bool _continueArmed;

	private bool _typewriting;

	private readonly long _typewriterResponseDelay = ClientConfiguration.Instance.TypewriterResponseDelay;

	private Typewriter? _writer;

	private readonly Dialog _dialog;

	private static EventWindow? _instance;

	public static EventWindow? Current => _instance;

	private bool WriterDone => _writer?.IsDone ?? true;

	private bool ResponseLocked
	{
		get
		{
			if (_writer != null && _writer.IsDone)
			{
				return Timing.Global.MillisecondsUtc - _writer.DoneAtMilliseconds < _typewriterResponseDelay;
			}
			return false;
		}
	}

	private EventWindow(Canvas gameCanvas, Dialog dialog)
		: base(gameCanvas, "EventWindow")
	{
		ThreadQueue.Default.ThrowIfNotOnMainThread();
		_dialog = dialog;
		_font = GameContentManager.Current.GetFont("chakrapetch");
		_strongFont = GameContentManager.Current.GetFont("rajdhanibold");
		base.Dock = Pos.None;
		base.ShouldDrawBackground = false;
		base.MouseInputEnabled = false;
		Event speaker = FindSpeaker(dialog.EventId);
		int num = Math.Max(420, Math.Min(780, gameCanvas.Width - 32));
		_box = new ImagePanel(this, "Box")
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_pnj_boite.png"),
			TextureNinePatchMargin = new Margin(16),
			MouseInputEnabled = true
		};
		_box.Clicked += delegate
		{
			OnBoxClicked();
		};
		ImagePanel imagePanel = new ImagePanel(_box, "Glow")
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_pnj_lueur.png"),
			MouseInputEnabled = false
		};
		bool flag = BuildPortrait(dialog, speaker);
		int num2 = (flag ? 166 : 22);
		int num3 = num - num2 - 22;
		string text = SpeakerName(speaker);
		_name = new Label(this, "SpeakerName")
		{
			Font = _strongFont,
			FontSize = 16,
			AutoSizeToContents = false,
			MouseInputEnabled = false,
			TextAlign = (Pos.Left | Pos.CenterV),
			Text = (text ?? string.Empty),
			IsHidden = (text == null)
		};
		_name.SetTextColor(Orange, ComponentState.Normal);
		ImagePanel imagePanel2 = new ImagePanel(this, "SpeakerRule")
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "inv_plein.png"),
			RenderColor = RuleColor,
			MouseInputEnabled = false,
			IsHidden = (text == null)
		};
		_promptScroller = new ScrollControl(this, "PromptScroller")
		{
			Padding = Padding.Zero,
			ShouldDrawBackground = false,
			MouseInputEnabled = true,
			OverflowX = OverflowBehavior.Hidden,
			OverflowY = OverflowBehavior.Auto
		};
		_promptScroller.VerticalScrollBar.SetBackgroundTemplate(null, "saiyan_scroll_bg.png");
		_promptScroller.VerticalScrollBar.SetScrollBarImage(null, "saiyan_scroll_bar.png", ComponentState.Normal);
		_promptScroller.VerticalScrollBar.SetScrollBarImage(null, "saiyan_scroll_bar_hover.png", ComponentState.Hovered);
		_promptScroller.VerticalScrollBar.SetScrollBarImage(null, "saiyan_scroll_bar_hover.png", ComponentState.Active);
		_promptLabel = new RichLabel(_promptScroller, "PromptLabel")
		{
			Dock = Pos.Fill,
			Padding = Padding.Zero,
			ShouldDrawBackground = false,
			Font = _font,
			FontSize = 11,
			MouseInputEnabled = false
		};
		int num4 = MeasurePrompt(num3 - 16);
		bool flag2 = num4 + 6 > 124;
		int num5 = Math.Clamp(num4 + 6, 40, 124);
		_promptScroller.OverflowY = ((!flag2) ? OverflowBehavior.Hidden : OverflowBehavior.Auto);
		CreateOptions();
		_hint = new Label(this, "KeyboardHint")
		{
			Font = _font,
			FontSize = 10,
			AutoSizeToContents = false,
			MouseInputEnabled = false,
			TextAlign = (Pos.Right | Pos.CenterV)
		};
		_hint.SetTextColor(HintColor, ComponentState.Normal);
		int num6 = ((_options.Count > 1) ? (_options.Count * 28 + (_options.Count - 1) * 4) : 28);
		int num7 = ((text == null) ? 18 : 50);
		_boxHeight = Math.Max(flag ? 130 : 0, num7 + num5 + 10 + num6 + 4 + 20 + 4);
		SetSize(num, 34 + _boxHeight);
		_box.SetBounds(0, 34, num, _boxHeight);
		int num8 = Math.Min(260, _boxHeight);
		imagePanel.SetTextureRect(0, 0, 400, num8);
		imagePanel.SetBounds(0, 0, Math.Min(400, num), num8);
		_name.SetBounds(num2, 42, num3, 30);
		imagePanel2.SetBounds(num2, 73, 42, 2);
		_promptScroller.SetBounds(num2, 34 + num7, num3, num5);
		int num9 = 34 + num7 + num5 + 10;
		if (_options.Count > 1)
		{
			for (int num10 = 0; num10 < _options.Count; num10++)
			{
				LayoutOption(_options[num10], num2, num9 + num10 * 32, num3);
			}
		}
		else if (_options.Count == 1)
		{
			LayoutOption(_options[0], num2 + num3 - 150, num9, 150);
		}
		_hint.SetBounds(num2, 34 + _boxHeight - 20 - 4, num3, 20);
		PlaceOnCanvas(gameCanvas.Size);
		ApplyTypewriter();
		RefreshHint();
		RunOnMainThread(delegate(EventWindow @this)
		{
			@this._promptScroller.ScrollToTop();
		}, this);
		bool dimmedEventWindowBackground = ClientConfiguration.Instance.DimmedEventWindowBackground;
		MakeModal(dimmedEventWindowBackground);
		BringToFront();
		Interface.InputBlockingComponents.Add(this);
		ApplicationContext.CurrentContext.Logger?.LogTrace("Event window opened");
	}

	private void PlaceOnCanvas(Point canvasSize)
	{
		_canvasSize = canvasSize;
		SetPosition((canvasSize.X - base.Width) / 2, canvasSize.Y - 34 - _boxHeight - 76);
	}

	private static Event? FindSpeaker(Guid eventId)
	{
		if (eventId == Guid.Empty)
		{
			return null;
		}
		foreach (MapInstance item in MapInstance.Lookup.Values.OfType<MapInstance>())
		{
			if (item.LocalEntities.TryGetValue(eventId, out Entity value) && value is Event result)
			{
				return result;
			}
		}
		return null;
	}

	private static string? SpeakerName(Event? speaker)
	{
		string text = speaker?.Name?.Trim();
		if (string.IsNullOrEmpty(text) || text.StartsWith("New Event", StringComparison.OrdinalIgnoreCase) || text.StartsWith("Nouvel", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		return text;
	}

	private bool BuildPortrait(Dialog dialog, Event? speaker)
	{
		IGameTexture gameTexture = (string.IsNullOrWhiteSpace(dialog.Face) ? null : Globals.ContentManager.GetTexture(TextureType.Face, dialog.Face));
		IGameTexture gameTexture2 = null;
		(int, int, int, int) frame = default((int, int, int, int));
		EventGraphic eventGraphic = speaker?.Graphic;
		string spriteName = ((eventGraphic != null && eventGraphic.Type == EventGraphicType.Sprite) ? eventGraphic.Filename : null);
		if (gameTexture == null)
		{
			SpriteSheets.Pose? pose = SpriteSheets.StandingBody(spriteName);
			if (pose.HasValue)
			{
				SpriteSheets.Pose valueOrDefault = pose.GetValueOrDefault();
				if (!valueOrDefault.IsEmpty)
				{
					gameTexture2 = valueOrDefault.Texture;
					frame = valueOrDefault.Frame;
				}
			}
		}
		if (gameTexture == null && gameTexture2 == null)
		{
			return false;
		}
		ImagePanel imagePanel = new ImagePanel(this, "PortraitBack")
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_pnj_portrait.png"),
			MouseInputEnabled = false
		};
		imagePanel.SetBounds(18, 0, 128, 150);
		int num = 118;
		int num2 = 140;
		ImagePanel imagePanel2 = new ImagePanel(imagePanel, "PortraitImage")
		{
			MouseInputEnabled = false
		};
		if (gameTexture != null)
		{
			float num3 = Math.Max((float)num / (float)Math.Max(1, gameTexture.Width), (float)num2 / (float)Math.Max(1, gameTexture.Height));
			int num4 = Math.Clamp((int)Math.Round((float)num / num3), 1, Math.Max(1, gameTexture.Width));
			int h = Math.Clamp((int)Math.Round((float)num2 / num3), 1, Math.Max(1, gameTexture.Height));
			imagePanel2.Texture = gameTexture;
			imagePanel2.SetTextureRect((gameTexture.Width - num4) / 2, 0, num4, h);
			imagePanel2.SetBounds(5, 5, num, num2);
		}
		else if (gameTexture2 != null)
		{
			(int Top, int Gauche, int Droite) tuple = PortraitBounds(gameTexture2, frame);
			int item = tuple.Top;
			int item2 = tuple.Gauche;
			int item3 = tuple.Droite;
			int num5 = Math.Max(1, item3 - item2 + 1);
			int num6 = ((item3 >= item2) ? Math.Clamp((int)Math.Round((float)num * 0.64f / (float)num5), 2, 6) : Math.Clamp((int)Math.Round((float)num * 1.35f / (float)frame.Item3), 2, 5));
			int num7 = 26 / num6;
			int num8 = Math.Min(frame.Item3, num / num6);
			int num9 = ((item3 >= item2) ? Math.Clamp((item2 + item3 + 1 - num8) / 2, 0, frame.Item3 - num8) : ((frame.Item3 - num8) / 2));
			int num10 = Math.Max(0, item - num7);
			int num11 = Math.Max(0, num7 - item) * num6;
			int num12 = Math.Min(frame.Item4 - num10, (num2 - num11) / num6);
			if (num8 < 1 || num12 < 1)
			{
				imagePanel.Parent?.RemoveChild(imagePanel, dispose: true);
				return false;
			}
			imagePanel2.Texture = gameTexture2;
			imagePanel2.SetTextureRect(frame.Item1 + num9, frame.Item2 + num10, num8, num12);
			imagePanel2.SetBounds(5 + (num - num8 * num6) / 2, 5 + num11, num8 * num6, num12 * num6);
		}
		ImagePanel imagePanel3 = new ImagePanel(imagePanel, "PortraitFrame")
		{
			Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_pnj_portrait_cadre.png"),
			MouseInputEnabled = false
		};
		imagePanel3.SetBounds(0, 0, 128, 150);
		return true;
	}

	private static (int Top, int Gauche, int Droite) PortraitBounds(IGameTexture texture, (int X, int Y, int Width, int Height) frame)
	{
		string key = $"{texture.Name}:{frame.X},{frame.Y},{frame.Width},{frame.Height}";
		if (PortraitTops.TryGetValue(key, out (int, int, int) value))
		{
			return value;
		}
		if (frame.X < 0 || frame.Y < 0 || frame.X + frame.Width > texture.Width || frame.Y + frame.Height > texture.Height || !PixelsPlanche.Lire(texture, out Color[] pixels) || pixels.Length < texture.Width * texture.Height)
		{
			return (Top: 0, Gauche: 0, Droite: -1);
		}
		int num = -1;
		int num2 = frame.Width;
		int num3 = -1;
		for (int i = 0; i < frame.Height; i++)
		{
			int num4 = (frame.Y + i) * texture.Width + frame.X;
			for (int j = 0; j < frame.Width; j++)
			{
				if (pixels[num4 + j].A > 8)
				{
					num = ((num < 0) ? i : num);
					num2 = Math.Min(num2, j);
					num3 = Math.Max(num3, j);
				}
			}
		}
		value = (Math.Max(0, num), num2, num3);
		PortraitTops[key] = value;
		return value;
	}

	private int MeasurePrompt(int width)
	{
		_promptLabel.SetSize(width, 16);
		_promptLabel.ClearText();
		foreach (ColorizedText item in TextColorParser.Parse(_dialog.Prompt ?? string.Empty, PromptColor))
		{
			_promptLabel.AddText(item.Text, item.Color, Alignments.Left, _font, 11);
		}
		_promptLabel.ForceImmediateRebuild();
		return _promptLabel.Height;
	}

	private void ApplyTypewriter()
	{
		SkipRender();
		int typewriting;
		if (ClientConfiguration.Instance.TypewriterEnabled)
		{
			GameDatabase? database = Globals.Database;
			typewriting = ((database == null || database.TypewriterBehavior != TypewriterBehavior.Off) ? 1 : 0);
		}
		else
		{
			typewriting = 0;
		}
		_typewriting = (byte)typewriting != 0;
		if (!_typewriting)
		{
			_writer = null;
			return;
		}
		List<ColorizedText> list = TextColorParser.Parse(_dialog.Prompt ?? string.Empty, PromptColor);
		_promptLabel.ClearText();
		_writer = new Typewriter(list.ToArray(), delegate(string text, Color color)
		{
			_promptLabel.AppendText(text, color, Alignments.Left, _font, 11);
		});
	}

	private void CreateOptions()
	{
		string[] array = _dialog.Options.Where((string value) => !string.IsNullOrEmpty(value)).ToArray();
		bool flag = array.Length < 1;
		if (flag)
		{
			array = new string[1] { Strings.EventWindow.Continue };
		}
		for (int num = 0; num < array.Length; num++)
		{
			EventResponseType response = (EventResponseType)(num + 1);
			ImagePanel imagePanel = new ImagePanel(this, $"Option{num}")
			{
				TextureNinePatchMargin = new Margin(6),
				MouseInputEnabled = true
			};
			ImagePanel imagePanel2 = new ImagePanel(imagePanel, "Badge")
			{
				Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_pnj_numero.png"),
				MouseInputEnabled = false
			};
			Label label = new Label(imagePanel2, "Number")
			{
				Font = _strongFont,
				FontSize = 11,
				AutoSizeToContents = false,
				MouseInputEnabled = false,
				TextAlign = Pos.Center,
				Text = (flag ? string.Empty : (num + 1).ToString())
			};
			label.SetTextColor(Orange, ComponentState.Normal);
			ImagePanel arrow = null;
			if (flag)
			{
				arrow = new ImagePanel(imagePanel2, "Arrow")
				{
					Texture = Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_pnj_fleche.png"),
					RenderColor = Orange,
					MouseInputEnabled = false
				};
			}
			Label label2 = new Label(imagePanel, "Text")
			{
				Font = _strongFont,
				FontSize = 13,
				AutoSizeToContents = false,
				MouseInputEnabled = false,
				TextAlign = (Pos.Left | Pos.CenterV),
				Text = array[num]
			};
			label2.SetTextColor(OptionColor, ComponentState.Normal);
			OptionRow option = new OptionRow(imagePanel, imagePanel2, label, arrow, label2, response);
			imagePanel.HoverEnter += delegate
			{
				Highlight(option, on: true);
			};
			imagePanel.HoverLeave += delegate
			{
				Highlight(option, on: false);
			};
			imagePanel.Clicked += delegate
			{
				CloseEventResponse(response);
			};
			_options.Add(option);
		}
	}

	private static void LayoutOption(OptionRow option, int x, int y, int width)
	{
		option.Row.SetBounds(x, y, width, 28);
		option.Badge.SetBounds(4, 3, 22, 22);
		option.Number.SetBounds(0, 0, 22, 22);
		option.Arrow?.SetBounds(8, 6, 7, 10);
		option.Text.SetBounds(36, 0, width - 40, 28);
	}

	private static void Highlight(OptionRow option, bool on)
	{
		option.Row.Texture = (on ? Globals.ContentManager.GetTexture(TextureType.Gui, "saiyan_pnj_choix.png") : null);
		option.Badge.Texture = Globals.ContentManager.GetTexture(TextureType.Gui, on ? "saiyan_pnj_numero_survol.png" : "saiyan_pnj_numero.png");
		option.Number.SetTextColor(on ? Night : Orange, ComponentState.Normal);
		if (option.Arrow != null)
		{
			option.Arrow.RenderColor = (on ? Night : Orange);
		}
		option.Text.SetTextColor(on ? Orange : OptionColor, ComponentState.Normal);
	}

	public bool ChooseByNumber(int number)
	{
		if (number < 1 || number > _options.Count)
		{
			return false;
		}
		CloseEventResponse(_options[number - 1].Response);
		return true;
	}

	private void OnBoxClicked()
	{
		if (!WriterDone)
		{
			SkipTypewriting();
		}
		else if (_options.Count == 1)
		{
			CloseEventResponse(_options[0].Response);
		}
	}

	private static string? InteractKeyName()
	{
		if (!Controls.ActiveControls.TryGetMappingFor(Control.AttackInteract, out ControlMapping mapping))
		{
			return null;
		}
		ControlBinding controlBinding = mapping.Bindings.FirstOrDefault((ControlBinding candidate) => candidate != null && !candidate.IsMouseKey && candidate.Key != Keys.None);
		if (controlBinding == null)
		{
			return null;
		}
		try
		{
			return Strings.Keys.FormatKeyName(controlBinding.Modifier, controlBinding.Key);
		}
		catch (KeyNotFoundException)
		{
			return null;
		}
	}

	private void RefreshHint()
	{
		string text = ((!WriterDone) ? "skip" : ((_options.Count > 1) ? "choices" : "continue"));
		if (!(text == _hintState))
		{
			_hintState = text;
			string text2 = InteractKeyName();
			Label hint = _hint;
			string text3 = ((text == "skip") ? ((text2 == null) ? string.Empty : Strings.EventWindow.HintSkip.ToString(text2)) : ((!(text == "choices")) ? ((text2 == null) ? ((string)Strings.EventWindow.HintContinueEnter) : Strings.EventWindow.HintContinue.ToString(text2)) : Strings.EventWindow.HintChoices.ToString(_options.Count)));
			hint.Text = text3;
		}
	}

	private void Update()
	{
		if (!base.IsVisibleInTree)
		{
			return;
		}
		Canvas canvas = base.Canvas;
		if (canvas != null && canvas.Size != _canvasSize)
		{
			PlaceOnCanvas(canvas.Size);
		}
		bool writerDone = WriterDone;
		RefreshHint();
		foreach (OptionRow option in _options)
		{
			option.Row.IsVisibleInParent = writerDone;
		}
		if (writerDone)
		{
			bool responseLocked = ResponseLocked;
			foreach (OptionRow option2 in _options)
			{
				option2.Row.IsDisabled = responseLocked;
			}
			if (_options.Count == 1 && !responseLocked)
			{
				if (Controls.IsControlJustPressed(Control.AttackInteract))
				{
					_continueArmed = true;
				}
				else if (_continueArmed && !Controls.IsControlPressed(Control.AttackInteract))
				{
					CloseEventResponse(_options[0].Response);
				}
			}
		}
		else if (Controls.IsControlJustPressed(Control.AttackInteract))
		{
			SkipTypewriting();
			PostLayout.Enqueue(_promptScroller.ScrollToBottom);
		}
		else
		{
			int index = Randomization.Next(0, ClientConfiguration.Instance.TypewriterSounds.Count);
			_writer.Write(ClientConfiguration.Instance.TypewriterSounds.ElementAtOrDefault(index));
			_promptScroller.ScrollToBottom();
		}
	}

	public static void ShowOrUpdateDialog(Canvas canvas)
	{
		EventWindow instance = _instance;
		if (instance != null)
		{
			instance.Update();
			return;
		}
		Dialog dialog = Globals.EventDialogs.FirstOrDefault();
		if (dialog != null)
		{
			_instance = new EventWindow(canvas, dialog);
		}
	}

	public void CloseEventResponse(EventResponseType response)
	{
		if (!WriterDone)
		{
			SkipTypewriting();
		}
		else if (!ResponseLocked)
		{
			Dialog dialog = _dialog;
			if (dialog != null && !dialog.ResponseSent)
			{
				PacketSender.SendEventResponse((byte)response, dialog);
				dialog.ResponseSent = true;
				EnsureDestroyed();
			}
		}
	}

	private void SkipTypewriting()
	{
		Typewriter? writer = _writer;
		if (writer != null && !writer.IsDone)
		{
			_writer.End();
		}
	}

	private void EnsureControlRestored()
	{
		if (_instance == this)
		{
			_instance = null;
		}
		Interface.InputBlockingComponents.Remove(this);
		RemoveModal();
	}

	private void EnsureDestroyed()
	{
		EnsureControlRestored();
		Base parent = base.Parent;
		if (parent != null)
		{
			parent.RemoveChild(this, dispose: true);
		}
		else
		{
			DelayedDelete();
		}
	}

	protected override void Dispose(bool disposing)
	{
		EnsureControlRestored();
		base.Dispose(disposing);
	}

	protected override void OnMouseClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseClicked(mouseButton, mousePosition, userAction);
		SkipTypewriting();
	}
}
