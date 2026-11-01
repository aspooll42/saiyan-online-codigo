using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core.Sounds;
using Intersect.Client.Framework.Audio;
using Intersect.Client.Framework.Core.Sounds;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.General;
using Intersect.Core;
using Intersect.Framework.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Core;

public static class Audio
{
	private static string sCurrentSong = string.Empty;

	private static int sFadeRate;

	private static long sFadeTimer;

	private static bool sFadingOut;

	private static readonly List<Sound> sGameSounds = new List<Sound>();

	private static bool sIsInitialized;

	private static int sQueuedFade;

	private static bool sQueuedLoop;

	private static string? sQueuedMusic = string.Empty;

	private static GameAudioInstance? _activeMusicSource;

	public static float Silence { get; set; } = 1f;

	public static void Init()
	{
		if (!sIsInitialized)
		{
			Globals.ContentManager.LoadAudio();
			sIsInitialized = true;
		}
	}

	public static void UpdateGlobalVolume()
	{
		_activeMusicSource?.Update();
		for (int i = 0; i < sGameSounds.Count; i++)
		{
			sGameSounds[i].Update();
			if (!sGameSounds[i].Loaded)
			{
				sGameSounds.RemoveAt(i);
			}
		}
	}

	public static void Update()
	{
		if (_activeMusicSource != null)
		{
			_activeMusicSource.Update();
			long millisecondsUtc = Timing.Global.MillisecondsUtc;
			if (sFadeTimer != 0L && sFadeTimer < millisecondsUtc)
			{
				if (sFadingOut)
				{
					if (--_activeMusicSource.Volume < 1)
					{
						StopMusic();
						if (!string.IsNullOrEmpty(sQueuedMusic))
						{
							PlayMusic(sQueuedMusic, 0, sQueuedFade, sQueuedLoop);
						}
						else
						{
							sFadeTimer = millisecondsUtc + sFadeRate;
						}
					}
					else
					{
						sFadeTimer = millisecondsUtc + sFadeRate;
					}
				}
				else if (++_activeMusicSource.Volume < 100)
				{
					sFadeTimer = millisecondsUtc + sFadeRate;
				}
				else
				{
					sFadeTimer = 0L;
				}
			}
		}
		for (int i = 0; i < sGameSounds.Count; i++)
		{
			sGameSounds[i].Update();
			if (!sGameSounds[i].Loaded)
			{
				sGameSounds.RemoveAt(i);
			}
		}
		Globals.ContentManager.SoundPacks?.UpdateCache();
		Globals.ContentManager.MusicPacks?.UpdateCache();
	}

	public static void PlayMusic(string filename, int fadeout = 0, int fadein = 0, bool loop = false)
	{
		if (string.IsNullOrWhiteSpace(filename))
		{
			StopMusic(fadeout);
			return;
		}
		ClearQueue();
		filename = GameContentManager.RemoveExtension(filename);
		if (_activeMusicSource != null)
		{
			if (fadeout == 0 || _activeMusicSource.State == GameAudioInstance.AudioInstanceState.Stopped || _activeMusicSource.State == GameAudioInstance.AudioInstanceState.Paused || _activeMusicSource.Volume < 1)
			{
				StopMusic();
				StartMusic(filename, fadein, loop);
			}
			else if (!string.Equals(sCurrentSong, filename, StringComparison.CurrentCultureIgnoreCase) || sFadingOut)
			{
				StopMusic(fadeout);
				sQueuedMusic = filename;
				sQueuedFade = fadein;
				sQueuedLoop = loop;
			}
		}
		else
		{
			StartMusic(filename, fadein, loop);
		}
	}

	private static void ClearQueue()
	{
		sQueuedMusic = null;
		sQueuedLoop = false;
		sQueuedFade = -1;
	}

	private static void StartMusic(string filename, int fadein = 0, bool loop = false)
	{
		GameAudioSource music = Globals.ContentManager.GetMusic(filename);
		if (music != null)
		{
			if (_activeMusicSource != null)
			{
				ApplicationContext.Context.Value?.Logger.LogTrace($"Trying to start '{filename}' without properly closing '{sCurrentSong}'.");
			}
			_activeMusicSource = music.CreateInstance();
			sCurrentSong = filename;
			_activeMusicSource.Play();
			_activeMusicSource.Volume = 0;
			_activeMusicSource.IsLooping = loop;
			sFadeRate = fadein / 100;
			sFadeTimer = Timing.Global.MillisecondsUtc + sFadeRate;
			sFadingOut = false;
		}
	}

	public static void StopMusic(int fadeout = 0)
	{
		if (_activeMusicSource != null)
		{
			if (fadeout == 0 || _activeMusicSource.State == GameAudioInstance.AudioInstanceState.Stopped || _activeMusicSource.State == GameAudioInstance.AudioInstanceState.Paused || _activeMusicSource.Volume < 1)
			{
				sCurrentSong = string.Empty;
				_activeMusicSource.Stop();
				_activeMusicSource.Dispose();
				_activeMusicSource = null;
				sFadeTimer = 0L;
			}
			else
			{
				sFadeRate = fadeout / Math.Max(1, _activeMusicSource.Volume);
				sFadeTimer = Timing.Global.MillisecondsUtc + sFadeRate;
				sFadingOut = true;
			}
		}
	}

	public static MapSound? AddMapSound(string filename, int x, int y, Guid mapId, bool loop, int loopInterval, int distance, IEntity? parent = null)
	{
		List<Sound> list = sGameSounds;
		if (list != null && list.Count > 128)
		{
			return null;
		}
		MapSound mapSound = new MapSound(filename, x, y, mapId, loop, loopInterval, distance, parent);
		sGameSounds?.Add(mapSound);
		return mapSound;
	}

	public static Sound? AddGameSound(string filename, bool loop)
	{
		List<Sound> list = sGameSounds;
		if (list != null && list.Count > 128)
		{
			return null;
		}
		Sound sound = new Sound(filename, loop, 0);
		sGameSounds?.Add(sound);
		return sound;
	}

	public static void StopSound(IMapSound sound)
	{
		sound?.Stop();
	}

	public static void StopAllGameSoundsOf(string[] filenames)
	{
		IEnumerable<Sound> enumerable = sGameSounds.Where((Sound s) => filenames.Contains<string>(s.Filename));
		foreach (Sound item in enumerable)
		{
			item.Stop();
		}
	}

	public static void StopAllSounds()
	{
		for (int i = 0; i < sGameSounds.Count; i++)
		{
			sGameSounds[i]?.Stop();
		}
	}

	internal static bool AjouterSonMeteo(Sound son)
	{
		if (sGameSounds.Count > 128)
		{
			return false;
		}
		sGameSounds.Add(son);
		return true;
	}
}
