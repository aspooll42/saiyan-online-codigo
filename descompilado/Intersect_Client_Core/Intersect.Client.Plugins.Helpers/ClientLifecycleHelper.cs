using System;
using System.Collections.Generic;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Interface;
using Intersect.Client.Plugins.Interfaces;
using Intersect.Plugins.Helpers;
using Intersect.Plugins.Interfaces;

namespace Intersect.Client.Plugins.Helpers;

internal sealed class ClientLifecycleHelper : ContextHelper<IClientPluginContext>, IClientLifecycleHelper, ILifecycleHelper
{
	public IMutableInterface Interface => Intersect.Client.Interface.Interface.CurrentInterface;

	public event GameUpdateHandler? GameUpdate;

	public event GameDrawHandler? GameDraw;

	public event LifecycleChangeStateHandler? LifecycleChangedState;

	public event LifecycleChangeStateHandler? LifecycleChangingState;

	internal ClientLifecycleHelper(IClientPluginContext context)
		: base(context)
	{
		Globals.ClientLifecycleHelpers.Add(this);
	}

	~ClientLifecycleHelper()
	{
		Globals.ClientLifecycleHelpers.Remove(this);
	}

	public void EmitLifecycleChangedState(GameStates state)
	{
		LifecycleChangeStateArgs lifecycleChangeStateArgs = new LifecycleChangeStateArgs(state);
		this.LifecycleChangedState?.Invoke(base.Context, lifecycleChangeStateArgs);
	}

	public void EmitLifecycleChangingState(GameStates state)
	{
		LifecycleChangeStateArgs lifecycleChangeStateArgs = new LifecycleChangeStateArgs(state);
		this.LifecycleChangingState?.Invoke(base.Context, lifecycleChangeStateArgs);
	}

	public void EmitGameUpdate(GameStates state, IPlayer player, Dictionary<Guid, IEntity> knownEntities, TimeSpan deltaTime)
	{
		GameUpdateArgs gameUpdateArgs = new GameUpdateArgs(state, player, knownEntities, deltaTime);
		this.GameUpdate?.Invoke(base.Context, gameUpdateArgs);
	}

	public void EmitGameDraw(DrawStates state, TimeSpan deltaTime)
	{
		GameDrawArgs drawGameArgs = new GameDrawArgs(state, deltaTime);
		this.GameDraw?.Invoke(base.Context, drawGameArgs);
	}

	public void EmitGameDraw(DrawStates state, IEntity entity, TimeSpan deltaTime)
	{
		GameDrawArgs drawGameArgs = new GameDrawArgs(state, entity, deltaTime);
		this.GameDraw?.Invoke(base.Context, drawGameArgs);
	}
}
