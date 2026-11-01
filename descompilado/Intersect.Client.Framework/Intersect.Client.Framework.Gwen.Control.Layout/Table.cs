using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control.Data;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Configuration;
using Intersect.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Framework.Gwen.Control.Layout;

public class Table : Base, ISmartAutoSizeToContents, IAutoSizeToContents, IColorableText
{
	private readonly List<int?> _columnWidths = new List<int?>();

	private readonly List<int> _computedColumnWidths = new List<int>();

	private readonly List<Pos> _columnTextAlignments = new List<Pos>();

	private int _columnCount;

	private int _defaultRowHeight;

	private Point _cellSpacing;

	private bool _sizeToContents;

	private ITableDataProvider? _dataProvider;

	private IFont? _font;

	private Color? _textColor;

	private Color? _textColorOverride;

	private bool _fitRowHeightToContents = true;

	private bool _autoSizeToContentWidth;

	private bool _autoSizeToContentHeight;

	private bool _autoSizeToContentWidthOnChildResize;

	private bool _autoSizeToContentHeightOnChildResize;

	private int _fontSize = 10;

	private int _rowCount;

	public IReadOnlyList<Pos> ColumnAlignments
	{
		get
		{
			return _columnTextAlignments.ToList();
		}
		set
		{
			int num = Math.Min(value.Count, _columnTextAlignments.Count);
			for (int i = 0; i < num; i++)
			{
				_columnTextAlignments[i] = value[i];
			}
			TableRow[] array = base.Children.OfType<TableRow>().ToArray();
			TableRow[] array2 = array;
			foreach (TableRow tableRow in array2)
			{
				tableRow.ColumnTextAlignments = value;
			}
		}
	}

	public int ColumnCount
	{
		get
		{
			return _columnCount;
		}
		set
		{
			SetAndDoIfChanged(ref _columnCount, value, OnColumnCountChanged);
		}
	}

	public Point CellSpacing
	{
		get
		{
			return _cellSpacing;
		}
		set
		{
			if (value == _cellSpacing)
			{
				return;
			}
			_cellSpacing = value;
			TableRow[] array = base.Children.OfType<TableRow>().ToArray();
			TableRow tableRow = array.FirstOrDefault();
			if (tableRow == null)
			{
				return;
			}
			Point point = value;
			point.Y = 0;
			Point cellSpacing = (tableRow.CellSpacing = point);
			TableRow[] array2 = array;
			foreach (TableRow tableRow2 in array2)
			{
				if (tableRow2 != tableRow)
				{
					tableRow2.Margin = new Margin(0, value.Y, 0, 0);
				}
				tableRow2.CellSpacing = cellSpacing;
			}
		}
	}

	public ITableDataProvider? DataProvider
	{
		get
		{
			return _dataProvider;
		}
		set
		{
			SetAndDoIfChanged<ITableDataProvider>(ref _dataProvider, value, delegate(ITableDataProvider oldValue, ITableDataProvider newValue)
			{
				if (oldValue != null)
				{
					oldValue.DataChanged -= OnDataChanged;
				}
				if (newValue != null)
				{
					newValue.DataChanged += OnDataChanged;
				}
			});
		}
	}

	public IFont? Font
	{
		get
		{
			return _font;
		}
		set
		{
			SetAndDoIfChanged(ref _font, value, SetFont);
		}
	}

	public string? FontName
	{
		get
		{
			return _font?.Name;
		}
		set
		{
			Font = GameContentManager.Current.GetFont(value);
		}
	}

	public int FontSize
	{
		get
		{
			return _fontSize;
		}
		set
		{
			SetAndDoIfChanged(ref _fontSize, value, SetFontSize);
		}
	}

	public int RowCount => base.Children.Count(TableRow.IsInstance);

	public Color? TextColor
	{
		get
		{
			return _textColor;
		}
		set
		{
			SetAndDoIfChanged(ref _textColor, value, SetTextColor);
		}
	}

	public Color? TextColorOverride
	{
		get
		{
			return _textColorOverride;
		}
		set
		{
			SetAndDoIfChanged(ref _textColorOverride, value, SetTextColorOverride);
		}
	}

	public int DefaultRowHeight
	{
		get
		{
			return _defaultRowHeight;
		}
		set
		{
			_defaultRowHeight = value;
		}
	}

	public IReadOnlyList<int?> ColumnWidths
	{
		get
		{
			return _columnWidths.ToArray();
		}
		set
		{
			for (int i = 0; i < _columnWidths.Count; i++)
			{
				_columnWidths[i] = ((i < value.Count) ? value[i] : ((int?)null));
			}
		}
	}

	public TableRow? this[int row] => base.Children.OfType<TableRow>().Skip(row).FirstOrDefault();

	public int[] ComputedColumnWidths => _computedColumnWidths.ToArray();

	public bool FitRowHeightToContents
	{
		get
		{
			return _fitRowHeightToContents;
		}
		set
		{
			if (value != _fitRowHeightToContents)
			{
				_fitRowHeightToContents = value;
				Invalidate();
			}
		}
	}

	public bool SizeToContents
	{
		get
		{
			return _sizeToContents;
		}
		set
		{
			if (value != _sizeToContents)
			{
				_sizeToContents = true;
				Invalidate();
			}
		}
	}

	public bool AutoSizeToContentWidth
	{
		get
		{
			return _autoSizeToContentWidth;
		}
		set
		{
			_autoSizeToContentWidth = value;
		}
	}

	public bool AutoSizeToContentHeight
	{
		get
		{
			return _autoSizeToContentHeight;
		}
		set
		{
			_autoSizeToContentHeight = value;
		}
	}

	public bool AutoSizeToContentWidthOnChildResize
	{
		get
		{
			return _autoSizeToContentWidthOnChildResize;
		}
		set
		{
			_autoSizeToContentWidthOnChildResize = value;
		}
	}

	public bool AutoSizeToContentHeightOnChildResize
	{
		get
		{
			return _autoSizeToContentHeightOnChildResize;
		}
		set
		{
			_autoSizeToContentHeightOnChildResize = value;
		}
	}

	public Table(Base parent, string? name = null)
		: base(parent, name)
	{
		_cellSpacing = new Point(2, 4);
		_columnCount = 1;
		_defaultRowHeight = 22;
		_sizeToContents = false;
		_columnTextAlignments = new List<Pos>(_columnCount);
		_columnWidths = new List<int?>(_columnCount);
		while (_columnWidths.Count < _columnCount)
		{
			_columnWidths.Add(null);
		}
		while (_columnTextAlignments.Count < _columnCount)
		{
			_columnTextAlignments.Add(Pos.Left | Pos.CenterV);
		}
	}

	private static void SetFont(Base @this, IFont? value)
	{
		foreach (TableRow item in @this.Children.OfType<TableRow>())
		{
			item.Font = value;
		}
	}

	private static void SetFontSize(Base @this, int value)
	{
		foreach (TableRow item in @this.Children.OfType<TableRow>())
		{
			item.FontSize = value;
		}
	}

	private static void SetTextColor(Base @this, Color? value)
	{
		foreach (IColorableText item in @this.Children.OfType<IColorableText>())
		{
			item.TextColor = value;
		}
	}

	private static void SetTextColorOverride(Base @this, Color? value)
	{
		foreach (IColorableText item in @this.Children.OfType<IColorableText>())
		{
			item.TextColorOverride = value;
		}
	}

	protected virtual void OnDataChanged(object sender, TableDataChangedEventArgs args)
	{
		while (base.Children.Count - 1 < args.Row)
		{
			AddRow();
		}
		TableRow tableRow = base.Children[args.Row] as TableRow;
		tableRow.SetCellText(args.Column, args.NewValue?.ToString());
	}

	public override JObject? GetJson(bool isRoot = false, bool onlySerializeIfNotEmpty = false)
	{
		JObject json = base.GetJson(isRoot, onlySerializeIfNotEmpty);
		if (json == null)
		{
			return null;
		}
		json.Add("SizeToContents", _sizeToContents);
		json.Add("DefaultRowHeight", _defaultRowHeight);
		json.Add("FontName", FontName);
		json.Add("FontSize", FontSize);
		json.Add("TextColor", TextColor?.ToString());
		json.Add("TextColorOverride", TextColorOverride?.ToString());
		return base.FixJson(json);
	}

	public override void LoadJson(JToken token, bool isRoot = false)
	{
		base.LoadJson(token, isRoot);
		if (!(token is JObject jObject))
		{
			return;
		}
		if (jObject["FitContents"] != null)
		{
			_sizeToContents = (bool)jObject["FitContents"];
		}
		if (jObject["DefaultRowHeight"] != null)
		{
			_defaultRowHeight = (int)jObject["DefaultRowHeight"];
		}
		if (jObject["Font"] != null)
		{
			string text = (string?)jObject["Font"];
			if (!string.IsNullOrWhiteSpace(text))
			{
				string[] array = text.Split(',');
				string name = array[0];
				int fontSize = int.Parse(array[1], CultureInfo.InvariantCulture);
				FontSize = fontSize;
				Font = GameContentManager.Current?.GetFont(name);
			}
		}
		if (jObject.TryGetValue("FontName", out JToken value) && value is JValue { Type: JTokenType.String } jValue)
		{
			FontName = jValue.Value<string>();
		}
		if (jObject.TryGetValue("FontSize", out JToken value2) && value2 is JValue { Type: JTokenType.Integer } jValue2)
		{
			FontSize = jValue2.Value<int>();
		}
		if (jObject["TextColor"] != null)
		{
			TextColor = Color.FromString((string?)jObject["TextColor"]);
		}
		if (jObject["TextColorOverride"] != null)
		{
			TextColorOverride = Color.FromString((string?)jObject["TextColorOverride"]);
		}
	}

	protected virtual void OnColumnCountChanged(int oldValue, int columnCount)
	{
		_columnWidths.Capacity = Math.Max(_columnWidths.Capacity, columnCount);
		while (_columnWidths.Count < columnCount)
		{
			_columnWidths.Add(null);
		}
		_columnTextAlignments.Capacity = Math.Max(_columnTextAlignments.Capacity, columnCount);
		while (_columnTextAlignments.Count < columnCount)
		{
			_columnTextAlignments.Add(Pos.Left | Pos.CenterV);
		}
		_computedColumnWidths.Capacity = Math.Max(_computedColumnWidths.Capacity, columnCount);
		int num = _computedColumnWidths.Sum();
		int num2 = Math.Max(0, base.InnerWidth - num);
		int val = columnCount - _computedColumnWidths.Count;
		int item = num2 / Math.Max(1, val);
		while (_computedColumnWidths.Count < columnCount)
		{
			_computedColumnWidths.Add(item);
		}
		TableRow[] array = base.Children.OfType<TableRow>().ToArray();
		TableRow[] array2 = array;
		foreach (TableRow tableRow in array2)
		{
			tableRow.ColumnCount = Math.Min(tableRow.ColumnCount, columnCount);
		}
	}

	public void SetColumnWidth(int column, int width)
	{
		if (_columnWidths[column] != width)
		{
			_columnWidths[column] = width;
			Invalidate();
		}
	}

	public TableRow AddRow()
	{
		return AddRow(ColumnCount);
	}

	public TableRow AddNamedRow(string? name)
	{
		return AddRow(ColumnCount, name);
	}

	public TableRow AddRow(params Base[] cellContents)
	{
		TableRow tableRow = AddRow();
		int num = Math.Min(cellContents.Length, tableRow.ColumnCount);
		for (int i = 0; i < num; i++)
		{
			Base control = cellContents[i];
			tableRow.SetCellContents(i, control);
		}
		return tableRow;
	}

	public TableRow AddRow(int columnCount, string? name = null)
	{
		return new TableRow(this, _computedColumnWidths.ToArray(), name)
		{
			CellSpacing = CellSpacing,
			ColumnCount = columnCount,
			ColumnTextAlignments = _columnTextAlignments,
			Dock = Pos.Top,
			FitHeightToContents = _fitRowHeightToContents,
			Font = Font,
			Height = _defaultRowHeight,
			Margin = new Margin(0, (_rowCount >= 1) ? CellSpacing.Y : 0, 0, 0),
			TextColor = TextColor,
			TextColorOverride = TextColorOverride,
			Width = base.InnerWidth
		};
	}

	public TableRow[] AddCells(params Base[] cellContents)
	{
		if (cellContents.Length < 1)
		{
			return Array.Empty<TableRow>();
		}
		TableRow tableRow = AddRow();
		int num = 1;
		List<TableRow> list = new List<TableRow>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<TableRow> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = tableRow;
		num2++;
		List<TableRow> list2 = list;
		int num3 = 0;
		foreach (Base control in cellContents)
		{
			if (num3 >= tableRow.ColumnCount)
			{
				tableRow = AddRow();
				list2.Add(tableRow);
				num3 = 0;
			}
			tableRow.SetCellContents(num3++, control);
		}
		return list2.ToArray();
	}

	public void AddRow(TableRow row)
	{
		if (row == null)
		{
			throw new ArgumentNullException("row");
		}
		row.Parent = this;
		row.FitHeightToContents = _fitRowHeightToContents;
		row.ColumnCount = Math.Min(_columnCount, row.ColumnCount);
		row.Dock = Pos.Top;
		if (row.Font == null)
		{
			IFont font = (row.Font = Font);
		}
		row.Height = _defaultRowHeight;
		row.Margin = new Margin(0, (_rowCount >= 1) ? CellSpacing.Y : 0, 0, 0);
		row.SetComputedColumnWidths(_computedColumnWidths.ToArray());
		row.SetColumnWidths(_columnWidths);
	}

	public TableRow AddRow(string text, string? name = null)
	{
		TableRow tableRow = AddNamedRow(name);
		tableRow.SetCellText(0, text);
		return tableRow;
	}

	public TableRow InsertRowSorted<TKey>(string text, Func<Base?, TKey?> keySelector, string? name = null, object? userData = null) where TKey : IComparable<TKey>
	{
		TableRow tableRow = AddRow(text, name);
		tableRow.UserData = userData;
		ResortChild(tableRow, keySelector);
		return tableRow;
	}

	public TableRow AddRow(string text, int columnCount, string? name = null, int columnIndex = 0)
	{
		TableRow tableRow = AddRow(columnCount, name);
		tableRow.SetCellText(columnIndex, text);
		return tableRow;
	}

	public void RemoveRow(TableRow row)
	{
		RemoveChild(row, dispose: true);
	}

	public void RemoveRow(int idx)
	{
		Base obj = base.Children[idx];
		RemoveRow(obj as TableRow);
	}

	public void RemoveAll()
	{
		while (RowCount > 0)
		{
			RemoveRow(0);
		}
		DoSizeToContents();
	}

	public int GetRowIndex(TableRow row)
	{
		return IndexOf(row);
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Layout(skin);
		if (_sizeToContents)
		{
			DoSizeToContents();
			_sizeToContents = false;
		}
		else
		{
			bool autoSizeToContentWidth = _autoSizeToContentWidth;
			bool autoSizeToContentHeight = _autoSizeToContentHeight;
			if (autoSizeToContentWidth || autoSizeToContentHeight)
			{
				DoSizeToContents(autoSizeToContentWidth, autoSizeToContentHeight);
			}
			else
			{
				ComputeColumnWidths();
			}
		}
		if (ClientConfiguration.Instance.EnableZebraStripedRows)
		{
			bool flag = false;
			TableRow[] array = base.Children.OfType<TableRow>().ToArray();
			TableRow[] array2 = array;
			foreach (TableRow tableRow in array2)
			{
				tableRow.EvenRow = flag;
				flag = !flag;
			}
		}
	}

	protected override void DoPostlayout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.DoPostlayout(skin);
	}

	public void FitContents(int? maxWidth = null)
	{
		Point maximumSize = base.MaximumSize;
		maximumSize.X = maxWidth ?? base.MaximumSize.X;
		base.MaximumSize = maximumSize;
		_sizeToContents = true;
		Invalidate();
	}

	protected virtual Point ComputeColumnWidths(TableRow[]? rows = null)
	{
		if (rows == null)
		{
			rows = base.Children.OfType<TableRow>().ToArray();
		}
		int columnCount = ColumnCount;
		int[] measuredContentWidths = rows.Select((TableRow row) => row.CalculateColumnContentWidths()).Aggregate(new int[columnCount], (int[] widths, int[] rowWidths) => widths.Select((int columnWidth, int columnIndex) => Math.Max(columnWidth, (rowWidths.Length > columnIndex) ? rowWidths[columnIndex] : 0)).ToArray());
		int x = CellSpacing.X;
		int availableWidth = base.InnerWidth;
		int?[] requestedWidths = _columnWidths.ToArray();
		requestedWidths = measuredContentWidths.Select(delegate(int measuredWidth, int columnIndex)
		{
			if (requestedWidths.Length <= columnIndex)
			{
				return measuredWidth;
			}
			int? num33 = requestedWidths[columnIndex];
			return (!num33.HasValue) ? num33 : new int?((num33.Value < 0) ? measuredWidth : num33.Value);
		}).ToArray();
		int flexColumnWidthSum = requestedWidths.Select((int? requestedWidth, int columnIndex) => (!requestedWidth.HasValue) ? measuredContentWidths[columnIndex] : 0).Sum();
		float[] array = requestedWidths.Select(delegate(int? requestedWidth, int columnIndex)
		{
			if (requestedWidths.Length == 1)
			{
				return 1f;
			}
			return (!requestedWidth.HasValue) ? ((float)measuredContentWidths[columnIndex] / Math.Max(flexColumnWidthSum, 1f)) : float.NaN;
		}).ToArray();
		int num = array.Count(float.IsFinite);
		float num2 = array.Sum((float ratio) => (!float.IsNaN(ratio)) ? ratio : 0f);
		if (num2 < 1f)
		{
			if (num2 > 0f)
			{
				float normalization = 1f / num2;
				array = array.Select((float ratio) => ratio * normalization).ToArray();
			}
			else
			{
				float num3 = 1f / (float)Math.Max(1, num);
				if (num == array.Length)
				{
					Array.Fill(array, num3);
				}
				else
				{
					for (int num4 = 0; num4 < array.Length; num4++)
					{
						if (!float.IsNaN(array[num4]))
						{
							array[num4] = num3;
						}
					}
				}
			}
		}
		int num5 = requestedWidths.Count((int? requestedWidth) => requestedWidth.HasValue);
		int num6 = requestedWidths.Sum((int? requestedWidth) => requestedWidth.GetValueOrDefault());
		availableWidth -= num6;
		if (flexColumnWidthSum < availableWidth)
		{
			int num7 = availableWidth - flexColumnWidthSum;
			int num8 = (int)Math.Floor((float)num7 / (float)num5);
			for (int num9 = 0; num9 < requestedWidths.Length; num9++)
			{
				int? num10 = requestedWidths[num9];
				if (num10.HasValue && num10.GetValueOrDefault() < 1)
				{
					requestedWidths[num9] += num8;
					availableWidth -= num8;
				}
			}
		}
		int[] flexColumnWidths = array.Select((float ratio) => (!float.IsNaN(ratio) && !ratio.Equals(0f)) ? ((int)MathF.Ceiling(Math.Max(10f / ratio, (float)availableWidth * ratio))) : 0).ToArray();
		int num11 = flexColumnWidths.Sum();
		if (num11 > availableWidth)
		{
			int num12 = num11 - availableWidth;
			if (num12 > columnCount)
			{
				int num13 = num12 % columnCount;
				int pixelsPerColumn = (num12 - num13) / columnCount;
				flexColumnWidths = flexColumnWidths.Select((int fcw) => fcw - pixelsPerColumn).ToArray();
				num12 = num13;
			}
			int[] array2 = flexColumnWidths.Distinct().ToArray();
			if (array2.Length == columnCount)
			{
				int num14 = 0;
				while (num14 < columnCount && num12 > 0)
				{
					flexColumnWidths[num14]--;
					num14++;
					num12--;
				}
			}
			else
			{
				int[] array3 = (from p in array2.ToDictionary((int dw) => dw, (int dw) => flexColumnWidths.Count((int fcw) => fcw == dw))
					orderby p.Value
					select p.Key).ToArray();
				int[] array4 = array3;
				foreach (int num16 in array4)
				{
					int num17 = 0;
					while (num17 < columnCount && num12 > 0)
					{
						if (flexColumnWidths[num17] == num16)
						{
							flexColumnWidths[num17]--;
						}
						num17++;
						num12--;
					}
				}
			}
		}
		int num18 = 0;
		int num19 = 0;
		int num20 = Math.Min(columnCount, requestedWidths.Length);
		int[] array5 = new int[Math.Max(_computedColumnWidths.Count, num20)];
		int value = ((rows.Length == 0) ? ((availableWidth - _cellSpacing.X * array5.Length) / Math.Max(1, array5.Length)) : 0);
		Array.Fill(array5, value);
		TableRow[] array6 = rows;
		foreach (TableRow tableRow in array6)
		{
			int[] array7 = new int[num20];
			for (int num22 = 0; num22 < num20; num22++)
			{
				int? num23 = requestedWidths[num22];
				int num24 = flexColumnWidths[num22];
				int num25 = num23 ?? num24;
				if (num22 > 0)
				{
					num25 -= x;
				}
				array7[num22] = num25;
			}
			Padding padding = tableRow.Padding;
			int num26 = padding.Left + padding.Right;
			if (num26 > 0)
			{
				int num27 = num26;
				if (num27 % num20 == 0)
				{
					int pixelsToRemovePerCell = num27 / num20;
					array7 = array7.Select((int rcw) => rcw - pixelsToRemovePerCell).ToArray();
				}
				else
				{
					int num28 = 0;
					while (num28 < num20 && num27 > 0)
					{
						array7[num28]--;
						num28++;
						num27--;
					}
				}
			}
			int num29 = 0;
			for (int num30 = 0; num30 < num20; num30++)
			{
				int num31 = array7[num30];
				TableCell column = tableRow.GetColumn(num30);
				if (column != null)
				{
					column.Width = num31;
				}
				array5[num30] = Math.Max(num31, array5[num30]);
				num29 += num31;
			}
			Point dockSpacing = tableRow.Dock.GetDockSpacing(base.DockChildSpacing);
			num18 = Math.Max(num18, num29) + dockSpacing.X;
			num19 += tableRow.OuterHeight + dockSpacing.Y;
		}
		_computedColumnWidths.Clear();
		_computedColumnWidths.AddRange(array5);
		TableRow[] array8 = rows;
		foreach (TableRow tableRow2 in array8)
		{
			tableRow2.SetComputedColumnWidths(array5);
		}
		ApplicationContext.CurrentContext.Logger.LogTrace("Computed table '{TableName}' content size: ({Width}, {Height})", base.ParentQualifiedName, num18, num19);
		return new Point(num18, num19);
	}

	public void DoSizeToContents(bool resizeX = false, bool resizeY = true)
	{
		TableRow[] array = base.Children.OfType<TableRow>().ToArray();
		if (_fitRowHeightToContents)
		{
			TableRow[] array2 = array;
			foreach (TableRow tableRow in array2)
			{
				tableRow.SizeToChildren(resizeX, resizeY, recursive: true);
			}
		}
		Point point = ComputeColumnWidths(array);
		int width = (resizeX ? point.X : base.Width);
		int height = (resizeY ? point.Y : base.Height);
		SetSize(width, height);
		InvalidateParent();
	}

	public override void Invalidate()
	{
		base.Invalidate();
	}

	public override Point GetChildrenSize()
	{
		Point childrenSize = base.GetChildrenSize();
		ApplicationContext.CurrentContext.Logger.LogTrace("Table {TableName} children size is {ChildrenSize}", base.ParentQualifiedName, childrenSize);
		return childrenSize;
	}

	public override bool SizeToChildren(SizeToChildrenArgs args)
	{
		ApplicationContext.CurrentContext.Logger.LogTrace("Resizing Table {TableName} to children ({Args})...", base.ParentQualifiedName, args);
		return base.SizeToChildren(args);
	}

	public void Invalidate(bool invalidateChildren, bool invalidateRecursive = true)
	{
		Invalidate();
		if (invalidateChildren)
		{
			InvalidateChildren(invalidateRecursive);
		}
	}

	protected override void OnChildBoundsChanged(Base child, Rectangle oldChildBounds, Rectangle newChildBounds)
	{
		base.OnChildBoundsChanged(child, oldChildBounds, newChildBounds);
		if (_autoSizeToContentWidthOnChildResize && oldChildBounds.Width != newChildBounds.Width)
		{
			Invalidate();
		}
		if (_autoSizeToContentHeightOnChildResize && oldChildBounds.Height != newChildBounds.Height)
		{
			Invalidate();
		}
	}

	protected override void OnSizeChanged(Point oldSize, Point newSize)
	{
		base.OnSizeChanged(oldSize, newSize);
		ApplicationContext.CurrentContext.Logger.LogTrace("Table {TableName} size changed from {OldSize} to {NewSize}", base.ParentQualifiedName, oldSize, newSize);
		if (oldSize.X != newSize.X && _rowCount <= 0)
		{
			int columnCount = _columnCount;
			int value = base.InnerWidth / columnCount;
			int[] array = new int[columnCount];
			Array.Fill(array, value);
			_computedColumnWidths.Clear();
			_computedColumnWidths.AddRange(array);
		}
	}

	protected override void OnBoundsChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		base.OnBoundsChanged(oldBounds, newBounds);
	}

	protected override void OnChildAdded(Base child)
	{
		base.OnChildAdded(child);
		if (TableRow.IsInstance(child))
		{
			_rowCount = Math.Min(base.Children.Count, _rowCount + 1);
		}
	}

	protected override void OnChildRemoved(Base child)
	{
		base.OnChildRemoved(child);
		if (TableRow.IsInstance(child))
		{
			_rowCount = Math.Max(0, _rowCount - 1);
		}
	}
}
