using System.ComponentModel;

namespace Intersect.Client.Framework.Gwen.Control.Utility;

public interface ISearchableTreeDataProvider
{
	event Base.GwenEventHandler<SearchableTreeDataEntriesEventArgs>? EntriesAdded;

	event Base.GwenEventHandler<SearchableTreeDataEntriesEventArgs>? EntriesRemoved;

	event Base.GwenEventHandler<SearchableTreeDataEntriesEventArgs>? EntriesChanged;

	bool DoesEntryMatchFilterCriteria(SearchableTreeFilterCriteria filterCriteria, SearchableTreeDataEntry entry, bool checkChildren = false);

	SearchableTreeDataEntry[] FilterEntries(SearchableTreeFilterCriteria filterCriteria, ListSortDirection? sortDirection);
}
