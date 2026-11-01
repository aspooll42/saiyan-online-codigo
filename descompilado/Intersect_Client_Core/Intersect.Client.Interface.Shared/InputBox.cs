using System;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Control.EventArguments.InputSubmissionEvent;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Localization;
using Intersect.Framework;
using Intersect.Framework.Reflection;

namespace Intersect.Client.Interface.Shared;

public class InputBox : Window
{
	private readonly IFont? _defaultFont;

	private readonly Panel _inputPanel;

	private readonly Panel _buttonPanel;

	private readonly ScrollControl _promptScroller;

	private readonly RichLabel _promptLabel;

	private readonly LabeledSlider? _numericInputSlider;

	private readonly TextBoxNumeric? _numericInputTextbox;

	private readonly TextBox? _stringInput;

	private readonly Button? _cancelButton;

	private readonly Button? _noButton;

	private readonly Button? _okayButton;

	private readonly Button? _yesButton;

	private int _initialScrollerInnerHeight;

	private int _initialMinimumHeight;

	private const int DialogWidth = 440;

	private const int DialogHeight = 200;

	private const int DialogTitleBarHeight = 40;

	private const int DialogSide = 16;

	private const int DialogButtonWidth = 128;

	private const int DialogButtonHeight = 28;

	private static readonly Color DialogWhite = new Color(255, 242, 242, 242);

	private static readonly Color DialogEntry = new Color(255, 196, 204, 220);

	private static readonly Color DialogDim = new Color(255, 150, 160, 182);

	private static readonly Color DialogNight = new Color(255, 11, 14, 23);

	private static readonly Color DialogOrangeLight = new Color(255, 255, 172, 82);

	private bool _skinned;

	public InputType InputType { get; }

	public new object? UserData { get; private set; }

	public SubmissionValue Value { get; private set; }

	public BooleanSubmissionValue BooleanValue => (Value as BooleanSubmissionValue) ?? throw CreateInvalidInputTypeException();

	public NumericalSubmissionValue NumericalValue => (Value as NumericalSubmissionValue) ?? throw CreateInvalidInputTypeException();

	public StringSubmissionValue StringValue => (Value as StringSubmissionValue) ?? throw CreateInvalidInputTypeException();

	public event GwenEventHandler<InputSubmissionEventArgs>? Submitted;

	public event GwenEventHandler<EventArgs>? Canceled;

	private InvalidOperationException CreateInvalidInputTypeException()
	{
		return new InvalidOperationException($"This {"InputBox"} is being used for {InputType} input.");
	}

	private (Panel, Panel) CreatePanelsForInputType(InputType inputType)
	{
		return inputType switch
		{
			InputType.Okay => CreatePanelsForOkay(), 
			InputType.YesNo => CreatePanelsForYesNo(), 
			InputType.YesNoCancel => CreatePanelsForYesNoCancel(), 
			InputType.NumericInput => CreatePanelsForNumericInput(), 
			InputType.NumericSliderInput => CreatePanelsForNumericSliderInput(), 
			InputType.TextInput => CreatePanelsForTextInput(), 
			_ => throw Exceptions.UnreachableInvalidEnum(inputType), 
		};
	}

	private (Panel, Panel) CreatePanelsForOkay()
	{
		Panel panel = new Panel(this, "_inputPanel")
		{
			BackgroundColor = Color.Transparent,
			Dock = Pos.Fill
		};
		ScrollControl parent = new ScrollControl(panel, "_promptScroller")
		{
			Dock = Pos.Fill
		};
		RichLabel richLabel = new RichLabel(parent, "_promptLabel")
		{
			Dock = Pos.Fill,
			Font = _defaultFont,
			FontSize = 12
		};
		Panel panel2 = new Panel(this, "_buttonPanel")
		{
			BackgroundColor = Color.Transparent,
			Dock = Pos.Bottom
		};
		Button button = new Button(panel2, "_okayButton")
		{
			Alignment = new Alignments[1] { Alignments.Center },
			AutoSizeToContents = true,
			Font = _defaultFont,
			FontSize = 12,
			MinimumSize = new Point(120, 24),
			Padding = new Padding(8, 4),
			Text = Strings.InputBox.Okay,
		};
		Button button2 = button;
		button2.Clicked += OkayButtonOnClicked;
		return (panel, panel2);
	}

	private void OkayButtonOnClicked(Base @base, MouseButtonState mouseButtonState)
	{
		SubmitInput();
	}

	private (Panel, Panel) CreatePanelsForYesNo()
	{
		Panel panel = new Panel(this, "_inputPanel")
		{
			BackgroundColor = Color.Transparent,
			Dock = Pos.Fill
		};
		ScrollControl parent = new ScrollControl(panel, "_promptScroller")
		{
			Dock = Pos.Fill
		};
		RichLabel richLabel = new RichLabel(parent, "_promptLabel")
		{
			Dock = Pos.Fill,
			Font = _defaultFont,
			FontSize = 12
		};
		Panel panel2 = new Panel(this, "_buttonPanel")
		{
			BackgroundColor = Color.Transparent,
			Dock = Pos.Bottom
		};
		Button button = new Button(panel2, "_yesButton")
		{
			Alignment = new Alignments[1] { Alignments.Left },
			AutoSizeToContents = true,
			Font = _defaultFont,
			FontSize = 12,
			MinimumSize = new Point(120, 24),
			Padding = new Padding(8, 4),
			Text = Strings.InputBox.Yes,
		};
		Button button2 = button;
		button2.Clicked += YesButtonOnClicked;
		button = new Button(panel2, "_noButton");
		button.Alignment = new Alignments[1] { Alignments.Right };
		button.AutoSizeToContents = true;
		button.Font = _defaultFont;
		button.FontSize = 12;
		button.MinimumSize = new Point(120, 24);
		button.Padding = new Padding(8, 4);
		button.Text = Strings.InputBox.No;
		Button button3 = button;
		button3.Clicked += NoButtonOnClicked;
		return (panel, panel2);
	}

	private (Panel, Panel) CreatePanelsForYesNoCancel()
	{
		Panel panel = new Panel(this, "_inputPanel")
		{
			BackgroundColor = Color.Transparent,
			Dock = Pos.Fill
		};
		ScrollControl parent = new ScrollControl(panel, "_promptScroller")
		{
			Dock = Pos.Fill
		};
		RichLabel richLabel = new RichLabel(parent, "_promptLabel")
		{
			Dock = Pos.Fill,
			Font = _defaultFont,
			FontSize = 12
		};
		Panel panel2 = new Panel(this, "_buttonPanel")
		{
			BackgroundColor = Color.Transparent,
			Dock = Pos.Bottom
		};
		Button button = new Button(panel2, "_yesButton")
		{
			Alignment = new Alignments[1] { Alignments.Left },
			AutoSizeToContents = true,
			Font = _defaultFont,
			FontSize = 12,
			MinimumSize = new Point(120, 24),
			Padding = new Padding(8, 4),
			Text = Strings.InputBox.Yes,
		};
		Button button2 = button;
		button2.Clicked += YesButtonOnClicked;
		button = new Button(panel2, "_noButton");
		button.Alignment = new Alignments[1] { Alignments.Center };
		button.AutoSizeToContents = true;
		button.Font = _defaultFont;
		button.FontSize = 12;
		button.MinimumSize = new Point(120, 24);
		button.Padding = new Padding(8, 4);
		button.Text = Strings.InputBox.No;
		Button button3 = button;
		button3.Clicked += NoButtonOnClicked;
		button = new Button(panel2, "_cancelButton");
		button.Alignment = new Alignments[1] { Alignments.Right };
		button.AutoSizeToContents = true;
		button.Font = _defaultFont;
		button.FontSize = 12;
		button.MinimumSize = new Point(120, 24);
		button.Padding = new Padding(8, 4);
		button.Text = Strings.InputBox.Cancel;
		Button button4 = button;
		button4.Clicked += CancelButtonOnClicked;
		return (panel, panel2);
	}

	private void NoButtonOnClicked(Base sender, MouseButtonState arguments)
	{
		if (InputType == InputType.YesNoCancel)
		{
			Value = new BooleanSubmissionValue(Value: false);
			SubmitInput();
		}
		else
		{
			CancelInput();
		}
	}

	private (Panel, Panel) CreatePanelsForNumericInput()
	{
		Panel panel = new Panel(this, "_inputPanel")
		{
			BackgroundColor = Color.Transparent,
			Dock = Pos.Fill
		};
		ScrollControl parent = new ScrollControl(panel, "_promptScroller")
		{
			Dock = Pos.Fill
		};
		RichLabel richLabel = new RichLabel(parent, "_promptLabel")
		{
			Dock = Pos.Fill,
			Font = _defaultFont,
			FontSize = 12
		};
		TextBoxNumeric textBoxNumeric = new TextBoxNumeric(panel, "_numericInputTextbox")
		{
			AutoSizeToContents = false,
			Dock = Pos.Bottom,
			Font = _defaultFont,
			FontSize = 12,
			Margin = new Margin(0, 8, 0, 0),
			TextAlign = Pos.Left,
			WrappingBehavior = WrappingBehavior.NoWrap
		};
		textBoxNumeric.SubmitPressed += NumericInputOnSubmitPressed;
		textBoxNumeric.ValueChanged += NumericInputOnValueChanged;
		textBoxNumeric.SelectAll();
		Panel panel2 = new Panel(this, "_buttonPanel")
		{
			BackgroundColor = Color.Transparent,
			Dock = Pos.Bottom
		};
		Button button = new Button(panel2, "_okayButton")
		{
			Alignment = new Alignments[1] { Alignments.Left },
			AutoSizeToContents = true,
			Font = _defaultFont,
			FontSize = 12,
			MinimumSize = new Point(120, 24),
			Padding = new Padding(8, 4),
			Text = Strings.InputBox.Okay,
		};
		Button button2 = button;
		button2.Clicked += OkayButtonOnClicked;
		button = new Button(panel2, "_cancelButton");
		button.Alignment = new Alignments[1] { Alignments.Right };
		button.AutoSizeToContents = true;
		button.Font = _defaultFont;
		button.FontSize = 12;
		button.MinimumSize = new Point(120, 24);
		button.Padding = new Padding(8, 4);
		button.Text = Strings.InputBox.Cancel;
		Button button3 = button;
		button3.Clicked += CancelButtonOnClicked;
		return (panel, panel2);
	}

	private void NumericInputOnValueChanged(TextBoxNumeric sender, ValueChangedEventArgs<double> arguments)
	{
		Value = new NumericalSubmissionValue(arguments.Value);
	}

	private void NumericInputOnSubmitPressed(TextBox sender, EventArgs arguments)
	{
		SubmitInput();
	}

	private (Panel, Panel) CreatePanelsForNumericSliderInput()
	{
		Panel panel = new Panel(this, "_inputPanel")
		{
			BackgroundColor = Color.Transparent,
			Dock = Pos.Fill
		};
		ScrollControl parent = new ScrollControl(panel, "_promptScroller")
		{
			Dock = Pos.Fill
		};
		RichLabel richLabel = new RichLabel(parent, "_promptLabel")
		{
			Dock = Pos.Fill,
			Font = _defaultFont,
			FontSize = 12
		};
		LabeledSlider labeledSlider = new LabeledSlider(panel, "_numericInputSlider")
		{
			AutoSizeToContents = false,
			Dock = Pos.Bottom,
			Font = _defaultFont,
			FontSize = 12,
			Margin = new Margin(0, 8, 0, 0),
			Rounding = 0
		};
		labeledSlider.ValueChanged += NumericSliderInputOnValueChanged;
		Panel panel2 = new Panel(this, "_buttonPanel")
		{
			BackgroundColor = Color.Transparent,
			Dock = Pos.Bottom
		};
		Button button = new Button(panel2, "_okayButton")
		{
			Alignment = new Alignments[1] { Alignments.Left },
			AutoSizeToContents = true,
			Font = _defaultFont,
			FontSize = 12,
			MinimumSize = new Point(120, 24),
			Padding = new Padding(8, 4),
			Text = Strings.InputBox.Okay,
		};
		Button button2 = button;
		button2.Clicked += OkayButtonOnClicked;
		button = new Button(panel2, "_cancelButton");
		button.Alignment = new Alignments[1] { Alignments.Right };
		button.AutoSizeToContents = true;
		button.Font = _defaultFont;
		button.FontSize = 12;
		button.MinimumSize = new Point(120, 24);
		button.Padding = new Padding(8, 4);
		button.Text = Strings.InputBox.Cancel;
		Button button3 = button;
		button3.Clicked += CancelButtonOnClicked;
		return (panel, panel2);
	}

	private void NumericSliderInputOnValueChanged(Base sender, ValueChangedEventArgs<double> arguments)
	{
		Value = new NumericalSubmissionValue(arguments.Value);
	}

	private (Panel, Panel) CreatePanelsForTextInput()
	{
		Panel panel = new Panel(this, "_inputPanel")
		{
			BackgroundColor = Color.Transparent,
			Dock = Pos.Fill
		};
		ScrollControl parent = new ScrollControl(panel, "_promptScroller")
		{
			Dock = Pos.Fill
		};
		RichLabel richLabel = new RichLabel(parent, "_promptLabel")
		{
			Dock = Pos.Fill,
			Font = _defaultFont,
			FontSize = 12
		};
		TextBox textBox = new TextBox(panel, "_stringInput")
		{
			AutoSizeToContents = false,
			Dock = Pos.Bottom,
			Font = _defaultFont,
			FontSize = 12,
			Margin = new Margin(0, 8, 0, 0),
			TextAlign = Pos.Left,
			WrappingBehavior = WrappingBehavior.NoWrap
		};
		textBox.SubmitPressed += StringInputOnSubmitPressed;
		textBox.TextChanged += StringInputOnTextChanged;
		textBox.SelectAll();
		Panel panel2 = new Panel(this, "_buttonPanel")
		{
			BackgroundColor = Color.Transparent,
			Dock = Pos.Bottom
		};
		Button button = new Button(panel2, "_okayButton")
		{
			Alignment = new Alignments[1] { Alignments.Left },
			AutoSizeToContents = true,
			Font = _defaultFont,
			FontSize = 12,
			MinimumSize = new Point(120, 24),
			Padding = new Padding(8, 4),
			Text = Strings.InputBox.Okay,
		};
		Button button2 = button;
		button2.Clicked += OkayButtonOnClicked;
		button = new Button(panel2, "_cancelButton");
		button.Alignment = new Alignments[1] { Alignments.Right };
		button.AutoSizeToContents = true;
		button.Font = _defaultFont;
		button.FontSize = 12;
		button.MinimumSize = new Point(120, 24);
		button.Padding = new Padding(8, 4);
		button.Text = Strings.InputBox.Cancel;
		Button button3 = button;
		button3.Clicked += CancelButtonOnClicked;
		return (panel, panel2);
	}

	private void StringInputOnSubmitPressed(TextBox sender, EventArgs arguments)
	{
		SubmitInput();
	}

	private void StringInputOnTextChanged(TextBox sender, ValueChangedEventArgs<string> arguments)
	{
		Value = new StringSubmissionValue(arguments.Value);
	}

	public InputBox(string title, string prompt, InputType inputType, GwenEventHandler<InputSubmissionEventArgs>? onSubmit, GwenEventHandler<EventArgs>? onCancel = null, object? userData = null, int quantity = 0, int maximumQuantity = int.MaxValue, int minimumQuantity = 0)
		: this("InputBox", title, prompt, inputType, onSubmit, onCancel, userData, quantity, maximumQuantity, minimumQuantity)
	{
	}

	protected InputBox(string name, string title, string prompt, InputType inputType, GwenEventHandler<InputSubmissionEventArgs>? onSubmit, GwenEventHandler<EventArgs>? onCancel = null, object? userData = null, int quantity = 0, int maximumQuantity = int.MaxValue, int minimumQuantity = 0)
		: base(Interface.CurrentInterface.Root, title, modal: true, name)
	{
		_defaultFont = GameContentManager.Current.GetFont(base.TitleLabel.FontName);
		UserData = userData;
		InputType = inputType;
		base.Alignment = new Alignments[1] { Alignments.Center };
		base.IsResizable = false;
		base.InnerPanel.DockChildSpacing = new Padding(8);
		Value = InputType switch
		{
			InputType.Okay => new BooleanSubmissionValue(Value: true), 
			InputType.YesNo => new BooleanSubmissionValue(Value: true), 
			InputType.YesNoCancel => new BooleanSubmissionValue(Value: false), 
			InputType.NumericInput => new NumericalSubmissionValue(0.0), 
			InputType.NumericSliderInput => new NumericalSubmissionValue(0.0), 
			InputType.TextInput => new StringSubmissionValue(null), 
			_ => throw Exceptions.UnreachableInvalidEnum(InputType), 
		};
		Interface.InputBlockingComponents.Add(this);
		(_inputPanel, _buttonPanel) = CreatePanelsForInputType(InputType);
		_buttonPanel.SizeToChildren(resizeX: true, resizeY: true, recursive: true);
		_inputPanel.SizeToChildren(resizeX: true, resizeY: true, recursive: true);
		_cancelButton = _inputPanel.FindChildByName<Button>("_cancelButton");
		_noButton = _inputPanel.FindChildByName<Button>("_noButton");
		_okayButton = _inputPanel.FindChildByName<Button>("_okayButton");
		_yesButton = _inputPanel.FindChildByName<Button>("_yesButton");
		_promptScroller = _inputPanel.FindChildByName<ScrollControl>("_promptScroller") ?? throw new InvalidOperationException("Prompt scroller wasn't created");
		_promptLabel = _promptScroller.FindChildByName<RichLabel>("_promptLabel") ?? throw new InvalidOperationException("Prompt label wasn't created");
		_promptLabel.Text = prompt;
		INumericInput numericInput = null;
		switch (InputType)
		{
		case InputType.NumericInput:
			_numericInputTextbox = _inputPanel.FindChildByName<TextBoxNumeric>("_numericInputTextbox") ?? throw new InvalidOperationException("Numeric input (textbox) wasn't created");
			numericInput = _numericInputTextbox;
			break;
		case InputType.NumericSliderInput:
		{
			_numericInputSlider = _inputPanel.FindChildByName<LabeledSlider>("_numericInputSlider") ?? throw new InvalidOperationException("Numeric input (slider) wasn't created");
			numericInput = _numericInputSlider;
			int notchCount = Math.Min(maximumQuantity, 5);
			_numericInputSlider.NotchCount = notchCount;
			break;
		}
		case InputType.TextInput:
			_stringInput = _inputPanel.FindChildByName<TextBox>("_stringInput") ?? throw new InvalidOperationException("String input wasn't created");
			break;
		}
		if (numericInput != null)
		{
			numericInput.Maximum = maximumQuantity;
			numericInput.Minimum = minimumQuantity;
			numericInput.Value = quantity;
		}
		Submitted += onSubmit;
		Canceled += onCancel;
		base.Closed += onCancel;
	}

	private void ApplySaiyanSkin()
	{
		if (_skinned)
		{
			return;
		}
		_skinned = true;
		GameContentManager current = GameContentManager.Current;
		IFont font = current.GetFont("chakrapetch");
		IFont font2 = current.GetFont("refuel");
		base.MinimumSize = new Point(440, 200);
		base.MaximumSize = new Point(440, 0);
		base.InnerPanelPadding = new Padding(16, 10, 16, 16);
		IGameTexture texture = current.GetTexture(TextureType.Gui, "saiyan_dialogue.png");
		if (texture != null)
		{
			SetImage(texture, "saiyan_dialogue.png", ControlState.Active);
			SetImage(texture, "saiyan_dialogue.png", ControlState.Inactive);
		}
		base.Titlebar.ShouldDrawBackground = false;
		Dragger titlebar = base.Titlebar;
		Point minimumSize = base.Titlebar.MinimumSize;
		minimumSize.Y = 40;
		titlebar.MinimumSize = minimumSize;
		base.TitleLabel.IsHidden = false;
		base.TitleLabel.UsesHeadingFace = false;
		base.TitleLabel.Font = font2;
		base.TitleLabel.FontSize = 15;
		base.TitleLabel.TextColorOverride = DialogWhite;
		base.TitleLabel.TextAlign = Pos.Left | Pos.CenterV;
		WindowTitle.Apply(base.TitleLabel);
		ImagePanel iconContainer = base.IconContainer;
		iconContainer.IsHidden = true;
		iconContainer.IsVisibleInTree = false;
		iconContainer.Margin = default(Margin);
		iconContainer.MinimumSize = default(Point);
		iconContainer.SetSize(0, 0);
		base.TitleLabel.Margin = default(Margin);
		base.Titlebar.Padding = new Padding(Math.Max(0, 16 - base.TitleLabel.Padding.Left), 0, 0, 0);
		Base innerPanel = _promptScroller.InnerPanel;
		Padding padding = _promptScroller.InnerPanel.Padding;
		padding.Left = 0;
		innerPanel.Padding = padding;
		_promptLabel.Font = font;
		_promptLabel.FontSize = 11;
		string text = _promptLabel.Text;
		_promptLabel.Text = text;
		Button[] array = new Button[4] { _okayButton, _yesButton, _noButton, _cancelButton };
		foreach (Button button in array)
		{
			if (button != null)
			{
				StyleDialogButton(button, font);
			}
		}
		Panel buttonPanel = _buttonPanel;
		minimumSize = _buttonPanel.MinimumSize;
		minimumSize.Y = 34;
		buttonPanel.MinimumSize = minimumSize;
		StyleDialogField(_stringInput, font, current);
		StyleDialogField(_numericInputTextbox, font, current);
	}

	private static void StyleDialogButton(Button button, IFont? body)
	{
		button.Font = body;
		button.FontSize = 11;
		button.TextAlign = Pos.Center;
		button.AutoSizeToContents = false;
		button.Padding = Padding.Zero;
		button.MinimumSize = new Point(128, 28);
		button.SetSize(128, 28);
		button.SetStateTexture(ComponentState.Normal, "saiyan_btn_128x28.png");
		button.SetStateTexture(ComponentState.Hovered, "saiyan_btn_128x28_hover.png");
		button.SetStateTexture(ComponentState.Active, "saiyan_btn_128x28_clicked.png");
		button.SetStateTexture(ComponentState.Disabled, "saiyan_btn_128x28_disabled.png");
		button.SetTextColor(DialogEntry, ComponentState.Normal);
		button.SetTextColor(DialogOrangeLight, ComponentState.Hovered);
		button.SetTextColor(DialogNight, ComponentState.Active);
		button.SetTextColor(DialogDim, ComponentState.Disabled);
	}

	private static void StyleDialogField(TextBox? field, IFont? body, GameContentManager content)
	{
		if (field != null)
		{
			field.Font = body;
			field.FontSize = 11;
			field.SetTextColor(DialogWhite, ComponentState.Normal);
			IGameTexture texture = content.GetTexture(TextureType.Gui, "saiyan_field_260x28.png");
			if (texture != null)
			{
				field.SetBackgroundTemplate(texture, "saiyan_field_260x28.png");
			}
		}
	}

	private void YesButtonOnClicked(Base sender, MouseButtonState arguments)
	{
		if (InputType == InputType.YesNoCancel)
		{
			Value = new BooleanSubmissionValue(Value: true);
		}
		SubmitInput();
	}

	private void CancelButtonOnClicked(Base sender, MouseButtonState arguments)
	{
		CancelInput();
	}

	public void CancelInput()
	{
		OnCanceled(this, EventArgs.Empty);
		this.Canceled?.Invoke(this, EventArgs.Empty);
		Close();
	}

	public void SubmitInput()
	{
		InputSubmissionEventArgs e = new InputSubmissionEventArgs(Value);
		OnSubmitted(this, e);
		this.Submitted?.Invoke(this, e);
		Close();
	}

	protected virtual void OnCanceled(Base sender, EventArgs args)
	{
	}

	protected virtual void OnSubmitted(Base sender, InputSubmissionEventArgs args)
	{
	}

	public override void Focus(bool moveMouse = false)
	{
		switch (InputType)
		{
		case InputType.NumericInput:
			_numericInputTextbox?.Focus(moveMouse);
			break;
		case InputType.NumericSliderInput:
			_numericInputSlider?.Focus(moveMouse);
			break;
		case InputType.TextInput:
			_stringInput?.Focus(moveMouse);
			break;
		default:
			throw Exceptions.UnreachableInvalidEnum(InputType);
		case InputType.Okay:
		case InputType.YesNo:
		case InputType.YesNoCancel:
			base.Focus(moveMouse);
			break;
		}
	}

	protected override void Dispose(bool disposing)
	{
		Interface.InputBlockingComponents.Remove(this);
		base.Dispose(disposing);
	}

	protected override void EnsureInitialized()
	{
		base.Name = $"{GetType().GetName()}_{InputType}";
		LoadJsonUi(GameContentManager.UI.Shared, Graphics.Renderer?.GetResolutionString());
		ApplySaiyanSkin();
		Padding padding = _promptScroller.InnerPanel.Padding;
		int num = padding.Bottom + padding.Top;
		_initialScrollerInnerHeight = _promptScroller.InnerHeight - num;
		_initialMinimumHeight = base.MinimumSize.Y;
		bool resizing = false;
		RichLabel promptLabel = _promptLabel;
		promptLabel.Rebuilt = (GwenEventHandler<EventArgs>)Delegate.Combine(promptLabel.Rebuilt, (GwenEventHandler<EventArgs>)delegate
		{
			if (resizing)
			{
				return;
			}
			resizing = true;
			try
			{
				int num2 = Math.Min(400, _promptLabel.Height);
				int num3 = num2 - _initialScrollerInnerHeight;
				InputBox inputBox = this;
				Point minimumSize = base.MinimumSize;
				minimumSize.Y = Math.Max(200, _initialMinimumHeight + num3);
				inputBox.MinimumSize = minimumSize;
				SizeToChildren();
			}
			finally
			{
				resizing = false;
			}
		});
		_promptLabel.ForceImmediateRebuild();
		Show();
		Focus();
	}
}
