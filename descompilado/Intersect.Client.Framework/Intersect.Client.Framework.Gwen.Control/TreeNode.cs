using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control;

public class TreeNode : Base
{
	public const int TREE_INDENTATION = 14;

	private readonly Dictionary<GwenEventHandler<MouseButtonState>, GwenEventHandler<MouseButtonState>> _wrappedMouseButtonStateDelegates = new Dictionary<GwenEventHandler<MouseButtonState>, GwenEventHandler<MouseButtonState>>();

	private IFont? _font;

	private Color? _textColor;

	private Color? _textColorOverride;

	private bool mSelected;

	protected TreeControl _treeControl;

	private int _fontSize = 10;

	protected Button? _toggleButton { get; set; }

	protected Button? _trigger { get; set; }

	public virtual int FontSize
	{
		get
		{
			return _trigger?.FontSize ?? _fontSize;
		}
		set
		{
			if (_fontSize != value)
			{
				_fontSize = value;
				Button trigger = _trigger;
				if (trigger != null)
				{
					trigger.FontSize = value;
				}
				TreeNode[] array = base.Children.OfType<TreeNode>().ToArray();
				TreeNode[] array2 = array;
				foreach (TreeNode treeNode in array2)
				{
					treeNode.FontSize = value;
				}
			}
		}
	}

	public virtual IFont? Font
	{
		get
		{
			return _trigger?.Font ?? _font;
		}
		set
		{
			_font = value;
			Button trigger = _trigger;
			if (trigger != null)
			{
				trigger.Font = value;
			}
			TreeNode[] array = base.Children.OfType<TreeNode>().ToArray();
			TreeNode[] array2 = array;
			foreach (TreeNode treeNode in array2)
			{
				treeNode.Font = value;
			}
		}
	}

	public bool IsRoot { get; set; }

	public TreeControl TreeControl
	{
		get
		{
			return _treeControl;
		}
		set
		{
			_treeControl = value;
		}
	}

	public bool IsSelectable { get; set; }

	public bool IsSelected
	{
		get
		{
			return mSelected;
		}
		set
		{
			if (IsSelectable && IsSelected != value)
			{
				mSelected = value;
				if (_trigger != null)
				{
					_trigger.ToggleState = value;
				}
				this.SelectionChanged?.Invoke(this, EventArgs.Empty);
				((TreeNode)_treeControl).SelectionChanged?.Invoke(this, EventArgs.Empty);
				if (value)
				{
					this.Selected?.Invoke(this, EventArgs.Empty);
					((TreeNode)_treeControl).Selected?.Invoke(this, EventArgs.Empty);
				}
				else
				{
					this.Unselected?.Invoke(this, EventArgs.Empty);
					((TreeNode)_treeControl).Unselected?.Invoke(this, EventArgs.Empty);
				}
			}
		}
	}

	public string? Text
	{
		get
		{
			return _trigger?.Text;
		}
		set
		{
			Button trigger = _trigger;
			if (trigger != null)
			{
				trigger.Text = value;
			}
		}
	}

	public Color? TextColor
	{
		get
		{
			return _trigger?.TextColor ?? _textColor;
		}
		set
		{
			_textColor = value;
			Button trigger = _trigger;
			if (trigger != null)
			{
				trigger.TextColor = value;
			}
		}
	}

	public Color? TextColorOverride
	{
		get
		{
			return _trigger?.TextColorOverride ?? _textColorOverride;
		}
		set
		{
			_textColorOverride = value;
			Button trigger = _trigger;
			if (trigger != null)
			{
				trigger.TextColorOverride = value;
			}
		}
	}

	public IEnumerable<TreeNode> SelectedChildren
	{
		get
		{
			List<TreeNode> list = new List<TreeNode>();
			foreach (Base child in base.Children)
			{
				if (child is TreeNode treeNode)
				{
					list.AddRange(treeNode.SelectedChildren);
				}
			}
			if (IsSelected)
			{
				list.Add(this);
			}
			return list;
		}
	}

	public event GwenEventHandler<EventArgs>? LabelPressed;

	public event GwenEventHandler<EventArgs>? SelectionChanged;

	public event GwenEventHandler<EventArgs>? Selected;

	public event GwenEventHandler<EventArgs>? Unselected;

	public event GwenEventHandler<EventArgs>? Expanded;

	public event GwenEventHandler<EventArgs>? Collapsed;

	public override event GwenEventHandler<MouseButtonState>? Clicked
	{
		add
		{
			Button trigger = _trigger;
			if (trigger == null)
			{
				base.Clicked += value;
				return;
			}
			if (value == null)
			{
				trigger.Clicked += value;
				return;
			}
			GwenEventHandler<MouseButtonState> value2 = delegate(Base _, MouseButtonState args)
			{
				value(this, args);
			};
			_wrappedMouseButtonStateDelegates[value] = value2;
			trigger.Clicked += value2;
		}
		remove
		{
			Button trigger = _trigger;
			GwenEventHandler<MouseButtonState> value2;
			if (trigger == null)
			{
				base.Clicked -= value;
			}
			else if (value == null)
			{
				trigger.Clicked -= value;
			}
			else if (_wrappedMouseButtonStateDelegates.Remove(value, out value2))
			{
				trigger.Clicked -= value2;
			}
		}
	}

	public override event GwenEventHandler<MouseButtonState>? DoubleClicked
	{
		add
		{
			Button trigger = _trigger;
			if (trigger == null)
			{
				base.DoubleClicked += value;
				return;
			}
			if (value == null)
			{
				trigger.DoubleClicked += value;
				return;
			}
			GwenEventHandler<MouseButtonState> value2 = delegate(Base _, MouseButtonState args)
			{
				value(this, args);
			};
			_wrappedMouseButtonStateDelegates[value] = value2;
			trigger.DoubleClicked += value2;
		}
		remove
		{
			Button trigger = _trigger;
			GwenEventHandler<MouseButtonState> value2;
			if (trigger == null)
			{
				base.DoubleClicked -= value;
			}
			else if (value == null)
			{
				trigger.DoubleClicked -= value;
			}
			else if (_wrappedMouseButtonStateDelegates.Remove(value, out value2))
			{
				trigger.DoubleClicked -= value2;
			}
		}
	}

	public TreeNode(Base parent, string? name = null)
		: base(parent, name)
	{
		_toggleButton = new TreeToggleButton(this)
		{
			Size = new Point(15, 15)
		};
		_toggleButton.Toggled += OnToggleButtonPress;
		_trigger = new TreeNodeLabel(this)
		{
			Dock = Pos.Top,
			FontSize = _fontSize,
			Margin = new Margin(16, 0, 0, 0)
		};
		_trigger.DoubleClicked += OnDoubleClickName;
		_trigger.Clicked += OnClickName;
		_innerPanel = new Base(this, "_innerPanel")
		{
			Dock = Pos.Top,
			Height = 100,
			IsVisibleInTree = false,
			Margin = new Margin(14, 1, 0, 0)
		};
		IsRoot = parent is TreeControl;
		mSelected = false;
		IsSelectable = true;
		base.Dock = Pos.Top;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		bool flag = _innerPanel?.IsVisibleInTree ?? false;
		int num = CalculateTreeNodeHeight(flag);
		skin.DrawTreeNode(this, flag, IsSelected, num, _trigger?.Height ?? num, _trigger?.TextRight ?? 0, (int)((float)_toggleButton.Y + (float)_toggleButton.Height * 0.5f), num, _treeControl == base.Parent);
		Invalidate();
	}

	private int CalculateTreeNodeHeight(bool isOpen)
	{
		Button trigger = _trigger;
		if (trigger == null)
		{
			return 0;
		}
		int height = trigger.Height;
		Base innerPanel = _innerPanel;
		if (innerPanel == null)
		{
			return height;
		}
		if (isOpen)
		{
			TreeNode treeNode = innerPanel.Children.OfType<TreeNode>().LastOrDefault((TreeNode child) => child.IsVisibleInTree);
			if (treeNode != null)
			{
				return height + treeNode.Y;
			}
		}
		if (height != 0)
		{
			return height;
		}
		return innerPanel.Height;
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (_toggleButton != null)
		{
			if (_trigger != null)
			{
				_toggleButton.SetPosition(0f, (float)(_trigger.Height - _toggleButton.Height) * 0.5f);
			}
			Base innerPanel = _innerPanel;
			if (innerPanel != null)
			{
				IReadOnlyList<Base> children = innerPanel.Children;
				if (children != null && children.Count > 0)
				{
					_toggleButton.Show();
					innerPanel.SizeToChildren(resizeX: false);
					goto IL_009d;
				}
			}
			_toggleButton.Hide();
			_toggleButton.ToggleState = false;
			_innerPanel?.Hide();
		}
		goto IL_009d;
		IL_009d:
		base.Layout(skin);
	}

	protected override void DoPostlayout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (SizeToChildren(resizeX: false))
		{
			InvalidateParentDock();
		}
	}

	public TreeNode AddNode(string label, object? userData = null)
	{
		return new TreeNode(this)
		{
			Font = Font,
			FontSize = FontSize,
			Text = label,
			TextColor = _textColor,
			TextColorOverride = _textColorOverride,
			UserData = userData
		};
	}

	public void Open()
	{
		_innerPanel?.Show();
		if (_toggleButton != null)
		{
			_toggleButton.ToggleState = true;
		}
		this.Expanded?.Invoke(this, EventArgs.Empty);
		((TreeNode)_treeControl).Expanded?.Invoke(this, EventArgs.Empty);
		InvalidateParentDock();
		Invalidate();
	}

	public void Close()
	{
		_innerPanel?.Hide();
		if (_toggleButton != null)
		{
			_toggleButton.ToggleState = false;
		}
		this.Collapsed?.Invoke(this, EventArgs.Empty);
		((TreeNode)_treeControl).Collapsed?.Invoke(this, EventArgs.Empty);
		Invalidate();
	}

	public void ExpandAll()
	{
		Open();
		RunOnMainThread(ExpandAllChildren, this);
	}

	private static void ExpandAllChildren(TreeNode @this)
	{
		foreach (Base child in @this.Children)
		{
			if (child is TreeNode treeNode)
			{
				treeNode.ExpandAll();
			}
		}
	}

	public void UnselectAll()
	{
		IsSelected = false;
		if (_trigger != null)
		{
			_trigger.ToggleState = false;
		}
		RunOnMainThread(UnselectChildren, this);
	}

	private static void UnselectChildren(TreeNode @this)
	{
		foreach (Base child in @this.Children)
		{
			if (child is TreeNode treeNode)
			{
				treeNode.UnselectAll();
			}
		}
	}

	protected virtual void OnToggleButtonPress(Base control, EventArgs args)
	{
		if (_toggleButton.ToggleState)
		{
			Open();
		}
		else
		{
			Close();
		}
	}

	protected virtual void OnDoubleClickName(Base control, EventArgs args)
	{
		if (_toggleButton.IsVisibleInTree)
		{
			_toggleButton.Toggle();
		}
	}

	protected virtual void OnClickName(Base control, EventArgs args)
	{
		if (this.LabelPressed != null)
		{
			this.LabelPressed(this, EventArgs.Empty);
		}
		IsSelected = !IsSelected;
	}

	public void SetImage(IGameTexture texture, string fileName = "")
	{
		_trigger.SetStateTexture(texture, fileName, ComponentState.Normal);
	}

	protected override void OnChildAdded(Base child)
	{
		if (child is TreeNode treeNode)
		{
			treeNode.TreeControl = _treeControl;
			_treeControl?.OnNodeAdded(treeNode);
		}
		base.OnChildAdded(child);
	}
}
