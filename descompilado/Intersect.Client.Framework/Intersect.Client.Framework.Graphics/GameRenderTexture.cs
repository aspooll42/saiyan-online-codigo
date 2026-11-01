using System;
using System.IO;
using Intersect.Client.Framework.Content;

namespace Intersect.Client.Framework.Graphics;

public abstract class GameRenderTexture<TPlatformRenderTexture, TPlatformRenderer> : GameTexture<TPlatformRenderTexture, TPlatformRenderer>, IGameRenderTexture, IGameTexture, IAsset, IComparable<IGameTexture>, IDisposable where TPlatformRenderTexture : class, IDisposable where TPlatformRenderer : GameRenderer
{
	protected GameRenderTexture(TPlatformRenderer renderer, TPlatformRenderTexture platformRenderTexture)
		: base(renderer, (string?)null, platformRenderTexture, (Func<Stream>?)null)
	{
	}

	public abstract bool Begin();

	public abstract void End();

	private protected override void InternalReload()
	{
	}

	public abstract void Clear(Color color);
}
