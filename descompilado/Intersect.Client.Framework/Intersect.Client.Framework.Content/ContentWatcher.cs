using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Intersect.Client.Framework.Content;

public sealed class ContentWatcher
{
	public enum Event
	{
		Change,
		Create,
		Delete,
		Rename
	}

	private readonly ConcurrentDictionary<string, bool> _ignore;

	private readonly Dictionary<Event, Dictionary<string, List<Action>>> _mappedHandlers;

	private readonly string _root;

	private readonly FileSystemWatcher _watcher;

	public bool Enabled
	{
		get
		{
			return _watcher.EnableRaisingEvents;
		}
		set
		{
			_watcher.EnableRaisingEvents = value;
		}
	}

	public ContentWatcher(string root)
	{
		_ignore = new ConcurrentDictionary<string, bool>();
		_mappedHandlers = new Dictionary<Event, Dictionary<string, List<Action>>>();
		if (string.IsNullOrWhiteSpace(root))
		{
			throw new ArgumentNullException("root");
		}
		_root = root;
		_watcher = new FileSystemWatcher(_root)
		{
			EnableRaisingEvents = false,
			IncludeSubdirectories = true,
			NotifyFilter = (NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Attributes | NotifyFilters.Size | NotifyFilters.LastWrite | NotifyFilters.LastAccess | NotifyFilters.CreationTime | NotifyFilters.Security)
		};
		_watcher.Changed += HandleChanged;
		_watcher.Created += HandleCreated;
		_watcher.Deleted += HandleDeleted;
		_watcher.Renamed += HandleRenamed;
	}

	public void AddEventListener(Event watcherEvent, string path, Action handler)
	{
		GetHandlersFor(watcherEvent, path, createIfMissing: true).Add(handler);
	}

	private Dictionary<string, List<Action>> GetHandlerMapFor(Event watcherEvent, bool createIfMissing = false)
	{
		if (_mappedHandlers.TryGetValue(watcherEvent, out Dictionary<string, List<Action>> value))
		{
			return value;
		}
		if (!createIfMissing)
		{
			return null;
		}
		value = new Dictionary<string, List<Action>>();
		_mappedHandlers.Add(watcherEvent, value);
		return value;
	}

	private List<Action> GetHandlersFor(Event watcherEvent, string path, bool createIfMissing = false)
	{
		Dictionary<string, List<Action>> handlerMapFor = GetHandlerMapFor(watcherEvent, createIfMissing);
		if (handlerMapFor == null)
		{
			return null;
		}
		if (handlerMapFor.TryGetValue(path, out var value))
		{
			return value;
		}
		if (!createIfMissing)
		{
			return null;
		}
		value = new List<Action>();
		handlerMapFor.Add(path, value);
		return value;
	}

	public void Modify(string path, Action modificationAction)
	{
		if (modificationAction == null)
		{
			throw new ArgumentNullException("modificationAction");
		}
		_ignore.TryAdd(path, value: true);
		modificationAction();
		Task.Delay(1000).ContinueWith(delegate
		{
			_ignore.TryRemove(path, out var _);
		}, TaskScheduler.Current);
	}

	private void HandleEvent(object sender, FileSystemEventArgs args, Event watcherEvent)
	{
		string text = args.FullPath.Replace(_root, string.Empty);
		if (text.StartsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
		{
			text = text.Substring(1);
		}
		if (_ignore.ContainsKey(text))
		{
			return;
		}
		List<Action> handlers = GetHandlersFor(watcherEvent, text);
		if (handlers == null)
		{
			return;
		}
		Task.Delay(1000).ContinueWith(delegate
		{
			foreach (Action item in handlers)
			{
				item?.Invoke();
			}
		}, TaskScheduler.Current);
	}

	private void HandleChanged(object sender, FileSystemEventArgs args)
	{
		HandleEvent(sender, args, Event.Change);
	}

	private void HandleCreated(object sender, FileSystemEventArgs args)
	{
		HandleEvent(sender, args, Event.Create);
	}

	private void HandleDeleted(object sender, FileSystemEventArgs args)
	{
		HandleEvent(sender, args, Event.Delete);
	}

	private void HandleRenamed(object sender, RenamedEventArgs args)
	{
		HandleEvent(sender, args, Event.Rename);
	}
}
