using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control.Data;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Platform;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Client.Interface.Data;
using Intersect.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Framework.Gwen.Control.Layout;

public class TableRow : Base, IColorableText, IFitHeightToContents
{
	private readonly List<Action> mDisposalActions = new List<Action>();

	private readonly List<TableCell> _columns = new List<TableCell>();

	private readonly List<Pos> _columnTextAlignments = new List<Pos>();

	private readonly List<int> _computedColumnWidths = new List<int>();

	protected string mClickSound;

	private int _columnCount;

	private bool _fitHeightToContents;

	private bool mEvenRow;

	private int _maximumColumns;

	private IFont? _font;

	private Color? _textColor;

	private Color? _textColorOverride;

	protected string mHoverSound;

	protected string mRightClickSound;

	private Point _cellSpacing;

	private int _fontSize = 10;

	public string HoverSound
	{
		get
		{
			return mHoverSound;
		}
		set
		{
			mHoverSound = value;
		}
	}

	public string ClickSound
	{
		get
		{
			return mClickSound;
		}
		set
		{
			mClickSound = value;
		}
	}

	public string RightClickSound
	{
		get
		{
			return mRightClickSound;
		}
		set
		{
			mRightClickSound = value;
		}
	}

	public IReadOnlyList<Pos> ColumnTextAlignments
	{
		get
		{
			return _columns.Select((TableCell column) => column.TextAlign).ToArray();
		}
		set
		{
			_columnTextAlignments.Clear();
			_columnTextAlignments.AddRange(value.Take(_columnCount));
			int num = Math.Min(_columns.Count, value.Count);
			for (int i = 0; i < num; i++)
			{
				_columns[i].TextAlign = value[i];
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
			SetAndDoIfChanged(ref _columnCount, value, ComputeColumns);
		}
	}

	public bool EvenRow
	{
		get
		{
			return mEvenRow;
		}
		set
		{
			mEvenRow = value;
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
			if (_font == value)
			{
				return;
			}
			_font = value;
			foreach (TableCell column in _columns)
			{
				column.Font = _font;
			}
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

	public int MaximumColumns
	{
		get
		{
			return _maximumColumns;
		}
		set
		{
			SetAndDoIfChanged(ref _maximumColumns, value, ComputeColumns);
		}
	}

	public string Text
	{
		get
		{
			return GetText();
		}
		set
		{
			SetCellText(0, value);
		}
	}

	public Color? TextColor
	{
		get
		{
			return _textColor;
		}
		set
		{
			Base.SetAndDoIfChanged(ref _textColor, value, SetTextColor, this);
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
			Base.SetAndDoIfChanged(ref _textColorOverride, value, SetTextColorOverride, this);
		}
	}

	public IEnumerable<string> TextColumns
	{
		get
		{
			return _columns.Select((TableCell column) => column.Text);
		}
		set
		{
			if (value == null)
			{
				foreach (TableCell column in _columns)
				{
					column.Text = string.Empty;
				}
			}
			int num = 0;
			foreach (string item in value)
			{
				if (num >= ColumnCount)
				{
					ColumnCount = num + 1;
				}
				SetCellText(num++, item);
			}
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
			if (!(value == _cellSpacing))
			{
				_cellSpacing = value;
				TableCell[] source = _columns.ToArray();
				TableCell tableCell = source.FirstOrDefault();
				TableCell[] array = source.Skip(1).ToArray();
				if (tableCell != null)
				{
					tableCell.Margin = default(Margin);
				}
				TableCell[] array2 = array;
				foreach (TableCell tableCell2 in array2)
				{
					tableCell2.Margin = new Margin(_cellSpacing.X, 0, 0, 0);
				}
			}
		}
	}

	public bool FitHeightToContents
	{
		get
		{
			return _fitHeightToContents;
		}
		set
		{
			SetAndDoIfChanged(ref _fitHeightToContents, value, Base.Invalidate);
		}
	}

	public event GwenEventHandler<ItemSelectedEventArgs> Selected;

	public TableRow(Table parent, int[]? columnWidths = null, string? name = null)
		: this(parent, parent.ColumnCount, columnWidths, name)
	{
	}

	public TableRow(Base parent, int columnCount, int[]? columnWidths = null, string? name = null)
		: base(parent, name)
	{
		if (columnWidths != null)
		{
			_computedColumnWidths.AddRange(columnWidths);
		}
		ColumnCount = columnCount;
		MaximumColumns = columnCount;
		base.KeyboardInputEnabled = true;
	}

	private static void SetFontSize(Base @this, int value)
	{
		if (!(@this is TableRow tableRow))
		{
			return;
		}
		foreach (TableCell column in tableRow._columns)
		{
			column.FontSize = value;
		}
	}

	private static void SetTextColor(TableRow @this, Color? value)
	{
		foreach (TableCell column in @this._columns)
		{
			column.TextColor = value;
		}
	}

	private static void SetTextColorOverride(TableRow @this, Color? value)
	{
		foreach (TableCell column in @this._columns)
		{
			column.TextColorOverride = value;
		}
	}

	protected override void OnMouseClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseClicked(mouseButton, mousePosition, userAction);
		PlaySound(mouseButton switch
		{
			MouseButton.Left => mClickSound, 
			MouseButton.Right => mRightClickSound, 
			_ => null, 
		});
	}

	protected override void OnMouseEntered()
	{
		base.OnMouseEntered();
		PlaySound(mHoverSound);
	}

	internal TableCell? GetColumn(int columnIndex)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(columnIndex, "columnIndex");
		if (columnIndex < _columnCount)
		{
			return _columns[columnIndex];
		}
		return null;
	}

	public override bool SizeToChildren(SizeToChildrenArgs args)
	{
		RunOnMainThread(SizeColumnsToChildren, this, args);
		return base.SizeToChildren(args);
	}

	private void SizeColumnsToChildren(TableRow @this, SizeToChildrenArgs args)
	{
		Padding padding = base.Padding;
		int num = padding.Bottom + padding.Top;
		int num2 = Math.Max(0, base.MinimumSize.Y - num);
		foreach (TableCell column in _columns)
		{
			num2 = Math.Max(num2, column.MeasureShrinkToContents().Y);
		}
		foreach (TableCell column2 in _columns)
		{
			if (!column2.IsEmpty)
			{
				SizeToChildrenArgs sizeToChildrenArgs = args;
				Point minimumSize = column2.MinimumSize;
				minimumSize.Y = num2;
				sizeToChildrenArgs.MinimumSize = minimumSize;
				SizeToChildrenArgs args2 = sizeToChildrenArgs;
				column2.SizeToChildren(args2);
			}
		}
	}

	protected override void OnSizeChanged(Point oldSize, Point newSize)
	{
		base.OnSizeChanged(oldSize, newSize);
		if (!string.IsNullOrWhiteSpace(base.Name))
		{
			ApplicationContext.CurrentContext.Logger.LogTrace("Table row {CanonicalName} resized from {OldSize} to {NewSize}", base.ParentQualifiedName, oldSize, newSize);
			string name = base.Name;
			if (!(name == "SectionGPU"))
			{
				_ = name == "SectionUI";
			}
		}
		if (oldSize.X == newSize.X)
		{
			return;
		}
		for (int i = 0; i < _computedColumnWidths.Count; i++)
		{
			int num = _computedColumnWidths[i];
			if (num >= 1)
			{
				TableCell column = GetColumn(i);
				if (column != null)
				{
					column.Width = num;
				}
			}
		}
	}

	protected override void OnChildBoundsChanged(Base child, Rectangle oldChildBounds, Rectangle newChildBounds)
	{
		base.OnChildBoundsChanged(child, oldChildBounds, newChildBounds);
	}

	protected override void OnChildSizeChanged(Base child, Point oldChildSize, Point newChildSize)
	{
		base.OnChildSizeChanged(child, oldChildSize, newChildSize);
		string parentQualifiedName = child.ParentQualifiedName;
		if (!parentQualifiedName.StartsWith('.') && !parentQualifiedName.EndsWith('.') && base.IsVisibleInTree)
		{
			if (!(parentQualifiedName == "SectionGPU.Column0"))
			{
				_ = parentQualifiedName == "SectionUI.Column0";
			}
			ApplicationContext.CurrentContext.Logger.LogTrace("Table row child {CanonicalName} resized from {OldChildSize} to {NewChildSize}", parentQualifiedName, oldChildSize, newChildSize);
		}
	}

	protected virtual void ComputeColumns(int oldValue, int columnCount)
	{
		_columns.Capacity = Math.Max(_columns.Capacity, columnCount);
		_columnTextAlignments.Capacity = Math.Max(_columnTextAlignments.Capacity, columnCount);
		int[] array = _computedColumnWidths.ToArray();
		if (array.Length < columnCount)
		{
			int num = array.Length;
			Array.Resize(ref array, columnCount);
			Array.Fill(array, 0, num, columnCount - num);
		}
		int num2 = array.Sum();
		int num3 = array.Count((int width) => width > 0);
		int num4 = base.InnerWidth - num2;
		int val = columnCount - num3;
		int num5 = num4 / Math.Max(1, val);
		while (_columns.Count < columnCount)
		{
			int count = _columns.Count;
			int num6 = array.Skip(count).FirstOrDefault();
			if (num6 < 1)
			{
				num6 = num5;
			}
			TableCell tableCell = new TableCell(this, $"Column{_columns.Count}")
			{
				AutoSizeToContents = false,
				Font = Font,
				FontSize = FontSize,
				MouseInputEnabled = false,
				Padding = default(Padding),
				RestrictToParent = true,
				TextColor = TextColor,
				TextColorOverride = TextColorOverride
			};
			if (num6 > 0)
			{
				tableCell.Width = num6;
			}
			if (_columnTextAlignments.Count > count)
			{
				tableCell.TextAlign = _columnTextAlignments[count];
			}
			_columns.Add(tableCell);
		}
		int num7 = columnCount - 1;
		for (int num8 = 0; num8 < columnCount; num8++)
		{
			TableCell tableCell2 = _columns[num8];
			bool flag = num8 == num7;
			tableCell2.AutoSizeToContents = false;
			if (flag)
			{
				tableCell2.Dock = Pos.Fill;
				tableCell2.Margin = Margin.Zero;
			}
			else
			{
				tableCell2.Dock = Pos.Left;
				tableCell2.Margin = new Margin(0, 0, _cellSpacing.X, 0);
			}
		}
		while (_columns.Count > MaximumColumns)
		{
			List<TableCell> columns = _columns;
			TableCell tableCell3 = columns[columns.Count - 1];
			tableCell3.Parent = null;
			_columns.RemoveAt(_columns.Count - 1);
		}
	}

	public TableRow Listen<TBaseValue>(int column, IDataProvider<TBaseValue> dataProvider, string? defaultText = null)
	{
		SetCellText(column, defaultText);
		AddDataProvider(dataProvider);
		dataProvider.ValueChanged += OnDataProviderEventHandler;
		return this;
		void OnDataProviderEventHandler(IDataProvider _, ValueChangedEventArgs<TBaseValue> args)
		{
			TBaseValue value = args.Value;
			string text = ((value != null) ? value.ToString() : null);
			SetCellText(column, text ?? defaultText);
		}
	}

	public TableRow Listen<TBaseValue, TDependentValue>(int column, IDataProvider<TBaseValue> dataProvider, ValueAdapterDelegate<TBaseValue, TDependentValue> adapter, string? defaultText = null)
	{
		SetCellText(column, defaultText);
		AddDataProvider(dataProvider);
		dataProvider.ValueChanged += OnDataProviderEventHandler;
		return this;
		void OnDataProviderEventHandler(IDataProvider _, ValueChangedEventArgs<TBaseValue> args)
		{
			string text = adapter(args.Value, args.OldValue)?.ToString();
			SetCellText(column, text ?? defaultText);
		}
	}

	public TableRow Listen(ITableDataProvider tableDataProvider, int row)
	{
		if (tableDataProvider == null)
		{
			throw new ArgumentNullException("tableDataProvider");
		}
		tableDataProvider.DataChanged += DataChanged;
		mDisposalActions.Add(delegate
		{
			tableDataProvider.DataChanged -= DataChanged;
		});
		return this;
		void DataChanged(object sender, TableDataChangedEventArgs args)
		{
			if (row == args.Row)
			{
				SetCellText(args.Column, args.NewValue?.ToString());
			}
		}
	}

	public TableRow Listen(ITableRowDataProvider tableRowDataProvider, int column)
	{
		if (tableRowDataProvider == null)
		{
			throw new ArgumentNullException("tableRowDataProvider");
		}
		tableRowDataProvider.DataChanged += DataChanged;
		mDisposalActions.Add(delegate
		{
			tableRowDataProvider.DataChanged -= DataChanged;
		});
		return this;
		void DataChanged(object sender, RowDataChangedEventArgs args)
		{
			if (args.Column == column)
			{
				SetCellText(column, args.NewValue?.ToString());
			}
		}
	}

	public void Listen(ITableCellDataProvider tableCellDataProvider, int column)
	{
		if (tableCellDataProvider == null)
		{
			throw new ArgumentNullException("tableCellDataProvider");
		}
		tableCellDataProvider.DataChanged += dataChanged;
		mDisposalActions.Add(delegate
		{
			tableCellDataProvider.DataChanged -= dataChanged;
		});
		void dataChanged(object sender, CellDataChangedEventArgs args)
		{
			SetCellText(column, args.NewValue?.ToString());
		}
	}

	protected override void Dispose(bool disposing)
	{
		while (mDisposalActions.Count > 0)
		{
			int index = mDisposalActions.Count - 1;
			mDisposalActions[index]?.Invoke();
			mDisposalActions.RemoveAt(index);
		}
		base.Dispose(disposing);
	}

	public void SetColumnWidth(int columnIndex, int width)
	{
		TableCell column = GetColumn(columnIndex);
		if (column != null && column.Width != width)
		{
			column.Width = width;
			Invalidate();
		}
	}

	public void SetCellText(int columnIndex, string? text)
	{
		if (_columns.Count < _columnCount)
		{
			ComputeColumns(0, _columnCount);
		}
		TableCell tableCell = _columns.Skip(columnIndex).FirstOrDefault();
		if (tableCell != null)
		{
			tableCell.Text = text;
		}
	}

	public void SetCellContents(int columnIndex, Base? control, bool enableMouseInput = false)
	{
		TableCell tableCell = _columns[columnIndex];
		if (tableCell != null)
		{
			Text text = tableCell.Children.OfType<Text>().FirstOrDefault();
			if (text != null)
			{
				text.IsVisibleInTree = control == null;
			}
			Base[] array = tableCell.Children.Where((Base child) => !(child is Text) && child != control).ToArray();
			Base[] array2 = array;
			foreach (Base obj in array2)
			{
				obj.Parent = null;
			}
			if (control != null)
			{
				control.Parent = tableCell;
				tableCell.MouseInputEnabled = enableMouseInput;
			}
		}
	}

	private Label GetCellLabel(int column)
	{
		return _columns[column];
	}

	public Base GetCellContents(int column)
	{
		return GetCellLabel(column);
	}

	protected virtual void OnRowSelected()
	{
		if (this.Selected != null)
		{
			this.Selected(this, new ItemSelectedEventArgs(this));
		}
	}

	public void SizeToContents()
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < _columnCount; i++)
		{
			TableCell tableCell = _columns[i];
			if (tableCell != null)
			{
				if (!tableCell.AutoSizeToContents)
				{
					tableCell.SizeToChildren();
				}
				num += tableCell.Width + tableCell.Margin.Left + tableCell.Margin.Right;
				num2 = Math.Max(num2, tableCell.Height + tableCell.Margin.Top + tableCell.Margin.Bottom);
			}
		}
		SetSize(num, num2);
	}

	public int[] CalculateColumnContentWidths()
	{
		return (from column in _columns.ToArray()
			select column.MeasureContent().X).ToArray();
	}

	public void SetTextColor(Color color)
	{
		for (int i = 0; i < _columnCount; i++)
		{
			if (_columns[i] != null)
			{
				_columns[i].TextColorOverride = color;
			}
		}
	}

	public string GetText(int column = 0)
	{
		return _columns[column].Text;
	}

	protected override void OnCopy(Base from, EventArgs args)
	{
		Neutral.SetClipboardText(Text);
	}

	public virtual void SetColumnWidths(IEnumerable<int?> columnWidths)
	{
		if (columnWidths == null)
		{
			return;
		}
		int num = 0;
		foreach (int? columnWidth in columnWidths)
		{
			TableCell column = GetColumn(num++);
			if (column != null && !column.AutoSizeToContents && columnWidth.HasValue)
			{
				column.Width = columnWidth.Value;
			}
		}
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (_fitHeightToContents)
		{
			SizeToChildren(resizeX: false);
		}
		base.Layout(skin);
	}

	public void SetComputedColumnWidths(int[] computedColumnWidths)
	{
		_computedColumnWidths.Clear();
		_computedColumnWidths.AddRange(computedColumnWidths);
	}

	public static bool IsInstance(Base? component)
	{
		return component is TableRow;
	}
}
