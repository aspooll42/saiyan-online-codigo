using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Control.Layout;
using Intersect.Client.Framework.Gwen.Skin;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Framework.Gwen.Control;

public class ListBox : ScrollControl
{
	private readonly List<ListBoxRow> mSelectedRows = new List<ListBoxRow>();

	private readonly Table _table;

	private bool mIsToggle;

	protected string mItemClickSound;

	protected string mItemHoverSound;

	protected string mItemRightClickSound;

	private bool mMultiSelect;

	private Pos mOldDock;

	private bool mSizeToContents;

	private Color? _textColor;

	private Color? _textColorOverride;

	public string? FontName
	{
		get
		{
			return _table.Font?.Name;
		}
		set
		{
			_table.Font = GameContentManager.Current.GetFont(value);
		}
	}

	public int FontSize
	{
		get
		{
			return _table.FontSize;
		}
		set
		{
			_table.FontSize = value;
		}
	}

	public bool AllowMultiSelect
	{
		get
		{
			return mMultiSelect;
		}
		set
		{
			mMultiSelect = value;
			if (value)
			{
				IsToggle = true;
			}
		}
	}

	public bool IsToggle
	{
		get
		{
			return mIsToggle;
		}
		set
		{
			mIsToggle = value;
		}
	}

	public int RowCount => _table.RowCount;

	public ListBoxRow this[int index] => _table[index] as ListBoxRow;

	public IEnumerable<TableRow> SelectedRows
	{
		get
		{
			List<TableRow> list = new List<TableRow>();
			foreach (ListBoxRow mSelectedRow in mSelectedRows)
			{
				list.Add(mSelectedRow);
			}
			return list;
		}
	}

	public ListBoxRow SelectedRow
	{
		get
		{
			if (mSelectedRows.Count == 0)
			{
				return null;
			}
			return mSelectedRows[0];
		}
		set
		{
			if (_table.Children.Contains(value))
			{
				if (AllowMultiSelect)
				{
					SelectRow(value);
				}
				else
				{
					SelectRow(value, clearOthers: true);
				}
			}
		}
	}

	public int SelectedRowIndex
	{
		get
		{
			ListBoxRow selectedRow = SelectedRow;
			if (selectedRow == null)
			{
				return -1;
			}
			return _table.GetRowIndex(selectedRow);
		}
		set
		{
			SelectRow(value);
		}
	}

	public int ColumnCount
	{
		get
		{
			return _table.ColumnCount;
		}
		set
		{
			_table.ColumnCount = value;
			Invalidate();
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

	public Point CellSpacing
	{
		get
		{
			return _table.CellSpacing;
		}
		set
		{
			_table.CellSpacing = value;
		}
	}

	public event GwenEventHandler<ItemSelectedEventArgs> RowSelected;

	public event GwenEventHandler<ItemSelectedEventArgs> RowUnselected;

	public ListBox(Base parent, string? name = null)
		: base(parent, name)
	{
		base.Margin = Margin.One;
		base.MouseInputEnabled = true;
		_textColor = Color.White;
		_textColorOverride = Color.Transparent;
		_table = new Table(this)
		{
			AutoSizeToContentHeightOnChildResize = true,
			AutoSizeToContentWidthOnChildResize = true,
			ColumnCount = 1,
			ColumnWidths = new global::_003C_003Ez__ReadOnlySingleElementList<int?>(null),
			Dock = Pos.Fill,
			FitRowHeightToContents = true,
			FontSize = 10
		};
		_table.BoundsChanged += TableResized;
		mMultiSelect = false;
		mIsToggle = false;
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

	public override JObject? GetJson(bool isRoot = false, bool onlySerializeIfNotEmpty = false)
	{
		JObject json = base.GetJson(isRoot, onlySerializeIfNotEmpty);
		if (json == null)
		{
			return null;
		}
		json.Add("SizeToContents", mSizeToContents);
		json.Add("MultiSelect", AllowMultiSelect);
		json.Add("IsToggle", IsToggle);
		json.Add("FontName", FontName);
		json.Add("FontSize", FontSize);
		json.Add("ItemHoverSound", mItemHoverSound);
		json.Add("ItemClickSound", mItemClickSound);
		json.Add("ItemRightClickSound", mItemRightClickSound);
		json.Add("TextColor", TextColor.ToString());
		json.Add("TextColorOverride", TextColorOverride.ToString());
		return base.FixJson(json);
	}

	public override void LoadJson(JToken token, bool isRoot = false)
	{
		base.LoadJson(token, isRoot);
		if (!(token is JObject jObject))
		{
			return;
		}
		if (jObject["SizeToContents"] != null)
		{
			mSizeToContents = (bool)jObject["SizeToContents"];
		}
		if (jObject["MultiSelect"] != null)
		{
			AllowMultiSelect = (bool)jObject["MultiSelect"];
		}
		if (jObject["IsToggle"] != null)
		{
			IsToggle = (bool)jObject["IsToggle"];
		}
		if (jObject["ItemHoverSound"] != null)
		{
			mItemHoverSound = (string?)jObject["ItemHoverSound"];
		}
		if (jObject["ItemClickSound"] != null)
		{
			mItemClickSound = (string?)jObject["ItemClickSound"];
		}
		if (jObject["ItemRightClickSound"] != null)
		{
			mItemRightClickSound = (string?)jObject["ItemRightClickSound"];
		}
		if (jObject["Font"] != null && jObject["Font"].Type != JTokenType.Null)
		{
			string[] array = ((string?)jObject["Font"]).Split(',');
			FontSize = int.Parse(array[1]);
			FontName = array[0];
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
		foreach (Base child in _table.Children)
		{
			ListBoxRow listBoxRow = (ListBoxRow)child;
			listBoxRow.HoverSound = mItemHoverSound;
			listBoxRow.ClickSound = mItemClickSound;
			listBoxRow.RightClickSound = mItemRightClickSound;
			listBoxRow.Font = _table.Font ?? listBoxRow.Font;
			listBoxRow.FontSize = FontSize;
		}
	}

	public void SelectRow(int index, bool clearOthers = false)
	{
		if (index >= 0 && index < _table.RowCount)
		{
			SelectRow(_table.Children[index], clearOthers);
		}
	}

	public void SelectRows(string rowText, bool clearOthers = false)
	{
		IEnumerable<ListBoxRow> enumerable = from x in _table.Children.OfType<ListBoxRow>()
			where x.Text == rowText
			select x;
		foreach (ListBoxRow item in enumerable)
		{
			SelectRow(item, clearOthers);
		}
	}

	public void SelectRowsByRegex(string pattern, RegexOptions regexOptions = RegexOptions.None, bool clearOthers = false)
	{
		IEnumerable<ListBoxRow> enumerable = from x in _table.Children.OfType<ListBoxRow>()
			where Regex.IsMatch(x.Text, pattern)
			select x;
		foreach (ListBoxRow item in enumerable)
		{
			SelectRow(item, clearOthers);
		}
	}

	public void SelectRow(Base control, bool clearOthers = false)
	{
		if (!AllowMultiSelect || clearOthers)
		{
			UnselectAll();
		}
		if (control is ListBoxRow listBoxRow)
		{
			listBoxRow.IsSelected = true;
			mSelectedRows.Add(listBoxRow);
			if (this.RowSelected != null)
			{
				this.RowSelected(this, new ItemSelectedEventArgs(listBoxRow));
			}
		}
	}

	public void RemoveAllRows()
	{
		_table.DeleteAllChildren();
	}

	public void RemoveRow(int idx)
	{
		_table.RemoveRow(idx);
		_table.DoSizeToContents();
	}

	public ListBoxRow AddRow(string label, object? userData)
	{
		return AddRow(label, null, userData);
	}

	public ListBoxRow AddRow(string label, string? name = null, object? userData = null)
	{
		int[] computedColumnWidths = _table.ComputedColumnWidths;
		ListBoxRow listBoxRow = new ListBoxRow(this, ColumnCount, computedColumnWidths)
		{
			ClickSound = mItemClickSound,
			FontSize = FontSize,
			HoverSound = mItemHoverSound,
			Name = name,
			RightClickSound = mItemRightClickSound,
			TextColor = TextColor,
			TextColorOverride = TextColorOverride,
			UserData = userData,
			Width = base.InnerWidth
		};
		_table.AddRow(listBoxRow);
		listBoxRow.SetCellText(0, label);
		listBoxRow.Selected += OnRowSelected;
		listBoxRow.Font = _table.Font ?? listBoxRow.Font;
		_table.FitContents(base.Width);
		return listBoxRow;
	}

	protected override void OnSizeChanged(Point oldSize, Point newSize)
	{
		base.OnSizeChanged(oldSize, newSize);
		if (oldSize.X != newSize.X)
		{
			_table.Width = base.InnerWidth;
		}
	}

	public void SetColumnWidth(int column, int width)
	{
		_table.SetColumnWidth(column, width);
		Invalidate();
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawListBox(this);
	}

	public virtual void UnselectAll()
	{
		foreach (ListBoxRow mSelectedRow in mSelectedRows)
		{
			mSelectedRow.IsSelected = false;
			if (this.RowUnselected != null)
			{
				this.RowUnselected(this, new ItemSelectedEventArgs(mSelectedRow));
			}
		}
		mSelectedRows.Clear();
	}

	public void UnselectRow(ListBoxRow row)
	{
		row.IsSelected = false;
		mSelectedRows.Remove(row);
		if (this.RowUnselected != null)
		{
			this.RowUnselected(this, new ItemSelectedEventArgs(row));
		}
	}

	protected virtual void OnRowSelected(Base control, ItemSelectedEventArgs args)
	{
		bool clearOthers = false;
		if (!(args.SelectedItem is ListBoxRow listBoxRow))
		{
			return;
		}
		if (listBoxRow.IsSelected)
		{
			if (IsToggle)
			{
				UnselectRow(listBoxRow);
			}
		}
		else
		{
			SelectRow(listBoxRow, clearOthers);
		}
	}

	public virtual void Clear()
	{
		UnselectAll();
		_table.RemoveAll();
	}

	public void SizeToContents()
	{
		mSizeToContents = true;
		_table.FitContents();
	}

	private void TableResized(Base control, ValueChangedEventArgs<Rectangle> args)
	{
		_table.SizeToChildren(resizeX: false);
	}

	public void SelectByText(string text)
	{
		foreach (ListBoxRow child in _table.Children)
		{
			if (child.Text == text)
			{
				SelectedRow = child;
				break;
			}
		}
	}

	public void SelectByName(string name)
	{
		foreach (ListBoxRow child in _table.Children)
		{
			if (child.Name == name)
			{
				SelectedRow = child;
				break;
			}
		}
	}

	public void SelectByUserData(object userdata)
	{
		foreach (ListBoxRow child in _table.Children)
		{
			if (userdata == null)
			{
				if (child.UserData == null)
				{
					SelectedRow = child;
					break;
				}
			}
			else if (userdata.Equals(child.UserData))
			{
				SelectedRow = child;
				break;
			}
		}
	}

	public ScrollBar GetVerticalScrollBar()
	{
		return base.VerticalScrollBar;
	}
}
