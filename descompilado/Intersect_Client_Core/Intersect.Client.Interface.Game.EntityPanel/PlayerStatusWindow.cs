using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.General;

namespace Intersect.Client.Interface.Game.EntityPanel;

public class PlayerStatusWindow : ImagePanel
{
	private readonly Dictionary<Guid, SpellStatus> _activeStatuses = new Dictionary<Guid, SpellStatus>();

	public readonly ImagePanel _playerStatusWindow;

	public readonly ScrollControl _playerStatusControl;

	public Player? MyEntity;

	public bool ShouldUpdateStatuses { get; set; }

	public PlayerStatusWindow(Canvas gameCanvas, string? name = null)
		: base(gameCanvas, name ?? "PlayerStatusWindow")
	{
		_playerStatusControl = new ScrollControl(this, "PlayerStatusControl");
		MyEntity = Globals.Me;
		LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
	}

	public void Update()
	{
		Player? myEntity = MyEntity;
		if (myEntity == null || myEntity.IsDisposed)
		{
			if (!base.IsHidden)
			{
				Hide();
			}
		}
		else if (MyEntity.IsDisposed)
		{
			Dispose();
		}
		else if (ShouldUpdateStatuses)
		{
			SpellStatus.UpdateSpellStatus(MyEntity, _playerStatusControl, _activeStatuses);
			foreach (var (_, spellStatus2) in _activeStatuses)
			{
				spellStatus2.Update();
			}
			ShouldUpdateStatuses = _activeStatuses.Count > 0;
			if (!base.IsVisibleInTree)
			{
				base.IsVisibleInTree = ShouldUpdateStatuses;
			}
		}
		else if (base.IsVisibleInTree)
		{
			base.IsVisibleInTree = false;
		}
	}
}
