using System;

namespace Intersect.Client.Framework.Content;

public interface IAsset
{
	bool IsDisposed { get; }

	bool IsLoaded { get; }

	string Id { get; }

	string Name { get; }

	event Action<IAsset>? Disposed;

	event Action<IAsset>? Loaded;

	event Action<IAsset>? Unloaded;
}
