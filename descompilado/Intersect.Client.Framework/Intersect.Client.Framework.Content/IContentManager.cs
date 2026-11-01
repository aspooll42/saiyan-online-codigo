using Intersect.Plugins;

namespace Intersect.Client.Framework.Content;

public interface IContentManager
{
	TAsset Find<TAsset>(ContentType contentType, string assetName) where TAsset : class, IAsset;

	TAsset Load<TAsset>(ContentType contentType, string assetPath, string assetAlias) where TAsset : class, IAsset;

	TAsset LoadEmbedded<TAsset>(IPluginContext context, ContentType contentType, string assetName) where TAsset : class, IAsset;

	TAsset LoadEmbedded<TAsset>(IPluginContext context, ContentType contentType, string assetName, string assetAlias) where TAsset : class, IAsset;
}
