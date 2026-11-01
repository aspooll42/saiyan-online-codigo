using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Control.Layout;
using Intersect.Client.Framework.Gwen.Control.Utility;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Data;
using Intersect.Client.Interface.Debugging.Providers;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Client.Networking;
using Intersect.Framework.Reflection;
using Intersect.Framework.SystemInformation;
using Intersect.IO.Files;

namespace Intersect.Client.Interface.Debugging;

internal sealed class DebugWindow : Window
{
	private const string NoValue = "-";

	private readonly IFont? _defaultFont;

	private bool _wasParentDrawDebugOutlinesEnabled;

	private bool _drawDebugOutlinesEnabled;

	private bool _viewClickedNodeInDebugger;

	private readonly NodeUnderCursorProvider _nodeUnderCursorProvider = new NodeUnderCursorProvider();

	private readonly Dictionary<IGameTexture, TableRow> _gpuAllocationsRowByTextureLookup = new Dictionary<IGameTexture, TableRow>();

	private bool _resizeGPUAllocationsTable;

	private Table GPUStatisticsTable { get; }

	private Table SystemStatisticsTable { get; }

	private Table GPUAllocationsTable { get; }

	private TabControl Tabs { get; }

	private TabButton TabInfo { get; }

	private TabButton TabAssets { get; }

	private TabButton TabSystem { get; }

	private SearchableTree AssetsList { get; }

	private Table AssetsToolsTable { get; }

	private Button AssetsButtonReloadAsset { get; }

	private LabeledCheckBox CheckboxDrawDebugOutlines { get; }

	private LabeledCheckBox CheckboxEnableLayoutHotReloading { get; }

	private LabeledCheckBox CheckboxIncludeTextNodesInHover { get; }

	private LabeledCheckBox CheckboxViewClickedNodeInDebugger { get; }

	private Button ButtonShutdownServer { get; }

	private Button ButtonShutdownServerAndExit { get; }

	private Table TableDebugStats { get; }

	public DebugWindow(Base parent)
		: base(parent, Strings.Debug.Title, modal: false, "DebugWindow")
	{
		_defaultFont = GameContentManager.Current?.GetFont("rajdhanibold");
		base.IsHidden = true;
		base.IsResizable = false;
		base.InnerPanelPadding = new Padding(4);
		base.MinimumSize = new Point(360, 320);
		base.Size = new Point(480, 600);
		base.MaximumSize = new Point(800, 600);
		Tabs = CreateTabs();
		TabInfo = Tabs.AddPage(Strings.Debug.TabLabelInfo, "TabInfo");
		CheckboxDrawDebugOutlines = CreateInfoCheckboxDrawDebugOutlines(TabInfo.Page);
		CheckboxEnableLayoutHotReloading = CreateInfoCheckboxEnableLayoutHotReloading(TabInfo.Page);
		CheckboxIncludeTextNodesInHover = CreateInfoCheckboxIncludeTextNodesInHover(TabInfo.Page);
		CheckboxViewClickedNodeInDebugger = CreateInfoCheckboxViewClickedNodeInDebugger(TabInfo.Page);
		ButtonShutdownServer = CreateInfoButtonShutdownServer(TabInfo.Page);
		ButtonShutdownServerAndExit = CreateInfoButtonShutdownServerAndExit(TabInfo.Page);
		TableDebugStats = CreateInfoTableDebugStats(TabInfo.Page);
		TabAssets = Tabs.AddPage(Strings.Debug.TabLabelAssets, "TabAssets");
		AssetsToolsTable = CreateAssetsToolsTable(TabAssets.Page);
		AssetsList = CreateAssetsList(TabAssets.Page);
		AssetsButtonReloadAsset = CreateAssetsButtonReloadAsset(AssetsToolsTable, AssetsList);
		TabSystem = Tabs.AddPage(Strings.Debug.TabLabelSystem, "TabSystem");
		new Label(TabSystem.Page, "HeaderSectionSystemStatistics")
		{
			Dock = Pos.Top,
			Font = _defaultFont,
			FontSize = 10,
			Text = Strings.Debug.SectionSystemStatistics,
			TextAlign = Pos.CenterH,
			TextColorOverride = new Color(191, 255, 191)
		};
		SystemStatisticsTable = CreateSystemStatisticsTable(TabSystem.Page);
		new Label(TabSystem.Page, "HeaderSectionGPUStatistics")
		{
			Dock = Pos.Top,
			Font = _defaultFont,
			FontSize = 10,
			Text = Strings.Debug.SectionGPUStatistics,
			TextAlign = Pos.CenterH,
			TextColorOverride = new Color(191, 255, 191)
		};
		GPUStatisticsTable = CreateGPUStatisticsTable(TabSystem.Page);
		new Label(TabSystem.Page, "HeaderSectionGPUAllocations")
		{
			Dock = Pos.Top,
			Font = _defaultFont,
			FontSize = 10,
			Text = Strings.Debug.SectionGPUAllocations,
			TextAlign = Pos.CenterH,
			TextColorOverride = new Color(191, 255, 191)
		};
		GPUAllocationsTable = CreateGPUAllocationsTable(TabSystem.Page);
		AssetsToolsTable.SizeToChildren();
	}

	protected override void Dispose(bool disposing)
	{
		UnsubscribeGPU();
		RemoveIntercepts();
		base.Dispose(disposing);
	}

	private Table CreateGPUAllocationsTable(Base parent)
	{
		Panel parent2 = new Panel(parent, "gpuAllocationsPanel")
		{
			Dock = Pos.Fill
		};
		ScrollControl scrollControl = new ScrollControl(parent2, "gpuAllocationsScroller")
		{
			Dock = Pos.Fill,
			InnerPanelPadding = new Padding(8, 0),
			ShouldCacheToTexture = true
		};
		scrollControl.VerticalScrollBar.BaseNudgeAmount *= 2f;
		Table table = new Table(scrollControl, "GPUAllocationsTable")
		{
			AutoSizeToContentHeightOnChildResize = true,
			AutoSizeToContentWidthOnChildResize = true,
			CellSpacing = new Point(8, 2),
			ColumnCount = 2,
			ColumnWidths = new global::_003C_003Ez__ReadOnlyArray<int?>(new int?[2] { null, 100 }),
			Dock = Pos.Fill,
			Font = _defaultFont,
			FontSize = 10,
			MinimumSize = new Point(408, 0),
			SizeToContents = true,
		};
		Table table2 = table;
		table2.VisibilityChanged += delegate(Base sender, VisibilityChangedEventArgs args)
		{
			if (args.IsVisibleInTree && _resizeGPUAllocationsTable)
			{
				_resizeGPUAllocationsTable = false;
				sender.SizeToChildren(resizeX: true, resizeY: true, recursive: true);
				sender.InvalidateParent();
			}
		};
		table2.SizeChanged += ResizeTableToChildrenOnSizeChanged;
		IGameTexture[] textures = Graphics.Renderer.Textures;
		IGameTexture[] array = textures;
		foreach (IGameTexture gameTexture in array)
		{
			EnsureGPUAllocationsRowFor(gameTexture, creating: true, table2);
		}
		SubscribeGPU();
		return table2;
	}

	private static void ResizeTableHeightToChildrenOnSizeChanged(Base sender, ValueChangedEventArgs<Point> args)
	{
		sender.SizeToChildren(resizeX: false, resizeY: true, recursive: true);
	}

	private static void ResizeTableToChildrenOnSizeChanged(Base sender, ValueChangedEventArgs<Point> args)
	{
		sender.SizeToChildren(args.Value.X > args.OldValue.X, resizeY: true, recursive: true);
	}

	private void SubscribeGPU()
	{
		Graphics.Renderer.TextureAllocated += RendererOnTextureAllocated;
		Graphics.Renderer.TextureCreated += RendererOnTextureCreated;
		Graphics.Renderer.TextureDisposed += RendererOnTextureDisposed;
		Graphics.Renderer.TextureFreed += RendererOnTextureFreed;
	}

	private void UnsubscribeGPU()
	{
		Graphics.Renderer.TextureAllocated -= RendererOnTextureAllocated;
		Graphics.Renderer.TextureCreated -= RendererOnTextureCreated;
		Graphics.Renderer.TextureDisposed -= RendererOnTextureDisposed;
		Graphics.Renderer.TextureFreed -= RendererOnTextureFreed;
	}

	private void RendererOnTextureDisposed(object? _, TextureEventArgs args)
	{
		IGameTexture gameTexture = args.GameTexture;
		if (_gpuAllocationsRowByTextureLookup.TryGetValue(gameTexture, out TableRow value))
		{
			GPUAllocationsTable.RemoveRow(value);
		}
	}

	private void RendererOnTextureFreed(object? _, TextureEventArgs args)
	{
		IGameTexture gameTexture = args.GameTexture;
		TableRow tableRow = EnsureGPUAllocationsRowFor(gameTexture, creating: false);
		if (tableRow.GetCellContents(1) is Label label)
		{
			label.Text = "Freed";
			label.TextColorOverride = new Color(255, 63, 63);
		}
	}

	private void RendererOnTextureAllocated(object? _, TextureEventArgs args)
	{
		IGameTexture gameTexture = args.GameTexture;
		TableRow tableRow = EnsureGPUAllocationsRowFor(gameTexture, creating: false);
		if (tableRow.GetCellContents(1) is Label label)
		{
			label.Text = "Allocated";
			label.TextColorOverride = new Color(63, 255, 63);
		}
	}

	private void RendererOnTextureCreated(object? _, TextureEventArgs args)
	{
		IGameTexture gameTexture = args.GameTexture;
		EnsureGPUAllocationsRowFor(gameTexture, creating: true);
	}

	private TableRow EnsureGPUAllocationsRowFor(IGameTexture gameTexture, bool creating, Table? table = null)
	{
		if (_gpuAllocationsRowByTextureLookup.TryGetValue(gameTexture, out TableRow value))
		{
			if (creating)
			{
				throw new InvalidOperationException();
			}
			return value;
		}
		if (table == null)
		{
			table = GPUAllocationsTable;
		}
		value = table.InsertRowSorted(gameTexture.Name, SelectRowUserDataGameTextureName, null, gameTexture);
		Base cellContents = value.GetCellContents(0);
		cellContents.UserData = gameTexture;
		cellContents.MouseInputEnabled = true;
		cellContents.Clicked += CopyAssetNameToClipboardOnNameCellClicked;
		value.SetCellText(1, "Created");
		if (value.GetCellContents(1) is Label label)
		{
			label.TextColorOverride = new Color(191, 191, 191);
		}
		_gpuAllocationsRowByTextureLookup[gameTexture] = value;
		if (table.IsVisibleInTree)
		{
			table.SizeToChildren(resizeX: true, resizeY: true, recursive: true);
		}
		else
		{
			_resizeGPUAllocationsTable = true;
		}
		return value;
	}

	private static void CopyAssetNameToClipboardOnNameCellClicked(Base sender, MouseButtonState _)
	{
		if (sender.UserData is IAsset asset)
		{
			GameClipboard.Instance.SetText(asset.Name);
		}
	}

	private static string? SelectRowUserDataGameTextureName(Base? node)
	{
		if (!(node?.UserData is IGameTexture gameTexture))
		{
			return null;
		}
		return gameTexture.Name;
	}

	private Table CreateGPUStatisticsTable(Base parent)
	{
		ScrollControl scroller = new ScrollControl(parent, "scroller")
		{
			Dock = Pos.Top,
			MinimumSize = new Point(0, 100)
		};
		scroller.VerticalScrollBar.BaseNudgeAmount *= 2f;
		Table table = new Table(scroller, "SystemStatisticsTable")
		{
			AutoSizeToContentHeightOnChildResize = true,
			AutoSizeToContentWidthOnChildResize = true,
			CellSpacing = new Point(8, 2),
			ColumnCount = 2,
			ColumnWidths = new global::_003C_003Ez__ReadOnlyArray<int?>(new int?[2] { 180, null }),
			Dock = Pos.Fill,
			Font = _defaultFont,
			FontSize = 10
		};
		table.SizeChanged += delegate(Base sender, ValueChangedEventArgs<Point> args)
		{
			ResizeTableHeightToChildrenOnSizeChanged(sender, args);
			Point minimumSize = new Point(0, table.OuterHeight + scroller.InnerPanelPadding.Top + scroller.InnerPanelPadding.Bottom);
			scroller.MinimumSize = minimumSize;
		};
		table.AddRow(Strings.Debug.Fps, "FPSRow").Listen(1, new DelegateDataProvider<int>(() => Graphics.Renderer.FPS), "-");
		table.AddRow(Strings.Debug.MapsDrawn, "MapsDrawnRow").Listen(1, new DelegateDataProvider<int>(() => Graphics.MapsDrawn), "-");
		table.AddRow(Strings.Debug.EntitiesDrawn, "EntitiesDrawnRow").Listen(1, new DelegateDataProvider<int>(() => Graphics.EntitiesDrawn), "-");
		table.AddRow(Strings.Debug.LightsDrawn, "LightsDrawnRow").Listen(1, new DelegateDataProvider<int>(() => Graphics.LightsDrawn), "-");
		table.AddRow(Strings.Debug.InterfaceObjects, "InterfaceObjectsRow").Listen(1, new DelegateDataProvider<int?>(() => Interface.CurrentInterface?.NodeCount, 1000), "-");
		table.AddRow(Strings.Debug.RenderTargetAllocations, "GPURenderTargetAllocations").Listen(1, new DelegateDataProvider<ulong>(() => Graphics.Renderer.RenderTargetAllocations), "-");
		table.AddRow(Strings.Debug.TextureAllocations, "GPUTextureAllocations").Listen(1, new DelegateDataProvider<ulong>(() => Graphics.Renderer.TextureAllocations), "-");
		table.AddRow(Strings.Debug.TextureCount, "GPUTextureCount").Listen(1, new DelegateDataProvider<ulong>(() => Graphics.Renderer.TextureCount), "-");
		table.AddRow(Strings.Debug.UsedVRAM, "GPUUsedVRAM").Listen(1, new DelegateDataProvider<string>(() => FileSystemHelper.FormatSize(Graphics.Renderer.UsedMemory)), "-");
		table.AddRow(Strings.Debug.FreeVRAM, "GPUFreeVRAM").Listen(1, new DelegateDataProvider<string>(() => FileSystemHelper.FormatSize(PlatformStatistics.AvailableGPUMemory)), "-");
		table.AddRow(Strings.Debug.TotalVRAM, "GPUTotalVRAM").Listen(1, new DelegateDataProvider<string>(() => FileSystemHelper.FormatSize(PlatformStatistics.TotalGPUMemory)), "-");
		table.PreLayout.Enqueue(delegate(Table t)
		{
			t.SizeToChildren(resizeX: true, resizeY: true, recursive: true);
		}, table);
		return table;
	}

	private Table CreateSystemStatisticsTable(Base parent)
	{
		ScrollControl scroller = new ScrollControl(parent, "scroller")
		{
			Dock = Pos.Top
		};
		scroller.VerticalScrollBar.BaseNudgeAmount *= 2f;
		Table table = new Table(scroller, "SystemStatisticsTable")
		{
			AutoSizeToContentHeightOnChildResize = true,
			AutoSizeToContentWidthOnChildResize = true,
			CellSpacing = new Point(8, 2),
			ColumnCount = 2,
			ColumnWidths = new global::_003C_003Ez__ReadOnlyArray<int?>(new int?[2] { 180, null }),
			Dock = Pos.Top,
			Font = _defaultFont,
			FontSize = 10
		};
		table.SizeChanged += delegate(Base sender, ValueChangedEventArgs<Point> args)
		{
			ResizeTableHeightToChildrenOnSizeChanged(sender, args);
			Point minimumSize = new Point(0, table.OuterHeight + scroller.InnerPanelPadding.Top + scroller.InnerPanelPadding.Bottom);
			scroller.MinimumSize = minimumSize;
		};
		table.AddRow(Strings.Debug.FreeVirtualRAM, "RAMFreeVirtual").Listen(1, new DelegateDataProvider<string>(() => FileSystemHelper.FormatSize(PlatformStatistics.AvailableSystemMemory)), "-");
		table.AddRow(Strings.Debug.TotalVirtualRAM, "RAMTotalVirtual").Listen(1, new DelegateDataProvider<string>(() => FileSystemHelper.FormatSize(PlatformStatistics.TotalSystemMemory)), "-");
		table.AddRow(Strings.Debug.FreePhysicalRAM, "RAMFreePhysical").Listen(1, new DelegateDataProvider<string>(() => FileSystemHelper.FormatSize(PlatformStatistics.AvailablePhysicalMemory)), "-");
		table.AddRow(Strings.Debug.TotalPhysicalRAM, "RAMTotalPhysical").Listen(1, new DelegateDataProvider<string>(() => FileSystemHelper.FormatSize(PlatformStatistics.TotalPhysicalMemory)), "-");
		table.PreLayout.Enqueue(delegate(Table t)
		{
			t.SizeToChildren(resizeX: true, resizeY: true, recursive: true);
		}, table);
		return table;
	}

	private SearchableTree CreateAssetsList(Base parent)
	{
		AssetsSearchableTreeDataProvider dataProvider = new AssetsSearchableTreeDataProvider(GameContentManager.Current, this);
		return new SearchableTree(parent, dataProvider, "AssetsList")
		{
			Dock = Pos.Fill,
			FontSearch = _defaultFont,
			FontSearchSize = 10,
			FontTree = _defaultFont,
			FontTreeSize = 10,
			SearchPlaceholderText = Strings.Debug.AssetsSearchPlaceholder
		};
	}

	private Table CreateAssetsToolsTable(Base assetsTabPage)
	{
		Table table = new Table(assetsTabPage, "AssetsToolsTable")
		{
			AutoSizeToContentHeightOnChildResize = true,
			AutoSizeToContentWidthOnChildResize = true,
			CellSpacing = new Point(4, 4),
			ColumnCount = 2,
			ColumnWidths = new global::_003C_003Ez__ReadOnlyArray<int?>(new int?[2]),
			Dock = Pos.Top,
			SizeToContents = true,
		};
		return table;
	}

	private Button CreateAssetsButtonReloadAsset(Table table, SearchableTree assetList)
	{
		TableRow tableRow = table.AddNamedRow("AssetsButtonReloadAssetRow");
		Button button = new Button(tableRow, "AssetsButtonReloadAsset")
		{
			IsDisabled = (assetList.SelectedNodes.Length < 1),
			Font = _defaultFont,
			FontSize = 10,
			Text = Strings.Debug.ReloadAsset
		};
		tableRow.SetCellContents(0, button);
		assetList.SelectionChanged += AssetListOnSelectionChanged;
		button.Clicked += ButtonReloadAssetOnClicked;
		tableRow.SetCellContents(0, button, enableMouseInput: true);
		return button;
	}

	private void ButtonReloadAssetOnClicked(Base @base, MouseButtonState mouseButtonState)
	{
		TreeNode[] selectedNodes = AssetsList.SelectedNodes;
		foreach (TreeNode treeNode in selectedNodes)
		{
			if (treeNode.UserData is SearchableTreeDataEntry { UserData: IAsset userData } && userData is IGameTexture gameTexture)
			{
				gameTexture.Reload();
			}
		}
	}

	private void AssetListOnSelectionChanged(Base @base, EventArgs eventArgs)
	{
		AssetsButtonReloadAsset.IsDisabled = AssetsList.SelectedNodes.All((TreeNode node) => !(node.UserData is SearchableTreeDataEntry searchableTreeDataEntry) || !(searchableTreeDataEntry.UserData is IGameTexture));
	}

	protected override void EnsureInitialized()
	{
	}

	protected override void OnAttached(Base parent)
	{
		base.OnAttached(parent);
		base.Root.DrawDebugOutlines = _drawDebugOutlinesEnabled;
	}

	protected override void OnAttaching(Base newParent)
	{
		base.OnAttaching(newParent);
		_wasParentDrawDebugOutlinesEnabled = newParent.DrawDebugOutlines;
	}

	protected override void OnDetaching(Base oldParent)
	{
		base.OnDetaching(oldParent);
		oldParent.DrawDebugOutlines = _wasParentDrawDebugOutlinesEnabled;
	}

	private TabControl CreateTabs()
	{
		return new TabControl(this, "DebugTabs")
		{
			Dock = Pos.Fill
		};
	}

	private LabeledCheckBox CreateInfoCheckboxDrawDebugOutlines(Base parent)
	{
		_drawDebugOutlinesEnabled = base.Root?.DrawDebugOutlines ?? false;
		LabeledCheckBox labeledCheckBox = new LabeledCheckBox(parent, "CheckboxDrawDebugOutlines")
		{
			Dock = Pos.Top,
			Font = _defaultFont,
			FontSize = 10,
			IsChecked = _drawDebugOutlinesEnabled,
			Text = Strings.Debug.DrawDebugOutlines
		};
		labeledCheckBox.CheckChanged += CheckboxDrawDebugOutlinesOnCheckChanged;
		return labeledCheckBox;
	}

	private void CheckboxDrawDebugOutlinesOnCheckChanged(ICheckbox sender, ValueChangedEventArgs<bool> eventArgs)
	{
		_drawDebugOutlinesEnabled = eventArgs.Value;
		Base root = base.Root;
		if (root != null)
		{
			root.DrawDebugOutlines = _drawDebugOutlinesEnabled;
		}
	}

	private LabeledCheckBox CreateInfoCheckboxEnableLayoutHotReloading(Base parent)
	{
		LabeledCheckBox labeledCheckBox = new LabeledCheckBox(parent, "CheckboxEnableLayoutHotReloading")
		{
			Dock = Pos.Top,
			Font = _defaultFont,
			FontSize = 10,
			IsChecked = GameContentManager.Current.ContentWatcher.Enabled,
			Text = Strings.Debug.EnableLayoutHotReloading,
			TextColorOverride = Color.Yellow
		};
		labeledCheckBox.CheckChanged += CheckboxEnableLayoutHotReloadOnCheckChanged;
		labeledCheckBox.SetToolTipText(Strings.Internals.ExperimentalFeatureTooltip);
		labeledCheckBox.TooltipFont = base.Skin.DefaultFont;
		return labeledCheckBox;
	}

	private static void CheckboxEnableLayoutHotReloadOnCheckChanged(ICheckbox sender, ValueChangedEventArgs<bool> args)
	{
		GameContentManager.Current.ContentWatcher.Enabled = args.Value;
	}

	private LabeledCheckBox CreateInfoCheckboxIncludeTextNodesInHover(Base parent)
	{
		LabeledCheckBox labeledCheckBox = new LabeledCheckBox(parent, "CheckboxIncludeTextNodesInHover")
		{
			Dock = Pos.Top,
			Font = _defaultFont,
			FontSize = 10,
			IsChecked = _nodeUnderCursorProvider.Filter.HasFlag(NodeFilter.IncludeText),
			Text = Strings.Debug.IncludeTextNodesInHover
		};
		labeledCheckBox.CheckChanged += CheckboxIncludesTextNodesInHoverOnCheckChanged;
		return labeledCheckBox;
	}

	private void CheckboxIncludesTextNodesInHoverOnCheckChanged(ICheckbox checkbox, ValueChangedEventArgs<bool> eventArgs)
	{
		if (eventArgs.Value)
		{
			_nodeUnderCursorProvider.Filter |= NodeFilter.IncludeText;
		}
		else
		{
			_nodeUnderCursorProvider.Filter &= ~NodeFilter.IncludeText;
		}
	}

	private LabeledCheckBox CreateInfoCheckboxViewClickedNodeInDebugger(Base parent)
	{
		LabeledCheckBox labeledCheckBox = new LabeledCheckBox(parent, "CheckboxViewClickedNodeInDebugger")
		{
			Dock = Pos.Top,
			Font = _defaultFont,
			FontSize = 10,
			IsChecked = _viewClickedNodeInDebugger,
			Text = Strings.Debug.ViewClickedNodeInDebugger,
			TooltipText = Strings.Debug.ViewClickedNodeInDebuggerTooltip
		};
		labeledCheckBox.CheckChanged += CheckboxViewClickedNodeInDebuggerOnCheckChanged;
		return labeledCheckBox;
	}

	private void CheckboxViewClickedNodeInDebuggerOnCheckChanged(ICheckbox checkbox, ValueChangedEventArgs<bool> eventArgs)
	{
		bool viewClickedNodeInDebugger = _viewClickedNodeInDebugger;
		_viewClickedNodeInDebugger = eventArgs.Value;
		if (_viewClickedNodeInDebugger)
		{
			if (!viewClickedNodeInDebugger)
			{
				AddIntercepts();
			}
		}
		else if (viewClickedNodeInDebugger)
		{
			RemoveIntercepts();
		}
	}

	private void AddIntercepts()
	{
		Input.MouseDownIntercept += MouseDownIntercept;
		Input.MouseUpIntercept += MouseUpIntercept;
	}

	private void RemoveIntercepts()
	{
		Input.MouseDownIntercept -= MouseDownIntercept;
		Input.MouseUpIntercept -= MouseUpIntercept;
	}

	private bool MouseDownIntercept(Keys modifier, MouseButton mouseButton)
	{
		if (base.IsVisibleInTree && _viewClickedNodeInDebugger)
		{
			return true;
		}
		RemoveIntercepts();
		return false;
	}

	private bool MouseUpIntercept(Keys modifier, MouseButton mouseButton)
	{
		if (!base.IsVisibleInTree || !_viewClickedNodeInDebugger)
		{
			RemoveIntercepts();
			return false;
		}
		Base nodeUnderCursor = GetNodeUnderCursor();
		Debugger.Break();
		return true;
	}

	private Button CreateInfoButtonShutdownServer(Base parent)
	{
		Button button = new Button(parent, "ButtonShutdownServer")
		{
			Dock = Pos.Top,
			Font = _defaultFont,
			FontSize = 10,
			IsHidden = true,
			Text = Strings.Debug.ShutdownServer
		};
		button.Clicked += delegate
		{
		};
		return button;
	}

	private Button CreateInfoButtonShutdownServerAndExit(Base parent)
	{
		Button button = new Button(parent, "ButtonShutdownServerAndExit")
		{
			Dock = Pos.Top,
			Font = _defaultFont,
			FontSize = 10,
			IsHidden = true,
			Text = Strings.Debug.ShutdownServerAndExit
		};
		button.Clicked += delegate
		{
		};
		return button;
	}

	private Table CreateInfoTableDebugStats(Base parent)
	{
		ScrollControl scrollControl = new ScrollControl(parent, "debugStatsScroller")
		{
			Dock = Pos.Fill
		};
		scrollControl.VerticalScrollBar.BaseNudgeAmount *= 2f;
		Table table = new Table(scrollControl, "TableDebugStats")
		{
			AutoSizeToContentHeightOnChildResize = true,
			AutoSizeToContentWidthOnChildResize = true,
			CellSpacing = new Point(8, 2),
			ColumnCount = 2,
			ColumnWidths = new global::_003C_003Ez__ReadOnlyArray<int?>(new int?[2] { 180, null }),
			Dock = Pos.Fill,
			Font = _defaultFont,
			FontSize = 10,
		};
		Table table2 = table;
		table2.SizeChanged += ResizeTableToChildrenOnSizeChanged;
		table2.AddRow(Strings.Debug.Fps, "FPSRow").Listen(1, new DelegateDataProvider<int>(() => Graphics.Renderer.FPS), "-");
		table2.AddRow(Strings.Debug.Draws, "DrawsRow").Listen(1, new DelegateDataProvider<int>(() => Graphics.DrawCalls), "-");
		table2.AddRow(Strings.Debug.Ping, "PingRow").Listen(1, new DelegateDataProvider<int>(() => Intersect.Client.Networking.Network.Ping, 5000), "-");
		table2.AddRow(Strings.Debug.Map, "MapRow").Listen(1, new DelegateDataProvider<string>(() => (!MapInstance.TryGet(Globals.Me?.MapId ?? default(Guid), out MapInstance instance)) ? null : instance.Name), "-");
		table2.AddRow(Strings.Internals.CoordinateX, "PlayerXRow").Listen(1, new DelegateDataProvider<int?>(() => Globals.Me?.X), "-");
		table2.AddRow(Strings.Internals.CoordinateY, "PlayerYRow").Listen(1, new DelegateDataProvider<int?>(() => Globals.Me?.Y), "-");
		table2.AddRow(Strings.Internals.CoordinateZ, "PlayerZRow").Listen(1, new DelegateDataProvider<int?>(() => Globals.Me?.Z), "-");
		table2.AddRow(Strings.Debug.Time, "TimeRow").Listen(1, new DelegateDataProvider<string>(Time.GetTime), "-");
		table2.AddRow(Strings.Debug.KnownMaps, "KnownMapsRow").Listen(1, new DelegateDataProvider<int>(() => MapInstance.Lookup.Count), "-");
		table2.AddRow(Strings.Debug.MapsDrawn, "MapsDrawnRow").Listen(1, new DelegateDataProvider<int>(() => Graphics.MapsDrawn), "-");
		table2.AddRow(Strings.Debug.EntitiesDrawn, "EntitiesDrawnRow").Listen(1, new DelegateDataProvider<int>(() => Graphics.EntitiesDrawn), "-");
		table2.AddRow(Strings.Debug.LightsDrawn, "LightsDrawnRow").Listen(1, new DelegateDataProvider<int>(() => Graphics.LightsDrawn), "-");
		table2.AddRow(Strings.Debug.InterfaceObjects, "InterfaceObjectsRow").Listen(1, new DelegateDataProvider<int?>(() => Interface.CurrentInterface?.NodeCount, 1000), "-");
		TableRow tableRow = table2.AddRow(Strings.Debug.SectionGPUStatistics, 2, "SectionGPU");
		if (tableRow.GetCellContents(0) is Label label)
		{
			label.TextColorOverride = new Color(191, 255, 191);
		}
		table2.AddRow(Strings.Debug.RenderTargetAllocations, "GPURenderTargetAllocations").Listen(1, new DelegateDataProvider<ulong>(() => Graphics.Renderer.RenderTargetAllocations), "-");
		table2.AddRow(Strings.Debug.TextureAllocations, "GPUTextureAllocations").Listen(1, new DelegateDataProvider<ulong>(() => Graphics.Renderer.TextureAllocations), "-");
		table2.AddRow(Strings.Debug.TextureCount, "GPUTextureCount").Listen(1, new DelegateDataProvider<ulong>(() => Graphics.Renderer.TextureCount), "-");
		table2.AddRow(Strings.Debug.FreeVRAM, "GPUFreeVRAM").Listen(1, new DelegateDataProvider<string>(() => FileSystemHelper.FormatSize(PlatformStatistics.AvailableGPUMemory)), "-");
		table2.AddRow(Strings.Debug.TotalVRAM, "GPUTotalVRAM").Listen(1, new DelegateDataProvider<string>(() => FileSystemHelper.FormatSize(PlatformStatistics.TotalGPUMemory)), "-");
		table2.AddRow(Strings.Debug.FreeVirtualRAM, "RAMFreeVirtual").Listen(1, new DelegateDataProvider<string>(() => FileSystemHelper.FormatSize(PlatformStatistics.AvailableSystemMemory)), "-");
		table2.AddRow(Strings.Debug.TotalVirtualRAM, "RAMTotalVirtual").Listen(1, new DelegateDataProvider<string>(() => FileSystemHelper.FormatSize(PlatformStatistics.TotalSystemMemory)), "-");
		table2.AddRow(Strings.Debug.FreePhysicalRAM, "RAMFreePhysical").Listen(1, new DelegateDataProvider<string>(() => FileSystemHelper.FormatSize(PlatformStatistics.AvailablePhysicalMemory)), "-");
		table2.AddRow(Strings.Debug.TotalPhysicalRAM, "RAMTotalPhysical").Listen(1, new DelegateDataProvider<string>(() => FileSystemHelper.FormatSize(PlatformStatistics.TotalPhysicalMemory)), "-");
		TableRow tableRow2 = table2.AddRow(Strings.Debug.ControlUnderCursor, 2, "SectionUI");
		if (tableRow2.GetCellContents(0) is Label label2)
		{
			label2.TextColorOverride = new Color(191, 255, 191);
		}
		table2.AddRow(Strings.Internals.Type, "TypeRow").Listen<Base, string>(1, _nodeUnderCursorProvider, (Base node, Base _) => node?.GetType().GetName(), Strings.Internals.NotApplicable);
		table2.AddRow(Strings.Internals.Name, "NameRow").Listen<Base, string>(1, _nodeUnderCursorProvider, (Base node, Base _) => node?.ParentQualifiedName, "-");
		table2.AddRow(Strings.Internals.IsVisibleInTree, "IsVisible").Listen<Base, bool?>(1, _nodeUnderCursorProvider, (Base node, Base _) => node?.IsVisibleInTree, "-");
		table2.AddRow(Strings.Internals.IsVisibleInParent, "IsVisibleInParent").Listen<Base, bool?>(1, _nodeUnderCursorProvider, (Base node, Base _) => node?.IsVisibleInParent, "-");
		table2.AddRow(Strings.Internals.IsDisabled, "IsDisabled").Listen<Base, bool?>(1, _nodeUnderCursorProvider, (Base node, Base _) => node?.IsDisabled, "-");
		table2.AddRow(Strings.Internals.IsDisabledByTree, "IsDisabledByTree").Listen<Base, bool?>(1, _nodeUnderCursorProvider, (Base node, Base _) => node?.IsDisabledByTree, "-");
		table2.AddRow(Strings.Internals.LocalItem.ToString(Strings.Internals.Bounds), "LocalBoundsRow").Listen<Base, Rectangle?>(1, _nodeUnderCursorProvider, (Base node, Base _) => node?.Bounds, "-");
		table2.AddRow(Strings.Internals.GlobalItem.ToString(Strings.Internals.Bounds), "GlobalBoundsRow").Listen<Base, Rectangle?>(1, _nodeUnderCursorProvider, (Base node, Base _) => node?.GlobalBounds, "-");
		table2.AddRow(Strings.Internals.InnerBounds, "InnerBoundsRow").Listen<Base, Rectangle?>(1, _nodeUnderCursorProvider, (Base node, Base _) => node?.InnerBounds, "-");
		table2.AddRow(Strings.Internals.OuterBounds, "OuterBounds").Listen<Base, Rectangle?>(1, _nodeUnderCursorProvider, (Base node, Base _) => node?.OuterBounds, "-");
		table2.AddRow(Strings.Internals.Margin, "MarginRow").Listen<Base, Margin?>(1, _nodeUnderCursorProvider, (Base node, Base _) => node?.Margin, "-");
		table2.AddRow(Strings.Internals.Padding, "PaddingRow").Listen<Base, Padding?>(1, _nodeUnderCursorProvider, (Base node, Base _) => node?.Padding, "-");
		table2.AddRow(Strings.Internals.Dock, "Dock").Listen<Base, Pos?>(1, _nodeUnderCursorProvider, (Base node, Base _) => node?.Dock, "-");
		table2.AddRow(Strings.Internals.Alignment, "Alignment").Listen<Base, string>(1, _nodeUnderCursorProvider, delegate(Base node, Base _)
		{
			if (node != null)
			{
				Alignments[] alignment = node.Alignment;
				if (alignment != null && alignment.Length > 0)
				{
					return string.Join(", ", node.Alignment);
				}
			}
			return (string)null;
		}, "-");
		table2.AddRow(Strings.Internals.Color, "ColorRow").Listen<Base, Color>(1, _nodeUnderCursorProvider, (Base node, Base _) => (node as IColorableText)?.TextColor, "-");
		table2.AddRow(Strings.Internals.ColorOverride, "ColorOverrideRow").Listen<Base, Color>(1, _nodeUnderCursorProvider, (Base node, Base _) => (node as IColorableText)?.TextColorOverride, "-");
		table2.AddRow(Strings.Internals.TextAlign, "TextAlign").Listen<Base, Pos?>(1, _nodeUnderCursorProvider, (Base node, Base _) => (node as Label)?.TextAlign, "-");
		table2.AddRow(Strings.Internals.Font, "Font").Listen<Base, string>(1, _nodeUnderCursorProvider, delegate(Base node, Base _)
		{
			if (node is Label label5)
			{
				return label5.FontName;
			}
			return (node is Text text) ? text.Font?.Name : null;
		}, "-");
		table2.AddRow(Strings.Internals.FontSize, "FontSize").Listen<Base, int?>(1, _nodeUnderCursorProvider, delegate(Base node, Base _)
		{
			if (node is Label label5)
			{
				return label5.FontSize;
			}
			return (node is Text text) ? new int?(text.FontSize) : ((int?)null);
		}, "-");
		table2.AddRow(Strings.Internals.AutoSizeToContents, "AutoSizeToContents").Listen<Base, bool?>(1, _nodeUnderCursorProvider, (Base node, Base _) => (node as IAutoSizeToContents)?.AutoSizeToContents, "-");
		table2.AddRow(Strings.Internals.AutoSizeToContentWidth, "AutoSizeToContentWidth").Listen<Base, bool?>(1, _nodeUnderCursorProvider, (Base node, Base _) => (node as ISmartAutoSizeToContents)?.AutoSizeToContentWidth, "-");
		table2.AddRow(Strings.Internals.AutoSizeToContentHeight, "AutoSizeToContentHeight").Listen<Base, bool?>(1, _nodeUnderCursorProvider, (Base node, Base _) => (node as ISmartAutoSizeToContents)?.AutoSizeToContentHeight, "-");
		table2.AddRow(Strings.Internals.AutoSizeToContentWidthOnChildResize, "AutoSizeToContentWidthOnChildResize").Listen<Base, bool?>(1, _nodeUnderCursorProvider, (Base node, Base _) => (node as ISmartAutoSizeToContents)?.AutoSizeToContentWidthOnChildResize, "-");
		table2.AddRow(Strings.Internals.AutoSizeToContentHeightOnChildResize, "AutoSizeToContentHeightOnChildResize").Listen<Base, bool?>(1, _nodeUnderCursorProvider, (Base node, Base _) => (node as ISmartAutoSizeToContents)?.AutoSizeToContentHeightOnChildResize, "-");
		TableRow[] array = table2.Children.OfType<TableRow>().ToArray();
		TableRow[] array2 = array;
		foreach (TableRow tableRow3 in array2)
		{
			if (tableRow3.Name.StartsWith("Section") && tableRow3.GetCellContents(1) is Label label3)
			{
				Padding padding = label3.Padding;
				padding.Top = 8;
				label3.Padding = padding;
			}
			if (tableRow3.GetCellContents(0) is Label label4)
			{
				label4.TextAlign = Pos.Right | Pos.CenterV;
			}
		}
		return table2;
	}

	private Base? GetNodeUnderCursor()
	{
		return Interface.FindComponentUnderCursor(_nodeUnderCursorProvider.Filter);
	}
}
