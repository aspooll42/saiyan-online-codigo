using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control;

public class LabeledComboBox : Base, IAutoSizeToContents, ITextContainer
{
	private readonly ComboBox _comboBox;

	private readonly Label _label;

	private bool _autoSizeToContents;

	public IFont? Font
	{
		get
		{
			return _label.Font;
		}
		set
		{
			_label.Font = value;
			_comboBox.Font = value;
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
			_comboBox.FontSize = value;
		}
	}

	public IFont? LabelFont
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

	public IFont? ItemFont
	{
		get
		{
			return _comboBox.Font;
		}
		set
		{
			_comboBox.Font = value;
		}
	}

	public string? Label
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

	public MenuItem? SelectedItem
	{
		get
		{
			return _comboBox.SelectedItem;
		}
		set
		{
			_comboBox.SelectedItem = value;
		}
	}

	public bool AutoSizeToContents
	{
		get
		{
			return _autoSizeToContents;
		}
		set
		{
			SetAndDoIfChanged(ref _autoSizeToContents, value, Base.Invalidate);
		}
	}

	public Padding LabelPadding
	{
		get
		{
			return _label.Padding;
		}
		set
		{
			_label.Padding = value;
		}
	}

	public Padding TextPadding
	{
		get
		{
			return _comboBox.Padding;
		}
		set
		{
			_comboBox.Padding = value;
		}
	}

	string? ITextContainer.Text
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

	public event GwenEventHandler<ItemSelectedEventArgs>? ItemSelected;

	public LabeledComboBox(Base parent, string? name = null)
		: base(parent, name)
	{
		_autoSizeToContents = true;
		_label = new Label(this, "_label")
		{
			Alignment = new Alignments[1] { Alignments.CenterV },
			Dock = (Pos.Left | Pos.CenterV)
		};
		_comboBox = new ComboBox(this, "_comboBox")
		{
			Alignment = new Alignments[1] { Alignments.CenterV },
			Dock = (Pos.Left | Pos.CenterV),
			Margin = new Margin(8, 0, 0, 0),
			Padding = new Padding(8, 4, 0, 4)
		};
		_comboBox.ItemSelected += delegate(Base _, ItemSelectedEventArgs args)
		{
			this.ItemSelected?.Invoke(this, args);
		};
	}

	public MenuItem AddItem(string label, string? name = null, object? userData = null)
	{
		return _comboBox.AddItem(label, name, userData);
	}

	public bool SelectByName(string name)
	{
		return _comboBox.SelectByName(name);
	}

	public bool SelectByText(string text)
	{
		return _comboBox.SelectByText(text);
	}

	public bool SelectByUserData(object? userData)
	{
		return _comboBox.SelectByUserData(userData);
	}

	protected override void OnDockChanged(Pos oldDock, Pos newDock)
	{
		base.OnDockChanged(oldDock, newDock);
		if (newDock.HasFlag(Pos.Fill) || (!_autoSizeToContents && (newDock.HasFlag(Pos.Bottom) || newDock.HasFlag(Pos.Top))))
		{
			_comboBox.Dock = Pos.CenterV | Pos.Fill;
			_comboBox.AutoSizeToContents = false;
		}
		else if (_comboBox.Dock.HasFlag(Pos.Fill))
		{
			_comboBox.Dock = Pos.Left | Pos.CenterV;
			_comboBox.AutoSizeToContents = true;
		}
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (_autoSizeToContents)
		{
			SizeToChildren();
		}
		base.Layout(skin);
	}

	protected override void OnChildBoundsChanged(Base child, Rectangle oldChildBounds, Rectangle newChildBounds)
	{
		base.OnChildBoundsChanged(child, oldChildBounds, newChildBounds);
		Invalidate();
	}

	public void ClearItems()
	{
		_comboBox.ClearItems();
	}
}
