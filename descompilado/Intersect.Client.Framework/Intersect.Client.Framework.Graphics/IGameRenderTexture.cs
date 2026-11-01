using System;
using Intersect.Client.Framework.Content;

namespace Intersect.Client.Framework.Graphics;

public interface IGameRenderTexture : IGameTexture, IAsset, IComparable<IGameTexture>, IDisposable
{
	bool Begin();

	void Clear(Color black);

	void End();
}
