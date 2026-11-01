using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.General;
using Intersect.GameObjects;
using Intersect.Models;
using Intersect.Utilities;

namespace Intersect.Client.Interface.Game.EntityPanel;

public class SpellStatus
{
	private Guid _currentSpellId;

	private Intersect.Client.Framework.Gwen.Control.Label _durationLabel;

	private Status _status;

	private string _textureLoaded = string.Empty;

	private ImagePanel _statusIcon;

	private ImagePanel _spellStatusContainer;

	public SpellStatus(Status status)
	{
		_status = status;
	}

	public void Setup()
	{
		_statusIcon = new ImagePanel(_spellStatusContainer, "StatusIcon");
		_statusIcon.HoverEnter += pnl_HoverEnter;
		_statusIcon.HoverLeave += pnl_HoverLeave;
		_durationLabel = new Intersect.Client.Framework.Gwen.Control.Label(_spellStatusContainer, "DurationLabel");
	}

	public void pnl_HoverLeave(Base sender, EventArgs arguments)
	{
		Interface.GameUi.SpellDescriptionWindow?.Hide();
	}

	private void pnl_HoverEnter(Base sender, EventArgs arguments)
	{
		Interface.GameUi.SpellDescriptionWindow?.Show(_status.SpellId);
	}

	public void UpdateStatus(Status status)
	{
		_status = status;
	}

	public static void UpdateSpellStatus(IEntity myEntity, ScrollControl spellStatusControl, Dictionary<Guid, SpellStatus> activeStatuses)
	{
		if (!(myEntity is Entity entity))
		{
			return;
		}
		Guid[] array = activeStatuses.Keys.ToArray();
		Guid[] array2 = array;
		foreach (Guid guid in array2)
		{
			if (!entity.StatusActive(guid))
			{
				SpellStatus spellStatus = activeStatuses[guid];
				spellStatus._statusIcon.Texture = null;
				spellStatus._spellStatusContainer.Hide();
				spellStatus._spellStatusContainer.Texture = null;
				spellStatusControl.RemoveChild(spellStatus._spellStatusContainer, dispose: true);
				spellStatus.pnl_HoverLeave(null, null);
				activeStatuses.Remove(guid);
			}
			else
			{
				activeStatuses[guid].UpdateStatus(entity.GetStatus(guid) as Status);
			}
		}
		for (int j = 0; j < myEntity.Status.Count; j++)
		{
			Guid spellId = myEntity.Status[j].SpellId;
			SpellStatus spellStatus2;
			if (!activeStatuses.ContainsKey(spellId) && myEntity.Status[j] is Status status)
			{
				spellStatus2 = new SpellStatus(status)
				{
					_spellStatusContainer = new ImagePanel(spellStatusControl, "SpellStatusIcon")
				};
				spellStatus2.Setup();
				spellStatus2._spellStatusContainer.LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
				spellStatus2._spellStatusContainer.Name = string.Empty;
				activeStatuses.Add(spellId, spellStatus2);
			}
			else
			{
				spellStatus2 = activeStatuses[spellId];
			}
			int num = spellStatus2._spellStatusContainer.Margin.Left + spellStatus2._spellStatusContainer.Margin.Right;
			int num2 = spellStatus2._spellStatusContainer.Margin.Top + spellStatus2._spellStatusContainer.Margin.Bottom;
			int val = spellStatusControl.Width / Math.Max(1, spellStatus2._spellStatusContainer.Width + num);
			int num3 = j / Math.Max(1, val);
			int num4 = j % Math.Max(1, val);
			int x = num4 * (spellStatus2._spellStatusContainer.Width + num) + num;
			int y = num3 * (spellStatus2._spellStatusContainer.Height + num2) + num2;
			spellStatus2._spellStatusContainer.SetPosition(x, y);
		}
	}

	public void Update()
	{
		if (_status == null)
		{
			return;
		}
		long remainingMs = _status.RemainingMs;
		SpellDescriptor spellDescriptor = DatabaseObject<SpellDescriptor>.Get(_status.SpellId);
		_durationLabel.Text = TimeSpan.FromMilliseconds(remainingMs).WithSuffix();
		if (((_textureLoaded != "" && spellDescriptor == null) || (spellDescriptor != null && _textureLoaded != spellDescriptor.Icon) || _currentSpellId != _status.SpellId) && remainingMs > 0)
		{
			_spellStatusContainer.Show();
			if (spellDescriptor != null)
			{
				IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Spell, spellDescriptor.Icon);
				if (texture != null)
				{
					_statusIcon.Texture = texture;
					_statusIcon.IsHidden = false;
				}
				else if (_statusIcon.Texture != null)
				{
					_statusIcon.Texture = null;
				}
				_textureLoaded = spellDescriptor.Icon;
				_currentSpellId = _status.SpellId;
			}
			else
			{
				if (_statusIcon.Texture != null)
				{
					_statusIcon.Texture = null;
				}
				_textureLoaded = string.Empty;
			}
		}
		else if (remainingMs <= 0)
		{
			if (_statusIcon.Texture != null)
			{
				_statusIcon.Texture = null;
			}
			_spellStatusContainer.Hide();
			_textureLoaded = string.Empty;
		}
	}
}
