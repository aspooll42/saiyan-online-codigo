using System;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Framework.Eventing;

namespace Intersect.Client.Framework.Gwen.Control;

public class LabeledCheckBox : Base, IAutoSizeToContents, ICheckbox, ITextContainer
{
	private readonly Checkbox _checkbox;

	private readonly Label _label;

	private bool _autoSizeToContents;

	public bool IsChecked
	{
		get
		{
			return _checkbox.IsChecked;
		}
		set
		{
			_checkbox.IsChecked = value;
		}
	}

	public IFont? Font
	{
		get
		{
			return _label.Font;
		}
		set
		{
			_label.Font = value;
		}
	}

	public string? FontName
	{
		get
		{
			return _label.FontName;
		}
		set
		{
			_label.FontName = value;
		}
	}

	public int FontSize
	{
		get
		{
			return _label.FontSize;
		}
		set
		{
			_label.FontSize = value;
		}
	}

	public Color? TextColor
	{
		get
		{
			return _label.TextColor;
		}
		set
		{
			_label.TextColor = value;
		}
	}

	public Color? TextColorOverride
	{
		get
		{
			return _label.TextColor;
		}
		set
		{
			_label.TextColorOverride = value;
		}
	}

	public override string? TooltipText
	{
		get
		{
			return _label.TooltipText;
		}
		set
		{
			_label.TooltipText = value;
		}
	}

	public override string? TooltipBackgroundName
	{
		get
		{
			return _label.TooltipBackgroundName;
		}
		set
		{
			_label.TooltipBackgroundName = value;
		}
	}

	public override string? TooltipFontName
	{
		get
		{
			return _label.TooltipFontName;
		}
		set
		{
			_label.TooltipFontName = value;
		}
	}

	public override int TooltipFontSize
	{
		get
		{
			return _label.TooltipFontSize;
		}
		set
		{
			_label.TooltipFontSize = value;
		}
	}

	public override Color? TooltipTextColor
	{
		get
		{
			return _label.TooltipTextColor;
		}
		set
		{
			_label.TooltipTextColor = value;
		}
	}

	public string? Text
	{
		get
		{
			return _label.Text;
		}
		set
		{
			_label.Text = value;
		}
	}

	public Color? TextPaddingDebugColor { get; set; }

	public bool AutoSizeToContents
	{
		get
		{
			return _autoSizeToContents;
		}
		set
		{
			SetAndDoIfChanged(ref _autoSizeToContents, value, InvalidateAutoSizeToContents);
		}
	}

	public event EventHandler<ICheckbox, EventArgs>? Checked;

	public event EventHandler<ICheckbox, EventArgs>? Unchecked;

	public event EventHandler<ICheckbox, ValueChangedEventArgs<bool>>? CheckChanged;

	public LabeledCheckBox(Base parent, string? name = null)
		: base(parent, name)
	{
		base.Size = new Point(208, 26);
		_checkbox = new Checkbox(this, "_checkbox")
		{
			InheritParentEnablementProperties = true,
			Dock = (Pos.Left | Pos.CenterV),
			Margin = new Margin(0, 2, 2, 2),
			IsTabable = false
		};
		_checkbox.CheckChanged += OnCheckChanged;
		_label = new Label(this, "_label")
		{
			Alignment = new Alignments[1] { Alignments.CenterV },
			AutoSizeToContents = true,
			Dock = (Pos.CenterV | Pos.Fill),
			InheritParentEnablementProperties = true,
			IsTabable = false,
			Padding = new Padding(2),
			TextAlign = (Pos.Left | Pos.CenterV)
		};
		_label.Clicked += delegate(Base control, MouseButtonState _)
		{
			_label.ProcessAlignments();
			_checkbox.Press(control);
		};
		base.IsTabable = false;
	}

	protected virtual void OnCheckChanged(ICheckbox sender, ValueChangedEventArgs<bool> args)
	{
		if (_checkbox.IsChecked)
		{
			this.Checked?.Invoke(this, EventArgs.Empty);
		}
		else
		{
			this.Unchecked?.Invoke(this, EventArgs.Empty);
		}
		this.CheckChanged?.Invoke(this, args);
	}

	public override Point GetChildrenSize()
	{
		return base.GetChildrenSize();
	}

	public void SetCheckSize(int w, int h)
	{
		_checkbox.SetSize(w, h);
	}

	public void SetImage(IGameTexture texture, string fileName, Checkbox.ControlState state)
	{
		_checkbox.SetImage(texture, fileName, state);
	}

	public void SetTextColor(Color clr, ComponentState state)
	{
		_label.SetTextColor(clr, state);
	}

	public void SetLabelDistance(int dist)
	{
		_checkbox.Margin = new Margin(0, 2, dist, 2);
	}

	public void SetFont(IFont font)
	{
		_label.Font = font;
	}

	protected override bool OnKeySpace(bool down)
	{
		base.OnKeySpace(down);
		if (!down)
		{
			_checkbox.IsChecked = !_checkbox.IsChecked;
		}
		return true;
	}

	private void InvalidateAutoSizeToContents(bool oldValue, bool newValue)
	{
		_label.AutoSizeToContents = newValue;
		Invalidate();
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (_autoSizeToContents)
		{
			SizeToChildren();
		}
		base.Layout(skin);
	}
}
