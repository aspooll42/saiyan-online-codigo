using Intersect.Client.Framework.Gwen.Control;

namespace Intersect.Client.Framework.Gwen.Renderer;

public interface ICacheToTexture
{
	void Initialize();

	void ShutDown();

	void SetupCacheTexture(Intersect.Client.Framework.Gwen.Control.Base control);

	void FinishCacheTexture(Intersect.Client.Framework.Gwen.Control.Base control);

	void DrawCachedControlTexture(Intersect.Client.Framework.Gwen.Control.Base control);

	void CreateControlCacheTexture(Intersect.Client.Framework.Gwen.Control.Base control);

	void UpdateControlCacheTexture(Intersect.Client.Framework.Gwen.Control.Base control);

	void DisposeCachedTexture(Intersect.Client.Framework.Gwen.Control.Base control);

	void SetRenderer(Base renderer);
}
