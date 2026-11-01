using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Configuration;
using Intersect.Core;
using Intersect.Framework;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Models;
using Intersect.Network;
using Intersect.Network.Packets.Client;
using Intersect.Network.Packets.Server;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Maps;

internal static class ChargementDesCartes
{
	private sealed class Travail
	{
		public required Task<Action?> Tache { get; init; }

		public required bool Bloquant { get; init; }
	}

	private const double BudgetParImageMs = 4.0;

	private static readonly List<Travail> Travaux = new List<Travail>();

	public static bool Bloque
	{
		get
		{
			foreach (Travail item in Travaux)
			{
				if (item.Bloquant)
				{
					return true;
				}
			}
			return false;
		}
	}

	public static bool SeLitAPart(object paquet)
	{
		if (paquet is MapPacket || paquet is MapAreaPacket)
		{
			return true;
		}
		return false;
	}

	public static void Vider()
	{
		Travaux.Clear();
	}

	public static void Installer()
	{
		if (Travaux.Count == 0)
		{
			return;
		}
		double maintenant = ChronoImage.Maintenant;
		int num = 0;
		int num2 = 0;
		while (num2 < Travaux.Count)
		{
			Travail travail = Travaux[num2];
			if (!travail.Tache.IsCompleted)
			{
				if (travail.Bloquant)
				{
					break;
				}
				num2++;
				continue;
			}
			if (num > 0 && ChronoImage.Maintenant - maintenant > 4.0)
			{
				break;
			}
			Travaux.RemoveAt(num2);
			num++;
			if (!travail.Tache.IsCompletedSuccessfully)
			{
				ApplicationContext.Context.Value?.Logger.LogError(travail.Tache.Exception, "Lecture d'une carte hors du fil du jeu : echec");
				continue;
			}
			try
			{
				travail.Tache.Result?.Invoke();
			}
			catch (Exception exception)
			{
				ApplicationContext.Context.Value?.Logger.LogError(exception, "Installation d'une carte : echec");
			}
		}
		if (num > 0)
		{
			ChronoImage.Ajouter("cartes (installation)", ChronoImage.Maintenant - maintenant);
		}
	}

	private static void Lancer(Func<Action?> lecture, bool bloquant)
	{
		Travaux.Add(new Travail
		{
			Tache = Task.Run(lecture),
			Bloquant = bloquant
		});
	}

	private static void AjouterPret(Action installation, bool bloquant)
	{
		Travaux.Add(new Travail
		{
			Tache = Task.FromResult(installation),
			Bloquant = bloquant
		});
	}

	public static void LireDuServeur(MapPacket paquet, bool chercherLesVoisines)
	{
		MapInstance.UpdateMapRequestTime(paquet.MapId);
		Lancer(delegate
		{
			RangerDansLeCache(paquet);
			MapInstance carte = Lire(paquet);
			return delegate
			{
				InstallerLaCarte(paquet, carte, depuisLeCache: false, chercherLesVoisines);
			};
		}, bloquant: true);
	}

	public static void LireDuCache(Guid[] identifiants)
	{
		Lancer(delegate
		{
			ObjectCacheKey<MapDescriptor>[] cles;
			List<(MapPacket Paquet, MapInstance Carte)> pretes;
			(cles, pretes) = LireLeCache(identifiants);
			return delegate
			{
				PacketSender.SendNeedMap(cles);
				foreach (var item in pretes)
				{
					var (paquet, carte) = item;
					AjouterPret(delegate
					{
						InstallerLaCarte(paquet, carte, depuisLeCache: true, chercherLesVoisines: false);
					}, bloquant: true);
				}
			};
		}, bloquant: true);
	}

	public static void DemanderLesVoisines(Guid[] identifiants)
	{
		MapInstance.UpdateMapRequestTime(identifiants);
		Lancer(delegate
		{
			ObjectCacheKey<MapDescriptor>[] cles;
			List<(MapPacket Paquet, MapInstance Carte)> pretes;
			(cles, pretes) = LireLeCache(identifiants);
			return delegate
			{
				MapInstance instance;
				ObjectCacheKey<MapDescriptor>[] array = cles.Where((ObjectCacheKey<MapDescriptor> cle) => !MapInstance.TryGet(cle.Id.Guid, out instance)).ToArray();
				if (array.Length != 0)
				{
					Intersect.Client.Networking.Network.SendPacket(new GetObjectData<MapDescriptor>(array));
					MapInstance.UpdateMapRequestTime(array.Select((ObjectCacheKey<MapDescriptor> cle) => cle.Id.Guid).ToArray());
				}
				foreach (var item in pretes)
				{
					var (paquet, carte) = item;
					AjouterPret(delegate
					{
						InstallerLaCarte(paquet, carte, depuisLeCache: true, chercherLesVoisines: false);
					}, bloquant: true);
				}
			};
		}, bloquant: false);
	}

	private static (ObjectCacheKey<MapDescriptor>[] Cles, List<(MapPacket Paquet, MapInstance Carte)> Pretes) LireLeCache(Guid[] identifiants)
	{
		List<ObjectCacheKey<MapDescriptor>> list = new List<ObjectCacheKey<MapDescriptor>>(identifiants.Length);
		List<(MapPacket, MapInstance)> list2 = new List<(MapPacket, MapInstance)>(identifiants.Length);
		foreach (Guid guid in identifiants)
		{
			if (ObjectDataDiskCache<MapDescriptor>.TryLoad(guid, out ObjectCacheData<MapDescriptor> data))
			{
				MapPacket mapPacket = MessagePacker.Instance.Deserialize<MapPacket>(data.Data, silent: true);
				MapInstance mapInstance = null;
				if (mapPacket != null)
				{
					try
					{
						mapInstance = Lire(mapPacket);
					}
					catch (Exception exception)
					{
						ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Carte {Id} du cache illisible, elle sera redemandee", guid);
					}
				}
				if (mapPacket != null && mapInstance != null)
				{
					list.Add(new ObjectCacheKey<MapDescriptor>(data.Id, data.Checksum, data.Version));
					list2.Add((mapPacket, mapInstance));
					continue;
				}
				ApplicationContext.Context.Value?.Logger.LogWarning("Failed to deserialized cached data for {Id}, will fetch again", guid);
			}
			list.Add(new ObjectCacheKey<MapDescriptor>(new Id<MapDescriptor>(guid)));
		}
		return (Cles: list.ToArray(), Pretes: list2);
	}

	private static void RangerDansLeCache(MapPacket paquet)
	{
		ApplicationContext.Context.Value?.Logger.LogDebug("Saving map {Id} @ ({GridX}, {GridY}) revision {Revision} version {Version} holds {CameraHolds}", paquet.MapId, paquet.GridX, paquet.GridY, paquet.Revision, paquet.CacheVersion, "[" + string.Join(", ", paquet.CameraHolds ?? Array.Empty<bool>()) + "]");
		ObjectCacheData<MapDescriptor> objectCacheData = new ObjectCacheData<MapDescriptor>
		{
			Id = new Id<MapDescriptor>(paquet.MapId),
			Data = ((IntersectPacket)paquet).Data,
			Version = paquet.CacheVersion
		};
		if (!ObjectDataDiskCache<MapDescriptor>.TrySave(objectCacheData))
		{
			ApplicationContext.Context.Value?.Logger.LogWarning("Failed to save cache for map {Id}", paquet.MapId);
		}
	}

	private static MapInstance Lire(MapPacket paquet)
	{
		MapInstance mapInstance = new MapInstance(paquet.MapId);
		lock (mapInstance.MapLock)
		{
			mapInstance.LireDonnees(paquet.Data);
			mapInstance.LoadTileData(paquet.TileData);
			mapInstance.AttributeData = paquet.AttributeData;
			return mapInstance;
		}
	}

	private static void InstallerLaCarte(MapPacket packet, MapInstance mapInstance, bool depuisLeCache, bool chercherLesVoisines)
	{
		Guid mapId = packet.MapId;
		MapInstance.UpdateMapRequestTime(mapId);
		if (MapInstance.TryGet(mapId, out MapInstance instance))
		{
			if (depuisLeCache)
			{
				return;
			}
			instance.Dispose(prep: false, killentities: false);
		}
		MapInstance.Lookup.Set(mapId, mapInstance);
		lock (mapInstance.MapLock)
		{
			mapInstance.FinirChargement();
			mapInstance.Name = Traduction.NomTraduit(mapId, mapInstance.Name);
			mapInstance.CreateMapSounds();
			Guid key = mapId;
			Guid? obj = Globals.Me?.MapId;
			if (key == obj)
			{
				Audio.PlayMusic(mapInstance.Music, ClientConfiguration.Instance.MusicFadeTimer, ClientConfiguration.Instance.MusicFadeTimer, loop: true);
			}
			if (!Globals.GridMaps.TryGetValue(packet.MapId, out var value))
			{
				ApplicationContext.Context.Value?.Logger.LogDebug("Falling back to packet position for map '{MapName}' ({MapId})", mapInstance.Name, mapInstance.Id);
				value = new Point(packet.GridX, packet.GridY);
			}
			mapInstance.GridX = value.X;
			mapInstance.GridY = value.Y;
			mapInstance.CameraHolds = packet.CameraHolds ?? new bool[4];
			ApplicationContext.Context.Value?.Logger.LogDebug("Loading map {Id} ({Name}) @ ({GridX}, {GridY}) revision {Revision} version {Version} holds {CameraHolds}", mapInstance.Id, mapInstance.Name, value.X, value.Y, mapInstance.Revision, packet.CacheVersion, "[" + string.Join(", ", mapInstance.CameraHolds) + "]");
			mapInstance.Autotiles.InitAutotiles(mapInstance.GenerateAutotileGrid());
			if (Globals.PendingEvents.TryGetValue(mapId, out Dictionary<Guid, EventEntityPacket> value2))
			{
				foreach (KeyValuePair<Guid, EventEntityPacket> item in value2)
				{
					item.Deconstruct(out key, out var value3);
					Guid evtId = key;
					EventEntityPacket packet2 = value3;
					mapInstance.AddEvent(evtId, packet2);
				}
				value2.Clear();
			}
		}
		mapInstance.MarkLoadFinished();
		if (chercherLesVoisines)
		{
			Player.FetchNewMaps();
		}
	}
}
