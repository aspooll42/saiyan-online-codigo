using System;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Maps;
using Intersect.Client.Networking;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.Entities;
using Intersect.Network.Packets.Server;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Entities;

public class Dash : IDash
{
	private const long MinSlideMilliseconds = 90L;

	private readonly Direction mChangeDirection = Direction.None;

	private readonly long _serverDashEndMilliseconds;

	private readonly long _dashLengthMilliseconds;

	private long _dashEndMilliseconds;

	private long _slideMilliseconds = 1L;

	private readonly Guid mEndMapId;

	private readonly byte mEndX;

	private float mEndXCoord;

	private readonly byte mEndY;

	private float mEndYCoord;

	private long mStartTime;

	private float mStartXCoord;

	private float mStartYCoord;

	private readonly float _finDecalageX;

	private readonly float _finDecalageY;

	private double _debutPrecis;

	private long _retenueEnFile;

	public float OffsetX => GetXOffset();

	public float OffsetY => GetYOffset();

	public bool IsKnockback
	{
		get
		{
			if (mChangeDirection == Direction.None)
			{
				return Nature == NatureDeGlissade.Ordinaire;
			}
			return false;
		}
	}

	public NatureDeGlissade Nature { get; }

	public Guid AuteurId { get; }

	public bool ContreLeDecor { get; init; }

	public (float X, float Y) StartOffset => (X: mStartXCoord, Y: mStartYCoord);

	public (float X, float Y) EndOffset => (X: mEndXCoord, Y: mEndYCoord);

	public long StartedAt => mStartTime;

	public long EndsAt => _dashEndMilliseconds;

	internal long FinAuServeur => _serverDashEndMilliseconds;

	internal byte PartHeritage { get; init; }

	internal MatiereDArret ArretHeritage { get; init; }

	internal int DegatsDArretHeritage { get; init; }

	internal FormeDeLancer FormeHeritage { get; init; }

	internal long RetenueEnFile => _retenueEnFile;

	public Dash(Guid endMapId, byte endX, byte endY, long dashEndMilliseconds, long dashLengthMilliseconds, Direction changeDirection = Direction.None, float finDecalageX = 0f, float finDecalageY = 0f, NatureDeGlissade nature = NatureDeGlissade.Ordinaire, Guid auteurId = default(Guid))
	{
		Nature = nature;
		AuteurId = auteurId;
		mChangeDirection = changeDirection;
		mEndMapId = endMapId;
		mEndX = endX;
		mEndY = endY;
		_serverDashEndMilliseconds = dashEndMilliseconds;
		_dashLengthMilliseconds = dashLengthMilliseconds;
		_finDecalageX = finDecalageX;
		_finDecalageY = finDecalageY;
	}

	public void Start(Entity entity)
	{
		long milliseconds = Timing.Global.Milliseconds;
		if (Nature != NatureDeGlissade.Chute && mEndMapId == entity.MapId && mEndX == entity.X && mEndY == entity.Y && entity.OffsetX == _finDecalageX && entity.OffsetY == _finDecalageY)
		{
			Finish(entity);
			return;
		}
		if (!MapInstance.TryGet(entity.MapId, out MapInstance instance) || !MapInstance.TryGet(mEndMapId, out MapInstance instance2))
		{
			entity.OffsetX = _finDecalageX;
			entity.OffsetY = _finDecalageY;
			entity.MapId = mEndMapId;
			entity.X = mEndX;
			entity.Y = mEndY;
			Finish(entity);
			return;
		}
		mStartTime = milliseconds;
		_debutPrecis = DeplacementLibre.MaintenantPrecis;
		_slideMilliseconds = Math.Clamp(_serverDashEndMilliseconds - milliseconds, 90L, Math.Max(90L, _dashLengthMilliseconds));
		_dashEndMilliseconds = mStartTime + _slideMilliseconds;
		if (Heritage.Actif && _retenueEnFile > 0)
		{
			RecalerApresRetenue(milliseconds);
		}
		ApplicationContext.CurrentContext.Logger.LogDebug("Starting dash that is {DashLength}ms long, and there are {RemainingTime}ms before it is completed on the server", _slideMilliseconds, _serverDashEndMilliseconds - milliseconds);
		mStartXCoord = entity.OffsetX;
		mStartYCoord = entity.OffsetY;
		mEndXCoord = (float)(instance2.X + mEndX * Options.Instance.Map.TileWidth) + _finDecalageX - (float)(instance.X + entity.X * Options.Instance.Map.TileWidth);
		mEndYCoord = (float)(instance2.Y + mEndY * Options.Instance.Map.TileHeight) + _finDecalageY - (float)(instance.Y + entity.Y * Options.Instance.Map.TileHeight);
		if (Heritage.Actif)
		{
			Heritage.Garde.Talons(entity, Nature, mEndXCoord - mStartXCoord, mEndYCoord - mStartYCoord, _slideMilliseconds);
		}
		if (mChangeDirection > Direction.None)
		{
			entity.DirectionFacing = mChangeDirection;
		}
	}

	private float Progress()
	{
		float num = Math.Clamp((float)((DeplacementLibre.MaintenantPrecis - _debutPrecis) / (double)Math.Max(1L, _slideMilliseconds)), 0f, 1f);
		if (Heritage.Actif)
		{
			byte partHeritage = PartHeritage;
			if (partHeritage > 0 && partHeritage < byte.MaxValue)
			{
				return Heritage.Decor.Progression(num, PartHeritage);
			}
		}
		float num2 = 1f - (1f - num) * (1f - num);
		if (!ContreLeDecor)
		{
			return num2;
		}
		return 0.55f * num2 + 0.45f * num;
	}

	public float GetXOffset()
	{
		return mStartXCoord + (mEndXCoord - mStartXCoord) * Progress();
	}

	public float GetYOffset()
	{
		return mStartYCoord + (mEndYCoord - mStartYCoord) * Progress();
	}

	public void Delay(long milliseconds)
	{
		mStartTime += milliseconds;
		_debutPrecis += milliseconds;
		_dashEndMilliseconds += milliseconds;
	}

	public bool Update(Entity entity)
	{
		if (DeplacementLibre.MaintenantPrecis - _debutPrecis <= (double)_slideMilliseconds)
		{
			return entity.Dashing != null;
		}
		ApplicationContext.CurrentContext.Logger.LogDebug("Dash finished");
		entity.OffsetX = _finDecalageX;
		entity.OffsetY = _finDecalageY;
		entity.MapId = mEndMapId;
		entity.X = mEndX;
		entity.Y = mEndY;
		Finish(entity);
		return false;
	}

	private static void Finish(Entity entity)
	{
		entity.Dashing = null;
		EntityMovePacket pendingDashMove = entity.PendingDashMove;
		entity.PendingDashMove = null;
		if (pendingDashMove != null)
		{
			PacketHandler packetHandler = Intersect.Client.Networking.Network.PacketHandler;
			packetHandler?.HandlePacket(packetHandler.VirtualSender, pendingDashMove);
		}
	}

	internal void RetenirEnFile(long millisecondes)
	{
		if (millisecondes > 0 && IsKnockback && (FormeHeritage & FormeDeLancer.DeLHeritage) == 0)
		{
			_retenueEnFile += millisecondes;
		}
	}

	internal static long DureeApresRetenue(long finAuServeur, long retenue, long maintenant, long longueur)
	{
		return Math.Clamp(finAuServeur + Math.Max(0L, retenue) - maintenant, 90L, Math.Max(90L, longueur));
	}

	internal void RecalerApresRetenue(long maintenant)
	{
		_slideMilliseconds = DureeApresRetenue(_serverDashEndMilliseconds, _retenueEnFile, maintenant, _dashLengthMilliseconds);
		_dashEndMilliseconds = mStartTime + _slideMilliseconds;
	}
}
