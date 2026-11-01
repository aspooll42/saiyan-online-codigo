using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Intersect.Client.Framework.Audio;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Anim;
using Intersect.Client.Framework.Gwen.Anim.Size;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Control.Layout;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.DragDrop;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Platform;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Client.Interface.Data;
using Intersect.Core;
using Intersect.Framework;
using Intersect.Framework.Collections;
using Intersect.Framework.Reflection;
using Intersect.Framework.Serialization;
using Intersect.Framework.Threading;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Framework.Gwen.Control;

public class Base : IDisposable
{
	public delegate void GwenEventHandler<in TArgs>(Base sender, TArgs arguments) where TArgs : EventArgs;

	public delegate void GwenEventHandler<in TSender, in TArgs>(TSender sender, TArgs arguments) where TSender : Base where TArgs : EventArgs;

	private record struct ComponentAtParams(Point Position, NodeFilter Filters);

	public record struct SizeToChildrenArgs(bool X = true, bool Y = true, bool Recurse = false, Point MinimumSize = default(Point));

	private struct DataProviderRefCount
	{
		public int Listening;

		public int Visible;
	}

	private const string PropertyNameInnerPanel = "InnerPanel";

	private bool _inheritParentEnablementProperties;

	private bool _disposed;

	private bool _disposeCompleted;

	private StackTrace? _disposeStack;

	private Canvas? _canvas;

	protected Modal? _modal;

	private Base? _previousParent;

	private Base? _parent;

	protected Base? _innerPanel;

	private readonly List<Base> _children = new List<Base>();

	private bool _disabled;

	private bool _skipRender;

	private bool _visible;

	private Package? _dragPayload;

	private object? _userData;

	private bool _drawDebugOutlines;

	private readonly Dictionary<string, GwenEventHandler<EventArgs>> _accelerators = new Dictionary<string, GwenEventHandler<EventArgs>>();

	private bool _keyboardInputEnabled;

	private bool _mouseInputEnabled;

	private bool _tabEnabled;

	private Rectangle _bounds;

	private Rectangle _boundsOnDisk;

	private Rectangle _innerBounds;

	private Rectangle _outerBounds;

	private Rectangle _renderBounds;

	private Point _maximumSize;

	private Point _minimumSize;

	private bool _restrictToParent;

	private Margin _margin;

	private Padding _padding;

	private List<Alignments> _alignments = new List<Alignments>();

	private Padding _alignmentPadding;

	private Point _alignmentTranslation;

	private Pos _dock;

	private Padding _dockChildSpacing;

	private string? _name;

	private string? _cachedToString;

	private bool _cacheTextureDirty;

	private bool _cacheToTexture;

	private bool _needsAlignment;

	private bool _layoutDirty;

	private bool _dockDirty;

	private bool _drawBackground;

	private Color _color;

	private Cursor _cursor;

	private Intersect.Client.Framework.Gwen.Skin.Base? _skin;

	private Base? _tooltip;

	private bool _tooltipEnabled;

	private string? _tooltipBackgroundName;

	private IFont? _tooltipFont;

	private string? _tooltipFontInfo;

	private Color? _tooltipTextColor;

	private int _tooltipFontSize = 10;

	private List<Base>? _enfantsAuDessin;

	protected bool _chronometrerLesEnfants;

	private readonly HashSet<MouseButton> _mouseButtonPressed = new HashSet<MouseButton>();

	private Point _requiredSizeForDockFillNodes;

	protected Point _dockFillSize;

	private int _tabOrder;

	private DisplayMode _displayMode;

	private readonly HashSet<IDataProvider> _dataProviders = new HashSet<IDataProvider>();

	private readonly Dictionary<IUpdatableDataProvider, DataProviderRefCount> _updatableDataProviderRefCounts = new Dictionary<IUpdatableDataProvider, DataProviderRefCount>();

	private readonly ThreadQueue _threadQueue;

	private readonly ManualActionQueueParent _preLayoutActionsParent = new ManualActionQueueParent();

	public readonly ManualActionQueue PreLayout;

	private readonly ManualActionQueueParent _postLayoutActionsParent = new ManualActionQueueParent();

	public readonly ManualActionQueue PostLayout;

	private int _pendingThreadQueues;

	internal bool InheritParentEnablementProperties
	{
		get
		{
			return _inheritParentEnablementProperties;
		}
		set
		{
			_inheritParentEnablementProperties = value;
			IsDisabled = Parent?.IsDisabled ?? IsDisabled;
			IsHidden = Parent?.IsHidden ?? IsHidden;
		}
	}

	private Base? _host { get; set; }

	private IGameTexture? _tooltipBackground { get; set; }

	public DisplayMode DisplayMode
	{
		get
		{
			return _displayMode;
		}
		set
		{
			SetAndDoIfChanged(ref _displayMode, value, InvalidateDock);
		}
	}

	public virtual string? TooltipFontName
	{
		get
		{
			return _tooltipFont?.Name;
		}
		set
		{
			if (!(value == TooltipFontName))
			{
				TooltipFont = GameContentManager.Current.GetFont(value);
			}
		}
	}

	public virtual int TooltipFontSize
	{
		get
		{
			return _tooltipFontSize;
		}
		set
		{
			if (value != _tooltipFontSize)
			{
				_tooltipFontSize = value;
				if (_tooltip is Label label)
				{
					label.FontSize = value;
				}
			}
		}
	}

	public virtual IGameTexture? TooltipBackground
	{
		get
		{
			return _tooltipBackground;
		}
		set
		{
			if (value != _tooltipBackground)
			{
				return;
			}
			if (value?.Name == _tooltipBackgroundName)
			{
				_tooltipBackground = value;
				return;
			}
			_tooltipBackground = value;
			_tooltipBackgroundName = value?.Name;
			if (Tooltip is Label label)
			{
				label.ToolTipBackground = _tooltipBackground;
			}
		}
	}

	public virtual string? TooltipBackgroundName
	{
		get
		{
			return _tooltipBackgroundName;
		}
		set
		{
			if (!(value == _tooltipBackgroundName))
			{
				_tooltipBackgroundName = value;
				IGameTexture tooltipBackground = null;
				if (!string.IsNullOrWhiteSpace(_tooltipBackgroundName))
				{
					tooltipBackground = GameContentManager.Current.GetTexture(TextureType.Gui, _tooltipBackgroundName);
				}
				_tooltipBackground = tooltipBackground;
				if (Tooltip is Label label)
				{
					label.ToolTipBackground = _tooltipBackground;
				}
			}
		}
	}

	public virtual Color? TooltipTextColor
	{
		get
		{
			return _tooltipTextColor;
		}
		set
		{
			if (!(value == _tooltipTextColor))
			{
				_tooltipTextColor = value;
				if (Tooltip is Label label)
				{
					label.TextColorOverride = _tooltipTextColor;
				}
			}
		}
	}

	public IFont? TooltipFont
	{
		get
		{
			return _tooltipFont;
		}
		set
		{
			_tooltipFont = value;
			_tooltipFontInfo = ((value == null) ? null : $"{value.Name},{_tooltipFontSize}");
			if (_tooltip is Label label)
			{
				label.Font = value;
			}
		}
	}

	public List<Alignments> CurAlignments => _alignments;

	internal bool ClickEventAssigned
	{
		get
		{
			if (this.Clicked == null)
			{
				return this.DoubleClicked != null;
			}
			return true;
		}
	}

	public IReadOnlyList<Base> Children => _innerPanel?.Children ?? _children;

	public Base? Parent
	{
		get
		{
			return _host;
		}
		set
		{
			if (_host != value)
			{
				Base host = _host;
				host?.RunOnMainThread(ProcessDetachingFromParent, new NodePair(this, host));
				if (value == null)
				{
					ProcessAttachingToParent(this, null);
				}
				else
				{
					value.RunOnMainThread(ProcessAttachingToParent, new NodePair(this, value));
				}
			}
		}
	}

	public Base Root
	{
		get
		{
			Base obj = this;
			while (obj.Parent != null)
			{
				obj = obj.Parent;
			}
			return obj;
		}
	}

	public Alignments[] Alignment
	{
		get
		{
			return _alignments.ToArray();
		}
		set
		{
			_alignments = value.ToList();
			ProcessAlignments();
		}
	}

	public Point AlignmentTranslation
	{
		get
		{
			return _alignmentTranslation;
		}
		set
		{
			SetAndDoIfChanged(ref _alignmentTranslation, value, InvalidateAlignment);
		}
	}

	public virtual bool IsBlockingInput => true;

	public Pos Dock
	{
		get
		{
			return _dock;
		}
		set
		{
			Pos dock = _dock;
			if (dock != value)
			{
				_dock = value;
				OnDockChanged(dock, value);
				InvalidateDock();
				InvalidateParentDock();
			}
		}
	}

	public Padding DockChildSpacing
	{
		get
		{
			return _dockChildSpacing;
		}
		set
		{
			_dockChildSpacing = value;
			Base innerPanel = _innerPanel;
			if (innerPanel != null)
			{
				innerPanel.DockChildSpacing = value;
			}
		}
	}

	protected bool HasSkin
	{
		get
		{
			if (_skin == null)
			{
				return _host?.HasSkin ?? false;
			}
			return true;
		}
	}

	protected Intersect.Client.Framework.Gwen.Skin.Base? SafeSkin
	{
		get
		{
			Intersect.Client.Framework.Gwen.Skin.Base? obj = _skin;
			if (obj == null)
			{
				Base? parent = _parent;
				if (parent == null)
				{
					return null;
				}
				obj = parent.SafeSkin;
			}
			return obj;
		}
	}

	public Intersect.Client.Framework.Gwen.Skin.Base Skin
	{
		get
		{
			Intersect.Client.Framework.Gwen.Skin.Base safeSkin = SafeSkin;
			if (safeSkin != null)
			{
				return safeSkin;
			}
			throw new InvalidOperationException("GetSkin: null");
		}
	}

	private Intersect.Client.Framework.Gwen.Skin.Base? DisposeSkin
	{
		get
		{
			Intersect.Client.Framework.Gwen.Skin.Base? obj = _skin;
			if (obj == null)
			{
				Base? host = _host;
				if (host == null)
				{
					return null;
				}
				obj = host.DisposeSkin;
			}
			return obj;
		}
	}

	public Base? Tooltip
	{
		get
		{
			return _tooltip;
		}
		set
		{
			if (value != _tooltip)
			{
				_tooltip = value;
				if (_tooltip != null)
				{
					_tooltip.Parent = this;
					_tooltip.IsHidden = true;
				}
			}
		}
	}

	public virtual string? TooltipText
	{
		get
		{
			return (_tooltip as Label)?.Text;
		}
		set
		{
			if (value == TooltipText)
			{
				return;
			}
			if (!(_tooltip is Label label))
			{
				if (_tooltip != null)
				{
					ApplicationContext.CurrentContext.Logger.LogWarning("Unable to set tooltip text of {ControlName} to '{TooltipText}' because it is set to an incompatible control type {ControlType}", ParentQualifiedName, value, _tooltip.GetType().GetName(qualified: true));
				}
				else
				{
					SetToolTipText(value);
				}
			}
			else
			{
				label.Text = value ?? string.Empty;
			}
		}
	}

	internal virtual bool IsMenuComponent
	{
		get
		{
			if (_host == null)
			{
				return false;
			}
			return _host.IsMenuComponent;
		}
	}

	protected virtual bool ShouldClip => true;

	public Padding Padding
	{
		get
		{
			return _padding;
		}
		set
		{
			if (!(_padding == value))
			{
				_padding = value;
				Invalidate();
				InvalidateParent();
			}
		}
	}

	public Padding AlignmentPadding
	{
		get
		{
			return _alignmentPadding;
		}
		set
		{
			SetAndDoIfChanged(ref _alignmentPadding, value, InvalidateAlignment);
		}
	}

	public Color RenderColor
	{
		get
		{
			return _color;
		}
		set
		{
			if (!(_color == value))
			{
				_color = value;
				Invalidate();
				InvalidateParent();
			}
		}
	}

	public Margin Margin
	{
		get
		{
			return _margin;
		}
		set
		{
			if (!(_margin == value))
			{
				_margin = value;
				Invalidate();
				InvalidateParent();
			}
		}
	}

	public bool IsOnTop
	{
		get
		{
			try
			{
				return Parent?.IndexOf(this) == (Parent?.Children.Count ?? (-1));
			}
			catch (StackOverflowException)
			{
				throw;
			}
		}
	}

	public object? UserData
	{
		get
		{
			return _userData;
		}
		set
		{
			_userData = value;
		}
	}

	public virtual bool IsHovered => InputHandler.HoveredControl == this;

	public bool HasFocus => InputHandler.KeyboardFocus == this;

	public bool IsDisabled
	{
		get
		{
			if (!_inheritParentEnablementProperties || Parent == null)
			{
				return _disabled;
			}
			return Parent.IsDisabled;
		}
		set
		{
			SetAndDoIfChanged(ref _disabled, value, OnDisabledChanged);
		}
	}

	public bool IsHidden
	{
		get
		{
			if (_inheritParentEnablementProperties)
			{
				Base parent = Parent;
				if (parent != null)
				{
					return parent.IsHidden;
				}
			}
			return !_visible;
		}
		set
		{
			(Canvas ?? this).RunOnMainThread(SetVisible, this, !value);
		}
	}

	public virtual bool IsHiddenByTree
	{
		get
		{
			if (_visible)
			{
				return Parent?.IsHiddenByTree ?? false;
			}
			return true;
		}
	}

	public virtual bool IsDisabledByTree
	{
		get
		{
			if (!_disabled)
			{
				return Parent?.IsDisabledByTree ?? false;
			}
			return true;
		}
	}

	public bool RestrictToParent
	{
		get
		{
			return _restrictToParent;
		}
		set
		{
			_restrictToParent = value;
		}
	}

	public bool MouseInputEnabled
	{
		get
		{
			return _mouseInputEnabled;
		}
		set
		{
			if (value != _mouseInputEnabled)
			{
				_mouseInputEnabled = value;
			}
		}
	}

	public bool KeyboardInputEnabled
	{
		get
		{
			return _keyboardInputEnabled;
		}
		set
		{
			_keyboardInputEnabled = value;
		}
	}

	public Cursor Cursor
	{
		get
		{
			return _cursor;
		}
		set
		{
			_cursor = value;
		}
	}

	public int GlobalX => X + (Parent?.GlobalX ?? 0);

	public int GlobalY => Y + (Parent?.GlobalY ?? 0);

	public Point PositionGlobal => new Point(X, Y) + (_parent?.PositionGlobal ?? Point.Empty);

	public Rectangle GlobalBounds => new Rectangle(PositionGlobal, Size);

	public bool IsTabable
	{
		get
		{
			return _tabEnabled;
		}
		set
		{
			_tabEnabled = value;
		}
	}

	public int TabOrder
	{
		get
		{
			return _tabOrder;
		}
		set
		{
			SetAndDoIfChanged(ref _tabOrder, value, OnTabOrderChanged);
		}
	}

	public bool ShouldDrawBackground
	{
		get
		{
			return _drawBackground;
		}
		set
		{
			_drawBackground = value;
		}
	}

	public bool ShouldCacheToTexture
	{
		get
		{
			return _cacheToTexture;
		}
		set
		{
			SetAndDoIfChanged(ref _cacheToTexture, value, Invalidate);
		}
	}

	public string ParentQualifiedName
	{
		get
		{
			Base host = _host;
			if (host == null)
			{
				return Name;
			}
			return host.Name + "." + Name;
		}
	}

	public string CanonicalName
	{
		get
		{
			Base obj = _parent ?? _host;
			string text;
			if (obj == null)
			{
				text = _name;
				if (text == null)
				{
					return "(unnamed " + GetType().GetName(qualified: true) + ")";
				}
			}
			else
			{
				text = obj.CanonicalName + "." + Name;
			}
			return text;
		}
	}

	public string QualifiedName => GetType().GetName(qualified: true) + " (" + Name + ")";

	public string Name
	{
		get
		{
			return _name ?? ("(unnamed " + GetType().Name + ")");
		}
		set
		{
			if (!(_name == value))
			{
				_name = value;
				_cachedToString = null;
			}
		}
	}

	public Rectangle Bounds => _bounds;

	public Rectangle OuterBounds => _outerBounds;

	public bool ClipContents { get; set; } = true;

	public Rectangle RenderBounds => _renderBounds;

	public Rectangle InnerBounds => _innerBounds;

	public Point MinimumSize
	{
		get
		{
			return _minimumSize;
		}
		set
		{
			Point minimumSize = _minimumSize;
			if (!(minimumSize == value))
			{
				_minimumSize = value;
				if (_innerPanel != null)
				{
					_innerPanel.MinimumSize = InnerPanelSizeFrom(value);
				}
				OnMinimumSizeChanged(minimumSize, value);
				Invalidate(alsoInvalidateParent: true);
			}
		}
	}

	public Point MaximumSize
	{
		get
		{
			return _maximumSize;
		}
		set
		{
			Point maximumSize = _maximumSize;
			if (!(maximumSize == value))
			{
				_maximumSize = value;
				if (_innerPanel != null)
				{
					_innerPanel.MaximumSize = InnerPanelSizeFrom(value);
				}
				OnMaximumSizeChanged(maximumSize, value);
				Invalidate(alsoInvalidateParent: true);
			}
		}
	}

	protected bool ShouldDrawHover
	{
		get
		{
			if (InputHandler.MouseFocus != this)
			{
				return InputHandler.MouseFocus == null;
			}
			return true;
		}
	}

	protected virtual bool AccelOnlyFocus => false;

	protected virtual bool NeedsInputChars => false;

	public bool IsVisibleInTree
	{
		get
		{
			if (_disposed)
			{
				return false;
			}
			if (!_visible)
			{
				return false;
			}
			Base parent = Parent;
			if (parent != null && !parent.IsVisibleInTree)
			{
				return ToolTip.IsActiveTooltip(parent);
			}
			return true;
		}
		set
		{
			(Canvas ?? this).RunOnMainThread(SetVisible, this, value);
		}
	}

	public bool IsVisibleInParent
	{
		get
		{
			if (IsHidden)
			{
				Base parent = Parent;
				if (parent != null)
				{
					return ToolTip.IsActiveTooltip(parent);
				}
			}
			return true;
		}
		set
		{
			Base obj = Canvas ?? this;
			obj.RunOnMainThread(SetVisible, this, value);
		}
	}

	public int X
	{
		get
		{
			return _bounds.X;
		}
		set
		{
			SetPosition(value, Y);
		}
	}

	public int Y
	{
		get
		{
			return _bounds.Y;
		}
		set
		{
			SetPosition(X, value);
		}
	}

	public int Width
	{
		get
		{
			return _bounds.Width;
		}
		set
		{
			SetSize(value, Height);
		}
	}

	public int Height
	{
		get
		{
			return _bounds.Height;
		}
		set
		{
			SetSize(Width, value);
		}
	}

	public int OuterWidth => Width + Margin.Left + Margin.Right;

	public int OuterHeight => Height + Margin.Top + Margin.Bottom;

	public Point Size
	{
		get
		{
			return _bounds.Size;
		}
		set
		{
			SetSize(value.X, value.Y);
		}
	}

	public int InnerWidth => _bounds.Width - (_padding.Left + _padding.Right);

	public int MaximumInnerWidth => _maximumSize.X - (_padding.Left + _padding.Right);

	public int InnerHeight => _bounds.Height - (_padding.Top + _padding.Bottom);

	public int MaximumInnerHeight => _maximumSize.Y - (_padding.Top + _padding.Bottom);

	public int Bottom => _bounds.Bottom + _margin.Bottom;

	public int Right => _bounds.Right + _margin.Right;

	public bool DrawDebugOutlines
	{
		get
		{
			return _drawDebugOutlines;
		}
		set
		{
			_drawDebugOutlines = value;
			Base innerPanel = _innerPanel;
			if (innerPanel != null)
			{
				innerPanel.DrawDebugOutlines = value;
			}
			RunOnMainThread(PropagateDrawDebugOutlinesToChildren, this, value);
		}
	}

	public Color PaddingOutlineColor { get; set; }

	public Color MarginOutlineColor { get; set; }

	public Color BoundsOutlineColor { get; set; }

	public Canvas? Canvas => _canvas;

	public bool SkipSerialization { get; set; }

	private List<Base> HostedChildren => _innerPanel?.HostedChildren ?? _children;

	public bool Escamote { get; set; }

	public bool IsActive
	{
		get
		{
			return IsMouseButtonActive(MouseButton.Left);
		}
		set
		{
			if (value)
			{
				_mouseButtonPressed.Add(MouseButton.Left);
			}
			else
			{
				_mouseButtonPressed.Remove(MouseButton.Left);
			}
		}
	}

	public virtual ComponentState ComponentState
	{
		get
		{
			ComponentState result = ComponentState.Normal;
			if (IsDisabledByTree)
			{
				result = ComponentState.Disabled;
			}
			else if (IsActive)
			{
				result = ComponentState.Active;
			}
			else if (IsHovered)
			{
				result = ComponentState.Hovered;
			}
			return result;
		}
	}

	public IReadOnlySet<MouseButton> MouseButtonPressed => _mouseButtonPressed;

	public bool KeepFocusOnMouseExit { get; set; }

	protected virtual bool ShouldSkipLayout
	{
		get
		{
			if (!_visible)
			{
				return !ToolTip.IsActiveTooltip(this);
			}
			return false;
		}
	}

	public int NodeCount => 1 + _children.Sum((Base child) => child.NodeCount);

	public event GwenEventHandler<EventArgs>? Disposed;

	public event GwenEventHandler<EventArgs>? BeforeDraw;

	public event GwenEventHandler<EventArgs>? AfterDraw;

	public event GwenEventHandler<EventArgs>? BeforeLayout;

	public event GwenEventHandler<EventArgs>? AfterLayout;

	public event GwenEventHandler<VisibilityChangedEventArgs>? VisibilityChanged;

	public event GwenEventHandler<EventArgs>? HoverEnter;

	public event GwenEventHandler<EventArgs>? HoverLeave;

	public event GwenEventHandler<ValueChangedEventArgs<Rectangle>>? BoundsChanged;

	public event GwenEventHandler<ValueChangedEventArgs<Point>>? PositionChanged;

	public event GwenEventHandler<ValueChangedEventArgs<Point>>? SizeChanged;

	public virtual event GwenEventHandler<MouseButtonState>? MouseDown;

	public virtual event GwenEventHandler<MouseButtonState>? MouseUp;

	public virtual event GwenEventHandler<MouseButtonState>? Clicked;

	public virtual event GwenEventHandler<MouseButtonState>? DoubleClicked;

	protected virtual string InternalToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(GetType().GetName());
		string value = _name?.Trim();
		if (!string.IsNullOrWhiteSpace(value))
		{
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(2, 1, stringBuilder2);
			handler.AppendLiteral("(");
			handler.AppendFormatted(value);
			handler.AppendLiteral(")");
			stringBuilder2.Append(ref handler);
		}
		return stringBuilder.ToString();
	}

	public Base(Base? parent = null, string? name = null)
	{
		_threadQueue = new ThreadQueue();
		_threadQueue.QueueNotEmpty += OnThreadQueueOnQueueNotEmpty;
		_name = name ?? string.Empty;
		_visible = true;
		_dockDirty = true;
		_layoutDirty = true;
		_disabled = false;
		_tooltip = null;
		_tooltipEnabled = true;
		_cacheTextureDirty = true;
		_cacheToTexture = false;
		_drawBackground = true;
		_keyboardInputEnabled = false;
		_mouseInputEnabled = true;
		_tabEnabled = false;
		PreLayout = new ManualActionQueue(_preLayoutActionsParent);
		PostLayout = new ManualActionQueue(_postLayoutActionsParent);
		if (this is Canvas canvas)
		{
			_canvas = canvas;
		}
		Parent = parent;
		_bounds = new Rectangle(0, 0, 10, 10);
		_padding = Padding.Zero;
		_margin = Margin.Zero;
		_color = Color.White;
		_alignmentPadding = Padding.Zero;
		RestrictToParent = false;
		_cursor = Cursors.Default;
		BoundsOutlineColor = Color.Red;
		MarginOutlineColor = Color.Green;
		PaddingOutlineColor = Color.Blue;
	}

	private void OnThreadQueueOnQueueNotEmpty()
	{
		UpdatePendingThreadQueues(1);
	}

	private static void ProcessDetachingFromParent(NodePair pair)
	{
		pair.This.NotifyDetachingFromRoot(pair.Parent.Root);
		pair.This.NotifyDetaching(pair.Parent);
		pair.Parent.RemoveChild(pair.This, dispose: false);
	}

	private static void ProcessAttachingToParent(NodePair pair)
	{
		ProcessAttachingToParent(pair.This, pair.Parent);
	}

	private static void ProcessAttachingToParent(Base @this, Base? parent)
	{
		ThreadQueue threadQueue = parent?._threadQueue;
		if (threadQueue != null)
		{
			@this._threadQueue.SetMainThreadId(threadQueue);
		}
		@this.PropagateCanvas(parent?._canvas);
		@this._host = parent;
		@this._parent = null;
		if (parent != null)
		{
			parent.AddChild(@this);
			@this.NotifyAttachingToRoot(parent.Root);
			@this.NotifyAttaching(parent);
		}
	}

	protected virtual void OnAttached(Base parent)
	{
	}

	protected virtual void OnAttaching(Base newParent)
	{
	}

	private void NotifyAttaching(Base parent)
	{
		try
		{
			parent.UpdatePendingThreadQueues(_pendingThreadQueues);
			OnAttaching(parent);
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Error occurred while invoking OnAttaching() for {NodeName}", ParentQualifiedName);
		}
	}

	protected virtual void OnDetached()
	{
	}

	protected virtual void OnDetaching(Base oldParent)
	{
	}

	private void NotifyDetaching(Base parent)
	{
		try
		{
			parent.UpdatePendingThreadQueues(-_pendingThreadQueues);
			OnDetaching(parent);
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Error occurred while invoking OnDetaching() for {NodeName}", ParentQualifiedName);
		}
	}

	protected virtual void OnDetachingFromRoot(Base root)
	{
	}

	private void NotifyDetachingFromRoot(Base root)
	{
		try
		{
			IUpdatableDataProvider[] providers = (from e in _updatableDataProviderRefCounts
				where e.Value.Visible > 0
				select e.Key).ToArray();
			root.VisibleUpdatableDataProviders(providers, visible: false);
			root.ListenUpdatableDataProviders(_updatableDataProviderRefCounts.Keys, listen: false);
			try
			{
				OnDetachingFromRoot(root);
			}
			catch (Exception exception)
			{
				ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Error occurred while invoking OnDetachingFromRoot() for {NodeName}", ParentQualifiedName);
			}
			foreach (Base child in _children)
			{
				child.NotifyDetachingFromRoot(root);
			}
		}
		catch (Exception exception2)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception2, "Error occurred while invoking NotifyDetachingFromRoot() for {NodeName} from {RootName}", ParentQualifiedName, root.ParentQualifiedName);
		}
	}

	protected virtual void OnAttachingToRoot(Base root)
	{
	}

	private void NotifyAttachingToRoot(Base root)
	{
		try
		{
			IUpdatableDataProvider[] providers = (from e in _updatableDataProviderRefCounts
				where e.Value.Visible > 0
				select e.Key).ToArray();
			Root.ListenUpdatableDataProviders(_updatableDataProviderRefCounts.Keys, listen: true);
			Root.VisibleUpdatableDataProviders(providers, visible: true);
			try
			{
				OnAttachingToRoot(root);
			}
			catch (Exception exception)
			{
				ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Error occurred while invoking OnAttachingToRoot() for {NodeName}", ParentQualifiedName);
			}
			foreach (Base child in _children)
			{
				child.NotifyAttachingToRoot(root);
			}
		}
		catch (Exception exception2)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception2, "Error occurred while invoking NotifyAttachingToRoot() for {NodeName} to {RootName}", ParentQualifiedName, root.ParentQualifiedName);
		}
	}

	private void PropagateCanvas(Canvas? canvas)
	{
		try
		{
			_canvas = canvas;
			foreach (Base child in _children)
			{
				child.PropagateCanvas(canvas);
			}
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Exception thrown while invoking PropagateCanvas() for {NodeName} with {CanvasName}", ParentQualifiedName, canvas?.ParentQualifiedName ?? "null");
		}
	}

	public void InvalidateAlignment()
	{
		if (!_needsAlignment)
		{
			_needsAlignment = true;
		}
		Invalidate();
	}

	protected static void InvalidateAlignment(Base @this)
	{
		@this.InvalidateAlignment();
	}

	protected virtual void OnDockChanged(Pos oldDock, Pos newDock)
	{
	}

	protected virtual void OnDisabledChanged(bool oldValue, bool newValue)
	{
		Invalidate();
	}

	private static void SetVisible(Base @this, bool value)
	{
		if (@this._disposed)
		{
			return;
		}
		bool visible = @this._visible;
		if (@this.GetType().Name == "VersionPanel")
		{
			Debugger.Break();
		}
		if (@this != null && @this._inheritParentEnablementProperties)
		{
			Base parent = @this.Parent;
			if (parent != null)
			{
				if (value != parent._visible)
				{
					ApplicationContext.CurrentContext.Logger.LogTrace("Tried to change visibility of restricted node '{NodeName}' to {Visible} when the parent ({ParentNodeName}) is set to {ParentVisible}", @this.CanonicalName, value, parent.CanonicalName, !value);
				}
				value = parent._visible;
			}
		}
		if (value != visible)
		{
			bool flag = value != @this.IsVisibleInTree;
			@this._visible = value;
			if (flag)
			{
				@this.NotifyVisibilityInTreeChanged(value);
			}
			if (@this._dock != Pos.None)
			{
				@this.InvalidateParentDock();
			}
			VisibilityChangedEventArgs e = new VisibilityChangedEventArgs(value, flag ? value : (!value));
			@this.OnVisibilityChanged(@this, e);
			@this.Parent?.OnChildVisibilityChanged(@this, e);
			@this.VisibilityChanged?.Invoke(@this, e);
			@this.Invalidate();
			@this.InvalidateParent();
		}
	}

	private void NotifyVisibilityInTreeChanged(bool visible)
	{
		VisibleUpdatableDataProviders(_updatableDataProviderRefCounts.Keys, visible);
		foreach (Base child in _children)
		{
			if (child._inheritParentEnablementProperties)
			{
				SetVisible(child, _visible);
			}
			else if (child._visible)
			{
				child.NotifyVisibilityInTreeChanged(visible);
				child.VisibilityChanged?.Invoke(this, new VisibilityChangedEventArgs(child._visible, visible));
			}
		}
	}

	protected virtual void OnVisibilityChanged(object? sender, VisibilityChangedEventArgs eventArgs)
	{
	}

	protected virtual void OnChildVisibilityChanged(object? sender, VisibilityChangedEventArgs eventArgs)
	{
	}

	protected virtual Point InnerPanelSizeFrom(Point size)
	{
		return size;
	}

	private void OnTabOrderChanged(int oldTabOrder, int newTabOrder)
	{
		IsTabable = newTabOrder > 0;
		Parent?.OnChildTabOrderChanged(this, oldTabOrder, newTabOrder);
	}

	protected virtual void OnChildTabOrderChanged(Base? childNode, int oldTabOrder, int newTabOrder)
	{
	}

	private static void PropagateDrawDebugOutlinesToChildren(Base @this, bool drawDebugOutlines)
	{
		foreach (Base child in @this._children)
		{
			child.DrawDebugOutlines = drawDebugOutlines;
		}
	}

	public void Dispose()
	{
		try
		{
			ObjectDisposedException.ThrowIf(_disposed, this);
		}
		catch
		{
			throw;
		}
		_disposeStack = new StackTrace(fNeedFileInfo: true);
		_disposed = true;
		_threadQueue.ClearPending();
		Dispose(disposing: true);
		ICacheToTexture cacheToTexture = null;
		try
		{
			cacheToTexture = DisposeSkin?.Renderer.Ctt;
		}
		catch
		{
		}
		if (ShouldCacheToTexture)
		{
			cacheToTexture?.DisposeCachedTexture(this);
		}
		if (InputHandler.HoveredControl == this)
		{
			InputHandler.HoveredControl = null;
		}
		if (InputHandler.KeyboardFocus == this)
		{
			InputHandler.KeyboardFocus = null;
		}
		if (InputHandler.MouseFocus == this)
		{
			InputHandler.MouseFocus = null;
		}
		DragAndDrop.ControlDeleted(this);
		ToolTip.ControlDeleted(this);
		Animation.Cancel(this);
		DisposeChildrenOf(this);
		GC.SuppressFinalize(this);
		_disposeCompleted = true;
		try
		{
			this.Disposed?.Invoke(this, EventArgs.Empty);
		}
		catch (Exception exception)
		{
			(ApplicationContext.GetContext<IApplicationContext>()?.Logger)?.LogWarning(exception, "Exception thrown by a Disposed event handler for '{Node}'", CanonicalName);
		}
	}

	protected virtual void Dispose(bool disposing)
	{
	}

	private void DisposeChildrenOf(Base target)
	{
		Base host = _host;
		if (host == null || host._disposeCompleted)
		{
			DisposeChildren(target);
		}
		else
		{
			host.DisposeChildrenOf(target);
		}
	}

	private static void DisposeChildren(Base @this)
	{
		Base[] array = @this._children.ToArray();
		try
		{
			Base[] array2 = array;
			foreach (Base obj in array2)
			{
				if (obj._disposed)
				{
					ApplicationContext.CurrentContext?.Logger.LogWarning("Enfant deja detruit dans {Parent} : {Enfant}, detruit la premiere fois par : {Pile}", @this.CanonicalName, obj.GetType().Name, obj._disposeStack);
				}
				else
				{
					obj.Dispose();
				}
			}
		}
		catch
		{
			throw;
		}
		if (@this is Modal)
		{
			ApplicationContext.CurrentContext.Logger.LogTrace("Clearing {ChildCount} children of Modal {Name}", @this._children.Count, @this.CanonicalName);
		}
		@this._children.Clear();
	}

	public void AddAlignment(Alignments alignment)
	{
		List<Alignments> alignments = _alignments;
		if (alignments != null && !alignments.Contains(alignment))
		{
			_alignments.Add(alignment);
		}
	}

	public void RemoveAlignments()
	{
		_alignments.Clear();
	}

	public virtual string GetJsonUI(bool isRoot = false)
	{
		return JsonConvert.SerializeObject(GetJson(isRoot), Formatting.Indented);
	}

	private static void AddChildrenToJson(Base @this, JObject json)
	{
		foreach (Base child in @this._children)
		{
			if (child == @this._innerPanel || child == @this.Tooltip)
			{
				continue;
			}
			string name = child._name;
			if (name != null && !string.IsNullOrWhiteSpace(name))
			{
				if (json.ContainsKey(name))
				{
					ApplicationContext.CurrentContext.Logger.LogWarning("Unable to add '{ChildName}' to the JSON of {ParentName} because a sibling shares the same name", name, @this.CanonicalName);
				}
				else
				{
					JObject json2 = child.GetJson();
					json.Add(child.Name, json2);
				}
			}
		}
	}

	public virtual JObject? GetJson(bool isRoot = false, bool onlySerializeIfNotEmpty = false)
	{
		JObject jObject = new JObject();
		RunOnMainThread(AddChildrenToJson, this, jObject);
		if (onlySerializeIfNotEmpty && !jObject.HasValues)
		{
			return null;
		}
		isRoot |= Parent == null;
		Rectangle rectangle = (isRoot ? new Rectangle(_boundsOnDisk.X, _boundsOnDisk.Y, _bounds.Width, _bounds.Height) : _bounds);
		JObject jObject2 = new JObject(new JProperty("Bounds", Rectangle.ToString(rectangle)), new JProperty("Dock", Dock.ToString()), new JProperty("Padding", Padding.ToString(_padding)), new JProperty("AlignmentEdgeDistances", Padding.ToString(_alignmentPadding)), new JProperty("AlignmentTransform", _alignmentTranslation.ToString()), new JProperty("Margin", _margin.ToString()), new JProperty("RenderColor", Color.ToString(_color)), new JProperty("Alignments", string.Join(",", _alignments.ToArray())), new JProperty("DrawBackground", _drawBackground), new JProperty("MinimumSize", _minimumSize.ToString()), new JProperty("MaximumSize", _maximumSize.ToString()), new JProperty("Disabled", _disabled), new JProperty("Hidden", !_visible), new JProperty("RestrictToParent", _restrictToParent), new JProperty("MouseInputEnabled", _mouseInputEnabled), new JProperty("HideToolTip", !_tooltipEnabled), new JProperty("ToolTipBackground", _tooltipBackgroundName), new JProperty("ToolTipFont", _tooltipFontInfo), new JProperty("TooltipTextColor", (_tooltipTextColor == null) ? ((object?)JValue.CreateNull()) : ((object?)Color.ToString(_tooltipTextColor))));
		Base innerPanel = _innerPanel;
		if (innerPanel != null)
		{
			JObject json = innerPanel.GetJson(isRoot: false, onlySerializeIfNotEmpty: true);
			if (json != null)
			{
				jObject2.Add("InnerPanel", json);
			}
		}
		if (jObject.HasValues)
		{
			jObject2.Add("Children", jObject);
		}
		return FixJson(jObject2);
	}

	public virtual JObject FixJson(JObject json)
	{
		if (json.Remove<string, JToken>("InnerPanel", out var value))
		{
			json.Add("InnerPanel", value);
		}
		if (json.Remove<string, JToken>("Children", out var value2))
		{
			json.Add("Children", value2);
		}
		return json;
	}

	protected virtual Rectangle ValidateJsonBounds(Rectangle bounds)
	{
		return bounds;
	}

	public void LoadJsonUi(GameContentManager.UI stage, string? resolution = null, bool saveOutput = true)
	{
		if (string.IsNullOrWhiteSpace(Name))
		{
			ApplicationContext.Context.Value?.Logger.LogWarning("Attempted to load layout for nameless " + GetType().FullName);
			return;
		}
		if (string.IsNullOrWhiteSpace(resolution) || string.IsNullOrEmpty(resolution))
		{
			ApplicationContext.Context.Value?.Logger.LogWarning("Attempted to load layout for " + Name + " with no resolution");
			return;
		}
		GameContentManager.Current?.GetLayout(stage, Name, resolution, skipCache: false, delegate(string rawLayout, bool cacheHit)
		{
			if (!string.IsNullOrWhiteSpace(rawLayout))
			{
				try
				{
					JObject jObject = JsonConvert.DeserializeObject<JObject>(rawLayout);
					if (jObject != null)
					{
						LoadJson(jObject, isRoot: true);
						ProcessAlignments();
						OnJsonReloaded();
					}
				}
				catch (Exception innerException)
				{
					throw new Exception("Error loading json ui for " + ParentQualifiedName, innerException);
				}
			}
			if (!cacheHit && saveOutput)
			{
				GameContentManager.Current?.SaveUIJson(stage, Name, GetJsonUI(isRoot: true), resolution);
			}
		});
	}

	protected virtual void OnJsonReloaded()
	{
	}

	public virtual void LoadJson(JToken token, bool isRoot = false)
	{
		if (!(token is JObject jObject))
		{
			return;
		}
		if (jObject["Alignments"] != null)
		{
			RemoveAlignments();
			string[] array = ((string?)jObject["Alignments"]).Split(',');
			string[] array2 = array;
			foreach (string text in array2)
			{
				switch (text.ToLower())
				{
				case "top":
					AddAlignment(Alignments.Top);
					break;
				case "bottom":
					AddAlignment(Alignments.Bottom);
					break;
				case "left":
					AddAlignment(Alignments.Left);
					break;
				case "right":
					AddAlignment(Alignments.Right);
					break;
				case "center":
					AddAlignment(Alignments.Center);
					break;
				case "centerh":
					AddAlignment(Alignments.CenterH);
					break;
				case "centerv":
					AddAlignment(Alignments.CenterV);
					break;
				}
			}
		}
		if (jObject["MinimumSize"] != null)
		{
			MinimumSize = Point.FromString((string?)jObject["MinimumSize"]);
		}
		if (jObject["MaximumSize"] != null)
		{
			MaximumSize = Point.FromString((string?)jObject["MaximumSize"]);
		}
		if (jObject["Bounds"] != null)
		{
			_boundsOnDisk = Rectangle.FromString((string?)jObject["Bounds"]);
			isRoot = isRoot || Parent == null;
			if (isRoot)
			{
				SetSize(_boundsOnDisk.Width, _boundsOnDisk.Height);
			}
			else
			{
				SetBounds(ValidateJsonBounds(_boundsOnDisk));
			}
		}
		if (jObject.TryGet<Pos>("Dock", out var propertyValue))
		{
			Dock = propertyValue;
		}
		if (jObject["Padding"] != null)
		{
			Padding = Padding.FromString((string?)jObject["Padding"]);
		}
		if (jObject["AlignmentEdgeDistances"] != null)
		{
			_alignmentPadding = Padding.FromString((string?)jObject["AlignmentEdgeDistances"]);
		}
		if (jObject["AlignmentTransform"] != null)
		{
			_alignmentTranslation = Point.FromString((string?)jObject["AlignmentTransform"]);
		}
		if (jObject["Margin"] != null)
		{
			Margin = Margin.FromString((string?)jObject["Margin"]);
		}
		if (jObject["RenderColor"] != null)
		{
			RenderColor = Color.FromString((string?)jObject["RenderColor"]);
		}
		if (jObject["DrawBackground"] != null)
		{
			ShouldDrawBackground = (bool)jObject["DrawBackground"];
		}
		if (jObject["Disabled"] != null)
		{
			IsDisabled = (bool)jObject["Disabled"];
		}
		if (jObject["Hidden"] != null)
		{
			IsHidden = (bool)jObject["Hidden"];
		}
		if (jObject["RestrictToParent"] != null)
		{
			RestrictToParent = (bool)jObject["RestrictToParent"];
		}
		if (jObject["MouseInputEnabled"] != null)
		{
			MouseInputEnabled = (bool)jObject["MouseInputEnabled"];
		}
		if (jObject["HideToolTip"] != null && (bool)jObject["HideToolTip"])
		{
			_tooltipEnabled = false;
			SetToolTipText(null);
		}
		if (jObject["TooltipBackground"] is JValue { Type: JTokenType.String } jValue)
		{
			string tooltipBackgroundName = jValue.Value<string>();
			TooltipBackgroundName = tooltipBackgroundName;
		}
		if (jObject.TryGetValue("TooltipFont", out JToken value) && value is JValue { Type: JTokenType.String } jValue2)
		{
			string text2 = jValue2.Value<string>();
			if (text2 != null)
			{
				string[] source = text2.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				string tooltipFontName = source.FirstOrDefault();
				if (!int.TryParse(source.Skip(1).FirstOrDefault(), out var result))
				{
					result = 10;
				}
				TooltipFontName = tooltipFontName;
				TooltipFontSize = result;
			}
			else
			{
				TooltipFont = null;
			}
		}
		if (jObject["TooltipTextColor"] is JValue { Type: JTokenType.String } jValue3)
		{
			string text3 = jValue3.Value<string>();
			if (!string.IsNullOrWhiteSpace(text3))
			{
				TooltipTextColor = Color.FromString(text3);
			}
		}
		UpdateToolTipProperties();
		Base innerPanel = _innerPanel;
		if (innerPanel != null && jObject.TryGetValue("InnerPanel", out JToken value2))
		{
			innerPanel.LoadJson(value2);
		}
		if (jObject.TryGetValue("Children", out JToken value3) && value3 is JObject state)
		{
			RunOnMainThread(LoadChildrenJson, this, state);
		}
		Invalidate(alsoInvalidateParent: true);
	}

	private static void LoadChildrenJson(Base @this, JObject objectChildren)
	{
		foreach (Base child in @this._children)
		{
			string name = child._name;
			if (name != null && !string.IsNullOrWhiteSpace(name) && objectChildren.TryGetValue(name, out JToken value))
			{
				child.LoadJson(value);
			}
		}
	}

	public virtual void ProcessAlignments()
	{
		foreach (Alignments alignment in _alignments)
		{
			switch (alignment)
			{
			case Alignments.Top:
				Align.AlignTop(this);
				break;
			case Alignments.Bottom:
				Align.AlignBottom(this);
				break;
			case Alignments.Left:
				Align.AlignLeft(this);
				break;
			case Alignments.Right:
				Align.AlignRight(this);
				break;
			case Alignments.Center:
				Align.Center(this);
				break;
			case Alignments.CenterH:
				Align.CenterHorizontally(this);
				break;
			case Alignments.CenterV:
				Align.CenterVertically(this);
				break;
			default:
				throw new ArgumentOutOfRangeException("alignment", alignment, null);
			}
		}
		MoveTo(X + _alignmentTranslation.X, Y + _alignmentTranslation.Y, aligning: true);
		foreach (Base child in Children)
		{
			child?.ProcessAlignments();
		}
	}

	public void DelayedDelete()
	{
		Canvas?.AddDelayedDelete(this);
	}

	public override string ToString()
	{
		return _cachedToString ?? (_cachedToString = InternalToString());
	}

	public void Enable()
	{
		IsDisabled = false;
	}

	public virtual void Disable()
	{
		IsDisabled = true;
	}

	private void DefaultAcceleratorHandler(Base control, EventArgs args)
	{
		OnAccelerator();
	}

	protected virtual void OnAccelerator()
	{
	}

	public virtual void Hide()
	{
		IsHidden = true;
	}

	public virtual void Show()
	{
		IsHidden = false;
	}

	public virtual void ToggleHidden()
	{
		IsHidden = !IsHidden;
	}

	public virtual void SetToolTipText(string? text)
	{
		Base tooltip = Tooltip;
		if (!_tooltipEnabled || string.IsNullOrWhiteSpace(text))
		{
			Base tooltip2 = Tooltip;
			if (tooltip2 != null && tooltip2.Parent != null)
			{
				Tooltip?.Parent.RemoveChild(Tooltip, dispose: true);
			}
			Tooltip = null;
			return;
		}
		Label label = tooltip as Label;
		if (label == null)
		{
			if (tooltip != null)
			{
				return;
			}
			label = (Label)(Tooltip = new Label(this, "Tooltip")
			{
				AutoSizeToContents = true,
				Font = (_tooltipFont ?? GameContentManager.Current.GetFont("sourcesansproblack")),
				FontSize = TooltipFontSize,
				MaximumSize = new Point(300, 0),
				Padding = new Padding(5, 3),
				TextColor = Skin.Colors.TooltipText,
				TextColorOverride = (_tooltipTextColor ?? Skin.Colors.TooltipText),
				ToolTipBackground = _tooltipBackground,
				WrappingBehavior = WrappingBehavior.Wrapped
			});
		}
		label.Text = text;
	}

	protected virtual void UpdateToolTipProperties()
	{
		if (Tooltip != null && Tooltip is Label label)
		{
			label.TextColorOverride = _tooltipTextColor ?? Skin.Colors.TooltipText;
			if (_tooltipFont != null)
			{
				label.Font = _tooltipFont;
			}
			label.ToolTipBackground = _tooltipBackground;
		}
	}

	protected virtual void InvalidateChildren(bool recursive = false)
	{
		RunOnMainThread(InvalidateChildren, this, recursive);
		_innerPanel?.Invalidate();
		_innerPanel?.InvalidateChildren(recursive: true);
	}

	private static void InvalidateChildren(Base @this, bool recursive)
	{
		foreach (Base child in @this._children)
		{
			child.Invalidate();
			if (recursive)
			{
				child.InvalidateChildren(recursive: true);
			}
		}
	}

	public virtual void Invalidate()
	{
		if (!_layoutDirty)
		{
			_layoutDirty = true;
		}
		if (!_cacheTextureDirty)
		{
			_cacheTextureDirty = true;
		}
	}

	protected static void InvalidateDock(Base @this)
	{
		@this.InvalidateDock();
	}

	protected static void Invalidate(Base @this)
	{
		@this.Invalidate();
	}

	public void Invalidate(bool alsoInvalidateParent)
	{
		Invalidate();
		if (alsoInvalidateParent)
		{
			InvalidateParent();
		}
	}

	public void MoveBefore(Base other)
	{
		if (this != other)
		{
			_host?.RunOnMainThread(MoveChildBeforeOther, this, other);
		}
	}

	private static void MoveChildBeforeOther(Base @this, Base childToMove, Base other)
	{
		if (other.Parent != @this)
		{
			return;
		}
		List<Base> hostedChildren = @this.HostedChildren;
		int num = hostedChildren.IndexOf(other);
		if (num < 0)
		{
			ApplicationContext.Context.Value?.Logger.LogError("Possible race condition detected: '{Other}' is not a child of '{Parent}'", other.CanonicalName, @this.CanonicalName);
			return;
		}
		int num2 = hostedChildren.IndexOf(childToMove);
		if (num2 < 0)
		{
			hostedChildren.Insert(num, childToMove);
			return;
		}
		int num3 = num;
		if (num2 < num)
		{
			num3--;
		}
		if (num2 != num3)
		{
			hostedChildren.Remove(childToMove);
			hostedChildren.Insert(num3, childToMove);
		}
	}

	public void MoveAfter(Base other)
	{
		if (this != other)
		{
			_host?.RunOnMainThread(MoveChildAfterOther, this, other);
		}
	}

	private static void MoveChildAfterOther(Base @this, Base childToMove, Base other)
	{
		if (other.Parent != @this)
		{
			return;
		}
		List<Base> hostedChildren = @this.HostedChildren;
		int num = hostedChildren.IndexOf(other);
		if (num < 0)
		{
			ApplicationContext.Context.Value?.Logger.LogError("Possible race condition detected: '{Other}' is not a child of '{Parent}'", other.CanonicalName, @this.CanonicalName);
			return;
		}
		int num2 = hostedChildren.IndexOf(childToMove);
		if (num2 < 0)
		{
			hostedChildren.Insert(num + 1, childToMove);
			return;
		}
		int num3 = num;
		if (num2 > num3)
		{
			num3++;
		}
		if (num2 != num3)
		{
			hostedChildren.Remove(childToMove);
			hostedChildren.Insert(num3, childToMove);
		}
	}

	public void SortChildrenBy<TSortKey>(Func<Base?, TSortKey> keySelector) where TSortKey : IComparable<TSortKey>
	{
		_children.Sort(delegate(Base a, Base b)
		{
			TSortKey other = keySelector(b);
			TSortKey val = keySelector(a);
			if (val != null)
			{
				return val.CompareTo(other);
			}
			ref TSortKey reference = ref other;
			TSortKey val2 = default(TSortKey);
			if (val2 == null)
			{
				val2 = reference;
				reference = ref val2;
				if (val2 == null)
				{
					return 0;
				}
			}
			return -reference.CompareTo(default(TSortKey));
		});
	}

	public void ClearChildren(bool dispose = false)
	{
		RunOnMainThread(ClearChildren, this, dispose);
	}

	private static void ClearChildren(Base @this, bool dispose)
	{
		if (@this is Modal)
		{
			ApplicationContext.CurrentContext.Logger.LogTrace("Clearing {ChildCount} children of Modal {Name}", @this._children.Count, @this.CanonicalName);
		}
		if (dispose)
		{
			foreach (Base hostedChild in @this.HostedChildren)
			{
				hostedChild.Dispose();
			}
		}
		@this.HostedChildren.Clear();
		@this.Invalidate();
	}

	public void Insert(int index, Base node)
	{
		RunOnMainThread(Insert, this, index, node);
	}

	private static void Insert(Base @this, int index, Base node)
	{
		@this._children.Insert(index, node);
	}

	public void Remove(Base node)
	{
		RunOnMainThread(Remove, this, node);
	}

	private static void Remove(Base @this, Base node)
	{
		int num = @this._children.BinarySearch(node);
		if (num >= 0)
		{
			RemoveAt(@this, num);
		}
	}

	public void RemoveAt(int index)
	{
		RunOnMainThread(RemoveAt, this, index);
	}

	private static void RemoveAt(Base @this, int index)
	{
		if (@this is Modal)
		{
			ApplicationContext.CurrentContext.Logger.LogTrace("Removing child at {Index} of Modal {Name}", index, @this.CanonicalName);
		}
		@this._children.RemoveAt(index);
	}

	public int IndexOf(Base? node)
	{
		if (node?.Parent != this)
		{
			return -1;
		}
		return RunOnMainThread(IndexOf, node, invalidate: true);
	}

	private static int IndexOf(Base @this, Base node)
	{
		return @this._children.IndexOf(node);
	}

	public int IndexOf(Func<Base?, bool> predicate)
	{
		return RunOnMainThread(IndexOf, predicate, invalidate: true);
	}

	private static int IndexOf(Base @this, Func<Base?, bool> predicate)
	{
		for (int i = 0; i < @this._children.Count; i++)
		{
			if (predicate(@this._children[i]))
			{
				return i;
			}
		}
		return -1;
	}

	public int LastIndexOf(Func<Base?, bool> predicate)
	{
		return RunOnMainThread(LastIndexOf, predicate, invalidate: true);
	}

	private static int LastIndexOf(Base @this, Func<Base?, bool> predicate)
	{
		for (int num = @this._children.Count - 1; num >= 0; num--)
		{
			if (predicate(@this._children[num]))
			{
				return num;
			}
		}
		return -1;
	}

	public void ResortChild<TKey>(Base child, Func<Base?, TKey?> keySelector) where TKey : IComparable<TKey>
	{
		if (child.Parent == this)
		{
			RunOnMainThread(ResortChild, this, child, keySelector);
		}
	}

	private static void ResortChild<TKey>(Base @this, Base child, Func<Base?, TKey?> keySelector) where TKey : IComparable<TKey>
	{
		@this._children.Resort(child, keySelector);
	}

	public void SendToBack()
	{
		RunOnMainThread(SendToBack, this);
	}

	private static void SendToBack(Base @this)
	{
		if (@this._parent != null && @this._parent._children.Count != 0 && @this._parent._children.First() != @this && @this._parent._children.Remove(@this))
		{
			@this._parent._children.Insert(0, @this);
			@this.InvalidateParent();
		}
	}

	public void BringToFront()
	{
		RunOnMainThread(BringToFront, this);
	}

	private static void BringToFront(Base @this)
	{
		if (@this._host is Modal modal)
		{
			modal.BringToFront();
		}
		Base parent = @this._parent;
		if (parent != null)
		{
			Base obj = parent._children.LastOrDefault();
			if (obj != null && obj != @this && parent._children.Remove(@this))
			{
				parent._children.Add(@this);
				@this.InvalidateParent();
				@this.Redraw();
			}
		}
	}

	public virtual Base Find(Predicate<Base> predicate, bool recurse = false)
	{
		Base obj = _children.Find(predicate);
		if (obj != null)
		{
			return obj;
		}
		if (!recurse)
		{
			return null;
		}
		return _children.Select((Base selectChild) => selectChild?.Find(predicate, recurse: true)).FirstOrDefault();
	}

	public (Base Highest, Base Closest)? FindMatchingNodes<TComponent>() where TComponent : class
	{
		Base obj = null;
		Base obj2 = null;
		for (Base obj3 = this; obj3 != null; obj3 = obj3.Parent)
		{
			if (obj3 is TComponent)
			{
				if (obj2 == null)
				{
					obj2 = obj3;
				}
				obj = obj3;
			}
		}
		if (obj != null && obj2 != null)
		{
			return (obj, obj2);
		}
		return null;
	}

	public virtual IEnumerable<Base> FindAll(Predicate<Base> predicate, bool recurse = false)
	{
		List<Base> list = new List<Base>();
		list.AddRange(_children.FindAll(predicate));
		if (recurse)
		{
			list.AddRange(_children.SelectMany((Base selectChild) => selectChild?.FindAll(predicate, recurse: true)));
		}
		return list;
	}

	public virtual Base? FindChildByName(string name, bool recursive = false)
	{
		return FindChildByName<Base>(name, recursive);
	}

	public virtual TComponent? FindChildByName<TComponent>(string name, bool recursive = false) where TComponent : Base
	{
		Base[] array = Children.ToArray();
		Base[] array2 = array;
		foreach (Base obj in array2)
		{
			if (obj is TComponent val && string.Equals(val.Name, name, StringComparison.Ordinal))
			{
				return val;
			}
			if (recursive)
			{
				return obj.FindChildByName<TComponent>(name, recursive: true);
			}
		}
		return null;
	}

	public Modal MakeModal(bool dim = false)
	{
		Modal modal = _modal;
		if (modal != null)
		{
			return modal;
		}
		Canvas canvas = Canvas;
		if (canvas == null)
		{
			throw new InvalidOperationException("Tried to make a modal without being attached to a canvas");
		}
		modal = (_modal = new Modal(canvas)
		{
			ShouldDrawBackground = dim
		});
		_previousParent = Parent;
		Parent = modal;
		return modal;
	}

	public void RemoveModal()
	{
		Modal modal = _modal;
		if (modal != null)
		{
			Parent = _previousParent;
			Canvas?.RemoveChild(modal, dispose: false);
			_modal = null;
		}
	}

	public void AddChild(Base node)
	{
		RunOnMainThread(AddChild, this, node);
	}

	private static void AddChild(Base @this, Base node)
	{
		if (@this._innerPanel == null)
		{
			@this._children.Add(node);
			node._parent = @this;
		}
		else
		{
			@this._innerPanel.AddChild(node);
		}
		if (node._dock != Pos.None)
		{
			@this.InvalidateDock();
		}
		node.DrawDebugOutlines = @this.DrawDebugOutlines;
		@this.OnChildAdded(node);
	}

	public virtual void RemoveChild(Base child, bool dispose)
	{
		RunOnMainThread(RemoveChild, this, child, dispose);
	}

	private static void RemoveChild(Base @this, Base child, bool dispose)
	{
		if (@this._innerPanel == child)
		{
			try
			{
				if (@this is Modal)
				{
					ApplicationContext.CurrentContext.Logger.LogTrace("Removing {ChildName} (inner panel) from Modal {Name} (Thread {ThreadId})", child.CanonicalName, @this.CanonicalName, Environment.CurrentManagedThreadId);
				}
				@this._children.Remove(child);
				if (dispose)
				{
					child.DelayedDelete();
				}
				@this._innerPanel = null;
				return;
			}
			catch (NullReferenceException)
			{
				throw;
			}
		}
		Base innerPanel = @this._innerPanel;
		if (innerPanel != null && innerPanel.Children.Contains(child))
		{
			innerPanel.RemoveChild(child, dispose);
			return;
		}
		if (@this is Modal)
		{
			ApplicationContext.CurrentContext.Logger.LogTrace("Removing {ChildName} (normal child) from Modal {Name} (Thread {ThreadId})", child.CanonicalName, @this.CanonicalName, Environment.CurrentManagedThreadId);
		}
		@this._children.Remove(child);
		@this.OnChildRemoved(child);
		if (dispose)
		{
			child.DelayedDelete();
		}
	}

	public virtual void DeleteAllChildren()
	{
		while (_children.Count > 0)
		{
			RemoveChild(_children[0], dispose: true);
		}
	}

	protected virtual void OnChildAdded(Base child)
	{
		child?.OnAttached(this);
		Invalidate();
	}

	protected virtual void OnChildRemoved(Base child)
	{
		child?.OnDetached();
		Invalidate();
	}

	public virtual void MoveBy(int x, int y)
	{
		SetBounds(X + x, Y + y, Width, Height);
	}

	public virtual void MoveTo(float x, float y)
	{
		MoveTo((int)x, (int)y);
	}

	public virtual void MoveTo(int x, int y, bool aligning = false)
	{
		int width = Width;
		int height = Height;
		Margin margin = Margin;
		if (RestrictToParent)
		{
			Base parent = Parent;
			if (parent != null)
			{
				Padding padding = Parent.Padding;
				int num = (aligning ? _alignmentPadding.Left : 0);
				int num2 = (aligning ? _alignmentPadding.Top : 0);
				int num3 = (aligning ? _alignmentPadding.Right : 0);
				int num4 = (aligning ? _alignmentPadding.Bottom : 0);
				int width2 = parent.Width;
				int height2 = parent.Height;
				int num5 = margin.Left + padding.Left + num;
				int num6 = width2 - margin.Right - width - padding.Right - num3;
				x = ((num5 <= num6) ? Math.Clamp(x, num5, num6) : ((int)((float)(num5 + num6) / 2f)));
				int num7 = margin.Top + padding.Top + num2;
				int num8 = height2 - margin.Bottom - height - padding.Bottom - num4;
				y = ((num7 <= num8) ? Math.Clamp(y, num7, num8) : ((int)((float)(num7 + num8) / 2f)));
				goto IL_0150;
			}
		}
		x = Math.Max(x, margin.Left);
		y = Math.Max(y, margin.Top);
		goto IL_0150;
		IL_0150:
		SetBounds(x, y, width, height);
	}

	public virtual void SetPosition(float x, float y)
	{
		SetPosition((int)x, (int)y);
	}

	public virtual void SetPosition(int x, int y)
	{
		SetBounds(x, y, Width, Height);
	}

	public virtual void SetPosition(Point point)
	{
		SetBounds(point.X, point.Y, Width, Height);
	}

	public virtual bool SetSize(int width, int height)
	{
		return SetBounds(X, Y, width, height);
	}

	public bool SetSize(Point point)
	{
		return SetSize(point.X, point.Y);
	}

	public virtual bool SetBounds(Rectangle bounds)
	{
		return SetBounds(bounds.X, bounds.Y, bounds.Width, bounds.Height);
	}

	public virtual bool SetBounds(Point position, Point size)
	{
		return SetBounds(position.X, position.Y, size.X, size.Y);
	}

	public virtual bool SetBounds(float x, float y, float width, float height)
	{
		return SetBounds((int)x, (int)y, (int)width, (int)height);
	}

	protected virtual void OnMaximumSizeChanged(Point oldSize, Point newSize)
	{
	}

	protected virtual void OnMinimumSizeChanged(Point oldSize, Point newSize)
	{
	}

	protected virtual void OnSizeChanged(Point oldSize, Point newSize)
	{
		Parent?.OnChildSizeChanged(this, oldSize, newSize);
	}

	public virtual bool SetBounds(int x, int y, int width, int height)
	{
		width = Math.Max(0, width);
		height = Math.Max(0, height);
		if (_bounds.X == x && _bounds.Y == y && _bounds.Width == width && _bounds.Height == height)
		{
			return false;
		}
		Rectangle bounds = Bounds;
		Rectangle bounds2 = _bounds;
		bounds2.X = x;
		bounds2.Y = y;
		Rectangle rectangle = bounds2;
		Point maximumSize = MaximumSize;
		Point minimumSize = MinimumSize;
		rectangle.Width = Math.Max((maximumSize.X > 0) ? Math.Min(maximumSize.X, width) : width, minimumSize.X);
		rectangle.Height = Math.Max((maximumSize.Y > 0) ? Math.Min(maximumSize.Y, height) : height, minimumSize.Y);
		if (rectangle.Width > 100000 || rectangle.Height > 100000)
		{
			ApplicationContext.CurrentContext.Logger.LogWarning("Extremely large component resize '{ComponentName}' {OldBounds} => {NewBounds}", ParentQualifiedName, bounds.Size, rectangle.Size);
		}
		_bounds = rectangle;
		Margin margin = _margin;
		Rectangle outerBounds = new Rectangle(rectangle);
		outerBounds.X -= margin.Left;
		outerBounds.Y -= margin.Top;
		outerBounds.Width += margin.Left + margin.Right;
		outerBounds.Height += margin.Top + margin.Bottom;
		_outerBounds = outerBounds;
		OnBoundsChanged(bounds, rectangle);
		if (bounds.Position != rectangle.Position)
		{
			OnPositionChanged(bounds.Position, rectangle.Position);
			this.PositionChanged?.Invoke(this, new ValueChangedEventArgs<Point>
			{
				Value = rectangle.Position,
				OldValue = bounds.Position
			});
		}
		if (bounds.Size != rectangle.Size)
		{
			if (_cacheToTexture)
			{
				Skin.Renderer.Ctt.DisposeCachedTexture(this);
				Redraw();
			}
			InvalidateDock();
			OnSizeChanged(bounds.Size, rectangle.Size);
			this.SizeChanged?.Invoke(this, new ValueChangedEventArgs<Point>
			{
				Value = rectangle.Size,
				OldValue = bounds.Size
			});
			ProcessAlignments();
		}
		if (_dock != Pos.None)
		{
			InvalidateDock();
			InvalidateParentDock();
		}
		this.BoundsChanged?.Invoke(this, new ValueChangedEventArgs<Rectangle>
		{
			Value = rectangle,
			OldValue = bounds
		});
		return true;
	}

	public void InvalidateDock()
	{
		if (!_dockDirty)
		{
			_dockDirty = true;
		}
		Invalidate();
	}

	protected void InvalidateParentDock()
	{
		_parent?.InvalidateDock();
	}

	public virtual void Position(Pos pos, int xPadding = 0, int yPadding = 0)
	{
		int width = Parent.Width;
		int height = Parent.Height;
		Padding padding = Parent.Padding;
		int x = X;
		int y = Y;
		if ((pos & Pos.Left) != Pos.None)
		{
			x = padding.Left + xPadding;
		}
		else if ((pos & Pos.Right) != Pos.None)
		{
			x = width - Width - padding.Right - xPadding;
		}
		else if ((pos & Pos.CenterH) != Pos.None)
		{
			x = (int)((float)(padding.Left + xPadding) + (float)(width - Width - padding.Left - padding.Right) * 0.5f);
		}
		if ((pos & Pos.Top) != Pos.None)
		{
			y = padding.Top + yPadding;
		}
		else if ((pos & Pos.Bottom) != Pos.None)
		{
			y = height - Height - padding.Bottom - yPadding;
		}
		else if ((pos & Pos.CenterV) != Pos.None)
		{
			y = (int)((float)(padding.Top + yPadding) + (float)(height - Height - padding.Bottom - padding.Top) * 0.5f);
		}
		SetPosition(x, y);
	}

	protected virtual void OnPositionChanged(Point oldPosition, Point newPosition)
	{
	}

	protected virtual void OnBoundsChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		Parent?.OnChildBoundsChanged(this, oldBounds, newBounds);
		if (_bounds.Width != oldBounds.Width || _bounds.Height != oldBounds.Height)
		{
			Invalidate();
		}
		Redraw();
		UpdateRenderBounds();
	}

	protected virtual void OnScaleChanged()
	{
		for (int i = 0; i < _children.Count; i++)
		{
			_children[i].OnScaleChanged();
		}
	}

	protected virtual void OnChildBoundsChanged(Base child, Rectangle oldChildBounds, Rectangle newChildBounds)
	{
		if ((child.Dock & ~Pos.Fill) != Pos.None)
		{
			InvalidateDock();
		}
	}

	protected virtual void OnChildSizeChanged(Base child, Point oldChildSize, Point newChildSize)
	{
	}

	protected virtual void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
	}

	protected virtual void DoCacheRender(Intersect.Client.Framework.Gwen.Skin.Base skin, Base master)
	{
		Intersect.Client.Framework.Gwen.Renderer.Base obj = skin?.Renderer;
		ICacheToTexture cacheToTexture = obj?.Ctt;
		if (cacheToTexture == null)
		{
			return;
		}
		Point renderOffset = obj.RenderOffset;
		Rectangle clipRegion = obj.ClipRegion;
		if (this != master)
		{
			obj.AddRenderOffset(Bounds);
			obj.AddClipRegion(Bounds);
		}
		else
		{
			obj.RenderOffset = Point.Empty;
			obj.ClipRegion = new Rectangle(0, 0, Width, Height);
		}
		if (_cacheTextureDirty && obj.ClipRegionVisible)
		{
			if (ClipContents)
			{
				obj.StartClip();
			}
			else
			{
				obj.EndClip();
			}
			if (ShouldCacheToTexture)
			{
				cacheToTexture.SetupCacheTexture(this);
			}
			Render(skin);
			IReadOnlyList<Base> readOnlyList = EnfantsAuDessin();
			for (int i = 0; i < readOnlyList.Count; i++)
			{
				readOnlyList[i].DoCacheRender(skin, master);
			}
			if (ShouldCacheToTexture)
			{
				cacheToTexture.FinishCacheTexture(this);
				_cacheTextureDirty = false;
			}
		}
		obj.ClipRegion = clipRegion;
		obj.StartClip();
		obj.RenderOffset = renderOffset;
		if (ShouldCacheToTexture)
		{
			cacheToTexture.DrawCachedControlTexture(this);
		}
	}

	private static bool IsNodeVisible(Base node)
	{
		return node.IsVisibleInTree;
	}

	private IReadOnlyList<Base> EnfantsAuDessin()
	{
		List<Base> list = _enfantsAuDessin ?? (_enfantsAuDessin = new List<Base>(_children.Count));
		list.Clear();
		for (int i = 0; i < _children.Count; i++)
		{
			Base obj = _children[i];
			if (!obj._disposed && obj._visible && !obj.Escamote)
			{
				list.Add(obj);
			}
		}
		return OrderChildrenForRendering(list);
	}

	internal virtual void DoRender(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (_skin != null)
		{
			skin = _skin;
		}
		Think();
		UpdateColors();
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		if (renderer.Ctt != null && ShouldCacheToTexture)
		{
			DoCacheRender(skin, this);
			if (DrawDebugOutlines)
			{
				RenderDebugOutlinesRecursive(skin);
			}
		}
		else
		{
			RenderRecursive(skin, Bounds);
			if (DrawDebugOutlines)
			{
				skin.DrawDebugOutlines(this);
			}
		}
	}

	internal virtual void RenderDebugOutlinesRecursive(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		Point renderOffset = skin.Renderer.RenderOffset;
		skin.Renderer.AddRenderOffset(Bounds);
		foreach (Base child in _children)
		{
			if (!child.IsHidden)
			{
				child.RenderDebugOutlinesRecursive(skin);
			}
		}
		skin.Renderer.RenderOffset = renderOffset;
		skin.DrawDebugOutlines(this);
	}

	protected virtual IReadOnlyList<Base> OrderChildrenForRendering(IReadOnlyList<Base> children)
	{
		return children;
	}

	protected virtual void OnPostDraw(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
	}

	protected virtual void OnPreDraw(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
	}

	public void SkipRender()
	{
		_skipRender = true;
	}

	protected virtual void RenderRecursive(Intersect.Client.Framework.Gwen.Skin.Base skin, Rectangle clipRect)
	{
		if (_skipRender)
		{
			_skipRender = false;
			return;
		}
		OnPreDraw(skin);
		this.BeforeDraw?.Invoke(this, EventArgs.Empty);
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		Point renderOffset = renderer.RenderOffset;
		renderer.AddRenderOffset(clipRect);
		RenderUnder(skin);
		Rectangle clipRegion = renderer.ClipRegion;
		if (ShouldClip)
		{
			renderer.AddClipRegion(clipRect);
			if (!renderer.ClipRegionVisible)
			{
				renderer.RenderOffset = renderOffset;
				renderer.ClipRegion = clipRegion;
				return;
			}
			renderer.StartClip();
		}
		Render(skin);
		IReadOnlyList<Base> readOnlyList = EnfantsAuDessin();
		bool flag = _chronometrerLesEnfants && Chrono.Etape != null;
		for (int i = 0; i < readOnlyList.Count; i++)
		{
			readOnlyList[i].DoRender(skin);
			if (flag)
			{
				Chrono.Tour(readOnlyList[i].Name);
			}
		}
		renderer.ClipRegion = clipRegion;
		renderer.StartClip();
		RenderOver(skin);
		RenderFocus(skin);
		renderer.RenderOffset = renderOffset;
		OnPostDraw(skin);
		this.AfterDraw?.Invoke(this, EventArgs.Empty);
	}

	public virtual void SetSkin(Intersect.Client.Framework.Gwen.Skin.Base skin, bool doChildren = false)
	{
		if (_skin == skin)
		{
			return;
		}
		_skin = skin;
		Invalidate();
		Redraw();
		OnSkinChanged(skin);
		if (doChildren)
		{
			for (int i = 0; i < _children.Count; i++)
			{
				_children[i].SetSkin(skin, doChildren: true);
			}
		}
	}

	protected virtual void OnSkinChanged(Intersect.Client.Framework.Gwen.Skin.Base newSkin)
	{
	}

	protected virtual bool OnMouseWheeled(int delta)
	{
		if (_parent != null)
		{
			return _parent.OnMouseWheeled(delta);
		}
		return false;
	}

	internal bool InputMouseWheeled(int delta)
	{
		if (IsDisabledByTree)
		{
			return false;
		}
		return OnMouseWheeled(delta);
	}

	protected virtual bool OnMouseHWheeled(int delta)
	{
		if (_parent != null)
		{
			return _parent.OnMouseHWheeled(delta);
		}
		return false;
	}

	internal bool InputMouseHWheeled(int delta)
	{
		if (IsDisabledByTree)
		{
			return false;
		}
		return OnMouseHWheeled(delta);
	}

	protected virtual void OnMouseMoved(int x, int y, int dx, int dy)
	{
	}

	internal void InputMouseMoved(int x, int y, int dx, int dy)
	{
		if (!IsDisabledByTree)
		{
			OnMouseMoved(x, y, dx, dy);
		}
	}

	internal void InputNonUserMouseClicked(MouseButton mouseButton, Point mousePosition, bool isPressed)
	{
		if (!IsDisabledByTree)
		{
			OnMouseDoubleClicked(mouseButton, mousePosition, userAction: false);
			this.Clicked?.Invoke(this, new MouseButtonState(mouseButton, mousePosition, isPressed));
		}
	}

	internal void InputMouseDoubleClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		if (!IsDisabledByTree)
		{
			OnMouseDoubleClicked(mouseButton, mousePosition, userAction);
			this.DoubleClicked?.Invoke(this, new MouseButtonState(mouseButton, mousePosition, isPressed: true));
		}
	}

	protected bool PlaySound(string? name)
	{
		if (name == null || IsDisabled)
		{
			return false;
		}
		Canvas canvas = Canvas;
		if (canvas == null)
		{
			return false;
		}
		name = GameContentManager.RemoveExtension(name).ToLower();
		GameAudioSource sound = GameContentManager.Current.GetSound(name);
		if (sound == null)
		{
			SonsAbsents.Signaler(name);
			return false;
		}
		GameAudioInstance gameAudioInstance = sound.CreateInstance();
		if (gameAudioInstance == null)
		{
			return false;
		}
		canvas.PlayAndAddSound(gameAudioInstance);
		return true;
	}

	public bool IsMouseButtonActive(MouseButton mouseButton)
	{
		return _mouseButtonPressed.Contains(mouseButton);
	}

	protected virtual void OnMouseClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
	}

	protected virtual void OnMouseDoubleClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
	}

	protected virtual void OnMouseDown(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
	}

	protected virtual void OnMouseUp(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
	}

	protected void EmitVirtualClick(MouseButtonState args)
	{
		this.Clicked?.Invoke(this, args);
	}

	internal void InputMouseButtonState(MouseButton mouseButton, Point mousePosition, bool pressed, bool userAction = true)
	{
		bool flag = !IsDisabledByTree;
		MouseButtonState arguments = new MouseButtonState(mouseButton, mousePosition.X, mousePosition.Y, pressed);
		bool flag2 = IsMouseButtonActive(mouseButton);
		if (pressed)
		{
			_mouseButtonPressed.Add(mouseButton);
			InputHandler.MouseFocus = this;
			if (!flag2)
			{
				OnMouseDown(mouseButton, mousePosition, userAction);
				if (flag)
				{
					this.MouseDown?.Invoke(this, arguments);
				}
			}
		}
		else
		{
			if (IsHovered && flag2)
			{
				OnMouseClicked(mouseButton, mousePosition, userAction);
				if (flag)
				{
					this.Clicked?.Invoke(this, arguments);
				}
			}
			_mouseButtonPressed.Remove(mouseButton);
			if (_mouseButtonPressed.Count < 1)
			{
				InputHandler.MouseFocus = null;
			}
			if (flag2)
			{
				OnMouseUp(mouseButton, mousePosition, userAction);
				if (flag)
				{
					this.MouseUp?.Invoke(this, arguments);
				}
			}
		}
		if (flag2 != pressed)
		{
			Redraw();
		}
	}

	protected virtual void OnMouseEntered()
	{
		this.HoverEnter?.Invoke(this, EventArgs.Empty);
		Redraw();
	}

	internal void InputMouseEntered()
	{
		if (Tooltip != null)
		{
			ToolTip.Enable(this);
		}
		else
		{
			Base parent = Parent;
			if (parent != null && parent.Tooltip != null)
			{
				ToolTip.Enable(Parent);
			}
		}
		if (!IsDisabledByTree)
		{
			OnMouseEntered();
		}
	}

	protected virtual void OnMouseLeft()
	{
		IsActive = false;
		this.HoverLeave?.Invoke(this, EventArgs.Empty);
		Redraw();
	}

	internal void InputMouseLeft()
	{
		if (Tooltip != null)
		{
			ToolTip.Disable(this);
		}
		else
		{
			Base parent = Parent;
			if (parent != null && parent.Tooltip != null)
			{
				ToolTip.Disable(Parent);
			}
		}
		if (!IsDisabledByTree)
		{
			OnMouseLeft();
			IsActive = false;
		}
	}

	public virtual void Focus(bool moveMouse = false)
	{
		if (InputHandler.KeyboardFocus != this)
		{
			if (InputHandler.KeyboardFocus != null)
			{
				InputHandler.KeyboardFocus.OnLostKeyboardFocus();
			}
			if (moveMouse)
			{
				InputHandler.Focus(FocusSource.Keyboard, this);
			}
			else
			{
				InputHandler.KeyboardFocus = this;
			}
			OnKeyboardFocus();
			Redraw();
		}
	}

	public virtual void Blur()
	{
		if (InputHandler.KeyboardFocus == this)
		{
			InputHandler.KeyboardFocus = null;
			OnLostKeyboardFocus();
			Redraw();
		}
	}

	public virtual void Touch()
	{
		if (Parent != null)
		{
			Parent.OnChildTouched(this);
		}
	}

	protected virtual void OnChildTouched(Base control)
	{
		Touch();
	}

	public Base? GetComponentAt(int x, int y, NodeFilter filters = NodeFilter.None)
	{
		if (x < 0 || Width <= x || y < 0 || Height <= y)
		{
			return null;
		}
		if ((IsHidden || Escamote) && !filters.HasFlag(NodeFilter.IncludeHidden))
		{
			return null;
		}
		Base obj = RunOnMainThread(FindComponentAt, new ComponentAtParams(new Point(x, y), filters), invalidate: false);
		if (obj != null)
		{
			return obj;
		}
		if (this is Text)
		{
			if (!filters.HasFlag(NodeFilter.IncludeText))
			{
				return null;
			}
			return this;
		}
		if (MouseInputEnabled || filters.HasFlag(NodeFilter.IncludeMouseInputDisabled))
		{
			return this;
		}
		return null;
	}

	private static Base? FindComponentAt(Base @this, ComponentAtParams componentAtParams)
	{
		ComponentAtParams componentAtParams2 = componentAtParams;
		var (point2, nodeFilter2) = componentAtParams2;
		point2.Deconstruct(out var x, out var y);
		int num = x;
		int num2 = y;
		NodeFilter filters = nodeFilter2;
		for (int num3 = @this._children.Count - 1; num3 >= 0; num3--)
		{
			Base obj = @this._children[num3];
			Base componentAt = obj.GetComponentAt(num - obj.X, num2 - obj.Y, filters);
			if (componentAt != null)
			{
				return componentAt;
			}
		}
		return null;
	}

	public Base? GetComponentAt(Point point, NodeFilter filters = NodeFilter.None)
	{
		return GetComponentAt(point.X, point.Y, filters);
	}

	protected virtual void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		UpdateColors();
		if (skin?.Renderer.Ctt != null && ShouldCacheToTexture)
		{
			skin.Renderer.Ctt.CreateControlCacheTexture(this);
		}
	}

	protected virtual void DoPrelayout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
	}

	protected void DoLayoutIfNeeded(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (_layoutDirty)
		{
			_layoutDirty = false;
			Layout(skin);
		}
	}

	protected void InvokeThreadQueue()
	{
		if (_pendingThreadQueues >= 1)
		{
			if (_threadQueue.InvokePending())
			{
				UpdatePendingThreadQueues(-1);
			}
			int count = _children.Count;
			for (int i = 0; i < count; i++)
			{
				Base obj = _children[i];
				obj.InvokeThreadQueue();
			}
		}
	}

	protected void RecurseLayout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (_skin != null)
		{
			skin = _skin;
		}
		if (ShouldSkipLayout)
		{
			return;
		}
		foreach (Base child in _children)
		{
			child.DoPrelayout(skin);
			_preLayoutActionsParent.IsExecuting = true;
			PreLayout.InvokePending();
			_preLayoutActionsParent.IsExecuting = false;
			child.BeforeLayout?.Invoke(child, EventArgs.Empty);
		}
		Point point = new Point(Math.Max(Width, MinimumSize.X), Math.Max(Height, MinimumSize.Y));
		if (point != Size)
		{
			Size = point;
		}
		DoLayoutIfNeeded(skin);
		if (_needsAlignment)
		{
			_needsAlignment = false;
			ProcessAlignments();
		}
		if (_dockDirty)
		{
			_dockDirty = false;
			Padding dockChildSpacing = DockChildSpacing;
			Rectangle renderBounds = RenderBounds;
			renderBounds.X += _padding.Left;
			renderBounds.Width -= _padding.Left + _padding.Right;
			renderBounds.Y += _padding.Top;
			renderBounds.Height -= _padding.Top + _padding.Bottom;
			DisplayMode displayMode = _displayMode;
			if (displayMode switch
			{
				DisplayMode.Initial => false, 
				DisplayMode.FlowStartToEnd => true, 
				DisplayMode.FlowTopToBottom => true, 
				_ => throw Exceptions.UnreachableInvalidEnum(displayMode), 
			})
			{
				Point position = renderBounds.Position;
				Base[] array = _children.Where((Base child) => child.IsVisibleInParent).ToArray();
				Point requiredSizeForDockFillNodes = array.Aggregate(default(Point), delegate(Point sum, Base node)
				{
					switch (displayMode)
					{
					case DisplayMode.FlowStartToEnd:
						sum.X += node.MinimumSize.X;
						sum.Y = Math.Max(sum.Y, node.MinimumSize.Y);
						break;
					case DisplayMode.FlowTopToBottom:
						sum.X = Math.Max(sum.X, node.MinimumSize.X);
						sum.Y += node.MinimumSize.Y;
						break;
					default:
						throw Exceptions.UnreachableInvalidEnum(displayMode);
					}
					return sum;
				});
				int num = 0;
				int num2 = 0;
				switch (displayMode)
				{
				case DisplayMode.FlowStartToEnd:
					num = array.Length - 1;
					break;
				case DisplayMode.FlowTopToBottom:
					num2 = array.Length - 1;
					break;
				default:
					throw Exceptions.UnreachableInvalidEnum(displayMode);
				}
				requiredSizeForDockFillNodes.X += DockChildSpacing.Left * num;
				requiredSizeForDockFillNodes.Y += DockChildSpacing.Top * num2;
				Point size = renderBounds.Size;
				if (size.X < requiredSizeForDockFillNodes.X || size.Y < requiredSizeForDockFillNodes.Y)
				{
					_requiredSizeForDockFillNodes = requiredSizeForDockFillNodes;
				}
				Point point2 = array.Aggregate(default(Point), (Point sum, Base node) => sum + node.OuterBounds.Size);
				Point point3 = ((array.Length > 1) ? ((size - point2) / (array.Length - 1)) : default(Point));
				switch (displayMode)
				{
				case DisplayMode.FlowStartToEnd:
				{
					int num7 = size.X % array.Length;
					Base[] array3 = array;
					foreach (Base obj2 in array3)
					{
						int num9 = ((num7-- > 0) ? 1 : 0);
						int num10 = num9 + obj2.Width + point3.X;
						int x2 = position.X + num9;
						int y2 = position.Y + (renderBounds.Height - obj2.Height) / 2;
						obj2.SetPosition(x2, y2);
						position.X += num10;
					}
					break;
				}
				case DisplayMode.FlowTopToBottom:
				{
					int num3 = size.Y % array.Length;
					Base[] array2 = array;
					foreach (Base obj in array2)
					{
						int num5 = ((num3-- > 0) ? 1 : 0);
						int num6 = num5 + obj.Height + point3.Y;
						int x = position.X + (renderBounds.Width - obj.Width) / 2;
						int y = position.Y + num5;
						obj.SetPosition(x, y);
						position.X += num6;
					}
					break;
				}
				default:
					throw Exceptions.UnreachableInvalidEnum(displayMode);
				}
			}
			else
			{
				Base[] array4 = _children.Where((Base child) => !child.ShouldSkipLayout && !child.Dock.HasFlag(Pos.Fill)).ToArray();
				Base[] array5 = _children.Where((Base child) => !child.ShouldSkipLayout && child.Dock.HasFlag(Pos.Fill)).ToArray();
				Base[] array6 = array4;
				foreach (Base obj3 in array6)
				{
					Pos dock = obj3.Dock;
					Margin margin = obj3.Margin;
					int num12 = margin.Left + margin.Right;
					int num13 = margin.Top + margin.Bottom;
					int num14 = num12 + obj3.Width;
					int num15 = num13 + obj3.Height;
					int num16 = renderBounds.Width - num12;
					int num17 = renderBounds.Height - num13;
					bool flag = false;
					bool flag2 = false;
					if (!(obj3 is ISmartAutoSizeToContents smartAutoSizeToContents))
					{
						if (!(obj3 is IAutoSizeToContents { AutoSizeToContents: not false }))
						{
							if (obj3 is IFitHeightToContents { FitHeightToContents: not false })
							{
								flag2 = true;
							}
						}
						else
						{
							flag = true;
							flag2 = true;
						}
					}
					else
					{
						flag = smartAutoSizeToContents.AutoSizeToContentWidth;
						flag2 = smartAutoSizeToContents.AutoSizeToContentHeight;
					}
					if (dock.HasFlag(Pos.Left))
					{
						int num18 = (flag2 ? obj3.Height : num17);
						int num19 = renderBounds.Y + margin.Top;
						if (dock.HasFlag(Pos.CenterV))
						{
							num18 = obj3.Height;
							int num20 = Math.Max(0, num17 - num18) / 2;
							if (num20 != 0)
							{
								num19 += num20;
							}
						}
						else if (dock.HasFlag(Pos.Bottom))
						{
							num19 = renderBounds.Bottom - (margin.Bottom + obj3.Height);
						}
						else if (!dock.HasFlag(Pos.Top))
						{
							int num21 = Math.Max(0, num17 - num18) / 2;
							if (num21 != 0)
							{
								num19 += num21;
							}
						}
						obj3.SetBounds(renderBounds.X + margin.Left, num19, obj3.Width, num18);
						int num22 = num14 + dockChildSpacing.Left;
						renderBounds.X += num22;
						renderBounds.Width -= num22;
					}
					if (dock.HasFlag(Pos.Right))
					{
						int num23 = (flag2 ? obj3.Height : num17);
						int num24 = renderBounds.Y + margin.Top;
						if (dock.HasFlag(Pos.CenterV))
						{
							num23 = obj3.Height;
							int num25 = Math.Max(0, num17 - num23) / 2;
							if (num25 != 0)
							{
								num24 += num25;
							}
						}
						else if (dock.HasFlag(Pos.Bottom))
						{
							num24 = renderBounds.Bottom - (margin.Bottom + obj3.Height);
						}
						else if (!dock.HasFlag(Pos.Top))
						{
							int num26 = Math.Max(0, num17 - num23) / 2;
							if (num26 != 0)
							{
								num24 += num26;
							}
						}
						int num27 = obj3.Width + margin.Right;
						obj3.SetBounds(renderBounds.X + renderBounds.Width - num27, num24, obj3.Width, num23);
						int num28 = num14 + dockChildSpacing.Right;
						renderBounds.Width -= num28;
					}
					if (dock.HasFlag(Pos.Top) && !dock.HasFlag(Pos.Left) && !dock.HasFlag(Pos.Right))
					{
						int width = (flag ? obj3.Width : num16);
						int x3 = renderBounds.Left + margin.Left;
						if (dock.HasFlag(Pos.CenterH))
						{
							x3 = renderBounds.Left + (renderBounds.Width - obj3.OuterWidth) / 2;
							width = obj3.Width;
						}
						obj3.SetBounds(x3, renderBounds.Top + margin.Top, width, obj3.Height);
						int num29 = num15 + dockChildSpacing.Top;
						renderBounds.Y += num29;
						renderBounds.Height -= num29;
					}
					if (dock.HasFlag(Pos.Bottom) && !dock.HasFlag(Pos.Left) && !dock.HasFlag(Pos.Right))
					{
						int width2 = (flag ? obj3.Width : num16);
						int num30 = obj3.Height + margin.Bottom;
						int x4 = renderBounds.Left + margin.Left;
						if (dock.HasFlag(Pos.CenterH))
						{
							x4 = renderBounds.Left + (renderBounds.Width - obj3.OuterWidth) / 2;
							width2 = obj3.Width;
						}
						obj3.SetBounds(x4, renderBounds.Bottom - num30, width2, obj3.Height);
						renderBounds.Height -= num15 + dockChildSpacing.Bottom;
					}
					obj3.RecurseLayout(skin);
				}
				Rectangle rectangle = renderBounds;
				_innerBounds = renderBounds;
				Point point4 = default(Point);
				Point point5 = array5.Aggregate(default(Point), (Point point6, Base node) => new Point(Math.Max(point6.X, node.Width), Math.Max(point6.Y, node.Height)));
				int num31;
				int num32;
				if (array5.Length < 2)
				{
					num31 = renderBounds.Width;
					num32 = renderBounds.Height;
				}
				else if (point5.Y > point5.X)
				{
					if (point5.Y > renderBounds.Height)
					{
						num31 = Math.Max(point5.X, renderBounds.Width);
						num32 = renderBounds.Height / array5.Length;
					}
					else
					{
						num31 = renderBounds.Width / array5.Length;
						num32 = Math.Max(point5.Y, renderBounds.Height);
					}
				}
				else if (point5.X > renderBounds.Width)
				{
					num31 = renderBounds.Width / array5.Length;
					num32 = Math.Max(point5.Y, renderBounds.Height);
				}
				else
				{
					num31 = Math.Max(point5.X, renderBounds.Width);
					num32 = renderBounds.Height / array5.Length;
				}
				bool flag3 = num32 == renderBounds.Height;
				Base[] array7 = array5;
				foreach (Base obj4 in array7)
				{
					Pos dock2 = obj4.Dock;
					Margin margin2 = obj4.Margin;
					int num34 = margin2.Left + margin2.Right;
					int num35 = margin2.Top + margin2.Bottom;
					Point position2 = new Point(renderBounds.X + margin2.Left, renderBounds.Y + margin2.Top);
					Point size2 = new Point(num31 - num34, num32 - num35);
					Point minimumSize = obj4.MinimumSize;
					int num36 = Math.Max(0, minimumSize.X - size2.X);
					int num37 = Math.Max(0, minimumSize.Y - size2.Y);
					bool flag4 = false;
					if (num36 > 0 || num37 > 0)
					{
						flag4 = true;
						if (point4 == default(Point))
						{
							point4 = Size;
						}
						point4.X += num36;
						point4.Y += num37;
					}
					else if (renderBounds.Width < 1 || renderBounds.Height < 1)
					{
						if (point4 == default(Point))
						{
							point4 = Size;
						}
						point4.X += Math.Max(10, rectangle.Width / array5.Length);
						point4.Y += Math.Max(10, rectangle.Height / array5.Length);
					}
					size2.X = Math.Max(minimumSize.X, size2.X);
					size2.Y = Math.Max(minimumSize.Y, size2.Y);
					if (obj4 is IAutoSizeToContents { AutoSizeToContents: not false })
					{
						if (Pos.Right == (dock2 & (Pos.Left | Pos.Right)))
						{
							int num38 = obj4.Width + margin2.Right + dockChildSpacing.Right;
							position2.X = renderBounds.Right - num38;
						}
						if (Pos.Bottom == (dock2 & (Pos.Top | Pos.Bottom)))
						{
							int num39 = obj4.Height + margin2.Bottom + dockChildSpacing.Bottom;
							position2.Y = renderBounds.Bottom - num39;
						}
						if (dock2.HasFlag(Pos.CenterH))
						{
							position2.X = renderBounds.X + (renderBounds.Width - (num34 + obj4.Width)) / 2;
						}
						if (dock2.HasFlag(Pos.CenterV))
						{
							position2.Y = renderBounds.Y + (renderBounds.Height - (num35 + obj4.Height)) / 2;
						}
						obj4.SetPosition(position2);
					}
					else
					{
						ApplyDockFill(obj4, position2, size2);
						Rectangle outerBounds = obj4.OuterBounds;
						if (flag3)
						{
							int num40 = outerBounds.Right - renderBounds.X;
							renderBounds.X = outerBounds.Right;
							renderBounds.Width -= num40;
						}
						else
						{
							int num41 = outerBounds.Bottom - renderBounds.Y;
							renderBounds.Y = outerBounds.Bottom;
							renderBounds.Height -= num41;
						}
					}
					if (flag4)
					{
						renderBounds.X += renderBounds.Width;
						renderBounds.Width = 0;
						renderBounds.Y += renderBounds.Height;
						renderBounds.Height = 0;
					}
					obj4.RecurseLayout(skin);
				}
			}
		}
		else
		{
			foreach (Base child2 in _children)
			{
				if (!child2.ShouldSkipLayout)
				{
					child2.RecurseLayout(skin);
				}
			}
		}
		DoPostlayout(skin);
		_postLayoutActionsParent.IsExecuting = true;
		PostLayout.InvokePending();
		_postLayoutActionsParent.IsExecuting = false;
		this.AfterLayout?.Invoke(this, EventArgs.Empty);
		Canvas canvas = _canvas;
		if (canvas != null)
		{
			if (IsTabable && !canvas._tabQueue.Contains(this))
			{
				canvas._tabQueue.AddLast(this);
			}
			if (InputHandler.KeyboardFocus == this && !canvas._tabQueue.Contains(this))
			{
				canvas._tabQueue.AddLast(this);
			}
		}
	}

	protected virtual void ApplyDockFill(Base child, Point position, Point size)
	{
		child._dockFillSize = size;
		child.SetBounds(position, size);
	}

	public bool IsChild(Base child)
	{
		return _children.Contains(child);
	}

	public Point ToCanvas(int x, int y)
	{
		Base host = _host;
		if (host == null)
		{
			return new Point(x, y);
		}
		x += X;
		y += Y;
		Base innerPanel = host._innerPanel;
		if (innerPanel != null && innerPanel.IsChild(this))
		{
			x += innerPanel.X;
			y += innerPanel.Y;
		}
		return _host.ToCanvas(x, y);
	}

	public Point ToCanvas(Point point)
	{
		return ToCanvas(point.X, point.Y);
	}

	public virtual Point CanvasPosToLocal(Point globalCoordinates)
	{
		return ToLocal(globalCoordinates.X, globalCoordinates.Y);
	}

	public virtual bool HasChild(Base component)
	{
		return IsChild(component);
	}

	public Point ToGlobal(Point point)
	{
		return ToGlobal(point.X, point.Y);
	}

	public virtual Point ToGlobal(int x, int y)
	{
		Base host = _host;
		if (host == null)
		{
			return new Point(x, y);
		}
		x += X;
		y += Y;
		Base innerPanel = host._innerPanel;
		if (innerPanel == null || !innerPanel.HasChild(this))
		{
			return host.ToGlobal(x, y);
		}
		return innerPanel.ToGlobal(x, y);
	}

	public Point ToLocal(Point point)
	{
		return ToLocal(point.X, point.Y);
	}

	public virtual Point ToLocal(int x, int y)
	{
		Base host = _host;
		if (host == null)
		{
			return new Point(x, y);
		}
		x -= X;
		y -= Y;
		Base innerPanel = host._innerPanel;
		if (innerPanel == null || !innerPanel.HasChild(this))
		{
			return host.ToLocal(x, y);
		}
		return innerPanel.ToLocal(x, y);
	}

	public virtual void CloseMenus()
	{
		List<Base> list = _children.FindAll((Base x) => true);
		foreach (Base item in list)
		{
			item.CloseMenus();
		}
	}

	protected virtual void UpdateRenderBounds()
	{
		_renderBounds.X = 0;
		_renderBounds.Y = 0;
		_renderBounds.Width = _bounds.Width;
		_renderBounds.Height = _bounds.Height;
	}

	public virtual void UpdateCursor()
	{
		Neutral.SetCursor(_cursor);
	}

	public virtual Package? DragAndDrop_GetPackage(int x, int y)
	{
		return _dragPayload;
	}

	public virtual bool DragAndDrop_Draggable()
	{
		if (_dragPayload == null)
		{
			return false;
		}
		return _dragPayload.IsDraggable;
	}

	public virtual void DragAndDrop_SetPackage(bool draggable, string name = "", object userData = null)
	{
		if (_dragPayload == null)
		{
			_dragPayload = new Package();
			_dragPayload.IsDraggable = draggable;
			_dragPayload.Name = name;
			_dragPayload.UserData = userData;
		}
	}

	public virtual bool DragAndDrop_ShouldStartDrag()
	{
		return true;
	}

	public virtual void DragAndDrop_StartDragging(Package package, int x, int y)
	{
		package.HoldOffset = CanvasPosToLocal(new Point(x, y));
		package.DrawControl = this;
	}

	public virtual void DragAndDrop_EndDragging(bool success, int x, int y)
	{
	}

	public virtual bool DragAndDrop_HandleDrop(Package p, int x, int y)
	{
		DragAndDrop.SourceControl.Parent = this;
		return true;
	}

	public virtual void DragAndDrop_HoverEnter(Package p, int x, int y)
	{
	}

	public virtual void DragAndDrop_HoverLeave(Package p)
	{
	}

	public virtual void DragAndDrop_Hover(Package p, int x, int y)
	{
	}

	public virtual bool DragAndDrop_CanAcceptPackage(Package p)
	{
		return false;
	}

	private static void SizeChildrenToChildren(Base @this, SizeToChildrenArgs args)
	{
		foreach (Base child in @this._children)
		{
			if (child._visible)
			{
				child.SizeToChildren(args);
			}
		}
	}

	public bool SizeToChildren(bool resizeX = true, bool resizeY = true, bool recursive = false)
	{
		return SizeToChildren(new SizeToChildrenArgs(resizeX, resizeY, recursive));
	}

	public virtual bool SizeToChildren(SizeToChildrenArgs args)
	{
		if (!args.X && !args.Y)
		{
			return false;
		}
		if (args.Recurse)
		{
			RunOnMainThread(SizeChildrenToChildren, this, args);
		}
		Point childrenSize = GetChildrenSize();
		childrenSize.X = Math.Max(childrenSize.X, args.MinimumSize.X);
		childrenSize.Y = Math.Max(childrenSize.Y, args.MinimumSize.Y);
		Padding padding = Padding;
		Point internalSize = childrenSize;
		int num = padding.Right + padding.Left;
		int num2 = padding.Bottom + padding.Top;
		internalSize.X = ((internalSize.X < 0) ? Width : (internalSize.X + num));
		internalSize.Y = ((internalSize.Y < 0) ? Height : (internalSize.Y + num2));
		if (_requiredSizeForDockFillNodes != default(Point))
		{
			internalSize.X = Math.Max(_requiredSizeForDockFillNodes.X, internalSize.X);
			internalSize.Y = Math.Max(_requiredSizeForDockFillNodes.Y, internalSize.Y);
			_requiredSizeForDockFillNodes = default(Point);
		}
		internalSize.X = Math.Max(Math.Min(internalSize.X, (_maximumSize.X < 1) ? internalSize.X : _maximumSize.X), _minimumSize.X);
		internalSize.Y = Math.Max(Math.Min(internalSize.Y, (_maximumSize.Y < 1) ? internalSize.Y : _maximumSize.Y), _minimumSize.Y);
		Point size = new Point(args.X ? internalSize.X : Width, args.Y ? internalSize.Y : Height);
		if (Dock.HasFlag(Pos.Fill))
		{
			Point point = ApplyDockFillOnSizeToChildren(size, internalSize);
			size = point;
		}
		if (!SetSize(size))
		{
			return false;
		}
		ProcessAlignments();
		return true;
	}

	protected virtual Point ApplyDockFillOnSizeToChildren(Point size, Point internalSize)
	{
		return new Point(Math.Max(Width, size.X), Math.Max(Height, size.Y));
	}

	public virtual Point GetChildrenSize()
	{
		return RunOnMainThread((Func<Base, Point>)GetChildrenSize);
	}

	private static Point GetChildrenSize(Base @this)
	{
		Point point = new Point(int.MaxValue, int.MaxValue);
		Point point2 = default(Point);
		foreach (Base child in @this._children)
		{
			if (child._visible)
			{
				Rectangle outerBounds = child.OuterBounds;
				point.X = Math.Min(point.X, outerBounds.Left);
				point.Y = Math.Min(point.Y, outerBounds.Top);
				point2.X = Math.Max(point2.X, outerBounds.Right);
				point2.Y = Math.Max(point2.Y, outerBounds.Bottom);
			}
		}
		return point2 - point;
	}

	public virtual Point MeasureContent()
	{
		Point childrenSize = GetChildrenSize();
		childrenSize.X += Padding.Left + Padding.Right;
		childrenSize.Y += Padding.Top + Padding.Bottom;
		Base innerPanel = _innerPanel;
		if (innerPanel != null)
		{
			childrenSize.X += innerPanel.Padding.Left + innerPanel.Padding.Right;
			childrenSize.Y += innerPanel.Padding.Top + innerPanel.Padding.Bottom;
		}
		return childrenSize;
	}

	internal virtual bool HandleAccelerator(string accelerator)
	{
		if ((InputHandler.KeyboardFocus == this || !AccelOnlyFocus) && _accelerators.ContainsKey(accelerator))
		{
			_accelerators[accelerator](this, EventArgs.Empty);
			return true;
		}
		return _children.Any((Base child) => child.HandleAccelerator(accelerator));
	}

	public void AddAccelerator(string? accelerator, GwenEventHandler<EventArgs> handler)
	{
		accelerator = accelerator?.Trim().ToUpperInvariant();
		if (!string.IsNullOrWhiteSpace(accelerator))
		{
			_accelerators[accelerator] = handler;
		}
	}

	public void AddAccelerator(string accelerator)
	{
		_accelerators[accelerator] = DefaultAcceleratorHandler;
	}

	protected virtual void DoPostlayout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
	}

	public virtual void Redraw()
	{
		_cacheTextureDirty = true;
		_host?.Redraw();
	}

	public virtual void UpdateColors()
	{
	}

	public void InvalidateParent()
	{
		_host?.Invalidate(alsoInvalidateParent: true);
	}

	protected virtual bool OnKeyPressed(Key key, bool down = true)
	{
		bool flag = false;
		switch (key)
		{
		case Key.Tab:
			flag = OnKeyTab(down);
			break;
		case Key.Space:
			flag = OnKeySpace(down);
			break;
		case Key.Home:
			flag = OnKeyHome(down);
			break;
		case Key.End:
			flag = OnKeyEnd(down);
			break;
		case Key.Return:
			flag = OnKeyReturn(down);
			break;
		case Key.Backspace:
			flag = OnKeyBackspace(down);
			break;
		case Key.Delete:
			flag = OnKeyDelete(down);
			break;
		case Key.Right:
			flag = OnKeyRight(down);
			break;
		case Key.Left:
			flag = OnKeyLeft(down);
			break;
		case Key.Up:
			flag = OnKeyUp(down);
			break;
		case Key.Down:
			flag = OnKeyDown(down);
			break;
		case Key.Escape:
			flag = OnKeyEscape(down);
			break;
		}
		if (flag)
		{
			return true;
		}
		Parent?.OnKeyPressed(key, down);
		return false;
	}

	internal bool InputKeyPressed(Key key, bool down = true)
	{
		if (IsDisabledByTree)
		{
			return false;
		}
		return OnKeyPressed(key, down);
	}

	protected virtual bool OnKeyReleaseed(Key key)
	{
		if (IsDisabledByTree)
		{
			return false;
		}
		return OnKeyPressed(key, down: false);
	}

	protected virtual bool OnKeyTab(bool down, bool shift = false)
	{
		if (!down)
		{
			return true;
		}
		Canvas canvas = _canvas;
		if (canvas == null)
		{
			return true;
		}
		Base obj2;
		lock (canvas._tabQueue)
		{
			if (!canvas._tabQueue.Any(IsNodeValidTabTarget))
			{
				return true;
			}
			Base keyboardFocus = InputHandler.KeyboardFocus;
			Base obj = (shift ? canvas._tabQueue.First : canvas._tabQueue.Last)?.Value;
			if (keyboardFocus != null && canvas._tabQueue.Contains(keyboardFocus) && keyboardFocus != obj)
			{
				while (canvas._tabQueue.Last?.Value != keyboardFocus)
				{
					obj2 = canvas._tabQueue.First?.Value;
					if (obj2 == null)
					{
						break;
					}
					canvas._tabQueue.RemoveFirst();
					canvas._tabQueue.AddLast(obj2);
				}
			}
			do
			{
				obj2 = (shift ? canvas._tabQueue.Last : canvas._tabQueue.First)?.Value;
				if (obj2 == null)
				{
					return true;
				}
				if (shift)
				{
					canvas._tabQueue.RemoveLast();
					canvas._tabQueue.AddFirst(obj2);
				}
				else
				{
					canvas._tabQueue.RemoveFirst();
					canvas._tabQueue.AddLast(obj2);
				}
			}
			while (!IsNodeValidTabTarget(obj2));
		}
		if (IsNodeValidTabTarget(obj2))
		{
			Console.WriteLine($"Focusing {obj2.ParentQualifiedName} ({obj2.GetFullishName()})");
			obj2.Focus(!(obj2 is TextBox));
		}
		Redraw();
		return true;
	}

	private static bool IsNodeValidTabTarget(Base? node)
	{
		if (node != null && !node.IsDisabledByTree && !node.IsHiddenByTree)
		{
			return node.IsTabable;
		}
		return false;
	}

	protected virtual bool OnKeySpace(bool down)
	{
		return false;
	}

	protected virtual bool OnKeyReturn(bool down)
	{
		return false;
	}

	protected virtual bool OnKeyBackspace(bool down)
	{
		return false;
	}

	protected virtual bool OnKeyDelete(bool down)
	{
		return false;
	}

	protected virtual bool OnKeyRight(bool down)
	{
		return false;
	}

	protected virtual bool OnKeyLeft(bool down)
	{
		return false;
	}

	protected virtual bool OnKeyHome(bool down)
	{
		return false;
	}

	protected virtual bool OnKeyEnd(bool down)
	{
		return false;
	}

	protected virtual bool OnKeyUp(bool down)
	{
		return false;
	}

	protected virtual bool OnKeyDown(bool down)
	{
		return false;
	}

	protected virtual bool OnKeyEscape(bool down)
	{
		return false;
	}

	protected virtual void OnPaste(Base from, EventArgs args)
	{
	}

	protected virtual void OnCopy(Base from, EventArgs args)
	{
	}

	protected virtual void OnCut(Base from, EventArgs args)
	{
	}

	protected virtual void OnSelectAll(Base from, EventArgs args)
	{
	}

	internal void InputCopy(Base from)
	{
		OnCopy(from, EventArgs.Empty);
	}

	internal void InputPaste(Base from)
	{
		if (!IsDisabledByTree)
		{
			OnPaste(from, EventArgs.Empty);
		}
	}

	internal void InputCut(Base from)
	{
		if (IsDisabledByTree)
		{
			OnCopy(from, EventArgs.Empty);
		}
		else
		{
			OnCut(from, EventArgs.Empty);
		}
	}

	internal void InputSelectAll(Base from)
	{
		OnSelectAll(from, EventArgs.Empty);
	}

	protected virtual void RenderFocus(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (InputHandler.KeyboardFocus == this && IsTabable)
		{
			skin.DrawKeyboardHighlight(this, RenderBounds, 3);
		}
	}

	protected virtual void RenderUnder(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
	}

	protected virtual void RenderOver(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
	}

	public virtual void Think()
	{
	}

	protected virtual void OnKeyboardFocus()
	{
	}

	protected virtual void OnLostKeyboardFocus()
	{
	}

	protected virtual bool OnChar(char chr)
	{
		return false;
	}

	internal bool InputChar(char chr)
	{
		if (IsDisabledByTree)
		{
			return false;
		}
		return OnChar(chr);
	}

	public virtual void Anim_WidthIn(float length, float delay = 0f, float ease = 1f)
	{
		Animation.Add(this, new Width(0, Width, length, hide: false, delay, ease));
		Width = 0;
	}

	public virtual void Anim_HeightIn(float length, float delay, float ease)
	{
		Animation.Add(this, new Height(0, Height, length, hide: false, delay, ease));
		Height = 0;
	}

	public virtual void Anim_WidthOut(float length, bool hide, float delay, float ease)
	{
		Animation.Add(this, new Width(Width, 0, length, hide, delay, ease));
	}

	public virtual void Anim_HeightOut(float length, bool hide, float delay, float ease)
	{
		Animation.Add(this, new Height(Height, 0, length, hide, delay, ease));
	}

	public void FitChildrenToSize()
	{
		foreach (Base child in Children)
		{
			child.X = Math.Min(Bounds.Width, child.X + child.Width) - child.Width;
			child.Y = Math.Min(Bounds.Height, child.Y + child.Height) - child.Height;
			child.X = Math.Max(0, child.X);
			child.Y = Math.Max(0, child.Y);
		}
	}

	protected bool SetIfChanged<T>(ref T field, T value)
	{
		T oldValue;
		return SetIfChanged(ref field, value, out oldValue);
	}

	protected bool SetIfChanged(ref string? field, string? value)
	{
		string oldValue;
		return SetIfChanged(ref field, value, out oldValue);
	}

	protected bool SetIfChanged(ref string? field, string? value, StringComparison stringComparison)
	{
		string oldValue;
		return SetIfChanged(ref field, value, stringComparison, out oldValue);
	}

	protected bool SetIfChanged<T>(ref T field, T value, out T oldValue)
	{
		oldValue = field;
		if (EqualityComparer<T>.Default.Equals(field, value))
		{
			return false;
		}
		field = value;
		return true;
	}

	protected bool SetIfChanged(ref string? field, string? value, out string? oldValue)
	{
		return SetIfChanged(ref field, value, StringComparison.Ordinal, out oldValue);
	}

	protected bool SetIfChanged(ref string? field, string? value, StringComparison stringComparison, out string? oldValue)
	{
		oldValue = field;
		if (string.Equals(field, value, stringComparison))
		{
			return false;
		}
		field = value;
		return true;
	}

	protected bool SetAndDoIfChanged<TValue>(ref TValue field, TValue value, Action<Base> action)
	{
		ArgumentNullException.ThrowIfNull(action, "action");
		if (!SetIfChanged(ref field, value))
		{
			return false;
		}
		action(this);
		return true;
	}

	protected static bool SetAndDoIfChanged<TNode, TValue>(ref TValue field, TValue value, Action<TNode> action, TNode @this) where TNode : Base
	{
		ArgumentNullException.ThrowIfNull(action, "action");
		if (!@this.SetIfChanged(ref field, value))
		{
			return false;
		}
		action(@this);
		return true;
	}

	protected static bool SetAndDoIfChanged<TNode, TValue>(ref TValue field, TValue value, Action<Base?, TNode> action, Base? parent, TNode @this) where TNode : Base
	{
		ArgumentNullException.ThrowIfNull(action, "action");
		if (!@this.SetIfChanged(ref field, value))
		{
			return false;
		}
		action(parent, @this);
		return true;
	}

	protected static bool SetAndDoIfChanged<TNode, TValue>(ref TValue field, TValue value, Action<TNode, TValue> action, TNode @this) where TNode : Base
	{
		ArgumentNullException.ThrowIfNull(action, "action");
		if (!@this.SetIfChanged(ref field, value))
		{
			return false;
		}
		action(@this, value);
		return true;
	}

	protected bool SetAndDoIfChanged<TValue>(ref TValue field, TValue value, Action<Base, TValue> action)
	{
		ArgumentNullException.ThrowIfNull(action, "action");
		if (!SetIfChanged(ref field, value))
		{
			return false;
		}
		action(this, value);
		return true;
	}

	protected bool SetAndDoIfChanged(ref string? field, string? value, Action action)
	{
		return SetAndDoIfChanged(ref field, value, StringComparison.Ordinal, action);
	}

	protected bool SetAndDoIfChanged(ref string? field, string? value, StringComparison stringComparison, Action action)
	{
		ArgumentNullException.ThrowIfNull(action, "action");
		if (!SetIfChanged(ref field, value, stringComparison))
		{
			return false;
		}
		action();
		return true;
	}

	protected bool SetAndDoIfChanged<T>(ref T field, T value, ValueChangeHandler<T> valueChangeHandler)
	{
		ArgumentNullException.ThrowIfNull(valueChangeHandler, "valueChangeHandler");
		if (!SetIfChanged(ref field, value, out var oldValue))
		{
			return false;
		}
		valueChangeHandler(oldValue, value);
		return true;
	}

	protected bool SetAndDoIfChanged(ref string? field, string value, ValueChangeHandler<string?> valueChangeHandler)
	{
		return SetAndDoIfChanged(ref field, value, StringComparison.Ordinal, valueChangeHandler);
	}

	protected bool SetAndDoIfChanged(ref string? field, string value, StringComparison stringComparison, ValueChangeHandler<string?> valueChangeHandler)
	{
		ArgumentNullException.ThrowIfNull(valueChangeHandler, "valueChangeHandler");
		if (!SetIfChanged(ref field, value, stringComparison, out string oldValue))
		{
			return false;
		}
		valueChangeHandler(oldValue, value);
		return true;
	}

	public bool AddDataProvider(IDataProvider dataProvider)
	{
		if (!_dataProviders.Add(dataProvider))
		{
			return false;
		}
		if (dataProvider is IUpdatableDataProvider updatableDataProvider)
		{
			ListenUpdatableDataProvider(updatableDataProvider, listen: true);
			if (IsVisibleInTree)
			{
				VisibleUpdatableDataProvider(updatableDataProvider, visible: true);
			}
		}
		return true;
	}

	public bool RemoveDataProvider(IDataProvider dataProvider)
	{
		if (!_dataProviders.Remove(dataProvider))
		{
			return false;
		}
		if (dataProvider is IUpdatableDataProvider updatableDataProvider)
		{
			ListenUpdatableDataProvider(updatableDataProvider, listen: false);
			if (IsVisibleInTree)
			{
				VisibleUpdatableDataProvider(updatableDataProvider, visible: false);
			}
		}
		return true;
	}

	private void ListenUpdatableDataProviders(IEnumerable<IUpdatableDataProvider> providers, bool listen)
	{
		foreach (IUpdatableDataProvider provider in providers)
		{
			ListenUpdatableDataProvider(provider, listen);
		}
	}

	private void ListenUpdatableDataProvider(IUpdatableDataProvider provider, bool listen)
	{
		DataProviderRefCount valueOrDefault = _updatableDataProviderRefCounts.GetValueOrDefault(provider, default(DataProviderRefCount));
		if (listen)
		{
			valueOrDefault.Listening++;
		}
		else
		{
			valueOrDefault.Listening--;
		}
		if (valueOrDefault.Listening > 0)
		{
			_updatableDataProviderRefCounts[provider] = valueOrDefault;
		}
		else
		{
			_updatableDataProviderRefCounts.Remove(provider);
		}
		Base root = Root;
		if (root != this)
		{
			root.ListenUpdatableDataProvider(provider, listen);
		}
	}

	private void VisibleUpdatableDataProviders(IEnumerable<IUpdatableDataProvider> providers, bool visible)
	{
		foreach (IUpdatableDataProvider provider in providers)
		{
			VisibleUpdatableDataProvider(provider, visible);
		}
	}

	private void VisibleUpdatableDataProvider(IUpdatableDataProvider updatableDataProvider, bool visible)
	{
		if (!_updatableDataProviderRefCounts.TryGetValue(updatableDataProvider, out var value))
		{
			throw new InvalidOperationException("Cannot mark a data provider as visible before listening to it");
		}
		if (visible)
		{
			value.Visible++;
		}
		else if (value.Visible > 0)
		{
			value.Visible--;
		}
		else
		{
			ApplicationContext.CurrentContext.Logger.LogError("Tried to decrease visible count below 0 of a data provider");
		}
		_updatableDataProviderRefCounts[updatableDataProvider] = value;
		Base root = Root;
		if (root != this)
		{
			root.VisibleUpdatableDataProvider(updatableDataProvider, visible);
		}
	}

	protected void UpdateDataProviders(TimeSpan elapsed, TimeSpan total)
	{
		foreach (var (updatableDataProvider2, dataProviderRefCount2) in _updatableDataProviderRefCounts)
		{
			if (dataProviderRefCount2.Visible >= 1)
			{
				try
				{
					updatableDataProvider2.TryUpdate(elapsed, total);
				}
				catch (Exception exception)
				{
					ApplicationContext.Context.Value?.Logger.LogWarning(exception, "An exception was improperly thrown from {DataProviderType}.TryUpdate(TimeSpan, TimeSpan)", updatableDataProvider2.GetType().GetName(qualified: true));
				}
			}
		}
	}

	private void UpdatePendingThreadQueues(int increment)
	{
		_pendingThreadQueues = Math.Max(0, _pendingThreadQueues + increment);
		Parent?.UpdatePendingThreadQueues(increment);
	}

	public void RunOnMainThread(Action action)
	{
		Invalidate();
		_threadQueue.RunOnMainThread(action);
	}

	public void RunOnMainThread(Action<Base> action)
	{
		RunOnMainThread(action, this);
	}

	public void RunOnMainThread<TState>(Action<TState> action, TState state)
	{
		Invalidate();
		_threadQueue.RunOnMainThread(action, state);
	}

	public TReturn RunOnMainThread<TReturn>(Func<Base, TReturn> func)
	{
		Invalidate();
		return _threadQueue.RunOnMainThread(func, this);
	}

	public void RunOnMainThread<TState>(Action<Base, TState> action, TState state)
	{
		Invalidate();
		_threadQueue.RunOnMainThread(action, this, state);
	}

	public TReturn RunOnMainThread<TState, TReturn>(Func<Base, TState, TReturn> func, TState state, bool invalidate = true)
	{
		if (invalidate)
		{
			Invalidate();
		}
		return _threadQueue.RunOnMainThread(func, this, state);
	}

	public void RunOnMainThread<TState0, TState1>(Action<TState0, TState1> action, TState0 state0, TState1 state1)
	{
		if (_disposed)
		{
			if (_disposeCompleted)
			{
				ObjectDisposedException.ThrowIf(condition: true, this);
			}
			ApplicationContext.CurrentContext.Logger.LogTrace("RunOnMainThread() called after {NodeName} was disposed", CanonicalName);
		}
		else
		{
			Invalidate();
			_threadQueue.RunOnMainThread(action, state0, state1);
		}
	}

	public void RunOnMainThread<TState0, TState1>(Action<Base, TState0, TState1> action, TState0 state0, TState1 state1)
	{
		Invalidate();
		_threadQueue.RunOnMainThread(action, this, state0, state1);
	}

	public void RunOnMainThread<TState0, TState1, TState2>(Action<TState0, TState1, TState2> action, TState0 state0, TState1 state1, TState2 state2)
	{
		Invalidate();
		_threadQueue.RunOnMainThread(action, state0, state1, state2);
	}
}
