using System;
using Intersect.Client.Framework.Maps;
using Intersect.Client.General;
using Intersect.Framework.Core;

namespace Intersect.Client.Maps;

public class ActionMessage : IActionMessage
{
	public Color Color { get; init; }

	public Guid EntityId { get; init; }

	public IMapInstance Map { get; init; }

	public string Text { get; init; }

	public long TransmissionTimer { get; init; }

	public long CreatedAt { get; init; } = Timing.Global.Milliseconds;

	public int X { get; init; }

	public int XOffset { get; init; }

	public int Y { get; init; }

	public ActionMessage(MapInstance map, int x, int y, string text, Color color, Guid entityId = default(Guid))
	{
		Map = map;
		X = x;
		Y = y;
		Text = text;
		Color = color;
		EntityId = entityId;
		XOffset = Globals.Random.Next(-30, 30);
		TransmissionTimer = Timing.Global.MillisecondsUtc + 1000;
	}

	public void TryRemove()
	{
		if (TransmissionTimer <= Timing.Global.MillisecondsUtc)
		{
			(Map as MapInstance)?.ActionMessages.Remove(this);
		}
	}
}
