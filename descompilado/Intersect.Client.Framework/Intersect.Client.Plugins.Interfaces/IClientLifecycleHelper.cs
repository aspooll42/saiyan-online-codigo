using System;
using System.Collections.Generic;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Interface;
using Intersect.Plugins.Interfaces;

namespace Intersect.Client.Plugins.Interfaces;

public interface IClientLifecycleHelper : ILifecycleHelper
{
	IMutableInterface Interface { get; }

	event GameUpdateHandler? GameUpdate;

	event GameDrawHandler? GameDraw;

	event LifecycleChangeStateHandler? LifecycleChangedState;

	event LifecycleChangeStateHandler? LifecycleChangingState;

	void EmitLifecycleChangedState(GameStates state);

	void EmitLifecycleChangingState(GameStates state);

	void EmitGameUpdate(GameStates state, IPlayer player, Dictionary<Guid, IEntity> knownEntities, TimeSpan deltaTime);

	void EmitGameDraw(DrawStates state, TimeSpan deltaTime);

	void EmitGameDraw(DrawStates state, IEntity entity, TimeSpan deltaTime);
}
