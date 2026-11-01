using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Framework.Gwen.Control.Utility;

public sealed class SearchableTree : Base
{
	private readonly Dictionary<string, TreeNode> _displayedNodes = new Dictionary<string, TreeNode>();

	private readonly TextBox _searchInput;

	private readonly TreeControl _tree;

	private readonly ISearchableTreeDataProvider _dataProvider;

	private ListSortDirection? _sortDirection;

	private bool _dirty;

	public SearchableTreeFilterCriteria FilterCriteria
	{
		get
		{
			return new SearchableTreeFilterCriteria(_searchInput.Text);
		}
		set
		{
			if (!string.Equals(value.Query, _searchInput.Text, StringComparison.CurrentCulture))
			{
				_searchInput.Text = value.Query;
				FetchEntries(value, Array.Empty<SearchableTreeDataEntry>());
			}
		}
	}

	public IFont? FontSearch
	{
		get
		{
			return _searchInput.Font;
		}
		set
		{
			_searchInput.Font = value;
		}
	}

	public int FontSearchSize
	{
		get
		{
			return _searchInput.FontSize;
		}
		set
		{
			_searchInput.FontSize = value;
		}
	}

	public IFont? FontTree
	{
		get
		{
			return _tree.Font;
		}
		set
		{
			_tree.Font = value;
		}
	}

	public int FontTreeSize
	{
		get
		{
			return _tree.FontSize;
		}
		set
		{
			_tree.FontSize = value;
		}
	}

	public string? SearchPlaceholderText
	{
		get
		{
			return _searchInput.PlaceholderText;
		}
		set
		{
			_searchInput.PlaceholderText = value;
		}
	}

	public TreeNode[] SelectedNodes => _tree.SelectedChildren.ToArray();

	public ListSortDirection? SortDirection
	{
		get
		{
			return _sortDirection;
		}
		set
		{
			if (value != _sortDirection)
			{
				_sortDirection = value;
				FetchEntries(FilterCriteria, Array.Empty<SearchableTreeDataEntry>(), value);
			}
		}
	}

	public event GwenEventHandler<EventArgs>? SelectionChanged;

	public SearchableTree(Base parent, ISearchableTreeDataProvider dataProvider, string? name = null)
		: base(parent, name)
	{
		_searchInput = new TextBox(this, "_searchInput")
		{
			Dock = Pos.Top,
			Margin = new Margin(0, 0, 0, 4)
		};
		_searchInput.TextChanged += SearchInputOnTextChanged;
		_tree = new TreeControl(this, "_tree")
		{
			Dock = Pos.Fill
		};
		_tree.SelectionChanged += TreeOnSelectionChanged;
		_dataProvider = dataProvider;
		_dataProvider.EntriesAdded += DataProvider_EntriesAdded;
		_dataProvider.EntriesChanged += DataProviderOnEntriesChanged;
		_dataProvider.EntriesRemoved += DataProviderOnEntriesRemoved;
		_dirty = true;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		if (_dirty)
		{
			_dirty = false;
			SearchableTreeFilterCriteria filterCriteria = FilterCriteria;
			SearchableTreeDataEntry[] added = Array.Empty<SearchableTreeDataEntry>();
			bool initial = _displayedNodes.Count < 1;
			FetchEntries(filterCriteria, added, null, initial);
		}
	}

	private void DataProvider_EntriesAdded(Base sender, SearchableTreeDataEntriesEventArgs arguments)
	{
		SearchableTreeFilterCriteria filterCriteria = FilterCriteria;
		SearchableTreeDataEntry[] array = arguments.Entries.Where((SearchableTreeDataEntry entry) => _dataProvider.DoesEntryMatchFilterCriteria(filterCriteria, entry)).ToArray();
		if (array.Length < 1)
		{
			ApplicationContext.Context.Value?.Logger.LogDebug("{EntryCount} entries were added but none were relevant to the current filter criteria {FilterCriteria}", arguments.Entries.Length, filterCriteria);
		}
		else
		{
			FetchEntries(filterCriteria, array);
		}
	}

	private void DataProviderOnEntriesChanged(Base sender, SearchableTreeDataEntriesEventArgs arguments)
	{
		SearchableTreeDataEntry[] entries = arguments.Entries;
		for (int i = 0; i < entries.Length; i++)
		{
			SearchableTreeDataEntry searchableTreeDataEntry = entries[i];
			if (_displayedNodes.TryGetValue(searchableTreeDataEntry.Id, out TreeNode value))
			{
				value.Text = searchableTreeDataEntry.DisplayText;
				value.TextColorOverride = searchableTreeDataEntry.DisplayColor;
			}
		}
	}

	private void DataProviderOnEntriesRemoved(Base sender, SearchableTreeDataEntriesEventArgs arguments)
	{
		SearchableTreeDataEntry[] entries = arguments.Entries;
		foreach (SearchableTreeDataEntry searchableTreeDataEntry in entries)
		{
			if (_displayedNodes.Remove(searchableTreeDataEntry.Id, out TreeNode value))
			{
				value.Parent = null;
			}
		}
	}

	private void FetchEntries(SearchableTreeFilterCriteria filterCriteria, SearchableTreeDataEntry[] added, ListSortDirection? sortDirection = null, bool initial = false)
	{
		ListSortDirection? listSortDirection = sortDirection;
		if (!listSortDirection.HasValue)
		{
			sortDirection = SortDirection;
		}
		SearchableTreeDataEntry[] array = _dataProvider.FilterEntries(filterCriteria, sortDirection);
		Dictionary<TreeNode, TreeNode> dictionary = new Dictionary<TreeNode, TreeNode>();
		HashSet<string> hashSet = array.Select((SearchableTreeDataEntry entry) => entry.Id).ToHashSet();
		foreach (TreeNode value4 in _displayedNodes.Values)
		{
			if (!(value4.UserData is SearchableTreeDataEntry searchableTreeDataEntry))
			{
				value4.IsVisibleInTree = false;
			}
			else
			{
				value4.IsVisibleInTree = hashSet.Contains(searchableTreeDataEntry.Id);
			}
		}
		SearchableTreeDataEntry[] array2 = array;
		for (int num = 0; num < array2.Length; num++)
		{
			SearchableTreeDataEntry searchableTreeDataEntry2 = array2[num];
			TreeNode treeNode = _tree;
			if (_displayedNodes.TryGetValue(searchableTreeDataEntry2.Id, out TreeNode value))
			{
				if (value.Parent is TreeNode treeNode2)
				{
					treeNode = treeNode2;
				}
				else
				{
					_displayedNodes.Remove(searchableTreeDataEntry2.Id);
					value = null;
				}
			}
			if (value == null)
			{
				if (!added.Contains(searchableTreeDataEntry2) && !initial)
				{
					ApplicationContext.CurrentContext.Logger.LogError("Entry '{EntryId}' ({EntryDisplayText}) was not found in the tree but wasn't added either, this state should be debugged", searchableTreeDataEntry2.Id, searchableTreeDataEntry2.Name ?? searchableTreeDataEntry2.DisplayText);
				}
				string parentId = searchableTreeDataEntry2.ParentId;
				if (parentId != null && !string.IsNullOrWhiteSpace(parentId))
				{
					if (!_displayedNodes.TryGetValue(parentId, out TreeNode value2))
					{
						throw new InvalidOperationException($"Missing parent entry '{parentId}' when trying to add '{searchableTreeDataEntry2.Id}'");
					}
					treeNode = value2;
				}
				value = treeNode.AddNode(searchableTreeDataEntry2.DisplayText);
				value.TextColorOverride = searchableTreeDataEntry2.DisplayColor;
				value.UserData = searchableTreeDataEntry2;
				_displayedNodes[searchableTreeDataEntry2.Id] = value;
			}
			if (dictionary.TryGetValue(treeNode, out var value3))
			{
				value.MoveAfter(value3);
			}
			else
			{
				value3 = treeNode.Children.OfType<TreeNode>().FirstOrDefault();
				if (value3 != value && value3 != null)
				{
					value.MoveBefore(value3);
				}
			}
			dictionary[treeNode] = value;
		}
	}

	private void SearchInputOnTextChanged(Base sender, EventArgs arguments)
	{
		FetchEntries(FilterCriteria, Array.Empty<SearchableTreeDataEntry>());
	}

	private void TreeOnSelectionChanged(Base sender, EventArgs arguments)
	{
		this.SelectionChanged?.Invoke(sender, arguments);
	}
}
