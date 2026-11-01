using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.Utility;
using Intersect.Client.Localization;
using Intersect.Core;
using Intersect.Framework.Reflection;
using Intersect.Localization;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Interface.Debugging;

public sealed class AssetsSearchableTreeDataProvider : ISearchableTreeDataProvider
{
	private readonly GameContentManager _contentManager;

	private readonly Dictionary<string, SearchableTreeDataEntry> _entries;

	private readonly Dictionary<IAsset, ContentType> _contentTypeByAsset;

	private readonly Dictionary<string, Dictionary<string, SearchableTreeDataEntry>> _entriesByParent;

	private readonly Base _parent;

	public event Base.GwenEventHandler<SearchableTreeDataEntriesEventArgs>? EntriesAdded;

	public event Base.GwenEventHandler<SearchableTreeDataEntriesEventArgs>? EntriesRemoved;

	public event Base.GwenEventHandler<SearchableTreeDataEntriesEventArgs>? EntriesChanged;

	public AssetsSearchableTreeDataProvider(GameContentManager contentManager, Base parent)
	{
		AssetsSearchableTreeDataProvider assetsSearchableTreeDataProvider = this;
		_contentManager = contentManager;
		_parent = parent;
		KeyValuePair<ContentType, ICollection<IAsset>>[] source = _contentManager.Content.ToArray();
		_contentTypeByAsset = source.SelectMany(delegate(KeyValuePair<ContentType, ICollection<IAsset>> pair)
		{
			KeyValuePair<ContentType, ICollection<IAsset>> keyValuePair2 = pair;
			var (contentType3, source2) = keyValuePair2;
			return source2.Select((IAsset key) => new KeyValuePair<IAsset, ContentType>(key, contentType3));
		}).ToDictionary();
		foreach (var (asset2, _) in _contentTypeByAsset)
		{
			asset2.Disposed += OnAssetStateChanged;
			asset2.Loaded += OnAssetStateChanged;
			asset2.Unloaded += OnAssetStateChanged;
		}
		_entries = _contentManager.Content.SelectMany<KeyValuePair<ContentType, ICollection<IAsset>>, SearchableTreeDataEntry>(delegate(KeyValuePair<ContentType, ICollection<IAsset>> contentTypeEntry)
		{
			ContentType key = contentTypeEntry.Key;
			string contentTypeId = $"{"ContentType"}:{key}";
			if (!Strings.Content.Types.TryGetValue(key, out LocalizedString value))
			{
				value = key.ToString();
			}
			IAsset[] source2 = contentTypeEntry.Value.ToArray();
			List<SearchableTreeDataEntry> list = new List<SearchableTreeDataEntry>();
			list.Add(new SearchableTreeDataEntry(contentTypeId, value, parent.Skin.Colors.Label.Normal));
			list.AddRange(source2.Select((IAsset asset3) => EntryForAsset(contentTypeId, asset3)));
			return list.ToArray();
		}).ToDictionary((SearchableTreeDataEntry entry) => entry.Id);
		_entriesByParent = (from tuple in _entries.Values.SelectMany((SearchableTreeDataEntry entry) => from ancestorId in assetsSearchableTreeDataProvider.GetAncestorIds(entry.ParentId)
				select (ancestorId: ancestorId, entry: entry))
			group tuple by tuple.ancestorId).ToDictionary((IGrouping<string, (string ancestorId, SearchableTreeDataEntry entry)> group) => group.Key, (IGrouping<string, (string ancestorId, SearchableTreeDataEntry entry)> group) => group.ToDictionary(((string ancestorId, SearchableTreeDataEntry entry) tuple) => tuple.entry.Id, ((string ancestorId, SearchableTreeDataEntry entry) tuple) => tuple.entry));
	}

	private void OnAssetStateChanged(IAsset changedAsset)
	{
		if (!_contentTypeByAsset.TryGetValue(changedAsset, out var value))
		{
			ApplicationContext.Context.Value?.Logger.LogWarning("Asset {AssetType} '{AssetId}' not indexed, unable to get content type", changedAsset.GetType().GetName(qualified: true), changedAsset.Id);
			return;
		}
		string parentId = $"{"ContentType"}:{value}";
		SearchableTreeDataEntry searchableTreeDataEntry = EntryForAsset(parentId, changedAsset);
		_entries[searchableTreeDataEntry.Id] = searchableTreeDataEntry;
		string[] ancestorIds = GetAncestorIds(searchableTreeDataEntry.ParentId);
		string[] array = ancestorIds;
		foreach (string key in array)
		{
			if (!_entriesByParent.TryGetValue(key, out Dictionary<string, SearchableTreeDataEntry> value2))
			{
				value2 = new Dictionary<string, SearchableTreeDataEntry>();
				_entriesByParent[key] = value2;
			}
			value2[searchableTreeDataEntry.Id] = searchableTreeDataEntry;
		}
		this.EntriesChanged?.Invoke(_parent, new SearchableTreeDataEntriesEventArgs
		{
			Entries = new SearchableTreeDataEntry[1] { searchableTreeDataEntry }
		});
	}

	private string[] GetAncestorIds(string? firstAncestorId)
	{
		if (string.IsNullOrWhiteSpace(firstAncestorId))
		{
			return Array.Empty<string>();
		}
		List<string> list = new List<string>();
		string text = firstAncestorId;
		while (!string.IsNullOrWhiteSpace(text))
		{
			list.Add(text);
			text = (_entries.TryGetValue(text, out var value) ? value.ParentId : null);
		}
		return list.ToArray();
	}

	private static SearchableTreeDataEntry EntryForAsset(string parentId, IAsset asset)
	{
		Color displayColor = Color.Green;
		if (asset.IsDisposed)
		{
			displayColor = Color.Red;
		}
		else if (!asset.IsLoaded)
		{
			displayColor = Color.White;
		}
		string text = asset.Name ?? asset.Id;
		string text2 = text;
		if (asset is IGameTexture { AtlasReference: not null })
		{
			text2 = Strings.Debug.FormatTextureFromAtlas.ToString(text2);
		}
		return new SearchableTreeDataEntry(parentId + ":" + text, text2, displayColor, text, parentId, asset);
	}

	public bool DoesEntryMatchFilterCriteria(SearchableTreeFilterCriteria filterCriteria, SearchableTreeDataEntry entry, bool checkChildren = false)
	{
		string value = filterCriteria.Query?.Trim();
		if (string.IsNullOrWhiteSpace(value))
		{
			return true;
		}
		if (checkChildren && _entriesByParent.TryGetValue(entry.Id, out Dictionary<string, SearchableTreeDataEntry> value2) && value2.Values.Any((SearchableTreeDataEntry child) => DoesEntryMatchFilterCriteria(filterCriteria, child)))
		{
			return true;
		}
		if (!string.IsNullOrWhiteSpace(entry.Name) && entry.Name.Contains(value))
		{
			return true;
		}
		if (entry.DisplayText.Contains(value))
		{
			return true;
		}
		string parentId = entry.ParentId;
		if (string.IsNullOrWhiteSpace(parentId))
		{
			return false;
		}
		if (!_entries.TryGetValue(parentId, out var value3))
		{
			return false;
		}
		return DoesEntryMatchFilterCriteria(filterCriteria, value3);
	}

	public SearchableTreeDataEntry[] FilterEntries(SearchableTreeFilterCriteria filterCriteria, ListSortDirection? sortDirection)
	{
		SearchableTreeDataEntry[] array = _entries.Values.Where((SearchableTreeDataEntry entry) => DoesEntryMatchFilterCriteria(filterCriteria, entry, checkChildren: true)).ToArray();
		return sortDirection switch
		{
			ListSortDirection.Ascending => array.OrderBy((SearchableTreeDataEntry entry) => entry.DisplayText).ToArray(), 
			ListSortDirection.Descending => array.OrderByDescending((SearchableTreeDataEntry entry) => entry.DisplayText).ToArray(), 
			null => array, 
			_ => throw new ArgumentOutOfRangeException("sortDirection", sortDirection, "Invalid sort direction"), 
		};
	}
}
