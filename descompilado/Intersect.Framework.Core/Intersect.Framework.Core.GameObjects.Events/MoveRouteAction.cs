using System;

namespace Intersect.Framework.Core.GameObjects.Events;

public class MoveRouteAction
{
	public Guid AnimationId { get; set; }

	public EventGraphic Graphic { get; set; }

	public MoveRouteEnum Type { get; set; }

	public MoveRouteAction Copy()
	{
		MoveRouteAction moveRouteAction = new MoveRouteAction
		{
			Type = Type
		};
		if (Type == MoveRouteEnum.SetGraphic)
		{
			moveRouteAction.Graphic = new EventGraphic();
			moveRouteAction.Graphic.CopyFrom(Graphic);
		}
		else if (Type == MoveRouteEnum.SetAnimation)
		{
			moveRouteAction.AnimationId = AnimationId;
		}
		return moveRouteAction;
	}
}
