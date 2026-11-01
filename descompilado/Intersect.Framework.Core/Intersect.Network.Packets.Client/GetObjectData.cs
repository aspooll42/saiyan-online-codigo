using System;
using System.Collections.Generic;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Models;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[GenericPacketTypeArguments(new Type[] { typeof(MapDescriptor) })]
[MessagePackObject(false)]
public class GetObjectData<TGameObject> : IntersectPacket
{
	[Key(0)]
	public List<ObjectCacheKey<TGameObject>> CacheKeys { get; set; }

	public GetObjectData()
	{
	}

	public GetObjectData(params ObjectCacheKey<TGameObject>[] cacheKeys)
	{
		CacheKeys = new List<ObjectCacheKey<TGameObject>>(cacheKeys);
	}
}
