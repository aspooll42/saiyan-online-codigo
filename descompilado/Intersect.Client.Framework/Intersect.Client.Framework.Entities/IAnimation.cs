using System;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Animations;

namespace Intersect.Client.Framework.Entities;

public interface IAnimation : IDisposable
{
	bool IsDisposed { get; }

	bool AutoRotate { get; set; }

	bool Hidden { get; set; }

	bool InfiniteLoop { get; set; }

	AnimationDescriptor? Descriptor { get; set; }

	Point Size { get; }

	event Action<IAnimation>? Disposed;

	event Action<IAnimation>? Finished;

	void Hide();

	void SetDir(Direction dir);

	void SetPosition(float worldX, float worldY, int mapx, int mapy, Guid mapId, Direction dir, int z = 0);

	void Show();
}
