using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Core.Controls;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Config;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Entities;

public static class Vanish
{
	private const long FlickerMs = 40L;

	private static readonly Dictionary<Guid, long> Windows = new Dictionary<Guid, long>();

	private static readonly Color Cyan = new Color(255, 150, 225, 255);

	private static long _readyAt;

	private static bool _heldLastFrame;

	private static long _figeJusqua;

	private static int _maximumConnu = -1;

	private static long _esquiveAuNiveauDitA;

	private static Guid _moiConnu;

	private static int _jetons = -1;

	private static long _jetonProchainA;

	public static long NouvelleChargeA { get; private set; } = -4611686018427387904L;

	public static long RefuseeA { get; private set; } = -4611686018427387904L;

	public static void UpdateInput(Player player)
	{
		if (player != Globals.Me || !Options.Instance.Combat.EnableVanish)
		{
			return;
		}
		bool flag = Controls.IsGameControlPressed(Control.Dodge);
		bool flag2 = flag && !_heldLastFrame;
		_heldLastFrame = flag;
		if (!flag2)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		if (Echanges.EstProjetee(player) || (Options.Instance.Combat.EsquiveJetons > 0 && (Echanges.EstSonne(player) || milliseconds < player.GuardStunnedUntil)))
		{
			PacketSender.SendVanishRequest(0, 0);
			return;
		}
		CombatOptions combat = Options.Instance.Combat;
		if (combat.EsquiveJetons > 0 && combat.JetonsDEsquivePourNiveau(player.Level) <= 0)
		{
			if (milliseconds - _esquiveAuNiveauDitA > 2000)
			{
				_esquiveAuNiveauDitA = milliseconds;
				CombatFeedback.Callout(player.Id, Strings.Vanish.EsquiveAuNiveau.ToString(combat.NiveauDeLEsquive), Cyan, 12);
			}
			return;
		}
		bool flag3 = Options.Instance.Combat.EsquiveJetons <= 0 && Echanges.PoursuitePossible();
		if ((milliseconds >= _readyAt || flag3) && !player.IsBusy && !player.IsDying && !player.IsBlocking && !player.IsCharging && (!player.IsBeaming || (Heritage.Actif && Heritage.Rayon.XAnnuleLaCharge(player))) && !player.IsCasting && !player.IsTransforming && milliseconds >= player.GuardStunnedUntil)
		{
			player.PoserSaPositionAvantDeSeFiger();
			Echanges.AbandonnerLaCharge(player);
			if (!flag3)
			{
				_readyAt = milliseconds + ((Options.Instance.Combat.EsquiveJetons <= 0) ? Math.Max(0, Options.Instance.Combat.VanishCooldown) : 0);
				_figeJusqua = milliseconds + Math.Max(0, Options.Instance.Combat.VanishWindow) + 150;
			}
			var (sensX, sensY) = player.DirectionTenuePourLeBond();
			PacketSender.SendVanishRequest(sensX, sensY);
		}
	}

	public static bool EstFige(Entity entity)
	{
		if (entity == Globals.Me)
		{
			return Timing.Global.Milliseconds < _figeJusqua;
		}
		return false;
	}

	public static void OnPacket(VanishPacket packet)
	{
		long milliseconds = Timing.Global.Milliseconds;
		Guid entityId = packet.EntityId;
		Guid? guid = Globals.Me?.Id;
		if (guid.HasValue && entityId == guid.GetValueOrDefault() && packet.Event != VanishEvent.Evaded)
		{
			_readyAt = milliseconds + packet.CooldownMs;
		}
		entityId = packet.EntityId;
		Guid? obj = Globals.Me?.Id;
		bool flag = entityId == obj;
		bool flag2 = flag;
		if (flag2)
		{
			VanishEvent vanishEvent = packet.Event;
			bool flag3 = ((vanishEvent - 1 <= VanishEvent.Evaded || vanishEvent == VanishEvent.Jetons) ? true : false);
			flag2 = flag3;
		}
		if (flag2)
		{
			_figeJusqua = 0L;
		}
		entityId = packet.EntityId;
		guid = Globals.Me?.Id;
		if (guid.HasValue && entityId == guid.GetValueOrDefault() && packet.Event == VanishEvent.Refused)
		{
			RefuseeA = milliseconds;
		}
		entityId = packet.EntityId;
		guid = Globals.Me?.Id;
		if (guid.HasValue && entityId == guid.GetValueOrDefault() && packet.Jetons != byte.MaxValue)
		{
			_jetons = packet.Jetons;
			_jetonProchainA = ((packet.JetonProchainMs > 0) ? (milliseconds + packet.JetonProchainMs) : 0);
		}
		if (Globals.Entities.TryGetValue(packet.EntityId, out Entity value) && value != null)
		{
			switch (packet.Event)
			{
			case VanishEvent.Started:
				Windows[value.Id] = milliseconds + packet.WindowMs;
				break;
			case VanishEvent.Refused:
				Windows.Remove(value.Id);
				break;
			case VanishEvent.Evaded:
				Windows.Remove(value.Id);
				Evade(value, packet);
				break;
			}
		}
	}

	public static bool Jetons(long maintenant, out int jetons, out int maximum, out float recharge)
	{
		Player me = Globals.Me;
		maximum = Options.Instance.Combat.JetonsDEsquivePourNiveau(me?.Level ?? 1);
		recharge = 0f;
		jetons = maximum;
		if (Options.Instance.Combat.EsquiveJetons <= 0)
		{
			return false;
		}
		if (me != null && me.Id != _moiConnu)
		{
			_moiConnu = me.Id;
			_maximumConnu = -1;
			_jetons = -1;
			_jetonProchainA = 0L;
		}
		if (_maximumConnu >= 0 && maximum > _maximumConnu)
		{
			if (_jetons >= 0)
			{
				_jetons += maximum - _maximumConnu;
			}
			NouvelleChargeA = maintenant;
			if (me != null)
			{
				string text = ((_maximumConnu == 0) ? Strings.Vanish.EsquiveDebloquee.ToString() : Strings.Vanish.NouvelleCharge.ToString(maximum));
				CombatFeedback.Callout(me.Id, text, Cyan, 13);
			}
		}
		_maximumConnu = maximum;
		if (maximum <= 0)
		{
			return false;
		}
		if (_jetons < 0)
		{
			return true;
		}
		int num = Math.Max(100, Options.Instance.Combat.EsquiveJetonRechargeMs);
		jetons = Math.Min(_jetons, maximum);
		long num2 = _jetonProchainA;
		while (jetons < maximum && num2 != 0L && maintenant >= num2)
		{
			jetons++;
			num2 = ((jetons < maximum) ? (num2 + num) : 0);
		}
		if (jetons < maximum && num2 != 0L)
		{
			recharge = Math.Clamp(1f - (float)(num2 - maintenant) / (float)num, 0f, 1f);
		}
		return true;
	}

	public static byte Flicker(Entity entity, byte alpha)
	{
		if (Windows.Count == 0 || !Windows.TryGetValue(entity.Id, out var value))
		{
			return alpha;
		}
		long milliseconds = Timing.Global.Milliseconds;
		if (milliseconds >= value)
		{
			Windows.Remove(entity.Id);
			return alpha;
		}
		if (milliseconds / 40 % 2 != 0L)
		{
			return alpha;
		}
		return (byte)((float)(int)alpha * 0.5f);
	}

	private static void Evade(Entity entity, VanishPacket packet)
	{
		int num = (packet.ToX - packet.FromX) * Options.Instance.Map.TileWidth;
		int num2 = (packet.ToY - packet.FromY) * Options.Instance.Map.TileHeight;
		Afterimages.Vanish(entity, num, num2);
		Audio.AddMapSound("Esquive.wav", entity.X, entity.Y, entity.MapId, loop: false, 0, 12, entity);
		entity.DashQueue.Clear();
		entity.Dashing = null;
		entity.PendingDashMove = null;
		entity.DashTimer = 0L;
		entity.IsMoving = false;
		entity.OffsetX = 0f;
		entity.OffsetY = 0f;
		entity.X = packet.ToX;
		entity.Y = packet.ToY;
		entity.DirectionFacing = (Direction)packet.Direction;
		if (Heritage.Actif)
		{
			entity.RecalculerLOrigine();
		}
		CombatFeedback.CancelPredictedStrike(entity.Id);
		CombatFeedback.Callout(entity.Id, Strings.Vanish.Evaded, Cyan, 12);
		Player me = Globals.Me;
		if (me != null && (entity == me || packet.AttackerId == me.Id))
		{
			CombatFeedback.HitStop(45L);
			CombatFeedback.PunchZoom(0.03f, 180L);
		}
	}
}
