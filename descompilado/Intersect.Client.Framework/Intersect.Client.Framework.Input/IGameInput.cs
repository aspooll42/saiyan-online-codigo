using System.Collections.Generic;
using System.Numerics;
using Intersect.Client.Framework.GenericClasses;

namespace Intersect.Client.Framework.Input;

public interface IGameInput
{
	IControlSet ControlSet { get; set; }

	Vector2 MousePosition { get; }

	IReadOnlySet<IControlsProvider> ControlsProviders { get; }

	bool IsKeyDown(Keys key);

	bool WasKeyDown(Keys key);

	bool IsMouseButtonDown(MouseButton mb);

	bool WasMouseButtonDown(MouseButton mb);

	bool AddControlsProviders(params IControlsProvider[] controlsProviders);

	bool RemoveControlsProviders(params IControlsProvider[] controlsProviders);
}
